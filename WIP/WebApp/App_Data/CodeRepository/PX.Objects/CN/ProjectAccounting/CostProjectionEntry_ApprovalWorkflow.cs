using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PM;

namespace PX.Objects.CN.ProjectAccounting
{
	using static BoundedTo<CostProjectionEntry, PMCostProjection>;

	public partial class CostProjectionEntry_ApprovalWorkflow : PXGraphExtension<CostProjectionEntry_Workflow, CostProjectionEntry>
	{
		private class CostProjectionSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<CostProjectionSetupApproval>(nameof(CostProjectionSetupApproval), typeof(PMSetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord setup = PXDatabase.SelectSingle<PMSetup>(new PXDataField<PMSetup.costProjectionApprovalMapID>()))
				{
					if (setup != null)
						RequestApproval = setup.GetInt32(0).HasValue;
				}
			}
		}

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && CostProjectionSetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(PMSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<CostProjectionEntry, PMCostProjection>());
		}

		protected virtual void Configure(WorkflowContext<CostProjectionEntry, PMCostProjection> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<PMCostProjection.rejected.IsEqual<True>>(),
				IsApproved
					= Bql<PMCostProjection.approved.IsEqual<True>>(),
				IsNotApproved
					= Bql<PMCostProjection.approved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<PMCostProjection.status.IsNotIn<CostProjectionStatus.pendingApproval, CostProjectionStatus.rejected>>()
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<CostProjectionEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.refreshBudget)
					.PlaceAfter(g => g.refreshBudget)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<PMCostProjection.approved>(e => e.SetFromValue(true))));

			var reject = context.ActionDefinitions
				.CreateExisting<CostProjectionEntry_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<PMCostProjection.rejected>(e => e.SetFromValue(true))));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<CostProjectionStatus.pendingApproval>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.hold);
										actions.Add(g => g.createRevision, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<CostProjectionStatus.rejected>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.hold, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.createRevision, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom<CostProjectionStatus.onHold>(ts =>
							{
								ts.Update(t => t
									.To<CostProjectionStatus.open>()
									.IsTriggeredOn(g => g.removeHold), t => t
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<CostProjectionStatus.pendingApproval>()
									.IsTriggeredOn(g => g.removeHold)
									.When(conditions.IsNotApproved));
							});
							transitions.AddGroupFrom<CostProjectionStatus.pendingApproval>(ts =>
							{
								ts.Add(t => t
									.To<CostProjectionStatus.open>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<CostProjectionStatus.rejected>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));
								ts.Add(t => t
									.To<CostProjectionStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
							});
							transitions.AddGroupFrom<CostProjectionStatus.rejected>(ts =>
							{
								ts.Add(t => t
									.To<CostProjectionStatus.onHold>()
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
								fa.Add<PMCostProjection.approved>(f => f.SetFromValue(false));
								fa.Add<PMCostProjection.rejected>(f => f.SetFromValue(false));
							}));
					}));
		}

		public PXAction<PMCostProjection> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<PMCostProjection> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}