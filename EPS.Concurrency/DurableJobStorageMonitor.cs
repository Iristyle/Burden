using System;
using System.Reactive.Linq;

namespace EPS.Concurrency
{
	public class DurableJobStorageMonitor<TQueue, TQueuePoison> 
		: IObservable<TQueue>
	{
		private readonly IDurableJobStorage<TQueue, TQueuePoison> durableJobStorage;
		private IObservable<TQueue> syncRequestPublisher;

		private readonly int maxQueueItemsToHandle;
		private const int maxDefinableQueueItemsToHandle = 5000;

		public DurableJobStorageMonitor(IDurableJobStorage<TQueue, TQueuePoison> durableJobStorage, int maxQueueItemsToHandle)
		{
			if (null == durableJobStorage)
			{
				throw new ArgumentNullException("durableJobStorage");
			}

			if (maxQueueItemsToHandle > maxDefinableQueueItemsToHandle)
			{
				throw new ArgumentOutOfRangeException("maxQueueItemsToHandle", String.Format("limited to {0}", maxDefinableQueueItemsToHandle));
			}

			this.maxQueueItemsToHandle = maxQueueItemsToHandle;

			//on first construction, we must move any items out of 'pending' and back into 'queued', in the event of a crash recovery, etc
			durableJobStorage.ResetPendingToQueued();

			//fire up our redis polling on an interval, slurping up all non-nulls from 'queued', to a max of X items, but don't start until connect is called
			syncRequestPublisher = Observable.Interval(TimeSpan.FromSeconds(20))
				.Select(interval => durableJobStorage.MoveNextItemToPending())
				.TakeWhile(request => null != request)
				.Take(maxQueueItemsToHandle)
				.Publish()
				.RefCount();

			//.DistinctUntilChanged(GenericEqualityComparer<OrderSynchronizationRequest>.ByAllMembers());
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