using System;
using System.Collections.Generic;
using System.Linq;
using EPS.Dynamic;
using EPS.Utility;
using Xunit;
using Xunit.Extensions;
using FakeItEasy;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IDurableJobStorageQueueTest<T, TQueue, TQueuePoison>
		where T: IDurableJobStorageQueue<TQueue, TQueuePoison>		
	{
		private readonly Func<T> jobStorageFactory;

		public IDurableJobStorageQueueTest(Func<T> jobStorageFactory)
		{
			this.jobStorageFactory = jobStorageFactory;
		}

		/*
		protected static IEnumerable<object[]> QueueItems 
		{
			get { throw new ApplicationException("Must be overridden"); } 
		}

		protected static IEnumerable<object[]> QueueItemLists
		{
			get { throw new ApplicationException("Must be overridden"); }
		}

		protected static IEnumerable<object[]> PoisonQueueItems
		{
			get { throw new ApplicationException("Must be overridden"); }
		}

		protected static IEnumerable<object[]> BuildPoisonQueueItems
		{
			get { throw new ApplicationException("Must be overridden"); }
		}
		*/

		protected void ClearAllQueues(IDurableJobStorageQueue<TQueue, TQueuePoison> storage)
		{
			SlideItemsToPending(storage);
			foreach (var item in storage.GetPending())
			{
				storage.Complete(item);
			}

			foreach (var item in storage.GetPoisoned())
			{
				storage.Delete(item);
			}
		}

		[Theory]
		[PropertyData("QueueItems")]
		public void Queue_DoesNotThrow(TQueue item)
		{
			var storage = jobStorageFactory();

			Assert.DoesNotThrow(() => storage.Queue(item));
		}

		[Fact]
		public void Queue_ThrowsOnNullItemForReferenceTypes()
		{
			var storage = jobStorageFactory();

			if (typeof(TQueue).IsValueType)
				return;

			var item = (null as object).Cast<TQueue>();
			Assert.Throws<ArgumentNullException>(() => storage.Queue(item));
		}

		[Theory]
		[PropertyData("QueueItems")]
		public void Queue_DoesNotModifyPendingList(TQueue item)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);
			storage.Queue(item);

			Assert.Empty(storage.GetPending());
		}

		[Theory]
		[PropertyData("QueueItems")]
		public void Queue_DoesNotModifyPoisonedList(TQueue item)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);
			storage.Queue(item);

			Assert.Empty(storage.GetPoisoned());
		}

		[Theory]
		[PropertyData("QueueItemLists")]
		public void TransitionNextQueuedItemToPending_PreservesOrderingInPendingList(IEnumerable<TQueue> queueItems)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			foreach (var item in queueItems)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
			}

			Assert.True(queueItems.SequenceEqual(storage.GetPending(), GenericEqualityComparer<TQueue>.ByAllMembers()));
		}

		[Theory]
		[PropertyData("QueueItemLists")]
		public void TransitionNextQueuedItemToPending_ProperlyRemovesItemFromQueuedList(IEnumerable<TQueue> queueItems)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			foreach (var item in queueItems)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
			}

			Assert.Empty(storage.GetQueued());
		}

		[Theory]
		[PropertyData("QueueItemLists")]
		public void TransitionNextQueuedItemToPending_DoesNotModifyPoisonedList(IEnumerable<TQueue> queueItems)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			foreach (var item in queueItems)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
			}

			Assert.Empty(storage.GetPoisoned());
		}

		[Fact]
		public void TransitionNextQueuedItemToPending_ReturnsNullOnEmptyQueueList()
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			Assert.Null(storage.TransitionNextQueuedItemToPending());
		}

		[Theory]
		[PropertyData("QueueItemLists")]
		public void ResetAllPendingToQueued_PreservesOriginalOrder(IEnumerable<TQueue> queueItems)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			foreach (var item in queueItems)
				storage.Queue(item);

			storage.ResetAllPendingToQueued();

			List<TQueue> items = SlideItemsToPending(storage).ToList();

			Assert.True(queueItems.SequenceEqual(items, GenericEqualityComparer<TQueue>.ByAllMembers()));
		}

		protected IEnumerable<TQueue> SlideItemsToPending(IDurableJobStorageQueue<TQueue, TQueuePoison> jobStorage)
		{
			while (true)
			{
				TQueue item = jobStorage.TransitionNextQueuedItemToPending();
				if (!item.Equals(default(TQueue)))
					yield return item;
				else
					yield break;
			}
		}

		[Fact]
		public void ResetAllPendingToQueued_DoesNotThrowOnEmptyPendingList()
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			Assert.DoesNotThrow(() => storage.ResetAllPendingToQueued());
		}

		[Fact]
		public void Poison_ThrowsOnNullItemForReferenceTypes()
		{
			var storage = jobStorageFactory();

			if (typeof(TQueue).IsValueType)
				return;

			var item = (null as object).Cast<TQueue>();
			Assert.Throws<ArgumentNullException>(() => storage.Poison(item, default(TQueuePoison)));
		}

		[Fact]
		public void Poison_ThrowsOnNullPoisonItemForReferenceTypes()
		{
			var storage = jobStorageFactory();

			if (typeof(TQueuePoison).IsValueType)
				return;

			var item = (null as object).Cast<TQueuePoison>();
			Assert.Throws<ArgumentNullException>(() => storage.Poison(A.Dummy<TQueue>(), item));
		}

		[Theory]
		[PropertyData("PoisonQueueItems")]
		public void Poison_ThrowsOnMissingItem(Tuple<TQueue, TQueuePoison> queueItem)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			Assert.Throws<ArgumentException>(() => storage.Poison(queueItem.Item1, queueItem.Item2));
		}

		[Theory]
		[PropertyData("PoisonQueueItems")]
		public void Poison_ThrowsOnQueuedItem(Tuple<TQueue, TQueuePoison> queueItem)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			storage.Queue(queueItem.Item1);

			Assert.Throws<ArgumentException>(() => storage.Poison(queueItem.Item1, queueItem.Item2));
		}

		[Theory]
		[PropertyData("PoisonQueueItems")]
		public void Poison_DoesNotThrowOnPendingItem(Tuple<TQueue, TQueuePoison> queueItem)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			storage.Queue(queueItem.Item1);
			storage.TransitionNextQueuedItemToPending();

			Assert.DoesNotThrow(() => storage.Poison(queueItem.Item1, queueItem.Item2));
		}

		[Theory]
		[PropertyData("PoisonQueueItems")]
		public void Poison_GetPoisoned_QueuesMatch(Tuple<TQueue, TQueuePoison> queueItem)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			storage.Queue(queueItem.Item1);
			storage.TransitionNextQueuedItemToPending();
			storage.Poison(queueItem.Item1, queueItem.Item2);

			Assert.True(storage.GetPoisoned().SequenceEqual(new[] { queueItem.Item2 }, GenericEqualityComparer<TQueuePoison>.ByAllMembers()));
		}

		[Fact]
		public void Delete_ThrowsOnNullItemForReferenceTypes()
		{
			var storage = jobStorageFactory();

			if (typeof(TQueuePoison).IsValueType)
				return;

			var item = (null as object).Cast<TQueuePoison>();
			Assert.Throws<ArgumentNullException>(() => storage.Delete(item));

		}

		[Theory]
		[PropertyData("PoisonQueueItems")]
		public void Delete_ThrowsOnMissingItem(Tuple<TQueue, TQueuePoison> queueItem)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			Assert.Throws<ArgumentException>(() => storage.Delete(queueItem.Item2));
		}

		[Theory]
		[PropertyData("PoisonQueueItems")]
		public void Delete_ThrowsOnPendingItem(Tuple<TQueue, TQueuePoison> queueItem)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			storage.Queue(queueItem.Item1);
			storage.TransitionNextQueuedItemToPending();

			Assert.Throws<ArgumentException>(() => storage.Delete(queueItem.Item2));
		}

		[Theory]
		[PropertyData("PoisonQueueItems")]
		public void Delete_DoesNotThrowOnPoisonedItem(Tuple<TQueue, TQueuePoison> queueItem)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			storage.Queue(queueItem.Item1);
			storage.TransitionNextQueuedItemToPending();
			storage.Poison(queueItem.Item1, queueItem.Item2);

			Assert.DoesNotThrow(() => storage.Delete(queueItem.Item2));
		}

		[Theory]
		[PropertyData("PoisonQueueItems")]
		public void Delete_GetPoisoned_ReturnsExpectedCount(Tuple<TQueue, TQueuePoison> queueItem)
		{
			//TODO: pass better params
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			storage.Queue(queueItem.Item1);
			storage.TransitionNextQueuedItemToPending();
			storage.Poison(queueItem.Item1, queueItem.Item2);
			storage.Delete(queueItem.Item2);

			Assert.Empty(storage.GetPoisoned());
		}

		public void Delete_DoesNotModifyQueued()
		{
		}

		public void Delete_DoesNotModifyPending()
		{
		}

		[Fact]
		public void Complete_ThrowsOnNullItemForReferenceTypes()
		{
			var storage = jobStorageFactory();

			if (typeof(TQueue).IsValueType)
				return;

			var item = (null as object).Cast<TQueue>();
			Assert.Throws<ArgumentNullException>(() => storage.Complete(item));
		}


		//void Complete(TQueue item);

		[Fact]
		public void GetQueued_IsEmptyOnClearedQueues()
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			Assert.Empty(storage.GetQueued());
		}

		[Fact]
		public void GetPending_IsEmptyOnClearedQueues()
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			Assert.Empty(storage.GetPending());
		}

		[Fact]
		public void GetPoisoned_IsEmptyOnClearedQueues()
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			Assert.Empty(storage.GetPoisoned());
		}

		[Theory]
		[PropertyData("QueueItemLists")]
		public void GetQueued_PreservesOrdering(IEnumerable<TQueue> queueItems)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			foreach (var item in queueItems)
				storage.Queue(item);

			Assert.True(queueItems.SequenceEqual(storage.GetQueued(), GenericEqualityComparer<TQueue>.ByAllMembers()));
		}

		[Theory]
		[PropertyData("QueueItemLists")]
		public void GetPending_PreservesOrdering(IEnumerable<TQueue> queueItems)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			foreach (var item in queueItems)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
			}

			Assert.True(queueItems.SequenceEqual(storage.GetPending(), GenericEqualityComparer<TQueue>.ByAllMembers()));
		}

		[Theory]
		[PropertyData("BuildPoisonQueueItems")]
		public void GetPoisoned_PreservesOrdering(IEnumerable<TQueue> queueItems, Func<TQueue, TQueuePoison> poisonBuilder)
		{
			var storage = jobStorageFactory();
			ClearAllQueues(storage);

			List<TQueuePoison> poisonedItems = new List<TQueuePoison>();

			foreach (var item in queueItems)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
				var poisoned = poisonBuilder(item);
				poisonedItems.Add(poisoned);
				storage.Poison(item, poisoned);
			}

			Assert.True(poisonedItems.SequenceEqual(storage.GetPoisoned(), GenericEqualityComparer<TQueuePoison>.ByAllMembers()));
		}
	}
}