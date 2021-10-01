using System;
using PX.Web.UI;

public partial class Page_SM402060 : PXPage
{
    protected void Page_Init(object sender, EventArgs e)
    {
        Master.CustomizationAvailable = false;
        Master.HelpAvailable = false;
        Master.ScreenTitle = null;
        Master.ScreenTitleVisible = false;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}
