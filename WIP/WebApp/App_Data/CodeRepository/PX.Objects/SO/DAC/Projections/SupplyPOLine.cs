using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.SupplyPOLine)]
	[PXProjection(typeof(Select2<POLine,
		InnerJoin<POOrder, On<POLine.FK.Order>>,
		Where<POOrder.isLegacyDropShip, Equal<boolFalse>>>),
		persistent: new [] { typeof(POLine) })]
	public class SupplyPOLine : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<SupplyPOLine>.By<orderType, orderNbr, lineNbr>
		{
			public static SupplyPOLine Find(PXGraph graph, string orderType, string orderNbr, int? lineNbr) => FindBy(graph, orderType, orderNbr, lineNbr);
		}
		public static class FK
		{
			public class SupplyOrder : SupplyPOOrder.PK.ForeignKeyOf<SupplyPOLine>.By<orderType, orderNbr> { }
		}
		#endregion

		#region Unbound

		#region SelectedSOLines
		public abstract class selectedSOLines : PX.Data.BQL.BqlByteArray.Field<selectedSOLines> { }
		// Store selected checkbox state for each SOLine in a sparse array. Selected == true <=> SOLine number presents in collection. 
		public virtual int?[] SelectedSOLines
		{
			get;
			set;
		}
		#endregion
		#region LinkedSOLines
		public abstract class linkedSOLines : PX.Data.BQL.BqlByteArray.Field<linkedSOLines> { }
		// Store line numbers of linked SOLines of current order in a sparse array.
		public virtual int?[] LinkedSOLines
		{
			get;
			set;
		}
		#endregion
		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }

		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected")]
		[PXBool]
		public virtual bool? Selected
		{
			get;
			set;
		}
		#endregion
		#region BaseDemandQty
		public abstract class baseDemandQty : PX.Data.BQL.BqlDecimal.Field<baseDemandQty> { }

		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXQuantity]
		public virtual Decimal? BaseDemandQty
		{
			get;
			set;
		}
		#endregion

		#endregion

		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

		[PXDefault]
		[POOrderType.List]
		[PXUIField(DisplayName = "PO Type", Enabled = false)]
		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(POLine.orderType))]
		public virtual String OrderType
		{
			get;
			set;
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }

		[PXDefault]
		[PXUIField(DisplayName = "PO Nbr.", Enabled = false)]
		[PXSelector(typeof(Search<POOrder.orderNbr, Where<POOrder.orderType, Equal<Current<SupplyPOLine.orderType>>>>),
			DescriptionField = typeof(POOrder.orderDesc))]
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(POLine.orderNbr))]
		public virtual String OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		[PXDefault]
		[PXUIField(DisplayName = "PO Line Nbr.", Enabled = false)]
		[PXDBInt(IsKey = true, BqlField = typeof(POLine.lineNbr))]
		public virtual Int32? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region LineType
		public abstract class lineType : PX.Data.BQL.BqlString.Field<lineType> { }

		[POLineType.List]
		[PXUIField(DisplayName = "Line Type", Enabled = false)]
		[PXDBString(2, IsFixed = true, BqlField = typeof(POLine.lineType))]
		public virtual String LineType
		{
			get;
			set;
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

		[Inventory(Filterable = true, BqlField = typeof(POLine.inventoryID), Enabled = false)]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }

		[SubItem(BqlField = typeof(POLine.subItemID), Enabled = false)]
		public virtual Int32? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region PlanID
		public abstract class planID : PX.Data.BQL.BqlLong.Field<planID> { }

		[PXUIField(Visible = false, Enabled = false)]
		[PXDBLong(BqlField = typeof(POLine.planID))]
		public virtual Int64? PlanID
		{
			get;
			set;
		}
		#endregion
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }

		[AP.Vendor(typeof(Search<CR.BAccountR.bAccountID, Where<AP.Vendor.type, NotEqual<CR.BAccountType.employeeType>>>),
			BqlField = typeof(POLine.vendorID), Enabled = false)]
		public virtual Int32? VendorID
		{
			get;
			set;
		}
		#endregion
		#region VendorRefNbr
		public abstract class vendorRefNbr : PX.Data.BQL.BqlString.Field<vendorRefNbr> { }

		[PXUIField(DisplayName = "Vendor Ref.", Enabled = false)]
		[PXDBString(40, IsUnicode = true, BqlField = typeof(POOrder.vendorRefNbr))]
		public virtual String VendorRefNbr
		{
			get;
			set;
		}
		#endregion
		#region Hold
		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }

		[PXDBBool(BqlField = typeof(POOrder.hold))]
		public virtual Boolean? Hold
		{
			get;
			set;
		}
		#endregion
		#region OrderDate
		public abstract class orderDate : PX.Data.BQL.BqlDateTime.Field<orderDate> { }

		[PXUIField(DisplayName = "Order Date", Enabled = false)]
		[PXDBDate(BqlField = typeof(POLine.orderDate))]
		public virtual DateTime? OrderDate
		{
			get;
			set;
		}
		#endregion
		#region PromisedDate
		public abstract class promisedDate : PX.Data.BQL.BqlDateTime.Field<promisedDate> { }

		[PXUIField(DisplayName = "Promised", Enabled = false)]
		[PXDBDate(BqlField = typeof(POLine.promisedDate))]
		public virtual DateTime? PromisedDate
		{
			get;
			set;
		}
		#endregion
		#region Cancelled
		public abstract class cancelled : PX.Data.BQL.BqlBool.Field<cancelled> { }

		[PXDBBool(BqlField = typeof(POLine.cancelled))]
		public virtual Boolean? Cancelled
		{
			get;
			set;
		}
		#endregion
		#region Completed
		public abstract class completed : PX.Data.BQL.BqlBool.Field<completed> { }

		[PXDBBool(BqlField = typeof(POLine.completed))]
		public virtual bool? Completed
		{
			get;
			set;
		}
		#endregion
		#region Closed
		public abstract class closed : PX.Data.BQL.BqlBool.Field<closed> { }

		[PXDBBool(BqlField = typeof(POLine.closed))]
		public virtual bool? Closed
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }

		[PXDBInt(BqlField = typeof(POLine.siteID))]
		public virtual Int32? SiteID
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		[INUnit(typeof(SupplyPOLine.inventoryID), DisplayName = "UOM", Enabled = false, BqlField = typeof(POLine.uOM))]
		public virtual String UOM
		{
			get;
			set;
		}
		#endregion
		#region OrderQty
		public abstract class orderQty : PX.Data.BQL.BqlDecimal.Field<orderQty> { }

		[PXUIField(DisplayName = "Order Qty.", Enabled = false)]
		[PXDBQuantity(typeof(SupplyPOLine.uOM), typeof(SupplyPOLine.baseOrderQty), BqlField = typeof(POLine.orderQty))]
		public virtual Decimal? OrderQty
		{
			get;
			set;
		}
		#endregion
		#region BaseOrderQty
		public abstract class baseOrderQty : PX.Data.BQL.BqlDecimal.Field<baseOrderQty> { }

		[PXDBQuantity(BqlField = typeof(POLine.baseOrderQty))]
		public virtual Decimal? BaseOrderQty
		{
			get;
			set;
		}
		#endregion
		#region OpenQty
		public abstract class openQty : PX.Data.BQL.BqlDecimal.Field<openQty> { }

		[PXUIField(DisplayName = "Open Qty.", Enabled = false)]
		[PXDBQuantity(typeof(SupplyPOLine.uOM), typeof(SupplyPOLine.baseOpenQty), BqlField = typeof(POLine.openQty))]
		public virtual Decimal? OpenQty
		{
			get;
			set;
		}
		#endregion
		#region BaseOpenQty
		public abstract class baseOpenQty : PX.Data.BQL.BqlDecimal.Field<baseOpenQty> { }

		[PXDBQuantity(BqlField = typeof(POLine.baseOpenQty))]
		public virtual Decimal? BaseOpenQty
		{
			get;
			set;
		}
		#endregion
		#region ReceivedQty
		public abstract class receivedQty : PX.Data.BQL.BqlDecimal.Field<receivedQty> { }

		[PXDBQuantity(typeof(SupplyPOLine.uOM), typeof(SupplyPOLine.baseReceivedQty), BqlField = typeof(POLine.receivedQty))]
		public virtual Decimal? ReceivedQty
		{
			get;
			set;
		}
		#endregion
		#region BaseReceivedQty
		public abstract class baseReceivedQty : PX.Data.BQL.BqlDecimal.Field<baseReceivedQty> { }

		[PXDBQuantity(BqlField = typeof(POLine.baseReceivedQty))]
		public virtual Decimal? BaseReceivedQty
		{
			get;
			set;
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }

		[PXUIField(DisplayName = "Line Description", Enabled = false)]
		[PXDBString(256, IsUnicode = true, BqlField = typeof(POLine.tranDesc))]
		public virtual String TranDesc
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp(RecordComesFirst = true, BqlField = typeof(POLine.Tstamp))]
		public virtual Byte[] tstamp
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID(BqlField = typeof(POLine.lastModifiedByID))]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID(BqlField = typeof(POLine.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime(BqlField = typeof(POLine.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
	}
}
