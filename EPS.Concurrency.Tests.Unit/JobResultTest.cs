using System;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class JobResultTest
	{
		[Fact]
		public void CreateOnError_ThrowsOnNullInput()
		{
			Assert.Throws<ArgumentNullException>(() => JobResult.CreateOnError(null as object, new ArgumentException()));
		}

		[Fact]
		public void CreateOnError_ThrowsOnNullException()
		{
			Assert.Throws<ArgumentNullException>(() => JobResult.CreateOnError(3, null));
		}

		[Fact]
		public void CreateOnError_HasJobResultTypeError()
		{
			var result = JobResult.CreateOnError(3, new ArgumentException());
			Assert.Equal(JobResultType.Error, result.Type);
		}

		[Fact]
		public void CreateOnError_RetrievesInputCorrectly()
		{
			var input = new object();
			var result = JobResult.CreateOnError(input, new ArgumentNullException());
			Assert.Same(input, result.Input);
		}

		[Fact]
		public void CreateOnError_RetrievesExceptionCorrectly()
		{
			var exception = new ArgumentException();
			var result = JobResult.CreateOnError("foo", exception);
			Assert.Same(exception, result.Exception);
		}

		[Fact]
		public void CreateOnCompletion_ThrowsOnNullInput()
		{
			Assert.Throws<ArgumentNullException>(() => JobResult.CreateOnCompletion(null as object, 3));
		}

		[Fact]
		public void CreateOnCompletion_ThrowsOnNullOutput()
		{
			Assert.Throws<ArgumentNullException>(() => JobResult.CreateOnCompletion(3, null as object));
		}

		[Fact]
		public void CreateOnCompletion_HasJobResultTypeCompletion()
		{
			var result = JobResult.CreateOnCompletion(3, "foo");
			Assert.Equal(JobResultType.Completed, result.Type);
		}

		[Fact]
		public void CreateOnCompletion_RetrievesInputCorrectly()
		{
			var input = new object();
			var result = JobResult.CreateOnCompletion(input, "foo");
			Assert.Same(input, result.Input);
		}

		[Fact]
		public void CreateOnCompletion_RetrievesOutputCorrectly()
		{
			var output = "check check";
			var result = JobResult.CreateOnCompletion("foo", output);
			Assert.Same(output, result.Output);
		}

		[Fact]
		public void JobResult_ExplicitlyCastsOutputTypeObjectToAnyOutputType()
		{
			var errorResult = JobResult.CreateOnError("foo", new ArgumentNullException());
			var castResult = (JobResult<string, int>)errorResult;

			Assert.NotNull(castResult);
		}

		[Fact]
		public void JobResult_ExplicitlyCastsMaintainsJobType()
		{
			var errorResult = JobResult.CreateOnError("foo", new ArgumentNullException());
			var castResult = (JobResult<string, int>)errorResult;

			Assert.Equal(errorResult.Type, castResult.Type);
		}

		[Fact]
		public void JobResult_ExplicitlyCastsMaintainsInputValue()
		{
			var errorResult = JobResult.CreateOnError("foo", new ArgumentNullException());
			var castResult = (JobResult<string, int>)errorResult;

			Assert.Equal(errorResult.Input, castResult.Input);
		}

		[Fact]
		public void JobResult_ExplicitlyCastsMaintainsExceptionValue()
		{
			var errorResult = JobResult.CreateOnError("foo", new ArgumentNullException());
			var castResult = (JobResult<string, int>)errorResult;

			Assert.Equal(errorResult.Exception, castResult.Exception);
		}

		[Fact]
		public void JobResult_ExplicitlyCastsHasDefaultOutputValue()
		{
			var errorResult = JobResult.CreateOnError("foo", new ArgumentNullException());
			var castResult = (JobResult<string, int>)errorResult;

			Assert.Equal(default(int), castResult.Output);
		}
	}
}