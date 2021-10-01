using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.GL
{
	[PXCacheName(Messages.BranchAcctMap)]
	[Serializable]
	[PXHidden]
	public partial class BranchAcctMap : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<BranchAcctMap>.By<branchID, lineNbr>
		{
			public static BranchAcctMap Find(PXGraph graph, int? branchID, int? lineNbr) => FindBy(graph, branchID, lineNbr);
		}
		public static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<BranchAcctMap>.By<branchID> { }
			public class FromBranch : GL.Branch.PK.ForeignKeyOf<BranchAcctMap>.By<fromBranchID> { }
			public class ToBranch : GL.Branch.PK.ForeignKeyOf<BranchAcctMap>.By<toBranchID> { }
			public class FromAccount : GL.Account.UK.ForeignKeyOf<BranchAcctMap>.By<fromAccountCD> { }
			public class ToAccount : GL.Account.UK.ForeignKeyOf<BranchAcctMap>.By<toAccountCD> { }
			public class MapAccount : GL.Account.PK.ForeignKeyOf<BranchAcctMap>.By<mapAccountID> { }
			public class MapSubaccount : GL.Sub.PK.ForeignKeyOf<BranchAcctMap>.By<mapSubID> { }
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(Branch.branchID))]
		public virtual int? BranchID
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(Branch.acctMapNbr))]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region FromBranchID
		public abstract class fromBranchID : PX.Data.BQL.BqlInt.Field<fromBranchID> { }
		[PXDBInt]
		public virtual int? FromBranchID
		{
			get;
			set;
		}
		#endregion
		#region ToBranchID
		public abstract class toBranchID : PX.Data.BQL.BqlInt.Field<toBranchID> { }
		[PXDBInt]
		public virtual int? ToBranchID
		{
			get;
			set;
		}
		#endregion
		#region FromAccountCD
		public abstract class fromAccountCD : PX.Data.BQL.BqlString.Field<fromAccountCD> { }
		[PXDefault]
		[AccountRaw]
		public virtual string FromAccountCD
		{
			get;
			set;
		}
		#endregion
		#region ToAccountCD
		public abstract class toAccountCD : PX.Data.BQL.BqlString.Field<toAccountCD> { }
		[PXDefault]
		[AccountRaw]
		public virtual string ToAccountCD
		{
			get;
			set;
		}
		#endregion
		#region MapAccountID
		public abstract class mapAccountID : PX.Data.BQL.BqlInt.Field<mapAccountID> { }
		[Account(DescriptionField = typeof(Account.description))]
		[PXDefault]
		[PXForeignReference(typeof(Field<BranchAcctMap.mapAccountID>.IsRelatedTo<Account.accountID>))]
        public virtual int? MapAccountID
		{
			get;
			set;
		}
		#endregion
		#region MapSubID
		public abstract class mapSubID : PX.Data.BQL.BqlInt.Field<mapSubID> { }
		[SubAccount(typeof(BranchAcctMap.mapAccountID), DescriptionField = typeof(Account.description))]
		[PXDefault]
		[PXForeignReference(typeof(Field<BranchAcctMap.mapSubID>.IsRelatedTo<Sub.subID>))]
        public virtual int? MapSubID
		{
			get;
			set;
		}
		#endregion
	}

	[PXProjection(typeof(Select<BranchAcctMap, Where<BranchAcctMap.branchID, Equal<BranchAcctMap.fromBranchID>>>), Persistent = true)]
	[PXCacheName(Messages.BranchAcctMapFrom)]
	[Serializable]
	public partial class BranchAcctMapFrom : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<BranchAcctMapFrom>.By<branchID, lineNbr>
		{
			public static BranchAcctMapFrom Find(PXGraph graph, int? branchID, int? lineNbr) => FindBy(graph, branchID, lineNbr);
		}
		public static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<BranchAcctMapFrom>.By<branchID> { }
			public class FromBranch : GL.Branch.PK.ForeignKeyOf<BranchAcctMapFrom>.By<fromBranchID> { }
			public class ToBranch : GL.Branch.PK.ForeignKeyOf<BranchAcctMapFrom>.By<toBranchID> { }
			public class FromAccount : GL.Account.UK.ForeignKeyOf<BranchAcctMapFrom>.By<fromAccountCD> { }
			public class ToAccount : GL.Account.UK.ForeignKeyOf<BranchAcctMapFrom>.By<toAccountCD> { }
			public class MapAccount : GL.Account.PK.ForeignKeyOf<BranchAcctMapFrom>.By<mapAccountID> { }
			public class MapSubaccount : GL.Sub.PK.ForeignKeyOf<BranchAcctMapFrom>.By<mapSubID> { }
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(BranchAcctMap))]
		[PXDBDefault(typeof(Branch.branchID))]
		public virtual int? BranchID
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(BranchAcctMap))]
		[PXLineNbr(typeof(Branch.acctMapNbr))]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region FromBranchID
		public abstract class fromBranchID : PX.Data.BQL.BqlInt.Field<fromBranchID> { }
		[PXDBInt(BqlTable = typeof(BranchAcctMap))]
		[PXDBDefault(typeof(Branch.branchID))]
		public virtual int? FromBranchID
		{
			get;
			set;
		}
		#endregion
		#region ToBranchID
		public abstract class toBranchID : PX.Data.BQL.BqlInt.Field<toBranchID> { }
		[PXDBInt(BqlTable = typeof(BranchAcctMap))]
		[PXSelector(typeof(Search<Branch.branchID, Where<Branch.branchID, NotEqual<Current<BranchAcctMapFrom.branchID>>>>), SubstituteKey = typeof(Branch.branchCD))]
		[PXUIField(DisplayName = "Destination Branch")]
		[PXRestrictor(typeof(Where<Branch.active, Equal<True>>), GL.Messages.BranchInactive)]
		public virtual int? ToBranchID
		{
			get;
			set;
		}
		#endregion
		#region FromAccountCD
		public abstract class fromAccountCD : PX.Data.BQL.BqlString.Field<fromAccountCD> { }
		[PXDBString(10, IsUnicode = true, InputMask = "", BqlTable = typeof(BranchAcctMap))]
		[PXDefault]
		[PXDimensionSelector("ACCOUNT", typeof(Account.accountCD), DescriptionField = typeof(Account.description))]
		[PXUIField(DisplayName = "Account From")]
		public virtual string FromAccountCD
		{
			get;
			set;
		}
		#endregion
		#region ToAccountCD
		public abstract class toAccountCD : PX.Data.BQL.BqlString.Field<toAccountCD> { }
		[PXDBString(10, IsUnicode = true, InputMask = "", BqlTable = typeof(BranchAcctMap))]
		[PXDefault]
		[PXDimensionSelector("ACCOUNT", typeof(Account.accountCD), DescriptionField = typeof(Account.description))]
		[PXUIField(DisplayName = "Account To")]
		public virtual string ToAccountCD
		{
			get;
			set;
		}
		#endregion
		#region MapAccountID
		public abstract class mapAccountID : PX.Data.BQL.BqlInt.Field<mapAccountID> { }
		[Account(null, typeof(Search<Account.accountID,
			Where<Account.isCashAccount, Equal<False>, And<Account.curyID, IsNull>>>),
			DescriptionField = typeof(Account.description), BqlTable = typeof(BranchAcctMap), DisplayName = "Offset Account", AvoidControlAccounts = true)]
		[PXDefault]
		[PXForeignReference(typeof(Field<BranchAcctMapFrom.mapAccountID>.IsRelatedTo<Account.accountID>))]
		public virtual int? MapAccountID
		{
			get;
			set;
		}
		#endregion
		#region MapSubID
		public abstract class mapSubID : PX.Data.BQL.BqlInt.Field<mapSubID> { }
		[SubAccount(typeof(BranchAcctMapFrom.mapAccountID), DisplayName = "Offset Subaccount", DescriptionField = typeof(Account.description), BqlTable = typeof(BranchAcctMap))]
		[PXDefault]
		[PXForeignReference(typeof(Field<BranchAcctMapFrom.mapSubID>.IsRelatedTo<Sub.subID>))]
		public virtual int? MapSubID
		{
			get;
			set;
		}
		#endregion
	}

	[PXProjection(typeof(Select<BranchAcctMap, Where<BranchAcctMap.branchID, Equal<BranchAcctMap.toBranchID>>>), Persistent = true)]
	[PXCacheName(Messages.BranchAcctMapTo)]
	[Serializable]
	public partial class BranchAcctMapTo : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<BranchAcctMapTo>.By<branchID, lineNbr>
		{
			public static BranchAcctMapTo Find(PXGraph graph, int? branchID, int? lineNbr) => FindBy(graph, branchID, lineNbr);
		}
		public static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<BranchAcctMapTo>.By<branchID> { }
			public class FromBranch : GL.Branch.PK.ForeignKeyOf<BranchAcctMapTo>.By<fromBranchID> { }
			public class ToBranch : GL.Branch.PK.ForeignKeyOf<BranchAcctMapTo>.By<toBranchID> { }
			public class FromAccount : GL.Account.UK.ForeignKeyOf<BranchAcctMapTo>.By<fromAccountCD> { }
			public class ToAccount : GL.Account.UK.ForeignKeyOf<BranchAcctMapTo>.By<toAccountCD> { }
			public class MapAccount : GL.Account.PK.ForeignKeyOf<BranchAcctMapTo>.By<mapAccountID> { }
			public class MapSubaccount : GL.Sub.PK.ForeignKeyOf<BranchAcctMapTo>.By<mapSubID> { }
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(BranchAcctMap))]
		[PXDBDefault(typeof(Branch.branchID))]
		public virtual int? BranchID
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true, BqlTable = typeof(BranchAcctMap))]
		[PXLineNbr(typeof(Branch.acctMapNbr))]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region FromBranchID
		public abstract class fromBranchID : PX.Data.BQL.BqlInt.Field<fromBranchID> { }
		[PXDBInt(BqlTable = typeof(BranchAcctMap))]
		[PXSelector(typeof(Search<Branch.branchID, Where<Branch.branchID, NotEqual<Current<BranchAcctMapFrom.branchID>>>>), SubstituteKey = typeof(Branch.branchCD))]
		[PXUIField(DisplayName = "Destination Branch")]
		[PXRestrictor(typeof(Where<Branch.active, Equal<True>>), GL.Messages.BranchInactive)]
		public virtual int? FromBranchID
		{
			get;
			set;
		}
		#endregion
		#region ToBranchID
		public abstract class toBranchID : PX.Data.BQL.BqlInt.Field<toBranchID> { }
		[PXDBInt(BqlTable = typeof(BranchAcctMap))]
		[PXDBDefault(typeof(Branch.branchID))]
		public virtual int? ToBranchID
		{
			get;
			set;
		}
		#endregion
		#region FromAccountCD
		public abstract class fromAccountCD : PX.Data.BQL.BqlString.Field<fromAccountCD> { }
		[PXDBString(10, IsUnicode = true, InputMask = "", BqlTable = typeof(BranchAcctMap))]
		[PXDefault]
		[PXDimensionSelector("ACCOUNT", typeof(Account.accountCD), DescriptionField = typeof(Account.description))]
		[PXUIField(DisplayName = "Account From")]
		public virtual string FromAccountCD
		{
			get;
			set;
		}
		#endregion
		#region ToAccountCD
		public abstract class toAccountCD : PX.Data.BQL.BqlString.Field<toAccountCD> { }
		[PXDBString(10, IsUnicode = true, InputMask = "", BqlTable = typeof(BranchAcctMap))]
		[PXDefault]
		[PXDimensionSelector("ACCOUNT", typeof(Account.accountCD), DescriptionField = typeof(Account.description))]
		[PXUIField(DisplayName = "Account To")]
		public virtual string ToAccountCD
		{
			get;
			set;
		}
		#endregion
		#region MapAccountID
		public abstract class mapAccountID : PX.Data.BQL.BqlInt.Field<mapAccountID> { }
		[Account(null, typeof(Search<Account.accountID,
			Where<Account.isCashAccount, Equal<False>, And<Account.curyID, IsNull>>>),
			DescriptionField = typeof(Account.description), BqlTable = typeof(BranchAcctMap), DisplayName = "Offset Account", AvoidControlAccounts = true)]
		[PXDefault]
		[PXForeignReference(typeof(Field<BranchAcctMapTo.mapAccountID>.IsRelatedTo<Account.accountID>))]
		public virtual int? MapAccountID
		{
			get;
			set;
		}
		#endregion
		#region MapSubID
		public abstract class mapSubID : PX.Data.BQL.BqlInt.Field<mapSubID> { }
		[SubAccount(typeof(BranchAcctMapTo.mapAccountID), DisplayName = "Offset Subaccount", DescriptionField = typeof(Account.description), BqlTable = typeof(BranchAcctMap))]
		[PXDefault]
		[PXForeignReference(typeof(Field<BranchAcctMapTo.mapSubID>.IsRelatedTo<Sub.subID>))]
		public virtual int? MapSubID
		{
			get;
			set;
		}
		#endregion
	}
}
