using System;
using EqualityComparer;
using Xunit;

namespace Burden.Tests
{
	public class TransientValueTypeJobQueueTest
	: TransientJobQueueTest<int, int>
	{
		public TransientValueTypeJobQueueTest()
			: base(input => 3)
		{ }
	}

	public class TransientReferenceTypeJobQueueTest
		: TransientJobQueueTest<string, string>
	{
		public TransientReferenceTypeJobQueueTest()
			: base(input => "foo")
		{ }
	}

	public abstract class TransientJobQueueTest<TQueue, TQueuePoison>
		: DurableJobQueueTest<TransientJobQueue<TQueue, TQueuePoison>, TQueue, TQueuePoison>
	{
		protected TransientJobQueueTest(Func<TQueue, TQueuePoison> poisonConverter)
			: base(() => new TransientJobQueue<TQueue, TQueuePoison>(GenericEqualityComparer<TQueue>.ByAllMembers(), GenericEqualityComparer<TQueuePoison>.ByAllMembers()), poisonConverter)
		{ }

		[Fact]
		public void Constructor_Throws_OnNullItemComparer()
		{
			Assert.Throws<ArgumentNullException>(() => new TransientJobQueue<TQueue, TQueuePoison>(null, GenericEqualityComparer<TQueuePoison>.ByAllMembers()));
		}

		[Fact]
		public void Constructor_Throws_OnNullPoisonComparer()
		{
			Assert.Throws<ArgumentNullException>(() => new TransientJobQueue<TQueue, TQueuePoison>(GenericEqualityComparer<TQueue>.ByAllMembers(), null));
		}
	}
}