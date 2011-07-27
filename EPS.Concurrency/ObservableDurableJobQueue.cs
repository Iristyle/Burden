using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EPS.Concurrency
{
	/// <summary>	Provides a way to wrap an IDurableJobQueue and provide notifications over IObservable on the major queue actions.  </summary>
	/// <remarks>	7/27/2011. </remarks>
	/// <typeparam name="TQueue">	   	Type of the queue. </typeparam>
	/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
	public class ObservableDurableJobQueue<TQueue, TQueuePoison>
		: IDurableJobQueue<TQueue, TQueuePoison>
	{
		private readonly IDurableJobQueue<TQueue, TQueuePoison> durableJobQueue;
		private readonly Subject<DurableJobQueueAction<TQueue, TQueuePoison>> onQueueAction =
			new Subject<DurableJobQueueAction<TQueue, TQueuePoison>>();

		/// <summary>	Constructor. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the durable job queue is null. </exception>
		/// <param name="durableJobQueue">	IDurableJobQueue to wrap. </param>
		public ObservableDurableJobQueue(IDurableJobQueue<TQueue, TQueuePoison> durableJobQueue)
		{
			if (null == durableJobQueue) { throw new ArgumentNullException("durableJobQueue"); }
			if (typeof(ObservableDurableJobQueue<,>).IsAssignableFrom(durableJobQueue.GetType().GetGenericTypeDefinition()))
				{ throw new ArgumentException("Incoming queue instance is an ObservableDurableJobQueue.  Nesting not supported.", "durableJobQueue"); }
			this.durableJobQueue = durableJobQueue;
		}

		/// <summary>	An IObservable thats fired when queue actions happens. </summary>
		/// <value>	An IObservable of queue actions. </value>
		public IObservable<DurableJobQueueAction<TQueue, TQueuePoison>> OnQueueAction 
		{
			get { return onQueueAction.AsObservable().Publish().RefCount(); }
		}

		/// <summary>	Adds a new item to the queue. </summary>
		/// <remarks>	Delegates to the underlying job queue. Fires a notification on OnQueueAction. </remarks>
		/// <param name="item">	The item. </param>
		public void Queue(TQueue item)
		{
			durableJobQueue.Queue(item);
			onQueueAction.OnNext(DurableJobQueueAction.Queued<TQueue, TQueuePoison>(item));
		}

		/// <summary>	Gets the next available queued item and transitions said item to the pending state. </summary>
		/// <remarks>	Delegates to the underlying job queue. Fires a notification on OnQueueAction if the operation returned an item. </remarks>
		/// <returns>	The item if an item was queued, otherwise null. </returns>
		public TQueue TransitionNextQueuedItemToPending()
		{
			//TODO: 7-27-2011 -- this call should perhaps be changed to include item and success status instead of just item, since this doesn't make sense easily for value types
			var item = durableJobQueue.TransitionNextQueuedItemToPending();
			if (null != item)
			{
				onQueueAction.OnNext(DurableJobQueueAction.Pending<TQueue, TQueuePoison>(item));
			}
			return item;
		}

		/// <summary>	Resets all pending items to the queued state. </summary>
		public void ResetAllPendingToQueued()
		{
			durableJobQueue.ResetAllPendingToQueued();
		}

		/// <summary>	Removes a queued item from the pending state. </summary>
		/// <remarks>
		/// Should throw an exception if no item exists in the pending state. Delegates to the underlying job queue. Fires a notification on
		/// OnQueueAction if the operation was a success.
		/// </remarks>
		/// <param name="item">	The item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Complete(TQueue item)
		{
			var result = durableJobQueue.Complete(item);
			if (result)
				onQueueAction.OnNext(DurableJobQueueAction.Completed<TQueue, TQueuePoison>(item));
			return result;
		}

		/// <summary>	Poisons an item, moving it from the pending state to the poisoned state. </summary>
		/// <remarks>
		/// Should throw an exception if no item exists in the pending state. Delegates to the underlying job queue. Fires a notification on
		/// OnQueueAction if the operation was a success.
		/// </remarks>
		/// <param name="item">		   	The original item. </param>
		/// <param name="poisonedItem">	The original item, converted to its poisoned representation. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Poison(TQueue item, TQueuePoison poisonedItem)
		{
			var result = durableJobQueue.Poison(item, poisonedItem);
			if (result)
				onQueueAction.OnNext(DurableJobQueueAction.Poisoned(item, poisonedItem));

			return result;
		}

		/// <summary>	Deletes the given item from the poisoned state. </summary>
		/// <remarks>	Delegates to the underlying job queue. Fires a notification on OnQueueAction if the operation was a success. </remarks>
		/// <param name="poisonedItem">	The poisoned representation of an item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Delete(TQueuePoison poisonedItem)
		{
			var result = durableJobQueue.Delete(poisonedItem);
			if (result)
				onQueueAction.OnNext(DurableJobQueueAction.Deleted<TQueue, TQueuePoison>(poisonedItem));
			return result;
		}

		/// <summary>	Returns all poisoned items stored for this queue. </summary>
		/// <remarks>	Delegates to the underlying job queue. </remarks>
		/// <returns>	An enumerable collection of poisoned items (that may be empty). </returns>
		public IEnumerable<TQueuePoison> GetPoisoned()
		{
			return durableJobQueue.GetPoisoned();
		}

		/// <summary>	Returns all queue items stored for this queue. </summary>
		/// <remarks>	Delegates to the underlying job queue. </remarks>
		/// <returns>	An enumerable collection of queue items (that may be empty). </returns>
		public IEnumerable<TQueue> GetQueued()
		{
			return durableJobQueue.GetQueued();
		}

		/// <summary>	Returns all pending items stored for this queue. </summary>
		/// <remarks>	Delegates to the underlying job queue. </remarks>
		/// <returns>	An enumerable collection of pending items (that may be empty). </returns>
		public IEnumerable<TQueue> GetPending()
		{
			return durableJobQueue.GetPending();
		}
	}
}