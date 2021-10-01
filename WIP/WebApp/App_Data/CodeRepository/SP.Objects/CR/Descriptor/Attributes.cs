using System;
using System.Collections;
using PX.Common;
using PX.Data;
using PX.EP;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.SM;

namespace SP.Objects.CR
{
	#region SPContactSelectorAttribute

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
	public class SPContactProductSelectorAttribute : PXCustomSelectorAttribute
	{
		public SPContactProductSelectorAttribute(params Type[] contactTypes)
			: base(typeof (Contact.contactID))
		{
			base.DescriptionField = typeof (Contact.displayName);
			base.Filterable = true;
		}

		public override Type DescriptionField
		{
			get { return base.DescriptionField; }
			set { }
		}

		public override bool Filterable
		{
			get { return base.Filterable; }
			set { }
		}

		public IEnumerable GetRecords()
		{
            foreach (BAccount _bAccount in PXSelect<BAccount,
                Where<BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>.Select(new PXGraph(),
                                                                                                     ReadBAccount.ReadCurrentAccount()
					                                                                                     .With(_ => _.BAccountID)))
			{
				foreach (var contact in PXSelectJoin<Contact,
                    LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.bAccountID>>,
						LeftJoin<Address, On<Address.addressID, Equal<Contact.defAddressID>>,
							LeftJoin<Users, On<Users.pKID, Equal<Contact.userID>>,
								LeftJoin<EPLoginType, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>,
								LeftJoin<CRContactClass, On<CRContactClass.classID, Equal<Contact.classID>>>>>>>,
					Where2<Where<CRContactClass.isInternal, Equal<False>, Or<CRContactClass.isInternal, IsNull>>,
						And<Contact.contactType, Equal<ContactTypesAttribute.person>,
						And<Contact.bAccountID, Equal<Required<Contact.bAccountID>>>>>>.Select(new PXGraph(), _bAccount.BAccountID))

					yield return contact;

			}
		}
	}
	#endregion

	public class SPLoginTypeSelectorAttribute : PXCustomSelectorAttribute
	{
		public SPLoginTypeSelectorAttribute()
			: base(typeof(EPLoginType.loginTypeID))
		{
			base.SubstituteKey = typeof(EPLoginType.loginTypeName);
		}

		public virtual IEnumerable GetRecords()
		{
			Users current = PXSelect<Users, Where<Users.pKID, Equal<Required<Users.pKID>>>>.Select(_Graph, _Graph.Accessinfo.UserID);
			return PXSelect<EPLoginType, Where<EPLoginType.entity, Equal<EPLoginType.entity.contact>>>.Select(_Graph).RowCast<EPLoginType>();
		}
	}

    #region SPContactPartnerSelectorAttribute

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class SPContactPartnerSelectorAttribute : PXCustomSelectorAttribute
    {
        public SPContactPartnerSelectorAttribute(params Type[] contactTypes)
            : base(typeof(Contact.contactID))
        {
            base.DescriptionField = typeof(Contact.displayName);
            base.Filterable = true;
        }

        public override Type DescriptionField
        {
            get
            {
                return base.DescriptionField;
            }
            set
            {
            }
        }

        public override bool Filterable
        {
            get
            {
                return base.Filterable;
            }
            set
            {
            }
        }

        public IEnumerable GetRecords()
        {
            foreach (BAccount _bAccount in PXSelect<BAccount,
                Where<BAccount.parentBAccountID, Equal<Required<BAccount.parentBAccountID>>>>.Select(new PXGraph(),
                                                                                         ReadBAccount.ReadCurrentAccount()
                                                                                             .With(_ => _.BAccountID)))
            {
                foreach (var contact in PXSelectJoin<Contact,
            LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.bAccountID>>,
            LeftJoin<Address, On<Address.addressID, Equal<Contact.defAddressID>>,
            LeftJoin<Users, On<Users.pKID, Equal<Contact.userID>>,
			LeftJoin<CRContactClass, On<CRContactClass.classID, Equal<Contact.classID>>,
            LeftJoin<EPLoginType, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>>>>>,
            Where2<Where<CRContactClass.isInternal, Equal<False>, Or<CRContactClass.isInternal, IsNull>>,
				And<Contact.contactType, Equal<ContactTypesAttribute.person>,
                And<Contact.bAccountID, Equal<Required<Contact.bAccountID>>>>>>.Select(new PXGraph(), _bAccount.BAccountID))

                    yield return contact;

            }

            foreach (var contact1 in PXSelectJoin<Contact,
            LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.bAccountID>>,
            LeftJoin<Address, On<Address.addressID, Equal<Contact.defAddressID>>,
            LeftJoin<Users, On<Users.pKID, Equal<Contact.userID>>,
			LeftJoin<CRContactClass, On<CRContactClass.classID, Equal<Contact.classID>>,
			LeftJoin<EPLoginType, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>>>>>>,
			Where2<Where<CRContactClass.isInternal, Equal<False>, Or<CRContactClass.isInternal, IsNull>>,
			And<Contact.contactType, Equal<ContactTypesAttribute.person>,
                And<Contact.parentBAccountID, Equal<Required<Contact.parentBAccountID>>>>>>.Select(new PXGraph(),
                                                                                         ReadBAccount.ReadCurrentAccount()
                                                                                             .With(_ => _.BAccountID)))
            {
                yield return contact1;
            }
        }
    }
    #endregion
}
