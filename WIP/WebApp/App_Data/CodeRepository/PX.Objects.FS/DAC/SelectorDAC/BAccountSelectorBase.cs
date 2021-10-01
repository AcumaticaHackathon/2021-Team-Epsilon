using System;
using PX.Data;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;

namespace PX.Objects.FS
{
    [System.SerializableAttribute]
    [CRCacheIndependentPrimaryGraphList(new Type[]{
        typeof(AR.CustomerMaint),
        typeof(EP.EmployeeMaint),
        typeof(AP.VendorMaint),
        typeof(CR.BusinessAccountMaint)},
        new Type[]{
            typeof(Select<AR.Customer, Where<Current<BAccount.bAccountID>, Less<Zero>,
                            Or<AR.Customer.bAccountID, Equal<Current<BAccount.bAccountID>>>>>),
            typeof(Select<EP.EPEmployee, Where<EP.EPEmployee.bAccountID, Equal<Current<BAccount.bAccountID>>>>),
            typeof(Select<AP.Vendor, Where<AP.Vendor.bAccountID, Equal<Current<BAccount.bAccountID>>>>),
            typeof(Select<CR.BAccount,
                Where2<Where<
                    CR.BAccount.type, Equal<BAccountType.prospectType>,
                    Or<CR.BAccount.type, Equal<BAccountType.customerType>,
                    Or<CR.BAccount.type, Equal<BAccountType.vendorType>,
                    Or<CR.BAccount.type, Equal<BAccountType.combinedType>>>>>,
                        And<Where<CR.BAccount.bAccountID, Equal<Current<BAccount.bAccountID>>,
                        Or<Current<BAccount.bAccountID>, Less<Zero>>>>>>)
        })]
    public class BAccountSelectorBase : BAccount
    {
        #region Keys
        public new class PK : PrimaryKeyOf<BAccountSelectorBase>.By<acctCD>
        {
            public static BAccountSelectorBase Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
        }
        public new class UK : PrimaryKeyOf<BAccount>.By<acctCD>
        {
            public static BAccount Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
        }
        public new static class FK
        {
            public class Class : CR.CRCustomerClass.PK.ForeignKeyOf<BAccount>.By<classID> { }
            public class ParentBusinessAccount : CR.BAccount.PK.ForeignKeyOf<BAccount>.By<parentBAccountID> { }

            public class Address : CR.Address.PK.ForeignKeyOf<BAccount>.By<defAddressID> { }
            public class ContactInfo : CR.Contact.PK.ForeignKeyOf<BAccount>.By<defContactID> { }
            public class PrimaryContact : CR.Contact.PK.ForeignKeyOf<BAccount>.By<primaryContactID> { }

            public class TaxZone : PX.Objects.TX.TaxZone.PK.ForeignKeyOf<BAccount>.By<taxZoneID> { }

            public class Owner : EP.EPEmployee.PK.ForeignKeyOf<BAccount>.By<ownerID> { }
            public class Workgroup : TM.EPCompanyTree.PK.ForeignKeyOf<BAccount>.By<workgroupID> { }
        }
        #endregion

        #region BAccountID
        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
        #endregion
        #region AcctCD
        public new abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }

        [PXDimensionSelector("BIZACCT", typeof(BAccount.acctCD), typeof(BAccount.acctCD), DescriptionField = typeof(BAccount.acctName))]
        [PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
        [PXDefault()]
        [PXUIField(DisplayName = "Account ID", Visibility = PXUIVisibility.SelectorVisible)]
        [PXFieldDescription]
        public override string AcctCD { get; set; }
        #endregion
        #region AcctName
        public new abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }

        [PXDBString(60, IsUnicode = true)]
        [PXDefault]
        [PXFieldDescription]
        [PXMassMergableField]
        [PXUIField(DisplayName = "Account Name", Visibility = PXUIVisibility.SelectorVisible)]
        public override string AcctName { get; set; }
        #endregion
        #region Type
        public new abstract class type : PX.Data.BQL.BqlString.Field<type> { }

        [PXDBString(2, IsFixed = true)]
        [PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [EmployeeType.List()]
        public override string Type { get; set; }
        #endregion

        public new abstract class classID : PX.Data.BQL.BqlString.Field<classID> { }
        public new abstract class parentBAccountID : PX.Data.BQL.BqlInt.Field<parentBAccountID> { }
        public new abstract class defAddressID : PX.Data.BQL.BqlInt.Field<defAddressID> { }
        public new abstract class defContactID : PX.Data.BQL.BqlInt.Field<defContactID> { }
        public new abstract class primaryContactID : PX.Data.BQL.BqlInt.Field<primaryContactID> { }
        public new abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }
        public new abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
        public new abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
    }
}
