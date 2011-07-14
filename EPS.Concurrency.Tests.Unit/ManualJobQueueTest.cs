using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;
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


	public class IntJobQueueTest : ManualJobQueueTest<int, int>
	{
		public IntJobQueueTest()
			: base ((scheduler) => new ManualJobQueue<int, int>(scheduler))
		{ }


	}
}