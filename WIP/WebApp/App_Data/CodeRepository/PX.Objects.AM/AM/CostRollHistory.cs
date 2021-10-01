using PX.Data;
using PX.Objects.AM.Attributes;

namespace PX.Objects.AM
{
    public class CostRollHistory : PXGraph<CostRollHistory>
    {
        [PXFilterable]
        public PXSelectJoin<AMBomCostHistory,
            LeftJoin<AMBomItem, On<AMBomCostHistory.bOMID, Equal<AMBomItem.bOMID>,
                And<AMBomCostHistory.revisionID, Equal<AMBomItem.revisionID>>>>> CostRollHistoryRecords;
        public PXSetup<AMBSetup> ambsetup;
        public PXSelect<AMBomItem> BomItemRecs;

        public CostRollHistory()
        {
            CostRollHistoryRecords.AllowDelete = false;
            CostRollHistoryRecords.AllowInsert = false;
            CostRollHistoryRecords.AllowUpdate = false;
        }

        [PXUIField(DisplayName = "Current Status")]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        protected virtual void _(Events.CacheAttached<AMBomItem.status> e) { }

        [PXUIField(DisplayName = "Date")]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        protected virtual void _(Events.CacheAttached<AMBomCostHistory.createdDateTime> e) { }

        public PXAction<AMBomCostHistory> ViewBOM;
        [PXUIField(DisplayName = "View BOM")]
        [PXButton]
        protected virtual void viewBOM()
        {
            BOMMaint.Redirect(CostRollHistoryRecords?.Current?.BOMID, CostRollHistoryRecords?.Current?.RevisionID);
        }

    }
}