using PX.Data;
using PX.Data.MassProcess;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using PX.CS.Contracts.Interfaces;
using PX.Objects.GDPR;
using PX.Common;
using PX.Objects.IN;

namespace PX.Objects.CR.Extensions.CRCreateActions
{
	/// <exclude/>
	public abstract class CRCreateLeadAction<TGraph, TMain> : CRCreateActionBaseInit<TGraph, TMain>
		where TGraph : PXGraph, new()
		where TMain : class, IBqlTable, new()
	{
		#region Actions

		public PXAction<TMain> CreateLead;
		[PXUIField(DisplayName = Messages.AddNewLead, FieldClass = FeaturesSet.customerModule.FieldClass, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void createLead()
		{
			if (Base.IsDirty)
				Base.Actions.PressSave();

			var document = Documents.Current;
			var contact = Contacts.SelectSingle();
			var address = Addresses.SelectSingle();

			if (document == null || contact == null || address == null) return;

			var graph = PXGraph.CreateInstance<LeadMaint>();

			var newLead = graph.Lead.Insert();

			newLead.RefContactID = document.ContactID < 0 ? null : document.ContactID;
			newLead.BAccountID = document.BAccountID;

			MapContact(contact, newLead);
			MapConsentable(contact, newLead);

			CRLeadClass cls = PXSelect<
					CRLeadClass,
				Where<
					CRLeadClass.classID, Equal<Current<CRLead.classID>>>>
				.SelectSingleBound(Base, new object[] { newLead });
			if (cls?.DefaultOwner == CRDefaultOwnerAttribute.Source)
			{
				newLead.WorkgroupID = document.WorkgroupID;
				newLead.OwnerID = document.OwnerID;
			}

			graph.Lead.Update(newLead);

			var newAddress = graph.AddressCurrent.SelectSingle()
				?? throw new InvalidOperationException("Cannot get Address for Lead."); // just to ensure

			MapAddress(address, newAddress);

			graph.AddressCurrent.Cache.Update(newAddress);

			if (!Base.IsContractBasedAPI)
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);

			graph.Save.Press();
		}

		#endregion
	}
}
