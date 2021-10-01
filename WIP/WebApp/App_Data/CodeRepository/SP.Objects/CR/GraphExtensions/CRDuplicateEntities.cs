using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.CR;

namespace SP.Objects.CR.GraphExtensions
{
	public class PortalCRDuplicateEntitiesForContactGraphExt : PXGraphExtension<ContactMaint.CRDuplicateEntitiesForContactGraphExt, ContactMaint>
	{
		public virtual void _(Events.RowSelected<Contact> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.CheckForDuplicates.SetVisible(false);
			Base1.DuplicateMerge.SetVisible(false);
			Base1.DuplicateAttach.SetVisible(false);
			Base1.ViewDuplicate.SetVisible(false);
			Base1.MarkAsValidated.SetVisible(false);
			Base1.CloseAsDuplicate.SetVisible(false);
		}
	}

	public class PortalCRDuplicateEntitiesForBAccountGraphExt : PXGraphExtension<BusinessAccountMaint.CRDuplicateEntitiesForBAccountGraphExt, BusinessAccountMaint>
	{
		public virtual void _(Events.RowSelected<BAccount> e, PXRowSelected del)
		{
			del?.Invoke(e.Cache, e.Args);

			Base1.CheckForDuplicates.SetVisible(false);
			Base1.DuplicateMerge.SetVisible(false);
			Base1.DuplicateAttach.SetVisible(false);
			Base1.ViewDuplicate.SetVisible(false);
			Base1.MarkAsValidated.SetVisible(false);
			Base1.CloseAsDuplicate.SetVisible(false);
		}
	}
}
