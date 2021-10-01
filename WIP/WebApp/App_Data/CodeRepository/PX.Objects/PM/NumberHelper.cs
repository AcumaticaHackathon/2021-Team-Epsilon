using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
	internal static class NumberHelper
	{
		internal static string IncreaseNumber(string number, int increaseValue)
		{
			int lastNumericDigitsWithZeroCount;
			long currentNumericValue = GetNumericValue(number, out lastNumericDigitsWithZeroCount);

			long newNumber = currentNumericValue + increaseValue;
			string newNumberString = newNumber.ToString();

			string textPart = string.Empty;
			if (number.Length - lastNumericDigitsWithZeroCount > 0)
			{
				textPart = number.Substring(0, number.Length - lastNumericDigitsWithZeroCount);
			}

			string zeroPart = string.Empty;
			if (newNumberString.Length < lastNumericDigitsWithZeroCount)
			{
				zeroPart = new string((char)48, lastNumericDigitsWithZeroCount - newNumberString.Length);
			}

			return string.Concat(textPart, zeroPart, newNumberString);
		}

		internal static string DecreaseNumber(string number, int decreaseValue)
		{
			int lastNumericDigitsWithZeroCount;
			long currentNumericValue = GetNumericValue(number, out lastNumericDigitsWithZeroCount);

			long newNumber = currentNumericValue - decreaseValue;
			if (newNumber < 0)
			{
				newNumber = 0;
			}
			string newNumberString = newNumber.ToString();

			string textPart = string.Empty;
			if (number.Length - lastNumericDigitsWithZeroCount > 0)
			{
				textPart = number.Substring(0, number.Length - lastNumericDigitsWithZeroCount);
			}

			string zeroPart = string.Empty;
			if (newNumberString.Length < lastNumericDigitsWithZeroCount)
			{
				zeroPart = new string((char)48, lastNumericDigitsWithZeroCount - newNumberString.Length);
			}

			return string.Concat(textPart, zeroPart, newNumberString);
		}

		internal static string GetTextPrefix(string number)
		{
			int lastNumericDigitsWithZeroCount;
			long currentNumericValue = GetNumericValue(number, out lastNumericDigitsWithZeroCount);

			string textPart = string.Empty;
			if (number.Length - lastNumericDigitsWithZeroCount > 0)
			{
				textPart = number.Substring(0, number.Length - lastNumericDigitsWithZeroCount);
			}

			return textPart;
		}

		internal static long GetNumericValue(string number)
		{
			int lastNumericDigitsWithZeroCount;
			long numericValue = GetNumericValue(number, out lastNumericDigitsWithZeroCount);
			return numericValue;
		}
		
		private static long GetNumericValue(string number, out int lastNumericDigitsWithZeroCount)
		{
			lastNumericDigitsWithZeroCount = 0;

			int lastNumericDigitsCount = 0;
			for (int i = number.Length - 1; i >= 0; i--)
			{
				int symbolCode = number[i];
				if (symbolCode == 48)
				{
					lastNumericDigitsWithZeroCount++;
				}
				else if (symbolCode >= 49 && symbolCode <= 57)
				{
					lastNumericDigitsWithZeroCount++;
					lastNumericDigitsCount = lastNumericDigitsWithZeroCount;
				}
				else
				{
					break;
				}
			}

			long currentNumber = 0;
			if (lastNumericDigitsCount > 0)
			{
				currentNumber = long.Parse(number.Substring(number.Length - lastNumericDigitsCount, lastNumericDigitsCount));
			}

			return currentNumber;
		}
	}
}
