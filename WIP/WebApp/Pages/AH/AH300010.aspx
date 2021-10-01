<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true"
    ValidateRequest="false" CodeFile="AH300010.aspx.cs" Inherits="Page_AH300010" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="AH.Objects.AHIG.IotDeviceMaint" PrimaryView="PagePrimaryView">
		<CallbackCommands></CallbackCommands>
	</px:PXDataSource>
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="PagePrimaryView">
		<Template>
			<px:PXLayoutRule runat="server" StartRow="True"/>
			<px:PXSelector ID="edDeviceCD" runat="server" DataField="DeviceCD"/>
			<px:PXTextEdit ID="edZone" runat="server" DataField="Zone"/>
			<px:PXLayoutRule runat="server" ColumnSpan="2"/>
			<px:PXTextEdit ID="edDeviceName" runat="server" DataField="DeviceName"/>
			<px:PXLayoutRule runat="server" StartColumn="True"/>
			<px:PXNumberEdit ID="edLatitude" runat="server" DataField="Latitude"/>
			<px:PXNumberEdit ID="edLongitude" runat="server" DataField="Longitude"/>
		</Template>
	</px:PXFormView>
</asp:Content>

<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%" Height="150px" DataSourceID="ds">
		<Items>
			<px:PXTabItem Text="Device Payloads">
				<Template>
					<px:PXGrid ID="grid1" runat="server" DataSourceID="ds" >
						<Levels>
                            <px:PXGridLevel DataMember="PayLoadView">
								<Columns>
									<px:PXGridColumn DataField="DeviceCD"/>
								</Columns>
	                        </px:PXGridLevel>
                        </Levels>
                    </px:PXGrid>
               	</Template>					
			</px:PXTabItem>
			
			<px:PXTabItem Text="Device Locations">
				<Template>
                	<px:PXGrid ID="grid1" runat="server" DataSourceID="ds" >
                		<Levels>
                			<px:PXGridLevel DataMember="BreadCrumbView">
                				<Columns>
	                                <px:PXGridColumn DataField="DeviceCD"/>
                					<px:PXGridColumn DataField="Latitude"/>
	                                <px:PXGridColumn DataField="Longitude"/>
                                </Columns>
                			</px:PXGridLevel>
                		</Levels>
                	</px:PXGrid>
                </Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150"></AutoSize>
	</px:PXTab>
</asp:Content>
