using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PM;

namespace PX.Objects.PM.ChangeRequest
{
	using static BoundedTo<ChangeRequestEntry, PMChangeRequest>;

	public partial class ChangeRequestEntry_ApprovalWorkflow : PXGraphExtension<ChangeRequestEntry_Workflow, ChangeRequestEntry>
	{
		private class ChangeRequestSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<ChangeRequestSetupApproval>(nameof(ChangeRequestSetupApproval), typeof(PMSetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord setup = PXDatabase.SelectSingle<PMSetup>(new PXDataField<PMSetup.changeOrderApprovalMapID>()))
				{
					if (setup != null)
						RequestApproval = setup.GetInt32(0).HasValue;
				}
			}
		}

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && ChangeRequestSetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(PMSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ChangeRequestEntry, PMChangeRequest>());
		}

		protected virtual void Configure(WorkflowContext<ChangeRequestEntry, PMChangeRequest> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<PMChangeRequest.rejected.IsEqual<True>>(),
				IsApproved
					= Bql<PMChangeRequest.approved.IsEqual<True>>(),
				IsNotApproved
					= Bql<PMChangeRequest.approved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<PMChangeRequest.status.IsNotIn<ChangeRequestStatus.pendingApproval, ChangeRequestStatus.rejected>>()
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<ChangeRequestEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.removeHold)
					.PlaceAfter(g => g.removeHold)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<PMChangeRequest.approved>(e => e.SetFromValue(true))));

			var reject = context.ActionDefinitions
				.CreateExisting<ChangeRequestEntry_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<PMChangeRequest.rejected>(e => e.SetFromValue(true))));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ChangeRequestStatus.pendingApproval>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.hold);
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.send);
									});
							});
							fss.Add<ChangeRequestStatus.rejected>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.hold, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.send);
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom<ChangeRequestStatus.onHold>(ts =>
							{
								ts.Update(t => t
									.To<ChangeRequestStatus.open>()
									.IsTriggeredOn(g => g.removeHold), t => t
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<ChangeRequestStatus.pendingApproval>()
									.IsTriggeredOn(g => g.removeHold)
									.When(conditions.IsNotApproved));
							});
							transitions.AddGroupFrom<ChangeRequestStatus.pendingApproval>(ts =>
							{
								ts.Add(t => t
									.To<ChangeRequestStatus.open>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<ChangeRequestStatus.rejected>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));
								ts.Add(t => t
									.To<ChangeRequestStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
							});
							transitions.AddGroupFrom<ChangeRequestStatus.rejected>(ts =>
							{
								ts.Add(t => t
									.To<ChangeRequestStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(approve);
						actions.Add(reject);
						actions.Update(
							g => g.hold,
							a => a.WithFieldAssignments(fa =>
							{
								fa.Add<PMChangeRequest.approved>(f => f.SetFromValue(false));
								fa.Add<PMChangeRequest.rejected>(f => f.SetFromValue(false));
							}));
					}));
		}

		public PXAction<PMChangeRequest> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<PMChangeRequest> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}