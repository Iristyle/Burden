using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using EqualityComparer;
using FakeItEasy;
using Xunit;
using Xunit.Extensions;

namespace EPS.Concurrency.Tests.Unit
{
	[SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "Non-issue for test classes really")]
	public abstract class JobExecutionQueueTest<TJobQueue, TJobInput, TJobOutput>
		where TJobQueue: IJobExecutionQueue<TJobInput, TJobOutput>
	{
		private readonly Func<IScheduler, TJobQueue> _jobQueueFactory;

		protected JobExecutionQueueTest(Func<IScheduler, TJobQueue> jobQueueFactory)
		{
			this._jobQueueFactory = jobQueueFactory;
		}

		[Fact]
		public void Add_ThrowsOnNullItemForReferenceTypes()
		{
			using (TJobQueue queue = _jobQueueFactory(Scheduler.Immediate))
			{
				if (typeof(TJobInput).IsValueType) return;
				var item = (null as object).Cast<TJobInput>();
				Assert.Throws<ArgumentNullException>(() => queue.Add(item, jobInput => default(TJobOutput)));
			}
		}

		[Fact]
		public void Add_ThrowsOnNullAction()
		{
			using (TJobQueue queue = _jobQueueFactory(Scheduler.Immediate))
			{
				Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, TJobOutput>));
			}
		}

		[Fact]
		public void Add_ThrowsOnNullObservableFactory()
		{
			using (TJobQueue queue = _jobQueueFactory(Scheduler.Immediate))
			{
				Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, IObservable<
				TJobOutput>>));
			}
		}

		[Fact]
		public void StartNext_ThrowsOnNullAsyncObservable()
		{
			using (TJobQueue queue = _jobQueueFactory(Scheduler.Immediate))
			{
				queue.Add(A.Dummy<TJobInput>(), jobInput => null as IObservable<TJobOutput>);

				Assert.Throws<ArgumentNullException>(() => queue.StartNext());
			}
		}

		private bool WaitsForJobToFinish(int secondsToWait, bool useAsyncOverload)
		{
			using (TJobQueue queue = _jobQueueFactory(Scheduler.Immediate))
			using (var jobExecuted = new ManualResetEventSlim(false))
			{				
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
		}

		[Fact]
		public void Add_RunsJob()
		{
			Assert.True(WaitsForJobToFinish(3, false));
		}

		[Fact]
		public void Add_AsyncOverload_RunsJob()
		{
			Assert.True(WaitsForJobToFinish(4, true));
		}

		private bool CompletedJobCallsJobCompleteObservable(int secondsToWait, bool useAsyncOverload)
		{
			using (TJobQueue queue = _jobQueueFactory(Scheduler.Immediate))
			using (var completedEvent = new ManualResetEventSlim(false))
			using (var subscription = queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate)
					.Subscribe(result => completedEvent.Set()))
			{				
				var action = new Func<TJobInput, TJobOutput>(i =>{
					return A.Dummy<TJobOutput>();});

				if (useAsyncOverload)
					queue.Add(A.Dummy<TJobInput>(), action.ToAsync());
				else
					queue.Add(A.Dummy<TJobInput>(), action);

				queue.StartNext();

				return completedEvent.Wait(TimeSpan.FromSeconds(secondsToWait));
			}
		}

		[Fact]
		public void MaxConcurrent_EqualsOne_WhenSetBelowMinimumAllowed()
		{
			using (var queue = _jobQueueFactory(Scheduler.Immediate))
			{
				queue.MaxConcurrent = 0;
				Assert.Equal(1, queue.MaxConcurrent);
			}
		}

		[Fact]
		public void MaxConcurrent_IgnoresSetValue_WhenSetAboveMaxAllowed()
		{
			using (var queue = _jobQueueFactory(Scheduler.Immediate))
			{
				queue.MaxConcurrent = int.MaxValue;
				Assert.NotEqual(int.MaxValue, queue.MaxConcurrent);
			}
		}

		[Theory]
		[InlineData(5)]
		[InlineData(25)]
		public void MaxConcurrent_Equals_SetValue(int maxConcurrent)
		{
			using (var queue = _jobQueueFactory(Scheduler.Immediate))
			{
				queue.MaxConcurrent = maxConcurrent;
				Assert.Equal(maxConcurrent, queue.MaxConcurrent);
			}
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
			using (TJobQueue queue = _jobQueueFactory(Scheduler.Immediate))
			using (var completedEvent = new ManualResetEventSlim(false))
			using (var subscription = queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate).Subscribe(r =>
			{
				result = r;
				completedEvent.Set();
			}))
			{
				queue.Add(input, job =>{ return output;});
				queue.StartNext();
				completedEvent.Wait(TimeSpan.FromSeconds(5));
			}

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
			using (TJobQueue queue = _jobQueueFactory(Scheduler.Immediate))
			using (var completedEvent = new ManualResetEventSlim(false))
			using (var subscription = queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate).Subscribe(r =>
			{
				result = r;
				completedEvent.Set();
			}))
			{
				var action = new Func<TJobInput, TJobOutput>(i =>{ throw toThrow;});
			
				if (useObservableOverload) queue.Add(input, action.ToAsync());
				else queue.Add(input, action);
				queue.StartNext();
				completedEvent.Wait(TimeSpan.FromSeconds(5));
			}

			return result;
		}

		[Fact]
		public void WhenJobCompletes_ThrowingObservableGeneratesJobResultOfTypeError()
		{
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(A.Dummy<TJobInput>(), new InvalidOperationException(),
			true);
			Assert.Equal(JobResultType.Error, result.ResultType);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingObservableReturnsExpectedException()
		{
			var exception = new InvalidOperationException();
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(A.Dummy<TJobInput>(), exception, true);
			Assert.Same(exception, result.Exception);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingObservableReturnsSameInputAsGiven()
		{
			var input = A.Dummy<TJobInput>();
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(input, new InvalidOperationException(), true);
			Assert.Equal(input, result.Input);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingFuncGeneratesJobResultOfTypeError()
		{
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(A.Dummy<TJobInput>(), new InvalidOperationException(),
			false);
			Assert.Equal(JobResultType.Error, result.ResultType);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingFuncReturnsExpectedException()
		{
			var exception = new InvalidOperationException();
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(A.Dummy<TJobInput>(), exception, false);
			Assert.Same(exception, result.Exception);
		}

		[Fact]
		public void WhenJobCompletes_ThrowingFuncReturnsSameInputAsGiven()
		{
			var input = A.Dummy<TJobInput>();
			JobResult<TJobInput, TJobOutput> result = GetResultOfJobThatThrows(input, new InvalidOperationException(), false);
			Assert.Equal(input, result.Input);
		}

		[Fact]
		public void RunningCount_InitiallyZero()
		{
			var queue = _jobQueueFactory(Scheduler.Immediate);
			Assert.Equal(0, queue.RunningCount);
		}

		protected IJobExecutionQueue<TJobInput, TJobOutput> NewQueueWithPausedJobs(int jobsCount, int toStart, ManualResetEventSlim jobWaitPrimitive, int maxConcurrent)
		{
			var queue = _jobQueueFactory(Scheduler.Immediate);
			queue.MaxConcurrent = maxConcurrent;
			for (int j = 0; j < jobsCount; j++)
			{
				queue.Add(A.Dummy<TJobInput>(), input =>
				{
					jobWaitPrimitive.Wait();
					return A.Dummy<TJobOutput>();
				});
			}

			queue.StartAsManyAs(toStart);

			return queue;
		}

		protected void CancelAndWaitOnPausedJobs(IJobExecutionQueue<TJobInput, TJobOutput> queue, params ManualResetEventSlim[] jobWaitPrimitives)
		{
			queue.CancelOutstandingJobs();
			int toWaitOn = queue.RunningCount;
			int completedCount = 0;
			
			//wait for the pauser to unlock paused jobs
			using (var completed = new ManualResetEventSlim(false))
			using (var onCompletion = queue.WhenJobCompletes.SubscribeOn(Scheduler.Immediate)
				.Subscribe(r =>
				{
					if (Interlocked.Increment(ref completedCount) == toWaitOn)
						completed.Set();
				}))
			{
				foreach (var waiter in jobWaitPrimitives)
				{
					waiter.Set();
				}

				if (queue.RunningCount != 0)
				{
					completed.Wait(TimeSpan.FromSeconds(3));
				}
			}
		}


		[Theory]
		[InlineData(20, 5, 4)]
		[InlineData(10, 3, 5)]
		[InlineData(1, 1, 10)]
		[InlineData(1, 2, 10)]
		[InlineData(2, 2, 3)]
		public void RunningCount_AdheresToMaxSpecifiedBy_StartAsManyAsCountAndMaxConcurrentValue(int fakeJobsToCreate, int toStart, int maxConcurrent)
		{
			using (var jobLock = new ManualResetEventSlim(false))
			using (var queue = NewQueueWithPausedJobs(fakeJobsToCreate, toStart, jobLock, maxConcurrent))
			{
				int expectedToStart = Math.Min(fakeJobsToCreate, toStart), shouldRun = Math.Min(maxConcurrent, expectedToStart);
				Assert.Equal(shouldRun, queue.RunningCount);
				CancelAndWaitOnPausedJobs(queue, jobLock);
			}
		}

		[Theory]
		[InlineData(8, 2, 2)]
		[InlineData(2, 3, 2)]
		[InlineData(10, 6, 5)]
		[InlineData(1, 1, 10)]
		public void QueuedCount_AdheresToMaxSpecifiedBy_StartAsManyAsCount(int fakeJobsToCreate, int toStart, int maxConcurrent)
		{
			using (var jobLock = new ManualResetEventSlim(false))
			using (var queue = NewQueueWithPausedJobs(fakeJobsToCreate, toStart, jobLock, maxConcurrent))
			{
				int expectedToStart = Math.Min(fakeJobsToCreate, toStart), shouldRun = Math.Min(maxConcurrent, expectedToStart);
				Assert.Equal(Math.Max(0, fakeJobsToCreate - shouldRun), queue.QueuedCount);
				CancelAndWaitOnPausedJobs(queue, jobLock);
			}
		}

		[Theory]
		[InlineData(20, 5, 30)]
		[InlineData(1, 1, 30)]
		[InlineData(1, 2, 30)]
		public void CancelOutstandingJobs_DoesNotCancelQueuedJobs(int fakeJobsToCreate, int toStart, int maxConcurrent)
		{
			using (var jobLock = new ManualResetEventSlim(false))
			using (var queue = NewQueueWithPausedJobs(fakeJobsToCreate, toStart, jobLock, maxConcurrent))
			{
				queue.CancelOutstandingJobs();

				Assert.Equal(Math.Min(fakeJobsToCreate, Math.Min(toStart, queue.MaxConcurrent)), queue.RunningCount);
				CancelAndWaitOnPausedJobs(queue, jobLock);
			}
		}


		[Theory]
		[InlineData(20, 5, 30)]
		[InlineData(1, 1, 30)]
		[InlineData(1, 2, 30)]
		public void CancelOutstandingJobs_OnlyClearsQueuedCalls(int fakeJobsToCreate, int toStart, int maxConcurrent)
		{
			using (var jobLock = new ManualResetEventSlim(false))
			using (var queue = NewQueueWithPausedJobs(fakeJobsToCreate, toStart, jobLock, maxConcurrent))
			{				
				queue.CancelOutstandingJobs();
				Assert.Equal(0, queue.QueuedCount);
				CancelAndWaitOnPausedJobs(queue, jobLock);
			}
		}

		[Theory]
		[InlineData(20, 5)]
		[InlineData(1, 1)]
		[InlineData(1, 2)]
		[InlineData(30, 30)]
		public void StartAsManyAs_ReturnedCount_MatchesActualRunningJobCount(int fakeJobsToCreate, int toStart)
		{
			using (var jobPauser = new ManualResetEventSlim(false))
			{
				var queue = _jobQueueFactory(Scheduler.Immediate);
				for (int j = 0; j < fakeJobsToCreate; j++)
				{
					queue.Add(A.Dummy<TJobInput>(), input =>
					{
						jobPauser.Wait();
						return A.Dummy<TJobOutput>();
					});
				}
				int started = queue.StartAsManyAs(toStart);
				Assert.Equal(queue.RunningCount, started);
				queue.CancelOutstandingJobs();
				jobPauser.Set();
			}
		}

		[Theory]
		[InlineData(20, 5, 30)]
		[InlineData(1, 1, 30)]
		[InlineData(1, 2, 30)]
		public void StartAsManyAs_CalledMultipleTimes_DoesNotStartNewJobs_WhenMaxConcurrentAlreadyRunning(int fakeJobsToCreate, int toStart, int maxConcurrent)
		{
			using (var jobPauser = new ManualResetEventSlim(false))
			using (var queue = NewQueueWithPausedJobs(fakeJobsToCreate, toStart, jobPauser, maxConcurrent))
			{
				//maxConcurrent jobs are already running, so this should not launch anything new
				int newJobsLaunched = 0;
				for (int i = 0; i < 20; i++)
				{
					newJobsLaunched += queue.StartAsManyAs(toStart);
				}
				Assert.Equal(0, newJobsLaunched);
				CancelAndWaitOnPausedJobs(queue, jobPauser);
			}
		}

		[Theory]
		[InlineData(20, 5, 30)]
		[InlineData(1, 1, 30)]
		[InlineData(1, 2, 30)]
		[InlineData(4, 2, 30)]
		public void StartAsManyAs_CalledMultipleTimesToSimulateRaceConditions_DoesNotIncreaseConcurrentlyExecutingJobs(int fakeJobsToCreate, int toStart, int maxConcurrent)
		{
			int completedCount = 0;

			using (var jobPauser = new ManualResetEventSlim(false))
			using (var completed = new ManualResetEventSlim(false))
			using (var queue = NewQueueWithPausedJobs(fakeJobsToCreate, toStart, jobPauser, maxConcurrent))
			using (var subscription = queue.WhenJobCompletes.SubscribeOn(Scheduler.Immediate)
				.Subscribe(result =>
				{
					if (Interlocked.Increment(ref completedCount) == Math.Min(fakeJobsToCreate, toStart)) 
						completed.Set();
				}))
			{
				//release the pending jobs, and wait for them to finish
				jobPauser.Set();
				if (!completed.Wait(TimeSpan.FromSeconds(2))) throw new InvalidOperationException("Jobs never completed");
				int newJobsLaunched = 0;
				jobPauser.Reset();
				//call StartAsManyAs successively to try to simulate a race conditions...
				for (int i = 0; i < 20; i++)
				{
					newJobsLaunched += queue.StartAsManyAs(toStart);
				}
				int shouldHaveCreated = Math.Min(Math.Max(fakeJobsToCreate - toStart, 0), toStart);
				Assert.Equal(shouldHaveCreated, newJobsLaunched);
				CancelAndWaitOnPausedJobs(queue, jobPauser);
			}
		}


		private List<JobResult<TJobInput, TJobOutput>> GetJobCancellations(int fakeJobsToCreate, int toStart, ManualResetEventSlim jobWaitPrimitive, int maxConcurrent)
		{
			var results = new List<JobResult<TJobInput, TJobOutput>>();

			using (var allCancellationsReceived = new ManualResetEventSlim(false))
			using (var queue = NewQueueWithPausedJobs(fakeJobsToCreate, toStart, jobWaitPrimitive, maxConcurrent))
			{
				var queuedCount = queue.QueuedCount;
				
				using (var subscription = queue.WhenJobCompletes.Subscribe(result =>
				{
					if (result.ResultType == JobResultType.Error)
					{
						results.Add(result);
						if (results.Count == queuedCount) allCancellationsReceived.Set();
					}
				}))
				{
					queue.CancelOutstandingJobs();
					if (queue.QueuedCount != 0)
					{
						allCancellationsReceived.Wait(TimeSpan.FromSeconds(5));
					}

					CancelAndWaitOnPausedJobs(queue, jobWaitPrimitive);
					return results;
				}
			}
		}

		[Theory]
		[InlineData(20, 5, 30)]
		[InlineData(1, 1, 30)]
		[InlineData(1, 2, 10)]
		public void WhenJobCompletes_ReceivesErrorJobTypes_ForAllQueuedJobs(int fakeJobsToCreate, int toStart, int maxConcurrent)
		{
			using (var jobLock = new ManualResetEventSlim(false))
			{
				var cancellations = GetJobCancellations(fakeJobsToCreate, toStart, jobLock, maxConcurrent);
				Assert.True(cancellations.TrueForAll(r => r.ResultType == JobResultType.Error));
				jobLock.Set();
			}
		}

		[Theory]
		[InlineData(20, 5, 20)]
		[InlineData(1, 1, 20)]
		[InlineData(1, 2, 20)]
		public void WhenJobCompletes_ResultExceptionIsOperationCanceledException_ForAllQueuedJobs(int fakeJobsToCreate, int toStart, int maxConcurrent)
		{
			using (var jobLock = new ManualResetEventSlim(false))
			{
				var cancellations = GetJobCancellations(fakeJobsToCreate, toStart, jobLock, maxConcurrent);
				Assert.True(cancellations.TrueForAll(r => r.Exception.GetType() == typeof(OperationCanceledException)));
				jobLock.Set();
			}
		}

		[Fact]
		public void QueuedCount_InitiallyZero()
		{
			var queue = _jobQueueFactory(Scheduler.Immediate);
			Assert.Equal(0, queue.QueuedCount);
		}
	
		class QueueCounts
		{
			private List<int> running = new List<int>();
			private List<int> queued = new List<int>();
			public TJobQueue Queue { get; set; }
			
			public List<int> Running { get { return running; } }
			public List<int> Queued { get { return queued; } }
		}

		private QueueCounts GetQueueCountsOverSequentialJobExecutions(int jobCount, bool queueUpFront)
		{
			var counts = new QueueCounts();
			var jobPauser = new ManualResetEventSlim(false);
			counts.Queue = _jobQueueFactory(Scheduler.Immediate);
			Action startAndQueue = () =>
			{
				counts.Queue.StartNext();
				counts.Running.Add(counts.Queue.RunningCount);
				counts.Queued.Add(counts.Queue.QueuedCount);
			};

			for (int i = 0; i < jobCount; ++i)
			{
				counts.Queue.Add(A.Dummy<TJobInput>(), input =>
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
		[InlineData(60)]
		public void StartNext_AddsToRunningCount_MaxingAtMaxConcurrent(int jobCount)
		{
			var counts = GetQueueCountsOverSequentialJobExecutions(jobCount, false);
			Assert.True(Enumerable.Range(1, jobCount)
				.Select(i => Math.Min(i, counts.Queue.MaxConcurrent))
				.SequenceEqual(counts.Running));
		}

		[Theory]
		[InlineData(20)]
		[InlineData(40)]
		[InlineData(60)]
		public void StartNext_DecreasesFromQueuedCount_UntilMaxConcurrentExceeded(int jobCount)
		{
			var counts = GetQueueCountsOverSequentialJobExecutions(jobCount, true);
			var expectedQueueCount = Enumerable.Range(0, jobCount).Reverse()
				.Select(i => Math.Max(i, jobCount - counts.Queue.MaxConcurrent));

			Assert.True(expectedQueueCount.SequenceEqual(counts.Queued));
		}

		[Fact]
		public void StartNext_ExecutesJobsInOrder()
		{
			//easy to compare object references for ordering, impossible for value types i'm afraid
			if (typeof(TJobInput).IsValueType)
			{
				Assert.True(true);
				return;
			}

			int jobCount = 20;
			var inputs = A.CollectionOfFake<TJobInput>(jobCount);
			var inputsExecuted = new List<TJobInput>();

			using (var emptied = new ManualResetEventSlim(false))
			using (var queue = _jobQueueFactory(Scheduler.Immediate))
			{
				foreach (var input in inputs)
				{
					queue.Add(input, i =>
					{
						inputsExecuted.Add(i);
						return A.Dummy<TJobOutput>();
					});
				}
				using (var subscription = queue.WhenQueueEmpty.ObserveOn(Scheduler.Immediate)
				.Subscribe(r => emptied.Set()))
				{
					for (int i = 0; i < jobCount; i++)
					{
						queue.StartNext();
					}
					emptied.Wait(TimeSpan.FromSeconds(5));
				}

				Assert.True(inputs.SequenceEqual(inputsExecuted, new GenericEqualityComparer<TJobInput>((a, b) => object.
				ReferenceEquals(a, b))));
			}
		}


		class NotificationCounts
		{
			public int Complete { get; set; }
			public int Empty { get; set; }
		}

		private NotificationCounts LaunchDummyJobsSegregatingIntoConcurrentBatches(int jobsPerBatch, int batches)
		{
			var notifications = new NotificationCounts();
			using (var allEmptysFired = new ManualResetEventSlim(false))
			using (var queue = _jobQueueFactory(Scheduler.Immediate))
			using (var subscription = queue.WhenQueueEmpty.ObserveOn(Scheduler.Immediate)
				.Subscribe(r =>
				{
					notifications.Empty++;
					if (batches == notifications.Empty) allEmptysFired.Set();
				}))
			{
				for (int i = 0; i < batches; i++)
				{
					using (var allCompletionsFired = new ManualResetEventSlim(false))
					using (var completeSubscription = queue.WhenJobCompletes.
						ObserveOn(Scheduler.Immediate).Subscribe(r =>
						{
							notifications.Complete++;
							if (notifications.Complete % jobsPerBatch == 0) allCompletionsFired.Set();
						}))
					{
						for (int j = 0; j < jobsPerBatch; j++)
						{
							queue.Add(A.Dummy<TJobInput>(), input => A.Dummy<TJobOutput>());
						}
						queue.StartAsManyAs(jobsPerBatch);
						allCompletionsFired.Wait(TimeSpan.FromSeconds(5));
					}
				}
				allEmptysFired.Wait(TimeSpan.FromSeconds(5));

				return notifications;
			}			
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