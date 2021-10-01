<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CA507000.aspx.cs" Inherits="Page_CA507000" Title="Import Settlement Batches" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Filter" TypeName="PX.Objects.CA.CCBatchEnq" />
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter">
        <ContentLayout Orientation="Horizontal" />
        <Template>
            <px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
            <px:PXSelector CommitChanges="True" ID="edProcessingCenterID" runat="server" DataField="ProcessingCenterID" />
            <px:PXLayoutRule ID="PXLayoutRule2" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
            <px:PXDateTimeEdit CommitChanges="True" ID="edLastSettlementDate" runat="server" DataField="LastSettlementDate" Enabled ="False"/>
            <px:PXLayoutRule ID="PXLayoutRule3" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
            <px:PXDateTimeEdit CommitChanges="True" ID="edImportThroughDate" runat="server" DataField="ImportThroughDate"/>
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Width="100%" ActionsPosition="Top" Caption="Transaction List" AllowPaging="true" AdjustPageSize="Auto" SkinID="PrimaryInquire" SyncPosition="true">
        <Levels>
            <px:PXGridLevel DataMember="Batches">
                <RowTemplate>
                    <px:PXSelector ID="edDepositNbr" runat="server" DataField="DepositNbr" AllowEdit="true" Enabled="false" />
                </RowTemplate>
                <Columns>
                    <px:PXGridColumn DataField="BatchID" LinkCommand="viewDocument" />
                    <px:PXGridColumn DataField="Status" />
                    <px:PXGridColumn DataField="SettlementTime" />
                    <px:PXGridColumn DataField="SettlementState" />
                    <px:PXGridColumn DataField="ExtBatchID" />
                    <px:PXGridColumn DataField="SettledAmount" />
                    <px:PXGridColumn DataField="RefundAmount" />
                    <px:PXGridColumn DataField="TransactionCount" />
                    <px:PXGridColumn DataField="ImportedTransactionCount" />
                    <px:PXGridColumn DataField="UnprocessedCount" />
                    <px:PXGridColumn DataField="MissingCount" />
                    <px:PXGridColumn DataField="DepositNbr" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
    </px:PXGrid>
</asp:Content>
