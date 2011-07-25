using System;
using System.Reactive.Concurrency;

namespace EPS.Concurrency
{
	/// <summary>	Queue that will automatically keep a specified minimum number of jobs running as long as they're available. </summary>
	/// <remarks>	Originally based on code from <a href="http://rxpowertoys.codeplex.com/" /> but modified heavily for jobs with inputs and outputs. </remarks>
	/// <typeparam name="TJobInput"> 	Type of the job input. </typeparam>
	/// <typeparam name="TJobOutput">	Type of the job output. </typeparam>
	public class AutoJobExecutionQueue<TJobInput, TJobOutput> 
		: ManualJobExecutionQueue<TJobInput, TJobOutput>
	{		
		private readonly int maxConcurrent;

		/// <summary>	Creates a new AutoJobExecutionQueue that will automatically always have. </summary>
		/// <remarks>	7/15/2011. </remarks>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when one or more arguments are outside the required range. </exception>
		/// <param name="maxConcurrent">	The maximum concurrent number of jobs to allow to execute. </param>
		public AutoJobExecutionQueue(int maxConcurrent)
#if SILVERLIGHT
			: this(Scheduler.ThreadPool, maxConcurrent)
#else
			: this(Scheduler.TaskPool, maxConcurrent)
#endif
		{
			if (maxConcurrent < 1)
			{
				throw new ArgumentOutOfRangeException("maxConcurrent");
			}
		}

		internal AutoJobExecutionQueue(IScheduler scheduler, int maxConcurrent)
			: base(scheduler)
		{
			//we'll allow maxConcurrent of 0 for the sake of internal tests
			this.maxConcurrent = maxConcurrent;
		}

		/// <summary>	Adds a job matching a given input / output typing and an input value, and will auto-start the job, running only up to the maxConcurrent number of jobs specified. </summary>
		/// <remarks>	7/17/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when asyncStart is null. </exception>
		/// <param name="input">	 	The input. </param>
		/// <param name="asyncStart">	The asynchronous observable action to perform. </param>
		/// <returns>	A sequence of Observable JobResult instances. </returns>
		public override IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, IObservable<TJobOutput>> asyncStart)
		{
			if (null == asyncStart) { throw new ArgumentNullException("asyncStart"); }

			var whenCompletes = base.Add(input, asyncStart);
			StartUpTo(maxConcurrent);
			return whenCompletes;
		}

		protected override void OnJobCompleted(Job job, TJobOutput jobResult, Exception error)
		{
			base.OnJobCompleted(job, jobResult, error);
			if (error != null)
#if SILVERLIGHT
			Scheduler.ThreadPool.Schedule(() => StartUpTo(maxConcurrent));
#else
			Scheduler.TaskPool.Schedule(() => StartUpTo(maxConcurrent));
#endif
				
			else
				StartUpTo(maxConcurrent);
		}
	}
}