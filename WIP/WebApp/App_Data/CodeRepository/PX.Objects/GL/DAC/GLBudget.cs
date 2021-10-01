using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.GL
{
	[Serializable]
	[PXPrimaryGraph(typeof(GLBudgetEntry), Filter = typeof(BudgetFilter))]
	[PXCacheName(GL.Messages.Budget)]
	public partial class GLBudget : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<GLBudget>.By<branchID, ledgerID, finYear>
		{
			public static GLBudget Find(PXGraph graph, int? branchID, int? ledgerID, string finYear) => FindBy(graph, branchID, ledgerID, finYear);
		}
		public static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<GLBudget>.By<branchID> { }
			public class Ledger : GL.Ledger.PK.ForeignKeyOf<GLBudget>.By<ledgerID> { }
		}
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

		/// <summary>
		/// The identifier of the <see cref="Branch">branch</see> to which the budget article belongs.
		/// This field is a part of the compound key.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Branch.BranchID"/> field.
		/// </value>
		[PXDBInt(IsKey = true)]
		public virtual int? BranchID { get; set; }
		#endregion
		#region LedgerID
		public abstract class ledgerID : PX.Data.BQL.BqlInt.Field<ledgerID> { }

		/// <summary>
		/// The identifier of the <see cref="Ledger">ledger</see> to which the budget article belongs.
		/// This field is a part of the compound key.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Ledger.LedgerID"/> field.
		/// </value>
		[PXDBInt(IsKey = true)]
		public virtual int? LedgerID { get; set; }
		#endregion
		#region FinYear
		public abstract class finYear : PX.Data.BQL.BqlString.Field<finYear> { }

		/// <summary>
		/// The <see cref="FinYear">financial year</see> to which the budget article belongs.
		/// This field is a part of the compound key.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="FinYear.year"/> field.
		/// </value>
		[PXDBString(4, IsKey = true, IsFixed = true)]
		public virtual string FinYear { get; set; }
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		[PXDBCreatedByID(Visible = false)]
		public virtual Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID(Visible = false)]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID]
		public virtual String LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
		#endregion
	}
}