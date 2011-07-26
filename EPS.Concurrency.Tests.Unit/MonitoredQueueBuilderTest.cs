using System;
using FakeItEasy;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class MonitoredQueueBuilderTest
	{
		[Fact]
		public void Constructor_Throws_OnNullDurableStorageFactory()
		{
			Assert.Throws<ArgumentNullException>(() => new MonitoredQueueBuilder(null));
		}

		[Fact]
		public void CreateMonitoredQueue_Throws_OnNullAction()
		{
			var factory = A.Fake<IDurableJobQueueFactory>();
			var manager = new MonitoredQueueBuilder(factory);

			Assert.Throws<ArgumentNullException>(() => manager.CreateMonitoredQueue(null as Func<int, int>));
		}

		[Fact]
		public void CreateMonitoredQueue_Throws_OnNullInspector()
		{
			var factory = A.Fake<IDurableJobQueueFactory>();
			var manager = new MonitoredQueueBuilder(factory);

			Assert.Throws<ArgumentNullException>(() => manager.CreateMonitoredQueue((int i) => 3, null as Func<JobResult<int, int>, JobQueueAction<Poison<int>>>));
		}

		[Fact]
		public void CreateMonitoredQueue_Calls_DurableStorageFactory_UsingPoisonT_WhenNoInspectorSpecified()
		{
			var factory = A.Fake<IDurableJobQueueFactory>();
			var manager = new MonitoredQueueBuilder(factory);

			manager.CreateMonitoredQueue((int i) => 3);

			A.CallTo(() => factory.CreateDurableJobQueue<int, Poison<int>>()).MustHaveHappened(Repeated.Exactly.Once);
		}

		[Fact]
		public void CreateMonitoredQueue_Calls_DurableStorageFactory_MultipleTimes_ForMultipleFuncsOfSameType()
		{
			//for now, we expect every func to return a new monitored queue-- no caching of queues, etc
			var factory = A.Fake<IDurableJobQueueFactory>();
			var manager = new MonitoredQueueBuilder(factory);

			int count = 10;
			for (int i = 0; i < count; i++)
			{
				manager.CreateMonitoredQueue((int j) => i);
			}

			A.CallTo(() => factory.CreateDurableJobQueue<int, Poison<int>>()).MustHaveHappened(Repeated.Exactly.Times(count));
		}

		[Fact]
		public void CreateMonitoredQueue_DoesNotReturnsSameMonitoredQueue_GivenSameFunc()
		{
			//for now, we expect every func to return a new monitored queue-- no caching of queues, etc
			var factory = A.Fake<IDurableJobQueueFactory>();
			var manager = new MonitoredQueueBuilder(factory);

			Func<int, int> func = (int i) => i;
			var queue1 = manager.CreateMonitoredQueue(func);
			var queue2 = manager.CreateMonitoredQueue(func);

			Assert.NotSame(queue1, queue2);
		}
	}
}