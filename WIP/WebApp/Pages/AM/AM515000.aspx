<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AM515000.aspx.cs"
    Inherits="Page_AM515000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.AM.CTPProcess" PrimaryView="Filter" PageLoadBehavior="PopulateSavedValues">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="Process" StartNewGroup="True" />
            <px:PXDSCallbackCommand Name="QtyAvailable" Visible="False" DependOnGrid="grid" />
        </CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" DataMember="Filter" Width="100%" Caption="Selection" DefaultControlID="edSOOrderNbr" EmailingGraph="" >
        <Activity HighlightColor="" SelectedColor="" Width="" Height=""></Activity>
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="SM" />
            <px:PXDropDown ID="edProcessAction" runat="server" DataField="ProcessAction"></px:PXDropDown>
            <px:PXSelector CommitChanges="True" ID="edSOOrderType" runat="server" DataField="SOOrderType" AutoRefresh="true" />
            <px:PXSelector CommitChanges="True" ID="edSOOrderNbr" runat="server" DataField="SOOrderNbr"  AutoRefresh="true"/>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="M" StartGroup="true" GroupCaption="CTP Acceptance Settings"/>
            <px:PXSelector ID="edDefaultOrderType" aurelia="true" runat="server" DataField="DefaultOrderType" AutoRefresh="True" DataSourceID="ds" CommitChanges="true">
                <GridProperties>
                    <Layout ColumnsMenu="False" />
                </GridProperties>
            </px:PXSelector>
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" Height="150px" AllowPaging="True" 
               AllowSearch="true" BatchUpdate="true" AdjustPageSize="Auto" SkinID="PrimaryInquire" Caption="Documents" SyncPosition="True">
        <Levels>
            <px:PXGridLevel DataMember="ProcessingRecords">
                <RowTemplate>
                    <px:PXSelector ID="edOrderType" DataField ="OrderType" runat="server" />
                    <px:PXTextEdit ID="edOrderNbr" DataField ="OrderNbr" runat="server" />
                    <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" />
                    <px:PXSegmentMask ID="edSubItemID" runat="server" DataField="SubItemID" AutoRefresh="True">
                        <Parameters>
                            <px:PXControlParam ControlID="grid" Name="SOLine.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]" Type="String" />
                        </Parameters>
                    </px:PXSegmentMask>
                    <px:PXSegmentMask ID="edSiteID" runat="server" DataField="SiteID" AutoRefresh="True">
                        <Parameters>
                            <px:PXControlParam ControlID="grid" Name="SOLine.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]" Type="String" />
                            <px:PXControlParam ControlID="grid" Name="SOLine.subItemID" PropertyName="DataValues[&quot;SubItemID&quot;]" Type="String" />
                        </Parameters>
                    </px:PXSegmentMask>
                    <px:PXTextEdit ID="edTranDesc" runat="server" DataField="TranDesc" />
                    <px:PXDateTimeEdit ID="edRequestDate" runat="server" DataField="RequestDate" />
                    <px:PXDateTimeEdit ID="edShipDate" runat="server" DataField="ShipDate" />
                    <px:PXNumberEdit ID="edOpenQty" runat="server" DataField="OpenQty" />
                    <px:PXTextEdit ID="edAMCTPOrderType" runat="server" DataField="AMCTPOrderType" />
                    <px:PXTextEdit ID="edProdOrdID" runat="server" DataField="ProdOrdID" AllowEdit="True" />
                    <px:PXTextEdit ID="edManualOrder" runat="server" DataField="ManualProdOrdID" AllowEdit="True" />
                </RowTemplate>
                <Columns>
                    <px:PXGridColumn DataField="Selected" Type="CheckBox" TextAlign="Center" AllowCheckAll="True" CommitChanges="true" />
                    <px:PXGridColumn DataField="OrderType" />
                    <px:PXGridColumn DataField="OrderNbr" />
                    <px:PXGridColumn DataField="LineNbr" TextAlign="Right" />
                    <px:PXGridColumn DataField="SortOrder" TextAlign="Right" />
                    <px:PXGridColumn DataField="InventoryID" />
                    <px:PXGridColumn DataField="SubItemID" NullText="<SPLIT>" />
                    <px:PXGridColumn DataField="TranDesc" />
                    <px:PXGridColumn DataField="SiteID" />
                    <px:PXGridColumn DataField="UOM" />
                    <px:PXGridColumn DataField="OpenQty" TextAlign="Right" LinkCommand="QtyAvailable" />
                    <px:PXGridColumn DataField="RequestDate" />
                    <px:PXGridColumn DataField="ShipDate" />
                    <px:PXGridColumn DataField="AMCTPOrderType" />
                    <px:PXGridColumn DataField="ProdOrdID" />
                    <px:PXGridColumn DataField="CTPDate" />
                    <px:PXGridColumn DataField="ManualProdOrdID" />
                    <px:PXGridColumn DataField="AMCTPAccepted" Type="CheckBox" TextAlign="Center" />
                    <px:PXGridColumn DataField="AMOrigRequestDate" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
    </px:PXGrid>
        <px:PXSmartPanel ID="QtyAvailablePanel" runat="server" AutoCallBack-Command="Refresh" AutoCallBack-Enabled="True"
        AutoCallBack-Target="FormQtyAvailable" Caption="Quantity Available" CaptionVisible="True" Key="QtyAvailableFilter"
        DesignView="Content" Height="210px" Width="400px" LoadOnDemand="true" >
        <px:PXFormView ID="FormQtyAvailable" runat="server" DataSourceID="ds" CaptionVisible="False"
            DataMember="QtyAvailableFilter" SkinID="Transparent" Width="100%">
            <Template>
                <px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="L" ControlSize="M" />
                <px:PXNumberEdit ID="edRequestQty" runat="server" DataField="RequestQty" />
                <px:PXNumberEdit ID="edQtyAvail" runat="server" DataField="QtyAvail" />
                <px:PXLayoutRule ID="PXLayoutRule2" runat="server" StartGroup="true" GroupCaption="DETAILS" LabelsWidth="L" ControlSize="M" />
                <px:PXNumberEdit ID="edQtyHardAvail" runat="server" DataField="QtyHardAvail" />
                <px:PXNumberEdit ID="edSupplyQty" runat="server" DataField="SupplyAvail" />
                <px:PXNumberEdit ID="edProdQty" runat="server" DataField="ProdAvail" />
                <px:PXNumberEdit ID="edTotalQty" runat="server" DataField="TotalAvail" />
            </Template>
        </px:PXFormView>
    </px:PXSmartPanel>
</asp:Content>