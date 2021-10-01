using PX.Data;
using PX.Objects.TX;
using PX.Objects.AR;
using System;
using PX.Objects.PM;
using PX.Objects.SO;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.FS
{
    #region PXProjection
    [Serializable]
    [PXProjection(typeof(
            Select2<FSServiceOrder,
                InnerJoin<FSSrvOrdType,
                    On<FSSrvOrdType.srvOrdType, Equal<FSServiceOrder.srvOrdType>>,
                InnerJoin<FSCustomerBillingSetup,
                    On<FSCustomerBillingSetup.cBID, Equal<FSServiceOrder.cBID>>,
                InnerJoin<FSBillingCycle,
                    On<FSBillingCycle.billingCycleID, Equal<FSCustomerBillingSetup.billingCycleID>>>>>,
                Where2<
                    Where<
                        FSServiceOrder.closed, Equal<True>,
                        Or<FSServiceOrder.completed, Equal<True>,
                            Or<
                            Where<FSBillingCycle.invoiceOnlyCompletedServiceOrder, Equal<False>,
                            And<FSServiceOrder.openDoc, Equal<True>>>>>>,
                                    And<
                        Where<FSServiceOrder.billServiceContractID, IsNull,
                        And<FSServiceOrder.billContractPeriodID, IsNull,
                        And<FSServiceOrder.allowInvoice, Equal<True>>>>>>>))]
    [PXGroupMask(typeof(InnerJoinSingleTable<Customer, On<Customer.bAccountID, Equal<ServiceOrderToPost.billCustomerID>, And<Match<Customer, Current<AccessInfo.userName>>>>>))]
    #endregion
    public class ServiceOrderToPost : FSServiceOrder, IPostLine
    {
        #region Keys
        public new class PK : PrimaryKeyOf<ServiceOrderToPost>.By<srvOrdType, refNbr>
        {
            public static ServiceOrderToPost Find(PXGraph graph, string srvOrdType, string refNbr) => FindBy(graph, srvOrdType, refNbr);
        }

        public new static class FK
        {
            public class Customer : AR.Customer.PK.ForeignKeyOf<ServiceOrderToPost>.By<customerID> { }
            public class CustomerLocation : CR.Location.PK.ForeignKeyOf<ServiceOrderToPost>.By<customerID, locationID> { }
            public class BillCustomer : AR.Customer.PK.ForeignKeyOf<ServiceOrderToPost>.By<billCustomerID> { }
            public class BillCustomerLocation : CR.Location.PK.ForeignKeyOf<ServiceOrderToPost>.By<billCustomerID, billLocationID> { }
            public class Branch : GL.Branch.PK.ForeignKeyOf<ServiceOrderToPost>.By<branchID> { }
            public class ServiceOrderType : FSSrvOrdType.PK.ForeignKeyOf<ServiceOrderToPost>.By<srvOrdType> { }
            public class Address : FSAddress.PK.ForeignKeyOf<ServiceOrderToPost>.By<serviceOrderAddressID> { }
            public class Contact : FSContact.PK.ForeignKeyOf<ServiceOrderToPost>.By<serviceOrderContactID> { }
            public class Contract : CT.Contract.PK.ForeignKeyOf<ServiceOrderToPost>.By<contractID> { }
            public class BranchLocation : FSBranchLocation.PK.ForeignKeyOf<ServiceOrderToPost>.By<branchLocationID> { }
            public class Project : PMProject.PK.ForeignKeyOf<ServiceOrderToPost>.By<projectID> { }
            public class Task : PMTask.PK.ForeignKeyOf<ServiceOrderToPost>.By<dfltProjectTaskID> { }
            public class WorkFlowStage : FSWFStage.PK.ForeignKeyOf<ServiceOrderToPost>.By<wFStageID> { }
            public class ServiceContract : FSServiceContract.PK.ForeignKeyOf<ServiceOrderToPost>.By<serviceContractID> { }
            public class Schedule : FSSchedule.PK.ForeignKeyOf<ServiceOrderToPost>.By<scheduleID> { }
            public class BillServiceContract : FSServiceContract.PK.ForeignKeyOf<ServiceOrderToPost>.By<billServiceContractID> { }
            public class CustomerBillingSetup : FSCustomerBillingSetup.PK.ForeignKeyOf<ServiceOrderToPost>.By<cBID> { }
            public class TaxZone : Objects.TX.TaxZone.PK.ForeignKeyOf<ServiceOrderToPost>.By<taxZoneID> { }
            public class Problem : FSProblem.UK.ForeignKeyOf<ServiceOrderToPost>.By<problemID> { }
            public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<ServiceOrderToPost>.By<curyInfoID> { }
            public class Currency : CM.Currency.PK.ForeignKeyOf<ServiceOrderToPost>.By<curyID> { }
            public class PostSOOrder : SOOrderType.PK.ForeignKeyOf<ServiceOrderToPost>.By<postOrderType> { }
            public class PostOrderNegativeBalance : SOOrderType.PK.ForeignKeyOf<ServiceOrderToPost>.By<postOrderTypeNegativeBalance> { }
            public class DfltTermIDARSO : CS.Terms.PK.ForeignKeyOf<ServiceOrderToPost>.By<dfltTermIDARSO> { }
            public class BillingCycle : FSBillingCycle.PK.ForeignKeyOf<ServiceOrderToPost>.By<billingCycleID> { }

        }
        #endregion

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
        #region DocType
        public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }

        [PXDBString(4, BqlField = typeof(FSSrvOrdType.srvOrdType))]
        public virtual string DocType { get; set; }
        #endregion

        #region TaxZoneID
        public new abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }

        [PXDBString(10, IsUnicode = true, BqlField = typeof(FSServiceOrder.taxZoneID))]
        [PXUIField(DisplayName = "Customer Tax Zone")]
        [PXSelector(typeof(TaxZone.taxZoneID), DescriptionField = typeof(TaxZone.descr), Filterable = true)]
        public override String TaxZoneID { get; set; }
        #endregion

        #region AppointmentID
        public abstract class appointmentID : PX.Data.BQL.BqlInt.Field<appointmentID> { }

        [PXInt]
        public virtual int? AppointmentID { get; set; }
        #endregion

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
                return ID.PostDoc_EntityType.SERVICE_ORDER;
            }
        }
        #endregion

        public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
        public new abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
        public new abstract class billCustomerID : PX.Data.BQL.BqlInt.Field<billCustomerID> { }
        public new abstract class billLocationID : PX.Data.BQL.BqlInt.Field<billLocationID> { }
        public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        public new abstract class srvOrdType : PX.Data.BQL.BqlString.Field<srvOrdType> { }
        public new abstract class serviceOrderAddressID : PX.Data.IBqlField { }
        public new abstract class serviceOrderContactID : PX.Data.IBqlField { }
        public new abstract class contractID : PX.Data.BQL.BqlInt.Field<contractID> { }
        public new abstract class branchLocationID : PX.Data.BQL.BqlInt.Field<branchLocationID> { }
        public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
        public new abstract class dfltProjectTaskID : PX.Data.BQL.BqlInt.Field<dfltProjectTaskID> { }
        public new abstract class wFStageID : PX.Data.BQL.BqlInt.Field<wFStageID> { }
        public new abstract class serviceContractID : PX.Data.BQL.BqlInt.Field<serviceContractID> { }
        public new abstract class scheduleID : PX.Data.BQL.BqlInt.Field<scheduleID> { }
        public new abstract class billServiceContractID : PX.Data.BQL.BqlInt.Field<billServiceContractID> { }
        public new abstract class cBID : PX.Data.BQL.BqlInt.Field<cBID> { }
        public new abstract class problemID : PX.Data.BQL.BqlInt.Field<problemID> { }
        public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
        public new abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

    }
}
