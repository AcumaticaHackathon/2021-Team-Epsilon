using System;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PJ.DailyFieldReports.PJ.DAC;
using PX.Objects.PJ.DailyFieldReports.PJ.Descriptor.Attributes;
using PX.Objects.PJ.ProjectsIssue.PJ.DAC;

namespace PX.Objects.PJ.DailyFieldReports.PJ.Graphs
{
	using static BoundedTo<DailyFieldReportEntry, DailyFieldReport>;

	public partial class DailyFieldReportEntry_Workflow : PXGraphExtension<DailyFieldReportEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<DailyFieldReportEntry, DailyFieldReport>());
		}

		protected virtual void Configure(WorkflowContext<DailyFieldReportEntry, DailyFieldReport> context)
		{
			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<DailyFieldReport.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<DailyFieldReportStatus.hold>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.complete, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.Print, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<DailyFieldReportStatus.completed>(flowState =>
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
							transitions.AddGroupFrom<DailyFieldReportStatus.hold>(ts =>
							{
								ts.Add(t => t
									.To<DailyFieldReportStatus.completed>()
									.IsTriggeredOn(g => g.complete));
							});
							transitions.AddGroupFrom<DailyFieldReportStatus.completed>(ts =>
							{
								ts.Add(t => t
									.To<DailyFieldReportStatus.hold>()
									.IsTriggeredOn(g => g.hold));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.complete, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<DailyFieldReport.hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.hold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<DailyFieldReport.hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.Print, c => c
							.InFolder(FolderType.ActionsFolder));
					}));
		}
	}

	public class DailyFieldReportEntry_Workflow_CbApi_Adapter : PXGraphExtension<DailyFieldReportEntry>
	{
		public static bool IsActive() => true;

		public override void Initialize()
		{
			base.Initialize();
			if (!Base.IsContractBasedAPI && !Base.IsImport)
				return;

			Base.RowUpdated.AddHandler<DailyFieldReport>(RowUpdated);

			void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
			{
				if (e.Row is DailyFieldReport row
					&& e.OldRow is DailyFieldReport oldRow
					&& row.Hold is bool newHold
					&& oldRow.Hold is bool oldHold
					&& newHold != oldHold)
				{
					// change it only by transition
					row.Hold = oldHold;

					Base.RowUpdated.RemoveHandler<DailyFieldReport>(RowUpdated);

					Base.OnAfterPersist += InvokeTransition;
					void InvokeTransition(PXGraph obj)
					{
						obj.OnAfterPersist -= InvokeTransition;
						(newHold ? Base.hold : Base.complete).PressImpl(internalCall: true);
					}
				}
			}
		}
	}
}