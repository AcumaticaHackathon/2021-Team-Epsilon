<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AM205000.aspx.cs" Inherits="Page_AM205000" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" Visible = "True" PrimaryView="ShiftRecords" TypeName="PX.Objects.AM.ShiftMaint" >
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Save" CommitChanges="True" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
	<px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" AllowPaging="True" AllowSearch="true" AdjustPageSize="Auto" DataSourceID="ds" SkinID="Primary">
		<Levels>
			<px:PXGridLevel DataKeyNames="ShiftID" DataMember="ShiftRecords">
                <RowTemplate>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                    <px:PXMaskEdit ID="edShiftID" runat="server" DataField="ShiftID" />
                    <px:PXTextEdit ID="edShftDesc" runat="server" DataField="ShftDesc" />
                    <px:PXDropDown ID="edDiffType" runat="server" DataField="DiffType" SelectedIndex="-1"/>
                    <px:PXNumberEdit ID="edShftDiff" runat="server" DataField="ShftDiff" />
                    <px:PXNumberEdit ID="edCrewSize" runat="server" DataField="CrewSize" />
                </RowTemplate>
                <Columns>
                    <px:PXGridColumn AutoCallBack="True" DataField="ShiftID" Label="Shift" Width="90px" />
                    <px:PXGridColumn DataField="ShftDesc" Width="180px" />
                    <px:PXGridColumn DataField="DiffType" RenderEditorText="True" Width="80px" />
                    <px:PXGridColumn DataField="ShftDiff" TextAlign="Right" Width="117px" />
                    <px:PXGridColumn DataField="CrewSize" TextAlign="Right" Width="117px" />
                </Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="200" />
		<ActionBar ActionsText="False">
		</ActionBar>
    </px:PXGrid>
</asp:Content>
