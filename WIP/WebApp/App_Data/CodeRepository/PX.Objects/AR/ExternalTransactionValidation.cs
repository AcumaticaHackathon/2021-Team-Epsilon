using System;
using System.Collections;
using System.Collections.Generic;
using PX.SM;
using PX.Data;
using PX.Common;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.SO;
using PX.Objects.Common.Attributes;

namespace PX.Objects.AR
{
	public class ExternalTransactionValidation : PXGraph<ExternalTransactionValidation>
	{
		#region CTor + public Member Decalaration
		public PXFilter<ExternalTransactionFilter> Filter;
		public PXCancel<ExternalTransactionFilter> Cancel;

		public PXAction<ExternalTransactionFilter> ViewDocument;
		public PXAction<ExternalTransactionFilter> ViewOrigDocument;
		public PXAction<ExternalTransactionFilter> ViewProcessingCenter;
		public PXAction<ExternalTransactionFilter> ViewExternalTransaction;

		[PXFilterable]
		public PXFilteredProcessing<ExternalTransaction, ExternalTransactionFilter,
						Where<ExternalTransaction.refNbr, IsNotNull,
							And<ExternalTransaction.docType, In3<ARDocType.payment, ARDocType.prepayment, ARDocType.refund>,
							And2<Where<ExternalTransaction.procStatus, In3<ExtTransactionProcStatusCode.authorizeHeldForReview, ExtTransactionProcStatusCode.captureHeldForReview>,
								Or<ExternalTransaction.needSync, Equal<True>>>,
							And<Where<ExternalTransaction.processingCenterID, Equal<Current<ExternalTransactionFilter.processingCenterID>>,
								Or<Current<ExternalTransactionFilter.processingCenterID>, IsNull>>>>>>,
					OrderBy<Desc<ExternalTransaction.refNbr>>> PaymentTrans;

		[PXHidden]
		public class ARPaymentByVoidLink : ARPayment
		{
			public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
			public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		}

		public ExternalTransactionValidation()
		{
			PaymentTrans.SetProcessCaption(AR.Messages.Validate);
			PaymentTrans.SetProcessAllCaption(Messages.ValidateAll);
		}
		#endregion

		#region Action

		[PXUIField(DisplayName = Messages.ViewDocument, Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewDocument(PXAdapter adapter)
		{
			ExternalTransaction tran = this.PaymentTrans.Current;
			if (tran != null)
			{
				PXGraph target = CCTransactionsHistoryEnq.FindSourceDocumentGraph(tran.DocType, tran.RefNbr, null, null);
				if (target != null)
					throw new PXRedirectRequiredException(target, true, Messages.ViewDocument) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}
			return Filter.Select();
		}

		[PXUIField(DisplayName = Messages.ViewOrigDocument, Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewOrigDocument(PXAdapter adapter)
		{
			ExternalTransaction tran = this.PaymentTrans.Current;
			if (tran != null && !string.IsNullOrWhiteSpace(tran.OrigRefNbr))
			{
				SO.SOOrderEntry graph = PXGraph.CreateInstance<SO.SOOrderEntry>();
				graph.Document.Current = graph.Document.Search<SO.SOOrder.orderNbr>(tran.OrigRefNbr, tran.OrigDocType);

				if (graph.Document.Current != null)
					throw new PXRedirectRequiredException(graph, true, Messages.ViewOrigDocument) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}
			return Filter.Select();
		}

		[PXUIField(DisplayName = Messages.ViewProcessingCenter, Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewProcessingCenter(PXAdapter adapter)
		{
			if (this.PaymentTrans.Current != null)
			{
				ExternalTransaction row = this.PaymentTrans.Current;
				CustomerPaymentMethod pmInstance = PXSelect<CustomerPaymentMethod, Where<CustomerPaymentMethod.pMInstanceID, Equal<Required<CustomerPaymentMethod.pMInstanceID>>>>.Select(this, row.PMInstanceID);
				CCProcessingCenterMaint graph = PXGraph.CreateInstance<CCProcessingCenterMaint>();
				graph.ProcessingCenter.Current = graph.ProcessingCenter.Search<CCProcessingCenter.processingCenterID>(pmInstance.CCProcessingCenterID);
				if (graph.ProcessingCenter.Current != null)
				{
					throw new PXRedirectRequiredException(graph, true, Messages.ViewProcessingCenter) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
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
				ExternalTransaction row = this.PaymentTrans.Current;
				ExternalTransactionMaint graph = PXGraph.CreateInstance<ExternalTransactionMaint>();
				graph.CurrentTransaction.Current = row;
				if (graph.CurrentTransaction.Current != null)
				{
					throw new PXRedirectRequiredException(graph, true, Messages.ViewExternalTransaction) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
			}
			return Filter.Select();
		}
		#endregion

		#region Internal Types
		[Serializable]
		[PXHidden]
		public partial class ExternalTransactionFilter : IBqlTable
		{
			#region ProcessingCenterID
			public abstract class processingCenterID : PX.Data.BQL.BqlString.Field<processingCenterID> { }

			[PXDBString(10, IsUnicode = true)]
			[PXSelector(typeof(Search<CCProcessingCenter.processingCenterID>), DescriptionField = typeof(CCProcessingCenter.name))]
			[PXUIField(DisplayName = "Proc. Center ID", Visibility = PXUIVisibility.SelectorVisible)]
			[DeprecatedProcessing(ChckVal = DeprecatedProcessingAttribute.CheckVal.ProcessingCenterId)]
			[DisabledProcCenter(CheckFieldValue = DisabledProcCenterAttribute.CheckFieldVal.ProcessingCenterId)]
			public virtual String ProcessingCenterID { get; set; }
			#endregion
			#region DisplayType
			public abstract class displayType : PX.Data.BQL.BqlString.Field<displayType> { }

			[PXDBString(IsUnicode = false)]
			[DisplayTypes.List()]
			[PXDefault(DisplayTypes.HeldForReview)]
			[PXUIField(DisplayName = "Display Transactions")]
			public virtual String DisplayType { get; set; }
			#endregion
		}

		private static class DisplayTypes
		{
			public const string HeldForReview = "HELDFORREVIEW";

			[PXLocalizable]
			public class UI
			{
				public const string HeldForReview = "Held for Review";
			}

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base(
					new string[] { HeldForReview },
					new string[] { UI.HeldForReview })
				{; }
			}
		}
		#endregion

		#region Filter Event Handlers

		protected virtual void ExternalTransactionFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			ExternalTransactionFilter filter = e.Row as ExternalTransactionFilter;
			if (filter == null) return;

			PaymentTrans.SetProcessDelegate(delegate (List<ExternalTransaction> list)
			{
				ExternalTransactionValidation graph = CreateInstance<ExternalTransactionValidation>();
				List<IExternalTransaction> newList = new List<IExternalTransaction>();
				newList.AddRange(list);
				ValidateCCPayment(graph, newList, true);
			});
		}
		#endregion

		#region Bussines logic

		public static void ValidateCCPayment(PXGraph graph, List<IExternalTransaction> list, bool isMassProcess)
		{
			bool failed = false;
			ARCashSaleEntry arCashSaleGraph = null;
			ARPaymentEntry arPaymentGraph = null;
			SOInvoiceEntry soInvoiceGraph = null;

			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] == null)
					continue;

				if ((i % 100) == 0)
				{
					if (arCashSaleGraph != null)
						arCashSaleGraph.Clear();
					if (arPaymentGraph != null)
						arPaymentGraph.Clear();
					if (soInvoiceGraph != null)
						soInvoiceGraph.Clear();
				}

				IExternalTransaction tran = list[i];

				var resultSet = PXSelectJoin<ExternalTransaction,
					InnerJoin<Standalone.ARRegister, On<Standalone.ARRegister.refNbr, Equal<ExternalTransaction.refNbr>,
						And<Standalone.ARRegister.docType, Equal<ExternalTransaction.docType>>>,
					LeftJoin<ARPayment, On<ARPayment.refNbr, Equal<Standalone.ARRegister.refNbr>,
						And<ARPayment.docType, Equal<Standalone.ARRegister.docType>>>,
					LeftJoin<ARPaymentByVoidLink, On<ARPaymentByVoidLink.refNbr, Equal<ExternalTransaction.voidRefNbr>, 
						And<ARPaymentByVoidLink.docType, Equal<ExternalTransaction.voidDocType>>>,
					LeftJoin<ARInvoice, On<ARInvoice.refNbr, Equal<Standalone.ARRegister.refNbr>,
						And<ARInvoice.docType, Equal<Standalone.ARRegister.docType>>>,
					LeftJoin<SOInvoice, On<SOInvoice.refNbr, Equal<Standalone.ARRegister.refNbr>,
						And<SOInvoice.docType, Equal<Standalone.ARRegister.docType>>>,
					LeftJoin<Standalone.ARCashSale, On<Standalone.ARCashSale.refNbr, Equal<Standalone.ARRegister.refNbr>,
						And<Standalone.ARCashSale.docType, Equal<Standalone.ARRegister.docType>>>>>>>>>,
					Where<ExternalTransaction.transactionID, Equal<Required<ExternalTransaction.transactionID>>>>
					.SelectSingleBound(graph, null, tran.TransactionID);

				foreach (PXResult<ExternalTransaction, Standalone.ARRegister, ARPayment, ARPaymentByVoidLink, ARInvoice, SOInvoice, Standalone.ARCashSale> item in resultSet)
				{
					if (item == null)
						continue;

					try
					{
						if ((ARInvoice)item is ARInvoice arInvoice
								&& arInvoice != null && arInvoice.RefNbr != null)
						{
							if ((Standalone.ARCashSale)item is Standalone.ARCashSale arCashSale
								&& arCashSale != null && arCashSale.RefNbr != null)
							{
								arCashSaleGraph = arCashSaleGraph != null ? arCashSaleGraph : PXGraph.CreateInstance<ARCashSaleEntry>();
								ARCashSaleEntry.PaymentTransaction ext = arCashSaleGraph.GetExtension<ARCashSaleEntry.PaymentTransaction>();
								arCashSaleGraph.Document.Current = arCashSale;
								if (ext.CanValidate(arCashSaleGraph.Document.Current))
								{
									ext.validateCCPayment.Press();
								}
							}
						}
						else if ((ARPayment)item is ARPayment arPayment
									&& arPayment != null && arPayment.RefNbr != null)
						{
							arPaymentGraph = arPaymentGraph != null ? arPaymentGraph : PXGraph.CreateInstance<ARPaymentEntry>();
							ARPaymentEntry.PaymentTransaction ext = arPaymentGraph.GetExtension<ARPaymentEntry.PaymentTransaction>();
							ARPaymentByVoidLink docByVoidLink = (ARPaymentByVoidLink)item;

							if (docByVoidLink?.RefNbr != null)
							{
								arPaymentGraph.Document.Current = docByVoidLink;
							}

							if (docByVoidLink?.RefNbr != null && ext.CanValidate(arPaymentGraph.Document.Current))
							{
								ext.validateCCPayment.Press();
							}
							else
							{
								arPaymentGraph.Document.Current = arPayment;
								if (ext.CanValidate(arPaymentGraph.Document.Current))
								{
									ext.validateCCPayment.Press();
								}
							}
						}

						if (isMassProcess)
						{
							PXProcessing<ExternalTransaction>.SetInfo(i, ActionsMessages.RecordProcessed);
						}
					}
					catch (Exception e)
					{
						failed = true;

						if (isMassProcess)
						{
							PXProcessing<ExternalTransaction>.SetError(i, e);
						}
						else
						{
							throw new Common.PXMassProcessException(i, e);
						}
					}
				}
			}

			if (failed)
			{
				throw new PXOperationCompletedWithErrorException(AR.Messages.DocumentsNotValidated);
			}
		}
		#endregion

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Visible), false)]
		protected virtual void ExternalTransaction_CreateProfile_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Visible), false)]
		protected virtual void ExternalTransaction_NeedSync_CacheAttached(PXCache sender) { }
	}
}