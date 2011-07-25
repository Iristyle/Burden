using System;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IDurableJobQueueFactoryTest<T>
		where T: IDurableJobQueueFactory
	{
		private Func<T> factory;

		public IDurableJobQueueFactoryTest(Func<T> factory)
		{
			this.factory = factory;
		}
		
		[Fact]
		public void CreateDurableQueue_IsNotNull_ValueTypes()
		{
			var instance = factory();
			Assert.NotNull(instance.CreateDurableJobQueue<int, int>());
		}

		class Foo
		{ }

		[Fact]
		public void CreateDurableQueue_IsNotNull_ReferenceTypes()
		{
			var instance = factory();
			Assert.NotNull(instance.CreateDurableJobQueue<Foo, Foo>());
		}

		[Fact]
		public void CreateDurableQueue_IsNotNull_PoionedValueType()
		{
			var instance = factory();
			Assert.NotNull(instance.CreateDurableJobQueue<int, Poison<int>>());
		}

		[Fact]
		public void CreateDurableQueue_IsNotNull_PoionedReferenceType()
		{
			var instance = factory();
			Assert.NotNull(instance.CreateDurableJobQueue<Foo, Poison<Foo>>());
		}
	}
}