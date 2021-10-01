using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR.Workflows;

namespace PX.Objects.AR
{
	using static PX.Data.WorkflowAPI.BoundedTo<CustomerMaint, Customer>;

	public class CustomerMaint_Workflow : PXGraphExtension<CustomerMaint>
	{
		public static bool IsActive() => false;

		internal static void SetStatusTo(Customer entity, string targetStatus)
		{
			var customerMaint = PXGraph.CreateInstance<CustomerMaint>();
			customerMaint.BAccount.Current = customerMaint.BAccount.Cache.CreateCopy(entity) as Customer;
			customerMaint.Cancel.Press();

			string action = null;

			switch (targetStatus)
			{
				case CustomerStatus.Active:
					switch (entity.Status)
					{
						case CustomerStatus.OneTime:
							action = nameof(CustomerMaint.SetAsRegular);
							break;

						case CustomerStatus.CreditHold:
							action = nameof(CustomerMaint.RemoveCreditHold);
							break;

						default:
							action = nameof(CustomerMaint.Activate);
							break;
					}
					break;

				case CustomerStatus.Hold:
					action = nameof(CustomerMaint.PutOnHold);
					break;

				case CustomerStatus.CreditHold:
					action = nameof(CustomerMaint.PutOnCreditHold);
					break;

				case CustomerStatus.OneTime:
					action = nameof(CustomerMaint.SetAsOneTime);
					break;

				case CustomerStatus.Inactive:
					action = nameof(CustomerMaint.Deactivate);
					break;
			}

			if (action == null)
				return;

			customerMaint.Actions[action].Press();
		}

		public override void Configure(PXScreenConfiguration configuration)
		{
			var context = configuration.GetScreenConfigurationContext<CustomerMaint, Customer>();

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<Customer.status>()
					.AddDefaultFlow(DefaultCustomerFlow)
					.WithActions(actions =>
					{
						// "Actions" folder
						actions.Add(g => g.Activate, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.PutOnHold, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.PutOnCreditHold, a => a
							.InFolder(FolderType.ActionsFolder)
							.MassProcessingScreen<ARCustomerCreditHoldProcess>());
						actions.Add(g => g.RemoveCreditHold, a => a
							.InFolder(FolderType.ActionsFolder)
							.MassProcessingScreen<ARCustomerCreditHoldProcess>());
						actions.Add(g => g.Deactivate, a => a.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.newInvoiceMemo, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.newSalesOrder, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.newPayment, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.writeOffBalance, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.generateOnDemandStatement, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.regenerateLastStatement, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add<CustomerMaint.ExtendToVendor>(g => g.extendToVendor, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add<CustomerMaint.ExtendToVendor>(g => g.viewVendor, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add<CustomerMaint.DefContactAddressExt>(g => g.ValidateAddresses, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.viewBusnessAccount, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.viewRestrictionGroups, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add<CustomerMaint.CreateContactFromCustomerGraphExt>(g => g.CreateContact, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.ChangeID, a => a.InFolder(FolderType.ActionsFolder, nameof(CustomerMaint.CreateContactFromCustomerGraphExt.CreateContact)));

						actions.Add(g => g.SetAsOneTime, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.SetAsRegular, a => a.InFolder(FolderType.ActionsFolder));

						// "Inquiries" folder
						actions.Add(g => g.customerDocuments, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.statementForCustomer, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.salesPrice, a => a.InFolder(FolderType.InquiriesFolder));

						// "Reports" folder
						actions.Add(g => g.aRBalanceByCustomer, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.customerHistory, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.aRAgedPastDue, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.aRAgedOutstanding, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.aRRegister, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.customerDetails, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.customerStatement, a => a.InFolder(FolderType.ReportsFolder));
					})
					.ForbidFurtherChanges();
			});

			Workflow.IConfigured DefaultCustomerFlow(Workflow.INeedStatesFlow flow)
			{
				#region States

				var activeState = context.FlowStates.Create(CustomerStatus.Active, state => state
					.IsInitial()
					.WithActions(actions =>
					{
						actions.Add(g => g.PutOnHold);
						actions.Add(g => g.PutOnCreditHold);
						actions.Add(g => g.Deactivate);
						actions.Add(g => g.SetAsOneTime);

						actions.Add(g => g.newInvoiceMemo);
						actions.Add(g => g.newSalesOrder);
						actions.Add(g => g.newPayment);
						actions.Add(g => g.writeOffBalance);

						actions.Add(g => g.generateOnDemandStatement);
						actions.Add(g => g.regenerateLastStatement);

						actions.Add(g => g.viewBusnessAccount, a => a.IsDuplicatedInToolbar());
						actions.Add<CustomerMaint.CreateContactFromCustomerGraphExt>(g => g.CreateContact, a => a.IsDuplicatedInToolbar());
					}));

				var holdState = context.FlowStates.Create(CustomerStatus.Hold, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.Activate);
						actions.Add(g => g.Deactivate);

						actions.Add(g => g.newPayment);
						actions.Add(g => g.viewBusnessAccount, a => a.IsDuplicatedInToolbar());
					}));

				var creditHoldState = context.FlowStates.Create(CustomerStatus.CreditHold, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.PutOnHold);
						actions.Add(g => g.RemoveCreditHold);
						actions.Add(g => g.Deactivate);

						actions.Add(g => g.newPayment);
						actions.Add(g => g.writeOffBalance);
						actions.Add(g => g.generateOnDemandStatement);
						actions.Add(g => g.regenerateLastStatement);
						actions.Add(g => g.viewBusnessAccount, a => a.IsDuplicatedInToolbar());
					}));

				var oneTimeState = context.FlowStates.Create(CustomerStatus.OneTime, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.SetAsRegular);
						actions.Add(g => g.Deactivate);

						actions.Add(g => g.newInvoiceMemo);
						actions.Add(g => g.newSalesOrder);
						actions.Add(g => g.newPayment);
						actions.Add(g => g.writeOffBalance);
						actions.Add(g => g.generateOnDemandStatement);
						actions.Add(g => g.regenerateLastStatement);
						actions.Add(g => g.viewBusnessAccount, a => a.IsDuplicatedInToolbar());
						actions.Add<CustomerMaint.CreateContactFromCustomerGraphExt>(g => g.CreateContact, a => a.IsDuplicatedInToolbar());
					}));

				var inactiveState = context.FlowStates.Create(CustomerStatus.Inactive, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.Activate);

						actions.Add(g => g.generateOnDemandStatement);
						actions.Add(g => g.regenerateLastStatement);
						actions.Add(g => g.viewBusnessAccount, a => a.IsDuplicatedInToolbar());
					}));

				#endregion

				return flow
					.WithFlowStates(states =>
					{
						states.Add(activeState);
						states.Add(holdState);
						states.Add(creditHoldState);
						states.Add(oneTimeState);
						states.Add(inactiveState);
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(CustomerStatus.Active, ts =>
						{
							ts.Add(t => t
								.To(CustomerStatus.Hold)
								.IsTriggeredOn(g => g.PutOnHold));

							ts.Add(t => t
								.To(CustomerStatus.CreditHold)
								.IsTriggeredOn(g => g.PutOnCreditHold));

							ts.Add(t => t
								.To(CustomerStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));

							ts.Add(t => t
								.To(CustomerStatus.OneTime)
								.IsTriggeredOn(g => g.SetAsOneTime));
						});

						transitions.AddGroupFrom(CustomerStatus.Hold, ts =>
						{
							ts.Add(t => t
								.To(CustomerStatus.Active)
								.IsTriggeredOn(g => g.Activate));

							ts.Add(t => t
								.To(CustomerStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(CustomerStatus.CreditHold, ts =>
						{
							ts.Add(t => t
								.To(CustomerStatus.Hold)
								.IsTriggeredOn(g => g.PutOnHold));

							ts.Add(t => t
								.To(CustomerStatus.Active)
								.IsTriggeredOn(g => g.RemoveCreditHold));

							ts.Add(t => t
								.To(CustomerStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(CustomerStatus.OneTime, ts =>
						{
							ts.Add(t => t
								.To(CustomerStatus.Active)
								.IsTriggeredOn(g => g.SetAsRegular));

							ts.Add(t => t
								.To(CustomerStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(CustomerStatus.Inactive, ts =>
						{
							ts.Add(t => t
								.To(CustomerStatus.Active)
								.IsTriggeredOn(g => g.Activate));
						});
					});
			}
		}
	}
}
