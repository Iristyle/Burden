using System;
using System.Collections.Generic;

namespace Burden
{
	/// <summary>	Values that describe the type of DurableJobQueueAction.  </summary>
	/// <remarks>	7/26/2011. </remarks>
	public enum DurableJobQueueActionType
	{
		/// <summary> An item has been queued.  </summary>
		Queued,
		/// <summary> An item is now in the pending result state (executing).  </summary>
		Pending,
		/// <summary> The execution for the job failed and has been poisoned.  </summary>
		Poisoned,
		/// <summary> The execution for the job completed and is being removed from the queue.  </summary>
		Completed,
		/// <summary> The failed job execution poison has been deleted.  </summary>
		Deleted
	}
}