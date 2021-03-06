<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AR303000.aspx.cs" Inherits="Page_AR303000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" UDFTypeField="CustomerClassID" EnableAttributes="true" Width="100%" TypeName="PX.Objects.AR.CustomerMaint" PrimaryView="BAccount">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Save" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="First" StartNewGroup="True" />
			<px:PXDSCallbackCommand Name="ViewRestrictionGroups" Visible="False" />
			<px:PXDSCallbackCommand Name="ExtendToVendor" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewVendor" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewBusnessAccount" Visible="True" />
			<px:PXDSCallbackCommand Name="CustomerDocuments" Visible="False" />
			<px:PXDSCallbackCommand Name="StatementForCustomer" Visible="False" />
			<px:PXDSCallbackCommand Name="ViewPaymentMethod" DependOnGrid="grdPaymentMethods" Visible="false" StartNewGroup="True" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="AddPaymentMethod" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="NewInvoiceMemo" Visible="False" />
			<px:PXDSCallbackCommand Name="NewSalesOrder" Visible="False" />
			<px:PXDSCallbackCommand Name="NewPayment" Visible="False" />
			<px:PXDSCallbackCommand Name="WriteOffBalance" Visible="False" />
			<px:PXDSCallbackCommand Name="Action" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="Inquiry" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="Report" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ARBalanceByCustomer" Visible="False" />
			<px:PXDSCallbackCommand Name="CustomerHistory" Visible="False" />
			<px:PXDSCallbackCommand Name="ARAgedPastDue" Visible="False" />
			<px:PXDSCallbackCommand Name="ARAgedOutstanding" Visible="False" />
			<px:PXDSCallbackCommand Name="ARRegister" Visible="False" />
			<px:PXDSCallbackCommand Name="CustomerDetails" Visible="False" />
			<px:PXDSCallbackCommand Name="CustomerStatement" Visible="False" />
			<px:PXDSCallbackCommand Name="SalesPrice" Visible="False" />
			<px:PXDSCallbackCommand Name="ViewServiceOrderHistory" Visible="False" />
			<px:PXDSCallbackCommand Name="ViewAppointmentHistory" Visible="False" />
			<px:PXDSCallbackCommand Name="ViewEquipmentSummary" Visible="False" />
			<px:PXDSCallbackCommand Name="ViewContractScheduleSummary" Visible="False" />
			<px:PXDSCallbackCommand Name="ScheduleAppointment" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="OpenMultipleStaffMemberBoard" Visible="False" />
			<px:PXDSCallbackCommand Name="OpenSingleStaffMemberBoard" Visible="False" />
			<px:PXDSCallbackCommand Name="ViewEquipmentSummary" Visible="False" />
			<px:PXDSCallbackCommand Name="ViewContractScheduleSummary" Visible="False" />
			<px:PXDSCallbackCommand Name="ValidateAddresses" StartNewGroup="True" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="RegenerateLastStatement" StartNewGroup="True" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="NewTask" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="NewEvent" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="NewActivity" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="NewMailActivity" DependOnGrid="gridActivities" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ViewActivity" DependOnGrid="gridActivities" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="OpenActivityOwner" Visible="False" CommitChanges="True" DependOnGrid="gridActivities" />

			<%-- Address --%>
			<px:PXDSCallbackCommand Name="ViewMainOnMap" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewBillAddressOnMap" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewDefLocationAddressOnMap" Visible="false" />
			<px:PXDSCallbackCommand Name="AddressLookupSelectAction" CommitChanges="true" Visible="false" />
			<px:PXDSCallbackCommand Name="AddressLookup" SelectControlsIDs="BAccount" RepaintControls="None" RepaintControlsIDs="ds,DefAddress" CommitChanges="true" Visible="false" />
			<px:PXDSCallbackCommand Name="BillingAddressLookup" SelectControlsIDs="BAccount" RepaintControls="None" RepaintControlsIDs="ds,BillAddress" CommitChanges="true" Visible="false" />
			<px:PXDSCallbackCommand Name="DefLocationAddressLookup"  SelectControlsIDs="BAccount" RepaintControls="None" RepaintControlsIDs="ds,DefLocationAddress" CommitChanges="true" Visible="false" />

			<%-- Contacts grid --%>
			<px:PXDSCallbackCommand Name="CreateContact" Visible="False"/>
			<px:PXDSCallbackCommand Name="MakeContactPrimary" DependOnGrid="grdContacts" Visible="False" RepaintControlsIDs="grdContacts" />

			<%-- Locations grid --%>
			<px:PXDSCallbackCommand Name="NewLocation" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="SetDefaultLocation" DependOnGrid="grdLocations" Visible="False" />

            <px:PXDSCallbackCommand Name="ComplianceDocument$PurchaseOrder$Link" Visible="false" DependOnGrid="grid" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ComplianceDocument$Subcontract$Link" CommitChanges="True" Visible="false" DependOnGrid="grid" />
			<px:PXDSCallbackCommand Name="ComplianceDocument$InvoiceID$Link" Visible="false" DependOnGrid="grdComplianceDocuments" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ComplianceDocument$BillID$Link" Visible="false" DependOnGrid="grdComplianceDocuments" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ComplianceDocument$ApCheckID$Link" Visible="false" DependOnGrid="grdComplianceDocuments" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ComplianceDocument$ArPaymentID$Link" Visible="false" DependOnGrid="grdComplianceDocuments" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ComplianceDocument$ProjectTransactionID$Link" Visible="false" DependOnGrid="grdComplianceDocuments" CommitChanges="True" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXSmartPanel ID="smpCPMInstance" runat="server" Key="PMInstanceEditor" InnerPageUrl="~/Pages/AR/AR303010.aspx?PopupPanel=On" CaptionVisible="true" Caption="Card Definition" RenderIFrame="true" Visible="False" DesignView="Content">
	</px:PXSmartPanel>
	<px:PXSmartPanel ID="pnlChangeID" runat="server" Caption="Specify New ID"
		CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" Key="ChangeIDDialog" CreateOnDemand="false" AutoCallBack-Enabled="true"
		AutoCallBack-Target="formChangeID" AutoCallBack-Command="Refresh" CallBackMode-CommitChanges="True" CallBackMode-PostData="Page"
		AcceptButtonID="btnOK">
		<px:PXFormView ID="formChangeID" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" CaptionVisible="False"
			DataMember="ChangeIDDialog">
			<ContentStyle BackColor="Transparent" BorderStyle="None" />
			<Template>
				<px:PXLayoutRule ID="rlAcctCD" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
				<px:PXSegmentMask ID="edAcctCD" runat="server" DataField="CD" />
			</Template>
		</px:PXFormView>
		<px:PXPanel ID="pnlChangeIDButton" runat="server" SkinID="Buttons">
			<px:PXButton ID="btnOK" runat="server" DialogResult="OK" Text="OK">
				<AutoCallBack Target="formChangeID" Command="Save" />
			</px:PXButton>
			<px:PXButton ID="btnCancel" runat="server" DialogResult="Cancel" Text="Cancel" />						
		</px:PXPanel>
	</px:PXSmartPanel>
    <px:PXSmartPanel ID="pnlOnDemandStatement" runat="server" Caption="Generate On-Demand Statement" 
        CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" Key="OnDemandStatementDialog" CreateOnDemand="false" 
        AutoCallBack-Enabled="true" AutoCallBack-Target="formOnDemandStatement" AutoCallBack-Command="Refresh"
        CallBackMode-CommitChanges="true" CallBackMode-PostData="Page" AcceptButtonID="btnOK">
        <px:PXFormView ID="formOnDemandStatement" runat="server" DataSourceID="ds" Style="z-index: 100" 
            Width="100%" CaptionVisible="False" DataMember="OnDemandStatementDialog">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXLayoutRule ID="lrStatementDate" runat="server" StartColumn="true" LabelsWidth="S" ControlSize="XM" />
                <px:PXDateTimeEdit ID="dteStatementDate" runat="server" DataField="StatementDate" />
            </Template>
        </px:PXFormView>
        <px:PXPanel ID="pnlOnDemandStatementButton" runat="server" SkinID="Buttons">
            <px:PXButton ID="btnOK1" runat="server" DialogResult="OK" Text="OK">
                <AutoCallBack Target="formOnDemandStatement" Command="Save" />
            </px:PXButton>
        </px:PXPanel>
    </px:PXSmartPanel>
	<px:PXFormView ID="BAccount" runat="server" Width="100%" Caption="Customer Summary" DataMember="BAccount" NoteIndicator="True" FilesIndicator="True" ActivityIndicator="false" ActivityField="NoteActivity" LinkIndicator="true" BPEventsIndicator="true" DefaultControlID="edAcctCD">
		<Template>

			<%-- column 1 --%>

			<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" LabelsWidth="SM" />
				<px:PXSegmentMask ID="edAcctCD" runat="server" DataField="AcctCD" FilterByAllFields="True" />
				<px:PXDropDown ID="edStatus" runat="server" DataField="Status" CommitChanges="True" />
				<px:PXSelector CommitChanges="True" ID="edCustomerClassID" runat="server" DataField="CustomerClassID" AllowEdit="True" />

			<%-- column 2 --%>

			<px:PXLayoutRule runat="server" StartColumn="True" />
				<px:PXFormView ID="CustomerBalance" runat="server" DataMember="CustomerBalance" RenderStyle="Simple">
					<Template>
						<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" />
						<px:PXLayoutRule ID="edMerge01" runat="server" Merge="true" />
	                        <px:PXTextEdit ID="edBalanceLabel" runat="server" 
	                                       Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
	                                       DataField="Balance_Label" 
	                                       SuppressLabel="true" 
	                                       Width="154px"  
	                                       Enabled="false"
	                                       IsClientControl="false" />
	                        <px:PXNumberEdit ID="edBBalance" runat="server" DataField="Balance" SuppressLabel="true" />
                        <px:PXLayoutRule ID="edMerge02" runat="server" Merge="false" />
						<px:PXLayoutRule ID="edMerge21" runat="server" Merge="true" />
	                        <px:PXTextEdit ID="edConsolidatedBalanceLabel" runat="server" 
	                                       Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
	                                       DataField="ConsolidatedBalance_Label" 
	                                       SuppressLabel="true" 
	                                       Width="154px"  
	                                       Enabled="false"
	                                       IsClientControl="false" />
	                        <px:PXNumberEdit ID="edConsolidatedBalance" runat="server" DataField="ConsolidatedBalance" SuppressLabel="true" />
                        <px:PXLayoutRule ID="edMerge22" runat="server" Merge="false" />
						<px:PXLayoutRule ID="edMerge31" runat="server" Merge="true" />
                            <px:PXTextEdit ID="edSignedDepositsBalanceLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="SignedDepositsBalance_Label" 
                                           SuppressLabel="true" 
                                           Width="154px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                            <px:PXNumberEdit ID="edSignedDepositsBalance" runat="server" DataField="SignedDepositsBalance" SuppressLabel="true" />
                        <px:PXLayoutRule ID="edMerge32" runat="server" Merge="false" />
						<px:PXLayoutRule ID="edMerge41" runat="server" Merge="true" />
	                        <px:PXTextEdit ID="edRetainageBalanceLabel" runat="server" 
	                                       Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
	                                       DataField="RetainageBalance_Label" 
	                                       SuppressLabel="true" 
	                                       Width="154px"  
	                                       Enabled="false"
	                                       IsClientControl="false" />
	                        <px:PXNumberEdit ID="edRetainageBalance" runat="server" DataField="RetainageBalance" SuppressLabel="true" />
                        <px:PXLayoutRule ID="edMerge42" runat="server" Merge="false" />						
						
					</Template>
				</px:PXFormView>

			<px:PXLayoutRule runat="server" />
				<px:PXCheckBox ID="chkServiceManagement" runat="server" DataField="ChkServiceManagement" Visible="False"/>

		</Template>
	</px:PXFormView>
</asp:Content>

<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Height="580px" Style="z-index: 100" Width="100%" DataMember="CurrentCustomer" DataSourceID="ds">
		<AutoSize Enabled="True" Container="Window" MinWidth="300" MinHeight="250" />
		<Activity HighlightColor="" SelectedColor="" Width="" Height="" />
		<Items>

			<px:PXTabItem Text="General" RepaintOnDemand="false">
				<Template>

					<%-- column 1 --%>

					<px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="XM" />
					<px:PXLayoutRule runat="server" GroupCaption="Account Info" />
						<px:PXTextEdit ID="edAcctName" runat="server" DataField="AcctName" CommitChanges="True" TabIndex="100" />

					<px:PXLayoutRule runat="server" GroupCaption="Account Address" StartGroup="True" />
						<px:PXFormView ID="DefAddress" runat="server" DataMember="DefAddress" RenderStyle="Simple" TabIndex="200">
							<Template>
								<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" />
									<px:PXButton ID="btnAddressLookup" runat="server" CommandName="AddressLookup" CommandSourceID="ds" Size="xs" TabIndex="-1" />
									<px:PXButton ID="btnViewMainOnMap" runat="server" CommandName="ViewMainOnMap" CommandSourceID="ds" Size="xs" Text="View on Map" TabIndex="-1" />
									<px:PXTextEdit ID="edDefAddressAddressLine1" runat="server" DataField="AddressLine1" CommitChanges="True" />
									<px:PXTextEdit ID="edDefAddressAddressLine2" runat="server" DataField="AddressLine2" CommitChanges="True" />
									<px:PXTextEdit ID="edDefAddressCity" runat="server" DataField="City" CommitChanges="True" />
									<px:PXSelector ID="edDefAddressState" runat="server" DataField="State" CommitChanges="True" AutoRefresh="True" AllowAddNew="True" />
									<px:PXMaskEdit ID="edDefAddressPostalCode" runat="server" DataField="PostalCode" CommitChanges="True" />
									<px:PXSelector ID="edDefAddressCountryID" runat="server" DataField="CountryID" CommitChanges="True" AllowAddNew="True" />
									<px:PXCheckBox ID="edDefAddressIsValidated" runat="server" DataField="IsValidated" Enabled="False" />
							</Template>
						</px:PXFormView>

					<px:PXLayoutRule runat="server" GroupCaption="Additional Account Info" StartGroup="True" />
						<px:PXFormView ID="DefContact1" runat="server" DataMember="DefContact" DataSourceID="ds" RenderStyle="Simple" TabIndex="300">
							<Template>
								<px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="XM" />
								<px:PXLayoutRule runat="server" Merge="True"/>
									<px:PXDropDown ID="edDefContactPhone1Type" runat="server" DataField="Phone1Type" Size="S" SuppressLabel="True" TabIndex="-1" />
									<px:PXLabel ID="edDefContactlblPhone1" runat="server" Text=" " SuppressLabel="true" />
									<px:PXMaskEdit ID="edDefContactPXMaskEdit1" runat="server" DataField="Phone1" SuppressLabel="True" LabelWidth="34px" />
								<px:PXLayoutRule runat="server" Merge="True" />	
									<px:PXDropDown ID="edDefContactPhone2Type" runat="server" DataField="Phone2Type" Size="S" SuppressLabel="True" TabIndex="-1" />
									<px:PXLabel ID="edDefContactlblPhone2" runat="server" Text=" " SuppressLabel="true" />
									<px:PXMaskEdit ID="edDefContactPXMaskEdit2" runat="server" DataField="Phone2" SuppressLabel="True" LabelWidth="34px" />
								<px:PXLayoutRule runat="server" Merge="True" />
									<px:PXDropDown ID="edDefContactFaxType" runat="server" DataField="FaxType" Size="S" SuppressLabel="True" TabIndex="-1" />
									<px:PXLabel ID="edDefContactlblFax" runat="server" Text=" " SuppressLabel="true" />
									<px:PXMaskEdit ID="edDefContactPXMaskEdit3" runat="server" DataField="Fax" SuppressLabel="True" LabelWidth="34px" />
								<px:PXLayoutRule runat="server" Merge="True" />
									<px:PXLabel ID="lblDefContactAccountEmail" runat="server" Text="Account Email:" />
									<px:PXMailEdit ID="edDefContactEMail" runat="server" DataField="EMail" CommandName="NewMailActivity" CommandSourceID="ds" CommitChanges="True" SuppressLabel="True"/>
								<px:PXLayoutRule runat="server" />
								<px:PXLinkEdit ID="edDefContactWebSite" runat="server" DataField="WebSite" CommitChanges="True" />
							</Template>
						</px:PXFormView>

						<px:PXTextEdit ID="edExtRefNbr" runat="server" DataField="AcctReferenceNbr" CommitChanges="True" />
						<px:PXSelector ID="edLocaleName" runat="server" DataField="LocaleName" CommitChanges="True" />
					
						<px:PXFormView ID="DefContact2" runat="server" DataMember="DefContact" DataSourceID="ds" RenderStyle="Simple" TabIndex="400">
							<Template>
								<px:PXLayoutRule runat="server" GroupCaption="Account Personal Data Privacy" StartGroup="True"  LabelsWidth="SM" ControlSize="XM" />
									<px:PXCheckBox ID="edDefContactConsentAgreement" runat="server" DataField="ConsentAgreement" AlignLeft="True" CommitChanges="True" TabIndex="-1"/>
									<px:PXDateTimeEdit ID="edDefContactConsentDate" runat="server" DataField="ConsentDate" CommitChanges="True"/>
									<px:PXDateTimeEdit ID="edDefContactConsentExpirationDate" runat="server" DataField="ConsentExpirationDate" CommitChanges="True"/>

							</Template>
						</px:PXFormView>

					<%-- column 2 --%>

					<px:PXLayoutRule runat="server" StartColumn="True" />
						<!--#include file="~\Pages\CR\Includes\Containers\CRPrimaryContactGraphExt.inc"-->

				</Template>
			</px:PXTabItem>
			
			<px:PXTabItem Text="Financial">
				<Template>

					<%-- column 1 --%>
					
					<px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="XM" />
					<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Financial Settings" />
						<px:PXSelector ID="edTermsID" runat="server" DataField="TermsID" AllowEdit="True" />
						<px:PXSelector ID="edStatementCycleId" runat="server" DataField="StatementCycleId" AllowEdit="True" CommitChanges="true" />
						<px:PXBranchSelector CommitChanges="True" ID="edCOrgBAccountID" runat="server" DataField="COrgBAccountID" InitialExpandLevel="0" />
						<px:PXCheckBox SuppressLabel="True" ID="chkAutoApplyPayments" runat="server" DataField="AutoApplyPayments" TabIndex="-1" />
						<px:PXCheckBox CommitChanges="True" ID="chkFinChargeApply" runat="server" DataField="FinChargeApply" TabIndex="-1" />
						<px:PXCheckBox CommitChanges="True" ID="chkSmallBalanceAllow" runat="server" DataField="SmallBalanceAllow" TabIndex="-1" />
						<px:PXLayoutRule ID="edMerge31" runat="server" Merge="true" />
	                        <px:PXTextEdit ID="edSmallBalanceLimitLabel" runat="server" 
	                                       Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
	                                       DataField="SmallBalanceLimit_Label" 
	                                       SuppressLabel="true" 
	                                       Width="134px"  
	                                       Enabled="false"
	                                       IsClientControl="false" />
	                        <px:PXNumberEdit ID="edSmallBalanceLimit" runat="server" DataField="SmallBalanceLimit" SuppressLabel="true" />
	                    <px:PXLayoutRule ID="edMerge32" runat="server" Merge="false" />
					<px:PXLayoutRule runat="server" Merge="True" />
						<px:PXSelector ID="edCuryID" runat="server" DataField="CuryID" Size="S" />
						<px:PXCheckBox ID="chkAllowOverrideCury" runat="server" DataField="AllowOverrideCury" TabIndex="-1" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" Merge="True" />
						<px:PXSelector ID="edCuryRateTypeID" runat="server" DataField="CuryRateTypeID" Size="S" />
						<px:PXCheckBox ID="chkAllowOverrideRate" runat="server" DataField="AllowOverrideRate" TabIndex="-1" />
					<px:PXLayoutRule runat="server" />
						<px:PXCheckBox ID="chkPaymentsByLinesAllowed" runat="server" DataField="PaymentsByLinesAllowed" TabIndex="-1" />

					<%-- column 2 --%>

					<px:PXLayoutRule runat="server" StartColumn="True" />
					<px:PXLayoutRule runat="server" GroupCaption="Retainage Settings" StartGroup="True" />
					<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" />
						<px:PXCheckBox runat="server" ID="edRetainageApply" DataField="RetainageApply" CommitChanges="True" TabIndex="-1" />
						<px:PXNumberEdit ID="edRetainagePct" runat="server" DataField="RetainagePct" CommitChanges="True" />

					<px:PXLayoutRule runat="server" GroupCaption="Credit Verification Rules" StartGroup="True" />
					<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" />
						<px:PXDropDown CommitChanges="True" ID="edCreditRule" runat="server" DataField="CreditRule" />
						<px:PXLayoutRule ID="edMerge41" runat="server" Merge="true" />
                            <px:PXTextEdit ID="edCreditLimitLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="CreditLimit_Label" 
                                           SuppressLabel="true" 
                                           Width="184px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                            <px:PXNumberEdit ID="edCreditLimit" runat="server" DataField="CreditLimit" SuppressLabel="true" CommitChanges="true"/>
                        <px:PXLayoutRule ID="edMerge42" runat="server" Merge="false" />
					<px:PXNumberEdit ID="edCreditDaysPastDue" runat="server" DataField="CreditDaysPastDue" />
						<px:PXFormView ID="CustomerBalance" runat="server" DataMember="CustomerBalance" RenderStyle="Simple">
							<Template>
								<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="XM" />
								<px:PXLayoutRule ID="edMerge01" runat="server" Merge="true" />
                                    <px:PXTextEdit ID="edUnreleasedBalanceLabel" runat="server" 
                                                   Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                                   DataField="UnreleasedBalance_Label" 
                                                   SuppressLabel="true" 
                                                   Width="184px"  
                                                   Enabled="false"
                                                   IsClientControl="false" />
                                    <px:PXNumberEdit ID="edUnreleasedBalance" runat="server" DataField="UnreleasedBalance" SuppressLabel="true" />
                                <px:PXLayoutRule ID="edMerge02" runat="server" Merge="false" />
								<px:PXLayoutRule ID="edMerge11" runat="server" Merge="true" />
                                    <px:PXTextEdit ID="edOpenOrdersBalanceLabel" runat="server" 
                                                   Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                                   DataField="OpenOrdersBalance_Label" 
                                                   SuppressLabel="true" 
                                                   Width="184px"  
                                                   Enabled="false"
                                                   IsClientControl="false" />
                                    <px:PXNumberEdit ID="edOpenOrdersBalance" runat="server" DataField="OpenOrdersBalance" SuppressLabel="true" />
                                <px:PXLayoutRule ID="edMerge12" runat="server" Merge="false" />
								<px:PXLayoutRule ID="edMerge21" runat="server" Merge="true" />
                                    <px:PXTextEdit ID="edRemainingCreditLimitLabel" runat="server" 
                                                   Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                                   DataField="RemainingCreditLimit_Label" 
                                                   SuppressLabel="true" 
                                                   Width="184px"  
                                                   Enabled="false"
                                                   IsClientControl="false" />
                                    <px:PXNumberEdit ID="edRemainingCreditLimit" runat="server" DataField="RemainingCreditLimit" SuppressLabel="true" />
                                <px:PXLayoutRule ID="edMerge22" runat="server" Merge="false" />
                               <px:PXDateTimeEdit ID="edOldInvoiceDate" runat="server" DataField="OldInvoiceDate" Enabled="False" />
							</Template>
						</px:PXFormView>

				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="Billing">
				<Template>

					<%-- column 1 --%>

					<px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="XM" />
					<px:PXLayoutRule runat="server" GroupCaption="Bill-To Address" StartGroup="True" />
						<px:PXCheckBox ID="chkOverrideBillAddress" runat="server" DataField="OverrideBillAddress" CommitChanges="True" SuppressLabel="True" TabIndex="-1" />
						<px:PXFormView ID="BillAddress" runat="server" DataMember="BillAddress" RenderStyle="Simple" SyncPosition="true" DataSourceID="ds">
							<Template>
								<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
									<px:PXButton ID="btnBillingAddressLookup" runat="server" CommandName="BillingAddressLookup" CommandSourceID="ds" Size="xs" TabIndex="-1" />
									<px:PXButton ID="btnViewBillAddressOnMap" runat="server" CommandName="ViewBillAddressOnMap" CommandSourceID="ds" Size="xs" Text="View on Map" TabIndex="-1" />
									<px:PXTextEdit ID="edAddressLine1" runat="server" DataField="AddressLine1" CommitChanges="True" />
									<px:PXTextEdit ID="edAddressLine2" runat="server" DataField="AddressLine2" CommitChanges="True" />
									<px:PXTextEdit ID="edCity" runat="server" DataField="City" CommitChanges="True" />
									<px:PXSelector ID="edState" runat="server" AllowAddNew="True" AutoRefresh="True" DataField="State" CommitChanges="True" />
									<px:PXMaskEdit ID="edPostalCode" runat="server" CommitChanges="True" DataField="PostalCode" />
									<px:PXSelector ID="edCountryID" runat="server" AutoRefresh="True" CommitChanges="True" DataField="CountryID" />
									<px:PXCheckBox ID="edIsValidated" runat="server" DataField="IsValidated" Enabled="False" />
							</Template>
						</px:PXFormView>

					<px:PXLayoutRule runat="server" GroupCaption="Bill-To Info" StartGroup="True" />
						<px:PXCheckBox ID="chkOverrideBillContact" runat="server" DataField="OverrideBillContact" CommitChanges="True" SuppressLabel="True" TabIndex="-1" />
						<px:PXFormView ID="BillContact" runat="server" DataMember="BillContact" RenderStyle="Simple" DataSourceID="ds">
							<Template>
								<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
									<px:PXTextEdit ID="edFullName" runat="server" DataField="FullName" />
									<px:PXTextEdit ID="edAttention" runat="server" DataField="Attention" />
									<px:PXLayoutRule runat="server" Merge="True"/>
										<px:PXDropDown ID="Phone1Type" runat="server" DataField="Phone1Type" Size="S" SuppressLabel="True" TabIndex="-1" />
										<px:PXLabel ID="lblPhone1" runat="server" Text=" " SuppressLabel="true" />
										<px:PXMaskEdit ID="PXMaskEdit1" runat="server" DataField="Phone1" SuppressLabel="True" LabelWidth="34px" />
									<px:PXLayoutRule runat="server" Merge="True" />
										<px:PXDropDown ID="Phone2Type" runat="server" DataField="Phone2Type" Size="S" SuppressLabel="True" TabIndex="-1" />
										<px:PXLabel ID="lblPhone2" runat="server" Text=" " SuppressLabel="true" />
										<px:PXMaskEdit ID="PXMaskEdit2" runat="server" DataField="Phone2" SuppressLabel="True" LabelWidth="34px" />
									<px:PXLayoutRule runat="server" Merge="True" />
										<px:PXDropDown ID="FaxType" runat="server" DataField="FaxType" Size="S" SuppressLabel="True" TabIndex="-1" />
										<px:PXLabel ID="lblFax" runat="server" Text=" " SuppressLabel="true" />
										<px:PXMaskEdit ID="PXMaskEdit3" runat="server" DataField="Fax" SuppressLabel="True" LabelWidth="34px" />
									<px:PXLayoutRule runat="server" />
									<px:PXMailEdit ID="edEMail" runat="server" DataField="EMail" CommitChanges="True" />
									<px:PXLinkEdit ID="edWebSite" runat="server" DataField="WebSite" CommitChanges="True" />
							</Template>
						</px:PXFormView>

					<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Service Management" />
						<px:PXCheckBox runat="server" ID="edRequireCustomerSignature" DataField="RequireCustomerSignature" TabIndex="-1" />
					<px:PXLayoutRule runat="server" />
						<px:PXSelector runat="server" ID="edBillingCycleID1" DataField="BillingCycleID" CommitChanges="True" AllowEdit="True" />
						<px:PXDropDown runat="server" ID="edSendInvoicesTo" DataField="SendInvoicesTo" />
						<px:PXDropDown runat="server" ID="edBillShipmentSource" DataField="BillShipmentSource" />
	                    <px:PXDropDown runat="server" ID="edDefaultBillingCustomerSource" DataField="DefaultBillingCustomerSource" CommitChanges="True" />
	                    <px:PXSegmentMask runat="server" ID="edBillCustomerID" DataField="BillCustomerID" CommitChanges="True" AutoRefresh="True" />
	                    <px:PXSegmentMask runat="server" ID="edBillLocationID" DataField="BillLocationID" AutoRefresh="True" />

					<%-- column 2 --%>

					<px:PXLayoutRule runat="server" StartColumn="True" />
					<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" />
					<px:PXLayoutRule runat="server" GroupCaption="Parent Info" StartGroup="True" />
						<px:PXSegmentMask ID="edParentBAccountID" runat="server" DataField="ParentBAccountID" AllowEdit="True" CommitChanges="true" AutoRefresh="true" />
						<px:PXCheckBox ID="edConsolidateToParent" runat="server" DataField="ConsolidateToParent" CommitChanges="true" TabIndex="-1" />
						<px:PXCheckBox ID="edConsolidateStatements" runat="server" DataField="ConsolidateStatements" CommitChanges="true" TabIndex="-1" />
						<px:PXCheckBox ID="edSharedCreditPolicy" runat="server" DataField="SharedCreditPolicy" CommitChanges="True" TabIndex="-1" />

					<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Print and Email Settings" />
					<px:PXLayoutRule runat="server" ControlSize="M" LabelsWidth="M" Merge="True" SuppressLabel="true" />
						<px:PXCheckBox ID="chkMailInvoices" runat="server" DataField="MailInvoices" Size="M" TabIndex="-1" />
						<px:PXCheckBox CommitChanges="True" ID="chkPrintInvoices" runat="server" DataField="PrintInvoices" Size="M" TabIndex="-1" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" ControlSize="M" LabelsWidth="M" Merge="True" SuppressLabel="true" />
						<px:PXCheckBox ID="chkMailDunningLetters" runat="server" DataField="MailDunningLetters" Size="M" CommitChanges="true" TabIndex="-1" />
						<px:PXCheckBox ID="chkPrintDunningLetters" runat="server" DataField="PrintDunningLetters" Size="M" CommitChanges="true" TabIndex="-1" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" ControlSize="M" LabelsWidth="M" Merge="True" SuppressLabel="true" />
						<px:PXCheckBox ID="chkSendStatementByEmails" runat="server" DataField="SendStatementByEmail" Size="M" CommitChanges="true" TabIndex="-1" />
						<px:PXCheckBox ID="chkPrintStatements" runat="server" DataField="PrintStatements" Size="M" CommitChanges="true" TabIndex="-1" />
					<px:PXLayoutRule runat="server" />
						<px:PXDropDown CommitChanges="True" ID="edStatementType" runat="server" DataField="StatementType" />
						<px:PXCheckBox ID="chkPrintCuryStatements" runat="server" DataField="PrintCuryStatements" CommitChanges="true" TabIndex="-1" />

					<px:PXLayoutRule runat="server" GroupCaption="Default Payment Method" StartGroup="True" />
					<px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="XM" />
						<px:PXSelector ID="edDefPaymentMethodID" runat="server" DataField="DefPaymentMethodID" />
						<px:PXFormView ID="DefPaymentMethodInstance" runat="server" DataMember="DefPaymentMethodInstance" RenderStyle="Simple">
							<Template>
								<px:PXLayoutRule runat="server" StartGroup="True" ControlSize="XM" LabelsWidth="SM" />
									<px:PXSelector ID="edProcessingCenter" runat="server" AutoRefresh="True" DataField="CCProcessingCenterID" CommitChanges="True" />
									<px:PXSelector ID="edCustomerCCPID" runat="server" AutoRefresh="True" DataField="CustomerCCPID" CommitChanges="True" />
									<px:PXSegmentMask CommitChanges="True" ID="edCashAccountID" runat="server" DataField="CashAccountID" AutoRefresh ="true"/>
									<px:PXTextEdit ID="edDescr" runat="server" DataField="Descr" />
									<px:PXGrid ID="grdPMInstanceDetails" runat="server" MatrixMode="True" Caption="Payment Method Details" SkinID="Attributes" Height="160px" Width="400px" DataSourceID="ds">
										<Levels>
											<px:PXGridLevel DataMember="DefPaymentMethodInstanceDetails" DataKeyNames="PMInstanceID,PaymentMethodID,DetailID">
												<Columns>
													<px:PXGridColumn DataField="DetailID_PaymentMethodDetail_descr" />
													<px:PXGridColumn DataField="Value" />
												</Columns>
												<Layout FormViewHeight="" />
											</px:PXGridLevel>
										</Levels>
									</px:PXGrid>
							</Template>
						</px:PXFormView>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="Shipping" LoadOnDemand="True">
				<Template>

					<px:PXFormView ID="DefLocation" runat="server" DataMember="DefLocation" SkinID="Transparent" Width="100%">
						<Template>

							<%-- column 1 --%>

							<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" />
							<px:PXLayoutRule runat="server" GroupCaption="Ship-to Address" StartGroup="True" />

							<!--#include file="~\Pages\CR\Includes\Containers\DefLocationAddress.inc"-->

							<px:PXLayoutRule runat="server" GroupCaption="Ship-to Info" StartGroup="True" />

							<!--#include file="~\Pages\CR\Includes\Containers\DefLocationContact.inc"-->

							<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Other Settings" />
								<px:PXSelector ID="edCBranchID" runat="server" DataField="CBranchID" AllowEdit="True" AutoRefresh="true"/>
								<px:PXSelector ID="edCPriceClassID" runat="server" DataField="CPriceClassID" AllowEdit="True" />
								<px:PXSelector ID="edCDefProjectID" runat="server" DataField="CDefProjectID" AllowEdit="True" />

							<%-- column 2 --%>
							
							<px:PXLayoutRule runat="server" StartColumn="True" />
							<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" />
							<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Tax Settings" />
								<px:PXTextEdit ID="edTaxRegistrationID" runat="server" DataField="TaxRegistrationID" />
								<px:PXSelector ID="edCTaxZoneID" runat="server" DataField="CTaxZoneID" AllowEdit="True" />
								<px:PXDropDown ID="edCTaxCalcMode" runat="server" DataField="CTaxCalcMode" />
								<px:PXTextEdit ID="edCAvalaraExemptionNumber" runat="server" DataField="CAvalaraExemptionNumber" />
								<px:PXDropDown ID="edCAvalaraCustomerUsageType" runat="server" DataField="CAvalaraCustomerUsageType" />

							<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" />
							<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Shipping Instructions" />
								<px:PXSegmentMask ID="edCSiteID" runat="server" DataField="CSiteID" AllowEdit="True" CommitChanges="True" AutoRefresh="True" />
								<px:PXSelector CommitChanges="True" ID="edCarrierID" runat="server" DataField="CCarrierID" AllowEdit="True" />
								<px:PXSelector ID="edShipTermsID" runat="server" DataField="CShipTermsID" AllowEdit="True" />
								<px:PXSelector ID="edShipZoneID" runat="server" DataField="CShipZoneID" AllowEdit="True" />
								<px:PXSelector ID="edFOBPointID" runat="server" DataField="CFOBPointID" AllowEdit="True" />
								<px:PXCheckBox ID="chkResedential" runat="server" DataField="CResedential" TabIndex="-1" />
								<px:PXCheckBox ID="chkSaturdayDelivery" runat="server" DataField="CSaturdayDelivery" TabIndex="-1" />
								<px:PXCheckBox ID="chkInsurance" runat="server" DataField="CInsurance" TabIndex="-1" />
								<px:PXDropDown ID="edCShipComplete" runat="server" DataField="CShipComplete" />
								<px:PXNumberEdit ID="edCOrderPriority" runat="server" DataField="COrderPriority" />
								<px:PXNumberEdit ID="edLeadTime" runat="server" DataField="CLeadTime" />
								<px:PXSelector ID="edCCalendarID" runat="server" DataField="CCalendarID" />

								<px:PXGrid ID="PXGridAccounts" runat="server" DataSourceID="ds" AllowFilter="False" Height="80px" Width="400px" SkinID="ShortList" FastFilterFields="CustomerID,CustomerID_description,CarrierAccount"
									CaptionVisible="True" Caption="Carrier Accounts">
									<Levels>
										<px:PXGridLevel DataMember="Carriers">
											<Columns>
												<px:PXGridColumn DataField="IsActive" Label="Active" TextAlign="Center" Type="CheckBox" />
												<px:PXGridColumn AutoCallBack="True" DataField="CarrierPluginID" Label="Carrier" />
												<px:PXGridColumn DataField="CarrierAccount" />
												<px:PXGridColumn DataField="PostalCode">
												</px:PXGridColumn>
											</Columns>
											<Layout FormViewHeight="" />
										</px:PXGridLevel>
									</Levels>
								</px:PXGrid>
						</Template>
					</px:PXFormView>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="Locations" LoadOnDemand="True">
				<Template>
					<px:PXGrid ID="grdLocations" runat="server" DataSourceID="ds" Height="100%" Width="100%" SkinID="Inquire" AllowSearch="True"
							 SyncPosition="true" AllowFilter="True" FastFilterFields="LocationCD,Descr,Address__City,Address__State,Address__CountryID" >
						<Levels>
							<px:PXGridLevel DataMember="Locations">
								<Columns>

									<%-- common fields --%>
									<px:PXGridColumn DataField="IsActive" TextAlign="Center" Type="CheckBox" />
									<px:PXGridColumn DataField="LocationCD" LinkCommand="ViewLocation" />
									<px:PXGridColumn DataField="Descr" />
									<px:PXGridColumn DataField="IsDefault" TextAlign="Center" Type="CheckBox" />
									<px:PXGridColumn DataField="Address__City" />
									<px:PXGridColumn DataField="Address__State" RenderEditorText="True" />
									<px:PXGridColumn DataField="Address__CountryID" RenderEditorText="True" AutoCallBack="True" />

									<%-- hidden by default --%>
									<px:PXGridColumn DataField="Address__PostalCode" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="Address__State_description" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="Address__CountryID_description" Visible="false" SyncVisible="false" />



									<%-- custom fields --%>
									<px:PXGridColumn DataField="CPriceClassID" />

									<%-- hidden by default --%>
									<px:PXGridColumn DataField="CreatedByID_Description" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CreatedDateTime" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="LastModifiedByID_Description" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="LastModifiedDateTime" Visible="false" SyncVisible="false" />

									<%-- other settings --%>
									<px:PXGridColumn DataField="CDefProjectID" Visible="false" SyncVisible="false" />

									<%-- tax settings --%>
									<px:PXGridColumn DataField="TaxRegistrationID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CTaxZoneID" />
									<px:PXGridColumn DataField="CTaxCalcMode" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CAvalaraExemptionNumber" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CAvalaraCustomerUsageType" Visible="false" SyncVisible="false" />

									<%-- shipping instructions --%>
									<px:PXGridColumn DataField="CSiteID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CCarrierID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CShipTermsID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CShipZoneID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CFOBPointID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CResedential" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CSaturdayDelivery" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CInsurance" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CShipComplete" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="COrderPriority" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CLeadTime" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CCalendarID" Visible="false" SyncVisible="false" />

									<%-- GL accounts --%>
									<px:PXGridColumn DataField="CSalesAcctID" AutoCallBack="True" />
									<px:PXGridColumn DataField="CSalesSubID" />
									<px:PXGridColumn DataField="CARAccountID" />
									<px:PXGridColumn DataField="CARSubID" />
									<px:PXGridColumn DataField="CDiscountAcctID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CDiscountSubID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CFreightAcctID" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="CFreightSubID" Visible="false" SyncVisible="false" />

									<px:PXGridColumn DataField="CBranchID" />
									<px:PXGridColumn DataField="CBranchID_description" Visible="false" SyncVisible="false" />

								</Columns>
								<Layout FormViewHeight="" />
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" MinHeight="100" MinWidth="100" />
						<ActionBar>
							<Actions>
								<FilterBar ToolBarVisible="Top" Order="0" GroupIndex="3" />
							</Actions>
							<CustomItems>
								<px:PXToolBarButton ImageKey="AddNew" Tooltip="Add New Location" DisplayStyle="Image">
									<AutoCallBack Command="NewLocation" Target="ds" />
									<PopupCommand Command="RefreshLocation" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Key="cmdViewLocation" Text="Location Details">
									<AutoCallBack Command="ViewLocation" Target="ds" />
									<PopupCommand Command="RefreshLocation" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Text="Set as Default">
									<AutoCallBack Command="SetDefaultLocation" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="Payment Methods">
				<Template>
					<px:PXGrid ID="grdPaymentMethods" runat="server" Height="300px" Width="100%" SkinID="DetailsInTab" DataSourceID="ds">
						<Levels>
							<px:PXGridLevel DataMember="PaymentMethods">
								<Columns>
									<px:PXGridColumn DataField="IsDefault" TextAlign="Center" Type="CheckBox" />
									<px:PXGridColumn DataField="PaymentMethodID" />
									<px:PXGridColumn DataField="Descr" />
									<px:PXGridColumn DataField="CashAccountID" />
									<px:PXGridColumn DataField="IsActive" TextAlign="Center" Type="CheckBox" />
									<px:PXGridColumn DataField="IsCustomerPaymentMethod" TextAlign="Center" Type="CheckBox" />
								</Columns>
								<Layout FormViewHeight="" />
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" MinHeight="150" />
						<ActionBar PagerVisible="False" DefaultAction="cmdViewPaymentMethod" PagerActionsText="True">
							<Actions>
								<Save Enabled="False" />
								<AddNew Enabled="False" />
								<Delete Enabled="False" />
								<EditRecord Enabled="False" />
							</Actions>
							<CustomItems>
								<px:PXToolBarButton Text="Add Payment Method">
									<AutoCallBack Command="AddPaymentMethod" Target="ds" />
									<PopupCommand Command="Cancel" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Key="cmdViewPaymentMethod" Text="View Payment Method">
									<AutoCallBack Command="ViewPaymentMethod" Target="ds" />
									<PopupCommand Command="Cancel" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>

			<!--#include file="~\Pages\CR\Includes\ContactDetails.inc"-->

			<px:PXTabItem Text="Salespersons" LoadOnDemand="True">
				<Template>
					<px:PXGrid ID="PXGrid1" runat="server" Height="300px" Width="100%" SkinID="DetailsInTab" DataSourceID="ds">
						<Levels>
							<px:PXGridLevel DataMember="SalesPersons" DataKeyNames="SalesPersonID,LocationID">
								<Columns>
									<px:PXGridColumn DataField="SalesPersonID" />
									<px:PXGridColumn DataField="SalesPersonID_SalesPerson_descr" />
									<px:PXGridColumn DataField="LocationID" />
									<px:PXGridColumn DataField="LocationID_description" />
									<px:PXGridColumn DataField="CommisionPct" TextAlign="Right" />
									<px:PXGridColumn DataField="IsDefault" Type="CheckBox" TextAlign="Center" />
								</Columns>
								<RowTemplate>
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
									<px:PXSegmentMask ID="edSalesPersonID" runat="server" DataField="SalesPersonID" AutoRefresh="True" AllowEdit="True" />
									<px:PXSegmentMask ID="edLocationID" runat="server" DataField="LocationID" AutoRefresh="True" AllowEdit="True" />
									<px:PXTextEdit ID="edLocation_descr" runat="server" DataField="LocationID_description" Enabled="False" />
									<px:PXNumberEdit ID="edCommisionPct" runat="server" DataField="CommisionPct" />
								</RowTemplate>
								<Mode InitNewRow="False" />
								<Layout FormViewHeight="" />
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" MinHeight="100" MinWidth="100" />
						<ActionBar>
							<Actions>
								<Save Enabled="False" />
							</Actions>
						</ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="Child Accounts" RepaintOnDemand="False" BindingContext="BAccount">
				<Template>
					<px:PXGrid ID="gridChildAccounts" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Width="100%" Height="300px" AllowPaging="True" AdjustPageSize="Auto" >
						<Levels>
							<px:PXGridLevel DataMember="ChildAccounts">
								<Columns>
									<px:PXGridColumn DataField="CustomerID" />
									<px:PXGridColumn DataField="CustomerName" />
									<px:PXGridColumn DataField="Balance" TextAlign="Right" />
									<px:PXGridColumn DataField="SignedDepositsBalance" TextAlign="Right" />
									<px:PXGridColumn DataField="UnreleasedBalance" TextAlign="Right" />
									<px:PXGridColumn DataField="OpenOrdersBalance" TextAlign="Right" />
									<px:PXGridColumn DataField="OldInvoiceDate" />
									<px:PXGridColumn DataField="ConsolidateToParent" Type="CheckBox" TextAlign="Center" />
									<px:PXGridColumn DataField="ConsolidateStatements" Type="CheckBox" TextAlign="Center" />
									<px:PXGridColumn DataField="SharedCreditPolicy" Type="CheckBox" TextAlign="Center" />
									<px:PXGridColumn DataField="StatementCycleId" TextAlign="Right" />
								</Columns>
								<RowTemplate>
									<px:PXLayoutRule runat="server" StartColumn="True" />
									<px:PXSegmentMask ID="edChildCustomerID" runat="server" DataField="CustomerID" AllowEdit="True" />
								</RowTemplate>
								<Mode InitNewRow="False" />
								<Layout FormViewHeight="" />
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" MinHeight="100" MinWidth="100" />
						<ActionBar>
							<Actions>
								<AddNew ToolBarVisible="False" />
								<Delete ToolBarVisible="False" />
							</Actions>
						</ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="Attributes">
				<Template>
					<px:PXGrid ID="PXGridAnswers" runat="server" DataSourceID="ds" SkinID="Inquire" Width="100%"
						Height="200px" MatrixMode="True">
						<Levels>
							<px:PXGridLevel DataMember="Answers">
								<Columns>
									<px:PXGridColumn DataField="AttributeID" TextAlign="Left" AllowShowHide="False"
										TextField="AttributeID_description" />
									<px:PXGridColumn DataField="isRequired" TextAlign="Center" Type="CheckBox" />
									<px:PXGridColumn DataField="Value" AllowShowHide="False" AllowSort="False" />
								</Columns>
								<Layout FormViewHeight="" />
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" MinHeight="200" />
						<ActionBar>
							<Actions>
								<Search Enabled="False" />
							</Actions>
						</ActionBar>
						<Mode AllowAddNew="False" AllowColMoving="False" AllowDelete="False" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="Activities" LoadOnDemand="True">
				<Template>
					<pxa:PXGridWithPreview ID="gridActivities" runat="server" DataSourceID="ds" Width="100%"
						AllowSearch="True" DataMember="Activities" AllowPaging="true" NoteField="NoteText"
						FilesField="NoteFiles" BorderWidth="0px" GridSkinID="Inquire" SplitterStyle="z-index: 100; border-top: solid 1px Gray;  border-bottom: solid 1px Gray"
						PreviewPanelStyle="z-index: 100; background-color: Window" PreviewPanelSkinID="Preview"
						BlankFilterHeader="All Activities" MatrixMode="true" PrimaryViewControlID="form">
						<ActionBar DefaultAction="cmdViewActivity" CustomItemsGroup="0" PagerVisible="False">
							<CustomItems>
								<px:PXToolBarButton Key="cmdAddTask">
									<AutoCallBack Command="NewTask" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Key="cmdAddEvent">
									<AutoCallBack Command="NewEvent" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Key="cmdAddEmail">
									<AutoCallBack Command="NewMailActivity" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Key="cmdAddActivity">
									<AutoCallBack Command="NewActivity" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="Activities">
								<Columns>
									<px:PXGridColumn DataField="IsCompleteIcon" AllowShowHide="False" AllowResize="False"
										ForceExport="True" />
									<px:PXGridColumn DataField="PriorityIcon" AllowShowHide="False" AllowResize="False"
										ForceExport="True" />
									<px:PXGridColumn DataField="CRReminder__ReminderIcon" AllowShowHide="False" AllowResize="False"
										ForceExport="True" />
									<px:PXGridColumn DataField="ClassIcon" AllowShowHide="False" AllowResize="False"
										ForceExport="True" />
									<px:PXGridColumn DataField="ClassInfo" />
									<px:PXGridColumn DataField="RefNoteID" Visible="false" AllowShowHide="False" />
									<px:PXGridColumn DataField="Subject" LinkCommand="ViewActivity" />
									<px:PXGridColumn DataField="UIStatus" />
                                    <px:PXGridColumn DataField="Released" TextAlign="Center" Type="CheckBox" />
									<px:PXGridColumn DataField="StartDate" DisplayFormat="g" />
									<px:PXGridColumn DataField="CreatedDateTime" DisplayFormat="g" Visible="False" />
									<px:PXGridColumn DataField="TimeSpent" />
									<px:PXGridColumn DataField="CreatedByID" Visible="false" AllowShowHide="False" />
									<px:PXGridColumn DataField="CreatedByID_Creator_Username" Visible="false"
										SyncVisible="False" SyncVisibility="False" Width="108px">
										<NavigateParams>
											<px:PXControlParam Name="PKID" ControlID="gridActivities" PropertyName="DataValues[&quot;CreatedByID&quot;]" />
										</NavigateParams>
									</px:PXGridColumn>
									<px:PXGridColumn DataField="WorkgroupID" />
									<px:PXGridColumn DataField="OwnerID" LinkCommand="OpenActivityOwner" DisplayMode="Text"/>
                                    <px:PXGridColumn DataField="ProjectID" AllowShowHide="true" Visible="false" SyncVisible="false" />
									<px:PXGridColumn DataField="ProjectTaskID" AllowShowHide="true" Visible="false" SyncVisible="false" />
                                    <px:PXGridColumn DataField="Source" AllowResize="True" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<PreviewPanelTemplate>
							<px:PXHtmlView ID="edBody" runat="server" DataField="body" TextMode="MultiLine"
								MaxLength="50" Width="100%" Height="100px" SkinID="Label">
								<AutoSize Container="Parent" Enabled="true" />
							</px:PXHtmlView>
						</PreviewPanelTemplate>
						<AutoSize Enabled="true" />
						<GridMode AllowAddNew="False" AllowDelete="False" AllowFormEdit="False" AllowUpdate="False" AllowUpload="False" />
					</pxa:PXGridWithPreview>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="GL Accounts">
				<Template>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
					<px:PXFormView ID="DefLocationAccount" runat="server" DataMember="DefLocation" RenderStyle="Simple" TabIndex="2500">
						<Template>
							<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
							<px:PXSegmentMask CommitChanges="True" ID="edCARAccountID" runat="server" DataField="CARAccountID" />
							<px:PXSegmentMask CommitChanges="True" ID="edCARSubID" runat="server" DataField="CARSubID" AutoRefresh="True" AllowEdit="True" />
							<px:PXSegmentMask CommitChanges="True" ID="edCSalesAcctID" runat="server" DataField="CSalesAcctID" />
							<px:PXSegmentMask CommitChanges="True" ID="edCSalesSubID" runat="server" DataField="CSalesSubID" AutoRefresh="True" AllowEdit="True" />
							<px:PXSegmentMask CommitChanges="True" ID="edCDiscountAcctID" runat="server" DataField="CDiscountAcctID" />
							<px:PXSegmentMask CommitChanges="True" ID="edCDiscountSubID" runat="server" DataField="CDiscountSubID" AutoRefresh="True" AllowEdit="True" />
							<px:PXSegmentMask CommitChanges="True" ID="edCFreightAcctID" runat="server" DataField="CFreightAcctID" />
							<px:PXSegmentMask CommitChanges="True" ID="edCFreightSubID" runat="server" DataField="CFreightSubID" AutoRefresh="True" AllowEdit="True" />
						</Template>
					</px:PXFormView>
					<px:PXSegmentMask CommitChanges="True" ID="edDiscTakenAcctID" runat="server" DataField="DiscTakenAcctID" />
					<px:PXSegmentMask CommitChanges="True" ID="edDiscTakenSubID" runat="server" DataField="DiscTakenSubID" AutoRefresh="True" AllowEdit="True" />
					<px:PXSegmentMask CommitChanges="True" ID="edPrepaymentAcctID" runat="server" DataField="PrepaymentAcctID" />
					<px:PXSegmentMask CommitChanges="True" ID="edPrepaymentSubID" runat="server" DataField="PrepaymentSubID" AutoRefresh="True" AllowEdit="True" />
					<px:PXSegmentMask CommitChanges="True" ID="edCOGSAcctID" runat="server" DataField="COGSAcctID" />
					<px:PXSegmentMask CommitChanges="True" ID="edCOGSSubID" runat="server" DataField="COGSSubID" AutoRefresh="True" AllowEdit="True" />
					<px:PXFormView ID="formRetainage" runat="server" DataMember="DefLocation" RenderStyle="Simple" >
						<Template>
							<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" StartColumn="True" />
							<px:PXSegmentMask ID="edCRetainageAcctID" runat="server" CommitChanges="True" DataField="CRetainageAcctID" />
							<px:PXSegmentMask ID="edCRetainageSubID" runat="server" DataField="CRetainageSubID" CommitChanges="True" />
						</Template>
					</px:PXFormView>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem Text="Mailing Settings" Overflow="Hidden" LoadOnDemand="True">
				<Template>
					<px:PXSplitContainer runat="server" ID="sp1" SplitterPosition="300" SkinID="Horizontal" Height="500px">
						<AutoSize Enabled="true" />
						<Template1>
							<px:PXGrid ID="gridNS" runat="server" SkinID="DetailsInTab" Width="100%" Height="150px" Caption="Mailings"
								AdjustPageSize="Auto" AllowPaging="True" DataSourceID="ds">
								<AutoSize Enabled="True" />
								<AutoCallBack Target="gridNR" Command="Refresh" />
								<Levels>
									<px:PXGridLevel DataMember="NotificationSources" DataKeyNames="SourceID,SetupID">
										<RowTemplate>
											<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
											<px:PXDropDown ID="edFormat" runat="server" DataField="Format" />
											<px:PXSegmentMask ID="edNBranchID" runat="server" DataField="NBranchID" />
											<px:PXCheckBox ID="chkActive" runat="server" Checked="True" DataField="Active" />
											<px:PXSelector ID="edSetupID" runat="server" DataField="SetupID" />
											<px:PXSelector ID="edReportID" runat="server" DataField="ReportID" ValueField="ScreenID" />
											<px:PXSelector ID="edNotificationID" runat="server" DataField="NotificationID" ValueField="Name" />
											<px:PXSelector ID="edEMailAccountID" runat="server" DataField="EMailAccountID" DisplayMode="Text" />
										</RowTemplate>
										<Columns>
											<px:PXGridColumn DataField="Active" TextAlign="Center" Type="CheckBox" />
											<px:PXGridColumn DataField="OverrideSource" TextAlign="Center" Type="CheckBox" AutoCallBack="True" />
											<px:PXGridColumn DataField="SetupID" AutoCallBack="True" />
											<px:PXGridColumn DataField="NBranchID" AutoCallBack="True" Label="Branch" />
											<px:PXGridColumn DataField="EMailAccountID" DisplayMode="Text" />
											<px:PXGridColumn DataField="ReportID" AutoCallBack="True" />
											<px:PXGridColumn DataField="NotificationID" AutoCallBack="True" />
											<px:PXGridColumn DataField="Format" RenderEditorText="True" AutoCallBack="True" />
											<px:PXGridColumn DataField="RecipientsBehavior" CommitChanges="True" />
										</Columns>
										<Layout FormViewHeight="" />
									</px:PXGridLevel>
								</Levels>
							</px:PXGrid>
						</Template1>
						<Template2>
							<px:PXGrid ID="gridNR" runat="server" SkinID="DetailsInTab" Width="100%" Caption="Recipients" AdjustPageSize="Auto"
								AllowPaging="True" DataSourceID="ds">
								<AutoSize Enabled="True" />
								<Mode InitNewRow="True"></Mode>
								<Parameters>
									<px:PXSyncGridParam ControlID="gridNS" />
								</Parameters>
								<CallbackCommands>
									<Save RepaintControls="None" RepaintControlsIDs="ds" />
									<FetchRow RepaintControls="None" />
								</CallbackCommands>
								<Levels>
									<px:PXGridLevel DataMember="NotificationRecipients" DataKeyNames="NotificationID">
										<Mode InitNewRow="True"></Mode>
										<Columns>
											<px:PXGridColumn DataField="Active" TextAlign="Center" Type="CheckBox" />
											<px:PXGridColumn DataField="ContactType" RenderEditorText="True" AutoCallBack="True" />
											<px:PXGridColumn DataField="OriginalContactID" Visible="False" AllowShowHide="False" />
											<px:PXGridColumn DataField="ContactID">
												<NavigateParams>
													<px:PXControlParam Name="ContactID" ControlID="gridNR" PropertyName="DataValues[&quot;OriginalContactID&quot;]" />
												</NavigateParams>
											</px:PXGridColumn>
											<px:PXGridColumn DataField="Email" />
											<px:PXGridColumn DataField="Format" RenderEditorText="True" AutoCallBack="True" />
											<px:PXGridColumn DataField="AddTo" />
										</Columns>
										<RowTemplate>
											<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
											<px:PXDropDown ID="edContactType" runat="server" DataField="ContactType" />
											<px:PXSelector ID="edContactID" runat="server" DataField="ContactID" AutoRefresh="True" ValueField="DisplayName"
												AllowEdit="True" />
										</RowTemplate>
										<Layout FormViewHeight="" />
									</px:PXGridLevel>
								</Levels>
							</px:PXGrid>
						</Template2>
					</px:PXSplitContainer>
				</Template>
			</px:PXTabItem>

			<px:PXTabItem LoadOnDemand="True" Text="Service Billing" RepaintOnDemand="False" VisibleExp="DataControls[&quot;chkServiceManagement&quot;].Value == 1" BindingContext="BAccount">
				<Template>
					<px:PXGrid runat="server" ID="gridBillingCycles" AutoGenerateColumns="None" SkinID="DetailsInTab" DataSourceID="ds" SyncPosition="True" RepaintColumns="True" KeepPosition="True" Style='height:300px;width:100%;'>
						<Levels>
							<px:PXGridLevel DataMember="CustomerBillingCycles">
								<Columns>
									<px:PXGridColumn DataField="SrvOrdType" CommitChanges="True" />
									<px:PXGridColumn DataField="BillingCycleID" CommitChanges="True" />
									<px:PXGridColumn DataField="SendInvoicesTo" CommitChanges="True" />
									<px:PXGridColumn DataField="BillShipmentSource" CommitChanges="True" />
									<px:PXGridColumn DataField="FrequencyType" CommitChanges="True" />
									<px:PXGridColumn DataField="WeeklyFrequency" />
									<px:PXGridColumn DataField="MonthlyFrequency" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" MinHeight="100" MinWidth="100" />
						<Mode InitNewRow="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>

            <px:PXTabItem Text="Compliance">
                <Template>
                    <px:PXGrid runat="server" ID="grdComplianceDocuments" Height="300px" DataSourceID="ds" Width="100%" SkinID="DetailsInTab" AutoGenerateColumns="Append" KeepPosition="True" SyncPosition="True" AllowPaging="True" PageSize="12">
                        <Levels>
                            <px:PXGridLevel DataMember="ComplianceDocuments">
                                <Columns>
                                    <px:PXGridColumn DataField="ExpirationDate" CommitChanges="True" TextAlign="Left" />
                                    <px:PXGridColumn DataField="DocumentType" CommitChanges="True" />
                                    <px:PXGridColumn DataField="CreationDate" TextAlign="Left" />
                                    <px:PXGridColumn DataField="Status" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Required" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="Received" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="ReceivedDate" TextAlign="Left" />
                                    <px:PXGridColumn DataField="IsProcessed" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="IsVoided" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="IsCreatedAutomatically" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="SentDate" TextAlign="Left" />
                                    <px:PXGridColumn DataField="ProjectID" LinkCommand="ComplianceViewProject" />
                                    <px:PXGridColumn DataField="CostTaskID" LinkCommand="ComplianceViewCostTask" CommitChanges="True" />
                                    <px:PXGridColumn DataField="RevenueTaskID" LinkCommand="ComplianceViewRevenueTask" CommitChanges="True" />
                                    <px:PXGridColumn DataField="CostCodeID" LinkCommand="ComplianceViewCostCode" CommitChanges="True" />
                                    <px:PXGridColumn DataField="VendorID" LinkCommand="ComplianceViewVendor" CommitChanges="True" />
                                    <px:PXGridColumn DataField="VendorName" TextAlign="Left" />
                                    <px:PXGridColumn DataField="BillID" LinkCommand="ComplianceDocument$BillID$Link" CommitChanges="True" DisplayMode="Text" />
                                    <px:PXGridColumn DataField="BillAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CustomerID" CommitChanges="True" LinkCommand="ComplianceViewCustomer" />
                                    <px:PXGridColumn DataField="ApCheckID" LinkCommand="ComplianceDocument$ApCheckID$Link" DisplayMode="Text" CommitChanges="True" TextAlign="Left" />
                                    <px:PXGridColumn DataField="CheckNumber" TextAlign="Left" />
                                    <px:PXGridColumn DataField="ArPaymentID" LinkCommand="ComplianceDocument$ArPaymentID$Link" DisplayMode="Text" TextAlign="Left" />
                                    <px:PXGridColumn DataField="CertificateNumber" TextAlign="Left" />
                                    <px:PXGridColumn DataField="CreatedByID" />
                                    <px:PXGridColumn DataField="CustomerName" TextAlign="Left" />
                                    <px:PXGridColumn DataField="AccountID" TextAlign="Left" CommitChanges="True" />
                                    <px:PXGridColumn DataField="DateIssued" TextAlign="Left" />
                                    <px:PXGridColumn DataField="EffectiveDate" TextAlign="Left" />
                                    <px:PXGridColumn DataField="InsuranceCompany" TextAlign="Left" />
                                    <px:PXGridColumn DataField="InvoiceAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="InvoiceID" LinkCommand="ComplianceDocument$InvoiceID$Link" CommitChanges="True" DisplayMode="Text" />
                                    <px:PXGridColumn DataField="IsExpired" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="IsRequiredJointCheck" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="JointAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="JointRelease" TextAlign="Left" />
                                    <px:PXGridColumn DataField="JointReleaseReceived" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="JointVendorInternalId" LinkCommand="ComplianceViewJointVendor" TextAlign="Left" />
                                    <px:PXGridColumn DataField="JointVendorExternalName" TextAlign="Left" />
                                    <px:PXGridColumn DataField="LastModifiedByID" />
                                    <px:PXGridColumn DataField="LienWaiverAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="Limit" TextAlign="Right" />
                                    <px:PXGridColumn DataField="MethodSent" TextAlign="Left" />
                                    <px:PXGridColumn DataField="PaymentDate" TextAlign="Left" />
                                    <px:PXGridColumn DataField="ArPaymentMethodID" />
                                    <px:PXGridColumn DataField="ApPaymentMethodID" />
                                    <px:PXGridColumn DataField="Policy" TextAlign="Left" />
                                    <px:PXGridColumn DataField="ProjectTransactionID" LinkCommand="ComplianceDocument$ProjectTransactionID$Link" CommitChanges="True" DisplayMode="Text" TextAlign="Left" />
                                    <px:PXGridColumn DataField="PurchaseOrder" TextAlign="Left" DisplayMode="Text" CommitChanges="True" LinkCommand="ComplianceDocument$PurchaseOrder$Link" />
                                    <px:PXGridColumn DataField="PurchaseOrderLineItem" TextAlign="Left" />
                                    <px:PXGridColumn DataField="Subcontract" DisplayMode="Text" CommitChanges="True" LinkCommand="ComplianceDocument$Subcontract$Link" />
                                    <px:PXGridColumn DataField="SubcontractLineItem" TextAlign="Left" />
                                    <px:PXGridColumn DataField="ChangeOrderNumber" DisplayMode="Text" LinkCommand="ComplianceDocument$ChangeOrderNumber$Link" CommitChanges="True" />
                                    <px:PXGridColumn DataField="ReceiptDate" TextAlign="Left" />
                                    <px:PXGridColumn DataField="ReceiveDate" TextAlign="Left" />
                                    <px:PXGridColumn DataField="ReceivedBy" TextAlign="Left" />
                                    <px:PXGridColumn DataField="SecondaryVendorID" LinkCommand="ComplianceViewSecondaryVendor" CommitChanges="True" />
                                    <px:PXGridColumn DataField="SecondaryVendorName" TextAlign="Left" />
                                    <px:PXGridColumn DataField="SourceType" TextAlign="Left" />
                                    <px:PXGridColumn DataField="SponsorOrganization" TextAlign="Left" />
                                    <px:PXGridColumn DataField="ThroughDate" TextAlign="Left" />
                                    <px:PXGridColumn DataField="DocumentTypeValue" CommitChanges="True" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXSegmentMask runat="server" DataField="CostCodeID" AutoRefresh="True" ID="edCostCode2" />
                                    <px:PXSelector runat="server" DataField="DocumentTypeValue" AutoRefresh="True" ID="edDocumentTypeValue" />
                                    <px:PXSelector runat="server" DataField="BillID" FilterByAllFields="True" AutoRefresh="True" ID="edBillID" />
                                    <px:PXSelector runat="server" DataField="InvoiceID" FilterByAllFields="True" AutoRefresh="True" ID="edInvoiceID" />
                                    <px:PXSelector runat="server" DataField="ApCheckID" FilterByAllFields="True" AutoRefresh="True" ID="edApCheckID" />
                                    <px:PXSelector runat="server" DataField="ArPaymentID" FilterByAllFields="True" AutoRefresh="True" ID="edArPaymentID" />
                                    <px:PXSelector runat="server" DataField="ProjectTransactionID" FilterByAllFields="True" AutoRefresh="True" ID="edProjectTransactionID" />
                                    <px:PXSelector runat="server" DataField="PurchaseOrder" FilterByAllFields="True" AutoRefresh="True" ID="edPurchaseOrder" CommitChanges="True" />
                                    <px:PXSelector runat="server" DataField="PurchaseOrderLineItem" AutoRefresh="True" ID="edPurchaseOrderLineItem" />
                                    <px:PXSelector runat="server" DataField="Subcontract" FilterByAllFields="True" AutoRefresh="True" CommitChanges="True" ID="edSubcontract" />
                                    <px:PXSelector runat="server" DataField="SubcontractLineItem" AutoRefresh="True" ID="edSubcontractLineItem" />
                                    <px:PXSelector runat="server" DataField="ChangeOrderNumber" AutoRefresh="True" ID="edChangeOrderNumber" />
                                    <px:PXSelector runat="server" DataField="CostTaskID" FilterByAllFields="True" AutoRefresh="True" ID="edCostTaskID" />
                                    <px:PXSelector runat="server" DataField="RevenueTaskID" FilterByAllFields="True" AutoRefresh="True" ID="edRevenueTaskID" />
                                </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                        <ActionBar>
                            <CustomItems />
                        </ActionBar>
                        <CallbackCommands>
                            <InitRow CommitChanges="True" />
                        </CallbackCommands>
                        <Mode InitNewRow="True" />
                        <AutoSize Enabled="True" MinHeight="150" />
                    </px:PXGrid>
                </Template>
                <AutoCallBack>
                    <Behavior CommitChanges="True" />
                </AutoCallBack>
            </px:PXTabItem>

		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="300" MinWidth="300" />
	</px:PXTab>

	<!--#include file="~\Pages\CR\Includes\CRCreateContactPanel.inc"-->

	<!--#include file="~\Pages\Includes\AddressLookupPanel.inc"-->

</asp:Content>
