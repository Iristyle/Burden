using System;
using Xunit;
using Xunit.Extensions;

namespace EPS.Concurrency.Tests.Unit
{
	public class JobQueueActionTestByValueTypePoison
		: JobQueueActionTest<int>
	{ }

	public class JobQueueActionTestByReferenceTypePoison
		: JobQueueActionTest<object>
	{ }

	public abstract class JobQueueActionTest<TQueuePoison>
	{
		[Theory]
		[InlineData(JobQueueActionType.Complete)]
		[InlineData(JobQueueActionType.Poison)]
		[InlineData(JobQueueActionType.Unknown)]
		public void Constructor_ByActionType_ReturnsExpectedActionType(JobQueueActionType actionType)
		{
			var jobQueueAction = new JobQueueAction<TQueuePoison>(actionType);

			Assert.Equal(actionType, jobQueueAction.ActionType);
		}

		[Theory]
		[InlineData(JobQueueActionType.Complete)]
		[InlineData(JobQueueActionType.Poison)]
		[InlineData(JobQueueActionType.Unknown)]
		public void Constructor_ByActionType_ReturnsDefaultObjectForPoison(JobQueueActionType actionType)
		{
			var jobQueueAction = new JobQueueAction<TQueuePoison>(actionType);

			Assert.Equal(default(TQueuePoison), jobQueueAction.QueuePoison);
		}

		[Fact]
		public void Constructor_Poison_ReturnsActionTypePoison()
		{
			var jobQueueAction = new JobQueueAction<TQueuePoison>(default(TQueuePoison));

			Assert.Equal(JobQueueActionType.Poison, jobQueueAction.ActionType);
		}

		[Fact]
		public void Constructor_Poison_ReturnsDefaultObjectForPoison()
		{
			var jobQueueAction = new JobQueueAction<TQueuePoison>(default(TQueuePoison));

			Assert.Equal(default(TQueuePoison), jobQueueAction.QueuePoison);
		}

	}
}

