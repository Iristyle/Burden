using System;
using System.Reactive;
using System.Reactive.Linq;
using FakeItEasy;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class JournalingJobResultQueueTest
	{
		[Fact]
		public void Constructor_ThrowsOnNullDurableJobStorage()
		{
			Assert.Throws<ArgumentNullException>(() => new JournalingJobResultQueue<int, int, object>(A.Fake<IObservable<Notification<JobResult<int, int>>>>(),
			A.Fake<IJobResultInspector<int, int, object>>(),
			null as IDurableJobStorageQueue<int, object>));
		}

		[Fact]
		public void Constructor_ThrowsOnNullInspector()
		{
			Assert.Throws<ArgumentNullException>(() => new JournalingJobResultQueue<int, int, object>(A.Fake<IObservable<Notification<JobResult<int, int>>>>(),
			null as IJobResultInspector<int, int, object>,
			A.Fake<IDurableJobStorageQueue<int, object>>()));
		}

		[Fact]
		public void Constructor_ThrowsOnNullObservable()
		{
			Assert.Throws<ArgumentNullException>(() => new JournalingJobResultQueue<int, int, object>(null as IObservable<Notification<JobResult<int, int>>>,
			A.Fake<IJobResultInspector<int, int, object>>(),
			A.Fake<IDurableJobStorageQueue<int, object>>()));
		}

		[Fact]
		public void Subscribe_EmptyObservableNeverTriggersInspection()
		{
			var durableJobStorage = A.Fake<IDurableJobStorageQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var observable = new Notification<JobResult<int, int>>[] { null }.ToObservable();

			var queue = new JournalingJobResultQueue<int, int, object>(observable, jobResultInspector, durableJobStorage);

			A.CallTo(() => jobResultInspector.Inspect(A<Notification<JobResult<int, int>>>.Ignored)).MustNotHaveHappened();
		}

		[Fact]
		public void Subscribe_NonEmptyObservableTriggersInspection()
		{
			var durableJobStorage = A.Fake<IDurableJobStorageQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var observable = new []
			{ 
				Notification.CreateOnNext(new JobResult<int, int>(1, 1))
			}.ToObservable();

			var queue = new JournalingJobResultQueue<int, int, object>(observable, jobResultInspector, durableJobStorage);

			A.CallTo(() => jobResultInspector.Inspect(A<Notification<JobResult<int, int>>>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
		}
	}
}