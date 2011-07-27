using System;
using System.Collections.Generic;
using System.Linq;
using EPS.Dynamic;
using EPS.Utility;
using Ploeh.AutoFixture;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IDurableJobQueueTest<T, TQueue, TQueuePoison>
		where T: IDurableJobQueue<TQueue, TQueuePoison>
	{
		protected Fixture fixture = new Fixture();
		protected readonly Func<T> jobStorageFactory;
		protected readonly Func<TQueue, TQueuePoison> poisonConverter;

		public IDurableJobQueueTest(Func<T> jobStorageFactory, Func<TQueue, TQueuePoison> poisonConverter)
		{
			this.jobStorageFactory = () =>
			{
				var instance = jobStorageFactory();
				ClearAllQueues(instance);
				return instance;
			};
			this.poisonConverter = poisonConverter;
		}

		protected void ClearAllQueues(IDurableJobQueue<TQueue, TQueuePoison> storage)
		{
			SlideItemsToPending(storage);
			Assert.Empty(storage.GetQueued());

			foreach (var item in storage.GetPending())
			{
				storage.Complete(item);				
			}
			Assert.Empty(storage.GetPending());				

			foreach (var item in storage.GetPoisoned())
			{
				storage.Delete(item);				
			}
			Assert.Empty(storage.GetPoisoned());
		}

		protected List<TQueue> SlideItemsToPending(IDurableJobQueue<TQueue, TQueuePoison> jobStorage)
		{
			List<TQueue> items = new List<TQueue>();
			while (true)
			{
				TQueue item = jobStorage.TransitionNextQueuedItemToPending();

				if (typeof(TQueue).IsValueType)
				{
					if (default(TQueue).Equals(item))
						break;
				}
				else
				{
					if (object.Equals(null, item))
						break;
				}

				items.Add(item);
			}

			return items;
		}

		[Fact]
		public void Queue_DoesNotThrow_NonNullItem()
		{
			var storage = jobStorageFactory();
			Assert.DoesNotThrow(() => storage.Queue(fixture.CreateAnonymous<TQueue>()));
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

		[Fact]
		public void Queue_DoesNotModifyPendingList()
		{
			var storage = jobStorageFactory();
			storage.Queue(fixture.CreateAnonymous<TQueue>());

			Assert.Empty(storage.GetPending());
		}

		[Fact]
		public void Queue_DoesNotModifyPoisonedList()
		{
			var storage = jobStorageFactory();
			storage.Queue(fixture.CreateAnonymous<TQueue>());

			Assert.Empty(storage.GetPoisoned());
		}

		[Fact]
		public void TransitionNextQueuedItemToPending_PreservesOrderingInPendingList()
		{
			var storage = jobStorageFactory();

			var items = fixture.CreateMany<TQueue>(15).ToList();
			foreach (var item in items)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
			}

			Assert.True(items.SequenceEqual(storage.GetPending(), GenericEqualityComparer<TQueue>.ByAllMembers()));
		}

		[Fact]
		public void TransitionNextQueuedItemToPending_ProperlyRemovesItemFromQueuedList()
		{
			var storage = jobStorageFactory();

			var items = fixture.CreateMany<TQueue>(15).ToList();
			foreach (var item in items)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
			}

			Assert.Empty(storage.GetQueued());
		}

		[Fact]
		public void TransitionNextQueuedItemToPending_DoesNotModifyPoisonedList()
		{
			var storage = jobStorageFactory();

			var items = fixture.CreateMany<TQueue>(15).ToList();
			foreach (var item in items)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
			}

			Assert.Empty(storage.GetPoisoned());
		}

		[Fact]
		public void TransitionNextQueuedItemToPending_ReturnsDefaultOnEmptyQueueList()
		{
			var storage = jobStorageFactory();
			Assert.Equal(default(TQueue), storage.TransitionNextQueuedItemToPending());
		}

		[Fact]
		public void ResetAllPendingToQueued_PreservesOriginalOrder()
		{
			var storage = jobStorageFactory();

			var queueItems = fixture.CreateMany<TQueue>(15).ToList();
			foreach (var item in queueItems)
				storage.Queue(item);

			storage.ResetAllPendingToQueued();

			List<TQueue> items = SlideItemsToPending(storage).ToList();

			Assert.True(queueItems.SequenceEqual(items, GenericEqualityComparer<TQueue>.ByAllMembers()));
		}

		[Fact]
		public void ResetAllPendingToQueued_DoesNotThrowOnEmptyPendingList()
		{
			var storage = jobStorageFactory();

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
			Assert.Throws<ArgumentNullException>(() => storage.Poison(fixture.CreateAnonymous<TQueue>(), item));
		}

		[Fact]
		public void Poison_ReturnsFalse_OnMissingItem()
		{
			var storage = jobStorageFactory();

			Assert.False(storage.Poison(fixture.CreateAnonymous<TQueue>(), fixture.CreateAnonymous<TQueuePoison>()));
		}

		[Fact]
		public void Poison_ReturnsFalse_OnQueuedItem()
		{
			var storage = jobStorageFactory();

			var item = fixture.CreateAnonymous<TQueue>();
			storage.Queue(item);

			Assert.False(storage.Poison(item, fixture.CreateAnonymous<TQueuePoison>()));
		}

		[Fact]
		public void Poison_ReturnsTrue_OnPendingItem()
		{
			var storage = jobStorageFactory();

			var item = fixture.CreateAnonymous<TQueue>();
			storage.Queue(item);
			storage.TransitionNextQueuedItemToPending();

			Assert.True(storage.Poison(item, poisonConverter(item)));
		}

		[Fact]
		public void Poison_GetPoisoned_QueuesMatch()
		{
			var storage = jobStorageFactory();

			var item = fixture.CreateAnonymous<TQueue>();
			storage.Queue(item);
			storage.TransitionNextQueuedItemToPending();
			var poison = poisonConverter(item);
			storage.Poison(item, poison);

			Assert.True(storage.GetPoisoned().SequenceEqual(new [] { poison }, GenericEqualityComparer<TQueuePoison>.
			ByAllMembers()));
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

		[Fact]
		public void Delete_ReturnsFalseOnMissingItem()
		{
			var storage = jobStorageFactory();

			Assert.False(storage.Delete(fixture.CreateAnonymous<TQueuePoison>()));
		}

		[Fact]
		public void Delete_ReturnsFalseOnPendingItem()
		{
			var storage = jobStorageFactory();

			var item = fixture.CreateAnonymous<TQueue>();
			storage.Queue(item);
			storage.TransitionNextQueuedItemToPending();
			var poison = poisonConverter(item);

			Assert.False(storage.Delete(poison));
		}

		private T GetStorageWithPoisonedItem(out TQueuePoison poison)
		{
			var storage = jobStorageFactory();
			var item = fixture.CreateAnonymous<TQueue>();
			storage.Queue(item);
			storage.TransitionNextQueuedItemToPending();
			poison = poisonConverter(item);
			storage.Poison(item, poison);

			return storage;
		}

		[Fact]
		public void Delete_ReturnsTrue_OnExistingPoisonedItem()
		{
			TQueuePoison poison;
			var storage = GetStorageWithPoisonedItem(out poison);

			Assert.True(storage.Delete(poison));
		}

		[Fact]
		public void Delete_GetPoisoned_ReturnsExpectedCount()
		{
			TQueuePoison poison;
			var storage = GetStorageWithPoisonedItem(out poison);
			storage.Delete(poison);

			Assert.Empty(storage.GetPoisoned());
		}

		[Fact]
		public void Delete_DoesNotModifyQueued()
		{
			TQueuePoison poison;
			var storage = GetStorageWithPoisonedItem(out poison);
			storage.Queue(fixture.CreateAnonymous<TQueue>());
			storage.Queue(fixture.CreateAnonymous<TQueue>());
			storage.TransitionNextQueuedItemToPending();
			storage.Delete(poison);

			Assert.Single(storage.GetQueued());
		}

		[Fact]
		public void Delete_DoesNotModifyPending()
		{
			TQueuePoison poison;
			var storage = GetStorageWithPoisonedItem(out poison);
			storage.Queue(fixture.CreateAnonymous<TQueue>());
			storage.Queue(fixture.CreateAnonymous<TQueue>());
			storage.TransitionNextQueuedItemToPending();
			storage.Delete(poison);

			Assert.Single(storage.GetPending());
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

		[Fact]
		public void Complete_ReturnsFalse_OnMissingItem()
		{
			var storage = jobStorageFactory();

			Assert.False(storage.Complete(fixture.CreateAnonymous<TQueue>()));
		}

		[Fact]
		public void Complete_ReturnsTrue_OnPendingItem()
		{
			var storage = jobStorageFactory();

			var item = fixture.CreateAnonymous<TQueue>();
			storage.Queue(item);
			storage.TransitionNextQueuedItemToPending();

			Assert.True(storage.Complete(item));
		}

		[Fact]
		public void GetQueued_IsEmptyOnClearedQueues()
		{
			var storage = jobStorageFactory();

			Assert.Empty(storage.GetQueued());
		}

		[Fact]
		public void GetQueued_PreservesOrdering()
		{
			var storage = jobStorageFactory();

			var queueItems = fixture.CreateMany<TQueue>(15).ToList();
			foreach (var item in queueItems)
				storage.Queue(item);

			Assert.True(queueItems.SequenceEqual(storage.GetQueued(), GenericEqualityComparer<TQueue>.ByAllMembers()));
		}

		[Fact]
		public void GetPending_IsEmptyOnClearedQueues()
		{
			var storage = jobStorageFactory();

			Assert.Empty(storage.GetPending());
		}

		[Fact]
		public void GetPending_PreservesOrdering()
		{
			var storage = jobStorageFactory();

			var queueItems = fixture.CreateMany<TQueue>(15).ToList();
			foreach (var item in queueItems)
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
			}

			Assert.True(queueItems.SequenceEqual(storage.GetPending(), GenericEqualityComparer<TQueue>.ByAllMembers()));
		}

		[Fact]
		public void GetPoisoned_IsEmptyOnClearedQueues()
		{
			var storage = jobStorageFactory();

			Assert.Empty(storage.GetPoisoned());
		}

		[Fact]
		public void GetPoisoned_PreservesOrdering()
		{
			var storage = jobStorageFactory();

			List<TQueuePoison> poisonedItems = new List<TQueuePoison>();

			foreach (var item in fixture.CreateMany<TQueue>(15))
			{
				storage.Queue(item);
				storage.TransitionNextQueuedItemToPending();
				var poisoned = poisonConverter(item);
				poisonedItems.Add(poisoned);
				storage.Poison(item, poisoned);
			}

			Assert.True(poisonedItems.SequenceEqual(storage.GetPoisoned(), GenericEqualityComparer<TQueuePoison>.ByAllMembers()));
		}
	}
}