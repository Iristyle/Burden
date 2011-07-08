using System;
using System.Reactive;

namespace EPS.Concurrency
{
	public class JournalingJobResultQueue<TQueue, TQueueResult, TQueuePoison>
	{
		private IDisposable jobCompleted;

		public JournalingJobResultQueue(IDurableJobStorageQueue<TQueue, TQueuePoison> durableJobStorage, 
			IJobResultInspector<TQueue, TQueueResult, TQueuePoison> jobResultInspector,
			IObservable<Notification<JobResult<TQueue, TQueueResult>>> whenJobCompletes)
		{
			if (null == durableJobStorage) { throw new ArgumentNullException("durableJobStorage"); }
			if (null == jobResultInspector) { throw new ArgumentNullException("jobResultInspector"); }
			if (null == whenJobCompletes) { throw new ArgumentNullException("whenJobCompletes"); }

			this.jobCompleted = whenJobCompletes
			.Subscribe(notification =>
			{
				var queueAction = jobResultInspector.Inspect(notification);

				if (queueAction.ActionType == JobQueueActionType.Poison)
				{
					durableJobStorage.Poison(notification.Value.Input, queueAction.QueuePoison);
				}
				else if (queueAction.ActionType == JobQueueActionType.Complete)
				{
					durableJobStorage.Complete(notification.Value.Input);
				}
				else
				{
					//TODO: 7-8-2011 -- we should probably be logging this off somewhere
				}
			});
		}
	}
}