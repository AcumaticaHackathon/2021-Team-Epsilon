using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.RQ;

namespace PX.Objects.PO
{
	public class POOrderEntryVisibilityRestriction : PXGraphExtension<POOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXCustomizeBaseAttribute(typeof(RestrictVendorByBranch), nameof(RestrictVendorByBranch.ResetVendor), true)]
		public virtual void POOrder_VendorID_CacheAttached(PXCache sender)
		{
		}

		[PXCustomizeBaseAttribute(typeof(RestrictVendorByBranch), nameof(RestrictVendorByBranch.ResetVendor), true)]
		public virtual void POOrder_PayToVendorID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search2<
			BAccount2.bAccountID,
			LeftJoin<Vendor,
				On<Vendor.bAccountID, Equal<BAccount2.bAccountID>,
				And<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>,
				And<Match<Vendor, Current<AccessInfo.userName>>>>>,
			LeftJoin<AR.Customer,
				On<AR.Customer.bAccountID, Equal<BAccount2.bAccountID>,
				And<Match<AR.Customer, Current<AccessInfo.userName>>>>,
			LeftJoin<GL.Branch,
				On<GL.Branch.bAccountID, Equal<BAccount2.bAccountID>,
				And<Match<GL.Branch, Current<AccessInfo.userName>>>>>>>,
			Where<Vendor.bAccountID, IsNotNull,
				And<Optional<POOrder.shipDestType>, Equal<POShippingDestination.vendor>,
				Or<Where<GL.Branch.bAccountID, IsNotNull,
					And<Optional<POOrder.shipDestType>, Equal<POShippingDestination.company>,
					Or<Where<AR.Customer.bAccountID, IsNotNull,
						And<Optional<POOrder.shipDestType>, Equal<POShippingDestination.customer>>>>>>>>>>),
				typeof(BAccount.acctCD), typeof(BAccount.acctName), typeof(BAccount.type), typeof(BAccount.acctReferenceNbr), typeof(BAccount.parentBAccountID),
			SubstituteKey = typeof(BAccount.acctCD), DescriptionField = typeof(BAccount.acctName), CacheGlobal = true)]
		public virtual void POOrder_ShipToBAccountID_CacheAttached(PXCache sender)
		{
		}
	}
}