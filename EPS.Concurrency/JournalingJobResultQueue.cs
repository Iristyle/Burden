using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Common.Logging;

namespace EPS.Concurrency
{
	/// <summary>
	/// This class journals the results of an in memory observable job publisher to a backing store for durability, using a custom job result
	/// inspector to determine if the original queue item should be removed or poisoned.  Bad notifications or inspection results are published to the logger.
	/// </summary>
	/// <remarks>	7/8/2011. </remarks>
	/// <typeparam name="TJobInput">   	The type of the input to the job. </typeparam>
	/// <typeparam name="TJobOutput">  	The type of the output from the job. </typeparam>
	/// <typeparam name="TQueuePoison">	Type to be stored if a job failed and should be poisoned. </typeparam>
	public class JournalingJobResultQueue<TJobInput, TJobOutput, TQueuePoison>
	{
		private IDisposable jobCompleted;

		/// <summary>	Constructs a new instance. </summary>
		/// <remarks>	7/9/2011. </remarks>
		/// <param name="jobCompletionNotifications">	The job completion notification stream. </param>
		/// <param name="jobResultInspector">		 	The job result inspector. </param>
		/// <param name="durableJobStorage">		 	The durable job storage. </param>
		public JournalingJobResultQueue(IObservable<Notification<JobResult<TJobInput, TJobOutput>>> jobCompletionNotifications, 
			IJobResultInspector<TJobInput, TJobOutput, TQueuePoison> jobResultInspector,
			IDurableJobStorageQueue<TJobInput, TQueuePoison> durableJobStorage) :
			this(jobCompletionNotifications, jobResultInspector, durableJobStorage, LogManager.GetCurrentClassLogger(), Scheduler.TaskPool)
		{ }

		internal JournalingJobResultQueue(IObservable<Notification<JobResult<TJobInput, TJobOutput>>> jobCompletionNotifications, 
			IJobResultInspector<TJobInput, TJobOutput, TQueuePoison> jobResultInspector,
			IDurableJobStorageQueue<TJobInput, TQueuePoison> durableJobStorage,
			ILog log,
			IScheduler scheduler)
		{

			if (null == durableJobStorage) { throw new ArgumentNullException("durableJobStorage"); }
			if (null == jobResultInspector) { throw new ArgumentNullException("jobResultInspector"); }
			if (null == jobCompletionNotifications) { throw new ArgumentNullException("jobCompletionNotifications"); }

			this.jobCompleted = jobCompletionNotifications
			.SubscribeOn(scheduler)
			.Subscribe(notification =>
			{
				if (null == notification)
				{
					log.Error(m => m("Received invalid NULL Notification<JobResult<{0},{1}>>", typeof(TJobInput).Name, typeof(TJobOutput).Name));
					return;
				}

				var queueAction = jobResultInspector.Inspect(notification);

				if (null == queueAction)
				{
					log.Error(m => m("Received invalid NULL JobQueueAction<{0}> from Inspect call", typeof(TQueuePoison).Name));
				}
				//no need to check 
				else if (queueAction.ActionType == JobQueueActionType.Poison)
				{
					durableJobStorage.Poison(notification.Value.Input, queueAction.QueuePoison);
				}
				else if (queueAction.ActionType == JobQueueActionType.Complete)
				{
					durableJobStorage.Complete(notification.Value.Input);
				}
				else
				{
					log.Error(m => m("Received invalid JobQueueAction<{0}> with JobQueueActionType of Unknown", typeof(TQueuePoison).Name));
				}
			});
		}
	}
}