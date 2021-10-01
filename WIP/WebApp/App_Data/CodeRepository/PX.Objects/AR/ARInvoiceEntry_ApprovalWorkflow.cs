using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.EP;

namespace PX.Objects.AR
{
	using State = ARDocStatus;
	using static ARInvoice;
	using static BoundedTo<ARInvoiceEntry, ARInvoice>;

	public class ARApprovalSettings : EPApprovalSettings<ARSetupApproval> { }
	
	public class ARInvoiceEntry_ApprovalWorkflow : PXGraphExtension<ARInvoiceEntry_Workflow, ARInvoiceEntry>
	{
		private static bool ApprovalIsActive => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>();


		[PXWorkflowDependsOnType(typeof(ARSetupApproval))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ARInvoiceEntry, ARInvoice>());
		}

		protected virtual void Configure(WorkflowContext<ARInvoiceEntry, ARInvoice> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<ARRegister.rejected.IsEqual<True>>(),
				IsApproved
					= Bql<ARRegister.approved.IsEqual<True>>(),
				IsNotApproved
					= Bql<ARRegister.approved.IsEqual<False>.And<ARRegister.rejected.IsEqual<False>>>(),
				IsApprovalDisabled
					= context.Conditions.FromBqlType(ARApprovalSettings
						.IsApprovalDisabled<docType, ARDocType, 
						Where<status.IsNotIn<ARDocStatus.pendingApproval, ARDocStatus.rejected>>>()),
				
				IsAutoPendingApproval
					= Bql<
						hold.IsEqual<False>.
						And<ARRegister.approved.IsEqual<False>>.
						And<origModule.IsNotEqual<GL.BatchModule.moduleSO>>>(),
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<ARInvoiceEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.releaseFromHold)
					.PlaceAfter(g => g.releaseFromHold)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<ARRegister.approved>(e => e.SetFromValue(true))));
			
			var reject = context.ActionDefinitions
				.CreateExisting<ARInvoiceEntry_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<ARRegister.rejected>(e => e.SetFromValue(true))));

			Workflow.ConfiguratorFlow InjectApprovalWorkflow(Workflow.ConfiguratorFlow flow)
			{
				const string initialState = "_";

				return flow
					.WithFlowStates(states =>
					{
						states.Add<State.pendingApproval>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(approve, a => a.IsDuplicatedInToolbar());
									actions.Add(reject, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.createSchedule);
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.printAREdit);
									actions.Add(g => g.customerDocuments);
								}).WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnConfirmSchedule);
									handlers.Add(g => g.OnUpdateStatus);
								});
						});
						states.Add<State.rejected>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.printAREdit);
									actions.Add(g => g.customerDocuments);
								});
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.UpdateGroupFrom(initialState, ts =>
						{
							ts.Add(t => t // New Pending Approval
								.To<State.pendingApproval>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsAutoPendingApproval)
								);
						});

						transitions.UpdateGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.pendingApproval>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsNotApproved)
								);
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsRejected));
							ts.Add(t => t
								.To<State.pendingApproval>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsAutoPendingApproval));
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsRejected));
						});


						transitions.AddGroupFrom<State.pendingApproval>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(context.Conditions.Get("IsOnHold")));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(approve)
								.When(context.Conditions.Get("IsOnCreditHold")));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(approve)
								.When(context.Conditions.Get("IsPendingPrint")));
							ts.Add(t => t
								.To<State.pendingEmail>()
								.IsTriggeredOn(approve)
								.When(context.Conditions.Get("IsPendingEmail")));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(approve)
								.When(context.Conditions.Get("IsBalanced")));
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(reject)
								.When(conditions.IsRejected));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist());
							ts.Add(t => t.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
						});

						transitions.AddGroupFrom<State.rejected>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								);
						});
					});
			}

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.UpdateDefaultFlow(InjectApprovalWorkflow)
					.WithActions(actions =>
					{
						actions.Add(approve);
						actions.Add(reject);
						actions.Update(
							g => g.putOnHold,
							a => a.WithFieldAssignments(fas =>
							{
								fas.Add<ARRegister.approved>(f => f.SetFromValue(false));
								fas.Add<ARRegister.rejected>(f => f.SetFromValue(false));
							}));
					});
			});
		}
		public PXAction<ARInvoice> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<ARInvoice> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}