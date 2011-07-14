using System;

namespace EPS.Concurrency
{
	/// <summary>	Describes a failed job by also capturing its input.  </summary>
	/// <remarks>	7/13/2011. </remarks>
	/// <typeparam name="TJobInput">	Type of the job input. </typeparam>
	public class JobQueueException<TJobInput> : Exception
	{
		public TJobInput Input { get; private set; }
			
		/// <summary>	Initializes a new instance of the JobQueueException class, where an input is expected. </summary>
		/// <remarks>	7/14/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when one or more required arguments are null. </exception>
		/// <param name="input">		 	The input. </param>
		/// <param name="innerException">	The inner exception. </param>
		public JobQueueException(TJobInput input, Exception innerException)
			:base("The execution of job with given input failed", innerException)
		{
			if (null == input) { throw new ArgumentNullException("input"); }

			Input = input;
		}
	}
}