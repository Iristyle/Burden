using System;

namespace Burden
{
	/// <summary>	Values that represent JobResultType - similar in spirit to System.Reactive.NotificationKind.  </summary>
	/// <remarks>	7/14/2011. </remarks>
	public enum JobResultType
	{
		/// <summary> The job completed.  </summary>
		Completed,
		/// <summary> An error occurred during the job, which could include cancellation.  </summary>
		Error
	}
}