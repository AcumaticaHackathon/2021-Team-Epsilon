using PX.Data;
using PX.Objects.Extensions;
using PX.Objects.PO;
using System.Collections;

namespace PX.Objects.IN.GraphExtensions
{
	public class InventoryItemMaintRedirectExt : PXGraphExtension<RedirectExtension<InventoryItemMaint>, InventoryItemMaint>
	{
		private RedirectExtension<InventoryItemMaint> BaseRedirect { get; set; }

		public PXAction<POVendorInventory> viewVendorEmployee;
		[PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewVendorEmployee(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<InventoryItemMaint>>();
			return BaseRedirect.ViewCustomerVendorEmployee<POVendorInventory.vendorID>(adapter);
		}

		public PXAction<POVendorInventory> viewVendorLocation;
		[PXUIField(DisplayName = "View Vendor Location", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewVendorLocation(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<InventoryItemMaint>>();
			return BaseRedirect.ViewVendorLocation<POVendorInventory.vendorLocationID, POVendorInventory.vendorID>(adapter);
		}

		public PXAction<INItemXRef> viewBAccount;
		[PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewBAccount(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<InventoryItemMaint>>();
			return BaseRedirect.ViewCustomerVendorEmployee<INItemXRef.bAccountID>(adapter);
		}
	}
}

