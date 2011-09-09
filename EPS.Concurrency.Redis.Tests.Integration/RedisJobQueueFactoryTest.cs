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
		private static RedisConnection redisConnection = RedisHostManager.Current();
		private static IRedisClientsManager _clientManager;
		private static IRedisClientsManager GetClientManager()
		{
			if (null == _clientManager)
				_clientManager = new BasicRedisClientManager(new string[] { String.Format(CultureInfo.InvariantCulture, "{0}:{1}", redisConnection.Host, redisConnection.Port) });
			return _clientManager;
		}

		public RedisJobQueueFactoryTest()
			: base(() => new RedisJobQueueFactory(() => GetClientManager().GetClient(), QueueNames.Default))
		{ }

		[Fact]
		public void Constructor_Throws_OnNullClientManager()
		{
			Assert.Throws<ArgumentNullException>(() => new RedisJobQueueFactory(null, QueueNames.Default));
		}

		[Fact]
		public void Constructor_Throws_OnNullQueueNames()
		{
			Assert.Throws<ArgumentNullException>(() => new RedisJobQueueFactory(() => GetClientManager().GetClient(), null));
		}
	}
}