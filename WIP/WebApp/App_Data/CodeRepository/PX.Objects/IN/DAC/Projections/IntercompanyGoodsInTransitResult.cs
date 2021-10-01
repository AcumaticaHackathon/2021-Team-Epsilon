using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PO;
using PX.Objects.SO;

namespace PX.Objects.IN
{
	/// <summary>
	/// The DAC is used as a result in Intercompany Goods in Transit Generic Inquiry
	/// </summary>
	[PXCacheName(Messages.IntercompanyGoodsInTransitResult)]
	[PXProjection(typeof(Select2<SOShipLine,
		InnerJoin<InventoryItem, On<SOShipLine.FK.InventoryItem>,
		InnerJoin<SOShipment, On<SOShipLine.FK.Shipment>,
		InnerJoin<SOOrder, On<SOShipLine.FK.Order>,
		InnerJoin<SOLine, On<SOShipLine.FK.OrderLine>,
		InnerJoin<Branch, On<SOOrder.FK.Branch>,
		LeftJoin<POReceipt, On<POReceipt.FK.IntercompanyShipment>,
		LeftJoin<POReceiptLine, On<Where2<POReceiptLine.FK.Receipt,
			And<POReceiptLine.intercompanyShipmentLineNbr, Equal<SOShipLine.lineNbr>>>>>>>>>>>,
		Where<SOShipment.isIntercompany, Equal<True>>>), Persistent = false)]
	public class IntercompanyGoodsInTransitResult : IBqlTable
	{
		#region ShipmentNbr
		public abstract class shipmentNbr : Data.BQL.BqlString.Field<shipmentNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(SOShipLine.shipmentNbr))]
		[PXSelector(typeof(Search<SOShipment.shipmentNbr>))]
		[PXUIField(DisplayName = "Shipment Nbr.", Enabled = false)]
		public virtual string ShipmentNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true, BqlField = typeof(SOShipLine.lineNbr))]
		[PXUIField(DisplayName = "Line Nbr.", Enabled = false)]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion

		#region Operation
		public abstract class operation : Data.BQL.BqlString.Field<operation> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOShipment.operation))]
		[PXUIField(DisplayName = "Operation")]
		[SOOperation.List]
		public virtual string Operation
		{
			get;
			set;
		}
		#endregion
		#region ShipmentType
		public abstract class shipmentType : Data.BQL.BqlString.Field<shipmentType> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(SOShipment.shipmentType))]
		[PXUIField(DisplayName = "Type")]
		[SOShipmentType.ShortList]
		public virtual string ShipmentType
		{
			get;
			set;
		}
		#endregion
		#region ShipDate
		public abstract class shipDate : Data.BQL.BqlDateTime.Field<shipDate> { }
		[PXDBDate(BqlField = typeof(SOShipment.shipDate))]
		[PXUIField(DisplayName = SOShipment.shipDate.DisplayName)]
		public virtual DateTime? ShipDate
		{
			get;
			set;
		}
		#endregion
		#region SellingBranchID
		public abstract class sellingBranchID : Data.BQL.BqlInt.Field<sellingBranchID> { }
		[Branch(typeof(AccessInfo.branchID), DisplayName = "Selling Company", Required = false, BqlField = typeof(SOOrder.branchID))]
		public virtual int? SellingBranchID
		{
			get;
			set;
		}
		#endregion
		#region SellingBranchBAccountID
		public abstract class sellingBranchBAccountID : Data.BQL.BqlInt.Field<sellingBranchBAccountID> { }
		[PXDBInt(BqlField = typeof(Branch.bAccountID))]
		public virtual int? SellingBranchBAccountID
		{
			get;
			set;
		}
		#endregion
		#region SellingSiteID
		public abstract class sellingSiteID : Data.BQL.BqlInt.Field<sellingSiteID> { }
		[Site(DisplayName = "Selling Warehouse", DescriptionField = typeof(INSite.descr), BqlField = typeof(SOShipment.siteID))]
		public virtual int? SellingSiteID
		{
			get;
			set;
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : Data.BQL.BqlInt.Field<inventoryID> { }
		[Inventory(BqlField = typeof(SOShipLine.inventoryID))]
		public virtual int? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : Data.BQL.BqlString.Field<tranDesc> { }
		[PXDBString(256, IsUnicode = true, BqlField = typeof(SOShipLine.tranDesc))]
		[PXUIField(DisplayName = "Description")]
		public virtual string TranDesc
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : Data.BQL.BqlString.Field<uOM> { }
		[INUnit(typeof(inventoryID), DisplayName = "UOM", BqlField = typeof(SOShipLine.uOM))]
		public virtual string UOM
		{
			get;
			set;
		}
		#endregion
		#region ShippedQty
		public abstract class shippedQty : Data.BQL.BqlDecimal.Field<shippedQty> { }
		[PXDBDecimal(BqlField = typeof(SOShipLine.shippedQty))]
		[PXUIField(DisplayName = "In-Transit Qty.")]
		public virtual decimal? ShippedQty
		{
			get;
			set;
		}
		#endregion
		#region ExtCost
		public abstract class extCost : Data.BQL.BqlDecimal.Field<extCost> { }
		[PXBaseCury]
		[PXDBCalced(typeof(Switch<Case<Where<SOLine.baseOrderQty, Equal<decimal0>>, SOLine.lineAmt>,
			Div<Mult<SOLine.lineAmt, SOShipLine.baseShippedQty>, SOLine.baseOrderQty>>), typeof(decimal))]
		[PXUIField(DisplayName = "Total Cost")]
		public virtual decimal? ExtCost
		{
			get;
			set;
		}
		#endregion

		#region DaysInTransit
		public abstract class daysInTransit : Data.BQL.BqlInt.Field<daysInTransit> { }
		[PXInt]
		[PXDBCalced(typeof(DateDiff<SOShipment.shipDate, businessDate, DateDiff.day>), typeof(int))]
		[PXUIField(DisplayName = "Days in Transit")]
		public virtual int? DaysInTransit
		{
			get;
			set;
		}
		#endregion
		#region DaysOverdue
		public abstract class daysOverdue : Data.BQL.BqlInt.Field<daysOverdue> { }
		[PXInt]
		[PXDBCalced(typeof(Switch<Case<Where<SOLine.requestDate, GreaterEqual<businessDate>>, Null>,
			DateDiff<SOLine.requestDate, businessDate, DateDiff.day>>), typeof(int))]
		[PXUIField(DisplayName = "Days Overdue")]
		public virtual int? DaysOverdue
		{
			get;
			set;
		}
		#endregion

		#region PurchasingBranchID
		public abstract class purchasingBranchID : Data.BQL.BqlInt.Field<purchasingBranchID> { }
		[Customer(DisplayName = "Purchasing Company", BqlField = typeof(SOOrder.customerID))]
		public virtual int? PurchasingBranchID
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
		#region POReceiptNbr
		public abstract class pOReceiptNbr : Data.BQL.BqlString.Field<pOReceiptNbr> { }
		[PXDBString(15, IsKey = true, IsUnicode = true, BqlField = typeof(POReceipt.receiptNbr))]
		[POReceiptType.RefNbr(typeof(Search<POReceipt.receiptNbr>), Filterable = true)]
		[PXUIField(DisplayName = POReceipt.receiptNbr.DisplayName)]
		public virtual string POReceiptNbr
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
		#region ShipmentConfirmed
		public abstract class shipmentConfirmed : Data.BQL.BqlBool.Field<shipmentConfirmed> { }
		[PXDBBool(BqlField = typeof(SOShipment.confirmed))]
		[PXUIField(DisplayName = "Confirmed")]
		public virtual bool? ShipmentConfirmed
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
		#region ReceiptReleased
		public abstract class receiptReleased : Data.BQL.BqlBool.Field<receiptReleased> { }
		[PXDBBool(BqlField = typeof(POReceipt.released))]
		[PXUIField(DisplayName = "Released", Visible = false)]
		public virtual bool? ReceiptReleased
		{
			get;
			set;
		}
		#endregion
		#region RequestDate
		public abstract class requestDate : Data.BQL.BqlDateTime.Field<requestDate> { }
		[PXDBDate(BqlField = typeof(SOLine.requestDate))]
		[PXUIField(DisplayName = "Requested On", Visible = false)]
		public virtual DateTime? RequestDate
		{
			get;
			set;
		}
		#endregion
		#region ReceiptDate
		public abstract class receiptDate : Data.BQL.BqlDateTime.Field<receiptDate> { }
		[PXDBDate(BqlField = typeof(POReceipt.receiptDate))]
		[PXUIField(DisplayName = Messages.ReceiptDate, Visible = false)]
		public virtual DateTime? ReceiptDate
		{
			get;
			set;
		}
		#endregion
		#region ExcludeFromIntercompanyProc
		public abstract class excludeFromIntercompanyProc : Data.BQL.BqlBool.Field<excludeFromIntercompanyProc> { }
		[PXDBBool(BqlField = typeof(SOShipment.excludeFromIntercompanyProc))]
		[PXUIField(DisplayName = "Exclude from Intercompany Processing")]
		public virtual bool? ExcludeFromIntercompanyProc
		{
			get;
			set;
		}
		#endregion

		#region NoteID
		public abstract class noteID : Data.BQL.BqlGuid.Field<noteID> { }
		[PXNote(BqlField = typeof(SOShipLine.noteID))]
		public virtual Guid? NoteID
		{
			get;
			set;
		}
		#endregion

		public class businessDate : Data.BQL.BqlDateTime.Constant<businessDate>
		{
			public businessDate() : base(PXContext.GetBusinessDate() ?? PXTimeZoneInfo.Today) { }
		}
	}
}
