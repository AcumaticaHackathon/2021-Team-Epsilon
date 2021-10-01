<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="SO302020.aspx.cs"
    Inherits="Page_SO302020" Title="Pick, Pack, and Ship" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:content id="cont1" contentplaceholderid="phDS" runat="Server">
    <script language="javascript" type="text/javascript">
        function Barcode_Initialize(ctrl) {
            ctrl.element.addEventListener('keydown', function (e) {
                if (e.keyCode === 13) { //Enter key 
                    e.preventDefault();
                    e.stopPropagation();
                }
            });
        };
    </script>
    <script language="javascript" type="text/javascript">
        function ActionCallback(callbackContext) {
            var baseUrl = (location.href.indexOf("HideScript") > 0) ? "../../Sounds/" : "../../../Sounds/";
            var edInfoMessageSoundFile = px_alls["edInfoMessageSoundFile"];

            if ((callbackContext.info.name.toLowerCase().startsWith("scan") || callbackContext.info.name == "ElapsedTime") && callbackContext.control.longRunInProcess == null && edInfoMessageSoundFile != null) {
                var soundFile = edInfoMessageSoundFile.getValue();
                if (soundFile != null && soundFile != "") {
                    var audio = new Audio(baseUrl + soundFile + '.wav');
                    audio.play();
                }
            }
        };

        window.addEventListener('load', function () { px_callback.addHandler(ActionCallback); });
    </script>
    <style>
        .ProcessingStatusIcon .main-icon-img {
            font-size: 90px;
            margin: -10px;
        }
        .ProcessingStatusIcon .main-icon {
            height: 100px;
            width: 100px;
        }
        .ProcessingStatusIcon div.checkBox {
            height: 100px;
            width: 50px;
        }
    </style>
    <pxa:DynamicDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.SO.PickPackShipHost,PX.Objects.SO.WMS.PickPackShip+Host" PrimaryView="HeaderView">
        <CallbackCommands>
            <%-- View Linked Documents --%>
            <px:PXDSCallbackCommand DependOnGrid="gridPicked" Name="ViewOrder" Visible="False"/>

            <%-- Hide Allocation buttons --%>
            <px:PXDSCallbackCommand Name="LSSOShipLine_generateLotSerial" Visible="False" />
            <px:PXDSCallbackCommand Name="LSSOShipLine_binLotSerial" Visible="False"/>
        </CallbackCommands>
    </pxa:DynamicDataSource>
</asp:content>
<asp:content id="cont2" contentplaceholderid="phF" runat="Server">
    <px:PXFormView ID="formHeader" runat="server" DataSourceID="ds" Height="120px" Width="100%" Visible="true" DataMember="HeaderView" DefaultControlID="edBarcode" FilesIndicator="True" >
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" />
            <px:PXTextEdit ID="edBarcode" runat="server" DataField="Barcode">
                <AutoCallBack Command="Scan" Target="ds">
                    <Behavior CommitChanges="True" />
                </AutoCallBack>
                <ClientEvents Initialize="Barcode_Initialize"/>
            </px:PXTextEdit>
            <px:PXSelector ID="edRefNbr" runat="server" DataField="RefNbr" AllowEdit="true" />
            <px:PXSelector ID="edWSNbr" runat="server" DataField="WorksheetNbr" AllowEdit="true" />
            <px:PXSelector ID="edCartID" runat="server" DataField="CartID" />

            <px:PXLayoutRule runat="server" StartColumn="true" ControlSize="XS"/>
            <px:PXCheckBox ID="chkStatusIcon" runat="server" DataField="ProcessingSucceeded" RenderStyle="Button" CommitChanges="true" AlignLeft="True" CssClass="ProcessingStatusIcon">
                <CheckImages Normal="main@Success" />
                <UncheckImages Normal="main@Fail" />
            </px:PXCheckBox>

            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" ColumnWidth="M" />
            <px:PXTextEdit ID="edMessage" runat="server" DataField="Message" Width="800px" Style="font-size: 10pt; font-weight: bold;" SuppressLabel="true" TextMode="MultiLine" Height="55px" SkinID="Label" DisableSpellcheck="True" Enabled="False" />

            <px:PXLayoutRule runat="server" ColumnSpan="3" Merge="True" />
            <px:PXCheckBox ID="chkRemove" runat="server" DataField="Remove" AlignLeft="True" />
            <px:PXCheckBox ID="chkCartLoaded" runat="server" DataField="CartLoaded" AlignLeft="True" />

            <%-- Tab switchers --%>
            <px:PXCheckBox ID="chkShowPickWS" runat="server" DataField="ShowPickWS" Visible ="False" />
            <px:PXCheckBox ID="chkShowPick" runat="server" DataField="ShowPick" Visible ="False" />
            <px:PXCheckBox ID="chkShowPack" runat="server" DataField="ShowPack" Visible ="False" />
            <px:PXCheckBox ID="chkShowShip" runat="server" DataField="ShowShip" Visible ="False" />
            <px:PXCheckBox ID="chkShowReturn" runat="server" DataField="ShowReturn" Visible ="False" />
            <px:PXCheckBox ID="chkShowLog"  runat="server" DataField="ShowLog" Visible ="False" />
        </Template>
    </px:PXFormView>
    <px:PXFormView ID="formInfo" runat="server" DataSourceID="ds" DataMember="Info">
        <Template>
            <px:PXTextEdit ID="edInfoMode" runat="server" DataField="Mode"/>
            <px:PXTextEdit ID="edInfoMessage" runat="server" DataField="Message"/>
            <px:PXTextEdit ID="edInfoMessageSoundFile" runat="server" DataField="MessageSoundFile"/>
            <px:PXTextEdit ID="edInfoPrompt" runat="server" DataField="Prompt"/>
        </Template>
    </px:PXFormView>
</asp:content>
<asp:content id="cont3" contentplaceholderid="phG" runat="Server">
    <px:PXTab ID="tab" runat="server" Height="540px" Style="z-index: 100;" Width="100%">
        <Items>
            <px:PXTabItem Text="Pick" VisibleExp="DataControls[&quot;chkShowPickWS&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="gridPickedWS" runat="server" DataSourceID="ds" SyncPosition="true" Width="100%" SkinID="Inquire" OnRowDataBound="PickWSGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="PickListOfPicker">
                                <Columns>
                                    <px:PXGridColumn DataField="FitsWS" Type="CheckBox" />
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="LocationID" />
                                    <px:PXGridColumn DataField="InventoryID" />
                                    <px:PXGridColumn DataField="LotSerialNbr" />
                                    <px:PXGridColumn DataField="ExpireDate" />
                                    <px:PXGridColumn DataField="PickedQty" />
                                    <px:PXGridColumn DataField="Qty" />
                                    <px:PXGridColumn DataField="UOM" />
                                    <px:PXGridColumn DataField="ShipmentNbr" />
                                    <px:PXGridColumn DataField="INTote__ToteCD" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXNumberEdit ID="edWSEntryNbr" runat="server" DataField="EntryNbr" Enabled="false"/>
                                    <px:PXSegmentMask ID="edWSInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edWSSiteID" runat="server" DataField="SiteID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edWSLocationID" runat="server" DataField="LocationID" Enabled="False"/>
                                    <px:PXTextEdit ID="edWSLotSerialNbr" runat="server" DataField="LotSerialNbr" Enabled="False" />
                                    <px:PXDateTimeEdit ID="edWSExpireDate" runat="server" DataField="ExpireDate" Enabled="False" />
                                    <px:PXNumberEdit ID="PXWSPickedQty" runat="server" DataField="PickedQty" Enabled="False"/>
                                    <px:PXNumberEdit ID="PXWSPackedQty" runat="server" DataField="PackedQty" Enabled="False"/>
                                    <px:PXNumberEdit ID="PXWSQty" runat="server" DataField="Qty" Enabled="False"/>
                                    <px:PXSelector ID="edWSUOM" runat="server" DataField="UOM" Enabled="False" />
                                 </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                         <AutoSize Enabled="True" />
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Pick" VisibleExp="DataControls[&quot;chkShowPick&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="gridPicked" runat="server" DataSourceID="ds" SyncPosition="true" Width="100%" SkinID="Inquire" OnRowDataBound="PickGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="Picked">
                                <Columns>
                                    <px:PXGridColumn DataField="Fits" Type="CheckBox" />
                                    <px:PXGridColumn DataField="LineNbr" />
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderType" />
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderNbr" LinkCommand="ViewOrder"/>
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="LocationID" />
                                    <px:PXGridColumn DataField="InventoryID" />
                                    <px:PXGridColumn DataField="SOShipLine__TranDesc" />
                                    <px:PXGridColumn DataField="LotSerialNbr" />
                                    <px:PXGridColumn DataField="ExpireDate" />
                                    <px:PXGridColumn DataField="CartQty" />
                                    <px:PXGridColumn DataField="OverAllCartQty" />
                                    <px:PXGridColumn DataField="PickedQty" />
                                    <px:PXGridColumn DataField="PackedQty" />
                                    <px:PXGridColumn DataField="Qty" />
                                    <px:PXGridColumn DataField="UOM" />
                                    <px:PXGridColumn DataField="SOShipLine__IsFree" Type="CheckBox" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXNumberEdit ID="edPickLineNbr" runat="server" DataField="LineNbr" Enabled="false"/>
                                    <px:PXTextEdit ID="edPickOrigOrderType" runat="server" DataField="SOShipLine__OrigOrderType" Enabled="False" />
                                    <px:PXSelector ID="edPickOrigOrderNbr" runat="server" DataField="SOShipLine__OrigOrderNbr" Enabled="False"/>
                                    <px:PXSegmentMask ID="edPickInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edPickSiteID" runat="server" DataField="SiteID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edPickLocationID" runat="server" DataField="LocationID" Enabled="False"/>
                                    <px:PXSelector ID="edPickLotSerialNbr" runat="server" DataField="LotSerialNbr" Enabled="False" />
                                    <px:PXDateTimeEdit ID="edPickExpireDate" runat="server" DataField="ExpireDate" Enabled="False" />
                                    <px:PXNumberEdit ID="PXPickPickedQty" runat="server" DataField="PickedQty" Enabled="False"/>
                                    <px:PXNumberEdit ID="PXPickPackedQty" runat="server" DataField="PackedQty" Enabled="False"/>
                                    <px:PXNumberEdit ID="PXPickQty" runat="server" DataField="Qty" Enabled="False"/>
                                    <px:PXSelector ID="edPickUOM" runat="server" DataField="UOM" Enabled="False" />
                                    <px:PXCheckBox ID="chkPickIsFree" runat="server" DataField="SOShipLine__IsFree" Enabled="False" />
                                 </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                         <AutoSize Enabled="True" />
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Return" VisibleExp="DataControls[&quot;chkShowReturn&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="gridReturned" runat="server" DataSourceID="ds" SyncPosition="true" Width="100%" SkinID="Inquire" OnRowDataBound="PickGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="Returned">
                                <Columns>
                                    <px:PXGridColumn DataField="Fits" Type="CheckBox" />
                                    <px:PXGridColumn DataField="LineNbr" />
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderType" />
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderNbr" LinkCommand="ViewOrder"/>
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="LocationID" />
                                    <px:PXGridColumn DataField="InventoryID" />
                                    <px:PXGridColumn DataField="SOShipLine__TranDesc" />
                                    <px:PXGridColumn DataField="LotSerialNbr" />
                                    <px:PXGridColumn DataField="ExpireDate" />
                                    <px:PXGridColumn DataField="PickedQty" />
                                    <px:PXGridColumn DataField="Qty" />
                                    <px:PXGridColumn DataField="UOM" />
                                    <px:PXGridColumn DataField="SOShipLine__IsFree" Type="CheckBox" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXNumberEdit ID="edReturnLineNbr" runat="server" DataField="LineNbr" Enabled="false"/>
                                    <px:PXTextEdit ID="edReturnOrigOrderType" runat="server" DataField="SOShipLine__OrigOrderType" Enabled="False" />
                                    <px:PXSelector ID="edReturnOrigOrderNbr" runat="server" DataField="SOShipLine__OrigOrderNbr" Enabled="False"/>
                                    <px:PXSegmentMask ID="edReturnInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edReturnSiteID" runat="server" DataField="SiteID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edReturnLocationID" runat="server" DataField="LocationID" Enabled="False"/>
                                    <px:PXSelector ID="edReturnLotSerialNbr" runat="server" DataField="LotSerialNbr" Enabled="False" />
                                    <px:PXDateTimeEdit ID="edReturnExpireDate" runat="server" DataField="ExpireDate" Enabled="False" />
                                    <px:PXNumberEdit ID="edReturnPickedQty" runat="server" DataField="PickedQty" Enabled="False"/>
                                    <px:PXNumberEdit ID="edReturnQty" runat="server" DataField="Qty" Enabled="False"/>
                                    <px:PXSelector ID="edReturnUOM" runat="server" DataField="UOM" Enabled="False" />
                                    <px:PXCheckBox ID="edReturnIsFree" runat="server" DataField="SOShipLine__IsFree" Enabled="False" />
                                 </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                         <AutoSize Enabled="True" />
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Pack" VisibleExp="DataControls[&quot;chkShowPack&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="gridPacked" runat="server" DataSourceID="ds" SyncPosition="true" Width="100%" SkinID="Inquire" Height="200px"  OnRowDataBound="PackGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="PickedForPack">
                                <Columns>
                                    <px:PXGridColumn DataField="Fits" Type="CheckBox" />
                                    <px:PXGridColumn DataField="LineNbr" />
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderType" />
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderNbr" LinkCommand="ViewOrder"/>
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="LocationID" />
                                    <px:PXGridColumn DataField="InventoryID" />
                                    <px:PXGridColumn DataField="SOShipLine__TranDesc" />
                                    <px:PXGridColumn DataField="LotSerialNbr" />
                                    <px:PXGridColumn DataField="ExpireDate" />
                                    <px:PXGridColumn DataField="CartQty" />
                                    <px:PXGridColumn DataField="OverAllCartQty" />
                                    <px:PXGridColumn DataField="PickedQty" />
                                    <px:PXGridColumn DataField="PackedQty" />
                                    <px:PXGridColumn DataField="Qty" />
                                    <px:PXGridColumn DataField="UOM" />
                                    <px:PXGridColumn DataField="SOShipLine__IsFree" Type="CheckBox" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXNumberEdit ID="edPackLineNbr" runat="server" DataField="LineNbr" Enabled="false"/>
                                    <px:PXTextEdit ID="edPackOrigOrderType" runat="server" DataField="SOShipLine__OrigOrderType" Enabled="False" />
                                    <px:PXSelector ID="edPackOrigOrderNbr" runat="server" DataField="SOShipLine__OrigOrderNbr" Enabled="False" />
                                    <px:PXSegmentMask ID="edPackInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edPackSiteID" runat="server" DataField="SiteID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edPackLocationID" runat="server" DataField="LocationID" Enabled="False"/>
                                    <px:PXTextEdit ID="edPackLotSerialNbr" runat="server" DataField="LotSerialNbr" Enabled="False" />
                                    <px:PXDateTimeEdit ID="edPackExpireDate" runat="server" DataField="ExpireDate" Enabled="False" />
                                    <px:PXNumberEdit ID="PXPackPickedQty" runat="server" DataField="PickedQty" Enabled="False"/>
                                    <px:PXNumberEdit ID="PXPackPackedQty" runat="server" DataField="PackedQty" Enabled="False"/>
                                    <px:PXNumberEdit ID="PXPackQty" runat="server" DataField="Qty" Enabled="False"/>
                                    <px:PXSelector ID="edPackUOM" runat="server" DataField="UOM" Enabled="False" />
                                    <px:PXCheckBox ID="chkPackIsFree" runat="server" DataField="SOShipLine__IsFree" Enabled="False" />
                                 </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                         <AutoSize Enabled="True" />
                        <AutoSize Container="Window" Enabled="True" MinHeight="200" />
                    </px:PXGrid>
                    <px:PXFormView ID="formBoxPackage" runat="server" DataSourceID="ds" DataMember="HeaderView" RenderStyle="Simple">
                        <Template>
                            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="S" />
                            <px:PXSelector ID="edPackage" runat="server" DataField="PackageLineNbrUI" CommitChanges="True" AutoRefresh="true"/>
                            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" />
                            <px:PXFormView ID="formBoxInfo" runat="server" DataSourceID="ds" DataMember="ShownPackage" RenderStyle="Simple">
                                <Template>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="XS" ControlSize="S" />
                                    <px:PXNumberEdit ID="edPackageWeight" runat="server" DataField="Weight" Enabled="false"/>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="S" />
                                    <px:PXNumberEdit ID="edBoxMaxWeight" runat="server" DataField="MaxWeight" />
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="XXS" ControlSize="XS" />
                                    <px:PXTextEdit ID="edWeightUOM" runat="server" DataField="WeightUOM"/>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="XS" ControlSize="XS" />
                                    <px:PXCheckBox ID="chPackageConfirmed" runat="server" DataField="Confirmed" AlignLeft="True" Enabled="false"/>
                                </Template>
                            </px:PXFormView>
                        </Template>
                    </px:PXFormView>
                    <px:PXGrid ID="gridPackedItems" runat="server" DataSourceID="ds" Width="100%" SkinID="Inquire" Height="200px" Caption="Package Content" CaptionVisible="true" AllowPaging="False" >
                        <Levels>
                            <px:PXGridLevel DataMember="Packed">
                                <Columns>
                                    <px:PXGridColumn DataField="LineNbr" />
                                    <px:PXGridColumn DataField="InventoryID" />
                                    <px:PXGridColumn DataField="SOShipLine__TranDesc" />
                                    <px:PXGridColumn DataField="LotSerialNbr" />
                                    <px:PXGridColumn DataField="PackedQtyPerBox" />
                                    <px:PXGridColumn DataField="Qty" />
                                    <px:PXGridColumn DataField="UOM" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXSegmentMask ID="edPackedItemInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" />
                                 </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="True" />
                        <AutoSize Container="Window" Enabled="True" MinHeight="200" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Ship" VisibleExp="DataControls[&quot;chkShowShip&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXFormView ID="formShipAddress" runat="server" DataMember="Shipping_Address" DataSourceID="ds" AllowCollapse="true" RenderStyle="Simple">
                        <Template>
                            <px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
                            <px:PXTextEdit ID="edAddressLine1" runat="server" DataField="AddressLine1"  Enabled="False"/>
                            <px:PXTextEdit ID="edAddressLine2" runat="server" DataField="AddressLine2"  Enabled="False"/>
                            <px:PXTextEdit ID="edCity" runat="server" DataField="City"  Enabled="False"/>
                            <px:PXLayoutRule runat="server" StartColumn="True" />
                            <px:PXSelector ID="edCountryID" runat="server" DataField="CountryID" AutoRefresh="True" CommitChanges="true"  Enabled="False"/>
                            <px:PXSelector ID="edState" runat="server" DataField="State" AutoRefresh="True" Enabled="False"/>
                            <px:PXMaskEdit ID="edPostalCode" runat="server" DataField="PostalCode" CommitChanges="true"  Enabled="False"/>
                            <px:PXLayoutRule runat="server" StartColumn="True" />
                            <px:PXFormView ID="formShipInfo" runat="server" DataMember="CurrentDocument" DataSourceID="ds" CaptionVisible="False" RenderStyle="Simple">
                                <Template>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
                                    <px:PXNumberEdit ID="edShipmentQty" runat="server" DataField="ShipmentQty" Enabled="False" />
                                    <px:PXNumberEdit ID="edShipmentWeight" runat="server" DataField="ShipmentWeight" Enabled="False" />
                                    <px:PXNumberEdit ID="edShipmentVolume" runat="server" DataField="ShipmentVolume" Enabled="False" />
                                    <px:PXLayoutRule runat="server" StartColumn="True" />
                                    <px:PXNumberEdit ID="edPackageCount" runat="server" DataField="PackageCount" Enabled="False" />
                                    <px:PXNumberEdit ID="edPackageWeight" runat="server" DataField="PackageWeight" Enabled="False" />
                                </Template>
                                <ContentStyle BackColor="Transparent" BorderStyle="None" />
                            </px:PXFormView>
                        </Template>
                        <ContentStyle BackColor="Transparent" BorderStyle="None" />
                    </px:PXFormView>
                    <px:PXGrid ID="gridRates" runat="server" Width="100%" DataSourceID="ds" Caption="Carrier Rates" SkinID="Details" Height="90px" CaptionVisible="True" AllowPaging="False" AllowFilter="False" AutoAdjustColumns="False" >
                        <Mode AllowAddNew="False" AllowDelete="False" AllowFormEdit="False" />
                        <ActionBar Position="Top" PagerVisible="False" CustomItemsGroup="1" ActionsVisible="True">
                            <CustomItems>
                                <px:PXToolBarButton CommandName="RefreshRates" CommandSourceID="ds"/>
                                <px:PXToolBarButton CommandName="GetReturnLabels" CommandSourceID="ds"/>
                            </CustomItems>
                        </ActionBar>
                        <Levels>
                            <px:PXGridLevel DataMember="CarrierRates">
                                <Columns>
                                    <px:PXGridColumn DataField="Selected" Type="CheckBox" CommitChanges="true" TextAlign="Center" />
                                    <px:PXGridColumn DataField="Method" Label="Code" />
                                    <px:PXGridColumn DataField="Description" Label="Description" />
                                    <px:PXGridColumn AllowUpdate="False" DataField="Amount" />
                                    <px:PXGridColumn AllowUpdate="False" DataField="DaysInTransit" Label="Days in Transit" />
                                    <px:PXGridColumn AllowUpdate="False" DataField="DeliveryDate" Label="Delivery Date" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="50" />
                    </px:PXGrid>
                    <px:PXGrid ID="gridPackages" runat="server" Width="100%" DataSourceID="ds" Caption="Packages" SkinID="Details" Height="90px" CaptionVisible="True" AllowPaging="False">
                        <Levels>
                            <px:PXGridLevel DataMember="Packages">
                                <Columns>
                                    <px:PXGridColumn DataField="BoxID" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Description" Label="Description" />
                                    <px:PXGridColumn DataField="WeightUOM" />
                                    <px:PXGridColumn DataField="Weight" />
                                    <px:PXGridColumn DataField="BoxWeight" />
                                    <px:PXGridColumn DataField="NetWeight" />
                                    <px:PXGridColumn DataField="MaxWeight" />
                                    <px:PXGridColumn DataField="DeclaredValue" />
                                    <px:PXGridColumn DataField="COD" />
                                    <px:PXGridColumn DataField="TrackNumber" />
                                    <px:PXGridColumn DataField="StampsAddOns" Type="DropDownList" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXDropDown runat="server" ID="edStampsAddOns" DataField="StampsAddOns" AllowMultiSelect="True" />
                                </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="50" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Scan Log" VisibleExp="DataControls[&quot;chkShowLog&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="grid4" runat="server" DataSourceID="ds" Style="height: 250px;" Width="100%" SkinID="Inquire" Height="372px" TabIndex="-7375" OnRowDataBound="LogGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="Logs">
                                <Columns>
                                    <px:PXGridColumn DataField="ScanTime" />
                                    <px:PXGridColumn DataField="Mode" />
                                    <px:PXGridColumn DataField="Prompt" />
                                    <px:PXGridColumn DataField="Scan" />
                                    <px:PXGridColumn DataField="Message" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
        </Items>
        <AutoSize Enabled="True" Container="Window" />
    </px:PXTab>
    <%-- Settings --%>
    <px:PXSmartPanel ID="PanelSettings" runat="server" Caption="Settings" CaptionVisible="True" ShowAfterLoad="true"
        Key="UserSetupView" AutoCallBack-Command="Refresh" AutoCallBack-Enabled="True" AutoCallBack-Target="frmSettings" CloseButtonDialogResult="Abort">
        <px:PXFormView ID="frmSettings" runat="server" DataSourceID="ds" DataMember="UserSetupView" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule ID="PXLayoutRule1" runat="server" LabelsWidth="M" ControlSize="M" StartGroup="True" SuppressLabel="True" GroupCaption="General"/>
                <px:PXCheckBox ID="edDefaultLocation" runat="server" DataField="DefaultLocationFromShipment" CommitChanges="true" />
                <px:PXCheckBox ID="edDefaultLotSerial" runat="server" DataField="DefaultLotSerialFromShipment" CommitChanges="true" />

                <px:PXLayoutRule ID="PXLayoutRule7" runat="server" LabelsWidth="M" ControlSize="M" StartGroup="True" SuppressLabel="True" GroupCaption="Printing"/>
                <px:PXCheckBox ID="edPrintShipmentConfirmation" runat="server" DataField="PrintShipmentConfirmation" CommitChanges="true" />
                <px:PXCheckBox ID="edPrintShipmentLabels" runat="server" DataField="PrintShipmentLabels" CommitChanges="true" />

                <px:PXLayoutRule ID="PXLayoutRule4" runat="server" LabelsWidth="M" ControlSize="M" StartGroup="True" SuppressLabel="True" GroupCaption="Scale"/>
                <px:PXCheckBox ID="edUseScale" runat="server" DataField="UseScale" CommitChanges="true" />
                <px:PXLayoutRule ID="PXLayoutRule6" runat="server" LabelsWidth="M" ControlSize="M" SuppressLabel="False"/>
                <px:PXSelector ID="edScaleID" runat="server" DataField="ScaleDeviceID" CommitChanges="true" AutoComplete="false" />

                <px:PXLayoutRule ID="PXLayoutRule8" runat="server" LabelsWidth="M" ControlSize="M" StartGroup="True" SuppressLabel="True"/>
                <px:PXCheckBox ID="edEnterSizeForPackages" runat="server" DataField="EnterSizeForPackages" CommitChanges="true" />

            </Template>
        </px:PXFormView>
        <px:PXPanel ID="PXPanel2" runat="server" SkinID="Buttons">
            <px:PXButton ID="pbClose" runat="server" DialogResult="OK" Text="Save"/>
            <px:PXButton ID="pbCancel" runat="server" DialogResult="Abort" Text="Cancel"/>
        </px:PXPanel>
    </px:PXSmartPanel>
</asp:content>