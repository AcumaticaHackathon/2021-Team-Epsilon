using PX.Data;
using PX.Objects.Extensions;
using PX.Objects.PO;
using System.Collections;

namespace PX.Objects.IN.GraphExtensions
{
	public class NonStockItemMaintRedirectExt : PXGraphExtension<RedirectExtension<NonStockItemMaint>, NonStockItemMaint>
	{
		private RedirectExtension<NonStockItemMaint> BaseRedirect { get; set; }

		public PXAction<POVendorInventory> viewVendorEmployee;
		[PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewVendorEmployee(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<NonStockItemMaint>>();
			return BaseRedirect.ViewCustomerVendorEmployee<POVendorInventory.vendorID>(adapter);
		}

		public PXAction<INItemXRef> viewBAccount;
		[PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select)]
		[PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
		public virtual IEnumerable ViewBAccount(PXAdapter adapter)
		{
			BaseRedirect = Base.GetExtension<RedirectExtension<NonStockItemMaint>>();
			return BaseRedirect.ViewCustomerVendorEmployee<INItemXRef.bAccountID>(adapter);
		}
	}
}
