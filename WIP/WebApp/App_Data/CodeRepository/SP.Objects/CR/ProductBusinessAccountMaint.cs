using System;
using System.Collections.Generic;
using PX.Common;
using PX.CS;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CR.Extensions;

namespace SP.Objects.CR
{
    public class ProductBusinessAccountMaint : PXGraph<ProductBusinessAccountMaint, BAccount>
    {
        #region ctor
        public ProductBusinessAccountMaint()
        {
            foreach (string action in this.Actions.Keys)
            {
                this.Actions[action].SetVisible(false);
            }

            this.Actions["Save"].SetVisible(true);
            this.Actions["Cancel"].SetVisible(true);

            PXUIFieldAttribute.SetVisible<BAccount.acctCD>(this.Caches[typeof(BAccount)], null, false);
        }
        #endregion

        #region Select

        [PXViewName(PX.Objects.CR.Messages.BAccount)]
        public PXSelect<BAccount,
                Where2<
                    Match<Current<AccessInfo.userName>>,
                    And<BAccount.bAccountID, Equal<Restriction.currentAccountID>>>> BAccount;

        [PXHidden]
        public PXSelect<
                BAccount,
            Where<
                BAccount.bAccountID, Equal<Current<BAccount.bAccountID>>>>
            CurrentBAccount;

        [PXHidden]
        public PXSelect<Address>
            AddressDummy;

        [PXHidden]
        public PXSelect<Contact>
            ContactDummy;

        #endregion

        public override void Persist()
        {
            Customer currentCustomer = PXSelect<
                    Customer,
                Where<
                    Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                .Select(this, ReadBAccount.ReadCurrentAccount()?.BAccountID);

            if (currentCustomer != null)
            {
                CustomerMaint graph = PXGraph.CreateInstance<CustomerMaint>();

                graph.BAccount.Current = currentCustomer;

                PXResult result = graph.CurrentCustomer.Search<Customer.bAccountID>(currentCustomer.BAccountID);
                Customer customer = result[typeof(Customer)] as Customer;
                customer.AcctName = ((BAccount)this.Caches[typeof(BAccount)].Current).AcctName;
                this.Caches[typeof(BAccount)].IsDirty = false;
                graph.Caches[typeof(Customer)].SetStatus(customer, PXEntryStatus.Updated);

                Contact currentbaseContact = this.Caches[typeof(Contact)].Current as Contact;
                graph.Caches[typeof(Contact)].SetStatus(currentbaseContact, PXEntryStatus.Updated);
                this.Caches[typeof(Contact)].IsDirty = false;


                Address currentbaseAddress = this.Caches[typeof(Address)].Current as Address;
                graph.Caches[typeof(Address)].SetStatus(currentbaseAddress, PXEntryStatus.Updated);

                this.Caches[typeof(Address)].IsDirty = false;

                graph.Persist();
            }
            else
            {
                base.Persist();
            }
        }

        #region Extensions

        public class DefContactAddress : DefContactAddressExt<ProductBusinessAccountMaint, BAccount, BAccount.acctName> { }

        #region Address Lookup Extension

        /// <exclude/>
        public class ProductBusinessAccountMaintAddressLookupExtension : PX.Objects.CR.Extensions.AddressLookupExtension<ProductBusinessAccountMaint, BAccount, Address>
        {
            protected override string AddressView => nameof(DefContactAddress.DefAddress);
        }

        #endregion

        #endregion
    }
}

