<%@ Page Language="C#" MasterPageFile="~/MasterPages/TabView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="FS100500.aspx.cs" Inherits="Page_FS100500" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/TabView.master" %>
 
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="SetupRecord" SuspendUnloading="False" TypeName="PX.Objects.FS.CalendarComponentSetupMaint">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="Save" CommitChanges="True" />
            <px:PXDSCallbackCommand Name="Cancel" Visible="True" />
            <px:PXDSCallbackCommand Name="AppBoxFieldsPasteLineCmd" Visible="False" CommitChanges="True" DependOnGrid="appointmentBoxGrid" />
            <px:PXDSCallbackCommand Name="AppBoxFieldsResetLineCmd" Visible="False" CommitChanges="True" DependOnGrid="appointmentBoxGrid" />
            <px:PXDSCallbackCommand Name="SOGridFieldsPasteLineCmd" Visible="False" CommitChanges="True" DependOnGrid="serviceOrderGrid" />
            <px:PXDSCallbackCommand Name="SOGridFieldsResetLineCmd" Visible="False" CommitChanges="True" DependOnGrid="serviceOrderGrid" />
            <px:PXDSCallbackCommand Name="UAGridFieldsPasteLineCmd" Visible="False" CommitChanges="True" DependOnGrid="unassignedAppGrid" />
            <px:PXDSCallbackCommand Name="UAGridFieldsResetLineCmd" Visible="False" CommitChanges="True" DependOnGrid="unassignedAppGrid" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>

<asp:Content ID="cont3" ContentPlaceHolderID="phF" runat="Server">
    <px:PXTab ID="tab" runat="server" Width="100%" Height="400px" DataSourceID="ds" DataMember="SetupRecord">
        <Items>
            <px:PXTabItem Text="Status Color">
                <Template>
                    <px:PXFormView ID="frmStatusColor" runat="server" DataMember="StatusColorSelected">
                        <Template>
                            <px:PXLayoutRule runat="server" LabelsWidth="S" ControlSize="XM" />
                            <px:PXLayoutRule runat="server" GroupCaption="Selected Status" StartGroup="True"/>
                            <px:PXTextEdit ID="plBackgroundColor" runat="server" DataField="BackgroundColor" 
						                       CommitChanges="True" TextMode="Color" />
                            <px:PXTextEdit ID="plTextColor" runat="server" DataField="TextColor" 
						                       CommitChanges="True" TextMode="Color" />
                        </Template>
                    </px:PXFormView>
                    <px:PXGrid ID="statusColorGrid" runat="server" Width="100%" Style="z-index: 100"
                        AllowPaging="True" AllowSearch="True" AdjustPageSize="Auto" DataSourceID="ds" 
                        SkinID="Details" TabIndex="1900" SyncPosition="True" KeepPosition="True">
                        <AutoCallBack Command="Refresh" Target="frmStatusColor" ActiveBehavior="true">
                            <Behavior RepaintControlsIDs="frmStatusColor" />
                        </AutoCallBack>
                        <Levels>
                            <px:PXGridLevel DataMember="StatusColorRecords">
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                    <px:PXTextEdit ID="edStatusID" runat="server" DataField="StatusID" CommitChanges="True"/>
                                    <px:PXTextEdit ID="edStatusLabel" runat="server" DataField="StatusLabel" />
                                    <px:PXTextEdit ID="edBackgroundColor" runat="server" DataField="BackgroundColor" 
                                    CommitChanges="True"/>
                                    <px:PXTextEdit ID="edTextColor" runat="server" DataField="TextColor" CommitChanges="True"/>
                                    <px:PXCheckBox ID="edIsVisible" runat="server" DataField="IsVisible" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="StatusID" CommitChanges="True"/>
                                    <px:PXGridColumn DataField="StatusLabel"/>
                                    <px:PXGridColumn DataField="BackgroundColor"/>
                                    <px:PXGridColumn DataField="TextColor"/>
                                    <px:PXGridColumn DataField="IsVisible" Type="CheckBox"/>
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="True" ></AutoSize>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Appointment Box">
                <Template>
                    <px:PXGrid ID="appointmentBoxGrid" runat="server" Width="100%" Style="z-index: 100"
                        AllowPaging="True" AllowSearch="True" AdjustPageSize="Auto" DataSourceID="ds" 
                        SkinID="Details" TabIndex="1900" MatrixMode="true" SyncPosition="True">
                        <Levels>
                            <px:PXGridLevel DataMember="AppointmentBoxFields" DataKeyNames="ComponentType,ObjectName,FieldName">
                                <RowTemplate>
                                    <px:PXDropDown ID="edImageUrl" runat="server" DataField="ImageUrl" />
                                    <px:PXDropDown ID="edObjectNameApp" runat="server" DataField="ObjectName" />
                                    <px:PXDropDown ID="edFieldNameApp" runat="server" DataField="FieldName" />
                                    <px:PXCheckbox ID="edIsActive" runat="server" DataField="IsActive" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="ImageUrl" Width="120px" AllowDragDrop="True" />
                                    <px:PXGridColumn DataField="ObjectName" Width="150px" Type="DropDownList" CommitChanges="True" AutoCallBack="True" AllowDragDrop="True" />
                                    <px:PXGridColumn DataField="FieldName" Width="150px" Type="DropDownList" AutoCallBack="True" AllowDragDrop="True" />
                                    <px:PXGridColumn DataField="IsActive" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <CallbackCommands PasteCommand="AppBoxFieldsPasteLineCmd">
                            <Save PostData="Container" />
                        </CallbackCommands>
                        <AutoSize Enabled="True" MinHeight="200" />
                        <ActionBar ActionsText="False"></ActionBar>
                        <Mode InitNewRow="True" AllowUpload="True" AllowDragRows="True" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Service Order">
                <Template>
                    <px:PXGrid ID="serviceOrderGrid" runat="server" Width="100%" Style="z-index: 100"
                        AllowPaging="True" AllowSearch="True" AdjustPageSize="Auto" DataSourceID="ds" 
                        SkinID="Details" TabIndex="1900" MatrixMode="true" SyncPosition="True">
                        <Levels>
                            <px:PXGridLevel DataMember="ServiceOrderFields" DataKeyNames="ComponentType,ObjectName,FieldName">
                                <RowTemplate>
                                    <px:PXDropDown ID="edSOObjectNameApp" runat="server" DataField="ObjectName" />
                                    <px:PXDropDown ID="edSOFieldNameApp" runat="server" DataField="FieldName" />
                                    <px:PXCheckbox ID="edSOIsActive" runat="server" DataField="IsActive" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="ObjectName" Width="150px" Type="DropDownList" CommitChanges="True" AutoCallBack="True" AllowDragDrop="True" />
                                    <px:PXGridColumn DataField="FieldName" Width="150px" Type="DropDownList" AutoCallBack="True" AllowDragDrop="True" />
                                    <px:PXGridColumn DataField="IsActive" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <CallbackCommands PasteCommand="SOGridFieldsPasteLineCmd">
                            <Save PostData="Container" />
                        </CallbackCommands>
                        <AutoSize Enabled="True" MinHeight="200" />
                        <ActionBar ActionsText="False"></ActionBar>
                        <Mode InitNewRow="True" AllowUpload="True" AllowDragRows="True" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Unassigned Appointment">
                <Template>
                    <px:PXGrid ID="unassignedAppGrid" runat="server" Width="100%" Style="z-index: 100"
                        AllowPaging="True" AllowSearch="True" AdjustPageSize="Auto" DataSourceID="ds" 
                        SkinID="Details" TabIndex="1900" MatrixMode="true" SyncPosition="True">
                        <Levels>
                            <px:PXGridLevel DataMember="UnassignedAppointmentFields" DataKeyNames="ComponentType,ObjectName,FieldName">
                                <RowTemplate>
                                    <px:PXDropDown ID="edUAObjectNameApp" runat="server" DataField="ObjectName" />
                                    <px:PXDropDown ID="edUAFieldNameApp" runat="server" DataField="FieldName" />
                                    <px:PXCheckbox ID="edUAIsActive" runat="server" DataField="IsActive" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="ObjectName" Width="150px" Type="DropDownList" CommitChanges="True" AutoCallBack="True" AllowDragDrop="True" />
                                    <px:PXGridColumn DataField="FieldName" Width="150px" Type="DropDownList" AutoCallBack="True" AllowDragDrop="True" />
                                    <px:PXGridColumn DataField="IsActive" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <CallbackCommands PasteCommand="UAGridFieldsPasteLineCmd">
                            <Save PostData="Container" />
                        </CallbackCommands>
                        <AutoSize Enabled="True" MinHeight="200" />
                        <ActionBar ActionsText="False"></ActionBar>
                        <Mode InitNewRow="True" AllowUpload="True" AllowDragRows="True" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
        </Items>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
    </px:PXTab>
</asp:Content>