using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using State = SOOrderStatus;
	using CreatePaymentExt = GraphExtensions.SOOrderEntryExt.CreatePaymentExt;
	using DropshipReturn = GraphExtensions.SOOrderEntryExt.DropshipReturn;
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public class ScreenConfiguration : PXGraphExtension<SOOrderEntry>
	{
		public class Conditions : Condition.Pack
		{
			public Condition CanNotBeCompleted => GetOrCreate(b => b.FromBql<
				completed.IsEqual<True>.
				Or<shipmentCntr.IsEqual<Zero>>.
				Or<openShipmentCntr.IsGreater<Zero>>.
				Or<openLineCntr.IsGreater<Zero>>
			>());
		}

		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<SOOrderEntry, SOOrder>());

		protected virtual void Configure(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<status>()
					.FlowTypeIdentifierIs<behavior>()
					.WithFlows(flows =>
					{
						// To be defined separatelly in workflow extensions
					})
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState);
						actions.Add<SOOrderEntry.SOQuickProcess>(g => g.quickProcess, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales));

						actions.Add(g => g.createShipmentIssue, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales)
							.MassProcessingScreen<SOCreateShipment>() // +RM Open, SO (Back-Order, Open)
							.InBatchMode());
						actions.Add(g => g.createShipmentReceipt, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales));

						actions.Add(g => g.openOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales)
							.MassProcessingScreen<SOCreateShipment>()); // +SO Back-Order
						actions.Add(g => g.reopenOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas =>
							{
								fas.Add<cancelled>(false);
								fas.Add<completed>(false);
							}));

						actions.Add(g => g.copyOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales));
						actions.Add(g => g.emailSalesOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales)
							.WithFieldAssignments(fass => fass.Add<emailed>(e => e.SetFromValue(true)))
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.MassProcessingScreen<SOOrderProcess>(/* +IN (Completed, Invoiced, Open), +SO (Back-Order, Completed, Credit-Hold, Open, Shipping) */)
							.InBatchMode());
						actions.Add(g => g.emailQuote, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales)
							.WithFieldAssignments(fass => fass.Add<emailed>(e => e.SetFromValue(true)))
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.MassProcessingScreen<SOOrderProcess>(/* +QT (Completed, Hold, Open) */)
							.InBatchMode());

						actions.Add(g => g.releaseFromCreditHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.Approval)
							.MassProcessingScreen<SOCreateShipment>() // +IN Credit-Hold, +SO Credit-Hold
							.WithFieldAssignments(fas =>
							{
								fas.Add<approvedCredit>(true);
								fas.Add<approvedCreditAmt>(e => e.SetFromField<orderTotal>());
							}));

						actions.Add(g => g.prepareInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales)
							.MassProcessingScreen<SOCreateShipment>() // +CM Open, +IN Open, +RM (Completed, Open), +SO (Back-Order, Completed, Open)
							.InBatchMode());

						actions.Add(g => g.createPurchaseOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.PurchaseForSales));
						actions.Add<DropshipReturn>(g => g.createVendorReturn, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.PurchaseForSales));
						actions.Add(g => g.createTransferOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.PurchaseForSales));

						actions.Add(g => g.completeOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.Approval)
							.IsDisabledWhen(conditions.CanNotBeCompleted)
							.WithFieldAssignments(fas => fas.Add<forceCompleteOrder>(true)));
						actions.Add(g => g.cancelOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales)
							.WithPersistOptions(ActionPersistOptions.ForcePersist)
							.MassProcessingScreen<SOCreateShipment>() // +QT (Hold, Open), +RM (Hold, Open), +SO (Hold, Open)
							.WithFieldAssignments(fas =>
							{
								fas.Add<cancelled>(true);
								fas.Add<hold>(false);
								fas.Add<creditHold>(false);
								fas.Add<inclCustOpenOrders>(false);
							}));
						actions.Add(g => g.placeOnBackOrder, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.Approval));

						actions.Add(g => g.putOnHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.Approval)
							.WithFieldAssignments(fas => fas.Add<hold>(true)));
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.Approval)
							.WithFieldAssignments(fas => fas.Add<hold>(false)));

						actions.Add(g => g.validateAddresses, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.recalculateDiscountsAction, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.RecalculateOrder));
						actions.Add<SOOrderEntryExternalTax>(e => e.recalcExternalTax, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.RecalculateOrder));

						actions.Add<CreatePaymentExt>(e => e.createAndAuthorizePayment, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales)
							.IsHiddenAlways() // only for mass processing
							.MassProcessingScreen<SOCreateShipment>());
						actions.Add<CreatePaymentExt>(e => e.createAndCapturePayment, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithCategory(ActionCategory.ProcessSales)
							.IsHiddenAlways() // only for mass processing
							.MassProcessingScreen<SOCreateShipment>());

						actions.Add(g => g.printSalesOrder, c => c
							.InFolder(FolderType.ReportsFolder)
							.MassProcessingScreen<SOOrderProcess>()
							// +SO (Cancelled, Completed, Hold, Open, Credit-Hold),
							// +RM (Cancelled, Completed, Hold, Open),
							// +IN (Cancelled, Completed, Hold, Open, Invoiced, Credit-Hold),
							// +CM (Cancelled, Completed, Hold, Open, Invoiced)
							.InBatchMode());
						actions.Add(g => g.printQuote, c => c
							.InFolder(FolderType.ReportsFolder)
							.MassProcessingScreen<SOOrderProcess>()
							// +QT (Hold, Open),
							.InBatchMode());
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.OrderDeleted)
								.Is(g => g.OnOrderDeleted_ReopenQuote)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<
										orderType.IsEqual<origOrderType.FromCurrent>.
										And<orderNbr.IsEqual<origOrderNbr.FromCurrent>>>
								>()
								.DisplayName("Reopen Quote when Order Deleted");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.ShipmentCreationFailed)
								.Is(g => g.OnShipmentCreationFailed)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Shipment Creation Failed");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.ObtainedPaymentInPendingProcessing)
								.Is(g => g.OnObtainedPaymentInPendingProcessing)
								.UsesTargetAsPrimaryEntity();
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.LostLastPaymentInPendingProcessing)
								.Is(g => g.OnLostLastPaymentInPendingProcessing)
								.UsesTargetAsPrimaryEntity();
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.PaymentRequirementsSatisfied)
								.Is(g => g.OnPaymentRequirementsSatisfied)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Payment Requirements Satisfied");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.PaymentRequirementsViolated)
								.Is(g => g.OnPaymentRequirementsViolated)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Payment Requirements Violated");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.CreditLimitSatisfied)
								.Is(g => g.OnCreditLimitSatisfied)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Credit Limit Satisfied");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.CreditLimitViolated)
								.Is(g => g.OnCreditLimitViolated)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Credit Limit Violated");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrderShipment>()
								.WithParametersOf<SOShipment>()
								.OfEntityEvent<SOOrderShipment.Events>(e => e.ShipmentLinked)
								.Is(g => g.OnShipmentLinked)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<SOOrder.orderType.IsEqual<SOOrderShipment.orderType.FromCurrent>.
										And<SOOrder.orderNbr.IsEqual<SOOrderShipment.orderNbr.FromCurrent>>>
								>()
								.DisplayName("Shipment Linked");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrderShipment>()
								.WithParametersOf<SOShipment>()
								.OfEntityEvent<SOOrderShipment.Events>(e => e.ShipmentUnlinked)
								.Is(g => g.OnShipmentUnlinked)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<SOOrder.orderType.IsEqual<SOOrderShipment.orderType.FromCurrent>.
										And<SOOrder.orderNbr.IsEqual<SOOrderShipment.orderNbr.FromCurrent>>>
								>()
								.DisplayName("Shipment Unlinked");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOShipment>()
								.OfEntityEvent<SOShipment.Events>(e => e.ShipmentConfirmed)
								.Is(g => g.OnShipmentConfirmed)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									InnerJoin<SOOrderShipment>.On<SOOrderShipment.FK.Order>.
									Where<SOOrderShipment.FK.Shipment.SameAsCurrent>
								>(allowSelectMultipleRecords: true)
								.DisplayName("Shipment Confirmed");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOShipment>()
								.OfEntityEvent<SOShipment.Events>(e => e.ShipmentCorrected)
								.Is(g => g.OnShipmentCorrected)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									InnerJoin<SOOrderShipment>.On<SOOrderShipment.FK.Order>.
									Where<SOOrderShipment.FK.Shipment.SameAsCurrent>
								>(allowSelectMultipleRecords: true)
								.DisplayName("Shipment Corrected");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrderShipment>()
								.WithParametersOf<SOInvoice>()
								.OfEntityEvent<SOOrderShipment.Events>(e => e.InvoiceLinked)
								.Is(g => g.OnInvoiceLinked)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<SOOrder.orderType.IsEqual<SOOrderShipment.orderType.FromCurrent>.
										And<SOOrder.orderNbr.IsEqual<SOOrderShipment.orderNbr.FromCurrent>>>
								>()
								.DisplayName("Invoice Linked");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrderShipment>()
								.WithParametersOf<SOInvoice>()
								.OfEntityEvent<SOOrderShipment.Events>(e => e.InvoiceUnlinked)
								.Is(g => g.OnInvoiceUnlinked)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<SOOrder.orderType.IsEqual<SOOrderShipment.orderType.FromCurrent>.
										And<SOOrder.orderNbr.IsEqual<SOOrderShipment.orderNbr.FromCurrent>>>
								>()
								.DisplayName("Invoice Unlinked");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOInvoice>()
								.OfEntityEvent<SOInvoice.Events>(e => e.InvoiceReleased)
								.Is(g => g.OnInvoiceReleased)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									InnerJoin<SOOrderShipment>.On<SOOrderShipment.FK.Order>.
									Where<SOOrderShipment.FK.Invoice.SameAsCurrent>
								>(allowSelectMultipleRecords: true)
								.DisplayName("Invoice Released");
						});
					});
			});
		}

		public static class ActionCategory
		{
			public const string Approval = "Approval";
			public const string ProcessSales = "Process Sales";
			public const string PurchaseForSales = "Purchase for Sales";
			public const string RecalculateOrder = "Recalculate Order";
		}
	}
}