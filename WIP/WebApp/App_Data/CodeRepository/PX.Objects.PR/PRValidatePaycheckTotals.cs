using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CS;
using PX.Objects.PM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	[PXHidden]
	public class PRValidatePaycheckTotals : PXGraph<PRValidatePaycheckTotals>
	{
		private DeductionDictionary _DeductCodes;

		public SelectFrom<PRPayment>.View Document;

		public SelectFrom<PREarningDetailExt>
			.Where<PREarningDetail.FK.Payment.SameAsCurrent>.View EarningDetails;

		public SelectFrom<PRPaymentEarningExt>
			.Where<PRPaymentEarning.FK.Payment.SameAsCurrent>.View SummaryEarnings;

		public SelectFrom<PRDeductionDetail>
			.Where<PRDeductionDetail.FK.Payment.SameAsCurrent>.View DeductionDetails;

		public SelectFrom<PRBenefitDetail>
			.Where<PRBenefitDetail.FK.Payment.SameAsCurrent>.View BenefitDetails;

		public SelectFrom<PRPaymentProjectPackageDeduct>
			.Where<PRPaymentProjectPackageDeduct.FK.Payment.SameAsCurrent>.View ProjectPackageDeductions;

		public SelectFrom<PRPaymentUnionPackageDeduct>
			.Where<PRPaymentUnionPackageDeduct.FK.Payment.SameAsCurrent>.View UnionPackageDeductions;

		public SelectFrom<PRPaymentWCPremium>
			.Where<PRPaymentWCPremium.FK.Payment.SameAsCurrent>.View WCPackageDeductions;

		public SelectFrom<PRPaymentDeduct>
			.Where<PRPaymentDeduct.FK.Payment.SameAsCurrent>.View SummaryDeductions;

		public SelectFrom<PRDeductionDetail>
			.LeftJoin<PRPaymentDeduct>.On<PRPaymentDeduct.FK.Payment.SameAsCurrent
				.And<PRPaymentDeduct.codeID.IsEqual<PRDeductionDetail.codeID>>>
			.Where<PRDeductionDetail.FK.Payment.SameAsCurrent
				.And<PRPaymentDeduct.codeID.IsNull>>.View DeductionDetailsWithMissingSummary;

		public SelectFrom<PRBenefitDetail>
			.LeftJoin<PRPaymentDeduct>.On<PRPaymentDeduct.FK.Payment.SameAsCurrent
				.And<PRPaymentDeduct.codeID.IsEqual<PRBenefitDetail.codeID>>>
			.Where<PRBenefitDetail.FK.Payment.SameAsCurrent
				.And<PRPaymentDeduct.codeID.IsNull>>.View BenefitDetailsWithMissingSummary;

		public SelectFrom<PRPaymentTaxSplit>
			.Where<PRPaymentTaxSplit.FK.Payment.SameAsCurrent>.View TaxSplits;

		public SelectFrom<PRTaxDetailExt>
			.Where<PRTaxDetail.FK.Payment.SameAsCurrent>.View TaxDetails;

		public SelectFrom<PRPaymentTaxSplitExt>
			.LeftJoin<PRTaxDetail>.On<PRTaxDetail.FK.Payment.SameAsCurrent
				.And<PRTaxDetail.taxID.IsEqual<PRPaymentTaxSplit.taxID>>>
			.Where<PRPaymentTaxSplit.FK.Payment.SameAsCurrent
				.And<PRPaymentTaxSplit.taxAmount.IsNotEqual<decimal0>>
				.And<PRTaxDetail.taxID.IsNull>>.View TaxSplitsWithDetailMissing;

		public SelectFrom<PRPaymentTaxExt>
			.Where<PRPaymentTax.FK.Payment.SameAsCurrent>.View SummaryTaxes;

		public SelectFrom<PRTaxDetailExt>
			.LeftJoin<PRPaymentTax>.On<PRPaymentTax.FK.Payment.SameAsCurrent
				.And<PRPaymentTax.taxID.IsEqual<PRTaxDetail.taxID>>>
			.Where<PRTaxDetail.FK.Payment.SameAsCurrent
				.And<PRPaymentTax.taxID.IsNull>>.View TaxDetailsWithSummaryMissing;

		public SelectFrom<PRDirectDepositSplit>
			.Where<PRDirectDepositSplit.FK.Payment.SameAsCurrent>.View DirectDepositSplits;

		public virtual void ValidateTotals(PRPayment payment, bool tryCorrectTotalsOnDiscrepancy)
		{
			Document.Current = payment;
			_DeductCodes = new DeductionDictionary();
			foreach (PRDeductCode deductCode in SelectFrom<PRDeductCode>.View.Select(this).FirstTableItems)
			{
				_DeductCodes[deductCode.CodeID] = deductCode;
			}

			ValidateSummaryEarnings(tryCorrectTotalsOnDiscrepancy);
			ValidateDeductionAndBenefitDetails();
			ValidateSummaryDeductionsAndBenefits(tryCorrectTotalsOnDiscrepancy);
			ValidateTaxDetails();
			ValidateSummaryTaxes(tryCorrectTotalsOnDiscrepancy);
			ValidateHeader(tryCorrectTotalsOnDiscrepancy);
			ValidateDirectDepositSplits(tryCorrectTotalsOnDiscrepancy);

			if (tryCorrectTotalsOnDiscrepancy)
			{
				Actions.PressSave();
			}
		}

		protected virtual void ValidateSummaryEarnings(bool tryCorrectTotalsOnDiscrepancy)
		{
			foreach (PRPaymentEarningExt summaryEarning in SummaryEarnings.Select())
			{
				PRPaymentEarningExt copy = SummaryEarnings.Cache.CreateCopy(summaryEarning) as PRPaymentEarningExt;
				PXFormulaAttribute.CalcAggregate<PREarningDetail.amount>(EarningDetails.Cache, copy);
				PXFormulaAttribute.CalcAggregate<PREarningDetail.hours>(EarningDetails.Cache, copy);

				CheckValues(copy.Amount, SummaryEarnings.Cache, summaryEarning, nameof(summaryEarning.Amount), tryCorrectTotalsOnDiscrepancy,
					Messages.SummaryEarningAmountDoesntMatch, summaryEarning.TypeCD, summaryEarning.LocationCD, summaryEarning.Amount, copy.Amount);
				CheckValues(copy.Hours, SummaryEarnings.Cache, summaryEarning, nameof(summaryEarning.Hours), tryCorrectTotalsOnDiscrepancy,
					Messages.SummaryEarningHoursDontMatch, summaryEarning.TypeCD, summaryEarning.LocationCD, summaryEarning.Hours, copy.Hours);

				if (tryCorrectTotalsOnDiscrepancy)
				{
					SummaryEarnings.Update(summaryEarning);
				}

				int ratePrecision = PRCurrencyAttribute.GetPrecision(SummaryEarnings.Cache, summaryEarning, nameof(PRPaymentEarning.rate)) ?? 2;

				if (summaryEarning.Rate.HasValue && summaryEarning.Hours != 0
					&& Math.Round((summaryEarning.Rate * summaryEarning.Hours).GetValueOrDefault(), 2, MidpointRounding.AwayFromZero) != summaryEarning.Amount
					&& Math.Round((summaryEarning.Amount / summaryEarning.Hours).GetValueOrDefault(), ratePrecision, MidpointRounding.AwayFromZero) != summaryEarning.Rate)
				{
					// Was the rate calculated on precise earning detail amounts and the extended amount rounded?
					PREarningDetail[] childrenEarningDetails =
						PXParentAttribute.SelectChildren(Caches[typeof(PREarningDetail)], summaryEarning, typeof(PRPaymentEarning)).Cast<PREarningDetail>().ToArray();
					decimal earningDetailsRate = PaymentEarningRateAttribute.GetPaymentEarningRate(summaryEarning, childrenEarningDetails);
					decimal roundedEarningDetailsRate = Math.Round(earningDetailsRate, ratePrecision, MidpointRounding.AwayFromZero);
					if (summaryEarning.Rate != roundedEarningDetailsRate)
					{
						throw new PXException(Messages.SummaryEarningRateDoesntMatch, summaryEarning.TypeCD, summaryEarning.LocationCD);
					}
				}
			}

			IEnumerable<PREarningDetailExt> detailsMissingParent = EarningDetails.Select().FirstTableItems
				.Where(x => PXParentAttribute.SelectParent<PRPaymentEarning>(EarningDetails.Cache, x) == null);
			if (detailsMissingParent.Any())
			{
				throw new PXException(Messages.SummaryEarningMissing, detailsMissingParent.First().TypeCD, detailsMissingParent.First().LocationCD);
			}
		}

		protected virtual void ValidateDeductionAndBenefitDetails()
		{
			List<PRDeductionDetail> deductionDetails = DeductionDetails.Select().FirstTableItems.ToList();
			List<PRBenefitDetail> benefitDetails = BenefitDetails.Select().FirstTableItems.ToList();
			List<PRPaymentDeduct> summaries = SummaryDeductions.Select().FirstTableItems.ToList();

			List<PRPaymentProjectPackageDeduct> projectPackages = ProjectPackageDeductions.Select().FirstTableItems.ToList();
			HashSet<int?> projectDeductCodes = new HashSet<int?>();
			projectPackages.ForEach(x => projectDeductCodes.Add(x.DeductCodeID));
			deductionDetails.Where(x => DeductionSourceListAttribute.GetSource(_DeductCodes[x.CodeID]) == DeductionSourceListAttribute.CertifiedProject)
				.ForEach(x => projectDeductCodes.Add(x.CodeID));
			benefitDetails.Where(x => DeductionSourceListAttribute.GetSource(_DeductCodes[x.CodeID]) == DeductionSourceListAttribute.CertifiedProject)
				.ForEach(x => projectDeductCodes.Add(x.CodeID));
			projectDeductCodes.ForEach(x => VerifyDeductionAndBenefitDetailsForProject(projectPackages, deductionDetails, benefitDetails, summaries, _DeductCodes[x]));

			List<PRPaymentUnionPackageDeduct> unionPackages = UnionPackageDeductions.Select().FirstTableItems.ToList();
			HashSet<int?> unionDeductCodes = new HashSet<int?>();
			unionPackages.ForEach(x => unionDeductCodes.Add(x.DeductCodeID));
			deductionDetails.Where(x => DeductionSourceListAttribute.GetSource(_DeductCodes[x.CodeID]) == DeductionSourceListAttribute.Union)
				.ForEach(x => unionDeductCodes.Add(x.CodeID));
			benefitDetails.Where(x => DeductionSourceListAttribute.GetSource(_DeductCodes[x.CodeID]) == DeductionSourceListAttribute.Union)
				.ForEach(x => unionDeductCodes.Add(x.CodeID));
			unionDeductCodes.ForEach(x => VerifyDeductionAndBenefitDetailsForUnion(unionPackages, deductionDetails, benefitDetails, summaries, _DeductCodes[x]));

			List<PRPaymentWCPremium> wcPackages = WCPackageDeductions.Select().FirstTableItems.ToList();
			HashSet<int?> wcDeductCodes = new HashSet<int?>();
			wcPackages.ForEach(x => wcDeductCodes.Add(x.DeductCodeID));
			deductionDetails.Where(x => DeductionSourceListAttribute.GetSource(_DeductCodes[x.CodeID]) == DeductionSourceListAttribute.WorkCode)
				.ForEach(x => wcDeductCodes.Add(x.CodeID));
			benefitDetails.Where(x => DeductionSourceListAttribute.GetSource(_DeductCodes[x.CodeID]) == DeductionSourceListAttribute.WorkCode)
				.ForEach(x => wcDeductCodes.Add(x.CodeID));
			wcDeductCodes.ForEach(x => VerifyDeductionAndBenefitDetailsForWC(wcPackages, deductionDetails, benefitDetails, x, _DeductCodes[x].CodeCD));
		}

		protected virtual void VerifyDeductionAndBenefitDetailsForProject(
			IEnumerable<PRPaymentProjectPackageDeduct> projectPackages,
			List<PRDeductionDetail> deductionDetails,
			IEnumerable<PRBenefitDetail> benefitDetails,
			IEnumerable<PRPaymentDeduct> benefitSummaries,
			PRDeductCode deductCode)
		{
			decimal? packageTotalDeductionAmount = projectPackages.Sum(x => x.DeductCodeID == deductCode.CodeID ? x.DeductionAmount : 0m);
			decimal? detailTotalDeductionAmount = deductionDetails.Sum(x => x.CodeID == deductCode.CodeID ? x.Amount : 0m);
			CheckValues(packageTotalDeductionAmount, detailTotalDeductionAmount, Messages.DeductionDetailSumDoesntMatchProject, deductCode.CodeCD, packageTotalDeductionAmount);

			decimal? packageTotalBenefitAmount = projectPackages.Sum(x => x.DeductCodeID == deductCode.CodeID ? x.BenefitAmount : 0m);
			decimal fringeAmountInBenefit = GetFringeAmountInBenefit(deductCode.CodeID).GetValueOrDefault();
			if (deductCode.NoFinancialTransaction == true)
			{
				decimal? summaryBenefitAmount = benefitSummaries.FirstOrDefault(x => x.CodeID == deductCode.CodeID)?.CntAmount;
				CheckValues(packageTotalBenefitAmount + fringeAmountInBenefit, summaryBenefitAmount,
					Messages.BenefitSummarySumDoesntMatchProject, deductCode.CodeCD, summaryBenefitAmount, fringeAmountInBenefit);
			}
			else
			{
				decimal? detailBenefitTotalAmount = benefitDetails.Sum(x => x.CodeID == deductCode.CodeID ? x.Amount : 0m);
				CheckValues(packageTotalBenefitAmount + fringeAmountInBenefit, detailBenefitTotalAmount,
					Messages.BenefitDetailSumDoesntMatchProject, deductCode.CodeCD, packageTotalBenefitAmount, fringeAmountInBenefit);
			}
		}

		protected virtual void VerifyDeductionAndBenefitDetailsForUnion(
			IEnumerable<PRPaymentUnionPackageDeduct> unionPackages,
			List<PRDeductionDetail> deductionDetails,
			IEnumerable<PRBenefitDetail> benefitDetails,
			IEnumerable<PRPaymentDeduct> benefitSummaries,
			PRDeductCode deductCode)
		{
			decimal? packageTotalDeductionAmount = unionPackages.Sum(x => x.DeductCodeID == deductCode.CodeID ? x.DeductionAmount : 0m);
			decimal? detailTotalDeductionAmount = deductionDetails.Sum(x => x.CodeID == deductCode.CodeID ? x.Amount : 0m);
			CheckValues(packageTotalDeductionAmount, detailTotalDeductionAmount, Messages.DeductionDetailSumDoesntMatchUnion, deductCode.CodeCD, packageTotalDeductionAmount);

			decimal? packageTotalBenefitAmount = unionPackages.Sum(x => x.DeductCodeID == deductCode.CodeID ? x.BenefitAmount : 0m);
			if (deductCode.NoFinancialTransaction == true)
			{
				decimal? summaryBenefitAmount = benefitSummaries.FirstOrDefault(x => x.CodeID == deductCode.CodeID)?.CntAmount;
				CheckValues(packageTotalBenefitAmount, summaryBenefitAmount, Messages.BenefitSummarySumDoesntMatchUnion, deductCode.CodeCD, packageTotalBenefitAmount);
			}
			else
			{
				decimal? detailBenefitTotalAmount = benefitDetails.Sum(x => x.CodeID == deductCode.CodeID ? x.Amount : 0m);
				CheckValues(packageTotalBenefitAmount, detailBenefitTotalAmount, Messages.BenefitDetailSumDoesntMatchUnion, deductCode.CodeCD, packageTotalBenefitAmount);
			}
		}

		protected virtual void VerifyDeductionAndBenefitDetailsForWC(
			List<PRPaymentWCPremium> wcPackages,
			List<PRDeductionDetail> deductionDetails,
			List<PRBenefitDetail> benefitDetails,
			int? codeID,
			string codeCD)
		{
			decimal? packageTotalDeductionAmount = wcPackages.Sum(x => x.DeductCodeID == codeID ? x.DeductionAmount : 0m);
			decimal? detailTotalDeductionAmount = deductionDetails.Sum(x => x.CodeID == codeID ? x.Amount : 0m);
			CheckValues(packageTotalDeductionAmount, detailTotalDeductionAmount, Messages.DeductionDetailSumDoesntMatchWC, codeCD, packageTotalDeductionAmount);

			decimal? packageTotalBenefitAmount = wcPackages.Sum(x => x.DeductCodeID == codeID ? x.Amount : 0m);
			decimal? detailTotalBenefitAmount = benefitDetails.Sum(x => x.CodeID == codeID ? x.Amount : 0m);
			CheckValues(packageTotalBenefitAmount, detailTotalBenefitAmount, Messages.BenefitDetailSumDoesntMatchWC, codeCD, packageTotalBenefitAmount);
		}

		protected virtual decimal? GetFringeAmountInBenefit(int? deductCodeID)
		{
			return new SelectFrom<PRPaymentFringeBenefit>
				.InnerJoin<PMProject>.On<PMProject.contractID.IsEqual<PRPaymentFringeBenefit.projectID>>
				.Where<PRPaymentFringeBenefit.docType.IsEqual<PRPayment.docType.FromCurrent>
					.And<PRPaymentFringeBenefit.refNbr.IsEqual<PRPayment.refNbr.FromCurrent>>
					.And<PMProjectExtension.benefitCodeReceivingFringeRate.IsEqual<P.AsInt>>>
				.AggregateTo<Sum<PRPaymentFringeBenefit.fringeAmountInBenefit>>.View(this).SelectSingle(deductCodeID)?.FringeAmountInBenefit;
		}

		protected virtual void ValidateSummaryDeductionsAndBenefits(bool tryCorrectTotalsOnDiscrepancy)
		{
			Dictionary<int?, IEnumerable<PRDeductionDetail>> deductionDetails = DeductionDetails.Select().FirstTableItems
				.GroupBy(x => x.CodeID)
				.ToDictionary(k => k.Key, v => v.AsEnumerable());
			Dictionary<int?, IEnumerable<PRBenefitDetail>> benefitDetails = BenefitDetails.Select().FirstTableItems
				.GroupBy(x => x.CodeID)
				.ToDictionary(k => k.Key, v => v.AsEnumerable());
			foreach (PRPaymentDeduct summaryDeduction in SummaryDeductions.Select())
			{
				decimal deductionDetailAmount = 0;
				if (deductionDetails.ContainsKey(summaryDeduction.CodeID))
				{
					deductionDetailAmount = deductionDetails[summaryDeduction.CodeID].Sum(x => x.Amount.GetValueOrDefault());
				}

				decimal benefitDetailAmount = 0;
				if (benefitDetails.ContainsKey(summaryDeduction.CodeID))
				{
					benefitDetailAmount = benefitDetails[summaryDeduction.CodeID].Sum(x => x.Amount.GetValueOrDefault());
				}

				CheckValues(deductionDetailAmount, SummaryDeductions.Cache, summaryDeduction, nameof(summaryDeduction.DedAmount), tryCorrectTotalsOnDiscrepancy,
					Messages.SummaryDeductionDoesntMatch, _DeductCodes[summaryDeduction.CodeID].CodeCD, summaryDeduction.DedAmount, deductionDetailAmount);

				if (_DeductCodes[summaryDeduction.CodeID].NoFinancialTransaction != true)
				{
					CheckValues(benefitDetailAmount, SummaryDeductions.Cache, summaryDeduction, nameof(summaryDeduction.CntAmount), tryCorrectTotalsOnDiscrepancy,
					Messages.SummaryBenefitDoesntMatch, _DeductCodes[summaryDeduction.CodeID].CodeCD, summaryDeduction.CntAmount, benefitDetailAmount);
				}

				if (tryCorrectTotalsOnDiscrepancy)
				{
					SummaryDeductions.Update(summaryDeduction);
				}
			}

			IEnumerable<int?> codesWithMissingSummary = DeductionDetailsWithMissingSummary.Select().FirstTableItems.Select(x => x.CodeID)
				.Union(BenefitDetailsWithMissingSummary.Select().FirstTableItems.Select(x => x.CodeID));
			if (codesWithMissingSummary.Any())
			{
				throw new PXException(Messages.SummaryDeductionMissing, _DeductCodes[codesWithMissingSummary.First()].CodeCD);
			}
		}

		protected virtual void ValidateTaxDetails()
		{
			Dictionary<int?, IEnumerable<PRPaymentTaxSplit>> taxSplits = TaxSplits.Select().FirstTableItems
				.GroupBy(x => x.TaxID)
				.ToDictionary(k => k.Key, v => v.AsEnumerable());
			foreach (IGrouping<int?, PRTaxDetailExt> taxDetails in TaxDetails.Select().FirstTableItems.GroupBy(x => x.TaxID))
			{
				decimal taxDetailAmount = taxDetails.Sum(x => x.Amount.GetValueOrDefault());
				decimal taxSplitAmount = 0;
				if (taxSplits.ContainsKey(taxDetails.Key))
				{
					taxSplitAmount = taxSplits[taxDetails.Key].Sum(x => x.TaxAmount.GetValueOrDefault());
				}

				CheckValues(taxSplitAmount, taxDetailAmount, Messages.TaxDetailsDontMatch, taxDetails.First().TaxCD, taxDetailAmount, taxSplitAmount);
			}

			IEnumerable<PRPaymentTaxSplitExt> splitsWithMissingDetail = TaxSplitsWithDetailMissing.Select().FirstTableItems;
			if (splitsWithMissingDetail.Any())
			{
				throw new PXException(Messages.TaxDetailMissing, splitsWithMissingDetail.First().TaxCD);
			}
		}

		protected virtual void ValidateSummaryTaxes(bool tryCorrectTotalsOnDiscrepancy)
		{
			foreach (PRPaymentTaxExt summaryTax in SummaryTaxes.Select())
			{
				PRPaymentTax copy = SummaryTaxes.Cache.CreateCopy(summaryTax) as PRPaymentTax;
				PXFormulaAttribute.CalcAggregate<PRPaymentTaxSplit.taxAmount>(TaxSplits.Cache, copy);
				PXFormulaAttribute.CalcAggregate<PRPaymentTaxSplit.wageBaseHours>(TaxSplits.Cache, copy);
				PXFormulaAttribute.CalcAggregate<PRPaymentTaxSplit.wageBaseHours>(TaxSplits.Cache, copy);
				PXFormulaAttribute.CalcAggregate<PRPaymentTaxSplit.wageBaseGrossAmt>(TaxSplits.Cache, copy);

				CheckValues(copy.TaxAmount, SummaryTaxes.Cache, summaryTax, nameof(summaryTax.TaxAmount), tryCorrectTotalsOnDiscrepancy,
					Messages.SummaryTaxAmountDoesntMatch, summaryTax.TaxCD, summaryTax.TaxAmount, copy.TaxAmount);
				CheckValues(copy.WageBaseHours, SummaryTaxes.Cache, summaryTax, nameof(summaryTax.WageBaseHours), tryCorrectTotalsOnDiscrepancy,
					Messages.SummaryTaxHoursDoesntMatch, summaryTax.TaxCD, summaryTax.WageBaseHours, copy.WageBaseHours);
				CheckValues(copy.WageBaseAmount, SummaryTaxes.Cache, summaryTax, nameof(summaryTax.WageBaseAmount), tryCorrectTotalsOnDiscrepancy,
					Messages.SummaryTaxWagesDontMatch, summaryTax.TaxCD, summaryTax.WageBaseAmount, copy.WageBaseAmount);
				CheckValues(copy.WageBaseGrossAmt, SummaryTaxes.Cache, summaryTax, nameof(summaryTax.WageBaseGrossAmt), tryCorrectTotalsOnDiscrepancy,
					Messages.SummaryTaxGrossDoesntMatch, summaryTax.TaxCD, summaryTax.WageBaseGrossAmt, copy.WageBaseGrossAmt);

				if (tryCorrectTotalsOnDiscrepancy)
				{
					SummaryTaxes.Update(summaryTax);
				}
			}

			IEnumerable<PRTaxDetailExt> detailsMissingParent = TaxDetails.Select().FirstTableItems
				.Where(x => PXParentAttribute.SelectParent<PRPaymentTax>(TaxDetails.Cache, x) == null);
			if (detailsMissingParent.Any())
			{
				throw new PXException(Messages.SummaryTaxMissing, detailsMissingParent.First().TaxCD);
			}
		}

		protected virtual void ValidateHeader(bool tryCorrectTotalsOnDiscrepancy)
		{
			PRPayment copy = Document.Cache.CreateCopy(Document.Current) as PRPayment;

			PXFormulaAttribute.CalcAggregate<PREarningDetail.amount>(EarningDetails.Cache, copy);
			PXFormulaAttribute.CalcAggregate<PREarningDetail.hours>(EarningDetails.Cache, copy);
			PXFormulaAttribute.CalcAggregate<PRPaymentDeduct.dedAmount>(SummaryDeductions.Cache, copy);
			PXFormulaAttribute.CalcAggregate<PRPaymentDeduct.cntAmount>(SummaryDeductions.Cache, copy);
			PXFormulaAttribute.CalcAggregate<PRPaymentTax.taxAmountWH>(SummaryTaxes.Cache, copy);
			PXFormulaAttribute.CalcAggregate<PRPaymentTax.taxAmountEmp>(SummaryTaxes.Cache, copy);

			CheckValues(copy.TotalEarnings, Document.Cache, Document.Current, nameof(Document.Current.TotalEarnings), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderEarningAmountDoesntMatch, Document.Current.TotalEarnings, copy.TotalEarnings);
			CheckValues(copy.TotalHours, Document.Cache, Document.Current, nameof(Document.Current.TotalHours), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderEarningHoursDontMatch, Document.Current.TotalHours, copy.TotalHours);
			CheckValues(copy.DedAmount, Document.Cache, Document.Current, nameof(Document.Current.DedAmount), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderDeductionAmountDoesntMatch, Document.Current.DedAmount, copy.DedAmount);
			CheckValues(copy.BenefitAmount, Document.Cache, Document.Current, nameof(Document.Current.BenefitAmount), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderBenefitAmountDoesntMatch, Document.Current.BenefitAmount, copy.BenefitAmount);
			CheckValues(copy.PayableBenefitAmount, Document.Cache, Document.Current, nameof(Document.Current.PayableBenefitAmount), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderPayableBenefitAmountDoesntMatch, Document.Current.PayableBenefitAmount, copy.PayableBenefitAmount);
			CheckValues(copy.TaxAmount, Document.Cache, Document.Current, nameof(Document.Current.TaxAmount), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderEmployeeTaxAmountDoesntMatch, Document.Current.TaxAmount, copy.TaxAmount);
			CheckValues(copy.EmployerTaxAmount, Document.Cache, Document.Current, nameof(Document.Current.EmployerTaxAmount), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderEmployerTaxAmountDoesntMatch, Document.Current.EmployerTaxAmount, copy.EmployerTaxAmount);

			if (tryCorrectTotalsOnDiscrepancy)
			{
				Document.Update(Document.Current);
			}

			decimal? grossAmount = PXFormulaAttribute.Evaluate<PRPayment.grossAmount>(Document.Cache, Document.Current) as decimal?;
			CheckValues(grossAmount, Document.Cache, Document.Current, nameof(Document.Current.GrossAmount), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderGrossAmountDoesntMatch, Document.Current.GrossAmount, copy.GrossAmount);
			if (tryCorrectTotalsOnDiscrepancy)
			{
				Document.Update(Document.Current);
			}

			decimal? netAmount = PXFormulaAttribute.Evaluate<PRPayment.netAmount>(Document.Cache, Document.Current) as decimal?;
			CheckValues(netAmount, Document.Cache, Document.Current, nameof(Document.Current.NetAmount), tryCorrectTotalsOnDiscrepancy,
				Messages.HeaderNetAmountDoesntMatch, Document.Current.NetAmount, copy.NetAmount);
			if (tryCorrectTotalsOnDiscrepancy)
			{
				Document.Update(Document.Current);
			}
		}

		protected void ValidateDirectDepositSplits(bool tryCorrectTotalsOnDiscrepancy)
		{
			if (Document.Current.Calculated != true || Document.Current.NetAmount <= 0)
			{
				return;
			}

			PaymentMethod paymentMethod = PXSelectorAttribute.Select<PRPayment.paymentMethodID>(Document.Cache, Document.Current) as PaymentMethod;
			PRxPaymentMethod paymentMethodExt = Caches[typeof(PaymentMethod)].GetExtension<PRxPaymentMethod>(paymentMethod);
			if (paymentMethodExt.PRPrintChecks == true)
			{
				return;
			}

			decimal directDepositSum = DirectDepositSplits.Select().FirstTableItems.Sum(x => x.Amount.GetValueOrDefault());

			if (tryCorrectTotalsOnDiscrepancy)
			{
				if (directDepositSum != Document.Current.NetAmount)
				{
					PRCalculationEngine calculationEngine = CreateInstance<PRCalculationEngine>();
					calculationEngine.SetDirectDepositSplit(Document.Current);
					calculationEngine.Persist();
					DirectDepositSplits.Cache.Clear();
					DirectDepositSplits.Cache.ClearQueryCache();
					ValidateDirectDepositSplits(false);
				}
			}
			else
			{
				CheckValues(directDepositSum, Document.Current.NetAmount, Messages.DirectDepositSplitsDontMatch, Document.Current.NetAmount, directDepositSum);
			}
		}

		protected virtual void CheckValues(
			object calculatedValue,
			PXCache cache,
			IBqlTable row,
			string fieldName,
			bool tryCorrectTotalsOnDiscrepancy,
			string errorMessage,
			params object[] errorMessageParams)
		{
			object recordValue = cache.GetValue(row, fieldName);
			if (!tryCorrectTotalsOnDiscrepancy)
			{
				CheckValues(calculatedValue, recordValue, errorMessage, errorMessageParams);
			}
			else if (!Equals(calculatedValue, recordValue))
			{
				cache.SetValue(row, fieldName, calculatedValue);
				PXTrace.WriteWarning(PXMessages.LocalizeFormat(errorMessage, errorMessageParams) + PXMessages.LocalizeNoPrefix(Messages.UpdatingFromDetailsWarning));
			}
		}

		protected virtual void CheckValues(object val1, object val2, string errorMessage, params object[] errorMessageParams)
		{
			if (!Equals(val1, val2))
			{
				throw new PXException(errorMessage, errorMessageParams);
			}
		}

		[PXHidden]
		public class PRPaymentEarningExt : PRPaymentEarning
		{
			#region LocationCD
			[PXString]
			[PXUnboundDefault(typeof(SearchFor<PRLocation.locationCD>.Where<PRLocation.locationID.IsEqual<locationID.FromCurrent>>))]
			public string LocationCD { get; set; }
			#endregion
		}

		[PXHidden]
		public class PREarningDetailExt : PREarningDetail
		{
			#region LocationCD
			[PXString]
			[PXUnboundDefault(typeof(SearchFor<PRLocation.locationCD>.Where<PRLocation.locationID.IsEqual<locationID.FromCurrent>>))]
			public string LocationCD { get; set; }
			#endregion
		}

		[PXHidden]
		public class PRPaymentTaxSplitExt : PRPaymentTaxSplit
		{
			#region TaxCD
			[PXString]
			[PXUnboundDefault(typeof(SearchFor<PRTaxCode.taxCD>.Where<PRTaxCode.taxID.IsEqual<taxID.FromCurrent>>))]
			public string TaxCD { get; set; }
			#endregion
		}

		[PXHidden]
		public class PRTaxDetailExt : PRTaxDetail
		{
			#region TaxCD
			[PXString]
			[PXUnboundDefault(typeof(SearchFor<PRTaxCode.taxCD>.Where<PRTaxCode.taxID.IsEqual<taxID.FromCurrent>>))]
			public string TaxCD { get; set; }
			#endregion
		}

		[PXHidden]
		public class PRPaymentTaxExt : PRPaymentTax
		{
			#region TaxCD
			[PXString]
			[PXUnboundDefault(typeof(SearchFor<PRTaxCode.taxCD>.Where<PRTaxCode.taxID.IsEqual<taxID.FromCurrent>>))]
			public string TaxCD { get; set; }
			#endregion
		}

		private class DeductionDictionary : Dictionary<int?, PRDeductCode>
		{
			public new PRDeductCode this[int? key]
			{
				get
				{
					if (!ContainsKey(key))
					{
						throw new PXException(Messages.CannotFindDeductCode, key);
					}

					return base[key];
				}
				set => base[key] = value;
			}
		}

		#region Avoid breaking changes in 2020R2
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		protected void ValidateDirectDepositSplits() 
		{
			ValidateDirectDepositSplits(false);
		}
		#endregion Avoid breaking changes in 2020R2
	}
}
