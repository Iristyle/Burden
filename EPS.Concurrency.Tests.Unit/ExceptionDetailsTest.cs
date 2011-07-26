using System;
using Ploeh.AutoFixture;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class ExceptionDetailsTest
	{
		private IFixture fixture = new Fixture();

		[Fact]
		public void Constructor_ThrowsOnNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new ExceptionDetails(null));
		}

		private ArgumentNullException ThrowToGetStackTrace()
		{
			try
			{
				throw new ArgumentNullException(fixture.CreateAnonymous<string>(), fixture.CreateAnonymous<string>()) 
					{ Source = fixture.CreateAnonymous<string>() };
			}
			catch (ArgumentNullException ex)
			{
				return ex;
			}
		}

		[Fact]
		public void Message_IsExpected()
		{
			var exception = ThrowToGetStackTrace();
			var details = new ExceptionDetails(exception);

			Assert.Equal(exception.Message, details.Message);
		}

		[Fact]
		public void Source_IsExpected()
		{
			var exception = ThrowToGetStackTrace();
			var details = new ExceptionDetails(exception);

			Assert.Equal(exception.Source, details.Source);
		}

		[Fact]
		public void StackTrace_IsExpected()
		{
			var exception = ThrowToGetStackTrace();
			var details = new ExceptionDetails(exception);

			Assert.Equal(exception.StackTrace, details.StackTrace);
		}

		[Fact]
		public void ExceptionType_IsExpected()
		{
			var exception = ThrowToGetStackTrace();
			var details = new ExceptionDetails(exception);

			Assert.Equal(typeof(Exception).Name, details.ExceptionType);
		}
	}
}
