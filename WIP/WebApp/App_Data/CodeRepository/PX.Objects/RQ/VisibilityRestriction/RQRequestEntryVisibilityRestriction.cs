using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.PO;

namespace PX.Objects.RQ
{
	public class RQRequestEntryVisibilityRestriction : PXGraphExtension<RQRequestEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search2<
			BAccount2.bAccountID,
			LeftJoin<Vendor,
				On<Vendor.bAccountID, Equal<BAccount2.bAccountID>,
				And<Vendor.type, NotEqual<BAccountType.employeeType>,
				And<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>>,
			LeftJoin<AR.Customer, 
				On<AR.Customer.bAccountID, Equal<BAccount2.bAccountID>>,
			LeftJoin<GL.Branch, 
				On<GL.Branch.bAccountID, Equal<BAccount2.bAccountID>>>>>,
			Where<Vendor.bAccountID, IsNotNull, 
				And<Optional<RQRequest.shipDestType>, Equal<POShippingDestination.vendor>,
				Or<Where<GL.Branch.bAccountID, IsNotNull, 
					And<Optional<RQRequest.shipDestType>, Equal<POShippingDestination.company>,
					Or<Where<AR.Customer.bAccountID, IsNotNull, 
						And<Optional<RQRequest.shipDestType>, Equal<POShippingDestination.customer>>>
				>>>>>>>),
				typeof(BAccount.acctCD), typeof(BAccount.acctName), typeof(BAccount.type), typeof(BAccount.acctReferenceNbr), typeof(BAccount.parentBAccountID),
			SubstituteKey = typeof(BAccount.acctCD), DescriptionField = typeof(BAccount.acctName))]
		public virtual void RQRequest_ShipToBAccountID_CacheAttached(PXCache sender)
		{
		}
	}
}