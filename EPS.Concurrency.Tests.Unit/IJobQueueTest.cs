using System;
using Xunit;
using System.Threading;
using FakeItEasy;
using EPS.Dynamic;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IJobQueueTest<TJobQueue, TJobInput, TJobOutput>
		where TJobQueue : IJobQueue<TJobInput, TJobOutput>
	{
		protected readonly Func<TJobQueue> jobQueueFactory;

		public IJobQueueTest(Func<TJobQueue> jobQueueFactory)
		{
			this.jobQueueFactory = jobQueueFactory;
		}

		[Fact]
		public void Add_ThrowsOnNullItemForReferenceTypes()
		{
			TJobQueue queue = jobQueueFactory();
			if (typeof(TJobInput).IsValueType)
				return;

			var item = (null as object).Cast<TJobInput>();

			Assert.Throws<ArgumentNullException>(() => queue.Add(item, jobInput => default(TJobOutput)));
		}

		[Fact]
		public void Add_ThrowsOnNullAction()
		{
			TJobQueue queue = jobQueueFactory();

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, TJobOutput>));
		}

		[Fact]
		public void Add_ThrowsOnNullItemForValueFactoryOverload()
		{
			TJobQueue queue = jobQueueFactory();

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), jobInput => null as IObservable<TJobOutput>));
		}

		[Fact]
		public void Add_ThrowsOnNullObservableFactory()
		{
			TJobQueue queue = jobQueueFactory();

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, IObservable<TJobOutput>>));
		}

		[Fact]
		public void Add_ThrowsOnNullObservable()
		{
			TJobQueue queue = jobQueueFactory();

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), (input) => null as IObservable<TJobOutput>));
		}

		[Fact]
		public void Add_RunsJob()
		{
			TJobQueue queue = jobQueueFactory();
			ManualResetEvent executedEvent = new ManualResetEvent(false);
			queue.Add(default(TJobInput), job =>
			{
				executedEvent.Set();
				return null;
			});

			Assert.True(executedEvent.WaitOne(TimeSpan.FromSeconds(1).Milliseconds));
		}

		[Fact]
		public void Add_RunsJob_CallsWhenJobCompletes()
		{
			TJobQueue queue = jobQueueFactory();
			ManualResetEvent completedEvent = new ManualResetEvent(false);
			queue.WhenJobCompletes.Subscribe((notification) => completedEvent.Set());
			queue.Add(default(TJobInput), job => { return null; });

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

		[Fact]
		public void WhenJobCompletes_FailureNotificationsAlwaysContainsJobQueueExceptions()
		{
		}
	}
}