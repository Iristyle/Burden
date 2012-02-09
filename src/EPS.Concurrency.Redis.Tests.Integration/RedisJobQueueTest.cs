using System;
using EPS.Concurrency.Tests.Unit;
using Xunit;

namespace EPS.Concurrency.Redis.Tests.Integration
{
	public class RedisValueTypeJobQueueTest
		: RedisJobQueueTest<int, int>
	{
		public RedisValueTypeJobQueueTest()
			: base(input => input * 2)
		{ }
	}

	public class RedisReferenceTypeJobQueueTest
		: RedisJobQueueTest<string, string>
	{
 		public RedisReferenceTypeJobQueueTest()
			: base(input => "fail : " + input)
		{ }	
	}

	public abstract class RedisJobQueueTest<TQueue, TQueuePoison>
		: DurableJobQueueTest<RedisJobQueue<TQueue, TQueuePoison>, TQueue, TQueuePoison>
	{
		protected RedisJobQueueTest(Func<TQueue, TQueuePoison> poisonConverter) :
			//HACK: 9-9-2011 -- the tests in DurableJobQueueTest should probably be fixed, because flushing the DB each time breaks 11 tests (so yeah, they could be written better)
			base(() => new RedisJobQueue<TQueue, TQueuePoison>(() => TestConnection.GetClientManager(false).GetClient(), QueueNames.Default), poisonConverter)
		{ }

		[Fact]
		public void Constructor_Throws_OnNullClientManager()
		{
			Assert.Throws<ArgumentNullException>(() => new RedisJobQueue<TQueue, TQueuePoison>(null, QueueNames.Default));
		}
		
		[Fact]
		public void Constructor_Throws_OnNullQueueNames()
		{
			Assert.Throws<ArgumentNullException>(() => new RedisJobQueue<TQueue, TQueuePoison>(() => TestConnection.GetClientManager().GetClient(), null));
		}
	}
}