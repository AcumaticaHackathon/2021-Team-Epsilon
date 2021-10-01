using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.GL.DAC;
using PX.Objects.GL;
using PX.Objects.SP.DAC;
using SP.Objects.SP;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;
using System.Linq;

namespace SP.Objects.AR
{
	[DashboardType((int)DashboardTypeAttribute.Type.Default, TableAndChartDashboardTypeAttribute._AMCHARTS_DASHBOART_TYPE)]
	public class ARDocumentEnqExt : PXGraphExtension<ARDocumentEnq>
	{
		public Customer currentCustomer;

		#region Cache Attached

		#region OrigDocAmt
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Document Total")]
		protected virtual void ARDocumentResult_OrigDocAmt_CacheAttached(PXCache sender)
		{
		}
		#endregion
		#region DocBal
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Open Balance")]

		protected virtual void ARDocumentResult_DocBal_CacheAttached(PXCache sender)
		{
		}
		#endregion

		#endregion

		#region Constructor

		public override void Initialize()
		{
			if (PortalSetup.Current == null)
				throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

			currentCustomer = ReadBAccount.ReadCurrentCustomerWithoutCheck();

			Base.actionsfolder.SetVisible(false);
			Base.reportsfolder.SetVisible(false);
			
			Base.Actions["PrintSelectedDocument"].SetEnabled(Base.Documents.Select().Count > 0);
			PXUIFieldAttribute.SetVisible<ARDocumentEnq.ARDocumentResult.openDoc>(Base.Caches[typeof(ARDocumentEnq.ARDocumentResult)], null, false);
			Base.Filter.Current.ShowAllDocs = true;
			PXFilterRow[] rows =
			{
				new PXFilterRow
				{
					Condition = PXCondition.EQ,
					DataField = typeof(ARRegister.openDoc).Name,
					Value = true
				}
			};
			PXFilterableAttribute.AddFilter(Base.GetType().FullName, "Documents", Messages.OpenDocument, rows);

			bool customerExists = currentCustomer != null;
			Base.Actions["PrintSelectedDocument"].SetEnabled(customerExists);
			Base.Actions["aRBalanceByCustomerReportportal"].SetEnabled(customerExists);
			Base.Actions["ARAgedPastDueReportportal"].SetEnabled(customerExists);
		}

		#endregion

		#region Actions

		#region Sub-screen Navigation Button

		public PXAction<ARDocumentEnq.ARDocumentFilter> PrintSelectedDocument;
		[PXUIField(DisplayName = "Print Invoice/Memo", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void printSelectedDocument()
		{
			if (Base.Documents.Current != null && Base.Filter.Current != null)
			{
				if ((Base.Documents.Current.DocType == ARDocType.Invoice) ||
					(Base.Documents.Current.DocType == ARDocType.DebitMemo) ||
					(Base.Documents.Current.DocType == ARDocType.CreditMemo))
				{
					Dictionary<string, string> parameters = new Dictionary<string, string>();
					Export(parameters, Base.Documents.Current);
					throw new PXReportRequiredException(parameters, "AR641000", "Invoice/Memo");
				}
			}
		}

		protected static void Export(Dictionary<string, string> aRes, ARDocumentEnq.ARDocumentResult aDetail)
		{
			aRes["DocType"] = aDetail.DocType;
			aRes["RefNbr"] = aDetail.RefNbr;
		}
		
		#endregion

		public PXAction<ARDocumentEnq.ARDocumentFilter> aRBalanceByCustomerReportportal;
		[PXUIField(DisplayName = "Print Account History", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARBalanceByCustomerReportportal(PXAdapter adapter)
		{
			ARDocumentEnq.ARDocumentFilter filter = Base.Filter.Current;
			if (filter != null)
			{
				PXResultset<Customer> res = PXSelect<Customer,
					Where<Customer.consolidatingBAccountID, Equal<Current<ARDocumentEnq.ARDocumentFilter.customerID>>>,
					OrderBy<Asc<Customer.parentBAccountID>>>.Select(Base);

				bool includeChild = filter.IncludeChildAccounts == true && res.Count > 1;

				Customer currentCustomer = PXSelect<Customer,
														Where<Customer.bAccountID, Equal<Current<ARDocumentEnq.ARDocumentFilter.customerID>>>>
														.Select(Base);

				Dictionary<string, string> parameters = new Dictionary<string, string>();

				var useMasterCalendar = PortalSetup.Current.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.ALL || filter.UseMasterCalendar == true;
				int? calendarOrganizationID = Base.FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, useMasterCalendar);
				
				FinPeriod lastFinPeriod = Base.FinPeriodRepository.FindLastPeriod(calendarOrganizationID);
				
                int? organizationID = null;
				if (PortalSetup.Current.RestrictByOrganizationID.HasValue)
				{
					Organization organization = PXSelect<Organization, Where<Organization.organizationID, Equal<Required<Organization.organizationID>>>>
												.SelectSingleBound(Base, null, PortalSetup.Current.RestrictByOrganizationID);
					organizationID = organization?.BAccountID;
				}

				BAccountR bAccount = SelectFrom<BAccountR>
					.Where<BAccount.bAccountID.IsEqual<@P.AsInt>>
					.View
					.Select(Base, PXAccess.GetBranch(filter.BranchID)?.BAccountID ?? organizationID);

				parameters["PeriodID"] = lastFinPeriod.PeriodNbr + lastFinPeriod.FinYear;
				parameters["CustomerID"] = currentCustomer.AcctCD;
				parameters["Format"] = "D";
                parameters["OrgBAccountID"] = bAccount?.AcctCD;

				throw new PXReportRequiredException(parameters, "AR632500", PX.Objects.AR.Messages.ARBalanceByCustomerReport);
			}
			return adapter.Get();
		}

		public PXAction<ARDocumentEnq.ARDocumentFilter> aRAgedPastDueReportportal;
		[PXUIField(DisplayName = "Aging Report", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARAgedPastDueReportportal(PXAdapter adapter)
		{
			ARDocumentEnq.ARDocumentFilter filter = Base.Filter.Current;
			if (filter != null)
			{
				PXResultset<Customer> res = PXSelect<Customer,
					Where<Customer.consolidatingBAccountID, Equal<Current<ARDocumentEnq.ARDocumentFilter.customerID>>>,
					OrderBy<Asc<Customer.parentBAccountID>>>.Select(Base);

				bool includeChild = filter.IncludeChildAccounts == true && res.Count > 1;

				Customer currentCustomer = PXSelect<Customer,
														Where<Customer.bAccountID, Equal<Current<ARDocumentEnq.ARDocumentFilter.customerID>>>>
														.Select(Base);
				int? organizationID = null;
				if (PortalSetup.Current.RestrictByOrganizationID.HasValue)
				{
					Organization organization = PXSelect<Organization, Where<Organization.organizationID, Equal<Required<Organization.organizationID>>>>
												.SelectSingleBound(Base, null, PortalSetup.Current.RestrictByOrganizationID);
					organizationID = organization?.BAccountID;
				}

				BAccountR bAccount = SelectFrom<BAccountR>
					.Where<BAccount.bAccountID.IsEqual<@P.AsInt>>
					.View
					.Select(Base, PXAccess.GetBranch(filter.BranchID)?.BAccountID ?? organizationID);

				Dictionary<string, string> parameters = new Dictionary<string, string>();
				parameters["CustomerID"] = currentCustomer.AcctCD;
				parameters["OrgBAccountID"] = bAccount?.AcctCD;
				throw new PXReportRequiredException(parameters, "AR631000", PX.Objects.AR.Messages.ARAgedPastDueReport);
			}
			return adapter.Get();
		}

		#endregion

		#region Event Handlers

		protected virtual void ARDocumentFilter_CustomerID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e, PXFieldDefaulting sel)
		{
			if (sel != null)
			{
				sel(sender, e);
			}
			
			ARDocumentEnq.ARDocumentFilter row = e.Row as ARDocumentEnq.ARDocumentFilter;
			if (row == null) 
				return;

			if (currentCustomer != null)
			{
				row.CustomerID = currentCustomer.BAccountID;
			}
		}

		protected virtual void ARDocumentFilter_BranchID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e, PXFieldDefaulting sel)
		{
			if (sel != null)
			{
				sel(sender, e);
			}

			var row = e.Row as ARDocumentEnq.ARDocumentFilter;
			if (row == null)
				return;

			switch (PortalSetup.Current.DisplayFinancialDocuments)
			{
				case FinancialDocumentsFilterAttribute.BY_BRANCH:
					e.NewValue = PortalSetup.Current.RestrictByBranchID;
					break;
				default:
					e.NewValue = null;
					break;
			}

			e.Cancel = true;
		}

		protected virtual void ARDocumentFilter_OrganizationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e, PXFieldDefaulting sel)
		{
			if (sel != null)
			{
				sel(sender, e);
			}

			var row = e.Row as ARDocumentEnq.ARDocumentFilter;
			if (row == null)
				return;

			switch (PortalSetup.Current.DisplayFinancialDocuments)
			{
				case FinancialDocumentsFilterAttribute.BY_COMPANY:
					e.NewValue = PortalSetup.Current.RestrictByOrganizationID;
					break;
				default:
					e.NewValue = null;
					break;
			}
			e.Cancel = true;
		}

		protected virtual void ARDocumentFilter_OrgBAccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e, PXFieldDefaulting sel)
		{
			if (sel != null)
			{
				sel(sender, e);
			}

			var row = e.Row as ARDocumentEnq.ARDocumentFilter;
			if (row == null)
				return;

			switch (PortalSetup.Current.DisplayFinancialDocuments)
			{
				case FinancialDocumentsFilterAttribute.BY_COMPANY:
					e.NewValue = sender.Graph
						.Select<Organization>()
						.Where(b => b.OrganizationID == PortalSetup.Current.RestrictByOrganizationID)
						.Select(r => r.BAccountID)
						.FirstOrDefault();
					break;
				case FinancialDocumentsFilterAttribute.BY_BRANCH:
					e.NewValue = sender.Graph
						.Select<Branch>()
						.Where(b => b.BranchID == PortalSetup.Current.RestrictByBranchID)
						.Select(r => r.BAccountID)
						.FirstOrDefault();
					break;
				default:
					e.NewValue = null;
					break;
			}
			e.Cancel = true;
		}

		protected virtual void ARDocumentFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
		{
			ARDocumentEnq.ARDocumentFilter row = e.Row as ARDocumentEnq.ARDocumentFilter;
			ARDocumentFilterExt rowExt = PXCache<ARDocumentEnq.ARDocumentFilter>.GetExtension<ARDocumentFilterExt>(row);
			
			rowExt.OpenInvoiceAndCharge = 0;
			rowExt.CreditMemosandUnappliedPayment = 0;

			foreach (ARDocumentEnq.ARDocumentResult record in Base.Documents.Select())
			{
				if (record.DocType == ARDocType.Invoice ||
					record.DocType == ARDocType.DebitMemo ||
					record.DocType == ARDocType.FinCharge ||
					record.DocType == ARDocType.CreditMemo)
				{
					rowExt.OpenInvoiceAndCharge += record.DocBal;
				}
				else if (record.DocType == ARDocType.Payment || record.DocType == ARDocType.Prepayment)
				{
					rowExt.CreditMemosandUnappliedPayment += record.DocBal;
				}
			}

			if (Base.Filter.Current != null)
			{
				rowExt.NetBalance = Base.Filter.Current.BalanceSummary;
			}
			if (currentCustomer != null)
			{
				CustomerMaint graph = PXGraph.CreateInstance<CustomerMaint>();

				Customer CustomerMaintCustomer = graph.BAccount.Search<BAccount.acctCD>(currentCustomer.AcctCD);
				graph.BAccount.Cache.Current = CustomerMaintCustomer;
				graph.CustomerBalance.Select();
				CustomerMaint.CustomerBalanceSummary CustomerMaintCustomerBalance = graph.CustomerBalance.Cache.Current as CustomerMaint.CustomerBalanceSummary;

				if (CustomerMaintCustomer != null)
				{
					rowExt.CreditLimit = CustomerMaintCustomer.CreditLimit;
				}

				if (CustomerMaintCustomerBalance != null)
				{
					rowExt.AvailableCredit = CustomerMaintCustomerBalance.RemainingCreditLimit;
				}
			}

			if (sel != null)
			{
				sel(sender, e);
			}
		}

		#endregion
	}
}



