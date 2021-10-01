using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using System;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.POForSalesOrderFilter)]
	public partial class POForSalesOrderFilter : IBqlTable
	{
		#region PODocType
		public abstract class pODocType : PX.Data.BQL.BqlString.Field<pODocType> { }
		[PXDBString(2, IsFixed = true)]
		[PXDefault(POCrossCompanyDocType.PurchaseOrders)]
		[POCrossCompanyDocType.List()]
		[PXUIField(DisplayName = "Purchase Doc. Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true)]
		[PX.Data.EP.PXFieldDescription]
		public virtual String PODocType { get; set; }
		#endregion

		#region DocDate
		public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
		[PXDate()]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? DocDate { get; set; }
		#endregion

		#region IntercompanyOrderType
		public abstract class intercompanyOrderType : Data.BQL.BqlString.Field<intercompanyOrderType> { }
		[PXDBString(2, IsFixed = true, InputMask = ">aa")]
		[PXUIField(DisplayName = "Sales Order Type", Required = false)]
		[PXDefault(typeof(Search2<SOOrderType.orderType, InnerJoin<SOSetup,
			On<Where2<Where<SOSetup.dfltIntercompanyOrderType, Equal<SOOrderType.orderType>, And<Current<pODocType>, Equal<POCrossCompanyDocType.purchaseOrder>>>,
				Or<Where<SOSetup.dfltIntercompanyRMAType, Equal<SOOrderType.orderType>, And<Current<pODocType>, Equal<POCrossCompanyDocType.purchaseReturn>>>>>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		//TODO: review condition for returns
		[PXSelector(typeof(Search2<SOOrderType.orderType,
			InnerJoin<SOOrderTypeOperation,
				On<SOOrderTypeOperation.orderType, Equal<SOOrderType.orderType>,
				And<SOOrderTypeOperation.operation, Equal<SOOrderType.defaultOperation>,
				And<SOOrderTypeOperation.iNDocType, NotEqual<IN.INTranType.transfer>>>>>,
			Where2<Where<Current<pODocType>, Equal<POCrossCompanyDocType.purchaseOrder>, And<SOOrderType.behavior, In3<SOBehavior.sO, SOBehavior.iN>>>,
				Or<Where<Current<pODocType>, Equal<POCrossCompanyDocType.purchaseReturn>, And<SOOrderType.behavior, In3<SOBehavior.rM, SOBehavior.cM>>>>>>),
			DescriptionField = typeof(SOOrderType.descr))]
		[PXRestrictor(typeof(Where<SOOrderType.active, Equal<True>>), Messages.OrderTypeInactive, typeof(SOOrderType.orderType))]
		public virtual string IntercompanyOrderType { get; set; }
		#endregion

		#region SellingCompany 
		public abstract class sellingCompany : PX.Data.BQL.BqlInt.Field<sellingCompany> { }
		[VendorActive(DisplayName = Messages.SellingCompany, Visibility = PXUIVisibility.SelectorVisible, Required = false, DescriptionField = typeof(Vendor.acctName), Filterable = true)]
		[PXRestrictor(typeof(Where<Vendor.isBranch, Equal<True>>), Messages.VendorIsNotBranch, typeof(Vendor.acctCD))]
		public virtual int? SellingCompany { get; set; }
		#endregion

		#region PurchasingCompany
		public abstract class purchasingCompany : PX.Data.BQL.BqlInt.Field<purchasingCompany> { }
		[CustomerActive(DisplayName = Messages.PurchasingCompany, Visibility = PXUIVisibility.SelectorVisible, Required = false, DescriptionField = typeof(Customer.acctName), Filterable = true)]
		[PXRestrictor(typeof(Where<Customer.isBranch, Equal<True>>), Messages.CustomerIsNotBranch, typeof(Customer.acctCD))]
		public virtual int? PurchasingCompany { get; set; }
		#endregion

		#region CopyProjectDetails
		public abstract class copyProjectDetails : PX.Data.BQL.BqlBool.Field<copyProjectDetails> { }
		[PXBool()]
		[PXUIField(DisplayName = "Copy Project Details to Generated Sales Orders", Visibility = PXUIVisibility.Visible)]
		[PXDefault(false)]
		public virtual Boolean? CopyProjectDetails { get; set; }
		#endregion
	}

	public class POCrossCompanyDocType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				new[]
				{
					Pair(PurchaseOrders, Messages.PurchaseOrders),
					Pair(PurchaseReturns, Messages.PurchaseReturns),
				})
			{
			}
		}

		public const string PurchaseOrders = "PO";
		public const string PurchaseReturns = "PR";

		public class purchaseOrder : PX.Data.BQL.BqlString.Constant<purchaseOrder>
		{
			public purchaseOrder() : base(PurchaseOrders) { }
		}

		public class purchaseReturn : PX.Data.BQL.BqlString.Constant<purchaseReturn>
		{
			public purchaseReturn() : base(PurchaseReturns) { }
		}
	}
}
