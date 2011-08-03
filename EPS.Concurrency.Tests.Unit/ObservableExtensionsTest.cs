using System;
using System.Reactive.Linq;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class ObservableExtensionsTest
	{
		class Foo {}
		class FooResult 
		{ 
			private readonly Foo _foo = null;
			public Foo Foo { get { return _foo; }}
			public FooResult(Foo foo)
			{
				this._foo = foo;
			}
		}

		class Client
		{
			public void SimulatedAsync(Foo foo, Action<FooResult> action)
			{
				action(new FooResult(foo));
			}
		}

		[Fact]
		public void FromCallbackPattern_ThrowsOnNullParameter()
		{
			Action<Foo, Action<int>> callback = (foo, action) => { };
			Assert.Throws<ArgumentNullException>(() => ObservableExtensions.FromCallbackPattern(null as Foo, callback));
		}

		[Fact]
		public void FromCallbackPattern_ThrowsOnNullAsynchronousCall()
		{
			Action<Foo, Action<int>> callback = null;
			Assert.Throws<ArgumentNullException>(() => ObservableExtensions.FromCallbackPattern(new Foo(), callback));
		}

		[Fact]
		public void FromCallbackPattern_FiresAsyncCallback()
		{
			bool fired = false;
			Action<Foo, Action<System.Reactive.Unit>> callback = (foo, action) => { fired = true; };
			using (var subscription = ObservableExtensions.FromCallbackPattern(new Foo(), callback).Subscribe())
			{
				Assert.True(fired);
			}
		}

		[Fact]
		public void FromCallbackPattern_ExposesResultOfActionOverObservable()
		{
			Foo setFoo = new Foo();
			Client client = new Client();
		
			FooResult result = ObservableExtensions.FromCallbackPattern(setFoo, (Foo foo, Action<FooResult> fooResult) => client.SimulatedAsync(foo, fooResult))
				.First();

			Assert.Same(setFoo, result.Foo);
		}
	}
}