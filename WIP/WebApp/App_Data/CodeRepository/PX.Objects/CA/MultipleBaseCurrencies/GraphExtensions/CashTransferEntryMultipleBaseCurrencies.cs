using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using System;

namespace PX.Objects.CA
{
	public class CashTransferEntryMultipleBaseCurrencies : PXGraphExtension<CashTransferEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldVerifying<CATransfer, CATransfer.outAccountID> e)
		{
			if (e.NewValue == null)
				return;

			CashAccount inAccount = PXSelectorAttribute.Select<CATransfer.inAccountID>(e.Cache, e.Row) as CashAccount;
			CashAccount outAccount = PXSelectorAttribute.Select<CATransfer.outAccountID>(e.Cache, e.Row, e.NewValue) as CashAccount;
			if (e.Row.InAccountID != null 
				&& inAccount.BaseCuryID != outAccount.BaseCuryID)
			{
				e.NewValue = ((CashAccount)PXSelectorAttribute.Select<CATransfer.outAccountID>(e.Cache, e.Row, e.NewValue)).CashAccountCD;
				throw new PXSetPropertyException(Messages.CashAccountDifferentBaseCury, 
					PXAccess.GetBranchCD(inAccount.BranchID), 
					inAccount.CashAccountCD,
					PXAccess.GetBranchCD(outAccount.BranchID), 
					outAccount.CashAccountCD);
			}
		}

		protected virtual void _(Events.FieldVerifying<CATransfer, CATransfer.inAccountID> e)
		{
			if (e.NewValue == null)
				return;

			CashAccount inAccount = PXSelectorAttribute.Select<CATransfer.inAccountID>(e.Cache, e.Row, e.NewValue) as CashAccount;
			CashAccount outAccount = PXSelectorAttribute.Select<CATransfer.outAccountID>(e.Cache, e.Row) as CashAccount;
			if (e.Row.OutAccountID != null
				&& inAccount.BaseCuryID != outAccount.BaseCuryID)
			{
				e.NewValue = ((CashAccount)PXSelectorAttribute.Select<CATransfer.outAccountID>(e.Cache, e.Row, e.NewValue)).CashAccountCD;
				throw new PXSetPropertyException(Messages.CashAccountDifferentBaseCury,
					PXAccess.GetBranchCD(inAccount.BranchID),
					inAccount.CashAccountCD,
					PXAccess.GetBranchCD(outAccount.BranchID),
					outAccount.CashAccountCD);
			}
		}
	}
}
