using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Threading;
using EPS.Utility;
using Ploeh.AutoFixture;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	[SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "Non-issue for test classes really")]
	public abstract class MonitoredJobQueueTestBase<TMonitoredQueue, TInput, TOutput, TPoison>
		where TMonitoredQueue : IMonitoredJobQueue<TInput, TOutput, TPoison>
	{
		public class TransientJobQueueFactory
			: IDurableJobQueueFactory
		{
			public IDurableJobQueue<TQueueInput, TQueuePoison> CreateDurableJobQueue<TQueueInput, TQueuePoison>()
			{
				return new TransientJobQueue<TQueueInput, TQueuePoison>(GenericEqualityComparer<TQueueInput>.ByAllMembers(), GenericEqualityComparer<TQueuePoison>.ByAllMembers());
			}
		}

		private readonly Fixture _fixture = new Fixture();
		private readonly Func<IScheduler, IDurableJobQueueFactory, IMonitoredJobQueue<TInput, TOutput, TPoison>> _monitoredJobQueueFactory;
		private readonly TransientJobQueueFactory _durableQueueFactory = new TransientJobQueueFactory();

		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs")]
		protected MonitoredJobQueueTestBase(Func<IScheduler, IDurableJobQueueFactory, IMonitoredJobQueue<TInput, TOutput, TPoison>> monitoredJobQueueFactory)
		{
			this._monitoredJobQueueFactory = monitoredJobQueueFactory;
		}
		
		[Fact]
		public void AddJob_DoesNotThrow()
		{
			var queue = _monitoredJobQueueFactory(new HistoricalScheduler(), _durableQueueFactory);
			Assert.DoesNotThrow(() => queue.AddJob(_fixture.CreateAnonymous<TInput>()));
		}

		//there's really only one thing we care about here -- that a job passes from input -> durable queue -> monitor -> job execution queue -> job result journaler
		/*
		[Fact]
		public void AddJob_Calls_Queue()
		{
			HistoricalScheduler scheduler = new HistoricalScheduler();
			var monitoredQueue = monitoredJobQueueFactory(new HistoricalScheduler(), durableQueue);
			TInput input = fixture.CreateAnonymous<TInput>();
			monitoredQueue.AddJob(input);
			scheduler.AdvanceBy(TimeSpan.FromSeconds(5));

			A.CallTo(() => durableQueue.Queue(A<TInput>.That.IsEqualTo(input))).MustHaveHappened(Repeated.Exactly.Once);
		}
		*/

		class CallCount
		{
			public IMonitoredJobQueue<TInput, TOutput, TPoison> MonitoredQueue { get; set; }
			public int Actual { get; set; }
		}

		private CallCount GetOnQueueActionCallCount(DurableJobQueueActionType filter, int maxConcurrent, int jobsToCreate)
		{
			var scheduler = new HistoricalScheduler();
			var queuedEvent = new ManualResetEventSlim(false);
			var callCount = new CallCount() 
			{
				MonitoredQueue = _monitoredJobQueueFactory(scheduler, _durableQueueFactory),
			};

			callCount.MonitoredQueue.MaxConcurrent = maxConcurrent;
			using (var subscription = callCount.MonitoredQueue.OnQueueAction
			.Subscribe(action =>{
				if (action.ActionType == filter)
				{
					++callCount.Actual;
					if (callCount.Actual == jobsToCreate)
						queuedEvent.Set();

					//else if (callCount.Actual == callCount.MonitoredQueue.MaxQueueItemsToPublishPerInterval)
					//	queuedEvent.Set();
				}}))
			{
				foreach (var input in _fixture.CreateMany<TInput>(jobsToCreate))
					callCount.MonitoredQueue.AddJob(input);

				scheduler.AdvanceBy(callCount.MonitoredQueue.PollingInterval.Add(TimeSpan.FromSeconds(1)));

				queuedEvent.Wait(TimeSpan.FromSeconds(20));

				return callCount;
			}
		}

		[Fact]
		public void MonitoredQueue_MoveItemsThrough_Queued_State()
		{
			var jobs = 10;
			var calls = GetOnQueueActionCallCount(DurableJobQueueActionType.Queued, 5, jobs);
			Assert.Equal(jobs, calls.Actual);
			calls.MonitoredQueue.Dispose();
		}

		[Fact]
		public void MonitoredQueue_MoveItemsThrough_Pending_State()
		{
			var jobs = 10;
			var calls = GetOnQueueActionCallCount(DurableJobQueueActionType.Pending, 5, jobs);
			Assert.Equal(jobs, calls.Actual);
			calls.MonitoredQueue.Dispose();
		}

		[Fact]
		public void MonitoredQueue_MoveItemsThrough_Completed_State()
		{
			var jobs = 10;
			var calls = GetOnQueueActionCallCount(DurableJobQueueActionType.Completed, 5, jobs);
			Assert.Equal(jobs, calls.Actual);
			calls.MonitoredQueue.Dispose();
		}

		class ExecutionInfo
		{
			public IMonitoredJobQueue<TInput, TOutput, TPoison> JobQueue { get; set; }
			public int CallsMade { get; set; }
			public TInput[] Inputs { get; set; }
		}

		/*
		private ExecutionInfo GetNextQueuedItem_CallCount()
		{
			HistoricalScheduler scheduler = new HistoricalScheduler();
			var executionInfo = new ExecutionInfo()
			{
				JobQueue = monitoredJobQueueFactory(new HistoricalScheduler(), durableQueue),
				Inputs = fixture.CreateMany<TInput>(100).ToArray()
			};

			int callsMade = 0;
			A.CallTo(() => durableQueue.NextQueuedItem())
				.Invokes(call => Interlocked.Increment(ref callsMade));

			foreach (var input in executionInfo.Inputs)
				durableQueue.Queue(input);

			//make sure at least one transition call and at least one inspection call have been made
			var movedToPendingEvent = new ManualResetEventSlim(false);
			var completedEvent = new ManualResetEventSlim(false);

			executionInfo.JobQueue.OnQueueAction.Subscribe(action =>
			{
				if (action.ActionType == DurableJobQueueActionType.Pending)
				{
					movedToPendingEvent.Set();
				}
				else if (action.ActionType == DurableJobQueueActionType.Completed)
				{
					completedEvent.Set();
				}
			});

			scheduler.AdvanceBy(TimeSpan.FromSeconds(30));

			movedToPendingEvent.Wait(TimeSpan.FromSeconds(5));
			completedEvent.Wait(TimeSpan.FromSeconds(5));
			onJobsInspected.Wait(TimeSpan.FromSeconds(5));

			executionInfo.CallsMade = callsMade;
			return executionInfo;
		}

		[Fact]
		public void AddJob_PullsFromDurableStorage_ExecutingJobs()
		{
			var info = GetNextQueuedItem_CallCount();

			Assert.True(info.Inputs.Take(info.CallsMade).UnsortedSequenceEqual(jobsExecuted.Take(info.CallsMade), GenericEqualityComparer<TInput>.
			ByAllMembers()));
		}

		[Fact]
		public void AddJob_CallsInspector()
		{
			GetNextQueuedItem_CallCount();
			Assert.NotEmpty(jobsInspected);
		}

		[Fact]
		public void AddJob_CallInspector_OnEachResult()
		{
			int callsMade = GetNextQueuedItem_CallCount().CallsMade;
			Assert.Equal(callsMade, jobsInspected.Count);
		}

		[Fact]
		public void AddJob_Calls_Complete_AtLeastOnce()
		{
			GetNextQueuedItem_CallCount();
			A.CallTo(() => durableQueue.Complete(A<TInput>.Ignored)).MustHaveHappened(Repeated.AtLeast.Once);
		}

		[Fact]
		public void AddJob_Calls_Complete_ExpectedNumberOfTimes()
		{
			int callsMade = GetNextQueuedItem_CallCount().CallsMade;
			A.CallTo(() => durableQueue.Complete(A<TInput>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(callsMade));
		}

		*/

		[Fact]
		public void CancelQueuedAndWaitForExecutingJobsToComplete_WaitsOnJobs()
		{
			var scheduler = new HistoricalScheduler();
			var queue = _monitoredJobQueueFactory(new HistoricalScheduler(), _durableQueueFactory);
			foreach (var input in _fixture.CreateMany<TInput>(30))
				queue.AddJob(input);

			scheduler.AdvanceBy(TimeSpan.FromSeconds(30));
			queue.CancelQueuedAndWaitForExecutingJobsToComplete(TimeSpan.FromSeconds(5));

			Assert.True(queue.RunningCount == 0 && queue.QueuedCount == 0);
		}
	}
}