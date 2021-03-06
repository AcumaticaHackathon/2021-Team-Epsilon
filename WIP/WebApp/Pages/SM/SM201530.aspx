<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPages/FormDetail.master"
	AutoEventWireup="true" CodeFile="SM201530.aspx.cs" Inherits="Pages_SM_SM201530"
	EnableViewState="False" EnableViewStateMac="False" %>

<asp:Content ID="Content1" ContentPlaceHolderID="phDS" runat="Server">
    <script type="text/javascript">
        var taskManagerReloaderIntervalId = 0;

        function updateDataCallback()
        {
            if (! <%=PX.Common.WebConfig.GetBool("EnableResourceUsageAutoUpdate", true).ToString().ToLowerInvariant()%>)
                return;

            var ds = px_alls['ds'];
            ds.executeCallback("actionUpdateData");

            
        }

        function updateChart(pxChart) {
            pxChart.fillDataSource(pxChart.data);
            pxChart.chart.dataProvider = pxChart.dataProvider;
            pxChart.chart.validateData();
        }

        function prepareCPUData(data, currentValue, maxLength) {
            if (data == null) {
                data = [];
                for (var i = 0; i < maxLength; i++) {
                    data[i] = ['', 0, ''];
                }
            }

            data[maxLength - 1][0] = '';
            data.shift();
            data[0][0] = (maxLength * 2) + ' sec';
            data.push(['0', parseInt(currentValue.substring(0, currentValue.length - 1)) , currentValue + ' - ' + new Date().toLocaleTimeString()]);

            return data;
        }

        function prepareMemoryData(data, currentValue, maxLength) {
            if (data == null) {
                data = [];
                for (var i = 0; i < maxLength; i++) {
                    data[i] = ['', 0, ''];
                }
            }

            data[maxLength - 1][0] = '';
            data.shift();
            data[0][0] = (maxLength * 2) + ' sec';
            data.push(['0', currentValue , currentValue + ' Mb - ' + new Date().toLocaleTimeString()]);

            return data;
        }

        var taskManagerReloader = {
            onReLoad: function() {
                var pxChart = px_alls["SerialChartCPU"];
                var reloadData = false;
                if (pxChart) {
                    pxChart.data = prepareCPUData(null, '0%', 30);
                    updateChart(pxChart);
                    reloadData = true;
                }

                pxChart = px_alls["SerialChartWorkingSet"];
                if (pxChart) {
                    pxChart.data = prepareMemoryData(null, 0, 30);
                    updateChart(pxChart);
                    reloadData = true;
                }

                if (reloadData) {
                    __px_callback(window).addHandler(Function.createDelegate(window, this.handleError));
                    taskManagerReloaderIntervalId = setInterval(updateDataCallback, 2000);
                }
            },
            handleError: function (context, error) {
                if (error) {
                    clearInterval(taskManagerReloaderIntervalId);
                } else {
                    var cpuUtilization = px_alls["CurrentUtilization"].getValue();
                    var workingSet = px_alls["WorkingSet"].getValue();
           
                    var pxChart = px_alls["SerialChartCPU"];
                    pxChart.data = prepareCPUData(pxChart.data, cpuUtilization, 30);
                    updateChart(pxChart);

                    pxChart = px_alls["SerialChartWorkingSet"];
                    pxChart.data = prepareMemoryData(pxChart.data, workingSet, 30);
                    updateChart(pxChart);
                }
            }
        };

        __px_cm(window).registerRequiresOnReLoad(taskManagerReloader);
    </script>
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Filter"
		TypeName="PX.SM.TaskManager" PageLoadBehavior="PopulateSavedValues">
		<CallbackCommands>
		    <px:PXDSCallbackCommand Name="actionStop" BlockPage="true" DependOnGrid="grid" CommitChanges="False" RepaintControls="All" Visible="False" />
		    <px:PXDSCallbackCommand Name="actionShow" DependOnGrid="grid" Visible="False" />
		    <px:PXDSCallbackCommand Name="actionStackTrace" Visible="False" CommitChanges="True" RepaintControls="All" />
		    <px:PXDSCallbackCommand Name="actionViewUser" CommitChanges="True" DependOnGrid="ActiveUsersGrid" Visible="False" />
		    <px:PXDSCallbackCommand Name="actionGC" Visible="False" />
		    <px:PXDSCallbackCommand Name="actionUpdateData" Visible="False" RepaintControls="None" RepaintControlsIDs="ResourceUsageForm" />
		    <px:PXDSCallbackCommand CommitChanges="True" Name="redirectToScreen" />
			<px:PXDSCallbackCommand Name="actionViewScreen" DependOnGrid="gridProfiler" Visible="False" />
			<px:PXDSCallbackCommand Name="actionViewSql" Visible="False" DependOnGrid="gridProfiler" />
			<px:PXDSCallbackCommand Name="actionViewExceptions" Visible="False" DependOnGrid="gridProfiler" />
			<px:PXDSCallbackCommand Name="actionViewTraces" Visible="False" DependOnGrid="gridProfiler" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXTab ID="MainTabControl" runat="server" Width="100%">
        <Items>
            <px:PXTabItem Text="RUNNING PROCESSES" Visible="True">
                <Template>
                    <px:PXFormView runat="server" ID="RunningProcessesForm" Width="100%" DataSourceID="ds" AllowCollapse="False" DataMember="Filter">
                        <Template>
                            <px:PXLayoutRule runat="server" StartColumn="True" ControlSize="M" LabelsWidth="M">
                            </px:PXLayoutRule>
                            <px:PXCheckBox ID="ShowAllUsers" runat="server" AlignLeft="True" DataField="ShowAllUsers"
                                           SuppressLabel="True" Text="Show All Users" CommitChanges="True">
                            </px:PXCheckBox>
                        </Template>
                    </px:PXFormView>
                    <px:PXGrid runat="server" ID="grid" Width="100%" Height="100%" DataSourceID="ds" AutoGenerateColumns="Recreate" SkinID="Details" Caption="Operations">
                                <AutoSize Enabled="true" Container="Window" />
                                <Mode AllowAddNew="False" AllowDelete="False" AllowFormEdit="False"/>
                                <ActionBar PagerVisible="False" DefaultAction="buttonStop">
                                    <Actions>
                                        <AddNew Enabled="false" MenuVisible="false" />
                                        <Delete Enabled="false" MenuVisible="false" />
                                    </Actions>
                                    <CustomItems>
                                        <px:PXToolBarButton Text="ABORT" Key="buttonStop">
                                            <AutoCallBack Target="ds" Command="actionStop"></AutoCallBack>
                                        </px:PXToolBarButton>
                                        <px:PXToolBarButton Text="VIEW SCREEN" Key="buttonShow">
                                            <AutoCallBack Target="ds" Command="actionShow"></AutoCallBack>
                                        </px:PXToolBarButton>
                                        <px:PXToolBarButton Text="ACTIVE THREADS" CommandSourceID="ds" >
                                            <AutoCallBack Target="ds" Enabled="True" Command="actionStackTrace" >
                                                <Behavior RepaintControls="All"></Behavior>
                                            </AutoCallBack>
                                        </px:PXToolBarButton>
                                    </CustomItems>
                                </ActionBar>
                                <Levels>
                                    <px:PXGridLevel DataMember="Items">
                                        <Columns>
                                            <px:PXGridColumn DataField="User" Width="200px">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="Screen">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="Title" Width="150px">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="Processed" TextAlign="Right" Width="100px">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="Total" TextAlign="Right" Width="100px">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="Errors" TextAlign="Right" Width="200px">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="WorkTime" Width="100px">
                                            </px:PXGridColumn>
                                        </Columns>
                                        <Layout FormViewHeight=""></Layout>
                                    </px:PXGridLevel>
                                </Levels>
                            </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="ACTIVE USERS" Visible="True">
                <Template>
                    <px:PXFormView runat="server" ID="ActiveUsersForm" Width="100%" DataSourceID="ds" AllowCollapse="False" DataMember="Filter">
                        <Template>
                            <px:PXLayoutRule runat="server" StartColumn="True" ControlSize="M" LabelsWidth="M">
                            </px:PXLayoutRule>
                            <px:PXDropDown ID="LoginTypeDropDown" runat="server" DataField="LoginType" Text="Login Type" ControlSize="XL" CommitChanges="True"/>  
                        </Template>
                    </px:PXFormView>
                    <px:PXGrid runat="server" ID="ActiveUsersGrid" Width="100%" Height="100%" DataSourceID="ds" AutoGenerateColumns="None" 
                               SkinID="Details" Caption="Active Users" SyncPosition="True">
                                <AutoSize Enabled="true" Container="Window" />
                                <Mode AllowAddNew="False" AllowDelete="False" AllowFormEdit="False"/>
                                <ActionBar PagerVisible="False" DefaultAction="buttonViewUser">
                                    <Actions>
                                        <AddNew Enabled="false" MenuVisible="false" />
                                        <Delete Enabled="false" MenuVisible="false" />
                                    </Actions>
                                    <CustomItems>
                                        <px:PXToolBarButton Text="VIEW USER" Key="buttonViewUser">
                                            <AutoCallBack Target="ds" Command="actionViewUser"></AutoCallBack>
                                        </px:PXToolBarButton>
                                    </CustomItems>
                                </ActionBar>
                                <Levels>
                                    <px:PXGridLevel DataMember="ActiveUsers">
                                        <Columns>
                                            <px:PXGridColumn DataField="User" Width="200px">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="Company"  Width="200px">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="LoginType"  Width="200px">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="LastActivity" Width="200px" >
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="LoginTimeSpan"  Width="200px">
                                            </px:PXGridColumn>
                                        </Columns>
                                        <Layout FormViewHeight=""></Layout>
                                    </px:PXGridLevel>
                                </Levels>
                            </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="RESOURCE USAGE" Visible="True" >
                <Template>
                    <px:PXFormView runat="server" ID="ResourceUsageForm" Width="100%" DataMember="Filter" CaptionVisible="False">
                        <Template>
                            <px:PXLayoutRule ID="Column0" runat="server" StartColumn="True" GroupCaption="MEMORY USAGE" LabelsWidth="M" />
                            <px:PXTextEdit runat="server" DataField="GCTotalMemory" ID="GCTotalMemory"  />
                            <px:PXTextEdit runat="server" DataField="WorkingSet" ID="WorkingSet" />
                            <px:PXTextEdit runat="server" DataField="GCCollection" ID="GCCollection" />
                            <px:PXButton runat="server" Text="COLLECT MEMORY" AlignLeft="True" >
                                <AutoCallBack Target="ds" Enabled="True" Command="actionGC"></AutoCallBack>
                            </px:PXButton>
                            
                            <px:PXLayoutRule ID="Column1" runat="server" StartColumn="True" GroupCaption="CPU USAGE" LabelsWidth="M"/>
                            <px:PXTextEdit runat="server" DataField="CurrentUtilization" ID="CurrentUtilization" />
                            <px:PXTextEdit runat="server" DataField="UpTime" ID="UpTime"  />
                            <px:PXTextEdit runat="server" DataField="ActiveRequests" ID="ActiveRequests" />
                            <px:PXTextEdit runat="server" DataField="RequestsSumLastMinute" ID="RequestsSumLastMinute" />
                        </Template>
                    </px:PXFormView>
                    <px:PXSerialChart ID="SerialChartCPU" runat="server" Width="100%" SkinID="Chart1" Height="300px" LegendEnabled="False" OnLoad="SerialChartCPU_OnLoad">
                        <Graphs>
                            <px:PXChartGraph LineColor="Black" Title="CPU"/>
                        </Graphs>
                        <DataFields Category="Category" Value="Values" Description="Labels"></DataFields>    
                        <CategoryAxis ShowFirstLabel="True" ShowLastLabel="True" LabelRotation="0" StartOnAxis="True"></CategoryAxis>
                    </px:PXSerialChart>
                    <px:PXSerialChart ID="SerialChartWorkingSet" runat="server" Width="100%" SkinID="Chart1" Height="300px" LegendEnabled="False" OnLoad="SerialChartWorkingSet_OnLoad">
                        <Graphs>
                            <px:PXChartGraph LineColor="Black" Title="Working set"/>
                        </Graphs>
                        <DataFields Category="Category" Value="Values" Description="Labels"></DataFields>                      
                        <CategoryAxis ShowFirstLabel="True" ShowLastLabel="True" LabelRotation="0" StartOnAxis="True"></CategoryAxis>
                    </px:PXSerialChart>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="SYSTEM EVENTS" Visible="True">
                <Template>
                    <px:PXFormView runat="server" ID="SystemEventsForm" Width="100%" AllowCollapse="False" DataMember="Filter">
                        <Template>
                            <px:PXLayoutRule runat="server" StartColumn="True" ControlSize="M" LabelsWidth="M"  />

                            <px:PXDropDown ID="SourceDropDown" runat="server" DataField="Source" Text="Source:" ControlSize="XL" CommitChanges="True" AllowMultiSelect="True"/>  
                            <px:PXDropDown ID="LevelDropDown" runat="server" DataField="Level" Text="Level:" ControlSize="XL" CommitChanges="True"/>  

                            <px:PXLayoutRule runat="server" StartColumn="True" ControlSize="M" LabelsWidth="M"  />

                            <px:PXDateTimeEdit ID="FromDateDropDown" runat="server" DataField="FromDate" Text="From:" ControlSize="XL" CommitChanges="True"/>  
                            <px:PXDateTimeEdit ID="ToDateDropDown" runat="server" DataField="ToDate" Text="To:" ControlSize="XL" CommitChanges="True"/>  
                        </Template>
                    </px:PXFormView>
                    <px:PXGrid runat="server" ID="gridSystemEvents" Width="100%" Height="100%" AutoGenerateColumns="None"  
                            SkinID="Details" SyncPosition="true" AllowPaging="true" AdjustPageSize="Auto"> 
                        <AutoCallBack Target="EventDetailsFormView" ActiveBehavior="true" Command="Refresh" >
                            <Behavior RepaintControlsIDs="EventDetailsFormView" RepaintControls="None" />
                        </AutoCallBack>
                        <AutoSize Enabled="true" Container="Window" />
                        <Mode AllowAddNew="False" AllowDelete="False" AllowFormEdit="False" AllowRowSelect="true" />
                        <Levels>
                            <px:PXGridLevel DataMember="SystemEvents" >
                                <Columns>
                                    <px:PXGridColumn runat="server" DataField="Level" Width="100px"   />
                                    <px:PXGridColumn runat="server" DataField="Source" Width="150px"/>
                                    <px:PXGridColumn runat="server" DataField="EventID" Width="450px" />
                                            
                                    <px:PXGridColumn runat="server" DataField="ScreenId" Width="100px" />

                                    <px:PXGridColumn runat="server" DataField="LinkToEntity"  Width="150px" LinkCommand="redirectToScreen" CommitChanges="true" />
                                    <px:PXGridColumn runat="server" DataField="Date" TextAlign="Right" Width="150px" />
                                    <px:PXGridColumn runat="server" DataField="TenantName" />
                                    <px:PXGridColumn runat="server" DataField="User" Width="200px" />
                                    <px:PXGridColumn runat="server" DataField="Details" Width="200px" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                    </px:PXGrid>
                    <px:PXFormView runat="server" ID="EventDetailsFormView" Width="100%" DataMember="CurrentSystemEvent"  > 
                        <Template>
                            <px:PXTextEdit runat="server" ID="EventDetailsText" DataField="FormattedProperties" TextMode="MultiLine" Height="200px" SuppressLabel="true" Width="100%" />
                        </Template>
                    </px:PXFormView>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="REQUESTS IN PROGRESS" Visible="True">
                <Template>
                    <px:PXFormView runat="server" ID="ProfilerForm" Width="100%" AllowCollapse="False" DataMember="Filter">
                        <Template>
                        </Template>
                    </px:PXFormView>
				    <px:PXGrid runat="server" ID="gridProfiler" SkinID="Details" Width="100%" Height="800px"
					    SyncPosition="True" NoteIndicator="False" FilesIndicator="False"
					    AutoGenerateColumns="None" AutoAdjustColumns="True" AllowPaging="True" AdjustPageSize="Auto">
					    <Levels>
						    <px:PXGridLevel DataMember="Samples" SortOrder="RequestStartTime DESC">
							    <Columns>
								    <px:PXGridColumn DataField="RequestStartTime" DisplayFormat="dd MMM HH:mm:ss" />
								    <px:PXGridColumn DataField="UserId" />
								    <px:PXGridColumn DataField="ScreenId" Width="170" LinkCommand="actionViewScreen" />
								    <px:PXGridColumn DataField="InternalScreenId" />
								    <px:PXGridColumn DataField="RequestType" />
								    <px:PXGridColumn DataField="CommandTarget" />
								    <px:PXGridColumn DataField="CommandName" />
								    <px:PXGridColumn DataField="RequestTimeMs" />
								    <px:PXGridColumn DataField="SelectTimeMs" AllowShowHide="True" SyncVisible="False" Visible="False" />
								    <px:PXGridColumn DataField="SqlTimeMs" />
								    <px:PXGridColumn DataField="RequestCpuTimeMs" />
								    <px:PXGridColumn DataField="SqlCounter" LinkCommand="actionViewSql" />
								    <px:PXGridColumn DataField="SqlRows" />
								    <px:PXGridColumn DataField="SelectCounter" AllowShowHide="True" SyncVisible="False" Visible="False" />
								    <px:PXGridColumn DataField="ExceptionCounter" LinkCommand="actionViewExceptions" />
								    <px:PXGridColumn DataField="EventCounter" LinkCommand="actionViewTrace"  />

								    <px:PXGridColumn DataField="MemBeforeMb" />
								    <px:PXGridColumn DataField="MemoryWorkingSet" AllowShowHide="True" SyncVisible="False" Visible="False" />
								    <px:PXGridColumn DataField="ProcessingItems" AllowShowHide="True" SyncVisible="False" Visible="False" />
								    <px:PXGridColumn DataField="SessionLoadTimeMs" AllowShowHide="True" SyncVisible="False" Visible="False" />
								    <px:PXGridColumn DataField="Headers" AllowShowHide="True" SyncVisible="False" Visible="False" />
								    <px:PXGridColumn DataField="TenantId" AllowShowHide="True" SyncVisible="False" Visible="False" />
								    <px:PXGridColumn DataField="InstallationId" AllowShowHide="True" SyncVisible="False" Visible="False" />
							    </Columns>

						    </px:PXGridLevel>
					    </Levels>
					    <AutoSize Enabled="True" Container="Window" />
					    <ActionBar PagerVisible="False" DefaultAction="buttonSql">
						    <CustomItems>
							    <px:PXToolBarButton Text="View SQL" PopupPanel="PanelSqlProfiler" Key="buttonSql" />
							    <px:PXToolBarButton Text="View Event Log" PopupPanel="PanelTraceProfiler" Key="buttonTrace" />
							    <px:PXToolBarButton Text="Open URL" Key="ViewScreen">
								    <AutoCallBack Target="ds" Command="actionViewScreen"></AutoCallBack>
							    </px:PXToolBarButton>
						    </CustomItems>
					    </ActionBar>
				    </px:PXGrid>

                </Template>
            </px:PXTabItem>
        </Items>
    </px:PXTab>
	
    <px:PXSmartPanel runat="server" ID="pnlCurrentThreads" Width="600px" Height="500px" CaptionVisible="True"
                     Caption="Active Threads" ShowMaximizeButton="True" Key="CurrentThreadsPanel" ShowAfterLoad="true" AutoRepaint="True">
        <px:PXFormView ID="frmCurrentThreads" runat="server" SkinID="Transparent" DataMember="CurrentThreadsPanel">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
                <px:PXTextEdit ID="PXCurrentThreadsText" runat="server" DataField="CurrentThreads" TextMode="MultiLine" SuppressLabel="true" Width="540px" Height="440px" />
            </Template>
        </px:PXFormView>
        <px:PXPanel ID="PXPanelCurrentThreadsButtons" runat="server" SkinID="Buttons">
            <px:PXButton ID="btnCurrentThreadsCancel" runat="server" DialogResult="Cancel" Text="Ok" />
        </px:PXPanel>
    </px:PXSmartPanel>

	<px:PXSmartPanel runat="server" ID="PanelSqlProfiler" Width="90%" Height="680px"
		ShowMaximizeButton="True"
		CaptionVisible="True"
		Caption="View SQL"
		AutoRepaint="True" Key="Sql">
		<px:PXGrid runat="server" ID="GridSqlProfiler"
			Width="100%"
			SkinID="Details"
			AdjustPageSize="Auto"
			AllowPaging="True" FeedbackMode="DisableAll">
			<Mode AllowFormEdit="True"></Mode>
			<Levels>
				<px:PXGridLevel DataMember="Sql">
					<Columns>
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
						<px:PXGridColumn DataField="RequestStartTime" Width="50px" />
						<px:PXGridColumn DataField="Source" Width="60px" />
						<px:PXGridColumn DataField="TraceType" Width="60px" />
						<px:PXGridColumn DataField="ShortMessage" Width="250px" />
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
						<px:PXGridColumn DataField="RequestStartTime" Width="50px" />
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

</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="phG" runat="Server">
	
</asp:Content>
