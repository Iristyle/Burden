using System;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class JobResultInspectorTest
	{
		[Fact]
		public void From_Throws_OnNullInspector()
		{
			Assert.Throws<ArgumentNullException>(() => JobResultInspector.From(null as Func<JobResult<int, int>, JobQueueAction<
			Poison<int>>>));
		}
	}

	public abstract class JobResultInspectorTest<TJobInput, TJobOutput>
		: IJobResultInspectorTest<TJobInput, TJobOutput, Poison<TJobInput>>
	{
		//real simple inspector with default-ish behavior
		protected static Func<JobResult<TJobInput, TJobOutput>, JobQueueAction<Poison<TJobInput>>> inspector
		= new Func<JobResult<TJobInput, TJobOutput>, JobQueueAction<Poison<TJobInput>>>(result =>
		{ 
			if (null != result.Exception)
				return new JobQueueAction<Poison<TJobInput>>(new Poison<TJobInput>(result.Input, result.Exception));

			return new JobQueueAction<Poison<TJobInput>>(JobQueueActionType.Complete);
		});

		public JobResultInspectorTest()
			: base(() => JobResultInspector.From(inspector))
		{ }

		[Fact]
		public void Constructor_ThrowsOnNullInspector()
		{
			Assert.Throws<ArgumentNullException>(() => new JobResultInspector<TJobInput, TJobInput, Poison<TJobInput>>(null));
		}
	}

	public class ValueTypeJobResultInspectorTest
		: JobResultInspectorTest<int, int>
	{
		public ValueTypeJobResultInspectorTest()
		{ }
	}

	public class ReferenceTypeJobResultInspectorTest
		: JobResultInspectorTest<object, string>
	{
		public ReferenceTypeJobResultInspectorTest()
		{ }
	}
}