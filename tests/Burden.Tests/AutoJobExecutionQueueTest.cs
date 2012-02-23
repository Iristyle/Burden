using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Threading;
using FakeItEasy;
using Xunit;
using Xunit.Extensions;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class AutoJobExecutionQueueTest<TJobInput, TJobOutput> :
		JobExecutionQueueTest<AutoJobExecutionQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		private Func<IScheduler, int, int, AutoJobExecutionQueue<TJobInput, TJobOutput>> _maxConcurrentJobQueueFactory;
		private Func<int, int, AutoJobExecutionQueue<TJobInput, TJobOutput>> _publicMaxConcurrentJobQueueFactory;

		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs")]
		protected AutoJobExecutionQueueTest(Func<IScheduler, int, int, AutoJobExecutionQueue<TJobInput, TJobOutput>> maxConcurrentJobQueueFactory,
			Func<int, int, AutoJobExecutionQueue<TJobInput, TJobOutput>> publicMaxConcurrentJobQueueFactory)
			//
			: base(s => maxConcurrentJobQueueFactory(s, AutoJobExecutionQueue<TJobInput, TJobOutput>.DefaultConcurrent, 0))
		{
			this._maxConcurrentJobQueueFactory = maxConcurrentJobQueueFactory;
			this._publicMaxConcurrentJobQueueFactory = publicMaxConcurrentJobQueueFactory;
		}

		[Fact]
		public void Constructor_ThrowsOnLessThanOneConcurrentJob()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => _publicMaxConcurrentJobQueueFactory(0, 0));
		}

		[Fact]
		public void Add_ToJobQueueInterface_AutomaticallyStartsJob()
		{
			var queue = _maxConcurrentJobQueueFactory(Scheduler.Immediate, 10, 10) as IJobExecutionQueue<TJobInput, TJobOutput>;
			var jobExecuted = new ManualResetEventSlim(false);
			queue.Add(A.Dummy<TJobInput>(), jobInput =>
			{
				jobExecuted.Set();
				return A.Dummy<TJobOutput>();
			});

			Assert.True(jobExecuted.Wait(TimeSpan.FromSeconds(2)));
		}

		class JobExecutionStatus
		{
			public int RemainingJobs { get; set; }
			public bool ExpectedNumberOfJobsFiredUp { get; set; }
			public ManualResetEventSlim PrimaryJobPauser { get; set; }
			public ManualResetEventSlim SecondaryJobPauser { get; set; }
			public ManualResetEventSlim AllJobsForIterationLaunched { get; set; }
			public IJobExecutionQueue<TJobInput, TJobOutput> Queue { get; set; }
		}

		private JobExecutionStatus WaitForMaxConcurrentJobsToStart(int jobsToAdd, int toStart, int iterations, bool
		jobShouldThrow)
		{
			var status = new JobExecutionStatus()
			{
				Queue = _maxConcurrentJobQueueFactory(Scheduler.Immediate, AutoJobExecutionQueue<TJobInput, TJobOutput>.DefaultConcurrent, toStart) as IJobExecutionQueue<TJobInput, TJobOutput>,
				AllJobsForIterationLaunched = new ManualResetEventSlim(false),
				PrimaryJobPauser = new ManualResetEventSlim(false),
				SecondaryJobPauser = new ManualResetEventSlim(false),
				RemainingJobs = jobsToAdd
			};

			using (var firstGateCrossedByAllJobs = new ManualResetEventSlim(false))
			using (var secondGateCrossedByAllJobs = new ManualResetEventSlim(false))
			{
				int jobCounter = 0, firstGateCounter = 0, secondGateCounter = 0;

				for (int i = 0; i < jobsToAdd; i++)
				{
					status.Queue.Add(A.Dummy<TJobInput>(), jobInput =>
					{
						int expectedCounterValue = Math.Min(status.Queue.MaxConcurrent, Math.Min(toStart, status.RemainingJobs));
						if (Interlocked.Increment(ref jobCounter) == expectedCounterValue)
							status.AllJobsForIterationLaunched.Set();

						//we have to get a bit complex here by controlling automatic job execution with 2 locks
						status.PrimaryJobPauser.Wait();

						if (Interlocked.Increment(ref firstGateCounter) == expectedCounterValue)
							firstGateCrossedByAllJobs.Set();

						status.SecondaryJobPauser.Wait();

						if (Interlocked.Increment(ref secondGateCounter) == expectedCounterValue)
							secondGateCrossedByAllJobs.Set();

						if (!jobShouldThrow)
							return A.Dummy<TJobOutput>();

						throw new ArgumentException();
					});
				}

				for (int j = 0; j < iterations; j++)
				{
					//wait for all jobs to start, then reset counter, allow them to complete, and reset allJobsLaunched
					status.AllJobsForIterationLaunched.Wait(TimeSpan.FromSeconds(5));
						//throw new ApplicationException();
					Interlocked.Exchange(ref jobCounter, 0);

					status.AllJobsForIterationLaunched.Reset();
					status.RemainingJobs = status.Queue.QueuedCount;
					//let the next set begin to flow through, by unlocking the first gate
					status.PrimaryJobPauser.Set();

					//wait until all the jobs have received it before resetting the pauser, which will hold the next set of jobs as they're pumped in
					firstGateCrossedByAllJobs.Wait(TimeSpan.FromSeconds(5));
					firstGateCrossedByAllJobs.Reset();
					status.PrimaryJobPauser.Reset();

					//reset the gate counter...
					Interlocked.Exchange(ref firstGateCounter, 0);

					//if it's our last iteration, leaved the jobs paused
					if (j != iterations - 1)
					{
						status.SecondaryJobPauser.Set();
						//wait again, this time for the second gate
						secondGateCrossedByAllJobs.Wait(TimeSpan.FromSeconds(5));
						secondGateCrossedByAllJobs.Reset();

						Interlocked.Exchange(ref secondGateCounter, 0);
					}

					status.SecondaryJobPauser.Reset();
				}

				return status;
			}
		}

		[Theory]
		[InlineData(25, 5, 1, false)]
		[InlineData(1, 1, 1, false)]
		[InlineData(1, 3, 1, false)]
		[InlineData(25, 5, 2, false)]
		[InlineData(7, 2, 3, false)]
		[InlineData(25, 5, 1, true)]
		[InlineData(1, 1, 1, true)]
		[InlineData(1, 3, 1, true)]
		[InlineData(25, 5, 2, true)]
		[InlineData(7, 2, 3, true)]
		public void Add_ToJobQueueInterface_AutomaticallyStartsOnlyUpToDefinedNumberOfJob(int jobsToAdd, int startInitially, int iterations, bool jobShouldThrow)
		{
			var status = WaitForMaxConcurrentJobsToStart(jobsToAdd, startInitially, iterations, jobShouldThrow);

			Assert.Equal(Math.Max(jobsToAdd - (startInitially * iterations), 0), status.Queue.QueuedCount);

			CancelAndWaitOnPausedJobs(status.Queue, status.PrimaryJobPauser, status.SecondaryJobPauser);
			status.Queue.Dispose();
		}

		[Theory]
		[InlineData(25, 5, 1)]
		[InlineData(8, 4, 2)]
		public void MaxConcurrent_OnJobQueueInterface_RestrictsNewJobsCountToGivenValue(int jobsToAdd, int startInitially, int thenStart)
		{
			using (var manualResetEvent = new ManualResetEventSlim(false))
			{
				var status = WaitForMaxConcurrentJobsToStart(jobsToAdd, startInitially, 1, false);
				status.Queue.MaxConcurrent = thenStart;
				int i = 0;
				using (var subscription = status.Queue.WhenJobCompletes.Subscribe(result =>
					{
						if (Interlocked.Increment(ref i) == startInitially)
							manualResetEvent.Set();
					}))
				{
					//release the jobs, and now only the new count should be running
					status.PrimaryJobPauser.Set();
					status.SecondaryJobPauser.Set();
					status.PrimaryJobPauser.Reset();
					//wait on jobs to complete
					manualResetEvent.Wait(TimeSpan.FromSeconds(3));
					//then to launch
					status.AllJobsForIterationLaunched.Wait(TimeSpan.FromSeconds(2));
					Assert.Equal(thenStart, status.Queue.RunningCount);

					CancelAndWaitOnPausedJobs(status.Queue, status.PrimaryJobPauser, status.SecondaryJobPauser);
				}
				status.Queue.Dispose();
			}
		}
	}

	public class AutoJobQueueValueTypeTest :
		AutoJobExecutionQueueTest<int, int>
	{
		public AutoJobQueueValueTypeTest()
			: base((scheduler, max, toAutostart) => new AutoJobExecutionQueue<int, int>(scheduler, max, toAutostart),
			(max, toAutostart) => new AutoJobExecutionQueue<int, int>(max))
		{ }
	}

	public class AutoJobQueueReferenceTypeTest :
		AutoJobExecutionQueueTest<object, object>
	{
		public AutoJobQueueReferenceTypeTest()
			: base((scheduler, max, toAutostart) => new AutoJobExecutionQueue<object, object>(scheduler, max, toAutostart),
			(max, toAutostart) => new AutoJobExecutionQueue<object, object>(max))
		{ }
	}
}