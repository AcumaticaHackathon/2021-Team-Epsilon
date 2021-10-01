using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PJ.DailyFieldReports.PJ.DAC;
using PX.Objects.PJ.DailyFieldReports.PJ.Descriptor.Attributes;
using PX.Objects.PJ.ProjectManagement.PJ.DAC;
using PX.Objects.PJ.ProjectsIssue.PJ.DAC;

namespace PX.Objects.PJ.DailyFieldReports.PJ.Graphs
{
	using static BoundedTo<DailyFieldReportEntry, DailyFieldReport>;

	public partial class DailyFieldReportEntry_ApprovalWorkflow : PXGraphExtension<DailyFieldReportEntry_Workflow, DailyFieldReportEntry>
	{
		private class DailyFieldReportSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<DailyFieldReportSetupApproval>(nameof(DailyFieldReportSetupApproval), typeof(ProjectManagementSetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord setup = PXDatabase.SelectSingle<ProjectManagementSetup>(new PXDataField<ProjectManagementSetup.dailyFieldReportApprovalMapId>()))
				{
					if (setup != null)
						RequestApproval = setup.GetInt32(0).HasValue;
				}
			}
		}

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && DailyFieldReportSetupApproval.IsActive;

		[PXWorkflowDependsOnType(typeof(ProjectManagementSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<DailyFieldReportEntry, DailyFieldReport>());
		}

		protected virtual void Configure(WorkflowContext<DailyFieldReportEntry, DailyFieldReport> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsRejected
					= Bql<DailyFieldReport.rejected.IsEqual<True>>(),
				IsApproved
					= Bql<DailyFieldReport.approved.IsEqual<True>>(),
				IsNotApproved
					= Bql<DailyFieldReport.approved.IsEqual<False>>(),
				IsApprovalDisabled
					= ApprovalIsActive()
						? Bql<True.IsEqual<False>>()
						: Bql<DailyFieldReport.status.IsNotIn<DailyFieldReportStatus.pendingApproval, DailyFieldReportStatus.rejected>>()
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<DailyFieldReportEntry_ApprovalWorkflow>(g => g.approve, a => a
					.InFolder(FolderType.ActionsFolder, g => g.complete)
					.PlaceAfter(g => g.complete)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<DailyFieldReport.approved>(e => e.SetFromValue(true))));

			var reject = context.ActionDefinitions
				.CreateExisting<DailyFieldReportEntry_ApprovalWorkflow>(g => g.reject, a => a
					.InFolder(FolderType.ActionsFolder, approve)
					.PlaceAfter(approve)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<DailyFieldReport.rejected>(e => e.SetFromValue(true))));

			context.UpdateScreenConfigurationFor(screen =>
				screen
					.UpdateDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<DailyFieldReportStatus.pendingApproval>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(approve, c => c.IsDuplicatedInToolbar());
										actions.Add(reject, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.hold);
										actions.Add(g => g.Print, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<DailyFieldReportStatus.rejected>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.hold, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.Print, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom<DailyFieldReportStatus.hold>(ts =>
							{
								ts.Update(t => t
									.To<DailyFieldReportStatus.completed>()
									.IsTriggeredOn(g => g.complete), t => t
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<DailyFieldReportStatus.pendingApproval>()
									.IsTriggeredOn(g => g.complete)
									.When(conditions.IsNotApproved));
							});
							transitions.AddGroupFrom<DailyFieldReportStatus.pendingApproval>(ts =>
							{
								ts.Add(t => t
									.To<DailyFieldReportStatus.completed>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<DailyFieldReportStatus.rejected>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));
								ts.Add(t => t
									.To<DailyFieldReportStatus.hold>()
									.IsTriggeredOn(g => g.hold));
							});
							transitions.AddGroupFrom<DailyFieldReportStatus.rejected>(ts =>
							{
								ts.Add(t => t
									.To<DailyFieldReportStatus.hold>()
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
								fa.Add<DailyFieldReport.approved>(f => f.SetFromValue(false));
								fa.Add<DailyFieldReport.rejected>(f => f.SetFromValue(false));
							}));
					}));
		}

		public PXAction<DailyFieldReport> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve")]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<DailyFieldReport> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject")]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}