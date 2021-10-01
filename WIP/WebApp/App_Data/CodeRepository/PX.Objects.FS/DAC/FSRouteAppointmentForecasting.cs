using System;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.FS
{
    [Serializable]
    [PXPrimaryGraph(typeof(RouteAppointmentForecastingInq))]
    public class FSRouteAppointmentForecasting : IBqlTable
    {
        #region Keys
        public class PK : PrimaryKeyOf<FSRouteAppointmentForecasting>.By<scheduleID, startDate>
        {
            public static FSRouteAppointmentForecasting Find(PXGraph graph, int? scheduleID, DateTime? startDate) => FindBy(graph, scheduleID, startDate);
        }

        public static class FK
        {
            public class Schedule : FSSchedule.PK.ForeignKeyOf<FSRouteAppointmentForecasting>.By<scheduleID> { }
            public class Customer : AR.Customer.PK.ForeignKeyOf<FSRouteAppointmentForecasting>.By<customerID> { }
            public class CustomerLocation : Location.PK.ForeignKeyOf<FSRouteAppointmentForecasting>.By<customerID, customerLocationID> { }
            public class Route : FSRoute.PK.ForeignKeyOf<FSRouteAppointmentForecasting>.By<routeID> { }
            public class ServiceContract : FSServiceContract.PK.ForeignKeyOf<FSRouteAppointmentForecasting>.By<serviceContractID> { }
        }

        #endregion

        #region ScheduleID
        public abstract class scheduleID : PX.Data.BQL.BqlInt.Field<scheduleID> { }

        [PXDBInt(IsKey = true)]
        [PXSelector(typeof(Search<FSSchedule.scheduleID,
                           Where<
                                FSSchedule.entityType, Equal<ListField_Schedule_EntityType.Contract>,
                                And<FSSchedule.entityID, Equal<Current<FSRouteAppointmentForecasting.serviceContractID>>>>>))]
        [PXUIField(Enabled = false)]
        public virtual int? ScheduleID { get; set; }
        #endregion
        #region StartDate
        public abstract class startDate : PX.Data.BQL.BqlDateTime.Field<startDate> { }

        [PXDBDate(IsKey = true)]
        [PXUIField(DisplayName = "Start Date", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual DateTime? StartDate { get; set; }
        #endregion
        #region RouteID
        public abstract class routeID : PX.Data.BQL.BqlInt.Field<routeID> { }

        [PXDBInt]
        [FSSelectorRouteID]
        [PXUIField(DisplayName = "Route ID")]
        public virtual int? RouteID { get; set; }

        #endregion
        #region CustomerID
        public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Customer ID", Visibility = PXUIVisibility.SelectorVisible)]
        [FSSelectorCustomer]
        public virtual int? CustomerID { get; set; }
        #endregion
        #region CustomerLocationID
        public abstract class customerLocationID : PX.Data.BQL.BqlInt.Field<customerLocationID> { }

        [LocationID(DisplayName = "Location ID", DescriptionField = typeof(Location.descr))]
        public virtual int? CustomerLocationID { get; set; }
        #endregion
        #region ServiceContractID
        public abstract class serviceContractID : PX.Data.BQL.BqlInt.Field<serviceContractID> { }

        [PXDBInt]
        [PXSelector(typeof(Search<FSServiceContract.serviceContractID,
                           Where<
                                FSServiceContract.customerID, Equal<Current<FSRouteAppointmentForecasting.customerID>>>>),
                           SubstituteKey = typeof(FSServiceContract.refNbr))]
        [PXUIField(DisplayName = "Service Contract ID", Enabled = false)]
        public virtual int? ServiceContractID { get; set; }
        #endregion
        #region SequenceOrder
        public abstract class sequenceOrder : PX.Data.BQL.BqlInt.Field<sequenceOrder> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Sequence Order")]
        public virtual int? SequenceOrder { get; set; }

        #endregion
    }
}