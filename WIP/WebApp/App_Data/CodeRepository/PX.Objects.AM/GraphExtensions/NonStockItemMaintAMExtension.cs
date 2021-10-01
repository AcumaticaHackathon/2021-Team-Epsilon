using System;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.AM.GraphExtensions
{
	using PX.Data.WorkflowAPI;
	using PX.Objects.AM.Attributes;

    public class NonStockItemMaintAMExtension : PXGraphExtension<NonStockItemMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>();
        }

        public override void Configure(PXScreenConfiguration configuration)
        {
            configuration
                .GetScreenConfigurationContext<NonStockItemMaint, InventoryItem>()
                .UpdateScreenConfigurationFor(screen =>
                    screen.WithActions(actions =>
                        actions.Add<NonStockItemMaintAMExtension>(e => e.WhereUsedInq, a => a.InFolder(FolderType.InquiriesFolder))));
        }

        [PXOverride]
        public void Persist(Action del)
        {
            var estimateInventoryCdUpdateRequired = !Base.Item.Cache.IsCurrentRowInserted() &&
                                                    EstimateGraphHelper
                                                        .InventoryCDUpdateRequired<InventoryItem.inventoryCD>(
                                                            Base.Item.Cache);

            del?.Invoke();

            if (estimateInventoryCdUpdateRequired)
            {
                EstimateGraphHelper.UpdateEstimateInventoryCD(Base.Item.Current, Base);
            }
        }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [MaterialDefaultMarkFor.NonStockItemList]
        protected virtual void InventoryItem_AMDefaultMarkFor_CacheAttached(PXCache sender)
        {
        }

        public PXAction<InventoryItem> WhereUsedInq;
        [PXButton]
        [PXUIField(DisplayName = "BOM Where Used", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
        public virtual System.Collections.IEnumerable whereUsedInq(PXAdapter adapter)
        {
            if (Base.Item.Current == null
                || Base.Item.Current.InventoryID.GetValueOrDefault() <= 0)
            {
                return adapter.Get();
            }

            var inqGraph = PXGraph.CreateInstance<BOMWhereUsedInq>();
            inqGraph.Filter.Current.InventoryID = Base.Item.Current.InventoryID;
            PXRedirectHelper.TryRedirect(inqGraph, PXRedirectHelper.WindowMode.NewWindow);

            return adapter.Get();
        }
    }
}