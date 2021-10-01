using System;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.EP
{
	public partial class ExpenseClaimEntry_Workflow : PXGraphExtension<ExpenseClaimEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<ExpenseClaimEntry, EPExpenseClaim>();
			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<EPExpenseClaim.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<EPExpenseClaimStatus.holdStatus>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.submit, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.expenseClaimPrint, c => c.IsDuplicatedInToolbar());
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnSubmit);
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<EPExpenseClaimStatus.approvedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.edit);
										actions.Add(g => g.expenseClaimPrint, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<EPExpenseClaimStatus.releasedStatus>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.expenseClaimPrint, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<EPExpenseClaimStatus.holdStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPExpenseClaimStatus.approvedStatus>()
									.IsTriggeredOn(g => g.submit));
								ts.Add(t => t
									.To<EPExpenseClaimStatus.approvedStatus>()
									.IsTriggeredOn(g => g.OnSubmit));
							});
							transitions.AddGroupFrom<EPExpenseClaimStatus.approvedStatus>(ts =>
							{
								ts.Add(t => t
									.To<EPExpenseClaimStatus.releasedStatus>()
									.IsTriggeredOn(g => g.release));
								ts.Add(t => t
									.To<EPExpenseClaimStatus.holdStatus>()
									.IsTriggeredOn(g => g.edit));
							});
							transitions.AddGroupFrom<EPExpenseClaimStatus.releasedStatus>(ts =>
							{
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.submit, c => c
							.InFolder(FolderType.ActionsFolder));
							//.WithFieldAssignments(fa => fa.Add<EPExpenseClaim.hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.edit, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fa => fa.Add<EPExpenseClaim.hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.expenseClaimPrint, c => c
							.InFolder(FolderType.ActionsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<EPExpenseClaim>()
							.OfEntityEvent<EPExpenseClaim.Events>(e => e.Submit)
							.Is(g => g.OnSubmit)
							.UsesTargetAsPrimaryEntity()
							.WithFieldAssignments(fa => fa.Add<EPExpenseClaim.hold>(f => f.SetFromValue(false))));
						handlers.Add(handler => handler
							.WithTargetOf<EPExpenseClaim>()
							.OfEntityEvent<EPExpenseClaim.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					}));
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<EPExpenseClaim>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<EPExpenseClaim>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<EPExpenseClaim.rejected>(e.Row, e.OldRow))
					{
						EPExpenseClaim.Events.Select(ev => ev.UpdateStatus).FireOn(g, (EPExpenseClaim)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}
