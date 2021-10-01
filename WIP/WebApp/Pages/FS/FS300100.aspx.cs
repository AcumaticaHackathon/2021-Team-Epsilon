using System;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using PX.Objects.FS;

public partial class Page_FS300100 : PX.Web.UI.PXPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
    	Master.PopupHeight = 700;
        Master.PopupWidth = 1080;
    }

    protected void edContactID_EditRecord(object sender, PX.Web.UI.PXNavigateEventArgs e)
    {
        ServiceOrderEntry graph = this.ds.DataGraph as ServiceOrderEntry;
        if (graph != null)
        {
            FSServiceOrder serivceOrder = this.ds.DataGraph.Views[this.ds.DataGraph.PrimaryView].Cache.Current as FSServiceOrder;
            if (serivceOrder.ContactID == null && serivceOrder.CustomerID != null)
            {
                try
                {
                    graph.addNewContact.Press();
                }
                catch (PX.Data.PXRedirectRequiredException e1)
                {
                    PX.Web.UI.PXBaseDataSource ds = this.ds as PX.Web.UI.PXBaseDataSource;
                    PX.Web.UI.PXBaseDataSource.RedirectHelper helper = new PX.Web.UI.PXBaseDataSource.RedirectHelper(ds);
                    helper.TryRedirect(e1);
                }
            }
        }
    }
}
