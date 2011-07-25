using System;

namespace EPS.Concurrency
{
	/// <summary>	A class that wraps up a given Func as an IJobResultInspector.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	/// <typeparam name="TJobInput">   	Type of the job input. </typeparam>
	/// <typeparam name="TJobOutput">  	Type of the job output. </typeparam>
	/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
	internal class JobResultInspector<TJobInput, TJobOutput, TQueuePoison> 
		: IJobResultInspector<TJobInput, TJobOutput, TQueuePoison>
	{
		private readonly Func<JobResult<TJobInput, TJobOutput>, JobQueueAction<TQueuePoison>> inspector;
		
		/// <summary>	Constructor an instance of an IJobResultInspector given an inspector Func. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the inspector function is null. </exception>
		/// <param name="inspector">	The inspector. </param>
		public JobResultInspector(Func<JobResult<TJobInput, TJobOutput>, JobQueueAction<TQueuePoison>> inspector)
		{
			if (null == inspector) { throw new ArgumentNullException("inspector"); }
			this.inspector = inspector;
		}

		/// <summary>	Inspects. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the jobResult is null. </exception>
		/// <param name="jobResult">	The job result. </param>
		/// <returns>	Delegates the call to the user Func specified in the constructor. </returns>
		public JobQueueAction<TQueuePoison> Inspect(JobResult<TJobInput, TJobOutput> jobResult)
		{
			if (null == jobResult) { throw new ArgumentNullException("jobResult"); }
			return inspector(jobResult);
		}
	}

	/// <summary>	Job result inspector factory that creates a IJobResultInspector given a Func. </summary>
	/// <remarks>	7/24/2011. </remarks>
	public static class JobResultInspector
	{
		/// <summary>	Creates an IJobResultInspector given a Func.  Uses compiler inference to hide the details of using generics. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when one the inspector Func is null. </exception>
		/// <typeparam name="TJobInput">   	Type of the job input. </typeparam>
		/// <typeparam name="TJobOutput">  	Type of the job output. </typeparam>
		/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
		/// <param name="inspector">	The inspector Func. </param>
		/// <returns>	An IJobResultInspector given a simple Func, that can be used to manufacturer job poisons. </returns>
		public static IJobResultInspector<TJobInput, TJobOutput, TQueuePoison> From<TJobInput, TJobOutput, TQueuePoison>(Func<JobResult<TJobInput, TJobOutput>, JobQueueAction<TQueuePoison>> inspector)
		{
			if (null == inspector) { throw new ArgumentNullException("inspector"); }
			return new JobResultInspector<TJobInput, TJobOutput, TQueuePoison>(inspector);
		}
	}
}