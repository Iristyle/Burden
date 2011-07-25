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
	/// <summary>
	/// A job queue of a given input and output type, where the jobs are executed by manually calling StartNext, or StartUpTo.
	/// </summary>
	/// <remarks>	7/15/2011. </remarks>
	/// <typeparam name="TJobInput"> 	Type of the job input. </typeparam>
	/// <typeparam name="TJobOutput">	Type of the job output. </typeparam>
	public class ManualJobExecutionQueue<TJobInput, TJobOutput> 
		: IJobExecutionQueue<TJobInput, TJobOutput>
	{
		//this class uniquely identifies an internal cancellation so we can properly modify the running job count
		private class JobQueueCancellationException : OperationCanceledException
		{ }

		protected struct Job
		{
			public TJobInput Input;
			public Func<TJobInput, IObservable<TJobOutput>> AsyncStart;
			public AsyncSubject<JobResult<TJobInput, TJobOutput>> CompletionHandler;
			public BooleanDisposable Cancel;
			public MultipleAssignmentDisposable JobSubscription;
		}

		private readonly IScheduler scheduler;
		private ConcurrentQueue<Job> queue = new ConcurrentQueue<Job>();
		private int runningCount;

		private Subject<JobResult<TJobInput, TJobOutput>> whenJobCompletes
			= new Subject<JobResult<TJobInput, TJobOutput>>();
		private Subject<Unit> whenQueueEmpty = new Subject<Unit>();

		/// <summary>	Default constructor, that uses the TaskPool scheduler in standard .NET or the ThreadPool scheduler in Silverlight. </summary>
		/// <remarks>	7/15/2011. </remarks>
		public ManualJobExecutionQueue()
#if SILVERLIGHT
			: this(Scheduler.ThreadPool)
#else
			: this(Scheduler.TaskPool)
#endif
		{ }

		internal ManualJobExecutionQueue(IScheduler scheduler)
		{
			this.scheduler = scheduler;

			// whenQueueEmpty subscription
			whenJobCompletes.Subscribe(n =>
			{
				int queueCount = queue.Count;
				int running = runningCount;
				//only decrement the running count if we're not dealing with a cancellation 
				if (null == n.Exception as JobQueueCancellationException)
				{
					running = Interlocked.Decrement(ref runningCount);
				}

				if (running == 0 && queueCount == 0)
					whenQueueEmpty.OnNext(new Unit());
			});
		}

		/// <summary>
		/// The Observable that monitors job completion, where completion can be either run to completion, exception or cancellation.
		/// </summary>
		/// <value>	A sequence of observable job completion notifications. </value>
		public IObservable<JobResult<TJobInput, TJobOutput>> WhenJobCompletes
		{
			get 
			{ 
				return whenJobCompletes
				.Select(result => 
				{ 
					//have to convert our custom exception back to something standard
					if (result.Exception is JobQueueCancellationException)
					{
						return JobResult.CreateOnError(result.Input, new OperationCanceledException());
					}
					return result;
				})
				.AsObservable(); 
			}
		}

		/// <summary>	The observable that monitors job queue empty status. </summary>
		/// <value>	A simple notification indicating the queue has reached empty status. </value>
		public IObservable<Unit> WhenQueueEmpty
		{
			get { return whenQueueEmpty.AsObservable(); }
		}

		/// <summary>	Gets the number of running jobs. </summary>
		/// <value>	The number of running jobs. </value>
		public int RunningCount
		{
			get { return runningCount; }
		}

		/// <summary>	Gets the number of queued jobs. </summary>
		/// <value>	The number of queued jobs. </value>
		public int QueuedCount
		{
			get { return queue.Count; }
		}

		/// <summary>	Adds a job matching a given input / output typing and an input value. </summary>
		/// <param name="input"> 	The input. </param>
		/// <param name="action">	The action to perform. </param>
		/// <returns>	A sequence of Observable JobResult instances. </returns>
		public IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, TJobOutput> action)
		{
			if (null == input) { throw new ArgumentNullException("input"); }
			if (null == action) { throw new ArgumentNullException("action"); }

			return Add(input, Observable.ToAsync(action));
		}

		/// <summary>	Adds a job matching a given input / output typing and an input value. </summary>
		/// <param name="input">	 	The input. </param>
		/// <param name="asyncStart">	The asynchronous observable action to perform. </param>
		/// <returns>	A sequence of Observable JobResult instances. </returns>
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
				job.Cancel))
			.ObserveOn(scheduler);

			job.CompletionHandler
				.ObserveOn(scheduler)
				.Materialize()
				.Where(n => n.Kind == NotificationKind.OnNext)
				.Select(n => n.Value)
				.Subscribe(whenJobCompletes.OnNext);
			// pass on errors and completions

			queue.Enqueue(job);
			return cancelable;
		}

		/// <summary>	Starts the next job in the queue. </summary>
		/// <returns>	true if it succeeds, false if it fails. </returns>
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

		/// <summary>	Starts up to the given number of jobs in the queue concurrently. </summary>
		/// <param name="maxConcurrentlyRunning">	The maximum concurrently running jobs to allow. </param>
		/// <returns>	The number of jobs started. </returns>
		public int StartUpTo(int maxConcurrentlyRunning)
		{
			int started = 0;
			while (true)
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

		/// <summary>	Cancel outstanding jobs, which will result in Notifications being pushed through the WhenJobCompletes observable. </summary>
		public void CancelOutstandingJobs()
		{
			Job job;
			while (TryDequeNextJob(out job))
			{
				job.Cancel.Dispose();
				job.CompletionHandler.OnNext(JobResult.CreateOnError(job.Input, new JobQueueCancellationException()));
				job.CompletionHandler.OnCompleted();
			}
		}

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
			job.CompletionHandler.OnNext(error == null ? JobResult.CreateOnCompletion(job.Input, jobResult)
				: JobResult.CreateOnError(job.Input, error));
			job.CompletionHandler.OnCompleted();
		}
	}
}