using System;

namespace EPS.Concurrency
{
	/// <summary>	Defines some basic parameters around how a job queue is monitored.  </summary>
	/// <remarks>	8/14/2011. </remarks>
	public class MonitoredJobQueueConfiguration
	{
		/// <summary>	Gets the maximum concurrent jobs to execute. </summary>
		/// <value>	The maximum concurrent jobs to execute. </value>
		public int MaxConcurrentJobsToExecute { get; private set; }

		/// <summary>	Gets the polling interval. </summary>
		/// <value>	The polling interval. </value>
		public TimeSpan PollingInterval { get; private set; }
	
		/// <summary>	Gets the maximum queue items to publish per interval. </summary>
		/// <value>	The maximum queue items to publish per interval. </value>
		public int MaxQueueItemsToPublishPerInterval { get; private set; }

		/// <summary>	Constructor. </summary>
		/// <remarks>	8/14/2011. </remarks>
		/// <param name="maxConcurrentJobsToExecute">			The maximum concurrent jobs to execute. </param>
		/// <param name="pollingInterval">						The polling interval. </param>
		/// <param name="maxJobsToReadFromQueuePerInterval">	The maximum jobs to read from queue per interval. </param>
		public MonitoredJobQueueConfiguration(int maxConcurrentJobsToExecute, TimeSpan pollingInterval,
			int maxJobsToReadFromQueuePerInterval)
		{
			//validation of parameters is handled by methods accepting this object
			MaxConcurrentJobsToExecute = maxConcurrentJobsToExecute;
			PollingInterval = pollingInterval;
			MaxQueueItemsToPublishPerInterval = maxJobsToReadFromQueuePerInterval;
		}

		/// <summary>	Constructor. </summary>
		/// <remarks>
		/// The polling interval will automatically be set as DurableJobQueueMonitor.DefaultPollingInterval.  The number of jobs to take per
		/// interval is automatically set at a rate of 2 jobs /second.
		/// </remarks>
		/// <param name="maxConcurrentJobsToExecute">	The maximum concurrent jobs to execute. </param>
		public MonitoredJobQueueConfiguration(int maxConcurrentJobsToExecute)
			: this(maxConcurrentJobsToExecute, DurableJobQueueMonitor.DefaultPollingInterval, GetEstimatedJobsToExecutePerInterval(DurableJobQueueMonitor.DefaultPollingInterval, maxConcurrentJobsToExecute))
		{ }

		/// <summary>	Constructor. </summary>
		/// <remarks>	The number of jobs to take per interval is automatically set at a rate of 2 jobs /second. </remarks>
		/// <param name="maxConcurrentJobsToExecute">	The maximum concurrent jobs to execute. </param>
		/// <param name="pollingInterval">			 	The polling interval. </param>
		public MonitoredJobQueueConfiguration(int maxConcurrentJobsToExecute, TimeSpan pollingInterval)
			: this(maxConcurrentJobsToExecute, pollingInterval, GetEstimatedJobsToExecutePerInterval(pollingInterval, maxConcurrentJobsToExecute))
		{ }

		private static int GetEstimatedJobsToExecutePerInterval(TimeSpan interval, int maxConcurrency)
		{
			//TODO: 8-1-2011 -- our base assumption, which is sure to be wrong, is that an avg job takes .5 seconds to complete, so grab that many + a 20% fudge factor
			return Convert.ToInt32(interval.TotalSeconds * 2 * maxConcurrency * 12 / 10);
		}
	}
}