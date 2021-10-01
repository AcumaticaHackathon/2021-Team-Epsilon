using System;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;

using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CM;

namespace PX.Objects.IN
{
	[PXCacheName(Messages.INPIReview)]
	[PXPrimaryGraph(typeof(INPIReview))]
	[PXGroupMask(typeof(InnerJoin<INSite, On<INSite.siteID.IsEqual<siteID>.And<MatchUserFor<INSite>>>>))]
	public partial class INPIHeader : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<INPIHeader>.By<pIID>
		{
			public static INPIHeader Find(PXGraph graph, string pIID) => FindBy(graph, pIID);
		}
		public static class FK
		{
			public class Site : INSite.PK.ForeignKeyOf<INPIHeader>.By<siteID> { }
			public class PIClass : INPIClass.PK.ForeignKeyOf<INPIHeader>.By<pIClassID> { }
			public class PIAdjustmentAccount : Account.PK.ForeignKeyOf<INPIHeader>.By<pIAdjAcctID> { }
			public class PIAdjustmentSubaccount : Sub.PK.ForeignKeyOf<INPIHeader>.By<pIAdjSubID> { }
			//todo public class Adjustment : INRegister.PK.ForeignKeyOf<INPIHeader>.By<INDocType.adjustment, pIAdjRefNbr> { }
			//todo public class Receipt : INRegister.PK.ForeignKeyOf<INPIHeader>.By<INDocType.receipt, pIAdjRefNbr> { }
			//todo public class FinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INPIHeader>.By<finPeriodID> { }
			//todo public class MasterFinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INPIHeader>.By<tranPeriodID> { }
		}
		#endregion
		#region PIID
		[PXDefault]
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<pIID, Where<True, Equal<True>>, OrderBy<Desc<pIID>>>), Filterable = true)]
		[AutoNumber(typeof(INSetup.pINumberingID), typeof(AccessInfo.businessDate))]
		[PX.Data.EP.PXFieldDescription]
		public virtual String PIID { get; set; }
		public abstract class pIID : BqlString.Field<pIID> { }
		#endregion
		#region PIClassID
		[PXDBString(30, IsUnicode = true)]
		public virtual String PIClassID { get; set; }
		public abstract class pIClassID : BqlString.Field<pIClassID> { }
		#endregion
		#region Descr
		[PXDBString(Common.Constants.TranDescLength, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String Descr { get; set; }
		public abstract class descr : BqlString.Field<descr> { }
		#endregion

		#region LineCntr
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Number Of Lines", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual int? LineCntr { get; set; }
		public abstract class lineCntr : BqlInt.Field<lineCntr> { }
		#endregion
		#region TagNumbered
		[PXDBBool]
		[PXUIField(DisplayName = "Tag Numbered", Visibility = PXUIVisibility.Visible)]
		[PXDefault(typeof(Search<INSetup.pIUseTags>))]
		public virtual Boolean? TagNumbered { get; set; }
		public abstract class tagNumbered : BqlBool.Field<tagNumbered> { }
		#endregion
		#region FirstTagNbr
		[PXDBInt]
		public virtual Int32? FirstTagNbr { get; set; }
		public abstract class firstTagNbr : BqlInt.Field<firstTagNbr> { }
		#endregion
		#region FinPeriodID
		[FinPeriodID]
		public virtual String FinPeriodID { get; set; }
		public abstract class finPeriodID : BqlString.Field<finPeriodID> { }
		#endregion
		#region TranPeriodID
		[FinPeriodID]
		public virtual String TranPeriodID { get; set; }
		public abstract class tranPeriodID : BqlString.Field<tranPeriodID> { }
		#endregion
		#region PIAdjAcctID
		[Account(Enabled = false)]
		[PXDefault(typeof(Search2<ReasonCode.accountID, InnerJoin<INSetup, On<INSetup.pIReasonCode.IsEqual<ReasonCode.reasonCodeID>>>>))]
		public virtual Int32? PIAdjAcctID { get; set; }
		public abstract class pIAdjAcctID : BqlInt.Field<pIAdjAcctID> { }
		#endregion
		#region PIAdjSubID
		[SubAccount(Enabled = false)]
		[PXDefault(typeof(Search2<ReasonCode.subID, InnerJoin<INSetup, On<INSetup.pIReasonCode.IsEqual<ReasonCode.reasonCodeID>>>>))]
		public virtual Int32? PIAdjSubID { get; set; }
		public abstract class pIAdjSubID : BqlInt.Field<pIAdjSubID> { }
		#endregion
		#region SiteID
		[Site(Enabled = false)]
		[PXDefault]
		[PXForeignReference(typeof(FK.Site))]
		public virtual Int32? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion

		#region Status
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INPIHdrStatus.Counting)]
		[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[INPIHdrStatus.List]
		public virtual String Status { get; set; }
		public abstract class status : BqlString.Field<status> { }
		#endregion

		#region CountDate
		[PXDBDate]
		[PXDefault(typeof(AccessInfo.businessDate))]
		// for the user interface : renamed to the 'Freeze Date'
		[PXUIField(DisplayName = "Freeze Date", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? CountDate { get; set; }
		public abstract class countDate : BqlDateTime.Field<countDate> { }
		#endregion

		#region PIAdjRefNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Adjustment Ref. Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXSelector(typeof(INRegister.refNbr))]
		public virtual String PIAdjRefNbr { get; set; }
		public abstract class pIAdjRefNbr : BqlString.Field<pIAdjRefNbr> { }
		#endregion

		#region PIRcptRefNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Receipt Ref. Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXSelector(typeof(INRegister.refNbr))]
		public virtual String PIRcptRefNbr { get; set; }
		public abstract class pIRcptRefNbr : BqlString.Field<pIRcptRefNbr> { }
		#endregion

		#region TotalPhysicalQty
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Total Physical Qty.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? TotalPhysicalQty { get; set; }
		public abstract class totalPhysicalQty : BqlDecimal.Field<totalPhysicalQty> { }
		#endregion

		#region TotalVarQty
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Variance Qty.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? TotalVarQty { get; set; }
		public abstract class totalVarQty : BqlDecimal.Field<totalVarQty> { }
		#endregion
		#region TotalVarCost
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Variance Cost", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? TotalVarCost { get; set; }
		public abstract class totalVarCost : BqlDecimal.Field<totalVarCost> { }
		#endregion

		#region TotalNbrOfTags
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Number Of Tags", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual int? TotalNbrOfTags { get; set; }
		public abstract class totalNbrOfTags : BqlInt.Field<totalNbrOfTags> { }
		#endregion

		#region NoteID
		[PXNote(DescriptionField = typeof(pIID), Selector = typeof(pIID))]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : BqlGuid.Field<noteID> { }
		#endregion
		#region Audit Fields
		#region CreatedByID
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion
		#region CreatedDateTime
		[PXDBCreatedDateTime]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		public virtual String LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#region tstamp
		[PXDBTimestamp]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion
		#endregion
	}

	public class INPIHdrStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Counting, Messages.Counting),
				Pair(Entering, Messages.DataEntering),
				Pair(InReview, Messages.InReview),
				Pair(Completed, Messages.Completed),
				Pair(Cancelled, Messages.Cancelled))
			{ }
		}

		public const string Counting = "N";
		public const string Entering = "E";
		public const string InReview = "R";
		public const string Completed = "C";
		public const string Cancelled = "X";

		public class counting : BqlString.Constant<counting> { public counting() : base(Counting) { } }
		public class entering : BqlString.Constant<entering> { public entering() : base(Entering) { } }
		public class onReview : BqlString.Constant<onReview> { public onReview() : base(InReview) { } }
		public class completed : BqlString.Constant<completed> { public completed() : base(Completed) { } }
		public class cancelled : BqlString.Constant<cancelled> { public cancelled() : base(Cancelled) { } }
	}
}