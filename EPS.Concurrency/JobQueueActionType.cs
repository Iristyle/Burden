using System;

namespace EPS.Concurrency
{
	/// <summary>	Potential action types for a queued job, for  that represent JobQueueActionType.  </summary>
	/// <remarks>	7/8/2011. </remarks>
	public enum JobQueueActionType
	{
		/// <summary> The job has successfully completed.  </summary>
		Complete,
		/// <summary> The job should be poisoned.  </summary>
		Poison,
		/// <summary> There is not enough information to determine what should be done with the job.  </summary>
		Unknown
	}
}