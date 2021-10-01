using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.Common.Extensions
{
	public static class ArrayExtensions
	{
		public static T?[] SparseArrayAddDistinct<T>(this T?[] sparseArray, T? item) where T : struct
		{
			const int defaultCapacity = 4;

			if (sparseArray == null)
				throw new ArgumentNullException(nameof(sparseArray));

			if (item == null)
				throw new ArgumentNullException(nameof(item));

			int emptySlot = -1;
			for (int i = 0; i < sparseArray.Length; i++)
			{
				if (Equals(sparseArray[i], item))
					return sparseArray;

				if (!sparseArray[i].HasValue && emptySlot == -1)
					emptySlot = i;
			}

			if (emptySlot != -1)
			{
				sparseArray[emptySlot] = item;
				return sparseArray;
			}

			int newCapacity = sparseArray.Length == 0 ? defaultCapacity : sparseArray.Length * 2;
			T?[] newArray = new T?[newCapacity];
			sparseArray.CopyTo(newArray, 0);
			newArray[sparseArray.Length] = item;

			return newArray;
		}

		public static void SparseArrayRemove<T>(this T?[] sparseArray, T? item) where T : struct
		{
			if (sparseArray == null)
				throw new ArgumentNullException(nameof(sparseArray));

			if (item == null)
				throw new ArgumentNullException(nameof(item));

			for (int i = 0; i < sparseArray.Length; i++)
			{
				if (!Equals(sparseArray[i], item))
					continue;

				sparseArray[i] = null;
				return;
			}
		}

		public static void SparseArrayClear<T>(this T?[] sparseArray) where T : struct
		{
			if (sparseArray == null)
				throw new ArgumentNullException(nameof(sparseArray));

			for (int i = 0; i < sparseArray.Length; i++)
			{
				sparseArray[i] = null;
			}
		}

		public static T?[] SparseArrayCopy<T>(this T?[] sparseArray) where T : struct
		{
			if (sparseArray == null)
				throw new ArgumentNullException(nameof(sparseArray));

			int newIndex = 0;
			T?[] newArray = new T?[sparseArray.Length];
			for (int i = 0; i < sparseArray.Length; i++)
			{
				if (sparseArray[i] == null)
					continue;

				newArray[newIndex] = sparseArray[i];
				newIndex++;
			}

			Array.Resize(ref newArray, newIndex + 1);
			return newArray;
		}
	}
}
