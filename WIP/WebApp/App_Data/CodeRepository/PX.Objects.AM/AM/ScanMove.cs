using PX.Objects.AM.Attributes;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Attributes;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WMSBase = PX.Objects.IN.WarehouseManagementSystemGraph<PX.Objects.AM.ScanMove, PX.Objects.AM.ScanMoveHost, PX.Objects.AM.AMBatch, PX.Objects.AM.ScanMove.Header>;

namespace PX.Objects.AM
{
    public class ScanMoveHost : MoveEntry
    {
        public override Type PrimaryItemType => typeof(ScanMove.Header);
        public PXFilter<ScanMove.Header> HeaderView;
    }

    public class ScanMove : WMSBase
    {
        public class UserSetup : PXUserSetupPerMode<UserSetup, ScanMoveHost, Header, AMScanUserSetup, AMScanUserSetup.userID, AMScanUserSetup.mode, Modes.scanMove> { }

        #region DACs
        [PXCacheName("MFG Header")]
        [Serializable]
        public class Header : WMSHeader, ILSMaster
        {
            #region BatNbr
            [PXUnboundDefault(typeof(AMBatch.batNbr))]
            [PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
            [PXUIField(DisplayName = "Batch Nbr.", Enabled = false)]
            [PXSelector(typeof(Search<AMBatch.batNbr, Where<AMBatch.docType, Equal<AMDocType.move>>>))]
            public override string RefNbr { get; set; }
            public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
            #endregion
            #region TranDate
            [PXDate]
            [PXUnboundDefault(typeof(AccessInfo.businessDate))]
            public virtual DateTime? TranDate { get; set; }
            public abstract class tranDate : PX.Data.BQL.BqlDateTime.Field<tranDate> { }
            #endregion
            #region SiteID
            [Site]
            public virtual int? SiteID { get; set; }
            public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
            #endregion
            #region LocationID
            [Location]
            public virtual int? LocationID { get; set; }
            public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
            #endregion
            #region InventoryID
            public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
            #endregion
            #region SubItemID
            public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
            #endregion
            #region LotSerialNbr
            //[AMLotSerialNbr(typeof(inventoryID), typeof(subItemID), typeof(locationID), PersistingCheck = PXPersistingCheck.Nothing)]
            [PXDBString(INLotSerialStatus.lotSerialNbr.LENGTH, IsUnicode = true, InputMask = "")]
            [PXUIField(DisplayName = "Lot/Serial Nbr.", FieldClass = "LotSerial")]
            [PXDefault("")]
            public virtual string LotSerialNbr { get; set; }
            public abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }
            #endregion
            #region ExpirationDate
            [INExpireDate(typeof(inventoryID), PersistingCheck = PXPersistingCheck.Nothing)]
            public virtual DateTime? ExpireDate { get; set; }
            public abstract class expireDate : PX.Data.BQL.BqlDateTime.Field<expireDate> { }
            #endregion

            #region LotSerTrack
            [PXString(1, IsFixed = true)]
            public virtual String LotSerTrack { get; set; }
            public abstract class lotSerTrack : PX.Data.BQL.BqlString.Field<lotSerTrack> { }
            #endregion
            #region LotSerAssign
            [PXString(1, IsFixed = true)]
            public virtual String LotSerAssign { get; set; }
            public abstract class lotSerAssign : PX.Data.BQL.BqlString.Field<lotSerAssign> { }
            #endregion
            #region LotSerTrackExpiration
            [PXBool]
            public virtual Boolean? LotSerTrackExpiration { get; set; }
            public abstract class lotSerTrackExpiration : PX.Data.BQL.BqlBool.Field<lotSerTrackExpiration> { }
            #endregion
            #region AutoNextNbr
            [PXBool]
            public virtual Boolean? AutoNextNbr { get; set; }
            public abstract class autoNextNbr : PX.Data.BQL.BqlBool.Field<autoNextNbr> { }
            #endregion
            #region OrderType
            public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

            protected String _OrderType;
            [PXString(2, IsFixed = true, InputMask = ">aa")]
            [PXUIField(DisplayName = "Order Type")]
            [PXUnboundDefault(typeof(AMPSetup.defaultOrderType))]
            public virtual String OrderType
            {
                get
                {
                    return this._OrderType;
                }
                set
                {
                    this._OrderType = value;
                }
            }
            #endregion
            #region ProdOrdID
            public abstract class prodOrdID : PX.Data.BQL.BqlString.Field<prodOrdID> { }

            protected String _ProdOrdID;
            [PXUnboundDefault]
            [PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
            [PXUIField(DisplayName = "Production Nbr", Visibility = PXUIVisibility.SelectorVisible)]
            public virtual String ProdOrdID
            {
                get
                {
                    return this._ProdOrdID;
                }
                set
                {
                    this._ProdOrdID = value;
                }
            }
            #endregion
            #region OperationID
            public abstract class operationID : PX.Data.BQL.BqlInt.Field<operationID> { }

            protected int? _OperationID;
            [PXInt]
            [PXUIField(DisplayName = "Operation ID")]
            [PXSelector(typeof(Search<AMProdOper.operationID,
                    Where<AMProdOper.orderType, Equal<Current<orderType>>,
                        And<AMProdOper.prodOrdID, Equal<Current<prodOrdID>>>>>),
                SubstituteKey = typeof(AMProdOper.operationCD))]
            [PXFormula(typeof(Validate<AMMTran.prodOrdID>))]
            public virtual int? OperationID
            {
                get
                {
                    return this._OperationID;
                }
                set
                {
                    this._OperationID = value;
                }
            }
            #endregion
            #region LastOperationID
            public abstract class lastOperationID : PX.Data.BQL.BqlInt.Field<lastOperationID> { }

            protected int? _LastOperationID;
            [OperationIDField(DisplayName = "Last Operation ID")]
            [PXSelector(typeof(Search<AMProdOper.operationID,
                    Where<AMProdOper.orderType, Equal<Current<AMProdItem.orderType>>,
                        And<AMProdOper.prodOrdID, Equal<Current<AMProdItem.prodOrdID>>>>>),
                SubstituteKey = typeof(AMProdOper.operationCD), ValidateValue = false)]
            public virtual int? LastOperationID
            {
                get
                {
                    return this._LastOperationID;
                }
                set
                {
                    this._LastOperationID = value;
                }
            }
            #endregion
            #region QtyRemaining  (Unbound)
            public abstract class qtyRemaining : PX.Data.BQL.BqlDecimal.Field<qtyRemaining> { }

            protected Decimal? _QtyRemaining;
            [PXQuantity]            
            [PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
            [PXUIField(DisplayName = "Qty Remaining", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
            public virtual Decimal? QtyRemaining
            {
                get
                {
                    return this._QtyRemaining;
                }
                set
                {
                    this._QtyRemaining = value;
                }
            }
            #endregion

            #region NoteID
            [BorrowedNote(typeof(AMBatch), typeof(MoveEntry))]
            public override Guid? NoteID { get; set; }
            public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
            #endregion

            #region ILSMaster implementation
            public string TranType => string.Empty;
            public short? InvtMult { get => -1; set { } }
            public int? ProjectID { get; set; }
            public int? TaskID { get; set; }
			bool? ILSMaster.IsIntercompany => false;
            #endregion
        }
        #endregion
        #region Views
        public override PXFilter<Header> HeaderView => Base.HeaderView;
        public PXSetupOptional<AMScanSetup, Where<AMScanSetup.branchID, Equal<Current<AccessInfo.branchID>>>> Setup;
        public PXSetupOptional<AMPSetup> AMSetup;
        #endregion
        #region Buttons
        public PXAction<Header> ScanRelease;
        [PXButton, PXUIField(DisplayName = "Release")]
        protected virtual IEnumerable scanRelease(PXAdapter adapter) => scanBarcode(adapter, ScanCommands.Release);

        public PXAction<Header> Review;
        [PXButton, PXUIField(DisplayName = "Review")]
        protected virtual IEnumerable review(PXAdapter adapter) => adapter.Get();
        #endregion

        #region Event Handlers
        protected override void _(Events.RowSelected<Header> e)
        {
            base._(e);

            bool notReleaseAndHasLines = Batch?.Released != true && ((AMMTran)Base.transactions.Select()) != null;
            ScanRemove.SetEnabled(notReleaseAndHasLines);
            ScanRelease.SetEnabled(notReleaseAndHasLines);
            ScanModeInReceive.SetEnabled(e.Row != null && e.Row.Mode != Modes.ScanMove);
            ScanConfirm.SetEnabled(Batch?.Released != true && e.Row?.ScanState == ScanStates.Confirm);

            Review.SetVisible(Base.IsMobile);

            Logs.AllowInsert = Logs.AllowDelete = Logs.AllowUpdate = false;
            Base.transactions.AllowInsert = false;
            Base.transactions.AllowDelete = Base.transactions.AllowUpdate = (Batch == null || Batch.Released != true);
        }

        protected virtual void _(Events.RowSelected<AMMTran> e)
        {
            bool isMobileAndNotReleased = Base.IsMobile && (Batch == null || Batch.Released != true);

            Base.transactions.Cache
            .Adjust<PXUIFieldAttribute>()
            .For<AMMTran.inventoryID>(ui => ui.Enabled = false)
            .SameFor<AMMTran.tranDesc>()
            .SameFor<AMMTran.qty>()
            .SameFor<AMMTran.uOM>()
            .For<AMMTran.lotSerialNbr>(ui => ui.Enabled = isMobileAndNotReleased)
            .SameFor<AMMTran.expireDate>()
            .SameFor<AMMTran.locationID>();
        }

        protected virtual void _(Events.FieldDefaulting<Header, Header.siteID> e) => e.NewValue = IsWarehouseRequired() ? null : DefaultSiteID;

        protected virtual void _(Events.FieldDefaulting<Header, Header.orderType> e) => e.NewValue = !UseDefaultOrderType() ? null : AMSetup.Current.DefaultOrderType;

        protected virtual void _(Events.RowPersisted<Header> e)
        {
            e.Row.RefNbr = Batch?.BatNbr;
            e.Row.TranDate = Batch?.TranDate;
            e.Row.NoteID = Batch?.NoteID;

            Base.transactions.Cache.Clear();
            Base.transactions.Cache.ClearQueryCache();
        }

        protected virtual void _(Events.FieldUpdated<Header, Header.refNbr> e)
        {
            Base.batch.Current = e.NewValue == null ? null : Base.batch.Search<AMBatch.batNbr>(e.NewValue);
        }

        protected virtual void _(Events.RowUpdated<AMScanUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);
        protected virtual void _(Events.RowInserted<AMScanUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);

        [Location]
        protected virtual void AMMTran_LocationID_CacheAttached(PXCache sender)
        { }

        [OperationIDField]
        [PXSelector(typeof(Search<AMProdOper.operationID,
                Where<AMProdOper.orderType, Equal<Current<AMMTran.orderType>>,
                    And<AMProdOper.prodOrdID, Equal<Current<AMMTran.prodOrdID>>>>>),
            SubstituteKey = typeof(AMProdOper.operationCD))]
        protected virtual void AMMTran_OperationID_CacheAttached(PXCache sender)
        { }

        #endregion

        private AMBatch Batch => Base.batch.Current;
        protected override BqlCommand DocumentSelectCommand()
            => new SelectFrom<AMBatch>.
                Where<AMBatch.docType.IsEqual<AMBatch.docType.AsOptional>
                    .And<AMBatch.batNbr.IsEqual<AMBatch.batNbr.AsOptional>>>();

        protected virtual bool UseDefaultOrderType() => Setup.Current.UseDefaultOrderType == true;
        protected virtual bool IsWarehouseRequired() => UserSetup.For(Base).DefaultWarehouse != true || DefaultSiteID == null;
        protected virtual bool IsLotSerialRequired() => UserSetup.For(Base).DefaultLotSerialNumber != true;
        protected virtual bool IsExpirationDateRequired() => UserSetup.For(Base).DefaultExpireDate != true || EnsureExpireDateDefault() == null;
        protected virtual bool IsLastOperation() => (HeaderView.Current.OperationID == HeaderView.Current.LastOperationID);

        protected override WMSModeOf<ScanMove, ScanMoveHost> DefaultMode => Modes.ScanMove;
        public override string CurrentModeName =>
            HeaderView.Current.Mode == Modes.ScanMove ? Msg.ScanMoveMode :
            Msg.FreeMode;
        protected override string GetModePrompt()
        {
            if (HeaderView.Current.Mode == Modes.ScanMove)
            {
                if (HeaderView.Current.OrderType == null)
                    return Localize(Msg.OrderTypePrompt);
                if (HeaderView.Current.ProdOrdID == null)
                    return Localize(Msg.ProdOrdPrompt);
                if (HeaderView.Current.OperationID == null)
                    return Localize(Msg.OperationPrompt);
                if(IsLastOperation())
                {
                    if (IsWarehouseRequired() && HeaderView.Current.SiteID == null)
                        return Localize(Msg.WarehousePrompt);
                    if (HeaderView.Current.LocationID == null)
                        return Localize(Msg.LocationPrompt);
                    if (HeaderView.Current.LotSerialNbr == null && IsLotSerialRequired())
                        return Localize(Msg.LotSerialPrompt);
                }

                return Localize(Msg.ConfirmationPrompt);
            }
            return null;
        }

        protected override bool ProcessCommand(string barcode)
        {
            switch (barcode)
            {
                case ScanCommands.Confirm:
                    if (HeaderView.Current.Remove != true) ProcessConfirm();
                    else ProcessConfirmRemove();
                    return true;

                case ScanCommands.Remove:
                    HeaderView.Current.Remove = true;
                    SetScanState(UseDefaultOrderType() ? ScanStates.ProdOrd : ScanStates.OrderType, Msg.RemoveMode);
                    return true;

                case ScanCommands.OrderType:
                    SetScanState(ScanStates.OrderType);
                    return true;

                case ScanCommands.Release:
                    ProcessRelease();
                    return true;
            }
            return false;
        }

        protected override bool ProcessByState(Header doc)
        {
            if (Batch?.Released == true)
            {
                ClearHeaderInfo();
                HeaderSetter.WithEventFiring.Set(h => h.RefNbr, null);
                HeaderView.Cache.SetDefaultExt<WMSHeader.noteID>(HeaderView.Current);
            }

            switch (doc.ScanState)
            {
                case ScanStates.OrderType:
                    ProcessOrderType(doc.Barcode);
                    return true;
                case ScanStates.ProdOrd:
                    ProcessProdOrd(doc.Barcode);
                    return true;
                case ScanStates.Operation:
                    ProcessOperation(doc.Barcode);
                    return true;
                case ScanStates.Warehouse:
                    ProcessWarehouse(doc.Barcode);
                    return true;
                case ScanStates.Item:
                    ProcessItemBarcode(doc.Barcode);
                    return true;
                case ScanStates.Confirm:
                    ProcessQtyBarcode(doc.Barcode);
                    return true;
                default:
                    return base.ProcessByState(doc);
            }
        }

        protected virtual void ProcessOperation(string barcode)
        {

            AMProdOper oper = PXSelectReadonly<AMProdOper,
                Where<AMProdOper.orderType, Equal<Required<AMProdOper.orderType>>,
                And<AMProdOper.prodOrdID, Equal<Required<AMProdOper.prodOrdID>>,
                And<AMProdOper.operationCD, Equal<Required<Header.barcode>>>>>>.Select(Base, HeaderView.Current.OrderType, HeaderView.Current.ProdOrdID, barcode);
            if (oper == null)
                ReportError(Msg.OperationMissing, barcode);
            else
            {
                HeaderSetter.Set(x => x.OperationID, oper.OperationID);
                HeaderSetter.Set(x => x.QtyRemaining, oper.QtyRemaining);
                if(UseRemainingQty && !IsSetQty)
                {
                    HeaderSetter.Set(x => x.Qty, oper.QtyRemaining);
                }
                if (IsLastOperation())
                    SetScanState(IsWarehouseRequired() ? ScanStates.Warehouse : ScanStates.Location);
                else
                {
                    ProcessItem();
                }                
            }
        }

        protected virtual void ProcessProdOrd(string barcode)
        {
            if (HeaderView.Current.OrderType == null)
                ReportError(Msg.OrderTypeMissing, barcode);

            AMProdItem prodOrd = PXSelect<AMProdItem,
                Where<AMProdItem.orderType, Equal<Required<AMProdItem.orderType>>,
                And<AMProdItem.prodOrdID, Equal<Required<Header.barcode>>>>>.Select(Base, HeaderView.Current.OrderType, barcode);
            if (prodOrd == null)
                ReportError(Msg.ProdOrdMissing, barcode);
            else if (prodOrd.Function == OrderTypeFunction.Disassemble)
                ReportError(Msg.ProdOrdWrongType, prodOrd.ProdOrdID);
            else if (!ProductionStatus.IsReleasedTransactionStatus(prodOrd))
                ReportError(Msg.ProdOrdWrongStatus, prodOrd.OrderType, prodOrd.ProdOrdID, ProductionOrderStatus.GetStatusDescription(prodOrd.StatusID));
            else
            {
                HeaderView.Current.ProdOrdID = prodOrd.ProdOrdID;
                HeaderView.Current.InventoryID = prodOrd.InventoryID;
                HeaderView.Current.LastOperationID = prodOrd.LastOperationID;
                SetScanState(ScanStates.Operation);
            }
        }

        protected virtual void ProcessOrderType(string barcode)
        {
            AMOrderType orderType = PXSelectReadonly<AMOrderType,
                Where<AMOrderType.orderType, Equal<Required<Header.barcode>>>>.Select(Base, barcode);
            if (orderType == null)
                ReportError(Msg.OrderTypeMissing, barcode);
            else
            {
                HeaderView.Current.OrderType = orderType.OrderType;
                SetScanState(ScanStates.ProdOrd);
            }

        }

        protected virtual void ProcessWarehouse(string barcode)
        {
            INSite site =
                PXSelectReadonly<INSite,
                Where<INSite.siteCD, Equal<Required<Header.barcode>>>>
                .Select(Base, barcode);

            if (site == null)
            {
                ReportError(Msg.WarehouseMissing, barcode);
            }
            else if (IsValid<Header.siteID>(site.SiteID, out string error) == false)
            {
                ReportError(error);
                return;
            }
            else
            {
                HeaderView.Current.SiteID = site.SiteID;
                SetScanState(ScanStates.Location, Msg.WarehouseReady, site.SiteCD);
            }
        }

        protected override void ProcessLocationBarcode(string barcode)
        {
            INLocation location = ReadLocationByBarcode(HeaderView.Current.SiteID, barcode);
            if (location == null)
                return;

            HeaderView.Current.LocationID = location.LocationID;
            ProcessItem();
        }

        protected virtual void ProcessItem()
        {
            var invtID = HeaderView.Current.InventoryID;
            InventoryItem inventoryItem = PXSelectReadonly<InventoryItem,
                Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>.Select(Base, invtID);
            if (inventoryItem == null)
            {
                ReportError(Msg.InventoryMissing, invtID);
            }

            ProcessItemBarcode(inventoryItem.InventoryCD);
        }

        protected override void ProcessItemBarcode(string barcode)
        {
            var item = ReadItemByBarcode(barcode);
            if (item == null)
            {
                if (HandleItemAbsence(barcode) == false)
                    ReportError(Msg.InventoryMissing, barcode);
                return;
            }

            INItemXRef xref = item;
            InventoryItem inventoryItem = item;
            INLotSerClass lsclass = item;
            var uom = xref.UOM ?? inventoryItem.SalesUnit;

            if (lsclass.LotSerTrack == INLotSerTrack.SerialNumbered &&
                uom != inventoryItem.BaseUnit)
            {
                ReportError(Msg.SerialItemNotComplexQty);
                return;
            }

            HeaderView.Current.InventoryID = xref.InventoryID;
            HeaderView.Current.SubItemID = xref.SubItemID;
            if (HeaderView.Current.UOM == null)
                HeaderView.Current.UOM = uom;
            HeaderView.Current.LotSerTrack = lsclass.LotSerTrack;
            HeaderView.Current.LotSerAssign = lsclass.LotSerAssign;
            HeaderView.Current.LotSerTrackExpiration = lsclass.LotSerTrackExpiration;
            HeaderView.Current.AutoNextNbr = lsclass.AutoNextNbr;

            Report(Msg.InventoryReady, inventoryItem.InventoryCD);

            if (IsLastOperation() && IsLotSerialRequired() && HeaderView.Current.LotSerTrack != INLotSerTrack.NotNumbered)
                SetScanState(ScanStates.LotSerial);
            else
                SetScanState(ScanStates.Confirm);
        }

        protected virtual bool HandleItemAbsence(string barcode)
        {
            ProcessLocationBarcode(barcode);
            if (Info.Current.MessageType == WMSMessageTypes.Information)
                return true; // location found

            return false;
        }

        protected override void ProcessLotSerialBarcode(string barcode)
        {
            if (IsValid<Header.lotSerialNbr>(barcode, out string error) == false)
            {
                ReportError(error);
                return;
            }

            HeaderView.Current.LotSerialNbr = barcode;
            Report(Msg.LotSerialReady, barcode);

            if (HeaderView.Current.LotSerAssign == INLotSerAssign.WhenUsed && HeaderView.Current.LotSerTrackExpiration == true && IsExpirationDateRequired())
                SetScanState(ScanStates.ExpireDate);
            else
                SetScanState(ScanStates.Confirm);
        }

        protected override void ProcessExpireDate(string barcode)
        {
            if (DateTime.TryParse(barcode.Trim(), out DateTime value) == false)
            {
                ReportError(Msg.LotSerialExpireDateBadFormat);
                return;
            }

            if (IsValid<Header.expireDate>(value, out string error) == false)
            {
                ReportError(error);
                return;
            }

            HeaderView.Current.ExpireDate = value;
            SetScanState(ScanStates.Confirm, Msg.LotSerialExpireDateReady, barcode);
        }

        protected override bool ProcessQtyBarcode(string barcode)
        {
            var result = base.ProcessQtyBarcode(barcode);
            if (HeaderView.Current.ScanState == ScanStates.Confirm)
                ProcessConfirm();
            return result;
        }

        protected virtual void ProcessConfirm()
        {
            try
            {
                if (!ValidateConfirmation())
                {
                    if (ExplicitLineConfirmation == false)
                        ClearHeaderInfo();
                    return;
                }

                var header = HeaderView.Current;
                var userLotSerial = header.LotSerialNbr;
                bool isSerialItem = header.LotSerTrack == INLotSerTrack.SerialNumbered;

                if (Batch == null)
                {
                    Base.batch.Insert();
                    Base.batch.Current.NoteID = header.NoteID;
                }

                AMMTran existTransaction = FindMoveRow(header);

                Action rollbackAction = null;
                decimal? newQty = header.Qty;

                if (existTransaction != null)
                {
                    var backup = Base.transactions.Cache.CreateCopy(existTransaction) as AMMTran;
                    //if not lot/serial or lot/serial is the same, update the tran qty, else insert a new split
                    if (header.LotSerTrack == INLotSerTrack.NotNumbered)
                    {
                        newQty += existTransaction.Qty;
                    Base.transactions.Cache.SetValueExt<AMMTran.lotSerialNbr>(existTransaction, userLotSerial);
                    if (header.LotSerTrackExpiration == true && header.ExpireDate != null)
                        Base.transactions.Cache.SetValueExt<AMMTran.expireDate>(existTransaction, header.ExpireDate);
                    Base.transactions.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
                    existTransaction = Base.transactions.Update(existTransaction);
                    }
                    else
                    {
                        var existingSplit = FindSplitRow(existTransaction);
                        if (existingSplit != null)
                        {
                            var newSplit = (AMMTranSplit)Base.splits.Cache.CreateCopy(existingSplit);
                            newSplit.SplitLineNbr = null;
                            newSplit.PlanID = null;
                            newSplit.Qty = newQty;
                            newSplit.LotSerialNbr = header.LotSerialNbr;
                            newSplit.ExpireDate = header.ExpireDate;
                            Base.splits.Insert(newSplit);
                        }
                    }
                    rollbackAction = () =>
                    {
                        Base.transactions.Delete(existTransaction);
                        Base.transactions.Insert(backup);
                    };
                }
                else
                {
                    existTransaction = Base.transactions.Insert();
                    Base.transactions.Cache.SetValueExt<AMMTran.orderType>(existTransaction, header.OrderType);
                    Base.transactions.Cache.SetValueExt<AMMTran.prodOrdID>(existTransaction, header.ProdOrdID);
                    Base.transactions.Cache.SetValueExt<AMMTran.operationID>(existTransaction, header.OperationID);
                    Base.transactions.Cache.SetValueExt<AMMTran.inventoryID>(existTransaction, header.InventoryID);
                    Base.transactions.Cache.SetValueExt<AMMTran.siteID>(existTransaction, header.SiteID);
                    Base.transactions.Cache.SetValueExt<AMMTran.locationID>(existTransaction, header.LocationID);
                    Base.transactions.Cache.SetValueExt<AMMTran.uOM>(existTransaction, header.UOM);
                    existTransaction = Base.transactions.Update(existTransaction);

                    Base.transactions.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
                    existTransaction = Base.transactions.Update(existTransaction);

                    Base.transactions.Cache.SetValueExt<AMMTran.lotSerialNbr>(existTransaction, userLotSerial);
                    if (header.LotSerTrackExpiration == true && header.ExpireDate != null)
                        Base.transactions.Cache.SetValueExt<AMMTran.expireDate>(existTransaction, header.ExpireDate);
                    existTransaction = Base.transactions.Update(existTransaction);

                    rollbackAction = () => Base.transactions.Delete(existTransaction);
                }
                decimal? dispQty = header.Qty;
                string dispUOM = header.UOM;

                ClearHeaderInfo();
                SetScanState(UseDefaultOrderType() ? ScanStates.ProdOrd : ScanStates.OrderType, Msg.InventoryAdded, Base.transactions.Cache.GetValueExt<AMMTran.inventoryID>(existTransaction), dispQty, dispUOM);

                if (!isSerialItem)
                    HeaderView.Current.IsQtyOverridable = true;
            }
            catch(Exception e)
            {
                PXTrace.WriteError(e);
                string errorMsg = e.Message;
                if (e is PXOuterException outerEx)
                {
                    if (outerEx.InnerMessages.Length > 0)
                        errorMsg += Environment.NewLine + string.Join(Environment.NewLine, outerEx.InnerMessages);
                    else if (outerEx.Row != null)
                        errorMsg += Environment.NewLine + string.Join(Environment.NewLine, PXUIFieldAttribute.GetErrors(Base.Caches[outerEx.Row.GetType()], outerEx.Row).Select(kvp => kvp.Value));
                }
                ReportError(errorMsg);
            }
        }

        protected virtual bool ValidateConfirmation()
        {
            if (Batch?.Released == true)
            {
                ReportError(PX.Objects.IN.Messages.Document_Status_Invalid);
                return false;
            }
            if (!HeaderView.Current.InventoryID.HasValue)
            {
                ReportError(Msg.InventoryNotSet);
                return false;
            }

            return true;
        }

        protected virtual void ProcessConfirmRemove()
        {
            if (!ValidateConfirmation())
            {
                if (ExplicitLineConfirmation == false)
                    ClearHeaderInfo();
                return;
            }

            var header = HeaderView.Current;

            AMMTran existTransaction = FindMoveRow(header);

            if (existTransaction != null)
            {
                bool isSerialItem = HeaderView.Current.LotSerTrack == INLotSerTrack.SerialNumbered;
                if (existTransaction.Qty == header.Qty)
                {
                    Base.transactions.Delete(existTransaction);
                }
                else
                {
                    var newQty = existTransaction.Qty - header.Qty;

                    if (!IsValid<AMMTran.qty, AMMTran>(existTransaction, newQty, out string error))
                    {
                        if (ExplicitLineConfirmation == false)
                            ClearHeaderInfo();
                        ReportError(error);
                        return;
                    }

                    Base.transactions.Cache.SetValueExt<AMMTran.qty>(existTransaction, newQty);
                    Base.transactions.Update(existTransaction);
                }

                SetScanState(
                    PromptLocationForEveryLine ? ScanStates.Location : ScanStates.Item,
                    Msg.InventoryRemoved,
                    Base.transactions.Cache.GetValueExt<AMMTran.inventoryID>(existTransaction), header.Qty, header.UOM);
                ClearHeaderInfo();

                if (!isSerialItem)
                    HeaderView.Current.IsQtyOverridable = true;
            }
            else
            {
                ReportError(Msg.BatchLineMissing, InventoryItem.PK.Find(Base, header.InventoryID).InventoryCD);
                ClearHeaderInfo();
                if (PromptLocationForEveryLine)
                    SetScanState(ScanStates.Location);
                else
                    ProcessItem();
            }
        }

        protected virtual void ProcessRelease()
        {
            if (Batch != null)
            {
                if (Batch.Released == true)
                {
                    ReportError(PX.Objects.IN.Messages.Document_Status_Invalid);
                    return;
                }

                if (Batch.Hold != false) Base.batch.Cache.SetValueExt<AMBatch.hold>(Batch, false);

                Save.Press();

                var clone = Base.Clone();

                WaitFor(
                (wArgs) =>
                {
                    AMDocumentRelease.ReleaseDoc(new List<AMBatch>() { wArgs.Document }, false);
                    PXLongOperation.SetCustomInfo(clone);
                    throw new PXOperationCompletedException(Msg.DocumentIsReleased);
                }, null, new DocumentWaitArguments(Batch), Msg.DocumentReleasing, Base.batch.Current.BatNbr);
            }
        }

        protected override void OnWaitEnd(PXLongRunStatus status, AMBatch primaryRow)
            => OnWaitEnd(status, primaryRow?.Released == true, Msg.DocumentIsReleased, Msg.DocumentReleaseFailed);

        protected virtual AMMTran FindMoveRow(Header header)
        {
            var existTransactions = Base.transactions.SelectMain().Where(t =>
                t.OrderType == header.OrderType &&
                t.ProdOrdID == header.ProdOrdID &&
                t.OperationID == header.OperationID &&
                t.InventoryID == header.InventoryID &&
                t.SiteID == header.SiteID &&
                t.LocationID == (header.LocationID ?? t.LocationID) &&
                t.UOM == header.UOM);

            AMMTran existTransaction = null;
                existTransaction = existTransactions.FirstOrDefault();

            return existTransaction;
        }

        protected virtual AMMTranSplit FindSplitRow(AMMTran tran)
        {
            Base.transactions.Current = tran;
            return Base.splits.SelectMain().FirstOrDefault();
        }

        protected override void ClearHeaderInfo(bool redirect = false)
        {
            base.ClearHeaderInfo(redirect);

            if (redirect)
            {
                HeaderView.Current.SiteID = null;
            }

            if (redirect || PromptLocationForEveryLine)
            {
                HeaderView.Current.LocationID = null;
            }

            HeaderView.Current.LotSerialNbr = null;
            HeaderView.Current.LotSerTrack = null;
            HeaderView.Current.ExpireDate = null;
            if (!UseDefaultOrderType())
            {
                HeaderView.Current.OrderType = null;
            }
            HeaderView.Current.ProdOrdID = null;
            HeaderView.Current.OperationID = null;
            IsSetQty = false;
        }

        protected override void ApplyState(string state)
        {
            switch (state)
            {
                case ScanStates.OrderType:
                    Prompt(Msg.OrderTypePrompt);
                    break;
                case ScanStates.ProdOrd:
                    Prompt(Msg.ProdOrdPrompt);
                    break;
                case ScanStates.Operation:
                    Prompt(Msg.OperationPrompt);
                    break;
                case ScanStates.Warehouse:
                    Prompt(Msg.WarehousePrompt);
                    break;
                case ScanStates.Location:
                    if (PXAccess.FeatureInstalled<FeaturesSet.warehouseLocation>())
                        Prompt(Msg.LocationPrompt);
                    else
                        ProcessItem();
                    break;
                case ScanStates.LotSerial:
                    Prompt(Msg.LotSerialPrompt);
                    break;
                case ScanStates.Confirm:
                    if (IsMandatoryQtyInput)
                    {
                        Prompt(Msg.QtyPrompt);
                        SetScanState(ScanStates.Qty);
                    }
                    else if (ExplicitLineConfirmation)
                        Prompt(Msg.ConfirmationPrompt);
                    else if (HeaderView.Current.Remove == false)
                        ProcessConfirm();
                    else
                        ProcessConfirmRemove();
                    break;
            }
        }

        protected override string GetDefaultState(Header header = null) => UseDefaultOrderType() ? ScanStates.ProdOrd : ScanStates.OrderType;

        protected override void ClearMode()
        {
            ClearHeaderInfo();
            SetScanState(UseDefaultOrderType() ? ScanStates.ProdOrd : ScanStates.OrderType, Msg.ScreenCleared);
        }

        protected override void ProcessDocumentNumber(string barcode) => throw new NotImplementedException();

        protected override void ProcessCartBarcode(string barcode) => throw new NotImplementedException();

        protected override bool PerformQtyCorrection(decimal qtyDelta)
        {
            if (UseRemainingQty)
            {
                var currentQty = HeaderView.Current.QtyRemaining;
                if (currentQty == null) return false;
                return base.PerformQtyCorrection(qtyDelta + 1 - currentQty.Value);
            }
            //if using explcit confirm and changing the qty, add the default 1 back in
            if(HeaderView.Current.ScanState == ScanStates.Confirm)
                return base.PerformQtyCorrection(qtyDelta + 1);
            return base.PerformQtyCorrection(qtyDelta);
        }

        [PXButton, PXUIField(DisplayName = "Set Qty")]
        protected override IEnumerable scanQty(PXAdapter adapter)
        {
            IsSetQty = true;
            return base.scanQty(adapter);
        }

        private DateTime? EnsureExpireDateDefault() => LSSelect.ExpireDateByLot(Base, HeaderView.Current, null);

        protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInMove != true;
        protected override bool ExplicitLineConfirmation => Setup.Current.ExplicitLineConfirmation == true;
        protected override bool DocumentLoaded => Batch != null;
        protected bool PromptLocationForEveryLine => Setup.Current.RequestLocationForEachItemInMove == true;
        protected bool UseRemainingQty => Setup.Current.UseRemainingQtyInMove == true;
        protected static bool IsSetQty = false;

        #region Constants & Messages
        public new abstract class Modes : WMSBase.Modes
        {
            public static WMSModeOf<ScanMove, ScanMoveHost> ScanMove { get; } = WMSMode("INRE");

            public class scanMove : PX.Data.BQL.BqlString.Constant<scanMove> { public scanMove() : base(ScanMove) { } }
        }

        public new abstract class ScanStates : WMSBase.ScanStates
        {
            public const string Warehouse = "SITE";
            public const string Confirm = "CONF";
            public const string OrderType = "OTYP";
            public const string ProdOrd = "PROD";
            public const string Operation = "OPER";

        }

        public new abstract class ScanCommands : WMSBase.ScanCommands
        {
            public const string Release = Marker + "RELEASE*RECEIPT";
            public const string OrderType = Marker + "TYPE";
        }

        [PXLocalizable]
        public new abstract class Msg : WMSBase.Msg
        {
            public const string ScanMoveMode = "Scan Move";

            public const string ConfirmationPrompt = "Confirm the line, or scan or enter the line quantity.";

            public const string BatchLineMissing = "Line {0} is not found in the batch.";

            public const string DocumentReleasing = "The {0} move is being released.";
            public const string DocumentIsReleased = "The move is successfully released.";
            public const string DocumentReleaseFailed = "The move release failed.";
            public const string OrderTypePrompt = "Scan the Order Type.";
            public const string ProdOrdPrompt = "Scan the Production Order ID";
            public const string OperationPrompt = "Scan the Operation ID.";
            public const string OrderTypeMissing = "The {0} order type is not found.";
            public const string ProdOrdMissing = "The {0} production order is not found.";
            public const string ProdOrdWrongStatus = "The production order {0}, {1} has a status of {2}";
            public const string ProdOrdWrongType = "The production order {0} is a Disassembly type.";
            public const string OperationMissing = "The {0} operation is not found.";
        }
        #endregion
    }
}