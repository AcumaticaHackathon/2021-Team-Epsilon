using System;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.AP
{
	public class APInvoiceEntryMultipleBaseCurrencies : PXGraphExtension<APInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldUpdated<APInvoice.branchID> e)
		{
			BranchAttribute.VerifyFieldInPXCache<APTran, APTran.branchID>(Base, Base.Transactions.Select());
		}

		[PXOverride]
		public virtual void Persist(Action persist)
		{
			BranchAttribute.VerifyFieldInPXCache<APTran, APTran.branchID>(Base, Base.Transactions.Select());

			persist();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		//Restore the PXRestrictor attribute that is deleted in the APInvoiceEntry graph
		[PXRestrictor(typeof(Where<Current2<APInvoiceMultipleBaseCurrenciesRestriction.branchBaseCuryID>, IsNull,
			Or<Vendor.baseCuryID, Equal<Current2<APInvoiceMultipleBaseCurrenciesRestriction.branchBaseCuryID>>>>), null)]
		protected virtual void _(Events.CacheAttached<APInvoice.vendorID> e) {}

		protected virtual void _(Events.FieldVerifying<APInvoice.branchID> e)
		{
			if (e.NewValue == null)
				return;

			Branch branch = PXSelectorAttribute.Select<APInvoice.branchID>(e.Cache, e.Row, (int)e.NewValue) as Branch;
			PXFieldState vendorBaseCuryID = e.Cache.GetValueExt<APInvoiceMultipleBaseCurrenciesRestriction.vendorBaseCuryID>(e.Row) as PXFieldState;
			if (vendorBaseCuryID?.Value != null
				&& branch.BaseCuryID != vendorBaseCuryID.ToString())
			{
				e.NewValue = branch.BranchCD;
				BAccountR vendor = PXSelectorAttribute.Select<APInvoice.vendorID>(e.Cache, e.Row) as BAccountR;
				throw new PXSetPropertyException(Messages.BranchVendorDifferentBaseCury, PXOrgAccess.GetCD(vendor.VOrgBAccountID), vendor.AcctCD);
			}
		}
	}
}