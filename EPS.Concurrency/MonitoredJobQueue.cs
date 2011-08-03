using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace EPS.Concurrency
{
	/// <summary>	Wraps up the durable storage and in-memory execution of jobs into something more convenient to use. </summary>
	/// <remarks>	7/24/2011. </remarks>
	/// <typeparam name="TInput"> 	Type of the input. </typeparam>
	/// <typeparam name="TOutput">	Type of the output. </typeparam>
	/// <typeparam name="TPoison">	Type of the poison. </typeparam>
	[SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "The heavy use of generics is mitigated by numerous static helpers that use compiler inference")]
	public class MonitoredJobQueue<TInput, TOutput, TPoison>
		: IMonitoredJobQueue<TInput, TOutput, TPoison>
	{
		private bool _disposed;
		private readonly ObservableDurableJobQueue<TInput, TPoison> _durableQueue;
		private readonly AutoJobExecutionQueue<TInput, TOutput> _jobQueue;
		private readonly DurableJobQueueMonitor<TInput, TPoison> _monitor;
		private readonly JobResultJournalWriter<TInput, TOutput, TPoison> _resultJournaler;
		private readonly IDisposable _subscription;

		//creation calls should go through the static factory 
		internal MonitoredJobQueue(ObservableDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction,
			int maxConcurrentJobsToExecute, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector,
			int maxQueueItemsToPublishPerInterval, TimeSpan pollingInterval, IScheduler scheduler)
		{
			//perform null checks only, knowing that additional checks are performed by the new() calls below and will bubble up
			if (null == durableQueue)
			{
				throw new ArgumentNullException("durableQueue");
			}
			if (null == jobAction)
			{
				throw new ArgumentNullException("jobAction");
			}
			if (null == resultsInspector)
			{
				throw new ArgumentNullException("resultsInspector");
			}
			if (null == scheduler)
			{
				throw new ArgumentNullException("scheduler");
			}
			this._durableQueue = durableQueue;
			this._monitor = new DurableJobQueueMonitor<TInput, TPoison>(durableQueue, maxQueueItemsToPublishPerInterval,
			pollingInterval, scheduler);
			this._jobQueue = new AutoJobExecutionQueue<TInput, TOutput>(scheduler, maxConcurrentJobsToExecute, maxConcurrentJobsToExecute);
			this._resultJournaler = new JobResultJournalWriter<TInput, TOutput, TPoison>(_jobQueue.WhenJobCompletes, resultsInspector,
			durableQueue, null, scheduler);

			this._subscription = _monitor
			.SubscribeOn(scheduler)
			.Subscribe(input => _jobQueue.Add(input, jobAction));
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
				CancelQueuedAndWaitForExecutingJobsToComplete(TimeSpan.FromSeconds(20));
				_jobQueue.Dispose();
				_subscription.Dispose();
				_resultJournaler.Dispose();
				_durableQueue.Dispose();
			}
		}

		/// <summary>	Gets notifications as items are moved around the durable queue. </summary>
		/// <value>	A sequence of observable durable queue notifications. </value>
		public IObservable<DurableJobQueueAction<TInput, TPoison>> OnQueueAction
		{
			get { return _durableQueue.OnQueueAction; }
		}

		/// <summary>
		/// The Observable that monitors job completion, where completion can be either run to completion, exception or cancellation.
		/// </summary>
		/// <value>	A sequence of observable job completion notifications. </value>
		public IObservable<JobResult<TInput, TOutput>> OnJobCompletion 
		{
			get { return _jobQueue.WhenJobCompletes; }
		}

		/// <summary>	Gets the maximum number of concurrent jobs allowed to execute for this queue. </summary>
		/// <value>	The maximum allowed concurrent jobs. </value>
		public int MaxConcurrent 
		{ 
			get { return _jobQueue.MaxConcurrent; }
			set { _jobQueue.MaxConcurrent = value; }
		}

		/// <summary>	Gets the number of running jobs from the job execution queue. </summary>
		/// <value>	The number of running jobs. </value>
		public int RunningCount
		{
			get { return _jobQueue.RunningCount; }
		}

		/// <summary>	Gets the number of queued jobs from the job execution queue. </summary>
		/// <value>	The number of queued jobs. </value>
		public int QueuedCount
		{
			get { return _jobQueue.QueuedCount; }
		}
		
		/// <summary>	Gets the maximum allowable queue items to publish per interval, presently 50000. </summary>
		/// <value>	The maximum allowable queue items to publish per interval, presently 50000. </value>
		public int MaxQueueItemsToPublishPerInterval
		{
			get { return this._monitor.MaxQueueItemsToPublishPerInterval; }
		}

		/// <summary>	Gets the polling interval. </summary>
		/// <value>	The polling interval. </value>
		public TimeSpan PollingInterval
		{
			get { return this._monitor.PollingInterval; }
		}

		/// <summary>	Adds a job.  </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <param name="input">	The input. </param>
		public void AddJob(TInput input)
		{
			_durableQueue.Queue(input);
		}

		/// <summary>	Cancel queued and wait for executing jobs to complete. </summary>
		/// <remarks>	7/24/2011. </remarks>
		public void CancelQueuedAndWaitForExecutingJobsToComplete(TimeSpan timeout)
		{
			var manualResetEventSlim = new ManualResetEventSlim(false);
			using (var completed = _jobQueue.WhenQueueEmpty
				.Do(n => {}, () => manualResetEventSlim.Set())
				.Subscribe())
			{
				_jobQueue.CancelOutstandingJobs();

				if (_jobQueue.RunningCount != 0)
				{
					manualResetEventSlim.Wait(timeout);
				}
			}
		}
	}

	/// <summary>
	/// A simple helper class for creating a simplified IMonitoredJobQueue facade given an IDurableJobQueueFactory and job action.  Overloads
	/// provide the ability to inspect given job output via a custom result inspection method.
	/// </summary>
	/// <remarks>	7/24/2011. </remarks>
	public static class MonitoredJobQueue
	{
		private static ObservableDurableJobQueue<TQueue, TQueuePoison> CreateQueue<TQueue, TQueuePoison>(IDurableJobQueueFactory durableQueueFactory)
		{
			if (null == durableQueueFactory) { throw new ArgumentNullException("durableQueueFactory"); }

			return new ObservableDurableJobQueue<TQueue, TQueuePoison>(durableQueueFactory.CreateDurableJobQueue<TQueue, TQueuePoison>());
		}

		private static int GetEstimatedJobsToExecutePerInterval(TimeSpan interval, int maxConcurrency)
		{
			//TODO: 8-1-2011 -- our base assumption, which is sure to be wrong, is that an avg job takes .5 seconds to complete, so grab that many + a 20% fudge factor
			return interval.Seconds * 2 * maxConcurrency * 12 / 10;
		}

		/// <summary>
		/// Creates a simplified IMonitoredJobQueue interface given a durable queue factory, job action and maximum items to queue per interval.
		/// Poison is defaulted to Poison{TInput}.  JobResultInspector is defaulted based on the job specification.
		/// DurableJobQueueMonitor.DefaultPollingInterval is used as a polling interval.
		/// </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <param name="durableQueueFactory">		 	A durable queue factory. </param>
		/// <param name="jobAction">				 	The job action. </param>
		/// <param name="maxConcurrentJobsToExecute">	The maximum queue items to execute (which manipulates the maximum queue items published
		/// 											per interval based on an assumed 1/2 sec per job). </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		/// <exception cref="ArgumentNullException">	  	Thrown when the factory or action are null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the maximum queue items to publish per interval is too high or low. </exception>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposables are now responsibility of MonitoredJobQueue")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and we get to use compiler inference here")]
		public static IMonitoredJobQueue<TInput, TOutput, Poison<TInput>> Create<TInput, TOutput>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, int maxConcurrentJobsToExecute)
		{
			return new MonitoredJobQueue<TInput, TOutput, Poison<TInput>>(CreateQueue<TInput, Poison<TInput>>(durableQueueFactory), jobAction,
			maxConcurrentJobsToExecute, JobResultInspector.FromJobSpecification(jobAction), GetEstimatedJobsToExecutePerInterval(DurableJobQueueMonitor.DefaultPollingInterval, maxConcurrentJobsToExecute),
			DurableJobQueueMonitor.DefaultPollingInterval, LocalScheduler.Default);
		}

		/// <summary>
		/// Creates a simplified IMonitoredJobQueue interface given a durable queue factory, a job action, a results inspector and maximum items
		/// to queue per interval. DurableJobQueueMonitor.DefaultPollingInterval is used as a polling interval.
		/// </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueueFactory">					A durable queue factory. </param>
		/// <param name="jobAction">							The job action. </param>
		/// <param name="resultsInspector">						The results inspector. </param>
		/// <param name="maxConcurrentJobsToExecute">	The maximum queue items to execute (which manipulates the maximum queue items published
		/// 											per interval based on an assumed 1/2 sec per job). </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		/// <exception cref="ArgumentNullException">	  	Thrown when the factory, job queue or results inspectors are null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the maximum queue items to publish per interval is too high or low. </exception>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposables are now responsibility of MonitoredJobQueue")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and we get to use compiler inference here")]
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, int maxConcurrentJobsToExecute, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction,
			maxConcurrentJobsToExecute, resultsInspector, GetEstimatedJobsToExecutePerInterval(DurableJobQueueMonitor.DefaultPollingInterval, maxConcurrentJobsToExecute), 
			DurableJobQueueMonitor.DefaultPollingInterval, LocalScheduler.Default);
		}

		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable queue factory and a job queue. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueueFactory">					A durable queue to which all job information is written in a fail-safe manner. </param>
		/// <param name="jobAction">							The job action. </param>
		/// <param name="resultsInspector">						The results inspector. </param>
		/// <param name="maxConcurrentJobsToExecute">	The maximum queue items to execute (which manipulates the maximum queue items published
		/// 											per interval based on an assumed 1/2 sec per job). </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		/// <exception cref="ArgumentNullException">	  	Thrown when the factory, job queue or results inspectors are null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the maximum queue items to publish per interval is too high or low. </exception>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposables are now responsibility of MonitoredJobQueue")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and we get to use compiler inference here")]
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, int maxConcurrentJobsToExecute, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction,
			maxConcurrentJobsToExecute, JobResultInspector.FromInspector(resultsInspector), GetEstimatedJobsToExecutePerInterval(DurableJobQueueMonitor.DefaultPollingInterval, maxConcurrentJobsToExecute),
			DurableJobQueueMonitor.DefaultPollingInterval, LocalScheduler.Default);
		}

		/// <summary>
		/// Creates a simplified IMonitoredJobQueue interface given a durable queue factory, job action and maximum items to queue per interval.
		/// Poison is defaulted to Poison{TInput}.  JobResultInspector is defaulted based on the job specification.
		/// </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <param name="durableQueueFactory">					A durable queue factory. </param>
		/// <param name="jobAction">							The job action. </param>
		/// <param name="maxConcurrentJobsToExecute">	The maximum queue items to execute (which manipulates the maximum queue items published
		/// 											per interval based on an assumed 1/2 sec per job). </param>
		/// <param name="pollingInterval">						The polling interval. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		/// <exception cref="ArgumentNullException">	  	Thrown when the factory or action are null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the maximum queue items to publish per interval is too high or low,
		/// 													or the polling interval is too fast or slow. </exception>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposables are now responsibility of MonitoredJobQueue")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and we get to use compiler inference here")]
		public static IMonitoredJobQueue<TInput, TOutput, Poison<TInput>> Create<TInput, TOutput>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, int maxConcurrentJobsToExecute, TimeSpan pollingInterval)
		{
			return new MonitoredJobQueue<TInput, TOutput, Poison<TInput>>(CreateQueue<TInput, Poison<TInput>>(durableQueueFactory), jobAction,
			maxConcurrentJobsToExecute, JobResultInspector.FromJobSpecification(jobAction), GetEstimatedJobsToExecutePerInterval(pollingInterval, maxConcurrentJobsToExecute), 
			pollingInterval, LocalScheduler.Default);
		}

		/// <summary>
		/// Creates a simplified IMonitoredJobQueue interface given a durable queue factory, a job action, a results inspector and maximum items
		/// to queue per interval. DurableJobQueueMonitor.
		/// </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueueFactory">					A durable queue factory. </param>
		/// <param name="jobAction">							The job action. </param>
		/// <param name="resultsInspector">						The results inspector. </param>
		/// <param name="maxConcurrentJobsToExecute">	The maximum queue items to execute (which manipulates the maximum queue items published
		/// 											per interval based on an assumed 1/2 sec per job). </param>
		/// <param name="pollingInterval">						The polling interval. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		/// <exception cref="ArgumentNullException">	  	Thrown when the factory, job queue or results inspectors are null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the maximum queue items to publish per interval is too high or low,
		/// 													or the polling interval is too fast or slow. </exception>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposables are now responsibility of MonitoredJobQueue")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and we get to use compiler inference here")]
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, int maxConcurrentJobsToExecute, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector,
			TimeSpan pollingInterval)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction, maxConcurrentJobsToExecute,
			resultsInspector, GetEstimatedJobsToExecutePerInterval(pollingInterval, maxConcurrentJobsToExecute), pollingInterval, LocalScheduler.Default);
		}

		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable queue factory and a job queue. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueueFactory">					A durable queue to which all job information is written in a fail-safe manner. </param>
		/// <param name="jobAction">							The job action. </param>
		/// <param name="resultsInspector">						The results inspector. </param>
		/// <param name="maxConcurrentJobsToExecute">	The maximum queue items to execute (which manipulates the maximum queue items published
		/// 											per interval based on an assumed 1/2 sec per job). </param>
		/// <param name="pollingInterval">						The polling interval. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		/// <exception cref="ArgumentNullException">	  	Thrown when the factory, job queue or results inspectors are null. </exception>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the maximum queue items to publish per interval is too high or low,
		/// 													or the polling interval is too fast or slow. </exception>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposables are now responsibility of MonitoredJobQueue")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and we get to use compiler inference here")]
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, int maxConcurrentJobsToExecute, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector, 
			TimeSpan pollingInterval)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction, maxConcurrentJobsToExecute,
			JobResultInspector.FromInspector(resultsInspector), GetEstimatedJobsToExecutePerInterval(pollingInterval, maxConcurrentJobsToExecute), pollingInterval,
			LocalScheduler.Default);
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposables are now responsibility of MonitoredJobQueue")]
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Funcs and we get to use compiler inference here")]
		[SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule", Justification = "Used by test classes to change scheduler")]
		internal static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, int maxConcurrentJobsToExecute, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector,
			TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval, IScheduler scheduler)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction, maxConcurrentJobsToExecute,
			JobResultInspector.FromInspector(resultsInspector), maxQueueItemsToPublishPerInterval, pollingInterval, scheduler);
		}
	}
}