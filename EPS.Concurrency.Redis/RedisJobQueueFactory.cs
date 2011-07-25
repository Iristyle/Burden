using System;
using ServiceStack.Redis;

namespace EPS.Concurrency.Redis
{
	/// <summary>	Factory used to create instances of RedisJobQueue.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	public class RedisJobQueueFactory : IDurableJobQueueFactory
	{
		private readonly IRedisClientsManager clientManager;
		private readonly QueueNames queueNames;

		/// <summary>	Constructor. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the cilentManager or queueNames are null. </exception>
		/// <param name="clientManager">	The Redis client manager. </param>
		/// <param name="queueNames">   	List of names of the queues. </param>
		public RedisJobQueueFactory(IRedisClientsManager clientManager, QueueNames queueNames)
		{
			if (null == clientManager) { throw new ArgumentNullException("clientManager"); }
			if (null == queueNames) { throw new ArgumentNullException("queueNames"); }

			this.clientManager = clientManager;
			this.queueNames = queueNames;
		}

		/// <summary>	Creates the durable job queue. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <returns>	A new instance of a RedisJobQueue. </returns>
		public IDurableJobQueue<TInput, TPoison> CreateDurableJobQueue<TInput, TPoison>()
		{
			return new RedisJobQueue<TInput, TPoison>(clientManager, queueNames);
		}
	}
}