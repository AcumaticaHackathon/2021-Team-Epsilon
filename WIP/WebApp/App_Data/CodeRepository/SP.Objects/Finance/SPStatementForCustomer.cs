using System;
using System.Collections.Generic;
using System.Globalization;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;

namespace SP.Objects.Finance
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

	public class SPStatementForCustomer : PXGraph<SPStatementForCustomer>
	{
		[System.SerializableAttribute()]
		public partial class SPStatementForCustomerParameters : IBqlTable
		{
			#region CustomerID

			public abstract class customerID : PX.Data.IBqlField
			{
			}

			protected Int32? _CustomerID;

			[PXInt()]
			[PXDefault()]
			[PXUIField(DisplayName = "Customer")]
			[Customer(DescriptionField = typeof (Customer.acctName))]
			public virtual Int32? CustomerID
			{
				get { return this._CustomerID; }
				set { this._CustomerID = value; }
			}

			#endregion

			#region FromDate

			public abstract class fromDate : PX.Data.IBqlField
			{
			}

			protected DateTime? _FromDate;

			[PXDate(MaxValue = "06/06/2079", MinValue = "01/01/1900")]
			[PXDefault(typeof (AccessInfo.businessDate))]
			[PXUIField(DisplayName = "From Date", Visibility = PXUIVisibility.Visible)]
			public virtual DateTime? FromDate
			{
				get { return this._FromDate; }
				set { this._FromDate = value; }
			}

			#endregion

			#region TillDate

			public abstract class tillDate : PX.Data.IBqlField
			{
			}

			protected DateTime? _TillDate;

			[PXDate(MaxValue = "06/06/2079", MinValue = "01/01/1900")]
			[PXDefault(typeof (AccessInfo.businessDate))]
			[PXUIField(DisplayName = "To Date", Visibility = PXUIVisibility.Visible)]
			public virtual DateTime? TillDate
			{
				get { return this._TillDate; }
				set { this._TillDate = value; }
			}

			#endregion
		}

		public PXFilter<SPStatementForCustomerParameters> Filter;
		public PXCancel<SPStatementForCustomerParameters> Cancel;
		[PXFilterable] 
		public PXSelect<ARStatementForCustomer.DetailsResult> Details;

		public SPStatementForCustomer()
		{
			ARSetup setup = ARSetup.Current;
			Details.Cache.AllowDelete = false;
			Details.Cache.AllowInsert = false;
			Details.Cache.AllowUpdate = false;
		}

		public PXSetup<ARSetup> ARSetup;

		protected virtual System.Collections.IEnumerable details()
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
			Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.Select(
				this, header.CustomerID);
			if (customer != null)
			{
				bool useCurrency = customer.PrintCuryStatements ?? false;
				PX.Objects.GL.Company company = PXSelect<PX.Objects.GL.Company>.Select(this);

				PXResult<ARStatement> temp = null;

				if (header.FromDate == null)
					header.FromDate = DateTime.MinValue;

				if (header.TillDate == null)
					header.TillDate = DateTime.MaxValue; 

				foreach (ARStatement st in PXSelect<ARStatement,
					Where<ARStatement.customerID, Equal<Required<ARStatement.customerID>>,
						And<ARStatement.statementDate, GreaterEqual<Required<ARStatement.statementDate>>,
						And<ARStatement.statementDate, LessEqual<Required<ARStatement.statementDate>>>>>,
					OrderBy<Asc<ARStatement.statementCycleId, Asc<ARStatement.statementDate, Asc<ARStatement.curyID>>>>>
					.Select(this, header.CustomerID, header.FromDate, header.TillDate))
				{
					ARStatementForCustomer.DetailsResult res = new ARStatementForCustomer.DetailsResult();
					res.Copy(st, customer);
					if (useCurrency)
					{
						ARStatementForCustomer.DetailsResult last = curyResult.Count > 0 ? curyResult[curyResult.Count - 1] : null;
						if (last != null
						    && last.StatementCycleId == res.StatementCycleId
						    && last.StatementDate == res.StatementDate && last.CuryID == res.CuryID)
						{
							last.Append(res);
						}
						else
						{
							curyResult.Add(res);
						}
						//curyResult.Add(res);
					}
					else
					{
						ARStatementForCustomer.DetailKey key = new ARStatementForCustomer.DetailKey(res.StatementDate.Value,
						                                                                            res.StatementCycleId);
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
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.curyStatementBalance>(this.Details.Cache, null,
				                                                                                         useCurrency);
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.curyOverdueBalance>(this.Details.Cache, null,
				                                                                                       useCurrency);
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.statementBalance>(this.Details.Cache, null,
				                                                                                     !useCurrency);
				PXUIFieldAttribute.SetVisible<ARStatementForCustomer.DetailsResult.overdueBalance>(this.Details.Cache, null,
				                                                                                   !useCurrency);

				return useCurrency ? (System.Collections.IEnumerable) curyResult : (System.Collections.IEnumerable) result.Values;
				//return (System.Collections.IEnumerable)result.Values;
			}
			return curyResult;
		}

		protected virtual void SPStatementForCustomerParameters_FromDate_FieldDefaulting(PXCache sender,
		                                                                                 PXFieldDefaultingEventArgs e)
		{
			var row = e.Row as SPStatementForCustomerParameters;
			if (row == null) return;
			row.FromDate = PXTimeZoneInfo.Now.AddMonths(-6);
		}

		protected virtual void SPStatementForCustomerParameters_CustomerID_FieldDefaulting(PXCache sender,
		                                                                                   PXFieldDefaultingEventArgs e)
		{
			var row = e.Row as SPStatementForCustomerParameters;
			if (row == null) return;
			row.CustomerID = ReadCurrentAccount().With(_ => _.BAccountID);
		}

		private BAccount ReadCurrentAccount()
		{
			Guid userId = PXAccess.GetUserID();
			if (userId == Guid.Empty) return null;
			var res = (PXResultset<BAccount>) PXSelectJoin<BAccount,
				                                  InnerJoin<Contact, On<Contact.bAccountID, Equal<BAccount.bAccountID>>>,
				                                  Where<Contact.userID, Equal<Required<Contact.userID>>>>.
				                                  Select(this, userId);
			return res != null && res.Count > 0 ? (BAccount) (res[0][typeof (BAccount)]) : null;
		}

		#region Sub-screen Navigation Button
		public PXAction<SPStatementForCustomerParameters> printReport;
		[PXUIField(DisplayName = "Print Statement", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public System.Collections.IEnumerable PrintReport(PXAdapter adapter)
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

						parameters[ARStatementReportParams.Parameters.PrintOnPaper] =
							customer.PrintStatements == true ?
							ARStatementReportParams.BoolValues.True :
							ARStatementReportParams.BoolValues.False;
						parameters[ARStatementReportParams.Parameters.SendByEmail] =
							customer.SendStatementByEmail == true ?
							ARStatementReportParams.BoolValues.True :
							ARStatementReportParams.BoolValues.False;

						string reportID = (customer.PrintCuryStatements ?? false) ? ARStatementReportParams.CS_CuryStatementReportID : ARStatementReportParams.CS_StatementReportID;
						throw new PXReportRequiredException(parameters, reportID, "AR Statement Report");
					}
				}
			}
			return Filter.Select();
		}
		protected static void Export(Dictionary<string, string> aRes, ARStatementForCustomer.DetailsResult aDetail)
		{
			aRes[ARStatementReportParams.Parameters.StatementCycleID] = aDetail.StatementCycleId;
			aRes[ARStatementReportParams.Parameters.StatementDate] = aDetail.StatementDate.Value.ToString("d", CultureInfo.InvariantCulture);
		}


		#endregion
	}
}