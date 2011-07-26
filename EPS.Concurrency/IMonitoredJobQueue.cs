using System;

namespace EPS.Concurrency
{
	/// <summary>	A very simple interface for adding items to a monitored job queue.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	/// <typeparam name="T">	Generic type parameter. </typeparam>
	public interface IMonitoredJobQueue<T>
		: IDisposable
	{
		/// <summary>	Adds a job.  </summary>
		/// <param name="input">	The input. </param>
		void AddJob(T input);
		

		/// <summary>	Cancel queued jobs and wait for executing jobs to complete. </summary>
		/// <remarks>	This should automatically be called upon Dispose. </remarks>
		void CancelQueuedAndWaitForExecutingJobsToComplete();
	}
}