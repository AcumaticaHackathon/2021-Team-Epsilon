using System;
using System.Collections.Generic;
using System.Text;
using PX.Data;
using PX.Common;

namespace PX.Objects.SO
{
	[PXLocalizable(Messages.Prefix)]
	public static class Messages
	{
		public const string Prefix = "SO Error";
		public const string Approval = "Approval";

		#region Captions
		public const string ViewDocument = "View Document";
		#endregion

		#region Graph Names
		public const string SOOrder = "Sales Order";
		public const string SOLine = "Sales Order Line";
		public const string BillingAddress = "Billing Address";
		public const string BillingContact = "Billing Contact";
		public const string ShippingAddress = "Shipping Address";
		public const string ShippingContact = "Shipping Contact";
		public const string SOSetup = "Sales Orders Preferences";
		public const string SOPaymentProcessFilter = "Credit Card Processing for Sales Filter";
		public const string SOPaymentProcessResult = "Credit Card Processing for Sales Result";
		public const string POForSalesOrderFilter = "Intercompany Purchase Orders Filter";
		public const string POForSalesOrderDocument = "Intercompany Purchase Orders Documents";
		#endregion

		#region View Names
		#endregion

		#region DAC Names
		public const string SOAdjust = "Sales Order Adjust";
		public const string SOLineSplit = "Sales Order Line Split";
		public const string SOOrderDiscountDetail = "Sales Order Discount Detail";
		public const string SOSalesPerTran = "SO Salesperson Commission";
		public const string SOTaxTran = "Sales Order Tax";
		public const string SOOrderShipment = "Sales Order Shipment";
		public const string SOShipmentDiscountDetail = "Shipment Discount Detail";
		public const string SOShipLineSplit = "Shipment Line Split";
		public const string SOShipLineSplitForPacking = "Shipment Line Split For Packing";
		public const string SOShipLineSplitPackage = "Shipment Package Detail";
		public const string SOPackageDetail = "SO Package Detail";
		public const string SOShipment = "Shipment";
		public const string SOShipLine = "Shipment Line";
		public const string ShipmentContact = "Shipment Contact";
		public const string ShipmentAddress = "Shipment Address";
		public const string SOFreightDetail = "SO Freight Detail";
		public const string SOInvoice = "SO Invoice";
		public const string SOAddress = "SO Address";
		public const string SOContact = "SO Contact";
		public const string Carrier = "Carrier";
		public const string SOTax = "SO Tax Detail";
		public const string SOOrderSite = "SO Order Warehouse";
		public const string SOOrderTypeOperation = "SO Order Type Operation";
		public const string SOPackageInfo = "SO Package Info";
		public const string SOSetupApproval = "SO Approval";
		public const string SOPackageDetailSplit = "Package Detail Split";
		public const string SOPickPackShipUserSetup = "Pick Pack Ship User Setup";
		public const string SOPickPackShipSetup = "Pick Pack Ship Setup";
		public const string SOCartShipment = "Shipment Cart";
		public const string SOShipmentSplitToCartSplitLink = "Shipment Line Split To Cart Split Link";
		public const string SOPickListEntryToCartSplitLink = "Pick List Entry To Cart Split Link";
		public const string SOPickingWorksheet = "SO Shipment Picking Worksheet";
		public const string SOPickingWorksheetLine = "SO Shipment Picking Worksheet Line";
		public const string SOPickingWorksheetLineSplit = "SO Shipment Picking Worksheet Line Split";
		public const string SOPickingWorksheetShipment = "SO Shipment Picking Worksheet Link";
		public const string SOPicker = "SO Picker";
		public const string SOPickerListEntry = "SO Picker List Entry";
		public const string SOPickerToShipmentLink = "SO Picker to Shipment Link";
		public const string SOPackingSlipParams = "SO Packing Slip Parameters";
		public const string SOQuickPayment = "SO Quick Payment";
		public const string SOImportCCPayment = "SO Import CC Payment";
		public const string DropShipSOLine = "SO Drop-Ship Line";
		public const string SupplyPOLine = "Supply PO Line";
		#endregion

		#region Field Names
		public const string OrigOrderQty = "Ordered Qty.";
		public const string OpenOrderQty = "Open Qty.";
		public const string SiteDescr = "Warehouse Description";
		public const string CarrierDescr = "Ship Via Description";
		public const string CustomerID = "Customer ID";

		public const string ReceiptNbr = "Receipt Nbr.";
		public const string ShipmentNbr = "Shipment Nbr.";
		public const string ReceiptDate = "Date";
		public const string ShipmentDate = "Shipment Date";
		public const string Fits = "Fits";
		public const string DropShip = "Drop Ship";

		#endregion

		#region Validation and Processing Messages

		public const string DontApprovedDocumentsCannotBeSelected = "No payment can be created for a sales order with any of the following statuses: Voided, Cancelled, or Pending approval.";
		public const string MustBeUserNumbering = "Invoice Number is specified, Manual Numbering should be activated for '{0}'.";
		public const string MissingShipmentControlTotal = "Control Total is required for shipment confirmation.";
		public const string UnableConfirmZeroShipment = "Unable to confirm zero shipment {0}.";
		public const string UnableConfirmShipment = "Unable to confirm empty shipment {0}.";
		public const string UnableConfirmZeroOrderShipment = "Unable to confirm shipment {0} with zero Shipped Qty. for Order {1} {2}.";
		public const string MissingMassProcessWorkFlow = "Work Flow is not set up for this screen.";
		public const string DocumentOutOfBalance = AR.Messages.DocumentOutOfBalance;
		public const string DocumentBalanceNegative = AR.Messages.DocumentBalanceNegative;
		public const string DocumentBalanceNegativePremiumFreight = "Negative Premium Freight is greater than Document Balance. Document Balance will go negative.";
		public const string AssignNotSetup = "Default Sales Order Assignment Map is not entered in Sales Orders Preferences";
		public const string AssignNotSetup_Shipment = "Default Sales Order Shipment Assignment Map is not entered in Sales Orders Preferences";
		public const string CannotShipComplete_Line = "Shipment cannot be confirmed because the quantity of the item for the line '{0}' with the Ship Complete setting is less than the ordered quantity.";
		public const string CannotShipComplete_Order = "Shipment cannot be confirmed for the order with the order-level Ship Complete setting, because the line '{0}' is not included in the shipment.";
		public const string CannotShipCompleteTraced = "Order {0} {1} cannot be shipped in full. Check Trace for more details.";
		public const string CannotShipTraced = "Order {0} {1} does not contain any available items. Check previous warnings.";
		public const string NothingToShipTraced = "Order {0} {1} does not contain any items planned for shipment on '{2}'.";
		public const string NothingToReceiveTraced = "Order {0} {1} does not contain any items planned for receipt on '{2}'.";
		[Obsolete(Common.Messages.MethodIsObsoleteAndWillBeRemoved2020R1)]
		public const string InvoiceCheck_QtyNegative = "Item '{0} {1}' in invoice '{2}' quantity returned is greater than quantity invoiced.";
		public const string InvoiceCheck_DecreaseQty = "The return quantity exceeds the quantity available for return for the related invoice line {0}, {1}. Decrease the quantity in the current line, or in the corresponding line of another return document or documents {2} that exist for the invoice line.";
		public const string InvoiceCheck_QtyLotSerialNegative = "Item '{0} {1}' in invoice '{2}' lot/serial number '{3}' quantity returned is greater than quantity invoiced.";
		public const string InvoiceCheck_SerialAlreadyReturned = "Item '{0} {1}' in invoice '{2}' serial number '{3}' is already returned.";
		public const string InvoiceCheck_LotSerialInvalid = "Item '{0} {1}' in invoice '{2}' lot/serial number '{3}' is missing from invoice.";
		public const string OrderCheck_QtyNegative = "Item '{0} {1}' in order '{2} {3}' quantity shipped is greater than quantity ordered.";
		public const string OrderSplitCheck_QtyNegative = "For item '{0} {1}' in order '{2} {3}', the quantity shipped is greater than the quantity allocated.";
		public const string BinLotSerialInvalid = "Shipment Scheduling and Bin/Lot/Serial assignment are not possible for non-stock items.";
		public const string BackOrderCannotBeDeleted = "Back Order cannot be deleted.";
		public const string ShippedSOOrderCannotBeDeleted = "The sales order cannot be deleted because it has a shipment.";
		public const string WarehouseWithoutAddressAndContact = "The Multiple Warehouses feature and the Transfer order type are activated in the system, in this case an address and a contact must be configured for the '{1}' warehouse.";
		public const string DestinationSiteContactMayNotBeEmpty = "The document cannot be saved, contact is not specified for selected destination warehouse.";
		public const string DestinationSiteAddressMayNotBeEmpty = "The document cannot be saved, address is not specified for selected destination warehouse.";
		public const string CustomerContactMayNotBeEmpty = "Shipping Contact";
		public const string CustomerAddressMayNotBeEmpty = "Shipping Contact";

		public const string Availability_Info = IN.Messages.Availability_Info;
		public const string Availability_AllocatedInfo = "On Hand {1} {0}, Available {2} {0}, Available for Shipping {3} {0}, Allocated {4} {0}";

		public const string DiscountEngine_InventoryIDIsNull = "Given line is not properly initialized before calling this method. InventoryID property should be set to a valid value.";
		public const string DiscountEngine_QtyIsNull = "Given line is not properly initialized before calling this method. Qty property should be set to a valid value.";
		public const string DiscountEngine_CuryExtPriceIsNull = "Given line is not properly initialized before calling this method. CuryExtPrice property should be set to a valid value.";
		public const string ShipmentExistsForSiteCannotReopen = "Another shipment already created for order {0} {1}, current shipment cannot be reopened.";
		public const string ShipmentInvoicedCannotReopen = "Shipment already posted to inventory or invoiced for order {0} {1}, shipment cannot be reopened.";
		public const string ShipmentCancelledCannotReopen = "Order {0} {1} is cancelled, shipment cannot be reopened.";
		public const string PromptReplenishment = "Transfer from Sales-not-allowed location or Replenishment is required for item '{0}'.";
		public const string ItemNotAvailableTraced = "There is no stock available at the {1} warehouse for the {0} item.";
		public const string ItemWithSubitemNotAvailableTraced = "There is no stock available at the {1} warehouse for the {0} item with the {2} subitem.";
		public const string ItemNotAvailable = "There is no stock available for this item.";
		public const string DefaultRateNotSetup = "Default Rate Type is not configured in Accounts Receivable Preferences.";
		public const string FreightAccountIsRequired = "Freight Account is required. Order Type is not properly setup.";
		public const string NoDropShipLocation = "Drop-Ship Location is not configured for warehouse {0}";
		public const string NoRMALocation = "RMA Location is not configured for warehouse {0}";
		public const string OrderTypeShipmentOrLocation = "Process Shipment or Require Location should be defined when Inventory Transaction Type is specified.";
		public const string OrderTypeShipmentNotLocation = "Require Location should be off if Process Shipment is specified.";
		public const string OrderTypeUnsupportedCombination = "Selected combination of Inventory Transaction Type and AR Document Type is not supported.";
		public const string OrderTypeUnsupportedOperation = "Selected Inventory Transaction Type is not supported for this type operation.";
		public const string FailedToConvertToBaseUnits = "Failed to convet to Base Units and Check for Minimum Gross Profit requirement";
		public const string CarrierServiceError = "Carrier Service returned error. {0}";
		public const string WarehouseIsRequired = "Warehouse is required. It's address is used as shipment origin.";
		public const string RateTypeNotSpecified = "Carrier returned rates in currency that differs from the Order currency. RateType that is used to convert from one currency to another is not specified in AR Preferences.";
		public const string ReprintLabels = "Labels can be used only once. Are you sure you want to reprint labels?";
		public const string WeightExceedsBoxSpecs = "The weight specified exceeds the max. weight of the box. Choose a bigger box or use multiple boxes.";
		public const string TransferOrderCreated = "Transfer Order '{0}' created.";
		public const string OrderApplyAmount_Cannot_Exceed_OrderTotal = "Amount Applied to Order cannot exceed Order Total";
		public const string PreAutorizationAmountShouldBeEntered = "Pre-authorized Amount must be entered if pre-authorization number is provided";
		public const string CCProcessingSOTranHeldWarning = "The transaction is held for review by the processing center. Use the processing center interface to approve or reject the transaction. After the transaction is processed, capture or void it on this form.";
		public const string CannotCancelCCProcessed = "Cannot cancel the order because Credit Card transaction is pre-authorized or captured. Please see the details of the error that occurred on updating the Sales Order in Trace Log.";
		public const string CannotPerformCCCapture = "Cannot capture the transaction because it is already linked to a payment. Open the payment on the Payments and Applications (AR302000) form and select Actions > Capture to capture the transaction.";
		public const string BinLotSerialNotAssigned = IN.Messages.BinLotSerialNotAssigned;
		public const string LineBinLotSerialNotAssigned = "There are '{0}' items in the document that do not have a location code or a lot/serial number assigned. Use the Line Details pop-up window to correct the items.";
		public const string CarrierWeightUOMIsEmpty = "Cannot request information from the carrier because the weight UOM for the carrier is not specified. Specify the weight UOM for the {0} carrier on the Carriers (CS207700) form.";
		public const string CarrierLinearUOMIsEmpty = "Cannot request information from the carrier because the linear UOM for the carrier is not specified. Specify the linear UOM for the {0} carrier on the Carriers (CS207700) form.";
		public const string ConfirmationIsRequired = "Confirmation for each and every Package is required. Please confirm and retry.";
		public const string MixedFormat = "Your selection contains mixed label format: {0} Thermal Labels and {1} Laser labels. Please select records of same kind and try again.";
		public const string FailedToSaveMergedFile = "Failed to Save Merged file.";
		public const string MissingSourceSite = "Missing Source Site to create transfer.";
		public const string EqualSourceDestinationSite = "Source and destination sites should not be the same.";
		public const string PartialInvoice = "Sales Order/Shipment cannot be invoiced partially.";
		public const string AddingOrderShipmentErrorsOccured = "The document cannot be saved because one or more errors have occurred during adding the line. Cancel the changes and verify the details of the sales order {0} and shipment {1}.";
		public const string ShippedLineDeleting = "Cannot delete line with shipment.";
		public const string ConfirmShipDateRecalc = "Do you want to update order lines with changed requested date and recalculate scheduled shipment date?";
		public const string PlanDateGreaterShipDate = "Scheduled Shipment Date greater than Shipment Date";
		public const string CannotDeleteTemplateOrderType = "Order type cannot be deleted. It is in use as template for order type '{0}'.";
		public const string CannotDeleteOrderType = "Order type cannot be deleted. It is used in transactions.";
		public const string CustomeCarrierAccountIsNotSetup = "Customer Account is not configured. Please setup the Carrier Account on the Carrier Plug-in screen.";
		public const string TaskWasNotAssigned = "Failed to automatically assign Project Task to the Transaction. Please check that the Account-Task mapping exists for the given account '{0}' and Task is Active and Visible in the given Module.";
		public const string PackageIsRequired = "At least one confirmed package is required to confirm this shipment.";
		public const string UnbilledBalanceWithoutTaxTaxIsNotUptodate = "Balance does not include Tax. Unbilled Tax is not up-to-date.";
		public const string UseInvoiceDateFromShipmentDateWarning = "Shipment Date will be used for Invoice Date.";
		public const string InvalidShipmentCounters = "Shipment counters are corrupted.";
		public const string SalesOrderWillBeDeleted = "The Sales Order has a payment applied. Deleting the Sales Order will delete the payment reservation. Continue?";
		public const string DiscountsWereNotCopiedToReturnOrder = "The group or document discounts from the originating {0} invoice are not inherited by the return order. Change the discount in the return order manually if needed.";

		public const string AtleastOnePackageIsRequired = "When using 'Manual Packaging' option at least one package must be defined before a Rate Quote can be requested from the Carriers.";
		public const string AtleastOnePackageIsInvalid = "Warehouse must be defined for all packages before a Rate Quote can be requested from the Carriers.";
		public const string AutoPackagingZeroPackWarning = "Autopackaging for {0} resulted in zero packages. Please check your settings. Make sure that boxes used for packing are configured for a carrier.";
		public const string WarehouseAddressIsEmpty = "The address information was not found. Make sure that {0} warehouse has the address information filled in on the Warehouses (IN204000) form.";

		public const string NoOrder = "<NEW>";
		public const string NoOrderType = "IN";
		public const string AccountMappingNotConfigured = "Account Task Mapping is not configured for the following Project: {0}, Account: {1}";
		public const string NoBoxForItem = "Packages is not configured properly for item {0}. Please correct in on Stock Items screen and try again. Given item do not fit in any of the box configured for it.";
		public const string OrderTypeInactive = "Order Type is inactive.";
		public const string CarrierBoxesAreNotSetup = "Carrier {0} do not have any boxes setup for it. Please correct this and try again.";
		public const string CarrierBoxesAreNotSetupForInventory = "Carrier \"{0}\" do not have any boxes setup for \"{1}\"";
		public const string ItemBoxesAreNotSetup = "\"{0}\" do not have any boxes";
		public const string PurchaseOrderCannotBeDeselected = "The purchase order cannot be deselected because it has been fully or partially received.";
		public const string DocumentOnHoldCannotBeCompleted = "Sales Order {0} is on Hold and cannot be completed. Operation aborted.";
		public const string LotSerialSelectionForOnReceiptOnly = "Lot/Serial Nbr. can be selected for items with When Received assignment method only.";
		public const string NonStockShipReceiptIsOff = "Require Ship/Receipt is OFF in the Non-Stock settings.";
		public const string CouldNotFindCCTranPayment = "The Payment associated with the credit card transaction could not be found";
		public const string CouldNotVoidCCTranPayment = "The Payment {0} associated with the credit card transaction could not be voided";
		public const string ShipmentPlanTypeNotSetup = "For order type '{0}' and operation '{1}', no shipment plan type is specified on the Order Types (SO.20.20.00) form.";
		public const string SOPOLinkIsIvalidInPOOrder = "In the purchase order '{0}', a drop-ship line with the item '{1}' is not linked to any sales order line.";
		public const string SOPOLinkIsIvalid = "Some of drop-ship lines in the purchase orders are not linked to any sales order lines. Check Trace for more details.";
		public const string SOPOLinkNotForNonStockKit = "Non-Stock kit items cannot be linked with purchase order.";
		public const string ReceiptShipmentRequiredForDropshipNonstock = "Only items for which both 'Require Receipt' and 'Require Shipment' are selected can be drop shipped.";
		public const string DropshipmentNotAllowedForOrderType = "Drop shipping is not allowed for the {0} order type.";
		public const string ValidationFailed = "One or more rows failed to validate. Please correct and try again.";
		public const string POLinkedToSOCancelled = "Purchase Order '{0}' is cancelled.";
		public const string InventoryIDCannotBeChanged = "You cannot change Inventory ID for the allocation line {0}, because the line has been already completed.";
		public const string InvalidUOM = "The UOM can be changed only to a base one or to UOM originally used in the sales order.";
		public const string OrderHasSubsequentShipments = "The {0} shipment cannot be reopened. There are subsequent shipments for the {1} item in the {2} {3} sales order. Only the last shipment in an order can be reopened.";
		public const string CustomerChangedOnOrderWithRestrictedCurrency = "Customer cannot be changed. Currency is not allowed for the new customer.";
		public const string CustomerChangedOnOrderWithRestrictedRateType = "Customer cannot be changed. Currency rate type is not allowed for the new customer.";
		public const string CustomerChangedOnShippedOrder = "Customer cannot be changed. Either linked documents or transactions exist for the order.";
		public const string CustomerChangedOnOrderWithCCProcessed = "Customer cannot be changed. Credit card payment is authorized or captured for the order.";
		public const string CustomerChangedOnOrderWithARPayments = "Customer cannot be changed. Payment is applied to the order.";
		public const string CustomerChangedOnOrderWithDropShip = "Customer cannot be changed. Drop-ship purchase order is linked to the order.";
		public const string CustomerChangedOnOrderWithInvoices = "Customer cannot be changed. Order line refers to an invoice.";
		public const string CustomerChangedOnQuoteWithOrder = "Customer cannot be changed.The quote is referred from an order.";
		public const string CannotAddNonStockKitDirectly = "A non-stock kit cannot be added to a document manually. Use the Sales Orders (SO301000) form to prepare an invoice for the corresponding sales order.";
		public const string CannotShipEmptyNonStockKit = "The shipment cannot be confirmed for the order with the empty non-stock kit '{0}' in the line '{1}'.";
		public const string CannotCreateShipmentNonInventoryNonStockKit = "The shipment cannot be created because the settings of the {0} line have been changed. Delete the line from the sales order and add it again to update the line details.";
		public const string CannotConfirmShipmentNonInventoryNonStockKit = "The shipment cannot be confirmed because the settings of the {0} line in the {1} sales order have been changed. Delete the line from the sales order and add it again to update the line details.";
		public const string CannotEditShipLineWithDiffSite = "The quantity in this line cannot be changed because the warehouse in the line differs from the warehouse in the related sales order line.";
		public const string KitComponentIsInactive = "The '{0}' component of the kit is inactive.";
		public const string NotKitsComponent = "The item cannot be added to the shipment. It is not a component of the kit.";
		public const string OrderHasOpenShipment = "New shipment cannot be created for the sales order. The {0} {1} sales order already has the {2} open shipment.";
		public const string ReturnReason = IN.Messages.Return;
		public const string CantAddShipmentDetail = "The system cannot add the shipment details.";
		public const string OrderLineNbrOrInventoryIdNotSpecified = "Neither Order Line Nbr. nor Inventory ID have been specified for the shipment detail.";
        public const string CannotSelectSpecificSOLine = "No sales order line can be found using the specified parameters.";
        public const string TermsChangedToMultipleInstallment = "All applications have been unlinked because multiple installment credit terms were specified.";
		public const string EmptyCurrencyRateType = "Currency rate type is not specified for the document.";
		public const string CannotRecalculateFreeItemQuantity = "Applied free item discount was not recalculated because it has already been partially or completely shipped.";
		public const string TooLongValueForUPS = "The value in the {0} box is too long. Only first {1} characters will be sent to UPS.";
		public const string ManualDiscountFlagSetOnAllLines = "In the current document, all discounts are now manual.";
		public const string OneOrMoreDiscountsDeleted = "All discounts that are not applicable to the current document have been deleted.";
		public const string CannotAddOrderToInvoiceDueToTaxZoneConflict = "The following sales orders were not added to the invoice because they have tax zones other than {0}: {1}.";
		public const string CannotAddOrderToInvoiceDueToTaxCalcModeConflict = "The following sales orders were not added to the invoice because they have tax calculation mode other than {0}: {1}.";
		public const string FreightPriceNotRecalcInSO = "Freight price has not been recalculated in the sales order.";
		public const string ShipTermsUsedInSO = "Cannot change shipping terms because current shipping terms are used in the {1} sales order of the {0} type.";
		public const string ShipTermsUsedInShipment = "Cannot change shipping terms because current shipping terms are used in the shipment {0}.";
		public const string PleaseAdjustManuallyInInvoice = "Please adjust the {0} manually in the corresponding invoice or invoices.";
		public const string CantSelectShipTermsWithFreightAmountSource = "Cannot select shipping terms with Invoice Freight Price Based On set to {0}.";
		public const string CantAddOrderWithFreightAmountSource = "Cannot add the sales order because it uses shipping terms with the Invoice Freight Price Based On set to {0}.";
		public const string AutomaticDiscountInSOInvoice = "Group and document discounts were recalculated based on all the document lines. Discounts inherited from the sales orders were not recalculated, however. Please verify all discounts and delete any unnecessary ones.";
		public const string UnableToProcessUnconfirmedShipment = "The system cannot process the unconfirmed shipment {0}.";
		public const string OrderCantBeCancelled = "The {0} {1} sales order cannot be cancelled because it already has the open {2} shipment.";
		public const string OrderHasShipmentAndCantBeCancelled = "The {0} sales order cannot be canceled because some items have been shipped.";
		public const string PMInstanceNotBelongToCustomer = "The {0} card or account number does not belong to the {1} customer.";
		public const string PMInstanceNotCorrespondToPaymentMethod = "The {0} card or account number does not correspond to the {1} payment method.";
		public const string NegShipmentCantBeCreatedLocationNotSetup = "The shipment cannot be created for the negative item quantity because the default shipping location is not defined for the item {0}.";
		public const string LowQuantityPrecision = "The decimal precision for quantity values is too low to perform UOM conversion for the item {0} without loss of quantity. Increase the quantity decimal precision, or use another UOM.";
		public const string CurrencyRateDiffersInSO = "The sales order {0} cannot be added to the invoice because the currency rate in the sales order differs from the currency rate in the invoice.";

		public const string NonStockNoShipCantBeInvoicedDirectly = "Cannot add an invoice line linked to the sales order line because the non-stock item {0} does not require shipment.";
		public const string SOLineNotFound = "Sales Order line was not found.";
		public const string SOTypeCantBeInvoicedDirectly = "Cannot add an invoice line linked to the sales order line because the sales order has the {0} order type. Check the settings of the order type.";
		public const string CompletedSOLineCantBeInvoicedDirectly = "Cannot add an invoice line linked to the sales order line because the sales order line is completed.";
		public const string CustomerDiffersInvoiceAndSO = "The customer specified in the invoice differs from the customer specified in the linked sales order.";
		public const string SOLineMarkedForPOCantBeInvoicedDirectly = "Cannot add an invoice line because it is linked to the sales order line that have the Mark for PO check box selected.";
		public const string InventoryItemDiffersInvoiceAndSO = "The inventory item specified in the invoice line differs from the inventory item specified in the linked sales order line.";
		public const string OperationDiffersInvoiceAndSO = "The operation specified in the invoice line differs from the operation specified in the linked sales order line.";
		public const string IncompleteLinkToOrigInvoiceLine = "Cannot save the document because {0} is not specified in the link to original SO invoice line.";
		public const string InappropriateDestSite = "Destination warehouse belongs to another branch.";
		public const string ShipViaNotApplicableToShipment = "The ship via code selected in the shipment is not applicable to the {0} warehouse. Select a ship via code that is associated with the {0} warehouse.";
		public const string ShipViaNotApplicableToOrder = "The ship via code selected in the sales order is not applicable to the warehouses selected in sales order lines.";
		public const string CustomerOrderIsEmpty = "The reference number of a customer order is required in this sales order.";
		public const string CustomerOrderHasDuplicateWarning = "The same customer order number is used in the {0} sales order.";
		public const string CustomerOrderHasDuplicateError = "The same customer order number is used in the {0} sales order. Specify another customer order number.";
		public const string NotSupportedFileTypeFromCarrier = "The carrier service has returned the file in the {0} format, which is not allowed for upload. Add the {0} type to the list of allowed file types on the File Upload Preferences form.";
		public const string PackageContentDeleted = "The content of the packages has been cleared because the system has recalculated the packages. Specify the content of new packages, if needed.";
		public const string NotAllocatedLines = "One or more lines contain items that are not allocated.";
		public const string SOOrderNotFound = "The import of the sales order failed.";
		
		public const string CantCancelDocType = "The document of the {0} type cannot be canceled.";
		public const string CantCancelMultipleInstallmentsInvoice = "The invoice cannot be canceled because credit terms with multiple installments is specified.";
		public const string CantCancelInvoiceWithUnreleasedApplications = "The invoice cannot be canceled because it has unreleased applications.";
		public const string CantCancelInvoiceWithCM = "The invoice {0} cannot be canceled because it has the credit memo {1} applied. Reverse the credit memo application before you cancel the invoice.";
		public const string CantCancelInvoiceWithPayment = "The invoice {0} cannot be canceled because the payment {1} is applied. Reverse the payment application before you cancel the invoice.";
		public const string CantCancelInvoiceWithDirectStockSales = "The invoice {0} cannot be canceled because it contains direct sale lines with stock items. Process a direct return to cancel the direct sale.";
		public const string CantCancelInvoiceWithOrdersNotRequiringShipments = "The invoice {0} cannot be canceled because it includes line or lines linked to an order that does not require shipping.";
		public const string CantCancelInvoiceWithNotCompletedCertainOrderType = "The {0} invoice cannot be canceled or corrected because one or multiple lines from the {0} invoice have been added to the {1} sales order of the {2} type.";
		public const string OnlyCancelCreditMemoCanBeApplied = "The cancellation credit memo can be applied to the invoice {0} only in full.";
		public const string CancellationInvoiceExists = "The cancellation invoice cannot be created because another cancellation invoice {0} already exists for the invoice {1}.";
		public const string CantCreateApplicationToInvoiceUnderCorrection = "The application cannot be created because another cancellation invoice or correction invoice already exists for the invoice {0}.";
		public const string CantReverseCancellationApplication = "The application of the cancellation credit memo {0} to invoice {1} cannot be reversed.";
		public const string InvoiceUnderCorrection = "The invoice {0} cannot be selected because the correction invoice exists for this invoice.";
		public const string InvoiceCanceled = "The invoice {0} cannot be selected because it is canceled.";

		public const string OrderWithAppliedPaymentsCantBeCancelled = "The sales order {0} cannot be canceled because it has the {1} payment applied. Reverse the payment application before you cancel the order.";
		public const string OrderWithAppliedPaymentsCantBeDeleted = "The sales order {0} cannot be deleted because it has the {1} payment applied. Reverse the payment application before you delete the order.";
		public const string PaymentsCantBeRemovedTransferedToInvoice = "The {0} payment cannot be removed because it has the nonzero Transferred to Invoice amount.";
		public const string PaymentShouldBeLessUnpaidAmount = "The payment amount should be less than or equal to the order unpaid amount ({0}).";
		public const string PaymentShouldBeMoreZero = "The payment amount should be more than zero.";
		public const string PrepaymentShouldBeMoreZero = "The value of the Prepayment Amount box should be greater than zero.";
		public const string PrepaymentShouldBeLessOrderTotalAmount = "The value of the Prepayment Amount box should be less than or equal to the total amount of the {0} sales order.";
		public const string PrepaymentPercentShouldBeMoreZero = "The value of the Prepayment Percent box should be greater than zero.";
		public const string PrepaymentPercentShouldBeLess100 = "The value of the Prepayment Percent box should be less than 100.";
		public const string PaymentProcessingError = "Processing of the {0} transaction has failed.";
		public const string CapturePaymentWithMultipleApplicationsError = "The {0} payment has multiple applications and cannot be captured for a separate document. To capture the payment, use the Actions > Capture action on the Payments and Applications (AR302000) form.";
		public const string CapturePrepaymentWithMultipleApplicationsError = "The {0} prepayment has multiple applications and cannot be captured for a separate document. To capture the prepayment, use the Actions > Capture action on the Payments and Applications (AR302000) form.";
		public const string VoidPaymentWithMultipleApplicationsError = "The {0} payment has multiple applications and cannot be voided for a separate document. To void the payment, click Actions > Void Card Payment on the Payments and Applications (AR302000) form.";
		public const string VoidPrepaymentWithMultipleApplicationsError = "The {0} prepayment has multiple applications and cannot be voided for a separate document. To void the prepayment, click Actions > Void Card Payment on the Payments and Applications (AR302000) form.";
		public const string CaptureTransferedToInvoicePaymentError = "The invoice number {0} is prepared for the sales order {1}. Capture the payment {2} on the Invoices (SO303000) form or on the Payments and Applications (AR302000) form.";
		public const string CaptureTransferedToInvoicePrepaymentError = "The invoice number {0} is prepared for the sales order {1}. Capture the prepayment {2} on the Invoices (SO303000) form or on the Payments and Applications (AR302000) form.";
		public const string VoidTransferedToInvoicePaymentError = "The invoice number {0} is prepared for the sales order {1}. Void the payment {2} on the Invoices (SO303000) form or on the Payments and Applications (AR302000) form.";
		public const string VoidTransferedToInvoicePrepaymentError = "The invoice number {0} is prepared for the sales order {1}. Void the prepayment {2} on the Invoices (SO303000) form or on the Payments and Applications (AR302000) form.";
		public const string AmountToCaptureMustBeGreaterThanZero = "Amount to capture must be greater than zero.";
		public const string CantProcessSOBecauseItHasLegacyCCTran = "Cannot process the {0} {1} sales order because it contains an obsolete credit card transaction. Please process the transaction on the Generate Payments for Card Transactions (SO511000) form before you can continue.";
		public const string CantProcessSOInvoiceBecauseItHasLegacyCCTran = "Cannot process the {0} invoice because it contains an obsolete credit card transaction. Please process the transaction on the Generate Payments for Card Transactions (SO511000) form before you can continue.";
		public const string PaymentHasNoActiveAuthorizedOrCapturedTransactions = "The {0} payment has no active pre-authorized or captured transactions. To process the {0} payment, open the Payments and Applications (AR302000) form.";
		public const string PaymentHasNoActiveAuthorizedOrCapturedTransactionsTriesMoreZero = "The max. number of attempts to reauthorize the {0} payment has not been reached. To process the {0} payment, open the Payments and Applications (AR302000) form.";
		public const string CCPaymentMethodIsNotSupportedInSOCashSale = "The credit card payment method is not supported in sales orders of the Cash Sales and Cash Return type.";
		public const string CCPaymentMethodIsNotSupportedInSOInvoiceCashSale = "The credit card payment method is not supported in invoices of the Cash Sales and Cash Return type.";

		public const string DropShipSOLineUnreleasedReceiptExists = "The check box cannot be cleared because there are one or more unreleased PO receipts that contain lines of the linked {0} purchase order.";
		public const string DropShipSOLineHasActiveLink = "The line has an active link to a line of the {0} purchase order. To make changes, clear the PO Linked check box.";
		public const string DropShipSOLineNoLink = "The sales order line has no active link to a line of a purchase order.";
		public const string DropShipSOLineValidationFailed = "The {0} column value in the line does not match the {0} column value in the linked line of the {1} purchase order.";
		public const string DropShipLinkedPOLineCompleted = "The linked {0} purchase order line has been completed.";
		public const string DropShipSOLineHasLinkAndCantBeDeleted = "The line cannot be deleted because there is an active link to a purchase order line.";
		public const string DropShipSOOrderDeletionConfirmation = "The {0} sales order has a link to a drop-ship purchase order. Do you want to remove the link and delete the {0} sales order?";
		public const string DropShipSOOrderCancelationConfirmation = "The {0} sales order has a link to a drop-ship purchase order. Do you want to remove the link and cancel the {0} sales order?";
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2021R2)]
		public const string DropShipSOLineShouldHaveSingleSplit = "A sales order line with multiple lines in the Line Details dialog box cannot be drop-shipped.";
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2021R2)]
		public const string DropShipSOLineShouldHaveSingleSplitExt = "The {0} line of the sales order cannot be drop-shipped because this line is split into multiple lines in the Line Details dialog box.";
		public const string DropShipSOLineCantHaveMultipleSplitsOrAllocation = "The line cannot be drop-shipped because it is split into multiple lines or allocated in the Line Details dialog box.";
		public const string DropShipSOOrderDeletionReceiptExists = "The {0} sales order cannot be deleted because there are one or more unreleased PO receipts that contain lines of the linked {1} purchase order.";
		public const string DropShipSOOrderCancelationReceiptExists = "The {0} sales order cannot be canceled because there are one or more unreleased PO receipts that contain lines of the linked {1} purchase order.";
		public const string BinLotSerialEntryDisabledDS = "The Line Details dialog box cannot be opened because the line with the {0} item is marked for drop-shipping.";
		#endregion

		#region Cross Company Sales
		public const string VendorIsNotBranch = "This vendor does not belong to your organization. Select another vendor.";
		public const string CustomerIsNotBranch = "This customer does not belong to your organization. Select another customer.";

		public const string IntercompanyTaxTotalDiffers = "The sales order tax total differs from the tax total in the related purchase order.";
		public const string IntercompanyOrderTotalDiffers = "The sales order total differs from the order total in the related purchase order.";
		public const string BranchIsNotExtendedToVendor = "The {0} company or branch has not been extended to a vendor. To create an intercompany purchase receipt, extend the company or branch to a vendor on the Companies (CS101500) or Branches (CS102000) form, respectively.";
		public const string IntercompanyDifferentProjectIDsOnPOLines = "A sales order has not been generated because different projects are specified in the purchase order lines. To create a sales order and copy project details from the related purchase order, create a separate purchase order for each project. If you do not need to copy project details, clear the Copy Project Details to Generated Sales Orders check box and generate a sales order again.";
		public const string SOCreatedSuccessfully = "The {1} sales order of the {0} type has been created successfully.";
		public const string PRCreatedSuccessfully = "The {0} purchase receipt has been created successfully.";
		public const string DiscountDetailLineCannotBeDeleted = "The discount cannot be deleted because the Disable Editing Prices and Discounts check box is selected on the Sales Orders Preferences (SO101000) form.";
		public const string IntercompanyPOCancelled = "The related purchase order has been canceled.";
		public const string POCancelledPRCannotBeCreated = "The purchase receipt cannot be created because the related {0} purchase order has been canceled.";
		

		public const string PurchaseOrders = "Purchase Orders";
		public const string PurchaseReturns = "Purchase Returns";

		public const string PurchasingCompany = "Purchasing Company";
		public const string SellingCompany = "Selling Company";

		public const string IntercompanyPONbr = "Related PO Nbr.";

		#endregion

		#region Translatable Strings used in the code


		public const string SOCreateShipment = "Create Shipment";

		public const string ShipComplete = "Ship Complete";
		public const string BackOrderAllowed = "Back Order Allowed";
		public const string CancelRemainder = "Cancel Remainder";

		public const string Inventory = "Goods for Inventory";
		public const string NonInventory = "Non-Inventory Goods";
		public const string MiscCharge = "Misc. Charge";

		public const string ShipVia = "Ship Via";
		public const string DocDiscDescr = AR.Messages.DocDiscDescr;
		public const string LineDiscDescr = "Item Discount";
		public const string FreightDescr = "Freight ShipVia {0}";
		public const string OrderType = "Order Type";
		public const string AddInvoice = "Add Invoice";

		public const string RefreshFreight = "Refresh Freight Cost";

		public const string ShipDate = "Ship Date";
		public const string CancelBy = "Cancel By";
		public const string OrderDate = "Order Date";

		public const string Packages = "Packages";

		public const string EmptyValuesFromExternalTaxProvider = AP.Messages.EmptyValuesFromExternalTaxProvider;
		public const string ExternalTaxVendorNotFound = TX.Messages.ExternalTaxVendorNotFound;
		public const string FailedGetFromAddressSO = "The system failed to get the From address from the sales order.";
		public const string FailedGetToAddressSO = "The system failed to get the To address from the sales order.";
		public const string CarrierWithIdNotFound = "No carrier with the given ID was found in the system.";
		public const string CarrierPluginWithIdNotFound = "No carrier plug-in with the given ID was found in the system.";
		public const string ShipViaMustBeSet = "Ship Via must be specified before auto packaging.";
		public const string TaxZoneIsNotSet = "The Tax Zone is not specified in the document.";
		public const string OrderHaveZeroBalanceError = "An order with authorization may not have a zero balance.";
		public const string PackagesRecalcErrorReleasedDocument = "Packages cannot be recalculated on a confirmed or released document.";
		public const string PackagesRecalcErrorWarehouseIdNotSpecified = "The Warehouse ID must be specified before packages can be recalculated.";
		public const string PackagesRecalcErrorNoBoxesThatFitItem = "No boxes that fit the item {0} by weight have been found. Create a new box on the Boxes (CS207600) form, or disable auto-packaging for the {0} item.";
		public const string FailedFindSOLine = "The system failed to find SO line associated with the SO ship line when allocating free items.";
		public const string CannotMergeFiles = "Files with different formats (extensions) cannot be merged.";
		public const string ReturnedError = "{0} Returned error:{1}";
		public const string TaxWasNotImported = "Tax {0} was not imported. Error: {1}";

		public const string ShipmentOrSalesOrderInformationNotFilled = "The shipment or sales order information is not filled in.";
		public const string ShipmentsNotFound = "Shipments were not found.";
		public const string FreightDesc = PO.Messages.Freight;
		public const string ShipNotInvoicedUpdateIN = "The shipped-not-invoiced account is not used. On the subsequent invoice, revenue might be posted to a different financial period not matching the COGS period. Update IN?";
		public const string ShipNotInvoicedWarning = "The shipped-not-invoiced account is not used. On the subsequent invoice, revenue might be posted to a different financial period not matching the COGS period.";
		public const string ShipmentLineQuantityNotPacked = "One or more lines for item '{0}' have quantities that have not been packed.";
		public const string ShipmentLineSplitQuantityNotPacked = "The {0} line has quantity that has not been packed.";
		public const string QuantityPackedExceedsShippedQuantityForLine = "The quantity packed exceeds the quantity shipped for this line.";
		public const string RefreshRatesButton = "Refresh Rates";

		public const string CorrectionOfInvoice = "Correction of the {0} invoice.";

		#endregion

		#region Order + Shipment Statuses
		public const string Order = "Order";
		public const string Open = "Open";
		public const string Hold = "On Hold";
		public const string CreditHold = "Credit Hold";
		public const string PendingProcessing = "Pending Processing";
		public const string AwaitingPayment = "Awaiting Payment";
		public const string Completed = "Completed";
		public const string Cancelled = "Canceled";
		public const string Confirmed = "Confirmed";
		public const string Invoiced = "Invoiced";
		public const string BackOrder = "Back Order";
		public const string Shipping = "Shipping";
		public const string Receipted = "Receipted";
		public const string AutoGenerated = "Auto-Generated";
		public const string PartiallyInvoiced = "Partially Invoiced";
		#endregion

		#region Shipment Type
		public const string Normal = "Shipment";
		#endregion

		#region Operation Type
		public const string Issue = IN.Messages.Issue;
		public const string Receipt = IN.Messages.Receipt;
		#endregion

		#region Order Behavior
		public const string SOName = "Sales Order";
		public const string INName = "Invoice";
		public const string QTName = "Quote";
		public const string RMName = "RMA Order";
		public const string CMName = "Credit Memo";
		#endregion

		#region AddItemType
		public const string BySite = "All Items";
		public const string ByCustomer = "Sold Since";
		#endregion

		#region Sales Price Update Unit

		public const string BaseUnit = "Base Unit";
		public const string SalesUnit = "Sales Unit";

		#endregion

		#region Custom Actions
		public const string Process = IN.Messages.Process;
		public const string ProcessAll = IN.Messages.ProcessAll;
		public const string CancelInvoice = "Cancel Invoice";
		public const string CorrectInvoice = "Correct Invoice";
		public const string CaptureCCPayment = "Capture";
		public const string VoidCCPayment = "Void Card Payment";
		public const string VoidExpiredCCPayment = "Void Expired Card Payment";
		public const string ValidateCCPayment = "Validate Card Payment";
		public const string ReAuthorizeCCPayment = "Reauthorize";

		public const string CreatePayment = "Create Payment";
		public const string CreatePrepayment = "Create Prepayment";

		#endregion

		#region DiscountAppliedTo
		public const string ExtendedPrice = "Item Extended Price";
		public const string SalesPrice = "Item Price";
		#endregion

		#region FreeItemShipType
		public const string Proportional = "Proportional";
		public const string OnLastShipment = "On Last Shipment";
		#endregion

		#region Freight Allocation
		public const string FullAmount = "Full Amount First Time";
		public const string Prorate = "Allocate Proportionally";
		#endregion

		#region Customer Order Validation
		public const string CustOrdeNoValidation = "Allow Duplicates";
		public const string CustOrderWarnOnDuplicates = "Warn About Duplicates";
		public const string CustOrderErrorOnDuplicates = "Forbid Duplicates";
		#endregion

		#region Minimal Gross Profit Validation Allocation
		public const string None = "No Validation";
		public const string Warning = "Warning";
		public const string SetToMin = "Set to minimum";
		#endregion

		#region PackagingType

		public const string PackagingType_Auto = SOPackageType.DisplayNames.Auto;
		public const string PackagingType_Manual = SOPackageType.DisplayNames.Manual;
		public const string PackagingType_Both = SOPackageType.ForFiltering.DisplayNames.Both;

		#endregion

		#region Non-Stock Kits Profitability

		public const string StockComponentsCostOnly = "Stock Component Cost";
		public const string NSKitStandardCostOnly = "Non-Stock Kit Standard Cost";
		public const string NSKitStandardAndStockComponentsCost = "Non-Stock Kit Standard Cost Plus Stock Component Cost";
		#endregion

		public static string GetLocal(string message)
		{
			return PXLocalizer.Localize(message, typeof(Messages).FullName);
		}
	}
}
