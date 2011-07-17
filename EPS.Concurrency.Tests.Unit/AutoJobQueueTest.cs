using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using EPS.Dynamic;
using FakeItEasy;
using Xunit;
using Xunit.Extensions;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class AutoJobQueueTest<TJobInput, TJobOutput> :
		IJobQueueTest<AutoJobQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		protected Func<IScheduler, int, AutoJobQueue<TJobInput, TJobOutput>> maxConcurrentJobQueueFactory;
		protected Func<int, AutoJobQueue<TJobInput, TJobOutput>> publicMaxConcurrentJobQueueFactory;

		public AutoJobQueueTest(Func<IScheduler, int, AutoJobQueue<TJobInput, TJobOutput>> maxConcurrentJobQueueFactory,
			Func<int, AutoJobQueue<TJobInput, TJobOutput>> publicMaxConcurrentJobQueueFactory)
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

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Add_ThrowsOnNullItemForReferenceTypes_OnAutostartOverload(bool autoStart)
		{
			var queue = jobQueueFactory(Scheduler.Immediate);
			if (typeof(TJobInput).IsValueType)
				return;

			var item = (null as object).Cast<TJobInput>();
			var async = new Func<TJobInput, TJobOutput>(jobInput => default(TJobOutput))
			.ToAsync();

			Assert.Throws<ArgumentNullException>(() => queue.Add(item, async, autoStart));
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Add_ThrowsOnNullObservableFactory_OnAutostartOverload(bool autoStart)
		{
			var queue = jobQueueFactory(Scheduler.Immediate);

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, IObservable<
			TJobOutput>>, autoStart));
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void StartNext_ThrowsOnNullObservableFactory_OnAutostartOverload(bool autoStart)
		{
			var queue = jobQueueFactory(Scheduler.Immediate);
			queue.Add(A.Dummy<TJobInput>(), jobInput => null as IObservable<TJobOutput>, autoStart);

			Assert.Throws<ArgumentNullException>(() => queue.StartNext());
		}

		[Fact]
		public void Add_OnJobQueueInterface_AutomaticallyStartsJob()
		{
			var queue = maxConcurrentJobQueueFactory(Scheduler.Immediate, 10) as IJobQueue<TJobInput, TJobOutput>;
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
			public ManualResetEventSlim JobPauser { get; set; }
			public IJobQueue<TJobInput, TJobOutput> Queue { get; set; }
		}

		private JobExecutionStatus WaitForMaxConcurrentJobsToStart(int jobsToAdd, int maxConcurrent, int iterations, bool jobShouldThrow)
		{
			var status = new JobExecutionStatus()
			{
				Queue = maxConcurrentJobQueueFactory(Scheduler.Immediate, maxConcurrent) as IJobQueue<TJobInput, TJobOutput>,
				JobPauser = new ManualResetEventSlim(false),
				RemainingJobs = jobsToAdd
			};

			var allJobsLaunched = new ManualResetEventSlim(false);
			int jobCounter = 0;

			for (int i = 0; i < jobsToAdd; i++)
			{
				status.Queue.Add(A.Dummy<TJobInput>(), jobInput =>
				{
					if (Math.Min(maxConcurrent, status.RemainingJobs) == Interlocked.Increment(ref jobCounter))
						allJobsLaunched.Set();

					status.JobPauser.Wait();
					if (!jobShouldThrow)
						return A.Dummy<TJobOutput>();

					throw new ArgumentException();
				});
			}

			for (int j = 0; j < iterations; j++)
			{
				//wait for all jobs to start, then reset counter, allow them to complete, and reset allJobsLaunched
				status.ExpectedNumberOfJobsFiredUp = allJobsLaunched.Wait(TimeSpan.FromSeconds(10));
				allJobsLaunched.Reset();
				status.RemainingJobs = status.Queue.QueuedCount;
				Interlocked.Exchange(ref jobCounter, 0);
				if (j != iterations -1)
					status.JobPauser.Set();
			}

			return status;
		}

		[Theory]
		/*
		[InlineData(25, 5, 1, false)]
		[InlineData(1, 1, 1, false)]
		[InlineData(1, 3, 1, false)]
		[InlineData(25, 5, 2, false)]
		 */
		[InlineData(7, 2, 3, false)]
		/*
		[InlineData(25, 5, 1, true)]
		[InlineData(1, 1, 1, true)]
		[InlineData(1, 3, 1, true)]
		[InlineData(25, 5, 2, true)]
		[InlineData(7, 2, 3, true)]
		 * */
		public void Add_OnJobQueueInterface_AutomaticallyStartsOnlyUpToDefinedNumberOfJob(int jobsToAdd, int maxConcurrent, int iterations, bool jobShouldThrow)
		{
			var status = WaitForMaxConcurrentJobsToStart(jobsToAdd, maxConcurrent, iterations, jobShouldThrow);
			Assert.True(status.ExpectedNumberOfJobsFiredUp && status.Queue.RunningCount <= maxConcurrent);

			status.Queue.CancelOutstandingJobs();
			status.JobPauser.Set();
		}
	}

	public class ValueTypeAutoJobQueueTest :
		AutoJobQueueTest<int, int>
	{
		public ValueTypeAutoJobQueueTest()
			: base((scheduler, max) => new AutoJobQueue<int, int>(scheduler, max),
			max => new AutoJobQueue<int, int>(max))
		{ }
	}

	public class ReferenceTypeAutoJobQueueTest :
		AutoJobQueueTest<object, object>
	{
		public ReferenceTypeAutoJobQueueTest()
			: base((scheduler, max) => new AutoJobQueue<object, object>(scheduler, max),
			max => new AutoJobQueue<object, object>(max))
		{ }
	}
}