using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.SO
{
	public sealed class SOShipmentVisibilityRestriction : PXCacheExtension<SOShipment>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByUserBranches(typeof(Customer.cOrgBAccountID), typeof(Or<Current<SOShipment.shipmentType>, Equal<SOShipmentType.transfer>>))]
		public int? CustomerID { get; set; }
		#endregion
	}
}