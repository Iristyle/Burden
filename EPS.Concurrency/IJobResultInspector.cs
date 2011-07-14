using System;
using System.Reactive;

namespace EPS.Concurrency
{
	/// <summary>	Defines a standard mechanism for inspecting a job result and defining an action as a result. </summary>
	/// <remarks>	7/8/2011. </remarks>
	/// <typeparam name="TJobInput">	Type of the job input. </typeparam>
	/// <typeparam name="TJobInput">	Type of the job output. </typeparam>
	/// <typeparam name="TJobInput">	Type of a poisoned job. </typeparam>
	public interface IJobResultInspector<TJobInput, TJobOutput, TQueuePoison>
	{
		/// <summary>	Inspects an Rx Notification of JobResult, determining if the result should cause a queue poison or completion. </summary>
		/// <remarks> If the Notification is an error, the exception passed in should be of type </remarks>
		/// <param name="jobResult">	The job result. </param>
		/// <returns>	A JobQueueAction specifying what to do about the JobResult. </returns>
		/// <exception cref="ArgumentNullException">	Should throw on a null Notification. </exception>
		JobQueueAction<TQueuePoison> Inspect(Notification<JobResult<TJobInput, TJobOutput>> jobResult);
	}
}