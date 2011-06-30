using System;
using System.Collections.Generic;

namespace EPS.Concurrency
{
	public interface IDurableJobStorage<TQueue, TQueuePoison>
	{
		void Queue(TQueue item);
		TQueue MoveNextItemToPending();
		void Poison(TQueue item, TQueuePoison poisonedItem);
		void ResetPendingToQueued();
		void Complete(TQueue item);
		IEnumerable<TQueuePoison> GetPoisoned();
	}
}