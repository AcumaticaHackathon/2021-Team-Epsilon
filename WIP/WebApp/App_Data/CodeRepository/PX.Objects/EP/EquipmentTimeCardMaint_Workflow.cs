using System;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.EP
{
	public partial class EquipmentTimeCardMaint_Workflow : PXGraphExtension<EquipmentTimeCardMaint>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<EquipmentTimeCardMaint, EPEquipmentTimeCard>();
			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<EPEquipmentTimeCard.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<EPEquipmentTimeCardStatusAttribute.onHold>(flowState =>
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
							fss.Add<EPEquipmentTimeCardStatusAttribute.approved>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.edit);
									});
							});
							fss.Add<EPEquipmentTimeCardStatusAttribute.released>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<EPEquipmentTimeCardStatusAttribute.onHold>(ts =>
							{
								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.approved>()
									.IsTriggeredOn(g => g.submit));
							});
							transitions.AddGroupFrom<EPEquipmentTimeCardStatusAttribute.approved>(ts =>
							{
								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.released>()
									.IsTriggeredOn(g => g.release));
								ts.Add(t => t
									.To<EPEquipmentTimeCardStatusAttribute.onHold>()
									.IsTriggeredOn(g => g.edit));
							});
							transitions.AddGroupFrom<EPEquipmentTimeCardStatusAttribute.released>(ts =>
							{
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.submit, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<EPEquipmentTimeCard.isHold>(f => f.SetFromValue(false))));
						actions.Add(g => g.edit, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<EPEquipmentTimeCard.isHold>(f => f.SetFromValue(true))));
						actions.Add(g => g.correct, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenAlways());
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<EPEquipmentTimeCard>()
							.OfEntityEvent<EPEquipmentTimeCard.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					}));
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<EPEquipmentTimeCard>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<EPEquipmentTimeCard>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<EPEquipmentTimeCard.isRejected>(e.Row, e.OldRow))
					{
						EPEquipmentTimeCard.Events.Select(ev => ev.UpdateStatus).FireOn(g, (EPEquipmentTimeCard)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}
