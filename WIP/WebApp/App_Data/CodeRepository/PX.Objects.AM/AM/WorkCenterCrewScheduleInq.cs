using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AM.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AM
{
    /// <summary>
    /// Manufacturing - Work Center Crew Schedule - Inquiry
    /// Screen ID = AM405100, Enabled with APS Only. 
    /// </summary>
    [PX.Objects.GL.TableAndChartDashboardType]
    public class WorkCenterCrewScheduleInq : PXGraph<WorkCenterCrewScheduleInq>
    {
        public PXCancel<WorkCenterCrewScheduleFilter> Cancel;
        public PXFilter<WorkCenterCrewScheduleFilter> Filter;
        public PXSelect<WorkCenterCrewScheduleDetail> ScheduleDetail;

        public PXSetup<AMPSetup> ProductionSetup;

        public WorkCenterCrewScheduleInq()
        {
            ScheduleDetail.AllowInsert =
                ScheduleDetail.AllowUpdate =
                    ScheduleDetail.AllowDelete = false;
        }

        protected virtual int BlockSize
        {
            get
            {
                if (ProductionSetup.Current == null)
                {
                    ProductionSetup.Current = ProductionSetup.Select();
                }

                return ProductionSetup.Current?.SchdBlockSize ?? 0;
            }
        }

        protected virtual void _(Events.FieldDefaulting<WorkCenterCrewScheduleFilter.toDate> e)
        {
            e.NewValue = Accessinfo.BusinessDate.GetValueOrDefault().AddDays(30);
        }

        protected virtual IEnumerable<WorkCenterCrewScheduleDetail> GetDetail()
        {
            var dateShiftSchedules = new List<AMWCSchdDetailSchdOper>();
            AMShiftMst dateShiftScheduleShiftMst = null;
            foreach (PXResult<AMWCSchdDetailSchdOper, AMShiftMst> result in SelectFrom<AMWCSchdDetailSchdOper>
                .InnerJoin<AMShiftMst>
                .On<AMWCSchdDetailSchdOper.shiftID.IsEqual<AMShiftMst.shiftID>>
                .InnerJoin<AMWC>
                .On<AMWCSchdDetailSchdOper.wcID.IsEqual<AMWC.wcID>>
                .Where<AMShiftMst.crewSize.IsGreater<Zero>
                .And<AMWCSchdDetailSchdOper.schdBlocks.IsGreater<Zero>>
                .And<AMWC.wcBasis.IsEqual<AMWC.BasisForCapacity.crewSize>>
                .And<Brackets<AMWCSchdDetailSchdOper.shiftID.IsEqual<WorkCenterCrewScheduleFilter.shiftID.FromCurrent>
                    .Or<WorkCenterCrewScheduleFilter.shiftID.FromCurrent.IsNull>>>
                .And<Brackets<AMWCSchdDetailSchdOper.schdDate.IsGreaterEqual<WorkCenterCrewScheduleFilter.fromDate.FromCurrent>
                    .Or<WorkCenterCrewScheduleFilter.fromDate.FromCurrent.IsNull>>
                .And<Brackets<AMWCSchdDetailSchdOper.schdDate.IsLessEqual<WorkCenterCrewScheduleFilter.toDate.FromCurrent>
                    .Or<WorkCenterCrewScheduleFilter.toDate.FromCurrent.IsNull>>
                    >>>
                .OrderBy<AMWCSchdDetailSchdOper.schdDate.Asc, AMWCSchdDetailSchdOper.shiftID.Asc, AMWCSchdDetailSchdOper.startTime.Asc>
                .View.Select(this))
            {
                var schdDetail = (AMWCSchdDetailSchdOper)result;
                var shiftMst = (AMShiftMst)result;

                if (schdDetail?.WcID == null || schdDetail.ResourceSize == 0 || schdDetail.IsBreak == true)
                {
                    continue;
                }

                if (dateShiftSchedules.Count == 0 || IsSameDateShift(schdDetail, dateShiftSchedules[0]))
                {
                    dateShiftScheduleShiftMst = shiftMst;
                    dateShiftSchedules.Add(schdDetail);
                    continue;
                }

                foreach (var newRow in ProcessSameDateSchd(dateShiftSchedules, dateShiftScheduleShiftMst, Filter?.Current))
                {
                    yield return newRow;
                }

                dateShiftSchedules.Clear();
                dateShiftSchedules.Add(schdDetail);
                dateShiftScheduleShiftMst = shiftMst;
            }

            // last matching rows
            foreach (var newRow in ProcessSameDateSchd(dateShiftSchedules, dateShiftScheduleShiftMst, Filter?.Current))
            {
                yield return newRow;
            }
        }

        protected virtual IEnumerable scheduleDetail()
        {
            return GetDetail();
        }

        public static bool IsSameDateShift(AMWCSchdDetailSchdOper schd1, AMWCSchdDetailSchdOper schd2)
        {
            return schd1?.SchdDate != null && schd2?.SchdDate != null && Common.Dates.DatesEqual(schd1.SchdDate, schd2.SchdDate) && schd1.ShiftID == schd2.ShiftID;
        }

        protected virtual IEnumerable<WorkCenterCrewScheduleDetail> ProcessSameDateSchd(IEnumerable<AMWCSchdDetailSchdOper> schdDetail, AMShiftMst shiftMst, WorkCenterCrewScheduleFilter filter)
        {
            foreach (var newRow in ProcessSameDateSchd(schdDetail, shiftMst))
            {
                if (newRow.SchdDate == null ||
                    // cannot filter Workcenter in bql because total numbers for date will be wrong (and need to include all WC)
                    (filter?.WcID != null && newRow.WcID != filter.WcID))
                {
                    continue;
                }

                yield return newRow;
            }
        }

        protected virtual IEnumerable<WorkCenterCrewScheduleDetail> ProcessSameDateSchd(IEnumerable<AMWCSchdDetailSchdOper> schdDetail, AMShiftMst shiftMst)
        {
            if (schdDetail == null)
            {
                yield break;
            }

            var schdDetails = schdDetail.ToList();
            if(schdDetails.Count == 0)
            {
                yield break;
            }

            var shiftCrewSize = shiftMst?.CrewSize ?? 1m;
            if (schdDetails.Count == 1)
            {
                var newSingle = Convert(schdDetails[0], shiftCrewSize);
                if(newSingle.CrewSizeMax > shiftCrewSize)
                {
                    yield return newSingle;
                }

                yield break;
            }

            var minStartTime = schdDetails.Min(r => r.StartTime);
            var maxStartTime = schdDetails.Max(r => r.EndTime);
            var timeBlocks = SetBlocksFromSchd(CreateBlockTimes(minStartTime.GetValueOrDefault(), maxStartTime.GetValueOrDefault()), schdDetails).ToList();

            foreach (var schd in schdDetails)
            {
                if (schd?.SchdDate == null)
                {
                    continue;
                }

                var crewSizeMax = timeBlocks
                    .Where(r => r.StartTime.GreaterThanOrEqualTo(schd.StartTime)
                        && r.EndTime.LessThanOrEqualTo(schd.EndTime))
                    .Max(r => r.ResourceSize) ?? 0;

                if (Filter?.Current?.ShowAll != true && crewSizeMax <= shiftCrewSize)
                {
                    continue;
                }

                var newRow = Convert(schd, shiftCrewSize);
                newRow.CrewSizeMax = crewSizeMax;
                yield return newRow;
            }
        }

        private IEnumerable<AMWCSchdDetailSchdOper> CreateBlockTimes(DateTime startDate, DateTime endDate)
        {
            var retList = new List<AMWCSchdDetailSchdOper>();
            var totalBlocks = ProductionScheduleEngine.DateTimesToBlocks(startDate, endDate, BlockSize, true);
            if (totalBlocks == 0)
            {
                return retList;
            }

            for (var i = 0; i < totalBlocks; i++)
            {
                var addMin = BlockSize * i;
                var start = startDate.AddMinutes(addMin);
                retList.Add(new AMWCSchdDetailSchdOper
                {
                    StartTime = start,
                    EndTime = start.AddMinutes(BlockSize)
                });
            }

            return retList;
        }

        protected virtual IEnumerable<AMWCSchdDetailSchdOper> SetBlocksFromSchd(IEnumerable<AMWCSchdDetailSchdOper> blocks, IEnumerable<AMWCSchdDetailSchdOper> schdDetail)
        {
            foreach (var timeBlock in blocks)
            {
                timeBlock.ResourceSize = schdDetail
                    .Where(r => r.StartTime.LessThanOrEqualTo(timeBlock.StartTime)
                        && r.EndTime.GreaterThanOrEqualTo(timeBlock.EndTime))
                    .Sum(r => r.ResourceSize);

                yield return timeBlock;
            }
        }

        protected virtual WorkCenterCrewScheduleDetail Convert(AMWCSchdDetailSchdOper schdDetail, decimal shiftCrewSize)
        {
            if (schdDetail == null)
            {
                return null;
            }

            return new WorkCenterCrewScheduleDetail
            {
                WcID = schdDetail.WcID,
                ShiftID = schdDetail.ShiftID,
                SchdBlocks = schdDetail.SchdBlocks,
                SchdDate = schdDetail.SchdDate,
                StartTime = schdDetail.StartTime,
                EndTime = schdDetail.EndTime,
                CrewSize = schdDetail.ResourceSize,
                ShiftCrewSize = shiftCrewSize,
                OrderType = schdDetail.OrderType,
                ProdOrdID = schdDetail.ProdOrdID,
                OperationID = schdDetail.OperationID
            };
        }

        public PXAction<WorkCenterCrewScheduleFilter> ViewSchedule;
        [PXUIField(DisplayName = "View Schedule")]
        [PXButton]
        protected virtual void viewSchedule()
        {
            ViewScheduleRedirect(new GIWorkCenterSchedule(), ScheduleDetail?.Current);
        }

        protected virtual void ViewScheduleRedirect(GIWorkCenterSchedule gi, WorkCenterCrewScheduleDetail schdDetail)
        {
            if (gi == null || schdDetail?.WcID == null)
            {
                return;
            }

            if (this.Filter?.Current?.WcID != null)
            {
                gi.SetParameter(GIWorkCenterSchedule.Parameters.WorkCenter, schdDetail.WcID);
            }

            gi.SetParameter(GIWorkCenterSchedule.Parameters.DateFrom, schdDetail.SchdDate, this);
            gi.SetParameter(GIWorkCenterSchedule.Parameters.DateTo, schdDetail.SchdDate, this);
            gi.CallGenericInquiry(PXBaseRedirectException.WindowMode.New);
        }

        #region Internals
        [Serializable]
        [PXCacheName("Work Center Crew Schedule Filter")]
        public class WorkCenterCrewScheduleFilter : IBqlTable
        {
            #region WcID

            public abstract class wcID : PX.Data.BQL.BqlString.Field<wcID> { }

            [WorkCenterIDField]
            [PXSelector(typeof(Search<AMWC.wcID>))]
            [PXRestrictor(typeof(Where<AMWC.wcBasis, Equal<AMWC.BasisForCapacity.crewSize>>), Messages.CrewWorkcenterRequired)]
            public virtual string WcID { get; set; }

            #endregion
            #region ShiftID
            public abstract class shiftID : PX.Data.BQL.BqlString.Field<shiftID> { }

            [PXDBString(4, InputMask = "####")]
            [PXUIField(DisplayName = "Shift")]
            // Not limiting shift by work center in case users want to see shift for all work centers
            [PXSelector(typeof(Search<AMShiftMst.shiftID>))]
            public virtual String ShiftID { get; set; }

            #endregion
            #region FromDate
            public abstract class fromDate : PX.Data.BQL.BqlDateTime.Field<fromDate> { }

            [PXDate]
            [PXUnboundDefault(typeof(AccessInfo.businessDate))]
            [PXUIField(DisplayName = "From Date")]
            public virtual DateTime? FromDate { get; set; }

            #endregion
            #region ToDate
            public abstract class toDate : PX.Data.BQL.BqlDateTime.Field<toDate> { }

            [PXDate]
            [PXUIField(DisplayName = "To Date")]
            public virtual DateTime? ToDate { get; set; }

            #endregion
            #region ShowAll
            public abstract class showAll : PX.Data.BQL.BqlBool.Field<showAll> { }
            [PXDBBool]
            [PXDefault(false)]
            [PXUIField(DisplayName = "Show All")]
            public virtual bool? ShowAll { get; set; }
            #endregion
        }

        /// <summary>
        /// Detail DAC for Work Center Crew Schedule screen (Not a DB Table)
        /// </summary>
        [Serializable]
        [PXCacheName("Work Center Crew Schedule Detail")]
        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        public class WorkCenterCrewScheduleDetail : IBqlTable
        {
            internal string DebuggerDisplay => $"SchdDate = {SchdDate?.ToShortDateString()}; {StartTime.GetValueOrDefault().ToShortTimeString()} - {EndTime.GetValueOrDefault().ToShortTimeString()}; CrewSize = {CrewSizeMax}; CrewSizeShortage = {CrewSizeShortage}";

            #region SchdDate (key)

            public abstract class schdDate : PX.Data.BQL.BqlDateTime.Field<schdDate> { }

            [PXDBDate(IsKey = true)]
            [PXDefault]
            [PXUIField(DisplayName = "Schedule Date", Enabled = false)]
            public virtual DateTime? SchdDate { get; set; }

            #endregion
            #region StartTime (key)

            public abstract class startTime : PX.Data.BQL.BqlDateTime.Field<startTime> { }

            [PXDBTime(DisplayMask = "t", UseTimeZone = false, IsKey = true)]
            [PXUIField(DisplayName = "Start Time", Enabled = false)]
            [PXDefault]
            public virtual DateTime? StartTime { get; set; }

            #endregion
            #region ShiftID (key)
            public abstract class shiftID : PX.Data.BQL.BqlString.Field<shiftID> { }

            [PXDBString(4, InputMask = "####", IsKey = true)]
            [PXUIField(DisplayName = "Shift")]
            [PXSelector(typeof(Search<AMShiftMst.shiftID>), ValidateValue = false)]
            public virtual String ShiftID { get; set; }

            #endregion
            #region WcID (key)

            public abstract class wcID : PX.Data.BQL.BqlString.Field<wcID> { }

            [WorkCenterIDField(IsKey = true)]
            [PXSelector(typeof(Search<AMWC.wcID>), ValidateValue = false)]
            public virtual string WcID { get; set; }

            #endregion
            #region EndTime

            public abstract class endTime : PX.Data.BQL.BqlDateTime.Field<endTime> { }

            [PXDBTime(DisplayMask = "t", UseTimeZone = false)]
            [PXUIField(DisplayName = "End Time", Enabled = false)]
            [PXDefault]
            public virtual DateTime? EndTime { get; set; }

            #endregion
            #region SchdBlocks
            public abstract class schdBlocks : PX.Data.BQL.BqlInt.Field<schdBlocks> { }

            protected int? _SchdBlocks;
            [PXDBInt]
            [PXUIField(DisplayName = "Scheduled Blocks", Enabled = false)]
            public virtual int? SchdBlocks { get; set; }
            #endregion
            #region CrewSize
            public abstract class crewSize : PX.Data.BQL.BqlDecimal.Field<crewSize> { }

            [PXDBDecimal(6)]
            [PXDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Crew Size")]
            public virtual Decimal? CrewSize { get; set; }
            #endregion
            #region ShiftCrewSize
            public abstract class shiftCrewSize : PX.Data.BQL.BqlDecimal.Field<shiftCrewSize> { }

            [PXDBDecimal(6)]
            [PXDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Shift Crew Size")]
            public virtual Decimal? ShiftCrewSize { get; set; }
            #endregion
            #region CrewSizeMax
            public abstract class crewSizeMax : PX.Data.BQL.BqlDecimal.Field<crewSizeMax> { }

            [PXDBDecimal(6)]
            [PXDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Max. Crew Size")]
            public virtual Decimal? CrewSizeMax { get; set; }
            #endregion
            #region CrewSizeShortage
            public abstract class crewSizeShortage : PX.Data.BQL.BqlDecimal.Field<crewSizeShortage> { }

            [PXDBDecimal(6)]
            [PXDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Crew Size Shortage")]
            public virtual Decimal? CrewSizeShortage => ShiftCrewSize.GetValueOrDefault() - CrewSizeMax.GetValueOrDefault();
            #endregion
            #region OrderType
            public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

            [AMOrderTypeField]
            [AMOrderTypeSelector(ValidateValue = false)]
            public virtual String OrderType { get; set; }
            #endregion
            #region ProdOrdID
            public abstract class prodOrdID : PX.Data.BQL.BqlString.Field<prodOrdID> { }

            [ProductionNbr]
            [ProductionOrderSelector(typeof(orderType), true, ValidateValue = false)]
            public virtual String ProdOrdID { get; set; }
            #endregion
            #region OperationID
            public abstract class operationID : PX.Data.BQL.BqlInt.Field<operationID> { }

            [OperationIDField]
            [PXSelector(typeof(Search<AMProdOper.operationID,
                    Where<AMProdOper.orderType, Equal<Current<AMMTran.orderType>>,
                        And<AMProdOper.prodOrdID, Equal<Current<AMMTran.prodOrdID>>>>>),
                SubstituteKey = typeof(AMProdOper.operationCD),
                ValidateValue = false)]
            public virtual int? OperationID { get; set; }
            #endregion
        }
        #endregion
    }
}