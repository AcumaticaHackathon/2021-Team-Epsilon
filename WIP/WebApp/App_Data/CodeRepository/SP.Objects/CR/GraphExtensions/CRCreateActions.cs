using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.CR;

namespace SP.Objects.CR.GraphExtensions
{
	public class PortalCreateLeadFromContactGraphExt : PXGraphExtension<ContactMaint.CreateLeadFromContactGraphExt, ContactMaint>
	{
		public virtual void _(Events.RowSelected<Contact> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.CreateLead.SetVisible(false);
		}
	}

	public class PortalCreateAccountFromContactGraphExt : PXGraphExtension<ContactMaint.CreateAccountFromContactGraphExt, ContactMaint>
	{
		public virtual void _(Events.RowSelected<Contact> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.CreateBAccount.SetVisible(false);
		}
	}
}
