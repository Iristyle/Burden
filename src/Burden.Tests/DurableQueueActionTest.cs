using System;
using Ploeh.AutoFixture;
using Xunit;

namespace Burden.Tests
{
	public class DurableValueTypeQueueActionTest :
		DurableQueueActionTest<int, int>
	{ }

	public class DurableReferenceTypeQueueActionTest :
		DurableQueueActionTest<string, string>
	{ }

	public abstract class DurableQueueActionTest<TQueue, TQueuePoison>
	{
		private Fixture fixture = new Fixture();
 
		[Fact]
		public void Completed_HasCompletedActionType()
		{
			var action = DurableJobQueueAction.Completed(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(DurableJobQueueActionType.Completed, action.ActionType);
		}

		[Fact]
		public void Completed_HasMatchingInput()
		{
			var item = fixture.CreateAnonymous<TQueue>();
			var action = DurableJobQueueAction.Completed(item);
			Assert.Equal(item, action.Input);
		}

		[Fact]
		public void Completed_HasNullPoison()
		{
			var action = DurableJobQueueAction.Completed(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(null, action.Poison);
		}

		[Fact]
		public void ImplicitCastOperator_Completed_HasDefaultPoison()
		{
			var action = DurableJobQueueAction.Completed(fixture.CreateAnonymous<TQueue>());
			var cast = (DurableJobQueueAction<TQueue, TQueuePoison>)action;
			Assert.Equal(default(TQueuePoison), cast.Poison);
		}

		[Fact]
		public void Completed_Throws_OnNullInput_ForReferenceTypes()
		{
			if (typeof(TQueue).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Completed(default(TQueue)));
		}

		[Fact]
		public void Pending_HasPendingActionType()
		{
			var action = DurableJobQueueAction.Pending(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(DurableJobQueueActionType.Pending, action.ActionType);
		}

		[Fact]
		public void Pending_HasMatchingInput()
		{
			var item = fixture.CreateAnonymous<TQueue>();
			var action = DurableJobQueueAction.Pending(item);
			Assert.Equal(item, action.Input);
		}

		[Fact]
		public void Pending_HasNullPoison()
		{
			var action = DurableJobQueueAction.Pending(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(null, action.Poison);
		}

		[Fact]
		public void ImplicitCastOperator_Pending_HasDefaultPoison()
		{
			var action = DurableJobQueueAction.Pending(fixture.CreateAnonymous<TQueue>());
			var cast = (DurableJobQueueAction<TQueue, TQueuePoison>)action;
			Assert.Equal(default(TQueuePoison), cast.Poison);
		}

		[Fact]
		public void Pending_Throws_OnNullInput_ForReferenceTypes()
		{
			if (typeof(TQueue).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Pending(default(TQueue)));
		}

		[Fact]
		public void Queued_HasQueuedActionType()
		{
			var action = DurableJobQueueAction.Queued(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(DurableJobQueueActionType.Queued, action.ActionType);
		}

		[Fact]
		public void Queued_HasMatchingInput()
		{
			var item = fixture.CreateAnonymous<TQueue>();
			var action = DurableJobQueueAction.Queued(item);
			Assert.Equal(item, action.Input);
		}

		[Fact]
		public void Queued_HasNullPoison()
		{
			var action = DurableJobQueueAction.Queued(fixture.CreateAnonymous<TQueue>());
			Assert.Equal(null, action.Poison);
		}

		[Fact]
		public void ImplicitCastOperator_Queued_HasDefaultPoison()
		{
			var action = DurableJobQueueAction.Queued(fixture.CreateAnonymous<TQueue>());
			var cast = (DurableJobQueueAction<TQueue, TQueuePoison>)action;
			Assert.Equal(default(TQueuePoison), cast.Poison);
		}

		[Fact]
		public void Queued_Throws_OnNullInput_ForReferenceTypes()
		{
			if (typeof(TQueue).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Queued(default(TQueue)));
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
			var action = DurableJobQueueAction.Deleted(fixture.CreateAnonymous<TQueuePoison>());
			Assert.Equal(DurableJobQueueActionType.Deleted, action.ActionType);
		}

		[Fact]
		public void Deleted_HasMatchingPoison()
		{
			var poison = fixture.CreateAnonymous<TQueuePoison>();
			var action = DurableJobQueueAction.Deleted(poison);
			Assert.Equal(poison, action.Poison);
		}

		[Fact]
		public void Deleted_HasNullInput()
		{
			var action = DurableJobQueueAction.Deleted(fixture.CreateAnonymous<TQueuePoison>());
			Assert.Equal(null, action.Input);
		}

		[Fact]
		public void ImplicitCastOperator_Deleted_HasDefaultInput()
		{
			var action = DurableJobQueueAction.Deleted(fixture.CreateAnonymous<TQueuePoison>());
			var cast = (DurableJobQueueAction<TQueue, TQueuePoison>)action;
			Assert.Equal(default(TQueue), cast.Input);
		}

		[Fact]
		public void Deleted_Throws_OnNullPoison_ForReferenceTypes()
		{
			if (typeof(TQueuePoison).IsValueType)
				return;

			Assert.Throws<ArgumentNullException>(() => DurableJobQueueAction.Deleted(default(TQueuePoison)));
		}
	}
}