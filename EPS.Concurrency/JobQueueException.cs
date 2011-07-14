using System;
using System.Reactive;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

namespace EPS.Concurrency
{
	/// <summary>	Describes a failed job by also capturing its input.  </summary>
	/// <remarks>	7/13/2011. </remarks>
	/// <typeparam name="TJobInput">	Type of the job input. </typeparam>
	public class JobQueueException<TJobInput> : Exception
	{
		public TJobInput Input { get; private set; }
			
		/// <summary>
		/// Initializes a new instance of the JobQueueException class.
		/// </summary>
		public JobQueueException(TJobInput input, Exception innerException)
			:base("The execution of job with given input failed", innerException)
		{
			Input = input;
		}
	}
}