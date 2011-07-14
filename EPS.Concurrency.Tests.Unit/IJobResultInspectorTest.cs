using System;
using FakeItEasy;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public abstract class IJobResultInspectorTest<TJobInput, TJobOutput, TQueuePoison>
	{
		protected Func<IJobResultInspector<TJobInput, TJobOutput, TQueuePoison>> factory;

		public IJobResultInspectorTest(Func<IJobResultInspector<TJobInput, TJobOutput, TQueuePoison>> factory)
		{
			this.factory = factory;
		}

		[Fact]
		public void Inspect_ThrowsOnNullNotification()
		{
			var instance = factory();
			Assert.Throws<ArgumentNullException>(() => instance.Inspect(null));
		}

		[Fact]
		public void Inspect_ReturnsNonNullResultOnNonNullInput()
		{
			var result = factory().Inspect(JobResult.CreateOnCompletion(A.Dummy<TJobInput>(), A.Dummy<TJobOutput>()));

			Assert.NotNull(result);
		}

		[Fact]
		public void Inspect_ReturnsUnknownJobResult_WhenNotificationKindOnError_WithNullJobResult()
		{
			var jobResult = JobResult.CreateOnError(A.Dummy<TJobInput>(), new ArgumentException());
			var result = factory().Inspect(jobResult);

			Assert.Equal(JobQueueActionType.Unknown, result.ActionType);
		}

		[Fact]
		public void Inspect_ReturnsPoisonJobResult_WhenNotificationKindOnError()
		{
			//fakeiteasy should attach a faked up TJobInput to the Error
			var jobResult = JobResult.CreateOnError(A.Dummy<TJobInput>(), new ArgumentNullException());
			var result = factory().Inspect(jobResult);

			Assert.Equal(JobQueueActionType.Poison, result.ActionType);		
		}

		[Fact]
		public void Inspect_ReturnsCompleteJobResult_WhenNotificationComplete()
		{
			var jobResult = JobResult.CreateOnCompletion(A.Dummy<TJobInput>(), A.Dummy<TJobOutput>());
			var result = factory().Inspect(jobResult);

			Assert.Equal(JobQueueActionType.Complete, result.ActionType);
		}
	}
}