using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using PX.Common;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CR;
using PX.SM;
using PX.Objects.AR;
using PX.Objects.CR.Extensions;
using PX.Objects.CS;
using PX.Objects.CR.Extensions.CRCreateActions;
using PX.Objects.GDPR;
using PX.CS.Contracts.Interfaces;

namespace SP.Objects.SP
{
    public class PartnerBusinessAccountMaint : PXGraph<PartnerBusinessAccountMaint, BAccount>
    {
        #region Select

        [PXViewName(PX.Objects.CR.Messages.BAccount)]
        public PXSelect<BAccount,
                Where2<
                    Match<Current<AccessInfo.userName>>,
                    And<BAccount.parentBAccountID, Equal<Restriction.currentAccountID>>>> BAccount;

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

        [PXViewName(PX.Objects.CR.Messages.Answers)]
        public CRAttributeList<BAccount>
            Answers;

        [PXViewName(PX.Objects.CR.Messages.Activities)]
        [PXFilterable]
        [CRReference(typeof(BAccount.bAccountID), Persistent = true)]
        public CRActivityList<BAccount>
            Activities;

        #endregion

        #region Event Handlers
        [PXDimensionSelector("BIZACCT",
            typeof(Search2<BAccount.acctCD,
            InnerJoin<Contact, On<Contact.bAccountID, Equal<BAccount.parentBAccountID>>>,
                Where<Contact.userID, Equal<Current<AccessInfo.userID>>>>
            ), typeof(BAccount.acctCD))]
        [PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
        [PXDefault()]
        [PXUIField(Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [PXFieldDescription]
        protected virtual void _(Events.CacheAttached<BAccount.acctCD> e) { }

        protected virtual void _(Events.RowSelected<BAccount> e)
        {
            BAccount acc = e.Row as BAccount;
            if (acc == null)
                return;

            PXUIFieldAttribute.SetEnabled<BAccount.classID>(e.Cache, acc, false);
            if (acc.Type != "PR")
            {
                Contact contact = PXSelect<Contact, Where<Contact.contactID, Equal<Required<Contact.contactID>>>>.Select(this, acc.DefContactID);

                if (contact != null)
                    PXUIFieldAttribute.SetEnabled<Contact.eMail>(this.Caches[typeof(Contact)], contact, false);

                this.Caches<CSAnswers>().AllowUpdate = false;
                this.Caches<Contact>().AllowInsert = false;
            }
            else
            {
                this.Caches<CSAnswers>().AllowUpdate = true;
                this.Caches<Contact>().AllowInsert = true;
            }
        }

        #endregion

        public override void Persist()
        {
            if (CurrentBAccount.Current != null)
            {
                Customer currentCustomer = PXSelect<Customer,
                Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.Select(this, CurrentBAccount.Current.BAccountID);

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
        }

        #region Extensions

        #region Details

        /// <exclude/>
        public class DefContactAddress : DefContactAddressExt<PartnerBusinessAccountMaint, BAccount, BAccount.acctName> { }

        /// <exclude/>
        public class ContactDetails : BusinessAccountContactDetailsExt<PartnerBusinessAccountMaint, CreateContactFromAccountGraphExt, BAccount, BAccount.bAccountID>
        {
            #region Actions

            [PXUIField(DisplayName = PX.Objects.CR.Messages.ViewContact)]
            [PXButton]
            public override void viewContact()
            {
                if (this.Contacts.Current != null)
                {
                    PartnerContactMaint graph = PXGraph.CreateInstance<PartnerContactMaint>();

                    PXResult result = graph.Contact.Search<Contact.contactID>(this.Contacts.Current.ContactID);
                    Contact contact = result[typeof(Contact)] as Contact;

                    graph.Contact.Current = contact;

                    PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
                }
            }

            #endregion
        }

        #endregion

        #region Address Lookup Extension

        /// <exclude/>
        public class PartnerBusinessAccountMaintAddressLookupExtension : PX.Objects.CR.Extensions.AddressLookupExtension<PartnerBusinessAccountMaint, BAccount, Address>
        {
            protected override string AddressView => nameof(DefContactAddress.DefAddress);
        }

        #endregion

        /// <exclude/>
        public class CreateContactFromAccountGraphExt : CRCreateContactActionBase<PartnerBusinessAccountMaint, BAccount>
        {
            #region Initialization

            protected override PXSelectBase<CRPMTimeActivity> Activities => Base.Activities;

            public override void Initialize()
            {
                base.Initialize();

                Addresses = new PXSelectExtension<DocumentAddress>(Base.GetExtension<DefContactAddress>().DefAddress);
                Contacts = new PXSelectExtension<DocumentContact>(Base.GetExtension<DefContactAddress>().DefContact);
                CreateContact.SetVisibleOnDataSource(true);
            }

            protected override DocumentContactMapping GetDocumentContactMapping()
            {
                return new DocumentContactMapping(typeof(Contact)) { Email = typeof(Contact.eMail) };
            }

            protected override DocumentAddressMapping GetDocumentAddressMapping()
            {
                return new DocumentAddressMapping(typeof(Address));
            }

            #endregion

            #region Events

            public virtual void _(Events.RowSelected<ContactFilter> e)
            {
                PXUIFieldAttribute.SetReadOnly<ContactFilter.fullName>(e.Cache, e.Row, true);
            }

            public virtual void _(Events.FieldDefaulting<ContactFilter, ContactFilter.fullName> e)
            {
                e.NewValue = Contacts.SelectSingle()?.FullName;
            }

            #endregion

            #region Overrides

            protected override ContactMaint CreateTargetGraph()
            {
                var graph = PXGraph.CreateInstance<PartnerContactMaint>();
                graph.Caches<BAccount>().Current = GetMainCurrent();
                return graph;
            }

            protected override IAddressBase MapAddress(DocumentAddress source, IAddressBase target)
            {
                if (source is null)
                    throw new ArgumentNullException(nameof(source));
                if (target is null)
                    throw new ArgumentNullException(nameof(target));

                return Base.GetExtension<DefContactAddress>().DefAddress.SelectSingle();
            }

            protected override void FillRelations<TNoteField>(CRRelationsList<TNoteField> relations, Contact target)
            {
            }

            protected override IConsentable MapConsentable(DocumentContact source, IConsentable target)
            {
                return target;
            }

            #endregion
        }

        #endregion
    }
}
