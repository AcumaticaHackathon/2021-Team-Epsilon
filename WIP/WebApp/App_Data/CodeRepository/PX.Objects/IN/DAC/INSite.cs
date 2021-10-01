using System;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;

using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.IN
{
	[PXHidden]
	public class INCostSite : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<INCostSite>.By<costSiteID>
		{
			public static INCostSite Find(PXGraph graph, int? costSiteID) => FindBy(graph, costSiteID);
		}
		#endregion
		#region CostSiteID
		[PXDBIdentity]
		public virtual Int32? CostSiteID { get; set; }
		public abstract class costSiteID : BqlInt.Field<costSiteID> { }
		#endregion
		#region tstamp
		[PXDBTimestamp]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion
	}

	[PXPrimaryGraph(
		new Type[] { typeof(INSiteMaint) },
		new Type[] { typeof(SelectFrom<INSite>.Where<siteID.IsEqual<siteID.FromCurrent>>)
	})]
	[PXCacheName(Messages.INSite, PXDacType.Catalogue, CacheGlobal = true)]
	public class INSite : IBqlTable, PX.SM.IIncludable
	{
		#region Keys
		public class PK : PrimaryKeyOf<INSite>.By<siteID>.Dirty
		{
			public static INSite Find(PXGraph graph, int? siteID) => FindBy(graph, siteID, (siteID ?? 0) <= 0);
		}
		public class UK : PrimaryKeyOf<INSite>.By<siteCD>
		{
			public static INSite Find(PXGraph graph, string siteCD) => FindBy(graph, siteCD);
		}
		public static class FK
		{
			public class InventoryAccount : Account.PK.ForeignKeyOf<INSite>.By<invtAcctID> { }
			public class InventorySubaccount : Sub.PK.ForeignKeyOf<INSite>.By<invtSubID> { }
			public class COGSAccount : Account.PK.ForeignKeyOf<INSite>.By<cOGSAcctID> { }
			public class COGSSubaccount : Sub.PK.ForeignKeyOf<INSite>.By<cOGSSubID> { }
			public class SalesAccount : Account.PK.ForeignKeyOf<INSite>.By<salesAcctID> { }
			public class SalesSubaccount : Sub.PK.ForeignKeyOf<INSite>.By<salesSubID> { }
			public class StandardCostRevaluationAccount : Account.PK.ForeignKeyOf<INSite>.By<stdCstRevAcctID> { }
			public class StandardCostRevaluationSubaccount : Sub.PK.ForeignKeyOf<INSite>.By<stdCstRevSubID> { }
			public class PPVAccount : Account.PK.ForeignKeyOf<INSite>.By<pPVAcctID> { }
			public class PPVSubaccount : Sub.PK.ForeignKeyOf<INSite>.By<pPVSubID> { }
			public class POAccrualAccount : Account.PK.ForeignKeyOf<INSite>.By<pOAccrualAcctID> { }
			public class POAccrualSubaccount : Sub.PK.ForeignKeyOf<INSite>.By<pOAccrualSubID> { }
			public class StandardCostVarianceAccount : Account.PK.ForeignKeyOf<INSite>.By<stdCstVarAcctID> { }
			public class StandardCostVarianceSubaccount : Sub.PK.ForeignKeyOf<INSite>.By<stdCstVarSubID> { }
			public class LandedCostVarianceAccount : Account.PK.ForeignKeyOf<INSite>.By<lCVarianceAcctID> { }
			public class LandedCostVarianceSubaccount : Sub.PK.ForeignKeyOf<INSite>.By<lCVarianceSubID> { }
			public class ReasonCodeSubaccount : Sub.PK.ForeignKeyOf<INSite>.By<reasonCodeSubID> { }

			public class ReceiptLocation : INLocation.PK.ForeignKeyOf<INSite>.By<receiptLocationID> { }
			public class ShipLocation : INLocation.PK.ForeignKeyOf<INSite>.By<shipLocationID> { }
			public class ReturnLocation : INLocation.PK.ForeignKeyOf<INSite>.By<returnLocationID> { }
			public class DropShipLocation : INLocation.PK.ForeignKeyOf<INSite>.By<dropShipLocationID> { }
			public class Branch : GL.Branch.PK.ForeignKeyOf<INSite>.By<branchID> { }
			public class ReplenishmentClass : INReplenishmentClass.PK.ForeignKeyOf<INSite>.By<replenishmentClassID> { }
			public class Address : CR.Address.PK.ForeignKeyOf<INSite>.By<addressID> { }
			public class Contact : CR.Contact.PK.ForeignKeyOf<INSite>.By<contactID> { }
			public class Building : INSiteBuilding.PK.ForeignKeyOf<INSite>.By<buildingID> { }

			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use InventorySubaccount instead.")]
			public class InvtSub : InventorySubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use COGSSubaccount instead.")]
			public class COGSSub : COGSSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use SalesSubaccount instead.")]
			public class SalesSub : SalesSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use StandardCostRevaluationSubaccount instead.")]
			public class StdCstRevSub : StandardCostRevaluationSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use PPVSubaccount instead.")]
			public class PPVSub : PPVSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use POAccrualSubaccount instead.")]
			public class POAccrualSub : POAccrualSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use StandardCostVarianceSubaccount instead.")]
			public class StdCstVarSub : StandardCostVarianceSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use LandedCostVarianceSubaccount instead.")]
			public class LCVarianceSub : LandedCostVarianceSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use ReasonCodeSubaccount instead.")]
			public class ReasonCodeSub : ReasonCodeSubaccount { }
		}
		#endregion
		#region SiteID
		[PXDBForeignIdentity(typeof(INCostSite))]
		[PXReferentialIntegrityCheck]
		public virtual Int32? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region SiteCD
		[PXRestrictor(typeof(Where<siteID.IsNotEqual<SiteAttribute.transitSiteID>>), Messages.TransitSiteIsNotAvailable)]
		[SiteRaw(true, IsKey = true)]
		[PXDefault]
		[PX.Data.EP.PXFieldDescription]
		public virtual String SiteCD { get; set; }
		public abstract class siteCD : BqlString.Field<siteCD> { }
		#endregion
		#region Descr
		[PXDBString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		[PX.Data.EP.PXFieldDescription]
		public virtual String Descr { get; set; }
		public abstract class descr : BqlString.Field<descr> { }
		#endregion
		#region ReasonCodeSubID
		[SubAccount(DisplayName = "Reason Code Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		public virtual Int32? ReasonCodeSubID { get; set; }
		public abstract class reasonCodeSubID : BqlInt.Field<reasonCodeSubID> { }
		#endregion
		#region SalesAcctID
		[Account(DisplayName = "Sales Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(FK.SalesAccount))]
		public virtual Int32? SalesAcctID { get; set; }
		public abstract class salesAcctID : BqlInt.Field<salesAcctID> { }
		#endregion
		#region SalesSubID
		[SubAccount(typeof(salesAcctID), DisplayName = "Sales Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.SalesSubaccount))]
		public virtual Int32? SalesSubID { get; set; }
		public abstract class salesSubID : BqlInt.Field<salesSubID> { }
		#endregion
		#region InvtAcctID
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[Account(DisplayName = "Inventory Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), ControlAccountForModule = ControlAccountModule.IN)]
		[PXForeignReference(typeof(FK.InventoryAccount))]
		public virtual Int32? InvtAcctID { get; set; }
		public abstract class invtAcctID : BqlInt.Field<invtAcctID> { }
		#endregion
		#region InvtSubID
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[SubAccount(typeof(invtAcctID), DisplayName = "Inventory Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.InventorySubaccount))]
		public virtual Int32? InvtSubID { get; set; }
		public abstract class invtSubID : BqlInt.Field<invtSubID> { }
		#endregion
		#region COGSAcctID
		[Account(DisplayName = "COGS/Expense Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(FK.COGSAccount))]
		public virtual Int32? COGSAcctID { get; set; }
		public abstract class cOGSAcctID : BqlInt.Field<cOGSAcctID> { }
		#endregion
		#region COGSSubID
		[SubAccount(typeof(cOGSAcctID), DisplayName = "COGS/Expense Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.COGSSubaccount))]
		public virtual Int32? COGSSubID { get; set; }
		public abstract class cOGSSubID : BqlInt.Field<cOGSSubID> { }
		#endregion
		#region StdCstRevAcctID
		[Account(DisplayName = "Standard Cost Revaluation Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(FK.StandardCostRevaluationAccount))]
		public virtual Int32? StdCstRevAcctID { get; set; }
		public abstract class stdCstRevAcctID : BqlInt.Field<stdCstRevAcctID> { }
		#endregion
		#region StdCstRevSubID
		[SubAccount(typeof(stdCstRevAcctID), DisplayName = "Standard Cost Revaluation Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.StandardCostRevaluationSubaccount))]
		public virtual Int32? StdCstRevSubID { get; set; }
		public abstract class stdCstRevSubID : BqlInt.Field<stdCstRevSubID> { }
		#endregion
		#region StdCstVarAcctID
		[Account(DisplayName = "Standard Cost Variance Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(FK.StandardCostVarianceAccount))]
		public virtual Int32? StdCstVarAcctID { get; set; }
		public abstract class stdCstVarAcctID : BqlInt.Field<stdCstVarAcctID> { }
		#endregion
		#region StdCstVarSubID
		[SubAccount(typeof(stdCstVarAcctID), DisplayName = "Standard Cost Variance Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.StandardCostVarianceSubaccount))]
		public virtual Int32? StdCstVarSubID { get; set; }
		public abstract class stdCstVarSubID : BqlInt.Field<stdCstVarSubID> { }
		#endregion
		#region PPVAcctID
		[Account(DisplayName = "Purchase Price Variance Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(FK.PPVAccount))]
		public virtual Int32? PPVAcctID { get; set; }
		public abstract class pPVAcctID : BqlInt.Field<pPVAcctID> { }
		#endregion
		#region PPVSubID
		[SubAccount(typeof(pPVAcctID), DisplayName = "Purchase Price Variance Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.PPVSubaccount))]
		public virtual Int32? PPVSubID { get; set; }
		public abstract class pPVSubID : BqlInt.Field<pPVSubID> { }
		#endregion
		#region POAccrualAcctID
		[Account(DisplayName = "PO Accrual Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), ControlAccountForModule = ControlAccountModule.PO)]
		[PXForeignReference(typeof(FK.POAccrualAccount))]
		public virtual Int32? POAccrualAcctID { get; set; }
		public abstract class pOAccrualAcctID : BqlInt.Field<pOAccrualAcctID> { }
		#endregion
		#region POAccrualSubID
		[SubAccount(typeof(pOAccrualAcctID), DisplayName = "PO Accrual Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.POAccrualSubaccount))]
		public virtual Int32? POAccrualSubID { get; set; }
		public abstract class pOAccrualSubID : BqlInt.Field<pOAccrualSubID> { }
		#endregion
		#region LCVarianceAcctID
		[Account(DisplayName = "Landed Cost Variance Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXForeignReference(typeof(FK.LandedCostVarianceAccount))]
		public virtual Int32? LCVarianceAcctID { get; set; }
		public abstract class lCVarianceAcctID : BqlInt.Field<lCVarianceAcctID> { }
		#endregion
		#region LCVarianceSubID
		[SubAccount(typeof(lCVarianceAcctID), DisplayName = "Landed Cost Variance Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.LandedCostVarianceSubaccount))]
		public virtual Int32? LCVarianceSubID { get; set; }
		public abstract class lCVarianceSubID : BqlInt.Field<lCVarianceSubID> { }
		#endregion
		#region ReceiptLocationID
		[PXRestrictor(typeof(Where<INLocation.active.IsEqual<True>>), Messages.LocationIsNotActive)]
		[Location(typeof(siteID), DisplayName = "Receiving Location", DescriptionField = typeof(INLocation.descr), DirtyRead = true)]
		public virtual Int32? ReceiptLocationID { get; set; }
		public abstract class receiptLocationID : BqlInt.Field<receiptLocationID> { }
		#endregion
		#region ReceiptLocationIDOverride
		[PXBool]
		public virtual Boolean? ReceiptLocationIDOverride { get; set; }
		public abstract class receiptLocationIDOverride : BqlBool.Field<receiptLocationIDOverride> { }
		#endregion
		#region ShipLocationID
		[PXRestrictor(typeof(Where<INLocation.active.IsEqual<True>>), Messages.LocationIsNotActive)]
		[Location(typeof(siteID), DisplayName = "Shipping Location", DescriptionField = typeof(INLocation.descr), DirtyRead = true)]
		public virtual Int32? ShipLocationID { get; set; }
		public abstract class shipLocationID : BqlInt.Field<shipLocationID> { }
		#endregion
		#region ShipLocationIDOverride
		[PXBool]
		public virtual Boolean? ShipLocationIDOverride { get; set; }
		public abstract class shipLocationIDOverride : BqlBool.Field<shipLocationIDOverride> { }
		#endregion
		#region DropShipLocationID
		[PXRestrictor(typeof(Where<INLocation.active.IsEqual<True>>), Messages.LocationIsNotActive)]
		[Location(typeof(siteID), DisplayName = "Drop-Ship Location", DescriptionField = typeof(INLocation.descr), DirtyRead = true)]
		public virtual Int32? DropShipLocationID { get; set; }
		public abstract class dropShipLocationID : BqlInt.Field<dropShipLocationID> { }
		#endregion
		#region ReturnLocationID
		[PXRestrictor(typeof(Where<INLocation.active.IsEqual<True>>), Messages.LocationIsNotActive)]
		[Location(typeof(siteID), DisplayName = "RMA Location", DescriptionField = typeof(INLocation.descr), DirtyRead = true)]
		public virtual Int32? ReturnLocationID { get; set; }
		public abstract class returnLocationID : BqlInt.Field<returnLocationID> { }
		#endregion
		#region LocationValid
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Location Entry")]
		[INLocationValid.List]
		[PXDefault(INLocationValid.Validate)]
		public virtual String LocationValid { get; set; }
		public abstract class locationValid : BqlString.Field<locationValid> { }
		#endregion
		#region BranchID
		[Branch]
		public virtual Int32? BranchID { get; set; }
		public abstract class branchID : BqlInt.Field<branchID> { }
		#endregion
		#region BAccountID
		[PXDBInt]
		[PXFormula(typeof(Selector<branchID, Branch.bAccountID>))]
		[PXDefault]
		public virtual int? BAccountID
		{
			get;
			set;
		}
		public abstract class bAccountID : BqlInt.Field<bAccountID> { }
		#endregion
		#region AddressID
		[PXDBInt]
		[PXDBChildIdentity(typeof(Address.addressID))]
		public virtual Int32? AddressID { get; set; }
		public abstract class addressID : BqlInt.Field<addressID> { }
		#endregion
		#region ContactID
		[PXDBInt]
		[PXDBChildIdentity(typeof(Contact.contactID))]
		public virtual Int32? ContactID { get; set; }
		public abstract class contactID : BqlInt.Field<contactID> { }
		#endregion
		#region BuildingID
		[PXDBInt]
		[PXUIField(DisplayName = "Building ID")]
		[PXForeignReference(typeof(FK.Building))]
		[PXSelector(
			typeof(SearchFor<INSiteBuilding.buildingID>.Where<INSiteBuilding.branchID.IsEqual<branchID.FromCurrent>>),
			SubstituteKey = typeof(INSiteBuilding.buildingCD),
			DescriptionField = typeof(INSiteBuilding.descr))]
		public virtual Int32? BuildingID { get; set; }
		public abstract class buildingID : BqlInt.Field<buildingID> { }
		#endregion
		#region NoteID
		[PXNote(DescriptionField = typeof(siteCD),
			Selector = typeof(siteCD),
			FieldList = new[] { typeof(siteCD), typeof(descr), typeof(replenishmentClassID) })]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : BqlGuid.Field<noteID> { }
		#endregion
		#region ReplenishmentClassID
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Replenishment Class", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<INReplenishmentClass.replenishmentClassID>), DescriptionField = typeof(INReplenishmentClass.descr))]
		public virtual String ReplenishmentClassID { get; set; }
		public abstract class replenishmentClassID : BqlString.Field<replenishmentClassID> { }
		#endregion
		#region AvgDefaultCost
		[PXDBString(1, IsFixed = true)]
		[PXDefault(avgDefaultCost.AverageCost, PersistingCheck = PXPersistingCheck.Nothing)]
		[avgDefaultCost.List]
		[PXUIField(DisplayName = "Avg. Default Returns Cost")]
		public virtual String AvgDefaultCost { get; set; }
		public abstract class avgDefaultCost : BqlString.Field<avgDefaultCost>
		{
			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base(
					Pair(AverageCost, Messages.AverageCost),
					Pair(LastCost, Messages.LastCost))
				{ }
			}

			public const string AverageCost = "A";
			public const string LastCost = "L";

			public class averageCost : BqlString.Constant<averageCost> { public averageCost() : base(AverageCost) { } }
			public class lastCost : BqlString.Constant<lastCost> { public lastCost() : base(LastCost) { } }
		}
		#endregion
		#region FIFODefaultCost
		[PXDBString(1, IsFixed = true)]
		[PXDefault(avgDefaultCost.AverageCost, PersistingCheck = PXPersistingCheck.Nothing)]
		[avgDefaultCost.List]
		[PXUIField(DisplayName = "FIFO Default Returns Cost")]
		public virtual String FIFODefaultCost { get; set; }
		public abstract class fIFODefaultCost : BqlString.Field<fIFODefaultCost> { }
		#endregion
		#region OverrideInvtAccSub
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = Messages.OverrideInventoryAcctSub)]
		public virtual bool? OverrideInvtAccSub { get; set; }
		public abstract class overrideInvtAccSub : BqlBool.Field<overrideInvtAccSub> { }
		#endregion
		#region Active
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Active")]
		public virtual bool? Active { get; set; }
		public abstract class active : BqlBool.Field<active> { }
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

		#region GroupMask
		[PXDBGroupMask]
		public virtual Byte[] GroupMask { get; set; }
		public abstract class groupMask : BqlByteArray.Field<groupMask> { }
		#endregion
		#region Included
		[PXBool]
		[PXUIField(DisplayName = "Included")]
		[PXUnboundDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? Included { get; set; }
		public abstract class included : BqlBool.Field<included> { }
		#endregion
		#region UseItemDefaultLocationForPicking
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Item Default Location for Picking")]
		public virtual bool? UseItemDefaultLocationForPicking { get; set; }
		public abstract class useItemDefaultLocationForPicking : BqlBool.Field<useItemDefaultLocationForPicking> { }
		#endregion

		#region DiscAcctID
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public abstract class discAcctID : BqlInt.Field<discAcctID> { }
		[PXInt]
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public virtual int? DiscAcctID
		{
			get => null;
			set { }
		}
		#endregion
		#region DiscSubID
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public abstract class discSubID : BqlInt.Field<discSubID> { }
		[PXInt]
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public virtual int? DiscSubID
		{
			get => null;
			set { }
		}
		#endregion
		#region FreightAcctID
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public abstract class freightAcctID : BqlInt.Field<freightAcctID> { }
		[PXInt]
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public virtual int? FreightAcctID
		{
			get => null;
			set { }
		}
		#endregion
		#region FreightSubID
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public abstract class freightSubID : BqlInt.Field<freightSubID> { }
		[PXInt]
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public virtual int? FreightSubID
		{
			get => null;
			set { }
		}
		#endregion
		#region MiscAcctID
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public abstract class miscAcctID : BqlInt.Field<miscAcctID> { }
		[PXInt]
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public virtual int? MiscAcctID
		{
			get => null;
			set { }
		}
		#endregion
		#region MiscSubID
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public abstract class miscSubID : BqlInt.Field<miscSubID> { }
		[PXInt]
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public virtual int? MiscSubID
		{
			get => null;
			set { }
		}
		#endregion

		public const string Main = "MAIN";
		public class main : BqlString.Constant<main> { public main() : base(Main) { } }
	}

	#region Attributes
	public class INLocationValid
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Validate, Messages.LocValidate),
				Pair(Warn, Messages.LocWarn),
				Pair(NoValidate, Messages.LocNoValidate))
			{ }
		}

		public const string Validate = "V";
		public const string Warn = "W";
		public const string NoValidate = "N";

		public class validate : BqlString.Constant<validate> { public validate() : base(Validate) { } }
		public class noValidate : BqlString.Constant<noValidate> { public noValidate() : base(NoValidate) { } }
		public class warn : BqlString.Constant<warn> { public warn() : base(Warn) { } }
	}
	#endregion

	public sealed class UserPreferenceExt : PXCacheExtension<PX.SM.UserPreferences>
	{
		#region DefaultSite
		[Site(DisplayName = "Default Warehouse")]
		[PXForeignReference(typeof(Field<defaultSite>.IsRelatedTo<INSite.siteID>))]
		public int? DefaultSite { get; set; }
		public abstract class defaultSite : BqlInt.Field<defaultSite> { }
		#endregion

		public static int? GetDefaultSite(PXGraph graph)
		{
			PX.SM.UserPreferences userPreferences = SelectFrom<PX.SM.UserPreferences>.Where<PX.SM.UserPreferences.userID.IsEqual<AccessInfo.userID.FromCurrent>>.View.Select(graph);
			UserPreferenceExt preferencesExt = userPreferences?.GetExtension<PX.SM.UserPreferences, UserPreferenceExt>();
			return preferencesExt?.DefaultSite;
		}
	}
}