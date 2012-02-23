using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Burden
{
	/// <summary>	Provides a way to wrap an IDurableJobQueue and provide notifications over IObservable on the major queue actions.  </summary>
	/// <remarks>	7/27/2011. </remarks>
	/// <typeparam name="TQueue">	   	Type of the queue. </typeparam>
	/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
	public class ObservableDurableJobQueue<TQueue, TQueuePoison>
		: IDurableJobQueue<TQueue, TQueuePoison>, IDisposable
	{
		private bool _disposed;
		private readonly IDurableJobQueue<TQueue, TQueuePoison> _durableJobQueue;
		private readonly Subject<DurableJobQueueAction<TQueue, TQueuePoison>> _onQueueAction =
			new Subject<DurableJobQueueAction<TQueue, TQueuePoison>>();

		/// <summary>	Constructor. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the durable job queue is null. </exception>
		/// <param name="durableJobQueue">	IDurableJobQueue to wrap. </param>
		public ObservableDurableJobQueue(IDurableJobQueue<TQueue, TQueuePoison> durableJobQueue)
		{
			if (null == durableJobQueue) { throw new ArgumentNullException("durableJobQueue"); }
			var queueType = durableJobQueue.GetType();
			if (queueType.IsGenericType && typeof(ObservableDurableJobQueue<,>).IsAssignableFrom(queueType.GetGenericTypeDefinition()))
				{ throw new ArgumentException("Incoming queue instance is an ObservableDurableJobQueue.  Nesting not supported.", "durableJobQueue"); }
			this._durableJobQueue = durableJobQueue;
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("this");
			}
		}

		/// <summary>	Dispose of this object, cleaning up any resources it uses. </summary>
		/// <remarks>	7/24/2011. </remarks>
		public void Dispose()
		{
			if (!this._disposed)
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Dispose of this object, cleaning up any resources it uses.  Will dispose the OnQueueAction subject, rendering queue monitoring impossible.
		/// </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <param name="disposing">	true if resources should be disposed, false if not. </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				this._disposed = true;
				_onQueueAction.Dispose();
			}
		}

		/// <summary>	An IObservable thats fired when queue actions happens. </summary>
		/// <value>	An IObservable of queue actions. </value>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed. </exception>
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and IObservables")]
		public IObservable<DurableJobQueueAction<TQueue, TQueuePoison>> OnQueueAction 
		{
			get 
			{
				ThrowIfDisposed();
				return _onQueueAction.AsObservable().Publish().RefCount(); 
			}
		}

		/// <summary>	Adds a new item to the queue. </summary>
		/// <remarks>	Delegates to the underlying job queue. Fires a notification on OnQueueAction. </remarks>
		/// <param name="item">	The item. </param>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed. </exception>
		public void Queue(TQueue item)
		{
			ThrowIfDisposed();
			_durableJobQueue.Queue(item);
			_onQueueAction.OnNext(DurableJobQueueAction.Queued(item));
		}

		/// <summary>	Gets the next available queued item and transitions said item to the pending state. </summary>
		/// <remarks>	Delegates to the underlying job queue. Fires a notification on OnQueueAction if the operation returned an item. </remarks>
		/// <returns>	The item if an item was queued, otherwise null. </returns>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed. </exception>
		public IItem<TQueue> NextQueuedItem()
		{
			ThrowIfDisposed();
			var item = _durableJobQueue.NextQueuedItem();
			if (item.Success)
			{
				_onQueueAction.OnNext(DurableJobQueueAction.Pending(item.Value));
			}
			return item;
		}

		/// <summary>	Resets all pending items to the queued state. </summary>
		public void ResetAllPendingToQueued()
		{
			_durableJobQueue.ResetAllPendingToQueued();
		}

		/// <summary>	Removes a queued item from the pending state. </summary>
		/// <remarks>
		/// Should throw an exception if no item exists in the pending state. Delegates to the underlying job queue. Fires a notification on
		/// OnQueueAction if the operation was a success.
		/// </remarks>
		/// <param name="item">	The item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed. </exception>
		public bool Complete(TQueue item)
		{
			ThrowIfDisposed();
			var result = _durableJobQueue.Complete(item);
			if (result)
				_onQueueAction.OnNext(DurableJobQueueAction.Completed(item));
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
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed. </exception>
		public bool Poison(TQueue item, TQueuePoison poisonedItem)
		{
			ThrowIfDisposed();
			var result = _durableJobQueue.Poison(item, poisonedItem);
			if (result)
				_onQueueAction.OnNext(DurableJobQueueAction.Poisoned(item, poisonedItem));

			return result;
		}

		/// <summary>	Deletes the given item from the poisoned state. </summary>
		/// <remarks>	Delegates to the underlying job queue. Fires a notification on OnQueueAction if the operation was a success. </remarks>
		/// <param name="poisonedItem">	The poisoned representation of an item. </param>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed. </exception>
		public bool Delete(TQueuePoison poisonedItem)
		{
			ThrowIfDisposed();
			var result = _durableJobQueue.Delete(poisonedItem);
			if (result)
				_onQueueAction.OnNext(DurableJobQueueAction.Deleted(poisonedItem));
			return result;
		}

		/// <summary>	Returns all poisoned items stored for this queue. </summary>
		/// <remarks>	Delegates to the underlying job queue. </remarks>
		/// <returns>	An enumerable collection of poisoned items (that may be empty). </returns>
		public IEnumerable<TQueuePoison> GetPoisoned()
		{
			return _durableJobQueue.GetPoisoned();
		}

		/// <summary>	Returns all queue items stored for this queue. </summary>
		/// <remarks>	Delegates to the underlying job queue. </remarks>
		/// <returns>	An enumerable collection of queue items (that may be empty). </returns>
		public IEnumerable<TQueue> GetQueued()
		{
			return _durableJobQueue.GetQueued();
		}

		/// <summary>	Returns all pending items stored for this queue. </summary>
		/// <remarks>	Delegates to the underlying job queue. </remarks>
		/// <returns>	An enumerable collection of pending items (that may be empty). </returns>
		public IEnumerable<TQueue> GetPending()
		{
			return _durableJobQueue.GetPending();
		}
	}
}