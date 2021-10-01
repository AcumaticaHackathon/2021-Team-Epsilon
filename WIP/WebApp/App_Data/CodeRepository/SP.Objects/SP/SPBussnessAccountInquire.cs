using System;
using System.Collections;
using PX.Common;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;

namespace SP.Objects.SP
{
	public class SPBussnessAccountInquire : PXGraph<SPBussnessAccountInquire>
	{
		#region ctor
		public SPBussnessAccountInquire()
		{
            PXUIFieldAttribute.SetDisplayName<BAccount.acctCD>(FilteredItems.Cache, "Business Account");
            PXUIFieldAttribute.SetDisplayName<BAccount.acctName>(FilteredItems.Cache, "Business Account Name");
            PXUIFieldAttribute.SetDisplayName<BAccount.classID>(FilteredItems.Cache, "Class");
		}
		#endregion
		
		#region Selects
        [PXViewName(PX.Objects.CR.Messages.BAccount)]
        [PXFilterable]
        public PXSelectJoin<BAccount,	
			LeftJoin<Contact, On<Contact.bAccountID, Equal<BAccount.bAccountID>,
				And<Contact.contactID, Equal<BAccount.defContactID>>>,
			LeftJoin<Address, On<Address.addressID, Equal<BAccount.defAddressID>>,
			LeftJoin<BAccountParent, On<BAccountParent.bAccountID, Equal<BAccount.parentBAccountID>>,
			LeftJoin<Location, On<Location.locationID, Equal<BAccount.defLocationID>>,
			LeftJoin<State,
				On<State.countryID, Equal<Address.countryID>,
					And<State.stateID, Equal<Address.state>>>>>>>>,
			Where<BAccount.parentBAccountID, Equal<Restriction.currentAccountID>>> FilteredItems;
		#endregion

		#region Actions
        public PXAction<BAccount> viewDetails;
        [PXUIField(DisplayName = "View BAccount", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
        [PXButton]
		public virtual IEnumerable ViewDetails(PXAdapter adapter)
		{
            if (FilteredItems.Current != null)
            {
                PartnerBusinessAccountMaint graph = CreateInstance<PartnerBusinessAccountMaint>();
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
