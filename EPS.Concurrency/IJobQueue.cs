using System;
using System.Reactive;

namespace EPS.Concurrency
{	
	/// <summary>
	/// Interface for a job queue, where each job is expected to have a discrete input and output.  Use the System.Reactive.Unit type to
	/// specify no input type, no output type, or both. Code originally based off of <a href="http://rxpowertoys.codeplex.com/" />, but
	/// modified heavily to move away from a fire and forget scenario to a scenario where the jobs are allowed to produce values, and 
	/// where job errors are tied to the jobs input (ultimately for matching up to a more durable queue).
	/// </summary>
	/// <remarks>	7/13/2011. </remarks>
	/// <typeparam name="TJobInput">		Type of the job input. Use <see cref="System.Reactive.Unit"/> for no input. </typeparam>
	/// <typeparam name="TJobInput">	Type of the job output. Use <see cref="System.Reactive.Unit"/> for no output. </typeparam>
	public interface IJobQueue<TJobInput, TJobOutput>
	{
		/// <summary>
		/// The Observable that monitors job completion, where completion can be either run to completion, exception or cancellation.
		/// </summary>
		/// <remarks>	When the NotificationKind is Error, Exception should always be a JobQueueException. </remarks>
		/// <value>	A sequence of observable job completion notifications. </value>
		IObservable<Notification<JobResult<TJobInput, TJobOutput>>> WhenJobCompletes { get; }

		/// <summary>	The observable that monitors job queue empty status. </summary>
		/// <value>	A sequence of job inputs that can result in an empty status. </value>
		IObservable<TJobInput> WhenQueueEmpty { get; }

		/// <summary>	Gets the number of running jobs. </summary>
		/// <value>	The number of running jobs. </value>
		int RunningCount { get; }

		/// <summary>	Gets the number of queued jobs. </summary>
		/// <value>	The number of queued jobs. </value>
		int QueuedCount { get; }

		/// <summary>	Adds a job matching a given input / output typing and an input value. </summary>
		/// <param name="input"> 	The input. </param>
		/// <param name="action">	The action to perform. </param>
		/// <returns>	A sequence of Observable JobResult instances. </returns>
		IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, TJobOutput> action);

		/// <summary>	Adds a job matching a given input / output typing and an input value. </summary>
		/// <param name="input">	 	The input. </param>
		/// <param name="asyncStart">	The asynchronous observable action to perform. </param>
		/// <returns>	A sequence of Observable JobResult instances. </returns>
		IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, IObservable<TJobOutput>> asyncStart);

		/// <summary>	Starts the next job in the queue. </summary>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		bool StartNext();


		/// <summary>	Starts up to the given number of jobs in the queue concurrently. </summary>
		/// <param name="maxConcurrentlyRunning">	The maximum concurrently running jobs to allow. </param>
		/// <returns>	The number of jobs started. </returns>
		int StartUpTo(int maxConcurrentlyRunning);

		/// <summary>	Cancel outstanding jobs, which will result in Notifications being pushed through the WhenJobCompletes observable. </summary>
		void CancelOutstandingJobs();
	}
}