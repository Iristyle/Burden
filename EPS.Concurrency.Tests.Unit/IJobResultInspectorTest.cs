using System;
using System.Reactive;
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
			var result = factory().Inspect(Notification.CreateOnNext(JobResult.Create(A.Dummy<TJobInput>(), A.Dummy<TJobOutput>())));

			Assert.NotNull(result);
		}

		[Fact]
		public void Inspect_ReturnsUnknownJobResult_WhenNotificationKindOnError_WithNullJobResult()
		{
			var notification = Notification.CreateOnError<JobResult<TJobInput, TJobOutput>>(new ArgumentException());
			var result = factory().Inspect(notification);

			Assert.Equal(JobQueueActionType.Unknown, result.ActionType);
		}

		[Fact]
		public void Inspect_ReturnsPoisonJobResult_WhenNotificationKindOnError()
		{
			//fakeiteasy should attach a faked up TJobInput to the Error
			var notification = Notification.CreateOnError<JobResult<TJobInput, TJobOutput>>(A.Fake<JobQueueException<TJobInput>>());
			var result = factory().Inspect(notification);

			Assert.Equal(JobQueueActionType.Poison, result.ActionType);		
		}

		[Fact]
		public void Inspect_ReturnsCompleteJobResult_WhenNotificationComplete()
		{
			var notification = Notification.CreateOnCompleted<JobResult<TJobInput, TJobOutput>>();
			var result = factory().Inspect(notification);

			Assert.Equal(JobQueueActionType.Complete, result.ActionType);
		}

		[Fact]
		public void Inspect_ReturnsNoActionJobResult_WhenNotificationOnNext()
		{
			var notification = Notification.CreateOnNext<JobResult<TJobInput, TJobOutput>>(JobResult.Create(A.Dummy<TJobInput>(), A.Dummy<TJobOutput>()));
			var result = factory().Inspect(notification);

			Assert.Equal(JobQueueActionType.NoAction, result.ActionType);
		}

	}
}