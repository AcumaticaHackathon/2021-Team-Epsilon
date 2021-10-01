using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.SO.GraphExtensions.SOShipmentEntryExt;

namespace PX.Objects.SO
{
	using State = SOShipmentStatus;
	using static SOShipment;
	using static BoundedTo<SOShipmentEntry, SOShipment>;

	public class SOShipmentEntry_Workflow : PXGraphExtension<SOShipmentEntry>
	{
		public class Conditions : Condition.Pack
		{
			public Condition IsOnHold => GetOrCreate(b => b.FromBql<
				hold.IsEqual<True>
			>());

			public Condition IsNotBillable => GetOrCreate(b => b.FromBql<
				unbilledOrderCntr.IsEqual<Zero>.
				And<billedOrderCntr.IsEqual<Zero>>.
				And<releasedOrderCntr.IsEqual<Zero>>
			>());

			public Condition IsConfirmed => GetOrCreate(b => b.FromBql<
				confirmed.IsEqual<True>.
				And<unbilledOrderCntr.IsGreater<Zero>>.
				And<billedOrderCntr.IsEqual<Zero>>.
				And<releasedOrderCntr.IsEqual<Zero>>
			>());

			public Condition IsPartiallyInvoiced => GetOrCreate(b => b.FromBql<
				confirmed.IsEqual<True>.
				And<unbilledOrderCntr.IsGreater<Zero>>.
				And<
					billedOrderCntr.IsGreater<Zero>.
					Or<releasedOrderCntr.IsGreater<Zero>>>
			>());

			public Condition IsInvoiced => GetOrCreate(b => b.FromBql<
				confirmed.IsEqual<True>.
				And<unbilledOrderCntr.IsEqual<Zero>>.
				And<billedOrderCntr.IsGreater<Zero>>
			>());

			public Condition IsCompleted => GetOrCreate(b => b.FromBql<
				confirmed.IsEqual<True>.
				And<unbilledOrderCntr.IsEqual<Zero>>.
				And<billedOrderCntr.IsEqual<Zero>>.
				And<releasedOrderCntr.IsGreater<Zero>>
			>());

			public Condition IsNotIntercompanyIssue => GetOrCreate(b => b.FromBql<
				isIntercompany.IsEqual<False>.Or<operation.IsNotEqual<SOOperation.issue>>>());

			public Condition IsIntercompanyReceiptGenerated => GetOrCreate(b => b.FromBql<
				intercompanyPOReceiptNbr.IsNotNull>());
		}

		protected virtual void DisableWholeScreen(FieldState.IContainerFillerFields states)
		{
			states.AddAllFields<SOShipment>(state => state.IsDisabled());
			states.AddField<SOShipment.shipmentNbr>();
			states.AddField<SOShipment.excludeFromIntercompanyProc>();
			states.AddTable<SOShipLine>(state => state.IsDisabled());
			states.AddTable<SOShipLineSplit>(state => state.IsDisabled());
			states.AddTable<SOShipmentAddress>(state => state.IsDisabled());
			states.AddTable<SOShipmentContact>(state => state.IsDisabled());
			states.AddTable<SOOrderShipment>(state => state.IsDisabled());
			//states.AddTable<*>(state => state.IsDisabled());
		}

		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<SOShipmentEntry, SOShipment>());

		protected virtual void Configure(WorkflowContext<SOShipmentEntry, SOShipment> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();
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
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.printPickListAction);
								});
						});
						fss.Add<State.open>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.confirmShipmentAction, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.printShipmentConfirmation);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.getReturnLabelsAction);
									actions.Add(g => g.printPickListAction);
								});
						});
						fss.Add<State.confirmed>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.createInvoice, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.UpdateIN, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.printShipmentConfirmation);
									actions.Add(g => g.correctShipmentAction);
									actions.Add(g => g.printLabels);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.emailShipment);
									actions.Add<Intercompany>(e => e.generatePOReceipt);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnInvoiceLinked);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						fss.Add<State.partiallyInvoiced>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.createInvoice, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.printShipmentConfirmation);
									actions.Add(g => g.printLabels);
									actions.Add(g => g.validateAddresses);
									actions.Add<Intercompany>(e => e.generatePOReceipt);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnInvoiceLinked);
									handlers.Add(g => g.OnInvoiceUnlinked);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						fss.Add<State.invoiced>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.validateAddresses);
									actions.Add<Intercompany>(e => e.generatePOReceipt);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnInvoiceUnlinked);
									handlers.Add(g => g.OnInvoiceReleased);
									handlers.Add(g => g.OnInvoiceCancelled);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						fss.Add<State.completed>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.printShipmentConfirmation);
									actions.Add<Intercompany>(e => e.generatePOReceipt);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnInvoiceUnlinked);
									handlers.Add(g => g.OnInvoiceCancelled);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						fss.Add<State.receipted>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.createDropshipInvoice, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.cancelReturn);
								});
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold));
							ts.Add(t => t.To<State.confirmed>().IsTriggeredOn(g => g.initializeState).When(conditions.IsConfirmed));
							ts.Add(t => t.To<State.partiallyInvoiced>().IsTriggeredOn(g => g.initializeState).When(conditions.IsPartiallyInvoiced));
							ts.Add(t => t.To<State.invoiced>().IsTriggeredOn(g => g.initializeState).When(conditions.IsInvoiced));
							ts.Add(t => t.To<State.completed>().IsTriggeredOn(g => g.initializeState).When(conditions.IsCompleted));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState));
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.releaseFromHold).WithFieldAssignments(fas => fas.Add<hold>(false)));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).WithFieldAssignments(fas => fas.Add<hold>(true)));
							ts.Add(t => t.To<State.confirmed>().IsTriggeredOn(g => g.confirmShipmentAction));
						});
						transitions.AddGroupFrom<State.confirmed>(ts =>
						{
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.correctShipmentAction));
							ts.Add(t => t.To<State.completed>().IsTriggeredOn(g => g.UpdateIN).When(conditions.IsNotBillable));
							ts.Add(t => t.To<State.invoiced>().IsTriggeredOn(g => g.OnInvoiceLinked).When(conditions.IsInvoiced));
							ts.Add(t => t.To<State.partiallyInvoiced>().IsTriggeredOn(g => g.OnInvoiceLinked).When(conditions.IsPartiallyInvoiced));
						});
						transitions.AddGroupFrom<State.partiallyInvoiced>(ts =>
						{
							ts.Add(t => t.To<State.confirmed>().IsTriggeredOn(g => g.OnInvoiceUnlinked).When(conditions.IsConfirmed));
							ts.Add(t => t.To<State.invoiced>().IsTriggeredOn(g => g.OnInvoiceLinked).When(conditions.IsInvoiced));
						});
						transitions.AddGroupFrom<State.invoiced>(ts =>
						{
							ts.Add(t => t.To<State.confirmed>().IsTriggeredOn(g => g.OnInvoiceUnlinked).When(conditions.IsConfirmed));
							ts.Add(t => t.To<State.partiallyInvoiced>().IsTriggeredOn(g => g.OnInvoiceUnlinked).When(conditions.IsPartiallyInvoiced));
							ts.Add(t => t.To<State.completed>().IsTriggeredOn(g => g.OnInvoiceReleased).When(conditions.IsCompleted));
							ts.Add(t => t.To<State.partiallyInvoiced>().IsTriggeredOn(g => g.OnInvoiceCancelled).When(conditions.IsPartiallyInvoiced));
						});
						transitions.AddGroupFrom<State.completed>(ts =>
						{
							ts.Add(t => t.To<State.confirmed>().IsTriggeredOn(g => g.OnInvoiceUnlinked).When(conditions.IsConfirmed));
							ts.Add(t => t.To<State.partiallyInvoiced>().IsTriggeredOn(g => g.OnInvoiceUnlinked).When(conditions.IsPartiallyInvoiced));
							ts.Add(t => t.To<State.invoiced>().IsTriggeredOn(g => g.OnInvoiceCancelled).When(conditions.IsInvoiced));
						});
						transitions.AddGroupFrom<State.receipted>(ts =>
						{
						});
					}))
				.WithActions(actions =>
				{
					actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
					actions.Add(g => g.confirmShipmentAction, a => a
						.InFolder(FolderType.ActionsFolder)
						.MassProcessingScreen<SOInvoiceShipment>()
						.InBatchMode());
					actions.Add(g => g.createInvoice, a => a
						.InFolder(FolderType.ActionsFolder)
						.IsDisabledWhen(conditions.IsNotBillable)
						.MassProcessingScreen<SOInvoiceShipment>()
						.InBatchMode());
					actions.Add(g => g.createDropshipInvoice, a => a
						.InFolder(FolderType.ActionsFolder)
						.MassProcessingScreen<SOInvoiceShipment>()
						.InBatchMode());
					actions.Add(g => g.UpdateIN, a => a
						.InFolder(FolderType.ActionsFolder)
						.MassProcessingScreen<SOInvoiceShipment>()
						.InBatchMode());
					actions.Add(g => g.correctShipmentAction, a => a
						.InFolder(FolderType.ActionsFolder)
						.IsDisabledWhen(conditions.IsIntercompanyReceiptGenerated));
					actions.Add(g => g.printLabels, a => a
						.InFolder(FolderType.ActionsFolder)
						.MassProcessingScreen<SOInvoiceShipment>(/* +Confirmed, +PartInvoice */)
						.InBatchMode());
					actions.Add(g => g.getReturnLabelsAction, a => a
						.InFolder(FolderType.ActionsFolder));
					actions.Add(g => g.cancelReturn, a => a
						.InFolder(FolderType.ActionsFolder));
					actions.Add(g => g.validateAddresses, a => a
						.InFolder(FolderType.ActionsFolder));
					actions.Add(g => g.emailShipment, a => a
						.InFolder(FolderType.ActionsFolder)
						.MassProcessingScreen<SOInvoiceShipment>());
					actions.Add(g => g.printPickListAction, a => a
						.InFolder(FolderType.ActionsFolder)
						.MassProcessingScreen<SOInvoiceShipment>()
						.InBatchMode());
					actions.Add(g => g.putOnHold, a => a
						// TODO: I used to add intermediate persist here because of UOM1 and UOM2 tests and because of 
						// NonDecimalUnitsNoVerifyOnHoldExt extension, that change validation of QuantityAttribute 
						// depending on current status of document.
						.WithPersistOptions(ActionPersistOptions.ForcePersist)
						.InFolder(FolderType.ActionsFolder));
					actions.Add(g => g.releaseFromHold, a => a
						.InFolder(FolderType.ActionsFolder));
					actions.Add(g => g.printShipmentConfirmation, a => a
						.InFolder(FolderType.ReportsFolder)
						.MassProcessingScreen<SOInvoiceShipment>(/* +Open */)
						.InBatchMode());
					actions.Add<Intercompany>(e => e.generatePOReceipt, a => a
						.InFolder(FolderType.ActionsFolder)
						.IsHiddenWhen(conditions.IsNotIntercompanyIssue)
						.IsDisabledWhen(conditions.IsIntercompanyReceiptGenerated));
				})
				.WithHandlers(handlers =>
				{
					handlers.Add(handler =>
					{
						return handler
							.WithTargetOf<SOOrderShipment>()
							.WithParametersOf<SOInvoice>()
							.OfEntityEvent<SOOrderShipment.Events>(e => e.InvoiceLinked)
							.Is(g => g.OnInvoiceLinked)
							.UsesPrimaryEntityGetter<
								SelectFrom<SOShipment>.
								Where<
									SOShipment.shipmentType.IsEqual<SOOrderShipment.shipmentType.FromCurrent>.
									And<SOShipment.shipmentNbr.IsEqual<SOOrderShipment.shipmentNbr.FromCurrent>>>
								>()
							.DisplayName("Invoice Linked");
					}); // Invoice Linked
					handlers.Add(handler =>
					{
						return handler
							.WithTargetOf<SOOrderShipment>()
							.WithParametersOf<SOInvoice>()
							.OfEntityEvent<SOOrderShipment.Events>(e => e.InvoiceUnlinked)
							.Is(g => g.OnInvoiceUnlinked)
							.UsesPrimaryEntityGetter<
								SelectFrom<SOShipment>.
								Where<
									SOShipment.shipmentType.IsEqual<SOOrderShipment.shipmentType.FromCurrent>.
									And<SOShipment.shipmentNbr.IsEqual<SOOrderShipment.shipmentNbr.FromCurrent>>>
								>()
							.DisplayName("Invoice Unlinked");
					}); // Invoice Unlinked
					handlers.Add(handler =>
					{
						return handler
							.WithTargetOf<SOInvoice>()
							.OfEntityEvent<SOInvoice.Events>(e => e.InvoiceReleased)
							.Is(g => g.OnInvoiceReleased)
							.UsesPrimaryEntityGetter<
								SelectFrom<SOShipment>.
								InnerJoin<SOOrderShipment>.On<SOOrderShipment.shipmentNbr.IsEqual<SOShipment.shipmentNbr>>.
								Where<SOOrderShipment.FK.Invoice.SameAsCurrent>
							>(allowSelectMultipleRecords: true)
							.DisplayName("Invoice Released");
					}); // Invoice Released
					handlers.Add(handler =>
					{
						return handler
							.WithTargetOf<SOInvoice>()
							.OfEntityEvent<SOInvoice.Events>(e => e.InvoiceCancelled)
							.Is(g => g.OnInvoiceCancelled)
							.UsesPrimaryEntityGetter<
								SelectFrom<SOShipment>.
								InnerJoin<SOOrderShipment>.On<SOOrderShipment.shipmentNbr.IsEqual<SOShipment.shipmentNbr>>.
								Where<SOOrderShipment.FK.Invoice.SameAsCurrent>
							>(allowSelectMultipleRecords: true)
							.DisplayName("Invoice Cancelled");
					}); // Invoice Cancelled
				}));
		}
	}
}