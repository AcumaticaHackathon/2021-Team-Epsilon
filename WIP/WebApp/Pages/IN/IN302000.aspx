<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="IN302000.aspx.cs"
    Inherits="Page_IN302000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource EnableAttributes="true" UDFTypeField="Status" ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.IN.INIssueEntry" PrimaryView="issue" HeaderDescriptionField="TranDesc">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="Save" CommitChanges="True" />
            <px:PXDSCallbackCommand Name="Insert" PostData="Self" />
            <px:PXDSCallbackCommand Name="First" PostData="Self" StartNewGroup="True" />
            <px:PXDSCallbackCommand Name="Last" PostData="Self" />
            <px:PXDSCallbackCommand Name="InventorySummary" Visible="False" DependOnGrid="grid" />
            <px:PXDSCallbackCommand CommitChanges="True" Visible="False" Name="LSINTran_generateLotSerial" />
            <px:PXDSCallbackCommand CommitChanges="True" Visible="False" Name="LSINTran_binLotSerial" DependOnGrid="grid" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" Caption="Document Summary" DataMember="issue"
        NoteIndicator="True" FilesIndicator="True" LinkIndicator="True" BPEventsIndicator="True" ActivityIndicator="True" ActivityField="NoteActivity" DefaultControlID="edRefNbr"
        EmailingGraph="PX.Objects.CR.CREmailActivityMaint,PX.Objects">
        <CallbackCommands>
            <Save PostData="Self" />
        </CallbackCommands>
        <Activity HighlightColor="" SelectedColor="" Width="" Height=""></Activity>
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="S" />
            <px:PXSelector ID="edRefNbr" runat="server" DataField="RefNbr" AutoRefresh="True" DataSourceID="ds" />
            <px:PXDropDown ID="edStatus" runat="server" DataField="Status" />
            <px:PXDateTimeEdit CommitChanges="True" ID="edTranDate" runat="server" DataField="TranDate" />
            <px:PXSelector CommitChanges="True" ID="edFinPeriodID" runat="server" DataField="FinPeriodID" DataSourceID="ds" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
            <px:PXTextEdit ID="edExtRefNbr" runat="server" DataField="ExtRefNbr" />
			<px:PXLayoutRule ID="PXLayoutRule1" runat="server" ColumnSpan="2" />
            <px:PXTextEdit ID="edTranDesc" runat="server" DataField="TranDesc" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
            <px:PXPanel ID="PXPanel1" runat="server" ContentLayout-Layout="Stack" RenderSimple="True" RenderStyle="Simple">
                <px:PXNumberEdit ID="edTotalQty" runat="server" DataField="TotalQty" Enabled="False" />
                <px:PXNumberEdit ID="edControlQty" runat="server" CommitChanges="True" DataField="ControlQty" />
                <px:PXNumberEdit ID="edTotalAmount" runat="server" DataField="TotalAmount" Enabled="False" />
                <px:PXNumberEdit ID="edControlAmount" runat="server" CommitChanges="True" DataField="ControlAmount" />
                <px:PXNumberEdit ID="edTotalCost" runat="server" DataField="TotalCost" Enabled="false" />
            </px:PXPanel>
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Height="216px" Style="z-index: 100;" Width="100%" TabIndex="23">
		<Activity HighlightColor="" SelectedColor="" Width="" Height=""></Activity>
		<Items>
			<px:PXTabItem Text="Details">
				<Template>
					<px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="250px" Style="z-index: 100; left: 0px;
						top: 0px;" Width="100%" SkinID="DetailsInTab" StatusField="Availability" SyncPosition="True">
						<AutoSize Enabled="True" MinHeight="150" />
						<Mode InitNewRow="True" AllowUpload="True" />
						<ActionBar>
							<CustomItems>
								<px:PXToolBarButton Text="Line Details" Key="cmdLS" CommandName="LSINTran_binLotSerial" CommandSourceID="ds"
									DependOnGrid="grid" />
							    <px:PXToolBarButton Text="Add Items" Key="cmdASI" >
									<AutoCallBack Command="AddInvBySite" Target="ds">
										<Behavior PostData="Page" CommitChanges="True" />
									</AutoCallBack>
							    </px:PXToolBarButton>
								<px:PXToolBarButton Text="Inventory Summary">
									<AutoCallBack Command="InventorySummary" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="transactions">
								<RowTemplate>
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
									<px:PXSegmentMask Size="xs" ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True">
										<GridProperties>
											<PagerSettings Mode="NextPrevFirstLast" />
										</GridProperties>
									</px:PXSegmentMask>
									<px:PXSegmentMask CommitChanges="True" ID="edProjectID" runat="server" DataField="ProjectID" />
									<px:PXSegmentMask Size="xxs" ID="edSubItemID" runat="server" DataField="SubItemID" AutoRefresh="True"
										NullText="<SPLIT>">
										<Parameters>
											<px:PXControlParam ControlID="grid" Name="INTran.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]"
												Type="String" />
										</Parameters>
									</px:PXSegmentMask>
									<px:PXSegmentMask ID="edTaskID" runat="server" DataField="TaskID" AutoRefresh="true">
										<Parameters>
											<px:PXSyncGridParam ControlID="grid" />
										</Parameters>
									</px:PXSegmentMask>
                                            <px:PXSegmentMask ID="edCostCodeID" runat="server" DataField="CostCodeID" AutoRefresh="True" />
									<px:PXSegmentMask ID="edSiteID" runat="server" DataField="SiteID" AutoRefresh="True">
										<Parameters>
											<px:PXControlParam ControlID="grid" Name="INTran.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]"
												Type="String" />
											<px:PXControlParam ControlID="grid" Name="INTran.subItemID" PropertyName="DataValues[&quot;SubItemID&quot;]"
												Type="String" />
										</Parameters>
									</px:PXSegmentMask>
									<px:PXSegmentMask ID="edLocationID" runat="server" DataField="LocationID" AutoRefresh="True" NullText="<SPLIT>">
										<Parameters>
											<px:PXControlParam ControlID="grid" Name="INTran.siteID" PropertyName="DataValues[&quot;SiteID&quot;]"
												Type="String" />
											<px:PXControlParam ControlID="grid" Name="INTran.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]"
												Type="String" />
											<px:PXControlParam ControlID="grid" Name="INTran.subItemID" PropertyName="DataValues[&quot;SubItemID&quot;]"
												Type="String" />
										</Parameters>
									</px:PXSegmentMask>
									<px:PXNumberEdit ID="edQty" runat="server" DataField="Qty" />
									<px:PXSelector ID="edUOM" runat="server" DataField="UOM" AutoRefresh="True">
										<Parameters>
											<px:PXControlParam ControlID="grid" Name="INTran.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]"
												Type="String" />
										</Parameters>
									</px:PXSelector>
									<px:PXNumberEdit ID="edUnitPrice" runat="server" DataField="UnitPrice" />
									<px:PXNumberEdit ID="edTranAmt" runat="server" DataField="TranAmt" />
									<px:PXSelector ID="edLotSerialNbr" runat="server" DataField="LotSerialNbr" NullText="<SPLIT>" AutoRefresh="True">
										<Parameters>
											<px:PXControlParam ControlID="grid" Name="INTran.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]"
												Type="String" />
											<px:PXControlParam ControlID="grid" Name="INTran.subItemID" PropertyName="DataValues[&quot;SubItemID&quot;]"
												Type="String" />
											<px:PXControlParam ControlID="grid" Name="INTran.locationID" PropertyName="DataValues[&quot;LocationID&quot;]"
												Type="String" />
											<px:PXSyncGridParam ControlID="grid" Name="SyncGrid" />
										</Parameters>
									</px:PXSelector>
									<px:PXDateTimeEdit ID="edExpireDate" runat="server" DataField="ExpireDate" DisplayFormat="d" />
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
									<px:PXTextEdit ID="edTranDesc" runat="server" DataField="TranDesc" />
									<px:PXSelector ID="edSOShipmentNbr" runat="server" AllowEdit="True" DataField="SOShipmentNbr" />
									<px:PXSelector ID="edSOOrderType" runat="server" DataField="SOOrderType" />
									<px:PXSelector ID="edSOOrderNbr" runat="server" DataField="SOOrderNbr" AllowEdit="True" />
                                    <px:PXSelector ID="edPOReceiptNbr" runat="server" DataField="POReceiptNbr" AllowEdit="True" />
									<px:PXSegmentMask CommitChanges="True" Height="19px" ID="edBranchID" runat="server" DataField="BranchID" />
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
									<px:PXDropDown ID="edTranType" runat="server" DataField="TranType" SelectedIndex="-1" />
									<px:PXSelector ID="edReasonCode" runat="server" DataField="ReasonCode">
										<Parameters>
											<px:PXControlParam ControlID="form" Name="INTran.docType" PropertyName="NewDataKey[&quot;DocType&quot;]"
												Type="String" />
										</Parameters>
									</px:PXSelector>
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="Availability" />
									<px:PXGridColumn DataField="BranchID" DisplayFormat="&gt;AAAAAAAAAA" AutoCallBack="True"
										RenderEditorText="True" />
									<px:PXGridColumn DataField="TranType" RenderEditorText="True" AutoCallBack="True" />
									<px:PXGridColumn DataField="InventoryID" DisplayFormat="&gt;AAAAAAAAAA" AutoCallBack="True" />
									<px:PXGridColumn DataField="SubItemID" DisplayFormat="&gt;AA-A" NullText="<SPLIT>" AutoCallBack="true" />
									<px:PXGridColumn DataField="SiteID" DisplayFormat="&gt;AAAAAAAAAA" AutoCallBack="True" />
									<px:PXGridColumn DataField="LocationID" DisplayFormat="&gt;AAAAAAAAAA" AutoCallBack="true"
										NullText="<SPLIT>" />
									<px:PXGridColumn AllowNull="False" DataField="Qty" TextAlign="Right" />
									<px:PXGridColumn DataField="UOM" DisplayFormat="&gt;aaaaaa" />
									<px:PXGridColumn AllowNull="False" DataField="UnitPrice" TextAlign="Right" />
									<px:PXGridColumn AllowNull="False" DataField="TranAmt" TextAlign="Right" />
									<px:PXGridColumn AllowNull="False" DataField="UnitCost" TextAlign="Right" />
									<px:PXGridColumn AllowNull="False" DataField="TranCost" TextAlign="Right" />
									<px:PXGridColumn DataField="LotSerialNbr" NullText="<SPLIT>" />
									<px:PXGridColumn DataField="ExpireDate" />
									<px:PXGridColumn DataField="ReasonCode" DisplayFormat="&gt;AAAAAAAAAA" />
									<px:PXGridColumn AutoCallBack="True" DataField="ProjectID" DisplayFormat="CCCCCCCCCC" Label="Project" />
									<px:PXGridColumn DataField="TaskID" DisplayFormat="CCCCCCCCCC" Label="Task" CommitChanges="true"/>
									<px:PXGridColumn DataField="CostCodeID" />
									<px:PXGridColumn DataField="TranDesc" />
									<px:PXGridColumn DataField="SOOrderType" />
									<px:PXGridColumn DataField="SOOrderNbr" />
									<px:PXGridColumn DataField="SOShipmentNbr" />
                                    <px:PXGridColumn DataField="POReceiptNbr" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Financial">
				<Template>
					<px:PXFormView ID="form2" runat="server" DataSourceID="ds" Width="100%" 
						DataMember="CurrentDocument" RenderStyle="Simple">
						<ContentLayout OuterSpacing="Around" />
						<Template>
							<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
							<px:PXSelector ID="edBatchNbr" runat="server" DataField="BatchNbr" Enabled="False" AllowEdit="True" />
							<px:PXSegmentMask CommitChanges="True" ID="edBranchID" runat="server" DataField="BranchID" />
                        </Template>
						<AutoSize Enabled="True" />
					</px:PXFormView>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Manufacturing">
				<Template>
					<px:PXFormView runat="server" ID="formam1" DataMember="CurrentDocument" DataSourceID="ds" RenderStyle="Simple">
						<Template>
							<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="M" LabelsWidth="M" />
							<px:PXSelector runat="server" DataField="AMBatNbr" Enabled="False" AllowEdit="True" ID="edAMBatNbr" />
							<px:PXDropDown runat="server" ID="edAMDocType" DataField="AMDocType" Enabled="False" />
						</Template>
					</px:PXFormView>
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="180" />
	</px:PXTab>
	<script type="text/javascript">
        function UpdateItemSiteCell(n, c) {
            var activeRow = c.cell.row;
            var sCell = activeRow.getCell("Selected");
            var qCell = activeRow.getCell("QtySelected");
            if (sCell == c.cell) {
                if (sCell.getValue() == true)
                    qCell.setValue("1");
                else
                    qCell.setValue("0");
            }
            if (qCell == c.cell) {
                if (qCell.getValue() == "0")
                    sCell.setValue(false);
                else
                    sCell.setValue(true);
            }
        }
    </script>
    <px:PXSmartPanel ID="PanelLS" runat="server" Width="764px" Caption="Line Details" DesignView="Content" CaptionVisible="True"
        Key="lsselect" AutoCallBack-Command="Refresh" AutoCallBack-Enabled="True" AutoCallBack-Target="optform" Height="500px">

        <px:PXFormView ID="optform" runat="server" Width="100%" CaptionVisible="False" DataMember="LSINTran_lotseropts" DataSourceID="ds"
            SkinID="Transparent" TabIndex="700">
            <Parameters>
                <px:PXSyncGridParam ControlID="grid" />
            </Parameters>
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                <px:PXNumberEdit ID="edUnassignedQty" runat="server" DataField="UnassignedQty" Enabled="False" />
                <px:PXNumberEdit ID="edQty" runat="server" DataField="Qty">
                    <AutoCallBack>
                        <Behavior CommitChanges="True" />
                    </AutoCallBack>
                </px:PXNumberEdit>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                <px:PXMaskEdit ID="edStartNumVal" runat="server" DataField="StartNumVal" />
                <px:PXButton ID="btnGenerate" runat="server" Text="Generate" Height="20px" CommandName="LSINTran_generateLotSerial" CommandSourceID="ds">
                </px:PXButton>
            </Template>
        </px:PXFormView>
        <px:PXGrid ID="grid2" runat="server" Width="100%" AutoAdjustColumns="True" DataSourceID="ds" SkinID="Details" SyncPosition="true">
            <Mode InitNewRow="True" />
            <AutoSize Enabled="true" />
            <Parameters>
                <px:PXSyncGridParam ControlID="grid" />
            </Parameters>
            <Levels>
                <px:PXGridLevel DataMember="splits">
                    <Columns>
                        <px:PXGridColumn DataField="InventoryID" />
                        <px:PXGridColumn DataField="SubItemID" />
                        <px:PXGridColumn DataField="LocationID" />
                        <px:PXGridColumn DataField="LotSerialNbr" />
                        <px:PXGridColumn DataField="Qty" TextAlign="Right" />
                        <px:PXGridColumn DataField="UOM" />
                        <px:PXGridColumn DataField="ExpireDate" />
                    </Columns>
                    <RowTemplate>
                        <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                        <px:PXSegmentMask ID="edSubItemID2" runat="server" DataField="SubItemID" AutoRefresh="True">
                            <Parameters>
                                <px:PXControlParam ControlID="grid2" Name="INTranSplit.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]" Type="String" />
                            </Parameters>
                        </px:PXSegmentMask>
                        <px:PXSegmentMask ID="edLocationID2" runat="server" DataField="LocationID" AutoRefresh="True">
                            <Parameters>
                                <px:PXControlParam ControlID="grid2" Name="INTranSplit.siteID" PropertyName="DataValues[&quot;SiteID&quot;]" Type="String" />
                                <px:PXControlParam ControlID="grid2" Name="INTranSplit.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]" Type="String" />
                                <px:PXControlParam ControlID="grid2" Name="INTranSplit.subItemID" PropertyName="DataValues[&quot;SubItemID&quot;]" Type="String" />
                            </Parameters>
                        </px:PXSegmentMask>
                        <px:PXNumberEdit ID="edQty2" runat="server" DataField="Qty" />
                        <px:PXSelector ID="edUOM2" runat="server" DataField="UOM" AutoRefresh="True">
                            <Parameters>
                                <px:PXControlParam ControlID="grid" Name="INTran.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]" Type="String" />
                            </Parameters>
                        </px:PXSelector>
                        <px:PXSelector ID="edLotSerialNbr2" runat="server" DataField="LotSerialNbr" AutoRefresh="True">
                            <Parameters>
                                <px:PXControlParam ControlID="grid2" Name="INTranSplit.inventoryID" PropertyName="DataValues[&quot;InventoryID&quot;]" Type="String" />
                                <px:PXControlParam ControlID="grid2" Name="INTranSplit.subItemID" PropertyName="DataValues[&quot;SubItemID&quot;]" Type="String" />
                                <px:PXControlParam ControlID="grid2" Name="INTranSplit.locationID" PropertyName="DataValues[&quot;LocationID&quot;]" Type="String" />
                            </Parameters>
                        </px:PXSelector>
                        <px:PXDateTimeEdit ID="edExpireDate2" runat="server" DataField="ExpireDate" />
                    </RowTemplate>
                    <Layout ColumnsMenu="False" />
                </px:PXGridLevel>
            </Levels>
        </px:PXGrid>
        <px:PXPanel ID="PXPanel1" runat="server" SkinID="Buttons">
            <px:PXButton ID="btnSave" runat="server" DialogResult="OK" Text="OK" />
        </px:PXPanel>
    </px:PXSmartPanel>
    <px:PXSmartPanel ID="PanelAddSiteStatus" runat="server" Key="sitestatus" LoadOnDemand="true" Width="900px" Height="500px"
        Caption="Inventory Lookup" CaptionVisible="true" AutoCallBack-Command='Refresh' AutoCallBack-Enabled="True" AutoCallBack-Target="formSitesStatus"
        DesignView="Content">
        <px:PXFormView ID="formSitesStatus" runat="server" CaptionVisible="False" DataMember="sitestatusfilter" DataSourceID="ds"
            Width="100%" SkinID="Transparent">
            <Activity Height="" HighlightColor="" SelectedColor="" Width="" />
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
                <px:PXTextEdit ID="edInventory" runat="server" DataField="Inventory" />
                <px:PXTextEdit CommitChanges="True" ID="edBarCode" runat="server" DataField="BarCode" />
                <px:PXSegmentMask CommitChanges="True" ID="edItemClassID" runat="server" DataField="ItemClass" />
                <px:PXCheckBox CommitChanges="True" ID="chkOnlyAvailable" runat="server" Checked="True" DataField="OnlyAvailable" />
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
                <px:PXSegmentMask CommitChanges="True" ID="edSubItem" runat="server" DataField="SubItem" AutoRefresh="true" />
                <px:PXSegmentMask CommitChanges="True" ID="edSiteID" runat="server" DataField="SiteID" AutoRefresh="true" />
                <px:PXSegmentMask CommitChanges="True" ID="edLocationID" runat="server" DataField="LocationID" AutoRefresh="true" /></Template>
        </px:PXFormView>
        <px:PXGrid ID="gripSiteStatus" runat="server" DataSourceID="ds"
                   AutoAdjustColumns="true" Width="100%" SkinID="Details" AdjustPageSize="Auto" AllowSearch="True" FastFilterID="edInventory"
            FastFilterFields="InventoryCD,Descr" BatchUpdate="true">
            <ClientEvents AfterCellUpdate="UpdateItemSiteCell" />
            <ActionBar PagerVisible="False">
                <PagerSettings Mode="NextPrevFirstLast" />
            </ActionBar>
            <Levels>
                <px:PXGridLevel DataMember="siteStatus">
                    <Mode AllowAddNew="false" AllowDelete="false" />
                    <RowTemplate>
                        <px:PXSegmentMask ID="editemClass" runat="server" DataField="ItemClassID" />
                    </RowTemplate>
                    <Columns>
                        <px:PXGridColumn AllowNull="False" DataField="Selected" TextAlign="Center" Type="CheckBox" AutoCallBack="true"
                            AllowCheckAll="true" />
                        <px:PXGridColumn AllowNull="False" DataField="QtySelected" TextAlign="Right" />
                        <px:PXGridColumn DataField="SiteID" />
						<px:PXGridColumn DataField="SiteCD" 
							AllowNull="False" SyncNullable ="false" 
							Visible="False" SyncVisible="false" 
							AllowShowHide ="False" SyncVisibility="false" />
                        <px:PXGridColumn DataField="LocationID" />
						<px:PXGridColumn DataField="LocationCD" 
							AllowNull="False" SyncNullable ="false" 
							Visible="False" SyncVisible="false" 
							AllowShowHide ="False" SyncVisibility="false" />
                        <px:PXGridColumn DataField="ItemClassID" />
                        <px:PXGridColumn DataField="ItemClassDescription" />
                        <px:PXGridColumn DataField="PriceClassID" />
                        <px:PXGridColumn DataField="PriceClassDescription" />
                        <px:PXGridColumn DataField="InventoryCD" DisplayFormat="&gt;AAAAAAAAAA" />
                        <px:PXGridColumn DataField="SubItemID" DisplayFormat="&gt;AA-A-A" />
						<px:PXGridColumn DataField="SubItemCD" 
							AllowNull="False" SyncNullable ="false" 
							Visible="False" SyncVisible="false" 
							AllowShowHide ="False" SyncVisibility="false" />
                        <px:PXGridColumn DataField="Descr" />
                        <px:PXGridColumn DataField="BaseUnit" DisplayFormat="&gt;aaaaaa" />
                        <px:PXGridColumn AllowNull="False" DataField="QtyAvail" TextAlign="Right" />
                        <px:PXGridColumn AllowNull="False" DataField="QtyOnHand" TextAlign="Right" />
                    </Columns>
                </px:PXGridLevel>
            </Levels>
            <AutoSize Enabled="true" />
        </px:PXGrid>
        <px:PXPanel ID="PXPanel2" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButton5" runat="server" CommandName="AddInvSelBySite" CommandSourceID="ds" Text="Add" SyncVisible="false"/>
            <px:PXButton ID="PXButton4" runat="server" Text="Add & Close" DialogResult="OK" />
            <px:PXButton ID="PXButton6" runat="server" DialogResult="Cancel" Text="Cancel" />
        </px:PXPanel>
    </px:PXSmartPanel>
</asp:Content>
