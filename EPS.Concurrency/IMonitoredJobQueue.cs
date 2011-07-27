using System;

namespace EPS.Concurrency
{
	/// <summary>	A very simple interface for adding items to a monitored job queue.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	/// <typeparam name="T">	Generic type parameter. </typeparam>
	public interface IMonitoredJobQueue<TJobInput, TJobOutput, TPoison>
		: IDisposable
	{
		/// <summary>	Adds a job.  </summary>
		/// <param name="input">	The input. </param>
		void AddJob(TJobInput input);

		/// <summary>
		/// The Observable that monitors job completion, where completion can be either run to completion, exception or cancellation from the job
		/// execution queue.
		/// </summary>
		/// <value>	A sequence of observable job completion notifications from the job execution queue. </value>
		IObservable<JobResult<TJobInput, TJobOutput>> OnJobCompletion { get; }

		/// <summary>	Gets notifications as items are moved around the durable queue. </summary>
		/// <value>	A sequence of observable durable queue notifications. </value>
		IObservable<DurableJobQueueAction<TJobInput, TPoison>> OnQueueAction { get; }

		/// <summary>	Cancel queued jobs and wait for executing jobs to complete. </summary>
		/// <remarks>	This should automatically be called upon Dispose by classes implementing this interface. </remarks>
		/// <param name="timeSpan">	The time span to block until terminating. </param>
		void CancelQueuedAndWaitForExecutingJobsToComplete(TimeSpan timeout);

		/// <summary>	Gets the number of running jobs from the job execution queue. </summary>
		/// <value>	The number of running jobs. </value>
		int RunningCount { get; }

		/// <summary>	Gets the number of queued jobs from the job execution queue. </summary>
		/// <value>	The number of queued jobs. </value>
		int QueuedCount { get; }

		/// <summary>	Gets the maximum allowable queue items to publish per interval, presently 50000. </summary>
		/// <value>	The maximum allowable queue items to publish per interval, presently 50000. </value>
		int MaxAllowedQueueItemsToPublishPerInterval { get; }

		/// <summary>	Gets the polling interval. </summary>
		/// <value>	The polling interval. </value>
		TimeSpan PollingInterval { get; }

		//TODO: 7-26-2011 -- consider surfacing additional observables from a wrapped IJobResultInspector
	}
}