using PX.Data;
using PX.Objects.AP;
using PX.Objects.Extensions;
using PX.Objects.PO;
using System.Collections;

namespace PX.Objects.FS
{
	public class AppointmentEntryRedirectExt : PXGraphExtension<RedirectExtension<AppointmentEntry>, AppointmentEntry>
	{
		private RedirectExtension<AppointmentEntry> BaseRedirect { get; set; }

		#region ViewPOVendor
		public PXAction<VendorR> viewPOVendor;
		[PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewPOVendor(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<AppointmentEntry>>();
			return BaseRedirect.ViewCustomerVendorEmployee<FSAppointmentDet.poVendorID>(adapter);
		}
		#endregion

		#region ViewPOVendorLocation
		public PXAction<VendorR> viewPOVendorLocation;
		[PXUIField(DisplayName = "View Vendor Location", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewPOVendorLocation(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<AppointmentEntry>>();
			return BaseRedirect.ViewVendorLocation<FSAppointmentDet.poVendorLocationID, FSAppointmentDet.poVendorID>(adapter);
		}
		#endregion

		#region ViewEmployee
		public PXAction<VendorR> viewEmployee;
		[PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewEmployee(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<AppointmentEntry>>();
			return BaseRedirect.ViewCustomerVendorEmployee<FSAppointmentEmployee.employeeID>(adapter);
		}
		#endregion
	}
}
