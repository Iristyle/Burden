using System;
using Xunit;
using System.Threading;
using FakeItEasy;
using EPS.Dynamic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IJobQueueTest<TJobQueue, TJobInput, TJobOutput>
		where TJobQueue : IJobQueue<TJobInput, TJobOutput>
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
		public void Add_ThrowsOnNullItemForValueFactoryOverload()
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), jobInput => null as IObservable<TJobOutput>));
		}

		[Fact]
		public void Add_ThrowsOnNullObservableFactory()
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, IObservable<TJobOutput>>));
		}

		[Fact]
		public void Add_RunsJob()
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			var jobExecuted = new ManualResetEventSlim(false);

			queue.Add(A.Dummy<TJobInput>(), job =>
			{
				jobExecuted.Set();
				return A.Dummy<TJobOutput>();
			});
			queue.StartNext();

			bool waitResult = jobExecuted.Wait(TimeSpan.FromSeconds(1));
			Assert.True(waitResult);
		}

		[Fact]
		public void Add_RunsJob_CallsWhenJobCompletes()
		{
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			var completedEvent = new ManualResetEventSlim(false);
			queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate).Subscribe((notification) => completedEvent.Set());
			queue.Add(A.Dummy<TJobInput>(), job => { return A.Dummy<TJobOutput>(); });
			queue.StartNext();

			bool waitResult = completedEvent.Wait(TimeSpan.FromSeconds(1));
			Assert.True(waitResult);
		}
		

		[Fact(Skip = "Write tests to ensure our JobQueue is working properly")]
		public void CancelOutstandingJobs_Tests()
		{
			
		}

		[Fact(Skip = "Write tests to ensure our JobQueue is working properly")]
		public void RunningCount_Tests()
		{
		}

		[Fact(Skip = "Write tests to ensure our JobQueue is working properly")]
		public void QueuedCount_Tests()
		{
		}

		[Fact(Skip = "Write tests to ensure our JobQueue is working properly")]
		public void StartNext_Tests()
		{
		}

		[Fact(Skip = "Write tests to ensure our JobQueue is working properly")]
		public void StartUpTo_Tests()
		{
		}

		[Fact(Skip = "Write tests to ensure our JobQueue is working properly")]
		public void WhenJobCompletes_Tests()
		{
		}

		[Fact(Skip = "Write tests to ensure our JobQueue is working properly")]
		public void WhenQueueEmpty_Tests()
		{
		}

		[Fact(Skip = "Write tests to ensure our JobQueue is working properly")]
		public void WhenJobFails_Tests()
		{
		}

		[Fact]
		public void WhenJobCompletes_FailureNotificationsAlwaysContainsJobQueueExceptions()
		{

		}
	}
}