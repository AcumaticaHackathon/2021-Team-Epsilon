<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="BC201000.aspx.cs" Inherits="Page_BC201000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource PageLoadBehavior="GoFirstRecord" ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Bindings" TypeName="PX.Commerce.BigCommerce.BCBigCommerceStoreMaint">

		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Reload" PostData="Self" Visible="False"></px:PXDSCallbackCommand>
			<px:PXDSCallbackCommand Name="navigate" DependOnGrid="CstPXGrid60"></px:PXDSCallbackCommand>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Bindings" TabIndex="6500">
		<Template>
			<px:PXLayoutRule runat="server" StartRow="True" ControlSize="L" LabelsWidth="SM"></px:PXLayoutRule>
			<px:PXDropDown AllowEdit="False" CommitChanges="True" runat="server" ID="CstPXDropDown16" DataField="ConnectorType"></px:PXDropDown>
			<px:PXSelector AutoRefresh="True" AllowEdit="False" CommitChanges="True" runat="server" ID="CstPXSelector17" DataField="BindingName"></px:PXSelector>
			<px:PXLayoutRule runat="server" ID="CstPXLayoutRule69" StartColumn="True" />
			<px:PXCheckBox CommitChanges="True" AlignLeft="True" runat="server" ID="CstPXCheckBox67" DataField="IsActive"></px:PXCheckBox>
			<px:PXCheckBox AlignLeft="True" runat="server" ID="CstPXCheckBox68" DataField="IsDefault"></px:PXCheckBox>
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%" Height="150px" DataSourceID="ds" DataMember="CurrentStore">
		<Items>
			<px:PXTabItem Text="Connection Settings">
				<Template>
					<px:PXLayoutRule runat="server" ID="CstPXLayoutRule118" StartColumn="True"></px:PXLayoutRule>
					<px:PXLayoutRule ControlSize="XL" LabelsWidth="SM" runat="server" ID="CstPXLayoutRule70" StartGroup="True"></px:PXLayoutRule>
					<px:PXFormView RenderStyle="Simple" DataMember="CurrentBindingBigCommerce" runat="server" ID="frmCurrentBindingBigCommerce1">
						<Template>
							<px:PXLayoutRule StartColumn="True" ControlSize="XL" LabelsWidth="SM" runat="server" ID="PXLayoutRule1" StartRow="True"></px:PXLayoutRule>
							<px:PXTextEdit CommitChanges="True" Size="" runat="server" ID="CstPXTextEdit60" DataField="StoreAdminUrl"></px:PXTextEdit>
							<px:PXLayoutRule runat="server" ControlSize="XL" LabelsWidth="SM" StartGroup="True" GroupCaption="REST Settings"></px:PXLayoutRule>
							<px:PXTextEdit ID="edStoreBaseURL" runat="server" AlreadyLocalized="False" DataField="StoreBaseURL" CommitChanges="True"></px:PXTextEdit>
							<px:PXTextEdit ID="edStoreXAuthClient" runat="server" AlreadyLocalized="False" DataField="StoreXAuthClient" CommitChanges="True"></px:PXTextEdit>
							<px:PXTextEdit ID="edStoreXAuthToken" runat="server" AlreadyLocalized="False" DataField="StoreXAuthToken" CommitChanges="True"></px:PXTextEdit>
							<px:PXLayoutRule ControlSize="XL" LabelsWidth="SM" runat="server" GroupCaption="WebDAV Settings" StartGroup="True"></px:PXLayoutRule>
							<px:PXTextEdit CommitChanges="True" runat="server" ID="CstPXTextEdit8" DataField="StoreWDAVServerUrl"></px:PXTextEdit>
							<px:PXTextEdit ID="edStoreWDAVClientUser" runat="server" AlreadyLocalized="False" DataField="StoreWDAVClientUser" CommitChanges="True"></px:PXTextEdit>
							<px:PXTextEdit ID="edStoreWDAVClientPass" runat="server" AlreadyLocalized="False" DataField="StoreWDAVClientPass" CommitChanges="True"></px:PXTextEdit>
						</Template>
					</px:PXFormView>
					<px:PXFormView RenderStyle="Simple" DataMember="CurrentBinding" runat="server" ID="PXFormView1">
						<Template>
							<px:PXLayoutRule runat="server" ID="CstPXLayoutRule15" StartGroup="True" GroupCaption="System Settings" LabelsWidth="SM" ControlSize="XL"></px:PXLayoutRule>
							<px:PXSelector runat="server" ID="CstPXSelectorLocaleName" DataField="LocaleName"></px:PXSelector>
						</Template>
					</px:PXFormView>
					<px:PXLayoutRule runat="server" ID="PXLayoutRule2" StartColumn="True"></px:PXLayoutRule>
					<px:PXLayoutRule runat="server" ControlSize="XL" LabelsWidth="SM" StartGroup="True" GroupCaption="Store Properties"></px:PXLayoutRule>
					<px:PXFormView RenderStyle="Simple" DataMember="CurrentBindingBigCommerce" runat="server" ID="PXFormView2">
						<Template>
							<px:PXLayoutRule StartColumn="True" ControlSize="XL" LabelsWidth="SM" runat="server" ID="PXLayoutRule1" StartRow="True"></px:PXLayoutRule>
							<px:PXTextEdit runat="server" ID="edBigCommerceDefaultCurrency" DataField="BigCommerceDefaultCurrency"></px:PXTextEdit>
							<px:PXTextEdit runat="server" ID="edBigCommerceStoreTimeZone" DataField="BigCommerceStoreTimeZone"></px:PXTextEdit>
						</Template>
					</px:PXFormView>
				</Template>
				<ContentLayout SpacingSize="Medium" ControlSize="XM" LabelsWidth="M"></ContentLayout>
			</px:PXTabItem>
			<px:PXTabItem Text="Entity Settings">
				<Template>
					<px:PXGrid MatrixMode="True" runat="server" SkinID="Details" Width="100%" ID="CstPXGrid60">
						<AutoSize Enabled="True" Container="Window"></AutoSize>
						<ActionBar DefaultAction="navigate">
							<Actions>
								<AddNew ToolBarVisible="False"></AddNew>
								<Delete ToolBarVisible="False"></Delete>
								<ExportExcel ToolBarVisible="False"></ExportExcel>
							</Actions>
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="Entities">
								<Columns>
									<px:PXGridColumn Type="CheckBox" TextAlign="Center" DataField="IsActive" Width="60" CommitChanges="True"></px:PXGridColumn>
									<px:PXGridColumn LinkCommand="Navigate" DataField="EntityType" Width="70"></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="Direction" Width="70"></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="PrimarySystem" Width="120"></px:PXGridColumn>
									<px:PXGridColumn DataField="ImportRealTimeStatus" Width="120"></px:PXGridColumn>
									<px:PXGridColumn DataField="ExportRealTimeStatus" Width="120"></px:PXGridColumn>
									<px:PXGridColumn DataField="RealTimeMode" Width="130"></px:PXGridColumn>
									<px:PXGridColumn DataField="MaxAttemptCount" Width="120"></px:PXGridColumn>
								</Columns>
								<RowTemplate>
									<px:PXNumberEdit runat="server" ID="CstPXNumberEdit70" DataField="MaxAttemptCount"></px:PXNumberEdit>
								</RowTemplate>
							</px:PXGridLevel>
						</Levels>
						<Mode AllowAddNew="False" AllowDelete="False" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Customer Settings">
				<Template>
					<px:PXLayoutRule runat="server" StartGroup="False" ControlSize="M" LabelsWidth="M" StartColumn="True">
					</px:PXLayoutRule>
					<px:PXLayoutRule GroupCaption="Customer" runat="server" ID="CstPXLayoutRule79" StartGroup="True"></px:PXLayoutRule>
					<px:PXSelector AllowEdit="True" ID="edCustomerClassID" runat="server" DataField="CustomerClassID" CommitChanges="True">
					</px:PXSelector>
					<px:PXSegmentMask runat="server" DataField="CustomerTemplate" ID="CstPXSegmentMask28"></px:PXSegmentMask>
					<px:PXSelector runat="server" ID="CstPXSelector27" DataField="CustomerNumberingID" AllowEdit="True"></px:PXSelector>
					<px:PXSegmentMask runat="server" DataField="LocationTemplate" ID="CstPXSegmentMask32"></px:PXSegmentMask>
					<px:PXSelector runat="server" ID="CstPXSelector31" DataField="LocationNumberingID" AllowEdit="True"></px:PXSelector>
					<px:PXSelector runat="server" ID="CstPXSelector29" DataField="InventoryNumberingID" AllowEdit="True"></px:PXSelector>
					<px:PXSegmentMask runat="server" DataField="InventoryTemplate" ID="CstPXSegmentMask30"></px:PXSegmentMask>
					<px:PXSegmentMask AllowEdit="True" runat="server" ID="CstPXSegmentMask49" DataField="GuestCustomerID"></px:PXSegmentMask>
				</Template>
				<ContentLayout ControlSize="XM" LabelsWidth="M"></ContentLayout>
			</px:PXTabItem>
			<px:PXTabItem Text="Inventory Settings">
				<Template>
					<px:PXLayoutRule ControlSize="L" LabelsWidth="M" runat="server" ID="CstPXLayoutRule56" StartGroup="True" GroupCaption="Inventory Settings"></px:PXLayoutRule>
					<px:PXSelector AllowEdit="True" CommitChanges="True" runat="server" ID="CstPXSelector10" DataField="StockItemClassID"></px:PXSelector>
					<px:PXSelector AllowEdit="True" CommitChanges="True" runat="server" ID="CstPXSelector9" DataField="NonStockItemClassID"></px:PXSelector>
					<px:PXDropDown runat="server" ID="CstPXDropDown68" DataField="StockSalesCategoriesIDs"></px:PXDropDown>
					<px:PXDropDown runat="server" ID="CstPXDropDown67" DataField="NonStockSalesCategoriesIDs"></px:PXDropDown>
					<px:PXDropDown AllowMultiSelect="True" runat="server" ID="CstPXDropDown121" DataField="RelatedItems"></px:PXDropDown>
					<px:PXDropDown CommitChanges="True" runat="server" ID="edVisibility" DataField="Visibility"></px:PXDropDown>
					<px:PXDropDown CommitChanges="True" runat="server" ID="CstPXDropDown57" DataField="Availability"></px:PXDropDown>
					<px:PXDropDown CommitChanges="True" runat="server" ID="CstPXDropDown58" DataField="NotAvailMode"></px:PXDropDown>
					<px:PXDropDown CommitChanges="True" runat="server" DataField="AvailabilityCalcRule" ID="CstPXDropDown45"></px:PXDropDown>
					<px:PXDropDown CommitChanges="True" runat="server" ID="CstPXDropDown71" DataField="WarehouseMode"></px:PXDropDown>
					<px:PXLayoutRule GroupCaption="Warehouse Settings" runat="server" ID="CstPXLayoutRule761" StartGroup="True"></px:PXLayoutRule>
					<px:PXGrid Height="200px" Width="510px" SyncPosition="True" AllowPaging="False" SkinID="Inquire" AutoAdjustColumns="False" MatrixMode="True" runat="server" ID="gridExportLocations">
						<Levels>
							<px:PXGridLevel DataMember="ExportLocations">
								<RowTemplate>
									<px:PXSelector AutoRefresh="True" CommitChanges="True" runat="server" DataField="LocationID" ID="pxsLocationId"></px:PXSelector>
									<px:PXTextEdit runat="server" DataField="Description" AlreadyLocalized="False" ID="pxtLocationDescription"></px:PXTextEdit>
									<px:PXSelector runat="server" ID="pxsSiteId" DataField="SiteID" DisplayMode="Hint" CommitChanges="True"></px:PXSelector>
									<%--<px:PXDropDown runat="server" ID="PXDDExtLocationId" DataField="ExternalLocationID"></px:PXDropDown>--%>
								</RowTemplate>
								<Columns>
									<px:PXGridColumn CommitChanges="True" DataField="SiteID" Width="140"></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="LocationID" Width="140px"></px:PXGridColumn>
									<%--<px:PXGridColumn DataField="ExternalLocationID" Width="200px"></px:PXGridColumn>--%>
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<ActionBar>
							<Actions>
								<AddNew Enabled="True" /></Actions></ActionBar>
						<ActionBar>
							<Actions>
								<Delete Enabled="True" /></Actions></ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Order Settings">
				<Template>
					<px:PXLayoutRule LabelsWidth="SM" ControlSize="M" runat="server" ID="CstPXLayoutRule112" StartColumn="True"></px:PXLayoutRule>
					<px:PXLayoutRule LabelsWidth="SM" ControlSize="M" runat="server" ID="CstPXLayoutRule115" StartGroup="True" GroupCaption="General"></px:PXLayoutRule>
					<px:PXFormView RenderStyle="Simple" DataMember="CurrentBinding" runat="server" ID="frmCurrentBinding1">
						<Template>
							<px:PXLayoutRule StartColumn="True" LabelsWidth="SM" ControlSize="M" runat="server" ID="PXLayoutRule1" StartRow="True"></px:PXLayoutRule>
							<px:PXSegmentMask runat="server" ID="edBranchID" DataField="BranchID" AllowEdit="True"></px:PXSegmentMask>
						</Template>
					</px:PXFormView>	
					<px:PXLayoutRule LabelsWidth="SM" ControlSize="M" GroupCaption="Order" runat="server" ID="CstPXLayoutRule75" StartGroup="True"></px:PXLayoutRule>
					<px:PXSelector CommitChanges="True" AllowEdit="True" runat="server" ID="edOrderTpe" DataField="OrderType"></px:PXSelector>
					<px:PXDropDown runat="server" ID="CstPXDropDown1" DataField="OtherSalesOrderTypes"></px:PXDropDown>
					<px:PXSelector CommitChanges="True" AllowEdit="True" runat="server" ID="edReturnOrderType" DataField="ReturnOrderType"></px:PXSelector>
					<px:PXSelector AllowEdit="True" runat="server" ID="edRefundItem" DataField="RefundAmountItemID"></px:PXSelector>
					<px:PXSelector AllowEdit="True" runat="server" ID="edReasonCode" DataField="ReasonCode"></px:PXSelector>
					<px:PXDropDown ID="edTimeZone" runat="server" DataField="OrderTimeZone" />
					<px:PXDropDown runat="server" ID="CstPXDropDown80" DataField="PostDiscounts" CommitChanges="True"></px:PXDropDown>
					<px:PXSelector AllowEdit="True" runat="server" ID="CstPXSelector81" DataField="GiftCertificateItemID"></px:PXSelector>
					<px:PXDateTimeEdit runat="server" ID="CstPXDateTimeSyncFrom" DataField="SyncOrdersFrom" />
					<px:PXLayoutRule LabelsWidth="SM"  ControlSize="M" runat="server" ID="CstPXLayoutRule117" StartGroup="True" GroupCaption="Taxes"></px:PXLayoutRule>
					<px:PXCheckBox runat="server" ID="CstPXCheckBox2" DataField="TaxSynchronization" CommitChanges="True"></px:PXCheckBox>
					<px:PXSelector AutoRefresh="True" runat="server" ID="CstPXSelector118" DataField="DefaultTaxZoneID" CommitChanges="True"></px:PXSelector>
					<px:PXCheckBox runat="server" ID="CstPXCheckBox1" DataField="UseAsPrimaryTaxZone"/>
					<px:PXLayoutRule LabelsWidth="SM"  ControlSize="M" runat="server" ID="PXLayoutRule118" StartGroup="True" GroupCaption="Substitution Lists"></px:PXLayoutRule>
					<px:PXSelector CommitChanges="True" AllowEdit="False" runat="server" ID="PXSelector2" DataField="TaxSubstitutionListID"></px:PXSelector>
					<px:PXSelector CommitChanges="True" AllowEdit="False" runat="server" ID="PXSelector3" DataField="TaxCategorySubstitutionListID"></px:PXSelector>
					<px:PXLayoutRule ControlSize="L" LabelsWidth="M" runat="server" ID="CstPXLayoutRule85" StartColumn="True"></px:PXLayoutRule>
					<px:PXLayoutRule GroupCaption="Shipping Option Mapping" runat="server" ID="CstPXLayoutRule76" StartGroup="True"></px:PXLayoutRule>
					<px:PXGrid Height="200px" Width="660px" SyncPosition="True" AllowPaging="False" SkinID="Inquire" AutoAdjustColumns="True" MatrixMode="True" runat="server" ID="ShippingMappings">
						<Levels>
							<px:PXGridLevel DataMember="ShippingMappings">
								<Columns>
									<px:PXGridColumn CommitChanges="True" TextAlign="Center" Type="CheckBox" DataField="Active" Width="80"></px:PXGridColumn>
									<px:PXGridColumn DataField="ShippingZone" Width="120"></px:PXGridColumn>
									<px:PXGridColumn DataField="ShippingMethod" Width="120"></px:PXGridColumn>
									<px:PXGridColumn DataField="CarrierID" Width="120"></px:PXGridColumn>
									<px:PXGridColumn DataField="ZoneID" Width="100"></px:PXGridColumn>
									<px:PXGridColumn DataField="ShipTermsID" Width="100"></px:PXGridColumn>
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<ActionBar>
							<Actions>
								<AddNew Enabled="True" /></Actions></ActionBar>
						<ActionBar>
							<Actions>
								<Delete Enabled="True" /></Actions></ActionBar>

					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Payment Settings">
				<Template>
					<px:PXLayoutRule ControlSize="L" LabelsWidth="M" runat="server" ID="CstPXLayoutRule120" StartColumn="True"></px:PXLayoutRule>
					<px:PXLayoutRule GroupCaption="Payment Method Mapping" runat="server" ID="CstPXLayoutRule122" StartGroup="True"></px:PXLayoutRule>
					<px:PXGrid SyncPosition="True" TabIndex="30400" Height="160px" runat="server" ID="PaymentsMethods" AllowPaging="False" AutoAdjustColumns="False" Caption="Base Currency Payment Methods" CaptionVisible="True" MatrixMode="True" SkinID="Inquire" Width="100%">
						<Levels>
							<px:PXGridLevel DataMember="PaymentMethods">
								<Columns>
									<px:PXGridColumn CommitChanges="True" TextAlign="Center" Type="CheckBox" DataField="Active" Width="80"></px:PXGridColumn>
									<px:PXGridColumn DataField="StorePaymentMethod" Width="200"></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="PaymentMethodID" Width="140"></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="CashAccountID" Width="140"></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="ProcessingCenterID" Width="120" />
									<px:PXGridColumn DataField="CuryID" Width="80"></px:PXGridColumn>
									<px:PXGridColumn TextAlign="Center" Type="CheckBox" DataField="ReleasePayments" Width="80"></px:PXGridColumn>
									<px:PXGridColumn DataField="StoreOrderPaymentMethod" Width="200" ></px:PXGridColumn>
									<px:PXGridColumn  TextAlign="Center" Type="CheckBox" DataField="ProcessRefunds" Width="100"></px:PXGridColumn>
								   <px:PXGridColumn TextAlign="Center" Type="CheckBox" DataField="CreatePaymentfromOrder" Width="120" ></px:PXGridColumn></Columns>
								
								<RowTemplate>
									<px:PXSelector AutoRefresh="True" runat="server" ID="CstPXSelector112" DataField="CashAccountID"></px:PXSelector>
								</RowTemplate>
							</px:PXGridLevel>
						</Levels>
						<Mode AllowDelete="True" AllowAddNew="True"></Mode>
						<ActionBar ActionsVisible="True" DefaultAction="">
							<Actions>
								<AddNew Enabled="true" ToolBarVisible="Top" MenuVisible="True"></AddNew>
								<Delete Enabled="true" ToolBarVisible="Top"></Delete>
							</Actions>
						</ActionBar>
						<AutoCallBack Target="gridMultiCurrency" Command="Refresh">
							<Behavior CommitChanges="False" RepaintControlsIDs="gridMultiCurrency"></Behavior>
						</AutoCallBack>
					</px:PXGrid>
					<px:PXGrid SkinID="Details" runat="server" ID="gridMultiCurrency" AllowPaging="False" AutoAdjustColumns="False" Caption="Multicurrency Cash Accounts" CaptionVisible="True" Height="100px" Width="450px">
						<Levels>
							<px:PXGridLevel DataMember="MultiCurrency">
								<Columns>
									<px:PXGridColumn CommitChanges="True" DataField="CashAccountID" Width="140"></px:PXGridColumn>
									<px:PXGridColumn DataField="CuryID" Width="140"></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="ProcessingCenterID" Width="150" />
								</Columns>
								<RowTemplate>
									<px:PXTextEdit runat="server" ID="CstPXTextEdit110" DataField="Currency"></px:PXTextEdit>
									<px:PXSelector CommitChanges="True" runat="server" ID="CstPXSelector111" DataField="CashAccountID" AutoRefresh="True"></px:PXSelector>
									<px:PXSelector CommitChanges="True" runat="server" ID="CstPXSelector117" DataField="ProcessingCenterID" AutoRefresh="True"></px:PXSelector>
								</RowTemplate>
							</px:PXGridLevel>
						</Levels>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150"></AutoSize>
	</px:PXTab>
</asp:Content>
