<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PM209800.aspx.cs"
	Inherits="Page_PM209800" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
		<script type="text/javascript">
			function refreshChildGrids(e, args) {
				if (args.oldRow != null && args.oldRow.element.id != args.row.element.id) {
					px_alls.costCodesGrid.refresh();
					px_alls.projectTasksGrid.refresh();
					px_alls.laborItemsGrid.refresh();
				}
			}

			function initChildGrids() {
				px_alls.costCodesGrid.refresh();
				px_alls.projectTasksGrid.refresh();
				px_alls.laborItemsGrid.refresh();
			}
		</script>

	<px:PXDataSource ID="ds" Width="100%" runat="server" TypeName="PX.Objects.PM.WorkCodeMaint" PrimaryView="Items" Visible="True">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Save" CommitChanges="True" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
	<px:PXSplitContainer ID="splitContainerWorkCodes" runat="server" PositionInPercent="true" SplitterPosition="40" Orientation="Vertical" Height="100%">
		<Template1>
			<px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" Style="z-index: 100" AllowPaging="True" AllowSearch="true"
				AdjustPageSize="Auto" DataSourceID="ds" SkinID="Details" SyncPosition="true" FastFilterFields="Description">
				<Levels>
					<px:PXGridLevel DataMember="Items">
						<Columns>
							<px:PXGridColumn DataField="IsActive" Type="CheckBox" />
							<px:PXGridColumn DataField="WorkCodeID" />
							<px:PXGridColumn DataField="Description" />
						</Columns>
					</px:PXGridLevel>
				</Levels>
				<AutoSize Container="Window" Enabled="True" MinHeight="200" />
				<Mode AllowUpload="true" />
				<ClientEvents AfterRowChange="refreshChildGrids" AfterRefresh="initChildGrids" />
			</px:PXGrid>
		</Template1>
		<Template2>
			<px:PXLabel runat="server" Text="Sources" cssClass="GridCaption" Width="100%" />
			<px:PXTab ID="tabSources" runat="server" Width="100%">
				<Items>
					<px:PXTabItem Text="Project Tasks">
						<Template>
							<px:PXGrid ID="projectTasksGrid" runat="server" Width="100%" AllowPaging="False" DataSourceID="ds" SkinID="Details">
								<Levels>
									<px:PXGridLevel DataMember="ProjectTaskSources">
										<Columns>
											<px:PXGridColumn DataField="ProjectID" CommitChanges="true" />
											<px:PXGridColumn DataField="ProjectTaskID" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
							</px:PXGrid>
						</Template>
					</px:PXTabItem>
					<px:PXTabItem Text="Labor Items">
						<Template>
							<px:PXGrid ID="laborItemsGrid" runat="server" Width="100%" AllowPaging="False" DataSourceID="ds" SkinID="Details">
								<Levels>
									<px:PXGridLevel DataMember="LaborItemSources">
										<Columns>
											<px:PXGridColumn DataField="LaborItemID" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
							</px:PXGrid>
						</Template>
					</px:PXTabItem>
					<px:PXTabItem Text="Cost Codes">
						<Template>
							<px:PXGrid ID="costCodesGrid" runat="server" Width="100%" AllowPaging="False" DataSourceID="ds" SkinID="Details">
								<Levels>
									<px:PXGridLevel DataMember="CostCodeRanges">
										<Columns>
											<px:PXGridColumn DataField="CostCodeFrom" Width="120px" />
											<px:PXGridColumn DataField="CostCodeTo" Width="120px" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
							</px:PXGrid>
						</Template>
					</px:PXTabItem>
				</Items>
				<AutoSize Container="Window" Enabled="True" MinHeight="200" />
			</px:PXTab>
		</Template2>
		<AutoSize Container="Window" Enabled="True" MinHeight="200" />
	</px:PXSplitContainer>
</asp:Content>
