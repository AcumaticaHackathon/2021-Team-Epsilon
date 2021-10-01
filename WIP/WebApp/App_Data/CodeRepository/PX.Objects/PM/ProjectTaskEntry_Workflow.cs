using System;
using PX.Data;
using PX.Data.WorkflowAPI;
using System.Collections;

namespace PX.Objects.PM
{
	public partial class ProjectTaskEntry_Workflow : PXGraphExtension<ProjectTaskEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<ProjectTaskEntry, PMTask>();

			var hold = context.ActionDefinitions
				.CreateExisting<ProjectTaskEntry_Workflow>(g => g.hold, a => a
					.InFolder(FolderType.ActionsFolder)
					.WithFieldAssignments(fa =>
					{
						fa.Add<PMTask.isActive>(e => e.SetFromValue(false));
						fa.Add<PMTask.isCompleted>(e => e.SetFromValue(false));
						fa.Add<PMTask.isCancelled>(e => e.SetFromValue(false));
					}));

			

	
			var cancelTask = context.ActionDefinitions
				.CreateExisting<ProjectTaskEntry_Workflow>(g => g.cancelTask, a => a
					.InFolder(FolderType.ActionsFolder)
					.PlaceAfter(g => g.complete)
					.WithFieldAssignments(fa =>
					{
						fa.Add<PMTask.isActive>(e => e.SetFromValue(false));
						fa.Add<PMTask.isCompleted>(e => e.SetFromValue(false));
						fa.Add<PMTask.isCancelled>(e => e.SetFromValue(true));
					}));

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<PMTask.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ProjectTaskStatus.planned>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.activate, c => c.IsDuplicatedInToolbar());
										actions.Add(cancelTask, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<ProjectTaskStatus.active>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.complete, c => c.IsDuplicatedInToolbar());
										actions.Add(cancelTask, c => c.IsDuplicatedInToolbar());
										actions.Add(hold);
									});
							});
							fss.Add<ProjectTaskStatus.completed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.activate, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<ProjectTaskStatus.canceled>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.activate, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<ProjectTaskStatus.planned>(ts =>
							{
								ts.Add(t => t
									.To<ProjectTaskStatus.active>()
									.IsTriggeredOn(g => g.activate));
								ts.Add(t => t
									.To<ProjectTaskStatus.canceled>()
									.IsTriggeredOn(cancelTask));
							});
							transitions.AddGroupFrom<ProjectTaskStatus.active>(ts =>
							{
								ts.Add(t => t
									.To<ProjectTaskStatus.completed>()
									.IsTriggeredOn(g => g.complete));
								ts.Add(t => t
									.To<ProjectTaskStatus.canceled>()
									.IsTriggeredOn(cancelTask));
								ts.Add(t => t
									.To<ProjectTaskStatus.planned>()
									.IsTriggeredOn(hold));
							});
							transitions.AddGroupFrom<ProjectTaskStatus.completed>(ts =>
							{
								ts.Add(t => t
									.To<ProjectTaskStatus.active>()
									.IsTriggeredOn(g => g.activate));
							});
							transitions.AddGroupFrom<ProjectTaskStatus.canceled>(ts =>
							{
								ts.Add(t => t
									.To<ProjectTaskStatus.active>()
									.IsTriggeredOn(g => g.activate));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.activate, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa =>
							{
								fa.Add<PMTask.isActive>(e => e.SetFromValue(true));
								fa.Add<PMTask.isCompleted>(e => e.SetFromValue(false));
								fa.Add<PMTask.isCancelled>(e => e.SetFromValue( false));
								
							}));
						actions.Add(g => g.complete, a => a
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa =>
							{
								fa.Add<PMTask.isActive>(e => e.SetFromValue(true));
								fa.Add<PMTask.isCompleted>(e => e.SetFromValue(true));
								fa.Add<PMTask.isCancelled>(e => e.SetFromValue(false));
							}));
						actions.Add(cancelTask);
						actions.Add(hold);
					}));
		}

		public PXAction<PMTask> hold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold")]
		protected virtual IEnumerable Hold(PXAdapter adapter) => adapter.Get();

		public PXAction<PMTask> cancelTask;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Cancel")]
		protected virtual IEnumerable CancelTask(PXAdapter adapter) => adapter.Get();
	}
}
