using System;
using Xunit;
using System.Threading;
using FakeItEasy;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IJobQueueTest<T>
		where T: IJobQueue
	{
		private readonly Func<T> jobQueueFactory;

		public IJobQueueTest(Func<T> jobQueueFactory)
		{
			this.jobQueueFactory = jobQueueFactory;
		}

		[Fact]
		public void Add_ThrowsOnNullAction()
		{
			T queue = jobQueueFactory();

			Assert.Throws<ArgumentNullException>(() => queue.Add(null as Action));
		}

		[Fact]
		public void Add_ThrowsOnNullObservableFactory()
		{
			T queue = jobQueueFactory();

			Assert.Throws<ArgumentNullException>(() => queue.Add(null as Func<IObservable<System.Reactive.Unit>>));
		}

		[Fact]
		public void Add_ThrowsOnNullObservable()
		{
			T queue = jobQueueFactory();

			Assert.Throws<ArgumentNullException>(() => queue.Add(() => null as IObservable<System.Reactive.Unit>));
		}

		[Fact]
		public void Add_RunsJob()
		{
			T queue = jobQueueFactory();
			ManualResetEvent executedEvent = new ManualResetEvent(false);
			queue.Add(() => executedEvent.Set());

			Assert.True(executedEvent.WaitOne(TimeSpan.FromSeconds(1).Milliseconds));
		}

		[Fact]
		public void Add_RunsJob_CallsWhenJobCompletes()
		{
			T queue = jobQueueFactory();
			ManualResetEvent completedEvent = new ManualResetEvent(false);
			queue.WhenJobCompletes.Subscribe((notification) => completedEvent.Set());
			queue.Add(() => { });

			Assert.True(completedEvent.WaitOne(TimeSpan.FromSeconds(1).Milliseconds));
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
	}
}