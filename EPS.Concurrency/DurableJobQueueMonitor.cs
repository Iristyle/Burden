using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace EPS.Concurrency
{
	/// <summary>	Provides a means for polling a durable job queue and publishing the inputs over an in-memory IObservable. </summary>
	/// <remarks>	7/15/2011. </remarks>
	/// <typeparam name="TQueue">	   	Type of the queue. </typeparam>
	/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
	public class DurableJobQueueMonitor<TQueue, TQueuePoison> 
		: IObservable<TQueue>
	{
		private readonly IDurableJobQueue<TQueue, TQueuePoison> durableJobQueue;
		private IObservable<TQueue> syncRequestPublisher;

		private readonly int maxQueueItemsToPublishPerInterval;
		private readonly TimeSpan pollingInterval;

		/// <summary>	Gets the maximum queue items to publish per interval. </summary>
		/// <value>	The maximum queue items to publish per interval. </value>
		public int MaxQueueItemsToPublishPerInterval 
		{
			get { return maxQueueItemsToPublishPerInterval; }
		}
		
		/// <summary>	Gets the polling interval. </summary>
		/// <value>	The polling interval. </value>
		public TimeSpan PollingInterval
		{
			get { return pollingInterval; }
		}

		/// <summary>	Constructor for internal uses only -- specifically. </summary>
		/// <remarks>	7/28/2011. </remarks>
		/// <exception cref="ArgumentNullException">	  	Thrown when either the queue or scheduler are null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the pollingInterval is below the minimum allowed threshold or greater
		/// 												than the maximum allowed threshold.  Thrown when the items to publish per interval is
		/// 												less than 1 or greater than the maximum allowed threshold. </exception>
		/// <param name="durableJobQueue">						Queue of durable jobs. </param>
		/// <param name="pollingInterval">						The polling interval. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	The maximum queue items to publish per interval. </param>
		/// <param name="scheduler">							The scheduler. </param>
		internal DurableJobQueueMonitor(IDurableJobQueue<TQueue, TQueuePoison> durableJobQueue, TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval, IScheduler scheduler)
		{
			if (null == durableJobQueue) { throw new ArgumentNullException("durableJobQueue"); }
			
			if (pollingInterval > DurableJobQueueMonitor.MaximumAllowedPollingInterval)
			{
				throw new ArgumentOutOfRangeException(String.Format("must be less than {0:c}", DurableJobQueueMonitor.
				MaximumAllowedPollingInterval.ToString(), "pollingInterval"));
			}
			if (pollingInterval < DurableJobQueueMonitor.MinimumAllowedPollingInterval)
			{
				throw new ArgumentOutOfRangeException(String.Format("must be at least {0:c}", DurableJobQueueMonitor.
				MaximumAllowedPollingInterval), "pollingInterval");
			}

			if (maxQueueItemsToPublishPerInterval > DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval)
			{
				throw new ArgumentOutOfRangeException("maxQueueItemsToPublishPerInterval", String.Format("limited to {0} items to publish per interval", DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval));
			}
			if (maxQueueItemsToPublishPerInterval < 1)
			{
				throw new ArgumentOutOfRangeException("maxQueueItemsToPublishPerInterval", "must be at least 1");
			}
			
			if (null == scheduler) { throw new ArgumentNullException("scheduler"); }


			this.durableJobQueue = durableJobQueue;
			this.maxQueueItemsToPublishPerInterval = maxQueueItemsToPublishPerInterval;
			this.pollingInterval = pollingInterval;

			//on first construction, we must move any items out of 'pending' and back into 'queued', in the event of a crash recovery, etc
			durableJobQueue.ResetAllPendingToQueued();

			//fire up our polling on an interval, slurping up all non-nulls from 'queued', to a max of X items, but don't start until connect is called
			syncRequestPublisher = Observable.Interval(pollingInterval, scheduler)
				.SelectMany(interval =>
					ReadQueuedItems()
					.TakeWhile(request => null != request)
					.Take(maxQueueItemsToPublishPerInterval))
				.Publish()
				.RefCount();
		}

		private IEnumerable<TQueue> ReadQueuedItems()
		{
			while (true)
			{
				yield return durableJobQueue.TransitionNextQueuedItemToPending();
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

	public static class DurableJobQueueMonitor
	{
		private static TimeSpan minimumAllowedPollingInterval = TimeSpan.FromSeconds(3);
		private static TimeSpan maximumAllowedPollingInterval = TimeSpan.FromHours(1);
		private static TimeSpan defaultPollingInterval = TimeSpan.FromSeconds(20);
		private static int maxAllowedQueueItemsToPublishPerInterval = 10000;

		/// <summary>	Gets the maximum allowable queue items to publish per interval, presently 10000. </summary>
		/// <value>	The maximum allowable queue items to publish per interval, presently 10000. </value>
		public static int MaxAllowedQueueItemsToPublishPerInterval
		{
			get { return maxAllowedQueueItemsToPublishPerInterval; }
		}

		/// <summary>	Gets the minimum allowed polling interval, presently 3 seconds. </summary>
		/// <value>	The minimum allowed polling interval. </value>
		public static TimeSpan MinimumAllowedPollingInterval
		{
			get { return minimumAllowedPollingInterval; }
		}

		/// <summary>	Gets the maximum allowed polling interval, presently 1 hour. </summary>
		/// <value>	The maximum allowed polling interval. </value>
		public static TimeSpan MaximumAllowedPollingInterval
		{
			get { return maximumAllowedPollingInterval; }
		}

		/// <summary>	Gets the default polling interval, presently 20 seconds. </summary>
		/// <value>	The default polling interval. </value>
		public static TimeSpan DefaultPollingInterval
		{
			get { return defaultPollingInterval; }
		}

		/// <summary>	Constructs a new monitor instance, given a durable job and a maximum number of items to publish over the observable per polling interval. </summary>
		/// <remarks>	7/15/2011. </remarks>
		/// <exception cref="ArgumentNullException">	  	Thrown when the queue is null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the items to publish per interval is
		/// 												less than 1 or greater than the maximum allowed threshold. </exception>
		/// <param name="durableJobQueue">						Queue of durable jobs. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	The maximum queue items to publish per interval. </param>
		public static DurableJobQueueMonitor<TQueue, TQueuePoison> Create<TQueue, TQueuePoison>(IDurableJobQueue<TQueue, TQueuePoison> durableJobQueue, int maxQueueItemsToPublishPerInterval)
		{
			return new DurableJobQueueMonitor<TQueue, TQueuePoison>(durableJobQueue, DefaultPollingInterval, 
			maxQueueItemsToPublishPerInterval, LocalScheduler.Default);
		}

		/// <summary>	Constructs a new monitor instance, given a durable job, the maximum number of items to publish over the observable per polling interval, and the polling interval. </summary>
		/// <exception cref="ArgumentNullException">	  	Thrown when the queue is null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the pollingInterval is below the minimum allowed threshold or greater
		/// 												than the maximum allowed threshold.  Thrown when the items to publish per interval is
		/// 												less than 1 or greater than the maximum allowed threshold. </exception>
		/// <param name="durableJobQueue">						Queue of durable jobs. </param>
		/// <param name="pollingInterval">						The polling interval. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	The maximum queue items to publish per interval. </param>
		public static DurableJobQueueMonitor<TQueue, TQueuePoison> Create<TQueue, TQueuePoison>(IDurableJobQueue<TQueue, TQueuePoison> durableJobQueue, int maxQueueItemsToPublishPerInterval, TimeSpan pollingInterval)
		{
			return new DurableJobQueueMonitor<TQueue, TQueuePoison>(durableJobQueue, pollingInterval, maxQueueItemsToPublishPerInterval, LocalScheduler.Default);
		}
	}
}