using System;
using EPS.Utility;
using Xunit;

namespace EPS.Concurrency.Tests.Unit
{
	public class JobResultInspectorTest
	{
		[Fact]
		public void FromInspector_Throws_OnNullInspector()
		{
			Assert.Throws<ArgumentNullException>(() => JobResultInspector.FromInspector(null as Func<JobResult<int, int>,
			JobQueueAction<
			Poison<int>>>));
		}

		[Fact]
		public void FromJobSpecification_Throws_OnNullInspector()
		{
			Assert.Throws<ArgumentNullException>(() => JobResultInspector.FromJobSpecification(null as Func<int, int>));
		}

		//these check the behavior on our default inspector
		[Fact]
		public void FromJobSpecification_Inspect_ThrowsOnNullJobResult()
		{
			var inspector = JobResultInspector.FromJobSpecification((int i) => 3);

			Assert.Throws<ArgumentNullException>(() => inspector.Inspect(null));
		}

		private static JobQueueAction<Poison<int>> GetSuccessInspectionResult()
		{
			var inspector = JobResultInspector.FromJobSpecification((int i) => 3);
			var inspectionResult = inspector.Inspect(new JobResult<int, int>(2, 3));
			return inspectionResult;
		}

		[Fact]
		public void FromJobSpecification_Inspect_ReturnJobQueueActionTypeOfComplete_OnGoodJobResult()
		{
			JobQueueAction<Poison<int>> inspectionResult = GetSuccessInspectionResult();
			Assert.Equal(JobQueueActionType.Complete, inspectionResult.ActionType);
		}

		[Fact]
		public void FromJobSpecification_Inspect_ReturnsNullQueuePoison_OnGoodJobResult()
		{
			JobQueueAction<Poison<int>> inspectionResult = GetSuccessInspectionResult();
			Assert.Equal(null, inspectionResult.QueuePoison);
		}

		private static JobQueueAction<Poison<int>> GetFailedInspectionResult(int input, Exception exception)
		{
			var inspector = JobResultInspector.FromJobSpecification((int i) => 3);
			return inspector.Inspect(new JobResult<int, int>(input, exception));
		}

		[Fact]
		public void FromJobSpecification_Inspect_ReturnJobQueueActionTypeOfPoison_OnFailedJobResult()
		{
			var inspectionResult = GetFailedInspectionResult(2, new ArgumentException());
			Assert.Equal(JobQueueActionType.Poison, inspectionResult.ActionType);
		}

		[Fact]
		public void FromJobSpecification_Inspect_ReturnsExpectedQueuePoison_OnFailedJobResult()
		{
			int input = 2;
			var exception = new ArgumentException();
			var inspectionResult = GetFailedInspectionResult(input, exception);

			Assert.Equal(new Poison<int>(input, exception), inspectionResult.QueuePoison, GenericEqualityComparer<Poison<int>>.ByAllMembers());
		}
	}

	public abstract class JobResultInspectorTest<TJobInput, TJobOutput>
		: IJobResultInspectorTest<TJobInput, TJobOutput, Poison<TJobInput>>
	{
		public JobResultInspectorTest(Func<TJobInput, TJobOutput> jobAction)
			: base(() => JobResultInspector.FromJobSpecification(jobAction))
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
			: base(i => 3)
		{ }
	}

	public class ReferenceTypeJobResultInspectorTest
		: JobResultInspectorTest<object, string>
	{
		public ReferenceTypeJobResultInspectorTest()
			: base(o => "foo") 
		{ }
	}
}