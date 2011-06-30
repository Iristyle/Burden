using System;
using System.Reactive;

namespace EPS.Concurrency
{
	public class JournalingJobResultQueue<TQueue, TQueuePoison>
	{
		private IDisposable jobCompleted;
		private IDisposable jobFailed;

		public JournalingJobResultQueue(IDurableJobStorage<TQueue, TQueuePoison> durableJobStorage, Func<TQueue, Exception, TQueuePoison> poisonBuilder,
			IObservable<Notification<TQueue>> whenJobCompletes, IObservable<Exception> whenJobFails)
		{
			if (null == durableJobStorage) { throw new ArgumentNullException("durableJobStorage"); }
			if (null == poisonBuilder) { throw new ArgumentNullException("poisonBuilder"); }
			if (null == whenJobCompletes) { throw new ArgumentNullException("whenJobCompletes"); }
			if (null == whenJobFails) { throw new ArgumentNullException("whenJobFails"); }


			this.jobCompleted = whenJobCompletes
			.Subscribe((notification) =>
			{
				if (null == notification.Exception)
				{
					durableJobStorage.Complete(notification.Value);
				}
				else
				{
					durableJobStorage.Poison(notification.Value, poisonBuilder(notification.Value, notification.Exception));
				}
			});

			this.jobFailed = whenJobFails
			.Subscribe((notification) =>
			{
				//**********************
				//TODO: 6-30-2011 -- CRITICAL! need to figure out a way to get at the original input value
				//***********************
				throw new NotImplementedException();
				durableJobStorage.Poison(default(TQueue), poisonBuilder(default(TQueue), notification));
			});
		}
	}
}