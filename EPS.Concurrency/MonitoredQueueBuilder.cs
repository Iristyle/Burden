using System;

namespace EPS.Concurrency
{
	/// <summary>	A simple durable job queue manager class that simplifies the details of using a durable queue that is monitored. </summary>
	/// <remarks>	7/24/2011. </remarks>
	public class MonitoredQueueBuilder
	{
		private readonly IDurableJobQueueFactory factory;

		/// <summary>	Constructor. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when one or more required arguments are null. </exception>
		/// <param name="factory">	The factory. </param>
		public MonitoredQueueBuilder(IDurableJobQueueFactory factory)
		{
			if (null == factory) { throw new ArgumentNullException("factory"); }
			this.factory = factory;
		}
		//TODO: 7-23-2011 - it would be nice to eliminate the need for a factory, using something like this
		//Expression<Func<object, object, IDurableJobQueue<TInput, TPoison>>> factory

		/// <summary>	Creates a monitored queue given a function. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the action is null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <param name="jobAction">	The job action. </param>
		/// <returns>
		/// A new IMonitoredJobQueue instance corresponding to the given action, where poisoned items are stored in the durable queue simply as
		/// input plus Exception - ie Poison{TInput}.  Call basically delegates to MonitoredJobQueue.Create.
		/// </returns>
		public IMonitoredJobQueue<TInput, TOutput, Poison<TInput>> CreateMonitoredQueue<TInput, TOutput>(Func<TInput, TOutput> jobAction)
		{
			if (null == jobAction) { throw new ArgumentNullException("jobAction"); }

			return MonitoredJobQueue.Create(factory, jobAction);
		}

		/// <summary>	Creates a monitored queue given a function and a result inspector. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the action or result inspectors are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="jobAction">	   	The action to be performed as the job. </param>
		/// <param name="resultsInspector">	The results inspector. </param>
		/// <returns>
		/// A new IMonitoredJobQueue instance corresponding to the given action, where the type of poisoned input is automatically inferred based
		/// on the given resultsInspector.Call basically delegates to MonitoredJobQueue.Create.
		/// </returns>
		public IMonitoredJobQueue<TInput, TOutput, TPoison> CreateMonitoredQueue<TInput, TOutput, TPoison>(Func<TInput, TOutput> jobAction, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector)
		{
			if (null == jobAction) { throw new ArgumentNullException("jobAction"); }
			if (null == resultsInspector) { throw new ArgumentNullException("resultsInspector"); }

			return MonitoredJobQueue.Create(factory, jobAction, resultsInspector);
		}
	}
}