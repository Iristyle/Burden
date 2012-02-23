using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using EqualityComparer;
using FakeItEasy;
using Ploeh.AutoFixture;
using Xunit;
using Xunit.Extensions;

namespace EPS.Concurrency.Tests.Unit
{
	public class DurableJobQueueMonitorTest
	{
		private Fixture fixture = new Fixture();

		public class Incoming
		{
			public int Id { get; set; }
		}

		[Fact]
		public void Constructor_ThrowsOnNullScheduler()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			Assert.Throws<ArgumentNullException>(() => new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 500, DurableJobQueueMonitor.DefaultPollingInterval, null));
		}

		[Fact]
		public void Constructor_ThrowsOnNullDurableStorage()
		{
			Assert.Throws<ArgumentNullException>(() => new DurableJobQueueMonitor<Incoming, Incoming>(null, 500, DurableJobQueueMonitor.DefaultPollingInterval, 
A.Dummy<IScheduler>()));
		}

		[Fact]
		public void Create_ThrowsOnNullJobQueue()
		{
			Assert.Throws<ArgumentNullException>(() => DurableJobQueueMonitor.Create(null as IDurableJobQueue<Incoming, Incoming>, 500, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Constructor_ThrowsOnLargerThanAllowedMaximumQueueSize()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			Assert.Throws<ArgumentOutOfRangeException>(() => new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 
DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1, DurableJobQueueMonitor.DefaultPollingInterval, 
A.Dummy<IScheduler>()));
		}

		[Fact]
		public void Create_ThrowsOnLargerThanAllowedMaximumQueueSize()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => DurableJobQueueMonitor.Create(A.Fake<IDurableJobQueue<Incoming, Incoming>>(), DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1));
		}

		[Fact]
		public void Create_ThrowsOnLargerThanAllowedMaximumQueueSizeWithSpecifiedPollingInterval()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => DurableJobQueueMonitor.Create(A.Fake<IDurableJobQueue<Incoming, Incoming>>(), DurableJobQueueMonitor.MaxAllowedQueueItemsToPublishPerInterval + 1, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Constructor_ThrowsOnSmallerThanMinimumAllowedQueueSize()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			Assert.Throws<ArgumentOutOfRangeException>(() => new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 0, DurableJobQueueMonitor.DefaultPollingInterval, 
A.Dummy<IScheduler>()));
		}

		[Fact]
		public void Create_ThrowsOnSmallerThanMinimumAllowedQueueSize()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => DurableJobQueueMonitor.Create(A.Fake<IDurableJobQueue<Incoming, Incoming>>(), 0));
		}

		[Fact]
		public void Create_ThrowsOnSmallerThanMinimumAllowedQueueSizeWithSpecifiedPollingInterval()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => DurableJobQueueMonitor.Create(A.Fake<IDurableJobQueue<Incoming, Incoming>>(), 0, DurableJobQueueMonitor.DefaultPollingInterval));
		}

		[Fact]
		public void Constructor_ThrowsOnLargerThanAllowedMaximumPollingInterval()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			Assert.Throws<ArgumentOutOfRangeException>(() => new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 500, 
DurableJobQueueMonitor.MaximumAllowedPollingInterval + TimeSpan.FromSeconds(1), A.Dummy<IScheduler>()));
		}

		[Fact]
		public void Create_ThrowsOnLargerThanAllowedMaximumPollingInterval()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => DurableJobQueueMonitor.Create(A.Fake<IDurableJobQueue<Incoming, Incoming>>(), 500, DurableJobQueueMonitor.MaximumAllowedPollingInterval + TimeSpan.FromSeconds(1)));
		}

		[Fact]
		public void Constructor_ThrowsOnSmallerThanMinimumAllowedPollingInterval()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			Assert.Throws<ArgumentOutOfRangeException>(() => new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 500, 
DurableJobQueueMonitor.MinimumAllowedPollingInterval - TimeSpan.FromTicks(1), A.Dummy<IScheduler>()));
		}

		[Fact]
		public void Create_ThrowsOnSmallerThanMinimumAllowedPollingInterval()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => DurableJobQueueMonitor.Create(A.Fake<IDurableJobQueue<Incoming, Incoming>>(), 500, DurableJobQueueMonitor.MinimumAllowedPollingInterval - TimeSpan.FromTicks(1)));
		}

		[Fact]
		public void MaxQueueItemsToPublishPerInterval_MatchesCreationValue()
		{
			var maxItems = fixture.CreateAnonymous<int>();
			var monitor = DurableJobQueueMonitor.Create(A.Fake<IDurableJobQueue<Incoming, Incoming>>(), maxItems);

			Assert.Equal(maxItems, monitor.MaxQueueItemsToPublishPerInterval);
		}

		[Fact]
		public void Constructor_ResetsPendingToQueued()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var monitor = DurableJobQueueMonitor.Create(jobStorage, 500);

			A.CallTo(() => jobStorage.ResetAllPendingToQueued()).MustHaveHappened(Repeated.Exactly.Once);
		}

		[Theory]
		[InlineData(25)]
		[InlineData(1)]
		public void Monitor_PollsOnSpecifiedInterval(int minutes)
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var interval = TimeSpan.FromMinutes(minutes);
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 20, interval, scheduler);
			
			using (var subscription = monitor.Subscribe())
			{
				A.CallTo(() => jobStorage.NextQueuedItem()).Returns(Item.From(new Incoming() { Id = 1 }));

				//this is a little hacky, but give ourselves a 2 second timespan to make the call against our fake
				scheduler.AdvanceBy(interval - TimeSpan.FromSeconds(2));
				A.CallTo(() => jobStorage.NextQueuedItem()).MustNotHaveHappened();
			}
		}

		[Fact]
		public void Monitor_DoesNotPumpQueueWithZeroSubscribers()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 20, DurableJobQueueMonitor.DefaultPollingInterval, scheduler);

			A.CallTo(() => jobStorage.NextQueuedItem()).Returns(Item.From(new Incoming() { Id = 1 }));

			scheduler.AdvanceBy(monitor.PollingInterval.Add(monitor.PollingInterval));

			A.CallTo(() => jobStorage.NextQueuedItem()).MustNotHaveHappened();
		}

		[Fact]
		public void Monitor_AwaitsFirstSubscriberBeforePublishing()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 20, DurableJobQueueMonitor.DefaultPollingInterval, scheduler);

			//X items + a null terminator per each elapsed interval
			var incomingItems = Enumerable.Repeat(Item.From(new Incoming() { Id = 2 }), 5)
				.Concat(new[] { Item.None<Incoming>(), Item.None<Incoming>() }).ToArray();

			A.CallTo(() => jobStorage.NextQueuedItem()).ReturnsNextFromSequence(incomingItems);

			//advance the amount of time that would normally cause our queue to be totally flushed
			scheduler.AdvanceBy(monitor.PollingInterval.Add(monitor.PollingInterval));
			using (var subscription = monitor.Subscribe())
			{
				//and now that we have a subscriber, elapse enough time to publish the queue contents
				scheduler.AdvanceBy(monitor.PollingInterval);
				scheduler.AdvanceBy(monitor.PollingInterval);

				//our transition method should have been called X times based on the list of valid items + 1 time for the null on the first scheduled execution, then an additional time w/ null for the next scheduled execution
				A.CallTo(() => jobStorage.NextQueuedItem()).MustHaveHappened(Repeated.Exactly.Times(incomingItems.Length));
			}
		}

		[Fact]
		public void Subscribe_CallsNextQueuedItemWhileNonNull()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 20, DurableJobQueueMonitor.DefaultPollingInterval, scheduler);

			var queuedItems = Enumerable.Repeat(Item.From(new Incoming() { Id = 1 }), 5)
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 2 }), 5))
				.Concat(new [] { Item.None<Incoming>() }).ToArray();

			using (var subscription = monitor.Subscribe())
			{
				A.CallTo(() => jobStorage.NextQueuedItem()).ReturnsNextFromSequence(queuedItems);

				scheduler.AdvanceBy(monitor.PollingInterval);

				A.CallTo(() => jobStorage.NextQueuedItem()).MustHaveHappened(Repeated.Exactly.Times(queuedItems.Length));
			}
		}

		[Fact]
		public static void Subscribe_CallsTransitionNextQueuedItemCorrectNumberOfTimesOverMultipleElapsedIntervals()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 20, DurableJobQueueMonitor.DefaultPollingInterval, scheduler);

			var queuedItems = Enumerable.Repeat(Item.From(new Incoming() { Id = 1 }), 5)
				.Concat(new [] { Item.None<Incoming>() })
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 2 }), 5))
				.Concat(new [] { Item.None<Incoming>() }).ToArray();

			using (var subscription = monitor.Subscribe())
			{
				A.CallTo(() => jobStorage.NextQueuedItem()).ReturnsNextFromSequence(queuedItems);

				scheduler.AdvanceBy(monitor.PollingInterval);
				scheduler.AdvanceBy(monitor.PollingInterval);

				A.CallTo(() => jobStorage.NextQueuedItem()).MustHaveHappened(Repeated.Exactly.Times(queuedItems.Length));
			}
		}

		[Fact]
		public void Subscribe_PublishesExactSequenceOfItemsAsTheyArePulledFromDurableStorage()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 20, DurableJobQueueMonitor.DefaultPollingInterval, scheduler);

			List<Incoming> incomingItems = new List<Incoming>();

			var queuedItems = Enumerable.Repeat(Item.From(new Incoming() { Id = 1 }), 5)
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 5 }), 2))
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 2 }), 4))
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 3 }), 3))
				.ToArray();

			A.CallTo(() => jobStorage.NextQueuedItem()).ReturnsNextFromSequence(queuedItems.Concat(new [] { Item.None<Incoming>() }).ToArray());

			using (var subscription = monitor.Subscribe(publishedItem =>{ incomingItems.Add(publishedItem);}))
			{
				scheduler.AdvanceBy(monitor.PollingInterval);

				Assert.True(queuedItems.Select(item => item.Value).SequenceEqual(incomingItems, GenericEqualityComparer<Incoming>.ByAllMembers()));
			}
		}

		[Fact]
		public static void Subscribe_PublishesExactSequenceOfItemsOverMultipleElapsedIntervals()
		{
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, 20, DurableJobQueueMonitor.DefaultPollingInterval, scheduler);

			List<Incoming> incomingItems = new List<Incoming>();

			var queuedItems = Enumerable.Repeat(Item.From(new Incoming() { Id = 1 }), 5)
				.Concat(new [] { Item.None<Incoming>() })
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 2 }), 5))
				.Concat(new [] { Item.None<Incoming>() })
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 1 }), 2))
				.Concat(new [] { Item.None<Incoming>() }).ToArray();

			using (var subscription = monitor.Subscribe(publishedItem =>{ incomingItems.Add(publishedItem);}))
			{
				A.CallTo(() => jobStorage.NextQueuedItem()).ReturnsNextFromSequence(queuedItems);

				scheduler.AdvanceBy(monitor.PollingInterval);
				scheduler.AdvanceBy(monitor.PollingInterval);
				scheduler.AdvanceBy(monitor.PollingInterval);

				Assert.True(queuedItems.Where(item => item.Success)
				.Select(item => item.Value)
				.SequenceEqual(incomingItems, GenericEqualityComparer<Incoming>.ByAllMembers()));
			}
		}

		[Fact]
		public void Subscribe_TakesNoMoreThanMaximumSpecifiedItemsPerInterval()
		{
			int maxToSlurp = 3;
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			A.CallTo(() => jobStorage.NextQueuedItem()).Returns(Item.From(new Incoming() { Id = 1 }));
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, maxToSlurp, DurableJobQueueMonitor.DefaultPollingInterval, 
scheduler);
			var completion = new ManualResetEventSlim(false);

			List<Incoming> incomingItems = new List<Incoming>();

			using (var subscription = monitor.Subscribe(publishedItem =>
			{
				incomingItems.Add(publishedItem);
				if (incomingItems.Count >= maxToSlurp)
				{
					completion.Set();
				}
			}))
			{
				scheduler.AdvanceBy(monitor.PollingInterval + TimeSpan.FromTicks(1));
				completion.Wait(TimeSpan.FromSeconds(3));

				Assert.Equal(maxToSlurp, incomingItems.Count);
			}
		}

		[Fact]
		public void Subscribe_ResumesInProperQueuePositionAfterReadingMaximumDuringInterval()
		{
			int maxToSlurp = 3;
			var jobStorage = A.Fake<IDurableJobQueue<Incoming, Incoming>>();
			var scheduler = new HistoricalScheduler();
			var monitor = new DurableJobQueueMonitor<Incoming, Incoming>(jobStorage, maxToSlurp, DurableJobQueueMonitor.DefaultPollingInterval, 
scheduler);

			List<Incoming> incomingItems = new List<Incoming>();

			var queuedItems = Enumerable.Repeat(Item.From(new Incoming() { Id = 1 }), 3)
				.Concat(new [] 
				{ 
					Item.From(new Incoming() { Id = 456 }), 
					Item.From(new Incoming() { Id = 222 }), 
					Item.From(new Incoming() { Id = 8714 }) 
				})
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 5 }), 2))
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 2 }), 4))
				.Concat(Enumerable.Repeat(Item.From(new Incoming() { Id = 3 }), 3))
				.ToArray();

			A.CallTo(() => jobStorage.NextQueuedItem()).ReturnsNextFromSequence(queuedItems);

			using (var subscription = monitor.Subscribe(publishedItem =>{ incomingItems.Add(publishedItem);}))
			{
				scheduler.AdvanceBy(monitor.PollingInterval);
				scheduler.AdvanceBy(monitor.PollingInterval);

				Assert.True(queuedItems.Take(maxToSlurp * 2)
					.Select(item => item.Value)
					.SequenceEqual(incomingItems, GenericEqualityComparer<Incoming>.ByAllMembers()));
			}
		}
	}
}