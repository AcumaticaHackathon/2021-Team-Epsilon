using System;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;
using SP.Objects.SP;

namespace SP.Objects.CR.DAC
{

    [System.SerializableAttribute()]
    [CRCacheIndependentPrimaryGraphList(new Type[]{
        typeof(PX.Objects.CR.BusinessAccountMaint),
		typeof(PX.Objects.EP.EmployeeMaint),
		typeof(PX.Objects.AP.VendorMaint),
		typeof(PX.Objects.AP.VendorMaint),
		typeof(PX.Objects.AR.CustomerMaint),
		typeof(PX.Objects.AR.CustomerMaint),
		typeof(PX.Objects.AP.VendorMaint),
		typeof(PX.Objects.AR.CustomerMaint),
		typeof(PX.Objects.CR.BusinessAccountMaint)},
        new Type[]{
            typeof(Select<PX.Objects.CR.BAccount, Where<PX.Objects.CR.BAccount.bAccountID, Equal<Current<BAccount.bAccountID>>,
                    And<Current<BAccount.viewInCrm>, Equal<True>>>>),
			typeof(Select<PX.Objects.EP.EPEmployee, Where<PX.Objects.EP.EPEmployee.bAccountID, Equal<Current<BAccount.bAccountID>>>>),
			typeof(Select<PX.Objects.AP.VendorR, Where<PX.Objects.AP.VendorR.bAccountID, Equal<Current<BAccount.bAccountID>>>>), 
			typeof(Select<PX.Objects.AP.Vendor, Where<PX.Objects.AP.Vendor.bAccountID, Equal<Current<BAccountR.bAccountID>>>>), 
			typeof(Select<PX.Objects.AR.Customer, Where<PX.Objects.AR.Customer.bAccountID, Equal<Current<BAccount.bAccountID>>>>),
			typeof(Select<PX.Objects.AR.Customer, Where<PX.Objects.AR.Customer.bAccountID, Equal<Current<BAccountR.bAccountID>>>>),
			typeof(Where<PX.Objects.CR.BAccountR.bAccountID, Less<Zero>,
					And<BAccountR.type, Equal<BAccountType.vendorType>>>), 
			typeof(Where<PX.Objects.CR.BAccountR.bAccountID, Less<Zero>,
					And<BAccountR.type, Equal<BAccountType.customerType>>>), 
			typeof(Select<PX.Objects.CR.BAccount, 
				Where<PX.Objects.CR.BAccount.bAccountID, Equal<Current<BAccount.bAccountID>>, 
					Or<Current<BAccount.bAccountID>, Less<Zero>>>>)
		})]
    [PXCacheName(PX.Objects.CR.Messages.BusinessAccount)]
    [CREmailContactsView(typeof(Select2<Contact,
        LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.bAccountID>>>,
        Where<Contact.bAccountID, Equal<Optional<BAccount.bAccountID>>,
                Or<Contact.contactType, Equal<ContactTypesAttribute.employee>>>>))]
    [PXEMailSource]//NOTE: for assignment map
	public class BAccountExt : PXCacheExtension<BAccount>
	{
        #region ClassID
        public abstract class classID : PX.Data.IBqlField { }

        [PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
        [PXMassMergableField]
        [PXMassUpdatableField]
        [PXSelector(typeof(Search<CRCustomerClass.cRCustomerClassID,
                Where<CRCustomerClass.isInternal, Equal<False>>>),
                DescriptionField = typeof(CRCustomerClass.description), CacheGlobal = true)]
        [PXUIField(DisplayName = "Class ID")]
        public virtual String ClassID { get; set; }
        #endregion 
	}
}
