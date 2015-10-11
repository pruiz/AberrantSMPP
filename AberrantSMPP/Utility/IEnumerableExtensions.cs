using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AberrantSMPP.Utility
{
	public static class IEnumerableExtensions
	{
		/// <summary>
		/// Finds the index in the collection where the predicate evaluates to true.
		/// 
		/// Returns -1 if no matching item found
		/// </summary>
		/// <typeparam name="TSource">Type of collection</typeparam>
		/// <param name="source">Source collection</param>
		/// <param name="predicate">Function to evaluate</param>
		/// <returns>Index where predicate is true, or -1 if not found.</returns>
		public static int IndexWhere<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			var enumerator = source.GetEnumerator();
			int index = 0;
			while (enumerator.MoveNext())
			{
				TSource obj = enumerator.Current;
				if (predicate(obj))
					return index;
				index++;
			}
			return -1;
		}

		/// <summary>
		/// Finds the index in the collection where the predicate evaluates to true.
		/// 
		/// Returns -1 if no matching item found
		/// </summary>
		/// <typeparam name="TSource">Type of collection</typeparam>
		/// <param name="source">Source collection</param>
		/// <param name="predicate">Function to evaluate</param>
		/// <returns>Index where predicate is true, or -1 if not found.</returns>
		public static int IndexWhere<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		{
			var enumerator = source.GetEnumerator();
			int index = 0;
			while (enumerator.MoveNext())
			{
				TSource obj = enumerator.Current;
				if (predicate(obj, index))
					return index;
				index++;
			}
			return -1;
		}
	}
}
