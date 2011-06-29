using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPS.Concurrency.Tests.Unit
{
	public class AutoJobQueueTest :
		IJobQueueTest<AutoJobQueue>
	{
		public AutoJobQueueTest()
			: base(() => new AutoJobQueue(200))
		{ }
	}

}