using System;
using System.Collections;
using System.Collections.Generic;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.GL;
using SP.Objects.CR;

namespace SP.Objects.SP
{
    [DashboardType((int)DashboardTypeAttribute.Type.Default, TableAndChartDashboardTypeAttribute._AMCHARTS_DASHBOART_TYPE)]
    public class SPContractInquiry : PXGraph<SPContractInquiry>
	{
		#region Filter
		public static class ServiceContractStatus
		{
			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute()
					: base(
					new string[] { AllStatus, Draft, InApproval, PendingActivation, Activated, Expired, InUpgrade, },
					new string[] { "All", "Draft", "Pending Approval", "Pending Activation", "Active", "Expired", "Pending Upgrade" }) { ; }
			}
			public const string AllStatus = "L";
			public const string Draft = "D";
			public const string InApproval = "I";
			public const string Activated = "A";
			public const string Expired = "E";
			public const string InUpgrade = "U";
			public const string PendingActivation = "P";

			public class ContractStatusAllStatus : Constant<string>
			{
				public ContractStatusAllStatus() : base(ServiceContractStatus.AllStatus) { ;}
			}
			public class ContractStatusActivated : Constant<string>
			{
				public ContractStatusActivated() : base(ServiceContractStatus.Activated) { ;}
			}
			public class ContractStatusExpired : Constant<string>
			{
				public ContractStatusExpired() : base(ServiceContractStatus.Expired) { ;}
			}
			
			public class ContractStatusUpgrade : Constant<string>
			{
				public ContractStatusUpgrade() : base(ServiceContractStatus.InUpgrade) { ;}
			}

			public class ContractStatusDraft : Constant<string>
			{
				public ContractStatusDraft() : base(ServiceContractStatus.Draft) { ;}
			}
			public class ContractStatusInApproval : Constant<string>
			{
				public ContractStatusInApproval() : base(ServiceContractStatus.InApproval) { ;}
			}
		}


		[Serializable]
		public class ContractStatusFilter : IBqlTable
		{
			#region Status
			public abstract class status : PX.Data.IBqlField
			{
			}
			protected String _Status;
			[PXString(1, IsFixed = true)]
			[ServiceContractStatus.List()]
			[PXDefault("L")]
			[PXUIField(DisplayName = "Status", Required = true, Visibility = PXUIVisibility.SelectorVisible)]
			public virtual String Status
			{
				get
				{
					return this._Status;
				}
				set
				{
					this._Status = value;
				}
			}
			#endregion
		}
		#endregion

		public SPContractInquiry()
		{
			FilteredItems.Cache.AllowUpdate = false;
		}

		#region Selects
		public PXFilter<ContractStatusFilter> Filter;

		[PXFilterable]
		public PXSelectJoinGroupBy<Contract,
                    LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contract.customerID>>,
                    LeftJoin<Contact, On<Contact.bAccountID, Equal<BAccount.bAccountID>>,
					LeftJoin<ContractBillingSchedule, On<ContractBillingSchedule.contractID, Equal<Contract.contractID>>,
					LeftJoin<Customer, On<Customer.bAccountID, Equal<Contract.customerID>>>>>>, 
					Where<Contract.customerID, IsNotNull,
						And<Contract.isTemplate, Equal<boolFalse>,
						And<Contract.baseType, Equal<Contract.ContractBaseType>,
						And<Contract.status, NotEqual<Contract.status.canceled>,
						And2<Where<Current<ContractStatusFilter.status>, IsNull, 
							Or<Contract.status, Equal<Current<ContractStatusFilter.status>>>>,
						And<
						Where2<
						Where2<MatchWithBAccount<Contract.customerID, Current<AccessInfo.userID>>, 
								And<Contract.customerID, IsNotNull>>, 
						Or<
						Where2<MatchWithBAccount<ContractBillingSchedule.accountID, Current<AccessInfo.userID>>, 
							And<ContractBillingSchedule.accountID, IsNotNull>>>>>>>>>>,
				Aggregate<GroupBy<Contract.contractID>>,
				OrderBy<Asc<Contract.contractCD>>> FilteredItems;
		#endregion

		public virtual IEnumerable filteredItems(PXAdapter adapter)
		{
			if (Filter.Current.Status == ServiceContractStatus.AllStatus || Filter.Current.Status == null)
			{
				foreach (var license in PXSelectJoinGroupBy<Contract,
                    LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contract.customerID>>,
                    LeftJoin<Contact, On<Contact.bAccountID, Equal<BAccount.bAccountID>>,
					LeftJoin<ContractBillingSchedule, On<ContractBillingSchedule.contractID, Equal<Contract.contractID>>,
					LeftJoin<Customer, On<Customer.bAccountID, Equal<Contract.customerID>>>>>>,
					Where<Contract.customerID, IsNotNull,
						And<Contract.isTemplate, Equal<boolFalse>,
						And<Contract.baseType, Equal<Contract.ContractBaseType>,
						And<Contract.status, NotEqual<Contract.status.canceled>, 
						And<
						Where2<
						Where2<MatchWithBAccount<Contract.customerID, Current<AccessInfo.userID>>,
								And<Contract.customerID, IsNotNull>>,
						Or<
						Where2<MatchWithBAccount<ContractBillingSchedule.accountID, Current<AccessInfo.userID>>,
							And<ContractBillingSchedule.accountID, IsNotNull>>>>>>>>>,
				Aggregate<GroupBy<Contract.contractID>>,
				OrderBy<Asc<Contract.contractCD>>>.Select(this))
				{
					yield return license;
				}
			}
			else
			{
				foreach (var license in PXSelectJoinGroupBy<Contract,
                    LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contract.customerID>>,
                    LeftJoin<Contact, On<Contact.bAccountID, Equal<BAccount.bAccountID>>,
					LeftJoin<ContractBillingSchedule, On<ContractBillingSchedule.contractID, Equal<Contract.contractID>>,
					LeftJoin<Customer, On<Customer.bAccountID, Equal<Contract.customerID>>>>>>,
					Where<Contract.customerID, IsNotNull,
						And<Contract.isTemplate, Equal<boolFalse>,
						And<Contract.baseType, Equal<Contract.ContractBaseType>,
						And<Contract.status, NotEqual<Contract.status.canceled>, 
						And2<Where<Current<ContractStatusFilter.status>, IsNull,
							Or<Contract.status, Equal<Current<ContractStatusFilter.status>>>>,
						And<
						Where2<
						Where2<MatchWithBAccount<Contract.customerID, Current<AccessInfo.userID>>,
								And<Contract.customerID, IsNotNull>>,
						Or<
						Where2<MatchWithBAccount<ContractBillingSchedule.accountID, Current<AccessInfo.userID>>,
							And<ContractBillingSchedule.accountID, IsNotNull>>>>>>>>>>,
				Aggregate<GroupBy<Contract.contractID>>,
				OrderBy<Asc<Contract.contractCD>>>.Select(this))
				{
					yield return license;
				}
			}
		}

		#region Actions
        public PXCancel<ContractStatusFilter> Cancel;

		public PXAction<ContractStatusFilter> ViewContract;
		[PXUIField(Visible = false)]
		public virtual IEnumerable viewContract(PXAdapter adapter)
		{
			if (FilteredItems.Current != null)
			{
				//PXRedirectHelper.TryRedirect(FilteredItems.Cache, FilteredItems.Current, "Contract", PXRedirectHelper.WindowMode.Same);
                PXRedirectHelper.TryRedirect(this, FilteredItems.Current, PXRedirectHelper.WindowMode.InlineWindow);
			}
			return adapter.Get();
		}
    
		#endregion
	}
}