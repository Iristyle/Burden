using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ServiceStack.Redis;

namespace EPS.Concurrency.Redis
{		
	/// <summary>	An implementation of a durable job queue based on Redis.  </summary>
	/// <remarks>	7/19/2011. </remarks>
	public class RedisJobQueue<TQueue, TQueuePoison>
		: IDurableJobQueue<TQueue, TQueuePoison>
	{
		private readonly Func<IRedisClient> _redisClientFactory;
		private readonly QueueNames _queueNames;

		/// <summary>	Create a new instance of the Redis based durable job queue given a client factory and the names of the queues. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the Func{IRedisClient} or QueueNames are null. </exception>
		/// <param name="redisClientFactory">	A simple factory that returns a client that we must dispose of. </param>
		/// <param name="queueNames">		 	List of names of the queues. </param>
		public RedisJobQueue(Func<IRedisClient> redisClientFactory, QueueNames queueNames)
		{
			if (null == redisClientFactory) { throw new ArgumentNullException("redisClientFactory"); }
			if (null == queueNames) { throw new ArgumentNullException("queueNames"); }

			this._redisClientFactory = redisClientFactory;
			this._queueNames = new QueueNames(
				String.Format(CultureInfo.InvariantCulture, "q:{0}", queueNames.Request),
				String.Format(CultureInfo.InvariantCulture, "q:{0}", queueNames.Pending),
				String.Format(CultureInfo.InvariantCulture, "q:{0}", queueNames.Poison));
		}

		/// <summary>	Queues an item. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <exception cref="ArgumentNullException">  	Thrown when the item is null. </exception>
		/// <param name="item">	The item. </param>
		public void Queue(TQueue item)
		{
			if (null == item) { throw new ArgumentNullException("item"); }

			using (var client = _redisClientFactory())
			using (var queueClient = client.As<TQueue>())
			{
				var requestList = queueClient.Lists[_queueNames.Request];
				requestList.Prepend(item);
			}
		}

		/// <summary>	Resets all items from the pending state to the queued state. </summary>
		/// <remarks>	7/19/2011. </remarks>
		public void ResetAllPendingToQueued()
		{
			using (var client = _redisClientFactory())
			using (var queueClient = client.As<TQueue>())
			{
				var requestQueue = queueClient.Lists[_queueNames.Request];
				var pendingQueue = queueClient.Lists[_queueNames.Pending];

				while (pendingQueue.Count != 0)
				{
					pendingQueue.PopAndPush(requestQueue);
				}
			}
		}

		/// <summary>	Gets a list of all the poisoned items in this queue. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <returns>	An enumerator that allows foreach to be used to process poisoned items in this collection. </returns>
		public IEnumerable<TQueuePoison> GetPoisoned()
		{
			using (var client = _redisClientFactory())
			using (var queuePoisonClient = client.As<TQueuePoison>())
			{
				return queuePoisonClient.Lists[_queueNames.Poison].Reverse();
			}
		}

		/// <summary>	If there is an available item in the request queue, it will be move to the pending queue and returned. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <returns>	An item if there was one available, otherwise null. </returns>
		public IItem<TQueue> NextQueuedItem()
		{
			using (var client = _redisClientFactory())
			using (var queueClient = client.As<TQueue>())
			{
				var requestQueue = queueClient.Lists[_queueNames.Request];
				var pendingQueue = queueClient.Lists[_queueNames.Pending];

				//TODO: 8-3-2011 -- I believe there is a race condition here and we need to use a distributed lock somehow
				return requestQueue.Count == 0 ? Item.None<TQueue>() : 
					//moves an item out of 'request' and into 'pending' (which will be reverted at start up, should the process be terminated, etc)
					Item.From(queueClient.PopAndPushItemBetweenLists(requestQueue, pendingQueue));
			}
		}

		/// <summary>	Poisons an item in the pending queue, by putting a new item in the poison queue. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <exception cref="ArgumentNullException">  	Thrown when the item or its poisoned representation are null. </exception>
		/// <param name="item">		   	The item. </param>
		/// <param name="poisonedItem">	The poisoned equivalent of the item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Poison(TQueue item, TQueuePoison poisonedItem)
		{
			if (null == item) { throw new ArgumentNullException("item"); }
			if (null == poisonedItem) { throw new ArgumentNullException("poisonedItem"); }

			using (var client = _redisClientFactory())
			using (var queueClient = client.As<TQueue>())
			using (var queuePoisonClient = client.As<TQueuePoison>())
			{
				var pendingQueue = queueClient.Lists[_queueNames.Pending];
				var poisonQueue = queuePoisonClient.Lists[_queueNames.Poison];

				if (!pendingQueue.Remove(item))
					return false;

				poisonQueue.Prepend(poisonedItem);

				return true;
			}
		}

		/// <summary>	Completes a pending item, by removing it from the queue. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <exception cref="ArgumentNullException">  	Thrown when the item is null. </exception>
		/// <param name="item">	The item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Complete(TQueue item)
		{
			if (null == item) { throw new ArgumentNullException("item"); }

			using (var client = _redisClientFactory())
			using (var queueClient = client.As<TQueue>())
			{
				return queueClient.Lists[_queueNames.Pending].Remove(item);
			}
		}

		/// <summary>	Gets a list of all the queued items in this queue. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <returns>	An enumerator that allows foreach to be used to process poisoned items in this collection. </returns>
		public IEnumerable<TQueue> GetQueued()
		{
			using (var client = _redisClientFactory())
			using (var queueClient = client.As<TQueue>())
			{
				return queueClient.Lists[_queueNames.Request].Reverse();
			}
		}

		/// <summary>	Gets a list of all the pending items in this queue. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <returns>	An enumerator that allows foreach to be used to process poisoned items in this collection. </returns>
		public IEnumerable<TQueue> GetPending()
		{
			using (var client = _redisClientFactory())
			using (var queueClient = client.As<TQueue>())
			{
				return queueClient.Lists[_queueNames.Pending].Reverse();
			}
		}

		/// <summary>	Deletes the given poisonedItem. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <exception cref="ArgumentNullException">  	Thrown when the given item is null. </exception>
		/// <param name="poisonedItem">	The OrderSynchronizationResult to delete. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Delete(TQueuePoison poisonedItem)
		{
			if (null == poisonedItem) { throw new ArgumentNullException("poisonedItem"); }

			using (var client = _redisClientFactory())
			using (var queuePoisonClient = client.As<TQueuePoison>())
			{
				var poisonQueue = queuePoisonClient.Lists[_queueNames.Poison];
				return poisonQueue.Remove(poisonedItem);
			}
		}
	}
}