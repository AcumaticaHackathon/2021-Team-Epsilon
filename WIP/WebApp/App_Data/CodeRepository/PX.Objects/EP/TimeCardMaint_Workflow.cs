using System;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.EP
{
	public partial class TimeCardMaint_Workflow : PXGraphExtension<TimeCardMaint>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<TimeCardMaint, EPTimeCard>();
			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<EPTimeCard.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<EPTimeCardStatusAttribute.holdStatus>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.submit, c => c.IsDuplicatedInToolbar());
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<EPTimeCardStatusAttribute.approvedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.edit);
									});
							});
							fss.Add<EPTimeCardStatusAttribute.releasedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.correct);
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<EPTimeCardStatusAttribute.holdStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.approvedStatus>()
									.IsTriggeredOn(g => g.submit));
							});
							transitions.AddGroupFrom<EPTimeCardStatusAttribute.approvedStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.releasedStatus>()
									.IsTriggeredOn(g => g.release));
								ts.Add(t => t
									.To<EPTimeCardStatusAttribute.holdStatus>()
									.IsTriggeredOn(g => g.edit));
							});
							transitions.AddGroupFrom<EPTimeCardStatusAttribute.releasedStatus>(ts =>
							{
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.correct, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.submit, c => c
							.InFolder(FolderType.ActionsFolder));
							//.WithFieldAssignments(fa => fa.Add<EPTimeCard.isHold>(f => f.SetFromValue(false))));
						actions.Add(g => g.edit, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<EPTimeCard.isHold>(f => f.SetFromValue(true))));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<EPTimeCard>()
							.OfEntityEvent<EPTimeCard.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					}));
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<EPTimeCard>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<EPTimeCard>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<EPTimeCard.isRejected>(e.Row, e.OldRow))
					{
						EPTimeCard.Events.Select(ev => ev.UpdateStatus).FireOn(g, (EPTimeCard)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}
