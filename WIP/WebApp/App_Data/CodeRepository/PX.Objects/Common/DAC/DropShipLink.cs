using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.Common.DAC
{
	/// <summary>
	/// Designed for reconciliation of drop-ship details between POLine and SOLine.
	/// </summary>
	[PXCacheName("Drop-Ship Link")]
	public class DropShipLink : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<DropShipLink>.By<sOOrderType, sOOrderNbr, sOLineNbr, pOOrderType, pOOrderNbr, pOLineNbr>
		{
			public static DropShipLink Find(PXGraph graph, string sOOrderType, string sOOrderNbr, int? sOLineNbr, string pOOrderType, string pOOrderNbr, int? pOLineNbr)
				=> FindBy(graph, sOOrderType, sOOrderNbr, sOLineNbr, pOOrderType, pOOrderNbr, pOLineNbr);
		}
		public class UK
		{
			public class ByPOLine : PrimaryKeyOf<DropShipLink>.By<pOOrderType, pOOrderNbr, pOLineNbr>
			{
				public static DropShipLink Find(PXGraph graph, string pOOrderType, string pOOrderNbr, int? pOLineNbr) => FindBy(graph, pOOrderType, pOOrderNbr, pOLineNbr);
				public static DropShipLink FindDirty(PXGraph graph, string pOOrderType, string pOOrderNbr, int? pOLineNbr) => PXSelect<DropShipLink,
					Where<pOOrderType, Equal<Required<pOOrderType>>,
						And<pOOrderNbr, Equal<Required<pOOrderNbr>>,
						And<pOLineNbr, Equal<Required<pOLineNbr>>>>>>.SelectWindowed(graph, 0, 1, pOOrderType, pOOrderNbr, pOLineNbr);
			}
			public class BySOLine : PrimaryKeyOf<DropShipLink>.By<sOOrderType, sOOrderNbr, sOLineNbr>
			{
				public static DropShipLink Find(PXGraph graph, string sOOrderType, string sOOrderNbr, int? sOLineNbr) => FindBy(graph, sOOrderType, sOOrderNbr, sOLineNbr);
				public static DropShipLink FindDirty(PXGraph graph, string sOOrderType, string sOOrderNbr, int? sOLineNbr) => PXSelect<DropShipLink,
					Where<sOOrderType, Equal<Required<sOOrderType>>, 
						And<sOOrderNbr, Equal<Required<sOOrderNbr>>, 
						And<sOLineNbr, Equal<Required<sOLineNbr>>>>>>.SelectWindowed(graph, 0, 1, sOOrderType, sOOrderNbr, sOLineNbr);
			}
		}
		public static class FK
		{
			public class POLine : PO.POLine.PK.ForeignKeyOf<DropShipLink>.By<pOOrderType, pOOrderNbr, pOLineNbr> { }
			public class POOrder : PO.POOrder.PK.ForeignKeyOf<DropShipLink>.By<pOOrderType, pOOrderNbr> { }
			public class SupplyPOLine : SO.SupplyPOLine.PK.ForeignKeyOf<DropShipLink>.By<pOOrderType, pOOrderNbr, pOLineNbr> { }
			public class SupplyPOOrder : SO.SupplyPOOrder.PK.ForeignKeyOf<DropShipLink>.By<pOOrderType, pOOrderNbr> { }
			public class SOLine : SO.SOLine.PK.ForeignKeyOf<DropShipLink>.By<sOOrderType, sOOrderNbr, sOLineNbr> { }
			public class DemandSOOrder : PO.DemandSOOrder.PK.ForeignKeyOf<DropShipLink>.By<sOOrderType, sOOrderNbr> { }
		}
		#endregion
		#region SOOrderType
		public abstract class sOOrderType : PX.Data.BQL.BqlString.Field<sOOrderType> { }

		[PXParent(typeof(FK.SOLine))]
		[PXDefault]
		[PXDBString(2, IsFixed = true, IsKey = true)]
		public virtual string SOOrderType
		{
			get;
			set;
		}
		#endregion
		#region SOOrderNbr
		public abstract class sOOrderNbr : PX.Data.BQL.BqlString.Field<sOOrderNbr> { }

		[PXDBDefault(typeof(SO.SOLine.orderNbr))]
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		public virtual string SOOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region SOLineNbr
		public abstract class sOLineNbr : PX.Data.BQL.BqlInt.Field<sOLineNbr> { }

		[PXDefault]
		[PXDBInt(IsKey = true)]
		public virtual int? SOLineNbr
		{
			get;
			set;
		}
		#endregion
		#region POOrderType
		public abstract class pOOrderType : PX.Data.BQL.BqlString.Field<pOOrderType> { }

		[PXParent(typeof(FK.POLine))]
		[PXDefault]
		[PO.POOrderType.List]
		[PXDBString(2, IsFixed = true, IsKey = true)]
		public virtual string POOrderType
		{
			get;
			set;
		}
		#endregion
		#region POOrderNbr
		public abstract class pOOrderNbr : PX.Data.BQL.BqlString.Field<pOOrderNbr> { }

		[PXDBDefault(typeof(PO.POLine.orderNbr))]
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
		public virtual string POOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region POLineNbr
		public abstract class pOLineNbr : PX.Data.BQL.BqlInt.Field<pOLineNbr> { }

		[PXDefault]
		[PXDBInt(IsKey = true)]
		public virtual int? POLineNbr
		{
			get;
			set;
		}
		#endregion
		#region Active
		public abstract class active : PX.Data.BQL.BqlBool.Field<active> { }

		[PXDefault(false)]
		[PXDBBool]
		public virtual bool? Active
		{
			get;
			set;
		}
		#endregion
		#region InReceipt
		public abstract class inReceipt : PX.Data.BQL.BqlBool.Field<inReceipt> { }

		[PXDefault(false)]
		[PXDBBool]
		public virtual bool? InReceipt
		{
			get;
			set;
		}
		#endregion
		#region SOCompleted
		public abstract class soCompleted : PX.Data.BQL.BqlBool.Field<soCompleted> { }

		[PXDefault(false)]
		[PXDBBool]
		public virtual bool? SOCompleted
		{
			get;
			set;
		}
		#endregion
		#region SOInventoryID
		public abstract class soInventoryID : PX.Data.BQL.BqlInt.Field<soInventoryID> { }

		[PXDefault]
		[PXDBInt]
		public virtual Int32? SOInventoryID
		{
			get;
			set;
		}
		#endregion
		#region SOSiteID
		public abstract class soSiteID : PX.Data.BQL.BqlInt.Field<soSiteID> { }

		[PXDefault]
		[PXDBInt]
		public virtual Int32? SOSiteID
		{
			get;
			set;
		}
		#endregion
		#region SOBaseOrderQty
		public abstract class soBaseOrderQty : PX.Data.BQL.BqlDecimal.Field<soBaseOrderQty> { }

		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBDecimal(6)]
		public virtual Decimal? SOBaseOrderQty
		{
			get;
			set;
		}
		#endregion
		#region POCompleted
		public abstract class poCompleted : PX.Data.BQL.BqlBool.Field<poCompleted> { }

		[PXDefault(false)]
		[PXDBBool]
		public virtual bool? POCompleted
		{
			get;
			set;
		}
		#endregion
		#region POInventoryID
		public abstract class poInventoryID : PX.Data.BQL.BqlInt.Field<poInventoryID> { }

		[PXDefault]
		[PXDBInt]
		public virtual Int32? POInventoryID
		{
			get;
			set;
		}
		#endregion
		#region POSiteID
		public abstract class poSiteID : PX.Data.BQL.BqlInt.Field<poSiteID> { }

		[PXDefault]
		[PXDBInt]
		public virtual Int32? POSiteID
		{
			get;
			set;
		}
		#endregion
		#region POBaseOrderQty
		public abstract class poBaseOrderQty : PX.Data.BQL.BqlDecimal.Field<poBaseOrderQty> { }

		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBDecimal(6)]
		public virtual Decimal? POBaseOrderQty
		{
			get;
			set;
		}
		#endregion
		#region BaseReceivedQty
		public abstract class baseReceivedQty : PX.Data.BQL.BqlDecimal.Field<baseReceivedQty> { }

		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBDecimal(6)]
		public virtual Decimal? BaseReceivedQty
		{
			get;
			set;
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID]
		public virtual Guid? CreatedByID
		{
			get;
			set;
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID]
		public virtual String CreatedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp(RecordComesFirst = true)]
		public virtual Byte[] tstamp
		{
			get;
			set;
		}
		#endregion
	}
}