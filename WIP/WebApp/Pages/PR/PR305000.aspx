<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PR305000.aspx.cs" Inherits="Page_PR305000" Title="Direct Deposit Batch" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.PR.PRDirectDepositBatchEntry" PrimaryView="Document" HeaderDescriptionField="HeaderDescription">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="AddPayment" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="DeleteBatchDetails" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="ShowExportDetails" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="Export" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="Pay" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="CancelPayment" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="DisplayPayStubs" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="PrintChecks" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="ExportPrenote" Visible="False" CommitChanges="true" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" DataMember="Document" Style="z-index: 100" Width="100%">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
			<px:PXSelector runat="server" DataField="BatchNbr" ID="edBatchNbr" />
			<px:PXDropDown runat="server" DataField="Filter.PaymentBatchStatus" ID="edPaymentBatchStatus" Enabled="False" />
			<px:PXDateTimeEdit runat="server" DataField="TranDate" ID="edTranDate" CommitChanges="True" />
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
			<px:PXSegmentMask CommitChanges="True" runat="server" DataField="CashAccountID" ID="edCashAccountID" Enabled="False" />
			<px:PXSelector CommitChanges="True" runat="server" DataField="PaymentMethodID" ID="edPaymentMethodID" Enabled="False" />
			<px:PXDateTimeEdit Size="M" runat="server" DataField="LatestExport.ExportDateTime" ID="edExportTime" />
			<px:PXLayoutRule runat="server" ColumnSpan="2" />
			<px:PXTextEdit runat="server" DataField="TranDesc" ID="edTranDesc" />
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="S" />
			<px:PXNumberEdit runat="server" Enabled="False" DataField="Filter.BatchTotal" ID="edBatchTotal" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%">
		<Items>
			<px:PXTabItem Text="Batch Details">
				<Template>
					<px:PXGrid ID="grid" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" Height="150px" SkinID="Inquire" SyncPosition="true">
						<Levels>
							<px:PXGridLevel DataMember="BatchPaymentsDetails">
								<Columns>
									<px:PXGridColumn DataField="OrigDocType" />
									<px:PXGridColumn DataField="PRPayment__RefNbr" LinkCommand="ViewPRDocument" />
									<px:PXGridColumn DataField="PRPayment__Status" />
									<px:PXGridColumn DataField="PRPayment__DocDesc" />
									<px:PXGridColumn DataField="PRPayment__EmployeeID" />
									<px:PXGridColumn DataField="PRPayment__PayGroupID" />
									<px:PXGridColumn DataField="PRPayment__PayPeriodID" />
									<px:PXGridColumn DataField="PRDirectDepositSplit__Amount" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Container="Window" Enabled="True" MinHeight="150" />
						<ActionBar>
							<CustomItems>
								<px:PXToolBarButton DependOnGrid="grid" CommandSourceID="ds" ImageKey="AddNew" DisplayStyle="Image">
									<AutoCallBack Command="AddPayment" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton DependOnGrid="grid" CommandSourceID="ds" ImageKey="RecordDel" DisplayStyle="Image">
									<AutoCallBack Command="DeleteBatchDetails" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Export History">
				<Template>
					<px:PXGrid ID="exportHistoryGrid" SyncPosition="true" SkinID="Inquire" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" Height="150px">
						<Levels>
							<px:PXGridLevel DataMember="ExportHistory">
								<Columns>
									<px:PXGridColumn DataField="UserID_Description" Width="200px" />
									<px:PXGridColumn DataField="ExportDateTime" DisplayFormat="g" Width="200px" />
									<px:PXGridColumn DataField="Reason" />
									<px:PXGridColumn DataField="BatchTotal" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<ActionBar>
							<CustomItems>
								<px:PXToolBarButton Text="Export Details" DependOnGrid="exportHistoryGrid" CommandSourceID="ds">
									<AutoCallBack Command="ShowExportDetails" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<AutoSize Container="Window" Enabled="True" MinHeight="150" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXTab>
	<px:PXSmartPanel runat="server" ID="spnlAddPayment" Caption="Add Payment" CaptionVisible="true" Key="PaymentsToAdd" AutoRepaint="True">
		<px:PXGrid ID="addPaymentGrid" runat="server" DataSourceID="ds" Style="z-index: 100" Height="150px" SkinID="Inquire" SyncPosition="true">
			<Levels>
				<px:PXGridLevel DataMember="PaymentsToAdd">
					<Columns>
						<px:PXGridColumn DataField="RefNbr" />
						<px:PXGridColumn DataField="EmployeeID" />
						<px:PXGridColumn DataField="EmployeeID_description" />
						<px:PXGridColumn DataField="PayGroupID" />
						<px:PXGridColumn DataField="PayPeriodID" />
						<px:PXGridColumn DataField="TransactionDate" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Container="Parent" Enabled="True" MinHeight="150" />
		</px:PXGrid>
		<px:PXPanel ID="pnlAddPayment" runat="server" SkinID="Buttons">
			<px:PXButton ID="btnOK" runat="server" CommandSourceID="ds" DialogResult="OK" Text="Add" AlignLeft="false" />
			<px:PXButton ID="btnCancel" runat="server" CommandSourceID="ds" DialogResult="Cancel" Text="Cancel" AlignLeft="false" />
		</px:PXPanel>
	</px:PXSmartPanel>
	<px:PXSmartPanel runat="server" ID="spnlExportReason" Caption="Export Reason" CaptionVisible="true" Key="ExportHistory" AutoRepaint="True">
		<px:PXFormView DataMember="Filter" SkinID="Transparent" Style="z-index: 100" Width="100%" ID="formExportReason" runat="server" DataSourceID="ds">
			<Template>
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
				<px:PXDropDown runat="server" DataField="ExportReason" ID="edExportReason" CommitChanges="true" />
				<px:PXTextEdit runat="server" DataField="OtherExportReason" ID="edOtherExportReason" CommitChanges="true" />
			</Template>
		</px:PXFormView>
		<px:PXPanel ID="PXPanel1" runat="server" SkinID="Buttons">
			<px:PXButton ID="PXButton1" runat="server" CommandSourceID="ds" DialogResult="OK" Text="OK" AlignLeft="false" />
		</px:PXPanel>
	</px:PXSmartPanel>
	<px:PXSmartPanel runat="server" ID="spnlPrintChecks" Caption="Print Checks" CaptionVisible="true" Key="Payments" AutoRepaint="True">
		<px:PXFormView DataMember="Filter" SkinID="Transparent" Style="z-index: 100" Width="100%" ID="formPrintChecks" runat="server" DataSourceID="ds">
			<Template>
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
				<px:PXDropDown runat="server" DataField="PrintReason" ID="edPrintReason" CommitChanges="true" />
				<px:PXTextEdit runat="server" DataField="OtherPrintReason" ID="edOtherPrintReason" CommitChanges="true" />
				<px:PXTextEdit runat="server" DataField="NextCheckNbr" ID="edNextCheckNbr" />
			</Template>
		</px:PXFormView>
		<px:PXGrid ID="gridChecksToPrint" runat="server" DataSourceID="ds" Style="z-index: 100" Height="150px" SkinID="Inquire">
			<Levels>
				<px:PXGridLevel DataMember="Payments">
					<Columns>
						<px:PXGridColumn DataField="Selected" Type="CheckBox" TextAlign="Center" AllowCheckAll="true" />
						<px:PXGridColumn DataField="DocType" />
						<px:PXGridColumn DataField="RefNbr" />
						<px:PXGridColumn DataField="Status" />
						<px:PXGridColumn DataField="DocDesc" />
						<px:PXGridColumn DataField="EmployeeID" />
						<px:PXGridColumn DataField="EmployeeID_description" />
						<px:PXGridColumn DataField="PayGroupID" />
						<px:PXGridColumn DataField="PayPeriodID" />
						<px:PXGridColumn DataField="NetAmount" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Container="Parent" Enabled="True" MinHeight="150" />
		</px:PXGrid>
		<px:PXPanel ID="PXPanel2" runat="server" SkinID="Buttons">
			<px:PXButton ID="PXButton2" runat="server" CommandSourceID="ds" DialogResult="OK" Text="OK" AlignLeft="false" />
		</px:PXPanel>
	</px:PXSmartPanel>
	<px:PXSmartPanel runat="server" ID="spnlExportDetails" Caption="Export Details" CaptionVisible="true" Key="ExportDetails" AutoRepaint="True">
		<px:PXFormView DataMember="CurrentExportHistory" SkinID="Transparent" Style="z-index: 100" Width="100%" ID="formExportDetails" runat="server" DataSourceID="ds">
			<Template>
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
				<px:PXTextEdit DataField="UserID_Description" ID="edUserID" Width="200px" runat="server" />
				<px:PXTextEdit DataField="ExportDateTime" ID="edExportDateTime" DisplayFormat="g" Width="200px" runat="server" />
				<px:PXTextEdit runat="server" DataField="Reason" ID="edReason" />
			</Template>
		</px:PXFormView>
		<px:PXGrid ID="gridExportDetails" runat="server" DataSourceID="ds" Style="z-index: 100" Height="150px" SkinID="Inquire">
			<Levels>
				<px:PXGridLevel DataMember="ExportDetails">
					<Columns>
						<px:PXGridColumn DataField="DocType" />
						<px:PXGridColumn DataField="RefNbr" />
						<px:PXGridColumn DataField="DocDesc" />
						<px:PXGridColumn DataField="EmployeeID" />
						<px:PXGridColumn DataField="EmployeeID_description" />
						<px:PXGridColumn DataField="PayGroupID" />
						<px:PXGridColumn DataField="PayPeriodID" />
						<px:PXGridColumn DataField="NetAmount" />
						<px:PXGridColumn DataField="ExtRefNbr" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Container="Parent" Enabled="True" MinHeight="150" />
		</px:PXGrid>
		<px:PXPanel ID="PXPanel3" runat="server" SkinID="Buttons">
			<px:PXButton ID="PXButton3" runat="server" CommandSourceID="ds" DialogResult="Cancel" Text="OK" AlignLeft="false" />
		</px:PXPanel>
	</px:PXSmartPanel>
</asp:Content>
