using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Common.Logging;

namespace EPS.Concurrency
{
	/// <summary>
	/// This class journals the results of an in memory observable job publisher to a backing store for durability, using a custom job result
	/// inspector to determine if the original queue item should be removed or poisoned.  Bad notifications or inspection results are
	/// published to the logger.
	/// </summary>
	/// <remarks>	7/8/2011. </remarks>
	/// <typeparam name="TJobInput">   	The type of the input to the job. </typeparam>
	/// <typeparam name="TJobOutput">  	The type of the output from the job. </typeparam>
	/// <typeparam name="TQueuePoison">	Type to be stored if a job failed and should be poisoned. </typeparam>
	[SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "The heavy use of generics is mitigated by numerous static helpers that use compiler inference")]
	public class JobResultJournalWriter<TJobInput, TJobOutput, TQueuePoison>
		: IDisposable
	{
		private bool _disposed;
		private readonly IDisposable _jobCompleted;

		/// <summary>
		/// Constructs a new instance that listens to completion notifications on Scheduler.TaskPool or Scheduler.ThreadPool when on Silverlight .
		/// </summary>
		/// <remarks>	7/9/2011. </remarks>
		/// <param name="jobCompletionNotifications">	The job completion notification stream. </param>
		/// <param name="jobResultInspector">		 	The job result inspector. </param>
		/// <param name="durableJobQueue">		 	The durable job queue. </param>
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and IObservables")]
		public JobResultJournalWriter(IObservable<JobResult<TJobInput, TJobOutput>> jobCompletionNotifications, 
			IJobResultInspector<TJobInput, TJobOutput, TQueuePoison> jobResultInspector,
			IDurableJobQueue<TJobInput, TQueuePoison> durableJobQueue)
			: this(jobCompletionNotifications, jobResultInspector, durableJobQueue, LogManager.GetCurrentClassLogger(), LocalScheduler.Default)
		{ }

		[SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule", Justification = "Used by test classes to change scheduler")]
		internal JobResultJournalWriter(IObservable<JobResult<TJobInput, TJobOutput>> jobCompletionNotifications, 
			IJobResultInspector<TJobInput, TJobOutput, TQueuePoison> jobResultInspector,
			IDurableJobQueue<TJobInput, TQueuePoison> durableJobQueue,
			ILog log,
			IScheduler scheduler)
		{
			if (null == durableJobQueue) { throw new ArgumentNullException("durableJobQueue"); }
			if (null == jobResultInspector) { throw new ArgumentNullException("jobResultInspector"); }
			if (null == jobCompletionNotifications) { throw new ArgumentNullException("jobCompletionNotifications"); }

			this._jobCompleted = jobCompletionNotifications
			.SubscribeOn(scheduler)
			.Subscribe(notification =>	
			{
				if (null == notification)
				{
					log.Error(CultureInfo.CurrentCulture, m => m("Received invalid NULL Notification<JobResult<{0},{1}>>", typeof(TJobInput).Name, typeof(TJobOutput).Name));
					return;
				}

				var queueAction = jobResultInspector.Inspect(notification);

				if (null == queueAction)
				{
					log.Error(CultureInfo.CurrentCulture, m => m("Received invalid NULL JobQueueAction<{0}> from Inspect call", typeof(TQueuePoison).Name));
				}
				//no need to check 
				else if (queueAction.ActionType == JobQueueActionType.Poison)
				{
					durableJobQueue.Poison(notification.Input, queueAction.QueuePoison);
				}
				else if (queueAction.ActionType == JobQueueActionType.Complete)
				{
					durableJobQueue.Complete(notification.Input);
				}
				else
				{
					log.Error(CultureInfo.CurrentCulture, m => m("Received invalid JobQueueAction<{0}> with JobQueueActionType of Unknown", typeof(TQueuePoison).Name));
				}
			});
		}

		/// <summary>	Dispose of this object, cleaning up any resources it uses. </summary>
		/// <remarks>	7/24/2011. </remarks>
		public void Dispose()
		{
			if (!this._disposed)
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Dispose of this object, cleaning up any resources it uses.  This simply terminates the listener before being auto-terminated by the
		/// publisher of the notification stream.
		/// </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <param name="disposing">	true if resources should be disposed, false if not. </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				this._disposed = true;
				_jobCompleted.Dispose();
			}
		}

	}
}