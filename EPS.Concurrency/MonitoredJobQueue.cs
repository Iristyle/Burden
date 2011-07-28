using System;
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
	public class MonitoredJobQueue<TInput, TOutput, TPoison>
		: IMonitoredJobQueue<TInput, TOutput, TPoison>
	{
		private bool _disposed;
		private readonly ObservableDurableJobQueue<TInput, TPoison> durableQueue;
		private readonly AutoJobExecutionQueue<TInput, TOutput> jobQueue;
		private readonly DurableJobQueueMonitor<TInput, TPoison> monitor;
		private readonly JobResultJournaler<TInput, TOutput, TPoison> resultJournaler;
		private readonly IDisposable subscription;
		private static int defaultItemsToPublishPerInterval = 200;

		/// <summary>	Initializes a new instance of the MonitoredJobQueue class. </summary>
		/// <remarks>	Uses DefaultPollingInterval and DefaultItemstoPublishPerInterval. </remarks>
		/// <param name="durableQueue">	   	The durable queue. </param>
		/// <param name="jobAction">	   	The job to perform. </param>
		/// <param name="">				   	The. </param>
		/// <param name="resultsInspector">	The results inspector. </param>
		/// <exception cref="ArgumentNullException">	Thrown when the durableQueue, jobAction or resultsInspector are null. </exception>
		public MonitoredJobQueue(ObservableDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector)
			: this(durableQueue, jobAction, JobResultInspector.FromInspector(resultsInspector), DurableJobQueueMonitor.DefaultPollingInterval, defaultItemsToPublishPerInterval, LocalScheduler.Default)
		{ }

		/// <summary>	Initializes a new instance of the MonitoredJobQueue class. </summary>
		/// <remarks>	Uses DefaultPollingInterval and DefaultItemstoPublishPerInterval. </remarks>
		/// <param name="durableQueue">	   	The durable queue. </param>
		/// <param name="jobAction">	   	The job to perform. </param>
		/// <param name="resultsInspector">	The results inspector. </param>
		/// <exception cref="ArgumentNullException">	Thrown when the durableQueue, jobAction or resultsInspector are null. </exception>
		public MonitoredJobQueue(ObservableDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector)
			: this(durableQueue, jobAction, resultsInspector, DurableJobQueueMonitor.DefaultPollingInterval, defaultItemsToPublishPerInterval, LocalScheduler.Default)
		{ }

		/// <summary>	Initializes a new instance of the MonitoredJobQueue class. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <param name="durableQueue">							   	The durable queue. </param>
		/// <param name="jobAction">							   	The job to perform. </param>
		/// <param name="">										   	The. </param>
		/// <param name="resultsInspector">						   	The results inspector. </param>
		/// <param name="pollingInterval">						   	The polling interval. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	The maximum queue items to publish per interval. </param>
		/// <exception cref="ArgumentNullException">	Thrown when the durableQueue, jobAction or resultsInspector are null. </exception>
		public MonitoredJobQueue(ObservableDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector,
			TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval)
			: this(durableQueue, jobAction, JobResultInspector.FromInspector(resultsInspector), pollingInterval, maxQueueItemsToPublishPerInterval, LocalScheduler.Default)
		{ }

		/// <summary>	Initializes a new instance of the MonitoredJobQueue class. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <param name="durableQueue">							   	The durable queue. </param>
		/// <param name="jobAction">							   	The job to perform. </param>
		/// <param name="resultsInspector">						   	The results inspector. </param>
		/// <param name="pollingInterval">						   	The polling interval. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	The maximum queue items to publish per interval. </param>
		/// <exception cref="ArgumentNullException">	Thrown when the durableQueue, jobAction or resultsInspector are null. </exception>
		public MonitoredJobQueue(ObservableDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector,
			TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval)
			: this(durableQueue, jobAction, resultsInspector, pollingInterval, maxQueueItemsToPublishPerInterval, LocalScheduler.Default)
		{ }

		internal MonitoredJobQueue(ObservableDurableJobQueue<TInput, TPoison> durableQueue, Func<TInput, TOutput> jobAction, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector, 
			TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval, IScheduler scheduler)
		{
			if (null == durableQueue) { throw new ArgumentNullException("durableQueue"); }
			if (null == jobAction) { throw new ArgumentNullException("jobAction"); }
			if (null == resultsInspector) { throw new ArgumentNullException("resultsInspector"); }
			if (null == scheduler) { throw new ArgumentNullException("scheduler"); }
			if (pollingInterval < TimeSpan.FromSeconds(1)) { throw new ArgumentOutOfRangeException("pollingInterval", "must be at least one second"); }
			if (maxQueueItemsToPublishPerInterval <= 0) { throw new ArgumentOutOfRangeException("maxQueueItemsToPublishPerInterval", "must be at least one"); }

			this.durableQueue = durableQueue;
			this.monitor = new DurableJobQueueMonitor<TInput, TPoison>(durableQueue, pollingInterval, maxQueueItemsToPublishPerInterval, scheduler);
			this.jobQueue = new AutoJobExecutionQueue<TInput, TOutput>(scheduler, 10);
			this.resultJournaler = new JobResultJournaler<TInput, TOutput, TPoison>(jobQueue.WhenJobCompletes, resultsInspector, durableQueue, null, scheduler);

			this.subscription = monitor
				.SubscribeOn(scheduler)
				.Subscribe(input => jobQueue.Add(input, jobAction));
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
		public int MaxQueueItemsToPublishPerInterval
		{
			get { return this.monitor.MaxQueueItemsToPublishPerInterval; }
		}

		/// <summary>	Gets the polling interval. </summary>
		/// <value>	The polling interval. </value>
		public TimeSpan PollingInterval
		{
			get { return this.monitor.PollingInterval; }
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
				.Do(n => { var @false = false; }, () => manualResetEventSlim.Set())
				.Subscribe())
			{
				jobQueue.CancelOutstandingJobs();

				if (jobQueue.RunningCount != 0)
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

		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable queue factory and a job action. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the factory or action are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <param name="durableQueueFactory">	The durable queue factory. </param>
		/// <param name="jobAction">		  	The job action. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, Poison<TInput>> Create<TInput, TOutput>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction)
		{
			return new MonitoredJobQueue<TInput, TOutput, Poison<TInput>>(CreateQueue<TInput, Poison<TInput>>(durableQueueFactory),
				jobAction, JobResultInspector.FromJobSpecification(jobAction));
		}

		/// <summary>
		/// Creates a simplified IMonitoredJobQueue interface given a durable queue factory, a job action and a results inspector.
		/// </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the durable queue, job queue or results inspectors are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueueFactory">	A durable queue to which all job information is written in a fail-safe manner. </param>
		/// <param name="jobAction">		  	The job action. </param>
		/// <param name="resultsInspector">   	The results inspector. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction, resultsInspector);
		}

		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable queue factory and a job queue. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the durable queue, job queue or results inspectors are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueueFactory">	A durable queue to which all job information is written in a fail-safe manner. </param>
		/// <param name="jobAction">		  	The job action. </param>
		/// <param name="resultsInspector">   	The results inspector. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction, resultsInspector);
		}


		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable queue factory and a job action. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when the factory or action are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <param name="durableQueueFactory">					   	The durable queue factory. </param>
		/// <param name="jobAction">							   	The job action. </param>
		/// <param name="pollingInterval">						   	The polling interval. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	The maximum queue items to publish per interval. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, Poison<TInput>> Create<TInput, TOutput>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval)
		{
			return new MonitoredJobQueue<TInput, TOutput, Poison<TInput>>(CreateQueue<TInput, Poison<TInput>>(durableQueueFactory),
				jobAction, JobResultInspector.FromJobSpecification(jobAction));
		}

		/// <summary>
		/// Creates a simplified IMonitoredJobQueue interface given a durable queue factory, a job action and a results inspector.
		/// </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the durable queue, job queue or results inspectors are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueueFactory">					   	A durable queue to which all job information is written in a fail-safe
		/// 														manner. </param>
		/// <param name="jobAction">							   	The job action. </param>
		/// <param name="resultsInspector">						   	The results inspector. </param>
		/// <param name="pollingInterval">						   	The polling interval. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	The maximum queue items to publish per interval. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, IJobResultInspector<TInput, TOutput, TPoison> resultsInspector,
			TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction, resultsInspector);
		}

		/// <summary>	Creates a simplified IMonitoredJobQueue interface given a durable queue factory and a job queue. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the durable queue, job queue or results inspectors are null. </exception>
		/// <typeparam name="TInput"> 	Type of the input. </typeparam>
		/// <typeparam name="TOutput">	Type of the output. </typeparam>
		/// <typeparam name="TPoison">	Type of the poison. </typeparam>
		/// <param name="durableQueueFactory">					   	A durable queue to which all job information is written in a fail-safe
		/// 														manner. </param>
		/// <param name="jobAction">							   	The job action. </param>
		/// <param name="">										   	The. </param>
		/// <param name="resultsInspector">						   	The results inspector. </param>
		/// <param name="pollingInterval">						   	The polling interval. </param>
		/// <param name="maxQueueItemsToPublishPerInterval">	The maximum queue items to publish per interval. </param>
		/// <returns>	An IMonitoredJobQueue instance that simplifies queuing inputs. </returns>
		public static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector, 
			TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction, resultsInspector);
		}

		internal static IMonitoredJobQueue<TInput, TOutput, TPoison> Create<TInput, TOutput, TPoison>(IDurableJobQueueFactory durableQueueFactory,
			Func<TInput, TOutput> jobAction, Func<JobResult<TInput, TOutput>, JobQueueAction<TPoison>> resultsInspector, 
			TimeSpan pollingInterval, int maxQueueItemsToPublishPerInterval, IScheduler scheduler)
		{
			return new MonitoredJobQueue<TInput, TOutput, TPoison>(CreateQueue<TInput, TPoison>(durableQueueFactory), jobAction, JobResultInspector.FromInspector(resultsInspector),
				pollingInterval, maxQueueItemsToPublishPerInterval, scheduler);
		}
	}
}