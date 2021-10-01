using System;
using PX.Data;
using PX.Objects.CR;
using PX.Web.UI;

public partial class Page_CR301000 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
		Master.PopupHeight = 700;
		Master.PopupWidth = 900;
	}

	protected void edRefContactID_EditRecord(object sender, PX.Web.UI.PXNavigateEventArgs e)
	{
		LeadMaint leadMaint = this.ds.DataGraph as LeadMaint;

		var selector = sender as PX.Web.UI.PXSelector;
		object value = null;

		if (selector != null)
			value = selector.Value;

		try
		{
			var graph = PXGraph.CreateInstance<ContactMaint>();

			if (value != null)
			{
				Contact contact = PXSelect<Contact, Where<Contact.contactID, Equal<Required<CRLead.refContactID>>>>.SelectSingleBound(graph, null, value);

				graph.Contact.Current = contact;
			}

			PXRedirectHelper.TryRedirect(graph);
		}
		catch (PX.Data.PXRedirectRequiredException e1)
		{
			PX.Web.UI.PXBaseDataSource ds = this.ds as PX.Web.UI.PXBaseDataSource;
			PX.Web.UI.PXBaseDataSource.RedirectHelper helper = new PX.Web.UI.PXBaseDataSource.RedirectHelper(ds);
			helper.TryRedirect(e1);
		}
	}

	protected void AccountInfoAttributes_DataBound(object sender, EventArgs e)
	{
		var AccountExt = this.ds.DataGraph.GetExtension<LeadMaint.CreateAccountFromLeadGraphExt>();

		(sender as PXFormView).Visible = AccountExt?.NeedToUse ?? true;
	}

	protected void ContactInfoAttributes_DataBound(object sender, EventArgs e)
	{
		var ContactExt = this.ds.DataGraph.GetExtension<LeadMaint.CreateContactFromLeadGraphExt>();

		(sender as PXFormView).Visible = ContactExt?.NeedToUse ?? true;
	}
}
