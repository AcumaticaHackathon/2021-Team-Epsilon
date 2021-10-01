using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.DAC.Unbound;
using System;

public partial class Page_CR304000 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
		this.Master.PopupHeight = 700;
		this.Master.PopupWidth = 900;
	}    

    protected void edContactID_EditRecord(object sender, PX.Web.UI.PXNavigateEventArgs e)
    {
        OpportunityMaint oppmaint = this.ds.DataGraph as OpportunityMaint;
        if (oppmaint != null)
        {
            CROpportunity currentopportunity = this.ds.DataGraph.Views[this.ds.DataGraph.PrimaryView].Cache.Current as CROpportunity;
            if (currentopportunity.ContactID == null && currentopportunity.BAccountID != null)
            {
                {
                    try
                    {
                        oppmaint.addNewContact.Press();
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

	protected void tab_DataBound(object sender, EventArgs e)
	{
		// hide tabs in case of Project Quote is Primary
		var visible = true;
		var graph = this.ds.DataGraph as OpportunityMaint;
		if (graph != null)
		{
			var opportunity = graph.Views[graph.PrimaryView].Cache.Current as CROpportunity;
			if (opportunity != null && opportunity.PrimaryQuoteType != null && opportunity.PrimaryQuoteType == "P")
				visible = false;
		}

		foreach (var tabKey in new string[] { "ProductsTab", "TaxDetailsTab", "OrdersTab", "InvoicesTab", "DiscountDetailsTab" })
		{
			if (tab.Items[tabKey] != null) tab.Items[tabKey].Visible = (tab.Items[tabKey].Visible != false) && visible;
		}
	}

	protected void MatrixAttributes_AfterSyncState(object sender, EventArgs e)
	{
		InventoryMatrixEntry.EnableCommitChangesAndMoveExtraColumnAtTheEnd(MatrixAttributes.Columns, 0);
	}

	protected void MatrixMatrix_AfterSyncState(object sender, EventArgs e)
	{
		InventoryMatrixEntry.EnableCommitChangesAndMoveExtraColumnAtTheEnd(MatrixMatrix.Columns);
	}

	protected void MatrixItems_AfterSyncState(object sender, EventArgs e)
	{
		InventoryMatrixEntry.InsertAttributeColumnsByTemplateColumn(
			MatrixItems.Columns, ds.DataGraph.Caches[typeof(MatrixInventoryItem)].Fields);
	}
}
