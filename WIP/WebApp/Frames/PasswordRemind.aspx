<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPages/Login.master" ClientIDMode="Static" AutoEventWireup="true" 
	CodeFile="PasswordRemind.aspx.cs" Inherits="Frames_PasswordRemind" EnableEventValidation="false" ValidateRequest="false" %>

<%@ MasterType VirtualPath="~/MasterPages/Login.master" %>

<asp:Content ID="Content3" ContentPlaceHolderID="phUser" runat="Server">
	<asp:Label runat="server" ID="lblTitle" Text="Credential recovery" CssClass="signin_caption" Visible="true" />
	<asp:TextBox ID="edLogin" runat="server" CssClass="login_user border-box" placeholder="Your Username" />
	
    <asp:DropDownList runat="server" ID="cmbCompany" CssClass="login_company border-box" AutoPostBack="true" OnSelectedIndexChanged="cmbCompany_OnSelectedIndexChanged"/>
    <input runat="server" id="txtDummyCpny" type="hidden" />
	<div runat="server" id="loginButtonsContainer">
	<asp:Button ID="btnSubmit" runat="server" Text="Submit" OnClick="btnSubmit_Click" CssClass="login_button" />
	<asp:HyperLink ID="lnkForgotLogin" runat="server" Text="Forgot your username?" CssClass="login_link" />
	</div>
</asp:Content>

<asp:Content ID="Content5" ContentPlaceHolderID="phLinks" runat="Server">
</asp:Content>

<asp:Content ID="Content6" ContentPlaceHolderID="phStart" runat="Server">
	<script type='text/javascript'>
		window.onload = function ()
		{
			var editor = document.form1['edLogin'];
			if (editor && !editor.readOnly) editor.focus();
			document.getElementById("login_data").style.paddingBottom = (document.getElementById("login_copyright").clientHeight + 40) + "px";
		}
	</script>
</asp:Content>
