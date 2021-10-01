using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.EP
{
	using static BoundedTo<EquipmentTimeCardMaint, EPEquipmentTimeCard>;

	public partial class EquipmentTimeCardMaint_ApprovalWorkflow : PXGraphExtension<EquipmentTimeCardMaint_Workflow, EquipmentTimeCardMaint>
	{
		private class EquipmentTimeCardSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<EquipmentTimeCardSetupApproval>(nameof(EquipmentTimeCardSetupApproval), typeof(EPSetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord epSetup = PXDatabase.SelectSingle<EPSetup>(new PXDataField<EPSetup.equipmentTimeCardAssignmentMapID>()))
				{
					if (epSetup != null)
						RequestApproval = epSetup.GetInt32(0).HasValue;
				}
			}
		}

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && EquipmentTimeCardSetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(EPSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<EquipmentTimeCardMaint, EPEquipmentTimeCard>());
		}

		protected virtual void Configure(WorkflowContext<EquipmentTimeCardMaint, EPEquipmentTimeCard> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<EPEquipmentTimeCard.isRejected.IsEqual<True>>(),
				IsApproved
					= Bql<EPEquipmentTimeCard.isApproved.IsEqual<True>>(),
				IsNotApproved
					= Bql<EPEquipmentTimeCard.isApproved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<EPEquipmentTimeCard.status.IsNotIn<EPEquipmentTimeCardStatusAttribute.pendingApproval, EPEquipmentTimeCardStatusAttribute.rejected>>(),
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<EquipmentTimeCardMaint_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.release)
					.PlaceAfter(g => g.release)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<EPEquipmentTimeCard.isApproved>(e => e.SetFromValue(true))));

			var reject = context.ActionDefinitions
				.CreateExisting<EquipmentTimeCardMaint_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<EPEquipmentTimeCard.isRejected>(e => e.SetFromValue(true))));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<EPEquipmentTimeCardStatusAttribute.pendingApproval>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.edit);
									});
							});
							fss.Add<EPEquipmentTimeCardStatusAttribute.rejected>(flowState =>
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
							transitions.UpdateGroupFrom<EPEquipmentTimeCardStatusAttribute.onHold>(ts =>
							{
								ts.Update(t => t
									.To<EPEquipmentTimeCardStatusAttribute.approved>()
									.IsTriggeredOn(g => g.submit), t => t
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.pendingApproval>()
									.IsTriggeredOn(g => g.submit)
									.When(conditions.IsNotApproved));
								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.rejected>()
									.IsTriggeredOn(g => g.OnUpdateStatus)
									.When(conditions.IsRejected));
							});
							transitions.AddGroupFrom<EPEquipmentTimeCardStatusAttribute.pendingApproval>(ts =>
							{
								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.approved>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));

								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.rejected>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));

								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.onHold>()
									.IsTriggeredOn(g => g.edit));
							});
							transitions.AddGroupFrom<EPEquipmentTimeCardStatusAttribute.rejected>(ts =>
							{
								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.onHold>()
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
								fa.Add<EPEquipmentTimeCard.isApproved>(f => f.SetFromValue(false));
								fa.Add<EPEquipmentTimeCard.isRejected>(f => f.SetFromValue(false));
							}));
					}));
		}

		public PXAction<EPEquipmentTimeCard> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<EPEquipmentTimeCard> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}
