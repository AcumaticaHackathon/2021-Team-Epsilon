using PX.Api;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class PRPrintChecks : PXGraph<PRPrintChecks>
	{
		public PXFilter<PrintChecksFilter> Filter;
		public PXCancel<PrintChecksFilter> Cancel;

		[PXFilterable]
		public PXFilteredProcessing<PRPayment, PrintChecksFilter,
			Where<PRPayment.paymentMethodID, Equal<Current<PrintChecksFilter.paymentMethodID>>,
				And<PRPayment.cashAccountID, Equal<Current<PrintChecksFilter.cashAccountID>>,
				And<PRPayment.status, Equal<PaymentStatus.pendingPayment>>>>,
			OrderBy<Asc<PRPayment.refNbr>>> PaymentList;

		public virtual IEnumerable paymentList()
		{
			if (PaymentList.Cache.IsDirty == false && PaymentList.Cache.Updated.Any_())
			{
				return PaymentList.Cache.Updated;
			}

			return null;
		}

		#region Events

		protected virtual void _(Events.RowSelected<PrintChecksFilter> e)
		{
			PrintChecksFilter filter = Filter.Current;
			if (filter != null && string.IsNullOrEmpty(filter.NextCheckNbr))
			{
				TrySetNextCheckNbr(filter);
			}

			PaymentList.SetProcessDelegate(delegate (List<PRPayment> list)
			{
				var graph = PXGraph.CreateInstance<PRPrintChecks>();
				graph.PrintPayments(list, filter);
			});

			bool isProcessEnabled = filter.PaymentMethodID != null && filter.CashAccountID != null;
			var paymentMethod = PXSelectorAttribute.Select<PrintChecksFilter.paymentMethodID>(e.Cache, e.Row) as PaymentMethod;
			var paymentMethodExt = paymentMethod?.GetExtension<PRxPaymentMethod>();
			if (paymentMethodExt?.PRCreateBatchPayment == false)
			{
				isProcessEnabled &= !string.IsNullOrWhiteSpace(filter.NextCheckNbr);
				PXUIFieldAttribute.SetWarning<PrintChecksFilter.nextCheckNbr>(e.Cache, filter, string.IsNullOrWhiteSpace(filter?.NextCheckNbr) ? Messages.RequiredCheckNumber : null);
			}

			PaymentList.SetProcessEnabled(isProcessEnabled);
			PaymentList.SetProcessAllEnabled(isProcessEnabled);
		}

		protected virtual void _(Events.FieldUpdated<PrintChecksFilter.paymentMethodID> e)
		{
			Filter.Cache.SetDefaultExt<PrintChecksFilter.cashAccountID>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PrintChecksFilter.cashAccountID> e)
		{
			var filter = e.Row as PrintChecksFilter;
			if (filter != null)
			{
				TrySetNextCheckNbr(filter);
			}
		}

		protected virtual void _(Events.RowUpdated<PrintChecksFilter> e)
		{
			if ((e.OldRow.CashAccountID == null && e.OldRow.PaymentMethodID == null) ||
				(e.OldRow.CashAccountID != e.Row.CashAccountID || e.OldRow.PaymentMethodID != e.Row.PaymentMethodID))
			{
				e.Row.SelTotal = 0m;
				e.Row.SelCount = 0;
				e.Row.NextCheckNbr = null;
				PaymentList.Cache.Clear();
				PaymentList.Cache.ClearQueryCache();
			}
		}

		protected virtual void _(Events.FieldUpdated<PRPayment.selected> e)
		{
			PrintChecksFilter filter = Filter.Current;
			var row = (PRPayment)e.Row;
			if (filter != null)
			{
				filter.SelTotal -= (bool?)e.OldValue == true ? row.NetAmount : 0m;
				filter.SelTotal += (bool?)e.NewValue == true ? row.NetAmount : 0m;

				filter.SelCount -= (bool?)e.OldValue == true ? 1 : 0;
				filter.SelCount += (bool?)e.NewValue == true ? 1 : 0;
			}
		}

		#endregion Events

		public void PrintPayments(List<PRPayment> list, PrintChecksFilter filter)
		{
			var paymentMethod = (PaymentMethod)PXSelectorAttribute.Select<PrintChecksFilter.paymentMethodID>(Filter.Cache, filter);
			var paymentMethodExt = PXCache<PaymentMethod>.GetExtension<PRxPaymentMethod>(paymentMethod);
			var payCheckGraph = PXGraph.CreateInstance<PRPayChecksAndAdjustments>();
			var paymentBatchGraph = CreateInstance<PRDirectDepositBatchEntry>();
			bool isNewBatch = filter.PaymentBatchNbr == null;
			payCheckGraph.PreparePaymentsForPrint(list);

			PRCABatch batch;
			if (isNewBatch)
			{
				batch = new PRCABatch();
				batch.OrigModule = BatchModule.PR;
				batch.CashAccountID = filter.CashAccountID;
				batch.PaymentMethodID = filter.PaymentMethodID;
				batch.CuryID = filter.CuryID;
				batch.Hold = false;
				batch = paymentBatchGraph.Document.Insert(batch);
				paymentBatchGraph.Save.Press();
			}
			else
			{
				batch = SelectFrom<PRCABatch>.Where<PRCABatch.batchNbr.IsEqual<P.AsString>>.View.Select(paymentBatchGraph, filter.PaymentBatchNbr).TopFirst;
			}
			paymentBatchGraph.Document.Current = batch;

			if (batch != null && paymentMethodExt.PRPrintChecks == true)
			{
				string nextCheckNbr = filter.NextCheckNbr;
				string firstCheckNbr = string.Empty;
				string lastCheckNbr = string.Empty;
				foreach (PRPayment payment in list)
				{
					if (string.IsNullOrWhiteSpace(nextCheckNbr))
					{
						var ex = new PXException(Messages.RequiredCheckNumber);
						PXProcessing.SetError<PRPayment>(list.IndexOf(payment), ex);
					}

					payment.ExtRefNbr = nextCheckNbr;
					payCheckGraph.Document.Update(payment);
					lastCheckNbr = nextCheckNbr;
					if (string.IsNullOrEmpty(firstCheckNbr))
					{
						firstCheckNbr = nextCheckNbr;
					}
					nextCheckNbr = AutoNumberAttribute.NextNumber(nextCheckNbr);
				}

				if (!string.IsNullOrWhiteSpace(lastCheckNbr))
				{
					PaymentMethodAccount paymentMethodAccount = payCheckGraph.PaymentMethodAccount.SelectSingle();
					paymentMethodAccount.APLastRefNbr = lastCheckNbr;
					payCheckGraph.PaymentMethodAccount.Update(paymentMethodAccount);
				}
				payCheckGraph.Persist();

				decimal batchTotal = 0;
					foreach (PRPayment payment in list)
					{
						PXProcessing<PRPayment>.SetCurrentItem(payment);
					///Set as selected for printing in <see cref="PRDirectDepositBatchEntry"/>
					payment.Selected = true;
					paymentBatchGraph.Payments.Update(payment);

							var detail = new CABatchDetail();
					detail.BatchNbr = batch.BatchNbr;
					detail.OrigRefNbr = payment.RefNbr;
					detail.OrigDocType = payment.DocType;
							detail.OrigModule = BatchModule.PR;
					paymentBatchGraph.BatchPaymentsDetails.Insert(detail);

					payment.PaymentBatchNbr = batch.BatchNbr;
					SetStatusAttribute.StatusSet(payCheckGraph.Document.Cache, payment);
					payCheckGraph.Document.Update(payment);

					batchTotal += payment.NetAmount.GetValueOrDefault();
						}

				paymentBatchGraph.Save.Press();
				payCheckGraph.Persist();
				PRCABatchUpdate.RecalculatePaymentBatchTotal(batch);

				PRPaymentBatchExportHistory hist = new PRPaymentBatchExportHistory();
				hist.PaymentBatchNbr = batch.BatchNbr;
				hist.ExportDateTime = DateTime.Now;
				hist.BatchTotal = batchTotal;
				hist.Reason = isNewBatch ? PaymentBatchPrintReason.Initial : PaymentBatchPrintReason.AddedPaycheck;
				paymentBatchGraph.ExportHistory.Insert(hist);
				paymentBatchGraph.Save.Press();

				RedirectToPrintedChecks(this, paymentMethodExt, firstCheckNbr, lastCheckNbr, filter.PaymentMethodID, filter.CashAccountID);
				return;
			}
			else if (batch != null && paymentMethodExt.PRCreateBatchPayment == true)
			{
				foreach (PRPayment payment in list)
				{
					PXProcessing<PRPayment>.SetCurrentItem(payment);
					IEnumerable<CABatchDetail> details = PRDirectDepositBatchEntry.CreatePaymentBatchDetails(this, payment.DocType, payment.RefNbr, filter.PaymentBatchNbr);
					details.ForEach(x => paymentBatchGraph.BatchPaymentsDetails.Insert(x));
					payment.PaymentBatchNbr = batch.BatchNbr;
					SetStatusAttribute.StatusSet(payCheckGraph.Document.Cache, payment);
						payCheckGraph.Document.Update(payment);
					}

				paymentBatchGraph.Save.Press();
				payCheckGraph.Persist();

				PRCABatchUpdate.RecalculatePaymentBatchTotal(batch);
				Redirect(batch);
			}
		}

		public static void RedirectToPrintedChecks(
			PXGraph graph,
			PRxPaymentMethod paymentMethodExt,
			string firstCheckNbr,
			string lastCheckNbr,
			string paymentMethodID,
			int? cashAccountID,
			PXBaseRedirectException.WindowMode windowMode = PXBaseRedirectException.WindowMode.Same)
		{
			var parameters = new Dictionary<string, string>();
			parameters[ReportParameters.CheckNbrFrom] = firstCheckNbr;
			parameters[ReportParameters.CheckNbrTo] = lastCheckNbr;
			parameters[ReportParameters.PaymentMethodID] = paymentMethodID;
			parameters[ReportParameters.CashAccountCD] = CashAccount.PK.Find(graph, cashAccountID).CashAccountCD;
			parameters[ReportParameters.PrintFlag] = ReportParameters.PrintFlagValues.Print;
			var reportRedirectionException = new PXReportRequiredException(parameters, paymentMethodExt.PRCheckReportID, windowMode, "Pay Checks");
			throw reportRedirectionException;
		}

		#region Helpers

		private static void Redirect(CABatch batch)
		{
			var ddEntryGraph = PXGraph.CreateInstance<PRDirectDepositBatchEntry>();
			PRCABatch payrollCABatch = SelectFrom<PRCABatch>.Where<PRCABatch.batchNbr.IsEqual<P.AsString>>.View.SelectSingleBound(ddEntryGraph, null, batch.BatchNbr);
			ddEntryGraph.Document.Current = payrollCABatch;
			throw new PXRedirectRequiredException(ddEntryGraph, "Redirect");
		}

		/// <summary>
		/// Try to determine what would be next check number according to info found in payment method.
		/// Inspired by PaymentRefAttribute.GetNextPaymentRef.
		/// </summary>
		public virtual void TrySetNextCheckNbr(PrintChecksFilter filter)
		{
			try
			{
				if (string.IsNullOrEmpty(filter.NextCheckNbr) && filter.CashAccountID != null && !string.IsNullOrEmpty(filter.PaymentMethodID))
				{
					var result = (PXResult<PaymentMethodAccount, PaymentMethod>)PXSelectReadonly2<PaymentMethodAccount,
						InnerJoin<PaymentMethod, On<PaymentMethod.paymentMethodID, Equal<PaymentMethodAccount.paymentMethodID>>>,
						Where<PaymentMethodAccount.cashAccountID, Equal<Required<PaymentMethodAccount.cashAccountID>>,
							And<PaymentMethodAccount.paymentMethodID, Equal<Required<PaymentMethodAccount.paymentMethodID>>,
							And<PRxPaymentMethodAccount.useForPR, Equal<True>>>>>.Select(this, filter.CashAccountID, filter.PaymentMethodID);
					PaymentMethod paymentMethod = result;
					PaymentMethodAccount paymentMethodAccount = result;
					if (paymentMethodAccount != null && paymentMethodAccount.APLastRefNbr == null)
					{
						paymentMethodAccount.APLastRefNbr = string.Empty;
						paymentMethodAccount.APLastRefNbrIsNull = true;
					}

					if (paymentMethod == null
						|| paymentMethodAccount == null
						|| paymentMethodAccount.APAutoNextNbr != true
						|| string.IsNullOrEmpty(paymentMethodAccount.APLastRefNbr))
					{
						return;
					}

					string nextNumber = AutoNumberAttribute.NextNumber(paymentMethodAccount.APLastRefNbr, 1);
					if (PXSelect<CashAccountCheck,
						Where<CashAccountCheck.accountID, Equal<Current<PaymentMethodAccount.cashAccountID>>,
							And<CashAccountCheck.paymentMethodID, Equal<Current<PaymentMethodAccount.paymentMethodID>>,
							And<CashAccountCheck.checkNbr, Equal<Required<CashAccountCheck.checkNbr>>>>>>
						.SelectSingleBound(this, new object[] { paymentMethodAccount }, nextNumber).Count == 1)
					{
						string maxNumber = PaymentRefAttribute.GetMaxCashAccountCheckNumberByPrefix(this, paymentMethodAccount.CashAccountID, paymentMethodAccount.PaymentMethodID, PaymentRefAttribute.GetAutonumberPrefix(nextNumber));
						nextNumber = string.IsNullOrEmpty(maxNumber) ? null : AutoNumberAttribute.NextNumber(maxNumber, 1);
					}

					filter.NextCheckNbr = nextNumber;
				}
			}
			catch (AutoNumberException) { }
		}

		#endregion Helpers

		public static class ReportParameters
		{
			public const string CheckNbrFrom = "CheckNbr_From";
			public const string CheckNbrTo = "CheckNbr_To";
			public const string CashAccountCD = "CashAccountCD";
			public const string PaymentMethodID = "PaymentMethodID";
			public const string PrintFlag = "PrintFlag";
			public static class PrintFlagValues
			{
				public const string Print = "PRINT";
				public const string NoPrint = "NOPRINT";
			}
		}
	}

	[PXCacheName(Messages.PrintChecksFilter)]
	public partial class PrintChecksFilter : IBqlTable
	{
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[Branch(Visible = true, Enabled = true)]
		public virtual Int32? BranchID { get; set; }
		#endregion

		#region PaymentMethodID
		public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
		[PXString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Payment Method", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		[PXSelector(typeof(Search<PaymentMethod.paymentMethodID,
			Where<PRxPaymentMethod.useForPR, Equal<True>,
				And<PaymentMethod.isActive, Equal<True>>>>))]
		[PXRestrictor(typeof(Where<PRxPaymentMethod.prPrintChecks, Equal<True>,
			Or<PRxPaymentMethod.prCreateBatchPayment, Equal<True>>>),
			AP.Messages.PaymentTypeNoPrintCheck, typeof(PaymentMethod.paymentMethodID))]
		public virtual string PaymentMethodID { get; set; }
		#endregion

		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
		[CashAccount(typeof(PrintChecksFilter.branchID), typeof(Search2<CashAccount.cashAccountID,
			InnerJoin<PaymentMethodAccount,
				On<PaymentMethodAccount.cashAccountID, Equal<CashAccount.cashAccountID>>>,
			Where2<Match<Current<AccessInfo.userName>>,
				And<CashAccount.clearingAccount, Equal<False>,
				And<PaymentMethodAccount.paymentMethodID, Equal<Current<PrintChecksFilter.paymentMethodID>>,
				And<PRxPaymentMethodAccount.useForPR, Equal<True>>>>>>), Visibility = PXUIVisibility.Visible)]
		[PXUnboundDefault(typeof(Search2<PaymentMethodAccount.cashAccountID,
			InnerJoin<CashAccount,
				On<CashAccount.cashAccountID, Equal<PaymentMethodAccount.cashAccountID>>>,
			Where<PaymentMethodAccount.paymentMethodID, Equal<Current<PrintChecksFilter.paymentMethodID>>,
				And<PRxPaymentMethodAccount.useForPR, Equal<True>,
				And<CashAccount.branchID, Equal<Current<AccessInfo.branchID>>,
				And<PaymentMethodAccount.aPIsDefault, Equal<True>>>>>>))]
		public virtual Int32? CashAccountID { get; set; }
		#endregion

		#region NextCheckNbr
		public abstract class nextCheckNbr : PX.Data.BQL.BqlString.Field<nextCheckNbr> { }
		[PXString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Next Check Number")]
		[PXUIVisible(typeof(Where<Selector<PrintChecksFilter.paymentMethodID, PRxPaymentMethod.prCreateBatchPayment>, Equal<False>>))]
		public virtual string NextCheckNbr { get; set; }
		#endregion

		#region SelTotal
		public abstract class selTotal : PX.Data.BQL.BqlDecimal.Field<selTotal> { }
		[PXDecimal(2)]
		[PXUIField(DisplayName = "Selection Total", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXUnboundDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? SelTotal { get; set; }
		#endregion

		#region SelCount
		public abstract class selCount : PX.Data.BQL.BqlInt.Field<selCount> { }
		[PXInt]
		[PXUnboundDefault(0)]
		[PXUIField(DisplayName = "Number of Payments", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual int? SelCount { get; set; }
		#endregion

		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXString(5, IsUnicode = true, InputMask = ">LLLLL")]
		[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXUnboundDefault(typeof(Search<CashAccount.curyID, Where<CashAccount.cashAccountID, Equal<Current<PrintChecksFilter.cashAccountID>>>>))]
		[PXSelector(typeof(Currency.curyID))]
		public virtual String CuryID { get; set; }
		#endregion

		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXLong()]
		[CurrencyInfo(ModuleCode = BatchModule.AP)]
		public virtual Int64? CuryInfoID { get; set; }
		#endregion

		#region CashBalance
		public abstract class cashBalance : PX.Data.BQL.BqlDecimal.Field<cashBalance> { }
		[PXUnboundDefault(TypeCode.Decimal, "0.0")]
		[PXCury(typeof(PrintChecksFilter.curyID))]
		[PXUIField(DisplayName = "Available Balance", Enabled = false)]
		[CashBalance(typeof(PrintChecksFilter.cashAccountID))]
		public virtual Decimal? CashBalance { get; set; }
		#endregion

		#region PaymentBatchNbr
		public abstract class paymentBatchNbr : PX.Data.BQL.BqlString.Field<paymentBatchNbr> { }
		[PXString(15)]
		[PXUIField(DisplayName = "Payment Batch")]
		[PRPaymentBatchSelector(typeof(CABatch.batchNbr), typeof(PrintChecksFilter.paymentMethodID), typeof(PrintChecksFilter.cashAccountID),
			typeof(CABatch.batchNbr), typeof(CABatch.tranDate), typeof(CABatch.tranDesc), typeof(CABatch.curyDetailTotal))]
		[PXUIEnabled(typeof(Where<PrintChecksFilter.paymentMethodID.IsNotNull.And<PrintChecksFilter.cashAccountID.IsNotNull>>))]
		public virtual string PaymentBatchNbr { get; set; }
		#endregion
	}

	public class PRPaymentBatchSelector : PXCustomSelectorAttribute
	{
		protected Type PaymentMethodField { get; set; }
		protected Type CashAccountField { get; set; }

		public class Payments : SelectFrom<PRPayment>
				.InnerJoin<CABatchDetail>.On<CABatchDetail.origDocType.IsEqual<PRPayment.docType>
					.And<CABatchDetail.origRefNbr.IsEqual<PRPayment.refNbr>
					.And<CABatchDetail.origModule.IsEqual<GL.BatchModule.modulePR>>>>
				.InnerJoin<CABatch>
					.On<CABatch.batchNbr.IsEqual<CABatchDetail.batchNbr>>
				.Where<CABatch.paymentMethodID.IsEqual<P.AsString>
					.And<CABatch.cashAccountID.IsEqual<P.AsInt>>>
				.AggregateTo<GroupBy<PRPayment.docType>, GroupBy<PRPayment.refNbr>>
		{ }

		public PRPaymentBatchSelector(Type type, Type paymentMethodField, Type cashAccountField) : base(type)
		{
			PaymentMethodField = paymentMethodField;
			CashAccountField = cashAccountField;
		}

		public PRPaymentBatchSelector(Type type, Type paymentMethodField, Type cashAccountField, params Type[] fieldList) : base(type, fieldList)
		{
			PaymentMethodField = paymentMethodField;
			CashAccountField = cashAccountField;
		}

		protected virtual IEnumerable GetRecords()
		{
			string paymentMethod = GetCurrentValue(this._Graph, PaymentMethodField)?.ToString();
			int? cashAccount = (int?)GetCurrentValue(this._Graph, CashAccountField);

			var results = Payments.View.Select(_Graph, paymentMethod, cashAccount)
				.GroupBy(x => ((CABatch)(PXResult<PRPayment, CABatchDetail, CABatch>)x).BatchNbr, y => (PXResult<PRPayment, CABatchDetail, CABatch>)y);
			foreach (IGrouping<string, PXResult<PRPayment, CABatchDetail, CABatch>> group in results)
			{
				string status = PaymentBatchStatus.GetStatus(group.Select(x => (PRPayment)x));
				if (status == PaymentBatchStatus.ReadyForExport || status == PaymentBatchStatus.WaitingPaycheckCalculation)
				{
					CABatch batch = group.First();
					yield return batch;
				}
			}
		}

		protected object GetCurrentValue(PXGraph graph, Type type)
		{
			return graph.Caches[BqlCommand.GetItemType(type)].GetValue(graph.Caches[BqlCommand.GetItemType(type)].Current, type.Name);
		}
	}
}
