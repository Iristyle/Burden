using System;
using System.Globalization;
using EPS.Concurrency.Tests.Unit;
using EPS.Test.Redis;
using ServiceStack.Redis;
using Xunit;

namespace EPS.Concurrency.Redis.Tests.Integration
{
	public class RedisJobQueueFactoryTest
		: DurableJobQueueFactoryTest<RedisJobQueueFactory>
	{
		private static RedisConnection connection = RedisHostManager.Current();

		private static IRedisClientsManager GetClientManager()
		{
			return new BasicRedisClientManager(String.Format(CultureInfo.InvariantCulture, "{0}:{1}", connection.Host, connection.Port));
		}

		public RedisJobQueueFactoryTest()
			: base(() => new RedisJobQueueFactory(GetClientManager(), QueueNames.Default))
		{ }

		[Fact]
		public void Constructor_Throws_OnNullClientManager()
		{
			Assert.Throws<ArgumentNullException>(() => new RedisJobQueueFactory(null, QueueNames.Default));
		}

		[Fact]
		public void Constructor_Throws_OnNullQueueNames()
		{
			Assert.Throws<ArgumentNullException>(() => new RedisJobQueueFactory(GetClientManager(), null));
		}
	}
}