using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;

namespace Burden
{
	/// <summary>	A very simple interface for adding items to a monitored job queue.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	/// <typeparam name="TJobInput">	The type of job input. </typeparam>
	/// <typeparam name="TJobOutput">	The type of job output. </typeparam>
	/// <typeparam name="TPoison">	The type of a poisoned job input. </typeparam>
	[SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "The heavy use of generics is mitigated by numerous static helpers that use compiler inference")]
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
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and IObservables")]
		IObservable<JobResult<TJobInput, TJobOutput>> OnJobCompletion { get; }

		/// <summary>	Gets notifications as items are moved around the durable queue. </summary>
		/// <value>	A sequence of observable durable queue notifications. </value>
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and IObservables")]
		IObservable<DurableJobQueueAction<TJobInput, TPoison>> OnQueueAction { get; }

		/// <summary>	Cancel queued jobs and wait for executing jobs to complete. </summary>
		/// <remarks>	This should automatically be called upon Dispose by classes implementing this interface. </remarks>
		/// <param name="timeout">	The time span to block until terminating. </param>
		void CancelQueuedAndWaitForExecutingJobsToComplete(TimeSpan timeout);

		/// <summary>	Gets the number of running jobs from the job execution queue. </summary>
		/// <value>	The number of running jobs. </value>
		int RunningCount { get; }

		/// <summary>	Gets the number of queued jobs from the job execution queue. </summary>
		/// <value>	The number of queued jobs. </value>
		int QueuedCount { get; }

		/// <summary>	Gets the maximum items to publish per interval for this instance. </summary>
		/// <value>	The maximum queue items to publish per interval for this instance. </value>
		int MaxQueueItemsToPublishPerInterval { get; }

		/// <summary>	Gets or sets the maximum number of concurrent jobs allowed to execute for this queue. </summary>
		/// <remarks>
		/// Implementers should not throw an exception if the max is set too high, but rather assume the maximum allowable value.
		/// </remarks>
		/// <value>	The maximum allowed concurrent jobs. </value>
		int MaxConcurrent { get; set; }

		/// <summary>	Gets the polling interval. </summary>
		/// <value>	The polling interval. </value>
		TimeSpan PollingInterval { get; }

		/// <summary>	Gets the scheduler being used for this queue. </summary>
		/// <value>	The scheduler. </value>
		IScheduler Scheduler { get; }
		//TODO: 7-26-2011 -- consider surfacing additional observables from a wrapped IJobResultInspector
	}
}