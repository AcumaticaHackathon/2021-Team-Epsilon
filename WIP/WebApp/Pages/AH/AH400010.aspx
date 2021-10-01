<%@ Page Language="C#" MasterPageFile="~/MasterPages/TabView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AH400010.aspx.cs" Inherits="Page_AH400010" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/TabView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="AH.Objects.AHIG.IotDeviceMaint" PrimaryView="PagePrimaryView">
		<CallbackCommands></CallbackCommands>
	</px:PXDataSource>
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
	<px:PXTab ID="tab" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100" Width="100%">
		<Items>
			<px:PXTabItem Text="Iot Devices">
				<Template>
					<px:PXGrid ID="grid1" runat="server" DataSourceID="ds" >
						<Levels>
							<px:PXGridLevel DataMember="PagePrimaryView">
								<Columns>
									<px:PXGridColumn DataField="DeviceName"/>
									<px:PXGridColumn DataField="AcumaticaEntityType"/>
									<px:PXGridColumn DataField="Latitude"/>
									<px:PXGridColumn DataField="Longitude"/>
									<px:PXGridColumn DataField="Zone"/>
								</Columns>
							</px:PXGridLevel>
						</Levels>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			
			
			<px:PXTabItem Text="Tab item 2">
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="200"></AutoSize>
	</px:PXTab>
</asp:Content>
