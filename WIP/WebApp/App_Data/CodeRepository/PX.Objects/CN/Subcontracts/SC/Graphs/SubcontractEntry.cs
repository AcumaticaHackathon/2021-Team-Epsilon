using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CM;
using PX.Objects.CN.CacheExtensions;
using PX.Objects.CN.Common.Extensions;
using PX.Objects.CN.Common.Helpers;
using PX.Objects.CN.Subcontracts.PO.CacheExtensions;
using PX.Objects.CN.Subcontracts.SC.DAC;
using PX.Objects.CN.Subcontracts.SC.Descriptor.Attributes;
using PX.Objects.CN.Subcontracts.SC.Views;
using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.DR;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.PO;
using PoMessages = PX.Objects.PO.Messages;
using ScMessages = PX.Objects.CN.Subcontracts.SC.Descriptor.Messages;
using ScInfoMessages = PX.Objects.CN.Subcontracts.SC.Descriptor.InfoMessages;
using PX.Common;

namespace PX.Objects.CN.Subcontracts.SC.Graphs
{
    public class SubcontractEntry : POOrderEntry
    {
        public CRAttributeList<POOrder> Answers;

        public SubcontractSetup SubcontractSetup;

        public SubcontractEntry()
        {
            FeaturesSetHelper.CheckConstructionFeature();
            UpdateDocumentSummaryFormLayout();
            UpdateDocumentDetailsGridLayout();
            RemoveShippingHandlers();
            AddSubcontractType();
        }

        [PXUIField]
        [PXDeleteButton(ConfirmationMessage = ScInfoMessages.SubcontractWillBeDeleted)]
        protected virtual IEnumerable delete(PXAdapter adapter)
        {
            Document.Delete(Document.Current);
            Save.Press();
            return adapter.Get();
        }

        public PXAction<POOrder> printSubcontract;
        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Print Subcontract", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual IEnumerable PrintSubcontract(
            PXAdapter adapter,
            [PXString]
            string reportID = null)
            => Report(adapter.Apply(a => a.Menu = "Print Subcontract"), null, "SC641000", false, true);

        public PXAction<POOrder> emailSubcontract;
        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Email Subcontract", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual IEnumerable EmailSubcontract(
            PXAdapter adapter,
            [PXString]
            string notificationCD = null) => Notification(adapter, notificationCD ?? "SUBCONTRACT");

		protected virtual void _(Events.RowSelecting<POSetup> e)
        {
	        PoSetupExt posetupExt = e.Row.GetExtension<PoSetupExt>();

			e.Row.RequireOrderControlTotal = posetupExt.RequireSubcontractControlTotal;
	        e.Row.OrderRequestApproval = posetupExt.SubcontractRequestApproval;
		}

		protected virtual void _(Events.RowInserting<POShipAddress> args)
        {
            args.Cancel = true;
        }

        protected virtual void _(Events.RowInserting<POShipContact> args)
        {
            args.Cancel = true;
        }

        protected virtual void _(Events.RowUpdating<POOrder> args)
        {
            var purchaseOrder = args.NewRow;
            if (purchaseOrder != null)
            {
                purchaseOrder.OrderType = POOrderType.RegularSubcontract;
            }
        }

        [PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
        protected virtual void _(Events.CacheAttached<POOrder.siteID> e)
        {
        }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXRemoveBaseAttribute(typeof(Objects.PO.PO.NumberingAttribute))]
        [AutoNumber(typeof(PoSetupExt.subcontractNumberingID), typeof(POOrder.orderDate))]
        protected virtual void _(Events.CacheAttached<POOrder.orderNbr> e)
        {
        }

        protected virtual void _(Events.RowInserted<POLine> args)
        {
            var orderLine = args.Row;
            if (orderLine != null)
            {
                orderLine.LineType = POLineType.Service;
            }
        }


        [PXBool]
        [DRTerms.Dates(typeof(POLine.dRTermStartDate), typeof(POLine.dRTermEndDate), typeof(POLine.inventoryID), VerifyDatesPresent = false)]
        protected virtual void POLine_ItemRequiresTerms_CacheAttached(PXCache sender) { }

        protected virtual void _(Events.FieldVerifying<POLine, POLine.inventoryID> e)
        {
            RaiseErrorIfReceiptIsRequired((int?) e.NewValue);
        }

        protected override void POOrder_ExpectedDate_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs args)
        {
            if (Transactions.Any())
            {
                Document.Ask(PoMessages.Warning, ScMessages.SubcontractStartDateChangeConfirmation,
                    MessageButtons.YesNo, MessageIcon.Question);
            }
        }

        protected override void POOrder_OrderDate_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs args)
        {
            if (Transactions.Any())
            {
                Document.Ask(PoMessages.Warning, ScMessages.SubcontractDateChangeConfirmation,
                    MessageButtons.YesNo, MessageIcon.Question);
            }
        }

        protected override void POOrder_OrderType_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs args)
        {
            args.NewValue = POOrderType.RegularSubcontract;
        }

        protected override void POOrder_RowSelected(PXCache cache, PXRowSelectedEventArgs args)
        {
            base.POOrder_RowSelected(cache, args);
            if (POSetup.Current != null && args.Row is POOrder order)
            {
                var setupExtension = POSetup.Current.GetExtension<PoSetupExt>();
                SetDefaultPurchaseOrderPreferences();
                UpdatePurchaseOrderBasedOnPreferences(cache, order, setupExtension);
            }
        }

        protected override void POOrder_RowDeleting(PXCache cache, PXRowDeletingEventArgs args)
        {
            var purchaseOrder = (POOrder) args.Row;
            if (purchaseOrder == null)
            {
                return;
            }
            ValidateSubcontractOnDelete(purchaseOrder);
        }

        protected override void POOrder_RowUpdated(PXCache cache, PXRowUpdatedEventArgs args)
        {
            if (args.Row is POOrder order)
            {
                SetControlTotalIfRequired(cache, order);
                base.POOrder_RowUpdated(cache, args);
            }
        }

        protected override void POLine_CuryUnitCost_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs args)
        {
            if (!SkipCostDefaulting && args.Row is POLine subcontractLine)
            {
                var subcontract = Document.Current;
                args.NewValue = GetCurrencyUnitCost(subcontract, subcontractLine, cache);
                if (subcontractLine.InventoryID != null && subcontract?.VendorID != null)
                {
                    APVendorPriceMaint.CheckNewUnitCost<POLine, POLine.curyUnitCost>(
                        cache, subcontractLine, args.NewValue);
                }
            }
        }

        protected override bool GetRequireControlTotal(string aOrderType)
        {
            if (aOrderType == POOrderType.RegularSubcontract)
            {
                PoSetupExt posetupExt = POSetup.Cache.GetExtension<PoSetupExt>(POSetup.Current);
                return posetupExt?.RequireSubcontractControlTotal == true;
            }

            return base.GetRequireControlTotal(aOrderType);
        }

        private void SetControlTotalIfRequired(PXCache cache, POOrder order)
        {
            if (order.Cancelled == false
                && POSetup.Current.RequireOrderControlTotal == false
                && order.CuryOrderTotal != order.CuryControlTotal)
            {
                var controlTotal = order.CuryOrderTotal.IsNullOrZero()
                    ? decimal.Zero
                    : order.CuryOrderTotal;
                cache.SetValueExt<POOrder.curyControlTotal>(order, controlTotal);
            }
        }

        private static void UpdatePurchaseOrderBasedOnPreferences(PXCache cache, POOrder order, PoSetupExt setup)
        {
            order.RequestApproval = setup.SubcontractRequestApproval;
            var isControlTotalVisible = setup.RequireSubcontractControlTotal == true;
            PXUIFieldAttribute.SetVisible<POOrder.curyControlTotal>(cache, order, isControlTotalVisible);
        }

        private void RaiseErrorIfReceiptIsRequired(int? inventoryID)
        {
            InventoryItem item = InventoryItem.PK.Find(this, inventoryID);
            if (item != null)
            {
                if (item.StkItem == true || item.NonStockReceipt == true)
                {
                    var ex = new PXSetPropertyException(PX.Objects.CN.Subcontracts.SC.Descriptor.Messages.ItemRequiringReceiptIsNotSupported);
                    ex.ErrorValue = item.InventoryCD;

                    throw ex;
                }
            }
        }

        private void SetDefaultPurchaseOrderPreferences()
        {
            POSetup.Current.UpdateSubOnOwnerChange = false;
            POSetup.Current.AutoReleaseAP = false;
            POSetup.Current.UpdateSubOnOwnerChange = false;
        }

        private void UpdateDocumentSummaryFormLayout()
        {
            PXUIFieldAttribute.SetDisplayName<POOrder.orderNbr>(Document.Cache,
                ScMessages.Subcontract.SubcontractNumber);
            PXUIFieldAttribute.SetDisplayName<POOrder.expectedDate>(Document.Cache, ScMessages.Subcontract.StartDate);
            PXUIFieldAttribute.SetDisplayName<POOrder.curyOrderTotal>(Document.Cache,
                ScMessages.Subcontract.SubcontractTotal);
            PXUIFieldAttribute.SetDisplayName<POLine.receivedQty>(Transactions.Cache,
                ScMessages.Subcontract.ReceivedQty);
        }

        private void UpdateDocumentDetailsGridLayout()
        {
            PXUIFieldAttribute.SetVisible<POOrder.orderType>(Document.Cache, null, false);
            PXUIFieldAttribute.SetDisplayName<POLine.promisedDate>(Transactions.Cache,
                ScMessages.Subcontract.StartDate);
            PXUIFieldAttribute.SetVisible<POLine.discountSequenceID>(Transactions.Cache, null, true);
            PXUIFieldAttribute.SetVisible<POLine.rcptQtyAction>(Transactions.Cache, null, false);
            PXUIFieldAttribute.SetVisible<POLine.requestedDate>(Transactions.Cache, null, false);
            PXUIFieldAttribute.SetVisible<POLine.promisedDate>(Transactions.Cache, null, false);
            PXUIFieldAttribute.SetVisible<POLine.completed>(Transactions.Cache, null, false);
            PXUIFieldAttribute.SetVisible<POLine.cancelled>(Transactions.Cache, null, false);
            PXUIFieldAttribute.SetVisible<POLine.pONbr>(Transactions.Cache, null, false);
            PXUIFieldAttribute.SetVisible<POLine.baseOrderQty>(Transactions.Cache, null, true);
            PXUIFieldAttribute.SetVisible<POLine.receivedQty>(Transactions.Cache, null, false);
        }

        private void RemoveShippingHandlers()
        {
            RowInserted.RemoveHandler<POOrder>(POOrder_RowInserted);
            FieldUpdated.RemoveHandler<POOrder.shipDestType>(POOrder_ShipDestType_FieldUpdated);
            FieldUpdated.RemoveHandler<POOrder.siteID>(POOrder_SiteID_FieldUpdated);
            FieldUpdated.RemoveHandler<POOrder.shipToLocationID>(POOrder_ShipToLocationID_FieldUpdated);
        }

        private decimal? GetCurrencyUnitCost(POOrder subcontract, POLine subcontractLine, PXCache cache)
        {
            if (subcontractLine.ManualPrice == true || subcontractLine.UOM == null ||
                subcontractLine.InventoryID == null || subcontract?.VendorID == null)
            {
                return subcontractLine.CuryUnitCost.GetValueOrDefault();
            }
            var currencyInfo = currencyinfo.Search<CurrencyInfo.curyInfoID>(subcontract.CuryInfoID);
            return APVendorPriceMaint.CalculateUnitCost(
                cache, subcontractLine.VendorID, subcontract.VendorLocationID, subcontractLine.InventoryID,
                subcontractLine.SiteID, currencyInfo, subcontractLine.UOM, subcontractLine.OrderQty,
                subcontract.OrderDate.GetValueOrDefault(), subcontractLine.CuryUnitCost);
        }

        private void AddSubcontractType()
        {
            var allowedValues = POOrderType.RegularSubcontract.CreateArray();
            PXStringListAttribute.AppendList<POOrder.orderType>(Document.Cache, null, allowedValues, allowedValues);
        }

        private void ValidateSubcontractOnDelete(POOrder purchaseOrder)
        {
            if (purchaseOrder.Hold != true && purchaseOrder.Behavior == POBehavior.ChangeOrder)
            {
                throw new PXException(ScMessages.CanNotDeleteWithChangeOrderBehavior);
            }
            if (GetSubcontractReceiptsCount(purchaseOrder) > 0)
            {
                throw new PXException(ScMessages.SubcontractHasReceiptsAndCannotBeDeleted);
            }
            if (GetSubcontractBillsReleasedCount(purchaseOrder) > 0)
            {
                throw new PXException(ScMessages.SubcontractHasBillsReleasedAndCannotBeDeleted);
            }
            if (GetSubcontractBillsGeneratedCount(purchaseOrder) > 0)
            {
                throw new PXException(ScMessages.SubcontractHasBillsGeneratedAndCannotBeDeleted);
            }
            Transactions.View.SetAnswer(ScMessages.SubcontractLineLinkedToSalesOrderLine, WebDialogResult.OK);
        }
             
        private int? GetSubcontractReceiptsCount(POOrder purchaseOrder)
        {
            return PXSelectGroupBy<POOrderReceipt,
                Where<POOrderReceipt.pONbr, Equal<Required<POOrder.orderNbr>>,
                    And<POOrderReceipt.pOType, Equal<Required<POOrder.orderType>>>>,
                Aggregate<Count>>.Select(this, purchaseOrder.OrderNbr, purchaseOrder.OrderType).RowCount;
        }

        private int? GetSubcontractBillsReleasedCount(POOrder purchaseOrder)
        {
            return PXSelectGroupBy<APTran,
                Where<APTran.pONbr, Equal<Required<POOrder.orderNbr>>,
                    And<APTran.pOOrderType, Equal<Required<POOrder.orderType>>,
                    And<APTran.released, Equal<True>>>>,
                Aggregate<Count>>.Select(this, purchaseOrder.OrderNbr, purchaseOrder.OrderType).RowCount;
        }

        private int? GetSubcontractBillsGeneratedCount(POOrder purchaseOrder)
        {
            return PXSelectGroupBy<APTran,
                Where<APTran.pONbr, Equal<Required<POOrder.orderNbr>>,
                    And<APTran.pOOrderType, Equal<Required<POOrder.orderType>>>>,
                Aggregate<Count>>.Select(this, purchaseOrder.OrderNbr, purchaseOrder.OrderType).RowCount;
        }

    }
}
