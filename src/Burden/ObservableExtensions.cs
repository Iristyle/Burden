using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;

namespace Burden
{
	/// <summary>	Adds some additional methods of creating Observables.  </summary>
	/// <remarks>	7/8/2011. </remarks>
	public static class ObservableExtensions
	{
		/// <summary>
		/// Creates a new observable from a standard callback pattern type API, for instance RestSharp, with ExecuteAsync(request, (response)
		/// =&gt; {}).
		/// Stolen from <a href="https://groups.google.com/forum/#!topic/restsharp/R7hMDz1YRU4" />
		/// </summary>
		/// <remarks>	7/8/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either the parameter or asynchronousCall are null. </exception>
		/// <typeparam name="T">	  	Generic type parameter. </typeparam>
		/// <typeparam name="TResult">	Type of the result. </typeparam>
		/// <param name="parameter">	   	The parameter. </param>
		/// <param name="asynchronousCall">	The asynchronous call. </param>
		/// <returns>	An IObservable. </returns>
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Nested generics, while advanced, are perfectly acceptable within Actions, especially in this case where we leverage compiler inference")]
		public static IObservable<TResult> FromCallbackPattern<T, TResult>(T parameter, Action<T, Action<TResult>> asynchronousCall)
		{
			if (null == parameter) { throw new ArgumentNullException("parameter"); }
			if (null == asynchronousCall) { throw new ArgumentNullException("asynchronousCall"); }

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