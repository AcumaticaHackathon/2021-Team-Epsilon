using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.EP
{
	using static BoundedTo<ExpenseClaimDetailEntry, EPExpenseClaimDetails>;

	public partial class ExpenseClaimDetailEntry_Workflow : PXGraphExtension<ExpenseClaimDetailEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ExpenseClaimDetailEntry, EPExpenseClaimDetails>());
		}

		protected virtual void Configure(WorkflowContext<ExpenseClaimDetailEntry, EPExpenseClaimDetails> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<EPExpenseClaimDetails.hold.IsEqual<True>>(),
				IsApproved
					= Bql<EPExpenseClaimDetails.approved.IsEqual<True>>(),
				IsHoldDisabled
					= Bql<EPExpenseClaimDetails.holdClaim.IsEqual<False>
						.Or<EPExpenseClaimDetails.rejected.IsEqual<False>.And<EPExpenseClaimDetails.bankTranDate.IsNotNull>>>()
			}.AutoNameConditions();

			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<EPExpenseClaimDetails.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
							fss.Add<EPExpenseClaimDetailsStatus.holdStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.Submit, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<EPExpenseClaimDetailsStatus.approvedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.hold);
										actions.Add(g => g.Claim);
									});
							});
							fss.Add<EPExpenseClaimDetailsStatus.releasedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom(initialState, ts =>
							{
								ts.Add(t => t
									.To<EPExpenseClaimDetailsStatus.holdStatus>()
									.IsTriggeredOn(g => g.initializeState)
									.When(conditions.IsOnHold));
								ts.Add(t => t
									.To<EPExpenseClaimDetailsStatus.approvedStatus>()
									.IsTriggeredOn(g => g.initializeState)
									.When(conditions.IsApproved));
							});
							transitions.AddGroupFrom<EPExpenseClaimDetailsStatus.holdStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPExpenseClaimDetailsStatus.approvedStatus>()
									.IsTriggeredOn(g => g.Submit));
							});
							transitions.AddGroupFrom<EPExpenseClaimDetailsStatus.approvedStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.holdStatus>()
									.IsTriggeredOn(g => g.hold));
							});
							transitions.AddGroupFrom<EPExpenseClaimDetailsStatus.releasedStatus>(ts =>
							{
							});
						}))
					.WithActions(actions =>
					{
						
						actions.Add(g => g.Submit, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa =>
							{
								fa.Add<EPExpenseClaimDetails.hold>(e => e.SetFromValue(false));
							}));
						actions.Add(g => g.hold, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsHoldDisabled)
							.WithFieldAssignments(fa =>
							{
								fa.Add<EPExpenseClaimDetails.hold>(e => e.SetFromValue(true));
							}));
						actions.Add(g => g.Claim, c => c
							.InFolder(FolderType.ActionsFolder));
					}));
		}
	}
}
