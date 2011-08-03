using System;
using System.Threading;
using EPS.Utility;
using Ploeh.AutoFixture;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class ObservableValueTypeDurableJobQueueTest
	: ObservableDurableJobQueueTest<int, int>
	{
		public ObservableValueTypeDurableJobQueueTest()
			: base(input => 3)
		{ }
	}

	public class ObservableReferenceTypeDurableJobQueueTest
	: ObservableDurableJobQueueTest<string, string>
	{
		public ObservableReferenceTypeDurableJobQueueTest()
			: base (input => "foo")
		{ }
	}

	public abstract class ObservableDurableJobQueueTest<TQueue, TQueuePoison>
	: DurableJobQueueTest<ObservableDurableJobQueue<TQueue, TQueuePoison>, TQueue, TQueuePoison>
	{
		private static IDurableJobQueue<TQueue, TQueuePoison> GetTransient()
		{
			return new TransientJobQueue<TQueue, TQueuePoison>(GenericEqualityComparer<TQueue>.ByAllMembers(), 
			GenericEqualityComparer<TQueuePoison>.ByAllMembers());
		}
		protected ObservableDurableJobQueueTest(Func<TQueue, TQueuePoison> poisonConverter)
			: base(() => new ObservableDurableJobQueue<TQueue, TQueuePoison>(GetTransient()), poisonConverter)
		{ }

		[Fact]
		public void Constructor_Throws_OnNullDurableJobQueue()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var q = new ObservableDurableJobQueue<TQueue, TQueuePoison>(null)) {} });
		}

		[Fact]
		public void Constructor_Throws_WhenNestingObservableDurableJobQueues()
		{
			Assert.Throws<ArgumentException>(() => { using (var q = new ObservableDurableJobQueue<TQueue, TQueuePoison>(new 
			ObservableDurableJobQueue<TQueue, TQueuePoison>(GetTransient()))) {} });
				
		}

		class Observation
		{
			public DurableJobQueueAction<TQueue, TQueuePoison> Action { get; set; }
			public TQueue Input { get; set; }
			public TQueuePoison Poison { get; set; }
		}

		private Observation SubscribeForAction(DurableJobQueueActionType filterType, Action<
		ObservableDurableJobQueue<TQueue, TQueuePoison>, TQueue, TQueuePoison> actions)
		{
			var observation = new Observation() {
				Input = Fixture.CreateAnonymous<TQueue>(),
				Poison = Fixture.CreateAnonymous<TQueuePoison>() };

			using (var store = JobStorageFactory())
			using (var fired = new ManualResetEventSlim(false))
			using (var subscription = store.OnQueueAction.Subscribe(a =>
			{
				if (a.ActionType == filterType)
				{
					observation.Action = a;
					fired.Set();
				}
			}))
			{
				actions(store, observation.Input, observation.Poison);
				fired.Wait(TimeSpan.FromSeconds(2));

				return observation;
			}			
		}

		[Fact]
		public void Queue_GeneratesExpectedOnQueueAction()
		{
			var actionType = DurableJobQueueActionType.Queued;
			var observation = SubscribeForAction(actionType, (store, item, poison) =>
			{ 
				store.Queue(item);
			});
			
			Assert.True(observation.Action.ActionType == actionType && GenericEqualityComparer<TQueue>.ByAllMembers().Equals(observation.Action.Input, observation.Input));
		}

		[Fact]
		public void Pending_GeneratesExpectedOnQueueAction()
		{
			var actionType = DurableJobQueueActionType.Pending;
			var observation = SubscribeForAction(actionType, (store, item, poison) =>
			{
				store.Queue(item);
				store.NextQueuedItem();
			});

			Assert.True(observation.Action.ActionType == actionType && GenericEqualityComparer<TQueue>.ByAllMembers().Equals(observation.Action.Input, observation.Input));
		}

		[Fact]
		public void Completed_GeneratesExpectedOnQueueAction()
		{
			var actionType = DurableJobQueueActionType.Completed;
			var observation = SubscribeForAction(actionType, (store, item, poison) =>
			{
				store.Queue(item);
				store.NextQueuedItem();
				store.Complete(item);
			});

			Assert.True(observation.Action.ActionType == actionType && GenericEqualityComparer<TQueue>.ByAllMembers().Equals(observation.Action.Input, observation.Input));
		}

		[Fact]
		public void Poisoned_GeneratesExpectedOnQueueAction()
		{
			var actionType = DurableJobQueueActionType.Poisoned;
			var observation = SubscribeForAction(actionType, (store, item, poison) =>
			{
				store.Queue(item);
				store.NextQueuedItem();
				store.Poison(item, poison);
			});

			Assert.True(observation.Action.ActionType == actionType
				&& GenericEqualityComparer<TQueue>.ByAllMembers().Equals(observation.Action.Input, observation.Input)
				&& GenericEqualityComparer<TQueuePoison>.ByAllMembers().Equals(observation.Action.Poison, observation.Poison));
		}

		[Fact]
		public void Deleted_GeneratesExpectedOnQueueAction()
		{
			var actionType = DurableJobQueueActionType.Deleted;
			var observation = SubscribeForAction(actionType, (store, item, poison) =>
			{
				store.Queue(item);
				store.NextQueuedItem();
				store.Poison(item, poison);
				store.Delete(poison);
			});

			Assert.True(observation.Action.ActionType == actionType
				&& GenericEqualityComparer<TQueuePoison>.ByAllMembers().Equals(observation.Action.Poison, observation.Poison));
		}
	}
}