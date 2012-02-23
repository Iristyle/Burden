using System;

namespace EPS.Concurrency
{
	/// <summary>	Defines the action that should be performed in response to a job result.  </summary>
	/// <remarks>	7/8/2011. </remarks>
	/// <typeparam name="TQueuePoison">	Specifies the poison result of a queue action. </typeparam>
	public class JobQueueAction<TQueuePoison>
	{
		private readonly JobQueueActionType _actionType = JobQueueActionType.Complete;
		private readonly TQueuePoison _queuePoison;

		/// <summary>	Creates a new instance of this type, overriding the action type. </summary>
		/// <remarks>	7/8/2011. </remarks>
		/// <param name="actionType">	Type of the action. </param>
		public JobQueueAction(JobQueueActionType actionType)
		{
			this._actionType = actionType;
		}

		/// <summary>	Creates a new instance of this type, overriding the action type with Poison, specifying the actual poison value. </summary>
		/// <remarks>	7/8/2011. </remarks>
		/// <param name="queuePoison">	The queue poison. </param>
		public JobQueueAction(TQueuePoison queuePoison)
		{
			this._actionType = JobQueueActionType.Poison;
			this._queuePoison = queuePoison;
		}

		/// <summary>	Gets the type of the action. </summary>
		/// <value>	The type of the action. </value>
		public JobQueueActionType ActionType
		{
			get { return _actionType; }
		}

		/// <summary>	Gets the poison queue item. </summary>
		/// <value>	The queue poison. </value>
		public TQueuePoison QueuePoison
		{
			get { return _queuePoison; }
		}
	}
}