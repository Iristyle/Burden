using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EPS.Concurrency.Tests.Unit
{
	//simple in-memory queue implementation for tests
	public class TestJobQueue<TQueue, TQueuePoison>
		: IDurableJobQueue<TQueue, TQueuePoison>
	{
		private List<TQueue> queue = new List<TQueue>();
		private readonly ManualResetEventSlim onJobsTransitioned;
		private readonly ManualResetEventSlim onJobsCompleted;
		private readonly int callsBeforeFiring = 0;
		private int transitionCallsMade = 0, completedCallsMade = 0;

		public TestJobQueue(int callsBeforeFiring, ManualResetEventSlim onJobsTransitioned, ManualResetEventSlim
		onJobsCompleted)
		{
			this.onJobsTransitioned = onJobsTransitioned;
			this.onJobsCompleted = onJobsCompleted;
			this.callsBeforeFiring = callsBeforeFiring;
		}

		public void Queue(TQueue item)
		{
			lock (queue)
			{
				queue.Add(item);
			}
		}

		public TQueue TransitionNextQueuedItemToPending()
		{
			lock (queue)
			{
				if (Interlocked.Increment(ref transitionCallsMade) == callsBeforeFiring)
				{
					this.onJobsTransitioned.Set();
				}

				if (queue.Count == 0)
					return default(TQueue);

				var item = queue.ElementAt(0);
				queue.RemoveAt(0);
				return item;
			}
		}

		public void ResetAllPendingToQueued()
		{
			lock (queue)
			{
				queue.Clear();
			}
		}

		public bool Complete(TQueue item)
		{
			lock (queue)
			{
				if (Interlocked.Increment(ref completedCallsMade) == callsBeforeFiring)
				{
					this.onJobsCompleted.Set();
				}

				if (queue.Count == 0)
				{
					return false;
				}
				int index = queue.IndexOf(item);
				if (-1 == index)
				{
					return false;
				}
				queue.RemoveAt(index);
				return true;
			}
		}

		public bool Poison(TQueue item, TQueuePoison poisonedItem)
		{
			throw new NotImplementedException();
		}

		public bool Delete(TQueuePoison poisonedItem)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<TQueuePoison> GetPoisoned()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<TQueue> GetQueued()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<TQueue> GetPending()
		{
			throw new NotImplementedException();
		}
	}
}

