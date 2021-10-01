using System;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Data.ReferentialIntegrity.Attributes;

using PX.Objects.GL;
using PX.Objects.CS;
using PX.Objects.CM;
using PX.Objects.SO;
using PX.Objects.PO;
using PX.Objects.Common.Attributes;

namespace PX.Objects.IN
{
	[PXPrimaryGraph(
		new Type[] {
			typeof(INReceiptEntry),
			typeof(INIssueEntry),
			typeof(INTransferEntry),
			typeof(INAdjustmentEntry),
			typeof(KitAssemblyEntry),
			typeof(KitAssemblyEntry)},
		new Type[] {
			typeof(Where<docType.IsEqual<INDocType.receipt>>),
			typeof(Where<docType.IsEqual<INDocType.issue>>),
			typeof(Where<docType.IsEqual<INDocType.transfer>>),
			typeof(Where<docType.IsEqual<INDocType.adjustment>>),
			typeof(SelectFrom<INKitRegister>.Where<INKitRegister.docType.IsEqual<INDocType.production>.And<INKitRegister.refNbr.IsEqual<refNbr.FromCurrent>>>),
			typeof(SelectFrom<INKitRegister>.Where<INKitRegister.docType.IsEqual<INDocType.disassembly>.And<INKitRegister.refNbr.IsEqual<refNbr.FromCurrent>>>),
		})]
	[INRegisterCacheName(Messages.Register)]
	public class INRegister : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<INRegister>.By<docType, refNbr>
		{
			public static INRegister Find(PXGraph graph, string docType, string refNbr) => FindBy(graph, docType, refNbr);
		}
		public static class FK
		{
			public class Site : INSite.PK.ForeignKeyOf<INRegister>.By<siteID> { }
			public class ToSite : INSite.PK.ForeignKeyOf<INRegister>.By<toSiteID> { }
			public class Branch : GL.Branch.PK.ForeignKeyOf<INRegister>.By<branchID> { }
			public class KitInventoryItem : InventoryItem.PK.ForeignKeyOf<INRegister>.By<kitInventoryID> { }
			public class KitTran : INTran.PK.ForeignKeyOf<INRegister>.By<docType, refNbr, kitLineNbr> { }
			public class KitSpecification : INKitSpecHdr.PK.ForeignKeyOf<INRegister>.By<kitInventoryID, kitRevisionID> { }
			public class SOOrderType : SO.SOOrderType.PK.ForeignKeyOf<INRegister>.By<sOOrderType> { }
			public class SOOrder : SO.SOOrder.PK.ForeignKeyOf<INRegister>.By<sOOrderType, sOOrderNbr> { }
			public class SOShipment : SO.SOShipment.UK.ForeignKeyOf<INRegister>.By<sOShipmentType, sOShipmentNbr> { }
			public class POReceipt : PO.POReceipt.UK.ForeignKeyOf<INRegister>.By<pOReceiptType, pOReceiptNbr> { }
			public class PIHeader : INPIHeader.PK.ForeignKeyOf<INRegister>.By<pIID> { }
			//todo public class FinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INRegister>.By<finPeriodID> { }
			//todo public class MasterFinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INRegister>.By<tranPeriodID> { }
		}
		#endregion
		#region Events
		public class Events : PXEntityEvent<INRegister>.Container<Events>
		{
			public PXEntityEvent<INRegister> DocumentReleased;
		}
		#endregion

		#region Selected
		[PXBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected { get; set; } = false;
		public abstract class selected : BqlBool.Field<selected> { }
		#endregion

		#region BranchID
		[Branch]
		public virtual Int32? BranchID { get; set; }
		public abstract class branchID : BqlInt.Field<branchID> { }
		#endregion
		#region DocType
		[PXDBString(1, IsKey = true, IsFixed = true)]
		[PXDefault]
		[INDocType.List]
		[PXUIField(DisplayName = docType.DisplayName, Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String DocType { get; set; }
		public abstract class docType : BqlString.Field<docType>
		{
			public const string DisplayName = "Document Type";
		}
		#endregion
		#region RefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXDefault]
		[PXUIField(DisplayName = refNbr.DisplayName, Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<refNbr, Where<docType.IsEqual<docType.AsOptional>>, OrderBy<Desc<refNbr>>>), Filterable = true)]
		[INDocType.Numbering]
		[PX.Data.EP.PXFieldDescription]
		public virtual String RefNbr { get; set; }
		public abstract class refNbr : BqlString.Field<refNbr>
		{
			public const string DisplayName = "Reference Nbr.";
		}
		#endregion

		#region OrigModule
		[PXDBString(2, IsFixed = true)]
		[PXDefault(BatchModule.IN)]
		[PXUIField(DisplayName = "Source", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[origModule.List]
		public virtual String OrigModule { get; set; }
		public abstract class origModule : BqlString.Field<origModule>
		{
			public const string PI = "PI";

			public class List : PXStringListAttribute
			{
				public List() : base(
					Pair(BatchModule.SO, GL.Messages.ModuleSO),
					Pair(BatchModule.PO, GL.Messages.ModulePO),
					Pair(BatchModule.IN, GL.Messages.ModuleIN),
					Pair(PI, Messages.ModulePI),
					Pair(BatchModule.AP, GL.Messages.ModuleAP))
				{ }
			}
		}
		#endregion
		#region OrigRefNbr
		[PXDBString(15, IsUnicode = true)]
		public virtual String OrigRefNbr { get; set; }
		public abstract class origRefNbr : BqlString.Field<origRefNbr> { }
		#endregion
		#region SrcDocType
		/// <summary>
		/// The field is used for consolidation by source document (Shipment or Invoice for direct stock item lines) of IN Issues created from one Invoice
		/// </summary>
		[PXString(3)]
		public virtual string SrcDocType { get; set; }
		public abstract class srcDocType : BqlString.Field<srcDocType> { }
		#endregion
		#region SrcRefNbr
		/// <summary>
		/// The field is used for consolidation by source document (Shipment or Invoice for direct stock item lines) of IN Issues created from one Invoice
		/// </summary>
		[PXString(15, IsUnicode = true)]
		public virtual string SrcRefNbr { get; set; }
		public abstract class srcRefNbr : BqlString.Field<srcRefNbr> { }
		#endregion
		#region SiteID
		[Site(DisplayName = "Warehouse ID", DescriptionField = typeof(INSite.descr))]
		[PXForeignReference(typeof(FK.Site))]
		public virtual Int32? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region ToSiteID
		[ToSite(DisplayName = "To Warehouse ID", DescriptionField = typeof(INSite.descr))]
		[PXForeignReference(typeof(FK.ToSite))]
		public virtual Int32? ToSiteID { get; set; }
		public abstract class toSiteID : BqlInt.Field<toSiteID> { }
		#endregion
		#region TransferType
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INTransferType.OneStep)]
		[INTransferType.List]
		[PXUIField(DisplayName = "Transfer Type")]
		public virtual String TransferType { get; set; }
		public abstract class transferType : BqlString.Field<transferType> { }
		#endregion
		#region TransferNbr
		/// <summary>
		/// Field used in INReceiptEntry screen.
		/// Unbound, calculated field. Filled up only on correspondent screen.
		/// </summary>
		[PXString(15, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Transfer Nbr.")]
		public virtual String TransferNbr { get; set; }
		public abstract class transferNbr : BqlString.Field<transferNbr> { }
		#endregion
		#region TranDesc
		[PXDBString(256, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String TranDesc { get; set; }
		public abstract class tranDesc : BqlString.Field<tranDesc> { }
		#endregion

		#region Released
		[PXDBBool]
		[PXDefault(false)]
		public virtual Boolean? Released { get; set; }
		public abstract class released : BqlBool.Field<released> { }
		#endregion
		#region ReleasedToVerify
		[PXDBRestrictionBool(typeof(released))]
		public virtual Boolean? ReleasedToVerify { get; set; }
		public abstract class releasedToVerify : PX.Data.BQL.BqlBool.Field<releasedToVerify> { }
		#endregion
		#region Hold
		[PXDBBool]
		[PXDefault(typeof(INSetup.holdEntry))]
		[PXUIField(DisplayName = "Hold", Enabled = false)]
		public virtual Boolean? Hold { get; set; }
		public abstract class hold : BqlBool.Field<hold> { }
		#endregion
		#region Status
		[PXDBString(1, IsFixed = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[INDocStatus.List]
		public virtual String Status { get; set; }
		public abstract class status : BqlString.Field<status> { }
		#endregion

		#region TranDate
		[PXDBDate]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? TranDate { get; set; }
		public abstract class tranDate : BqlDateTime.Field<tranDate> { }
		#endregion
		#region FinPeriodID
		[INOpenPeriod(
			sourceType: typeof(tranDate),
			branchSourceType: typeof(branchID),
			masterFinPeriodIDType: typeof(tranPeriodID),
			IsHeader = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String FinPeriodID { get; set; }
		public abstract class finPeriodID : BqlString.Field<finPeriodID> { }
		#endregion
		#region TranPeriodID
		[PeriodID]
		public virtual String TranPeriodID { get; set; }
		public abstract class tranPeriodID : BqlString.Field<tranPeriodID> { }
		#endregion
		#region LineCntr
		[PXDBInt]
		[PXDefault(0)]
		public virtual Int32? LineCntr { get; set; }
		public abstract class lineCntr : BqlInt.Field<lineCntr> { }
		#endregion

		#region TotalQty
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Qty.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? TotalQty { get; set; }
		public abstract class totalQty : BqlDecimal.Field<totalQty> { }
		#endregion
		#region TotalAmount
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Amount", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? TotalAmount { get; set; }
		public abstract class totalAmount : BqlDecimal.Field<totalAmount> { }
		#endregion
		#region TotalCost
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Cost", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? TotalCost { get; set; }
		public abstract class totalCost : BqlDecimal.Field<totalCost> { }
		#endregion

		#region ControlQty
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Control Qty.")]
		public virtual Decimal? ControlQty { get; set; }
		public abstract class controlQty : BqlDecimal.Field<controlQty> { }
		#endregion
		#region ControlAmount
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Control Amount")]
		public virtual Decimal? ControlAmount { get; set; }
		public abstract class controlAmount : BqlDecimal.Field<controlAmount> { }
		#endregion
		#region ControlCost
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Control Cost")]
		public virtual Decimal? ControlCost { get; set; }
		public abstract class controlCost : BqlDecimal.Field<controlCost> { }
		#endregion

		#region BatchNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXSelector(typeof(Search<Batch.batchNbr, Where<Batch.module.IsEqual<BatchModule.moduleIN>>>))]
		public virtual String BatchNbr { get; set; }
		public abstract class batchNbr : BqlString.Field<batchNbr> { }
		#endregion
		#region ExtRefNbr
		[PXDBString(40, IsUnicode = true)]
		[PXUIField(DisplayName = "External Ref.", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String ExtRefNbr { get; set; }
		public abstract class extRefNbr : BqlString.Field<extRefNbr> { }
		#endregion

		#region KitInventoryID
		[PXDBInt]
		[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible)]
		[PXDimensionSelector(InventoryAttribute.DimensionName, typeof(Search<InventoryItem.inventoryID>), typeof(InventoryItem.inventoryCD), DescriptionField = typeof(InventoryItem.descr))]
		public virtual Int32? KitInventoryID { get; set; }
		public abstract class kitInventoryID : BqlInt.Field<kitInventoryID> { }
		#endregion
		#region KitRevisionID
		protected String _KitRevisionID;
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Revision")]
		public virtual String KitRevisionID { get; set; }
		public abstract class kitRevisionID : BqlString.Field<kitRevisionID> { }
		#endregion
		#region KitLineNbr
		[PXDBInt]
		[PXDefault(0)]
		public virtual Int32? KitLineNbr { get; set; }
		public abstract class kitLineNbr : BqlInt.Field<kitLineNbr> { }
		#endregion

		#region SOOrderType
		[PXDBString(2, IsFixed = true)]
		[PXUIField(DisplayName = "SO Order Type", Visible = false)]
		[PXSelector(typeof(Search<SOOrderType.orderType>))]
		public virtual String SOOrderType { get; set; }
		public abstract class sOOrderType : BqlString.Field<sOOrderType> { }
		#endregion
		#region SOOrderNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "SO Order Nbr.", Visible = false, Enabled = false)]
		[PXSelector(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType.IsEqual<sOOrderType.FromCurrent>>>))]
		public virtual String SOOrderNbr { get; set; }
		public abstract class sOOrderNbr : BqlString.Field<sOOrderNbr> { }
		#endregion

		#region SOShipmentType
		[PXDBString(1, IsFixed = true)]
		public virtual String SOShipmentType { get; set; }
		public abstract class sOShipmentType : BqlString.Field<sOShipmentType> { }
		#endregion
		#region SOShipmentNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "SO Shipment Nbr.", Visible = false, Enabled = false)]
		[PXSelector(typeof(Search<SOShipment.shipmentNbr>))]
		public virtual String SOShipmentNbr { get; set; }
		public abstract class sOShipmentNbr : BqlString.Field<sOShipmentNbr> { }
		#endregion

		#region POReceiptType
		[PXDBString(2, IsFixed = true)]
		[PXUIField(DisplayName = "PO Receipt Type", Visible = false, Enabled = false)]
		public virtual String POReceiptType { get; set; }
		public abstract class pOReceiptType : BqlString.Field<pOReceiptType> { }
		#endregion
		#region POReceiptNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "PO Receipt Nbr.", Visible = false, Enabled = false)]
		[PXSelector(typeof(Search<POReceipt.receiptNbr>))]
		public virtual String POReceiptNbr { get; set; }
		public abstract class pOReceiptNbr : BqlString.Field<pOReceiptNbr> { }
		#endregion

		#region PIID
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "PI Count Reference Nbr.", IsReadOnly = true)]
		[PXSelector(typeof(Search<INPIHeader.pIID>))]
		public virtual String PIID { get; set; }
		public abstract class pIID : BqlString.Field<pIID> { }
		#endregion

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
		#region NoteID
		[PXSearchable(
			category: SM.SearchCategory.IN,
			titlePrefix: "{0}: {1}", titleFields: new Type[] { typeof(docType), typeof(refNbr) },
			fields: new Type[] { typeof(tranDesc), typeof(extRefNbr), typeof(transferNbr) },
			NumberFields = new Type[] { typeof(refNbr) },
			Line1Format = "{0}{1:d}{2}{3}{4}", Line1Fields = new Type[] { typeof(extRefNbr), typeof(tranDate), typeof(transferType), typeof(transferNbr), typeof(status) },
			Line2Format = "{0}", Line2Fields = new Type[] { typeof(tranDesc) },
			WhereConstraint = typeof(Where<docType.IsNotIn<INDocType.production, INDocType.disassembly>>)
		)]
		[PXNote(DescriptionField = typeof(INRegister.refNbr), Selector = typeof(INRegister.refNbr))]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : BqlGuid.Field<noteID> { }
		#endregion
		#region tstamp
		[PXDBTimestamp]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion

		#region IsPPVTran
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? IsPPVTran { get; set; } = false;
		public abstract class isPPVTran : BqlBool.Field<isPPVTran> { }
		#endregion
		#region IsTaxAdjustmentTran
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? IsTaxAdjustmentTran { get; set; }
		public abstract class isTaxAdjustmentTran : BqlBool.Field<isTaxAdjustmentTran> { }
		#endregion
	}

	[PXHidden, PXProjection(typeof(
		SelectFrom<INTransitLineStatus>.
		Where<INTransitLineStatus.qtyOnHand.IsGreater<Zero>>.
		AggregateTo<GroupBy<INTransitLineStatus.transferNbr>>))]
	public class INTransferInTransit : IBqlTable
	{
		#region TransferNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(INTransitLineStatus.transferNbr))]
		public virtual String TransferNbr { get; set; }
		public abstract class transferNbr : BqlString.Field<transferNbr> { }
		#endregion
		#region RefNoteID
		[PXNote(BqlField = typeof(INTransitLineStatus.refNoteID))]
		public virtual Guid? RefNoteID { get; set; }
		public abstract class refNoteID : BqlGuid.Field<refNoteID> { }
		#endregion
	}

	[PXHidden, PXProjection(typeof(
		SelectFrom<INTransitLineStatus>.
		Where<INTransitLineStatus.qtyOnHand.IsGreater<Zero>>))]
	public class INTranInTransit : IBqlTable
	{
		#region RefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(INTransitLineStatus.transferNbr))]
		public virtual String RefNbr { get; set; }
		public abstract class refNbr : BqlString.Field<refNbr> { }
		#endregion
		#region LineNbr
		[PXDBInt(IsKey = true, BqlField = typeof(INTransitLineStatus.transferLineNbr))]
		public virtual Int32? LineNbr { get; set; }
		public abstract class lineNbr : BqlInt.Field<lineNbr> { }
		#endregion
		#region OrigModule
		[PXDBString(2, IsFixed = true, BqlField = typeof(INTransitLineStatus.origModule))]
		public virtual String OrigModule { get; set; }
		public abstract class origModule : BqlString.Field<origModule> { }
		#endregion
	}

	public class INDocType
	{
		public class NumberingAttribute : AutoNumberAttribute
		{
			public NumberingAttribute() : base(
				typeof(INRegister.docType),
				typeof(INRegister.tranDate),
				Pair(Issue).To<INSetup.issueNumberingID>(),
				Pair(Receipt).To<INSetup.receiptNumberingID>(),
				Pair(Transfer).To<INSetup.receiptNumberingID>(),
				Pair(Adjustment).To<INSetup.adjustmentNumberingID>(),
				Pair(Production).To<INSetup.kitAssemblyNumberingID>(),
				Pair(Change).To<INSetup.kitAssemblyNumberingID>(),
				Pair(Disassembly).To<INSetup.kitAssemblyNumberingID>()) { }
		}

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Issue, Messages.Issue),
				Pair(Receipt, Messages.Receipt),
				Pair(Transfer, Messages.Transfer),
				Pair(Adjustment, Messages.Adjustment),
				Pair(Production, Messages.Production),
				Pair(Disassembly, Messages.Disassembly))
			{ }
		}

		public class KitListAttribute : PXStringListAttribute
		{
			public KitListAttribute() : base(
				Pair(Production, Messages.Production),
				Pair(Disassembly, Messages.Disassembly))
			{ }
		}

		public class SOListAttribute : PXStringListAttribute
		{
			public SOListAttribute() : base(
				Pair(Issue, Messages.Issue),
				Pair(Receipt, Messages.Receipt),
				Pair(Transfer, Messages.Transfer),
				Pair(Adjustment, Messages.Adjustment),
				Pair(Production, Messages.Production),
				Pair(Disassembly, Messages.Disassembly),
				Pair(DropShip, Messages.DropShip))
			{ }
		}

		public const string Undefined = "0";
		public const string Issue = "I";
		public const string Receipt = "R";
		public const string Transfer = "T";
		public const string Adjustment = "A";
		public const string Production = "P";
		public const string Change = "C";
		public const string Disassembly = "D";
		public const string DropShip = "H";
		public const string Invoice = "N";

		public class undefined : BqlString.Constant<undefined> { public undefined() : base(Undefined) { } }
		public class issue : BqlString.Constant<issue> { public issue() : base(Issue) { } }
		public class receipt : BqlString.Constant<receipt> { public receipt() : base(Receipt) { } }
		public class transfer : BqlString.Constant<transfer> { public transfer() : base(Transfer) { } }
		public class adjustment : BqlString.Constant<adjustment> { public adjustment() : base(Adjustment) { } }
		public class production : BqlString.Constant<production> { public production() : base(Production) { } }
		public class change : BqlString.Constant<change> { public change() : base(Change) { } }
		public class disassembly : BqlString.Constant<disassembly> { public disassembly() : base(Disassembly) { } }
		public class dropShip : BqlString.Constant<dropShip> { public dropShip() : base(DropShip) { } }
	}

	public class INDocStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Hold, Messages.Hold),
				Pair(Balanced, Messages.Balanced),
				Pair(Released, Messages.Released))
			{ }
		}

		public const string Hold = "H";
		public const string Balanced = "B";
		public const string Released = "R";

		public class hold : BqlString.Constant<hold> { public hold() : base(Hold) { } }
		public class balanced : BqlString.Constant<balanced> { public balanced() : base(Balanced) { } }
		public class released : BqlString.Constant<released> { public released() : base(Released) { } }
	}

	public class INTransferType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(OneStep, Messages.OneStep),
				Pair(TwoStep, Messages.TwoStep))
			{ }
		}

		public const string OneStep = "1";
		public const string TwoStep = "2";

		public class oneStep : BqlString.Constant<oneStep> { public oneStep() : base(OneStep) { } }
		public class twoStep : BqlString.Constant<twoStep> { public twoStep() : base(TwoStep) { } }
	}
}