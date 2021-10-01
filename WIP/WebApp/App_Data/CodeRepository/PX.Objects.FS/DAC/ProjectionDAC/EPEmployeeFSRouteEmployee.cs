using System;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;

namespace PX.Objects.FS
{
    [Serializable]
    [PXPrimaryGraph(typeof(EPEmployeeMaintBridge))]
    [PXProjection(typeof(
        Select2<EPEmployee,
            InnerJoin<BAccount,
                On<BAccount.bAccountID, Equal<EPEmployee.bAccountID>>,
            InnerJoin<FSRouteEmployee,
                On<FSRouteEmployee.employeeID, Equal<BAccount.bAccountID>>>>>))]
    public class EPEmployeeFSRouteEmployee : EPEmployee
    {
        #region Keys
        public new class PK : PrimaryKeyOf<EPEmployeeFSRouteEmployee>.By<bAccountID, acctCD>
        {
            public static EPEmployeeFSRouteEmployee Find(PXGraph graph, int? bAccountID, string acctCD) => FindBy(graph, bAccountID, acctCD);
        }

        public new static class FK
        {
            public class Class : CR.CRCustomerClass.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<classID> { }
            public class ParentBusinessAccount : CR.BAccount.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<parentBAccountID> { }
            public class Address : CR.Address.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<defAddressID> { }
            public class ContactInfo : CR.Contact.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<defContactID> { }
            public class PrimaryContact : CR.Contact.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<primaryContactID> { }
            public class Department : EP.EPDepartment.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<departmentID> { }
            public class ReportsTo : EP.EPEmployee.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<supervisorID> { }
            public class SalesPerson : AR.SalesPerson.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<salesPersonID> { }
            public class LabourItem : IN.InventoryItem.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<labourItemID> { }

            public class SalesAccount : GL.Account.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<salesAcctID> { }
            public class SalesSubaccount : GL.Sub.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<salesSubID> { }

            public class CashDiscountAccount : GL.Account.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<discTakenAcctID> { }
            public class CashDiscountSubaccount : GL.Sub.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<discTakenSubID> { }

            public class ExpenseAccount : GL.Account.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<expenseAcctID> { }
            public class ExpenseSubaccount : GL.Sub.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<expenseSubID> { }

            public class PrepaymentAccount : GL.Account.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<prepaymentAcctID> { }
            public class PrepaymentSubaccount : GL.Sub.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<prepaymentSubID> { }

            public class TaxZone : PX.Objects.TX.TaxZone.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<taxZoneID> { }

            public class Owner : EP.EPEmployee.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<ownerID> { }
            public class Workgroup : TM.EPCompanyTree.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<workgroupID> { }

            public class User : PX.SM.Users.PK.ForeignKeyOf<EPEmployeeFSRouteEmployee>.By<userID> { }
        }
        #endregion

        #region BAccountID
        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }

        [PXDBInt(IsKey = true, BqlField = typeof(BAccount.bAccountID))]
        [PXUIField(Visible = false, Visibility = PXUIVisibility.Invisible)]
        public override int? BAccountID { get; set; }
        #endregion

        #region AcctCD
        public new abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }

        [EmployeeRaw]
        [PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(BAccount.acctCD))]
        [PXDefault]
        [PXFieldDescription]
        [PXUIField(DisplayName = "Driver ID", Visibility = PXUIVisibility.SelectorVisible)]
        public override string AcctCD { get; set; }
        #endregion

        #region AcctName
        public new abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }

        [PXDBString(60, IsUnicode = true, BqlField = typeof(BAccount.acctName))]
        [PXDefault]
        [PXUIField(DisplayName = "Driver Name", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
        public override string AcctName { get; set; }
        #endregion

        #region RouteID
        public abstract class routeID : PX.Data.BQL.BqlInt.Field<routeID> { }

        [PXDefault]
        [PXDBInt(BqlField = typeof(FSRouteEmployee.routeID))]
        public virtual int? RouteID { get; set; }
        #endregion

        #region PriorityPreference
        public abstract class priorityPreference : PX.Data.BQL.BqlInt.Field<priorityPreference> { }

        [PXDBInt(BqlField = typeof(FSRouteEmployee.priorityPreference))]
        [PXDefault(1)]
        [PXUIField(DisplayName = "Priority Preference", Required = true, Visibility = PXUIVisibility.SelectorVisible)]
        public virtual int? PriorityPreference { get; set; }
        #endregion

        #region VStatus
        public new abstract class vStatus : PX.Data.BQL.BqlString.Field<vStatus>
        {
        }

        [PXDBString(1, IsFixed = true, BqlField = typeof(BAccount.vStatus))]
        [PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible)]
        [VendorStatus.List]
        public override string VStatus { get; set; }
        #endregion

        #region DepartmentID
        public new abstract class departmentID : PX.Data.BQL.BqlString.Field<departmentID> { }

        [PXDBString(10, IsUnicode = true, BqlField = typeof(EPEmployee.departmentID))]
        [PXDefault]
        [PXSelector(typeof(EPDepartment.departmentID), DescriptionField = typeof(EPDepartment.description))]
        [PXUIField(DisplayName = "Department", Visibility = PXUIVisibility.SelectorVisible)]
        public override string DepartmentID { get; set; }
        #endregion

        #region Memory Helper
        #region MemDriverName
        public abstract class memDriverName : PX.Data.BQL.BqlString.Field<memDriverName>
        {
        }

        [PXString]
        [PXUIField(DisplayName = "Driver Name", Enabled = false)]
        public virtual string MemDriverName { get; set; }
        #endregion
        #endregion

        public new abstract class parentBAccountID : PX.Data.BQL.BqlInt.Field<parentBAccountID> { }
        public new abstract class defContactID : PX.Data.BQL.BqlInt.Field<defContactID> { }
        public new abstract class defAddressID : PX.Data.BQL.BqlInt.Field<defAddressID> { }
        public new abstract class supervisorID : PX.Data.BQL.BqlInt.Field<supervisorID> { }
        public new abstract class salesPersonID : PX.Data.BQL.BqlInt.Field<salesPersonID> { }
        public new abstract class labourItemID : PX.Data.BQL.BqlInt.Field<labourItemID> { }
        public new abstract class classID : PX.Data.BQL.BqlString.Field<classID> { }
        public new abstract class salesAcctID : PX.Data.BQL.BqlInt.Field<salesAcctID> { }
        public new abstract class salesSubID : PX.Data.BQL.BqlInt.Field<salesSubID> { }
        public new abstract class primaryContactID : PX.Data.BQL.BqlInt.Field<primaryContactID> { }
        public new abstract class discTakenAcctID : PX.Data.BQL.BqlInt.Field<discTakenAcctID> { }
        public new abstract class discTakenSubID : PX.Data.BQL.BqlInt.Field<discTakenSubID> { }
        public new abstract class expenseAcctID : PX.Data.BQL.BqlInt.Field<expenseAcctID> { }
        public new abstract class expenseSubID : PX.Data.BQL.BqlInt.Field<expenseSubID> { }
        public new abstract class prepaymentAcctID : PX.Data.BQL.BqlInt.Field<prepaymentAcctID> { }
        public new abstract class prepaymentSubID : PX.Data.BQL.BqlInt.Field<prepaymentSubID> { }
        public new abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }
        public new abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
        public new abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
        public new abstract class userID : PX.Data.BQL.BqlGuid.Field<userID> { }
    }
}
