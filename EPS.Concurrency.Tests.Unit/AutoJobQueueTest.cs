using System;
using Xunit;
using EPS.Dynamic;

namespace EPS.Concurrency.Tests.Unit
{	
	public class AutoJobQueueTest<TJobInput, TJobOutput> :
		IJobQueueTest<AutoJobQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		public AutoJobQueueTest(Func<AutoJobQueue<TJobInput, TJobOutput>> jobQueueFactory)
			: base(jobQueueFactory)
		{ }

		[Fact]
		public void Add_ThrowsOnNullItemForReferenceTypesOnAutostartOverload()
		{
			var queue = jobQueueFactory();
			if (typeof(TJobInput).IsValueType)
				return;

			var item = (null as object).Cast<TJobInput>();

			Assert.Throws<ArgumentNullException>(() => queue.Add(item, jobInput => null as IObservable<TJobOutput>, true));
		}

		[Fact]
		public void Add_ThrowsOnNullAsyncStartWithAutostartOverload()
		{
			var queue = jobQueueFactory();

			Assert.Throws<ArgumentNullException>(() => queue.Add(default(TJobInput), null as Func<TJobInput, IObservable<TJobOutput>>, true));
		}

		[Fact]
		public void Add_ThrowsOnNullObservableWithAutostartOverload()
		{
			var queue = jobQueueFactory();

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

