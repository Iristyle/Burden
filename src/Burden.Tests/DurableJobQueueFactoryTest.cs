using System;
using Xunit;

namespace Burden.Tests
{
	public abstract class DurableJobQueueFactoryTest<T>
		where T: IDurableJobQueueFactory
	{
		private Func<T> _factory;

		protected DurableJobQueueFactoryTest(Func<T> factory)
		{
			this._factory = factory;
		}
		
		[Fact]
		public void CreateDurableQueue_IsNotNull_ValueTypes()
		{
			var instance = _factory();
			Assert.NotNull(instance.CreateDurableJobQueue<int, int>());
		}

		class Foo
		{ }

		[Fact]
		public void CreateDurableQueue_IsNotNull_ReferenceTypes()
		{
			var instance = _factory();
			Assert.NotNull(instance.CreateDurableJobQueue<Foo, Foo>());
		}

		[Fact]
		public void CreateDurableQueue_IsNotNull_PoisonedValueType()
		{
			var instance = _factory();
			Assert.NotNull(instance.CreateDurableJobQueue<int, Poison<int>>());
		}

		[Fact]
		public void CreateDurableQueue_IsNotNull_PoisonedReferenceType()
		{
			var instance = _factory();
			Assert.NotNull(instance.CreateDurableJobQueue<Foo, Poison<Foo>>());
		}
	}
}