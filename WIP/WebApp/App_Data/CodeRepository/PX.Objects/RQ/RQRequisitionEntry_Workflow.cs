using System;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;

using PX.Objects.PO;
using PX.Objects.SO;
using PX.Objects.CM;

namespace PX.Objects.RQ
{
	using State = RQRequisitionStatus;
	using static RQRequisition;
	using static BoundedTo<RQRequisitionEntry, RQRequisition>;

	public class RQRequisitionEntry_Workflow : PXGraphExtension<RQRequisitionEntry>
	{
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<RQRequisitionEntry, RQRequisition>());

		protected virtual void Configure(WorkflowContext<RQRequisitionEntry, RQRequisition> context)
		{
			#region Conditions
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsCancelled
					= Bql<cancelled.IsEqual<True>>(),

				IsOnHold
					= Bql<hold.IsEqual<True>>(),

				IsReleased
					= Bql<released.IsEqual<True>>(),

				HasZeroOpenOrderQty
					= Bql<openOrderQty.IsEqual<Zero>>(),

				IsQuoted
					= Bql<biddingComplete.IsEqual<True>.And<quoted.IsEqual<True>>>(),

				IsBiddingCompleted
					= Bql<biddingComplete.IsEqual<True>>(),
			}.AutoNameConditions();
			#endregion

			#region Macroses
			void DisableWholeScreen(FieldState.IContainerFillerFields fieldStates)
			{
				fieldStates.AddTable<RQRequisition>(fs => fs.IsDisabled());
				fieldStates.AddTable<RQRequisitionLine>(fs => fs.IsDisabled());
				fieldStates.AddTable<POShipAddress>(fs => fs.IsDisabled());
				fieldStates.AddTable<POShipContact>(fs => fs.IsDisabled());
				fieldStates.AddTable<PORemitAddress>(fs => fs.IsDisabled());
				fieldStates.AddTable<PORemitContact>(fs => fs.IsDisabled());
				fieldStates.AddTable<RQRequisitionContent>(fs => fs.IsDisabled());
				fieldStates.AddTable<CurrencyInfo>(fs => fs.IsDisabled());
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
									actions.Add(g => g.ChooseVendor);
								});
						});
						flowStates.Add<State.bidding>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.ViewBidding, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.sendRequestToAllVendors, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.cancelRequest);
									actions.Add(g => g.requestForProposal);

									actions.Add(g => g.sendRequestToCurrentVendor);
									actions.Add(g => g.ChooseVendor);
									actions.Add(g => g.ResponseVendor);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnBiddingCompleted);
								})
								.WithFieldStates(fieldStates =>
								{
									fieldStates.AddTable<RQRequisition>(fs => fs.IsDisabled());
									fieldStates.AddTable<RQRequisitionLine>(fs => fs.IsDisabled());
									fieldStates.AddTable<POShipAddress>(fs => fs.IsDisabled());
									fieldStates.AddTable<RQRequisitionContent>(fs => fs.IsDisabled());

									// but enable these
									fieldStates.AddField<RQRequisitionLine.requestedDate>();
									fieldStates.AddField<RQRequisitionLine.promisedDate>();
									fieldStates.AddField<RQRequisitionLine.cancelled>();
								});
						});
						flowStates.Add<State.pendingQuotation>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.createQTOrder, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.markQuoted);
									actions.Add(g => g.cancelRequest);
									actions.Add(g => g.ViewBidding);
								})
								.WithFieldStates(fieldStates =>
								{
									DisableWholeScreen(fieldStates);

									// but enable these
									fieldStates.AddField<RQRequisitionLine.siteID>();
									fieldStates.AddField<RQRequisitionLine.subItemID>();
									fieldStates.AddField<RQRequisitionLine.uOM>();
								});
						});
						flowStates.Add<State.open>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.createPOOrder, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.cancelRequest);
									actions.Add(g => g.ViewBidding);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnSOOrderUnlinked);
									// OnOpenOrderQtyExhausted -> closed
								})
								.WithFieldStates(fieldStates =>
								{
									DisableWholeScreen(fieldStates);

									// but enable these
									fieldStates.AddField<RQRequisitionLine.requestedDate>();
									fieldStates.AddField<RQRequisitionLine.promisedDate>();
									fieldStates.AddField<RQRequisitionLine.cancelled>();

									fieldStates.AddField<RQRequisitionLine.siteID>();
									fieldStates.AddField<RQRequisitionLine.subItemID>();
									fieldStates.AddField<RQRequisitionLine.uOM>();
								});
						});
						flowStates.Add<State.released>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.ViewBidding);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnPOOrderUnlinked);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.closed>(flowState =>
						{
							return flowState // OpenOrderQty == 0
								.WithActions(actions =>
								{
									actions.Add(g => g.ViewBidding);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.canceled>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold);
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
							ts.Add(t => t.To<State.released>().IsTriggeredOn(g => g.initializeState).When(conditions.IsReleased));
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.initializeState).When(conditions.HasZeroOpenOrderQty));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState).When(conditions.IsQuoted));
							ts.Add(t => t.To<State.pendingQuotation>().IsTriggeredOn(g => g.initializeState).When(conditions.IsBiddingCompleted));
							ts.Add(t => t.To<State.bidding>().IsTriggeredOn(g => g.initializeState));
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.releaseFromHold).When(conditions.IsQuoted));
							ts.Add(t => t.To<State.pendingQuotation>().IsTriggeredOn(g => g.releaseFromHold).When(conditions.IsBiddingCompleted));
							ts.Add(t => t.To<State.bidding>().IsTriggeredOn(g => g.releaseFromHold));
						});
						transitions.AddGroupFrom<State.bidding>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.OnBiddingCompleted).When(conditions.IsQuoted));
							ts.Add(t => t.To<State.pendingQuotation>().IsTriggeredOn(g => g.OnBiddingCompleted).When(conditions.IsBiddingCompleted));
							ts.Add(t => t.To<State.canceled>().IsTriggeredOn(g => g.cancelRequest).When(conditions.IsCancelled));
						});
						transitions.AddGroupFrom<State.pendingQuotation>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
							ts.Add(t => t.To<State.canceled>().IsTriggeredOn(g => g.cancelRequest).When(conditions.IsCancelled));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.markQuoted).When(conditions.IsQuoted));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.createQTOrder).When(conditions.IsQuoted));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
							ts.Add(t => t.To<State.pendingQuotation>().IsTriggeredOn(g => g.OnSOOrderUnlinked).When(conditions.IsBiddingCompleted));
							ts.Add(t => t.To<State.bidding>().IsTriggeredOn(g => g.OnSOOrderUnlinked));
							ts.Add(t => t.To<State.released>().IsTriggeredOn(g => g.createPOOrder).When(conditions.IsReleased));
							//ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.OnOpenOrderQtyExhausted));
							ts.Add(t => t.To<State.canceled>().IsTriggeredOn(g => g.cancelRequest).When(conditions.IsCancelled));
						});
						transitions.AddGroupFrom<State.released>(ts =>
						{
							//ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.OnPOOrderUnlinked).When(conditions.IsQuoted));
							//ts.Add(t => t.To<State.pendingQuotation>().IsTriggeredOn(g => g.OnPOOrderUnlinked).When(conditions.IsBiddingCompleted));
							//ts.Add(t => t.To<State.bidding>().IsTriggeredOn(g => g.OnPOOrderUnlinked));
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

					actions.Add(g => g.ViewBidding, a => a
						.InFolder(FolderType.ActionsFolder));

					actions.Add(g => g.createQTOrder, a => a
						.InFolder(FolderType.ActionsFolder)
						.WithFieldAssignments(fass => fass.Add<quoted>(v => v.SetFromValue(true)))
						.MassProcessingScreen<RQRequisitionProcess>()
						.InBatchMode());
					actions.Add(g => g.markQuoted, a => a
						.InFolder(FolderType.ActionsFolder)
						.WithFieldAssignments(fass => fass.Add<quoted>(v => v.SetFromValue(true))));
					actions.Add(g => g.createPOOrder, a => a
						.InFolder(FolderType.ActionsFolder)
						.WithFieldAssignments(fass => fass.Add<released>(v => v.SetFromValue(true)))
						.MassProcessingScreen<RQRequisitionProcess>()
						.InBatchMode());

					actions.Add(g => g.cancelRequest, a => a
						.InFolder(FolderType.ActionsFolder)
						.WithFieldAssignments(fass => fass.Add<cancelled>(v => v.SetFromValue(true))));

					actions.Add(g => g.sendRequestToAllVendors, a => a
						.InFolder(FolderType.ActionsFolder));
					actions.Add(g => g.validateAddresses, a => a
						.InFolder(FolderType.ActionsFolder));

					actions.Add(g => g.requestForProposal, a => a
						.InFolder(FolderType.ReportsFolder));

					// some grid-level actions
					actions.Add(g => g.ChooseVendor);
					actions.Add(g => g.ResponseVendor);
					actions.Add(g => g.sendRequestToCurrentVendor);
				})
				.WithHandlers(handlers =>
				{
					handlers.Add(handler => handler
						.WithTargetOf<RQRequisition>()
						.OfEntityEvent<Events>(e => e.BiddingCompleted)
						.Is(g => g.OnBiddingCompleted)
						.UsesTargetAsPrimaryEntity());
					handlers.Add(handler => handler
						.WithTargetOf<RQRequisitionOrder>()
						.WithParametersOf<SOOrder>()
						.OfEntityEvent<RQRequisitionOrder.Events>(e => e.SOOrderUnlinked)
						.Is(g => g.OnSOOrderUnlinked)
						.UsesPrimaryEntityGetter<
							SelectFrom<RQRequisition>.
							Where<reqNbr.IsEqual<RQRequisitionOrder.reqNbr.FromCurrent>>
						>());
					handlers.Add(handler => handler
						.WithTargetOf<RQRequisitionOrder>()
						.WithParametersOf<POOrder>()
						.OfEntityEvent<RQRequisitionOrder.Events>(e => e.POOrderUnlinked)
						.Is(g => g.OnPOOrderUnlinked)
						.UsesPrimaryEntityGetter<
							SelectFrom<RQRequisition>.
							Where<reqNbr.IsEqual<RQRequisitionOrder.reqNbr.FromCurrent>>
						>());
				}));
		}
	}
}