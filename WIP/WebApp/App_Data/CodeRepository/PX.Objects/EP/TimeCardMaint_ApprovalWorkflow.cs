using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.EP
{
	using static BoundedTo<TimeCardMaint, EPTimeCard>;

	public partial class TimeCardMaint_ApprovalWorkflow : PXGraphExtension<TimeCardMaint_Workflow, TimeCardMaint>
	{
		private class TimeCardSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<TimeCardSetupApproval>(nameof(TimeCardSetupApproval), typeof(EPSetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord epSetup = PXDatabase.SelectSingle<EPSetup>(new PXDataField<EPSetup.timeCardAssignmentMapID>()))
				{
					if (epSetup != null)
						RequestApproval = epSetup.GetInt32(0).HasValue;
				}
			}
		}

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && TimeCardSetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(EPSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<TimeCardMaint, EPTimeCard>());
		}

		protected virtual void Configure(WorkflowContext<TimeCardMaint, EPTimeCard> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<EPTimeCard.isRejected.IsEqual<True>>(),
				IsApproved
					= Bql<EPTimeCard.isApproved.IsEqual<True>>(),
				IsNotApproved
					= Bql<EPTimeCard.isApproved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<EPTimeCard.status.IsNotIn<EPTimeCardStatusAttribute.openStatus, EPTimeCardStatusAttribute.rejectedStatus>>(),
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<TimeCardMaint_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.release)
					.PlaceAfter(g => g.release)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<EPTimeCard.isApproved>(e => e.SetFromValue(true))));

			var reject = context.ActionDefinitions
				.CreateExisting<TimeCardMaint_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<EPTimeCard.isRejected>(e => e.SetFromValue(true))));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<EPTimeCardStatusAttribute.openStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.edit);
									});
							});
							fss.Add<EPTimeCardStatusAttribute.rejectedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.edit, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom<EPTimeCardStatusAttribute.holdStatus>(ts =>
							{
								ts.Update(t => t
									.To<EPTimeCardStatusAttribute.approvedStatus>()
									.IsTriggeredOn(g => g.submit), t => t
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.openStatus>()
									.IsTriggeredOn(g => g.submit)
									.When(conditions.IsNotApproved));
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.rejectedStatus>()
									.IsTriggeredOn(g => g.OnUpdateStatus)
									.When(conditions.IsRejected));
							});
							transitions.AddGroupFrom<EPTimeCardStatusAttribute.openStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.approvedStatus>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.rejectedStatus>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.holdStatus>()
									.IsTriggeredOn(g => g.edit));
							});
							transitions.AddGroupFrom<EPTimeCardStatusAttribute.rejectedStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.holdStatus>()
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
								fa.Add<EPTimeCard.isApproved>(f => f.SetFromValue(false));
								fa.Add<EPTimeCard.isRejected>(f => f.SetFromValue(false));
							}));
					}));
		}

		public PXAction<EPTimeCard> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<EPTimeCard> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}
