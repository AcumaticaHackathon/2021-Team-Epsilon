using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
	[PXHidden]
	[PXProjection(typeof(Select<SOOrder>))]
	public class DemandSOOrder : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<DemandSOOrder>.By<orderType, orderNbr>
		{
			public static DemandSOOrder Find(PXGraph graph, string orderType, string orderNbr) => FindBy(graph, orderType, orderNbr);
			public static DemandSOOrder FindDirty(PXGraph graph, string orderType, string orderNbr) => PXSelect<DemandSOOrder,
				Where<orderType, Equal<Required<orderType>>,
					And<orderNbr, Equal<Required<orderNbr>>>>>
					.SelectWindowed(graph, 0, 1, orderType, orderNbr);
		}
		#endregion
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

		[PXDefault]
		[PXDBString(2, IsFixed = true, IsKey = true, BqlField = typeof(SOOrder.orderType))]
		public virtual string OrderType
		{
			get;
			set;
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }

		[PXDefault]
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(SOOrder.orderNbr))]
		public virtual string OrderNbr
		{
			get;
			set;
		}
		#endregion
		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

		[PXDBString(1, IsFixed = true, BqlField = typeof(SOOrder.status))]
		[PXUIField(DisplayName = "Sales Order Status", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[SOOrderStatus.List]
		public virtual String Status
		{
			get;
			set;
		}
		#endregion
		#region Cancelled
		public abstract class cancelled : PX.Data.BQL.BqlBool.Field<cancelled> { }
		
		[PXDBBool(BqlField = typeof(SOOrder.cancelled))]
		public virtual Boolean? Cancelled
		{
			get;
			set;
		}
		#endregion
	}
}
