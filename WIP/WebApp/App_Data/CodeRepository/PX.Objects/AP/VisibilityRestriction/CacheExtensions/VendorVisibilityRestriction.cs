using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;

namespace PX.Objects.AP
{
	public sealed class VendorVisibilityRestriction: PXCacheExtension<Vendor>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorClassID
		// Acuminator disable once PX1030 PXDefaultIncorrectUse [DBField definded in the base DAC]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(Search2<APSetup.dfltVendorClassID,
			InnerJoin<VendorClass, On<VendorClass.vendorClassID, Equal<APSetup.dfltVendorClassID>>>,
			Where<VendorClass.orgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>))]
		[PXSelector(typeof(Search<
			VendorClass.vendorClassID, 
			Where<VendorClass.orgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>),
			DescriptionField = typeof(VendorClass.descr),
			CacheGlobal = true)]
		public string VendorClassID { get; set; }
		#endregion

		#region VOrgBAccountID
		public abstract class vOrgBAccountID : PX.Data.BQL.BqlInt.Field<vOrgBAccountID> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(0, typeof(Search<VendorClass.orgBAccountID, Where<VendorClass.vendorClassID, Equal<Current<Vendor.vendorClassID>>>>))]
		public int? VOrgBAccountID { get; set; }
		#endregion

		#region ParentBAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByUserBranches(typeof(BAccountR.cOrgBAccountID))]
		[RestrictVendorByUserBranches(typeof(BAccountR.vOrgBAccountID))]
		public int? ParentBAccountID { get; set; }
		#endregion

		#region PayToVendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches(typeof(BAccountR.vOrgBAccountID))]
		public int? PayToVendorID { get; set; }
		#endregion
	}
}
