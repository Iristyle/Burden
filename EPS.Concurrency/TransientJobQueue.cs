using System;
using System.Collections.Generic;
using System.Linq;

namespace EPS.Concurrency
{
	/// <summary>	Queue of transient jobs, with no backing store. Provided only for testing and demonstration purposes. </summary>
	/// <remarks>	7/27/2011.  Performance of this class will be terrible. </remarks>
	/// <typeparam name="TQueue">	   	Type of the queue. </typeparam>
	/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
	public class TransientJobQueue<TQueue, TQueuePoison>
	: IDurableJobQueue<TQueue, TQueuePoison>
	{
		private List<TQueue> queue = new List<TQueue>();
		private List<TQueue> pending = new List<TQueue>();
		private List<TQueuePoison> poisoned = new List<TQueuePoison>();
		private IEqualityComparer<TQueue> itemComparer;
		private IEqualityComparer<TQueuePoison> poisonComparer;

		/// <summary>	Constructor. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the itemComparer or poisonComparer are null. </exception>
		/// <param name="itemComparer">  	The item comparer. </param>
		/// <param name="poisonComparer">	The poison comparer. </param>
		public TransientJobQueue(IEqualityComparer<TQueue> itemComparer, IEqualityComparer<TQueuePoison> poisonComparer)
		{
			if (null == itemComparer) { throw new ArgumentNullException("itemComparer"); }
			if (null == poisonComparer) { throw new ArgumentNullException("poisonComparer"); }

			this.itemComparer = itemComparer;
			this.poisonComparer = poisonComparer;
		}

		/// <summary>	Adds a new item to the queue. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the item is null. </exception>
		/// <param name="item">	The item. </param>
		public void Queue(TQueue item)
		{
			if (null == item) { throw new ArgumentNullException("item"); }

			lock (queue)
			{
				queue.Add(item);
			}
		}

		/// <summary>	Gets the next available queued item and transitions said item to the pending state. </summary>
		/// <returns>	The item if an item was queued, otherwise null. </returns>
		public IItem<TQueue> NextQueuedItem()
		{
			lock (queue)
			{
				if (queue.Count == 0)
					return Item.None<TQueue>();

				var item = queue.ElementAt(0);
				queue.RemoveAt(0);

				pending.Add(item);
				return Item.From(item);
			}
		}

		/// <summary>	Resets all pending items to the queued state. </summary>
		public void ResetAllPendingToQueued()
		{
			lock (queue)
			{
				if (pending.Count == 0)
					return;

				queue = pending.Concat(queue).ToList();
				pending.Clear();
			}
		}

		/// <summary>	Removes a queued item from the pending state. </summary>
		/// <remarks>	Should throw an exception if no item exists in the pending state. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the item is null. </exception>
		/// <param name="item">	The item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Complete(TQueue item)
		{
			if (null == item) { throw new ArgumentNullException("item"); }

			lock (queue)
			{
				int index = pending.FindIndex(p => itemComparer.Equals(p, item));
				if (-1 == index)
				{
					return false;
				}
				pending.RemoveAt(index);
				return true;
			}
		}

		/// <summary>	Poisons an item, moving it from the pending state to the poisoned state. </summary>
		/// <remarks>	Should throw an exception if no item exists in the pending state. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the item or poisonedItem are null. </exception>
		/// <param name="item">		   	The original item. </param>
		/// <param name="poisonedItem">	The original item, converted to its poisoned representation. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Poison(TQueue item, TQueuePoison poisonedItem)
		{
			if (null == item) { throw new ArgumentNullException("item"); }
			if (null == poisonedItem) { throw new ArgumentNullException("poisonedItem"); }

			lock (queue)
			{
				int index = pending.FindIndex(p => itemComparer.Equals(p, item));
				if (-1 == index)
				{
					return false;
				}
				pending.RemoveAt(index);
				poisoned.Add(poisonedItem);
				return true;
			}
		}

		/// <summary>	Deletes the given item from the poisoned state. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the poisonedItem is null. </exception>
		/// <param name="poisonedItem">	The poisoned representation of an item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool Delete(TQueuePoison poisonedItem)
		{
			if (null == poisonedItem) { throw new ArgumentNullException("poisonedItem"); }

			lock (queue)
			{
				int index = poisoned.FindIndex(poison => poisonComparer.Equals(poison, poisonedItem));
				if (-1 == index)
				{
					return false;
				}
				poisoned.RemoveAt(index);
				return true;
			}
		}

		/// <summary>	Returns all poisoned items stored for this queue. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <returns>	An enumerable collection of poisoned items (that may be empty). </returns>
		public IEnumerable<TQueuePoison> GetPoisoned()
		{
			return poisoned.AsEnumerable();
		}

		/// <summary>	Returns all queue items stored for this queue. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <returns>	An enumerable collection of queue items (that may be empty). </returns>
		public IEnumerable<TQueue> GetQueued()
		{
			return queue.AsEnumerable();
		}

		/// <summary>	Returns all pending items stored for this queue. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <returns>	An enumerable collection of pending items (that may be empty). </returns>
		public IEnumerable<TQueue> GetPending()
		{
			return pending.AsEnumerable();
		}
	}
}