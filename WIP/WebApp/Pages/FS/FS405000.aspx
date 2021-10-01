<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="FS405000.aspx.cs" Inherits="Page_FS405000" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" Width="100%" runat="server" TypeName="PX.Objects.FS.BillHistoryInq"
        PrimaryView="BillHistoryRecords" Visible="true">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="MenuActions" CommitChanges="True"/>
            <px:PXDSCallbackCommand Name="FSBillHistory$ParentDocLink$Link" Visible="False" CommitChanges="True" DependOnGrid="grid" />
            <px:PXDSCallbackCommand Name="FSBillHistory$ChildDocLink$Link" Visible="False" CommitChanges="True" DependOnGrid="grid" />
            <px:PXDSCallbackCommand Name="FSBillHistory$RelatedDocument$Link" Visible="false" CommitChanges="True" DependOnGrid="grid" />
            <px:PXDSCallbackCommand Name="openPostBatch" Visible="false" CommitChanges="True" DependOnGrid="grid" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
    <px:PXGrid ID="grid" runat="server" Width="100%" AllowPaging="True" AllowSearch="true" AdjustPageSize="Auto" DataSourceID="ds" 
        SkinID="PrimaryInquire" AllowFilter ="true" FastFilterFields="RelatedDocumentType, RelatedDocument, ChildEntityType, ChildDocLink" >
        <Levels>
            <px:PXGridLevel DataMember="BillHistoryRecords" DataKeyNames="RefNbr">
                <RowTemplate>
                    <px:PXSelector ID="edBatchID" runat="server" DataField="BatchID" AllowEdit="True"/>
                    <px:PXTextEdit ID="edRelatedDocumentType" runat="server" DataField="RelatedDocumentType" />
                    <px:PXTextEdit ID="edRelatedDocument" runat="server" DataField="RelatedDocument"/>
                    <px:PXTextEdit ID="edChildEntityType" runat="server" DataField="ChildEntityType" />
                    <px:PXTextEdit ID="edChildDocLink" runat="server" DataField="ChildDocLink" />
                    <px:PXDateTimeEdit ID="edChildDocDate" runat="server" DataField="ChildDocDate" />
                    <px:PXTextEdit ID="edChildDocStatus" runat="server" DataField="ChildDocStatus" />
                    <px:PXNumberEdit ID="edChildAmount" runat="server" DataField="ChildAmount" />
                    <px:PXSelector ID="edServiceContractPeriodID" runat="server" DataField="ServiceContractPeriodID" />
                </RowTemplate>
                <Columns>
                    <px:PXGridColumn DataField="BatchID" LinkCommand="openPostBatch"/>
                    <px:PXGridColumn DataField="RelatedDocumentType"/>
                    <px:PXGridColumn DataField="RelatedDocument" LinkCommand="FSBillHistory$RelatedDocument$Link"/>
                    <px:PXGridColumn DataField="ChildEntityType" />
                    <px:PXGridColumn DataField="ChildDocLink" RenderEditorText="True" LinkCommand="FSBillHistory$ChildDocLink$Link"/>
                    <px:PXGridColumn DataField="ChildDocDate" />
                    <px:PXGridColumn DataField="ChildDocStatus" />
                    <px:PXGridColumn DataField="ChildAmount" />
                    <px:PXGridColumn DataField="ServiceContractPeriodID" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="200" />
    </px:PXGrid>
</asp:Content>
