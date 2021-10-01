using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.CN.Compliance.CL.DAC
{
	public sealed class ComplianceDocumentVisibilityRestriction : PXCacheExtension<ComplianceDocument>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region JointVendorInternalId
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? JointVendorInternalId { get; set; }
		#endregion

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorID { get; set; }
		#endregion

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByUserBranches]
		public int? CustomerID { get; set; }
		#endregion
		
		#region SecondaryVendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? SecondaryVendorID { get; set; }
		#endregion
	}
}