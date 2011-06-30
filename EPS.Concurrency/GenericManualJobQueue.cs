using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace EPS.Concurrency
{
	public class ManualJobQueue<T> : IJobQueue<T>
	{
		protected struct Job
		{
			public Func<IObservable<T>> AsyncStart;
			public AsyncSubject<T> CompletionHandler;
			public BooleanDisposable Cancel;
			public MultipleAssignmentDisposable JobSubscription;
		}

		ConcurrentQueue<Job> queue;
		int runningCount;

		Subject<Notification<T>> whenJobCompletes;
		Subject<T> whenQueueEmpty;
		IObservable<Exception> whenJobFails;

		public ManualJobQueue()
		{
			queue = new ConcurrentQueue<Job>();
			whenJobCompletes = new Subject<Notification<T>>();
			whenQueueEmpty = new Subject<T>();

			// whenJobFailes subscription
			whenJobFails = whenJobCompletes.Where(n => n.Kind == NotificationKind.OnError)
			.Select(n => n.Exception);

			// whenQueueEmpty subscription
			whenJobCompletes.Subscribe(n =>
			{
				int queueCount = queue.Count;
				if (Interlocked.Decrement(ref runningCount) == 0 && queueCount == 0)
					whenQueueEmpty.OnNext(n.Value);
			});
		}

		#region IJobQueue Implementation

		public IObservable<Notification<T>> WhenJobCompletes
		{
			get { return whenJobCompletes.AsObservable(); }
		}

		public IObservable<T> WhenQueueEmpty
		{
			get { return whenQueueEmpty.AsObservable(); }
		}

		public IObservable<Exception> WhenJobFails
		{
			get { return whenJobFails; }
		}

		public int RunningCount
		{
			get { return runningCount; }
		}

		public int QueuedCount
		{
			get { return queue.Count; }
		}

		public IObservable<T> Add(Func<T> action)
		{
			if (null == action)
			{
				throw new ArgumentNullException("action");
			}

			return Add(Observable.ToAsync(action));
		}

		public virtual IObservable<T> Add(Func<IObservable<T>> asyncStart)
		{
			if (null == asyncStart)
			{
				throw new ArgumentNullException("asyncStart");
			}

			Job job = new Job()
			{
				AsyncStart = asyncStart,
				CompletionHandler = new AsyncSubject<T>(),
				Cancel = new BooleanDisposable(),
				JobSubscription = new MultipleAssignmentDisposable()
			};

			var cancelable = Observable.Create<T>(o => new CompositeDisposable(
			job.CompletionHandler.Subscribe(o),
			job.JobSubscription,
			job.Cancel
			)
			);

			job.CompletionHandler
			.Materialize()
			.Where(n => n.Kind == NotificationKind.OnCompleted || n.Kind == NotificationKind.OnError)
			.Subscribe(whenJobCompletes.OnNext);
			// pass on errors and completions

			queue.Enqueue(job);
			return cancelable;
		}

		public bool StartNext()
		{
			Job job;
			if (TryDequeNextJob(out job))
			{
				Interlocked.Increment(ref runningCount);
				StartJob(job);
				return true;
			}

			return false;
		}

		public int StartUpTo(int maxConcurrentlyRunning)
		{
			int started = 0;
			for (; ; )
			{
				for (; ; )
				{
					int running = 0;

					do // test and increment with compare and swap
					{
						running = runningCount;
						if (running >= maxConcurrentlyRunning)
							return started;
					}
					while (Interlocked.CompareExchange(ref runningCount, running + 1, running) != running);

					Job job;
					if (TryDequeNextJob(out job))
					{
						StartJob(job);
						++started;
					}
					else
					{
						// dequeing job failed but we already incremented running count
						Interlocked.Decrement(ref runningCount);

						// ensure that no other thread queued an item and did not start it
						// because the running count was too high
						if (queue.Count == 0)
						{
							// if there is nothing in the queue after the decrement 
							// we can safely return
							return started;
						}
					}
				}
			}
		}

		public void CancelOutstandingJobs()
		{
			Job job;
			while (TryDequeNextJob(out job))
			{
				job.Cancel.Dispose();
				job.CompletionHandler.OnError(new OperationCanceledException());
			}
		}

		#endregion

		private bool TryDequeNextJob(out Job job)
		{
			do
			{
				if (!queue.TryDequeue(out job))
					return false;
			}
			while (job.Cancel.IsDisposed);
			return true;
		}

		private void StartJob(Job job)
		{
			try
			{
				IDisposable jobSubscription =
				job.AsyncStart().Subscribe(
				u => OnJobCompleted(job, null),
				e => OnJobCompleted(job, e)
				);
				job.JobSubscription.Disposable = jobSubscription;

				if (job.Cancel.IsDisposed)
					job.JobSubscription.Dispose();
			}
			catch (Exception ex)
			{
				OnJobCompleted(job, ex);
				throw;
			}
		}

		protected virtual void OnJobCompleted(Job job, Exception error)
		{
			if (error == null)
			{
				job.CompletionHandler.OnNext(default(T));
				job.CompletionHandler.OnCompleted();
			}
			else
			{
				job.CompletionHandler.OnError(error);
			}
		}
	}
}

