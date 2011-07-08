using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class ManualJobQueueTest<TJobInput, TJobOutput> :
		IJobQueueTest<ManualJobQueue<TJobInput, TJobOutput>, TJobInput, TJobOutput>
	{
		public ManualJobQueueTest(Func<ManualJobQueue<TJobInput, TJobOutput>> jobQueueFactory)
			: base (jobQueueFactory)
		{ }
	}
}