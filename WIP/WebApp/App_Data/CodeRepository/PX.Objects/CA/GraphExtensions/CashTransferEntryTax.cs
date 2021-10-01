using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.Extensions.SalesTax;
using PX.Objects.TX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxDetail = PX.Objects.Extensions.SalesTax.TaxDetail;

namespace PX.Objects.CA
{
	public class CashTransferEntryTax : TaxGraph<CashTransferEntry, CATransfer>
	{
		protected Type DocumentTypeCuryDocBal = typeof(CAExpense.curyTranAmt);

		protected IEnumerable details()
		{
			yield return base.Documents.Current;
		}

		protected override DocumentMapping GetDocumentMapping()
		{
			return new DocumentMapping(typeof(CAExpense))
			{
				DocumentDate = typeof(CAExpense.tranDate),
				CuryDocBal = typeof(CAExpense.curyTranAmt),
				CuryTaxTotal = typeof(CAExpense.curyTaxTotal),
				CuryLinetotal = typeof(CAExpense.curyTaxableAmt),
				BranchID = typeof(CAExpense.branchID),
				CuryID = typeof(CAExpense.curyID),
				CuryInfoID = typeof(CAExpense.curyInfoID),
				CuryTaxRoundDiff = typeof(CAExpense.curyTaxRoundDiff),
				TaxRoundDiff = typeof(CAExpense.taxRoundDiff),
				TaxZoneID = typeof(CAExpense.taxZoneID),
				FinPeriodID = typeof(CAExpense.finPeriodID),
				TaxCalcMode = typeof(CAExpense.taxCalcMode),
				CuryExtPriceTotal = typeof(CAExpense.curyTaxableAmt),
				CuryOrigWhTaxAmt = typeof(CAExpense.curyTaxAmt),
			};
		}

		protected override DetailMapping GetDetailMapping()
		{
			return new DetailMapping(typeof(CAExpense))
			{
				CuryTranAmt = typeof(CAExpense.curyTaxableAmt),
				CuryTranExtPrice = typeof(CAExpense.curyTaxableAmt),
				CuryInfoID = typeof(CAExpense.curyInfoID),
				TaxCategoryID = typeof(CAExpense.taxCategoryID),
			};
		}

		protected override TaxDetailMapping GetTaxDetailMapping()
		{
			return new TaxDetailMapping(typeof(CAExpenseTax), typeof(CAExpenseTax.taxID));
		}

		protected override TaxTotalMapping GetTaxTotalMapping()
		{
			return new TaxTotalMapping(typeof(CAExpenseTaxTran), typeof(CAExpenseTaxTran.taxID));
		}

		protected override bool AskConfirmationToRecalculateExtCost { get => false; }

		protected override PXResultset<Detail> SelectDetails()
		{
			var result = new PXResultset<Detail>();
			result.Add(new PXResult<Detail>(Documents.Cache.GetExtension<Detail>(Documents.Current)));
			return result;
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			Dictionary<string, PXResult<Tax, TaxRev>> tail = new Dictionary<string, PXResult<Tax, TaxRev>>();
			object[] currents = new object[] { row };

			foreach (PXResult<Tax, TaxRev> record in PXSelectReadonly2<Tax,
				LeftJoin<TaxRev, On<TaxRev.taxID, Equal<Tax.taxID>,
					And<TaxRev.outdated, Equal<boolFalse>,
					And2<Where<TaxRev.taxType, Equal<TaxType.purchase>, And<Tax.reverseTax, Equal<boolFalse>,
						Or<TaxRev.taxType, Equal<TaxType.sales>, And<Tax.reverseTax, Equal<boolTrue>,
						Or<Tax.taxType, Equal<CSTaxType.use>,
						Or<Tax.taxType, Equal<CSTaxType.withholding>>>>>>>,
					And<Current<CAExpense.tranDate>, Between<TaxRev.startDate, TaxRev.endDate>>>>>>,
				Where>
				.SelectMultiBound(graph, currents, parameters))
			{
				Tax adjdTax = base.AdjustTaxLevel((Tax)record);
				tail[((Tax)record).TaxID] = new PXResult<Tax, TaxRev>(adjdTax, (TaxRev)record);
			}

			List<object> ret = new List<object>();

			switch (taxchk)
			{
				case PXTaxCheck.Line:
				case PXTaxCheck.RecalcLine:
					PXSelectBase<CAExpenseTax> cmdCAExpenseTax = new PXSelect<CAExpenseTax,
										Where<CAExpenseTax.refNbr, Equal<Current<CAExpense.refNbr>>,
										And<CAExpenseTax.lineNbr, Equal<Current<CAExpense.lineNbr>>>>>(graph);
					foreach (CAExpenseTax record in cmdCAExpenseTax.View.SelectMultiBound(currents))
					{
						PXResult<Tax, TaxRev> line;
						if (tail.TryGetValue(record.TaxID, out line))
						{
							int idx;
							for (idx = ret.Count;
								(idx > 0)
								&& String.Compare(((Tax)(PXResult<CAExpenseTax, Tax, TaxRev>)ret[idx - 1]).TaxCalcLevel, ((Tax)line).TaxCalcLevel) > 0;
								idx--) ;
							ret.Insert(idx, new PXResult<CAExpenseTax, Tax, TaxRev>(record, (Tax)line, (TaxRev)line));
						}
					}
					break;
				case PXTaxCheck.RecalcTotals:
					PXSelectBase<CAExpenseTaxTran> cmdCAExpenseTaxTrans = new PXSelect<CAExpenseTaxTran,
										Where<CAExpenseTaxTran.refNbr, Equal<Current<CAExpense.refNbr>>,
										And<CAExpenseTaxTran.lineNbr, Equal<Current<CAExpense.lineNbr>>>>>(graph);
					foreach (CAExpenseTaxTran record in cmdCAExpenseTaxTrans.View.SelectMultiBound(currents))
					{
						if (string.IsNullOrEmpty(record.TaxID))
						{
							continue;
						}

						PXResult<Tax, TaxRev> line;
						if (tail.TryGetValue(record.TaxID, out line))
						{
							int idx;
							for (idx = ret.Count;
								(idx > 0)
								&& String.Compare(((Tax)(PXResult<CAExpenseTaxTran, Tax, TaxRev>)ret[idx - 1]).TaxCalcLevel, ((Tax)line).TaxCalcLevel) > 0;
								idx--) ;
							ret.Insert(idx, new PXResult<CAExpenseTaxTran, Tax, TaxRev>(record, (Tax)line, (TaxRev)line));
						}
					}
					break;
			}
			return ret;
		}

		protected override List<object> SelectDocumentLines(PXGraph graph, object row)
		{
			List<object> ret = PXSelect<CAExpense,
								Where<CAExpense.refNbr, Equal<Current<CAExpense.refNbr>>,
									And<CAExpense.lineNbr, Equal<Current<CAExpense.lineNbr>>>>>
									.SelectMultiBound(graph, new object[] { row })
									.RowCast<CAExpense>()
									.Select(_ => (object)_)
									.ToList();
			return ret;
		}

		protected override List<object> ChildSelect(PXCache cache, object data)
		{
			return new List<object>() { cache.Current };
		}
		protected virtual string GetRefNbr(PXCache sender, object row)
		{
			return (string)sender.GetValue<CAExpense.refNbr>(row);
		}
		protected virtual int? GetLineNbr(PXCache sender, object row)
		{
			return (int?)sender.GetValue<CAExpense.lineNbr>(row);
		}
		protected virtual string GetEntryTypeID(PXCache sender, object row)
		{
			return (string)sender.GetValue<CAExpense.entryTypeID>(row);
		}

		protected virtual string GetTaxZoneLocal(PXCache sender, object row)
		{
			return (string)sender.GetValue<CAExpense.taxZoneID>(row);
		}

		protected override string GetExtCostLabel(PXCache sender, object row)
		{
			return ((PXDecimalState)sender.GetValueExt<CAExpense.curyTranAmt>(row)).DisplayName;
		}

		protected override decimal? GetCuryTranAmt(PXCache sender, object row)
		{
			return (decimal?)sender.GetValue<CAExpense.curyTranAmt>(row);
		}

		protected override void CalcDocTotals(object row, decimal CuryTaxTotal, decimal CuryInclTaxTotal, decimal CuryWhTaxTotal)
		{
			decimal CuryLineTotal = 0m;
			decimal DiscountLineTotal = 0m;
			decimal TranExtPriceTotal = 0m;

			CalcSingleLineTotals(row, Documents.Current, out CuryLineTotal, out DiscountLineTotal, out TranExtPriceTotal);

			decimal CuryDocTotal = CuryLineTotal + CuryTaxTotal - CuryInclTaxTotal;

			decimal doc_CuryLineTotal = CurrentDocument.CuryLineTotal ?? 0m;
			decimal doc_CuryTaxTotal = CurrentDocument.CuryTaxTotal ?? 0m;

				if (Equals(CuryLineTotal, doc_CuryLineTotal) == false ||
					Equals(CuryTaxTotal, doc_CuryTaxTotal) == false)
				{
					ParentSetValue<Document.curyLineTotal>(CuryLineTotal);
					ParentSetValue<Document.curyDiscountLineTotal>(DiscountLineTotal);
					ParentSetValue<Document.curyExtPriceTotal>(TranExtPriceTotal);
					ParentSetValue<Document.curyTaxTotal>(CuryTaxTotal);
					if (GetDocumentMapping().CuryDocBal != typeof(Document.curyDocBal))
					{
						ParentSetValue<Document.curyDocBal>(CuryDocTotal);
						return;
					}
			}

			if (GetDocumentMapping().CuryDocBal != typeof(Document.curyDocBal))
			{
				decimal doc_CuryDocBal = CurrentDocument.CuryDocBal ?? 0m;
				if (Equals(CuryDocTotal, doc_CuryDocBal) == false)
				{
					ParentSetValue<Document.curyDocBal>(CuryDocTotal);
				}
			}
		}

		protected virtual void CalcSingleLineTotals(object row, Document document, out decimal curyLineTotal, out decimal discountLineTotal, out decimal tranExtPriceTotal)
		{
			Detail orig_row = Details.Cache.GetExtension<Detail>(document);

			curyLineTotal = orig_row?.CuryTranAmt ?? 0m;
			discountLineTotal = orig_row?.CuryTranDiscount ?? 0m;
			tranExtPriceTotal = orig_row?.CuryTranExtPrice ?? 0m;
		}

		public override void Detail_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			var row = (CAExpense)CurrentDocument.Base;

			if (row?.CashAccountID == null || row.EntryTypeID == null)
			{
				return;
			}

			if (!object.Equals(GetTaxCategory(sender, e.OldRow), GetTaxCategory(sender, e.Row)) ||
				!object.Equals(GetCuryTranAmt(sender, e.OldRow), GetCuryTranAmt(sender, e.Row)) ||
				!object.Equals(GetTaxZoneLocal(sender, e.OldRow), GetTaxZoneLocal(sender, e.Row)) ||
				!object.Equals(GetRefNbr(sender, e.OldRow), GetRefNbr(sender, e.Row)) ||
				!object.Equals(GetLineNbr(sender, e.OldRow), GetLineNbr(sender, e.Row)) ||
				!object.Equals(GetEntryTypeID(sender, e.OldRow), GetEntryTypeID(sender, e.Row))
				)
			{
				PXCache cache = sender.Graph.Caches[typeof(CAExpense)];
				Preload();

				ReDefaultTaxes(SelectTheDetail(row));

				Document rowDoc = sender.GetExtension<Document>(e.Row);
				_ParentRow = rowDoc;
				CalcTaxes(null);
				_ParentRow = null;
			}

			base.Detail_RowUpdated(sender, e);

		}

		/// <summary>
		/// The method converts the current row to <see cref = "PXResultset{T0}" />.
		/// The method replaces the base Details.Select() call to avoid the selection of all details according to the feature that the expense is a document and line 2-in-1.
		/// </summary>
		private PXResultset<Detail> SelectTheDetail(CAExpense row)
		{
			var currentDetail = Details.Cache.GetExtension<Detail>(row);
			var seletResult = new PXResultset<Detail>();
			seletResult.Add(new PXResult<Detail>(currentDetail));
			return seletResult;
		}

		public override void Detail_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			if(CurrentDocument != null)
			{
				base.Detail_RowDeleted(sender, e);
			}
		}

		protected override void CurrencyInfo_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (e.Row is CAExpense)
			{
				base.CurrencyInfo_RowUpdated(sender, e);

				TaxCalc taxCalc = CurrentDocument.TaxCalc ?? TaxCalc.NoCalc;
				if (taxCalc == TaxCalc.Calc || taxCalc == TaxCalc.ManualLineCalc)
				{
					if (e.Row != null && ((CurrencyInfo)e.Row).CuryRate != null && (e.OldRow == null || !sender.ObjectsEqual<CurrencyInfo.curyRate, CurrencyInfo.curyMultDiv>(e.Row, e.OldRow)))
					{
						if (Base.Expenses.SelectSingle() != null)
						{
							CalcTaxes(null);
						}
					}
				}
			}
		}

		protected override void SetExtCostExt(PXCache sender, object child, decimal? value)
		{
			CAExpense row = child as CAExpense;
			if (row != null)
			{
				row.CuryTaxableAmt = value;
				sender.Update(row);
			}
		}

		protected override void DefaultTaxes(Detail row, bool DefaultExisting)
		{
			if(CurrentDocument == null)
			{
				return;
			}

			base.DefaultTaxes(row, DefaultExisting);
		}

		#region CAExpenseTaxTran_FieldDefaultings
		protected virtual void CAExpenseTaxTran_TaxType_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row != null && Base.Expenses.Current != null)
			{
				if (Base.Expenses.Current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(sender.Graph, ((CAExpenseTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.TranTaxType;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(sender.Graph, ((CAExpenseTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.TranTaxType;
						e.Cancel = true;
					}
				}
			}
		}

		protected virtual void CAExpenseTaxTran_AccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row != null && Base.Expenses.Current != null)
			{
				if (Base.Expenses.Current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(sender.Graph, ((CAExpenseTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxAcctID;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(sender.Graph, ((CAExpenseTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxAcctID;
						e.Cancel = true;
					}
				}
			}
		}

		protected virtual void CAExpenseTaxTran_SubID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row != null && Base.Expenses.Current != null)
			{
				if (Base.Expenses.Current.DrCr == CADrCr.CACredit)
				{
					AP.PurchaseTax tax = PXSelect<AP.PurchaseTax, Where<AP.PurchaseTax.taxID, Equal<Required<AP.PurchaseTax.taxID>>>>.Select(sender.Graph, ((CAExpenseTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxSubID;
						e.Cancel = true;
					}
				}
				else
				{
					AR.SalesTax tax = PXSelect<AR.SalesTax, Where<AR.SalesTax.taxID, Equal<Required<AR.SalesTax.taxID>>>>.Select(sender.Graph, ((CAExpenseTaxTran)e.Row).TaxID);
					if (tax != null)
					{
						e.NewValue = tax.HistTaxSubID;
						e.Cancel = true;
					}
				}
			}
		}
		#endregion
		#region Buttons
		public PXAction<CATransfer> ViewExpenseTaxes;
		[PXUIField(DisplayName = "View Taxes", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Enabled = true, Visible = false)]
		[PXLookupButton]
		protected virtual IEnumerable viewExpenseTaxes(PXAdapter adapter)
		{
			Base.ExpenseTaxTrans.AskExt(true);
			return adapter.Get();
		}
		#endregion
	}
}
