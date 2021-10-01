using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using System;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.PM;
using PX.Objects.AR;

namespace PX.Objects.FS
{
    [Serializable]
    [PXPrimaryGraph(typeof(AppointmentEntry))]
    [PXProjection(typeof(
        Select2<FSAppointment,
                InnerJoin<FSServiceOrder,
                    On<FSServiceOrder.sOID, Equal<FSAppointment.sOID>>,
                InnerJoin<FSAddress,
                    On<FSAddress.addressID, Equal<FSServiceOrder.serviceOrderAddressID>>,
                LeftJoin<FSServiceContract, 
                    On<FSAppointment.serviceContractID, Equal<FSServiceContract.serviceContractID>>,
                CrossJoinSingleTable<FSSetup>>>>>))]
    public partial class FSAppointmentInRoute : FSAppointment
    {
        #region Keys
        public new class PK : PrimaryKeyOf<FSAppointmentInRoute>.By<srvOrdType, refNbr>
        {
            public static FSAppointmentInRoute Find(PXGraph graph, string srvOrdType, string refNbr) => FindBy(graph, srvOrdType, refNbr);
        }

        public new class UK : PrimaryKeyOf<FSAppointmentInRoute>.By<appointmentID>
        {
            public static FSAppointmentInRoute Find(PXGraph graph, int? appointmentID) => FindBy(graph, appointmentID);
        }
        public new static class FK
        {
            public class Customer : AR.Customer.PK.ForeignKeyOf<FSAppointmentInRoute>.By<customerID> { }
            public class Branch : GL.Branch.PK.ForeignKeyOf<FSAppointmentInRoute>.By<branchID> { }
            public class ServiceOrderType : FSSrvOrdType.PK.ForeignKeyOf<FSAppointmentInRoute>.By<srvOrdType> { }
            public class ServiceOrder : FSServiceOrder.PK.ForeignKeyOf<FSAppointmentInRoute>.By<srvOrdType, soRefNbr> { }
            public class Project : PMProject.PK.ForeignKeyOf<FSAppointmentInRoute>.By<projectID> { }
            public class Task : PMTask.PK.ForeignKeyOf<FSAppointmentInRoute>.By<dfltProjectTaskID> { }
            public class WorkFlowStage : FSWFStage.PK.ForeignKeyOf<FSAppointmentInRoute>.By<wFStageID> { }
            public class ServiceContract : FSServiceContract.PK.ForeignKeyOf<FSAppointmentInRoute>.By<serviceContractID> { }
            public class Schedule : FSSchedule.PK.ForeignKeyOf<FSAppointmentInRoute>.By<scheduleID> { }
            public class BillServiceContract : FSServiceContract.PK.ForeignKeyOf<FSAppointmentInRoute>.By<billServiceContractID> { }
            public class TaxZone : Objects.TX.TaxZone.PK.ForeignKeyOf<FSAppointmentInRoute>.By<taxZoneID> { }
            public class Route : FSRoute.PK.ForeignKeyOf<FSAppointmentInRoute>.By<routeID> { }
            public class RouteDocument : FSRouteDocument.PK.ForeignKeyOf<FSAppointmentInRoute>.By<routeDocumentID> { }
            public class Vehicle : FSVehicle.PK.ForeignKeyOf<FSAppointmentInRoute>.By<vehicleID> { }
            public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<FSAppointmentInRoute>.By<curyInfoID> { }
            public class Currency : CM.Currency.PK.ForeignKeyOf<FSAppointmentInRoute>.By<curyID> { }
            public class SalesPerson : AR.SalesPerson.PK.ForeignKeyOf<FSAppointmentInRoute>.By<salesPersonID> { }
            public class BillCustomer : AR.Customer.PK.ForeignKeyOf<FSAppointmentInRoute>.By<billCustomerID> { }
            public class CustomerLocation : Location.PK.ForeignKeyOf<FSAppointmentInRoute>.By<customerID, locationID> { }
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

        #region CustomerID
        public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

        [PXDBInt(BqlField = typeof(FSServiceOrder.customerID))]
        [PXUIField(DisplayName = "Customer ID")]
        [FSSelectorCustomer]
        public override int? CustomerID { get; set; }
        #endregion

        #region CustomerContractNbr
        public abstract class customerContractNbr : PX.Data.IBqlField
        {
        }

        [PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(FSServiceContract.customerContractNbr))]
        [PXUIField(DisplayName = "Customer Contract Nbr.")]
        public virtual string CustomerContractNbr { get; set; }
        #endregion

        #region LocationID
        public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }

        [LocationID(BqlField = typeof(FSServiceOrder.locationID), DisplayName = "Location ID", DescriptionField = typeof(Location.descr))]
        public virtual int? LocationID { get; set; }
        #endregion

        #region State
        public abstract class state : PX.Data.BQL.BqlString.Field<state> { }

        [PXDBString(50, IsUnicode = true, BqlField = typeof(FSAddress.state))]
        [PXUIField(DisplayName = "State")]
        [State(typeof(FSAddress.countryID), DescriptionField = typeof(State.name))]
        public virtual string State { get; set; }
        #endregion

        #region AddressLine1
        public abstract class addressLine1 : PX.Data.BQL.BqlString.Field<addressLine1> { }

        [PXDBString(50, IsUnicode = true, BqlField = typeof(FSAddress.addressLine1))]
        [PXUIField(DisplayName = "Address Line 1")]
        public virtual string AddressLine1 { get; set; }
        #endregion

        #region AddressLine2
        public abstract class addressLine2 : PX.Data.BQL.BqlString.Field<addressLine2> { }

        [PXDBString(50, IsUnicode = true, BqlField = typeof(FSAddress.addressLine2))]
        [PXUIField(DisplayName = "Address Line 2")]
        public virtual string AddressLine2 { get; set; }
        #endregion

        #region PostalCode
        public abstract class postalCode : PX.Data.BQL.BqlString.Field<postalCode> { }

        [PXDBString(20, BqlField = typeof(FSAddress.postalCode))]
        [PXUIField(DisplayName = "Postal code")]
        [PXZipValidation(typeof(Country.zipCodeRegexp), typeof(Country.zipCodeMask), typeof(FSAddress.countryID))]
        [PXDynamicMask(typeof(Search<Country.zipCodeMask, Where<Country.countryID, Equal<Current<FSAddress.countryID>>>>))]
        [PXFormula(typeof(Default<FSAddress.countryID>))]
        public virtual string PostalCode { get; set; }
        #endregion

        #region City
        public abstract class city : PX.Data.BQL.BqlString.Field<city> { }

        [PXDBString(50, IsUnicode = true, BqlField = typeof(FSAddress.city))]
        [PXUIField(DisplayName = "City")]
        public virtual string City { get; set; }
        #endregion

        #region MapApiKey
        public abstract class mapApiKey : PX.Data.BQL.BqlString.Field<mapApiKey> { }

        [PXDBString(255, IsUnicode = true, BqlField = typeof(FSSetup.mapApiKey))]
        [PXUIField(DisplayName = "Map API Key")]
        public virtual string MapApiKey { get; set; }
        #endregion

        #region ServiceContractID
        public new abstract class serviceContractID : PX.Data.IBqlField
        {
        }

        [PXDBInt(BqlField = typeof(FSServiceOrder.serviceContractID))]
        [PXSelector(typeof(Search<FSServiceContract.serviceContractID,
                           Where<
                                FSServiceContract.customerID, Equal<Current<FSAppointmentInRoute.customerID>>>>),
                           SubstituteKey = typeof(FSServiceContract.refNbr))]
        [PXUIField(DisplayName = "Source Service Contract ID", Enabled = false, FieldClass = "FSCONTRACT")]
        public override int? ServiceContractID { get; set; }
        #endregion

        public new abstract class sOID : PX.Data.BQL.BqlInt.Field<sOID> { }
        public new abstract class scheduleID : PX.Data.BQL.BqlInt.Field<scheduleID> { }
        public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        public new abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }
        public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
        public new abstract class dfltProjectTaskID : PX.Data.BQL.BqlInt.Field<dfltProjectTaskID> { }
        public new abstract class wFStageID : PX.Data.BQL.BqlInt.Field<wFStageID> { }
        public new abstract class routeID : PX.Data.BQL.BqlInt.Field<routeID> { }
        public new abstract class routeDocumentID : PX.Data.BQL.BqlInt.Field<routeDocumentID> { }
        public new abstract class vehicleID : PX.Data.BQL.BqlInt.Field<vehicleID> { }
        public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
        public new abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
        public new abstract class salesPersonID : PX.Data.BQL.BqlInt.Field<salesPersonID> { }
        public new abstract class billCustomerID : PX.Data.BQL.BqlInt.Field<billCustomerID> { }
        public new abstract class billServiceContractID : PX.Data.BQL.BqlInt.Field<billServiceContractID> { }
        public new abstract class notStarted : PX.Data.BQL.BqlBool.Field<notStarted> { }
        public new abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }
        public new abstract class awaiting : PX.Data.BQL.BqlBool.Field<awaiting> { }
        public new abstract class inProcess : PX.Data.BQL.BqlBool.Field<inProcess> { }
        public new abstract class paused : PX.Data.BQL.BqlBool.Field<paused> { }
        public new abstract class completed : PX.Data.BQL.BqlBool.Field<completed> { }
        public new abstract class closed : PX.Data.BQL.BqlBool.Field<closed> { }
        public new abstract class canceled : PX.Data.BQL.BqlBool.Field<canceled> { }
        public new abstract class billed : PX.Data.BQL.BqlBool.Field<billed> { }
        public new abstract class generatedByContract : PX.Data.BQL.BqlBool.Field<generatedByContract> { }
    }
}