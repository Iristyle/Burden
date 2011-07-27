using System;
using System.Reactive.Linq;
using System.Threading;
using System.Reactive.Concurrency;

namespace EPS.Concurrency
{
	/// <summary>	Wraps up the durable storage and in-memory execution of jobs into something more convenient to use. </summary>
	/// <remarks>	7/24/2011. </remarks>
	/// <typeparam name="TInput"> 	Type of the input. </typeparam>
	/// <typeparam name="TOutput">	Type of the output. </typeparam>
	/// <typeparam name="TPoison">	Type of the poison. </typeparam>
	public class MonitoredJobQueue<TInput, TOutput, TPoison>
		: IMonitoredJobQueue<TInput, TOutput, TPoison>
	{
		private bool _disposed;
		private readonly ObservableDurableJobQueue<TInput, TPoison> durableQueue;
		private readonly AutoJobExecutionQueue<TInput, TOutput> jobQueue;
		private readonly DurableJobQueueMonitor<TInput, TPoison> monitor;
		private readonly JobResultJournaler<TInput, TOutput, TPoison> resultJournaler;
		private readonly IDisposable subscription;

		/// <summary>	Initializes a new instance of the MonitoredJobQueue class. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the durableQueue, jobAction or resultsInspector are null. </exception>
		/// <param name="durableQueue">	   	The durable queue. </param>
		/// <param name="jobAction">	   	The job to perform. </param>
		/// <param name="resultsInspector">	The results inspector. </param>
		public MonitoredJobQueue(IDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction, Func<
		JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector)
			: this(durableQueue, jobAction, JobResultInspector.FromInspector(resultsInspector),
#if SILVERLIGHT
			Scheduler.ThreadPool)
#else
			Scheduler.TaskPool)
#endif
		{ }

		/// <summary>	Initializes a new instance of the MonitoredJobQueue class. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the durableQueue, jobAction or resultsInspector are null. </exception>
		/// <param name="durableQueue">	   	The durable queue. </param>
		/// <param name="jobAction">	   	The job to perform. </param>
		/// <param name="resultsInspector">	The results inspector. </param>
		public MonitoredJobQueue(IDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector)
			: this(durableQueue, jobAction, resultsInspector,
#if SILVERLIGHT
			Scheduler.ThreadPool)
#else
 Scheduler.TaskPool)
#endif
		{ }


		internal MonitoredJobQueue(IDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector, IScheduler scheduler)
		{
			if (null == durableQueue) { throw new ArgumentNullException("durableQueue"); }
			if (null == jobAction) { throw new ArgumentNullException("jobAction"); }
			if (null == resultsInspector) { throw new ArgumentNullException("resultsInspector"); }

			this.durableQueue = new ObservableDurableJobQueue<TInput, TPoison>(durableQueue);
			this.monitor = new DurableJobQueueMonitor<TInput, TPoison>(durableQueue, 20, scheduler);			
			this.jobQueue = new AutoJobExecutionQueue<TInput, TOutput>(scheduler, 10);
			this.resultJournaler = new JobResultJournaler<TInput, TOutput, TPoison>(jobQueue.WhenJobCompletes, resultsInspector, durableQueue, null, scheduler);

			this.subscription = monitor
				.SubscribeOn(scheduler)
				.Subscribe(input => jobQueue.Add(input, jobAction));
			
		}

		/// <summary>	Adds a job.  </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <param name="input">	The input. </param>
		public void AddJob(TInput input)
		{
			durableQueue.Queue(input);
		}

		/// <summary>	Cancel queued and wait for executing jobs to complete. </summary>
		/// <remarks>	7/24/2011. </remarks>
		public void CancelQueuedAndWaitForExecutingJobsToComplete(TimeSpan timeout)
		{
			var manualResetEventSlim = new ManualResetEventSlim(false);
			using (var completed = jobQueue.WhenQueueEmpty
				.Do(n =>{ var @false = false; }, () => manualResetEventSlim.Set())
				.Subscribe())
			{
				jobQueue.CancelOutstandingJobs();

				if (jobQueue.RunningCount != 0)
				{
					manualResetEventSlim.Wait(timeout);
				}
			}
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
				subscription.Dispose();
			}
		}

		/// <summary>	Gets notifications as items are moved around the durable queue. </summary>
		/// <value>	A sequence of observable durable queue notifications. </value>
		public IObservable<DurableJobQueueAction<TInput, TPoison>> OnQueueAction
		{
			get { return durableQueue.OnQueueAction; }
		}

		/// <summary>
		/// The Observable that monitors job completion, where completion can be either run to completion, exception or cancellation.
		/// </summary>
		/// <value>	A sequence of observable job completion notifications. </value>
		public IObservable<JobResult<TInput, TOutput>> OnJobCompletion 
		{
			get { return jobQueue.WhenJobCompletes; }
		}

		/// <summary>	Gets the number of running jobs from the job execution queue. </summary>
		/// <value>	The number of running jobs. </value>
		public int RunningCount
		{
			get { return jobQueue.RunningCount; }
		}

		/// <summary>	Gets the number of queued jobs from the job execution queue. </summary>
		/// <value>	The number of queued jobs. </value>
		public int QueuedCount
		{
			get { return jobQueue.QueuedCount; }
		}
		
		/// <summary>	Gets the maximum allowable queue items to publish per interval, presently 50000. </summary>
		/// <value>	The maximum allowable queue items to publish per interval, presently 50000. </value>
		public int MaxAllowedQueueItemsToPublishPerInterval
		{
			get { return this.monitor.MaxAllowedQueueItemsToPublishPerInterval; }
		}

		/// <summary>	Gets the polling interval. </summary>
		/// <value>	The polling interval. </value>
		public TimeSpan PollingInterval
		{
			get { return this.monitor.PollingInterval; }
		}
	}

	/// <summary>	A simple helper class for creating a simplified IMonitoredJobQueue facade given an IDurableJobQueue and IJobExecutionQueue.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	internal static class MonitoredJobQueue
	{
		public static IMonitoredJobQueue<TInput, TOutput, Poison<TInput>> Create<TInput, TOutput>(IDurableJobQueue<TInput, Poison<TInput>> durableQueue,
			Func<TInput, TOutput> jobAction)
		{
			if (null == durableQueue) { throw new ArgumentNullException("durableQueue"); }
			if (null == jobAction) { throw new ArgumentNullException("jobAction"); }

			return new MonitoredJobQueue<TInput, TOutput, Poison<TInput>>(durableQueue, jobAction, JobResultInspector.FromJobSpecification(jobAction));
		}

		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable storage queue and a job queue. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the durable queue, job queue or results inspectors are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueue">	   	A durable queue to which all job information is written in a fail-safe manner. </param>
		/// <param name="jobAction">	   	The job action. </param>
		/// <param name="resultsInspector">	The results inspector. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueue<TInput, TPoison> durableQueue,
			Func<TInput, TOutput> jobAction, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector)
		{
			if (null == durableQueue) { throw new ArgumentNullException("durableQueue"); }
			if (null == jobAction) { throw new ArgumentNullException("jobAction"); }
			if (null == resultsInspector) { throw new ArgumentNullException("resultsInspector"); }

			return new MonitoredJobQueue<TInput, TOutput, TPoison>(durableQueue, jobAction, resultsInspector);
		}

		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable storage queue and a job queue. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the durable queue, job queue or results inspectors are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueue">	   	A durable queue to which all job information is written in a fail-safe manner. </param>
		/// <param name="jobAction">	   	The job action. </param>
		/// <param name="resultsInspector">	The results inspector. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueue<TInput, TPoison> durableQueue,
			Func<TInput, TOutput> jobAction, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector)
		{
			if (null == durableQueue) { throw new ArgumentNullException("durableQueue"); }
			if (null == jobAction) { throw new ArgumentNullException("jobAction"); }
			if (null == resultsInspector) { throw new ArgumentNullException("resultsInspector"); }
	
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(durableQueue, jobAction, resultsInspector);
		}

		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable storage queue and a job queue. </summary>
		/// <remarks>	7/25/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the durable queue, job queue, results inspectors or scheduler are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueue">	   	A durable queue to which all job information is written in a fail-safe manner. </param>
		/// <param name="jobAction">	   	The job action. </param>
		/// <param name="resultsInspector">	The results inspector. </param>
		/// <param name="scheduler">	   	The scheduler. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueue<TInput, TPoison> durableQueue,
			Func<TInput, TOutput> jobAction, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector, IScheduler scheduler)
		{
			if (null == durableQueue) { throw new ArgumentNullException("durableQueue"); }
			if (null == jobAction) { throw new ArgumentNullException("jobAction"); }
			if (null == resultsInspector) { throw new ArgumentNullException("resultsInspector"); }
			if (null == scheduler) { throw new ArgumentNullException("scheduler"); }

			return new MonitoredJobQueue<TInput, TOutput, TPoison>(durableQueue, jobAction, JobResultInspector.FromInspector(resultsInspector), scheduler);
		}
	}
}