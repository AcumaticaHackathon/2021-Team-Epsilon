using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.CA
{
	public sealed class CABankTranMultipleBaseCurrenciesRestriction : PXCacheExtension<CABankTran>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<BAccountR.baseCuryID, Equal<Current<CABankTransactionsMaint.Filter.baseCuryID>>>),
			Messages.IncorrectBAccountBaseCuryID,
		new Type[] { typeof(CABankTransactionsMaint.Filter.cashAccountID), typeof(CashAccount.branchID), typeof(CR.BAccountR.cOrgBAccountID), typeof(CR.BAccountR.bAccountID) })]
		public int? PayeeBAccountID { get; set; }
	}
}