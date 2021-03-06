using System;
using ServiceStack.Redis;

namespace Burden.Redis
{
	/// <summary>	Factory used to create instances of RedisJobQueue.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	public class RedisJobQueueFactory 
		: IDurableJobQueueFactory
	{
		private readonly Func<IRedisClient> _redisClientFactory;
		private readonly QueueNames _queueNames;

		/// <summary>	Constructor. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the client factory or queueNames are null. </exception>
		/// <param name="redisClientsManager">	The Redis client manager. </param>
		/// <param name="queueNames">   	List of names of the queues. </param>
		public RedisJobQueueFactory(Func<IRedisClient> redisClientFactory, QueueNames queueNames)
		{
			if (null == redisClientFactory) { throw new ArgumentNullException("redisClientFactory"); }
			if (null == queueNames) { throw new ArgumentNullException("queueNames"); }

			this._redisClientFactory = redisClientFactory;
			this._queueNames = queueNames;
		}

		/// <summary>	Creates the durable job queue. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <returns>	A new instance of a RedisJobQueue. </returns>
		public IDurableJobQueue<TInput, TPoison> CreateDurableJobQueue<TInput, TPoison>()
		{
			return new RedisJobQueue<TInput, TPoison>(_redisClientFactory, _queueNames);
		}
	}
}