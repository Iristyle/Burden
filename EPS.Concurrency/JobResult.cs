using System;

namespace EPS.Concurrency
{
	/// <summary>	Describes the details of a job in terms of inupt and output.  </summary>
	/// <remarks>	7/8/2011. </remarks>
	/// <typeparam name="TJobInput"> 	Type of the input. </typeparam>
	/// <typeparam name="TJobOutput">	Type of the output. </typeparam>
	public class JobResult<TJobInput, TJobOutput>
	{
		private readonly TJobInput input;
		private readonly TJobOutput output;

		internal JobResult(TJobInput input, TJobOutput output)
		{
			this.input = input;
			this.output = output;
		}

		/// <summary>	Gets the job input. </summary>
		/// <value>	The job input. </value>
		public TJobInput Input
		{
			get { return input; }
		}

		/// <summary>	Gets the job output. </summary>
		/// <value>	The job output. </value>
		public TJobOutput Output
		{
			get { return output; }
		}
	
	}

	/// <summary>	A simple JobResult factory hiding the details of using generics.  </summary>
	/// <remarks>	7/8/2011. </remarks>
	public static class JobResult
	{
		/// <summary>	Convenience factory method for creating a job detail object without specifying generic parameters </summary>
		/// <remarks>	7/8/2011. </remarks>
		/// <param name="input"> 	The input. </param>
		/// <param name="output">	The output. </param>
		/// <returns>	A new JobResult instance. </returns>
		public static JobResult<TJobInput, TJobOutput> Create<TJobInput, TJobOutput>(TJobInput input, TJobOutput output)
		{
			return new JobResult<TJobInput, TJobOutput>(input, output);
		}
	}
}