using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.CR;

namespace SP.Objects.CR.GraphExtensions
{
	public class PortalContactSFGraph : PXGraphExtension<PX.Salesforce.ContactExt, ContactMaint>
	{
		public virtual void _(Events.RowSelected<Contact> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.GoToSalesforce.SetVisible(false);
			Base1.SyncSalesforce.SetVisible(false);
		}
	}

	public class PortalCaseFGraph : PXGraphExtension<PX.Salesforce.CRCaseMaintExt, CRCaseMaint>
	{
		public virtual void _(Events.RowSelected<CRCase> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.GoToSalesforce.SetVisible(false);
			Base1.SyncSalesforce.SetVisible(false);
		}
	}

	public class PortalBAccountFGraph : PXGraphExtension<PX.Salesforce.BAccountExt, BusinessAccountMaint>
	{
		public virtual void _(Events.RowSelected<BAccount2> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.GoToSalesforce.SetVisible(false);
			Base1.SyncSalesforce.SetVisible(false);
		}
	}
}
