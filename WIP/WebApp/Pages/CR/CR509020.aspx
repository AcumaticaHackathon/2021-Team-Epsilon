<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CR509020.aspx.cs" Inherits="Page_CR509020" Title="Validate Addresses" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" Visible="True" PrimaryView="Filter" TypeName="PX.Objects.CR.CRValidateAddressProcess">
		<CallbackCommands/>
	</px:PXDataSource>
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter"> 
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="M" LabelsWidth="SM"/>
			<px:PXSelector ID="edCountry" runat="server" DataField="Country" AutoRefresh="True" DataSourceID="ds" CommitChanges="true" />
			<px:PXDropDown ID="edBAccountType" runat="server" DataField="BAccountType" AllowNull="True" NullText="All" CommitChanges="True" />
			<px:PXDropDown ID="edBAccountStatus" runat="server" DataField="BAccountStatus" AllowNull="True" NullText="All" CommitChanges="True" />
			<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="SM" LabelsWidth="XS"/>
			<px:PXCheckBox ID="edIsOverride" runat="server" DataField="IsOverride" />
		</Template>	
	</px:PXFormView>
	<px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" Style="z-index: 100" AllowPaging="true" AdjustPageSize="Auto" AllowSearch="true" DataSourceID="ds" BatchUpdate="True" SkinID="PrimaryInquire" Caption="Addresses"  
		FastFilterFields="AcctCD, AddressLine1, AddressLine2, City, CountryID, State, PostalCode" SyncPosition="true">
		<Levels>
			<px:PXGridLevel DataMember="AddressList">
				<Columns>
					<px:PXGridColumn DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" AllowSort="False" AllowMove="False" />
					<px:PXGridColumn DataField="AcctCD" Width="150px" LinkCommand="viewDetails" />
					<px:PXGridColumn DataField="AcctName" Width="200px"  />
					<px:PXGridColumn DataField="Type" Width="120px" />
					<px:PXGridColumn DataField="Status" Width="100px" />
					<px:PXGridColumn DataField="AddressLine1" Width="250px" />
					<px:PXGridColumn DataField="AddressLine2" Width="250px" />
					<px:PXGridColumn DataField="City" Width="150px" />
					<px:PXGridColumn DataField="State" Width="150px" />
					<px:PXGridColumn DataField="PostalCode" Width="150px" />
					<px:PXGridColumn DataField="CountryID" Width="150px" />
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="400" />
		<Layout ShowRowStatus="False" />
	</px:PXGrid>
</asp:Content>