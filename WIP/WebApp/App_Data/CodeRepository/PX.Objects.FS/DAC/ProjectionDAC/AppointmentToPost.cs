using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.PM;
using PX.Objects.SO;
using PX.Objects.TX;
using System;

namespace PX.Objects.FS
{
    #region PXProjection
    [Serializable]
    [PXProjection(typeof(
            Select2<FSAppointment,
                InnerJoin<FSServiceOrder, 
                    On<
                        FSServiceOrder.sOID, Equal<FSAppointment.sOID>>,
                InnerJoin<FSSrvOrdType,
                    On<
                        FSSrvOrdType.srvOrdType, Equal<FSAppointment.srvOrdType>>,
                InnerJoin<FSCustomerBillingSetup,
                    On<
                        FSCustomerBillingSetup.cBID, Equal<FSServiceOrder.cBID>>,
                InnerJoin<FSBillingCycle,
                    On<
                        FSBillingCycle.billingCycleID, Equal<FSCustomerBillingSetup.billingCycleID>>>>>>,
                Where2<
                    Where2<
                        Where<
                            FSAppointment.pendingAPARSOPost, Equal<True>,
                            And<FSAppointment.billServiceContractID, IsNull,
                            And<FSAppointment.billContractPeriodID, IsNull,
                            And<FSServiceOrder.billServiceContractID, IsNull,
                            And<FSServiceOrder.billContractPeriodID, IsNull>>>>>,
                        And<
                            FSAppointment.closed, Equal<True>,
                            Or<
                                Where<
                                        FSSrvOrdType.allowInvoiceOnlyClosedAppointment, Equal<False>,
                                    And<FSAppointment.completed, Equal<True>,
                                        And<FSAppointment.timeRegistered, Equal<True>>>>>>>,
                    And<
                        Where<
                            FSServiceOrder.closed, Equal<True>,
                            Or<FSServiceOrder.completed, Equal<True>,
                            Or<
                                Where<FSBillingCycle.invoiceOnlyCompletedServiceOrder, Equal<False>,
                                    And<FSServiceOrder.openDoc, Equal<True>>>>>>>>>))]
    [PXGroupMask(typeof(InnerJoinSingleTable<AR.Customer, On<AR.Customer.bAccountID, Equal<AppointmentToPost.billCustomerID>, And<Match<AR.Customer, Current<AccessInfo.userName>>>>>))]
    #endregion
    public class AppointmentToPost : FSAppointment, IPostLine
    {
        #region Keys
        public new class PK : PrimaryKeyOf<AppointmentToPost>.By<srvOrdType, refNbr>
        {
            public static AppointmentToPost Find(PXGraph graph, string srvOrdType, string refNbr) => FindBy(graph, srvOrdType, refNbr);
        }
        public new class UK : PrimaryKeyOf<AppointmentToPost>.By<appointmentID>
        {
            public static AppointmentToPost Find(PXGraph graph, int? appointmentID) => FindBy(graph, appointmentID);
        }
        public new static class FK
        {
            public class Customer : AR.Customer.PK.ForeignKeyOf<AppointmentToPost>.By<customerID> { }
            public class Branch : GL.Branch.PK.ForeignKeyOf<AppointmentToPost>.By<branchID> { }
            public class ServiceOrderType : FSSrvOrdType.PK.ForeignKeyOf<AppointmentToPost>.By<srvOrdType> { }
            public class ServiceOrder : FSServiceOrder.PK.ForeignKeyOf<AppointmentToPost>.By<srvOrdType, soRefNbr> { }
            public class Project : PMProject.PK.ForeignKeyOf<AppointmentToPost>.By<projectID> { }
            public class Task : PMTask.PK.ForeignKeyOf<AppointmentToPost>.By<dfltProjectTaskID> { }
            public class WorkFlowStage : FSWFStage.PK.ForeignKeyOf<AppointmentToPost>.By<wFStageID> { }
            public class ServiceContract : FSServiceContract.PK.ForeignKeyOf<AppointmentToPost>.By<serviceContractID> { }
            public class Schedule : FSSchedule.PK.ForeignKeyOf<AppointmentToPost>.By<scheduleID> { }
            public class BillServiceContract : FSServiceContract.PK.ForeignKeyOf<AppointmentToPost>.By<billServiceContractID> { }
            public class TaxZone : Objects.TX.TaxZone.PK.ForeignKeyOf<AppointmentToPost>.By<taxZoneID> { }
            public class Route : FSRoute.PK.ForeignKeyOf<AppointmentToPost>.By<routeID> { }
            public class RouteDocument : FSRouteDocument.PK.ForeignKeyOf<AppointmentToPost>.By<routeDocumentID> { }
            public class Vehicle : FSVehicle.PK.ForeignKeyOf<AppointmentToPost>.By<vehicleID> { }
            public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<AppointmentToPost>.By<curyInfoID> { }
            public class Currency : CM.Currency.PK.ForeignKeyOf<AppointmentToPost>.By<curyID> { }
            public class SalesPerson : AR.SalesPerson.PK.ForeignKeyOf<AppointmentToPost>.By<salesPersonID> { }
            public class PostSOOrder : SOOrderType.PK.ForeignKeyOf<AppointmentToPost>.By<postOrderType> { }
            public class PostOrderNegativeBalance : SOOrderType.PK.ForeignKeyOf<AppointmentToPost>.By<postOrderTypeNegativeBalance> { }
            public class DefaultTermsARSO : Terms.PK.ForeignKeyOf<AppointmentToPost>.By<dfltTermIDARSO> { }
            public class BillingCycle : FSBillingCycle.PK.ForeignKeyOf<AppointmentToPost>.By<billingCycleID> { }
            public class BillCustomer : AR.Customer.PK.ForeignKeyOf<AppointmentToPost>.By<billCustomerID> { }
            public class BillCustomerLocation : Location.PK.ForeignKeyOf<AppointmentToPost>.By<billCustomerID, billLocationID> { }
            public class PostBatch : FSPostBatch.PK.ForeignKeyOf<AppointmentToPost>.By<batchID> { }
            public class BranchLocation : FSBranchLocation.PK.ForeignKeyOf<AppointmentToPost>.By<branchLocationID> { }

        }
        #endregion

        #region SrvOrdType
        public new abstract class srvOrdType : PX.Data.BQL.BqlString.Field<srvOrdType> { }

        [PXDBString(4, IsFixed = true, IsKey = true, InputMask = ">AAAA", BqlField = typeof(FSAppointment.srvOrdType))]
        [PXUIField(DisplayName = "Service Order Type")]
        [FSSelectorSrvOrdTypeNOTQuote]
        [PX.Data.EP.PXFieldDescription]
        public override string SrvOrdType { get; set; }
        #endregion
        #region RefNbr
        public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

        [PXDBString(20, IsKey = true, IsUnicode = true, InputMask = "CCCCCCCCCCCCCCCCCCCC", BqlField = typeof(FSAppointment.refNbr))]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Appointment Nbr.", Visibility = PXUIVisibility.SelectorVisible, Visible = true, Enabled = true)]
        [PXSelector(typeof(
            Search2<FSAppointment.refNbr,
            LeftJoin<FSServiceOrder,
                On<FSServiceOrder.sOID, Equal<FSAppointment.sOID>>,
            LeftJoin<Customer,
                On<Customer.bAccountID, Equal<FSServiceOrder.customerID>>,
            LeftJoin<Location,
                On<Location.bAccountID, Equal<FSServiceOrder.customerID>,
                    And<Location.locationID, Equal<FSServiceOrder.locationID>>>>>>,
            Where2<
            Where<
                FSAppointment.srvOrdType, Equal<Optional<FSAppointment.srvOrdType>>>,
                And<Where<
                    Customer.bAccountID, IsNull,
                    Or<Match<Customer, Current<AccessInfo.userName>>>>>>,
            OrderBy<
                Desc<FSAppointment.refNbr>>>),
                    new Type[] {
                                typeof(FSAppointment.refNbr),
                                typeof(Customer.acctCD),
                                typeof(Customer.acctName),
                                typeof(Location.locationCD),
                                typeof(FSAppointment.docDesc),
                                typeof(FSAppointment.status),
                                typeof(FSAppointment.scheduledDateTimeBegin)
                    })]
        public override string RefNbr { get; set; }
        #endregion
        #region AppointmentID
        public new abstract class appointmentID : PX.Data.BQL.BqlInt.Field<appointmentID> { }

        [PXDBInt(BqlField = typeof(FSAppointment.appointmentID))]
        public override int? AppointmentID { get; set; }
        #endregion
        #region SORefNbr
        public new abstract class soRefNbr : PX.Data.BQL.BqlString.Field<soRefNbr> { }

        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDBString(15, IsUnicode = true, BqlField = typeof(FSAppointment.soRefNbr))]
        [PXUIField(DisplayName = "Service Order Nbr.")]
        [FSSelectorSORefNbr_Appointment]
        public override string SORefNbr { get; set; }
        #endregion

        public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

        #region PostTo
        public abstract class postTo : PX.Data.BQL.BqlString.Field<postTo> { }

        [PXDBString(2, BqlField = typeof(FSSrvOrdType.postTo))]
        [FSPostTo.List]
        [PXUIField(DisplayName = "Post To")]
        public virtual string PostTo { get; set; }
        #endregion
        #region PostOrderType
        public abstract class postOrderType : PX.Data.BQL.BqlString.Field<postOrderType> { }

        [PXDBString(2, BqlField = typeof(FSSrvOrdType.postOrderType))]
        public virtual string PostOrderType { get; set; }
        #endregion
        #region PostOrderTypeNegativeBalance
        public abstract class postOrderTypeNegativeBalance : PX.Data.BQL.BqlString.Field<postOrderTypeNegativeBalance> { }

        [PXDBString(2, BqlField = typeof(FSSrvOrdType.postOrderTypeNegativeBalance))]
        public virtual string PostOrderTypeNegativeBalance { get; set; }
        #endregion
        #region PostNegBalanceToAP
        public abstract class postNegBalanceToAP : PX.Data.BQL.BqlBool.Field<postNegBalanceToAP> { }

        [PXDBBool(BqlField = typeof(FSSrvOrdType.postNegBalanceToAP))]
        [PXUIField(DisplayName = "Create a Bill Document in AP for Negative Balances")]
        public virtual bool? PostNegBalanceToAP { get; set; }
        #endregion
        #region DfltTermIDARSO
        public abstract class dfltTermIDARSO : PX.Data.BQL.BqlString.Field<dfltTermIDARSO> { }

        [PXDBString(10, IsUnicode = true, BqlField = typeof(FSSrvOrdType.dfltTermIDARSO))]
        public virtual string DfltTermIDARSO { get; set; }
        #endregion

        #region BillingCycleID
        public abstract class billingCycleID : PX.Data.BQL.BqlInt.Field<billingCycleID> { }

        [PXDBInt(BqlField = typeof(FSCustomerBillingSetup.billingCycleID))]
        [PXSelector(
                    typeof(Search<FSBillingCycle.billingCycleID>),
                    SubstituteKey = typeof(FSBillingCycle.billingCycleCD),
                    DescriptionField = typeof(FSBillingCycle.descr))]
        [PXUIField(DisplayName = "Billing Cycle ID", Enabled = false)]
        public virtual int? BillingCycleID { get; set; }
        #endregion
        #region FrequencyType
        public abstract class frequencyType : ListField_Frequency_Type
        {
        }

        [PXDBString(2, IsFixed = true, BqlField = typeof(FSCustomerBillingSetup.frequencyType))]
        [frequencyType.ListAtrribute]
        [PXUIField(DisplayName = "Frequency Type", Enabled = false)]
        public virtual string FrequencyType { get; set; }
        #endregion
        #region WeeklyFrequency
        public abstract class weeklyFrequency : ListField_WeekDaysNumber
        {
        }

        [PXDBInt(BqlField = typeof(FSCustomerBillingSetup.weeklyFrequency))]
        [PXUIField(DisplayName = "Frequency Week Day")]
        [weeklyFrequency.ListAtrribute]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual int? WeeklyFrequency { get; set; }
        #endregion
        #region MonthlyFrequency
        public abstract class monthlyFrequency : PX.Data.BQL.BqlInt.Field<monthlyFrequency> { }

        [PXDBInt(BqlField = typeof(FSCustomerBillingSetup.monthlyFrequency))]
        [PXUIField(DisplayName = "Frequency Month Day")]
        [PXIntList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 }, new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31" })]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual int? MonthlyFrequency { get; set; }
        #endregion
        #region SendInvoicesTo
        public abstract class sendInvoicesTo : ListField_Send_Invoices_To
        {
        }

        [PXDBString(2, IsFixed = true, BqlField = typeof(FSCustomerBillingSetup.sendInvoicesTo))]
        [sendInvoicesTo.ListAtrribute]
        [PXUIField(DisplayName = "Send Invoices to")]
        public virtual string SendInvoicesTo { get; set; }
        #endregion

        #region BillingBy
        public abstract class billingBy : ListField_Billing_By
        {
        }

        [PXDBString(2, IsFixed = true, BqlField = typeof(FSBillingCycle.billingBy))]
        [billingBy.ListAtrribute]
        public virtual string BillingBy { get; set; }
        #endregion
        #region GroupBillByLocations
        public abstract class groupBillByLocations : PX.Data.BQL.BqlBool.Field<groupBillByLocations> { }

        [PXDBBool(BqlField = typeof(FSBillingCycle.groupBillByLocations))]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Create Separate Invoices for Customer Locations")]
        public virtual bool? GroupBillByLocations { get; set; }
        #endregion

        #region BillCustomerID
        public new abstract class billCustomerID : PX.Data.BQL.BqlInt.Field<billCustomerID> { }

        [PXDBInt(BqlField = typeof(FSServiceOrder.billCustomerID))]
        [PXUIField(DisplayName = "Billing Customer ID")]
        [FSSelectorCustomer]
        public override int? BillCustomerID { get; set; }
        #endregion
        #region BillLocationID
        public abstract class billLocationID : PX.Data.BQL.BqlInt.Field<billLocationID> { }

        [LocationID(typeof(Where<Location.bAccountID, Equal<Current<AppointmentToPost.billCustomerID>>>),
                    DescriptionField = typeof(Location.descr),
                    BqlField = typeof(FSServiceOrder.billLocationID), DisplayName = "Billing Location", DirtyRead = true)]
        public virtual int? BillLocationID { get; set; }
        #endregion
        #region BranchLocationID
        public abstract class branchLocationID : PX.Data.BQL.BqlInt.Field<branchLocationID> { }

        [PXDBInt(BqlField = typeof(FSServiceOrder.branchLocationID))]
        [PXDefault(typeof(
            Search<FSxUserPreferences.dfltBranchLocationID,
            Where<
                PX.SM.UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>,
                And<PX.SM.UserPreferences.defBranchID, Equal<Current<FSServiceOrder.branchID>>>>>))]
        [PXUIField(DisplayName = "Branch Location ID")]
        [PXSelector(typeof(
            Search<FSBranchLocation.branchLocationID,
            Where<
                FSBranchLocation.branchID, Equal<Current<FSServiceOrder.branchID>>>>),
            SubstituteKey = typeof(FSBranchLocation.branchLocationCD),
            DescriptionField = typeof(FSBranchLocation.descr))]
        [PXFormula(typeof(Default<FSServiceOrder.branchID>))]
        public virtual int? BranchLocationID { get; set; }
        #endregion
        #region Status
        public abstract class serviceOrderStatus : PX.Data.BQL.BqlString.Field<serviceOrderStatus>
        {
            public abstract class Values : ListField.ServiceOrderStatus { }
        }

        [PXDBString(1, IsFixed = true, BqlField = typeof(FSServiceOrder.status))]
        [PXUIField(DisplayName = "Status")]
        [serviceOrderStatus.Values.List]
        public virtual string ServiceOrderStatus { get; set; }
        #endregion
        #region CustWorkOrderRefNbr
        public abstract class custWorkOrderRefNbr : PX.Data.BQL.BqlString.Field<custWorkOrderRefNbr> { }

        [PXDBString(40, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC", BqlField = typeof(FSServiceOrder.custWorkOrderRefNbr))]
        [PXUIField(DisplayName = "Customer Work Order Ref. Nbr.")]
        public virtual string CustWorkOrderRefNbr { get; set; }
        #endregion
        #region CustPORefNbr
        public abstract class custPORefNbr : PX.Data.BQL.BqlString.Field<custPORefNbr> { }

        [PXDBString(40, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC", BqlField = typeof(FSServiceOrder.custPORefNbr))]
        [PXUIField(DisplayName = "Customer Purchase Order Ref. Nbr.")]
        public virtual string CustPORefNbr { get; set; }
        #endregion
        #region PostedBy
        public abstract class postedBy : PX.Data.BQL.BqlString.Field<postedBy> { }

        [PXDBString(2, IsFixed = true, BqlField = typeof(FSServiceOrder.postedBy))]
        public virtual string PostedBy { get; set; }
        #endregion

        #region BillingCycleCD
        public abstract class billingCycleCD : PX.Data.BQL.BqlString.Field<billingCycleCD> { }

        [PXDBString(15, IsUnicode = true, InputMask = ">AAAAAAAAAAAAAAA", BqlField = typeof(FSBillingCycle.billingCycleCD))]
        [PXUIField(DisplayName = "Billing Cycle ID", Visibility = PXUIVisibility.SelectorVisible)]
        [PXSelector(typeof(FSBillingCycle.billingCycleCD))]
        public virtual string BillingCycleCD { get; set; }
        #endregion
        #region BillingCycleType
        public abstract class billingCycleType : ListField_Billing_Cycle_Type
        {
        }

        [PXDBString(2, IsFixed = true, BqlField = typeof(FSBillingCycle.billingCycleType))]
        [billingCycleType.ListAtrribute]
        [PXDefault(ID.Billing_Cycle_Type.TIME_FRAME)]
        public virtual string BillingCycleType { get; set; }
        #endregion
        #region InvoiceOnlyCompletedServiceOrder
        public abstract class invoiceOnlyCompletedServiceOrder : PX.Data.BQL.BqlBool.Field<invoiceOnlyCompletedServiceOrder> { }

        [PXDBBool(BqlField = typeof(FSBillingCycle.invoiceOnlyCompletedServiceOrder))]
        [PXUIField(DisplayName = "Invoice only completed or closed Service Orders")]
        public virtual bool? InvoiceOnlyCompletedServiceOrder { get; set; }
        #endregion
        #region TimeCycleType
        public abstract class timeCycleType : ListField_Time_Cycle_Type
        {
        }

        [PXDBString(2, IsFixed = true, BqlField = typeof(FSBillingCycle.timeCycleType))]
        [PXDefault(ID.Time_Cycle_Type.DAY_OF_MONTH)]
        [timeCycleType.ListAtrribute]
        [PXUIField(DisplayName = "Time Cycle Type")]
        public virtual string TimeCycleType { get; set; }
        #endregion
        #region TimeCycleWeekDay
        public abstract class timeCycleWeekDay : ListField_WeekDaysNumber
        {
        }

        [PXDBInt(BqlField = typeof(FSBillingCycle.timeCycleWeekDay))]
        [PXUIField(DisplayName = "Day of Week", Visible = false)]
        [timeCycleWeekDay.ListAtrribute]
        public virtual int? TimeCycleWeekDay { get; set; }
        #endregion
        #region TimeCycleDayOfMonth
        public abstract class timeCycleDayOfMonth : PX.Data.BQL.BqlInt.Field<timeCycleDayOfMonth> { }

        [PXDBInt(BqlField = typeof(FSBillingCycle.timeCycleDayOfMonth))]
        [PXUIField(DisplayName = "Day of Month")]
        [PXIntList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 }, new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31" })]        
        public virtual int? TimeCycleDayOfMonth { get; set; }
        #endregion
        #region ProjectID
        public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

        [PXDBInt(BqlField = typeof(FSServiceOrder.projectID))]
        [PXUIField(DisplayName = "Project ID")]
        public override int? ProjectID { get; set; }
        #endregion

        #region DocType
        public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }

        [PXDBString(4, BqlField = typeof(FSSrvOrdType.srvOrdType))]
        public virtual string DocType { get; set; }
        #endregion

        #region TaxZoneID
        public new abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }

        [PXDBString(10, IsUnicode = true, BqlField = typeof(FSAppointment.taxZoneID))]
        [PXUIField(DisplayName = "Customer Tax Zone")]
        [PXSelector(typeof(TaxZone.taxZoneID), DescriptionField = typeof(TaxZone.descr), Filterable = true)]
        public override String TaxZoneID { get; set; }
        #endregion

        public new abstract class dfltProjectTaskID : PX.Data.BQL.BqlInt.Field<dfltProjectTaskID> { }
        public new abstract class wFStageID : PX.Data.BQL.BqlInt.Field<wFStageID> { }
        public new abstract class serviceContractID : PX.Data.BQL.BqlInt.Field<serviceContractID> { }
        public new abstract class scheduleID : PX.Data.BQL.BqlInt.Field<scheduleID> { }
        public new abstract class billServiceContractID : PX.Data.BQL.BqlInt.Field<billServiceContractID> { }
        public new abstract class routeID : PX.Data.BQL.BqlInt.Field<routeID> { }
        public new abstract class routeDocumentID : PX.Data.BQL.BqlInt.Field<routeDocumentID> { }
        public new abstract class vehicleID : PX.Data.BQL.BqlInt.Field<vehicleID> { }
        public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
        public new abstract class salesPersonID : PX.Data.BQL.BqlInt.Field<salesPersonID> { }

        #region RowIndex
        public abstract class rowIndex : PX.Data.BQL.BqlInt.Field<rowIndex> { }

        [PXInt]
        public virtual int? RowIndex { get; set; }
        #endregion
        #region GroupKey
        public abstract class groupKey : PX.Data.BQL.BqlString.Field<groupKey> { }

        [PXString]
        public virtual string GroupKey { get; set; }
        #endregion

        #region BatchID
        public abstract class batchID : PX.Data.BQL.BqlInt.Field<batchID> { }

        [PXInt]
        [PXUIField(DisplayName = "Batch Nbr.", Enabled = false)]
        [PXSelector(typeof(Search<FSPostBatch.batchID>), SubstituteKey = typeof(FSPostBatch.batchNbr))]
        public virtual int? BatchID { get; set; }
        #endregion
        #region ErrorFlag
        public abstract class errorFlag : PX.Data.BQL.BqlBool.Field<errorFlag> { }

        [PXBool]
        public virtual bool? ErrorFlag { get; set; }
        #endregion

        #region Unbound fields
        public string EntityType
        {
            get
            {
                return ID.PostDoc_EntityType.APPOINTMENT;
            }
        }
        #endregion
    }
}
