using System;

namespace Burden
{
	/// <summary>	Describes all useful information from an Exception, in a format thats easy to serialize and compare.  </summary>
	/// <remarks>	7/19/2011. </remarks>
	public class ExceptionDetails
	{
		/// <summary>	Gets a message that describes the current exception. </summary>
		/// <value>	The error message that explains the reason for the exception, or an empty string. </value>
		public string Message { get; private set; }

		/// <summary>	Gets or sets the name of the application or the object that causes the error. </summary>
		/// <value>	The name of the application or the object that causes the error. </value>
		public string Source { get; private set; }

		/// <summary>	Gets a string representation of the immediate frames on the call stack. </summary>
		/// <value>	A string that describes the immediate frames of the call stack. </value>
		public string StackTrace { get; private set; }

		/// <summary>	Gets the actual type of the exception. </summary>
		/// <value>	The type of the exception. </value>
		public string ExceptionType { get; private set; }

		/// <summary>	Constructs a new ExceptionDetails instance given an existing Exception. </summary>
		/// <remarks>	7/19/2011. </remarks>
		/// <exception cref="ArgumentException">	Thrown when the exception is null. </exception>
		/// <param name="exception">	The exception. </param>
		public ExceptionDetails(Exception exception)
		{
			if (null == exception)
			{
				throw new ArgumentNullException("exception");
			}

			this.Message = exception.Message;
			this.StackTrace = exception.StackTrace;
			this.ExceptionType = typeof(Exception).Name;
			this.Source = exception.Source;
		}
	}
}