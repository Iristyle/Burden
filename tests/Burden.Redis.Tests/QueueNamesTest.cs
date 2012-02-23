using System;
using Ploeh.AutoFixture;
using Xunit;

namespace EPS.Concurrency.Redis.Tests.Integration
{
	public class QueueNamesTest
	{
		private IFixture fixture = new Fixture();

		[Fact]
		public void Default_QueueNameIsExpected()
		{
			//this is a contract with how names are stored in Redis so shouldn't change
			Assert.Equal("request", QueueNames.Default.Request);
		}

		[Fact]
		public void Default_PendingNameIsExpected()
		{
			//this is a contract with how names are stored in Redis so shouldn't change
			Assert.Equal("pending", QueueNames.Default.Pending);
		}

		[Fact]
		public void Default_PoisonNameIsExpected()
		{
			//this is a contract with how names are stored in Redis so shouldn't change
			Assert.Equal("poison", QueueNames.Default.Poison);
		}

		[Fact]
		public void Constructor_Throws_OnNullRequestQueueName()
		{
			Assert.Throws<ArgumentNullException>(() => new QueueNames(null, fixture.CreateAnonymous<string>(), fixture.CreateAnonymous<
			string>()));
		}

		[Fact]
		public void Constructor_Throws_OnNullPendingQueueName()
		{
			Assert.Throws<ArgumentNullException>(() => new QueueNames(fixture.CreateAnonymous<string>(), null, fixture.CreateAnonymous<
			string>()));
		}

		[Fact]
		public void Constructor_Throws_OnNullPoisonQueueName()
		{
			Assert.Throws<ArgumentNullException>(() => new QueueNames(fixture.CreateAnonymous<string>(), fixture.CreateAnonymous<string>(), null));
		}

		[Fact]
		public void Constructor_ReturnsCorrectRequestQueueName()
		{
			var name = fixture.CreateAnonymous<string>();
			var names = new QueueNames(name, fixture.CreateAnonymous<string>(), fixture.CreateAnonymous<string>());
			Assert.Equal(name, names.Request);
		}

		[Fact]
		public void Constructor_ReturnsCorrectPendingQueueName()
		{
			var name = fixture.CreateAnonymous<string>();
			var names = new QueueNames(fixture.CreateAnonymous<string>(), name, fixture.CreateAnonymous<string>());
			Assert.Equal(name, names.Pending);
		}

		[Fact]
		public void Constructor_ReturnsCorrectPoisonQueueName()
		{
			var name = fixture.CreateAnonymous<string>();
			var names = new QueueNames(fixture.CreateAnonymous<string>(), fixture.CreateAnonymous<string>(), name);
			Assert.Equal(name, names.Poison);
		}
	}
}