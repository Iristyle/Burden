using System;
using Xunit;
using EPS.Dynamic;
using System.Reactive.Concurrency;

namespace EPS.Concurrency.Tests.Unit
{	
	public class AutoJobQueueTest<TJobInput, TJobOutput> :
		IJobQueueTest<AutoJobQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		public AutoJobQueueTest(Func<IScheduler, AutoJobQueue<TJobInput, TJobOutput>> jobQueueFactory)
			: base(jobQueueFactory)
		{ }

		[Fact]
		public void Add_ThrowsOnNullItemForReferenceTypesOnAutostartOverload()
		{
			var queue = jobQueueFactory(Scheduler.Immediate);
			if (typeof(TJobInput).IsValueType)
				return;

			var item = (null as object).Cast<TJobInput>();

			Assert.Throws<ArgumentNullException>(() => queue.Add(item, jobInput => null as IObservable<TJobOutput>, true));
		}

		[Fact]
		public void Add_ThrowsOnNullAsyncStartWithAutostartOverload()
		{
			var queue = jobQueueFactory(Scheduler.Immediate);

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, IObservable<TJobOutput>>, true));
		}

		[Fact]
		public void Add_ThrowsOnNullObservableWithAutostartOverload()
		{
			var queue = jobQueueFactory(Scheduler.Immediate);

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), (input) => null as IObservable<TJobOutput>, true));
		}
	}

	/*
	public class IntegerBasedAutoJobQueueTest :
		AutoJobQueueTest<int>
	{
		public IntegerBasedAutoJobQueueTest()
			: base(() => new AutoJobQueue<int>(200))
		{ }
	}
	*/
}

