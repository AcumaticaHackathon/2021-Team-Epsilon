using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Api.Models;
using PX.Objects.EP;

namespace PX.Objects.AP
{
	using State = APDocStatus;
	using static APInvoice;
	using static BoundedTo<APInvoiceEntry, APInvoice>;
	public class APApprovalSettings : EPApprovalSettings<APSetupApproval> { }
	
	public class APInvoiceEntry_ApprovalWorkflow : PXGraphExtension<APInvoiceEntry_Workflow, APInvoiceEntry>
	{
		private static bool ApprovalIsActive => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>();


		[PXWorkflowDependsOnType(typeof(APSetupApproval))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<APInvoiceEntry, APInvoice>());
		}

		protected virtual void Configure(WorkflowContext<APInvoiceEntry, APInvoice> context)
		{
			Type isApprovalDisabled = APApprovalSettings
				.IsApprovalDisabled<docType, APDocType, 
					Where<status.IsNotIn<APDocStatus.pendingApproval, APDocStatus.rejected>>>
					(APDocType.Invoice, APDocType.CreditAdj, APDocType.DebitAdj);
			
			if (APApprovalSettings.ApprovedDocTypes.Contains(APDocType.PrepaymentRequest))
			{
				isApprovalDisabled =
					APApprovalSettings.ComposeAnd<Where<docType, NotEqual<APDocType.prepayment>>>(isApprovalDisabled);
			}
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsNotOnHoldAndIsApproved
					= Bql<hold.IsEqual<False>.And<APRegister.approved.IsEqual<True>>>(),

				IsNotOnHoldAndIsNotApproved
					= Bql<hold.IsEqual<False>.And<APRegister.approved.IsEqual<False>>>(),
				
				IsRejected
					= Bql<APRegister.rejected.IsEqual<True>>(),

				IsApproved
					= Bql<APRegister.approved.IsEqual<True>>(),

				IsNotApproved
					= Bql<APRegister.approved.IsEqual<False>.And<APRegister.rejected.IsEqual<False>>>(),
				
				IsNotPendingApproval 
					= Bql<APRegister.status.IsNotEqual<State.pendingApproval>>(),
				
				IsApprovalDisabled
					= ApprovalIsActive 
						? context.Conditions.FromBqlType(isApprovalDisabled)
						: Bql<status.IsNotIn<APDocStatus.pendingApproval, APDocStatus.rejected>>(),
				
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<APInvoiceEntry_ApprovalWorkflow>(g => g.approve, a => a
				.InFolder(FolderType.ActionsFolder, g => g.releaseFromHold)
				.PlaceAfter(g => g.releaseFromHold)
				.IsHiddenWhen(conditions.IsApprovalDisabled)
				.WithFieldAssignments(fa => fa.Add<APRegister.approved>(e => e.SetFromValue(true))));
			
			var reject = context.ActionDefinitions
				.CreateExisting<APInvoiceEntry_ApprovalWorkflow>(g => g.reject, a => a
				.InFolder(FolderType.ActionsFolder, approve)
				.PlaceAfter(approve)
				.IsHiddenWhen(conditions.IsApprovalDisabled)
				.WithFieldAssignments(fa => fa.Add<APRegister.rejected>(e => e.SetFromValue(true))));

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
									actions.Add(g => g.printAPEdit);
									actions.Add(g => g.vendorDocuments);
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
									actions.Add(g => g.printAPEdit);
									actions.Add(g => g.vendorDocuments);
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
								.When(conditions.IsNotOnHoldAndIsNotApproved)
								);
							ts.Update(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.initializeState), t => t
								.When(conditions.IsNotOnHoldAndIsApproved)); // IsNotOnHold -> IsNotOnHoldAndIsApproved
						});

						transitions.UpdateGroupFrom<State.hold>(ts =>
						{
							ts.Update(
								t => t
									.To<State.balanced>()
									.IsTriggeredOn(g => g.releaseFromHold), t => t
									.When(conditions.IsApproved));
							ts.Add(t => t
								.To<State.pendingApproval>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsNotApproved));
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsRejected));
						});


						transitions.AddGroupFrom<State.pendingApproval>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(context.Conditions.Get("IsOnHold")));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(approve)
								.When(conditions.IsApproved));
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
								.DoesNotPersist());
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
								fas.Add<APRegister.approved>(f => f.SetFromValue(false));
								fas.Add<APRegister.rejected>(f => f.SetFromValue(false));
							}));
					});
			});
		}

		public PXAction<APInvoice> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<APInvoice> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}