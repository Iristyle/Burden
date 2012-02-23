using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Burden
{
	/// <summary>
	/// A job queue of a given input and output type, where the jobs are executed by manually calling StartNext, or StartAsManyAs.
	/// </summary>
	/// <remarks>	7/15/2011. </remarks>
	/// <typeparam name="TJobInput"> 	Type of the job input. </typeparam>
	/// <typeparam name="TJobOutput">	Type of the job output. </typeparam>
	public class ManualJobExecutionQueue<TJobInput, TJobOutput> 
		: IJobExecutionQueue<TJobInput, TJobOutput>
	{
		//this class uniquely identifies an internal cancellation so we can properly modify the running job count
		[SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "This is only used within this class, and doesn't need a complete implementation")]
		[SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "This is only used within this class, and doesn't need a complete implementation")]
		private sealed class JobQueueCancellationException : OperationCanceledException
		{ }

		/// <summary>	Defines the members necessary to encapsulate a job.  </summary>
		/// <remarks>	8/3/2011. </remarks>
		protected struct Job
		{
			/// <summary>	Gets or sets the jobs input. </summary>
			/// <value>	The input. </value>
			public TJobInput Input { get; set; }
			
			/// <summary>	Gets or sets the asynchronous Func used to start the job. </summary>
			/// <value>	The asynchronous start. </value>
			[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and IObservables, especially given this use case")]
			public Func<TJobInput, IObservable<TJobOutput>> AsyncStart { get; set; }
			
			/// <summary>	Gets or sets the completion handler. </summary>
			/// <value>	The completion handler. </value>
			[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and IObservables, especially given this use case")]
			public AsyncSubject<JobResult<TJobInput, TJobOutput>> CompletionHandler { get; set; }
			
			/// <summary>	Keeps track of the job cancellation. </summary>
			/// <value>	The cancel. </value>
			public BooleanDisposable Cancel { get; set; }
			
			/// <summary>	Gets or sets the job subscription. </summary>
			/// <value>	The job subscription. </value>
			public MultipleAssignmentDisposable JobSubscription { get; set; }
		}

		private bool _disposed, _completionDisposed;
		private readonly IScheduler _scheduler;
		private ConcurrentQueue<Job> _queue = new ConcurrentQueue<Job>();
		private int _runningCount;
		private static int _maxAllowedConcurrentJobs = 50;

		private static int _defaultConcurrent = 20;
		private int _maxConcurrent;

		private Subject<JobResult<TJobInput, TJobOutput>> _whenJobCompletes
			= new Subject<JobResult<TJobInput, TJobOutput>>();
		private Subject<Unit> _whenQueueEmpty = new Subject<Unit>();

		/// <summary>	Default constructor, that uses the TaskPool scheduler in standard .NET or the ThreadPool scheduler in Silverlight. </summary>
		/// <remarks>	7/15/2011. </remarks>
		public ManualJobExecutionQueue()
			: this(DefaultConcurrent)
		{ }

		/// <summary>	Default constructor, that uses the TaskPool scheduler in standard .NET or the ThreadPool scheduler in Silverlight. </summary>
		/// <remarks>	7/15/2011. </remarks>
		public ManualJobExecutionQueue(int maxConcurrent)
			: this(LocalScheduler.Default, maxConcurrent)
		{
			if (maxConcurrent < 1 || maxConcurrent > _maxAllowedConcurrentJobs)
			{
				throw new ArgumentOutOfRangeException("maxConcurrent", maxConcurrent, "must be at least 1 and less than or equal to MaxAllowedConcurrentJobs");
			}
		}

		//allowing maxConcurrent here lets us use 0 in tests
		[SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule", Justification = "Used by test classes to change scheduler")]
		internal ManualJobExecutionQueue(IScheduler scheduler, int maxConcurrent)
		{
			this._scheduler = scheduler;
			this._maxConcurrent = maxConcurrent;

			// whenQueueEmpty subscription
			_whenJobCompletes.Subscribe(n =>
			{
				int queueCount = _queue.Count;
				int running = _runningCount;
				//only decrement the running count if we're not dealing with a cancellation 
				if (null == n.Exception as JobQueueCancellationException)
				{
					running = Interlocked.Decrement(ref _runningCount);
				}

				if (running == 0 && queueCount == 0)
					_whenQueueEmpty.OnNext(new Unit());
			});
		}

		/// <summary>	Throws an ObjectDisposedException if this object has been disposed of. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <exception cref="ObjectDisposedException">	Thrown when a supplied object has been disposed. </exception>
		protected void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("this");
			}
		}

		/// <summary>	Gets a value indicating whether the instance has been disposed. </summary>
		/// <value>	true if disposed, false if not. </value>
		protected bool Disposed
		{
			get { return _disposed; }
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
		/// Dispose of this object, cleaning up any resources it uses.  Will cancel any outstanding un-executed jobs, and will wait on jobs
		/// currently executing to complete.
		/// </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <param name="disposing">	true if resources should be disposed, false if not. </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				this._disposed = true;
				
				//attempt to wait for jobs currently executing to complete before disposing
				CancelOutstandingJobs();
				using (var wait = new ManualResetEventSlim(false))
				using (var subscription = _whenQueueEmpty.Subscribe(n => wait.Set()))
				{
					if (RunningCount != 0)
					{
						wait.Wait(TimeSpan.FromSeconds(20));
					}
				}
				_whenJobCompletes.Dispose();
				this._completionDisposed = true;
				_whenQueueEmpty.Dispose();
			}
		}

		/// <summary>	Gets the default number of concurrent jobs to run on this ManualJobExecutionQueue. </summary>
		/// <value>	The default number of concurrent jobs. </value>
		public static int DefaultConcurrent
		{
			get { return _defaultConcurrent; }
		}

		/// <summary>	Gets the maximum allowed concurrent jobs for a ManualJobExecutionQueue. </summary>
		/// <value>	The maximum allowed concurrent jobs. </value>
		public static int MaxAllowedConcurrentJobs
		{
			get { return _maxAllowedConcurrentJobs; }
		}

		/// <summary>
		/// Gets or sets the maximum number of allowed concurrent jobs.  If a value is set above MaxAllowedConcurrentJobs, then
		/// MaxAllowedConcurrentJobs is set as the value.  If set below 1, the value is set to 1.  Does not affect the status of currently
		/// executing jobs.
		/// </summary>
		/// <value>	The maximum allowed concurrent jobs, which defaults to the maximum allowed 50. </value>
		public int MaxConcurrent
		{
			get { return _maxConcurrent; }
			set 
			{
				_maxConcurrent = Math.Max(1, Math.Min(value, MaxAllowedConcurrentJobs)); 
			}
		}

		/// <summary>
		/// The Observable that monitors job completion, where completion can be either run to completion, exception or cancellation.
		/// </summary>
		/// <value>	A sequence of observable job completion notifications. </value>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed of, and is therefore no longer accepting new
		/// 												jobs or publishing notifications. </exception>
		public IObservable<JobResult<TJobInput, TJobOutput>> WhenJobCompletes
		{
			get 
			{ 
				ThrowIfDisposed();
				return _whenJobCompletes
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
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed of, and is therefore no longer accepting new
		/// 												jobs or publishing notifications. </exception>
		public IObservable<Unit> WhenQueueEmpty
		{
			get 
			{ 
				ThrowIfDisposed();
				return _whenQueueEmpty.AsObservable(); 
			}
		}

		/// <summary>	Gets the number of running jobs. </summary>
		/// <value>	The number of running jobs. </value>
		public int RunningCount
		{
			get { return _runningCount; }
		}

		/// <summary>	Gets the number of queued jobs. </summary>
		/// <value>	The number of queued jobs. </value>
		public int QueuedCount
		{
			get { return _queue.Count; }
		}

		/// <summary>	Adds a job matching a given input / output typing and an input value. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when one the input or action are null. </exception>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed of, and is therefore no longer accepting new
		/// 												jobs or publishing notifications. </exception>
		/// <param name="input"> 	The input. </param>
		/// <param name="action">	The action to perform. </param>
		/// <returns>	A sequence of Observable JobResult instances. </returns>
		public IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, TJobOutput> action)
		{
			ThrowIfDisposed();
			if (null == input) { throw new ArgumentNullException("input"); }
			if (null == action) { throw new ArgumentNullException("action"); }

			return Add(input, Observable.ToAsync(action));
		}

		/// <summary>	Adds a job matching a given input / output typing and an input value. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the input or asyncStart are null. </exception>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed of, and is therefore no longer accepting new
		/// 												jobs or publishing notifications. </exception>
		/// <param name="input">	 	The input. </param>
		/// <param name="asyncStart">	The asynchronous observable action to perform. </param>
		/// <returns>	A sequence of Observable JobResult instances. </returns>
		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Job disposables are tracked and later disposed as necessary")]		
		public virtual IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, IObservable<TJobOutput>> asyncStart)
		{
			ThrowIfDisposed();
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
			.ObserveOn(_scheduler);

			job.CompletionHandler
				.ObserveOn(_scheduler)
				.Materialize()
				.Where(n => n.Kind == NotificationKind.OnNext)
				.Select(n => n.Value)
				.Subscribe(_whenJobCompletes.OnNext);
			// pass on errors and completions

			_queue.Enqueue(job);
			return cancelable;
		}

		/// <summary>
		/// Starts the next job in the queue, as long as the current number of running jobs does not exceed the maximum upper limit allowed by
		/// the job queue, presently 50.
		/// </summary>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed of, and is therefore no longer accepting new
		/// 												jobs or publishing notifications. </exception>
		/// <remarks>	7/30/2011. </remarks>
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool StartNext()
		{
			ThrowIfDisposed();

			if (_runningCount >= _maxConcurrent)
			{
				return false;
			}

			Job job;
			if (TryDequeNextJob(out job))
			{
				Interlocked.Increment(ref _runningCount);
				StartJob(job);
				return true;
			}

			return false;
		}

		/// <summary>	Starts up to the given number of jobs in the queue concurrently. </summary>
		/// <remarks>	7/30/2011. </remarks>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed of, and is therefore no longer accepting new
		/// 												jobs or publishing notifications. </exception>
		/// <param name="maxConcurrentlyRunning">	The maximum concurrently running jobs to allow, which will be set to an upper limit of
		/// 										MaxAllowedConcurrentJobs (presently 50). </param>
		/// <returns>	The number of jobs started. </returns>
		public int StartAsManyAs(int maxConcurrentlyRunning)
		{
			ThrowIfDisposed();

			maxConcurrentlyRunning = Math.Min(maxConcurrentlyRunning, _maxConcurrent);

			int started = 0;
			while (true)
			{
				int running = 0;

				do // test and increment with compare and swap
				{
					running = _runningCount;
					if (running >= maxConcurrentlyRunning)
						return started;
				}
				while (Interlocked.CompareExchange(ref _runningCount, running + 1, running) != running);

				Job job;
				if (TryDequeNextJob(out job))
				{
					StartJob(job);
					++started;
				}
				else
				{
					// dequeing job failed but we already incremented running count
					Interlocked.Decrement(ref _runningCount);

					// ensure that no other thread queued an item and did not start it
					// because the running count was too high
					if (_queue.Count == 0)
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
				if (!_queue.TryDequeue(out job))
					return false;
			}
			while (job.Cancel.IsDisposed);
			return true;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "Proper job disposal is spread out amongst several methods")]
		private void StartJob(Job job)
		{
			try
			{
				job.JobSubscription.Disposable = job.AsyncStart(job.Input).ObserveOn(_scheduler).Subscribe(
					result => OnJobCompleted(job, result, null),
					e => OnJobCompleted(job, default(TJobOutput), e));

				if (job.Cancel.IsDisposed)
					job.JobSubscription.Dispose();
			}
			catch (Exception ex)
			{
				OnJobCompleted(job, default(TJobOutput), ex);
				throw;
			}
		}

		/// <summary>	Fires OnNext for our CompletionHandler, passing along the appropriate JobResult based on whether or not there was an error. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <param name="job">			The job. </param>
		/// <param name="jobResult">	The job result. </param>
		/// <param name="exception">		The error, if one exists. </param>
		protected virtual void OnJobCompleted(Job job, TJobOutput jobResult, Exception exception)
		{
			if (_completionDisposed)
			{
				return;
			}

			job.CompletionHandler.OnNext(exception == null ? JobResult.CreateOnCompletion(job.Input, jobResult)
				: JobResult.CreateOnError(job.Input, exception));
			job.CompletionHandler.OnCompleted();
		}
	}
}