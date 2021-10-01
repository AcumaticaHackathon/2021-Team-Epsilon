using System;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;

namespace PX.Objects.RQ
{
	using State = RQRequestStatus;
	using static RQRequest;
	using static BoundedTo<RQRequestEntry, RQRequest>;

	public class RQRequestEntry_Workflow : PXGraphExtension<RQRequestEntry>
	{
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<RQRequestEntry, RQRequest>());

		protected virtual void Configure(WorkflowContext<RQRequestEntry, RQRequest> context)
		{
			#region Conditions
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsCancelled
					= Bql<cancelled.IsEqual<True>>(),

				IsOnHold
					= Bql<hold.IsEqual<True>>(),

				HasOpenOrderQty
					= Bql<openOrderQty.IsGreater<CS.decimal0>>(),

				HasZeroOpenOrderQty
					= Bql<openOrderQty.IsEqual<CS.decimal0>>(),
			}.AutoNameConditions();
			#endregion

			#region Macroses
			void DisableWholeScreen(FieldState.IContainerFillerFields fieldStates)
			{
				fieldStates.AddTable<RQRequest>(fs => fs.IsDisabled());
				fieldStates.AddTable<RQRequestLine>(fs => fs.IsDisabled());
				fieldStates.AddTable<PO.POShipAddress>(fs => fs.IsDisabled());
				fieldStates.AddTable<PO.POShipContact>(fs => fs.IsDisabled());
				fieldStates.AddTable<CM.CurrencyInfo>(fs => fs.IsDisabled());
			}
			#endregion

			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
				screen
				.StateIdentifierIs<status>()
				.AddDefaultFlow(flow =>
					flow
					.WithFlowStates(flowStates =>
					{
						flowStates.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
						flowStates.Add<State.hold>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.requestForm);
								});
						});
						flowStates.Add<State.open>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.cancelRequest);
									actions.Add(g => g.requestForm);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnOpenOrderQtyExhausted);
								})
								.WithFieldStates(fieldStates =>
								{
									DisableWholeScreen(fieldStates);

									// but enable these
									fieldStates.AddField<RQRequestLine.requestedDate>();
									fieldStates.AddField<RQRequestLine.promisedDate>();
									fieldStates.AddField<RQRequestLine.cancelled>();
									fieldStates.AddField<RQRequestLine.inventoryID>();
									fieldStates.AddField<RQRequestLine.subItemID>();
									fieldStates.AddField<RQRequestLine.description>();
									fieldStates.AddField<RQRequestLine.orderQty>();
									fieldStates.AddField<RQRequestLine.uOM>();
									fieldStates.AddField<RQRequestLine.estUnitCost>();
								});
						});
						flowStates.Add<State.closed>(flowState =>
						{
							return flowState // OpenOrderQty == 0
								.WithActions(actions =>
								{
									actions.Add(g => g.requestForm);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnOpenOrderQtyIncreased);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.canceled>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.requestForm);
								})
								.WithFieldStates(DisableWholeScreen);
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.canceled>().IsTriggeredOn(g => g.initializeState).When(conditions.IsCancelled));
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold));
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.initializeState).When(conditions.HasZeroOpenOrderQty));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState));
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.releaseFromHold).When(conditions.HasZeroOpenOrderQty));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.releaseFromHold));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
							ts.Add(t => t.To<State.canceled>().IsTriggeredOn(g => g.cancelRequest).When(conditions.IsCancelled));
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.OnOpenOrderQtyExhausted));
						});
						transitions.AddGroupFrom<State.closed>(ts =>
						{
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.OnOpenOrderQtyIncreased));
						});
						transitions.AddGroupFrom<State.canceled>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
						});
					}))
				.WithActions(actions =>
				{
					actions.Add(g => g.initializeState, a => a.IsHiddenAlways());

					actions.Add(g => g.putOnHold, a => a
						.InFolder(FolderType.ActionsFolder)
						.WithFieldAssignments(fass => fass.Add<hold>(v => v.SetFromValue(true))));
					actions.Add(g => g.releaseFromHold, a => a
						.InFolder(FolderType.ActionsFolder)
						.WithFieldAssignments(fass => fass.Add<hold>(v => v.SetFromValue(false))));
					actions.Add(g => g.cancelRequest, a => a
						.InFolder(FolderType.ActionsFolder)
						.WithFieldAssignments(fass => fass.Add<cancelled>(v => v.SetFromValue(true))));

					actions.Add(g => g.validateAddresses, a => a
						.InFolder(FolderType.ActionsFolder));

					actions.Add(g => g.requestForm, a => a
						.InFolder(FolderType.ReportsFolder));
				})
				.WithHandlers(handlers =>
				{
					handlers.Add(handler => handler
						.WithTargetOf<RQRequest>()
						.OfEntityEvent<Events>(e => e.OpenOrderQtyChanged)
						.Is(g => g.OnOpenOrderQtyExhausted)
						.UsesTargetAsPrimaryEntity()
						.AppliesWhen(conditions.HasZeroOpenOrderQty));
					handlers.Add(handler => handler
						.WithTargetOf<RQRequest>()
						.OfEntityEvent<Events>(e => e.OpenOrderQtyChanged)
						.Is(g => g.OnOpenOrderQtyIncreased)
						.UsesTargetAsPrimaryEntity()
						.AppliesWhen(conditions.HasOpenOrderQty));
				}));
		}
	}
}