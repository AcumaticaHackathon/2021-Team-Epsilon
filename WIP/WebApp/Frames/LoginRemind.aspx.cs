using System;
using System.Collections;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CommonServiceLocator;
using PX.Data;
using PX.Web.UI;

public partial class Frames_LoginRemind : Page
{
	private readonly ILoginUiService _loginUiService = ServiceLocator.Current.GetInstance<ILoginUiService>();

	protected void Page_Init(object sender, EventArgs e)
	{
		this.lblTitle.Text = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.CredentialsRecovery);
		string[] companies = PXDatabase.Companies;
		if (companies.Length == 0 || PXDatabase.SecureCompanyID)
		{
			this.cmbCompany.Visible = false;
		}
		else if (this.cmbCompany.Items.Count == 0)
		{
			for (int i = 0; i < companies.Length; i++) this.cmbCompany.Items.Add(companies[i]);
			string company = Request.QueryString.Get("Company");
			if (company != null) this.cmbCompany.SelectedIndex = Int32.Parse(company);
		}

		edEmail.Attributes["placeholder"] = PXMessages.LocalizeNoPrefix(Msg.ReminderPageForgotLogin);
		btnSubmit.Text = PXMessages.LocalizeNoPrefix(Msg.ReminderPageSubmitButton);
	}

	protected void btnSubmit_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrEmpty(edEmail.Text))
		{
			this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.EmailEmpty);
			return;
		}

		if (Request.QueryString.GetValues("ReturnUrl") == null)
		{
			this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.InvalidQueryString);
			return;
		}

		string link = PX.Common.PXUrl.SiteUrlWithPath().TrimEnd('/');
		link += "/Frames/Login.aspx";
		link += "?ReturnUrl=" + HttpUtility.UrlEncode(Request.QueryString.GetValues("ReturnUrl")[0]);

		string[] companies = PXDatabase.Companies;
		bool anySuccess = false; string errorMsg = null;
		if (companies.Length > 0)
		{
			if (!PXDatabase.SecureCompanyID)
				companies = new string[] { cmbCompany.SelectedItem.Value };
			
			foreach (string companyID in companies)
			{
				try
				{
					PXDatabase.ResetCredentials();
					_loginUiService.SendUserLogin(companyID, edEmail.Text, link);
					anySuccess = true;
				}
				catch (Exception ex)
				{
					errorMsg = ex.Message;
				}
			}
		}
		else
		{
			try
			{
				_loginUiService.SendUserLogin(null, edEmail.Text, link);
				anySuccess = true;
			}
			catch (Exception ex)
			{
				errorMsg = ex.Message;
			}
		}
		
		if (anySuccess)
		{
			//lblMsg.ForeColor = System.Drawing.Color.Black;
			this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.PasswordSent);
			link = ControlHelper.PreventXSSAttacks(link);
			LiteralControl metaRefresh = new LiteralControl("<meta http-equiv=\"Refresh\" content=\"4;URL=" + link + "\" />");
			Page.Header.Controls.Add(metaRefresh);
		}
		else if (!String.IsNullOrEmpty(errorMsg))
		{
			this.Master.Message = errorMsg;
		}
	}
}
