using System.Collections.Generic;

namespace Compas.Core.Extensions
{
	/// <summary>
	/// A generic queue with a fixed maximum size.
	/// When the queue reaches max capacity, the oldest item is automatically removed before adding a new one.
	/// </summary>
	/// <typeparam name="T">The type of items stored in the queue.</typeparam>
	public class FixedSizeQueue<T>
	{
		private readonly Queue<T> queue = new Queue<T>();
		private readonly int maxSize;

		/// <summary>
		/// Initializes a new instance of the FixedSizeQueue with the specified maximum size.
		/// </summary>
		/// <param name="maxSize">The maximum number of items the queue can hold.</param>
		public FixedSizeQueue(int maxSize) => this.maxSize = maxSize;

		/// <summary>
		/// Adds an item to the queue.
		/// If the queue is at max capacity, the oldest item is removed first.
		/// </summary>
		/// <param name="item">The item to add to the queue.</param>
		public void Enqueue(T item)
		{
			if (queue.Count == maxSize)
			{
				queue.Dequeue(); // drop oldest
			}
			queue.Enqueue(item);
		}

		/// <summary>
		/// Removes and returns the oldest item from the queue.
		/// </summary>
		/// <returns>The oldest item in the queue.</returns>
		public T Dequeue() => queue.Dequeue();
	}
}
