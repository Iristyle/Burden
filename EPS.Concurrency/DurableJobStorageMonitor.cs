using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace EPS.Concurrency
{
	public class DurableJobStorageMonitor<TQueue, TQueuePoison> 
		: IObservable<TQueue>
	{
		private readonly IDurableJobStorageQueue<TQueue, TQueuePoison> durableJobStorage;
		private IObservable<TQueue> syncRequestPublisher;

		private readonly int maxQueueItemsToHandle;
		private const int maxDefinableQueueItemsToHandle = 50000;
		private static TimeSpan pollingInterval = TimeSpan.FromSeconds(20);

		public int MaxQueueItemsAllowed
		{
			get { return maxDefinableQueueItemsToHandle; }
		}

		public TimeSpan PollingInterval
		{
			get { return pollingInterval; }
		}


		public DurableJobStorageMonitor(IDurableJobStorageQueue<TQueue, TQueuePoison> durableJobStorage, int maxQueueItemsToHandle)
			: this(durableJobStorage, maxQueueItemsToHandle, Scheduler.ThreadPool)
		{ }

		internal DurableJobStorageMonitor(IDurableJobStorageQueue<TQueue, TQueuePoison> durableJobStorage, int maxQueueItemsToHandle, IScheduler scheduler)
		{
			if (null == scheduler)
			{
				throw new ArgumentNullException("scheduler");
			}

			if (null == durableJobStorage)
			{
				throw new ArgumentNullException("durableJobStorage");
			}

			if (maxQueueItemsToHandle > maxDefinableQueueItemsToHandle)
			{
				throw new ArgumentOutOfRangeException("maxQueueItemsToHandle", String.Format("limited to {0}", maxDefinableQueueItemsToHandle));
			}

			if (maxQueueItemsToHandle < 1)
			{
				throw new ArgumentOutOfRangeException("maxQueueItemsToHandle", "must be at least 1");
			}

			this.durableJobStorage = durableJobStorage;
			this.maxQueueItemsToHandle = maxQueueItemsToHandle;

			//on first construction, we must move any items out of 'pending' and back into 'queued', in the event of a crash recovery, etc
			durableJobStorage.ResetAllPendingToQueued();

			//fire up our polling on an interval, slurping up all non-nulls from 'queued', to a max of X items, but don't start until connect is called
			syncRequestPublisher = Observable.Interval(pollingInterval, scheduler)
				.SelectMany(interval => 
					ReadQueuedItems()
					.TakeWhile(request => null != request)
					.Take(maxQueueItemsToHandle))
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