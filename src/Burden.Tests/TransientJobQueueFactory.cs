using System;
using EqualityComparer;

namespace Burden.Tests
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