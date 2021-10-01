using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using PX.Data.Access.ActiveDirectory;

public partial class Page_SM202000 : PX.Web.UI.PXPage
{
	public void Page_Load(object sender, EventArgs e)
	{
		if (!((PX.SM.RoleAccess) ds.DataGraph).ActiveDirectoryProvider.IsEnabled())
			tab.Items["ad"].Visible = false;
	}
}
