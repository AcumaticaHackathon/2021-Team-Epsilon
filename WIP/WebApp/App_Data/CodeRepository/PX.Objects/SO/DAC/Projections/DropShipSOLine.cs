using PX.Data;
using PX.Objects.Common.DAC;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.DropShipSOLine)]
	[PXProjection(typeof(Select2<SOLine,
	LeftJoin<DropShipLink, On<DropShipLink.FK.SOLine>>,
	Where<SOLine.pOCreate, Equal<True>,
		And<SOLine.pOSource, Equal<INReplenishmentSource.dropShipToOrder>>>>))]
	public class DropShipSOLine : IBqlTable
	{
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(SOLine.orderType))]
		[PXUIField(DisplayName = "Order Type")]
		public virtual String OrderType
		{
			get;
			set;
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }

		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(SOLine.orderNbr))]
		[PXSelector(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType, Equal<Current<DropShipSOLine.orderType>>>>))]
		[PXUIField(DisplayName = "Order Nbr.")]
		public virtual String OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		[PXDBInt(IsKey = true, BqlField = typeof(SOLine.lineNbr))]
		[PXUIField(DisplayName = "Line Nbr.")]
		public virtual Int32? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

		[SOLineInventoryItem(Filterable = true, BqlField = typeof(SOLine.inventoryID))]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }

		[PXDBString(256, IsUnicode = true, BqlField = typeof(SOLine.tranDesc))]
		[PXUIField(DisplayName = "Description")]
		public virtual String TranDesc
		{
			get;
			set;
		}
		#endregion
		#region OrderQty
		public abstract class orderQty : PX.Data.BQL.BqlDecimal.Field<orderQty> { }

		[PXDBQuantity(BqlField = typeof(SOLine.orderQty))]
		[PXUIField(DisplayName = "Not Linked Qty.")]
		public virtual Decimal? OrderQty
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		[INUnit(typeof(DropShipSOLine.inventoryID), DisplayName = "UOM", BqlField = typeof(SOLine.uOM))]
		public virtual String UOM
		{
			get;
			set;
		}
		#endregion
		#region IsLegacyDropShip
		public abstract class isLegacyDropShip : PX.Data.BQL.BqlBool.Field<isLegacyDropShip> { }

		[PXDBBool(BqlField = typeof(SOLine.isLegacyDropShip))]
		public virtual bool? IsLegacyDropShip
		{
			get;
			set;
		}
		#endregion
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }

		[PXNote(BqlField = typeof(SOLine.noteID))]
		public virtual Guid? NoteID
		{
			get;
			set;
		}
		#endregion
		#region POOrderType
		public abstract class pOOrderType : PX.Data.BQL.BqlString.Field<pOOrderType> { }

		[PXDBString(2, IsFixed = true, BqlField = typeof(DropShipLink.pOOrderType))]
		public virtual string POOrderType
		{
			get;
			set;
		}
		#endregion
		#region POOrderNbr
		public abstract class pOOrderNbr : PX.Data.BQL.BqlString.Field<pOOrderNbr> { }

		[PXUIField(DisplayName = "Drop-Ship PO Nbr.")]
		[PXSelector(typeof(Search<PO.POOrder.orderNbr, Where<PO.POOrder.orderType, Equal<Current<DropShipSOLine.pOOrderType>>>>))]
		[PXDBString(15, IsUnicode = true, BqlField = typeof(DropShipLink.pOOrderNbr))]
		public virtual string POOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region POLineNbr
		public abstract class pOLineNbr : PX.Data.BQL.BqlInt.Field<pOLineNbr> { }

		[PXUIField(DisplayName = "Drop-Ship PO Line Nbr.")]
		[PXDBInt(BqlField = typeof(DropShipLink.pOLineNbr))]
		public virtual int? POLineNbr
		{
			get;
			set;
		}
		#endregion
		#region POLinkActive
		public abstract class pOLinkActive : PX.Data.BQL.BqlBool.Field<pOLinkActive> { }

		[PXDBBool(BqlField = typeof(DropShipLink.active))]
		[PXUIField(DisplayName = "PO Linked")]
		public virtual bool? POLinkActive
		{
			get;
			set;
		}
		#endregion
	}
}
