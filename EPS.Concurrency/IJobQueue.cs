using System;
using System.Reactive;

namespace EPS.Concurrency
{
	public interface IJobQueue : IJobQueue<Unit>
	{
		//http://rxpowertoys.codeplex.com/
		IObservable<Unit> Add(Action action);
	}

	public interface IJobQueue<T>
	{
		//http://rxpowertoys.codeplex.com/

		IObservable<Notification<T>> WhenJobCompletes { get; }
		IObservable<T> WhenQueueEmpty { get; }
		IObservable<Exception> WhenJobFails { get; }

		int RunningCount { get; }
		int QueuedCount { get; }

		IObservable<T> Add(Func<T> action);
		IObservable<T> Add(Func<IObservable<T>> asyncStart);

		bool StartNext();
		int StartUpTo(int maxConcurrentlyRunning);

		void CancelOutstandingJobs();
	}
}