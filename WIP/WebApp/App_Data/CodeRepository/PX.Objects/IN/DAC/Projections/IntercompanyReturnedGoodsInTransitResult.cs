using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PO;
using PX.Objects.SO;

namespace PX.Objects.IN
{
	/// <summary>
	/// The DAC is used as a result in Intercompany Returned Goods in Transit Generic Inquiry
	/// </summary>
	[PXCacheName(Messages.IntercompanyReturnedGoodsInTransitResult)]
	[PXProjection(typeof(Select2<POReceiptLine,
		InnerJoin<InventoryItem, On<POReceiptLine.FK.InventoryItem>,
		InnerJoin<POReceipt, On<POReceiptLine.FK.Receipt>,
		InnerJoin<Branch, On<POReceipt.FK.Branch>,
		LeftJoin<SOOrder, On<SOOrder.FK.IntercompanyPOReturn>,
		LeftJoin<SOLine, On<Where2<SOLine.FK.Order,
			And<SOLine.intercompanyPOLineNbr, Equal<POReceiptLine.lineNbr>>>>,
		LeftJoin<SOShipLine, On<Where2<SOShipLine.FK.OrderLine,
			And<SOShipLine.confirmed, Equal<False>>>>,
		LeftJoin<SOShipment, On<SOShipLine.FK.Shipment>>>>>>>>,
		Where<POReceipt.receiptType, Equal<POReceiptType.poreturn>,
			And<POReceipt.isIntercompany, Equal<True>>>>), Persistent = false)]
	public class IntercompanyReturnedGoodsInTransitResult : IBqlTable
	{
		#region POReturnNbr
		public abstract class pOReturnNbr : Data.BQL.BqlString.Field<pOReturnNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(POReceiptLine.receiptNbr))]
		[POReceiptType.RefNbr(typeof(Search<POReceipt.receiptNbr>), Filterable = true)]
		[PXUIField(DisplayName = "Return Nbr.")]
		public virtual string POReturnNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true, BqlField = typeof(POReceiptLine.lineNbr))]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion

		#region PurchasingBranchID
		public abstract class purchasingBranchID : Data.BQL.BqlInt.Field<purchasingBranchID> { }
		[Branch(typeof(AccessInfo.branchID), DisplayName = "Purchasing Company", Required = false, BqlField = typeof(POReceipt.branchID))]
		public virtual int? PurchasingBranchID
		{
			get;
			set;
		}
		#endregion
		#region PurchasingBranchBAccountID
		public abstract class purchasingBranchBAccountID : Data.BQL.BqlInt.Field<purchasingBranchBAccountID> { }
		[PXDBInt(BqlField = typeof(Branch.bAccountID))]
		public virtual int? PurchasingBranchBAccountID
		{
			get;
			set;
		}
		#endregion
		#region PurchasingSiteID
		public abstract class purchasingSiteID : Data.BQL.BqlInt.Field<purchasingSiteID> { }
		[Site(DisplayName = "Purchasing Warehouse", DescriptionField = typeof(INSite.descr), BqlField = typeof(POReceiptLine.siteID))]
		public virtual int? PurchasingSiteID
		{
			get;
			set;
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : Data.BQL.BqlInt.Field<inventoryID> { }
		[Inventory(BqlField = typeof(POReceiptLine.inventoryID))]
		public virtual int? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : Data.BQL.BqlString.Field<tranDesc> { }
		[PXDBString(256, IsUnicode = true, BqlField = typeof(POReceiptLine.tranDesc))]
		[PXUIField(DisplayName = "Description")]
		public virtual string TranDesc
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : Data.BQL.BqlString.Field<uOM> { }
		[INUnit(typeof(inventoryID), DisplayName = "UOM", BqlField = typeof(POReceiptLine.uOM))]
		public virtual string UOM
		{
			get;
			set;
		}
		#endregion
		#region ReturnedQty
		public abstract class returnedQty : Data.BQL.BqlDecimal.Field<returnedQty> { }
		[PXDBDecimal(BqlField = typeof(POReceiptLine.receiptQty))]
		[PXUIField(DisplayName = "In-Transit Qty.")]
		public virtual decimal? ReturnedQty
		{
			get;
			set;
		}
		#endregion
		#region ExtCost
		public abstract class extCost : Data.BQL.BqlDecimal.Field<extCost> { }
		[PXDBBaseCury(BqlField = typeof(POReceiptLine.tranCost))]
		[PXUIField(DisplayName = "Total Cost")]
		public virtual decimal? ExtCost
		{
			get;
			set;
		}
		#endregion

		#region ReturnDate
		public abstract class returnDate : Data.BQL.BqlDateTime.Field<returnDate> { }
		[PXDBDate(BqlField = typeof(POReceipt.receiptDate))]
		[PXUIField(DisplayName = "Return Date")]
		public virtual DateTime? ReturnDate
		{
			get;
			set;
		}
		#endregion
		#region DaysInTransit
		public abstract class daysInTransit : Data.BQL.BqlInt.Field<daysInTransit> { }
		[PXInt]
		[PXDBCalced(typeof(DateDiff<POReceipt.receiptDate, IntercompanyGoodsInTransitResult.businessDate, DateDiff.day>), typeof(int))]
		[PXUIField(DisplayName = "Days in Transit")]
		public virtual int? DaysInTransit
		{
			get;
			set;
		}
		#endregion

		#region SellingBranchID
		public abstract class sellingBranchID : Data.BQL.BqlInt.Field<sellingBranchID> { }
		[Vendor(DisplayName = "Selling Company", BqlField = typeof(POReceipt.vendorID))]
		public virtual int? SellingBranchID
		{
			get;
			set;
		}
		#endregion
		#region SellingSiteID
		public abstract class sellingSiteID : Data.BQL.BqlInt.Field<sellingSiteID> { }
		[Site(DisplayName = "Selling Warehouse", DescriptionField = typeof(INSite.descr), BqlField = typeof(SOLine.siteID))]
		public virtual int? SellingSiteID
		{
			get;
			set;
		}
		#endregion
		#region SOType
		public abstract class sOType : Data.BQL.BqlString.Field<sOType> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLine.orderType))]
		[PXUIField(DisplayName = "SO Type")]
		public virtual string SOType
		{
			get;
			set;
		}
		#endregion
		#region SONbr
		public abstract class sONbr : Data.BQL.BqlString.Field<sONbr> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(SOLine.orderNbr))]
		[PXUIField(DisplayName = "SO Nbr.")]
		[SO.SO.RefNbr(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType, Equal<Current<sOType>>>>))]
		public virtual string SONbr
		{
			get;
			set;
		}
		#endregion
		#region SOLineNbr
		public abstract class sOLineNbr : Data.BQL.BqlInt.Field<sOLineNbr> { }
		[PXDBInt(BqlField = typeof(SOLine.lineNbr))]
		public virtual int? SOLineNbr
		{
			get;
			set;
		}
		#endregion
		#region SOBehavior
		public abstract class sOBehavior : Data.BQL.BqlString.Field<sOBehavior> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLine.behavior))]
		[SOBehavior.List]
		public virtual string SOBehavior
		{
			get;
			set;
		}
		#endregion

		#region StkItem
		public abstract class stkItem : Data.BQL.BqlBool.Field<stkItem> { }
		[PXDBBool(BqlField = typeof(InventoryItem.stkItem))]
		[PXUIField(DisplayName = "Stock Item")]
		public virtual bool? StkItem { get; set; }
		#endregion
		#region ReturnReleased
		public abstract class returnReleased : Data.BQL.BqlBool.Field<returnReleased> { }
		[PXDBBool(BqlField = typeof(POReceipt.released))]
		[PXUIField(DisplayName = "Released")]
		public virtual bool? ReturnReleased
		{
			get;
			set;
		}
		#endregion
		#region ShipmentNbr
		public abstract class shipmentNbr : Data.BQL.BqlString.Field<shipmentNbr> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(SOShipLine.shipmentNbr))]
		[PXSelector(typeof(Search<SOShipment.shipmentNbr>))]
		[PXUIField(DisplayName = "Shipment Nbr.", Enabled = false)]
		public virtual string ShipmentNbr
		{
			get;
			set;
		}
		#endregion
		#region ShipmentStatus
		public abstract class shipmentStatus : Data.BQL.BqlString.Field<shipmentStatus> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOShipment.status))]
		[PXUIField(DisplayName = "Shipment Status", Enabled = false, Visible = false)]
		[SOShipmentStatus.List]
		public virtual string ShipmentStatus
		{
			get;
			set;
		}
		#endregion
		#region ShipmentDate
		public abstract class shipmentDate : Data.BQL.BqlDateTime.Field<shipmentDate> { }
		[PXDBDate(BqlField = typeof(SOShipment.shipDate))]
		[PXUIField(DisplayName = SOShipment.shipDate.DisplayName, Visible = false)]
		public virtual DateTime? ShipmentDate
		{
			get;
			set;
		}
		#endregion
		#region ExcludeFromIntercompanyProc
		public abstract class excludeFromIntercompanyProc : Data.BQL.BqlBool.Field<excludeFromIntercompanyProc> { }
		[PXDBBool(BqlField = typeof(POReceipt.excludeFromIntercompanyProc))]
		[PXUIField(DisplayName = "Exclude from Intercompany Processing")]
		public virtual bool? ExcludeFromIntercompanyProc
		{
			get;
			set;
		}
		#endregion

		#region OrigReceiptNbr
		public abstract class origReceiptNbr : Data.BQL.BqlString.Field<origReceiptNbr> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(POReceiptLine.origReceiptNbr))]
		public virtual string OrigReceiptNbr
		{
			get;
			set;
		}
		#endregion

		#region NoteID
		public abstract class noteID : Data.BQL.BqlGuid.Field<noteID> { }
		[PXNote(BqlField = typeof(POReceiptLine.noteID))]
		public virtual Guid? NoteID
		{
			get;
			set;
		}
		#endregion
	}
}
