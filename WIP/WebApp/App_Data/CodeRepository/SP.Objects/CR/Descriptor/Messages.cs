using PX.Common;

namespace SP.Objects
{
	[PXLocalizable]
	public class Messages
	{
		public const string WarningNumReached = "The numbering sequence is expiring.";
		public const string EndOfNumberingReached = "Cannot generate the next number for the sequence because it is expired.";
		public const string CantManualNumber = "Cannot generate the next number. Manual Numbering is activated for '{0}'";
		public const string NoNumberNewSymbol = "<SELECT>";
		public const string NumberingIDNull = "Numbering ID is null.";
		public const string CantAutoNumber = "Cannot generate the next number for the sequence.";
		public const string CantAutoNumberSpecific = "Cannot generate the next number for {0}.";

        public const string NeedToConfigCaseSuccessPage = "Please, configure success case creation wiki-page.";
		public const string NeedToConfigAcceptOpportunitySuccessPage = "Please, configure success mail creation creation wiki-page.";

		public const string Open = "Open";
		public const string AnswerProvided = "Answer Provided";
		public const string MoreInfoRequested = "More Info Requested";
		public const string ConfirmRequested = "Confirm Requested";

		public const string Resolved = "Resolved";
		public const string CustomerAbandoned = "Customer Abandoned";
        public const string CustomerReplied = "Answered";
		public const string NoSolution = "No Solution";
		public const string NoAnswer = "No Answer";
		public const string Duplicate = "Duplicate";
        public const string ContractExpired = "Contract Expired";
        public const string Provisioning = "Provisioning";
        public const string Ready = "Ready to use";
        public const string Available= "Available";
        public const string NotAvailable= "Not available";
		public const string NewSupportCase = "New Support Case";
		public const string Confirmation = "Confirmation";

        public const string NotValidStartPage = "Wiki cannot be selected as Start Page";

		public const string OpenDocument = "Open Document";
		public const string DeliveryDate = "Delivery Date";

		public const string GetBaccountsErrorMessage = "Your user profile is not associated with any Business Account";
		public const string CustomerInactiveErrorMessage = "Your customer account is inactive";
		public const string ConfigurationError = "Configuration Error";
        public const string CustomerHaveNoDefContact = "Your customer have no contact";
        public const string ErrorType = "Warning";

        public const string ClearCartMessage = "All items will be removed from cart. Are you sure you want to clear cart?";

		public const string EmptyCart = "Your cart is empty";

		public const string PrintOrder = "Print Order";
		public const string ViewShipments = "View Shipments";
		public const string CopyOrdertoCart = "Copy Order to Cart";
		public const string CancelOrder = "Cancel Order";
		public const string DeletefromCart = "Delete from Cart";
		public const string PrintOrderAccessError = "The current user is not registered as a contact of the customer that is specified in the order.";	

		public const string ReopenCase = "Reopen Case";
		public const string CloseCase = "Close Case";
        public const string UnfortunatelyItsNotPosibleToReopenThisCase = "Unfortunately, it's not posible to reopen this case";
		public const string AreYouSureToReopenThisCase = "Are you sure to reopen this case?";
		public const string AreYouSureToCloseThisCase = "Are you sure to close this case?";

		public const string CompanyIsNotActive = "Company is inactive";
		public const string CompanyIsDeleted = "Company is deleted";
		public const string BranchIsNotActive = "Branch is inactive";
		public const string BranchIsDeleted = "Branch is deleted";

		public const string PortalSiteIdIsNotSpecified = "The PortalSiteID setting is not specified in web.config. " +
			"The following node should be added to the appSettings section in web.config: <add key=\"PortalSiteID\" value=\"{string}\"/>";
	}
}
