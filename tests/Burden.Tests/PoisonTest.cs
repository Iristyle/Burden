using System;
using EqualityComparer;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class PoisonTest
	{
		[Fact]
		public void Constructor_Throws_OnNullInput_ForReferenceType()
		{
			Assert.Throws<ArgumentNullException>(() => new Poison<string>(null, new ArgumentNullException()));
		}

		[Fact]
		public void Constructor_Throws_OnNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new Poison<string>("foo", null));
		}
		
		[Fact]
		public void Constructor_StoresValueType()
		{
			int value = 42;
			var poison = new Poison<int>(value, new ArgumentNullException());

			Assert.Equal(value, poison.Input);
		}

		[Fact]
		public void Constructor_StoresReferenceType()
		{
			string value = "test";
			var poison = new Poison<string>(value, new ArgumentNullException());

			Assert.Equal(value, poison.Input);
		}

		[Fact]
		public void Constructor_StoresException()
		{
			var exception = new ArgumentNullException("foo");
			var poison = new Poison<int>(42, exception);

			Assert.Equal(new ExceptionDetails(exception), poison.Exception, GenericEqualityComparer<ExceptionDetails>.ByAllMembers());
		}
	}
}