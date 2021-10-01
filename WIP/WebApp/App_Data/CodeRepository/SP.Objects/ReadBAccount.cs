using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Web;
using System.Web.UI;
using PX.Common;
using PX.Data;
using PX.Data.Automation;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.SM;
using PX.Web.Customization.utils;

namespace SP.Objects
{
    public static class ReadBAccount
    {
		public static Contact ReadCurrentContact()
        {
            Contact cont = ContactDefinition.Contact;

            if (cont == null)
            {
				if (PXGraph.ProxyIsActive || PX.Translation.ResourceCollectingManager.IsStringCollecting || HttpContext.Current == null)
				{
					return new Contact { FullName = "A B" };
				}
				var url = string.Format("~/Frames/Error.aspx?exceptionID={0}&typeID={1}&errorcode={2}&HideScript=On", HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.GetBaccountsErrorMessage)),
                    HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ErrorType)), HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ConfigurationError)));
                var page = HttpContext.Current.CurrentHandler as Page;
                if (page != null && page.IsCallback)
                    throw new PXRedirectToUrlException(url, "");
                else
                    HttpContext.Current.Response.Redirect(url);
            }
            return cont;
        }

        public static Contact ReadCurrentContactWithoutCheck()
        {
            Guid userId = PXAccess.GetUserID();
			if (userId != Guid.Empty)
			{
				var res = PXSelect<Contact,
					Where<Contact.userID, Equal<Required<Contact.userID>>>>.
					Select(new PXGraph(), userId);

				if (res != null && res.Count > 0)
				{
					return res[0];
				}
			}
			if (PXGraph.ProxyIsActive || PX.Translation.ResourceCollectingManager.IsStringCollecting)
			{
				return new Contact { FullName = "A B" };
			}
			return null;
		}

		public static BAccount ReadCurrentAccount()
        {
            BAccount acc = BaccountDefinition.Account;
            if (acc == null)
            {
				if (PXGraph.ProxyIsActive || PX.Translation.ResourceCollectingManager.IsStringCollecting || HttpContext.Current == null)
				{
					return new BAccount { AcctCD = "C1", Status = CustomerStatus.Active };
				}
				var url = string.Format("~/Frames/Error.aspx?exceptionID={0}&typeID={1}&errorcode={2}&HideScript=On", HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.GetBaccountsErrorMessage)),
                    HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ErrorType)), HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ConfigurationError)));
                var page = HttpContext.Current.CurrentHandler as Page;
                if (page != null && page.IsCallback)
                    throw new PXRedirectToUrlException(url, "");
                else
                    HttpContext.Current.Response.Redirect(url);
            }
            return acc;
        }

        public static BAccount ReadCurrentAccountWithoutCheck()
        {
			if (BaccountDefinition.Account == null && (PXGraph.ProxyIsActive || PX.Translation.ResourceCollectingManager.IsStringCollecting))
			{
				return new BAccount { AcctCD = "C1", Status = CustomerStatus.Active };
			}
			return BaccountDefinition.Account;
        }

        public static Customer ReadCurrentCustomer()
        {
            Customer cust = CustomerDefinition.Customer;
            if (cust == null)
            {
				if (PXGraph.ProxyIsActive 
                    || PX.Translation.ResourceCollectingManager.IsStringCollecting
                    || HttpContext.Current == null
                    || PXCustomizationEditorScope.Active
                    || PXUseSystemOnlyWorkflow.UseSystemOnlyWorkflow()) // PXUseSystemOnlyWorkflow.UseSystemOnlyWorkflow() denotes
                                                                        // that the current code path is a part of
                                                                        // a graph creation by the workflow engine in AU-screen
                                                                        // mode (e.g., from Customization Editor context)
                {
					return new Customer { AcctCD = "C1", Status = CustomerStatus.Active };
				}
                var url = string.Format("~/Frames/Error.aspx?exceptionID={0}&typeID={1}&errorcode={2}&HideScript=On", HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.GetBaccountsErrorMessage)),
                    HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ErrorType)), HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ConfigurationError)));
                var page = HttpContext.Current.CurrentHandler as Page;
                if (page != null && page.IsCallback)
                    throw new PXRedirectToUrlException(url, "");
                else
                    HttpContext.Current.Response.Redirect(url);
            }
            return cust;
        }

        public static Customer ReadCurrentCustomerWithoutCheck()
        {
			if (CustomerDefinition.Customer == null && (PXGraph.ProxyIsActive || PX.Translation.ResourceCollectingManager.IsStringCollecting))
			{
				return new Customer { AcctCD = "C1", Status = CustomerStatus.Active };
			}
			return CustomerDefinition.Customer;
        }

        private static BAccountUserDefinition BaccountDefinition
        {
            get
            {
                BAccountUserDefinition definition = PXDatabase.GetSlot<BAccountUserDefinition>("BAccountByUser"+ PXAccess.GetUserName(), typeof (BAccount), typeof (Contact), typeof(Users));

                if (definition.Account == null)
                {
                    Guid userId = PXAccess.GetUserID();
                    if (userId == Guid.Empty) return definition;
                    lock (definition)
                    {
                        definition.Find(userId);
                    }
                }
                return definition;

            }
        }

        private static CustomerUserDefinition CustomerDefinition
        {
            get
            {
                CustomerUserDefinition definition = PXDatabase.GetSlot<CustomerUserDefinition>("CustomerByUser" + PXAccess.GetUserName(), typeof(BAccount), typeof(Contact), typeof(Users));

                if (definition.Customer == null)
                {
                    Guid userId = PXAccess.GetUserID();
                    if (userId == Guid.Empty) return definition;
                    lock (definition)
                    {
                        definition.Find(userId);
                    }
                }
                return definition;
            }
        }

        private static ContactUserDefinition ContactDefinition
        {
            get
            {
                ContactUserDefinition definition = PXDatabase.GetSlot<ContactUserDefinition>("ContactByUser" + PXAccess.GetUserName(), typeof(Contact), typeof(Users));

                if (definition.Contact == null)
        {
            Guid userId = PXAccess.GetUserID();
                    if (userId == Guid.Empty) return definition;
                    lock (definition)
                    {
                        definition.Find(userId);
                    }
                }
                return definition;
            }
        }

        public class DefContactAccountUserDefinition
        {
            public Contact Contact;
            public void Find(int defContactID)
            {
                var res = PXSelect<Contact, Where<Contact.contactID, Equal<Required<Contact.contactID>>>>.Select(new PXGraph(), defContactID);

                if (res != null && res.Count > 0)
                {
                    Contact = (Contact)(res[0][typeof(Contact)]);
                }
            }
        }

        private static DefContactAccountUserDefinition DefContactAccountDefinition
        {
            get
            {
                DefContactAccountUserDefinition definition = PXDatabase.GetSlot<DefContactAccountUserDefinition>("DefContactByUser" + PXAccess.GetUserName(), typeof(Contact), typeof(Users));

                if (definition.Contact == null)
                {
                    var bAccount = ReadCurrentAccount();
                    if (bAccount == null || bAccount.DefContactID == null) return definition;
                    lock (definition)
                    {
                        definition.Find((int)bAccount.DefContactID);
                    }
                }
                return definition;
            }
        }

        public static Contact ReadDefContactForCurrentAccount()
        {
            Contact cont = DefContactAccountDefinition.Contact;

            if (cont == null)
            {
				if (PXGraph.ProxyIsActive || PX.Translation.ResourceCollectingManager.IsStringCollecting || HttpContext.Current == null)
				{
					return new Contact { FullName = "A B" };
				}
				var url = string.Format("~/Frames/Error.aspx?exceptionID={0}&typeID={1}&errorcode={2}", HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.CustomerHaveNoDefContact)), HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ErrorType)), HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ConfigurationError)));
                var page = HttpContext.Current.CurrentHandler as Page;
                if (page != null && page.IsCallback)
                    throw new PXRedirectToUrlException(url, "");
                else
                    HttpContext.Current.Response.Redirect(url);
            }
            return cont;
        }


        public static Contact ReadCurrentAccountDefContactWithoutCheck()
        {
            int? contactId = BaccountDefinition.Account?.DefContactID;
			if (BaccountDefinition.Account != null
				&& BaccountDefinition.Account.DefContactID.HasValue)
			{
				var res = PXSelect<Contact,
						Where<Contact.bAccountID, Equal<Required<BAccount.bAccountID>>,
							And<Contact.contactID, Equal<Required<BAccount.defContactID>>>>>
							.Select(new PXGraph(), BaccountDefinition.Account.BAccountID, BaccountDefinition.Account.DefContactID);

				return res != null && res.Count > 0 ? res[0] : null;
			}
			if (PXGraph.ProxyIsActive || PX.Translation.ResourceCollectingManager.IsStringCollecting)
			{
				return new Contact { FullName = "C1" };
			}
			return null;
        }
    }

    public class BAccountUserDefinition
    {
        public BAccount Account;

        public void Find(Guid userID)
        {
            if (userID == Guid.Empty) return;

            var res = (PXResultset<BAccount>)PXSelectJoin<BAccount,
                InnerJoin<Contact, On<Contact.bAccountID, Equal<BAccount.bAccountID>>>,
                Where<Contact.userID, Equal<Required<Contact.userID>>>>.
                Select(new PXGraph(), userID);

            if (res != null && res.Count > 0)
            {
                Account = (BAccount)(res[0][typeof(BAccount)]);
            }
        }
    }

    public class ContactUserDefinition
    {
        public Contact Contact;
        public void Find(Guid userID)
        {
            if (userID == Guid.Empty) return;

            var res = PXSelect<Contact, Where<Contact.userID, Equal<Required<Contact.userID>>>>.Select(new PXGraph(), userID);

            if (res != null && res.Count > 0)
            {
                Contact = (Contact)(res[0][typeof(Contact)]);
            }
        }
    }

    public class CustomerUserDefinition
    {
        public Customer Customer;

        public void Find(Guid userID)
        {
            if (userID == Guid.Empty) return;

            var res = (PXResultset<Customer>)PXSelectJoin<Customer,
                InnerJoin<Contact, On<Contact.bAccountID, Equal<Customer.bAccountID>>>,
                Where<Contact.userID, Equal<Required<Contact.userID>>>>.
                Select(new PXGraph(), userID);

            if (res != null && res.Count > 0)
            {
                Customer = (Customer)(res[0][typeof(Customer)]);
            }
        }
    }

	public class CustomerEnabled
	{
		public class isEnabled : BqlType<IBqlBool, bool>.Constant<isEnabled>
		{
			public isEnabled() : base(ReadBAccount.ReadCurrentCustomerWithoutCheck() != null) { }
		}
	}

	public class Restriction
	{
		public sealed class currentAccountID : PX.Data.BQL.BqlInt.Constant<Restriction.currentAccountID>
		{
			public currentAccountID() : base(ReadBAccount.ReadCurrentAccount().BAccountID.GetValueOrDefault(0)) { }
		}
	}
}
