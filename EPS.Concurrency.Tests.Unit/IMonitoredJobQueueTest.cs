using System;
using System.Reactive.Concurrency;
using System.Threading;
using EPS.Utility;
using Ploeh.AutoFixture;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IMonitoredJobQueueTest<TMonitoredQueue, TInput, TOutput, TPoison>
		where TMonitoredQueue : IMonitoredJobQueue<TInput, TOutput, TPoison>
	{
		private class TransientJobQueueFactory
			: IDurableJobQueueFactory
		{
			public IDurableJobQueue<TInput, TPoison> CreateDurableJobQueue<TInput, TPoison>()
			{
				return new TransientJobQueue<TInput, TPoison>(GenericEqualityComparer<TInput>.ByAllMembers(), GenericEqualityComparer<TPoison>.ByAllMembers());
			}
		}

		protected IFixture fixture = new Fixture();
		protected Func<IScheduler, IDurableJobQueueFactory, IMonitoredJobQueue<TInput, TOutput, TPoison>> monitoredJobQueueFactory;
		protected IDurableJobQueueFactory durableQueueFactory = new TransientJobQueueFactory();
		protected IDurableJobQueue<TInput, TPoison> durableQueue = new TransientJobQueue<TInput, TPoison>(GenericEqualityComparer<TInput>.ByAllMembers(), GenericEqualityComparer<TPoison>.ByAllMembers());

		public IMonitoredJobQueueTest(Func<IScheduler, IDurableJobQueueFactory, IMonitoredJobQueue<TInput, TOutput, TPoison>> monitoredJobQueueFactory)
		{
			this.monitoredJobQueueFactory = monitoredJobQueueFactory;
		}
		
		[Fact]
		public void AddJob_DoesNotThrow()
		{
			var queue = monitoredJobQueueFactory(new HistoricalScheduler(), durableQueueFactory);
			Assert.DoesNotThrow(() => queue.AddJob(fixture.CreateAnonymous<TInput>()));
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
			scheduler.AdvanceBy(TimeSpan.FromSeconds(30));

			A.CallTo(() => durableQueue.Queue(A<TInput>.That.IsEqualTo(input))).MustHaveHappened(Repeated.Exactly.Once);
		}
		*/

		class CallCount
		{
			public int Expected { get; set; }
			public int Actual { get; set; }
		}

		private CallCount GetOnQueueActionCallCount(DurableJobQueueActionType filter)
		{
			HistoricalScheduler scheduler = new HistoricalScheduler();
			var monitoredQueue = monitoredJobQueueFactory(new HistoricalScheduler(), durableQueueFactory);
			var queuedEvent = new ManualResetEventSlim(false);
			var callCount = new CallCount() { Expected = monitoredQueue.MaxQueueItemsToPublishPerInterval };

			monitoredQueue.OnQueueAction.Subscribe(action =>
			{
				if (action.ActionType == filter)
				{
					++callCount.Actual;
					if (callCount.Actual == monitoredQueue.MaxQueueItemsToPublishPerInterval)
						queuedEvent.Set();
				}
			});

			foreach (var input in fixture.CreateMany<TInput>(monitoredQueue.MaxQueueItemsToPublishPerInterval))
				monitoredQueue.AddJob(input);

			scheduler.AdvanceBy(monitoredQueue.PollingInterval.Add(TimeSpan.FromSeconds(1)));

			queuedEvent.Wait(TimeSpan.FromSeconds(5));

			return callCount;
		}

		[Fact]
		public void MonitoredQueue_MoveItemsThrough_Queued_State()
		{
			var calls = GetOnQueueActionCallCount(DurableJobQueueActionType.Queued);
			Assert.Equal(calls.Expected, calls.Actual);
		}

		[Fact]
		public void MonitoredQueue_MoveItemsThrough_Pending_State()
		{
			var calls = GetOnQueueActionCallCount(DurableJobQueueActionType.Pending);
			Assert.Equal(calls.Expected, calls.Actual);
		}

		[Fact]
		public void MonitoredQueue_MoveItemsThrough_Completed_State()
		{
			var calls = GetOnQueueActionCallCount(DurableJobQueueActionType.Completed);
			Assert.Equal(calls.Expected, calls.Actual);
		}


		class ExecutionInfo
		{
			public IMonitoredJobQueue<TInput, TOutput, TPoison> JobQueue { get; set; }
			public int CallsMade { get; set; }
			public TInput[] Inputs { get; set; }
		}

		/*
		private ExecutionInfo GetTransitionNextQueuedItemToPending_CallCount()
		{
			HistoricalScheduler scheduler = new HistoricalScheduler();
			var executionInfo = new ExecutionInfo()
			{
				JobQueue = monitoredJobQueueFactory(new HistoricalScheduler(), durableQueue),
				Inputs = fixture.CreateMany<TInput>(100).ToArray()
			};

			int callsMade = 0;
			A.CallTo(() => durableQueue.TransitionNextQueuedItemToPending())
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
			var info = GetTransitionNextQueuedItemToPending_CallCount();

			Assert.True(info.Inputs.Take(info.CallsMade).UnsortedSequenceEqual(jobsExecuted.Take(info.CallsMade), GenericEqualityComparer<TInput>.
			ByAllMembers()));
		}

		[Fact]
		public void AddJob_CallsInspector()
		{
			GetTransitionNextQueuedItemToPending_CallCount();
			Assert.NotEmpty(jobsInspected);
		}

		[Fact]
		public void AddJob_CallInspector_OnEachResult()
		{
			int callsMade = GetTransitionNextQueuedItemToPending_CallCount().CallsMade;
			Assert.Equal(callsMade, jobsInspected.Count);
		}

		[Fact]
		public void AddJob_Calls_Complete_AtLeastOnce()
		{
			GetTransitionNextQueuedItemToPending_CallCount();
			A.CallTo(() => durableQueue.Complete(A<TInput>.Ignored)).MustHaveHappened(Repeated.AtLeast.Once);
		}

		[Fact]
		public void AddJob_Calls_Complete_ExpectedNumberOfTimes()
		{
			int callsMade = GetTransitionNextQueuedItemToPending_CallCount().CallsMade;
			A.CallTo(() => durableQueue.Complete(A<TInput>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(callsMade));
		}

		*/

		[Fact]
		public void CancelQueuedAndWaitForExecutingJobsToComplete_WaitsOnJobs()
		{
			var scheduler = new HistoricalScheduler();
			var queue = monitoredJobQueueFactory(new HistoricalScheduler(), durableQueueFactory);

			foreach (var input in fixture.CreateMany<TInput>(30))
				queue.AddJob(input);

			scheduler.AdvanceBy(TimeSpan.FromSeconds(30));
			queue.CancelQueuedAndWaitForExecutingJobsToComplete(TimeSpan.FromSeconds(5));

			Assert.True(queue.RunningCount == 0 && queue.QueuedCount == 0);
		}
	}
}