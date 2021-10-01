<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CA403000.aspx.cs" Inherits="Page_CA403000" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" Visible="True" PrimaryView="Documents" TypeName="PX.Objects.CA.CAPendingReviewEnq" />
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100;" Width="100%" Caption="Documents" SkinID="PrimaryInquire"
        FastFilterFields="RefNbr,CustomerID" SyncPosition="true">
        <Levels>
            <px:PXGridLevel DataMember="Documents">
                <Columns>
                    <px:PXGridColumn DataField="DocType" />
                    <px:PXGridColumn DataField="RefNbr" LinkCommand="redirectToDoc"/>
                    <px:PXGridColumn DataField="PaymentMethodID" LinkCommand="redirectToPaymentMethod" />
                    <px:PXGridColumn DataField="PMInstanceDescr" />
                    <px:PXGridColumn DataField="ProcessingCenterID" LinkCommand="redirectToProcCenter"/>
                    <px:PXGridColumn DataField="CustomerID" LinkCommand="redirectToCustomer"/>
                    <px:PXGridColumn DataField="CuryOrigDocAmt" />
                    <px:PXGridColumn DataField="DocDate" />
                    <px:PXGridColumn DataField="CCPaymentStateDescr" />
                    <px:PXGridColumn DataField="ValidationStatus" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
        <Mode InitNewRow="True" />
    </px:PXGrid>
</asp:Content>
