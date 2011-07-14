using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using EPS.Dynamic;
using FakeItEasy;
using Xunit;
using System.Reactive;

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

		private Tuple<TJobInput, TJobOutput, Notification<JobResult<TJobInput, TJobOutput>>> GetDataFromFauxJobExecution()
		{
			var input = A.Dummy<TJobInput>();
			var output = A.Dummy<TJobOutput>();

			Notification<JobResult<TJobInput, TJobOutput>> notification = null;
			TJobQueue queue = jobQueueFactory(Scheduler.Immediate);
			var completedEvent = new ManualResetEventSlim(false);
			queue.WhenJobCompletes.ObserveOn(Scheduler.Immediate).Subscribe(n =>
			{
				notification = n;
				completedEvent.Set();
			});
			queue.Add(input, job => { return output; });
			queue.StartNext();

			completedEvent.Wait();
			completedEvent.Wait();

			return Tuple.Create(input, output, notification);			
		}

		[Fact]
		public void WhenJobCompletes_NotificationContainsOriginalInput()
		{
			var data = GetDataFromFauxJobExecution();

			Assert.Same(data.Item1, data.Item3.Value.Input);
		}

		[Fact]
		public void WhenJobCompletes_NotificationContainsOriginalOutput()
		{
			var data = GetDataFromFauxJobExecution();

			Assert.Same(data.Item2, data.Item3.Value.Output);
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