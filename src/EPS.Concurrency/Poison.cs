using System;

namespace EPS.Concurrency
{
	/// <summary>	A generic class representing an input annotated with an Exception.  </summary>
	/// <remarks>	7/24/2011. </remarks>
	/// <typeparam name="T">	The Type of the input. </typeparam>
	public class Poison<T>
	{
		/// <summary>	Gets the input. </summary>
		/// <value>	The input. </value>
		public T Input { get; private set; }
		
		/// <summary>	Gets the exception details. </summary>
		/// <value>	The exception. </value>
		public ExceptionDetails Exception { get; private set; }
		
		/// <summary>	Constructor. </summary>
		/// <remarks>	7/24/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when T is a reference type and input is null OR when exception is null. </exception>
		/// <param name="input">		The input. </param>
		/// <param name="exception">	The exception, which will be wrapped up as an ExceptionDetails. </param>
		public Poison(T input, Exception exception)
		{
			if (!typeof(T).IsValueType && object.ReferenceEquals(input, null))
			{
				throw new ArgumentNullException("input");
			}
			if (null == exception) { throw new ArgumentNullException("exception"); }

			this.Input = input;
			this.Exception = new ExceptionDetails(exception);
		}
	}
}