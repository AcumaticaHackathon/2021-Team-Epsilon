using System;
using System.Collections.Generic;
using System.Collections;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CS;
using PX.Objects.Extensions.PaymentTransaction;
using PX.Data.Update.ExchangeService;
using System.Linq;

namespace PX.Objects.AR
{
	[PX.Objects.GL.TableAndChartDashboardType]
	[Serializable]
	public class FailedCCPaymentEnq : PXGraph<FailedCCPaymentEnq>
	{
		#region Internal Types
		[Serializable]
		public partial class CCPaymentFilter : IBqlTable
		{
			#region BeginDate
			public abstract class beginDate : PX.Data.BQL.BqlDateTime.Field<beginDate> { }

			protected DateTime? _BeginDate;

			[PXDBDate()]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "Start Date")]
			public virtual DateTime? BeginDate
			{
				get
				{
					return this._BeginDate;
				}
				set
				{
					this._BeginDate = value;
				}
			}
			#endregion
			#region EndDate
			public abstract class endDate : PX.Data.BQL.BqlDateTime.Field<endDate> { }

			protected DateTime? _EndDate;
			[PXDBDate()]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "End Date")]
			public virtual DateTime? EndDate
			{
				get
				{
					return this._EndDate;
				}
				set
				{
					this._EndDate = value;
				}
			}
			#endregion
			#region CustomerClassID
			public abstract class customerClassID : PX.Data.BQL.BqlString.Field<customerClassID> { }
			protected String _CustomerClassID;
			[PXDBString(10, IsUnicode = true)]
			[PXSelector(typeof(CustomerClass.customerClassID), DescriptionField = typeof(CustomerClass.descr), CacheGlobal = true)]
			[PXUIField(DisplayName = "Customer Class")]
			public virtual String CustomerClassID
			{
				get
				{
					return this._CustomerClassID;
				}
				set
				{
					this._CustomerClassID = value;
				}
			}
			#endregion
			#region ProcessingCenterID
			public abstract class processingCenterID : PX.Data.BQL.BqlString.Field<processingCenterID> { }
			protected String _ProcessingCenterID;
			[PXDBString(10, IsUnicode = true)]
			[PXSelector(typeof(Search<CCProcessingCenter.processingCenterID>), DescriptionField = typeof(CCProcessingCenter.name))]
			[PXUIField(DisplayName = "Proc. Center ID", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual String ProcessingCenterID
			{
				get
				{
					return this._ProcessingCenterID;
				}
				set
				{
					this._ProcessingCenterID = value;
				}
			}
			#endregion
			#region CustomerID
			public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
			protected Int32? _CustomerID;
			[Customer(DescriptionField = typeof(Customer.acctName))]
			public virtual Int32? CustomerID
			{
				get
				{
					return this._CustomerID;
				}
				set
				{
					this._CustomerID = value;
				}
			}
			#endregion
			#region DisplayType
			public abstract class displayType : PX.Data.BQL.BqlString.Field<displayType> { }
			protected String _DisplayType;
			[PXDBString(3, IsFixed = true, IsUnicode = false)]
			[DisplayTypes.List()]
			[PXDefault(DisplayTypes.All)]
			[PXUIField(DisplayName = "Display Transactions")]
			public virtual String DisplayType
			{
				get
				{
					return this._DisplayType;
				}
				set
				{
					this._DisplayType = value;
				}
			}
			#endregion


		}
		private static class DisplayTypes
		{
			public const string All = "ALL";
			public const string Failed = "FLD";

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base(
					new string[] { All, Failed },
					new string[] { Messages.AllTransactions, Messages.FailedOnlyTransactions })
				{; }
			}
		}

		[Serializable]
		public partial class CCProcTranH : CCProcTran
		{
			#region TranNbr
			public new abstract class tranNbr : PX.Data.BQL.BqlInt.Field<tranNbr> { }
			#endregion
			#region PMInstanceID
			public new abstract class pMInstanceID : PX.Data.BQL.BqlInt.Field<pMInstanceID> { }
			#endregion
			#region ProcessingCenterID
			public new abstract class processingCenterID : PX.Data.BQL.BqlString.Field<processingCenterID> { }
			#endregion
			#region DocType
			public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
			
			#endregion
			#region RefNbr
			public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
			#endregion
		}
		#endregion

		#region CTor + public Member Decalaration
		public PXFilter<CCPaymentFilter> Filter;
		public PXCancel<CCPaymentFilter> Cancel;
		[PXFilterable]
		public PXSelectOrderBy<CCProcTran,OrderBy<Desc<CCProcTran.refNbr>>> PaymentTrans;
		public PXSelectJoin<CCProcTran,
					InnerJoin<CustomerPaymentMethod, On<CustomerPaymentMethod.pMInstanceID, Equal<CCProcTran.pMInstanceID>>,
					LeftJoin<ARPayment, On<CCProcTran.refNbr, Equal<ARPayment.refNbr>,
						And<CCProcTran.docType, Equal<ARPayment.docType>>>,
					InnerJoin<Customer, On<Customer.bAccountID, Equal<CustomerPaymentMethod.bAccountID>>>>>,
					Where<CCProcTran.startTime, GreaterEqual<Required<CCPaymentFilter.beginDate>>,
						And<CCProcTran.startTime, LessEqual<Required<CCPaymentFilter.endDate>>>>> CpmExists;
		public PXSelectJoin<CCProcTran,
					LeftJoin<CustomerPaymentMethod, On<CustomerPaymentMethod.pMInstanceID, Equal<CCProcTran.pMInstanceID>>,
					InnerJoin<ARPayment, On<CCProcTran.refNbr, Equal<ARPayment.refNbr>,
						And<CCProcTran.docType, Equal<ARPayment.docType>>>,
					InnerJoin<Customer, On<Customer.bAccountID, Equal<ARPayment.customerID>>>>>,
					Where<CCProcTran.startTime, GreaterEqual<Required<CCPaymentFilter.beginDate>>,
						And<CCProcTran.startTime, LessEqual<Required<CCPaymentFilter.endDate>>,
						And<CCProcTran.pMInstanceID, IsNull>>>> CpmNotExists;

		public PXAction<CCPaymentFilter> ViewDocument;
		public PXAction<CCPaymentFilter> ViewCustomer;
		public PXAction<CCPaymentFilter> ViewPaymentMethod;
		public PXAction<CCPaymentFilter> ViewExternalTransaction;

		public FailedCCPaymentEnq()
		{
			this.PaymentTrans.Cache.AllowInsert = false;
			this.PaymentTrans.Cache.AllowUpdate = false;
			this.PaymentTrans.Cache.AllowDelete = false;
		}

		#region Actions

		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewDocument(PXAdapter adapter)
		{
			CCProcTran tran = this.PaymentTrans.Current;
			if (tran != null)
			{
				PXGraph target = CCTransactionsHistoryEnq.FindSourceDocumentGraph(tran.DocType, tran.RefNbr, tran.OrigDocType, tran.OrigRefNbr);
				if (target != null)
					throw new PXRedirectRequiredException(target, true, Messages.ViewDocument) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}
			return Filter.Select();
		}

		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewCustomer(PXAdapter adapter)
		{
			if (this.PaymentTrans.Current != null)
			{
				var row = this.PaymentTrans.Current;
				ARPayment arPayment = PXSelect<ARPayment, Where<ARPayment.docType, Equal<Required<ARPayment.docType>>, 
					And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>.Select(this, row.DocType, row.RefNbr);

				if (arPayment != null)
				{
					CustomerMaint graph = PXGraph.CreateInstance<CustomerMaint>();
					graph.BAccount.Current = graph.BAccount.Search<Customer.bAccountID>(arPayment.CustomerID);
					
					if (graph.BAccount.Current != null)
					{
						throw new PXRedirectRequiredException(graph, true, Messages.ViewCustomer) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
					}
				}
			}
			return adapter.Get();
		}

		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewPaymentMethod(PXAdapter adapter)
		{
			if (this.PaymentTrans.Current != null)
			{
				CCProcTran row = this.PaymentTrans.Current;
				CustomerPaymentMethod pmInstance = PXSelect<CustomerPaymentMethod, 
												   Where<CustomerPaymentMethod.pMInstanceID, Equal<Required<CustomerPaymentMethod.pMInstanceID>>>>.Select(this, row.PMInstanceID);
	
				if (pmInstance != null)
				{
					CustomerPaymentMethodMaint graph = PXGraph.CreateInstance<CustomerPaymentMethodMaint>();
					graph.CustomerPaymentMethod.Current = graph.CustomerPaymentMethod.Search<CustomerPaymentMethod.pMInstanceID>(pmInstance.PMInstanceID, pmInstance.BAccountID);
					if (graph.CustomerPaymentMethod.Current != null)
					{
						throw new PXRedirectRequiredException(graph, true, Messages.ViewPaymentMethod) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
					}
				}
				else
				{
					ARPayment arPayment = PXSelect<ARPayment, Where<ARPayment.docType, Equal<Required<ARPayment.docType>>, 
															  And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>.Select(this, row.DocType, row.RefNbr);

					PaymentMethod paymentMethod = PXSelect<PaymentMethod, 
												  Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>.Select(this, arPayment.PaymentMethodID);
					PaymentMethodMaint graph = PXGraph.CreateInstance<PaymentMethodMaint>();
					graph.PaymentMethod.Current = graph.PaymentMethod.Search<PaymentMethod.paymentMethodID>(paymentMethod.PaymentMethodID);
					if (graph.PaymentMethod.Current != null)
					{
						throw new PXRedirectRequiredException(graph, true, Messages.ViewPaymentMethod) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
					}
				}
			}
			return Filter.Select();
		}

		[PXUIField(DisplayName = Messages.ViewExternalTransaction, Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewExternalTransaction(PXAdapter adapter)
		{
			if (this.PaymentTrans.Current != null)
			{
				CCProcTran row = this.PaymentTrans.Current;
				ExternalTransactionMaint graph = PXGraph.CreateInstance<ExternalTransactionMaint>();
				graph.CurrentTransaction.Current = graph.CurrentTransaction.Search<ExternalTransaction.transactionID>(row.TransactionID);
				if (graph.CurrentTransaction.Current != null)
				{
					throw new PXRedirectRequiredException(graph, true, Messages.ViewExternalTransaction) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
			}
			return Filter.Select();
		}
		#endregion
		#endregion

		#region Delegates
		public virtual IEnumerable paymentTrans()
		{
			PXDelegateResult delegateResult = new PXDelegateResult() { IsResultSorted = false };

			CCPaymentFilter filter = this.Filter.Current;
			if (filter != null)
			{
				ApplyFilters(CpmExists);
				ApplyFilters(CpmNotExists);
	
				if (filter.EndDate != null)
				{
					var result = CpmExists.Select(filter.BeginDate, filter.EndDate.Value.AddDays(1));
					result.AddRange(CpmNotExists.Select(filter.BeginDate, filter.EndDate.Value.AddDays(1)));

					foreach (PXResult<CCProcTran, CustomerPaymentMethod, ARPayment, Customer> it in result)
					{
						ARPayment doc = it;
						CCProcTran ccTran = it;
						if (ccTran.TranType == CCTranTypeCode.Credit || ccTran.TranType == CCTranTypeCode.VoidTran)
						{
							ARPayment voiding = PXSelect<ARPayment, Where<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>,
								And<ARPayment.docType, Equal<ARDocType.voidPayment>>>>.Select(this, doc.RefNbr);
							if (voiding != null)
							{
								doc.DocType = ARDocType.VoidPayment;
							}
						}

						delegateResult.Add(it);
					}
				}
			}
			return delegateResult;
		}

		#endregion

		#region Filter Event Handlers

		protected virtual void CCPaymentFilter_BeginDate_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CCPaymentFilter row = (CCPaymentFilter)e.Row;
			if (row.BeginDate.HasValue && row.EndDate.HasValue && row.EndDate.Value < row.BeginDate.Value)
			{
				row.EndDate = row.BeginDate;
			}
		}

		protected virtual void CCPaymentFilter_EndDate_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CCPaymentFilter row = (CCPaymentFilter)e.Row;
			if (row.BeginDate.HasValue && row.EndDate.HasValue && row.EndDate.Value < row.BeginDate.Value)
			{
				row.BeginDate = row.EndDate;
			}
		}

		#endregion

		private void ApplyFilters(PXSelectBase<CCProcTran> query)
		{
			CCPaymentFilter filter = this.Filter.Current;
			if (!string.IsNullOrEmpty(filter.ProcessingCenterID))
			{
				query.WhereAnd<Where<CCProcTran.processingCenterID, Equal<Current<CCPaymentFilter.processingCenterID>>>>();
			}

			if (!string.IsNullOrEmpty(filter.CustomerClassID))
			{
				query.WhereAnd<Where<Customer.customerClassID, Equal<Current<CCPaymentFilter.customerClassID>>>>();
			}

			if (filter.CustomerID.HasValue)
			{
				query.WhereAnd<Where<Customer.bAccountID, Equal<Current<CCPaymentFilter.customerID>>>>();
			}

			if (filter.DisplayType == DisplayTypes.Failed)
			{
				query.Join<LeftJoin<CCProcTranH, On<CCProcTranH.docType, Equal<CCProcTran.docType>,
										And<CCProcTranH.refNbr, Equal<CCProcTran.refNbr>,
										And<CCProcTranH.tranNbr, Greater<CCProcTran.tranNbr>>>>>>();
				query.WhereAnd<Where<CCProcTranH.tranNbr, IsNull,
									And<Where<CCProcTran.tranStatus, NotEqual<CCTranStatusCode.approved>,
										Or<CCProcTran.tranStatus, IsNull>>>>>();
			}
		}
	}
}
