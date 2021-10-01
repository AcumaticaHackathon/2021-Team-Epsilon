using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AR;
using State = PX.Objects.AR.CustomerStatus;

namespace PX.Objects.CR.Workflows
{
	using static PX.Data.WorkflowAPI.BoundedTo<BusinessAccountMaint, BAccount>;

	public class BusinessAccountWorkflow : PXGraphExtension<BusinessAccountMaint>
	{
		public static bool IsActive() => false;

		public override void Configure(PXScreenConfiguration configuration)
		{
			var context = configuration.GetScreenConfigurationContext<BusinessAccountMaint, BAccount>();

			#region Conditions

			var conditions = new
			{
				IsBusinessAccount
					= context.Conditions.FromBql<BAccount.type.IsEqual<BAccountType.prospectType>>(),

				IsCustomer
					= context.Conditions.FromBql<BAccount.type.IsEqual<BAccountType.customerType>.Or<BAccount.type.IsEqual<BAccountType.combinedType>>>(),

				IsNotCustomer
					= context.Conditions.FromBql<BAccount.type.IsNotEqual<BAccountType.customerType>.And<BAccount.type.IsNotEqual<BAccountType.combinedType>>>(),

				IsVendor
					= context.Conditions.FromBql<BAccount.type.IsEqual<BAccountType.vendorType>.Or<BAccount.type.IsEqual<BAccountType.combinedType>>>(),

			}.AutoNameConditions();

			#endregion

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<BAccount.status>()
					.AddDefaultFlow(DefaultBusinessAccountFlow)
					.WithActions(actions =>
					{
						actions.Add(g => g.Activate, a => a
							.InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.ForcePersist)
							.MassProcessingScreen<UpdateBAccountMassProcess>());
						actions.Add(g => g.PutOnHold, a => a
							.InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.ForcePersist)
							.MassProcessingScreen<UpdateBAccountMassProcess>());
						actions.Add(g => g.PutOnCreditHold, a => a
							.InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.ForcePersist)
							.MassProcessingScreen<UpdateBAccountMassProcess>());
						actions.Add(g => g.RemoveCreditHold, a => a
							.InFolder(FolderType.ActionsFolder)
							.WithPersistOptions(ActionPersistOptions.ForcePersist)
							.MassProcessingScreen<UpdateBAccountMassProcess>());
						actions.Add(g => g.Deactivate, a => a
							.InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.ForcePersist)
							.MassProcessingScreen<UpdateBAccountMassProcess>());

						actions.Add<BusinessAccountMaint.ExtendToCustomer>(e => e.extendToCustomer, a=>a.WithPersistOptions(ActionPersistOptions.ForcePersist));
						actions.Add<BusinessAccountMaint.ExtendToCustomer>(e => e.viewCustomer, a=>a.WithPersistOptions(ActionPersistOptions.ForcePersist));
						actions.Add<BusinessAccountMaint.ExtendToVendor>(e => e.extendToVendor, a => a.InFolder(FolderType.ActionsFolder).WithPersistOptions(ActionPersistOptions.ForcePersist));
						actions.Add<BusinessAccountMaint.ExtendToVendor>(e => e.viewVendor, a => a.InFolder(FolderType.ActionsFolder).WithPersistOptions(ActionPersistOptions.ForcePersist));
						actions.Add(g => g.ChangeID, a => a.InFolder(FolderType.ActionsFolder, nameof(BusinessAccountMaint.ExtendToVendor.viewVendor)).WithPersistOptions(ActionPersistOptions.ForcePersist));
						actions.Add<BusinessAccountMaint.DefContactAddressExt>(e => e.ValidateAddresses, a => a.InFolder(FolderType.ActionsFolder).WithPersistOptions(ActionPersistOptions.ForcePersist));
						actions.Add<BusinessAccountMaint.CreateContactFromAccountGraphExt>(e => e.CreateContact, a => a.InFolder(FolderType.ActionsFolder).WithPersistOptions(ActionPersistOptions.ForcePersist));
						actions.Add<BusinessAccountMaint.CreateLeadFromAccountGraphExt>(e => e.CreateLead, a => a.InFolder(FolderType.ActionsFolder).WithPersistOptions(ActionPersistOptions.ForcePersist));

						actions.Add(g => g.SetAsOneTime, a => a
							.InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.ForcePersist)
							.MassProcessingScreen<UpdateBAccountMassProcess>());
						actions.Add(g => g.SetAsRegular, a => a
							.InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.ForcePersist)
							.MassProcessingScreen<UpdateBAccountMassProcess>());
					})
					.ForbidFurtherChanges();
			});

			Workflow.IConfigured DefaultBusinessAccountFlow(Workflow.INeedStatesFlow flow)
			{
				#region States

				var prospectState = context.FlowStates.Create(State.Prospect, state => state
					.IsInitial()
					.WithActions(actions =>
					{
						actions.Add<BusinessAccountMaint.ExtendToCustomer>(e => e.extendToCustomer);
						actions.Add(g => g.PutOnHold);
						actions.Add(g => g.Deactivate);
					}));

				var activeState = context.FlowStates.Create(State.Active, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.PutOnHold);
						actions.Add(g => g.PutOnCreditHold);
						actions.Add(g => g.Deactivate);
						actions.Add(g => g.SetAsOneTime);
					}));

				var holdState = context.FlowStates.Create(State.Hold, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.Activate);
						actions.Add(g => g.Deactivate);
					}));

				var creditHoldState = context.FlowStates.Create(State.CreditHold, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.PutOnHold);
						actions.Add(g => g.RemoveCreditHold);
						actions.Add(g => g.Deactivate);
					}));

				var oneTimeState = context.FlowStates.Create(State.OneTime, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.SetAsRegular);
						actions.Add(g => g.Deactivate);
					}));

				var inactiveState = context.FlowStates.Create(State.Inactive, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.Activate);
					}));

				#endregion

				return flow
					.WithFlowStates(states =>
					{
						states.Add(prospectState);
						states.Add(activeState);
						states.Add(holdState);
						states.Add(creditHoldState);
						states.Add(oneTimeState);
						states.Add(inactiveState);
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(State.Prospect, ts =>
						{
							ts.Add(t => t
								.To(State.Active)
								.IsTriggeredOn<BusinessAccountMaint.ExtendToCustomer>(e => e.extendToCustomer)
								.DoesNotPersist());

							ts.Add(t => t
								.To(State.Hold)
								.IsTriggeredOn(g => g.PutOnHold));

							ts.Add(t => t
								.To(State.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(State.Active, ts =>
						{
							ts.Add(t => t
								.To(State.Hold)
								.IsTriggeredOn(g => g.PutOnHold));

							ts.Add(t => t
								.To(State.CreditHold)
								.IsTriggeredOn(g => g.PutOnCreditHold));

							ts.Add(t => t
								.To(State.Inactive)
								.IsTriggeredOn(g => g.Deactivate));

							ts.Add(t => t
								.To(State.OneTime)
								.IsTriggeredOn(g => g.SetAsOneTime));
						});

						transitions.AddGroupFrom(State.Hold, ts =>
						{
							ts.Add(t => t
								.To(State.Prospect)
								.IsTriggeredOn(g => g.Activate)
								.When(conditions.IsNotCustomer));

							ts.Add(t => t
								.To(State.Active)
								.IsTriggeredOn(g => g.Activate)
								.When(conditions.IsCustomer));

							ts.Add(t => t
								.To(State.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(State.CreditHold, ts =>
						{
							ts.Add(t => t
								.To(State.Hold)
								.IsTriggeredOn(g => g.PutOnHold));

							ts.Add(t => t
								.To(State.Prospect)
								.IsTriggeredOn(g => g.RemoveCreditHold)
								.When(conditions.IsNotCustomer));

							ts.Add(t => t
								.To(State.Active)
								.IsTriggeredOn(g => g.RemoveCreditHold)
								.When(conditions.IsCustomer));

							ts.Add(t => t
								.To(State.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(State.OneTime, ts =>
						{
							ts.Add(t => t
								.To(State.Prospect)
								.IsTriggeredOn(g => g.SetAsRegular)
								.When(conditions.IsNotCustomer));

							ts.Add(t => t
								.To(State.Active)
								.IsTriggeredOn(g => g.SetAsRegular)
								.When(conditions.IsCustomer));

							ts.Add(t => t
								.To(State.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(State.Inactive, ts =>
						{
							ts.Add(t => t
								.To(State.Prospect)
								.IsTriggeredOn(g => g.Activate)
								.When(conditions.IsNotCustomer));

							ts.Add(t => t
								.To(State.Active)
								.IsTriggeredOn(g => g.Activate)
								.When(conditions.IsCustomer));
						});
					});
			}
		}
	}
}
