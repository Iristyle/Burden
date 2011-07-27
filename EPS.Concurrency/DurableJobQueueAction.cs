using System;
using System.Collections.Generic;

namespace EPS.Concurrency
{
	/// <summary>	Describes a durable job queue action.  </summary>
	/// <remarks>	7/26/2011. </remarks>
	/// <typeparam name="TQueue">	   	Type of the queue input. </typeparam>
	/// <typeparam name="TQueuePoison">	Type of the queue poison items. </typeparam>
	public class DurableJobQueueAction<TQueue, TQueuePoison>
	{
		/// <summary>	Gets the type of the action. </summary>
		/// <value>	The type of the action. </value>
		public DurableJobQueueActionType ActionType { get; private set; }
		
		/// <summary>	Gets the queue input. </summary>
		/// <value>	The input. </value>
		public TQueue Input { get; private set; }

		/// <summary>	Gets or sets the poison. </summary>
		/// <value>	The poison. </value>
		public TQueuePoison Poison { get; private set; }
		
		private readonly bool isInputValueType = typeof(TQueue).IsValueType;
		private readonly bool isPoisonValueType = typeof(TQueuePoison).IsValueType;

		internal DurableJobQueueAction(DurableJobQueueActionType actionType, TQueue input, TQueuePoison poison)
		{
			ActionType = actionType;
			Input = input;
			Poison = poison;

			switch (actionType)
			{
				case DurableJobQueueActionType.Queued:
				case DurableJobQueueActionType.Completed:
				case DurableJobQueueActionType.Pending:
				default:
					if (!isInputValueType && null == input)
					{
						throw new ArgumentNullException("input");
					}
					if (!isPoisonValueType && null != poison)
					{
						throw new ArgumentException("poison", "must be null");
					}
					break;

				case DurableJobQueueActionType.Deleted:
					if (!isInputValueType && null != input)
					{
						throw new ArgumentException("input", "must be null");
					}
					if (!isPoisonValueType && null == poison)
					{
						throw new ArgumentNullException("poison");
					}
					break;

				case DurableJobQueueActionType.Poisoned:
					if (!isInputValueType && null == input)
					{
						throw new ArgumentNullException("input");
					}
					if (!isPoisonValueType && null == poison)
					{
						throw new ArgumentNullException("poison");
					}
					break;
			}
		}
	}

	/// <summary>	Convenience class for creating instances of DurableJobQueueAction.  </summary>
	/// <remarks>	7/27/2011. </remarks>
	public class DurableJobQueueAction
	{
		/// <summary>	Creates an action description for a queued event. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <typeparam name="TQueue">	   	Type of the queue input. </typeparam>
		/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
		/// <param name="input">	The input. </param>
		/// <returns>	A new DurableJobQueueAction instance. </returns>
		public static DurableJobQueueAction<TQueue, TQueuePoison> Queued<TQueue, TQueuePoison>(TQueue input)
		{
			return new DurableJobQueueAction<TQueue, TQueuePoison>(DurableJobQueueActionType.Queued, input, default(TQueuePoison));
		}

		/// <summary>	Creates an action description for a pending event. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <typeparam name="TQueue">	   	Type of the queue input. </typeparam>
		/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
		/// <param name="input">	The input. </param>
		/// <returns>	A new DurableJobQueueAction instance. </returns>
		public static DurableJobQueueAction<TQueue, TQueuePoison> Pending<TQueue, TQueuePoison>(TQueue input)
		{
			return new DurableJobQueueAction<TQueue, TQueuePoison>(DurableJobQueueActionType.Pending, input, default(TQueuePoison));
		}

		/// <summary>	Creates an action description for a completed event. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <typeparam name="TQueue">	   	Type of the queue input. </typeparam>
		/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
		/// <param name="input">	The input. </param>
		/// <returns>	A new DurableJobQueueAction instance. </returns>
		public static DurableJobQueueAction<TQueue, TQueuePoison> Completed<TQueue, TQueuePoison>(TQueue input)
		{
			return new DurableJobQueueAction<TQueue, TQueuePoison>(DurableJobQueueActionType.Completed, input, default(TQueuePoison));
		}

		/// <summary>	Creates an action description for a poisoned event. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <typeparam name="TQueue">	   	Type of the queue input. </typeparam>
		/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
		/// <param name="input"> 	The input. </param>
		/// <param name="poison">	The poison. </param>
		/// <returns>	A new DurableJobQueueAction instance. </returns>
		public static DurableJobQueueAction<TQueue, TQueuePoison> Poisoned<TQueue, TQueuePoison>(TQueue input, TQueuePoison poison)
		{
			return new DurableJobQueueAction<TQueue, TQueuePoison>(DurableJobQueueActionType.Poisoned, input, poison);
		}

		/// <summary>	Creates an action description for a deleted event. </summary>
		/// <remarks>	7/27/2011. </remarks>
		/// <typeparam name="TQueue">	   	Type of the queue input. </typeparam>
		/// <typeparam name="TQueuePoison">	Type of the queue poison. </typeparam>
		/// <param name="poison">	The poison. </param>
		/// <returns>	A new DurableJobQueueAction instance. </returns>
		public static DurableJobQueueAction<TQueue, TQueuePoison> Deleted<TQueue, TQueuePoison>(TQueuePoison poison)
		{
			return new DurableJobQueueAction<TQueue, TQueuePoison>(DurableJobQueueActionType.Deleted, default(TQueue), poison);
		}
	}
}