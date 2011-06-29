using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class ManualJobQueueTest : 
		IJobQueueTest<ManualJobQueue>
	{
		public ManualJobQueueTest()
			: base (() => new ManualJobQueue())
		{ }
	}
}