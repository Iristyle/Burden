using System;
using System.Reactive.Concurrency;

namespace EPS.Concurrency
{
	public class AutoJobQueue<TJobInput, TJobOutput> : ManualJobQueue<TJobInput, TJobOutput>
	{
		//http://rxpowertoys.codeplex.com/
		int maxConcurrent;

		public AutoJobQueue(int maxConcurrent)
			: this(Scheduler.TaskPool, maxConcurrent)
		{ }

		internal AutoJobQueue(IScheduler scheduler, int maxConcurrent)
		{
			if (maxConcurrent < 1)
			{
				throw new ArgumentOutOfRangeException("maxConcurrent");
			}

			this.maxConcurrent = maxConcurrent;
		}

		public override IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, IObservable<TJobOutput>> asyncStart)
		{
			if (null == asyncStart) { throw new ArgumentNullException("asyncStart"); }

			return Add(input, asyncStart, true);
		}

		public IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, IObservable<TJobOutput>> asyncStart, bool autoStart)
		{
			if (null == asyncStart) { throw new ArgumentNullException("asyncStart"); }

			var whenCompletes = base.Add(input, asyncStart);
			if (autoStart)
				StartUpTo(maxConcurrent);
			return whenCompletes;
		}

		protected override void OnJobCompleted(Job job, TJobOutput jobResult, Exception error)
		{
			base.OnJobCompleted(job, jobResult, error);
			if (error != null)
				Scheduler.TaskPool.Schedule(() => StartUpTo(maxConcurrent));
			else
				StartUpTo(maxConcurrent);
		}
	}
}
