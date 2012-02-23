using System;
using System.Globalization;
using RedisIntegration;
using ServiceStack.Redis;

namespace Burden.Redis.Tests
{
	public static class TestConnection
	{
		private static Connection _redisConnection = HostManager.RunInstance();
		private static IRedisClientsManager _redisClientsManager;

		public static IRedisClientsManager GetClientManager(bool flush = true)
		{
			if (null == _redisClientsManager)
				_redisClientsManager = new BasicRedisClientManager(new string[] { String.Format(CultureInfo.InvariantCulture, "{0}:{1}", _redisConnection.Host, _redisConnection.Port) });
			
			if (flush)
			{
				using (var client = _redisClientsManager.GetClient())
				{
					client.FlushAll();
				}
			}

			return _redisClientsManager;
		}
	}
}