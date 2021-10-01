using System;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.Extensions.MultiCurrency;
using PX.Data;

namespace PX.Objects.CM.TemporaryHelpers
{
	/// <summary>
	/// Povides SOME operations for currency-related calculations. Uses IPXCurrencyHelper (MultiCurrency Extension) if available, otherwise uses PXCurrencyAttribute
	/// </summary>
	class MCExtensionAdapter
	{
		public static void CuryConvBase(PXCache sender, object row, decimal curyval, out decimal baseval)
		{
			IPXCurrencyHelper pXCurrencyHelper = sender.Graph.FindImplementation<IPXCurrencyHelper>();
			if (pXCurrencyHelper == null)
				PXCurrencyAttribute.CuryConvBase(sender, row, curyval, out baseval);
			else
				pXCurrencyHelper.CuryConvBase(curyval, out baseval);
		}

		public static void CuryConvBaseSkipRounding(PXCache sender, object row, decimal curyval, out decimal baseval)
		{
			IPXCurrencyHelper pXCurrencyHelper = sender.Graph.FindImplementation<IPXCurrencyHelper>();
			if (pXCurrencyHelper == null)
				PXCurrencyAttribute.CuryConvBase(sender, row, curyval, out baseval, true);
			else
			{
				CM.Extensions.CurrencyInfo currencyInfo = PXCache<CM.Extensions.CurrencyInfo>.CreateCopy(pXCurrencyHelper.GetDefaultCurrencyInfo());
				currencyInfo.BasePrecision = null;
				pXCurrencyHelper.CuryConvBase(currencyInfo, curyval, out baseval);
			}
		}

		public static void CuryConvCury(PXCache sender, object row, decimal baseval, out decimal curyval, int precision)
		{
			IPXCurrencyHelper pXCurrencyHelper = sender.Graph.FindImplementation<IPXCurrencyHelper>();
			if (pXCurrencyHelper == null)
				PXCurrencyAttribute.CuryConvCury(sender, row, baseval, out curyval, precision);
			else
			{
				CM.Extensions.CurrencyInfo currencyInfo = PXCache<CM.Extensions.CurrencyInfo>.CreateCopy(pXCurrencyHelper.GetDefaultCurrencyInfo());
				currencyInfo.CuryPrecision = (short)precision;
				pXCurrencyHelper.CuryConvCury(baseval, out curyval);
			}
		}

		public static decimal RoundCury(PXCache sender, object row, decimal val)
		{
			IPXCurrencyHelper pXCurrencyHelper = sender.Graph.FindImplementation<IPXCurrencyHelper>();
			if (pXCurrencyHelper == null) return PXCurrencyAttribute.RoundCury(sender, row, val);
			else return pXCurrencyHelper.RoundCury(val);
		}
	}
}
