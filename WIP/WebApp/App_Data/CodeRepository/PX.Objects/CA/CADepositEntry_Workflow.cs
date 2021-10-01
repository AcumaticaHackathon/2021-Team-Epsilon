using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	using State = CADepositStatus;
	using static CADeposit;
	using static BoundedTo<CADepositEntry, CADeposit>;

	public partial class CADepositEntry_Workflow : PXGraphExtension<CADepositEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<CADepositEntry, CADeposit>());

		protected virtual void Configure(WorkflowContext<CADepositEntry, CADeposit> context)
		{
			#region Conditions

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>>(),

				IsNotOnHold
					= Bql<hold.IsEqual<False>>(),
				
				IsVoided 
					= Bql<tranType.IsEqual<CATranType.cAVoidDeposit>>(),

			}.AutoNameConditions();

			#endregion
			#region Event Handlers
			WorkflowEventHandlerDefinition.IHandlerConfiguredBase OnReleaseDocument(WorkflowEventHandlerDefinition.INeedEventTarget handler)
			{
				return handler
					.WithTargetOf<CADeposit>()
					.OfEntityEvent<CADeposit.Events>(e => e.ReleaseDocument)
					.Is(g => g.OnReleaseDocument)
					.UsesTargetAsPrimaryEntity();
			}
			WorkflowEventHandlerDefinition.IHandlerConfiguredBase OnVoidDocument(WorkflowEventHandlerDefinition.INeedEventTarget handler)
			{
				return handler
					.WithTargetOf<CADeposit>()
					.OfEntityEvent<CADeposit.Events>(e => e.VoidDocument)
					.Is(g => g.OnVoidDocument)
					.UsesTargetAsPrimaryEntity();
			}
			WorkflowEventHandlerDefinition.IHandlerConfiguredBase OnUpdateStatus(WorkflowEventHandlerDefinition.INeedEventTarget handler)
			{
				return handler
					.WithTargetOf<CADeposit>()
					.OfEntityEvent<CADeposit.Events>(e => e.UpdateStatus)
					.Is(g => g.OnUpdateStatus)
					.UsesTargetAsPrimaryEntity();
			}
			
			#endregion

			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(flow =>
						flow
						.WithFlowStates(fss =>
						{
							fss.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
							fss.Add<State.hold>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.addPayment);
										actions.Add(g => g.printDepositSlip);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.Release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.addPayment);
										actions.Add(g => g.printDepositSlip);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.released>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.VoidDocument, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printDepositSlip);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnVoidDocument);
									});
							});

						})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)); // New Hold
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsNotOnHold)); // New Balanced
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsNotOnHold));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t.To<State.released>()
								.IsTriggeredOn(g => g.OnReleaseDocument));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
						});
						transitions.AddGroupFrom<State.released>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
						});
					}
					))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.putOnHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false)))); 
						actions.Add(g => g.Release, c => c
							.InFolder(FolderType.ActionsFolder)
							.PlaceAfter(nameof(CADepositEntry.Last)));
						actions.Add(g => g.addPayment);
						actions.Add(g => g.VoidDocument, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen((conditions.IsVoided)));
						actions.Add(g => g.printDepositSlip, c => c
							.InFolder(FolderType.ReportsFolder));

					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<CADeposit>()
							.OfEntityEvent<CADeposit.Events>(e => e.ReleaseDocument)
							.Is(g => g.OnReleaseDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<CADeposit>()
							.OfEntityEvent<CADeposit.Events>(e => e.VoidDocument)
							.Is(g => g.OnVoidDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<CADeposit>()
							.OfEntityEvent<CADeposit.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
			);
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<CADeposit>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<CADeposit>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<CADeposit.hold>(e.Row, e.OldRow))
					{
						CADeposit.Events.Select(ev => ev.UpdateStatus).FireOn(g, (CADeposit)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}