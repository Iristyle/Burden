using System;
using System.Reactive.Concurrency;

namespace EPS.Concurrency.Tests.Unit
{

	public abstract class ManualJobExecutionQueueTest<TJobInput, TJobOutput> :
		IJobExecutionQueueTest<ManualJobExecutionQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		public ManualJobExecutionQueueTest(Func<IScheduler, ManualJobExecutionQueue<TJobInput, TJobOutput>> jobQueueFactory)
			: base (jobQueueFactory)
		{ }
	}


	public class ValueTypeManualJobExecutionQueueTest : ManualJobExecutionQueueTest<int, int>
	{
		public ValueTypeManualJobExecutionQueueTest()
			: base ((scheduler) => new ManualJobExecutionQueue<int, int>(scheduler))
		{ }
	}

	public class ReferenceTypeManualJobExecutionQueueTest : ManualJobExecutionQueueTest<object, object>
	{
		public ReferenceTypeManualJobExecutionQueueTest()
			: base((scheduler) => new ManualJobExecutionQueue<object, object>(scheduler))
		{ }
	}
}