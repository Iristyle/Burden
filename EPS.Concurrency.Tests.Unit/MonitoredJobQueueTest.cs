using System;
using System.Collections.Generic;
using System.Threading;
using FakeItEasy;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class MonitoredJobQueueTest
	{
		//tedious tests for factory overloads
		[Fact]
		public void Create_Throws_OnNullFactory()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, 200));
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, 200, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, result => new JobQueueAction<int>(3), 200));
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, A.Fake<IJobResultInspector<int, int, int>>(), 200));
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, result => new JobQueueAction<int>(3), 200, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithAlternateInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, A.Fake<IJobResultInspector<int, int, int>>(), 200, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnNullJob()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, 200));
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, 200, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, result => new JobQueueAction<int>(3), 200));
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, A.Fake<IJobResultInspector<int, int, int>>(), 200));
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, result => new JobQueueAction<int>(3), 200, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithAlternateInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, A.Fake<IJobResultInspector<int, int, int>>(), 200, DurableJobQueueMonitor.DefaultPollingInterval));
		}


		[Fact]
		public void Create_Throws_OnNullInspector()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), (int input) => 3, null as Func<JobResult<int, int>, JobQueueAction<int>>, 200));
		}

		[Fact]
		public void Create_Throws_OnNullAlternateInspector()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), (int input) => 3, null as IJobResultInspector<int, int, int>, 200));
		}

		[Fact]
		public void Create_Throws_OnNullInspector_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), (int input) => 3, null as Func<JobResult<int, int>, JobQueueAction<int>>, 200, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnNullAlternateInspector_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), (int input) => 3, null as IJobResultInspector<int, int, int>, 200, DurableJobQueueMonitor.DefaultPollingInterval));
		}


		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, result => new JobQueueAction<int>(3), DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, A.Fake<IJobResultInspector<int, int, int>>(), DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, result => new JobQueueAction<int>(3), DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithAlternateInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, A.Fake<IJobResultInspector<int, int, int>>(), DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1, DurableJobQueueMonitor.DefaultPollingInterval));
		}


		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, 0));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, 0, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, result => new JobQueueAction<int>(3), 0));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, A.Fake<IJobResultInspector<int, int, int>>(), 0));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, result => new JobQueueAction<int>(3), 0, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithAlternateInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, A.Fake<IJobResultInspector<int, int, int>>(), 0, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Create_Throws_OnIntervalTooFast()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, 200, DurableJobQueueMonitor.MinimumAllowedPollingInterval - TimeSpan.FromTicks(1)));
		}

		[Fact]
		public void Create_Throws_OnIntervalTooFast_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, result => new JobQueueAction<int>(3), 200, DurableJobQueueMonitor.MinimumAllowedPollingInterval - TimeSpan.FromTicks(1)));
		}

		[Fact]
		public void Create_Throws_OnIntervalTooFast_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, A.Fake<IJobResultInspector<int, int, int>>(), 200, DurableJobQueueMonitor.MinimumAllowedPollingInterval - TimeSpan.FromTicks(1)));
		}

		[Fact]
		public void Create_Throws_OnIntervalTooSlow()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, 200, DurableJobQueueMonitor.MaximumAllowedPollingInterval + TimeSpan.FromTicks(1)));
		}

		[Fact]
		public void Create_Throws_OnIntervalTooSlow_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, result => new JobQueueAction<int>(3), 200, DurableJobQueueMonitor.MaximumAllowedPollingInterval + TimeSpan.FromTicks(1)));
		}

		[Fact]
		public void Create_Throws_OnIntervalTooSlow_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => MonitoredJobQueue.Create(null, (int input) => 3, A.Fake<IJobResultInspector<int, int, int>>(), 200, DurableJobQueueMonitor.MaximumAllowedPollingInterval + TimeSpan.FromTicks(1)));
		}


		[Fact]
		public void CreateMonitoredQueue_Calls_DurableStorageFactory_UsingPoisonT_WhenNoInspectorSpecified()
		{
			var factory = A.Fake<IDurableJobQueueFactory>();

			MonitoredJobQueue.Create(factory, (int i) => 3, 200);

			A.CallTo(() => factory.CreateDurableJobQueue<int, Poison<int>>()).MustHaveHappened(Repeated.Exactly.Once);
		}

		[Fact]
		public void CreateMonitoredQueue_Calls_DurableStorageFactory_MultipleTimes_ForMultipleFuncsOfSameType()
		{
			//for now, we expect every func to return a new monitored queue-- no caching of queues, etc
			var factory = A.Fake<IDurableJobQueueFactory>();

			int count = 10;
			for (int i = 0; i < count; i++)
			{
				MonitoredJobQueue.Create(factory, (int j) => i, 200);
			}

			A.CallTo(() => factory.CreateDurableJobQueue<int, Poison<int>>()).MustHaveHappened(Repeated.Exactly.Times(count));
		}

		[Fact]
		public void CreateMonitoredQueue_DoesNotReturnsSameMonitoredQueue_GivenSameFunc()
		{
			//for now, we expect every func to return a new monitored queue-- no caching of queues, etc
			var factory = A.Fake<IDurableJobQueueFactory>();

			Func<int, int> func = (int i) => i;
			var queue1 = MonitoredJobQueue.Create(factory, func, 200);
			var queue2 = MonitoredJobQueue.Create(factory, func, 200);

			Assert.NotSame(queue1, queue2);
		}
	}

	public abstract class MonitoredJobQueueTest<TInput, TOutput>
	: IMonitoredJobQueueTest<MonitoredJobQueue<TInput, TOutput, Poison<TInput>>, TInput, TOutput, Poison<TInput>>
	{
		protected Func<JobResult<TInput, TOutput>, JobQueueAction<Poison<TInput>>> inspector;
		protected List<TInput> jobsExecuted;
		protected List<JobResult<TInput, TOutput>> jobsInspected;
		protected ManualResetEventSlim onJobsInspected;

		//have to stack on 2 private constructors to properly init state -- UGLY -- don't try this at home kids!
		public MonitoredJobQueueTest(Func<TInput, TOutput> jobAction)
			: this(jobAction, new ManualResetEventSlim(false), new List<JobResult<TInput, TOutput>>())
		{ }

		private MonitoredJobQueueTest(Func<TInput, TOutput> jobAction, ManualResetEventSlim onJobsInspected, List<JobResult<TInput, TOutput>> jobsInspected)
			: this(jobAction,
			//run of the mill inspector
			result =>
			{
				jobsInspected.Add(result);
				onJobsInspected.Set();
				return JobResultInspector.FromJobSpecification(jobAction).Inspect(result);
			},
			new List<TInput>())
		{
			this.onJobsInspected = onJobsInspected;
			this.jobsInspected = jobsInspected;
		}

		private MonitoredJobQueueTest(Func<TInput, TOutput> jobAction,
			Func<JobResult<TInput, TOutput>, JobQueueAction<Poison<TInput>>> inspector,
			List<TInput> jobsExecuted)
			: base((scheduler, durableQueueFactory) =>
			{
				return MonitoredJobQueue.Create(durableQueueFactory, input =>
				{
					jobsExecuted.Add(input);
					return jobAction(input);
				}, inspector, DurableJobQueueMonitor.DefaultPollingInterval, 50, scheduler);
			})
		{
			this.inspector = inspector;
			this.jobsExecuted = jobsExecuted;
		}


		[Fact(Skip = "Need to cook up some tests here")]
		public void CancelQueuedAndWaitForExecutingJobsToComplete()
		{
		}
	}

	public class MonitoredJobQueueValueTypeTest
		: MonitoredJobQueueTest<int, int>
	{
		public MonitoredJobQueueValueTypeTest()
			: base(input => input * 3)
		{ }
	}

	public class MonitoredJobQueueReferenceTypeTest
	: MonitoredJobQueueTest<string, string>
	{
		public MonitoredJobQueueReferenceTypeTest()
			: base(input => input + "garbage")
		{ }
	}
}