using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.EP
{
	using static BoundedTo<ExpenseClaimDetailEntry, EPExpenseClaimDetails>;

	public partial class ExpenseClaimDetailEntry_ApprovalWorkflow : PXGraphExtension<ExpenseClaimDetailEntry_Workflow, ExpenseClaimDetailEntry>
	{
		private class ExpenseClaimDetailetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<ExpenseClaimDetailetupApproval>(nameof(ExpenseClaimDetailetupApproval), typeof(EPSetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord epSetup = PXDatabase.SelectSingle<EPSetup>(new PXDataField<EPSetup.claimDetailsAssignmentMapID>()))
				{
					if (epSetup != null)
						RequestApproval = epSetup.GetInt32(0).HasValue;
				}
			}
		}

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && ExpenseClaimDetailetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(EPSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ExpenseClaimDetailEntry, EPExpenseClaimDetails>());
		}

		protected virtual void Configure(WorkflowContext<ExpenseClaimDetailEntry, EPExpenseClaimDetails> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<EPExpenseClaimDetails.rejected.IsEqual<True>>(),
				IsNotApproved
					= Bql<EPExpenseClaimDetails.approved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<EPExpenseClaimDetails.status.IsNotIn<EPExpenseClaimDetailsStatus.openStatus, EPExpenseClaimDetailsStatus.rejectedStatus>>(),
				IsRejectDisabled
					= Bql<EPExpenseClaimDetails.bankTranDate.IsNotNull>(),
				IsApproveDisabled
					= Bql<EPExpenseClaimDetails.holdClaim.IsEqual<False>.And<EPExpenseClaimDetails.bankTranDate.IsNotNull>>()
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<ExpenseClaimDetailEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.Submit)
					.PlaceAfter(g => g.Submit)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.IsDisabledWhen(conditions.IsApproveDisabled)
					.WithFieldAssignments(fa =>
					{
						fa.Add<EPExpenseClaimDetails.approved>(e => e.SetFromValue(true));
					}));

			var reject = context.ActionDefinitions
				.CreateExisting<ExpenseClaimDetailEntry_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.IsDisabledWhen(conditions.IsRejectDisabled)
					.WithFieldAssignments(fa =>
					{
						fa.Add<EPExpenseClaimDetails.rejected>(e => e.SetFromValue(true));
					}));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<EPExpenseClaimDetailsStatus.openStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.hold);
									});
							});
							fss.Add<EPExpenseClaimDetailsStatus.rejectedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.hold, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom<EPExpenseClaimDetailsStatus.holdStatus>(ts =>
							{
								ts.Update(t => t
									.To<EPExpenseClaimDetailsStatus.approvedStatus>()
									.IsTriggeredOn(g => g.Submit), t => t
									.When(context.Conditions.Get("IsApproved")));
								ts.Add(t => t
									.To<EPExpenseClaimDetailsStatus.openStatus>()
									.IsTriggeredOn(g => g.Submit)
									.When(conditions.IsNotApproved));
							});
							transitions.AddGroupFrom<EPExpenseClaimDetailsStatus.openStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPExpenseClaimDetailsStatus.approvedStatus>()
									.IsTriggeredOn(approve)
									.When(context.Conditions.Get("IsApproved")));

								ts.Add(t => t
									.To<EPExpenseClaimDetailsStatus.rejectedStatus>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));

								ts.Add(t => t
									.To<EPExpenseClaimDetailsStatus.holdStatus>()
									.IsTriggeredOn(g => g.hold));
							});
							transitions.AddGroupFrom<EPExpenseClaimDetailsStatus.rejectedStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPExpenseClaimDetailsStatus.holdStatus>()
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
								fa.Add<EPExpenseClaimDetails.approved>(f => f.SetFromValue(false));
								fa.Add<EPExpenseClaimDetails.rejected>(f => f.SetFromValue(false));
							}));
					}));
		}

		public PXAction<EPExpenseClaimDetails> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<EPExpenseClaimDetails> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}
