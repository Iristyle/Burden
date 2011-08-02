using System;
using System.Reactive.Concurrency;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{

	public abstract class ManualJobExecutionQueueTest<TJobInput, TJobOutput> :
		IJobExecutionQueueTest<ManualJobExecutionQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		public ManualJobExecutionQueueTest(Func<IScheduler, ManualJobExecutionQueue<TJobInput, TJobOutput>> jobQueueFactory)
			: base (jobQueueFactory)
		{ }

		[Fact]
		public void Constructor_Throws_OnMinimumThanAllowedConcurrentExceeded()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new ManualJobExecutionQueue<TJobInput, TJobOutput>(0));
		}

		[Fact]
		public void Constructor_Throws_OnMaxConcurrentExceeded()
		{
			var instance = new ManualJobExecutionQueue<TJobInput, TJobOutput>();
			Assert.Throws<ArgumentOutOfRangeException>(() => new ManualJobExecutionQueue<TJobInput, TJobOutput>(ManualJobExecutionQueue<TJobInput, TJobOutput>.MaxAllowedConcurrentJobs + 1));
		}
	}

	public class ValueTypeManualJobExecutionQueueTest : ManualJobExecutionQueueTest<int, int>
	{
		public ValueTypeManualJobExecutionQueueTest()
			: base((scheduler) => new ManualJobExecutionQueue<int, int>(scheduler, ManualJobExecutionQueue<int, int>.DefaultConcurrent))
		{ }
	}

	public class ReferenceTypeManualJobExecutionQueueTest : ManualJobExecutionQueueTest<object, object>
	{
		public ReferenceTypeManualJobExecutionQueueTest()
			: base((scheduler) => new ManualJobExecutionQueue<object, object>(scheduler, ManualJobExecutionQueue<object, object>.DefaultConcurrent))
		{ }
	}
}