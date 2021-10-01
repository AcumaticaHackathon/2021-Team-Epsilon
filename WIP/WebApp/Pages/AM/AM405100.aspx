<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AM405100.aspx.cs" Inherits="Page_AM405100" Title="Work Center Crew Schedule" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.AM.WorkCenterCrewScheduleInq" PrimaryView="Filter">
		<CallbackCommands>
		    <px:PXDSCallbackCommand Name="ViewSchedule" Visible="False" DependOnGrid="grid" />   
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter" DefaultControlID="edWcID" TabIndex="100" >
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True"/>
            <px:PXSelector ID="edWcIDFilter" runat="server" DataField="WcID" CommitChanges="True" />
            <px:PXSelector ID="edShiftIDFilter" runat="server" DataField="ShiftID" CommitChanges="True" />
            <px:PXLayoutRule runat="server" StartColumn="True" />
            <px:PXDateTimeEdit ID="edFromDate" runat="server" DataField="FromDate" CommitChanges="True" />
            <px:PXDateTimeEdit ID="edToDate" runat="server" DataField="ToDate" CommitChanges="True" />
            <px:PXLayoutRule runat="server" StartColumn="True" />
            <px:PXCheckBox ID="edShowAll" runat="server" DataField="ShowAll" AlignLeft="True" CommitChanges="true" />
        </Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" Runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" SkinID="Details" Width="100%" TabIndex="1700">
		<Levels>
			<px:PXGridLevel DataMember="ScheduleDetail">
                <RowTemplate>
                    <px:PXSelector ID="edWcID" runat="server" DataField="WcID" AllowEdit="True"/>
                    <px:PXSelector ID="edShiftID" runat="server" DataField="ShiftID" AllowEdit="True" />
                    <px:PXNumberEdit ID="edSchdBlocks" runat="server" DataField="SchdBlocks" />
                    <px:PXDateTimeEdit ID="edSchdDate" runat="server" DataField="SchdDate" DisplayFormat="t" />
                    <px:PXDateTimeEdit ID="edStartTime" runat="server" DataField="StartTime" DisplayFormat="t" />
                    <px:PXDateTimeEdit ID="edEndTime" runat="server" DataField="EndTime" />
                    <px:PXNumberEdit ID="edCrewSize" runat="server" DataField="CrewSize" />
                    <px:PXNumberEdit ID="edShiftCrewSize" runat="server" DataField="ShiftCrewSize" />
                    <px:PXNumberEdit ID="edCrewSizeShortage" runat="server" DataField="CrewSizeShortage" />
                    <px:PXSelector ID="edOrderType" runat="server" DataField="OrderType" AllowEdit="True" />
                    <px:PXSelector ID="edProdOrdID" runat="server" DataField="ProdOrdID" AllowEdit="True" />
                    <px:PXSelector ID="edOperationID" runat="server" DataField="OperationID" AllowEdit="True" />
                </RowTemplate>
                <Columns>
                    <px:PXGridColumn DataField="WcID" />
                    <px:PXGridColumn DataField="ShiftID" />
                    <px:PXGridColumn DataField="SchdBlocks" TextAlign="Right" Width="90px" />
                    <px:PXGridColumn DataField="SchdDate" Width="90px" LinkCommand="ViewSchedule" />
                    <px:PXGridColumn DataField="StartTime" Width="90px" TimeMode="true" />
                    <px:PXGridColumn DataField="EndTime" Width="90px" TimeMode="true" />
                    <px:PXGridColumn DataField="CrewSize" />
                    <px:PXGridColumn DataField="ShiftCrewSize" />
                    <px:PXGridColumn DataField="CrewSizeShortage" />
                    <px:PXGridColumn DataField="OrderType" />
                    <px:PXGridColumn DataField="ProdOrdID" />
                    <px:PXGridColumn DataField="OperationID" />
                  </Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
		<ActionBar ActionsText="False">
		</ActionBar>
	</px:PXGrid>
</asp:Content>
