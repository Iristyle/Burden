using System;
using System.Reactive.Concurrency;
using System.Threading;
using FakeItEasy;
using Xunit;
using Xunit.Extensions;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class AutoJobExecutionQueueTest<TJobInput, TJobOutput> :
		IJobExecutionQueueTest<AutoJobExecutionQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		protected Func<IScheduler, int, AutoJobExecutionQueue<TJobInput, TJobOutput>> maxConcurrentJobQueueFactory;
		protected Func<int, AutoJobExecutionQueue<TJobInput, TJobOutput>> publicMaxConcurrentJobQueueFactory;

		public AutoJobExecutionQueueTest(Func<IScheduler, int, AutoJobExecutionQueue<TJobInput, TJobOutput>> maxConcurrentJobQueueFactory,
			Func<int, AutoJobExecutionQueue<TJobInput, TJobOutput>> publicMaxConcurrentJobQueueFactory)
			: base(s => maxConcurrentJobQueueFactory(s, 0))
		{
			this.maxConcurrentJobQueueFactory = maxConcurrentJobQueueFactory;
			this.publicMaxConcurrentJobQueueFactory = publicMaxConcurrentJobQueueFactory;
		}

		[Fact]
		public void Constructor_ThrowsOnLessThanOneConcurrentJob()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => publicMaxConcurrentJobQueueFactory(0));
		}

		[Fact]
		public void Add_OnJobQueueInterface_AutomaticallyStartsJob()
		{
			var queue = maxConcurrentJobQueueFactory(Scheduler.Immediate, 10) as IJobExecutionQueue<TJobInput, TJobOutput>;
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
			public IJobExecutionQueue<TJobInput, TJobOutput> Queue { get; set; }
		}

		private JobExecutionStatus WaitForMaxConcurrentJobsToStart(int jobsToAdd, int maxConcurrent, int iterations, bool
		jobShouldThrow)
		{
			var status = new JobExecutionStatus()
			{
				Queue = maxConcurrentJobQueueFactory(Scheduler.Immediate, maxConcurrent) as IJobExecutionQueue<TJobInput, TJobOutput>,
				PrimaryJobPauser = new ManualResetEventSlim(false),
				SecondaryJobPauser = new ManualResetEventSlim(false),
				RemainingJobs = jobsToAdd
			};

			ManualResetEventSlim allJobsLaunched = new ManualResetEventSlim(false),
				firstGateCrossedByAllJobs = new ManualResetEventSlim(false),
				secondGateCrossedByAllJobs = new ManualResetEventSlim(false);

			int jobCounter = 0, firstGateCounter = 0, secondGateCounter = 0;

			for (int i = 0; i < jobsToAdd; i++)
			{
				status.Queue.Add(A.Dummy<TJobInput>(), jobInput =>
				{
					int expectedCounterValue = Math.Min(maxConcurrent, status.RemainingJobs);
					if (Interlocked.Increment(ref jobCounter) == expectedCounterValue)
						allJobsLaunched.Set();

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
				allJobsLaunched.Wait(TimeSpan.FromSeconds(5));
					//throw new ApplicationException();
				Interlocked.Exchange(ref jobCounter, 0);

				allJobsLaunched.Reset();
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
		public void Add_OnJobQueueInterface_AutomaticallyStartsOnlyUpToDefinedNumberOfJob(int jobsToAdd, int maxConcurrent, int iterations, bool jobShouldThrow)
		{
			var status = WaitForMaxConcurrentJobsToStart(jobsToAdd, maxConcurrent, iterations, jobShouldThrow);

			Assert.Equal(Math.Max(jobsToAdd - (maxConcurrent * iterations), 0), status.Queue.QueuedCount);

			status.Queue.CancelOutstandingJobs();
			status.PrimaryJobPauser.Set();
			status.SecondaryJobPauser.Set();
		}
	}

	public class ValueTypeAutoJobQueueTest :
		AutoJobExecutionQueueTest<int, int>
	{
		public ValueTypeAutoJobQueueTest()
			: base((scheduler, max) => new AutoJobExecutionQueue<int, int>(scheduler, max),
			max => new AutoJobExecutionQueue<int, int>(max))
		{ }
	}

	public class ReferenceTypeAutoJobQueueTest :
		AutoJobExecutionQueueTest<object, object>
	{
		public ReferenceTypeAutoJobQueueTest()
			: base((scheduler, max) => new AutoJobExecutionQueue<object, object>(scheduler, max),
			max => new AutoJobExecutionQueue<object, object>(max))
		{ }
	}
}