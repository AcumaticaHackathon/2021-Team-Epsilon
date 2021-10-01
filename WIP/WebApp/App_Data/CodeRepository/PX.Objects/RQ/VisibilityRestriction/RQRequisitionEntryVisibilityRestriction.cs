using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.PO;

namespace PX.Objects.RQ
{
	public class RQRequisitionEntryVisibilityRestriction : PXGraphExtension<RQRequisitionEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public virtual void RQRequisition_VendorID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public virtual void RQBiddingVendor_VendorID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public virtual void RQRequestLineFilter_VendorID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search2<
			BAccount2.bAccountID,
			LeftJoin<Vendor,
				On<Vendor.bAccountID, Equal<BAccount2.bAccountID>,
				And<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>,
				And<Vendor.type, NotEqual<BAccountType.employeeType>,
				And<Match<Vendor, Current<AccessInfo.userName>>>>>>,
			LeftJoin<Customer,
				On<Customer.bAccountID, Equal<BAccount2.bAccountID>,
				And<Customer.cOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>,
				And<Customer.type, NotEqual<BAccountType.employeeType>,
				And<Match<Customer, Current<AccessInfo.userName>>>>>>,
			LeftJoin<GL.Branch,
				On<GL.Branch.bAccountID, Equal<BAccount2.bAccountID>,
				And<Match<GL.Branch, Current<AccessInfo.userName>>>>>>>,
			Where<Vendor.bAccountID, IsNotNull,
				And<Optional<RQRequisition.shipDestType>, Equal<POShippingDestination.vendor>,
				Or<Where<GL.Branch.bAccountID, IsNotNull,
					And<Optional<RQRequisition.shipDestType>, Equal<POShippingDestination.company>,
					Or<Where<AR.Customer.bAccountID, IsNotNull,
						And<Optional<RQRequisition.shipDestType>, Equal<POShippingDestination.customer>>>>>>>>>>),
				typeof(BAccount.acctCD), typeof(BAccount.acctName), typeof(BAccount.type), typeof(BAccount.acctReferenceNbr), typeof(BAccount.parentBAccountID),
			SubstituteKey = typeof(BAccount.acctCD), DescriptionField = typeof(BAccount.acctName))]
		public virtual void RQRequisition_ShipToBAccountID_CacheAttached(PXCache sender)
		{ 
		}
	}
}