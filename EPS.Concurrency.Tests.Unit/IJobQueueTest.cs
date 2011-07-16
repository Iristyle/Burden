using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using EPS.Dynamic;
using EPS.Utility;
using FakeItEasy;
using Xunit;
using Xunit.Extensions;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IJobQueueTest<TJobQueue, TJobInput, TJobOutput>
		where TJobQueue: IJobQueue<TJobInput, TJobOutput>
	{
		protected readonly Func<IScheduler, TJobQueue> jobQueueFactory;

		public IJobQueueTest(Func<IScheduler, TJobQueue> jobQueueFactory)
		{
			this.jobQueueFactory = jobQueueFactory;
		}

		[Fact]
		public void Add_ThrowsOnNullItemForReferenceTypes()
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			if (typeof(TJobInput).IsValueType)
				return;

			var item = (null as object).Cast<TJobInput>();

			Assert.Throws<ArgumentNullException>(() => queue.Add(item, jobInput => default(TJobOutput)));
		}

		[Fact]
		public void Add_ThrowsOnNullAction()
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, TJobOutput>));
		}

		[Fact]
		public void Add_ThrowsOnNullObservableFactory()
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, IObservable<
			TJobOutput>>));
		}

		[Fact]
		public void StartNext_ThrowsOnNullAsyncObservable()
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			queue.Add(A.Dummy<TJobInput>(), jobInput => null as IObservable<TJobOutput>);

			Assert.Throws<ArgumentNullException>(() => queue.StartNext());
		}

		private bool WaitsForJobToFinish(int secondsToWait, bool useAsyncOverload)
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			var jobExecuted = new ManualResetEventSlim(false);
			var action = new Func<TJobInput, TJobOutput>(i =>
			{
				jobExecuted.Set();
				return A.Dummy<TJobOutput>();
			});
				
			if (useAsyncOverload)
				queue.Add(A.Dummy<TJobInput>(), action.ToAsync());
			else
				queue.Add(A.Dummy<TJobInput>(), action);

			queue.StartNext();
			return jobExecuted.Wait(TimeSpan.FromSeconds(secondsToWait));
		}

		[Fact]
		public void Add_RunsJob()
		{
			Assert.True(WaitsForJobToFinish(3, false));
		}

		[Fact]
		public void Add_AsyncOverload_RunsJob()
		{
			Assert.True(WaitsForJobToFinish(3, true));
		}

		private bool CompletedJobCallsJobCompleteObservable(int secondsToWait, bool useAsyncOverload)
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			var completedEvent = new ManualResetEventSlim(false);
			queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate).Subscribe(result => completedEvent.Set());

			var action = new Func<TJobInput, TJobOutput>(i =>
			{
				return A.Dummy<TJobOutput>();
			});

			if (useAsyncOverload)
				queue.Add(A.Dummy<TJobInput>(), action.ToAsync());
			else
				queue.Add(A.Dummy<TJobInput>(), action);

			queue.StartNext();

			return completedEvent.Wait(TimeSpan.FromSeconds(secondsToWait));
		}

		[Fact]
		public void WhenJobCompletes_FiredForExecutedJob()
		{
			Assert.True(CompletedJobCallsJobCompleteObservable(3, false));
		}

		[Fact]
		public void WhenJobCompletes_FiredForExecutedAsyncJob()
		{
			Assert.True(CompletedJobCallsJobCompleteObservable(3, true));
		}

		private Tuple<TJobInput, TJobOutput, JobResult<TJobInput, TJobOutput>> GetDataFromFauxJobExecution()
		{
			var input = A.Dummy<TJobInput>();
			var output = A.Dummy<TJobOutput>();

			JobResult<TJobInput, TJobOutput> result = null;
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			var completedEvent = new ManualResetEventSlim(false);
			queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate).Subscribe(r =>
			{
				result = r;
				completedEvent.Set();
			});
			queue.Add(input, job => { return output; });
			queue.StartNext();

			completedEvent.Wait(TimeSpan.FromSeconds(5));

			return Tuple.Create(input, output, result);
		}

		[Fact]
		public void WhenJobCompletes_NotificationContainsOriginalInput()
		{
			var data = GetDataFromFauxJobExecution();

			if (typeof(TJobInput).IsValueType)
				Assert.Equal(data.Item1, data.Item3.Input);
			else
				Assert.Same(data.Item1, data.Item3.Input);
		}

		[Fact]
		public void WhenJobCompletes_NotificationContainsOriginalOutput()
		{
			var data = GetDataFromFauxJobExecution();

			if (typeof(TJobOutput).IsValueType)
				Assert.Equal(data.Item2, data.Item3.Output);
			else
				Assert.Same(data.Item2, data.Item3.Output);
		}

		private JobResult<TJobInput, TJobOutput> GetResultOfJobThatThrows(TJobInput input, Exception toThrow, bool
		useObservableOverload)
		{
			JobResult<TJobInput, TJobOutput> result = null;
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			var completedEvent = new ManualResetEventSlim(false);
			queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate).Subscribe(r =>
			{
				result = r;
				completedEvent.Set();
			});
			var action = new Func<TJobInput, TJobOutput>(i => { throw toThrow; });
			if (useObservableOverload)
				queue.Add(input, action.ToAsync());
			else
				queue.Add(input, action);
			queue.StartNext();

			completedEvent.Wait(TimeSpan.FromSeconds(5));
			return result;
		}

		[Fact]
		public void WhenJobCompletes_ThrowingObservableGeneratesJobResultOfTypeError()
		{
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(A.Dummy<TJobInput>(), new ApplicationException(),
			true);
			Assert.Equal(JobResultType.Error, result.Type);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingObservableRetunsExpectedException()
		{
			var exception = new ApplicationException();
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(A.Dummy<TJobInput>(), exception, true);
			Assert.Same(exception, result.Exception);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingObservableRetunsSameInputAsGiven()
		{
			var input = A.Dummy<TJobInput>();
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(input, new ApplicationException(), true);
			Assert.Equal(input, result.Input);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingFuncGeneratesJobResultOfTypeError()
		{
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(A.Dummy<TJobInput>(), new ApplicationException(),
			false);
			Assert.Equal(JobResultType.Error, result.Type);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingFuncRetunsExpectedException()
		{
			var exception = new ApplicationException();
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(A.Dummy<TJobInput>(), exception, false);
			Assert.Same(exception, result.Exception);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingFuncRetunsSameInputAsGiven()
		{
			var input = A.Dummy<TJobInput>();
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(input, new ApplicationException(), false);
			Assert.Equal(input, result.Input);
		}

		[Fact]
		public void RunningCount_InitiallyZero()
		{
			var queue = jobQueueFactory(Scheduler.Immediate);
			Assert.Equal(0, queue.RunningCount);
		}

		public IJobQueue<TJobInput, TJobOutput> NewQueueWithPausedJobs(int jobsCount, int maxConcurrent, ManualResetEventSlim jobWaitPrimitive)
		{
			var queue = jobQueueFactory(Scheduler.Immediate);
			for (int j = 0; j < jobsCount; j++)
			{
				queue.Add(A.Dummy<TJobInput>(), input =>
				{
					jobWaitPrimitive.Wait();
					return A.Dummy<TJobOutput>();
				});
			}

			queue.StartUpTo(maxConcurrent);

			return queue;
		}

		[Theory]
		[InlineData(20, 5)]
		[InlineData(1, 1)]
		[InlineData(1, 2)]
		public void RunningCount_AdheresToMaxSpecifiedBy_StartupToCount(int fakeJobsToCreate, int maxConcurrent)
		{
			var jobLock = new ManualResetEventSlim(false);
			var queue = NewQueueWithPausedJobs(fakeJobsToCreate, maxConcurrent, jobLock);
			Assert.Equal(Math.Min(fakeJobsToCreate, maxConcurrent), queue.RunningCount);

			jobLock.Set();
			queue.CancelOutstandingJobs();
		}

		[Theory]
		[InlineData(8, 2)]
		[InlineData(2, 3)]
		public void QueuedCount_AdheresToMaxSpecifiedBy_StartupToCount(int fakeJobsToCreate, int maxConcurrent)
		{
			var jobLock = new ManualResetEventSlim(false);
			var queue = NewQueueWithPausedJobs(fakeJobsToCreate, maxConcurrent, jobLock);
			Assert.Equal(Math.Max(0, fakeJobsToCreate - maxConcurrent), queue.QueuedCount);

			jobLock.Set();
			queue.CancelOutstandingJobs();
		}

		[Theory]
		[InlineData(20, 5)]
		[InlineData(1, 1)]
		[InlineData(1, 2)]
		public void CancelOutstandingJobs_DoesNotCancelQueuedJobs(int fakeJobsToCreate, int maxConcurrent)
		{
			var jobLock = new ManualResetEventSlim(false);
			var queue = NewQueueWithPausedJobs(fakeJobsToCreate, maxConcurrent, jobLock);
			queue.CancelOutstandingJobs();

			Assert.Equal(Math.Min(fakeJobsToCreate, maxConcurrent), queue.RunningCount);
			jobLock.Set();
		}

		[Theory]
		[InlineData(20, 5)]
		[InlineData(1, 1)]
		[InlineData(1, 2)]
		public void CancelOutstandingJobs_OnlyClearsQueuedCalls(int fakeJobsToCreate, int maxConcurrent)
		{
			var jobLock = new ManualResetEventSlim(false);
			var queue = NewQueueWithPausedJobs(fakeJobsToCreate, maxConcurrent, jobLock);
			queue.CancelOutstandingJobs();

			Assert.Equal(0, queue.QueuedCount);
			jobLock.Set();
		}

		private List<JobResult<TJobInput, TJobOutput>> GetJobCancellations(int fakeJobsToCreate, int maxConcurrent, ManualResetEventSlim jobWaitPrimitive)
		{
			ManualResetEventSlim allCancellationsReceived = new ManualResetEventSlim(false);

			var queue = NewQueueWithPausedJobs(fakeJobsToCreate, maxConcurrent, jobWaitPrimitive);
			var queuedCount = queue.QueuedCount;
			var results = new List<JobResult<TJobInput, TJobOutput>>();
			queue.WhenJobCompletes.Subscribe(result =>
			{
				results.Add(result);
				if (results.Count == queuedCount)
					allCancellationsReceived.Set();

			});
			queue.CancelOutstandingJobs();

			allCancellationsReceived.Wait(TimeSpan.FromSeconds(5));

			return results;
		}

		[Theory]
		[InlineData(20, 5)]
		[InlineData(1, 1)]
		[InlineData(1, 2)]
		public void WhenJobCompletes_ReceivesErrorJobTypes_ForAllQueuedJobs(int fakeJobsToCreate, int maxConcurrent)
		{
			ManualResetEventSlim jobLock = new ManualResetEventSlim(false);
			var cancellations = GetJobCancellations(fakeJobsToCreate, maxConcurrent, jobLock);

			Assert.True(cancellations.TrueForAll(r => r.Type == JobResultType.Error));
			jobLock.Set();
		}

		[Theory]
		[InlineData(20, 5)]
		[InlineData(1, 1)]
		[InlineData(1, 2)]
		public void WhenJobCompletes_ResultExceptionIsOperationCanceledException_ForAllQueuedJobs(int fakeJobsToCreate, int maxConcurrent)
		{
			ManualResetEventSlim jobLock = new ManualResetEventSlim(false);
			var cancellations = GetJobCancellations(fakeJobsToCreate, maxConcurrent, jobLock);

			Assert.True(cancellations.TrueForAll(r => r.Exception.GetType() == typeof(OperationCanceledException)));
			jobLock.Set();
		}

		[Fact]
		public void QueuedCount_InitiallyZero()
		{
			var queue = jobQueueFactory(Scheduler.Immediate);
			Assert.Equal(0, queue.QueuedCount);
		}
	
		class QueueCounts
		{
			private List<int> running = new List<int>();
			private List<int> queued = new List<int>();

			public List<int> Running { get { return running; } }
			public List<int> Queued { get { return queued; } }
		}

		private QueueCounts GetQueueCountsOverSequentialJobExecutions(int jobCount, bool queueUpFront)
		{
			var counts = new QueueCounts();
			var jobPauser = new ManualResetEventSlim(false);
			var queue = jobQueueFactory(Scheduler.Immediate);
			Action startAndQueue = () =>
			{
				queue.StartNext();
				counts.Running.Add(queue.RunningCount);
				counts.Queued.Add(queue.QueuedCount);
			};

			for (int i = 0; i < jobCount; ++i)
			{
				queue.Add(A.Dummy<TJobInput>(), input =>
				{
					jobPauser.Wait();
					return A.Dummy<TJobOutput>();
				});

				if (!queueUpFront)
					startAndQueue();
			}

			if (queueUpFront)
			{
				for (int i = 0; i < jobCount; i++)
					startAndQueue();
			}

			jobPauser.Set();
			
			return counts;
		}

		[Theory]
		[InlineData(20)]
		[InlineData(40)]
		public void StartNext_AddsToRunningCount(int jobCount)
		{
			var counts = GetQueueCountsOverSequentialJobExecutions(jobCount, false);
			Assert.True(Enumerable.Range(1, jobCount).SequenceEqual(counts.Running));
		}

		[Theory]
		[InlineData(20)]
		[InlineData(40)]
		public void StartNext_DecreasesFromQueuedCount(int jobCount)
		{
			var counts = GetQueueCountsOverSequentialJobExecutions(jobCount, true);
			Assert.True(Enumerable.Range(0, jobCount).Reverse().SequenceEqual(counts.Queued));
		}

		[Fact]
		public void StartUpTo_ExecutesJobsInOrder()
		{
			//easy to compare object references for ordering, impossible for value types i'm afraid
			if (typeof(TJobInput).IsValueType)
			{
				Assert.True(true);
				return;
			}

			int jobCount = 20;
			var emptied = new ManualResetEventSlim(false);
			var inputs = A.CollectionOfFake<TJobInput>(jobCount);
			var inputsExecuted = new List<TJobInput>();
			var queue = jobQueueFactory(Scheduler.Immediate);

			foreach (var input in inputs)
			{
				queue.Add(input, i =>
				{
					inputsExecuted.Add(i);
					return A.Dummy<TJobOutput>();
				});
			}

			queue.WhenQueueEmpty.ObserveOn(Scheduler.Immediate)
				.Subscribe(r => emptied.Set());
			queue.StartUpTo(jobCount);
			emptied.Wait(TimeSpan.FromSeconds(10));

			Assert.True(inputs.SequenceEqual(inputsExecuted, new GenericEqualityComparer<TJobInput>((a, b) => object.ReferenceEquals(a, b))));
		}

		class NotificationCounts
		{
			public int Complete { get; set; }
			public int Empty { get; set; }
		}

		private NotificationCounts LaunchDummyJobsSegregatingIntoConcurrentBatches(int jobsPerBatch, int batches)
		{
			var notifications = new NotificationCounts();
			var allEmptysFired = new ManualResetEventSlim(false);

			var queue = jobQueueFactory(Scheduler.Immediate);
			queue.WhenQueueEmpty.ObserveOn(Scheduler.Immediate).Subscribe(r =>
			{
				notifications.Empty++;

				if (batches == notifications.Empty)
					allEmptysFired.Set();
			});

			for (int i = 0; i < batches; i++)
			{
				ManualResetEventSlim allCompletionsFired = new ManualResetEventSlim(false);

				//for each batch
				using (var subscription = queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate).Subscribe(r =>
				{
					notifications.Complete++;

					if (notifications.Complete % jobsPerBatch == 0)
						allCompletionsFired.Set();
				}))
				{
					for (int j = 0; j < jobsPerBatch; j++)
					{
						queue.Add(A.Dummy<TJobInput>(), input => A.Dummy<TJobOutput>());
					}

					queue.StartUpTo(jobsPerBatch);

					allCompletionsFired.Wait(TimeSpan.FromSeconds(10));
				}
			}

			allEmptysFired.Wait(TimeSpan.FromSeconds(10));
			return notifications;
		}

		[Theory]
		[InlineData(1, 1)]
		[InlineData(5, 3)]
		public void WhenQueueEmpty_FiresExpectedNumberOfTimesAfterExecutingJobsInBatches(int jobsPerBatch, int batches)
		{
			var counts = LaunchDummyJobsSegregatingIntoConcurrentBatches(jobsPerBatch, batches);

			Assert.True(counts.Empty == batches);
		}

		[Theory]
		[InlineData(1, 1)]
		[InlineData(2, 4)]
		public void WhenJobCompletes_FiresExpectedNumberOfTimesAfterExecutingJobsInBatches(int jobsPerBatch, int batches)
		{
			var counts = LaunchDummyJobsSegregatingIntoConcurrentBatches(jobsPerBatch, batches);

			Assert.True(counts.Complete == jobsPerBatch * batches);
		}
	}
}