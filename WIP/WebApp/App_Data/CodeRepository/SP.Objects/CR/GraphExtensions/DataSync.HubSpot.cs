using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.CR;

namespace SP.Objects.CR.GraphExtensions
{
	public class PortalContactHSGraph : PXGraphExtension<PX.DataSync.HubSpot.ContactExt, ContactMaint>
	{
		public virtual void _(Events.RowSelected<Contact> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.GoToHubSpot.SetVisible(false);
			Base1.SyncHubSpot.SetVisible(false);
			Base1.PushToHubSpot.SetVisible(false);
			Base1.PullFromHubSpot.SetVisible(false);
		}
	}

	public class PortalBAccountHSGraph : PXGraphExtension<PX.DataSync.HubSpot.BAccountExt, BusinessAccountMaint>
	{
		public virtual void _(Events.RowSelected<BAccount> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.GoToHubSpot.SetVisible(false);
			Base1.SyncHubSpot.SetVisible(false);
			Base1.PushToHubSpot.SetVisible(false);
			Base1.PullFromHubSpot.SetVisible(false);
		}
	}
}
