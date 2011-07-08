using System;
using System.Reactive;

namespace EPS.Concurrency
{	
	public interface IJobQueue<TJobInput, TJobOutput>
	{
		//http://rxpowertoys.codeplex.com/

		IObservable<Notification<JobResult<TJobInput, TJobOutput>>> WhenJobCompletes { get; }
		IObservable<TJobInput> WhenQueueEmpty { get; }

		int RunningCount { get; }
		int QueuedCount { get; }

		IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, TJobOutput> action);
		IObservable<JobResult<TJobInput, TJobOutput>> Add(TJobInput input, Func<TJobInput, IObservable<TJobOutput>> asyncStart);

		bool StartNext();
		int StartUpTo(int maxConcurrentlyRunning);

		void CancelOutstandingJobs();
	}
}