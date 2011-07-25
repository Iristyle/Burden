using System;

namespace EPS.Concurrency
{
	/// <summary>	Simple interface that defines how to construct an IDurableJobQueue given input and poison types.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	public interface IDurableJobQueueFactory
	{
		/// <summary>	Creates the durable job queue. </summary>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <returns>	A new instance of an IDurableJobQueue. </returns>
		IDurableJobQueue<TInput, TPoison> CreateDurableJobQueue<TInput, TPoison>();
	}
}