using System;
using System.Linq.Expressions;

namespace EPS.Concurrency
{
	//TODO: 7-27-2011 -- these are some test concepts / ideas not ready for public consumption
	//TODO: 7-27-2011 -- the idea would be alleviate the need for a real IDurableJobQueueFactory, but to instead provide an Expression that describes what it looks like to create one
	internal static class DurableQueueFactory
	{
		private class DurableQueueFactoryExpressionWrapper<TInput, TPoison>
			: IDurableJobQueueFactory
		{
			private Expression<Func<object, object, IDurableJobQueue<object, object>>> builder;

			/// <summary>
			/// Initializes a new instance of the DurableQueueFactoryExpressionWrapper class.
			/// </summary>
			/// <param name="builder"></param>
			public DurableQueueFactoryExpressionWrapper(Expression<Func<object, object, IDurableJobQueue<object, object>>> builder)
			{
				this.builder = builder;
			}

			public IDurableJobQueue<TInput, TPoison> CreateDurableJobQueue<TInput, TPoison>()
			{
				throw new NotImplementedException();
			}
		}

		//not sure that this is even possible (or necessary)
		public static IDurableJobQueueFactory From<object, object>(Expression<Func<object, object, IDurableJobQueue<object, object>>> builder)
		{
			return new DurableQueueFactoryExpressionWrapper<object, object>(builder);
		}
	}
}