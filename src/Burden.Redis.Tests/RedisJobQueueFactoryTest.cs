using System;
using EPS.Concurrency.Tests.Unit;
using Xunit;

namespace EPS.Concurrency.Redis.Tests.Integration
{
	public class RedisJobQueueFactoryTest
		: DurableJobQueueFactoryTest<RedisJobQueueFactory>
	{
		public RedisJobQueueFactoryTest()
			: base(() => new RedisJobQueueFactory(() => TestConnection.GetClientManager().GetClient(), QueueNames.Default))
		{ }

		[Fact]
		public void Constructor_Throws_OnNullClientManager()
		{
			Assert.Throws<ArgumentNullException>(() => new RedisJobQueueFactory(null, QueueNames.Default));
		}

		[Fact]
		public void Constructor_Throws_OnNullQueueNames()
		{
			Assert.Throws<ArgumentNullException>(() => new RedisJobQueueFactory(() => TestConnection.GetClientManager().GetClient(), null));
		}
	}
}