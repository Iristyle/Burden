using System;
using System.Reactive;

namespace EPS.Concurrency
{
	public interface IJobQueue
	{
		//http://rxpowertoys.codeplex.com/

		IObservable<Notification<Unit>> WhenJobCompletes { get; }
		IObservable<Unit> WhenQueueEmpty { get; }
		IObservable<Exception> WhenJobFails { get; }

		int RunningCount { get; }
		int QueuedCount { get; }

		IObservable<Unit> Add(Action action);
		IObservable<Unit> Add(Func<IObservable<Unit>> asyncStart);

		bool StartNext();
		int StartUpTo(int maxConcurrentlyRunning);

		void CancelOutstandingJobs();
	}
}