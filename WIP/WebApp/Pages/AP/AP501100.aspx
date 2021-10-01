<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
    ValidateRequest="false" CodeFile="AP501100.aspx.cs" Inherits="Page_AP501100"
    Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Filter"
        TypeName="PX.Objects.AP.InvoiceRecognition.RecognizedRecordProcess" PageLoadBehavior="PopulateSavedValues">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="viewDocument" Visible="false" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" Width="100%" Caption="Filter" DataMember="Filter" DefaultControlID="edCreatedBefore">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="true" LabelsWidth="S" ControlSize="S" />
            <px:PXDateTimeEdit runat="server" CommitChanges="True" ID="edCreatedBefore" DataField="CreatedBefore" />

            <px:PXLayoutRule runat="server" StartColumn="true" />
            <px:PXCheckBox runat="server" CommitChanges="true" ID="edShowUnprocessedRecords" DataField="ShowUnprocessedRecords" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" Height="150px" Width="100%" Caption="Outdated Recognition Results" AllowPaging="true"
        SkinID="PrimaryInquire" AdjustPageSize="Auto" AutoAdjustColumns="true" SyncPosition="true" NoteIndicator="true">
        <Levels>
            <px:PXGridLevel DataMember="Records">
                <Columns>
                    <px:PXGridColumn DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="true" AllowMove="false" AllowSort="true" />
                    <px:PXGridColumn DataField="EntityType" Width="100px" />
                    <px:PXGridColumn DataField="Status" Width="120px" />
                    <px:PXGridColumn DataField="DocumentLink" LinkCommand="viewDocument" />
                    <px:PXGridColumn DataField="CreatedDateTime" />
                    <px:PXGridColumn DataField="MailFrom" />
                    <px:PXGridColumn DataField="Subject" />
                    <px:PXGridColumn DataField="Owner" Width="130px" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="true" MinHeight="400" />
    </px:PXGrid>
</asp:Content>
