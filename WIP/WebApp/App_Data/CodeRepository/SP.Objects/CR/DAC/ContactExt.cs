using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;
using System;
using PX.Objects.SP.DAC;

namespace SP.Objects.CR
{
    [Serializable]
    [CRContactCacheName("Lead/Contact")]
    [PXEMailSource]
	[CRPrimaryGraphRestricted(
		new[]{
			typeof(ProductContactMaint)
		},
		new[]{
			typeof(Select<
				Contact,
				Where<
					Contact.contactID, Equal<Current<Contact.contactID>>,
					And<MatchWithBAccountNotNull<Contact.bAccountID>>>>)
		})]
	public class ContactExt : PXCacheExtension<Contact>
    {
        #region ClassID
        public abstract class classID : PX.Data.IBqlField { }

        [PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
        [PXMassMergableField]
        [PXMassUpdatableField]
        [PXDefault(typeof(PortalSetup.defaultContactClassID), PersistingCheck = PXPersistingCheck.Nothing)]
        [PXSelector(typeof(Search<CRContactClass.classID,
                Where<CRContactClass.isInternal, Equal<False>>>),
                DescriptionField = typeof(CRContactClass.description), CacheGlobal = true)]
        [PXUIField(DisplayName = "Class ID")]
        public virtual String ClassID { get; set; }
        #endregion

        #region FirstName
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "First Name")]
        [PXMassMergableField]
        public virtual string FirstName { get; set; }
        #endregion


        #region HasRigths
        public abstract class hasRigths : PX.Data.IBqlField { }
        [PXBool()]
        [PXUIField(Visible = false)]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? HasRigths { get; set; }
        #endregion        
    }
}
