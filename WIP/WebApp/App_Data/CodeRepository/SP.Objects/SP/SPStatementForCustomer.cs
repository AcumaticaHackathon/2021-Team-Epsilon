using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.GL;
using PX.Objects.SP.DAC;

namespace SP.Objects.SP
{
#if false
	public class DetailKey : IComparable<DetailKey>, IEquatable<DetailKey>
	{
		public DetailKey(string aFirst, string aSecond) 
		{
			this.first = aFirst;
			this.second = aSecond;
		}
		public string first;
		public string second;

	#region IComparable<CashAcctKey> Members
		public virtual int CompareTo(DetailKey other)
		{
			int res = this.first.CompareTo(other.first);
			if (res == 0) return (this.second.CompareTo(other.second));
			return res;
		}

		public override int GetHashCode()
		{
			return (this.first.GetHashCode())^(this.second.GetHashCode()); //Force to call the CompareTo methods in dicts
		}
	#endregion


	#region IComparable<DetailKey> Members

		int IComparable<DetailKey>.CompareTo(DetailKey other)
		{
			return this.CompareTo(other);
		}

	#endregion

	#region IEquatable<DetailKey> Members

		public virtual bool Equals(DetailKey other)
		{
			return (this.CompareTo(other)==0);
		}

		//public override bool Equals(Object obj) 
		//{
		//    DetailKey key = obj as DetailKey;
		//    if (key == null) return false;
		//    return Equals(key);
		//}
	#endregion
	}
#endif

	[DashboardType((int)DashboardTypeAttribute.Type.Default, TableAndChartDashboardTypeAttribute._AMCHARTS_DASHBOART_TYPE)]
	public class SPStatementForCustomer : PXGraph<SPStatementForCustomer>
	{
		[System.SerializableAttribute()]
		public partial class SPStatementForCustomerParameters : IBqlTable
		{
			#region CustomerID
			public abstract class customerID : PX.Data.IBqlField
			{
			}

			[PXInt()]
			[PXDefault()]
			[PXUIField(DisplayName = "Customer")]
			[Customer(DescriptionField = typeof (Customer.acctName))]
			public virtual int? CustomerID { get; set; }
			#endregion
			#region FromDate
			public abstract class fromDate : PX.Data.IBqlField
			{
			}

			[PXDate(MaxValue = "06/06/2079", MinValue = "01/01/1900")]
			[PXDefault(typeof (AccessInfo.businessDate))]
			[PXUIField(DisplayName = "From Date", Visibility = PXUIVisibility.Visible)]
			public virtual DateTime? FromDate { get; set; }
			#endregion
			#region TillDate
			public abstract class tillDate : PX.Data.IBqlField
			{
			}

			[PXDate(MaxValue = "06/06/2079", MinValue = "01/01/1900")]
			[PXDefault(typeof (AccessInfo.businessDate))]
			[PXUIField(DisplayName = "To Date", Visibility = PXUIVisibility.Visible)]
			public virtual DateTime? TillDate { get; set; }
			#endregion
		}

		public PXFilter<SPStatementForCustomerParameters> Filter;
		public PXCancel<SPStatementForCustomerParameters> Cancel;
		[PXFilterable] 
		public PXSelect<ARStatementForCustomer.DetailsResult> Details;

		public SPStatementForCustomer()
		{
			if (PortalSetup.Current == null)
				throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

			ARSetup setup = ARSetup.Current;
			Details.Cache.AllowDelete = false;
			Details.Cache.AllowInsert = false;
			Details.Cache.AllowUpdate = false;
		}

		public PXSetup<ARSetup> ARSetup;

		public virtual IEnumerable details()
		{
			SPStatementForCustomerParameters header = Filter.Current;
			Dictionary<ARStatementForCustomer.DetailKey, ARStatementForCustomer.DetailsResult> result =
				new Dictionary<ARStatementForCustomer.DetailKey, ARStatementForCustomer.DetailsResult>(
					EqualityComparer<ARStatementForCustomer.DetailKey>.Default);
			List<ARStatementForCustomer.DetailsResult> curyResult = new List<ARStatementForCustomer.DetailsResult>();
			if (header == null)
			{
				return curyResult;
			}
			
			Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.Select(this, header.CustomerID);
			if (customer != null)
			{
				bool useCurrency = customer.PrintCuryStatements ?? false;
				Company company = PXSelect<Company>.Select(this);

				header.FromDate = header.FromDate ?? DateTime.MinValue;
				header.TillDate = header.TillDate ?? DateTime.MaxValue;

				var select = new PXSelect<ARStatement,
					Where<ARStatement.statementCustomerID, Equal<Required<ARStatement.statementCustomerID>>,
						And<ARStatement.statementDate, GreaterEqual<Required<ARStatement.statementDate>>,
						And<ARStatement.statementDate, LessEqual<Required<ARStatement.statementDate>>>>>,
					OrderBy<Asc<ARStatement.statementCycleId, Asc<ARStatement.statementDate, Asc<ARStatement.curyID>>>>>(this);

				var branches = new int[] { };
				if (PortalSetup.Current.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_BRANCH)
				{
					branches = new int[] { PortalSetup.Current.RestrictByBranchID.Value };
					select.WhereAnd<Where<ARStatement.branchID, In<Required<ARStatement.branchID>>>>();
				}
				else if (PortalSetup.Current.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_COMPANY)
				{
					branches = PXAccess.GetChildBranchIDs(PortalSetup.Current.RestrictByOrganizationID, false);
					if (branches.Length > 0)
						select.WhereAnd<Where<ARStatement.branchID, In<Required<ARStatement.branchID>>>>();
					else
						select.WhereAnd<Where<True, Equal<False>>>();
				}

				foreach (ARStatement st in select.Select(header.CustomerID, header.FromDate, header.TillDate, branches))
				{
					ARStatementForCustomer.DetailsResult res = new ARStatementForCustomer.DetailsResult();
					res.Copy(st, customer);
					if (useCurrency)
					{
						ARStatementForCustomer.DetailsResult last = curyResult.Count > 0 ? curyResult[curyResult.Count - 1] : null;
						if (last != null && 
							last.StatementCycleId == res.StatementCycleId && 
							last.StatementDate == res.StatementDate && 
							last.CuryID == res.CuryID)
						{
							last.Append(res);
						}
						else
						{
							curyResult.Add(res);
						}
					}
					else
					{
						ARStatementForCustomer.DetailKey key = new ARStatementForCustomer.DetailKey(res.StatementDate.Value, res.StatementCycleId, res.BranchID.Value);
						res.ResetToBaseCury(company.BaseCuryID);
						if (!result.ContainsKey(key))
						{
							result[key] = res;
						}
						else
						{
							result[key].Append(res);
						}
					}
				}
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.curyID>(this.Details.Cache, null, useCurrency);
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.curyStatementBalance>(this.Details.Cache, null, useCurrency);
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.curyOverdueBalance>(this.Details.Cache, null, useCurrency);
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.statementBalance>(this.Details.Cache, null, !useCurrency);
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.overdueBalance>(this.Details.Cache, null, !useCurrency);

				return useCurrency ? curyResult : (IEnumerable)result.Values;
			}
			return curyResult;
		}

		protected virtual void SPStatementForCustomerParameters_FromDate_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			var row = e.Row as SPStatementForCustomerParameters;
			if (row == null) 
				return;

			row.FromDate = PXTimeZoneInfo.Now.AddMonths(-6);
		}

		protected virtual void SPStatementForCustomerParameters_CustomerID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			var row = e.Row as SPStatementForCustomerParameters;
			if (row == null) 
				return;

			row.CustomerID = ReadBAccount.ReadCurrentAccount().With(_ => _.BAccountID);
		}

		#region Sub-screen Navigation Button
		public PXAction<SPStatementForCustomerParameters> printReport;
		[PXUIField(DisplayName = "Print Statement", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable PrintReport(PXAdapter adapter)
		{
			if (this.Details.Current != null && this.Filter.Current != null)
			{
				if (this.Filter.Current.CustomerID.HasValue)
				{
					Customer customer = PXSelect<Customer,
						Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
						.Select(this, Filter.Current.CustomerID);
					if (customer != null)
					{
						Dictionary<string, string> parameters = new Dictionary<string, string>();

						Export(parameters, this.Details.Current);
						parameters[ARStatementReportParams.Parameters.CustomerID] = customer.AcctCD;

						string reportID = (customer.PrintCuryStatements ?? false) ? ARStatementReportParams.CS_CuryStatementReportID : ARStatementReportParams.CS_StatementReportID;
						throw new PXReportRequiredException(parameters, reportID, "AR Statement Report");
					}
				}
			}
			return Filter.Select();
		}
		protected static void Export(Dictionary<string, string> aRes, ARStatementForCustomer.DetailsResult aDetail)
		{
			aRes[ARStatementReportParams.Parameters.BranchID] = PXAccess.GetBranchCD(aDetail.BranchID);
			aRes[ARStatementReportParams.Parameters.StatementCycleID] = aDetail.StatementCycleId;
			aRes[ARStatementReportParams.Parameters.StatementDate] = aDetail.StatementDate.Value.ToString("d", CultureInfo.InvariantCulture);
		}
		#endregion
	}
}