using System;
using System.Collections;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;
using PX.Data.EP;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.SO;
using PX.SM;
using PX.Objects.CR.DAC;
using PX.Objects.CA;
using PX.Objects.CR.MassProcess;
using PX.Data.MassProcess;
using PX.Objects.CR.Extensions;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Objects.CR.Extensions.CRCreateActions;
using System.Linq;
using System.Collections.Generic;
using PX.Objects.CR.Extensions.Relational;
using PX.CS.Contracts.Interfaces;
using PX.Objects.CR.Workflows;
using PX.Objects.GDPR;

namespace PX.Objects.CR.Extensions
{
	/// <summary>
	/// Represents the Contacts grid
	/// </summary>
	public abstract class BusinessAccountContactDetailsExt<TGraph, TCreateContactExt, TMaster, FBAccountIDField>
		: ContactDetailsExt<TGraph, TCreateContactExt, TMaster, Contact.bAccountID, FBAccountIDField>
		where TGraph : PXGraph, new()
		where TCreateContactExt : CRCreateContactActionBase<TGraph, TMaster>	//no usages, just need to be deaclared in the graph
		where TMaster : BAccount, IBqlTable, new()
		where FBAccountIDField : class, IBqlField
	{
		#region Events

		[PXOverride]
		public virtual void Persist(Action del)
		{
			using (PXTransactionScope ts = new PXTransactionScope())
			{
				InactivateActiveContacts();

				del();

				ts.Complete();
			}
		}

		#endregion

		#region Methods

		public virtual void InactivateActiveContacts()
		{
			TMaster acct = Base.Caches[typeof(TMaster)].Current as TMaster;

			if (acct != null && acct.Status == CustomerStatus.Inactive)
			{
				bool isChanged = false;

				foreach (Contact contact in this.Contacts.Select())
				{
					if (contact.IsActive == false)
						continue;

					ContactWorkflow.SetStatusTo(contact, ContactStatus.Inactive);

					isChanged = true;
				}

				if (isChanged)
				{
					Base.SelectTimeStamp();
				}
			}
		}

		#endregion
	}

	/// <summary>
	/// Represents the Contacts grid
	/// </summary>
	public abstract class ContactDetailsExt<TGraph, TCreateContactExt, TMaster, FContactField, FMasterField> : PXGraphExtension<TGraph>
		where TGraph : PXGraph, new()
		where TCreateContactExt : CRCreateContactActionBase<TGraph, TMaster>	//no usages, just need to be deaclared in the graph
		where TMaster : class, IBqlTable, new()
		where FContactField : class, IBqlField
		where FMasterField : class, IBqlField
	{
		#region Views

		[PXViewName(Messages.Contacts)]
		[PXFilterable]
		public PXSelectJoin<
				Contact,
			LeftJoin<Address,
				On<Address.addressID, Equal<Contact.defAddressID>>>,
			Where<
				FContactField, Equal<Current<FMasterField>>,
				And<Contact.contactType, Equal<ContactTypesAttribute.person>>>>
			Contacts;

		#endregion

		#region Actions
		public PXAction<TMaster> RefreshContact;
		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void refreshContact()
		{
			Base.SelectTimeStamp();
			Base.Caches<Contact>().ClearQueryCache();
		}

		public PXAction<TMaster> ViewContact;
		[PXUIField(DisplayName = Messages.ViewContact, Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewContact()
		{
			if (this.Contacts.Current == null)
				return;

			if (Base.Caches[typeof(TMaster)].GetStatus(Base.Caches[typeof(TMaster)].Current) == PXEntryStatus.Inserted)
				return;

			Contact current = this.Contacts.Current;

			ContactMaint graph = PXGraph.CreateInstance<ContactMaint>();

			graph.Contact.Current = current;

			PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);
		}

		#endregion
	}
}
