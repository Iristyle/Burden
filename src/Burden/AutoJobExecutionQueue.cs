using System;
using System.Reactive.Concurrency;
using System.Diagnostics.CodeAnalysis;

namespace Burden
{
	/// <summary>	Queue that will automatically keep a specified minimum number of jobs running as long as they're available. </summary>
	/// <remarks>	Originally based on code from <a href="http://rxpowertoys.codeplex.com/" /> but modified heavily for jobs with inputs and outputs. </remarks>
	/// <typeparam name="TJobInput"> 	Type of the job input. </typeparam>
	/// <typeparam name="TJobOutput">	Type of the job output. </typeparam>
	public class AutoJobExecutionQueue<TJobInput, TJobOutput> 
		: ManualJobExecutionQueue<TJobInput, TJobOutput>
	{		
		private readonly int _maxToAutoStart;

		/// <summary>	Creates a new AutoJobExecutionQueue that will automatically always have. </summary>
		/// <remarks>	7/15/2011. </remarks>
		/// <exception cref="ArgumentOutOfRangeException">	Thrown when the number of concurrent jobs allowed is less than one or greater than the maximum allowed. </exception>
		/// <param name="maxConcurrent">	The maximum concurrent number of jobs to allow to execute. </param>
		public AutoJobExecutionQueue(int maxConcurrent)
			: base(maxConcurrent)
		{
			this._maxToAutoStart = maxConcurrent;
		}

		//this allow maxToAutoStart of 0 with a maxConcurrent of a higher value for the sake of internal tests
		[SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule", Justification = "Used by test classes to change scheduler")]
		internal AutoJobExecutionQueue(IScheduler scheduler, int maxConcurrent, int maxToAutoStart)
			: base(scheduler, maxConcurrent)
		{ 
			this._maxToAutoStart = maxToAutoStart;
		}

		/// <summary>	Adds a job matching a given input / output typing and an input value, and will auto-start the job, running only up to the maxConcurrent number of jobs specified. </summary>
		/// <remarks>	7/17/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when asyncStart is null. </exception>
		/// <exception cref="ObjectDisposedException">	Thrown when the object has been disposed of, and is therefore no longer accepting new
		/// 												jobs or publishing notifications. </exception>
		/// <param name="input">	 	The input. </param>
		/// <param name="asyncStart">	The asynchronous observable action to perform. </param>
		/// <returns>	A sequence of Observable JobResult instances. </returns>
		public override IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, IObservable<TJobOutput>> asyncStart)
		{
			ThrowIfDisposed();
			if (null == asyncStart) { throw new ArgumentNullException("asyncStart"); }

			var whenCompletes = base.Add(input, asyncStart);
			StartAsManyAs(_maxToAutoStart);
			return whenCompletes;
		}

		/// <summary>
		/// Based on the base class, fires OnNext for our CompletionHandler, passing along the appropriate JobResult based on whether or not
		/// there was an error. Additionally attempts to execute additional jobs if found based on our maxToAutoStart setup.
		/// </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <param name="job">			The job. </param>
		/// <param name="jobResult">	The job result. </param>
		/// <param name="exception">		The error, if one exists. </param>
		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Job disposables are tracked and later disposed as necessary")]		
		protected override void OnJobCompleted(Job job, TJobOutput jobResult, Exception exception)
		{
			base.OnJobCompleted(job, jobResult, exception);
			if (exception != null)
				LocalScheduler.Default.Schedule(() => StartAsManyAs(_maxToAutoStart));
			else
				StartAsManyAs(_maxToAutoStart);
		}
	}
}