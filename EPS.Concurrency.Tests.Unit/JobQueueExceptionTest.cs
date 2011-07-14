using System;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class JobQueueExceptionTest
	{
		[Fact]
		public void Constructor_ThrowsOnNullInput()
		{
			Assert.Throws<ArgumentNullException>(() => new JobQueueException<object>(null, new ArgumentException()));
		}

		[Fact]
		public void Constructor_StoresInputForLaterRetrieval()
		{
			var o = new object();
			var exception = new JobQueueException<object>(o, new ArgumentException());

			Assert.Same(o, exception.Input);
		}

		[Fact]
		public void Constructor_StoresExceptionAsInnerExceptionForLaterRetrieval()
		{
			var argumentException = new ArgumentException();
			var exception = new JobQueueException<object>(new object(), argumentException);

			Assert.Same(argumentException, exception.InnerException);
		}
	}
}