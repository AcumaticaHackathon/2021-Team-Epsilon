using System;
using System.Collections;
using PX.Data;
using System.Collections.Generic;
using PX.Common;
using PX.Objects.AM.Attributes;
using System.Linq;

namespace PX.Objects.AM
{
    /// <summary>
    /// Approve Clock Entires (AM516000)
    /// </summary>
    public class ClockApprovalProcess : PXGraph<ClockApprovalProcess>
    {
        public PXCancel<AMClockTran> Cancel;
        public PXSave<AMClockTran> Save;
        [PXFilterable]
        public PXProcessing<AMClockTran,
            Where2<Where<AMClockTran.employeeID, Equal<Current<ClockTranFilter.employeeID>>, Or<Current<ClockTranFilter.employeeID>, IsNull>>,
                And2<Where<AMClockTran.orderType, Equal<Current<ClockTranFilter.orderType>>, Or<Current<ClockTranFilter.orderType>, IsNull>>,
                    And2<Where<AMClockTran.prodOrdID, Equal<Current<ClockTranFilter.prodOrdID>>, Or<Current<ClockTranFilter.prodOrdID>, IsNull>>,
                        And<Where<AMClockTran.released, Equal<True>, And<AMClockTran.closeflg, Equal<False>>>>>>>> UnapprovedTrans;
        public PXSelect<AMClockTranSplit, Where<AMClockTranSplit.employeeID, Equal<Current<AMClockTran.employeeID>>,
            And<AMClockTranSplit.lineNbr, Equal<Current<AMClockTran.lineNbr>>>>> splits;
        public PXFilter<ClockTranFilter> Filter;
        public PXSetup<AMPSetup> ampsetup;
        public LSAMClockTran lsselect;

        public ClockApprovalProcess()
        {
            ClockTranFilter filter = Filter.Current;
            UnapprovedTrans.SetProcessDelegate(delegate (List<AMClockTran> list)
            {
                // Acuminator disable once PX1088 InvalidViewUsageInProcessingDelegate [Using new instance of process]
                CreateLaborBatch(list, true, filter);
            });

            PXUIFieldAttribute.SetEnabled<AMClockTran.orderType>(UnapprovedTrans.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<AMClockTran.prodOrdID>(UnapprovedTrans.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<AMClockTran.operationID>(UnapprovedTrans.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<AMClockTran.shiftID>(UnapprovedTrans.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<AMClockTran.qty>(UnapprovedTrans.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<AMClockTran.tranDesc>(UnapprovedTrans.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<AMClockTran.startTime>(UnapprovedTrans.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<AMClockTran.endTime>(UnapprovedTrans.Cache, null, true);
        }

        public PXAction<AMClockTran> delete;
        [PXUIField(DisplayName = "Delete")]
        [PXButton]
        public virtual IEnumerable Delete(PXAdapter adapter)
        {
            if (UnapprovedTrans.Current == null)
                return adapter.Get();

            UnapprovedTrans.Cache.Delete(UnapprovedTrans.Current);
            return adapter.Get();
        }

        [Obsolete(InternalMessages.ClassIsObsoleteAndWillBeRemoved2021R2)]
        protected virtual void EnableFields(PXCache cache)
        {
            throw new NotImplementedException();
        }

        public static void CreateLaborBatch(List<AMClockTran> list, bool isMassProcess, ClockTranFilter filter)
        {
            var graph = CreateInstance<LaborEntry>();
            graph.ampsetup.Current.RequireControlTotal = false;
            var batch = graph.batch.Insert();
            if (batch == null)
            {
                throw new PXException(Messages.UnableToCreateRelatedTransaction);
            }

            batch.TranDesc = Messages.GetLocal(Messages.ClockLine);
            batch.OrigDocType = AMDocType.Clock;
            batch.Hold = false;
            var included = new List<int>();

            for (var i = 0; i < list.Count; i++)
            {
                var clock = list[i];
                if(!IsClockTranValid(clock))
                {
                    PXProcessing<AMClockTran>.SetError(i, Messages.LaborTimeGreaterZeroLessEqual24);
                    continue;
                }

                try
                {
                    var newTran = graph.transactions.Insert();
                    if (newTran == null)
                    {
                        PXProcessing<AMClockTran>.SetError(i, Messages.UnableToCreateRelatedTransaction);
                        continue;
                    }
                    newTran.EmployeeID = clock.EmployeeID;
                    newTran.OrderType = clock.OrderType;
                    newTran.ProdOrdID = clock.ProdOrdID;
                    newTran.OperationID = clock.OperationID;
                    newTran.ShiftID = clock.ShiftID;
                    newTran.Qty = clock.LastOper == true ? 0 : clock.Qty;
                    newTran.UOM = clock.UOM;
                    newTran.StartTime = PXDBDateAndTimeAttribute.CombineDateTime(Common.Dates.BeginOfTimeDate, clock.StartTime);
                    newTran.EndTime = PXDBDateAndTimeAttribute.CombineDateTime(Common.Dates.BeginOfTimeDate, clock.EndTime);
                    newTran.OrigDocType = AMDocType.Clock;
                    newTran.OrigBatNbr = clock.EmployeeID.ToString();
                    newTran.OrigLineNbr = clock.LineNbr;
                    newTran.TranDate = clock.TranDate;
                    graph.transactions.Update(newTran);
                    if (clock.LastOper == true)
                    {
                        var splitList = PXSelect<AMClockTranSplit, Where<AMClockTranSplit.employeeID, Equal<Required<AMClockTranSplit.employeeID>>,
                            And<AMClockTranSplit.lineNbr, Equal<Required<AMClockTranSplit.lineNbr>>>>>.Select(graph, clock.EmployeeID, clock.LineNbr);
                        for (var j = 0; j < splitList.Count; j++)
                        {
                            var split = (AMClockTranSplit)splitList[j];
                            var newSplit = graph.splits.Insert();
                            newSplit.LocationID = split.LocationID;
                            newSplit.LotSerialNbr = split.LotSerialNbr;
                            newSplit.ExpireDate = split.ExpireDate;
                            newSplit.Qty = split.Qty;
                            graph.splits.Update(newSplit);
                        }
                    }

                    PXProcessing<AMClockTran>.SetInfo(i, ActionsMessages.RecordProcessed);
                    included.Add(i);
                }
                catch (Exception ex)
                {
                    PXTrace.WriteError($"[Employee ID = {clock.EmployeeID}; Line Nbr = {clock.LineNbr}] {ex.Message}");
                    PXProcessing<AMClockTran>.SetError(i, ex);
                    graph.transactions.Delete(graph.transactions.Current);
                }
            }

            var graphSaved = false;
            try
            {
                if (graph.transactions.Cache.Inserted.Count() > 0)
                {
                    graph.Persist();
                    graphSaved = true;
                    AMDocumentRelease.ReleaseDoc(new List<AMBatch> { graph.batch.Current }, false);
                }
            }
            catch (Exception e)
            {
                foreach (var i in included)
                {
                    PXProcessing<AMClockTran>.SetError(i, e);
                }

                if(e is PXOuterException)
                {
                    PXTraceHelper.PxTraceOuterException(e, PXTraceHelper.ErrorLevel.Error);
                }

                if (graphSaved)
                {
                    graph.Delete.Press(); 
                }
            }
        }

        /// <summary>
        /// Calculate the time between the user entered start/end times
        /// </summary>
        protected virtual int GetStartEndLaborTime(PXCache cache, AMClockTran tran)
        {
            return ClockTimeAttribute.GetTimeBetween(tran?.StartTime, tran?.EndTime);
        }

        /// <summary>
        /// Sets the Labor Time field with the calculated start/end labor hours value
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="tran"></param>
        protected virtual void CalcLaborTime(PXCache cache, AMClockTran tran)
        {
            if (tran == null)
            {
                return;
            }

            var newLaborTime = GetStartEndLaborTime(cache, tran);
            cache.SetValueExt<AMClockTran.laborTime>(tran, newLaborTime);
        }

        protected virtual void _(Events.FieldUpdated<ClockTranFilter.orderType> e)
        {
            e.Cache.SetValueExt<ClockTranFilter.prodOrdID>(e.Row, null);
        }

        protected virtual void _(Events.RowUpdated<AMClockTran> e)
        {
            if(e.Cache.ObjectsEqual<AMClockTran.startTime, AMClockTran.endTime>(e.Row, e.OldRow))
            {
                return;
            }

            CalcLaborTime(e.Cache, e.Row);

            if(e.Row.StartTime == null)
            {
                return;
            }

            // Keep start and tran date in sync
            var startDate = e.Row.StartTime.GetValueOrDefault().Date;
            if (startDate.Equals(e.Row.TranDate.GetValueOrDefault()))
            {
                return;
            }

            e.Cache.SetValueExt<AMClockTran.tranDate>(e.Row, startDate);
        }

        protected virtual void _(Events.RowPersisting<AMClockTran> e)
        {
            if (string.IsNullOrWhiteSpace(e.Row?.ProdOrdID) || e.Operation == PXDBOperation.Delete)
            {
                return;
            }

            if (e.Row.LaborTime.GetValueOrDefault() == 0 && e.Row.Qty.GetValueOrDefault() == 0 )
            {
                e.Cache.RaiseExceptionHandling<AMClockTran.qty>(
                    e.Row,
                    e.Row.Qty,
                    new PXSetPropertyException(Messages.GetLocal(Messages.FieldCannotBeZero, PXUIFieldAttribute.GetDisplayName<AMClockTran.qty>(e.Cache)),
                        PXErrorLevel.Error));
                e.Cache.RaiseExceptionHandling<AMClockTran.laborTime>(
                    e.Row,
                    e.Row.LaborTime,
                    new PXSetPropertyException(Messages.GetLocal(Messages.FieldCannotBeZero, PXUIFieldAttribute.GetDisplayName<AMClockTran.laborTime>(e.Cache)),
                        PXErrorLevel.Error));
            }
        }

        public static bool IsClockTranValid(AMClockTran clockTran)
        {
            return clockTran?.LaborTime != null && clockTran.LaborTime.GetValueOrDefault().BetweenInclusive(1, 1440);
        }
    }

    [Serializable]
    [PXCacheName("Clock Filter")]
    public class ClockTranFilter : IBqlTable
    {
        #region EmployeeID
        public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }

        protected Int32? _EmployeeID;
        [PXInt]
        [ProductionEmployeeSelector]
        [PXUIField(DisplayName = "Employee ID")]
        public virtual Int32? EmployeeID
        {
            get
            {
                return this._EmployeeID;
            }
            set
            {
                this._EmployeeID = value;
            }
        }
        #endregion
        #region OrderType
        public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

        protected String _OrderType;
        [PXDefault(typeof(AMPSetup.defaultOrderType))]
        [AMOrderTypeField(Required = false)]
        [PXRestrictor(typeof(Where<AMOrderType.function, NotEqual<OrderTypeFunction.planning>>), Messages.IncorrectOrderTypeFunction)]
        [PXRestrictor(typeof(Where<AMOrderType.active, Equal<True>>), PX.Objects.SO.Messages.OrderTypeInactive)]
        [AMOrderTypeSelector]
        public virtual String OrderType
        {
            get
            {
                return this._OrderType;
            }
            set
            {
                this._OrderType = value;
            }
        }
        #endregion
        #region ProdOrdID
        public abstract class prodOrdID : PX.Data.BQL.BqlString.Field<prodOrdID> { }

        protected String _ProdOrdID;
        [ProductionNbr]
        [ProductionOrderSelector(typeof(orderType), true)]
        [PXRestrictor(typeof(Where<AMProdItem.hold, NotEqual<True>,
            And<Where<AMProdItem.statusID, Equal<ProductionOrderStatus.released>,
                Or<AMProdItem.statusID, Equal<ProductionOrderStatus.inProcess>,
                    Or<AMProdItem.statusID, Equal<ProductionOrderStatus.completed>>>>>>),
            Messages.ProdStatusInvalidForProcess, typeof(AMProdItem.orderType), typeof(AMProdItem.prodOrdID), typeof(AMProdItem.statusID))]
        public virtual String ProdOrdID
        {
            get
            {
                return this._ProdOrdID;
            }
            set
            {
                this._ProdOrdID = value;
            }
        }
        #endregion
    }
}