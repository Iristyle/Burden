using System;

namespace EPS.Concurrency
{
	/// <summary>	Defines a standard mechanism for inspecting a job result and defining an action as a result. </summary>
	/// <remarks>	7/8/2011. </remarks>
	/// <typeparam name="TJobInput">	Type of the job input. </typeparam>
	/// <typeparam name="TJobInput">	Type of the job output. </typeparam>
	/// <typeparam name="TJobInput">	Type of a poisoned job, should it be deemed a failure. </typeparam>
	public interface IJobResultInspector<TJobInput, TJobOutput, TQueuePoison>
	{
		/// <summary>	Inspects an Rx Notification of JobResult, determining if the result should cause a queue poison or completion. </summary>
		/// <param name="jobResult">	The job result. </param>
		/// <returns>	A JobQueueAction specifying what to do about the JobResult. </returns>
		/// <exception cref="ArgumentNullException">	Should throw on a null JobResult. </exception>
		JobQueueAction<TQueuePoison> Inspect(JobResult<TJobInput, TJobOutput> jobResult);
	}
}