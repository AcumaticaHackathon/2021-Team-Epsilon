using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;


namespace PX.Objects.FA
{
	using State = FARegister.status;
	using static FARegister;
	using static BoundedTo<TransactionEntry, FARegister>;

	public partial class TransactionEntry_Workflow : PXGraphExtension<TransactionEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<TransactionEntry, FARegister>());

		protected virtual void Configure(WorkflowContext<TransactionEntry, FARegister> context)
		{
			#region Conditions

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>.And<released.IsEqual<False>>>(),
				IsNotOnHold
					= Bql<hold.IsEqual<False>.And<released.IsEqual<False>>>(),
				IsReleased 
					=  Bql<posted.IsEqual<False>.And<released.IsEqual<True>>>(),
				IsPosted
					= Bql<posted.IsEqual<True>.And<released.IsEqual<True>>>(),
			}.AutoNameConditions();

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
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
										
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnReleaseDocument);
									});
							});
						}
						)
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)); // New Hold
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsNotOnHold)); // New Hold
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsNotOnHold));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							
							ts.Add(t => t.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
							ts.Add(t => t.To<State.unposted>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsReleased));
							ts.Add(t => t.To<State.posted>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsPosted));
						});
						transitions.AddGroupFrom<State.unposted>(ts =>{ });
						transitions.AddGroupFrom<State.posted>(ts =>{ });
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
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder)
							.PlaceAfter(nameof(TransactionEntry.Last))
						);
					})
					.WithHandlers(handlers =>
						{
							handlers.Add(handler => handler
								.WithTargetOf<FARegister>()
								.OfEntityEvent<FARegister.Events>(e => e.UpdateStatus)
								.Is(g => g.OnUpdateStatus)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<FARegister>()
								.OfEntityEvent<FARegister.Events>(e => e.ReleaseDocument)
								.Is(g => g.OnReleaseDocument)
								.UsesTargetAsPrimaryEntity());
						}
					));
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<FARegister>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<FARegister>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<FARegister.hold, FARegister.released, FARegister.posted>(e.Row, e.OldRow))
					{
						FARegister.Events.Select(ev => ev.UpdateStatus)
							.FireOn(g, (FARegister)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}