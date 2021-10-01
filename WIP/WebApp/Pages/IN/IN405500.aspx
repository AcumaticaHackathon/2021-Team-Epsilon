<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="IN405500.aspx.cs"
    Inherits="Page_IN405500" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.IN.INDeadStockEnq" PrimaryView="Filter">
        <CallbackCommands>
		</CallbackCommands>
	</px:PXDataSource>
    
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" DataMember="Filter" Width="100%" Height="100px">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
			<px:PXSelector runat="server" ID="selSiteID" DataField="SiteID" CommitChanges="True" />
			<px:PXSelector runat="server" ID="selItemClassID" DataField="ItemClassID" CommitChanges="True" />
			<px:PXSelector runat="server" ID="selInventoryID" DataField="InventoryID" CommitChanges="True" />

			<px:PXLayoutRule runat="server" StartColumn="True" />
            <px:PXDropDown ID="ddSelectBy" runat="server" AllowNull="False" DataField="SelectBy" CommitChanges="true" />
			<px:PXNumberEdit runat="server" ID="edInStockDays" DataField="InStockDays" CommitChanges="True" AllowNull="true" />
			<px:PXDateTimeEdit CommitChanges="True" ID="dtInStockSince" runat="server" DataField="InStockSince" />
			<px:PXNumberEdit runat="server" ID="edNoSalesDays" DataField="NoSalesDays" CommitChanges="True" AllowNull="true" />
			<px:PXDateTimeEdit CommitChanges="True" ID="dtNoSalesSince" runat="server" DataField="NoSalesSince" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXGrid ID="grid" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" Height="150px" SkinID="PrimaryInquire" CaptionVisible="false"
        SyncPosition="True" AdjustPageSize="Auto">
        <Levels>
            <px:PXGridLevel DataMember="Result">
                <RowTemplate>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                    <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" />
                </RowTemplate>
                <Columns>
					<px:PXGridColumn DataField="SiteID" />
                    <px:PXGridColumn DataField="InventoryID" />
                    <px:PXGridColumn DataField="SubItemID" />
					<px:PXGridColumn DataField="InventoryItem__Descr" />
					<px:PXGridColumn DataField="InStockQty" />
					<px:PXGridColumn DataField="DeadStockQty" />
					<px:PXGridColumn DataField="InventoryItem__BaseUnit" />
					<px:PXGridColumn DataField="InDeadStockDays" />
                    <px:PXGridColumn DataField="LastSaleDate" />
                    <px:PXGridColumn DataField="LastCost" />
					<px:PXGridColumn DataField="TotalDeadStockCost" />
					<px:PXGridColumn DataField="AverageItemCost" />
					<px:PXGridColumn DataField="BaseCuryID" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
    </px:PXGrid>
</asp:Content>
