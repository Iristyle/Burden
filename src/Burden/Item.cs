using System;

namespace Burden
{
	/// <summary>
	/// A generic class representing the result of a queue read, since value types cannot be null, and it is necessary to return a read
	/// failure / success status from a queue operation.
	/// </summary>
	/// <remarks>	8/2/2011. </remarks>
	/// <typeparam name="T">	Generic type parameter. </typeparam>
	internal struct Item<T> : IItem<T>
	{
		/// <summary>	Gets the item value, if it was able to be read. </summary>
		/// <value>	The input. </value>
		public T Value { get; set; }

		/// <summary>	Gets or sets the read failure / success status. </summary>
		/// <value>	true if success, false if not. </value>
		public bool Success { get; set; }
	}

	/// <summary>	A simple static class used to construct IItem{T} instances. </summary>
	/// <remarks>	8/3/2011. </remarks>
	public static class Item
	{
		/// <summary>	Gets a placeholder for No item. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <returns>	A new instance where Success is false, and the item is its default value. </returns>
		public static IItem<T> None<T>()
		{
			return new Item<T>() { Success = false };
		}

		/// <summary>	Constructs a new IItem instance from an existing item instance. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <exception cref="ArgumentNullException">	Thrown when T is a reference type and instance is null. </exception>
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <param name="instance">	The instance. </param>
		/// <returns>	A new IItem{T} instance given the type of the passed item. </returns>
		public static IItem<T> From<T>(T instance)
		{
			if (!typeof(T).IsValueType && (null == instance))
			{
				throw new ArgumentNullException("instance");
			}

			return new Item<T>() 
			{ 
				Value = instance,
				Success = true 
			};
		}
	}
}