using System;
using Ploeh.AutoFixture;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class DurableQueueActionTest<TQueue, TQueuePoison>
	{
		private Fixture fixture = new Fixture();
 
		[Fact]
		public void Completed_HasCompletedActionType()
		{
			var action = DurableJobQueueAction.Completed<TQueue, TQueuePoison>(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(DurableJobQueueActionType.Completed, action.ActionType);
		}

		[Fact]
		public void Completed_HasMatchingInput()
		{
			var item = fixture.CreateAnonymous<TQueue>();
			var action = DurableJobQueueAction.Completed<TQueue, TQueuePoison>(item);
			Assert.Equal(item, action.Input);
		}

		[Fact]
		public void Completed_HasDefaultPoison()
		{
			var action = DurableJobQueueAction.Completed<TQueue, TQueuePoison>(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(default(TQueuePoison), action.Poison);
		}

		[Fact]
		public void Completed_Throws_OnNullInput_ForReferenceTypes()
		{
			if (typeof(TQueue).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Completed<TQueue, TQueuePoison>(default(TQueue)));
		}

		[Fact]
		public void Pending_HasPendingActionType()
		{
			var action = DurableJobQueueAction.Pending<TQueue, TQueuePoison>(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(DurableJobQueueActionType.Pending, action.ActionType);
		}

		[Fact]
		public void Pending_HasMatchingInput()
		{
			var item = fixture.CreateAnonymous<TQueue>();
			var action = DurableJobQueueAction.Pending<TQueue, TQueuePoison>(item);
			Assert.Equal(item, action.Input);
		}

		[Fact]
		public void Pending_HasDefaultPoison()
		{
			var action = DurableJobQueueAction.Pending<TQueue, TQueuePoison>(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(default(TQueuePoison), action.Poison);
		}

		[Fact]
		public void Pending_Throws_OnNullInput_ForReferenceTypes()
		{
			if (typeof(TQueue).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Pending<TQueue, TQueuePoison>(default(TQueue)));
		}

		[Fact]
		public void Queued_HasQueuedActionType()
		{
			var action = DurableJobQueueAction.Queued<TQueue, TQueuePoison>(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(DurableJobQueueActionType.Queued, action.ActionType);
		}

		[Fact]
		public void Queued_HasMatchingInput()
		{
			var item = fixture.CreateAnonymous<TQueue>();
			var action = DurableJobQueueAction.Queued<TQueue, TQueuePoison>(item);
			Assert.Equal(item, action.Input);
		}

		[Fact]
		public void Queued_HasDefaultPoison()
		{
			var action = DurableJobQueueAction.Queued<TQueue, TQueuePoison>(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(default(TQueuePoison), action.Poison);
		}

		[Fact]
		public void Queued_Throws_OnNullInput_ForReferenceTypes()
		{
			if (typeof(TQueue).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Queued<TQueue, TQueuePoison>(default(TQueue)));
		}

		[Fact]
		public void Poisoned_HasPoisonedActionType()
		{
			var action = DurableJobQueueAction.Poisoned(fixture.CreateAnonymous<TQueue>(), fixture.CreateAnonymous<TQueuePoison>());
			Assert.Equal(DurableJobQueueActionType.Poisoned, action.ActionType);
		}

		[Fact]
		public void Poisoned_HasMatchingInput()
		{
			var item = fixture.CreateAnonymous<TQueue>();
			var action = DurableJobQueueAction.Poisoned(item, fixture.CreateAnonymous<TQueuePoison>());
			Assert.Equal(item, action.Input);
		}

		[Fact]
		public void Poisoned_HasMatchingPoison()
		{
			var poison = fixture.CreateAnonymous<TQueuePoison>();
			var action = DurableJobQueueAction.Poisoned(fixture.CreateAnonymous<TQueue>(), poison);
			Assert.Equal(poison, action.Poison);
		}

		[Fact]
		public void Poisoned_Throws_OnNullInput_ForReferenceTypes()
		{
			if (typeof(TQueue).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Poisoned(default(TQueue), fixture.CreateAnonymous<TQueuePoison>()));
		}

		[Fact]
		public void Poisoned_Throws_OnNullPoison_ForReferenceTypes()
		{
			if (typeof(TQueuePoison).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Poisoned(fixture.CreateAnonymous<TQueue>(), default(TQueuePoison)));
		}

		[Fact]
		public void Deleted_HasDeletedActionType()
		{
			var action = DurableJobQueueAction.Deleted<TQueue, TQueuePoison>(fixture.CreateAnonymous<TQueuePoison>());
			Assert.Equal(DurableJobQueueActionType.Deleted, action.ActionType);
		}

		[Fact]
		public void Deleted_HasMatchingPoison()
		{
			var poison = fixture.CreateAnonymous<TQueuePoison>();
			var action = DurableJobQueueAction.Deleted<TQueue, TQueuePoison>(poison);
			Assert.Equal(poison, action.Poison);
		}

		[Fact]
		public void Deleted_HasDefaultInput()
		{
			var action = DurableJobQueueAction.Deleted<TQueue, TQueuePoison>(fixture.CreateAnonymous<TQueuePoison>());
			Assert.Equal(default(TQueue), action.Input);
		}

		[Fact]
		public void Deleted_Throws_OnNullPoison_ForReferenceTypes()
		{
			if (typeof(TQueuePoison).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Deleted<TQueue, TQueuePoison>(default(TQueuePoison)));
		}
	}

	public class DurableValueTypeQueueActionTest :
		DurableQueueActionTest<int, int>
	{ }

	public class DurableReferenceTypeQueueActionTest :
		DurableQueueActionTest<string, string>
	{ }
}