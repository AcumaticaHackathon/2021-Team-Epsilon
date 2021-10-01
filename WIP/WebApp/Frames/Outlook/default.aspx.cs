using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using PX.Data;
using System.Text;

public partial class Frames_Outlook_default : System.Web.UI.Page
{
	private const string fontAwesomeHref = "~/Content/font-awesome.css";
	protected void Page_Load(object sender, EventArgs e)
	{

	}

	protected void Page_Init(object sender, EventArgs e)
	{
		if (!this.Page.IsCallback)
		{
			var fa = new HtmlLink() { Href = fontAwesomeHref };
			fa.Attributes["type"] = "text/css";
			fa.Attributes["rel"] = "stylesheet";
			this.Page.Header.Controls.AddAt(0, fa);
		}
	}

	//-----------------------------------------------------------------------------
	/// <summary>
	/// Fill the allowed companies drop-down.
	/// </summary>
	public string LoginDictionaries()
	{
		string[] companies = PXDatabase.AvailableCompanies;
		if (companies.Length == 0)
		{
			return "";
		}

		string login = $"temp@{companies[0]}";
		PXLocale[] pxLocales = PXLocalesProvider.GetLocales(login);
		var locales = pxLocales.Select(loc => new { loc.DisplayName, loc.Name }).ToList();


		var sb = new StringBuilder();
		sb.AppendFormat(@"
<script>
window.LoginDictionaries = {{
companies: [""{0}""],
locales: [{1}]
}}

</script>",
	String.Join("\",\"", companies),
	String.Join(",", locales.Select(l => $"{{Name:\"{l.Name}\",DisplayName:\"{l.DisplayName}\"}}").ToArray())
);




		var result = sb.ToString();
		return result;
	}



}