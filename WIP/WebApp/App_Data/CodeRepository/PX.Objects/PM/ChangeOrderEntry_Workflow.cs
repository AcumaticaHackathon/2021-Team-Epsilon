using System;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PM;

namespace PX.Objects.PM
{
	using static BoundedTo<ChangeOrderEntry, PMChangeOrder>;

	public partial class ChangeOrderEntry_Workflow : PXGraphExtension<ChangeOrderEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ChangeOrderEntry, PMChangeOrder>());
		}

		protected virtual void Configure(WorkflowContext<ChangeOrderEntry, PMChangeOrder> context)
		{
			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<PMCostProjection.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ChangeOrderStatus.onHold>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.removeHold, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.send);
									});
							});
							fss.Add<ChangeOrderStatus.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.hold);
										actions.Add(g => g.send);
									});
							});
							fss.Add<ChangeOrderStatus.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.send);
										actions.Add(g => g.reverse);
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<ChangeOrderStatus.onHold>(ts =>
							{
								ts.Add(t => t
									.To<ChangeOrderStatus.open>()
									.IsTriggeredOn(g => g.removeHold));
							});
							transitions.AddGroupFrom<ChangeOrderStatus.open>(ts =>
							{
								ts.Add(t => t
									.To<ChangeOrderStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
								ts.Add(t => t
									.To<ChangeOrderStatus.closed>()
									.IsTriggeredOn(g => g.release));
							});
							transitions.AddGroupFrom<ChangeOrderStatus.closed>(ts =>
							{
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.removeHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<PMChangeOrder.hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.hold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<PMChangeOrder.hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.send, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.reverse, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.coReport, c => c
							.InFolder(FolderType.ReportsFolder));
					}));
		}
	}

	public class ChangeOrderEntry_Workflow_CbApi_Adapter : PXGraphExtension<ChangeOrderEntry>
	{
		public static bool IsActive() => true;

		public override void Initialize()
		{
			base.Initialize();
			if (!Base.IsContractBasedAPI && !Base.IsImport)
				return;

			Base.RowUpdated.AddHandler<PMChangeOrder>(RowUpdated);

			void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
			{
				if (e.Row is PMChangeOrder row
					&& e.OldRow is PMChangeOrder oldRow
					&& row.Hold is bool newHold
					&& oldRow.Hold is bool oldHold
					&& newHold != oldHold)
				{
					// change it only by transition
					row.Hold = oldHold;

					Base.RowUpdated.RemoveHandler<PMChangeOrder>(RowUpdated);

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