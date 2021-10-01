using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PO;
using System;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.POForSalesOrderDocument)]
	public partial class POForSalesOrderDocument : IBqlTable
	{
		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Switch<Case<Where<excluded, Equal<True>>, True>, Current<selected>>))]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected { get; set; }
		#endregion

		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[POVendor(DisplayName = "Selling Company", Visibility = PXUIVisibility.SelectorVisible, DescriptionField = typeof(Vendor.acctName), CacheGlobal = true, Filterable = true)]
		public virtual Int32? VendorID { get; set; }
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[GL.Branch(DisplayName = "Purchasing Company")]
		public virtual Int32? BranchID { get; set; }
		#endregion

		#region DocType
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		[PXString(2, IsFixed = true, IsKey = true)]
		[PODocType.List()]
		[PXUIField(DisplayName = "Document Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true)]
		[PX.Data.EP.PXFieldDescription]
		public virtual String DocType { get; set; }
		#endregion

		#region DocNbr
		public abstract class docNbr : PX.Data.BQL.BqlString.Field<docNbr> { }
		[PXString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Document Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String DocNbr { get; set; }
		#endregion

		#region DocDate
		public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
		[PXDate()]
		[PXUIField(DisplayName = "Document Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? DocDate { get; set; }
		#endregion

		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[FinPeriodSelector()]
		[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
		public virtual String FinPeriodID { get; set; }
		#endregion

		#region ExpectedDate
		public abstract class expectedDate : PX.Data.BQL.BqlDateTime.Field<expectedDate> { }
		[PXDate()]
		[PXUIField(DisplayName = "Promised Date")]
		public virtual DateTime? ExpectedDate { get; set; }
		#endregion

		#region CuryDocTotal
		public abstract class curyDocTotal : PX.Data.BQL.BqlDecimal.Field<curyDocTotal> { }
		[PXDecimal()]
		[PXUIField(DisplayName = "Total Amount", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? CuryDocTotal { get; set; }
		#endregion

		#region CuryDiscTot
		public abstract class curyDiscTot : PX.Data.BQL.BqlDecimal.Field<curyDiscTot> { }
		[PXDecimal()]
		[PXUIField(DisplayName = "Discount Total", Visible = false)]
		public virtual Decimal? CuryDiscTot { get; set; }
		#endregion

		#region CuryTaxTotal
		public abstract class curyTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyTaxTotal> { }
		[PXDecimal()]
		[PXUIField(DisplayName = "Tax Total", Visible = false)]
		public virtual Decimal? CuryTaxTotal { get; set; }
		#endregion

		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
		[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String CuryID { get; set; }
		#endregion

		#region DocQty
		public abstract class docQty : PX.Data.BQL.BqlDecimal.Field<docQty> { }
		[PXQuantity()]
		[PXUIField(DisplayName = "Total Qty.")]
		public virtual Decimal? DocQty { get; set; }
		#endregion

		#region EmployeeID
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }
		[PXDBInt()]
		[PXSubordinateSelector]
		[PXUIField(DisplayName = "Purchase Order Owner", Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
		public virtual Int32? EmployeeID { get; set; }
		#endregion
		
		#region WorkgroupID
		public abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
		[PXDBInt]
		[PX.TM.PXCompanyTreeSelector]
		[PXUIField(DisplayName = "Workgroup", Visibility = PXUIVisibility.Visible, Visible = false)]
		public virtual int? WorkgroupID { get; set; }
		#endregion

		#region OwnerID
		public abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
		[PX.TM.Owner(typeof(POForSalesOrderDocument.workgroupID), Visibility = PXUIVisibility.SelectorVisible, DisplayName = "Purchase Return Owner", Visible = false)]
		public virtual int? OwnerID { get; set; }
		#endregion

		#region DocDesc
		public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }
		[PXDBString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
		public virtual String DocDesc { get; set; }
		#endregion

		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		[PXString(2)]
		public virtual String OrderType { get; set; }
		#endregion

		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Related Order Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String OrderNbr { get; set; }
		#endregion

		#region Excluded
		public abstract class excluded : PX.Data.BQL.BqlBool.Field<excluded> { }
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Excluded")]
		public virtual bool? Excluded { get; set; }
		#endregion
	}

	public class PODocType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				new[]
				{
					Pair(RegularOrder, PO.Messages.RegularOrder),
					Pair(DropShip, PO.Messages.DropShip),
					Pair(POReturn, PO.Messages.PurchaseReturn)
				})
			{ }
		}

		public const string RegularOrder = POOrderType.RegularOrder;
		public const string DropShip = POOrderType.DropShip;
		public const string POReturn = POReceiptType.POReturn;
	}
}
