using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EqualityComparer;
using Ploeh.AutoFixture;
using Xunit;

namespace Burden.Tests
{
	[SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "Non-issue for test classes really")]
	public abstract class DurableJobQueueTest<T, TQueue, TQueuePoison>
		where T: IDurableJobQueue<TQueue, TQueuePoison>
		{
			private readonly Fixture _fixture = new Fixture();
			private readonly Func<T> _jobStorageFactory;
			private readonly Func<TQueue, TQueuePoison> _poisonConverter;

			protected DurableJobQueueTest(Func<T> jobStorageFactory, Func<TQueue, TQueuePoison> poisonConverter)
			{
				this._jobStorageFactory = () =>
				{
					var instance = jobStorageFactory();
					ClearAllQueues(instance);
					return instance;
				};
				this._poisonConverter = poisonConverter;
			}

			protected Fixture Fixture
			{
				get { return _fixture; }
			}

			public Func<T> JobStorageFactory
			{
				get { return _jobStorageFactory; }
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

			protected ReadOnlyCollection<TQueue> SlideItemsToPending(IDurableJobQueue<TQueue, TQueuePoison> jobStorage)
			{
				List<TQueue> items = new List<TQueue>();
				while (true)
				{
					var item = jobStorage.NextQueuedItem();
					if (!item.Success) break;

					items.Add(item.Value);
				}

				return new ReadOnlyCollection<TQueue>(items);
			}

			[Fact]
			public void Queue_DoesNotThrow_NonNullItem()
			{
				var storage = JobStorageFactory();
				Assert.DoesNotThrow(() => storage.Queue(Fixture.CreateAnonymous<TQueue>()));
			}

			[Fact]
			public void Queue_ThrowsOnNullItemForReferenceTypes()
			{
				var storage = JobStorageFactory();

				if (typeof(TQueue).IsValueType)
					return;

				var item = (null as object).Cast<TQueue>();
				Assert.Throws<ArgumentNullException>(() => storage.Queue(item));
			}

			[Fact]
			public void Queue_DoesNotModifyPendingList()
			{
				var storage = JobStorageFactory();
				storage.Queue(Fixture.CreateAnonymous<TQueue>());

				Assert.Empty(storage.GetPending());
			}

			[Fact]
			public void Queue_DoesNotModifyPoisonedList()
			{
				var storage = JobStorageFactory();
				storage.Queue(Fixture.CreateAnonymous<TQueue>());

				Assert.Empty(storage.GetPoisoned());
			}

			[Fact]
			public void NextQueuedItem_PreservesOrderingInPendingList()
			{
				var storage = JobStorageFactory();

				var items = Fixture.CreateMany<TQueue>(15).ToList();
				foreach (var item in items)
				{
					storage.Queue(item);
					storage.NextQueuedItem();
				}

				Assert.True(items.SequenceEqual(storage.GetPending(), GenericEqualityComparer<TQueue>.ByAllMembers()));
			}

			[Fact]
			public void NextQueuedItem_ProperlyRemovesItemFromQueuedList()
			{
				var storage = JobStorageFactory();

				var items = Fixture.CreateMany<TQueue>(15).ToList();
				foreach (var item in items)
				{
					storage.Queue(item);
					storage.NextQueuedItem();
				}

				Assert.Empty(storage.GetQueued());
			}

			[Fact]
			public void NextQueuedItem_DoesNotModifyPoisonedList()
			{
				var storage = JobStorageFactory();

				var items = Fixture.CreateMany<TQueue>(15).ToList();
				foreach (var item in items)
				{
					storage.Queue(item);
					storage.NextQueuedItem();
				}

				Assert.Empty(storage.GetPoisoned());
			}

			[Fact]
			public void NextQueuedItem_ReturnsDefaultOnEmptyQueueList()
			{
				var storage = JobStorageFactory();
				Assert.Equal(Item.None<TQueue>(), storage.NextQueuedItem());
			}

			[Fact]
			public void ResetAllPendingToQueued_PreservesOriginalOrder()
			{
				var storage = JobStorageFactory();

				var queueItems = Fixture.CreateMany<TQueue>(15).ToList();
				foreach (var item in queueItems)
					storage.Queue(item);

				storage.ResetAllPendingToQueued();

				List<TQueue> items = SlideItemsToPending(storage).ToList();

				Assert.True(queueItems.SequenceEqual(items, GenericEqualityComparer<TQueue>.ByAllMembers()));
			}

			[Fact]
			public void ResetAllPendingToQueued_DoesNotThrowOnEmptyPendingList()
			{
				var storage = JobStorageFactory();

				Assert.DoesNotThrow(() => storage.ResetAllPendingToQueued());
			}

			[Fact]
			public void Poison_ThrowsOnNullItemForReferenceTypes()
			{
				var storage = JobStorageFactory();

				if (typeof(TQueue).IsValueType)
					return;

				var item = (null as object).Cast<TQueue>();
				Assert.Throws<ArgumentNullException>(() => storage.Poison(item, default(TQueuePoison)));
			}

			[Fact]
			public void Poison_ThrowsOnNullPoisonItemForReferenceTypes()
			{
				var storage = JobStorageFactory();

				if (typeof(TQueuePoison).IsValueType)
					return;

				var item = (null as object).Cast<TQueuePoison>();
				Assert.Throws<ArgumentNullException>(() => storage.Poison(Fixture.CreateAnonymous<TQueue>(), item));
			}

			[Fact]
			public void Poison_ReturnsFalse_OnMissingItem()
			{
				var storage = JobStorageFactory();

				Assert.False(storage.Poison(Fixture.CreateAnonymous<TQueue>(), Fixture.CreateAnonymous<TQueuePoison>()));
			}

			[Fact]
			public void Poison_ReturnsFalse_OnQueuedItem()
			{
				var storage = JobStorageFactory();

				var item = Fixture.CreateAnonymous<TQueue>();
				storage.Queue(item);

				Assert.False(storage.Poison(item, Fixture.CreateAnonymous<TQueuePoison>()));
			}

			[Fact]
			public void Poison_ReturnsTrue_OnPendingItem()
			{
				var storage = JobStorageFactory();

				var item = Fixture.CreateAnonymous<TQueue>();
				storage.Queue(item);
				storage.NextQueuedItem();

				Assert.True(storage.Poison(item, _poisonConverter(item)));
			}

			[Fact]
			public void Poison_GetPoisoned_QueuesMatch()
			{
				var storage = JobStorageFactory();

				var item = Fixture.CreateAnonymous<TQueue>();
				storage.Queue(item);
				storage.NextQueuedItem();
				var poison = _poisonConverter(item);
				storage.Poison(item, poison);

				Assert.True(storage.GetPoisoned().SequenceEqual(new [] { poison }, GenericEqualityComparer<TQueuePoison>.
				ByAllMembers()));
			}

			[Fact]
			public void Delete_ThrowsOnNullItemForReferenceTypes()
			{
				var storage = JobStorageFactory();

				if (typeof(TQueuePoison).IsValueType)
					return;

				var item = (null as object).Cast<TQueuePoison>();
				Assert.Throws<ArgumentNullException>(() => storage.Delete(item));
			}

			[Fact]
			public void Delete_ReturnsFalseOnMissingItem()
			{
				var storage = JobStorageFactory();

				Assert.False(storage.Delete(Fixture.CreateAnonymous<TQueuePoison>()));
			}

			[Fact]
			public void Delete_ReturnsFalseOnPendingItem()
			{
				var storage = JobStorageFactory();

				var item = Fixture.CreateAnonymous<TQueue>();
				storage.Queue(item);
				storage.NextQueuedItem();
				var poison = _poisonConverter(item);

				Assert.False(storage.Delete(poison));
			}

		private T GetStorageWithPoisonedItem(out TQueuePoison poison)
		{
			var storage = JobStorageFactory();
			var item = Fixture.CreateAnonymous<TQueue>();
			storage.Queue(item);
			storage.NextQueuedItem();
			poison = _poisonConverter(item);
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
				storage.Queue(Fixture.CreateAnonymous<TQueue>());
				storage.Queue(Fixture.CreateAnonymous<TQueue>());
				storage.NextQueuedItem();
				storage.Delete(poison);

				Assert.Single(storage.GetQueued());
			}

			[Fact]
			public void Delete_DoesNotModifyPending()
			{
				TQueuePoison poison;
				var storage = GetStorageWithPoisonedItem(out poison);
				storage.Queue(Fixture.CreateAnonymous<TQueue>());
				storage.Queue(Fixture.CreateAnonymous<TQueue>());
				storage.NextQueuedItem();
				storage.Delete(poison);

				Assert.Single(storage.GetPending());
			}

			[Fact]
			public void Complete_ThrowsOnNullItemForReferenceTypes()
			{
				var storage = JobStorageFactory();

				if (typeof(TQueue).IsValueType)
					return;

				var item = (null as object).Cast<TQueue>();
				Assert.Throws<ArgumentNullException>(() => storage.Complete(item));
			}

			[Fact]
			public void Complete_ReturnsFalse_OnMissingItem()
			{
				var storage = JobStorageFactory();

				Assert.False(storage.Complete(Fixture.CreateAnonymous<TQueue>()));
			}

			[Fact]
			public void Complete_ReturnsTrue_OnPendingItem()
			{
				var storage = JobStorageFactory();

				var item = Fixture.CreateAnonymous<TQueue>();
				storage.Queue(item);
				storage.NextQueuedItem();

				Assert.True(storage.Complete(item));
			}

			[Fact]
			public void GetQueued_IsEmptyOnClearedQueues()
			{
				var storage = JobStorageFactory();

				Assert.Empty(storage.GetQueued());
			}

			[Fact]
			public void GetQueued_PreservesOrdering()
			{
				var storage = JobStorageFactory();

				var queueItems = Fixture.CreateMany<TQueue>(15).ToList();
				foreach (var item in queueItems)
					storage.Queue(item);

				Assert.True(queueItems.SequenceEqual(storage.GetQueued(), GenericEqualityComparer<TQueue>.ByAllMembers()));
			}

			[Fact]
			public void GetPending_IsEmptyOnClearedQueues()
			{
				var storage = JobStorageFactory();

				Assert.Empty(storage.GetPending());
			}

			[Fact]
			public void GetPending_PreservesOrdering()
			{
				var storage = JobStorageFactory();

				var queueItems = Fixture.CreateMany<TQueue>(15).ToList();
				foreach (var item in queueItems)
				{
					storage.Queue(item);
					storage.NextQueuedItem();
				}

				Assert.True(queueItems.SequenceEqual(storage.GetPending(), GenericEqualityComparer<TQueue>.ByAllMembers()));
			}

			[Fact]
			public void GetPoisoned_IsEmptyOnClearedQueues()
			{
				var storage = JobStorageFactory();

				Assert.Empty(storage.GetPoisoned());
			}

			[Fact]
			public void GetPoisoned_PreservesOrdering()
			{
				var storage = JobStorageFactory();

				List<TQueuePoison> poisonedItems = new List<TQueuePoison>();

				foreach (var item in Fixture.CreateMany<TQueue>(15))
				{
					storage.Queue(item);
					storage.NextQueuedItem();
					var poisoned = _poisonConverter(item);
					poisonedItems.Add(poisoned);
					storage.Poison(item, poisoned);
				}

				Assert.True(poisonedItems.SequenceEqual(storage.GetPoisoned(), GenericEqualityComparer<TQueuePoison>.ByAllMembers())
				);
			}
		}
}