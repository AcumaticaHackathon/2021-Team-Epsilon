<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="SO504000.aspx.cs" Inherits="Pages_SO504000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" TypeName="PX.Objects.SO.SOCreateIntercompanySalesOrders"
		PrimaryView="Filter" BorderStyle="NotSet" Width="100%" />

</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100"
		Width="100%" DataMember="Filter" Caption="Selection" DefaultControlID="edPODocType">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
			<px:PXDropDown CommitChanges="True" ID="edPODocType" runat="server" AllowNull="False" DataField="PODocType" />
			<px:PXDateTimeEdit CommitChanges="True" ID="edDocDate" runat="server" DataField="DocDate" />
			<px:PXSelector CommitChanges="True" ID="edIntercompanyOrderType" runat="server" DataField="IntercompanyOrderType" />
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
			<px:PXSegmentMask CommitChanges="True" ID="edSellingCompany" runat="server" DataField="SellingCompany" />
			<px:PXSegmentMask CommitChanges="True" ID="edPurchasingCompany" runat="server" DataField="PurchasingCompany" />
			<px:PXCheckBox CommitChanges="True" ID="chkCopyProjectDetails" runat="server" DataField="CopyProjectDetails" AlignLeft="true" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100"
		Width="100%" ActionsPosition="Top" Caption="Documents" SkinID="PrimaryInquire" SyncPosition="true" FastFilterFields="VendorID,BranchID,DocType,DocNbr" AllowPaging="True">
		<Levels>
			<px:PXGridLevel DataMember="Documents">
				<Columns>
					<px:PXGridColumn AllowNull="False" DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" AllowSort="False" AllowMove="False" AllowUpdate="False" AutoCallBack="True" />
					<px:PXGridColumn DataField="VendorID" />
					<px:PXGridColumn DataField="BranchID" />
					<px:PXGridColumn DataField="DocType" />
					<px:PXGridColumn DataField="DocNbr" LinkCommand="ViewPODocument" />
					<px:PXGridColumn DataField="DocDate" />
					<px:PXGridColumn DataField="FinPeriodID" />
					<px:PXGridColumn DataField="ExpectedDate" />
					<px:PXGridColumn DataField="CuryID" />
					<px:PXGridColumn DataField="CuryDocTotal" />
					<px:PXGridColumn DataField="CuryDiscTot" />
					<px:PXGridColumn DataField="CuryTaxTotal" />
					<px:PXGridColumn DataField="DocQty" />
					<px:PXGridColumn DataField="EmployeeID" />
					<px:PXGridColumn DataField="OwnerID" />
					<px:PXGridColumn DataField="WorkgroupID" />
					<px:PXGridColumn DataField="DocDesc" Width="400" />
					<px:PXGridColumn DataField="Excluded" TextAlign="Center" Type="CheckBox" CommitChanges="True" />
					<px:PXGridColumn DataField="OrderNbr" LinkCommand="ViewSOOrder" />
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<ActionBar DefaultAction="ViewPODocument" />
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXGrid>
</asp:Content>
