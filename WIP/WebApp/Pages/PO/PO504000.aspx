<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="PO504000.aspx.cs" Inherits="Pages_PO504000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" TypeName="PX.Objects.PO.POCreateIntercompanySalesOrder"
		PrimaryView="Filter" BorderStyle="NotSet" Width="100%" />

</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100"
		Width="100%" DataMember="Filter" Caption="Selection" DefaultControlID="edPODocType">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
			<px:PXDateTimeEdit CommitChanges="True" ID="edDocDate" runat="server" DataField="DocDate" />
			<px:PXSegmentMask CommitChanges="True" ID="edPurchasingCompany" runat="server" DataField="PurchasingCompany" />
			<px:PXSegmentMask CommitChanges="True" ID="edSellingCompany" runat="server" DataField="SellingCompany" />
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
			<px:PXCheckBox CommitChanges="True" ID="chkPutReceiptsOnHold" runat="server" DataField="PutReceiptsOnHold" AlignLeft="true" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100"
		Width="100%" ActionsPosition="Top" Caption="Documents" SkinID="PrimaryInquire" SyncPosition="true" FastFilterFields="CustomerID,ShipmentNbr" AllowPaging="True">
		<Levels>
			<px:PXGridLevel DataMember="Documents">
				<RowTemplate>
					<px:PXSelector runat="server" ID="edSOOrder__IntercompanyPONbr" DataField="SOOrder__IntercompanyPONbr" CommitChanges="True" AllowEdit="True" />
				</RowTemplate>
				<Columns>
					<px:PXGridColumn AllowNull="False" DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" AllowSort="False" AllowMove="False" AllowUpdate="False" AutoCallBack="True" />
					<px:PXGridColumn DataField="CustomerID" />
					<px:PXGridColumn DataField="SOOrder__BranchID" />
					<px:PXGridColumn DataField="ShipmentNbr" LinkCommand="ViewSODocument" />
					<px:PXGridColumn DataField="Status" />
					<px:PXGridColumn DataField="ShipDate" />
					<px:PXGridColumn DataField="ShipmentQty" />
					<px:PXGridColumn DataField="SiteID" />
					<px:PXGridColumn DataField="ShipmentDesc" Width="260"/>
					<px:PXGridColumn DataField="WorkgroupID" />
					<px:PXGridColumn DataField="ShipmentWeight"  />
					<px:PXGridColumn DataField="ShipmentVolume" />
					<px:PXGridColumn DataField="PackageCount" />
					<px:PXGridColumn DataField="PackageWeight"/>
					<px:PXGridColumn DataField="Excluded" TextAlign="Center" Type="CheckBox" CommitChanges="True" />
					<px:PXGridColumn DataField="SOOrder__IntercompanyPONbr" />
					<px:PXGridColumn DataField="IntercompanyPOReceiptNbr" LinkCommand="ViewPOReceipt"/>
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<ActionBar DefaultAction="ViewPODocument" />
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXGrid>
</asp:Content>
