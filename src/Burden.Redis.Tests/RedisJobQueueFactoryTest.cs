using System;
using Burden.Tests;
using Xunit;

namespace Burden.Redis.Tests
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