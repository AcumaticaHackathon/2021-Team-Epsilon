using System;
using System.Collections;
using PX.Common;
using PX.Data;
using PX.EP;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;
using PX.Objects.EP;
using PX.Objects.SP.DAC;
using PX.SM;
using SP.Objects.CR;

namespace SP.Objects.SP
{
	public class PartnerContactMaint : ContactMaint
	{
	}

	public class PartnerContactMaintExt : PXGraphExtension<PartnerContactMaint>
	{
		#region Ctor
		public override void Initialize()
		{
			if (PortalSetup.Current == null)
				throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

			Base.Actions[nameof(ContactMaint.Action)].SetMenu(new ButtonMenu[0]);
			Base.Actions[nameof(ContactMaint.Action)].SetVisible(false);
			Base.Actions[nameof(ContactMaint.Activate)].SetVisible(false);
			Base.Actions[nameof(ContactMaint.Deactivate)].SetVisible(false);
			Base.Actions[nameof(ContactMaint.CRDuplicateEntitiesForContactGraphExt.DuplicateAttach)]?.SetVisible(false);

            /*var row1 = PXCache<Contact>.GetExtension<ContactExt>(Contact.Current);
            row1.HasRigths = false;
            foreach (var res in PXSelectJoin<EPLoginType,
                                InnerJoin<Users, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>,
                                Where<Users.pKID, Equal<Required<Users.pKID>>, And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base, Contact.Current.UserID))
            {
                EPLoginType lt = res[typeof(EPLoginType)] as EPLoginType;

                foreach (var res1 in PXSelectJoin<EPLoginType,
                                InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                                InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                                Where<Users.pKID, Equal<Current<AccessInfo.userID>>, And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base))
                {
                    EPManagedLoginType mt = res1[typeof(EPManagedLoginType)] as EPManagedLoginType;
                    if (mt.LoginTypeID == lt.LoginTypeID)
                        row1.HasRigths = true;
                }
            }
            row1.HasRigths = row1.HasRigths == true || Contact.Current.ContactID < 0 || Contact.Current.UserID == null;*/
        }
		#endregion

		#region Views
        public PXSetup<Address> dummy;

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
            Contact;

        [PXViewName(PX.Objects.CR.Messages.Contact)]
		public PXSelectJoin<Contact,
            LeftJoin<BAccount, On<Contact.bAccountID, Equal<BAccount.bAccountID>>,
			LeftJoin<Address, On<Address.addressID, Equal<Contact.defAddressID>>,
			LeftJoin<Users, On<Users.pKID, Equal<Contact.userID>>,
			LeftJoin<EPLoginType, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>>>>,
			Where<Contact.contactID, Equal<Current<Contact.contactID>>>>
			ContactCurrent;

		[PXViewName(ViewNamesMessage.UserPrefs)]
		public PXSelect<UserPreferences, Where<UserPreferences.userID, Equal<Current<Contact.userID>>>> UserPrefs;
		#endregion


		#region Contact Cache Attached
		[PXDBIdentity(IsKey = true)]
		[PXUIField(DisplayName = "Contact ID", Visibility = PXUIVisibility.Invisible)]
		[SPContactPartnerSelector()]
		protected virtual void Contact_ContactID_CacheAttached(PXCache sender)
		{
		}


        [PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
        [PXMassMergableField]
        [PXMassUpdatableField]
        [PXSelector(typeof(Search<CRContactClass.classID,
                Where<CRContactClass.isInternal, Equal<False>>>),
                DescriptionField = typeof(CRContactClass.description), CacheGlobal = true)]
        [PXUIField(DisplayName = "Class ID")]
        [PXDefault(typeof(PortalSetup.defaultContactClassID), PersistingCheck = PXPersistingCheck.Nothing)]
        protected virtual void Contact_ClassID_CacheAttached(PXCache sender)
        { 
        }
		#endregion

		#region Event Handlers
		protected virtual void UserPreferences_RowInserting(PXCache sender, PXRowInsertingEventArgs e, PXRowInserting ins)
		{
			var row = e.Row as UserPreferences;
			if (row == null) return;

			if (ins != null)
				ins(sender, e);

			if (((Contact)Base.Caches<Contact>().Current).UserID != null)
				row.UserID = ((Contact)Base.Caches<Contact>().Current).UserID;
			else
				e.Cancel = true;
		}

		protected virtual void Contact_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
        {
            var row = e.Row as Contact;
            if (row == null) return;

            if (sel != null)
                sel(sender, e);

			PXUIFieldAttribute.SetEnabled<UserPreferences.timeZone>(Base.Caches[typeof(UserPreferences)], UserPrefs.Current, ((Contact)Base.Caches<Contact>().Current).UserID != null);

			int Status = 0;

            PXUIFieldAttribute.SetEnabled<Contact.isActive>(sender, row, PXAccess.GetUserID() != row.UserID);

            // есть ли у него права на добавление
            var list = PXSelectJoin<EPLoginType,
                               InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                               InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                               Where<Users.pKID, Equal<Current<AccessInfo.userID>>,
                               And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base);

            if (list == null || list.Count == 0)
            {
                Base.Actions["Insert"].SetVisible(false);
            }

            if (row.UserID != null)
            {
                // Есть у него права на редактирование
                var list1 = PXSelectJoin<EPLoginType,
                                   InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                                   InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                                   Where<Users.pKID, Equal<Current<AccessInfo.userID>>,
                                   And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base);

                var list2 = PXSelectJoin<EPLoginType,
                                InnerJoin<Users, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>,
                                Where<Users.pKID, Equal<Required<Users.pKID>>, And<EPLoginType.entity,
                                Equal<EPLoginType.entity.contact>>>>.Select(Base, row.UserID);

                if (list2 == null || list2.Count == 0)
                {
                    Status = 2;
                }
                else
                {
                    foreach (var res in list2)
                    {
                        EPLoginType lt = res[typeof(EPLoginType)] as EPLoginType;
                        foreach (var res1 in list1)
                        {
                            EPManagedLoginType mt = res1[typeof(EPManagedLoginType)] as EPManagedLoginType;
                            if (mt.LoginTypeID == lt.LoginTypeID)
                            {
                                Status = 2;
                            }
                        }
                    }
                }
            }

            else
            {
                Status = 2;
            }

            if (row.UserID == PXAccess.GetUserID() && Status == 0)
            {
                Status = 1;
            }

            var row1 = PXCache<Contact>.GetExtension<ContactExt>(row);

            switch (Status)
            {
                case 0:
                    Base.Caches[typeof(Address)].AllowDelete = false;
                    Base.Caches[typeof(Address)].AllowUpdate = false;
                    
                    Base.Caches[typeof(Contact)].AllowDelete = false;
                    Base.Caches[typeof(Contact)].AllowUpdate = false;

                    row1.HasRigths = false;
                    break;
                case 1:
                    Base.Caches[typeof(Address)].AllowDelete = false;
                    Base.Caches[typeof(Address)].AllowUpdate = true;

                    Base.Caches[typeof(Contact)].AllowDelete = false;
                    Base.Caches[typeof(Contact)].AllowUpdate = true;

                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowDelete = false;
                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowUpdate = false;
                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowInsert = false;

                    row1.HasRigths = true;
                    break;
                case 2:
                    Base.Caches[typeof(Address)].AllowDelete = true;
                    Base.Caches[typeof(Address)].AllowUpdate = true;

                    Base.Caches[typeof(Contact)].AllowDelete = true;
                    Base.Caches[typeof(Contact)].AllowUpdate = true;

                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowDelete = true;
                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowUpdate = true;
                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowInsert = false;

                    row1.HasRigths = true;
                    break;
                default:
                    Base.Caches[typeof(Address)].AllowDelete = false;
                    Base.Caches[typeof(Address)].AllowUpdate = false;

                    Base.Caches[typeof(Contact)].AllowDelete = false;
                    Base.Caches[typeof(Contact)].AllowUpdate = false;

                    row1.HasRigths = false;
                    break;
            }

            BAccount bAcc = PXSelect<BAccount,Where<BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>.Select(sender.Graph, row?.BAccountID);
            if ((bAcc != null) && (bAcc.Type != BAccountType.ProspectType) && (sender.GetStatus(row) != PXEntryStatus.Inserted))
            {
                PXUIFieldAttribute.SetEnabled<Contact.isActive>(sender, row, false);
                PXUIFieldAttribute.SetEnabled<Contact.eMail>(sender, row, false);
            }
            else
            {
                PXUIFieldAttribute.SetEnabled<Contact.isActive>(sender, row, true);
                PXUIFieldAttribute.SetEnabled<Contact.eMail>(sender, row, true);
            }
        }

        protected virtual void Users_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
        {
            // for Action Hide
            if (sel != null)
                sel(sender, e);

            var row = e.Row as Users;
            if (row == null) return;

            int Status = 0;

            PXUIFieldAttribute.SetEnabled<Contact.isActive>(sender, row, PXAccess.GetUserID() != row.PKID);

            // есть ли у него права на добавление
            var list = PXSelectJoin<EPLoginType,
                               InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                               InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                               Where<Users.pKID, Equal<Current<AccessInfo.userID>>,
                               And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base);

            if (list == null || list.Count == 0)
            {
                Base.Actions["Insert"].SetVisible(false);
            }

            if (row.PKID != null)
            {
                // Есть у него права на редактирование
                var list1 = PXSelectJoin<EPLoginType,
                                   InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                                   InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                                   Where<Users.pKID, Equal<Current<AccessInfo.userID>>,
                                   And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base);

                var list2 = PXSelectJoin<EPLoginType,
                                InnerJoin<Users, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>,
                                Where<Users.pKID, Equal<Required<Users.pKID>>, And<EPLoginType.entity,
                                Equal<EPLoginType.entity.contact>>>>.Select(Base, row.PKID);

                if (list2 == null || list2.Count == 0)
                {
                    Status = 2;
                }
                else
                {
                    foreach (var res in list2)
                    {
                        EPLoginType lt = res[typeof(EPLoginType)] as EPLoginType;
                        foreach (var res1 in list1)
                        {
                            EPManagedLoginType mt = res1[typeof(EPManagedLoginType)] as EPManagedLoginType;
                            if (mt.LoginTypeID == lt.LoginTypeID)
                            {
                                Status = 2;
                            }
                        }
                    }
                }
            }

            else
            {
                Status = 2;
            }

            if (row.PKID == PXAccess.GetUserID() && Status == 0)
            {
                Status = 1;
            }

            switch (Status)
            {
                case 1:
                    Base.Actions["ResetPassword"].SetEnabled(true);
                    Base.Actions["ActivateLogin"].SetEnabled(false);
                    Base.Actions["EnableLogin"].SetEnabled(false);
                    Base.Actions["DisableLogin"].SetEnabled(false);
                    Base.Actions["UnlockLogin"].SetEnabled(false);

                    PXUIFieldAttribute.SetEnabled<Users.loginTypeID>(sender.Graph.Caches[typeof(Users)], Base.User.Current, false);
                    PXUIFieldAttribute.SetEnabled<Users.state>(Base.Caches[typeof(Users)], Base.User.Current, false);
                    PXUIFieldAttribute.SetEnabled<Users.username>(Base.Caches[typeof(Users)], Base.User.Current, false);
                    PXUIFieldAttribute.SetEnabled<Users.password>(Base.Caches[typeof(Users)], Base.User.Current, true);
                    PXUIFieldAttribute.SetEnabled<Users.generatePassword>(Base.Caches[typeof(Users)], Base.User.Current, true);
                    break;
            }
        }

        protected virtual void Address_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
        {
            var row = e.Row as Address;
            if (row == null) return;

            if (sel != null)
                sel(sender, e);

            int Status = 0;

            PXUIFieldAttribute.SetEnabled<Contact.isActive>(sender, row, PXAccess.GetUserID() != Base.Contact.Current?.UserID);

            // есть ли у него права на добавление
            var list = PXSelectJoin<EPLoginType,
                               InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                               InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                               Where<Users.pKID, Equal<Current<AccessInfo.userID>>,
                               And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base);

            if (list == null || list.Count == 0)
            {
                Base.Actions["Insert"].SetVisible(false);
            }

            if (Base.Contact.Current?.UserID != null)
            {
                // Есть у него права на редактирование
                var list1 = PXSelectJoin<EPLoginType,
                                   InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                                   InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                                   Where<Users.pKID, Equal<Current<AccessInfo.userID>>,
                                   And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base);

                var list2 = PXSelectJoin<EPLoginType,
                                InnerJoin<Users, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>,
                                Where<Users.pKID, Equal<Required<Users.pKID>>, And<EPLoginType.entity,
                                Equal<EPLoginType.entity.contact>>>>.Select(Base, Base.Contact.Current.UserID);

                if (list2 == null || list2.Count == 0)
                {
                    Status = 2;
                }
                else
                {
                    foreach (var res in list2)
                    {
                        EPLoginType lt = res[typeof(EPLoginType)] as EPLoginType;
                        foreach (var res1 in list1)
                        {
                            EPManagedLoginType mt = res1[typeof(EPManagedLoginType)] as EPManagedLoginType;
                            if (mt.LoginTypeID == lt.LoginTypeID)
                            {
                                Status = 2;
                            }
                        }
                    }
                }
            }

            else
            {
                Status = 2;
            }

            if (Base.Contact.Current?.UserID == PXAccess.GetUserID() && Status == 0)
            {
                Status = 1;
            }

            switch (Status)
            {
                case 0:
                    sender.AllowDelete = false;
                    sender.AllowUpdate = false;
                    PXUIFieldAttribute.SetEnabled<Address.addressLine1>(sender, row, false);
                    break;
                case 1:
                    sender.AllowDelete = false;
                    sender.AllowUpdate = true;
                    break;
                case 2:
                    sender.AllowDelete = true;
                    sender.AllowUpdate = true;
                    break;
                default:
                    sender.AllowDelete = false;
                    sender.AllowUpdate = false;
                    break;
            }
        }

        protected virtual void Contact_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e, PXRowUpdated sel)
        {
            var row = e.Row as Contact;
            if (row == null) return;

            if (sel != null)
                sel(sender, e);

            int Status = 0;

            PXUIFieldAttribute.SetEnabled<Contact.isActive>(sender, row, PXAccess.GetUserID() != row.UserID);

            // есть ли у него права на добавление
            var list = PXSelectJoin<EPLoginType,
                               InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                               InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                               Where<Users.pKID, Equal<Current<AccessInfo.userID>>,
                               And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base);

            if (list == null || list.Count == 0)
            {
                Base.Actions["Insert"].SetVisible(false);
            }

            if (row.UserID != null)
            {
                // Есть у него права на редактирование
                var list1 = PXSelectJoin<EPLoginType,
                                   InnerJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
                                   InnerJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>>>,
                                   Where<Users.pKID, Equal<Current<AccessInfo.userID>>,
                                   And<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>>.Select(Base);

                var list2 = PXSelectJoin<EPLoginType,
                                InnerJoin<Users, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>,
                                Where<Users.pKID, Equal<Required<Users.pKID>>, And<EPLoginType.entity,
                                Equal<EPLoginType.entity.contact>>>>.Select(Base, row.UserID);

                if (list2 == null || list2.Count == 0)
                {
                    Status = 2;
                }
                else
                {
                    foreach (var res in list2)
                    {
                        EPLoginType lt = res[typeof(EPLoginType)] as EPLoginType;
                        foreach (var res1 in list1)
                        {
                            EPManagedLoginType mt = res1[typeof(EPManagedLoginType)] as EPManagedLoginType;
                            if (mt.LoginTypeID == lt.LoginTypeID)
                            {
                                Status = 2;
                            }
                        }
                    }
                }
            }

            else
            {
                Status = 2;
            }

            if (row.UserID == PXAccess.GetUserID() && Status == 0)
            {
                Status = 1;
            }

            var row1 = PXCache<Contact>.GetExtension<ContactExt>(row);

            switch (Status)
            {
                case 0:
                    Base.Caches[typeof(Address)].AllowDelete = false;
                    Base.Caches[typeof(Address)].AllowUpdate = false;

                    Base.Caches[typeof(Contact)].AllowDelete = false;
                    Base.Caches[typeof(Contact)].AllowUpdate = false;

                    row1.HasRigths = false;
                    break;
                case 1:
                    Base.Caches[typeof(Address)].AllowDelete = false;
                    Base.Caches[typeof(Address)].AllowUpdate = true;

                    Base.Caches[typeof(Contact)].AllowDelete = false;
                    Base.Caches[typeof(Contact)].AllowUpdate = true;

                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowDelete = false;
                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowUpdate = false;
                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowInsert = false;

                    row1.HasRigths = true;
                    break;
                case 2:
                    Base.Caches[typeof(Address)].AllowDelete = true;
                    Base.Caches[typeof(Address)].AllowUpdate = true;

                    Base.Caches[typeof(Contact)].AllowDelete = true;
                    Base.Caches[typeof(Contact)].AllowUpdate = true;

                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowDelete = true;
                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowUpdate = true;
                    Base.Caches[typeof(EPLoginTypeAllowsRole)].AllowInsert = false;

                    row1.HasRigths = true;
                    break;
                default:
                    Base.Caches[typeof(Address)].AllowDelete = false;
                    Base.Caches[typeof(Address)].AllowUpdate = false;

                    Base.Caches[typeof(Contact)].AllowDelete = false;
                    Base.Caches[typeof(Contact)].AllowUpdate = false;

                    row1.HasRigths = false;
                    break;
            }
        }

        #endregion
	}
}
