using PX.Data;
using PX.Objects.AP;
using PX.Objects.Extensions;
using System.Collections;

namespace PX.Objects.FS
{
	public class ServiceOrderEntryRedirectExt : PXGraphExtension<RedirectExtension<ServiceOrderEntry>, ServiceOrderEntry>
	{
		private RedirectExtension<ServiceOrderEntry> BaseRedirect { get; set; }

		#region ViewPOVendor
		public PXAction<VendorR> viewPOVendor;
		[PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewPOVendor(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<ServiceOrderEntry>>();
			return BaseRedirect.ViewCustomerVendorEmployee<FSSODet.poVendorID>(adapter);
		}
		#endregion

		#region ViewPOVendorLocation
		public PXAction<VendorR> viewPOVendorLocation;
		[PXUIField(DisplayName = "View Vendor Location", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewPOVendorLocation(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<ServiceOrderEntry>>();
			return BaseRedirect.ViewVendorLocation<FSSODet.poVendorLocationID, FSSODet.poVendorID>(adapter);
		}
		#endregion

		#region ViewEmployee
		public PXAction<VendorR> viewEmployee;
		[PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewEmployee(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<ServiceOrderEntry>>();
			return BaseRedirect.ViewCustomerVendorEmployee<FSSOEmployee.employeeID>(adapter);
		}
		#endregion
	}
}
