using System;
using System.Reactive.Concurrency;

namespace EPS.Concurrency.Tests.Unit
{

	public abstract class ManualJobQueueTest<TJobInput, TJobOutput> :
		IJobQueueTest<ManualJobQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		public ManualJobQueueTest(Func<IScheduler, ManualJobQueue<TJobInput, TJobOutput>> jobQueueFactory)
			: base (jobQueueFactory)
		{ }
	}


	public class ValueTypeManualJobQueueTest : ManualJobQueueTest<int, int>
	{
		public ValueTypeManualJobQueueTest()
			: base ((scheduler) => new ManualJobQueue<int, int>(scheduler))
		{ }
	}

	public class ReferenceTypeManualJobQueueTest : ManualJobQueueTest<object, object>
	{
		public ReferenceTypeManualJobQueueTest()
			: base((scheduler) => new ManualJobQueue<object, object>(scheduler))
		{ }
	}
}