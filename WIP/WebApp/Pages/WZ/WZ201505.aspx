﻿<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormView.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="WZ201505.aspx.cs" Inherits="Page_WZ201505"
	Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormView.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.WZ.WizardNotActiveScenario" PrimaryView="Scenario" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="phF" runat="Server">
    
        <style type="text/css">
        .phF
        {
            padding-left: 40px;
            padding-right: 20px;
            padding-top: 30px;
            font-family: Arial;
        }
        .errCode
        {
            padding-bottom: 20px;
            font-family: Arial;
            font-size: 15pt;
        }
        .errMsg
        {
            font-size: 12pt;
        }
        .img
        {
            float: left;
            margin-right: 10px;
        }
        .nxtSt
        {
            margin-top: 30px;
            font-family: Arial;
            font-size: 15pt;
        }
        .navTo
        {
            margin-top: 10px;
            margin-left: 20px;
        }
        .errPnl
        {
            padding: 10px;
            padding-top: 15px;
        }
        .grayBox
        {
            border: solid 1px #CCC;
            background-color: #F9F9F9;
            padding-top: 20px;
            padding-bottom: 25px;
            padding-left: 10px;
            padding-right: 20px;
        }
        .activateBtn {
            margin-top: 10px;
        }
    </style>
        
        <px:PXFormView ID="frmBottom" runat="server" SkinID="Transparent" DataMember="Scenario" DataSourceID="ds">
            <Template>
                <px:PXTextEdit runat="server" DataField="ScenarioID" Visible="False"></px:PXTextEdit>
                <div class="errCode">
                    <px:PXLabel ID="lblErrCode" runat="server" Text="" CssClass="errCode"></px:PXLabel>
                    <px:PXLabel ID="lblErrCodeEnding" runat="server" Text=" scenario is not active" CssClass="errCode"></px:PXLabel>
                </div>
                <div class="grayBox">
                    <div class="img">
                        <asp:Image ID="imgMessage" runat="server" ImageUrl="~/App_Themes/Default/Images/Message/warning.gif" />
                    </div>
                    <div class="errMsg">
                        <px:PXLabel ID="lblMessage" CssClass="errMsg" runat="server" 
                            Text="The scenario is not active. ">
                        </px:PXLabel>
                        <px:PXLabel ID="lastExecTimePre"  CssClass="errMsg" runat="server" Text="The last execution date is"></px:PXLabel>
                        <px:PXLabel ID="lastExecTimeVal"  CssClass="errMsg" runat="server" Text=""></px:PXLabel>
                        <px:PXLabel ID="lastExecByPre"  CssClass="errMsg" runat="server" Text="by"></px:PXLabel>
                        <px:PXLabel ID="lastExecByVal"  CssClass="errMsg" runat="server" Text=""></px:PXLabel>
                        <px:PXLabel ID="wantActivateQstn"  CssClass="errMsg" runat="server" Text="To activate it, click the Activate Scenario button."></px:PXLabel>
                    </div>
                </div>
                <div class="activateBtn">
                    <asp:Button ID="btnActivate" runat="server" Text="Activate Scenario" OnClick="btnActivate_Click" ></asp:Button>
					<asp:Button ID="btnHistory" runat="server" Text="View Execution History" OnClick="btnHistory_Click" ></asp:Button>
                </div>        
            </Template>
            <AutoSize Enabled="True" Container="Window" />
        </px:PXFormView>
        
    
</asp:Content>