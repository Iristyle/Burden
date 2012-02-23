namespace EPS.Concurrency.Tests.Unit
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;

	public static class Dynamic
	{
		[SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "type", Justification = "FxCop rule broken, this syntax is a neat way of inferring anonymous types")]
		[SuppressMessage("Gendarme.Rules.Design.Linq", "AvoidExtensionMethodOnSystemObjectRule", Justification = "Someone cares about VB.Net?")]
		public static T Cast<T>(this object instance, T type)
		{
			return (T)instance;
		}

		[SuppressMessage("Gendarme.Rules.Design.Linq", "AvoidExtensionMethodOnSystemObjectRule", Justification = "Someone cares about VB.Net?")]
		public static T Cast<T>(this object value)
		{
			return (T)value;
		}

	}
}
