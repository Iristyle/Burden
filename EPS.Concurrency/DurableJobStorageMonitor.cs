using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace EPS.Concurrency
{
	/// <summary>	Provides a means for polling a durable job storage queue and publishing those over an in-memory IObservable. </summary>
	/// <remarks>	7/15/2011. </remarks>
	/// <typeparam name="TQueue">	   	Type of the queue. </typeparam>
	/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
	public class DurableJobStorageMonitor<TQueue, TQueuePoison> 
		: IObservable<TQueue>
	{
		private readonly IDurableJobStorageQueue<TQueue, TQueuePoison> durableJobStorage;
		private IObservable<TQueue> syncRequestPublisher;

		private readonly int maxQueueItemsToPublishPerInterval;
		private const int maxAllowedQueueItemsToPublishPerInterval = 50000;
		private static TimeSpan pollingInterval = TimeSpan.FromSeconds(20);

		/// <summary>	Gets the maximum allowable queue items to publish per interval, presently 50000. </summary>
		/// <value>	The maximum allowable queue items to publish per interval, presently 50000. </value>
		public int MaxAllowedQueueItemsToPublishPerInterval
		{
			get { return maxAllowedQueueItemsToPublishPerInterval; }
		}

		/// <summary>	Gets the polling interval. </summary>
		/// <value>	The polling interval. </value>
		public TimeSpan PollingInterval
		{
			get { return pollingInterval; }
		}

		/// <summary>	Constructs a new monitor instance, given a durable job storage and a maximum number of items to publish over the observable per polling interval. </summary>
		/// <remarks>	7/15/2011. </remarks>
		/// <param name="durableJobStorage">		The durable job storage. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	Handle of the maximum queue items to. </param>
		public DurableJobStorageMonitor(IDurableJobStorageQueue<TQueue, TQueuePoison> durableJobStorage, int maxQueueItemsToPublishPerInterval)
#if SILVERLIGHT
			: this(durableJobStorage, maxQueueItemsToHandle, Scheduler.ThreadPool)
#else
			: this(durableJobStorage, maxQueueItemsToPublishPerInterval, Scheduler.TaskPool)
#endif
		{ }

		internal DurableJobStorageMonitor(IDurableJobStorageQueue<TQueue, TQueuePoison> durableJobStorage, int maxQueueItemsToPublishPerInterval, IScheduler scheduler)
		{
			if (null == scheduler)
			{
				throw new ArgumentNullException("scheduler");
			}

			if (null == durableJobStorage)
			{
				throw new ArgumentNullException("durableJobStorage");
			}

			if (maxQueueItemsToPublishPerInterval > maxAllowedQueueItemsToPublishPerInterval)
			{
				throw new ArgumentOutOfRangeException("maxQueueItemsToPublishPerInterval", String.Format("limited to {0}", maxQueueItemsToPublishPerInterval));
			}

			if (maxQueueItemsToPublishPerInterval < 1)
			{
				throw new ArgumentOutOfRangeException("maxQueueItemsToPublishPerInterval", "must be at least 1");
			}

			this.durableJobStorage = durableJobStorage;
			this.maxQueueItemsToPublishPerInterval = maxQueueItemsToPublishPerInterval;

			//on first construction, we must move any items out of 'pending' and back into 'queued', in the event of a crash recovery, etc
			durableJobStorage.ResetAllPendingToQueued();

			//fire up our polling on an interval, slurping up all non-nulls from 'queued', to a max of X items, but don't start until connect is called
			syncRequestPublisher = Observable.Interval(pollingInterval, scheduler)
				.SelectMany(interval =>
					ReadQueuedItems()
					.TakeWhile(request => null != request)
					.Take(maxQueueItemsToPublishPerInterval))
				.Publish()
				.RefCount();

			//.DistinctUntilChanged(GenericEqualityComparer<OrderSynchronizationRequest>.ByAllMembers());
		}

		private IEnumerable<TQueue> ReadQueuedItems()
		{
			while (true)
			{
				yield return durableJobStorage.TransitionNextQueuedItemToPending();
			}
		}

		/// <summary>	Subscribes to TQueue notifications. </summary>
		/// <remarks>	7/15/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the observer is null. </exception>
		/// <param name="observer">	The observer. </param>
		/// <returns>	A subscription. </returns>
		public IDisposable Subscribe(IObserver<TQueue> observer)
		{
			if (null == observer)
			{
				throw new ArgumentNullException("observer");
			}

			return syncRequestPublisher.Subscribe(observer);
		}
	}
}