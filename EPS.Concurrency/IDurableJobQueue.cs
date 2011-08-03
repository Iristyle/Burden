using System;
using System.Collections.Generic;

namespace EPS.Concurrency
{
	/// <summary>	Defines a standard queue interface for durable storage of jobs. </summary>
	/// <remarks>	7/5/2011. </remarks>
	/// <typeparam name="TQueue">		Type of the queue item. </typeparam>
	/// <typeparam name="TQueuePoison">	Type of the poisoned queue item. </typeparam>
	public interface IDurableJobQueue<TQueue, TQueuePoison>
	{
		/// <summary>	Adds a new item to the queue. </summary>
		/// <param name="item">	The item. </param>
		void Queue(TQueue item);

		/// <summary>	Gets the next available queued item and transitions said item to the pending state. </summary>
		/// <returns>	The item if an item was queued, otherwise null. </returns>
		IItem<TQueue> NextQueuedItem();
				
		/// <summary>	Resets all pending items to the queued state. </summary>
		void ResetAllPendingToQueued();

		/// <summary>	Removes a queued item from the pending state. </summary>
		/// <remarks>	Should throw an exception if no item exists in the pending state. </remarks>
		/// <param name="item">	The item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		bool Complete(TQueue item);

		/// <summary>	Poisons an item, moving it from the pending state to the poisoned state. </summary>
		/// <remarks>	Should throw an exception if no item exists in the pending state. </remarks>
		/// <param name="item">		   	The original item. </param>
		/// <param name="poisonedItem">	The original item, converted to its poisoned representation. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		bool Poison(TQueue item, TQueuePoison poisonedItem);

		/// <summary>	Deletes the given item from the poisoned state. </summary>
		/// <param name="poisonedItem">	The poisoned representation of an item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		bool Delete(TQueuePoison poisonedItem);

		/// <summary>	Returns all poisoned items stored for this queue. </summary>
		/// <returns>	An enumerable collection of poisoned items (that may be empty). </returns>
		IEnumerable<TQueuePoison> GetPoisoned();

		/// <summary>	Returns all queue items stored for this queue. </summary>
		/// <returns>	An enumerable collection of queue items (that may be empty). </returns>
		IEnumerable<TQueue> GetQueued();

		/// <summary>	Returns all pending items stored for this queue. </summary>
		/// <returns>	An enumerable collection of pending items (that may be empty). </returns>
		IEnumerable<TQueue> GetPending();
	}
}