using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.WorkflowAPI;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.RQ
{
	public partial class RQRequisitionOrder : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<RQRequisitionOrder>.By<reqNbr, orderCategory, orderType, orderNbr>
		{
			public static RQRequisitionOrder Find(PXGraph graph, string reqNbr, string orderCategory, string orderType, string orderNbr)
				=> FindBy(graph, reqNbr, orderCategory, orderType, orderNbr);
		}
		public static class FK
		{
			public class Requisition : RQRequisition.PK.ForeignKeyOf<RQRequisitionOrder>.By<reqNbr> { }
			public class SOOrder : SO.SOOrder.PK.ForeignKeyOf<RQRequisitionOrder>.By<orderType, orderNbr> { }
			public class POOrder : PO.POOrder.PK.ForeignKeyOf<RQRequisitionOrder>.By<orderType, orderNbr> { }
		}
		#endregion
		#region Events
		public class Events : PXEntityEvent<RQRequisitionOrder>.Container<Events>
		{
			public PXEntityEvent<RQRequisitionOrder, SO.SOOrder> SOOrderUnlinked;
			public PXEntityEvent<RQRequisitionOrder, PO.POOrder> POOrderUnlinked;
		}
		#endregion

		#region ReqNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault(typeof(RQRequisition.reqNbr))]
		[PXParent(typeof(FK.Requisition))]
		public virtual String ReqNbr { get; set; }
		public abstract class reqNbr : BqlString.Field<reqNbr> { }
		#endregion
		#region OrderCategory
		[PXDBString(2, IsKey = true, IsFixed = true, InputMask = ">aa")]
		[PXDefault]
		public virtual String OrderCategory { get; set; }
		public abstract class orderCategory : BqlString.Field<orderCategory> { }
		#endregion
		#region OrderType
		[PXDBString(2, IsKey = true, IsFixed = true, InputMask = ">aa")]
		[PXDefault]
		public virtual String OrderType { get; set; }
		public abstract class orderType : BqlString.Field<orderType> { }
		#endregion
		#region OrderNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault]
		public virtual String OrderNbr { get; set; }
		public abstract class orderNbr : BqlString.Field<orderNbr> { }
		#endregion
	}

	public class RQOrderCategory
	{
		public const string PO = "PO";
		public const string SO = "SO";

		public class po : BqlString.Constant<po> { public po() : base(PO) { } }
		public class so : BqlString.Constant<so> { public so() : base(SO) { } }
	}
}