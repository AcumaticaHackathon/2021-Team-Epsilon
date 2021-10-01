using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.CT
{
	using State = Contract.status;
	using static Contract;
	using static BoundedTo<ContractMaint, Contract>;

	public partial class ContractMaint_Workflow : PXGraphExtension<ContractMaint>
	{

		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<ContractMaint, Contract>());

		protected virtual void Configure(WorkflowContext<ContractMaint, Contract> context)
		{
			#region Conditions

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
			}.AutoNameConditions();

			#endregion
			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(flow =>
						flow
							.WithFlowStates(fss =>
								{
									fss.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
									fss.Add<State.draft>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.setup, a => a.IsDuplicatedInToolbar());
												actions.Add(g => g.setupAndActivate);
												actions.Add(g => g.ChangeID);
											}).WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnSetupContract);
												handlers.Add(g => g.OnActivateContract);
											});
									});
									fss.Add<State.pendingActivation>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.activate, a => a.IsDuplicatedInToolbar());
												actions.Add(g => g.undoBilling);
												actions.Add(g => g.ChangeID);
												
											}).WithFieldStates(fields =>
											{
												fields.AddField<startDate>(field => field.IsDisabled());
												fields.AddField<terminationDate>(field => field.IsDisabled());
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnActivateContract);
											});
									});
									fss.Add<State.active>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.bill, a => a.IsDuplicatedInToolbar());
												actions.Add(g => g.renew);
												actions.Add(g => g.terminate);
												actions.Add(g => g.upgrade);
												actions.Add(g => g.undoBilling);
												actions.Add(g => g.ChangeID);
												actions.Add(g => g.viewUsage);
											})
											.WithFieldStates(fields =>
											{
												fields.AddField<startDate>(field => field.IsDisabled());
												fields.AddField<activationDate>(field => field.IsDisabled());
												fields.AddField<terminationDate>(field => field.IsDisabled());
												fields.AddField<pendingSetup>(field => field.IsHidden());
												fields.AddField<pendingRecurring>(field => field.IsHidden());
												fields.AddField<pendingRenewal>(field => field.IsHidden());
												fields.AddField<totalPending>(field => field.IsHidden());
												fields.AddField<discountID>(field => field.IsDisabled());
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnExpireContract);
												handlers.Add(g => g.OnUpgradeContract);
												handlers.Add(g => g.OnCancelContract);
											});
									});
									fss.Add<State.expired>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.renew, a => a.IsDuplicatedInToolbar());
												actions.Add(g => g.terminate);
												actions.Add(g => g.undoBilling);
												actions.Add(g => g.ChangeID);
											})
											.WithFieldStates(fields =>
											{
												fields.AddField<startDate>(field => field.IsDisabled());
												fields.AddField<activationDate>(field => field.IsDisabled());
												fields.AddField<discountID>(field => field.IsDisabled());
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnActivateContract);
												handlers.Add(g => g.OnCancelContract);
											});
									});
									fss.Add<State.inUpgrade>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.activateUpgrade, a => a.IsDuplicatedInToolbar() );
												actions.Add(g => g.bill);
												actions.Add(g => g.undoBilling);
												actions.Add(g => g.ChangeID);
												actions.Add(g => g.viewUsage);
											})
											.WithFieldStates(fields =>
											{
												fields.AddField<startDate>(field => field.IsDisabled());
												fields.AddField<terminationDate>(field => field.IsDisabled());
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnExpireContract);
											});;
									});
									fss.Add<State.canceled>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.undoBilling);
												actions.Add(g => g.ChangeID);
											});
									});
								}
							)
							.WithTransitions(transitions =>
							{
								transitions.AddGroupFrom(initialState, ts =>
								{
									ts.Add(t => t.To<State.draft>().IsTriggeredOn(g => g.initializeState)); // New Hold
								});
								transitions.AddGroupFrom<State.draft>(ts =>
								{
									ts.Add(t => t
										.To<State.pendingActivation>()
										.IsTriggeredOn(g => g.setup));
									ts.Add(t => t
										.To<State.pendingActivation>()
										.IsTriggeredOn(g => g.OnSetupContract));
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.OnActivateContract));
								});
								transitions.AddGroupFrom<State.pendingActivation>(ts =>
								{
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.OnActivateContract));
									
								});
								transitions.AddGroupFrom<State.active>(ts =>
								{
									ts.Add(t => t
										.To<State.expired>()
										.IsTriggeredOn(g => g.OnExpireContract));
									ts.Add(t => t
										.To<State.canceled>()
										.IsTriggeredOn(g => g.OnCancelContract));
									ts.Add(t => t
										.To<State.inUpgrade>()
										.IsTriggeredOn(g => g.OnUpgradeContract));
								});
								transitions.AddGroupFrom<State.expired>(ts =>
								{
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.renew));
									ts.Add(t => t
										.To<State.canceled>()
										.IsTriggeredOn(g => g.OnCancelContract));
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.OnActivateContract));
								});
								transitions.AddGroupFrom<State.inUpgrade>(ts =>
								{
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.activateUpgrade));
									ts.Add(t => t
										.To<State.expired>()
										.IsTriggeredOn(g => g.OnExpireContract));
								});
								transitions.AddGroupFrom<State.canceled>(ts =>
								{
								});
							}))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.setup, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.activate, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.setupAndActivate, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.bill, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.renew, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.terminate, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.upgrade, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.activateUpgrade, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.undoBilling, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.ChangeID, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.viewUsage, c => c
							.InFolder(FolderType.InquiriesFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<Contract>()
							.OfEntityEvent<Contract.Events>(e => e.SetupContract)
							.Is(g => g.OnSetupContract)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<Contract>()
							.OfEntityEvent<Contract.Events>(e => e.ExpireContract)
							.Is(g => g.OnExpireContract)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<Contract>()
							.OfEntityEvent<Contract.Events>(e => e.ActivateContract)
							.Is(g => g.OnActivateContract)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<Contract>()
							.OfEntityEvent<Contract.Events>(e => e.CancelContract)
							.Is(g => g.OnCancelContract)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<Contract>()
							.OfEntityEvent<Contract.Events>(e => e.UpgradeContract)
							.Is(g => g.OnUpgradeContract)
							.UsesTargetAsPrimaryEntity());
					})
			);
		}
	}
}