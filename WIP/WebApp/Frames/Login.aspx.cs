using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using CommonServiceLocator;
using PX.Common;
using PX.Data;
using PX.Data.Auth;
using PX.Data.Maintenance;
using PX.SM;
using PX.Web.UI;
using PX.Data.MultiFactorAuth;
using PX.Licensing;
using PX.OidcClient;
using PX.AscxControlsMessages;

public partial class Frames_Login : System.Web.UI.Page
{
	#region Fields

	private const string Ismultifactorpasswordchange = "IsMultiFactorPasswordChange";
	private const string OidcCommandPrefix = "Oidc_";
	private const string CompanyIdCookieName = "CompanyID";
	private IMultiFactorService _multifactorService = ServiceLocator.Current.GetInstance<IMultiFactorService>();
	internal IReadOnlyDictionary<string, ITwoFactorSender> MultifactorProviders = ServiceLocator.Current.GetInstance<IReadOnlyDictionary<string, ITwoFactorSender>>();
	private readonly IExternalAuthenticationUiService _externalAuthenticationService = ServiceLocator.Current.GetInstance<IExternalAuthenticationUiService>();
	private readonly ILoginUiService _loginUiService = ServiceLocator.Current.GetInstance<ILoginUiService>();
	private readonly ILicensing _licensing = ServiceLocator.Current.GetInstance<ILicensing>();
	private readonly IOidcUiService _oidcUiService = ServiceLocator.Current.GetInstance<IOidcUiService>();

	private bool _passwordRecoveryLinkExpired = false;
	private bool MultiCompaniesSecure
	{
		get
		{
			return PXDatabase.SecureCompanyID &&
					Membership.Provider is PXBaseMembershipProvider &&
					_loginUiService.GetCompanies(txtUser.Text, txtPass.Text).Length > 1;
		}
	}
	private bool _signInWith = false;
	#endregion

	#region Event handlers
	/// <summary>
	/// 
	/// </summary>
	protected void Page_Init(object sender, EventArgs e)
	{
		ControlHelper.CheckBrowserSupported(this);

		InitialiseRemindLink();
		PXContext.Session.SetString("LastUrl", null);

		// if we have troubles with this functions and it is not postback
		// then we should notify user about problems with database
		try
		{
			FillCompanyCombo();
			FillLocalesCombo();
			InitialiseExternalLogins();
		}
		catch
		{
			if (GetPostBackControl(this.Page) == null)
			{
				this.btnLogin.Visible = false;
				this.Master.Message = "Database could not be accessed";
			}
		}
		this.ClientScript.RegisterClientScriptInclude(this.GetType(), "jq", VirtualPathUtility.ToAbsolute("~/Scripts/jquery-3.1.1.min.js"));
		this.ClientScript.RegisterClientScriptInclude(this.GetType(), "jqsr", VirtualPathUtility.ToAbsolute("~/Scripts/jquery.signalR-2.2.1.min.js"));
		this.ClientScript.RegisterClientScriptInclude(this.GetType(), "hb", VirtualPathUtility.ToAbsolute("~/signalr/hubs"));
	}

	/// <summary>
	/// 
	/// </summary>
	protected void Page_Load(object sender, EventArgs e)
    {
		using (PXAccess.GetAdminLoginScope(this.cmbCompany.SelectedValue))
		{
		if (cmbLang.SelectedValue != null)
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cmbLang.SelectedValue);
			btnLogin.Text = PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.SignIn);
			txtUser.Attributes["placeholder"] = PXMessages.LocalizeNoPrefix(LoginScreen.Username);
			txtPass.Attributes["placeholder"] = PXMessages.LocalizeNoPrefix(LoginScreen.Password);
			txtNewPassword.Attributes["placeholder"] = PXMessages.LocalizeNoPrefix(Msg.LoginPageNewPassword);
			txtConfirmPassword.Attributes["placeholder"] = PXMessages.LocalizeNoPrefix(Msg.LoginPageConfirmPassword);
			lnkForgotPswd.Text = PXMessages.LocalizeNoPrefix(Msg.LoginPageForgotCredentials);
			lblSignIn.Text = _signInWith ? PXMessages.LocalizeNoPrefix(LoginScreen.SignInWithLabel) : PXMessages.LocalizeNoPrefix(LoginScreen.EnterCredentials);
			lblTenant.Text = PXMessages.LocalizeNoPrefix(PXDatabase.SecureCompanyID ? LoginScreen.Tenant : LoginScreen.SelectTenant);
		}

        lbl2FactorCap.Text = PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.TwoFactorAuth);
        lbl2FactorMethod.Text = PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.TwoFactorSelectMethod);
        rememberDevice.Text = PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.RememberDevice);


		if (PX.Data.Update.PXUpdateHelper.CheckUpdateLock()) 
			throw new PXUnderMaintenanceException();

		var lockoutStatus = PXSiteLockout.GetStatus(true);
		if (lockoutStatus == PXSiteLockout.Status.Locked)
		{
			lblUnderMaintenance.Text = PXMessages.Localize(PX.Data.Update.Messages.SiteUnderMaintenance);
			lblUnderMaintenance.Visible = true;

			if (!string.IsNullOrWhiteSpace(PXSiteLockout.Message))
			{
				lblUnderMaintenanceReason.Text = string.Format(
					PXMessages.Localize(PX.Data.Update.Messages.LockoutReason), PXSiteLockout.Message);
				lblUnderMaintenanceReason.Visible = true;
			}
		}

		if (lockoutStatus == PXSiteLockout.Status.Pending)
		{
			string datetime = string.Format("{0} ({1} UTC)", PXSiteLockout.DateTime, PXSiteLockout.DateTimeUtc);
			lblUnderMaintenance.Text = string.Format(
				PXMessages.Localize(PX.Data.Update.Messages.PendingLockout),

				datetime, PXSiteLockout.Message);
			lblUnderMaintenance.Visible = true;
		}
		}

		if (GetPostBackControl(this.Page) == cmbCompany || GetPostBackControl(this.Page) == cmbLang) txtPass.Attributes.Add("value", txtPass.Text);

		if (GetPostBackControl(this.Page) == btnLogin && !String.IsNullOrEmpty(txtDummyCpny.Value))
			cmbCompany.SelectedValue = txtDummyCpny.Value;

        if (string.IsNullOrWhiteSpace(MultiFactorPipelineNotStarted.Value))
        {
            MultiFactorPipelineNotStarted.Value = "true";
        }

		// if user already set password then we should disabling login and password
		if (!String.IsNullOrEmpty(txtVeryDummyPass.Value))
		{
			txtPass.Text = txtVeryDummyPass.Value;
			DisablingUserPassword();
            if (!MultiCompaniesSecure && string.IsNullOrEmpty(MultiFactorWarninigWasShown.Value))
                EnablingChangingPassword();
		}

		// if (SecureCompanyID) then we should hide combobox before first login.
		// and also we should shrink companies list
		if (PXDatabase.SecureCompanyID && (Membership.Provider is PXBaseMembershipProvider))
		{
			this.cmbCompany.Visible = !String.IsNullOrEmpty(txtVeryDummyPass.Value);

			if (!String.IsNullOrEmpty(txtVeryDummyPass.Value))
			{
				List<String> companyFilter = new List<String>(_loginUiService.GetCompanies(txtUser.Text, txtVeryDummyPass.Value));
				for (int i = cmbCompany.Items.Count - 1; i >= 0; i--)
				{
					ListItem item = cmbCompany.Items[i];
					if (!companyFilter.Contains(item.Value)) cmbCompany.Items.RemoveAt(i);
				}
			}
		}

		this.txtSecureTenantNameDisplay.Attributes.Add("readonly", "readonly");
		if (SecureTenantFlowState == SecureTenantFlowStates.Initial && !IsPostBack && TryEnterSecureTenantName(GetTenantName()))
		{
			// TryEnterSecureTenantName includes UpdateSecureTenantFlowControls
		}
		else
		{
			UpdateSecureTenantFlowControls();
		}

		// Is user trying to recover his password using link from Email?
		if (Request.QueryString.AllKeys.Length > 0 && Request.QueryString.GetValues("gk") != null)
		{
			RemindUserPassword();
		}
		try
		{
			this.SetInfoText();
		}
		catch { /*SKIP ERROS*/ }
		//try silent login
		btnLoginSilent_Click(sender, e);
	}

	/// <summary>
	/// Fill the info about system,
	/// </summary>
	private void SetInfoText()
	{
		string copyR = PXVersionInfo.Copyright;
		txtDummyInstallationID.Value = _licensing.PrettyInstallationId;

		bool hasError = false;
		if (!PX.Data.Update.PXUpdateHelper.ChectUpdateStatus())
		{
			this.updateError.Style["display"] = "";
			hasError = true;
		}

		this.logOutReasoneMsg.InnerText = PXMessages.LocalizeNoPrefix(Msg.UpdateError);
		this.dbmsProblems.InnerText = PXMessages.LocalizeNoPrefix(Msg.DbmsProblems);
		this.dbmsMisconfiguredLabel.InnerText = PXMessages.LocalizeNoPrefix(Msg.ContactServerAdministratorLabel);
		this.updateErrorMsg.InnerText = PXMessages.LocalizeNoPrefix(Msg.UpdateError);
		this.updateErrorLabel.InnerText = PXMessages.LocalizeNoPrefix(Msg.ContactServerAdministratorLabel);
		this.customizationErrorMsg.InnerText = PXMessages.LocalizeNoPrefix(Msg.CustomizationError);
		this.customizationErrorLabel.InnerHtml = PXMessages.LocalizeFormatNoPrefix(Msg.CustomizationErrorLabel, "<a href=\"#\" onclick=\"document.getElementById('custErrorDetails').style.display='';\">", "</a>");

		if (Request.QueryString["licenseexceeded"] != null)
		{
			this.logOutReasone.Style["display"] = "";
			this.logOutReasoneMsg.InnerText = PXMessages.LocalizeFormatNoPrefix(
				PX.Data.ActionsMessages.LogoutReason, Request.QueryString["licenseexceeded"]);
			hasError = true;
		}
		else if (Request.QueryString[Constants.ExceptionIdQueryParameter] != null)
		{
			PXException exception = PXContext.Session.Exception[Request.Params[Constants.ExceptionIdQueryParameter]] as PXException;
			if (exception != null)
			{
				this.logOutReasone.Style["display"] = "";
				this.logOutReasoneMsg.InnerText = exception.MessageNoPrefix;
				hasError = true;
			}
		}
		else if (Request.QueryString["message"] != null)
		{
			this.logOutReasone.Style["display"] = "";
			this.logOutReasoneMsg.InnerText = PXMessages.LocalizeNoPrefix(Request.QueryString["message"]);
			hasError = true;
		}
		else if (_passwordRecoveryLinkExpired)
		{
			this.passwordRecoveryError.Style["display"] = "";
			this.passwordRecoveryErrorMsg.InnerText = PXMessages.LocalizeFormatNoPrefix(ErrorMessages.PasswordRecoveryLinkExpired);
			hasError = true;
		}
		else if (PXDatabase.Companies.Length > PXDatabase.AvailableCompanies.Length)
		{
			this.logOutReasone.Style["display"] = "";
			this.logOutReasoneMsg.InnerText = PXMessages.LocalizeNoPrefix(PX.Data.ActionsMessages.CompaniesOverlimit);
			hasError = true;
		}

		List<string> dbProblems = new List<string>();
		if (!PXDatabase.Provider.CreateDbServicesPoint().IsDatabaseReadyToWork(ref dbProblems))
		{
			this.dbmsMisconfigured.Style["display"] = "";
			this.dbmsProblems.InnerHtml += "<UL><li>" + String.Join("</li><li>", dbProblems) + "</li></ul>";
			hasError = true;
		}


		// sets the customization info text
		string status = Customization.CstWebsiteStorage.GetUpgradeStatus();
		if (!String.IsNullOrEmpty(status))
		{
			this.customizationError.Style["display"] = "";
			this.custErrorContent.InnerText = status;
			hasError = true;
		}
		login_info.Style[HtmlTextWriterStyle.Display] = hasError ? "" : "none";
	}

	/// <summary>
	/// The page Init event handler.
	/// </summary>
	protected override void OnInit(EventArgs e)
	{
		base.OnInit(e);
	}

	protected void cmbCompany_SelectedIndexChanged(object sender, EventArgs e)
	{
		InitialiseLocales();
		InitialiseExternalLogins();
		InitialiseRemindLink();
	}

	private void InitialiseLocales()
	{
		string lang = cmbLang.SelectedValue;
		cmbLang.Items.Clear(); FillLocalesCombo();
		if (!string.IsNullOrEmpty(lang)) cmbLang.SelectedValue = lang;
	}

	protected void btnMoreOpenIdProviders_Click(object sender, EventArgs e)
	{
		UpdateSecureTenantFlowState(SecureTenantFlowStates.EnterTenantName);
	}

	protected void btnSecureTenantSubmit_Click(object sender, EventArgs e)
	{
		if (TryEnterSecureTenantName(this.txtSecureTenantName.Text))
		{
			InitialiseExternalLogins();
		}
	}

	protected void btnSecureTenantCancel_Click(object sender, EventArgs e)
	{
		UpdateSecureTenantFlowState(SecureTenantFlowStates.Initial);
		this.txtSecureTenantNameDisplay.Text = null;
	}
	#endregion

	#region Login methods
	/// <summary>
	/// The login button event handler.
	/// </summary>
	protected void btnLoginOAuth_Click(object sender, EventArgs e)
	{
		var company = GetLoginCompany();
	    var providerName = (sender as IButtonControl).CommandName;
	    _externalAuthenticationService.SignInOAuth(HttpContext.Current, providerName, company, cmbLang.SelectedValue);
	}

    private string GetLoginCompany()
    {
        var queryCompany = Page.Request.QueryString[PXUrl.CompanyID];
		if (PXDatabase.Companies.Length <= 0)
            return null;
        if (cmbCompany.SelectedIndex != -1 && !PXDatabase.SecureCompanyID)
           return cmbCompany.SelectedValue;
        if (string.IsNullOrEmpty(queryCompany))
            return GetExternalLoginCompany();
        return queryCompany;
    }

    protected void btnLoginFederation_Click(object sender, EventArgs e)
    {
        String company = GetLoginCompany();
        _externalAuthenticationService.SignInFederation(HttpContext.Current, company, cmbLang.SelectedValue);
    }

	protected void btnLoginOpenId_Click(object sender, EventArgs e)
	{
		String company = GetTenantName();
		var commandName = this.txtDummyOidcProviderName.Value;
		if (commandName.StartsWith(OidcCommandPrefix))
			commandName = commandName.Substring(OidcCommandPrefix.Length);
		Guid providerId = Guid.Parse(commandName.Replace('_', '-'));
		try
		{
			_oidcUiService.SignIn(providerId, HttpContext.Current, company, cmbLang.SelectedValue);
		}
		catch (PXException ex)
		{
			this.Master.Message = ex.MessageNoPrefix;
		}
	}

	protected void btnLoginSilent_Click(object sender, EventArgs e)
	{
		if (Request.QueryString[Constants.ExceptionIdQueryParameter] != null)
			return;
		String company = GetLoginCompany();
		_externalAuthenticationService.SignInSilent(HttpContext.Current, company, cmbLang.SelectedValue);
	}
	protected void btnLogin_Click(object sender, EventArgs e)
	{
		try
		{
			string loginText = txtUser.Text;
			if (loginText != null && loginText.Contains(":"))
			{
				this.Master.Message = PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.IncorrectLoginSymbols);
				return;
			}
			if (String.IsNullOrEmpty(loginText))
			{
				this.Master.Message = PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.InvalidLogin);
				return;
			}

			if (String.IsNullOrEmpty(txtNewPassword.Text) && String.IsNullOrEmpty(txtConfirmPassword.Text))
			{
			    string[] companies = _loginUiService.GetCompanies(loginText, txtPass.Text);
			    if (MultiCompaniesSecure && String.IsNullOrEmpty(txtVeryDummyPass.Value))
				{
					if (CanSkipLoginPassTenantSelectionState() && this.cmbCompany.Items.FindByValue(GetTenantName())?.Value is string selectedCompany)
					{
						this.cmbCompany.SelectedValue = selectedCompany;
						NormalLogin(companies);
					}
					else
					{
						SecureLogin(companies);
					}
				}
				else
				{
					NormalLogin(companies);
				}
			}
			else //if user should change it password than we will login different way
			{
				ChangingPassword();
			}
		}
		catch (PXException ex)
		{
			this.Master.Message = ex.MessageNoPrefix;
		}
		catch (System.Reflection.TargetInvocationException ex)
		{
			this.Master.Message = PXException.ExtractInner(ex).Message;
		}
		catch (Exception ex)
		{
			this.Master.Message = ex.Message;
		}
	}

    protected void btnCancel_Click(object sender, EventArgs e)
	{
		try
		{
			_externalAuthenticationService.CancelAssociate(HttpContext.Current);
		}
		catch (PXException ex)
		{
			this.Master.Message = ex.MessageNoPrefix;
		}
		catch (Exception ex)
		{
			this.Master.Message = ex.Message;
		}
	}

	//-----------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	private void NormalLogin(string[] companies)
	{
		if (companies != null && companies.Length == 1)
		{
			cmbCompany.Items.Clear();
			cmbCompany.Items.Add(companies[0]);
		}

		string loginText = txtUser.Text.Trim();
		string userName = PXDatabase.Companies.Length > 0 ? loginText + "@" +
			(cmbCompany.SelectedIndex != -1 ? cmbCompany.SelectedItem.Value : PXDatabase.Companies[0]) : loginText;
	    Tuple<int, Guid, bool> user;
	    ErrorReason? reason;
	    if (_multifactorService.IsAccessCodeValid(userName, txtPass.Text, oneTimePasswordText.Text, (object)Request, out user, out reason))
	    {
	        try
	        {
	            if (!_loginUiService.LoginUser(ref userName, txtPass.Text))
	            {
	                // we will change password during next round-trip
	                PXContext.Session.SetString("ChangingPassword", txtPass.Text);
	                if (user !=null&&user.Item3)
	                    PXContext.Session.SetValueType(Ismultifactorpasswordchange, user.Item3);

	                DisablingUserPassword();
	                EnablingChangingPassword();

	                this.Master.Message = string.Empty;
	            }
	            else
	            {
					if (reason == ErrorReason.FeatureDisabled && string.IsNullOrEmpty(MultiFactorWarninigWasShown.Value))
					{
						DisablingUserPassword();
						if (cmbCompany.SelectedIndex != -1) txtDummyCpny.Value = cmbCompany.SelectedItem.Text;
						this.cmbCompany.Visible = false;
						btnLogin.Text = "Sign In Anyway";
						btnLogin.CssClass += " login_button_wide";

						this.Master.Message = "Warning: " + PX.Data.ErrorMessages.CantSignInTwoFactorDisabled;
						MultiFactorWarninigWasShown.Value = "true";
					}
					else
					{
						_oidcUiService.TryAssociate(HttpContext.Current, userName);

						_loginUiService.InitUserEnvironment(userName, cmbLang.SelectedValue);
						if (this.rememberDevice.Checked && !string.IsNullOrWhiteSpace(oneTimePasswordText.Text))//we should not update remember cookie if we authenticate using it
							_multifactorService.RememberDevice(userName, txtPass.Text, HttpContext.Current);
					}

	            }
	        }
	        catch (PXException)
	        {
	            this.MultiFactorPipelineNotStarted.Value = "true";
	            throw;
	        }
	    }
	    else
	    {
	        this.MultiFactorPipelineNotStarted.Value = "true";
	        var cookie = Response.Cookies[_multifactorService.GetCookieName(userName)];
            if(cookie!=null)
	            cookie.Expires = DateTime.Now.AddDays(-1);
	        this.oneTimePasswordText.Text = string.Empty;
            throw new PXException(PX.Data.ErrorMessages.LoginOTPInvalid);
	    }
	}

	/// <summary>
	/// 
	/// </summary>
	protected void SecureLogin(string[] companies)
	{
		this.cmbCompany.Items.Clear();
		for (int i = 0; i < companies.Length; i++) this.cmbCompany.Items.Add(companies[i]);

		HttpCookie cookie = Request.Cookies[CompanyIdCookieName];
        String company = Page.Request.QueryString[PXUrl.CompanyID];
        if (!string.IsNullOrEmpty(company)&&companies.Contains(company))
        {
            this.cmbCompany.SelectedValue = company;
        }
		else if (cookie != null && !string.IsNullOrEmpty(cookie.Value) &&
			this.cmbCompany.Items.FindByValue(cookie.Value) != null)
		{
			this.cmbCompany.SelectedValue = cookie.Value;
		}
        else if (this.cmbCompany.Items.Count > 0)
		{
			this.cmbCompany.SelectedValue = this.cmbCompany.Items[0].Value;
		}

		DisablingUserPassword();
		this.cmbCompany.Visible = true;
		UpdateSecureTenantFlowState(SecureTenantFlowStates.LoginPassTenantSelection);
		//this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.PleaseSelectCompany);
	}

	//-----------------------------------------------------------------------------
	/// <summary>
	/// Perform the user password changing.
	/// </summary>
	protected void ChangingPassword()
	{
		string loginText = txtUser.Text;
		if (txtRecoveryAnswer.Visible && !_loginUiService.ValidateAnswer(PXDatabase.Companies.Length > 0 ?
			loginText + "@" + cmbCompany.SelectedItem.Value : loginText, txtRecoveryAnswer.Text))
		{
			this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.InvalidRecoveryAnswer);
		}
		if (txtNewPassword.Text != txtConfirmPassword.Text)
		{
			this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.PasswordNotConfirmed);
		}
		if ((string)PXContext.Session["ChangingPassword"] == txtNewPassword.Text)
		{
			this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.NewPasswordMustDiffer);
		}
		if (string.IsNullOrEmpty(txtNewPassword.Text))
		{
			this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.PasswordBlank);
		}

		string changingPass = (string)PXContext.Session["ChangingPassword"];
		if (!String.IsNullOrEmpty(this.Master.Message))
		{
			txtVeryDummyPass.Value = changingPass;
			DisablingUserPassword();
			EnablingChangingPassword();
			return;
		}

		string gk = Request.QueryString.Get("gk");

		if (gk == null && changingPass == null)
			return;

		string userName = PXDatabase.Companies.Length > 0
			? loginText + "@" + (cmbCompany.SelectedIndex != -1 ? cmbCompany.SelectedItem.Value : PXDatabase.Companies[0])
			: loginText;

		try
		{
			_loginUiService.LoginUser(
				ref userName,
				gk ?? changingPass,
				txtNewPassword.Text);
		}
		catch
		{
			txtVeryDummyPass.Value = changingPass;
			DisablingUserPassword();
			EnablingChangingPassword();

			throw;
		}

	    bool isMultiFactorEnabled;
	    string[] multifactorProviders;
	    bool isPasswordChanging;
	    var users = _multifactorService.GetUserIdsWithTwoFactorType(userName, txtNewPassword.Text, out isMultiFactorEnabled, out multifactorProviders, out isPasswordChanging);
	    if (isMultiFactorEnabled)
	    {
	        _loginUiService.LogoutUser(loginText);
            if(Request.QueryString["gk"]!=null)
                Page.Response.Redirect("~");
            else
                Page.Response.Redirect("~", true);
            return;
	    }

		_loginUiService.InitUserEnvironment(userName, cmbLang.SelectedValue);
		AgreeToEula(loginText);
	}

    #endregion

	#region Private methods
	//-----------------------------------------------------------------------------
	/// <summary>
	/// Fill the system locales drop-down.
	/// </summary>
	private void FillLocalesCombo()
	{
		try
		{
			if (cmbLang.Items.Count != 0) return;

			Boolean found = false;
			string login = !String.IsNullOrEmpty(txtUser.Text) ? txtUser.Text : "temp";
			if (PXDatabase.Companies.Length > 0)
			{
				string company = this.Request.Form[cmbCompany.UniqueID];
				if (string.IsNullOrEmpty(company))
					company = cmbCompany.SelectedIndex != -1 ? cmbCompany.SelectedItem.Value : PXDatabase.Companies[0];
				login += "@" + company;
			}
			PXLocale[] locales = PXLocalesProvider.GetLocales(login);

			foreach (PXLocale loc in locales)
			{
				ListItem item = new ListItem(loc.DisplayName, loc.Name);
				cmbLang.Items.Add(item);
				if (!found && Request.Cookies["Locale"] != null && Request.Cookies["Locale"]["Culture"] != null &&
					string.Compare(Request.Cookies["Locale"]["Culture"], item.Value, true) == 0)
				{
					cmbLang.SelectedValue = item.Value;
					found = true;
				}
			}

			String value = this.Request.Form[cmbLang.ClientID.Replace('_', '$')];
			if (!String.IsNullOrEmpty(value) && locales.Any(l => l.Name == value))
			{
				cmbLang.SelectedValue = value;
				found = true;
			}
			if (!string.IsNullOrEmpty(Page.Request.QueryString["LocaleID"]))
			{
				String locale = Page.Request.QueryString["LocaleID"];
				if (locales.Select(l => l.Name).Contains(locale))
					this.cmbCompany.SelectedValue = locale;
			}
			if (cmbLang.Items.Count == 1) cmbLang.Style[HtmlTextWriterStyle.Display] = "none";
			else cmbLang.Style[HtmlTextWriterStyle.Display] = null;
		}
		catch
		{
			cmbLang.Visible = false;
			this.btnLogin.Visible = false;
			this.Master.Message = "Database could not be accessed";
		}
	}

	//-----------------------------------------------------------------------------
	/// <summary>
	/// Fill the allowed companies drop-down.
	/// </summary>
	private void FillCompanyCombo()
	{
		string[] companies = PXDatabase.AvailableCompanies;
		if (companies.Length == 0)
		{
			this.cmbCompany.Visible = false;
		}
		else
		{
			this.cmbCompany.Items.Clear();
			for (int i = 0; i < companies.Length; i++) this.cmbCompany.Items.Add(companies[i]);

			if (companies.Length == 1)
			{
				this.cmbCompany.Visible = false;
				this.cmbCompany.SelectedValue = this.cmbCompany.Items[0].Value;
			}
			else
			{
				HttpCookie cookie = this.Request.Cookies[CompanyIdCookieName];
				if (cookie != null && !string.IsNullOrEmpty(cookie.Value))
					this.cmbCompany.SelectedValue = cookie.Value;
				if (!string.IsNullOrEmpty(Page.Request.QueryString[PXUrl.CompanyID]))
				{
					String company = Page.Request.QueryString[PXUrl.CompanyID];
					if (companies.Contains(company))
						this.cmbCompany.SelectedValue = company;
				}
			}
		}
	}

	/// <summary>
	/// Sets the password reminder url.
	/// </summary>
	private void InitialiseRemindLink()
	{
		string path = PX.Common.PXUrl.SiteUrlWithPath();

		path += path.EndsWith("/") ? "" : "/";
		if (Request.QueryString.Keys.Count != 0)
			lnkForgotPswd.NavigateUrl = path + "Frames/PasswordRemind.aspx" + Request.Url.Query;
		
		if (this.cmbCompany.SelectedIndex > 0)
			lnkForgotPswd.NavigateUrl += string.Format("&Company={0}",this.cmbCompany.SelectedIndex);
	}

	private string GetExternalLoginCompany()
	{
		if (PXDatabase.Companies.Length == 0) return null;
		string[] providers = this.Master.FindControl("phExt").Controls.OfType<LinkButton>().Where(b => !string.IsNullOrEmpty(b.CommandName)).Select(b => b.CommandName).ToArray();
        foreach (string company in PXDatabase.AvailableCompanies)
		{
			if (_externalAuthenticationService.FederatedLoginEnabled(company)) return company;
			foreach (string provider in providers)
			{
				if (_externalAuthenticationService.OAuthProviderLoginEnabled(provider, company)) return company;
			}
		}
		return null;
	}

	/// <summary>
	/// Initialise External Logins
	/// </summary>
	private void InitialiseExternalLogins()
	{
		lblSignIn.Text = PXMessages.LocalizeNoPrefix(LoginScreen.EnterCredentials);

		if (!this.btnLogin.Visible) return;

		var returnUrl = Request.Params.Get("ReturnUrl");
		var isOutlookPlugin = returnUrl != null && returnUrl.Contains("OU201000.aspx");
		if (isOutlookPlugin)
		{
			return;
		}

		try
		{
			bool multicompany = PXDatabase.Companies.Length > 0;
			string company = GetLoginCompany();
			bool oAuthEnabled = false;
			if (!multicompany || company != null)
			{
				this.btnLoginFederation.Visible = _externalAuthenticationService.FederatedLoginEnabled(company);

				foreach (var b in this.Master.FindControl("phExt").Controls.OfType<LinkButton>().Where(b => !string.IsNullOrEmpty(b.CommandName)))
				{
					b.Visible = _externalAuthenticationService.OAuthProviderLoginEnabled(b.CommandName, company);
					if (b.Visible) oAuthEnabled = true;
				}
			}

			string oidcTenant = GetTenantName();
			if (!multicompany || oidcTenant != null)
			{
				var phExtControls = this.Master.FindControl("phExt").Controls;
				var oldOidcControls = phExtControls.OfType<LinkButton>()
					.Where(b => b.CommandName.IsPresent() && b.CommandName.StartsWith(OidcCommandPrefix))
					.ToArray();
				oldOidcControls.ForEach(c => phExtControls.Remove(c));

				foreach (var oidcProvider in _oidcUiService.GetProviders(oidcTenant))
				{
					var providerId = oidcProvider.ProviderID.ToString().Replace('-', '_');
					var iconUrl = _oidcUiService.GetIconUrl(oidcProvider.ProviderID.Value);
					var providerBtn = new LinkButton
					{
						CommandName = OidcCommandPrefix + providerId,
						ID = OidcCommandPrefix + providerId,
						CssClass = "extlogin_wide_button",
						Text = $"<img src='{iconUrl}' alt='logo' /><span>{oidcProvider.Name}</span>",
						OnClientClick = "return OidcLoginOnClick(this);"
					};
					var insertIndex = phExtControls.IndexOf(btnMoreOpenIdProviders) - 1;
					phExtControls.AddAt(insertIndex, providerBtn);
					oAuthEnabled = true;
				}
			}
			
			if (oAuthEnabled || this.btnLoginFederation.Visible)
			{
				_signInWith = true;
				this.lblSignIn.Text = PXMessages.LocalizeNoPrefix(LoginScreen.SignInWithLabel);
			}

			this.btnCancel.Visible = _externalAuthenticationService.AssociateLoginEnabled(HttpContext.Current) || _oidcUiService.AssociateLoginEnabled(HttpContext.Current);
		}
		catch
		{
			this.btnCancel.Visible = false;
			this.btnLoginFederation.Visible = false;
			this.btnLoginGoogle.Visible = false;
			this.btnLoginMicrosoft.Visible = false;

			throw;
		}
	}

	//-----------------------------------------------------------------------------
	/// <summary>
	/// Disable the password field.
	/// </summary>
	private void DisablingUserPassword()
	{
		txtPass.ReadOnly = txtUser.ReadOnly = true;
		txtPass.BackColor = txtUser.BackColor = System.Drawing.Color.LightGray;

		if (!String.IsNullOrEmpty(txtPass.Text))
		{
			txtVeryDummyPass.Value = txtPass.Text;
			txtPass.Attributes.Add("value", txtPass.Text);
		}
	}

	/// <summary>
	/// Activate the password change mode.
	/// </summary>
	private void EnablingChangingPassword()
	{
		if (cmbCompany.SelectedIndex != -1) txtDummyCpny.Value = cmbCompany.SelectedItem.Text;
		cmbCompany.Enabled = cmbLang.Enabled = false;
		txtNewPassword.Visible = txtConfirmPassword.Visible = true;
		lnkForgotPswd.Visible = false;
		HandleEula(this.txtUser.Text, txtDummyCpny.Value);
	}
	private void HandleEula(string username, string company)
	{
		string fullname = string.IsNullOrEmpty(company) ?
												username :
												string.Format("{0}@{1}", username, company);
		if (username == "admin" && _loginUiService.EulaRequired(fullname))
		{
			PXContext.Session.SetString("EulaRequired", fullname);
			divEula.Visible = true;
			this.btnLogin.Enabled = this.chkEula.Checked;
		}
	}

	private void AgreeToEula(string username)
	{
		var fullname = (string)PXContext.Session["EulaRequired"];
		if (username == "admin" && !string.IsNullOrEmpty(fullname))
			_loginUiService.AgreeToEula(fullname);
	}

	/// <summary>
	/// 
	/// </summary>
	private static Control GetPostBackControl(Page page)
	{
		Control control = null;
		string ctrlname = page.Request.Params.Get("__EVENTTARGET");
		if (ctrlname != null && ctrlname != string.Empty)
		{
			control = page.FindControl(ctrlname);
		}
		else
		{
			foreach (string ctl in page.Request.Form)
			{
				Control c = page.FindControl(ctl);
				if (c is System.Web.UI.WebControls.Button) { control = c; break; }
			}
		}
		return control;
	}

	public string GetLoginMethodIcon(string key)
	{
		switch(key)
		{
			case "Email": return "email_outline";
			case "AccessCode": return "unlock";
			case "MobilePush": return "smartphone";
			case "Sms": return "message";
		}
		return string.Empty;
	}

	//-----------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	private void RemindUserPassword()
	{
		string login = "";
		string cid = null;
		if (PXDatabase.Companies.Length > 0 && Request.QueryString.GetValues("cid") != null)
		{
			cid = Request.QueryString.Get("cid");
			login = "temp@" + cid;
		}
		try
		{
			string username = _loginUiService.FindUserByHash(Request.QueryString.Get("gk"), login);
			if (username != null)
			{
				_passwordRecoveryLinkExpired = false;
				txtUser.Text = username;
				txtPass.Text = Request.QueryString.Get("gk");
				txtUser.ReadOnly = true;
				txtUser.BackColor = System.Drawing.Color.LightGray;

				lnkForgotPswd.Visible = false;
				txtPass.Visible = false;
				txtPass.TextMode = TextBoxMode.SingleLine;
				txtDummyPass.Text = txtPass.Text;
				txtDummyPass.Visible = true;
				txtNewPassword.Visible = txtConfirmPassword.Visible = true;

				txtRecoveryQuestion.Text = _loginUiService.FindQuestionByUsername(username, login);
				if (!string.IsNullOrEmpty(txtRecoveryQuestion.Text))
				{
					txtRecoveryQuestion.ReadOnly = true;
					txtRecoveryQuestion.BackColor = System.Drawing.Color.LightGray;
					txtRecoveryQuestion.Visible = true;
					txtRecoveryAnswer.Visible = true;
				}

				//this.Master.Message = PX.Data.PXMessages.LocalizeNoPrefix(PX.AscxControlsMessages.LoginScreen.PleaseChangePassword);
				if (cid != null)
				{
					this.cmbCompany.SelectedValue = cid;
					this.cmbCompany.Enabled = false;
					this.txtDummyCpny.Value = this.cmbCompany.SelectedValue;
				}
			}
		}
		catch (PXPasswordRecoveryExpiredException)
		{
			_passwordRecoveryLinkExpired = true;
		}
	}

	private string ValidateAndNormalizeTenant(string tenant)
	{
		return PXDatabase.AvailableCompanies.FirstOrDefault(c => c.Equals(tenant, StringComparison.OrdinalIgnoreCase));
	}

	private string GetTenantName()
	{
		if (PXDatabase.SecureCompanyID)
		{
			if (this.txtSecureTenantNameDisplay.Text.IsPresent())
			{
				return this.txtSecureTenantNameDisplay.Text;
			}

			if (Page.Request.QueryString[PXUrl.CompanyID].IsPresent())
			{
				return Page.Request.QueryString[PXUrl.CompanyID];
			}

			HttpCookie cookie = this.Request.Cookies[CompanyIdCookieName];
			if (cookie != null && cookie.Value.IsPresent())
				return cookie.Value;

			return null;
		}
		else
		{
			return cmbCompany.SelectedValue;
		}
	}

	private bool TryEnterSecureTenantName(string enteredTenantName)
	{
		if (enteredTenantName.IsMissing())
			return false;

		var validTenant = ValidateAndNormalizeTenant(enteredTenantName);

		if (validTenant.IsPresent())
			enteredTenantName = validTenant;

		this.txtSecureTenantNameDisplay.Text = enteredTenantName;
		if (this.cmbCompany.Items.FindByValue(enteredTenantName)?.Value is string selectedCompany)
		{
			this.cmbCompany.SelectedValue = selectedCompany;
			InitialiseLocales();
			InitialiseRemindLink();
		}
		UpdateSecureTenantFlowState(validTenant.IsPresent()
			? SecureTenantFlowStates.TenantNameEntered
			: SecureTenantFlowStates.InvalidTenantNameEntered);
		return true;
	}

	private enum SecureTenantFlowStates
	{
		Initial = 0,
		LoginPassTenantSelection = 1,
		EnterTenantName = 2,
		TenantNameEntered = 3,
		InvalidTenantNameEntered = 4,
	}

	private SecureTenantFlowStates SecureTenantFlowState
	{
		get
		{
			return Enum.TryParse<SecureTenantFlowStates>(this.txtSecureTenantFlowState.Value, out var state)
				? state
				: SecureTenantFlowStates.Initial;
		}
		set
		{
			this.txtSecureTenantFlowState.Value = value.ToString();
		}
	}

	/// <summary>
	/// Updates value of flag and state of corresponding controls 
	/// </summary>
	private void UpdateSecureTenantFlowState(SecureTenantFlowStates value)
	{
		SecureTenantFlowState = value;
		UpdateSecureTenantFlowControls();
	}

	private void UpdateSecureTenantFlowControls()
	{
		bool isMultiCompany = PXDatabase.Companies.Length > 0;
		bool isSecure = PXDatabase.SecureCompanyID;
		var state = SecureTenantFlowState;

		if (!isMultiCompany)
		{
			this.tenantContainer.Visible = false;
		}
		else if (isSecure)
		{
			// default values
			ShowOnClient(this.tenantContainer, this.loginPasswordContainer, this.loginButtonsContainer);
			this.lblTenant.Text = PXMessages.LocalizeNoPrefix(LoginScreen.Tenant);
			this.txtSecureTenantName.Attributes["placeholder"] = PXLocalizer.Localize(LoginScreen.TenantName);
			this.txtSecureTenantName.Visible = false;
			HideOnClient(this.secureTenantNameDisplayContainer);
			this.btnSecureTenantSubmit.Text = PXLocalizer.Localize(LoginScreen.Next);
			this.btnSecureTenantSubmit.Visible = false;
			this.btnSecureTenantCancel.Text = PXLocalizer.Localize(LoginScreen.Cancel);
			this.btnSecureTenantCancel.Visible = false;
			this.Master.FindControl("phExt").Visible = true;
			this.btnMoreOpenIdProviders.Text = $"<img src='../Icons/moreOidcProviders.png' alt='logo' /><span>{PXMessages.LocalizeNoPrefix(LoginScreen.MoreSignInOptionsForTenant)}</span>";
			this.btnMoreOpenIdProviders.Visible = false;
			this.Master.Message = null; // todo: replace by separate error label
			
			switch (state)
			{
				case SecureTenantFlowStates.Initial:
					{
						btnLogin.Text = PXMessages.LocalizeNoPrefix(LoginScreen.Next);
						HideOnClient(this.tenantContainer);
						this.btnMoreOpenIdProviders.Visible = _oidcUiService.AnyProviders();
						break;
					}
				case SecureTenantFlowStates.LoginPassTenantSelection:
					{
						this.lblTenant.Text = PXMessages.LocalizeNoPrefix(LoginScreen.SelectTenant);
						btnLogin.Text = PXMessages.LocalizeNoPrefix(LoginScreen.SignIn);
						HideOnClient(this.loginPasswordContainer);
						this.Master.FindControl("phExt").Visible = false;
						break;
					}
				case SecureTenantFlowStates.EnterTenantName:
					{
						this.lblTenant.Text = PXMessages.LocalizeNoPrefix(LoginScreen.EnterTenantsName);
						this.txtSecureTenantName.Visible = true;
						this.btnSecureTenantSubmit.Visible = true;
						this.btnSecureTenantCancel.Visible = true;
						HideOnClient(this.loginPasswordContainer, this.loginButtonsContainer);
						this.Master.FindControl("phExt").Visible = false;
						break;
					}
				case SecureTenantFlowStates.TenantNameEntered:
					{
						ShowOnClient(this.secureTenantNameDisplayContainer);
						break;
					}
				case SecureTenantFlowStates.InvalidTenantNameEntered:
					{
						ShowOnClient(this.secureTenantNameDisplayContainer);
						this.Master.Message = PXMessages.LocalizeNoPrefix(LoginScreen.NoSignInMethodsForThisTenant); // todo: replace by separate error label
						HideOnClient(this.loginPasswordContainer, this.loginButtonsContainer);
						this.Master.FindControl("phExt").Visible = false;
						break;
					}
				default:
					break;
			}
		}

	}

	private bool CanSkipLoginPassTenantSelectionState()
	{
		return SecureTenantFlowState == SecureTenantFlowStates.TenantNameEntered && GetTenantName().IsPresent();
	}

	// hiding control on client (instead of Visible=false) makes in invisible but allows control to maintain a value
	private void HideOnClient(params Control[] control)
	{
		control.Select(GetStyle).WhereNotNull().ForEach(style => style[HtmlTextWriterStyle.Display] = "none");
	}

	private void ShowOnClient(params Control[] control)
	{
		control.Select(GetStyle).WhereNotNull().ForEach(style => style[HtmlTextWriterStyle.Display] = null);
	}

	private CssStyleCollection GetStyle(Control control)
	{
		if (control is WebControl webControl) return webControl.Style;
		if (control is System.Web.UI.HtmlControls.HtmlControl htmlControl) return htmlControl.Style;
		return null;
	}
	#endregion
}