using System;
using System.Collections;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.EP;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.SM;

namespace SP.Objects.SP
{
	public class SPContactPartnerInquiry : PXGraph<SPContactPartnerInquiry>
	{
		#region Select
		[PXViewName(PX.Objects.CR.Messages.Contacts)]
		[PXFilterable]
		public PXSelectJoin<Contact,
			LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.bAccountID>>,
			LeftJoin<Address, On<Address.addressID, Equal<Contact.defAddressID>>,
			LeftJoin<CRContactClass, On<CRContactClass.classID, Equal<Contact.classID>>,
			LeftJoin<Users, On<Users.pKID, Equal<Contact.userID>>,
			LeftJoin<EPLoginType, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>>>>>,
			Where2<Where<CRContactClass.isInternal, Equal<False>, Or<CRContactClass.isInternal, IsNull>>,
				And<Contact.contactType, Equal<ContactTypesAttribute.person>,
				And<Where<Contact.parentBAccountID, Equal<Restriction.currentAccountID>,
					Or<BAccount.parentBAccountID, Equal<Restriction.currentAccountID>>>>>>>
			FilteredItems;

	   #endregion

		#region Action
        public PXAction<Contact> viewDetails;
		[PXUIField(DisplayName = "Contact Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXButton]
		public virtual IEnumerable ViewDetails(PXAdapter adapter)
		{
			if (this.FilteredItems.Current != null)
			{
				PartnerContactMaint graph = PXGraph.CreateInstance<PartnerContactMaint>();
				PXResult result = graph.Contact.Search<Contact.contactID>(FilteredItems.Current.ContactID);
				Contact contact = result[typeof(Contact)] as Contact;
				graph.Contact.Current = contact;
                PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.InlineWindow);
			}
			return adapter.Get();
		}

        public PXAction<Contact> viewBAccountDetails;
		[PXUIField(DisplayName = "Business Account Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXButton]
		public virtual IEnumerable ViewBAccountDetails(PXAdapter adapter)
		{
			if (this.FilteredItems.Current != null)
			{
				PartnerBusinessAccountMaint graph = PXGraph.CreateInstance<PartnerBusinessAccountMaint>();
                PXResult result = graph.BAccount.Search<BAccount.bAccountID>(FilteredItems.Current.BAccountID);
                BAccount bAccount = result[typeof(BAccount)] as BAccount;
				graph.BAccount.Current = bAccount;
                PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.InlineWindow);
			}
			return adapter.Get();
		}
		#endregion
	}
}
