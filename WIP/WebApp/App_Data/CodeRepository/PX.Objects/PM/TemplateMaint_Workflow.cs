using System;
using PX.Data;
using PX.Data.WorkflowAPI;
using System.Collections;

namespace PX.Objects.PM
{
	public partial class TemplateMaint_Workflow : PXGraphExtension<TemplateMaint>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<TemplateMaint, PMProject>();

			var activate = context.ActionDefinitions
				.CreateExisting<TemplateMaint_Workflow>(g => g.activate, a => a
					.InFolder(FolderType.ActionsFolder)
					.WithFieldAssignments(fa =>
					{ 
						fa.Add<PMProject.isActive>(e => e.SetFromValue(true));
						fa.Add<PMProject.hold>(e => e.SetFromValue(false));
					}));

			var hold = context.ActionDefinitions
				.CreateExisting<TemplateMaint_Workflow>(g => g.hold, a => a
					.InFolder(FolderType.ActionsFolder)
					.WithFieldAssignments(fa =>
					{
						fa.Add<PMProject.isActive>(e => e.SetFromValue(false));
						fa.Add<PMProject.hold>(e => e.SetFromValue(true));
					}));

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<PMProject.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ProjectStatus.onHold>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(activate, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.copyTemplate, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<ProjectStatus.active>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(hold, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.copyTemplate, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<ProjectStatus.onHold>(ts =>
							{
								ts.Add(t => t
									.To<ProjectStatus.active>()
									.IsTriggeredOn(activate));
							});
							transitions.AddGroupFrom<ProjectStatus.active>(ts =>
							{
								ts.Add(t => t
									.To<ProjectStatus.onHold>()
									.IsTriggeredOn(hold));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.copyTemplate, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(activate);
						actions.Add(hold);
					}));
		}

		public PXAction<PMProject> activate;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Activate")]
		protected virtual IEnumerable Activate(PXAdapter adapter) => adapter.Get();

		public PXAction<PMProject> hold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold")]
		protected virtual IEnumerable Hold(PXAdapter adapter) => adapter.Get();
	}
}
