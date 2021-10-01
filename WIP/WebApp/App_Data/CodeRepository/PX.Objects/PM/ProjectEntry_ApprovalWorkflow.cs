using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.PM
{
	using static BoundedTo<ProjectEntry, PMProject>;

	public partial class ProjectEntry_ApprovalWorkflow : PXGraphExtension<ProjectEntry_Workflow, ProjectEntry>
	{
		private class ProjectSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<ProjectSetupApproval>(nameof(ProjectSetupApproval), typeof(PMSetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord setup = PXDatabase.SelectSingle<PMSetup>(new PXDataField<PMSetup.assignmentMapID>()))
				{
					if (setup != null)
						RequestApproval = setup.GetInt32(0).HasValue;
				}
			}
		}

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && ProjectSetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(PMSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ProjectEntry, PMProject>());
		}

		protected virtual void Configure(WorkflowContext<ProjectEntry, PMProject> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsApproved
					= Bql<PMProject.approved.IsEqual<True>>(),
				IsNotApproved
					= Bql<PMProject.approved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<PMProject.status.IsNotEqual<ProjectStatus.pendingApproval>>()
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<ProjectEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.activate)
					.PlaceAfter(g => g.activate)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa =>
					{
						fa.Add<PMProject.approved>(e => e.SetFromValue(true));
						fa.Add<PMProject.isActive>(e => e.SetFromValue(true));
					}));

			var reject = context.ActionDefinitions
				.CreateExisting<ProjectEntry_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<PMProject.hold>(e => e.SetFromValue(true))));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ProjectStatus.pendingApproval>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.hold);
										actions.Add(g => g.createChangeOrder);
										actions.Add(g => g.lockBudget);
										actions.Add(g => g.unlockBudget);
										actions.Add(g => g.lockCommitments);
										actions.Add(g => g.unlockCommitments);
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.validateBalance);
										actions.Add(g => g.createTemplate);
										actions.Add(g => g.runAllocation);
										actions.Add(g => g.autoBudget);
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom<ProjectStatus.planned>(ts =>
							{
								ts.Update(t => t
									.To<ProjectStatus.active>()
									.IsTriggeredOn(g => g.activate), t => t
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<ProjectStatus.pendingApproval>()
									.IsTriggeredOn(g => g.activate)
									.When(conditions.IsNotApproved));
							});
							transitions.AddGroupFrom<ProjectStatus.pendingApproval>(ts =>
							{
								ts.Add(t => t
									.To<ProjectStatus.active>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));

								ts.Add(t => t
									.To<ProjectStatus.planned>()
									.IsTriggeredOn(reject));

								ts.Add(t => t
									.To<ProjectStatus.planned>()
									.IsTriggeredOn(g => g.hold));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(approve);
						actions.Add(reject);
						actions.Update(
							g => g.hold,
							a => a.WithFieldAssignments(fa => fa.Add<PMProject.approved>(f => f.SetFromValue(false))));
					}));
		}

		public PXAction<PMProject> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<PMProject> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}