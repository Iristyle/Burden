using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using EPS.Utility;
using FakeItEasy;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class DurableJobStorageMonitorTest
	{
		public class Incoming
		{
			public int Id { get; set; }
		}

		[Fact]
		public void Constructor_ThrowsOnNullScheduler()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			Assert.Throws<ArgumentNullException>(() => new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, 500, null));
		}

		[Fact]
		public void Constructor_ThrowsOnNullDurableStorage()
		{
			Assert.Throws<ArgumentNullException>(() => new DurableJobStorageMonitor<Incoming, Incoming>(null, 500));
		}

		[Fact]
		public void Constructor_ThrowsOnLargerThanAllowedMaximumQueueSize()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var test = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, 20);
			Assert.Throws<ArgumentOutOfRangeException>(() => new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, test.MaxQueueItemsAllowed + 1));
		}

		[Fact]
		public void Constructor_ThrowsOnSmallerThanAllowedQueueSize()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			Assert.Throws<ArgumentOutOfRangeException>(() => new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, -1));
		}

		[Fact]
		public void Constructor_ResetsPendingToQueued()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var monitor = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, 500);

			A.CallTo(() => jobStorage.ResetAllPendingToQueued()).MustHaveHappened(Repeated.Exactly.Once);
		}

		[Fact]
		public void Monitor_DoesNotPumpQueueWithZeroSubscribers()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, 20, scheduler);

			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).Returns(new Incoming() { Id = 1 });

			scheduler.AdvanceBy(monitor.PollingInterval.Add(monitor.PollingInterval));

			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).MustNotHaveHappened();
		}

		[Fact]
		public void Monitor_AwaitsFirstSubscriberBeforePublishing()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, 20, scheduler);

			//X items + a null terminator
			var incomingItems = Enumerable.Repeat(new Incoming() { Id = 2 }, 5)
				.Concat(new Incoming[] { null }).ToArray();

			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).ReturnsNextFromSequence(incomingItems);

			//advance the amount of time that would normally cause our queue to be totally flushed
			scheduler.AdvanceBy(monitor.PollingInterval.Add(monitor.PollingInterval));
			//TODO: 7-6-2011-- minor bug here
			monitor.Subscribe();

			//and now that we have a subscriber, elapse enough time to publish the queue contents
			scheduler.AdvanceBy(monitor.PollingInterval.Add(monitor.PollingInterval));

			//our transition method should have been called X times based on the list of valid items + 1 time for the null
			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).MustHaveHappened(Repeated.Exactly.Times(incomingItems.Length));
		}

		[Fact]
		public void Subscribe_CallsTransitionNextQueuedItemToPendingWhileNonNull()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, 20, scheduler);

			var queuedItems = Enumerable.Repeat(new Incoming() { Id = 1 }, 5)
				.Concat(Enumerable.Repeat(new Incoming() { Id = 2 }, 5))
				.Concat(new Incoming[] { null }).ToArray();

			monitor.Subscribe();

			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).ReturnsNextFromSequence(queuedItems);

			scheduler.AdvanceBy(monitor.PollingInterval.Add(TimeSpan.FromSeconds(1)));

			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).MustHaveHappened(Repeated.Exactly.Times(queuedItems.Length));
		}

		[Fact]
		public static void Subscribe_CallsTransitionNextQueuedItemCorrectNumberOfTimesOverMultipleElapsedIntervals()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, 20, scheduler);

			var queuedItems = Enumerable.Repeat(new Incoming() { Id = 1 }, 5)
				.Concat(Enumerable.Repeat(new Incoming() { Id = 2 }, 5))
				.Concat(new Incoming[] { null }).ToArray();

			monitor.Subscribe();

			//TODO: 7-6-2011-- minor bug here
			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).ReturnsNextFromSequence(queuedItems);

			scheduler.AdvanceBy(monitor.PollingInterval.Add(monitor.PollingInterval));

			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).MustHaveHappened(Repeated.Exactly.Times(queuedItems.Length + 1));
		}

		[Fact]
		public void Subscribe_PublishesExactSequenceOfItemsAsTheyArePulledFromDurableStorage()
		{
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, 20, scheduler);

			List<Incoming> incomingItems = new List<Incoming>();

			var queuedItems = Enumerable.Repeat(new Incoming() { Id = 1 }, 5)
				.Concat(Enumerable.Repeat(new Incoming() { Id = 5 }, 2))
				.Concat(Enumerable.Repeat(new Incoming() { Id = 2 }, 4))
				.Concat(Enumerable.Repeat(new Incoming() { Id = 3 }, 3))
				.ToArray();

			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).ReturnsNextFromSequence(queuedItems.Concat(new Incoming[] { null }).ToArray());
			monitor.Subscribe(publishedItem => { incomingItems.Add(publishedItem); });
			scheduler.AdvanceBy(monitor.PollingInterval.Add(TimeSpan.FromSeconds(1)));

			Assert.True(queuedItems.SequenceEqual(incomingItems, GenericEqualityComparer<Incoming>.ByAllMembers()));
		}

		[Fact]
		public void Subscribe_TakesNoMoreThanMaximumSpecifiedItemsPerInterval()
		{
			int maxToSlurp = 3;
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, maxToSlurp, scheduler);

			List<Incoming> incomingItems = new List<Incoming>();

			monitor.Subscribe(publishedItem => { incomingItems.Add(publishedItem); });
			scheduler.AdvanceBy(monitor.PollingInterval.Add(TimeSpan.FromSeconds(1)));

			Assert.Equal(maxToSlurp, incomingItems.Count);
		}

		[Fact]
		public void Subscribe_ResumesInProperQueuePositionAfterReadingMaximumDuringInterval()
		{
			int maxToSlurp = 3;
			var jobStorage = A.Fake<IDurableJobStorageQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobStorageMonitor<Incoming, Incoming>(jobStorage, maxToSlurp, scheduler);

			List<Incoming> incomingItems = new List<Incoming>();

			var queuedItems = Enumerable.Repeat(new Incoming() { Id = 1 }, 3)
				.Concat(new Incoming[] { new Incoming() { Id = 456 }, new Incoming() { Id = 222 }, new Incoming() { Id = 8714 } })
				.Concat(Enumerable.Repeat(new Incoming() { Id = 5 }, 2))
				.Concat(Enumerable.Repeat(new Incoming() { Id = 2 }, 4))
				.Concat(Enumerable.Repeat(new Incoming() { Id = 3 }, 3))
				.ToArray();

			A.CallTo(() => jobStorage.TransitionNextQueuedItemToPending()).ReturnsNextFromSequence(queuedItems);

			monitor.Subscribe(publishedItem => { incomingItems.Add(publishedItem); });
			scheduler.AdvanceBy(monitor.PollingInterval.Add(monitor.PollingInterval));

			Assert.True(queuedItems.Take(maxToSlurp * 2).SequenceEqual(incomingItems, GenericEqualityComparer<Incoming>.ByAllMembers()));
		}
	}
}