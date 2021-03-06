<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true"
    ValidateRequest="false" CodeFile="FS500201.aspx.cs" Inherits="Page_FS500201" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Filter" TypeName="PX.Objects.FS.CloneAppointmentProcess">
		<CallbackCommands>
		    <px:PXDSCallbackCommand Name="Clone" PopupCommand="" PopupCommandTarget="" PopupPanel="" Text="" Visible="True">
            </px:PXDSCallbackCommand>
			<px:PXDSCallbackCommand Name="OpenAppointment" PopupCommand="" PopupCommandTarget="" PopupPanel="" Text="" Visible="false">
            </px:PXDSCallbackCommand>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100; margin-bottom: 0px;"
		Width="100%" DataMember="AppointmentSelected" NoteIndicator="True" TabIndex="-14736">
        <Template>
            <px:PXLayoutRule runat="server" ControlSize="S" LabelsWidth="SM" StartColumn="True">
            </px:PXLayoutRule>
            <px:PXSelector ID="edSrvOrdType" runat="server" DataField="SrvOrdType">
            </px:PXSelector>
            <px:PXSelector ID="edRefNbr" runat="server" DataField="RefNbr">
            </px:PXSelector>
            <px:PXSelector ID="edSORefNbr" runat="server" DataField="SORefNbr">
            </px:PXSelector>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM">
            </px:PXLayoutRule>
            <px:PXFormView ID="serviceOrderHeaderForm" runat="server" DataSourceID="ds" SkinID="Transparent"
                Width="100%" DataMember="ServiceOrderRelated" NoteIndicator="True" TabIndex="-14736">
                <Template>
                    <px:PXSegmentMask ID="edCustomerID" runat="server" AllowEdit="True" DataField="CustomerID" DataSourceID="ds" Enabled="False">
                    </px:PXSegmentMask>
                    <px:PXSegmentMask ID="edLocationID" runat="server" DataField="LocationID" CommitChanges="True" AutoRefresh="True" Enabled="False" AllowEdit="True">
                    </px:PXSegmentMask>
                    <px:PXSelector ID="edBranchLocationID" runat="server" DataField="BranchLocationID" Enabled="False">
                    </px:PXSelector>
                </Template>
            </px:PXFormView> 
            <px:PXLayoutRule runat="server" StartRow="True"/>
            <px:PXLabel ID="PXLabel1" runat="server"></px:PXLabel>
            <px:PXFormView ID="PXFormView1" runat="server" DataMember="Filter" DataSourceID="ds" DefaultControlID="SingleGenerationDate" RenderStyle="Simple" Style="z-index: 100; margin-bottom: 0px;" TabIndex="10600" Width="100%">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" GroupCaption="Cloning Type" ControlSize="S" LabelsWidth="SM"/>
                    <px:PXGroupBox CommitChanges="True" RenderStyle="Simple" ID="gbCloningType" runat="server" AllowNull="False" Caption="CloningType" DataField="CloningType" RenderSimple="True">
                        <Template>
                            <px:PXRadioButton runat="server" ID="edCloningTypeSimple" Size="XM" GroupName="gbCloningType" Text="Single" Value="SI"/>
                            <px:PXRadioButton runat="server" ID="edCloningTypeMultiple" Size="XM" GroupName="gbCloningType" Text="Multiple" Value="MU"/>
                        </Template>
                    </px:PXGroupBox>
                    <px:PXLayoutRule runat="server" GroupCaption="Cloning Details" StartColumn="True" LabelsWidth="SM">
                    </px:PXLayoutRule>
                    <px:PXDateTimeEdit ID="edSingleGenerationDate" runat="server" DataField="SingleGenerationDate" SuppressLabel="False" CommitChanges="True">
                    </px:PXDateTimeEdit>
                    <px:PXDateTimeEdit ID="edMultGenerationFromDate" runat="server" CommitChanges="True" DataField="MultGenerationFromDate">
                    </px:PXDateTimeEdit>
                    <px:PXDateTimeEdit ID="edMultGenerationToDate" runat="server" CommitChanges="True" DataField="MultGenerationToDate">
                    </px:PXDateTimeEdit>
                    <px:PXPanel ID="PXPanelWeek" runat="server" RenderSimple="True" RenderStyle="Simple">
                        <px:PXLayoutRule runat="server" StartColumn="True" SuppressLabel="True" />
                        <px:PXCheckBox ID="edActiveOnSunday" runat="server" DataField="ActiveOnSunday" Text="Sunday" CommitChanges="True">
                        </px:PXCheckBox>
                        <px:PXCheckBox ID="edActiveOnMonday" runat="server" DataField="ActiveOnMonday" Text="Monday" CommitChanges="True">
                        </px:PXCheckBox>
                        <px:PXCheckBox ID="edActiveOnTuesday" runat="server" DataField="ActiveOnTuesday" Text="Tuesday" CommitChanges="True">
                        </px:PXCheckBox>
                        <px:PXLayoutRule runat="server" StartColumn="True" SuppressLabel="True" />
                        <px:PXCheckBox ID="edActiveOnWednesday" runat="server" DataField="ActiveOnWednesday" Text="Wednesday" CommitChanges="True">
                        </px:PXCheckBox>
                        <px:PXCheckBox ID="edActiveOnThursday" runat="server" DataField="ActiveOnThursday" Text="Thursday " CommitChanges="True">
                        </px:PXCheckBox>
                        <px:PXCheckBox ID="edActiveOnFriday" runat="server" DataField="ActiveOnFriday" Text="Friday" CommitChanges="True">
                        </px:PXCheckBox>
                        <px:PXLayoutRule runat="server" StartColumn="True" SuppressLabel="True" />
                        <px:PXCheckBox ID="edActiveOnSaturday" runat="server" DataField="ActiveOnSaturday" Text="Saturday" CommitChanges="True">
                        </px:PXCheckBox>
                    </px:PXPanel>
                    <px:PXLayoutRule runat="server" GroupCaption="Appointment Details" LabelsWidth="SM"/>
                    <px:PXDateTimeEdit ID="edScheduledStartTime_Time" runat="server" DataField="ScheduledStartTime_Time" TimeMode="True" SuppressLabel="False" CommitChanges="True">
                    </px:PXDateTimeEdit>
                    <px:PXLayoutRule runat="server" Merge="True"/>
                    <px:PXMaskEdit ID="edApptDuration" runat="server" DataField="ApptDuration" CommitChanges="True" Size="S">
                    </px:PXMaskEdit>
                    <px:PXCheckBox ID="edOverrideApptDuration" runat="server" DataField="OverrideApptDuration" Text="Saturday" CommitChanges="True">
                    </px:PXCheckBox>
                </Template>
            </px:PXFormView> 
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont4" ContentPlaceHolderID="phG" runat="Server">
    <px:PXTab ID="ClonesTab" runat="server" Height="150px" Style="z-index: 100" Width="100%" DataMember="AppointmentClones" DataSourceID="ds">
        <AutoSize Enabled="True" Container="Window" MinWidth="300" MinHeight="250"></AutoSize>        
        <Items>
            <px:PXTabItem Text="Cloned Appointments">
                <Template>                        
                    <px:PXGrid ID="PXRouteEmployees" runat="server" DataSourceID="ds" SkinID="Inquire" Width="100%" KeepPosition="True" SyncPosition="True"
                        AllowPaging="True" AdjustPageSize="Auto" Height="200px" TabIndex="11300" FilesIndicator="False" NoteIndicator="False">
                        <Levels>
                            <px:PXGridLevel DataMember="AppointmentClones">
                                <RowTemplate>
                                    <px:PXSelector ID="edRefNbr" runat="server" DataField="RefNbr" AllowEdit="True" />
                                    <px:PXSelector ID="edSrvOrdType" runat="server" DataField="SrvOrdType" />
                                    <px:PXSelector ID="edSORefNbr" runat="server" DataField="SORefNbr" />
                                    <px:PXDateTimeEdit ID="edScheduledDateTimeBegin" runat="server" DataField="ScheduledDateTimeBegin" />
                                    <px:PXDateTimeEdit ID="edScheduledDateTimeBegin_Time" runat="server" DataField="ScheduledDateTimeBegin_Time" />
                                    <px:PXDateTimeEdit ID="edScheduledDateTimeEnd" runat="server" DataField="ScheduledDateTimeEnd" />
                                    <px:PXDateTimeEdit ID="edScheduledDateTimeEnd_Time" runat="server" DataField="ScheduledDateTimeEnd_Time" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="SrvOrdType" />
                                    <px:PXGridColumn DataField="SORefNbr" />
                                    <px:PXGridColumn DataField="RefNbr" LinkCommand="OpenAppointment" />
                                    <px:PXGridColumn DataField="ScheduledDateTimeBegin" />
                                    <px:PXGridColumn DataField="ScheduledDateTimeBegin_Time" />
                                    <px:PXGridColumn DataField="ScheduledDateTimeEnd" />
                                    <px:PXGridColumn DataField="ScheduledDateTimeEnd_Time" />
                                    <px:PXGridColumn DataField="Status" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                    <AutoSize Enabled="True" MinHeight="200" ></AutoSize>                    
                    </px:PXGrid>                                            
                </Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXTab>
</asp:Content>