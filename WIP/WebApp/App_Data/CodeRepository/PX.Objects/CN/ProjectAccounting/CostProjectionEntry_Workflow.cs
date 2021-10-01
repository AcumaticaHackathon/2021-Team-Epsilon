using System;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PM;

namespace PX.Objects.CN.ProjectAccounting
{
	using static BoundedTo<CostProjectionEntry, PMCostProjection>;

	public partial class CostProjectionEntry_Workflow : PXGraphExtension<CostProjectionEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<CostProjectionEntry, PMCostProjection>());
		}

		protected virtual void Configure(WorkflowContext<CostProjectionEntry, PMCostProjection> context)
		{
			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<PMCostProjection.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<CostProjectionStatus.onHold>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.refreshBudget, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.createRevision, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.removeHold, c => c.IsDuplicatedInToolbar());

									});
							});
							fss.Add<CostProjectionStatus.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.createRevision, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.hold);
										actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<CostProjectionStatus.released>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.createRevision, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<CostProjectionStatus.onHold>(ts =>
							{
								ts.Add(t => t
									.To<CostProjectionStatus.open>()
									.IsTriggeredOn(g => g.removeHold));
							});
							transitions.AddGroupFrom<CostProjectionStatus.open>(ts =>
							{
								ts.Add(t => t
									.To<CostProjectionStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
								ts.Add(t => t
									.To<CostProjectionStatus.released>()
									.IsTriggeredOn(g => g.release));
							});
							transitions.AddGroupFrom<CostProjectionStatus.released>(ts =>
							{
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.refreshBudget, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.createRevision, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.hold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<PMCostProjection.hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.removeHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<PMCostProjection.hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
					}));
		}

	}

	public class CostProjectionEntry_Workflow_CbApi_Adapter : PXGraphExtension<CostProjectionEntry>
	{
		public static bool IsActive() => true;

		public override void Initialize()
		{
			base.Initialize();
			if (!Base.IsContractBasedAPI && !Base.IsImport)
				return;

			Base.RowUpdated.AddHandler<PMCostProjection>(RowUpdated);

			void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
			{
				if (e.Row is PMCostProjection row
					&& e.OldRow is PMCostProjection oldRow
					&& row.Hold is bool newHold
					&& oldRow.Hold is bool oldHold
					&& newHold != oldHold)
				{
					// change it only by transition
					row.Hold = oldHold;

					Base.RowUpdated.RemoveHandler<PMCostProjection>(RowUpdated);

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