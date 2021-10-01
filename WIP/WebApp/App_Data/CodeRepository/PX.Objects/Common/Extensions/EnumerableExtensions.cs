using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;

using PX.Data;

namespace PX.Objects.Common.Extensions
{
	/// <summary>
	/// This class contains extension methods for enumerable objects.
	/// </summary>
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Returns <c>true</c> if the collection is empty. If the collection is <c>null</c>,
		/// an exception is thrown.
		/// </summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="sequence">The enumerable object.</param>
		/// <returns></returns>
		public static bool IsEmpty<T>(this IEnumerable<T> sequence) => !sequence.Any();

		/// <summary>
		/// Returns <c>true</c> if <paramref name="sequence"/> contains exactly one element.
		/// Otherwise, the method returns <c>false</c>.
		/// </summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="sequence">The enumerable object.</param>
		/// <returns></returns>
		public static bool IsSingleElement<T>(this IEnumerable<T> sequence)
		{
			if (sequence == null) throw new ArgumentNullException(nameof(sequence));

			using (IEnumerator<T> enumerator = sequence.GetEnumerator())
				return enumerator.MoveNext() && !enumerator.MoveNext();
		}

		/// <exclude/>
		public static IEnumerable<TNode> DistinctByKeys<TNode>(this IEnumerable<TNode> sequence, PXGraph graph)
			where TNode : class, IBqlTable, new()
			=> DistinctByKeys(sequence, graph?.Caches[typeof(TNode)]);

		/// <summary>
		/// For a sequence of records and a <see cref="PXCache"/> object,
		/// returns a sequence of elements that have different keys.
		/// </summary>
		/// <remarks>The collection of keys is defined by <see cref="PXCache.Keys"/>.</remarks>
		public static IEnumerable<TNode> DistinctByKeys<TNode>(this IEnumerable<TNode> sequence, PXCache cache)
			where TNode : class, IBqlTable, new()
		{
			if (sequence == null) throw new ArgumentNullException(nameof(sequence));
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			return sequence.Distinct(new RecordKeyComparer<TNode>(cache));
		}

		/// <summary>
		/// Returns the index of the first element that satisfies the 
		/// specified predicate, or a negative value if such an
		/// element cannot be found.
		/// </summary>
		/// <remarks>
		/// If no element satisfying the predicate can
		/// be found, the negative value returned by this method is
		/// the opposite of the number of elements in the sequence
		/// increased by one (namely, -(N+1)).
		/// </remarks>
		public static int FindIndex<T>(this IEnumerable<T> sequence, Predicate<T> predicate)
		{
			int index = 0;

			foreach (T element in sequence)
			{
				if (predicate(element)) return index;
				++index;
			}

			return -index - 1;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="sequence"/> contains two or more elements.
		/// Otherwise, the method returns <c>false</c>.
		/// </summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="sequence">The enumerable object.</param>
		/// <returns></returns>
		public static bool HasAtLeastTwoItems<T>(this IEnumerable<T> sequence) => sequence.HasAtLeast(2);

		/// <summary>
		/// Flattens a sequence of element groups into a sequence of elements.
		/// </summary>
		public static IEnumerable<TValue> Flatten<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> sequenceOfGroups)
			=> sequenceOfGroups.SelectMany(x => x);

		/// <summary>
		/// Returns a row with the sum of all decimal fields and the identical value if all rows contains that value;
        /// otherwise, the method returns <c>null</c>.
		/// </summary>
		public static RecordType CalculateSumTotal<RecordType>(this IEnumerable<RecordType> rows, PX.Data.PXCache cache)
					where RecordType : PX.Data.IBqlTable, new()
		{
			var total = new RecordType();

			var properties = cache.GetAttributesOfType<PX.Data.PXUIFieldAttribute>(null, null)
				.Select(p => new
				{
					uiAttribute = p,
					sumGroupOperation = cache.GetAttributesOfType<PX.Data.PXDecimalAttribute>(null, p.FieldName).Any() ||
						cache.GetAttributesOfType<PX.Data.PXDBDecimalAttribute>(null, p.FieldName).Any()
				}).ToList();

			var propertiesWithSum = properties.Where(p => p.sumGroupOperation).ToList();
			var propertiesWithoutSum = properties.Where(p => !p.sumGroupOperation).ToList();

			var sumAggregateValues = new decimal[propertiesWithSum.Count];
			var aggregateValues = new object[propertiesWithoutSum.Count];

			bool firstRow = true;

			foreach (var row in rows)
			{
				for (int fieldIndex = 0; fieldIndex < propertiesWithSum.Count; fieldIndex++)
				{
					var value = (decimal?)cache.GetValue(row, propertiesWithSum[fieldIndex].uiAttribute.FieldName);
					sumAggregateValues[fieldIndex] += value ?? 0;
				}

				for (int fieldIndex = 0; fieldIndex < propertiesWithoutSum.Count; fieldIndex++)
				{
					var value = cache.GetValue(row, propertiesWithoutSum[fieldIndex].uiAttribute.FieldName);
					aggregateValues[fieldIndex] = (firstRow || aggregateValues[fieldIndex]?.Equals(value) == true) ? value : null;
				}

				firstRow = false;
			}

			for (int fieldIndex = 0; fieldIndex < propertiesWithSum.Count; fieldIndex++)
			{
				cache.SetValue(total, propertiesWithSum[fieldIndex].uiAttribute.FieldName, sumAggregateValues[fieldIndex]);
			}
			for (int fieldIndex = 0; fieldIndex < propertiesWithoutSum.Count; fieldIndex++)
			{
				cache.SetValue(total, propertiesWithoutSum[fieldIndex].uiAttribute.FieldName, aggregateValues[fieldIndex]);
			}

			return total;
		}

		/// <exclude/>
		public static IEnumerable<TResult> Batch<TSource, TResult>(this IEnumerable<TSource> source, int size,
			Func<IEnumerable<TSource>, TResult> resultSelector)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

			return _(); IEnumerable<TResult> _()
			{
				TSource[] bucket = null;
				var count = 0;

				foreach (var item in source)
				{
					if (bucket == null)
					{
						bucket = new TSource[size];
					}

					bucket[count++] = item;

					// The bucket is fully buffered before it's yielded
					if (count != size)
					{
						continue;
					}

					yield return resultSelector(bucket);

					bucket = null;
					count = 0;
				}

				// Return the last bucket with all remaining elements
				if (bucket != null && count > 0)
				{
					Array.Resize(ref bucket, count);
					yield return resultSelector(bucket);
				}
			}
		}
	}
}
