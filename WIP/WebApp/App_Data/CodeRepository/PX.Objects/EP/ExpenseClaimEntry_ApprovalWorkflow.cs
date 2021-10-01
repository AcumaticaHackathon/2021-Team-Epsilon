using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.EP
{
	using static BoundedTo<ExpenseClaimEntry, EPExpenseClaim>;

	public partial class ExpenseClaimEntry_ApprovalWorkflow : PXGraphExtension<ExpenseClaimEntry_Workflow, ExpenseClaimEntry>
	{
		private class ExpenseClaimSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<ExpenseClaimSetupApproval>(nameof(ExpenseClaimSetupApproval), typeof(EPSetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord epSetup = PXDatabase.SelectSingle<EPSetup>(new PXDataField<EPSetup.claimAssignmentMapID>()))
				{
					if (epSetup != null)
						RequestApproval = epSetup.GetInt32(0).HasValue;
				}
			}
		}

		protected static bool ApprovalIsActive() => !PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() || ExpenseClaimSetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(EPSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ExpenseClaimEntry, EPExpenseClaim>());
		}

		protected virtual void Configure(WorkflowContext<ExpenseClaimEntry, EPExpenseClaim> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<EPExpenseClaim.rejected.IsEqual<True>>(),
				IsApproved
					= Bql<EPExpenseClaim.approved.IsEqual<True>>(),
				IsNotApproved
					= Bql<EPExpenseClaim.approved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<EPExpenseClaim.status.IsNotIn<EPExpenseClaimStatus.openStatus, EPExpenseClaimStatus.rejectedStatus>>(),
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<ExpenseClaimEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.release)
					.PlaceAfter(g => g.release)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<EPExpenseClaim.approved>(e => e.SetFromValue(true))));

			var reject = context.ActionDefinitions
				.CreateExisting<ExpenseClaimEntry_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<EPExpenseClaim.rejected>(e => e.SetFromValue(true))));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<EPExpenseClaimStatus.openStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.edit);
										actions.Add(g => g.expenseClaimPrint, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<EPExpenseClaimStatus.rejectedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.edit, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.expenseClaimPrint, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom<EPExpenseClaimStatus.holdStatus>(ts =>
							{
								ts.Update(t => t
									.To<EPExpenseClaimStatus.approvedStatus>()
									.IsTriggeredOn(g => g.submit), t => t
									.When(conditions.IsApproved));
								ts.Update(t => t
									.To<EPExpenseClaimStatus.approvedStatus>()
									.IsTriggeredOn(g => g.OnSubmit), t => t
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<EPExpenseClaimStatus.openStatus>()
									.IsTriggeredOn(g => g.submit)
									.When(conditions.IsNotApproved));
								ts.Add(t => t
									.To<EPExpenseClaimStatus.openStatus>()
									.IsTriggeredOn(g => g.OnSubmit)
									.When(conditions.IsNotApproved));
								ts.Add(t => t
									.To<EPExpenseClaimStatus.rejectedStatus>()
									.IsTriggeredOn(g => g.OnUpdateStatus)
									.When(conditions.IsRejected));
							});
							transitions.AddGroupFrom<EPExpenseClaimStatus.openStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPExpenseClaimStatus.approvedStatus>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<EPExpenseClaimStatus.rejectedStatus>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));
								ts.Add(t => t
									.To<EPExpenseClaimStatus.holdStatus>()
									.IsTriggeredOn(g => g.edit));
							});
							transitions.AddGroupFrom<EPExpenseClaimStatus.rejectedStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPExpenseClaimStatus.holdStatus>()
									.IsTriggeredOn(g => g.edit));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(approve);
						actions.Add(reject);
						actions.Update(
							g => g.edit,
							a => a.WithFieldAssignments(fa =>
							{
								fa.Add<EPExpenseClaim.approved>(f => f.SetFromValue(false));
								fa.Add<EPExpenseClaim.rejected>(f => f.SetFromValue(false));
							}));
					}));
		}

		public PXAction<EPExpenseClaim> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<EPExpenseClaim> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}
