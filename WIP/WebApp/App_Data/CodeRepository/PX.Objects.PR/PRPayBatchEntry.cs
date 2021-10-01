using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;

namespace PX.Objects.PR
{
	public class PRPayBatchEntry : PXGraph<PRPayBatchEntry, PRBatch>, PXImportAttribute.IPXPrepareItems
	{
		#region Views
		//The three "Dummy" views below are needed to avoid "Invalid column name" error with a list of existing fields of the PREmployee table/DAC.
		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXSelect<BAccount> DummyBAccountView;
		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXSelect<Vendor> DummyVendorView;
		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXSelect<EPEmployee> DummyEmployeeView;

		public SelectFrom<PRBatch>.Where<MatchWithPayGroup<PRBatch.payGroupID>>.View Document;
		public SelectFrom<PRBatch>.Where<PRBatch.batchNbr.IsEqual<PRBatch.batchNbr.AsOptional>>.View CurrentDocument;

		public SelectFrom<PRBatchEmployeeExt>.
			Where<MatchWithBranch<PRBatchEmployee.branchID>.
				And<MatchWithPayGroup<PRBatchEmployee.payGroupID>.
				And<PRBatchEmployee.batchNbr.IsEqual<PRBatch.batchNbr.FromCurrent>>>>.View Transactions;

		public SelectFrom<PRBatchEmployee>.
			Where<PRBatchEmployee.batchNbr.IsEqual<PRBatchEmployee.batchNbr.AsOptional>.
				And<PRBatchEmployee.employeeID.IsEqual<PRBatchEmployee.employeeID.AsOptional>>>.View CurrentTransaction;

		public PXSelect<PREarningDetail,
			Where<PREarningDetail.batchNbr, Equal<Current<PRBatchEmployee.batchNbr>>,
				And<PREarningDetail.employeeID, Equal<Optional<PRBatchEmployee.employeeID>>,
				And<PREarningDetail.isFringeRateEarning, Equal<False>,
				And<Where<PREarningDetail.paymentDocType, IsNull, Or<PREarningDetail.paymentDocType, NotEqual<PayrollType.voidCheck>>>>>>>,
			OrderBy<Asc<PREarningDetail.date, Asc<PREarningDetail.sortingRecordID, Asc<PREarningDetail.rate>>>>>
			EmployeeEarningDetails;

		[PXImport(typeof(PRBatch))]
		public PXSelect<PREarningDetail,
			Where<PREarningDetail.batchNbr, Equal<Current<PRBatch.batchNbr>>,
				And<PREarningDetail.isFringeRateEarning, Equal<False>,
				And<Where<PREarningDetail.paymentDocType, IsNull, Or<PREarningDetail.paymentDocType, NotEqual<PayrollType.voidCheck>>>>>>,
			OrderBy<Asc<PREarningDetail.employeeID, Asc<PREarningDetail.date, Asc<PREarningDetail.sortingRecordID, Asc<PREarningDetail.rate>>>>>>
			EarningDetails;

		public PXSelect<PREarningDetail,
			Where<PREarningDetail.batchNbr, Equal<Current<PRBatchEmployee.batchNbr>>,
				And<PREarningDetail.isFringeRateEarning, Equal<False>,
				And<Where<PREarningDetail.paymentDocType, IsNull, 
					Or<PREarningDetail.paymentDocType, NotEqual<PayrollType.voidCheck>>>>>>,
			OrderBy<Asc<PREarningDetail.date, Asc<PREarningDetail.sortingRecordID, Asc<PREarningDetail.rate>>>>> AllEarningDetails;

		public SelectFrom<PRBatchDeduct>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRBatchDeduct.codeID>>
			.Where<PRBatchDeduct.batchNbr.IsEqual<PRBatch.batchNbr.FromCurrent>>.View Deductions;

		public SelectFrom<PRBatchOvertimeRule>.InnerJoin<PROvertimeRule>.
			On<PRBatchOvertimeRule.overtimeRuleID.IsEqual<PROvertimeRule.overtimeRuleID>>.
			Where<PRBatchOvertimeRule.batchNbr.IsEqual<PRBatch.batchNbr.FromCurrent>>.View BatchOvertimeRules;

		public SelectFrom<PRPayment>.Where<PRPayment.released.IsEqual<False>.
			And<PRPayment.payBatchNbr.IsEqual<P.AsString>>>.View NonReleasedPayBatchPayments;

		public PXFilter<TaxUpdateHelpers.UpdateTaxesWarning> UpdateTaxesPopupView;
		public SelectFrom<PRTaxUpdateHistory>.View UpdateHistory;
		#endregion

		#region Data View Delegates

		protected virtual IEnumerable transactions()
		{
			if (Document.Current == null)
			{
				return null;
			}

			PXView negativeHoursEarningsView = new PXView(this, false, EarningDetails.View.BqlSelect);
			negativeHoursEarningsView.WhereAnd(typeof(Where<PREarningDetail.hours.IsLess<decimal0>>));
			HashSet<int?> employeesWithNegativeHoursEarnings = negativeHoursEarningsView.SelectMulti().Select(x => ((PREarningDetail)x).EmployeeID).ToHashSet();

			PXView query = new PXView(this, false, Transactions.View.BqlSelect);
			List<PRBatchEmployeeExt> result = new List<PRBatchEmployeeExt>();
			bool voidPayCheckExist = false;

			int totalRows = 0;
			int startRow = PXView.StartRow;
			foreach (PRBatchEmployeeExt batchEmployee in query.Select(PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns,
				PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows))
			{
				if (Document.Current.Status != BatchStatus.Hold && Document.Current.Status != BatchStatus.Balanced)
				{
					IEnumerable<PRPayment> payments = SelectFrom<PRPayment>.
						Where<PRPayment.payBatchNbr.IsEqual<PRBatch.batchNbr.FromCurrent>.
							And<PRPayment.employeeID.IsEqual<P.AsInt>>>.View.Select(this, batchEmployee.EmployeeID).FirstTableItems;

					foreach (PRPayment payment in payments)
					{
						batchEmployee.PaymentRefNbr = payment.RefNbr;

						if (payment.DocType == Document.Current.PayrollType)
							batchEmployee.PaymentDocAndRef = payment.PaymentDocAndRef;

						if (payment.DocType == PayrollType.VoidCheck)
						{
							batchEmployee.VoidPaymentDocAndRef = payment.PaymentDocAndRef;
							voidPayCheckExist = true;
						}
					}
				}

				batchEmployee.HasNegativeHoursEarnings = employeesWithNegativeHoursEarnings.Contains(batchEmployee.EmployeeID);
				result.Add(batchEmployee);
			}

			if (PXView.MaximumRows == 0 && PXLongOperation.GetStatus(UID) == PXLongRunStatus.NotExists)
			{
			if (result.Count == Document.Current.NumberOfEmployees.GetValueOrDefault())
			{
				PRBatchTotalsFilter.Current.HideTotals = false;
				PRBatchTotalsFilter.Current.TotalEarnings = Document.Current.TotalEarnings;
				PRBatchTotalsFilter.Current.TotalHourQty = Document.Current.TotalHourQty;
			}
			else
			{
				PRBatchTotalsFilter.Current.HideTotals = true;
				PRBatchTotalsFilter.Current.TotalEarnings = null;
				PRBatchTotalsFilter.Current.TotalHourQty = null;
				PXUIFieldAttribute.SetWarning<PRBatchTotalsFilter.totalHourQty>(PRBatchTotalsFilter.View.Cache, PRBatchTotalsFilter.Current, Messages.NoAccessToAllEmployeesInPRBatch);
				PXUIFieldAttribute.SetWarning<PRBatchTotalsFilter.totalEarnings>(PRBatchTotalsFilter.View.Cache, PRBatchTotalsFilter.Current, Messages.NoAccessToAllEmployeesInPRBatch);
			}
			Document.Cache.RaiseRowSelected(Document.Current);
			}
			
			PXUIFieldAttribute.SetVisible<PRBatchEmployeeExt.voidPaymentDocAndRef>(Transactions.Cache, null, voidPayCheckExist);

			PXView.StartRow = 0;
			return result;
		}

		protected virtual IEnumerable employees()
		{
			int startRow = PXView.StartRow;
			int totalRows = 0;

			PXView query = new PXView(this, false, Employees.View.BqlSelect);
			List<PXView.PXSearchColumn> searchColumns = Employees.View.GetContextualExternalSearchColumns();
			PXFilterRow[] externalFilters = Employees.View.GetExternalFilters();

			IEnumerable<PXResult<PREmployee, GL.Branch>> filteredEmployees = query.Select(PXView.Currents, PXView.Parameters,
				searchColumns.GetSearches(), searchColumns.GetSortColumns(), searchColumns.GetDescendings(),
				externalFilters, ref startRow, PXView.MaximumRows, ref totalRows).
				Select(x => (PXResult<PREmployee, GL.Branch>)x);

			PXView.StartRow = 0;

			bool selectedEmployeesExist =
				filteredEmployees.Any(item => ((PREmployee)item)?.Selected == true);

			AddEmployeeFilter.Current.SelectedEmployeesExist = selectedEmployeesExist;
			AddSelectedEmployees.SetEnabled(selectedEmployeesExist);
			AddSelectedEmployeesAndClose.SetEnabled(selectedEmployeesExist);

			return filteredEmployees;
		}

		#endregion

		#region Populate Earning Details Dataviews
		[PXHidden]
		public PXSelect<PREarningDetail, Where<PREarningDetail.batchNbr, Equal<Current<PRBatch.batchNbr>>>> TransactionDetails;

		public PXSelectJoin<
			ARSPCommnHistory,
			InnerJoin<ARSPCommissionPeriod,
				On<ARSPCommissionPeriod.commnPeriodID, Equal<ARSPCommnHistory.commnPeriod>>>,
			Where<ARSPCommnHistory.pRProcessedDate, IsNull,
				And<ARSPCommissionPeriod.status, Equal<Required<ARSPCommissionPeriod.status>>,
				And<ARSPCommnHistory.salesPersonID, Equal<Required<ARSPCommnHistory.salesPersonID>>>>>>
			CommissionHistory;
		#endregion

		#region Preferences
		public PXSetup<PRSetup> Preferences;
		#endregion

		#region CacheAttached
		[EmployeeActiveInPayrollBatch(IsKey = true)]
		[PXDBDefault(typeof(PRBatchEmployee.employeeID))]
		[PXParent(typeof(Select<
			PRBatchEmployee, 
			Where<PRBatchEmployee.batchNbr, Equal<Current<PREarningDetail.batchNbr>>,
				And<PRBatchEmployee.employeeID, Equal<Current<PREarningDetail.employeeID>>,
				And<Where<Current<PREarningDetail.paymentDocType>, IsNull, 
					Or<Current<PREarningDetail.paymentDocType>, NotEqual<PayrollType.voidCheck>>>>>>>))]
		protected virtual void _(Events.CacheAttached<PREarningDetail.employeeID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Employee", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void _(Events.CacheAttached<PREmployee.acctCD> e) { }

		[PXDBString(3)]
		[PXUIField(DisplayName = "Deduction Calculation Method")]
		[DedCntCalculationMethod.List]
		protected void _(Events.CacheAttached<PRDeductCode.dedCalcType> e) { }

		[PRCurrency]
		[PXUIField(DisplayName = "Deduction Amount")]
		protected void _(Events.CacheAttached<PRDeductCode.dedAmount> e) { }

		[PXDBDecimal]
		[PXUIField(DisplayName = "Deduction Percent")]
		protected void _(Events.CacheAttached<PRDeductCode.dedPercent> e) { }

		[PXDBString(3)]
		[PXUIField(DisplayName = "Benefit Calculation Method")]
		[DedCntCalculationMethod.List]
		protected void _(Events.CacheAttached<PRDeductCode.cntCalcType> e) { }

		[PRCurrency]
		[PXUIField(DisplayName = "Benefit Amount")]
		protected void _(Events.CacheAttached<PRDeductCode.cntAmount> e) { }

		[PXDBDecimal]
		[PXUIField(DisplayName = "Benefit Percent")]
		protected void _(Events.CacheAttached<PRDeductCode.cntPercent> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDBDateAttribute))]
		[DateInPeriod(typeof(PRBatch), typeof(PRBatch.startDate), typeof(PRBatch.endDate), nameof(EmployeeEarningDetails))]
		protected virtual void _(Events.CacheAttached<PREarningDetail.date> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(EarningsAccountAttribute), nameof(EarningsAccountAttribute.PayGroupField), typeof(PRBatch.payGroupID))]
		protected virtual void _(Events.CacheAttached<PREarningDetail.accountID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(EarningSubAccountAttribute), nameof(EarningSubAccountAttribute.EmployeeIDField), typeof(PREarningDetail.employeeID))]
		[PXCustomizeBaseAttribute(typeof(EarningSubAccountAttribute), nameof(EarningSubAccountAttribute.PayGroupIDField), typeof(PRBatch.payGroupID))]
		protected virtual void _(Events.CacheAttached<PREarningDetail.subID> e) { }
		
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[GL.Branch(typeof(SelectFrom<GL.Branch>.InnerJoin<EPEmployee>.On<GL.Branch.bAccountID.IsEqual<EPEmployee.parentBAccountID>>.
			Where<EPEmployee.bAccountID.IsEqual<PREarningDetail.employeeID.FromCurrent>>.SearchFor<GL.Branch.branchID>), IsDetail = false)]
		[PXFormula(typeof(Default<PREarningDetail.employeeID>))]
		protected virtual void _(Events.CacheAttached<PREarningDetail.branchID> e) { }
		#endregion

		#region Event Handlers

		protected virtual void _(Events.RowSelected<PRBatch> e)
		{
			PRBatch currentBatch = Document.Current;

			bool fullBatchVisible = PRBatchTotalsFilter.Current.HideTotals != true;

			bool addEmployeeActionEnabled =
				currentBatch != null &&
				!string.IsNullOrWhiteSpace(currentBatch.PayrollType) &&
				!string.IsNullOrWhiteSpace(currentBatch.PayGroupID) &&
				!string.IsNullOrWhiteSpace(currentBatch.PayPeriodID) &&
				currentBatch.StartDate != null &&
				currentBatch.EndDate != null &&
				currentBatch.Status != BatchStatus.Open &&
				currentBatch.Status != BatchStatus.Closed;
			AddEmployees.SetEnabled(addEmployeeActionEnabled);

			bool releaseButtonEnabled =
				currentBatch != null &&
				currentBatch.Status == BatchStatus.Balanced &&
				currentBatch.NumberOfEmployees > 0 &&
				fullBatchVisible;

			bool allowBatchModification =
				currentBatch != null &&
				(currentBatch.Status == BatchStatus.Hold ||
				 currentBatch.Status == BatchStatus.Balanced);

			Delete.SetEnabled(allowBatchModification && fullBatchVisible);
			Document.AllowDelete = allowBatchModification && fullBatchVisible;
			Document.AllowUpdate = allowBatchModification;

			Transactions.AllowDelete = allowBatchModification;

			EarningDetails.AllowInsert = addEmployeeActionEnabled;
			EarningDetails.AllowUpdate = allowBatchModification;
			EarningDetails.AllowDelete = allowBatchModification;

			EmployeeEarningDetails.AllowInsert = allowBatchModification;
			EmployeeEarningDetails.AllowUpdate = allowBatchModification;
			EmployeeEarningDetails.AllowDelete = allowBatchModification;

			CopySelectedEarningDetailLine.SetEnabled(allowBatchModification);

			BatchOvertimeRules.AllowUpdate = allowBatchModification;

			Release.SetEnabled(releaseButtonEnabled);
			CheckTaxUpdateTimestamp.SetEnabled(true);
			RedirectTaxMaintenance.SetEnabled(true);
		}

		protected virtual void _(Events.RowSelected<PRBatchEmployee> e)
		{
			PRBatchEmployeeExt currentBatchEmployee = e.Row as PRBatchEmployeeExt;
			ViewEarningDetails.SetEnabled(currentBatchEmployee != null);

			if (currentBatchEmployee?.HasNegativeHoursEarnings == true)
			{
				string error = string.Format(Messages.NegativeHoursEarnings, currentBatchEmployee.AcctCD);
				PXUIFieldAttribute.SetWarning<PRBatchEmployeeExt.hourQty>(e.Cache, e.Row, error);
			}
			else
			{
				PXUIFieldAttribute.SetWarning<PRBatchEmployeeExt.hourQty>(e.Cache, e.Row, null);
			}
		}

		protected virtual void _(Events.RowSelected<PRBatchOvertimeRule> e) 
		{
			if (e.Row == null || !BatchOvertimeRules.AllowUpdate)
				return;

			bool overtimeRuleEnabled = Document.Current.IsWeeklyOrBiWeeklyPeriod == true || e.Row.RuleType == PROvertimeRuleType.Daily;
			PXUIFieldAttribute.SetEnabled(BatchOvertimeRules.Cache, e.Row, overtimeRuleEnabled);
			if (!overtimeRuleEnabled)
				PXUIFieldAttribute.SetWarning<PROvertimeRule.overtimeRuleID>(BatchOvertimeRules.Cache, e.Row, Messages.WeeklyOvertimeRulesApplyToWeeklyPeriods);
		}

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		protected virtual void _(Events.RowSelected<EPEmployee> e)
		{ 
		}
		
		protected virtual void _(Events.RowSelected<PREmployee> e)
		{
			PREmployee currentEmployee = e.Row;
			if (currentEmployee?.BAccountID == null)
				return;

			NonSelectableEmployee nonSelectableEmployee =
				NonSelectableEmployees.Cache.Cached.Cast<NonSelectableEmployee>().FirstOrDefault(item => item.EmployeeID == currentEmployee.BAccountID);

			if (!string.IsNullOrWhiteSpace(nonSelectableEmployee?.ErrorMessage))
			{
				PXUIFieldAttribute.SetEnabled(Employees.Cache, currentEmployee, false);
				PXUIFieldAttribute.SetWarning<PREmployee.acctName>(Employees.Cache, currentEmployee, nonSelectableEmployee.ErrorMessage);
			}
		}

		protected virtual void _(Events.RowSelected<AddEmployeeFilter> e)
		{
			bool isRegularHoursTypeSetUp = !string.IsNullOrWhiteSpace(Preferences.Current.RegularHoursType);
			bool isHolidaysTypeSetUp = !string.IsNullOrWhiteSpace(Preferences.Current.HolidaysType);
			bool useQuickPayEnabled = isRegularHoursTypeSetUp && isHolidaysTypeSetUp;
			bool useSalesCommissionsEnabled = !string.IsNullOrWhiteSpace(Preferences.Current.CommissionType);

			PXUIFieldAttribute.SetEnabled<AddEmployeeFilter.useQuickPay>(e.Cache, e.Row, useQuickPayEnabled);
			if (!isRegularHoursTypeSetUp && !isHolidaysTypeSetUp)
				PXUIFieldAttribute.SetWarning<AddEmployeeFilter.useQuickPay>(e.Cache, e.Row, Messages.RegularAndHolidaysTypesAreNotSetUp);
			else if (!isRegularHoursTypeSetUp)
				PXUIFieldAttribute.SetWarning<AddEmployeeFilter.useQuickPay>(e.Cache, e.Row, Messages.RegularHoursTypeIsNotSetUp);
			else if (!isHolidaysTypeSetUp)
				PXUIFieldAttribute.SetWarning<AddEmployeeFilter.useQuickPay>(e.Cache, e.Row, Messages.HolidaysTypeIsNotSetUp);

			PXUIFieldAttribute.SetEnabled<AddEmployeeFilter.useSalesComm>(e.Cache, e.Row, useSalesCommissionsEnabled);
			if (!useSalesCommissionsEnabled)
				PXUIFieldAttribute.SetWarning<AddEmployeeFilter.useSalesComm>(e.Cache, e.Row, Messages.CommissionTypeIsNotSetUp);
		}

		protected virtual void _(Events.RowSelected<PREarningDetail> e)
		{
			PXUIFieldAttribute.SetWarning<PREarningDetail.hours>(e.Cache, e.Row, e.Row?.Hours < 0 ? Messages.InvalidNegative : null);
		}

		protected virtual void _(Events.FieldUpdated<EPEmployee.selected> e)
		{
			if (e.ExternalCall)
			{
				AddEmployeeFilter.Current.SelectedEmployeesExist =
					e.NewValue as bool? == true ||
					Employees.Select().FirstTableItems.Any(item => item.Selected == true);
			}
		}

		protected virtual void _(Events.FieldUpdated<PRBatch.applyOvertimeRules> e)
		{
			bool applyOvertimeRules = e.NewValue as bool? ?? false;

			if (applyOvertimeRules)
				ReinsertBatchOvertimeRules();
			else
				DeleteBatchOvertimeRules();
		}

		protected virtual void _(Events.FieldUpdated<PRBatch.payPeriodID> e)
		{
			var row = e.Row as PRBatch;
			if (row == null)
			{
				return;
			}

			ReinsertBatchDeductions();
			if (row.ApplyOvertimeRules == true)
			{
				ReinsertBatchOvertimeRules();
			}
		}

		protected virtual void _(Events.FieldUpdated<PRBatch.payGroupID> e)
		{
			var row = e.Row as PRBatch;
			if (row == null)
				return;

			e.Cache.SetDefaultExt<PRBatch.payPeriodID>(row);
		}

		protected virtual void _(Events.RowUpdated<PRBatchEmployee> e)
		{
			if (Document.Cache.Deleted.OfType<PRBatch>().Any(payrollBatch => payrollBatch.BatchNbr == e.Row?.BatchNbr))
				return;

			PRBatch batch = Document.Current;

			if (e.ExternalCall)
			{
				PXFormulaAttribute.CalcAggregate<PRBatchEmployee.amount>(Transactions.Cache, batch);
				Document.Update(batch);
			}
		}

		protected virtual void _(Events.FieldVerifying<PREarningDetail.employeeID> e)
		{
			if (!e.ExternalCall || TrySelectPayrollBatchEmployee(e.NewValue as int?, out string errorMessage))
				return;

			e.Cancel = true;
			e.NewValue = null;
			e.Cache.RaiseExceptionHandling<PREarningDetail.employeeID>(e.Row, e.NewValue, new PXSetPropertyException(errorMessage, PXErrorLevel.Error));
		}

		protected virtual void _(Events.FieldUpdated<PREarningDetail.employeeID> e)
		{
			if (!IsImportFromExcel)
				e.Cache.SetDefaultExt<PREarningDetail.locationID>(e.Row);

			if (!e.ExternalCall)
				return;

			AddPayrollBatchEmployeeIfNotExists(e.NewValue as int?);
		}

		protected virtual void _(Events.FieldUpdated<PREarningDetail.projectID> e)
		{
			if (!IsImportFromExcel)
				e.Cache.SetDefaultExt<PREarningDetail.locationID>(e.Row);
		}

		protected virtual void _(Events.FieldUpdating<PRBatchEmployee.employeeID> e)
		{
			if (IsImport && e.NewValue != null && e.NewValue is string acctCD)
			{
				int? employeeID = SelectFrom<PREmployee>.Where<PREmployee.acctCD.IsEqual<P.AsString>>.View.Select(this, acctCD).TopFirst?.BAccountID;

				if (employeeID != null)
					e.NewValue = employeeID;
			}
		}

		public void _(Events.FieldVerifying<PREarningDetail.amount> e)
		{
			PREarningType earningType = (PXSelectorAttribute.Select(e.Cache, e.Row, nameof(PREarningDetail.typeCD)) as EPEarningType)?.GetExtension<PREarningType>();
			if (earningType?.IsAmountBased == true)
			{
				CheckForNegative<PREarningDetail.amount>(e.NewValue as decimal?);
			}
		}

		public void _(Events.FieldVerifying<PREarningDetail.units> e)
		{
			CheckForNegative<PREarningDetail.units>(e.NewValue as decimal?);
		}

		public void _(Events.FieldVerifying<PREarningDetail.rate> e)
		{
			CheckForNegative<PREarningDetail.rate>(e.NewValue as decimal?);
		}

		public void _(Events.FieldSelecting<PRDeductCode.dedCalcType> e)
		{
			PRDeductCode row = e.Row as PRDeductCode;
			if (row == null || row.ContribType != ContributionType.EmployerContribution)
			{
				return;
			}

			e.ReturnValue = null;
		}

		public void _(Events.FieldSelecting<PRDeductCode.cntCalcType> e)
		{
			PRDeductCode row = e.Row as PRDeductCode;
			if (row == null || row.ContribType != ContributionType.EmployeeDeduction)
			{
				return;
			}

			e.ReturnValue = null;
		}

		#region PXImportAttribute.IPXPrepareItems
		public virtual bool PrepareImportRow(string viewName, IDictionary keys, IDictionary values)
		{
			if (viewName == nameof(EarningDetails))
				return PrepareEarningDetailImport(values);

			return true;
		}

		public virtual bool RowImporting(string viewName, object row)
		{
			// row is always null
			return true;
		}

		public virtual bool RowImported(string viewName, object row, object oldRow)
		{
			return true;
		}

		public virtual void PrepareItems(string viewName, IEnumerable items)
		{
			// https://asiablog.acumatica.com/2016/12/enabling-upload-from-excel-for-the-grid.html
			// "PrepareItems – Used to review items before import, but right now does not execute for performance optimizations."
		}
		#endregion PXImportAttribute.IPXPrepareItems

		#region Event Handler Helpers
		private void ReinsertBatchDeductions()
		{
			Deductions.Select().ForEach(x => Deductions.Delete(x));
			foreach (PRDeductCode deductCode in SelectFrom<PRDeductCode>.Where<PRDeductCode.isActive.IsEqual<True>
				.And<PRDeductCode.isWorkersCompensation.IsEqual<False>>
				.And<PRDeductCode.isCertifiedProject.IsEqual<False>>
				.And<PRDeductCode.isUnion.IsEqual<False>>>
				.View.Select(this))
			{
				var batchDeduct = new PRBatchDeduct()
				{
					CodeID = deductCode.CodeID
				};
				Deductions.Insert(batchDeduct);
			}
		}

		private void ReinsertBatchOvertimeRules()
		{
			PXResultset<PROvertimeRule> activeOvertimeRules = 
				SelectFrom<PROvertimeRule>.
				Where<PROvertimeRule.isActive.IsEqual<True>>.View.Select(this);

			bool weeklyOvertimeRulesAllowed = Document.Current.IsWeeklyOrBiWeeklyPeriod == true;
			DeleteBatchOvertimeRules();
			foreach (PROvertimeRule overtimeRule in activeOvertimeRules)
			{
				PRBatchOvertimeRule batchOvertimeRule = new PRBatchOvertimeRule
				{
					OvertimeRuleID = overtimeRule.OvertimeRuleID,
					IsActive = weeklyOvertimeRulesAllowed || overtimeRule.RuleType == PROvertimeRuleType.Daily
				};
				BatchOvertimeRules.Update(batchOvertimeRule);
			}
		}

		private void DeleteBatchOvertimeRules()
		{
			BatchOvertimeRules.Select().ForEach(batchOvertimeRule => BatchOvertimeRules.Delete(batchOvertimeRule));
		}

		public virtual void CheckForNegative<TField>(decimal? newValue, string message = Messages.InvalidNegative) where TField : IBqlField
		{
			if (newValue < 0)
			{
				throw new PXSetPropertyException<TField>(message, PXErrorLevel.Error);
			}
		}

		protected virtual bool PrepareEarningDetailImport(IDictionary values)
		{
			string acctCD = values[nameof(PREarningDetail.EmployeeID)] as string;

			if (!CheckEmployeeForImport(acctCD, out string errorMessage))
			{
				PXTrace.WriteError(errorMessage);
				return false;
			}

			if (values.Contains(nameof(PREarningDetail.ManualRate)))
			{
				values.Remove(nameof(PREarningDetail.ManualRate));
			}

			return true;
		}

		protected virtual bool CheckEmployeeForImport(string acctCD, out string errorMessage)
		{
			errorMessage = null;
			if (string.IsNullOrEmpty(acctCD))
			{
				errorMessage = Messages.EmployeeIDCannotBeNull;
				return false;
			}

			PREmployee payrollEmployee = Employees.Search<PREmployee.acctCD>(acctCD);

			if (payrollEmployee == null)
			{
				errorMessage =  string.Format(Messages.EmployeeCannotBeAddedToPayrollBatch, acctCD);
				return false;
			}

			if (!TrySelectPayrollBatchEmployee(payrollEmployee.BAccountID, out errorMessage))
				return false;

			AddPayrollBatchEmployeeIfNotExists(payrollEmployee.BAccountID);
			return true;
		}

		protected virtual void AddPayrollBatchEmployeeIfNotExists(int? employeeID)
		{
			if (employeeID == null || Transactions.Search<PRBatchEmployee.employeeID>(employeeID).TopFirst != null)
				return;

			PRBatchEmployeeExt newPRBatchEmployeeRecord = new PRBatchEmployeeExt();
			newPRBatchEmployeeRecord.EmployeeID = employeeID;
			newPRBatchEmployeeRecord = Transactions.Insert(newPRBatchEmployeeRecord);
		}

		protected virtual bool TrySelectPayrollBatchEmployee(int? employeeID, out string errorMessage)
		{
			errorMessage = null;
			if (employeeID == null)
				return true;

			if (Document.Current.PayrollType != PayrollType.Regular)
				return true;

			PRBatchEmployee existingPRBatch = GetExistingRegularPayrollBatches(new[] { employeeID }).FirstOrDefault();
			if (existingPRBatch != null)
			{
				errorMessage = string.Format(Messages.EmployeeAlreadyAddedToAnotherBatch, existingPRBatch.BatchNbr);
				return false;
			}

			PRPayment existingPRPayment = GetExistingRegularPayments(new[] { employeeID }).FirstOrDefault();
			if (existingPRPayment != null)
			{
				errorMessage = string.Format(Messages.EmployeeAlreadyAddedToPaycheck, existingPRPayment.PaymentDocAndRef);
				return false;
			}

			return true;
		}

		#endregion
		#endregion

		#region Actions
		public PXAction<PRBatch> Release;
		[PXUIField(DisplayName = "Release", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton()]
		public virtual IEnumerable release(PXAdapter adapter)
		{
			Actions.PressSave();
			PRBatch currentBatch = Document.Current;
			if (currentBatch.Status != BatchStatus.Balanced)
				yield break;

			foreach(PREarningDetail earning in EarningDetails.Select())
			{
				CheckForNegative<PREarningDetail.hours>(earning.Hours, Messages.CantReleaseWithNegativeHours);
			}

			PXLongOperation.StartOperation(this, () =>
			{
				using (PXTransactionScope transactionScope = new PXTransactionScope())
				{
					CreatePayments(currentBatch);

					currentBatch.Open = true;
					Document.Update(currentBatch);
					if (Caches[typeof(PRPayment)] != null)
					{
						Caches[typeof(PRPayment)].Clear();
						Caches[typeof(PRPayment)].ClearQueryCache();
					}
					Actions.PressSave();
					transactionScope.Complete(this);
				}
			});

			yield return Document.Current;
		}

		private void CreatePayments(PRBatch currentBatch)
		{
			List<PRBatchEmployeeExt> employeesInCurrent = Transactions.Select().FirstTableItems.ToList();
			List<PRPayment> existingPayments = GetExistingRegularPayments(employeesInCurrent.Select(x => x.EmployeeID).ToArray());
			foreach (PRBatchEmployee batchEmployee in employeesInCurrent)
			{
				PRPayment existingPayment = existingPayments.FirstOrDefault(x => x.EmployeeID == batchEmployee.EmployeeID);
				if (existingPayment != null)
				{
					EPEmployee currentEmployee = 
						SelectFrom<EPEmployee>.Where<EPEmployee.bAccountID.IsEqual<P.AsInt>>.View.
						SelectSingleBound(this, null, batchEmployee.EmployeeID);

					throw new PXException(Messages.EmployeeAlreadyAddedToPaycheckBatchRelease, 
						existingPayment.PaymentDocAndRef, string.Format("{0}:{1}", currentEmployee.AcctCD, currentEmployee.AcctName));
				}

				PRPayment payment = new PRPayment();

				payment.PayBatchNbr = currentBatch.BatchNbr;
				payment.DocType = currentBatch.PayrollType;
				payment.Hold = Preferences.Current.HoldEntry;
				payment.Released = false;
				payment.PayGroupID = currentBatch.PayGroupID;
				payment.PayPeriodID = currentBatch.PayPeriodID;
				payment.StartDate = currentBatch.StartDate;
				payment.TransactionDate = currentBatch.TransactionDate;

				payment.EmployeeID = batchEmployee.EmployeeID;
				payment.SalariedNonExempt = batchEmployee.SalariedNonExempt;
				payment.RegularAmount = batchEmployee.RegularAmount;
				payment.ManualRegularAmount = batchEmployee.ManualRegularAmount;

				PRPayChecksAndAdjustments payCheckGraph = PXGraph.CreateInstance<PRPayChecksAndAdjustments>();
				payment = payCheckGraph.InsertNewPayment(payment);

				AttachEarningDetailsToPayment(batchEmployee, payment, payCheckGraph);
				payCheckGraph.Actions.PressSave();

				var employeeEarningDetails = new Dictionary<(string TypeCD, int? LocationID), PREarningDetail[]>();

				foreach (IGrouping<(string TypeCD, int? LocationID), PREarningDetail> groupItem in 
					EmployeeEarningDetails.Select(batchEmployee.EmployeeID).FirstTableItems.GroupBy(item => (item.TypeCD, item.LocationID)).ToArray())
				{
					employeeEarningDetails[groupItem.Key] = groupItem.Select(item => item).ToArray();
				}

				foreach (PRPaymentEarning paymentEarning in payCheckGraph.SummaryEarnings.Select().FirstTableItems)
				{
					if (employeeEarningDetails.TryGetValue((paymentEarning.TypeCD, paymentEarning.LocationID), out PREarningDetail[] childrenEarningDetails))
					{
						paymentEarning.Rate = PaymentEarningRateAttribute.GetPaymentEarningRate(paymentEarning, childrenEarningDetails);
						payCheckGraph.SummaryEarnings.Update(paymentEarning);
					}
				}
				payCheckGraph.Actions.PressSave();
			}
		}

		private List<PRPayment> GetExistingRegularPayments(int?[] employeeIDs)
		{
			if (Document.Current.PayrollType != PayrollType.Regular)
				return new List<PRPayment>();

			return SelectFrom<PRPayment>.
				Where<PRPayment.docType.IsEqual<PayrollType.regular>.
					And<PRPayment.payPeriodID.IsEqual<PRBatch.payPeriodID.FromCurrent>>.
					And<PRPayment.payGroupID.IsEqual<PRBatch.payGroupID.FromCurrent>>.
					And<PRPayment.voided.IsNotEqual<True>>.
					And<PRPayment.employeeID.IsIn<P.AsInt>>>.View.
				Select(this, employeeIDs).FirstTableItems.ToList();
		}

		private List<PRBatchEmployee> GetExistingRegularPayrollBatches(int?[] employeeIDsInPayGroup)
		{
			if (Document.Current.PayrollType != PayrollType.Regular)
				return new List<PRBatchEmployee>();

			return SelectFrom<PRBatchEmployee>.
				InnerJoin<PRBatch>.On<PRBatch.batchNbr.IsEqual<PRBatchEmployee.batchNbr>>.
				Where<PRBatch.batchNbr.IsNotEqual<PRBatch.batchNbr.FromCurrent>.
					And<PRBatch.open.IsNotEqual<True>>.
					And<PRBatch.closed.IsNotEqual<True>>.
					And<PRBatch.payrollType.IsEqual<PayrollType.regular>>.
					And<PRBatch.payPeriodID.IsEqual<PRBatch.payPeriodID.FromCurrent>>.
					And<PRBatch.payGroupID.IsEqual<PRBatch.payGroupID.FromCurrent>>.
					And<PRBatchEmployee.employeeID.IsIn<P.AsInt>>>.View.
				Select(this, employeeIDsInPayGroup).FirstTableItems.ToList();
		}

		private void AttachEarningDetailsToPayment(PRBatchEmployee batchEmployee, PRPayment payment, PRPayChecksAndAdjustments payCheckGraph)
		{
			foreach (PREarningDetail earningDetail in EmployeeEarningDetails.Select(batchEmployee.EmployeeID))
			{
				earningDetail.PaymentDocType = payment.DocType;
				earningDetail.PaymentRefNbr = payment.RefNbr;

				PREarningDetail copy = (PREarningDetail)EmployeeEarningDetails.Cache.CreateCopy(earningDetail);
				payCheckGraph.Earnings.Cache.RaiseRowInserted(earningDetail);
				EmployeeEarningDetails.Cache.RestoreCopy(earningDetail, copy);
				EmployeeEarningDetails.Update(earningDetail);
			}
		}

		public PXAction<PRBatch> ViewEarningDetails;
		[PXUIField(DisplayName = "Employee Earning Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable viewEarningDetails(PXAdapter adapter)
		{
			if (EmployeeEarningDetails.AskExt() == WebDialogResult.OK)
			{
				return adapter.Get();
			}

			EmployeeEarningDetails.Cache.Clear();
			return adapter.Get();
		}

		public PXAction<PRBatch> ViewPayCheck;
		[PXUIField(DisplayName = "View Paycheck", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		protected virtual void viewPayCheck()
		{
			ViewEmployeePaycheck(false);
		}
		
		public PXAction<PRBatch> ViewVoidPayCheck;
		[PXUIField(DisplayName = "View Void Paycheck", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		protected virtual void viewVoidPayCheck()
		{
			ViewEmployeePaycheck(true);
		}
		
		public PXAction<PRBatch> CopySelectedEarningDetailLine;
		[PXUIField(DisplayName = "Copy Selected Entry", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable copySelectedEarningDetailLine(PXAdapter adapter)
		{
			return CopySelectedEarningDetailRecord(adapter, EarningDetails.Cache);
		}

		public PXAction<PRBatch> CopySelectedEmployeeEarningDetailLine;
		[PXUIField(DisplayName = "Copy Selected Entry", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable copySelectedEmployeeEarningDetailLine(PXAdapter adapter)
		{
			return CopySelectedEarningDetailRecord(adapter, EmployeeEarningDetails.Cache);
		}

		protected virtual IEnumerable CopySelectedEarningDetailRecord(PXAdapter adapter, PXCache earningDetailsCache)
		{
			RegularAmountAttribute.EnforceEarningDetailUpdate<PRBatchEmployee.regularAmount>(Transactions.Cache, Transactions.Current, false);
			EarningDetailHelper.CopySelectedEarningDetailRecord(earningDetailsCache);
			RegularAmountAttribute.EnforceEarningDetailUpdate<PRBatchEmployee.regularAmount>(Transactions.Cache, Transactions.Current, true);
			return adapter.Get();
		}

		public PXAction<PRBatch> AddEmployees;
		[PXUIField(DisplayName = "Add Bulk Employees", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable addEmployees(PXAdapter adapter)
		{
			PrepareEmployeesPanel(false);
			
			Employees.AskExt();

			Employees.Cache.Clear();
			NonSelectableEmployees.Cache.Clear();
			return adapter.Get();
		}
		
		public PXAction<PRBatch> ToggleSelected;
		[PXUIField(DisplayName = "Toggle Selected")]
		[PXProcessButton]
		public virtual IEnumerable toggleSelected(PXAdapter adapter)
		{
			ToggleSelectedEmployees();
			return adapter.Get();
		}

		public PXAction<PRBatch> AddSelectedEmployees;
		[PXUIField(DisplayName = "Add", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable addSelectedEmployees(PXAdapter adapter)
		{
			try
			{
				AddEarningDetailsForSelectedEmployees();
			}
			finally
			{
				PrepareEmployeesPanel(true);
			}
			return adapter.Get();
		}

		public PXAction<PRBatch> AddSelectedEmployeesAndClose;
		[PXUIField(DisplayName = "Add & Close", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable addSelectedEmployeesAndClose(PXAdapter adapter)
		{
			AddEarningDetailsForSelectedEmployees();
			return adapter.Get();
		}

		public CatchRightsErrorAction<PRPayment> CheckTaxUpdateTimestamp;
		[PXButton]
		[PXUIField(Visible = false)]
		public virtual void checkTaxUpdateTimestamp()
		{
			if (!TaxUpdateHelpers.CheckTaxUpdateTimestamp(UpdateHistory.View))
			{
				UpdateTaxesPopupView.Current.Message = Messages.TaxUpdateNeeded;
				UpdateTaxesPopupView.AskExt();
			}
		}

		public PXAction<PRPayment> RedirectTaxMaintenance;
		[PXButton]
		[PXUIField(DisplayName = "Tax Maintenance")]
		protected virtual IEnumerable redirectTaxMaintenance(PXAdapter adapter)
		{
			PXRedirectHelper.TryRedirect(CreateInstance<PRTaxMaintenance>(), PXRedirectHelper.WindowMode.Same);
			return adapter.Get();
		}
		#endregion

		#region  Employees Lookup
		public PXFilter<AddEmployeeFilter> AddEmployeeFilter;
		public PXFilter<PRBatchTotalsFilter> PRBatchTotalsFilter;
		public SelectFrom<NonSelectableEmployee>.View NonSelectableEmployees;

		[PXFilterable]
		[PXCopyPasteHiddenView]
		public SelectFrom<PREmployee>.
			InnerJoin<GL.Branch>.On<PREmployee.parentBAccountID.IsEqual<GL.Branch.bAccountID>>.
			Where<MatchWithBranch<GL.Branch.branchID>.
				And<MatchWithPayGroup<PREmployee.payGroupID>>.
				And<EPEmployee.vStatus.IsEqual<VendorStatus.active>>.
				And<PREmployee.activeInPayroll.IsEqual<True>.
				And<PREmployee.payGroupID.IsEqual<PRBatch.payGroupID.FromCurrent>>.
				And<AddEmployeeFilter.employeeClassID.FromCurrent.IsNull.
					Or<PREmployee.employeeClassID.IsEqual<AddEmployeeFilter.employeeClassID.FromCurrent>>>.
				And<AddEmployeeFilter.employeeType.FromCurrent.IsNull.
					Or<PREmployee.empType.IsEqual<AddEmployeeFilter.employeeType.FromCurrent>>>>>.View 
			Employees;

		#endregion


		public PRPayBatchEntry()
		{
			AddEmployees.SetEnabled(false);
			ViewEarningDetails.SetEnabled(false);

			Employees.AllowInsert = false;
			Employees.AllowDelete = false;
			Employees.Cache.Adjust<PXUIFieldAttribute>().
				ForAllFields(field => field.Enabled = false).
				For<EPEmployee.selected>(field => field.Enabled = true);

			Deductions.AllowInsert = false;
			Deductions.AllowDelete = false;
			BatchOvertimeRules.AllowInsert = false;
			BatchOvertimeRules.AllowDelete = false;
		}

		public void UpdatePayrollBatch(string batchNumber, int? employeeID)
		{
			if (string.IsNullOrWhiteSpace(batchNumber))
				return;

			PRBatch batch = CurrentDocument.Select(batchNumber);
			Document.Current = batch;
			PRBatchEmployee batchEmployee = CurrentTransaction.Select(batchNumber, employeeID);

			PXFormulaAttribute.CalcAggregate<PREarningDetail.amount>(EmployeeEarningDetails.Cache, batchEmployee);
			PXFormulaAttribute.CalcAggregate<PREarningDetail.hours>(EmployeeEarningDetails.Cache, batchEmployee);
			PXFormulaAttribute.CalcAggregate<PREarningDetail.rate>(EmployeeEarningDetails.Cache, batchEmployee);
			CurrentTransaction.Update(batchEmployee);

			PXFormulaAttribute.CalcAggregate<PRBatchEmployee.amount>(CurrentTransaction.Cache, batch);
			PXFormulaAttribute.CalcAggregate<PRBatchEmployee.hourQty>(CurrentTransaction.Cache, batch);
			PXFormulaAttribute.CalcAggregate<PREarningDetail.employeeID>(CurrentTransaction.Cache, batch);
			CurrentDocument.Update(batch);

			ClosePayBatchIfAllPaymentsAreReleased(batchNumber);

			Actions.PressSave();
		}

		private void ClosePayBatchIfAllPaymentsAreReleased(string payBatchNumber)
		{
			if (string.IsNullOrWhiteSpace(payBatchNumber))
				return;

			PRPayment nonReleasedPaymentInBatch = NonReleasedPayBatchPayments.SelectSingle(payBatchNumber);
			if (nonReleasedPaymentInBatch != null)
				return;

			PRBatch batch = CurrentDocument.Select(payBatchNumber);
			batch.Closed = true;
			CurrentDocument.Update(batch);
		}

		private void ViewEmployeePaycheck(bool voidPaycheck)
		{
			PRBatchEmployeeExt batchEmployeeExt = Transactions.Current;
			if (batchEmployeeExt == null)
				return;

			string paymentDocType = voidPaycheck ? PayrollType.VoidCheck : Document.Current.PayrollType;

			PRPayment payment = SelectFrom<PRPayment>.
				Where<PRPayment.refNbr.IsEqual<P.AsString>.And<PRPayment.docType.IsEqual<P.AsString>>>.View.
				SelectSingleBound(this, null, batchEmployeeExt.PaymentRefNbr, paymentDocType);

			PRPayChecksAndAdjustments graph = PXGraph.CreateInstance<PRPayChecksAndAdjustments>();
			graph.Document.Current = payment;
			throw new PXRedirectRequiredException(graph, true, "Pay Checks and Adjustments");
		}

		private void PrepareEmployeesPanel(bool retainClassAndTypeFilters)
		{
			string employeeClassID = AddEmployeeFilter.Current.EmployeeClassID;
			string employeeType = AddEmployeeFilter.Current.EmployeeType;

			Employees.Cache.Clear();
			AddEmployeeFilter.Cache.Clear();
			NonSelectableEmployees.Cache.Clear();

			if (retainClassAndTypeFilters)
			{
				AddEmployeeFilter.Current.EmployeeClassID = employeeClassID;
				AddEmployeeFilter.Current.EmployeeType = employeeType;
			}
			else
			{
				Employees.View.RequestFiltersReset();
			}

			List<PREmployee> employeesInPayGroup = Employees.Select().FirstTableItems.ToList();
			List<PRBatchEmployeeExt> employeesInCurrent = Transactions.Select().FirstTableItems.ToList();
			int?[] employeeIDsInPayGroupAndNotInBatch = employeesInPayGroup.Select(x => x.BAccountID).Except(employeesInCurrent.Select(x => x.EmployeeID)).ToArray();
			List<PRBatchEmployee> existingRegularBatchesWithSamePayPeriodForEmployeesNotInBatch = GetExistingRegularPayrollBatches(employeeIDsInPayGroupAndNotInBatch);
			List<PRPayment> existingRegularPaymentsWithSamePayPeriodForEmployeesNotInBatch = GetExistingRegularPayments(employeeIDsInPayGroupAndNotInBatch);
			foreach (PREmployee employee in employeesInPayGroup)
			{
				int? employeeID = employee.BAccountID;
				bool allowEmployeeSelection = true;
				string message = null;
				if (employeesInCurrent.Any(x => x.EmployeeID == employeeID))
				{
					allowEmployeeSelection = false;
					message = Messages.EmployeeAlreadyAddedToThisBatch;
				}

				if (allowEmployeeSelection && Document.Current.PayrollType == PayrollType.Regular)
				{
					string existingRegularBatchWithSamePayPeriod = 
						existingRegularBatchesWithSamePayPeriodForEmployeesNotInBatch.FirstOrDefault(x => x.EmployeeID == employeeID)?.BatchNbr;
					if (!string.IsNullOrWhiteSpace(existingRegularBatchWithSamePayPeriod))
					{
						allowEmployeeSelection = false;
						message = string.Format(Messages.EmployeeAlreadyAddedToAnotherBatch, existingRegularBatchWithSamePayPeriod);
					}

					if (allowEmployeeSelection)
					{
						string existingRegularPaymentWithSamePayPeriod = 
							existingRegularPaymentsWithSamePayPeriodForEmployeesNotInBatch.FirstOrDefault(x => x.EmployeeID == employeeID)?.PaymentDocAndRef;
						if (!string.IsNullOrWhiteSpace(existingRegularPaymentWithSamePayPeriod))
						{
							allowEmployeeSelection = false;
							message = string.Format(Messages.EmployeeAlreadyAddedToPaycheck, existingRegularPaymentWithSamePayPeriod);
						}
					}
				}

				employee.Selected = allowEmployeeSelection;
				if (allowEmployeeSelection)
					AddEmployeeFilter.Current.SelectedEmployeesExist = true;
				Employees.Cache.Update(employee);

				if (!allowEmployeeSelection)
				{
					var nonSelectableEmployee = new NonSelectableEmployee
					{
						EmployeeID = employeeID,
						ErrorMessage = message
					};
					NonSelectableEmployees.Cache.SetStatus(nonSelectableEmployee, PXEntryStatus.Held);
				}
			}
		}
		
		private void ToggleSelectedEmployees()
		{
			bool employeeSelected = !(AddEmployeeFilter.Current.SelectedEmployeesExist == true);
			bool selectedEmployeesExist = false;

			foreach (EPEmployee employee in Employees.Select())
			{
				NonSelectableEmployee nonSelectableEmployee = 
					NonSelectableEmployees.Cache.Cached.Cast<NonSelectableEmployee>().FirstOrDefault(item => item.EmployeeID == employee.BAccountID);

				if (nonSelectableEmployee != null)
					continue;

				employee.Selected = employeeSelected;
				if (employeeSelected)
					selectedEmployeesExist = true;
				Employees.Cache.Update(employee);
			}

			AddEmployeeFilter.Current.SelectedEmployeesExist = selectedEmployeesExist;
		}

		private void AddEarningDetailsForSelectedEmployees()
		{
			bool hasErrors = false;
			Transactions.Cache.ForceExceptionHandling = true;

			PRPayGroupYearSetup payGroupYearSetup = SelectFrom<PRPayGroupYearSetup>.
				Where<PRPayGroupYearSetup.payGroupID.IsEqual<PRBatch.payGroupID.FromCurrent>>.View.
				SelectSingleBound(this, new object[] { Document.Current });
			short numberOfPayPeriods = payGroupYearSetup?.FinPeriods ?? 0;

			foreach (PREmployee employee in Employees.Select().FirstTableItems)
			{
				PRBatchEmployeeExt newPRBatchEmployeeRecord = null;

				if (employee.Selected != true)
					continue;

				try
				{
					newPRBatchEmployeeRecord = new PRBatchEmployeeExt();
					RegularAmountAttribute.EnforceEarningDetailUpdate<PRBatchEmployee.regularAmount>(Transactions.Cache, newPRBatchEmployeeRecord, false);
					newPRBatchEmployeeRecord.EmployeeID = employee.BAccountID;
					Transactions.Current = newPRBatchEmployeeRecord;
					newPRBatchEmployeeRecord = Transactions.Insert(newPRBatchEmployeeRecord);
					CreateTransactionDetails(employee, numberOfPayPeriods);
					RegularAmountAttribute.EnforceEarningDetailUpdate<PRBatchEmployee.regularAmount>(Transactions.Cache, newPRBatchEmployeeRecord, true);
				}
				catch (PXException exception)
				{
					hasErrors = true;
					PXTrace.WriteError(Messages.EmployeeEarningDetailsCreationFailed, 
						$"{employee.AcctCD},{employee.AcctName}", exception.Message);

					if (newPRBatchEmployeeRecord != null)
						Transactions.Delete(newPRBatchEmployeeRecord);
				}
			}

			if (hasErrors)
				throw new PXException(Messages.EarningDetailsCreationFailedForSomeEmployees);
		}

		private void CreateTransactionDetails(PREmployee currentEmployee, short numberOfPayPeriods)
		{
			if (AddEmployeeFilter.Current.UseQuickPay != true &&
				AddEmployeeFilter.Current.UseTimeSheets != true &&
				AddEmployeeFilter.Current.UseSalesComm != true)
			{
				return;
			}

			int? currentEmployeeID = currentEmployee.BAccountID;
			if (currentEmployeeID == null)
				throw new PXException(Messages.EmployeeIDCannotBeNull);
			if (Document.Current.StartDate == null)
				throw new PXException(Messages.BatchStartDateCannotBeNull);
			if (Document.Current.EndDate == null)
				throw new PXException(Messages.BatchEndDateCannotBeNull);

			DateTime batchStartDate = Document.Current.StartDate.Value;
			DateTime batchEndDate = Document.Current.EndDate.Value;
			HashSet<DateTime> employmentDates = GetEmploymentDates(currentEmployeeID, batchStartDate, batchEndDate,
				out bool employedForEntireBatchPeriod);

			if (!employedForEntireBatchPeriod && !employmentDates.Any())
				throw new PXException(Messages.EmployeeWasNotEmployed);

			EarningDetailUpdateParameters updateParameters = new EarningDetailUpdateParameters(
				currentEmployee, currentEmployee.CalendarID, currentEmployee.HoursPerYear ?? 0m,
				batchStartDate, batchEndDate,
				employedForEntireBatchPeriod, employmentDates,
				Preferences.Current.RegularHoursType,
				Preferences.Current.HolidaysType,
				Preferences.Current.CommissionType,
				Document.Current.TransactionDate.Value,
				numberOfPayPeriods);

			bool useQuickPayForSalariedEmployee = 
				Document.Current.IsWeeklyOrBiWeeklyPeriod != true && currentEmployee.EmpType == EmployeeType.SalariedExempt;

			if (useQuickPayForSalariedEmployee)
			{
				if (AddEmployeeFilter.Current.UseTimeSheets == true || AddEmployeeFilter.Current.UseQuickPay == true)
					UpdateUsingQuickPayForSalaried(updateParameters);
			}
			else
			{
				if (AddEmployeeFilter.Current.UseTimeSheets == true)
					UpdateUsingTimeActivities(updateParameters);

				if (AddEmployeeFilter.Current.UseQuickPay == true)
					UpdateUsingQuickPay(updateParameters);
			}

			if (AddEmployeeFilter.Current.UseSalesComm == true)
			{
				UpdateUsingSalesCommissions(updateParameters);
			}
		}

		private HashSet<DateTime> GetEmploymentDates(int? currentEmployeeID, DateTime batchStartDate, DateTime batchEndDate,
			out bool employedForEntireBatchPeriod)
		{
			HashSet<DateTime> employmentDates = new HashSet<DateTime>();
			employedForEntireBatchPeriod = false;

			PXResultset<EPEmployeePosition> employeePositionsWithinBatchPeriod = 
				SelectFrom<EPEmployeePosition>.
				Where<EPEmployeePosition.employeeID.IsEqual<P.AsInt>.
					And<EPEmployeePosition.startDate.IsLessEqual<P.AsDateTime>>.
					And<EPEmployeePosition.endDate.IsNull.
						Or<EPEmployeePosition.endDate.IsGreaterEqual<P.AsDateTime>>>>.
				OrderBy<EPEmployeePosition.startDate.Desc>.View.
				Select(this, currentEmployeeID, batchEndDate, batchStartDate);

			foreach (EPEmployeePosition position in employeePositionsWithinBatchPeriod)
			{
				if (position.StartDate <= batchStartDate &&
				    (position.EndDate == null || position.EndDate >= batchEndDate))
				{
					employedForEntireBatchPeriod = true;
					return new HashSet<DateTime>();
				}

				DateTime startDate = position.StartDate == null || position.StartDate <= batchStartDate
					? batchStartDate
					: position.StartDate.Value;
				DateTime endDate = position.EndDate == null || position.EndDate >= batchEndDate
					? batchEndDate
					: position.EndDate.Value;
				FillEmploymentDates(employmentDates, startDate, endDate);
			}

			for (DateTime date = batchStartDate; date <= batchEndDate; date = date.AddDays(1))
			{
				if (!employmentDates.Contains(date))
				{
					return employmentDates;
				}
			}
			employedForEntireBatchPeriod = true;
			return new HashSet<DateTime>();
		}

		private void FillEmploymentDates(HashSet<DateTime> employmentDates, DateTime startDate, DateTime endDate)
		{
			for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
			{
				employmentDates.Add(date.Date);
			}
		}
		
		private void UpdateUsingTimeActivities(EarningDetailUpdateParameters parameters)
		{
			PXTimeZoneInfo currentTimeZone = LocaleInfo.GetTimeZone();

			DateTime adjustedStartDate = parameters.BatchStartDate.AddDays(-1); // To account for time zones west of UTC
			DateTime adjustedEndDate = parameters.BatchEndDate.AddDays(2); // To account for activities in the last day after 00:00 and time zones east of UTC

			//Get Activity records for the current employee within the batch start and end dates
			var timeActivities = PXSelectJoin<
				PMTimeActivity,
				InnerJoin<EPEmployee,
					On<PMTimeActivity.ownerID, Equal<EPEmployee.defContactID>>>,
				Where<EPEmployee.bAccountID, Equal<Required<EPEmployee.bAccountID>>,
					And<PMTimeActivity.trackTime, Equal<boolTrue>,
					And<PMTimeActivity.isCorrected, Equal<boolFalse>,
					And<PMTimeActivity.approvalStatus, NotEqual<ActivityStatusAttribute.rejected>,
					And<PMTimeActivity.approvalStatus, NotEqual<ActivityStatusAttribute.canceled>,
					And<Where<PMTimeActivity.date, GreaterEqual<Required<PMTimeActivity.date>>,
						And<PMTimeActivity.date, LessEqual<Required<PMTimeActivity.date>>>>>>>>>>,
				OrderBy<
					Asc<PMTimeActivity.date>>>
				.Select(this, parameters.CurrentEmployeeID, adjustedStartDate, adjustedEndDate);

			foreach (PMTimeActivity item in timeActivities)
			{
				if (item.TimeSpent == null || item.TimeSpent == 0 || item.Date == null)
					continue;

				TimeSpan zoneDiff = new TimeSpan(0);
				PXTimeZoneInfo itemTimeZone = PXTimeZoneInfo.FindSystemTimeZoneById(item.ReportedInTimeZoneID);
				if (itemTimeZone != null && currentTimeZone.Id != itemTimeZone.Id)
				{
					zoneDiff = itemTimeZone.UtcOffset - currentTimeZone.BaseUtcOffset;
				}

				DateTime timeZoneAdjustedDate = item.Date.Value.Add(zoneDiff).Date;
				if (timeZoneAdjustedDate < parameters.BatchStartDate || timeZoneAdjustedDate > parameters.BatchEndDate)
					continue;

				if (item.ApprovalStatus == ActivityStatusListAttribute.Open)
					throw new PXException(Messages.ActivityOnHold);

				if (item.ApprovalStatus == ApprovalStatusListAttribute.PendingApproval)
					throw new PXException(Messages.ActivityPendingApproval);

				if (item.ApprovalStatus != ActivityStatusListAttribute.Released)
					throw new PXException(Messages.ActivityNotReleased);
				
				if (!parameters.IsEmployedOnDate(item.Date.Value))
					throw new PXException(Messages.ActivityWhenNotEmployed);

				PREarningDetail earningDetail = FillEarningDetails(
					date: timeZoneAdjustedDate,
					earningTypeID: item.EarningTypeID,
					hours: Math.Round((decimal) item.TimeSpent / 60, 2),
					costCodeID: item.CostCodeID,
					certifiedJob: item.CertifiedJob,
					labourItemID: item.LabourItemID,
					projectID: item.ProjectID,
					projectTaskID: item.ProjectTaskID,
					earningDetailSourceType: EarningDetailSourceType.TimeActivity,
					sourceNoteID: item.NoteID);
				earningDetail.WorkCodeID = item.WorkCodeID;
				TransactionDetails.Update(earningDetail);
			}
		}
		
		private void UpdateUsingQuickPayForSalaried(EarningDetailUpdateParameters parameters)
		{
			if (parameters.RegularHoursEarningType == null && parameters.HolidaysEarningType == null)
				throw new PXException(Messages.RegularAndHolidaysTypesAreNotSetUp);

			if (parameters.RegularHoursEarningType == null)
				throw new PXException(Messages.RegularHoursTypeIsNotSetUp);

			if (parameters.HolidaysEarningType == null)
				throw new PXException(Messages.HolidaysTypeIsNotSetUp);

			if (parameters.NumberOfPayPeriods < 1)
				throw new PXException(Messages.IncorrectNumberOfPayPeriods);

			AddQuickPayHolidayEarningDetails(parameters, out decimal totalHolidayHours);

			decimal regularHoursForPayPeriod = parameters.WorkingHoursPerYear / parameters.NumberOfPayPeriods;
			if (regularHoursForPayPeriod >= totalHolidayHours)
				regularHoursForPayPeriod -= totalHolidayHours;
			else
				regularHoursForPayPeriod = 0;

			FillEarningDetails(
				date: parameters.BatchEndDate,
				earningTypeID: parameters.RegularHoursEarningType,
				hours: regularHoursForPayPeriod,
				earningDetailSourceType: EarningDetailSourceType.QuickPay,
				labourItemID: parameters.EPEmployeeInfo.LabourItemID);
		}

		private void UpdateUsingQuickPay(EarningDetailUpdateParameters parameters)
		{
			if (parameters.RegularHoursEarningType == null && parameters.HolidaysEarningType == null)
				throw new PXException(Messages.RegularAndHolidaysTypesAreNotSetUp);

			if (parameters.RegularHoursEarningType == null)
				throw new PXException(Messages.RegularHoursTypeIsNotSetUp);

			if (parameters.HolidaysEarningType == null)
				throw new PXException(Messages.HolidaysTypeIsNotSetUp);

			AddQuickPayRegularEarningDetails(parameters);
			AddQuickPayHolidayEarningDetails(parameters, out decimal totalHolidayHours);
		}

		private void AddQuickPayRegularEarningDetails(EarningDetailUpdateParameters parameters)
		{
			Dictionary<DateTime, decimal> importedHours = GetDailyHoursFromTimeActivities(parameters.CurrentEmployeeID);
			decimal totalImportedHours = importedHours.Values.Sum();

			Dictionary<DateTime, decimal> standardWorkingHours = GetStandardWorkingHours(parameters);
			decimal expectedWorkingHours = standardWorkingHours.Values.Sum();

			decimal totalQuickPayHours = expectedWorkingHours - totalImportedHours;

			if (totalQuickPayHours <= 0)
				return;
			
			Dictionary<DateTime, decimal> quickPayHours = new Dictionary<DateTime, decimal>();
			
			foreach (KeyValuePair<DateTime, decimal> dailyWorkingHours in standardWorkingHours)
			{
				if (!importedHours.TryGetValue(dailyWorkingHours.Key, out decimal importedDailyHours))
					importedDailyHours = 0;

				if (importedDailyHours >= dailyWorkingHours.Value)
					continue;

				decimal currentDayQuickPayHours = dailyWorkingHours.Value - importedDailyHours;
				if (currentDayQuickPayHours > totalQuickPayHours)
					currentDayQuickPayHours = totalQuickPayHours;

				totalQuickPayHours -= currentDayQuickPayHours;
				quickPayHours[dailyWorkingHours.Key] = currentDayQuickPayHours;

				if (totalQuickPayHours == 0)
					break;
			}
			
			foreach (KeyValuePair<DateTime, decimal> quickPayDailyHours in quickPayHours)
			{
				FillEarningDetails(
					date: quickPayDailyHours.Key,
					earningTypeID: parameters.RegularHoursEarningType,
					hours: quickPayDailyHours.Value,
					earningDetailSourceType: EarningDetailSourceType.QuickPay,
					labourItemID: parameters.EPEmployeeInfo.LabourItemID);
			}
		}

		private void AddQuickPayHolidayEarningDetails(EarningDetailUpdateParameters parameters, out decimal totalHolidayHours)
		{
			PXResultset<CSCalendarExceptions> calendarExceptions = SelectFrom<CSCalendarExceptions>.
				Where<CSCalendarExceptions.calendarID.IsEqual<P.AsString>.
					And<CSCalendarExceptions.date.IsGreaterEqual<P.AsDateTime>>.
					And<CSCalendarExceptions.date.IsLessEqual<P.AsDateTime>>>.View.
				Select(this, parameters.CalendarID, parameters.BatchStartDate, parameters.BatchEndDate);

			totalHolidayHours = 0;

			foreach (CSCalendarExceptions holiday in calendarExceptions)
			{
				if (holiday.StartTime == null || holiday.EndTime == null || holiday.Date == null)
					continue;

				DateTime holidayDate = holiday.Date.Value.Date;
				if (!parameters.IsEmployedOnDate(holidayDate))
					continue;

				decimal holidayHours = ((decimal)(holiday.EndTime.Value - holiday.StartTime.Value).TotalHours) - ((decimal)holiday.UnpaidTime.GetValueOrDefault() / 60);
				FillEarningDetails(
					date: holiday.Date,
					earningDetailSourceType: EarningDetailSourceType.QuickPay,
					earningTypeID: holiday.WorkDay == true ? parameters.RegularHoursEarningType : parameters.HolidaysEarningType,
					hours: holidayHours,
					labourItemID: parameters.EPEmployeeInfo.LabourItemID);

				totalHolidayHours += holidayHours;
			}
		}

		private Dictionary<DateTime, decimal> GetDailyHoursFromTimeActivities(int? currentEmployeeID)
		{
			var result = new Dictionary<DateTime, decimal>();
			foreach (PREarningDetail earningDetail in EmployeeEarningDetails.Select(currentEmployeeID))
			{
				if (earningDetail.SourceType == EarningDetailSourceType.TimeActivity &&
				    earningDetail.UnitType == UnitType.Hour &&
				    earningDetail.Hours != null &&
				    earningDetail.Date != null)
				{
					DateTime date = earningDetail.Date.Value.Date;
					decimal hours = earningDetail.Hours.Value;

					if (result.ContainsKey(date))
						result[date] += hours;
					else
						result[date] = hours;
				}
			}

			return result;
		}

		private Dictionary<DateTime, decimal> GetStandardWorkingHours(EarningDetailUpdateParameters parameters)
		{
			var result = new Dictionary<DateTime, decimal>();
			if (string.IsNullOrWhiteSpace(parameters.CalendarID))
				return result;

			DateTime dateToCheck = parameters.BatchStartDate.Date;
			while (dateToCheck <= parameters.BatchEndDate.Date)
			{
				if (!parameters.IsEmployedOnDate(dateToCheck))
				{
					dateToCheck = dateToCheck.AddDays(1);
					continue;
				}

				decimal workingHours = 0;

				//Check if the current day is a holiday
				CSCalendarExceptions holidayCheck = SelectFrom<CSCalendarExceptions>.
					Where<CSCalendarExceptions.calendarID.IsEqual<P.AsString>.
						And<CSCalendarExceptions.date.IsEqual<P.AsDateTime>>>.View.ReadOnly.
					Select(this, parameters.CalendarID, dateToCheck);

				//If the current day is not a holiday day, add the hours
				if (holidayCheck == null)
					workingHours = GetCalendarDayWorkHours(dateToCheck, parameters.CalendarID);

				result[dateToCheck] = workingHours;
				dateToCheck = dateToCheck.AddDays(1);
			}

			return result;
		}

		private decimal GetCalendarDayWorkHours(DateTime date, string calendarID)
		{
			DayOfWeek dayOfWeek = date.DayOfWeek;

			if (!string.IsNullOrWhiteSpace(calendarID))
			{
				CSCalendar calendar = SelectFrom<CSCalendar>.
					Where<CSCalendar.calendarID.IsEqual<P.AsString>>.View.ReadOnly.
					Select(this, calendarID);

				if (calendar != null)
					return CalendarHelper.GetHoursWorkedOnDay(calendar, dayOfWeek);
			}

			return 0;
		}

		private void UpdateUsingSalesCommissions(EarningDetailUpdateParameters parameters)
		{
			if (parameters.EPEmployeeInfo.SalesPersonID == null)
				return;

			if (parameters.CommissionEarningType == null)
				throw new PXException(Messages.CommissionTypeIsNotSetUp);

			decimal commissionAmount = 0;

			ARSPCommnHistory commissionHistory = null;
			foreach (PXResult<ARSPCommnHistory, ARSPCommissionPeriod> employeeCommission in
				CommissionHistory.Select(ARSPCommissionPeriodStatus.Closed, parameters.EPEmployeeInfo.SalesPersonID))
			{
				commissionHistory = employeeCommission;
				commissionAmount += commissionHistory.CommnAmt ?? 0;
				commissionHistory.PRProcessedDate = parameters.TransactionDate;
				CommissionHistory.Update(commissionHistory);
			}

			if (commissionAmount <= 0)
				return;

			FillEarningDetails(
				date: parameters.BatchEndDate,
				earningDetailSourceType: EarningDetailSourceType.SalesCommission,
				earningTypeID: parameters.CommissionEarningType,
				hours: null,
				commissionAmount: commissionAmount,
				sourceCommnPeriod: commissionHistory.CommnPeriod,
				labourItemID: parameters.EPEmployeeInfo.LabourItemID);
		}

		PREarningDetail FillEarningDetails(
			DateTime? date,
			string earningDetailSourceType,
			string earningTypeID,
			decimal? hours,
			decimal? commissionAmount = null,
			int? costCodeID = null,
			bool? certifiedJob = null,
			int? labourItemID = null,
			int? projectID = null,
			int? projectTaskID = null,
			Guid? sourceNoteID = null,
			string sourceCommnPeriod = null)
		{
			PREarningDetail earningDetails = TransactionDetails.Insert();

			earningDetails.Date = date;
			earningDetails.TypeCD = earningTypeID;
			
			if (commissionAmount == null)
				earningDetails.Hours = hours;
			else
				earningDetails.Amount = commissionAmount;

			earningDetails.CostCodeID = costCodeID;
			earningDetails.CertifiedJob = certifiedJob ?? false;
			earningDetails.LabourItemID = labourItemID;

			earningDetails.ProjectID = projectID;
			earningDetails.ProjectTaskID = projectTaskID;

			earningDetails.SourceType = earningDetailSourceType;
			earningDetails.SourceNoteID = sourceNoteID;
			earningDetails.SourceCommnPeriod = sourceCommnPeriod;

			return TransactionDetails.Update(earningDetails);
		}

		private class EarningDetailUpdateParameters
		{
			public EarningDetailUpdateParameters(EPEmployee employeeInfo, string calendarID, decimal hoursPerYear, DateTime batchStartDate, DateTime batchEndDate, bool employedForEntireBatchPeriod, HashSet<DateTime> employmentDates, string regularHoursEarningType, string holidaysEarningType, string commissionEarningType, DateTime transactionDate, short numberOfPayPeriods)
			{
				CurrentEmployeeID = employeeInfo.BAccountID;
				EPEmployeeInfo = employeeInfo;
				CalendarID = calendarID;
				WorkingHoursPerYear = hoursPerYear;
				BatchStartDate = batchStartDate;
				BatchEndDate = batchEndDate;
				_EmployedForEntireBatchPeriod = employedForEntireBatchPeriod;
				_EmploymentDates = employmentDates;
				RegularHoursEarningType = regularHoursEarningType;
				HolidaysEarningType = holidaysEarningType;
				CommissionEarningType = commissionEarningType;
				TransactionDate = transactionDate;
				NumberOfPayPeriods = numberOfPayPeriods;
			}

			private readonly bool _EmployedForEntireBatchPeriod;
			private readonly HashSet<DateTime> _EmploymentDates;

			public int? CurrentEmployeeID { get; }
			public EPEmployee EPEmployeeInfo { get; }
			public string CalendarID { get; }
			public decimal WorkingHoursPerYear { get; }
			public DateTime BatchStartDate { get; }
			public DateTime BatchEndDate { get; }
			public string RegularHoursEarningType { get; }
			public string HolidaysEarningType { get; }
			public string CommissionEarningType { get; }
			public DateTime TransactionDate { get; }
			public short NumberOfPayPeriods { get; }

			public bool IsEmployedOnDate(DateTime date)
			{
				return _EmployedForEntireBatchPeriod || _EmploymentDates.Contains(date.Date);
			}
		}
	}

	[Serializable]
	[PXHidden]
	public sealed class PRBatchEmployeeExt : PRBatchEmployee
	{
		#region PaymentRefNbr
		public abstract class paymentRefNbr : BqlString.Field<paymentRefNbr> { }
		[PXString]
		public string PaymentRefNbr { get; set; }
		#endregion

		#region PaymentDocAndRef
		public abstract class paymentDocAndRef : BqlString.Field<paymentDocAndRef> { }
		[PXString]
		[PXUIField(DisplayName = "Paycheck Ref", Enabled = false)]
		public string PaymentDocAndRef { get; set; }
		#endregion

		#region VoidPaymentDocAndRef
		public abstract class voidPaymentDocAndRef : BqlString.Field<voidPaymentDocAndRef> { }
		[PXString]
		[PXUIField(DisplayName = "Void Paycheck Ref", Visible = false)]
		public string VoidPaymentDocAndRef { get; set; }
		#endregion

		#region HasNegativeHoursEarnings
		[PXBool]
		[PXUnboundDefault(false)]
		public bool? HasNegativeHoursEarnings { get; set; }
		public abstract class hasNegativeHoursEarnings : BqlBool.Field<hasNegativeHoursEarnings> { }
		#endregion
	}

	[Serializable]
	[PXHidden]
	public class AddEmployeeFilter : IBqlTable
	{
		#region EmployeeClassID
		public abstract class employeeClassID : BqlString.Field<employeeClassID> { }
		[PXString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Employee Class")]
		[PXSelector(typeof(Search<PREmployeeClass.employeeClassID>), DescriptionField = typeof(PREmployeeClass.descr))]
		public virtual string EmployeeClassID { get; set; }
		#endregion
		#region EmployeeType
		public abstract class employeeType : BqlString.Field<employeeType> { }
		[PXString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Employee Type")]
		[EmployeeType.List]
		public virtual string EmployeeType { get; set; }
		#endregion
		#region UseQuickPay
		public abstract class useQuickPay : BqlBool.Field<useQuickPay> { }
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Pre-Populate with Employee Defaults (Quick Pay)")]
		public bool? UseQuickPay { get; set; }
		#endregion
		#region UseTimeSheets
		public abstract class useTimeSheets : BqlBool.Field<useTimeSheets> { }
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Time Activities (will override defaults as applicable)")]
		public bool? UseTimeSheets { get; set; }
		#endregion
		#region UseSalesComm
		public abstract class useSalesComm : BqlBool.Field<useSalesComm> { }
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Sales Commissions")]
		public bool? UseSalesComm { get; set; }
		#endregion
		#region SelectedEmployeesExist
		public abstract class selectedEmployeesExist : BqlBool.Field<selectedEmployeesExist> { }
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(Visible = false)]
		public virtual bool? SelectedEmployeesExist { get; set; }
		#endregion
	}

	[Serializable]
	[PXHidden]
	public class PRBatchTotalsFilter : IBqlTable
	{
		#region HideTotals
		public abstract class hideTotals : PX.Data.BQL.BqlDecimal.Field<hideTotals> { }
		[PXBool]
		public virtual bool? HideTotals { get; set; }
		#endregion
		#region TotalEarnings
		public abstract class totalEarnings : PX.Data.BQL.BqlDecimal.Field<totalEarnings> { }
		[PXDecimal]
		[PXUIField(DisplayName = "Total Earnings", Visibility = PXUIVisibility.Visible, Enabled = false)]
		public virtual decimal? TotalEarnings { get; set; }
		#endregion
		#region TotalHourQty
		public abstract class totalHourQty : BqlInt.Field<totalHourQty> { }
		[PXDecimal]
		[PXUIField(DisplayName = "Total Hour Qty", Visibility = PXUIVisibility.Visible, Enabled = false)]
		public virtual decimal? TotalHourQty { set; get; }
		#endregion
	}

	[Serializable]
	[PXHidden]
	public class NonSelectableEmployee : IBqlTable
	{
		#region EmployeeID
		public abstract class employeeID :BqlInt.Field<employeeID> { }
		[PXInt(IsKey = true)]
		public int? EmployeeID { get; set; }
		#endregion
		#region EmployeeClassID
		public abstract class errorMessage : BqlString.Field<errorMessage> { }
		[PXString(IsUnicode = true)]
		public virtual string ErrorMessage { get; set; }
		#endregion
	}
}
