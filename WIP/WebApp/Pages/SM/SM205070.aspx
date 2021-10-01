<%@ Page Title="Untitled Page" Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" CodeFile="SM205070.aspx.cs" Inherits="Pages_SM_SM205070" ValidateRequest="False" %>

<asp:Content ID="Content1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource runat="server"
		ID="ds"
		Visible="True"
		TypeName="PX.SM.PerformanceMonitorMaint"
		PrimaryView="Filter"
		Width="100%">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="actionFlushSamples" RepaintControls="Bound" />
			<px:PXDSCallbackCommand Name="actionClearSamples" RepaintControls="Bound" />
			<px:PXDSCallbackCommand Name="actionViewScreen" DependOnGrid="grid" Visible="False" />
			<px:PXDSCallbackCommand Name="actionViewSql" Visible="False" DependOnGrid="grid" />
			<px:PXDSCallbackCommand Name="actionViewExceptions" Visible="False" DependOnGrid="grid" />
			<px:PXDSCallbackCommand Name="actionViewTraces" Visible="False" DependOnGrid="grid" />
			<px:PXDSCallbackCommand Name="actionPinRows" Visible="False" DependOnGrid="grid" />
			<px:PXDSCallbackCommand Name="actionViewSqlSummaryRows" Visible="False" DependOnGrid="GridSqlSummary" />
			<px:PXDSCallbackCommand Name="actionExceptionDetails" DependOnGrid="GridExceptionsLog" Visible="False" />
			<px:PXDSCallbackCommand Name="actionImportLogs" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXUploadDialog ID="dlgUploadFile" runat="server" Key="UploadDialogPanel" Height="60px" Style="position: static" Width="450px" 
		Caption="Upload Logs" AutoSaveFile="false" SessionKey="UploadedPerformanceLogsKey" AllowedTypes=".zip" IgnoreSize="true" RenderImportOptions="false"
		RenderCheckIn="false" RenderLinkTextBox="false" RenderLink="false" RenderComment="false" />
	<px:PXFormView runat="server"
		ID="form"
		Width="100%"
		DataMember="Filter" CaptionVisible="False">
		<Template>
			<px:PXLayoutRule runat="server" StartRow="True" />
			<px:PXCheckBox runat="server" ID="PXCheckBox5" DataField="DefaultLogging" CommitChanges="True" AlignLeft="true" Size="SM" Width="600px" />
			<px:PXLayoutRule runat="server" StartRow="True" />
			<px:PXPanel runat="server" ID="pnlFilters" RenderStyle="Simple">
			<px:PXPanel runat="server" ID="pnl1" Caption="Request Logging" RenderStyle="Fieldset">
				<%--<px:PXLayoutRule runat="server" GroupCaption="Request Log" />--%>
				<px:PXLayoutRule runat="server" StartColumn="True" />
				<px:PXCheckBox runat="server" ID="ProfilerEnabled" DataField="ProfilerEnabled" CommitChanges="True" AlignLeft="true" Size="M" />
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
				<px:PXTextEdit runat="server" DataField="TimeLimit" ID="edTimeLimit" CommitChanges="True" />
				<px:PXTextEdit runat="server" DataField="SqlCounterLimit" ID="SqlCounterLimit" CommitChanges="True" />
				<%--<px:PXCheckBox runat="server" ID="PXCheckBox1" DataField="LogExpensiveRequests" Text="Log Expensive Requests" CommitChanges="True" AlignLeft="true" />--%>
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
				<px:PXTextEdit runat="server" DataField="ScreenId" ID="edScreenId" CommitChanges="True" />
				<px:PXTextEdit runat="server" DataField="UserId" ID="edUserId" CommitChanges="True" />
				<%--<px:PXCheckBox runat="server" ID="PXCheckBox2" DataField="LogImportantExceptions" Text="Log Important Exceptions" CommitChanges="True" AlignLeft="true" />--%>
			</px:PXPanel>



			<%--<px:PXLayoutRule runat="server" StartRow="true" />--%>
			<px:PXPanel runat="server" ID="PXPanel2" Caption="SQL Logging" RenderStyle="Fieldset">
				<%--<px:PXLayoutRule ID="PXLayoutRule4" runat="server" StartRow="True" GroupCaption="SQL Log" SuppressLabel="True" />--%>
				<px:PXLayoutRule runat="server" StartColumn="True" />
				<px:PXCheckBox runat="server" ID="SqlProfiler" DataField="SqlProfiler" CommitChanges="True" AlignLeft="true" Size="M" />
				<px:PXLayoutRule ID="PXLayoutRule7" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
				<px:PXTextEdit runat="server" DataField="SqlRowCounterLimit" ID="PXTextEdit6" CommitChanges="True" />
				<px:PXTextEdit runat="server" DataField="SqlTimeLimit" ID="PXTextEdit5" CommitChanges="True" />
				<px:PXLayoutRule ID="PXLayoutRule8" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
				<px:PXTextEdit runat="server" DataField="SqlMethodFilter" ID="PXTextEdit7" CommitChanges="True" />
				<px:PXCheckBox runat="server" DataField="SqlProfilerIncludeQueryCache" ID="PXCheckBox3" CommitChanges="True" AlignLeft="true" />
			</px:PXPanel>

			<px:PXLayoutRule runat="server" StartColumn="true" />
			<px:PXPanel runat="server" ID="PXPanel3" Caption="Exceptions Logging" RenderStyle="Fieldset" Width="400px">
				<px:PXLayoutRule ID="Column1" runat="server" StartColumn="True" />
				<px:PXCheckBox runat="server" ID="TraceExceptionsEnabled" DataField="TraceExceptionsEnabled" CommitChanges="True" AlignLeft="true" />
			</px:PXPanel>

			<%--<px:PXLayoutRule runat="server" StartColumn="true" />--%>
			<px:PXPanel runat="server" ID="PXPanel1" Caption="Event Logging" RenderStyle="Fieldset" >
				<px:PXLayoutRule ID="PXLayoutRule3" runat="server" StartColumn="True" />
				<px:PXCheckBox runat="server" ID="PXCheckBox4" DataField="TraceEnabled" CommitChanges="True" AlignLeft="true" />
				<px:PXLayoutRule ID="PXLayoutRule4" runat="server" StartColumn="True" />
				<px:PXDropDown runat="server" ID="PXDropDown1" DataField="LogLevelFilter" CommitChanges="true" Width="100" ></px:PXDropDown>
				<%--<px:PXCheckBox runat="server" ID="TraceEnabled" DataField="TraceEnabled" CommitChanges="True" AlignLeft="true" />--%>
				<%--<px:PXCheckBox runat="server" ID="TraceExceptionsEnabled" DataField="TraceExceptionsEnabled" CommitChanges="True" AlignLeft="true" />--%>
				<px:PXDropDown runat="server" ID="PXDropDown2" DataField="LogCategoryFilter" AllowMultiSelect="True" CommitChanges="true" Width="100" ></px:PXDropDown>
			</px:PXPanel>
			</px:PXPanel>
		</Template>
	</px:PXFormView>
</asp:Content>



<asp:Content ID="Content3" ContentPlaceHolderID="phG" runat="Server">

<px:PXTab ID="tab" runat="server" Style="z-index: 100;" Width="100%">
	<Items>
		<px:PXTabItem Text="REQUESTS">
			<Template>

				<px:PXGrid runat="server" ID="grid" SkinID="Details" Width="100%" Height="400px"
					SyncPosition="True" NoteIndicator="False" FilesIndicator="False"
					AutoGenerateColumns="Append" AutoAdjustColumns="True" AllowPaging="True" AdjustPageSize="Auto">
					<Levels>
						<px:PXGridLevel DataMember="Samples" SortOrder="RequestStartTime DESC">
							<Columns>
								<%--<px:PXGridColumn DataField="IsChecked" Type="CheckBox" AllowMove="False" AllowSort="True" Width="30" AllowCheckAll="True" CommitChanges="True" />--%>
								<px:PXGridColumn DataField="IsPinned" Width="38px" AllowMove="False" AllowFilter="False" AllowSort="True" />
								<px:PXGridColumn DataField="RequestStartTime" DisplayFormat="dd MMM HH:mm:ss" Width="150px" />
								<px:PXGridColumn DataField="UserId" />
								<px:PXGridColumn DataField="ScreenId" Width="170" LinkCommand="actionViewScreen" />
								<px:PXGridColumn DataField="InternalScreenId" />
								<px:PXGridColumn DataField="RequestType" />
								<px:PXGridColumn DataField="Status" />
								<px:PXGridColumn DataField="CommandTarget" />
								<px:PXGridColumn DataField="CommandName" />
								<px:PXGridColumn DataField="ScriptTimeMs" />
								<px:PXGridColumn DataField="RequestTimeMs" />
								<px:PXGridColumn DataField="SelectTimeMs" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="SqlTimeMs" />
								<px:PXGridColumn DataField="RequestCpuTimeMs" />
								<px:PXGridColumn DataField="SqlCounter" />
								<px:PXGridColumn DataField="LoggedSqlCounter" LinkCommand="actionViewSql" />
								<px:PXGridColumn DataField="SqlRows" />
								<px:PXGridColumn DataField="SelectCounter" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="ExceptionCounter" />
								<px:PXGridColumn DataField="LoggedExceptionCounter" LinkCommand="actionViewExceptions" />
								<px:PXGridColumn DataField="EventCounter" />
								<px:PXGridColumn DataField="LoggedEventCounter" LinkCommand="actionViewTrace" />

								<px:PXGridColumn DataField="MemBeforeMb" />
								<px:PXGridColumn DataField="MemDeltaMb" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="MemoryWorkingSet" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="ProcessingItems" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="SessionLoadTimeMs" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="SessionSaveTimeMs" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="Headers" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="TenantId" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="InstallationId" AllowShowHide="True" SyncVisible="False" Visible="False" />
								<px:PXGridColumn DataField="SqlDigest" AllowShowHide="True" SyncVisible="False" Visible="False" />
							</Columns>

						</px:PXGridLevel>
					</Levels>
					<%--        <CallbackCommands>
						<Refresh RepaintControls="Bound" />
					</CallbackCommands>--%>
					<AutoSize Enabled="True" Container="Window" />
					<ActionBar PagerVisible="False" DefaultAction="buttonSql">
						<CustomItems>
							<px:PXToolBarButton Text="View SQL" PopupPanel="PanelSqlProfiler" Key="buttonSql" />
							<px:PXToolBarButton Text="View Event Log" PopupPanel="PanelTraceProfiler" Key="buttonTrace" />
							<px:PXToolBarButton Text="Open URL" Key="ViewScreen">
								<AutoCallBack Target="ds" Command="actionViewScreen"></AutoCallBack>
							</px:PXToolBarButton>
							<px:PXToolBarButton Text="PIN/UNPIN" Key="PinRows">
								<AutoCallBack Target="ds" Command="actionPinRows"></AutoCallBack>
							</px:PXToolBarButton>
							<px:PXToolBarButton Text="Select Rows" Key="SelectRows">
								<AutoCallBack Target="ds" Command="actionSelectRows"></AutoCallBack>
							</px:PXToolBarButton>
							<px:PXToolBarButton Text="Save Rows" Key="SaveRows">
								<AutoCallBack Target="ds" Command="actionSaveRows"></AutoCallBack>
							</px:PXToolBarButton>

						</CustomItems>
					</ActionBar>


				</px:PXGrid>
			</Template>
		</px:PXTabItem>
		<px:PXTabItem Text="SQL">
			<Template>
				<px:PXGrid runat="server" ID="GridSqlSummary"
					Width="100%" Height="400px" SyncPosition="True" 
					SkinID="Details"
					AdjustPageSize="Auto"
					AllowPaging="True" >
					
					<Levels>

						<px:PXGridLevel DataMember="SqlSummary" SortOrder="TotalSQLTime DESC">
							<Columns>
								<px:PXGridColumn DataField="RecordId" LinkCommand="actionViewSqlSummaryRows" />
								<px:PXGridColumn DataField="TableList" Width="300" />
								<px:PXGridColumn DataField="SqlText"  Width="300" />
								<px:PXGridColumn DataField="TotalSQLTime" />
								<px:PXGridColumn DataField="TotalExecutions" />
								<px:PXGridColumn DataField="TotalRows" />
							</Columns>
						</px:PXGridLevel>
					</Levels>
					<AutoSize Enabled="True" Container="Window" />
					<ActionBar PagerVisible="False" />

				</px:PXGrid>
			</Template>
		</px:PXTabItem>
		<px:PXTabItem Text="EXCEPTIONS">
			<Template>
				<px:PXGrid runat="server" ID="GridExceptionsLog" Width="100%" Height="400px" SyncPosition="True" 
					SkinID="Details"
					AdjustPageSize="Auto"
					AllowPaging="True" >
					<Levels>

						<px:PXGridLevel DataMember="TraceExceptionsSummary">
							<Columns>
								<px:PXGridColumn DataField="Tenant" Width="90px" />
								<px:PXGridColumn DataField="ExceptionType" Width="90px" />
								<px:PXGridColumn DataField="ExceptionMessage" Width="150px" />
								<px:PXGridColumn DataField="Count" Width="80px" />
								<px:PXGridColumn DataField="LastOccured" Width="80px" DisplayFormat="dd MMM HH:mm:ss" />
								<px:PXGridColumn DataField="LastUrl" Width="80px" />
								<px:PXGridColumn DataField="LastCommandTarget" Width="80px" />
								<px:PXGridColumn DataField="LastCommandName" Width="80px" />
								<px:PXGridColumn DataField="LastStackTrace" Width="200px" />

							</Columns>
						</px:PXGridLevel>
					</Levels>
					<AutoSize Enabled="True" Container="Window" />
					<ActionBar PagerVisible="False" DefaultAction="buttonExceptionDetails">
						<CustomItems>
							<px:PXToolBarButton Text="View Exception Details" Key="buttonExceptionDetails" >
								<AutoCallBack Target="ds" Command="actionExceptionDetails"></AutoCallBack>
							</px:PXToolBarButton>
						</CustomItems>
					</ActionBar>
				</px:PXGrid>

			</Template>
		</px:PXTabItem>
		<px:PXTabItem Text="EVENT LOG">
			<Template>

				<px:PXGrid runat="server" ID="GridTraceLog" Width="100%" Height="400px" SyncPosition="True" 
					SkinID="Details"
					AdjustPageSize="Auto"
					AllowPaging="True" >
					<Levels>

						<px:PXGridLevel DataMember="TraceEventsLog" SortOrder="EventDateTime DESC">
							<Columns>
								<px:PXGridColumn DataField="EventDateTime" Width="120px" DisplayFormat="dd MMM HH:mm:ss"  />
								<px:PXGridColumn DataField="TraceType" Width="90px" />
								<px:PXGridColumn DataField="SMPerformanceInfo__UserId" Width="90px" />
								<px:PXGridColumn DataField="SMPerformanceInfo__TenantId" Width="90px" />
								<px:PXGridColumn DataField="Source" Width="80px" />
								<px:PXGridColumn DataField="SMPerformanceInfo__InternalScreenId" Width="90px" />
								<px:PXGridColumn DataField="ShortMessage" Width="250px" />
								<px:PXGridColumn DataField="StackTrace" Width="250px" />
								<px:PXGridColumn DataField="SMPerformanceInfo__ScreenId" Width="150px" LinkCommand="actionViewScreen" />
								<px:PXGridColumn DataField="SMPerformanceInfo__CommandTarget" />
								<px:PXGridColumn DataField="SMPerformanceInfo__CommandName" />
							</Columns>
							<RowTemplate>
								<px:PXLayoutRule runat="server" />

								<px:PXTextEdit runat="server" ID="PXTextEdit3" DataField="MessageWithStackTrace" SelectOnFocus="False" TextMode="MultiLine" Width="600px" Height="490px" />

							</RowTemplate>
						</px:PXGridLevel>
					</Levels>
					<AutoSize Enabled="True" Container="Window" />
					<ActionBar PagerVisible="False" DefaultAction="buttonEventLogDetails">
						<CustomItems>
							<px:PXToolBarButton Text="View Event Details" PopupPanel="PanelEventLogDetails" Key="buttonEventLogDetails" />
						</CustomItems>
					</ActionBar>

				</px:PXGrid>
			</Template>
		</px:PXTabItem>

	</Items>
</px:PXTab>

	<px:PXSmartPanel runat="server" ID="PanelSqlProfiler" Width="90%" Height="680px"
		ShowMaximizeButton="True"
		CaptionVisible="True"
		Caption="View SQL"
		AutoRepaint="True" Key="SqlSamples">


		<px:PXGrid runat="server" ID="GridProfiler"
			Width="100%"
			SkinID="Details"
			AdjustPageSize="Auto"
			AllowPaging="True" FeedbackMode="DisableAll">
			<Mode AllowFormEdit="True"></Mode>
			<Levels>

				<px:PXGridLevel DataMember="Sql">
					<Columns>
						<px:PXGridColumn DataField="QueryOrderID" />
						<px:PXGridColumn DataField="SqlId" />
						<px:PXGridColumn DataField="TableList" Width="300" />
						<px:PXGridColumn DataField="NRows" />
						<px:PXGridColumn DataField="RequestStartTime" />
						<px:PXGridColumn DataField="SqlTimeMs" />
						<px:PXGridColumn DataField="ShortParams" Width="250" />
						<px:PXGridColumn DataField="QueryCache" TextAlign="Center" Type="CheckBox" />

					</Columns>
					<RowTemplate>
						<px:PXLayoutRule runat="server" />

						<px:PXTextEdit runat="server" ID="tables" DataField="TableList" SelectOnFocus="False" Width="600px" />

						<px:PXFormView runat="server"
							ID="PXFormView1"
							Width="100%"
							DataMember="Filter" CaptionVisible="False" RenderStyle="Simple">
							<Template>
								<px:PXCheckBox runat="server" ID="SqlProfilerShowStackTrace" DataField="SqlProfilerShowStackTrace" CommitChanges="True" Style="margin-left: 130px" />
							</Template>
						</px:PXFormView>

						<px:PXTextEdit runat="server" ID="SqlText" DataField="SQLWithStackTrace" SelectOnFocus="False" TextMode="MultiLine" Width="600px" Height="490px" />

					</RowTemplate>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="True" Container="Parent" />
			<ActionBar PagerVisible="False" />

		</px:PXGrid>
	</px:PXSmartPanel>

	<px:PXSmartPanel runat="server" ID="PanelExceptionsProfiler" Width="90%" Height="650px"
		ShowMaximizeButton="True"
		CaptionVisible="True"
		Caption="Exception Profiler"
		AutoRepaint="True" Key="TraceExceptions">


		<px:PXGrid runat="server" ID="ExceptionsGrid" FeedbackMode="DisableAll"
			Width="100%"
			SkinID="Details"
			PageSize="25"
			AllowPaging="True">
			<Mode AllowFormEdit="True"></Mode>
			<Levels>

				<px:PXGridLevel DataMember="TraceExceptions">
					<Columns>
						<px:PXGridColumn DataField="RequestStartTime" Width="70px" />
						<px:PXGridColumn DataField="Source" Width="60px" />
						<px:PXGridColumn DataField="ExceptionType" Width="100px" />
						<px:PXGridColumn DataField="MessageText" Width="250px" />


					</Columns>
					<RowTemplate>
						<px:PXLayoutRule runat="server" />

						<px:PXTextEdit runat="server" ID="ExceptionText" DataField="MessageWithStackTrace" SelectOnFocus="False" TextMode="MultiLine" Width="600px" Height="490px" />

					</RowTemplate>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="True" Container="Parent" />
			<ActionBar PagerVisible="False" />

		</px:PXGrid>
	</px:PXSmartPanel>

	<px:PXSmartPanel runat="server" ID="PanelTraceProfiler" Width="90%" Height="650px"
		ShowMaximizeButton="True"
		CaptionVisible="True"
		Caption="View Event Log"
		AutoRepaint="True" Key="TraceEvents">


		<px:PXGrid runat="server" ID="TraceEventsGrid" FeedbackMode="DisableAll"
			Width="100%"
			SkinID="Details"
			PageSize="25"
			AllowPaging="True">
			<Mode AllowFormEdit="True"></Mode>
			<Levels>

				<px:PXGridLevel DataMember="TraceEvents">
					<Columns>
						<px:PXGridColumn DataField="RequestStartTime" Width="70px" />
						<px:PXGridColumn DataField="Source" Width="60px" />
						<px:PXGridColumn DataField="TraceType" Width="60px" />
						<px:PXGridColumn DataField="ShortMessage" Width="250px" />


					</Columns>
					<RowTemplate>
						<px:PXLayoutRule runat="server" />

						<px:PXTextEdit runat="server" ID="MessageText" DataField="MessageWithStackTrace" SelectOnFocus="False" TextMode="MultiLine" Width="600px" Height="490px" />

					</RowTemplate>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="True" Container="Parent" />
			<ActionBar PagerVisible="False" />

		</px:PXGrid>
	</px:PXSmartPanel>

	<px:PXSmartPanel runat="server" ID="PanelSqlSummaryRows" Width="90%" Height="680px"
		ShowMaximizeButton="True"
		CaptionVisible="True"
		Caption="SQL Details"
		AutoRepaint="True" Key="SqlSummary">

		<px:PXFormView runat="server"
			ID="FormSqlSummaryRows"
			Width="100%"
			DataMember="SqlSummaryFilter" RenderStyle="Simple" >
			<Template>
				<px:PXLayoutRule ID="Column0" runat="server" StartColumn="True" />
				<px:PXTextEdit runat="server" DataField="RecordId" ID="edScreenId" Enabled="false" LabelWidth="135" />
				<px:PXTextEdit runat="server" DataField="TotalSQLTime" ID="edUserId"  LabelWidth="135" />

				<px:PXLayoutRule ID="Column1" runat="server" StartColumn="True" />
				<px:PXTextEdit runat="server" DataField="TotalExecutions" ID="edTimeLimit" LabelWidth="135" />
				<px:PXTextEdit runat="server" DataField="TotalRows" ID="SqlCounterLimit" LabelWidth="135" />
			</Template>
		</px:PXFormView>

		<px:PXGrid runat="server" ID="SqlSummaryRowsGrid"
			Width="100%"
			SkinID="Details"
			AdjustPageSize="Auto" SyncPosition="true"
			AllowPaging="True">
			<Levels>

				<px:PXGridLevel DataMember="SqlSummaryRows">
					<Columns>
						<px:PXGridColumn DataField="RequestDateTime" DisplayFormat="dd MMM HH:mm:ss" Width="120px" />
						<px:PXGridColumn DataField="SQLParams" Width="250" />
						<px:PXGridColumn DataField="SqlTimeMs" />
						<px:PXGridColumn DataField="NRows" />
						<px:PXGridColumn DataField="SMPerformanceInfo__ScreenId" />
						<px:PXGridColumn DataField="SMPerformanceInfo__CommandTarget" />
						<px:PXGridColumn DataField="SMPerformanceInfo__CommandName" />
						<px:PXGridColumn DataField="StackTrace" />

					</Columns>
					<RowTemplate>
						<px:PXLayoutRule runat="server" />

						<px:PXTextEdit runat="server" ID="PXTextEdit1" DataField="TableList" SelectOnFocus="False" Width="600px" />

						<px:PXFormView runat="server"
							ID="PXFormView2"
							Width="100%"
							DataMember="Filter" CaptionVisible="False" RenderStyle="Simple">
							<Template>
								<px:PXCheckBox runat="server" ID="SqlProfilerShowStackTrace" DataField="SqlProfilerShowStackTrace" CommitChanges="True" />
							</Template>
						</px:PXFormView>

						<px:PXTextEdit runat="server" ID="PXTextEdit2" DataField="SQLWithStackTrace" SelectOnFocus="False" TextMode="MultiLine" Width="600px" Height="490px" />

					</RowTemplate>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="True" Container="Parent" />
			<ActionBar PagerVisible="False" />
			<AutoCallBack Target="FormSqlSummaryRowDetails" Command="Refresh" Enabled="True" />

		</px:PXGrid>

		<px:PXFormView runat="server"
			ID="FormSqlSummaryRowDetails" DataSourceID="ds"
			Width="100%" CaptionVisible="false"
			DataMember="SqlSummaryRowsPreview" RenderStyle="Simple" >
			<Template>
				<px:PXTextEdit runat="server" ID="PXTextEdit2" DataField="SQLWithStackTrace" SelectOnFocus="False" TextMode="MultiLine" SuppressLabel="true" Width="100%" Height="300px" />
			</Template>
		</px:PXFormView>
	</px:PXSmartPanel>

	<px:PXSmartPanel runat="server" ID="PanelEventLogDetails" Width="50%" Height="680px"
		ShowMaximizeButton="True"
		CaptionVisible="True"
		Caption="View Event Details"
		AutoRepaint="True" Key="PanelEventLogDetails">

		<px:PXFormView runat="server"
			ID="PXFormView3"
			Width="100%"
			DataMember="TraceEventsLogDetails" RenderStyle="Simple" >
			<Template>
				<px:PXTextEdit runat="server" DataField="EventDateTime" ID="edRequestStartTimeDetails" Enabled="false" LabelWidth="135" DisplayFormat="dd MMM HH:mm:ss" />
				<px:PXLayoutRule ID="PXLayoutRule2" runat="server" StartColumn="true" />
				<px:PXTextEdit runat="server" DataField="TraceType" ID="edTraceTypeDetails" Enabled="false" LabelWidth="135" />

				<px:PXLayoutRule ID="Row1" runat="server" StartRow="true" />
				<px:PXTextEdit runat="server" DataField="EventDetails" ID="edMessageTextDetails" Enabled="false" LabelWidth="135" TextMode="MultiLine" Width="600px" Height="290px" />
				<px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartRow="true" />
				<px:PXTextEdit runat="server" DataField="StackTrace" ID="edStackTraceDetails" Enabled="false" LabelWidth="135" TextMode="MultiLine" Width="600px" Height="290px" />
			</Template>
		</px:PXFormView>

	</px:PXSmartPanel>

	<px:PXSmartPanel runat="server" ID="PanelExceptionDetails" Width="50%" Height="650px"
		ShowMaximizeButton="True"
		CaptionVisible="True"
		Caption="View Exception Details"
		AutoRepaint="True" Key="TraceExceptionsSummary">


		<px:PXGrid runat="server" ID="PXGrid1"
			Width="100%"
			SkinID="Details" 
			AdjustPageSize="Auto"
			AllowPaging="True">
			<Levels>

				<px:PXGridLevel DataMember="TraceExceptionDetails">
					<Columns>
						<px:PXGridColumn DataField="EventDateTime" Width="80px" DisplayFormat="dd MMM HH:mm:ss" />
						<px:PXGridColumn DataField="SMPerformanceInfo__InternalScreenId" Width="90px" />
						<px:PXGridColumn DataField="SMPerformanceInfo__ScreenId" Width="90px" />
						<px:PXGridColumn DataField="SMPerformanceInfo__CommandTarget" Width="90px" />
						<px:PXGridColumn DataField="SMPerformanceInfo__CommandName" Width="90px" />
						<px:PXGridColumn DataField="StackTrace" Width="250px" />


					</Columns>
					<RowTemplate>
						<px:PXLayoutRule runat="server" />

						<px:PXTextEdit runat="server" ID="PXTextEdit4" DataField="MessageWithStackTrace" SelectOnFocus="False" TextMode="MultiLine" Width="600px" Height="490px" />

					</RowTemplate>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="True" Container="Parent" />
			<ActionBar PagerVisible="False" />

		</px:PXGrid>
	</px:PXSmartPanel>

</asp:Content>

