using Customization;
using PX.Objects.AM.Attributes;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.AM.Upgrade
{
    /// <summary>
    /// In early release versions with Allocations there was an issue when closing, deleting, cancelling a production order would either leave the allocation record or orphan the allocation record.
    /// This creates an issue when looking at allocation details because the record remain and the user doesn't have a way to clear them out.
    /// </summary>
    internal sealed class UpgradeClosedProductionAllocations : UpgradeProcessVersionBase
    {
        public UpgradeClosedProductionAllocations(UpgradeProcess upgradeGraph, CustomizationPlugin plugin) : base(upgradeGraph, plugin)
        {
        }

        public UpgradeClosedProductionAllocations(UpgradeProcess upgradeGraph) : base(upgradeGraph)
        {
        }

        public override int Version => UpgradeVersions.Version2019R1Ver31;

        public override void ProcessTables()
        {
            ProcessClosedProdItemSplitPlanRecords();
            ProcessClosedProdMatlSplitPlanRecords();
        }

        private void ProcessClosedProdItemSplitPlanRecords()
        {
            _ProcessClosedProdItemSplitPlanRecords();
        }

        internal static void _ProcessClosedProdItemSplitPlanRecords()
        {
            var allocGraph = PXGraph.CreateInstance<QtyAllocationUpgradeGraph>();

            foreach (PXResult<INItemPlan, AMProdItemSplit, AMProdItemStatus> result in PXSelectJoin<
                INItemPlan,
                InnerJoin<AMProdItemSplit,
                    On<INItemPlan.planID, Equal<AMProdItemSplit.planID>>,
                InnerJoin<AMProdItemStatus,
                    On<AMProdItemSplit.orderType, Equal<AMProdItemStatus.orderType>,
                    And<AMProdItemSplit.prodOrdID, Equal<AMProdItemStatus.prodOrdID>>>>>,
                Where<AMProdItemStatus.statusID, Equal<ProductionOrderStatus.closed>,
                    Or<AMProdItemStatus.statusID, Equal<ProductionOrderStatus.cancel>>>>
                .Select(allocGraph))
            {
                var split = (AMProdItemSplit)result;

                if (string.IsNullOrWhiteSpace(split?.ProdOrdID))
                {
                    continue;
                }
#if DEBUG
                AMDebug.TraceWriteMethodName($"{split.OrderType}:{split.ProdOrdID}:{split.SplitLineNbr}; PlanID = {split.PlanID}");
#endif
                allocGraph.ProdItemSplits.Delete(split);
            }

            allocGraph.Actions.PressSave();
        }

        private void ProcessClosedProdMatlSplitPlanRecords()
        {
            _ProcessClosedProdMatlSplitPlanRecords();
        }

        internal static void _ProcessClosedProdMatlSplitPlanRecords()
        {
            var allocGraph = PXGraph.CreateInstance<QtyAllocationUpgradeGraph>();

            foreach (PXResult<INItemPlan, AMProdMatlSplit, AMProdItemStatus> result in PXSelectJoin<
                INItemPlan,
                InnerJoin<AMProdMatlSplit,
                    On<INItemPlan.planID, Equal<AMProdMatlSplit.planID>>,
                InnerJoin<AMProdItemStatus,
                    On<AMProdMatlSplit.orderType, Equal<AMProdItemStatus.orderType>,
                    And<AMProdMatlSplit.prodOrdID, Equal<AMProdItemStatus.prodOrdID>>>>>,
                Where<AMProdItemStatus.statusID, Equal<ProductionOrderStatus.closed>,
                    Or<AMProdItemStatus.statusID, Equal<ProductionOrderStatus.cancel>>>>
                .Select(allocGraph))
            {
                //var itemPlan = (INItemPlan) result;
                var split = (AMProdMatlSplit)result;

                if (string.IsNullOrWhiteSpace(split?.ProdOrdID))
                {
                    continue;
                }
#if DEBUG
                AMDebug.TraceWriteMethodName($"{split.OrderType}:{split.ProdOrdID}:{split.OperationID}:{split.LineID}:{split.SplitLineNbr}; PlanID = {split.PlanID}");
#endif
                allocGraph.ProdMatlSplits.Delete(split);
            }

            allocGraph.Actions.PressSave();
        }
    }

    /// <summary>
    /// Process for correcting allocations related to closed production orders
    /// </summary>
    public class CorrectClosedProductionOrderAllocations
    {
        public void ProcessAsLongOperation(PXGraph graph)
        {
            PXLongOperation.StartOperation(graph, () => Process());
        }

        public void Process()
        {
            PXTrace.WriteInformation("Processing Closed Production Item Allocations");
            UpgradeClosedProductionAllocations._ProcessClosedProdItemSplitPlanRecords();

            PXTrace.WriteInformation("Processing Closed Production Material Allocations");
            UpgradeClosedProductionAllocations._ProcessClosedProdMatlSplitPlanRecords();

            ProcessOrphanedMfgItemPlans();
        }

        /// <summary>
        /// Delete <see cref="INItemPlan"/> which are marked with PlanType of manufacturing but do not have a manufacturing related record linked by PlanID
        /// </summary>
        public void ProcessOrphanedMfgItemPlans()
        {
            PXTrace.WriteInformation("Processing Orphaned Manufacturing Item Plans");

            var allocGraph = PXGraph.CreateInstance<QtyAllocationUpgradeGraph>();
            Common.Cache.AddCacheView<INItemPlan>(allocGraph);

            foreach (PXResult<INItemPlan, AMProdMatlSplit, AMProdItemSplit> result in PXSelectJoin<
                INItemPlan,
                LeftJoin<AMProdMatlSplit,
                    On<INItemPlan.planID, Equal<AMProdMatlSplit.planID>>,
                LeftJoin<AMProdItemSplit,
                    On<INItemPlan.planID, Equal<AMProdItemSplit.planID>>>>,
                Where<AMProdMatlSplit.prodOrdID, IsNull,
                    And<AMProdItemSplit.prodOrdID, IsNull,
                    //( 'M1', 'M2', 'M5', 'M6', 'M7', 'M9', 'MA', 'MB', 'MC', 'MD', 'ME' ) /* NOT Purchase (M3, M4) or Sales (M8) */
                    And<
                        Where<INItemPlan.planType, Equal<INPlanConstants.planM1>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planM2>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planM5>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planM6>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planM7>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planM9>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planMA>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planMB>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planMC>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planMD>,
                            Or<INItemPlan.planType, Equal<INPlanConstants.planME>>>>>>>>>>>>>>>>
                .Select(allocGraph))
            {
                var itemPlan = (INItemPlan) result;
                if (itemPlan?.PlanID == null)
                {
                    continue;
                }

                allocGraph.Caches[typeof(INItemPlan)].Delete(itemPlan);

            }

            allocGraph.Actions.PressSave();
        }
    }
}