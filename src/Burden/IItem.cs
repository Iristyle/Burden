using System;

namespace Burden
{
	/// <summary>
	/// Describes a queue item returned from IDurableJobQueue.NextQueuedItem.  Since value types cannot be null, and it is necessary to
	/// return a read failure / success status from a queue operation.
	/// </summary>
	/// <remarks>	8/3/2011. </remarks>
	/// <typeparam name="T">	Generic type parameter. </typeparam>
	public interface IItem<T>
	{
		/// <summary>	Gets the item value. </summary>
		/// <value>	The value. </value>
		T Value { get; }
		
		/// <summary>	Gets a value indicating whether reading from the queue was a success. </summary>
		/// <value>	true if success, false if not. </value>
		bool Success { get; }
	}
}