<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CM505000.aspx.cs" Inherits="Page_CM505000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Filter" TypeName="PX.Objects.CM.RevalueARAccounts" />
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" Style="z-index: 100" Width="100%" DataMember="Filter" Caption="Revaluation Summary" TemplateContainer="" MarkRequired="Dynamic">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
            <px:PXBranchSelector CommitChanges="True" ID="edOrgBAccountID" runat="server" DataField="OrgBAccountID" />
            <px:PXSelector CommitChanges="True" ID="edFinPeriodID" runat="server" DataField="FinPeriodID" AutoRefresh="true"/>
            <px:PXDateTimeEdit CommitChanges="True" ID="edCuryEffDate" runat="server" DataField="CuryEffDate" />
            <px:PXSelector CommitChanges="True" ID="edCuryID" runat="server" DataField="CuryID" AutoRefresh="true"/>
            <px:PXLayoutRule runat="server" ColumnSpan="2" />
            <px:PXTextEdit ID="edDescription" runat="server" DataField="Description" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="S" />
            <px:PXLayoutRule ID="edMerge01" runat="server" Merge="true" />
                   <px:PXTextEdit ID="edTotalRevaluedLabel" runat="server"
                                  Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                  DataField="TotalRevalued_Label"
                                  SuppressLabel="true"
                                  Width="134px" 
                                  Enabled="false"
                                  IsClientControl="false" />
                   <px:PXNumberEdit ID="edTotalRevalued" runat="server" DataField="TotalRevalued" Enabled="False" SuppressLabel="true"/>
             <px:PXLayoutRule ID="edMerge02" runat="server" Merge="false" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" Height="150px" Style="z-index: 100;" Width="100%" ActionsPosition="Top" Caption="Revaluation Details" 
        SkinID="PrimaryInquire" FastFilterFields="AccountID, AccountID_Account_Description, CustomerID, CustomerID_BAccountR_acctName">
        <Levels>
            <px:PXGridLevel DataMember="ARAccountList">
                <Columns>
                    <px:PXGridColumn DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" AllowMove="False" AllowSort="False" />
                    <px:PXGridColumn DataField="BranchID"/>
                    <px:PXGridColumn DataField="AccountID" />
                    <px:PXGridColumn DataField="AccountID_Account_Description" />
                    <px:PXGridColumn DataField="SubID" TextCase="Upper" />
                    <px:PXGridColumn DataField="CustomerID" />
                    <px:PXGridColumn DataField="CustomerID_BAccountR_acctName" />
                    <px:PXGridColumn DataField="CuryRateTypeID" />
                    <px:PXGridColumn DataField="CuryRate" TextAlign="Right" />
                    <px:PXGridColumn DataField="CuryFinYtdBalance" TextAlign="Right" />
                    <px:PXGridColumn DataField="FinYtdBalance" TextAlign="Right" />
                    <px:PXGridColumn DataField="FinPrevRevalued" TextAlign="Right" />
                    <px:PXGridColumn DataField="FinYtdRevalued" TextAlign="Right" />
                    <px:PXGridColumn DataField="FinPtdRevalued" TextAlign="Right" />
                    <px:PXGridColumn DataField="LastRevaluedFinPeriodID" TextAlign="Right" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
    </px:PXGrid>
</asp:Content>
