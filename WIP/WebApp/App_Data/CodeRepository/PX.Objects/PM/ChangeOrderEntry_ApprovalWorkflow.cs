using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PM;

namespace PX.Objects.PM
{
	using static BoundedTo<ChangeOrderEntry, PMChangeOrder>;

	public partial class ChangeOrderEntry_ApprovalWorkflow : PXGraphExtension<ChangeOrderEntry_Workflow, ChangeOrderEntry>
	{
		private class PMChangeOrderSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<PMChangeOrderSetupApproval>(nameof(PMChangeOrderSetupApproval), typeof(PMSetup)).RequestApproval;

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

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && PMChangeOrderSetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(PMSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ChangeOrderEntry, PMChangeOrder>());
		}

		protected virtual void Configure(WorkflowContext<ChangeOrderEntry, PMChangeOrder> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<PMChangeOrder.rejected.IsEqual<True>>(),
				IsApproved
					= Bql<PMChangeOrder.approved.IsEqual<True>>(),
				IsNotApproved
					= Bql<PMChangeOrder.approved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<PMChangeOrder.status.IsNotIn<ChangeOrderStatus.pendingApproval, ChangeOrderStatus.rejected>>()
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<ChangeOrderEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.removeHold)
					.PlaceAfter(g => g.removeHold)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<PMChangeOrder.approved>(e => e.SetFromValue(true))));

			var reject = context.ActionDefinitions
				.CreateExisting<ChangeOrderEntry_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<PMChangeOrder.rejected>(e => e.SetFromValue(true))));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ChangeOrderStatus.pendingApproval>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.hold);
										actions.Add(g => g.send);
									});
							});
							fss.Add<ChangeOrderStatus.rejected>(flowState =>
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
							transitions.UpdateGroupFrom<ChangeOrderStatus.onHold>(ts =>
							{
								ts.Update(t => t
									.To<ChangeOrderStatus.open>()
									.IsTriggeredOn(g => g.removeHold), t => t
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<ChangeOrderStatus.pendingApproval>()
									.IsTriggeredOn(g => g.removeHold)
									.When(conditions.IsNotApproved));
							});
							transitions.AddGroupFrom<ChangeOrderStatus.pendingApproval>(ts =>
							{
								ts.Add(t => t
									.To<ChangeOrderStatus.open>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<ChangeOrderStatus.rejected>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));
								ts.Add(t => t
									.To<ChangeOrderStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
							});
							transitions.AddGroupFrom<ChangeOrderStatus.rejected>(ts =>
							{
								ts.Add(t => t
									.To<ChangeOrderStatus.onHold>()
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
								fa.Add<PMChangeOrder.approved>(f => f.SetFromValue(false));
								fa.Add<PMChangeOrder.rejected>(f => f.SetFromValue(false));
							}));
					}));
		}

		public PXAction<PMChangeOrder> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<PMChangeOrder> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
		
	}
}