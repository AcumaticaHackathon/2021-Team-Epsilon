<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CS102500.aspx.cs" Inherits="Page_CS102500" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%"
		TypeName="PX.Objects.CS.CompanyGroupsMaint" PrimaryView="BAccount" HeaderDescriptionField="AcctName">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Insert" PostData="Self" />
			<px:PXDSCallbackCommand Name="First" PostData="Self" StartNewGroup="True" />
			<px:PXDSCallbackCommand Name="Last" PostData="Self" />
			<px:PXDSCallbackCommand Name="Save" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ViewMainOnMap" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewContact" DependOnGrid="grdEmployees" Visible="False" />
			<px:PXDSCallbackCommand Name="NewContact" Visible="False" />
			<px:PXDSCallbackCommand Name="DeleteOrganizationLedgerLink" Visible="false"/>
			<px:PXDSCallbackCommand Name="AddBranch" Visible="false"/>
			<px:PXDSCallbackCommand Name="ViewBranch" Visible="false"/>
			<px:PXDSCallbackCommand Name="AddLedger" Visible="false"/>
			<px:PXDSCallbackCommand Name="ViewLedger" Visible="false"/>
			<px:PXDSCallbackCommand Name="CreateLedger" Visible="false" />
			<px:PXDSCallbackCommand Name="ChangeID" Visible="false" />

			<px:PXDSCallbackCommand Name="ValidateAddresses" StartNewGroup="True" Visible="False" />
			<px:PXDSCallbackCommand Name="AddressLookupSelectAction" CommitChanges="true" Visible="false" />
			<px:PXDSCallbackCommand Name="AddressLookup" SelectControlsIDs="PXFormView1" RepaintControls="None" RepaintControlsIDs="ds,DefAddress" CommitChanges="true" Visible="false" />
			<px:PXDSCallbackCommand Name="DefLocationAddressLookup" SelectControlsIDs="PXFormView1" RepaintControls="None" RepaintControlsIDs="ds,frmDefLocation" CommitChanges="true" Visible="false" />

			<px:PXDSCallbackCommand Name="NewLocation" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="ViewDefLocationAddressOnMap" Visible="false" />
			<px:PXDSCallbackCommand Name="SetDefaultLocation" DependOnGrid="grdLocations" Visible="False" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" Width="100%"  DataSourceID="ds"
		DataMember="BAccount" Caption="Groups">
		<Template>
			<px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
			<px:PXSegmentMask ID="edAcctCD" runat="server" DataField="AcctCD" />
			<px:PXTextEdit CommitChanges="True" ID="edAcctName" runat="server" DataField="AcctName" />	
			<px:PXFormView ID="BaseCurrency" runat="server" Width="100%" RenderStyle="Simple" DataMember="OrganizationView" DataSourceID="ds">
				<Template>
					<px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
					<px:PXSelector CommitChanges="True" ID="edBaseCuryID" runat="server" DataField="BaseCuryID" DataSourceID="ds" AllowEdit="True"  />
				</Template>
			</px:PXFormView>
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXGrid ID="grid" runat="server" Height="150px" DataSourceID="ds" SyncPosition="True"
		Width="100%" Caption="Organizations" SkinID="Details">
		<Levels>
			<px:PXGridLevel DataMember="Organizations">
				 <RowTemplate>
					 <px:PXSelector ID="edOrganizationID" runat="server" DataField="OrganizationID" AutoRefresh="true" Enabled="False" />
				 </RowTemplate>
				<Columns>
					<px:PXGridColumn DataField="OrganizationID" LinkCommand="ViewCompany" CommitChanges="true" />
					<px:PXGridColumn DataField="Organization__OrganizationName" />
					<px:PXGridColumn DataField="PrimaryGroup__GroupID" />					
					<px:PXGridColumn DataField="Ledger__LedgerCD" />
				</Columns>
				<Layout FormViewHeight=""></Layout>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXGrid>
</asp:Content>


