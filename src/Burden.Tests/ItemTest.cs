using System;
using Ploeh.AutoFixture;
using Xunit;

namespace Burden.Tests
{
	public class ItemTest
	{
		private Fixture fixture = new Fixture();

		[Fact]
		public void None_SuccessIsFalse_OnValueTypes()
		{
			Assert.False(Item.None<int>().Success);
		}

		[Fact]
		public void None_SuccessIsFalse_OnReferenceTypes()
		{
			Assert.False(Item.None<string>().Success);
		}

		[Fact]
		public void None_ValueIsDefault_OnValueTypes()
		{
			Assert.Equal(default(int), Item.None<int>().Value);
		}

		[Fact]
		public void None_ValueIsNull_OnReferenceTypes()
		{
			Assert.Equal(null, Item.None<string>().Value);
		}

		[Fact]
		public void From_DoesNotThrow_OnDefaultValueTypes()
		{
			Assert.DoesNotThrow(() => Item.From(default(int)));
		}

		[Fact]
		public void From_Throws_OnNullReferenceType()
		{
			Assert.Throws<ArgumentNullException>(() => Item.From(null as string));
		}

		[Fact]
		public void From_SuccessIsTrue_OnValueType()
		{
			Assert.True(Item.From(fixture.CreateAnonymous<int>()).Success);
		}

		[Fact]
		public void From_SuccessIsTrue_OnReferenceType()
		{
			Assert.True(Item.From(fixture.CreateAnonymous<string>()).Success);
		}
	}
}