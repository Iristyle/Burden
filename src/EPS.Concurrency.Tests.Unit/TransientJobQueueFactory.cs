using System;
using EPS.Utility;

namespace EPS.Concurrency.Tests.Unit
{
	public class TransientJobQueueFactory
		: IDurableJobQueueFactory
	{
		public IDurableJobQueue<TQueueInput, TQueuePoison> CreateDurableJobQueue<TQueueInput, TQueuePoison>()
		{
			return new TransientJobQueue<TQueueInput, TQueuePoison>(GenericEqualityComparer<TQueueInput>.ByAllMembers(), GenericEqualityComparer<TQueuePoison>.ByAllMembers());
		}
	}
}