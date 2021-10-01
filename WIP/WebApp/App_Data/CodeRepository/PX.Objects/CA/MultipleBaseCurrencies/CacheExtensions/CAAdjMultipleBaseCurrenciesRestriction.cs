using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.CA
{
	public sealed class CAAdjMultipleBaseCurrenciesRestriction : PXCacheExtension<CAAdj>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<CashAccount.baseCuryID, Equal<Current<AccessInfo.baseCuryID>>>), null)]
		public int? CashAccountID { get; set; }
	}
}