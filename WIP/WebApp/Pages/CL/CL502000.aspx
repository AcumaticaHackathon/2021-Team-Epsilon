<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CL502000.aspx.cs"
Inherits="Page_CL502000" Title="Print/Email Lien Waivers" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%"
                     TypeName="PX.Objects.CN.Compliance.CL.Graphs.PrintEmailLienWaiversProcess"
                     PrimaryView="Filter"
                     PageLoadBehavior="PopulateSavedValues">
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Filter" Caption="Selection"
                   DefaultControlID="edAction">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
            <px:PXDropDown CommitChanges="True" ID="edAction" runat="server" DataField="Action" />
            <px:PXSelector CommitChanges="True" ID="edProjectId" runat="server" DataField="ProjectId" />
            <px:PXSelector CommitChanges="True" ID="edVendorId" runat="server" DataField="VendorId" />
            <px:PXDropDown CommitChanges="True" ID="edLienWaiverType" runat="server" DataField="LienWaiverType" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
            <px:PXDateTimeEdit CommitChanges="True" ID="edStartDate" runat="server" AlreadyLocalized="False" DataField="StartDate" />
            <px:PXDateTimeEdit CommitChanges="True" ID="edEndDate" runat="server" AlreadyLocalized="False" DataField="EndDate" />
            <px:PXCheckBox CommitChanges="True" ID="chkShouldShowProcessed" runat="server" AlignLeft="True" AlreadyLocalized="False" DataField="ShouldShowProcessed" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
            <px:PXCheckBox CommitChanges="True" ID="chkPrintWithDeviceHub" runat="server" AlignLeft="True" AlreadyLocalized="False" DataField="PrintWithDeviceHub" />
            <px:PXCheckBox CommitChanges="True" ID="chkDefinePrinterManually" runat="server" AlignLeft="True" AlreadyLocalized="False" DataField="DefinePrinterManually" />
            <px:PXSelector CommitChanges="True" ID="edPrinterID" runat="server" DataField="PrinterID" />
            <px:PXTextEdit CommitChanges="true" ID="edNumberOfCopies" runat="server" DataField="NumberOfCopies" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100" Width="100%"
               SkinID="Details" SyncPosition="True">
        <Levels>
            <px:PXGridLevel DataMember="LienWaivers">
                <Columns>
                    <px:PXGridColumn DataField="Selected"  Type="CheckBox" AllowCheckAll="True" AllowSort="False" AllowMove="False" />
                    <px:PXGridColumn DataField="CreationDate" />
                    <px:PXGridColumn DataField="DocumentTypeValue" />
                    <px:PXGridColumn DataField="Status" />
                    <px:PXGridColumn DataField="Required"  Type="CheckBox"/>
                    <px:PXGridColumn DataField="Received"  Type="CheckBox"/>
                    <px:PXGridColumn DataField="IsReceivedFromJointVendor"  Type="CheckBox"/>
                    <px:PXGridColumn DataField="IsProcessed"  Type="CheckBox"/>
                    <px:PXGridColumn DataField="IsVoided"  Type="CheckBox"/>
                    <px:PXGridColumn DataField="IsCreatedAutomatically"  Type="CheckBox"/>
                    <px:PXGridColumn DataField="ProjectID" />
                    <px:PXGridColumn DataField="CustomerID" />
                    <px:PXGridColumn DataField="CustomerName" />
                    <px:PXGridColumn DataField="VendorID" />
                    <px:PXGridColumn DataField="VendorName" />
                    <px:PXGridColumn DataField="Subcontract" />
                    <px:PXGridColumn DataField="ComplianceDocumentAPDocumentReference__Type" />
                    <px:PXGridColumn DataField="ComplianceDocumentAPDocumentReference__ReferenceNumber" />
                    <px:PXGridColumn DataField="BillAmount" />
                    <px:PXGridColumn DataField="LienWaiverAmount" />
                    <px:PXGridColumn DataField="LienNoticeAmount" />
                    <px:PXGridColumn DataField="ComplianceDocumentAPPaymentReference__Type" />
                    <px:PXGridColumn DataField="ComplianceDocumentAPPaymentReference__ReferenceNumber" />
                    <px:PXGridColumn DataField="PaymentDate" />
                    <px:PXGridColumn DataField="ThroughDate" />
                    <px:PXGridColumn DataField="JointVendorInternalId" />
                    <px:PXGridColumn DataField="JointVendorExternalName" />
                    <px:PXGridColumn DataField="JointAmount" />
                    <px:PXGridColumn DataField="JointLienWaiverAmount" />
                    <px:PXGridColumn DataField="JointLienNoticeAmount" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
        <ActionBar>
            <Actions>
                <Delete ToolBarVisible="False" />
                <AddNew ToolBarVisible="False" />
            </Actions>
        </ActionBar>
    </px:PXGrid>
</asp:Content>
