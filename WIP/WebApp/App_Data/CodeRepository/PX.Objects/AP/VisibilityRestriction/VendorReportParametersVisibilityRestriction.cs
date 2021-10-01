using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;

namespace PX.Objects.Common.DAC.ReportParameters
{
	public sealed class VendorReportParametersVisibilityRestriction : PXCacheExtension<VendorReportParameters>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorClassID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorClassByUserBranches]
		public string VendorClassID { get; set; }
		#endregion

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorID { get; set; }
		#endregion

		#region VendorActiveID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorActiveID { get; set; }
		#endregion

		#region VendorIDPOReceipt
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorIDPOReceipt { get; set; }
		#endregion

		#region VendorIDNonEmployeeActive
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorIDNonEmployeeActive { get; set; }
		#endregion
	}
}
