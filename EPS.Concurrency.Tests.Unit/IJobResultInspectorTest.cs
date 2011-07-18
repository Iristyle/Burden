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
		public void Inspect_ReturnsNonNullResult_OnNonNullInput()
		{
			var result = factory().Inspect(JobResult.CreateOnCompletion(A.Dummy<TJobInput>(), A.Dummy<TJobOutput>()));

			Assert.NotNull(result);
		}

		private JobQueueAction<TQueuePoison> GetErrorInspectionResults(Exception exception)
		{
			var jobResult = JobResult.CreateOnError(A.Dummy<TJobInput>(), new ArgumentNullException());
			return factory().Inspect(jobResult);
		}

		[Fact]
		public void Inspect_ReturnsPoisonQueueAction_ForErrorJobResult_WithNonNullJobResult_NonNullException()
		{
			var result = GetErrorInspectionResults(new ArgumentNullException());
			Assert.Equal(JobQueueActionType.Poison, result.ActionType);
		}

		[Fact]
		public void Inspect_ReturnsPoisonQueueAction_ForErrorJobResult_WithNonNullJobResult_NullException()
		{
			var result = GetErrorInspectionResults(null);
			Assert.Equal(JobQueueActionType.Poison, result.ActionType);
		}
	}
}