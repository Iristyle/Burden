using System;
using System.Diagnostics.CodeAnalysis;

namespace EPS.Concurrency
{
	/// <summary>	Describes the details of a job in terms of input and output, or input and resultant Exception. </summary>
	/// <remarks>
	/// This class represents immutable details about a job result, guaranteeing that if the JobResultType is Completed, that input and
	/// output will be non-null (<see cref="System.Reactive.Unit"/> is allowed). If JobResultType is Error, Input and Exception are
	/// guaranteed to be non-null <see cref="System.Reactive.Unit"/> is allowed).
	/// </remarks>
	/// <typeparam name="TJobInput"> 	Type of the input. </typeparam>
	/// <typeparam name="TJobOutput">	Type of the output. </typeparam>
	public class JobResult<TJobInput, TJobOutput>
	{
		private readonly TJobInput _input;
		private readonly TJobOutput _output;
		private readonly JobResultType _jobResultType;
		private readonly Exception _exception;

		/// <summary>	Constructs a new JobResult given an input and output. </summary>
		/// <remarks>	7/14/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either input or output is null. </exception>
		/// <param name="input"> 	The input result. </param>
		/// <param name="output">	The output. </param>
		[SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule", Justification = "Used by static factory class")]
		internal JobResult(TJobInput input, TJobOutput output)
		{
			if (null == input) { throw new ArgumentNullException("input"); }
			if (null == output) { throw new ArgumentNullException("output"); }

			this._jobResultType = JobResultType.Completed;
			this._input = input;
			this._output = output;
		}

		/// <summary>	Constructs a new JobResult given an input and exception. </summary>
		/// <remarks>	7/14/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when either input or exception is null. </exception>
		/// <param name="input">		The input result. </param>
		/// <param name="exception">	The exception. </param>
		[SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule", Justification = "Used by static factory class")]
		internal JobResult(TJobInput input, Exception exception)
		{
			if (null == input) { throw new ArgumentNullException("input"); }
			if (null == exception) { throw new ArgumentNullException("exception"); }

			this._jobResultType = JobResultType.Error;
			this._input = input;
			this._exception = exception;
		}

		/// <summary>	Gets the type of the job result. </summary>
		/// <value>	The type. </value>
		public JobResultType ResultType
		{
			get { return _jobResultType; }
		}

		/// <summary>	Gets the job input. </summary>
		/// <value>	The job input. </value>
		public TJobInput Input
		{
			get { return _input; }
		}

		/// <summary>	Gets the job output. </summary>
		/// <value>	The job output. </value>
		public TJobOutput Output
		{
			get { return _output; }
		}

		/// <summary>	Gets the jobs exception if there was one. </summary>
		/// <value>	The exception. </value>
		public Exception Exception
		{
			get { return _exception; }
		}

		/// <summary>
		/// Convenience overload to cast a JobResult{TJobInput, object} generated by JobResult.CreateOnError to a JobResult{TJobInput,
		/// TJobOutput} implicitly.
		/// </summary>
		/// <remarks>	7/14/2011. </remarks>
		/// <param name="input">	The input result. </param>
		/// <returns>	The result of the operation. </returns>
		[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Supplying these static methods on a generic class makes them very difficult to use")]
		public static implicit operator JobResult<TJobInput, TJobOutput>(JobResult<TJobInput, object> input)
		{
			if (null == input) return null;

			return new JobResult<TJobInput, TJobOutput>(input._input, input._exception);
		}
	}

	/// <summary>
	/// A simple JobResult factory the hides the details of the generic JobResult behind methods that can use compiler inference.
	/// </summary>
	/// <remarks>	7/8/2011. </remarks>
	public static class JobResult
	{
		/// <summary>	Convenience factory method for creating a job detail object without specifying generic parameters. </summary>
		/// <remarks>	7/8/2011. </remarks>
		/// <typeparam name="TJobInput"> 	Type of the job input. </typeparam>
		/// <typeparam name="TJobOutput">	Type of the job output. </typeparam>
		/// <param name="input"> 	The input. To specify no input, use the <see cref="System.Reactive.Unit"/> type. </param>
		/// <param name="output">	The output. To specify no output, use the <see cref="System.Reactive.Unit"/> type. </param>
		/// <returns>	A new JobResult instance. </returns>
		/// <exception cref="ArgumentNullException">	Thrown when input or output are null. </exception>
		public static JobResult<TJobInput, TJobOutput> CreateOnCompletion<TJobInput, TJobOutput>(TJobInput input, TJobOutput output)
		{
			return new JobResult<TJobInput, TJobOutput>(input, output);
		}

		/// <summary>	Convenience factory method for creating a job detail object without specifying generic parameters. </summary>
		/// <remarks>	7/14/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when input or exception are null. </exception>
		/// <typeparam name="TJobInput">	Type of the job input. </typeparam>
		/// <param name="input">		The input. To specify no input, use the <see cref="System.Reactive.Unit"/> type. </param>
		/// <param name="exception">	The exception, which cannot be null. </param>
		/// <returns>	A new JobResult instance. </returns>
		public static JobResult<TJobInput, object> CreateOnError<TJobInput>(TJobInput input, Exception exception)
		{
			return new JobResult<TJobInput, object>(input, exception);
		}
	}
}