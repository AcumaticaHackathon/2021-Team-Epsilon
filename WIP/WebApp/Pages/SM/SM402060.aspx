<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="SM402060.aspx.cs" Inherits="Page_SM402060" Title="Notifications" %>
<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
    <px:PXDataSource ID="ds" Runat="server" TypeName="PX.BusinessProcess.UI.NotificationInquiry" PrimaryView="Notifications" Visible="True" Width="100%">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="createNotification" Visible="True" CommitChanges="True" />
            <px:PXDSCallbackCommand Name="viewNotification" Visible="False" DependOnGrid="grid" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:content ID="cont2" ContentPlaceHolderID="phL" Runat="Server">
	<px:PXGrid ID="grid" Runat="server" SkinID="Details" Height="350" Width="100%" AdjustPageSize="Auto" AutoAdjustColumns="True"
        AllowPaging="True" AllowSearch="True" SyncPosition="True">
        <Levels>
            <px:PXGridLevel DataMember="Notifications">
                <Columns>
                    <px:PXGridColumn DataField="Type" Width="200" />
					<px:PXGridColumn DataField="Name" Width="450" LinkCommand="ViewNotification" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <ActionBar>
            <Actions>
                <AddNew ToolBarVisible="False" MenuVisible="False" />
                <Delete ToolBarVisible="False" MenuVisible="False" />
                <ExportExcel ToolBarVisible="False" MenuVisible="False" />
            </Actions>
        </ActionBar>
        <Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
        <AutoSize Container="Window" Enabled="True" MinHeight="250" />
    </px:PXGrid>
</asp:Content>
