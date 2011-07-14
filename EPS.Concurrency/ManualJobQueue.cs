using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace EPS.Concurrency
{
	public class ManualJobQueue<TJobInput, TJobOutput> : IJobQueue<TJobInput, TJobOutput>
	{
		protected struct Job
		{
			public TJobInput Input;
			public Func<TJobInput, IObservable<TJobOutput>> AsyncStart;
			public AsyncSubject<JobResult<TJobInput, TJobOutput>> CompletionHandler;
			public BooleanDisposable Cancel;
			public MultipleAssignmentDisposable JobSubscription;
		}

		private IScheduler scheduler;
		private ConcurrentQueue<Job> queue;
		private int runningCount;

		private Subject<JobResult<TJobInput, TJobOutput>> whenJobCompletes;
		private Subject<Unit> whenQueueEmpty;

		public ManualJobQueue()
			: this(Scheduler.TaskPool)
		{ }

		internal ManualJobQueue(IScheduler scheduler)
		{
			this.scheduler = scheduler;
			queue = new ConcurrentQueue<Job>();
			whenJobCompletes = new Subject<JobResult<TJobInput, TJobOutput>>();
			whenQueueEmpty = new Subject<Unit>();

			// whenQueueEmpty subscription
			whenJobCompletes.Subscribe(n =>
			{
				int queueCount = queue.Count;
				if (Interlocked.Decrement(ref runningCount) == 0 && queueCount == 0)
					whenQueueEmpty.OnNext(new Unit());
			});
		}

		#region IJobQueue Implementation

		public IObservable<JobResult<TJobInput, TJobOutput>> WhenJobCompletes
		{
			get { return whenJobCompletes.AsObservable(); }
		}

		public IObservable<Unit> WhenQueueEmpty
		{
			get { return whenQueueEmpty.AsObservable(); }
		}

		public int RunningCount
		{
			get { return runningCount; }
		}

		public int QueuedCount
		{
			get { return queue.Count; }
		}

		public IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, TJobOutput> action)
		{
			if (null == input) { throw new ArgumentNullException("input"); }
			if (null == action) { throw new ArgumentNullException("action"); }

			return Add(input, Observable.ToAsync(action));
		}

		public virtual IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, IObservable<TJobOutput>> asyncStart)
		{
			if (null == input) { throw new ArgumentNullException("input"); }
			if (null == asyncStart) { throw new ArgumentNullException("asyncStart"); }

			Job job = new Job()
			{
				AsyncStart = asyncStart,
				CompletionHandler = new AsyncSubject<JobResult<TJobInput, TJobOutput>>(),
				Cancel = new BooleanDisposable(),
				JobSubscription = new MultipleAssignmentDisposable(),
				Input = input
			};

			var cancelable = Observable.Create<JobResult<TJobInput, TJobOutput>>(o => new CompositeDisposable(
			job.CompletionHandler.Subscribe(o),
			job.JobSubscription,
			job.Cancel
			)
			).ObserveOn(scheduler);

			job.CompletionHandler
			.Materialize()
			.Select(n => n.Value)
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
				IDisposable jobSubscription = job.AsyncStart(job.Input).ObserveOn(scheduler).Subscribe(
					result => OnJobCompleted(job, result, null),
					e => OnJobCompleted(job, default(TJobOutput), e));

				job.JobSubscription.Disposable = jobSubscription;

				if (job.Cancel.IsDisposed)
					job.JobSubscription.Dispose();
			}
			catch (Exception ex)
			{
				OnJobCompleted(job, default(TJobOutput), ex);
				throw;
			}
		}

		protected virtual void OnJobCompleted(Job job, TJobOutput jobResult, Exception error)
		{
			if (error == null)
			{
				job.CompletionHandler.OnNext(JobResult.CreateOnCompletion(job.Input, jobResult));
			}
			else
			{
				job.CompletionHandler.OnNext(JobResult.CreateOnError(job.Input, error));
			}
		}
	}
}