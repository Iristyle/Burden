using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace EPS.Concurrency
{
	class ObservableExtensions
	{
		public static IObservable<TResult> FromCallbackPattern<T,TResult>(T parameter, Action<T, Action<TResult>> asynchronousCall)
		{
			//https://groups.google.com/forum/#!topic/restsharp/R7hMDz1YRU4
			return Observable
				.Create<TResult>(observer =>
				{
					var subscribed = true;
					asynchronousCall(parameter, value =>
					{
						if (!subscribed) return;
						
						observer.OnNext(value);
						observer.OnCompleted();
					});
					
					return () => { subscribed = false; };
				});
		} 
	}
}
