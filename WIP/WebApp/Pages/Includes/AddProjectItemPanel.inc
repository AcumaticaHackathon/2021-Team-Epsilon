<px:PXSmartPanel ID="AddProjectItemPanel" runat="server" Height="396px" Width="850px" Caption="Add Project Items" CaptionVisible="True" Key="AvailableProjectItems" AutoCallBack-Command="Refresh"
        AutoCallBack-Enabled="True" AutoCallBack-Target="AvailableProjectItemsGrid" LoadOnDemand="true" AutoRepaint="true">
        <px:PXFormView ID="formProjectItemFilter" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="ProjectItemFilter" DefaultControlID="edProjectID" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True"/>
                <px:PXSegmentMask CommitChanges="True" ID="edProjectID" runat="server" DataField="ProjectID" AllowEdit="True"/>
            </Template>
        </px:PXFormView>
        <px:PXGrid ID="AvailableProjectItemsGrid" runat="server" Height="240px" Width="100%" DataSourceID="ds" SkinID="Details" SyncPosition="true">
            <AutoSize Enabled="true" />
            <Levels>
                <px:PXGridLevel DataMember="AvailableProjectItems">
                    <Columns>
                        <px:PXGridColumn DataField="Selected" Label="Selected" Type="CheckBox" AllowCheckAll="true" />
                        <px:PXGridColumn AutoCallBack="True" DataField="ProjectTaskID" />
                        <px:PXGridColumn AutoCallBack="True" DataField="InventoryID" />
                        <px:PXGridColumn AutoCallBack="True" DataField="CostCodeID" />
                        <px:PXGridColumn AutoCallBack="True" DataField="AccountGroupID" />
                        <px:PXGridColumn DataField="Description" />
                        <px:PXGridColumn AutoCallBack="True" DataField="UOM" />
                        <px:PXGridColumn DataField="CuryUnitRate" TextAlign="Right" CommitChanges="true" />
                        <px:PXGridColumn DataField="Qty" TextAlign="Right" CommitChanges="true" />
                        <px:PXGridColumn DataField="CuryAmount" TextAlign="Right" CommitChanges="true" />
                        <px:PXGridColumn DataField="RevisedQty" TextAlign="Right" CommitChanges="true" />
                        <px:PXGridColumn DataField="CuryRevisedAmount" TextAlign="Right" CommitChanges="true" />
                        <px:PXGridColumn DataField="ChangeOrderQty" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryChangeOrderAmount" TextAlign="Right" />
                        <px:PXGridColumn DataField="CommittedQty" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryCommittedAmount" TextAlign="Right" />
                        <px:PXGridColumn DataField="CommittedReceivedQty" TextAlign="Right" />
                        <px:PXGridColumn DataField="CommittedInvoicedQty" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryCommittedInvoicedAmount" TextAlign="Right" />
                        <px:PXGridColumn DataField="CommittedOpenQty" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryCommittedOpenAmount" TextAlign="Right" />
                        <px:PXGridColumn DataField="ActualQty" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryActualAmount" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryActualPlusOpenCommittedAmount" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryVarianceAmount" TextAlign="Right" />
                        <px:PXGridColumn DataField="Performance" TextAlign="Right" />
                        <px:PXGridColumn DataField="IsProduction" AutoCallBack="True" TextAlign="Center" Type="CheckBox" />
                        <px:PXGridColumn DataField="CuryLastCostToComplete" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryCostToComplete" TextAlign="Right" />
                        <px:PXGridColumn DataField="LastPercentCompleted" TextAlign="Right" />
                        <px:PXGridColumn DataField="PercentCompleted" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryLastCostAtCompletion" TextAlign="Right" />
                        <px:PXGridColumn DataField="CuryCostAtCompletion" TextAlign="Right" />
                        <px:PXGridColumn DataField="RevenueTaskID" AutoCallBack="True" />
                        <px:PXGridColumn DataField="RevenueInventoryID" AutoCallBack="True" />
                    </Columns>
                </px:PXGridLevel>
            </Levels>
            <ActionBar>
                <Actions>
                    <AddNew MenuVisible="False" ToolBarVisible="False" />
                    <Delete MenuVisible="False" ToolBarVisible="Top" />
                    <NoteShow MenuVisible="False" ToolBarVisible="False" />
                </Actions>
            </ActionBar>
            <Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
        </px:PXGrid>
         <px:PXPanel ID="PXPanelBtn" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButtonAdd" runat="server" Text="Add Lines" CommandName="AppendSelectedProjectItems"  CommandSourceID="ds" />
            <px:PXButton ID="PXButtonAddClose" runat="server" Text="Add Lines & Close" DialogResult="OK"  />
            <px:PXButton ID="PXButtonClose" runat="server" DialogResult="Cancel" Text="Close" />      
        </px:PXPanel>
    </px:PXSmartPanel>