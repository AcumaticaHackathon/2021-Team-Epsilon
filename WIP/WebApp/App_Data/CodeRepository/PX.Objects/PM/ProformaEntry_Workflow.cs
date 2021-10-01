using System;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.PM
{
	using static BoundedTo<ProformaEntry, PMProforma>;

	public partial class ProformaEntry_Workflow : PXGraphExtension<ProformaEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ProformaEntry, PMProforma>());
		}

		protected virtual void Configure(WorkflowContext<ProformaEntry, PMProforma> context)
		{
			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<PMProforma.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ProformaStatus.onHold>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.removeHold, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.send);
									});
							});
							fss.Add<ProformaStatus.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.hold);
										actions.Add(g => g.send);
									});
							});
							fss.Add<ProformaStatus.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.send);
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<ProformaStatus.onHold>(ts =>
							{
								ts.Add(t => t
									.To<ProformaStatus.open>()
									.IsTriggeredOn(g => g.removeHold));
							});
							transitions.AddGroupFrom<ProformaStatus.open>(ts =>
							{
								ts.Add(t => t
									.To<ProformaStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
								ts.Add(t => t
									.To<ProformaStatus.closed>()
									.IsTriggeredOn(g => g.release));
							});
							transitions.AddGroupFrom<ProformaStatus.closed>(ts =>
							{
							});
						}))
					.WithActions(actions =>
					{

						actions.Add(g => g.removeHold, c => c
							.InFolder(FolderType.ActionsFolder));
						//.WithFieldAssignments(fa => fa.Add<PMProforma.hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.hold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<PMProforma.hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.send, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.proformaReport, c => c
							.InFolder(FolderType.ReportsFolder));
					}));
		}
	}

	public class ProformaEntry_Workflow_CbApi_Adapter : PXGraphExtension<ProformaEntry>
	{
		public static bool IsActive() => true;

		public override void Initialize()
		{
			base.Initialize();
			if (!Base.IsContractBasedAPI && !Base.IsImport)
				return;

			Base.RowUpdated.AddHandler<PMProforma>(RowUpdated);

			void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
			{
				if (e.Row is PMProforma row
					&& e.OldRow is PMProforma oldRow
					&& row.Hold is bool newHold
					&& oldRow.Hold is bool oldHold
					&& newHold != oldHold)
				{
					// change it only by transition
					row.Hold = oldHold;

					Base.RowUpdated.RemoveHandler<PMProforma>(RowUpdated);

					Base.OnAfterPersist += InvokeTransition;
					void InvokeTransition(PXGraph obj)
					{
						obj.OnAfterPersist -= InvokeTransition;
						(newHold ? Base.hold : Base.removeHold).PressImpl(internalCall: true);
					}
				}
			}
		}
	}
}