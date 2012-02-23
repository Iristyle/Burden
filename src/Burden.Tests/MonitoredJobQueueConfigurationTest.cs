using System;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit;
using Xunit;
using Xunit.Extensions;

namespace Burden.Tests
{
	public class MonitoredJobQueueConfigurationTest
	{
		class CustomFixture : Fixture
		{
			public CustomFixture()
			{
				var random = new Random();
				this.Customize<int>(c => c.FromFactory(() => random.Next(5, 50)));
				this.Customize<TimeSpan>(c => c.FromFactory(() => new TimeSpan(0, 0, random.Next(5, 5000))));
			}
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void MaxConcurrentJobs_ReturnsConstructorValue(int maxConcurrent)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent);

			Assert.Equal(maxConcurrent, config.MaxConcurrentJobsToExecute);
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void MaxConcurrentJobs_ReturnsConstructorValue_WithIntervalOverload(int maxConcurrent, TimeSpan pollingInterval)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent, pollingInterval);

			Assert.Equal(maxConcurrent, config.MaxConcurrentJobsToExecute);
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void MaxConcurrentJobs_ReturnsConstructorValue_WithIntervalAndMaxToReadOverload(int maxConcurrent, TimeSpan pollingInterval, int maxQueueItemsToPublish)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent, pollingInterval, maxQueueItemsToPublish);

			Assert.Equal(maxConcurrent, config.MaxConcurrentJobsToExecute);
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void PollingInterval_ReturnsDefaultPollingInterval(int maxConcurrent)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent);

			Assert.Equal(DurableJobQueueMonitor.DefaultPollingInterval, config.PollingInterval);
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void PollingInterval_ReturnsConstructorValue_WithIntervalOverload(int maxConcurrent, TimeSpan pollingInterval)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent, pollingInterval);

			Assert.Equal(pollingInterval, config.PollingInterval);
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void PollingInterval_ReturnsConstructorValue_WithIntervalAndMaxToReadOverload(int maxConcurrent, TimeSpan pollingInterval, int maxQueueItemsToPublish)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent, pollingInterval, maxQueueItemsToPublish);

			Assert.Equal(pollingInterval, config.PollingInterval);
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void MaxQueueItemsToPublishPerInterval_ReturnsConstructorValue(int maxConcurrent)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent);
			var expected = config.PollingInterval.TotalSeconds * config.MaxConcurrentJobsToExecute * 2;
			Assert.InRange(config.MaxQueueItemsToPublishPerInterval, expected, 2 * expected);
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void MaxQueueItemsToPublishPerInterval_ReturnsConstructorValue_WithIntervalOverload(int maxConcurrent, TimeSpan pollingInterval)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent, pollingInterval);
			var expected = config.PollingInterval.TotalSeconds * config.MaxConcurrentJobsToExecute * 2;
			Assert.InRange(config.MaxQueueItemsToPublishPerInterval, expected, 2 * expected);
		}

		[Theory]
		[AutoData(typeof(CustomFixture))]
		public void MaxQueueItemsToPublishPerInterval_ReturnsConstructorValue_WithIntervalAndMaxToReadOverload(int maxConcurrent, TimeSpan pollingInterval, int maxQueueItemsToPublish)
		{
			var config = new MonitoredJobQueueConfiguration(maxConcurrent, pollingInterval, maxQueueItemsToPublish);

			Assert.Equal(maxQueueItemsToPublish, config.MaxQueueItemsToPublishPerInterval);
		}
	}
}