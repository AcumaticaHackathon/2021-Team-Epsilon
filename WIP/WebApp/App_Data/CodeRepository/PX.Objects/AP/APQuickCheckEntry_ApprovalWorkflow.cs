using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.AP.Standalone;

namespace PX.Objects.AP
{
	using State = APDocStatus;
	using static APQuickCheck;
	using static BoundedTo<APQuickCheckEntry, APQuickCheck>;

	public class APQuickCheckEntry_ApprovalWorkflow : PXGraphExtension<APQuickCheckEntry_Workflow, APQuickCheckEntry>
	{
		private static bool ApprovalIsActive => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>();


		[PXWorkflowDependsOnType(typeof(APSetupApproval))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<APQuickCheckEntry, APQuickCheck>());
		}

		protected virtual void Configure(WorkflowContext<APQuickCheckEntry, APQuickCheck> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<APRegister.rejected.IsEqual<True>>(),
				IsApproved
					= Bql<APRegister.approved.IsEqual<True>>(),
				IsNotApproved
					= Bql<APRegister.approved.IsEqual<False>.And<APRegister.rejected.IsEqual<False>>>(),
				IsApprovalDisabled
					=  context.Conditions.FromBqlType(
						APApprovalSettings.IsApprovalDisabled<docType, APDocType, 
								Where<status.IsNotIn<APDocStatus.pendingApproval, APDocStatus.rejected>>>
							(APDocType.QuickCheck)),
				IsAutoPendingApproval
					= Bql<hold.IsEqual<False>.And<APRegister.approved.IsEqual<False>>>(),
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<APQuickCheckEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.releaseFromHold)
					.PlaceAfter(g => g.releaseFromHold)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<APRegister.approved>(e => e.SetFromValue(true))));
			
			var reject = context.ActionDefinitions
				.CreateExisting<APQuickCheckEntry_ApprovalWorkflow>(g => g.reject, a => a
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
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.printAPEdit);
									actions.Add(g => g.vendorDocuments);


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
								.IsTriggeredOn(g => g.OnUpdateStatus)
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
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsRejected));
						});
						transitions.AddGroupFrom<State.pendingApproval>(ts =>
						{
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(approve)
								.When(context.Conditions.Get("IsNotPrinted")));
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
								fas.Add<APRegister.approved>(f => f.SetFromValue(false));
								fas.Add<APRegister.rejected>(f => f.SetFromValue(false));
							}));
					});
			});
		}
		public PXAction<APQuickCheck> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<APQuickCheck> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}