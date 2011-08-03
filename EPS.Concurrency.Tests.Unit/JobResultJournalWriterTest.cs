using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Common.Logging;
using FakeItEasy;
using Xunit;
using Xunit.Extensions;

namespace EPS.Concurrency.Tests.Unit
{
	public class JobResultJournalWriterTest
	{
		[Fact]
		public void Constructor_ThrowsOnNullDurableJobStorage()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var j = new JobResultJournalWriter<int, int, object>(A.Fake<IObservable<JobResult<int, int>>>(),
			A.Fake<IJobResultInspector<int, int, object>>(),
			null as IDurableJobQueue<int, object>)) {} });
		}

		[Fact]
		public void Constructor_ThrowsOnNullInspector()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var j = new JobResultJournalWriter<int, int, object>(A.Fake<IObservable<JobResult<int, int>>>(),
			null as IJobResultInspector<int, int, object>,
			A.Fake<IDurableJobQueue<int, object>>())) {} });
		}

		[Fact]
		public void Constructor_ThrowsOnNullObservable()
		{
			Assert.Throws<ArgumentNullException>(() => { using (var j = new JobResultJournalWriter<int, int, object>(null as IObservable<JobResult<int, int>>,
			A.Fake<IJobResultInspector<int, int, object>>(),
			A.Fake<IDurableJobQueue<int, object>>())) {} });
		}

		[Fact]
		public void Subscribe_IgnoresNullNotifications()
		{
			var durableJobStorage = A.Fake<IDurableJobQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var items = new JobResult<int, int>[]
			{
				null,
				new JobResult<int, int>(1, 1),
				null,
				null,
				new JobResult<int, int>(1, 1)
			};
			var observable = items.ToObservable();
			using (var queue = new JobResultJournalWriter<int, int, object>(observable, jobResultInspector, durableJobStorage, A.Fake<ILog>(), Scheduler.Immediate))
			{
				A.CallTo(() => jobResultInspector.Inspect(A<JobResult<int, int>>.Ignored))
					.MustHaveHappened(Repeated.Exactly.Times(items.Count(n => null != n)));
			}
		}

		[Fact]
		public void Subscribe_NullNotificationTriggersLogging()
		{
			var durableJobStorage = A.Fake<IDurableJobQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var results = new JobResult<int, int>[]	{ null };
			var observable = results.ToObservable();
			var log = A.Fake<ILog>();

			using (var writer = new JobResultJournalWriter<int, int, object>(observable, jobResultInspector, durableJobStorage, 
			log, Scheduler.Immediate))
			{
				A.CallTo(() => log.Error(CultureInfo.CurrentCulture, A<Action<FormatMessageHandler>>.Ignored))
					.MustHaveHappened(Repeated.Exactly.Times(results.Count(n => null == n)));
			}
		}

		[Fact]
		public void Subscribe_EmptyObservableNeverTriggersInspection()
		{
			var durableJobStorage = A.Fake<IDurableJobQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var observable = new JobResult<int, int>[]
			{ }.ToObservable();

			using (var writer = new JobResultJournalWriter<int, int, object>(observable, jobResultInspector, durableJobStorage, 
				A.Fake<ILog>(), Scheduler.Immediate))
			{
				A.CallTo(() => jobResultInspector.Inspect(A<JobResult<int, int>>.Ignored)).MustNotHaveHappened();
			}
		}

		[Fact]
		public void Subscribe_NonemptyObservableTriggersInspection()
		{
			var durableJobStorage = A.Fake<IDurableJobQueue<int, object>>();
			var jobResultInspector = A.Fake<IJobResultInspector<int, int, object>>();
			var observable = new []
			{
				new JobResult<int, int>(1, 1)
			}.ToObservable();

			using (var writer = new JobResultJournalWriter<int, int, object>(observable, jobResultInspector, durableJobStorage, 
				A.Fake<ILog>(), Scheduler.Immediate))
			{
				A.CallTo(() => jobResultInspector.Inspect(A<JobResult<int, int>>.Ignored)).MustHaveHappened(Repeated.Exactly.Once);
			}
		}

		class JobDetails
		{
			public IDurableJobQueue<int, object> DurableStorage { get; set; }
			public JobResultJournalWriter<int, int, object> JournalingJobResultQueue { get; set; }
			public IJobResultInspector<int, int, object> Inspector { get; set; }
			public ILog Log { get; set; }
		}

		private static JobDetails SimulateInspectionResultsToMonitorDurableJobStorageBehavior(JobQueueAction<object> [] inspectionResults)
		{
			var details = new JobDetails()
			{
				DurableStorage = A.Fake<IDurableJobQueue<int, object>>(),
				Inspector = A.Fake<IJobResultInspector<int, int, object>>(),
				Log = A.Fake<ILog>()
			};
			var observable = Enumerable.Repeat(new JobResult<int, int>(1, 1), inspectionResults.Length)
			.ToObservable();

			int counter = 0;
			using (var wait = new ManualResetEventSlim(false))
			{
				A.CallTo(() => details.Inspector.Inspect(A<JobResult<int, int>>.Ignored))
				.Invokes(call =>
					{ 
						if (Interlocked.Increment(ref counter) == inspectionResults.Length)
						{
							wait.Set();
						}
					})
				.ReturnsNextFromSequence(inspectionResults);

				details.JournalingJobResultQueue = new JobResultJournalWriter<int, int, object>(observable, details.Inspector, 
					details.DurableStorage, details.Log, Scheduler.Immediate);

				wait.Wait(TimeSpan.FromSeconds(5));

				return details;
			}
		}

		public static IEnumerable<object[]> GetExpectedCompletionCountsForJobQueueAction
		{
			get
			{
				yield return new object[] { Enumerable.Repeat(null as JobQueueAction<object>, 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 5), 5 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(new object()), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 12), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15), 15 };
				//mix'n'match
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15)
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(new object()), 3))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 4))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 2))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 1))
					.Concat(Enumerable.Repeat(null as JobQueueAction<object>, 2))
					, 17 };
			}
		}

		[Theory]
		[PropertyData("GetExpectedCompletionCountsForJobQueueAction")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Perfectly acceptable nested usage")]
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
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(new object()), 3), 3 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 12), 12 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15), 0 };
				//mix'n'match
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15)
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(new object()), 3))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 4))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 2))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 1))
					.Concat(Enumerable.Repeat(null as JobQueueAction<object>, 2))
					, 4 };
				
			}
		}

		[Theory]
		[PropertyData("GetExpectedPoisonCountsForJobQueueAction")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Perfectly acceptable nested usage")]
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
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(new object()), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 12), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15), 0 };
				//mix'n'match
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15)
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(new object()), 3))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 4))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 2))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 1))
					.Concat(Enumerable.Repeat(null as JobQueueAction<object>, 2))
					, 0 };
			}
		}

		[Theory]
		[PropertyData("GetExpectedDeleteCountsForJobQueueAction")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Perfectly acceptable nested usage")]
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
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 5), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(new object()), 3), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 12), 0 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 3), 3 };
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15), 0 };
				//mix'n'match
				yield return new object[] { Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 15)
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(new object()), 3))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Unknown), 4))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Complete), 2))
					.Concat(Enumerable.Repeat(new JobQueueAction<object>(JobQueueActionType.Poison), 1))
					.Concat(Enumerable.Repeat(null as JobQueueAction<object>, 2))
					, 6 };
			}
		}

		[Theory]
		[PropertyData("GetExpectedLogCountsForJobQueueAction")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Perfectly acceptable nested usage")]
		public void Subscribe_JobQueueActionOfTypeTriggersCorrectNumberOfErrorLogCalls(IEnumerable<JobQueueAction<object>> jobQueueActions, int expectedCalls)
		{
			var jobDetails = SimulateInspectionResultsToMonitorDurableJobStorageBehavior(jobQueueActions.ToArray());
			A.CallTo(() => jobDetails.Log.Error(CultureInfo.CurrentCulture, A<Action<FormatMessageHandler>>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(expectedCalls));
		}
	}
}