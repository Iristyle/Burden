using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using Xunit;

namespace Burden.Tests
{

	public abstract class ManualJobExecutionQueueTest<TJobInput, TJobOutput> :
		JobExecutionQueueTest<ManualJobExecutionQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs")]
		protected ManualJobExecutionQueueTest(Func<IScheduler, ManualJobExecutionQueue<TJobInput, TJobOutput>> jobQueueFactory)
			: base (jobQueueFactory)
		{ }

		[Fact]
		public void Constructor_Throws_OnMinimumThanAllowedConcurrentExceeded()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => { using (var m =  new ManualJobExecutionQueue<TJobInput, TJobOutput>(0)) { } });
		}

		[Fact]
		public void Constructor_Throws_OnMaxConcurrentExceeded()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => { using (var m = new ManualJobExecutionQueue<TJobInput, TJobOutput>(ManualJobExecutionQueue<TJobInput, TJobOutput>.MaxAllowedConcurrentJobs + 1)) { } });
		}
	}

	public class ManualJobExecutionQueueValueTypeTest : ManualJobExecutionQueueTest<int, int>
	{
		public ManualJobExecutionQueueValueTypeTest()
			: base((scheduler) => new ManualJobExecutionQueue<int, int>(scheduler, ManualJobExecutionQueue<int, int>.DefaultConcurrent))
		{ }
	}

	public class ManualJobExecutionQueueReferenceTypeTest : ManualJobExecutionQueueTest<object, object>
	{
		public ManualJobExecutionQueueReferenceTypeTest()
			: base((scheduler) => new ManualJobExecutionQueue<object, object>(scheduler, ManualJobExecutionQueue<object, object>.DefaultConcurrent))
		{ }
	}
}