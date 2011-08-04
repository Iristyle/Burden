using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 30)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, result => new JobQueueAction<int>(3))) { } });
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, A.Fake<IJobResultInspector<int, int, int>>())) { } });
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, result => new JobQueueAction<int>(3), DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullFactory_WithAlternateInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, A.Fake<IJobResultInspector<int, int, int>>(), DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullJob()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, 200)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, 200, DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, 200, result => new JobQueueAction<int>(3))) { } });
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, 200, A.Fake<IJobResultInspector<int, int, int>>())) { } });
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, 200, result => new JobQueueAction<int>(3), DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullJob_WithAlternateInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), null as Func<int, int>, 200, A.Fake<IJobResultInspector<int, int, int>>(), DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}


		[Fact]
		public void Create_Throws_OnNullInspector()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), (int input) => 3, 200, null as Func<JobResult<int, int>, JobQueueAction<int>>)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullAlternateInspector()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), (int input) => 3, 200, null as IJobResultInspector<int, int, int>)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullInspector_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), (int input) => 3, 200, null as Func<JobResult<int, int>, JobQueueAction<int>>, DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnNullAlternateInspector_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(A.Fake<IDurableJobQueueFactory>(), (int input) => 3, 200, null as IJobResultInspector<int, int, int>, DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}


		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1)) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, AutoJobExecutionQueue<object,object>.MaxAllowedConcurrentJobs + 1, DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, AutoJobExecutionQueue<object,object>.MaxAllowedConcurrentJobs + 1, result => new JobQueueAction<int>(3))) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, AutoJobExecutionQueue<object,object>.MaxAllowedConcurrentJobs + 1, A.Fake<IJobResultInspector<int, int, int>>())) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, AutoJobExecutionQueue<object,object>.MaxAllowedConcurrentJobs + 1, result => new JobQueueAction<int>(3), DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooLarge_WithAlternateInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, AutoJobExecutionQueue<object,object>.MaxAllowedConcurrentJobs + 1, A.Fake<IJobResultInspector<int, int, int>>(), DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 0)) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 0, DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 0, result => new JobQueueAction<int>(3))) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 0, A.Fake<IJobResultInspector<int, int, int>>())) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 0, result => new JobQueueAction<int>(3), DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnMaximumItemsTooSmall_WithAlternateInspectorAndIntervalSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 0, A.Fake<IJobResultInspector<int, int, int>>(), DurableJobQueueMonitor.DefaultPollingInterval)) { } });
		}

		[Fact]
		public void Create_Throws_OnIntervalTooFast()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, DurableJobQueueMonitor.MinimumAllowedPollingInterval - TimeSpan.FromTicks(1))) { } });
		}

		[Fact]
		public void Create_Throws_OnIntervalTooFast_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, result => new JobQueueAction<int>(3), DurableJobQueueMonitor.MinimumAllowedPollingInterval - TimeSpan.FromTicks(1))) { } });
		}

		[Fact]
		public void Create_Throws_OnIntervalTooFast_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, A.Fake<IJobResultInspector<int, int, int>>(), DurableJobQueueMonitor.MinimumAllowedPollingInterval - TimeSpan.FromTicks(1))) { } });
		}

		[Fact]
		public void Create_Throws_OnIntervalTooSlow()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, DurableJobQueueMonitor.MaximumAllowedPollingInterval + TimeSpan.FromTicks(1))) { } });
		}

		[Fact]
		public void Create_Throws_OnIntervalTooSlow_WithInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, result => new JobQueueAction<int>(3), DurableJobQueueMonitor.MaximumAllowedPollingInterval + TimeSpan.FromTicks(1))) { } });
		}

		[Fact]
		public void Create_Throws_OnIntervalTooSlow_WithAlternateInspectorSpecified()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var m = MonitoredJobQueue.Create(null, (int input) => 3, 200, A.Fake<IJobResultInspector<int, int, int>>(), DurableJobQueueMonitor.MaximumAllowedPollingInterval + TimeSpan.FromTicks(1))) { } });
		}
	}

	public abstract class MonitoredJobQueueTest<TInput, TOutput>
	: MonitoredJobQueueTestBase<MonitoredJobQueue<TInput, TOutput, Poison<TInput>>, TInput, TOutput, Poison<TInput>>
	{
		private readonly Func<JobResult<TInput, TOutput>, JobQueueAction<Poison<TInput>>> _inspector;
		private readonly List<TInput> _jobsExecuted;
		private readonly List<JobResult<TInput, TOutput>> _jobsInspected;
		private readonly ManualResetEventSlim _onJobsInspected;

		//have to stack on 2 private constructors to properly init state -- UGLY -- don't try this at home kids!
		protected MonitoredJobQueueTest(Func<TInput, TOutput> jobAction)
			: this(jobAction, new ManualResetEventSlim(false), new List<JobResult<TInput, TOutput>>())
		{ }

		private MonitoredJobQueueTest(Func<TInput, TOutput> jobAction, ManualResetEventSlim onJobsInspected, List<JobResult<
		TInput, TOutput>> jobsInspected)
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
			this._onJobsInspected = onJobsInspected;
			this._jobsInspected = jobsInspected;
		}

		private MonitoredJobQueueTest(Func<TInput, TOutput> jobAction,
		Func<JobResult<TInput, TOutput>, JobQueueAction<Poison<TInput>>> inspector,
		List<TInput> jobsExecuted)
			: base((scheduler, durableQueueFactory) =>{
				return MonitoredJobQueue.Create(durableQueueFactory, input =>
				{
					jobsExecuted.Add(input);
					return jobAction(input);
				}, AutoJobExecutionQueue<TInput, TOutput>.DefaultConcurrent, inspector, 
				DurableJobQueueMonitor.DefaultPollingInterval, 50, null, scheduler);})
		{
			this._inspector = inspector;
			this._jobsExecuted = jobsExecuted;
		}

		protected ReadOnlyCollection<TInput> JobsExecuted
		{
			get { return new ReadOnlyCollection<TInput>(_jobsExecuted); }
		}

		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Perfectly acceptable nested usage")]
		protected ReadOnlyCollection<JobResult<TInput, TOutput>> JobsInspected
		{
			get { return new ReadOnlyCollection<JobResult<TInput,TOutput>>(_jobsInspected); }
		}

		[Fact]
		public void CreateMonitoredQueue_Calls_DurableStorageFactory_UsingPoisonT_WhenNoInspectorSpecified()
		{
			var factory = A.Fake<IDurableJobQueueFactory>();

			using (var queue = MonitoredJobQueue.Create(factory, (int i) => 3, 200))
			{
				A.CallTo(() => factory.CreateDurableJobQueue<int, Poison<int>>()).MustHaveHappened(Repeated.Exactly.Once);
			}
		}

		[Fact]
		public void CreateMonitoredQueue_Calls_DurableStorageFactory_MultipleTimes_ForMultipleJobFunctionsOfSameType()
		{
			//for now, we expect every func to return a new monitored queue-- no caching of queues, etc
			var factory = A.Fake<IDurableJobQueueFactory>();

			int count = 10;
			var queues = new List<IDisposable>(count);
			for (int i = 0; i < count; i++)
			{
				queues.Add(MonitoredJobQueue.Create(factory, (int j) => i, 200));
			}

			A.CallTo(() => factory.CreateDurableJobQueue<int, Poison<int>>()).MustHaveHappened(Repeated.Exactly.Times(count));
			
			foreach (var q in queues)
				q.Dispose();
		}

		[Fact]
		public void CreateMonitoredQueue_DoesNotReturnsSameMonitoredQueue_GivenSameFunc()
		{
			//for now, we expect every func to return a new monitored queue-- no caching of queues, etc
			var factory = A.Fake<IDurableJobQueueFactory>();

			Func<int, int> func = (int i) => i;
			using (var queue1 = MonitoredJobQueue.Create(factory, func, 200))
			using (var queue2 = MonitoredJobQueue.Create(factory, func, 200))
			{
				Assert.NotSame(queue1, queue2);
			}
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