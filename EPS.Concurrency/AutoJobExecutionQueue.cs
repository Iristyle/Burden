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
		private readonly int maxToAutoStart;

		/// <summary>	Creates a new AutoJobExecutionQueue that will automatically always have. </summary>
		/// <remarks>	7/15/2011. </remarks>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the number of concurrent jobs allowed is less than one or greater than the maximum allowed. </exception>
		/// <param name="maxConcurrent">	The maximum concurrent number of jobs to allow to execute. </param>
		public AutoJobExecutionQueue(int maxConcurrent)
			: base(maxConcurrent)
		{
			this.maxToAutoStart = maxConcurrent;
		}

		//this allow maxToAutoStart of 0 with a maxConcurrent of a higher value for the sake of internal tests
		internal AutoJobExecutionQueue(IScheduler scheduler, int maxConcurrent, int maxToAutoStart)
			: base(scheduler, maxConcurrent)
		{ 
			this.maxToAutoStart = maxToAutoStart;
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
			StartUpTo(maxToAutoStart);
			return whenCompletes;
		}

		protected override void OnJobCompleted(Job job, TJobOutput jobResult, Exception error)
		{
			base.OnJobCompleted(job, jobResult, error);
			if (error != null)
				LocalScheduler.Default.Schedule(() => StartUpTo(maxToAutoStart));
			else
				StartUpTo(maxToAutoStart);
		}
	}
}