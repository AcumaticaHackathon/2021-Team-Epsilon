<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CA307000.aspx.cs" Inherits="Page_CA307000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%"
		TypeName="PX.Objects.CA.CCBatchMaint" HeaderDescriptionField="Description"
		PrimaryView="BatchView">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="CreateDeposit" PostData="Self" RepaintControls="None" RepaintControlsIDs="ds,form,FormActions" />
			<px:PXDSCallbackCommand DependOnGrid="grdAllTransactions" Name="Unhide" Visible="false" RepaintControls="None" RepaintControlsIDs="ds,form,FormActions,grdMissingTransactions,grdAllTransactions" />
			<px:PXDSCallbackCommand DependOnGrid="grdMissingTransactions" Name="Hide" Visible="false" RepaintControls="None" RepaintControlsIDs="ds,form,FormActions,grdMissingTransactions,grdAllTransactions" />
			<px:PXDSCallbackCommand DependOnGrid="grdMissingTransactions" Name="RepeatMatching" Visible="false" RepaintControlsIDs="ds,form,FormActions,grdMissingTransactions,grdAllTransactions" />
			<px:PXDSCallbackCommand DependOnGrid="grdMissingTransactions" Name="Record" CommitChanges="True" Visible="false" RepaintControls="None" RepaintControlsIDs="ds,form,FormActions,grdMissingTransactions,grdAllTransactions" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="BatchView" Caption="Settlement Batches" NoteIndicator="True" FilesIndicator="True" ActivityIndicator="True"
		ActivityField="NoteActivity" TabIndex="30100">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="M" />
			<px:PXSelector ID="edBatchID" runat="server" DataField="BatchID" AutoRefresh="True" DataSourceID="ds" />
			<px:PXDropDown ID="edStatus" runat="server" AllowNull="False" DataField="Status" Enabled="False" />
			<px:PXTextEdit ID="edProcCenterID" runat="server" AllowNull="False" DataField="ProcessingCenterID" Enabled="False" />
			<px:PXTextEdit ID="edExtBatchID" runat="server" AllowNull="False" DataField="ExtBatchID" Enabled="False" />
			<px:PXDateTimeEdit ID="dtSettlementTime" runat="server" AllowNull="False" DataField="SettlementTime" Size="M" Enabled="False" />
			<px:PXDropDown ID="edSettlementState" runat="server" AllowNull="False" DataField="SettlementState" Enabled="False" />
			<px:PXSelector ID="edDepositNbr" runat="server" DataField="DepositNbr" AllowEdit="true" Enabled="false" />
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
			<px:PXNumberEdit ID="neSettledAmount" runat="server" AllowNull="False" DataField="SettledAmount" Enabled="False" />
			<px:PXNumberEdit ID="neSettledCount" runat="server" AllowNull="False" DataField="SettledCount" Enabled="False" />
			<px:PXNumberEdit ID="neRefundAmount" runat="server" AllowNull="False" DataField="RefundAmount" Enabled="False" />
			<px:PXNumberEdit ID="neRefundCount" runat="server" AllowNull="False" DataField="RefundCount" Enabled="False" />
			<px:PXNumberEdit ID="neVoidedCount" runat="server" AllowNull="False" DataField="VoidCount" Enabled="False" />
			<px:PXNumberEdit ID="neDeclineCount" runat="server" AllowNull="False" DataField="DeclineCount" Enabled="False" />
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="S" />
			<px:PXNumberEdit ID="neTransactionCount" runat="server" AllowNull="False" DataField="TransactionCount" Enabled="False" />
			<px:PXNumberEdit ID="neImportedCount" runat="server" AllowNull="False" DataField="ImportedTransactionCount" Enabled="False"  />
			<px:PXNumberEdit ID="neProcessedCount" runat="server" AllowNull="False" DataField="ProcessedCount" Enabled="False" />
			<px:PXNumberEdit ID="neMissingCount" runat="server" AllowNull="False" DataField="MissingCount" Enabled="False" />
			<px:PXNumberEdit ID="neHiddenCount" runat="server" AllowNull="False" DataField="HiddenCount" Enabled="False" />
			<px:PXNumberEdit ID="neExcludedCount" runat="server" AllowNull="False" DataField="ExcludedCount" Enabled="False" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100" Width="100%" AllowAutoHide="false"
	SelectedIndexExpr='<%#
		((PXGrid)ControlHelper.FindControl("grdMissingTransactions", Page))?.Rows?.Count > 0 ? 1 :
		((PXGrid)ControlHelper.FindControl("grdExcludedTransactions", Page))?.Rows?.Count > 0 ? 2 : 0
		%>'>
		<Items>
			<px:PXTabItem Text="All Transactions" LoadOnDemand="False" RepaintOnDemand="False" >
				<Template>
					<px:PXGrid ID="grdAllTransactions" runat="server" DataSourceID="ds" Height="100%" Width="100%" SkinID="DetailsInTab" SyncPosition="true" OnRowDataBound="Tran_RowDataBound" >
						<Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
						<AutoSize Enabled="True" />
						<CallbackCommands>
							<Save RepaintControls="None" RepaintControlsIDs="ds,form,FormActions,grdMissingTransactions,grdAllTransactions" />
						</CallbackCommands>
						<ActionBar>
							<Actions>
								<Save Enabled="False" />
								<AddNew Enabled="False" />
								<Delete Enabled="False" />
								<EditRecord Enabled="False" />
							</Actions>
							<CustomItems>
								<px:PXToolBarButton Text="Unhide">
									<AutoCallBack Command="Unhide" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="Transactions">
								<Columns>
									<px:PXGridColumn DataField="SelectedToUnhide" AllowCheckAll="True" RenderEditorText="True" TextAlign="Center" Type="CheckBox" AutoCallBack="True" />
									<px:PXGridColumn DataField="PCTranNumber" />
									<px:PXGridColumn DataField="SettlementStatus" />
									<px:PXGridColumn DataField="ProcessingStatus" />
									<px:PXGridColumn DataField="Amount" />
									<px:PXGridColumn DataField="DocType" />
									<px:PXGridColumn DataField="RefNbr" LinkCommand="viewPaymentAll" />
									<px:PXGridColumn DataField="ARPayment__Status" />
									<px:PXGridColumn DataField="AccountNumber" />
									<px:PXGridColumn DataField="InvoiceNbr" />
									<px:PXGridColumn DataField="SubmitTime" />
									<px:PXGridColumn DataField="CardType" />
									<px:PXGridColumn DataField="FixedFee" />
									<px:PXGridColumn DataField="PercentageFee" />
									<px:PXGridColumn DataField="TotalFee" />
									<px:PXGridColumn DataField="FeeType" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Missing Transactions" LoadOnDemand="False" RepaintOnDemand="False" >
				<Template>
					<px:PXGrid ID="grdMissingTransactions" runat="server" DataSourceID="ds" Height="100%" Width="100%" SkinID="DetailsInTab" SyncPosition="true" >
						<Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
						<AutoSize Enabled="True" />
						<CallbackCommands>
							<Save RepaintControls="None" RepaintControlsIDs="ds,form,FormActions,grdMissingTransactions,grdAllTransactions" />
						</CallbackCommands>
						<ActionBar>
							<Actions>
								<Save Enabled="False" />
								<AddNew Enabled="False" />
								<Delete Enabled="False" />
								<EditRecord Enabled="False" />
							</Actions>
							<CustomItems>
								<px:PXToolBarButton Text="Record">
									<AutoCallBack Command="Record" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Text="Hide" >
									<AutoCallBack Command="Hide" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Text="Match">
									<AutoCallBack Command="RepeatMatching" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="MissingTransactions">
								<Columns>
									<px:PXGridColumn DataField="SelectedToHide" AllowCheckAll="True" RenderEditorText="True" TextAlign="Center" Type="CheckBox" AutoCallBack="True" />
									<px:PXGridColumn DataField="PCTranNumber" />
									<px:PXGridColumn DataField="SettlementStatus" />
									<px:PXGridColumn DataField="Amount" />
									<px:PXGridColumn DataField="AccountNumber" />
									<px:PXGridColumn DataField="InvoiceNbr" />
									<px:PXGridColumn DataField="SubmitTime" />
									<px:PXGridColumn DataField="CardType" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Excluded from Deposit" LoadOnDemand="False" RepaintOnDemand="False" >
				<Template>
					<px:PXGrid ID="grdExcludedTransactions" runat="server" DataSourceID="ds" Height="100%" Width="100%" SkinID="DetailsInTab" SyncPosition="true" >
						<Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
						<AutoSize Enabled="True" />
						<ActionBar>
							<Actions>
								<Save Enabled="False" />
								<AddNew Enabled="False" />
								<Delete Enabled="False" />
								<EditRecord Enabled="False" />
							</Actions>
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="ExcludedFromDepositTransactions">
								<Columns>
									<px:PXGridColumn DataField="PCTranNumber" />
									<px:PXGridColumn DataField="SettlementStatus" />
									<px:PXGridColumn DataField="SubmitTime" />
									<px:PXGridColumn DataField="Amount" />
									<px:PXGridColumn DataField="AccountNumber" />
									<px:PXGridColumn DataField="InvoiceNbr" />
									<px:PXGridColumn DataField="DocType" />
									<px:PXGridColumn DataField="RefNbr" LinkCommand="viewPaymentExcl" />
									<px:PXGridColumn DataField="ARPayment__CashAccountID" />
									<px:PXGridColumn DataField="ARPayment__DepositNbr" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Card Type Summary">
				<Template>
					<px:PXGrid ID="grdCardTypeSummary" runat="server" DataSourceID="ds" Height="100%" Width="100%" SkinID="DetailsInTab">
						<Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
						<AutoSize Enabled="True" />
						<ActionBar>
							<Actions>
								<Save Enabled="False" />
								<AddNew Enabled="False" />
								<Delete Enabled="False" />
								<EditRecord Enabled="False" />
							</Actions>
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="CardTypeSummary">
								<RowTemplate>
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
									<px:PXMaskEdit ID="edCardType" runat="server" DataField="CardType" />
									<px:PXNumberEdit ID="neSettledAmount" runat="server" DataField="SettledAmount" />
									<px:PXNumberEdit ID="neSettledCount" runat="server" DataField="SettledCount" />
									<px:PXNumberEdit ID="neRefundAmount" runat="server" DataField="RefundAmount" />
									<px:PXNumberEdit ID="nePXNumberEdit1" runat="server" DataField="RefundCount" />
									<px:PXNumberEdit ID="nePXNumberEdit2" runat="server" DataField="VoidCount" />
									<px:PXNumberEdit ID="neDeclineCount" runat="server" DataField="DeclineCount" />
									<px:PXNumberEdit ID="neErrorCount" runat="server" DataField="ErrorCount" />
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="CardType" />
									<px:PXGridColumn DataField="SettledAmount" />
									<px:PXGridColumn DataField="SettledCount" />
									<px:PXGridColumn DataField="RefundAmount" />
									<px:PXGridColumn DataField="RefundCount" />
									<px:PXGridColumn DataField="VoidCount" />
									<px:PXGridColumn DataField="DeclineCount" />
									<px:PXGridColumn DataField="ErrorCount" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXTab>
</asp:Content>
