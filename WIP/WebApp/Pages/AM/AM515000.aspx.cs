using System;

public partial class Page_AM515000 : PX.Web.UI.PXPage
{
    protected void Page_Init(object sender, EventArgs e)
    {
        // Required when the page is a in page pop-up panel. 
        // This makes the default size larger than the small default
        Master.PopupHeight = 600;
        Master.PopupWidth = 1125;
    }
}
