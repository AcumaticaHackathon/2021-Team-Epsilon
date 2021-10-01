using PX.Data;
using System;
using PX.Objects.SO;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Commerce.Objects
{
	[Serializable]
	[PXCacheName("SO Order Risks")]
	public class SOOrderRisks : IBqlTable
	{
		public class PK : PrimaryKeyOf<SOOrderRisks>.By<SOOrderRisks.orderType, SOOrderRisks.orderNbr>
		{
			public static SOOrderRisks Find(PXGraph graph, string orderType, string orderNbr) => FindBy(graph, orderType, orderNbr);
		}
		public static class FK
		{
			public class Entity : SOOrderRisks.PK.ForeignKeyOf<SOOrder>.By<orderType, orderNbr> { }
		}
		#region OrderType
		[PXDBString(IsKey = true)]
		[PXDBDefault(typeof(SOOrder.orderType))]
		public virtual string OrderType { get; set; }
		public abstract class orderType : IBqlField { }
		#endregion

		#region OrderNbr
		[PXDBString(IsKey = true)]
		[PXDBDefault(typeof(SOOrder.orderNbr))]
		public virtual string OrderNbr { get; set; }
		public abstract class orderNbr : IBqlField { }
		#endregion

		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		[PXUIField(DisplayName ="Line Nbr." , Visible =false)]
		[PXLineNbr(typeof(SOOrder.riskLineCntr))]
		[PXParent(typeof(Select<SOOrder, Where<SOOrder.orderType, Equal<Current<SOOrderRisks.orderType>>,
											And<SOOrder.orderNbr, Equal<Current<SOOrderRisks.orderNbr>>
											>>>))]
		public virtual Int32? LineNbr { get; set; }
		#endregion

		#region NoteID
		[PXNote()]
		public Guid? NoteID { get; set; }
		public abstract class noteID : IBqlField { }
		#endregion

		#region Recommendation
		[PXDBString(IsUnicode = true)]
		[PXUIField(DisplayName = "Recommendation")]
		public virtual string Recommendation { get; set; }
		public abstract class recommendation : IBqlField { }
		#endregion

		#region Message
		[PXDBString(IsUnicode = true)]
		[PXUIField(DisplayName = "Message")]
		public virtual string Message { get; set; }
		public abstract class message : IBqlField { }
		#endregion

		#region Score
		[PXDBDecimal()]
		[PXUIField(DisplayName = "Score %")]
	
		public virtual decimal? Score { get; set; }
		public abstract class score : IBqlField { }
		#endregion
	}
}
