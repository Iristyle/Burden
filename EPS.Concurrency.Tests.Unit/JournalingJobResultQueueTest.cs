using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Common.Logging;
using FakeItEasy;
using Xunit;
using Xunit.Extensions;

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
		public void Subscribe_IgnoresNullNotifications()
		{
			var durableJobStorage = A.Fake<IDurableJobStorageQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var items = new Notification<JobResult<int, int>>[]
			{
				null,
				Notification.CreateOnNext(new JobResult<int, int>(1, 1)),
				null,
				null,
				Notification.CreateOnNext(new JobResult<int, int>(1, 1))
			};
			var observable = items.ToObservable();
			var queue = new JournalingJobResultQueue<int, int, object>(observable, jobResultInspector, durableJobStorage, A.Fake<ILog>(), Scheduler.Immediate);

			A.CallTo(() => jobResultInspector.Inspect(A<Notification<JobResult<int, int>>>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(items.Count(n => null != n)));
		}

		[Fact]
		public void Subscribe_NullNotificationTriggersLogging()
		{
			var durableJobStorage = A.Fake<IDurableJobStorageQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var observable = new Notification<JobResult<int, int>>[]
			{ null
			}.ToObservable();
			var log = A.Fake<ILog>();

			var queue = new JournalingJobResultQueue<int, int, object>(observable, jobResultInspector, durableJobStorage, log, Scheduler.Immediate);

			A.CallTo(() => log.Error(A<Action<FormatMessageHandler>>.Ignored)).MustHaveHappened();
		}

		[Fact]
		public void Subscribe_EmptyObservableNeverTriggersInspection()
		{
			var durableJobStorage = A.Fake<IDurableJobStorageQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var observable = new Notification<JobResult<int, int>>[]
			{ }.ToObservable();

			var queue = new JournalingJobResultQueue<int, int, object>(observable, jobResultInspector, durableJobStorage, A.Fake<ILog>(), Scheduler.Immediate);

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

			var queue = new JournalingJobResultQueue<int, int, object>(observable, jobResultInspector, durableJobStorage, A.Fake<ILog>(), Scheduler.Immediate);

			A.CallTo(() => jobResultInspector.Inspect(A<Notification<JobResult<int, int>>>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
		}

		class JobDetails
		{
			public IDurableJobStorageQueue<int, object> DurableStorage { get; set; }
			public JournalingJobResultQueue<int, int, object> JournalingJobResultQueue { get; set; }
			public IJobResultInspector<int, int, object> Inspector { get; set; }
			public ILog Log { get; set; }
		}

		private static JobDetails SimulateInspectionResultsToMonitorDurableJobStorageBehavior(JobQueueAction<object> [] inspectionResults)
		{
			var details = new JobDetails()
			{
				DurableStorage = A.Fake<IDurableJobStorageQueue<int, object>>(),
				Inspector = A.Fake<IJobResultInspector<int, int, object>>(),
				Log = A.Fake<ILog>()
			};
			var observable = Enumerable.Repeat(Notification.CreateOnNext(new JobResult<int, int>(1, 1)), inspectionResults.Length)
			.ToObservable();

			A.CallTo(() => details.Inspector.Inspect(A<Notification<JobResult<int, int>>>.Ignored)).ReturnsNextFromSequence(inspectionResults);

			details.JournalingJobResultQueue = new JournalingJobResultQueue<int, int, object>(observable, details.Inspector, details.DurableStorage, details.Log, Scheduler.CurrentThread);
			
			return details;
		}

		public static IEnumerable<object[]> GetExpectedCompletionCountsForJobQueueAction
		{
			get
			{
				yield return new object[] { Enumerable.Repeat(null as JobQueueAction<object>, 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(), 5), 5 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(new object()), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 12), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15), 15 };
				//mix'n'match
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15)
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(new object()), 3))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 4))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(), 2))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 1))
					.Concat(Enumerable.Repeat(null as JobQueueAction<object>, 2))
					, 17 };
			}
		}

		[Theory]
		[PropertyData("GetExpectedCompletionCountsForJobQueueAction")]
		public void Subscribe_JobQueueActionOfTypeTriggersCorrectNumberOfCallsToDurableJobQueueComplete(IEnumerable<JobQueueAction<object>> jobQueueActions, int expectedCalls)
		{
			var jobDetails = SimulateInspectionResultsToMonitorDurableJobStorageBehavior(jobQueueActions.ToArray());
			A.CallTo(() => jobDetails.DurableStorage.Complete(A<int>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(expectedCalls));
		}

		public static IEnumerable<object[]> GetExpectedPoisonCountsForJobQueueAction
		{
			get
			{
				yield return new object[] { Enumerable.Repeat(null as JobQueueAction<object>, 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(), 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(new object()), 3), 3 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 12), 12 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15), 0 };
				//mix'n'match
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15)
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(new object()), 3))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 4))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(), 2))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 1))
					.Concat(Enumerable.Repeat(null as JobQueueAction<object>, 2))
					, 4 };
				
			}
		}

		[Theory]
		[PropertyData("GetExpectedPoisonCountsForJobQueueAction")]
		public void Subscribe_JobQueueActionOfTypeTriggersCorrectNumberOfCallsToDurableJobQueuePoison(IEnumerable<JobQueueAction<object>> jobQueueActions, int expectedCalls)
		{
			var jobDetails = SimulateInspectionResultsToMonitorDurableJobStorageBehavior(jobQueueActions.ToArray());
			A.CallTo(() => jobDetails.DurableStorage.Poison(A<int>.Ignored, A<object>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(expectedCalls));
		}

		public static IEnumerable<object[]> GetExpectedDeleteCountsForJobQueueAction
		{
			get
			{
				yield return new object[] { Enumerable.Repeat(null as JobQueueAction<object>, 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(), 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(new object()), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 12), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15), 0 };
				//mix'n'match
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15)
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(new object()), 3))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 4))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(), 2))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 1))
					.Concat(Enumerable.Repeat(null as JobQueueAction<object>, 2))
					, 0 };
			}
		}

		[Theory]
		[PropertyData("GetExpectedDeleteCountsForJobQueueAction")]
		public void Subscribe_JobQueueActionOfTypeTriggersCorrectNumberOfCallsToDurableJobQueueDelete(IEnumerable<JobQueueAction<object>> jobQueueActions, int expectedCalls)
		{
			var jobDetails = SimulateInspectionResultsToMonitorDurableJobStorageBehavior(jobQueueActions.ToArray());
			A.CallTo(() => jobDetails.DurableStorage.Delete(A<object>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(expectedCalls));
		}

		public static IEnumerable<object[]> GetExpectedLogCountsForJobQueueAction
		{
			get
			{
				yield return new object[] { Enumerable.Repeat(null as JobQueueAction<object>, 5), 5 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(), 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(new object()), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 12), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 3), 3 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15), 0 };
				//mix'n'match
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15)
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(new object()), 3))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 4))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(), 2))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 1))
					.Concat(Enumerable.Repeat(null as JobQueueAction<object>, 2))
					, 6 };
			}
		}

		[Theory]
		[PropertyData("GetExpectedLogCountsForJobQueueAction")]
		public void Subscribe_JobQueueActionOfTypeTriggersCorrectNumberOfErrorLogCalls(IEnumerable<JobQueueAction<object>> jobQueueActions, int expectedCalls)
		{
			var jobDetails = SimulateInspectionResultsToMonitorDurableJobStorageBehavior(jobQueueActions.ToArray());
			A.CallTo(() => jobDetails.Log.Error(A<Action<FormatMessageHandler>>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(expectedCalls));
		}
	}
}