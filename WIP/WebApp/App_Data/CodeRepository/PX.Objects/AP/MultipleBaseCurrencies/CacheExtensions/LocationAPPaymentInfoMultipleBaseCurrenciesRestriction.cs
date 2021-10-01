using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;
using PX.Objects.CA;

namespace PX.Objects.AP
{
	public sealed class LocationAPPaymentInfoMultipleBaseCurrenciesRestriction : PXCacheExtension<LocationAPPaymentInfo>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region VCashAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<CashAccount.baseCuryID, Equal<Current<BAccountR.baseCuryID>>>), Messages.CashAccountSameBaseCurrency, SuppressVerify = true)]
		public Int32? VCashAccountID { get; set; }
		#endregion
	}
}