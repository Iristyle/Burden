using System;
using System.Reactive;
using System.Reactive.Linq;

namespace EPS.Concurrency
{
	//http://rxpowertoys.codeplex.com/
	public class ManualJobQueue : ManualJobQueue<Unit>, IJobQueue
	{
		public IObservable<Unit> Add(Action action)
		{
			if (null == action) { throw new ArgumentNullException("action"); }

			return Add(Observable.ToAsync(action));
		}
	}
}