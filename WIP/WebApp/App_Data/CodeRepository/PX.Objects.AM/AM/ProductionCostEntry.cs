using PX.Data;
using PX.Objects.IN;
using PX.Objects.CM;
using PX.Objects.AM.Attributes;
using System.Collections;
using System.Collections.Generic;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

namespace PX.Objects.AM
{
    /// <summary>
    /// Entry for production costs. If an entry page exists it would be read only. These transactions are built automatically from other entry pages (Move/Labor)
    /// </summary>
    public class ProductionCostEntry : AMBatchSimpleEntryBase
    {
        public PXSelect<AMBatch, Where<AMBatch.docType, Equal<AMDocType.prodCost>>> batch;
        public PXSelect<AMMTran, Where<AMMTran.docType, Equal<Current<AMBatch.docType>>, And<AMMTran.batNbr, Equal<Current<AMBatch.batNbr>>>>> transactions;

        public ProductionCostEntry()
        {
            batch.AllowUpdate = false;
            batch.AllowDelete = false;

            transactions.AllowUpdate = false;
            transactions.AllowInsert = false;
            transactions.AllowDelete = false;

            PXUIFieldAttribute.SetVisible<AMMTran.tranType>(transactions.Cache, null, true);
        }

        #region Cache Attached

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Tran Description", Visible = true)]
        protected virtual void _(Events.CacheAttached<AMMTran.tranDesc> e) { }

        [OperationIDField]
        [PXSelector(typeof(Search<AMProdOper.operationID,
                Where<AMProdOper.orderType, Equal<Current<AMMTran.orderType>>,
                    And<AMProdOper.prodOrdID, Equal<Current<AMMTran.prodOrdID>>>>>),
            SubstituteKey = typeof(AMProdOper.operationCD), ValidateValue = false)]
        protected virtual void AMMTran_OperationID_CacheAttached(PXCache sender)
        {
        }

        [ProductionNbr]
        [ProductionOrderSelector(typeof(AMMTran.orderType), DescriptionField = typeof(AMProdItem.descr))]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        protected virtual void AMMTran_ProdOrdID_CacheAttached(PXCache sender)
        {
        }

        [AMOrderTypeSelector(ValidateValue = false)]
        [AMOrderTypeField]
        protected virtual void AMMTran_OrderType_CacheAttached(PXCache sender)
        {
        }

        [Inventory]
        protected virtual void AMMTran_InventoryID_CacheAttached(PXCache sender)
        {
        }

        [PXDBInt]
        protected virtual void AMMTran_LocationID_CacheAttached(PXCache sender)
        {
        }

        [PXDBInt]
        protected virtual void AMMTran_SiteID_CacheAttached(PXCache sender)
        {
        }

        [INUnit(typeof(AMMTran.inventoryID))]
        protected virtual void AMMTran_UOM_CacheAttached(PXCache sender)
        {
        }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Quantity", Visible = false, Enabled = false)]
        protected virtual void AMMTran_Qty_CacheAttached(PXCache sender)
        {
        }

        #endregion

        protected virtual void AMBatch_DocType_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            e.NewValue = AMDocType.ProdCost;
        }

        protected virtual void AMMTran_DocType_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            e.NewValue = AMDocType.ProdCost;
        }

        protected virtual void AMBatch_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            if (ampsetup.Current.RequireControlTotal == false)
            {
                if (PXCurrencyAttribute.IsNullOrEmpty(((AMBatch)e.Row).TotalAmount) == false)
                {
                    sender.SetValue<AMBatch.controlAmount>(e.Row, ((AMBatch)e.Row).TotalAmount);
                }
                else
                {
                    sender.SetValue<AMBatch.controlAmount>(e.Row, 0m);
                }
            }
        }

        protected virtual void AMBatch_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var batch = (AMBatch) e.Row;
            if (batch == null)
            {
                return;
            }

            var editablebatch = batch.EditableBatch == true;
            var balancedBatch = batch.DeletableBatch == true;

            sender.AllowInsert = true;
            sender.AllowUpdate = editablebatch;
            Delete.SetEnabled(balancedBatch);
            release.SetVisible(balancedBatch);
            release.SetEnabled(balancedBatch);

            PXUIFieldAttribute.SetVisible<AMBatch.controlQty>(sender, batch, false);
            PXUIFieldAttribute.SetVisible<AMBatch.controlAmount>(sender, batch, false);
            PXUIFieldAttribute.SetEnabled<AMBatch.status>(sender, batch, false);
            PXUIFieldAttribute.SetEnabled<AMBatch.hold>(sender, batch, editablebatch);
            PXUIFieldAttribute.SetEnabled<AMBatch.finPeriodID>(sender, batch, editablebatch);
            PXUIFieldAttribute.SetEnabled<AMBatch.controlQty>(sender, batch, editablebatch);
            PXUIFieldAttribute.SetEnabled<AMBatch.controlQty>(sender, batch, editablebatch);
        }



        #region Buttons

        public PXAction<AMBatch> release;
        [PXUIField(DisplayName = Messages.Release, MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Delete)]
        [PXProcessButton]
        public virtual IEnumerable Release(PXAdapter adapter)
        {
            PXCache cache = batch.Cache;
            List<AMBatch> list = new List<AMBatch>();
            foreach (AMBatch amdoc in adapter.Get<AMBatch>())
            {
                if (amdoc.Hold == false && amdoc.Released == false)
                {
                    cache.Update(amdoc);
                    list.Add(amdoc);
                }
            }
            if (list.Count == 0)
            {
                throw new PXException(PX.Objects.IN.Messages.Document_Status_Invalid);
            }
            Save.Press();

            PXLongOperation.StartOperation(this,
                delegate ()
                {
                    AMDocumentRelease.ReleaseDoc(list);
                });

            return list;
        }

        [PXDeleteButton]
        [PXUIField(DisplayName = "")]
        protected virtual IEnumerable delete(PXAdapter a)
        {
            var row = batch.Current;
            if (row == null)
                return a.Get();

            if (ReferenceDeleteGraph.HasReleasedReferenceDocs(this, row, true))
            {
                throw new PXException(Messages.ReleasedBatchExist);
            }

            //make sure the original move/labor batch is not balanced
            AMBatch result = SelectFrom<AMBatch>.Where<AMBatch.docType.IsEqual<@P.AsString>
                .And<AMBatch.batNbr.IsEqual<@P.AsString>>
                .And<AMBatch.released.IsEqual<False>>>.View.Select(this, row.OrigDocType, row.OrigBatNbr);
            if(result != null)
            {
                throw new PXException(Messages.UnreleasedBatchExist);
            }

            batch.Delete(row);
            this.Save.Press();

            return a.Get();
        }

        #endregion

        #region AMBatchSimpleEntryBase members

        public override PXSelectBase<AMBatch> AMBatchDataMember => batch;
        public override PXSelectBase<AMMTran> AMMTranDataMember => transactions;

        #endregion
    }
}
