using PX.Data;
using PX.Objects.CR;
using PX.Objects.EP;
using System.Linq;
using System;

namespace PX.Objects.FS
{
    public static class TimeCardHelper
    {
        public static bool IsAccessedFromAppointment(string screenID)
        {
            string[] logActionsMobileScreens = { 
                ID.ScreenID.LOG_ACTION_START_TRAVEL_SRV_MOBILE,
                ID.ScreenID.LOG_ACTION_START_STAFF_MOBILE,
                ID.ScreenID.LOG_ACTION_START_STAFF_ASSIGNED_MOBILE,
                ID.ScreenID.LOG_ACTION_COMPLETE_SERVICE_MOBILE,
                ID.ScreenID.LOG_ACTION_COMPLETE_TRAVEL_MOBILE
            };

            return screenID == SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT)
                    || Array.Exists(logActionsMobileScreens, x => SharedFunctions.SetScreenIDToDotFormat(x) == screenID);
        }

        public static bool CanCurrentUserEnterTimeCards(PXGraph graph, string callerScreenID)
        {
            if (callerScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT)
                && callerScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT_INQUIRY))
            {
                return true;
            }

            EPEmployee employeeByUserID = PXSelect<EPEmployee, Where<EPEmployee.userID, Equal<Current<AccessInfo.userID>>>>.Select(graph);
            if (employeeByUserID == null)
            {
                return false;
            }

            return true;
        }

        [Obsolete("Function depricated, will be removed in 2021r2")]
        public static void PMTimeActivity_RowPersisting_Handler(PXCache cache, PXGraph graph, PMTimeActivity pmTimeActivityRow, PXRowPersistingEventArgs e)
        {
            PMTimeActivity_RowPersisting_Handler(cache,graph, null, pmTimeActivityRow, e);
        }

        public static void PMTimeActivity_RowPersisting_Handler(PXCache cache, PXGraph graph, AppointmentEntry appGraph, PMTimeActivity pmTimeActivityRow, PXRowPersistingEventArgs e)
        {
            if (pmTimeActivityRow == null)
            {
                return;
            }

            FSxPMTimeActivity fsxPMTimeActivityRow = PXCache<PMTimeActivity>.GetExtension<FSxPMTimeActivity>(pmTimeActivityRow);

            if (fsxPMTimeActivityRow.AppointmentID == null || fsxPMTimeActivityRow.LogLineNbr == null)
            {
                return;
            }

            if (e.Operation == PXDBOperation.Delete
                    && graph.Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT))
            {
                    PXUpdate<
                        Set<FSAppointmentLog.trackTime, False>,
                    FSAppointmentLog,
                    Where<
                        FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                        And<FSAppointmentLog.lineNbr, Equal<Required<FSAppointmentLog.lineNbr>>>>>
                    .Update(graph, fsxPMTimeActivityRow.AppointmentID, fsxPMTimeActivityRow.LogLineNbr);
                }

            if ((e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update)
                    && !IsAccessedFromAppointment(graph.Accessinfo.ScreenID))
            {
                PXCache appointmentLogCache = graph.Caches[typeof(FSAppointmentLog)];
                appointmentLogCache.ClearQueryCache();

                FSAppointmentLog fsAppointmentLogRow = PXSelectReadonly<FSAppointmentLog,
                        Where<
                            FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                            And<FSAppointmentLog.lineNbr, Equal<Required<FSAppointmentLog.lineNbr>>>>>
                        .Select(graph, fsxPMTimeActivityRow.AppointmentID, fsxPMTimeActivityRow.LogLineNbr);

                if (fsAppointmentLogRow == null)
                {
                    throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSAppointmentLog)));
                }
                
                AppointmentEntry graphAppointmentEntry = appGraph != null ? appGraph: PXGraph.CreateInstance<AppointmentEntry>();
                bool updateAppointment = false;

                if (fsAppointmentLogRow.ProjectTaskID != pmTimeActivityRow.ProjectTaskID)
                {
                    LoadAppointmentGraph(graph,
                        fsxPMTimeActivityRow,
                        fsAppointmentLogRow,
                        ref graphAppointmentEntry);

                    fsAppointmentLogRow.ProjectTaskID = pmTimeActivityRow.ProjectTaskID;
                    updateAppointment = true;
                }

                if (fsAppointmentLogRow.EarningType != pmTimeActivityRow.EarningTypeID)
                {
                    LoadAppointmentGraph(graph,
                        fsxPMTimeActivityRow,
                        fsAppointmentLogRow,
                        ref graphAppointmentEntry);

                    fsAppointmentLogRow.EarningType = pmTimeActivityRow.EarningTypeID;
                    updateAppointment = true;
                }

                if (fsAppointmentLogRow.TimeDuration != pmTimeActivityRow.TimeSpent)
                {
                    LoadAppointmentGraph(graph,
                        fsxPMTimeActivityRow,
                        fsAppointmentLogRow,
                        ref graphAppointmentEntry);

                    fsAppointmentLogRow.TimeDuration = pmTimeActivityRow.TimeSpent;
                    updateAppointment = true;
                }

                if (fsAppointmentLogRow.IsBillable != pmTimeActivityRow.IsBillable)
                {
                    LoadAppointmentGraph(graph,
                        fsxPMTimeActivityRow,
                        fsAppointmentLogRow,
                        ref graphAppointmentEntry);

                    if (graphAppointmentEntry.ShouldUpdateAppointmentLogBillableFieldsFromTimeCard() == true)
                    {
                        fsAppointmentLogRow.IsBillable = pmTimeActivityRow.IsBillable;
                        updateAppointment = true;
                    }
                }

                if (fsAppointmentLogRow.BillableTimeDuration != pmTimeActivityRow.TimeBillable)
                {
                    LoadAppointmentGraph(graph,
                        fsxPMTimeActivityRow,
                        fsAppointmentLogRow,
                        ref graphAppointmentEntry);

                    if (graphAppointmentEntry.ShouldUpdateAppointmentLogBillableFieldsFromTimeCard() == true)
                    {
                        fsAppointmentLogRow.BillableTimeDuration = pmTimeActivityRow.TimeBillable;
                        updateAppointment = true;
                    }
                    }

                if (updateAppointment == true)
                {
                    graphAppointmentEntry.SkipTimeCardUpdate = true;
                    graphAppointmentEntry.LogRecords.Update(fsAppointmentLogRow);

                    graphAppointmentEntry.Save.Press();
                }
            }
        }

        public static void LoadAppointmentGraph(PXGraph graph,
                                                FSxPMTimeActivity fsxPMTimeActivityRow,
                                                FSAppointmentLog fsAppointmentLogRow,
                                                ref AppointmentEntry graphAppointmentEntry)
        {
            if (fsxPMTimeActivityRow == null
                || fsAppointmentLogRow == null
                || fsxPMTimeActivityRow.AppointmentID != fsAppointmentLogRow.DocID)
            {
                throw new PXInvalidOperationException();
            }

            if (graphAppointmentEntry == null)
            {
                graphAppointmentEntry = PXGraph.CreateInstance<AppointmentEntry>();
            }

            if (graphAppointmentEntry.AppointmentRecords.Current == null
                || fsAppointmentLogRow.DocID != graphAppointmentEntry.AppointmentRecords.Current.AppointmentID)
            {
                FSAppointment fsAppointmentRow = PXSelect<FSAppointment,
                                                 Where<
                                                     FSAppointment.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>
                                                 .Select(graph, fsxPMTimeActivityRow.AppointmentID);

                if (fsAppointmentRow == null)
                {
                    throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSAppointment)));
                }

                graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Search<FSAppointment.appointmentID>
                                                        (fsAppointmentRow.AppointmentID, fsAppointmentRow.SrvOrdType);

                if (graphAppointmentEntry.AppointmentRecords.Current.AppointmentID != fsAppointmentRow.AppointmentID)
                {
                    throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSAppointment)));
                }
            }
        }

        /// <summary>
        /// Checks if the Employee Time Cards Integration is enabled in the Service Management Setup.
        /// </summary>
        public static bool IsTheTimeCardIntegrationEnabled(PXGraph graph)
        {
            FSSetup fsSetupRow = ServiceManagementSetup.GetServiceManagementSetup(graph);
            return fsSetupRow != null && fsSetupRow.EnableEmpTimeCardIntegration == true;
        }

        public static void PMTimeActivity_RowSelected_Handler(PXCache cache, PMTimeActivity pmTimeActivityRow)
        {
            FSxPMTimeActivity fsxPMTimeActivityRow = cache.GetExtension<FSxPMTimeActivity>(pmTimeActivityRow);

            PXUIFieldAttribute.SetEnabled<FSxPMTimeActivity.appointmentID>(cache, pmTimeActivityRow, false);
            PXUIFieldAttribute.SetEnabled<FSxPMTimeActivity.appointmentCustomerID>(cache, pmTimeActivityRow, false);
            PXUIFieldAttribute.SetEnabled<FSxPMTimeActivity.sOID>(cache, pmTimeActivityRow, false);
            PXUIFieldAttribute.SetEnabled<FSxPMTimeActivity.logLineNbr>(cache, pmTimeActivityRow, false);
            PXUIFieldAttribute.SetEnabled<FSxPMTimeActivity.serviceID>(cache, pmTimeActivityRow, false);
        }

        /// <summary>
        /// Checks if the all Appointment Service lines are approved by a Time Card, then sets Time Register to true and completes the appointment.
        /// </summary>
        public static void CheckTimeCardAppointmentApprovalsAndComplete(AppointmentEntry graphAppointmentEntry, PXCache cache, FSAppointment fsAppointmentRow)
        {
            bool allLinesApprovedByTimeCard = true;
            bool isEmployeeLines = false;
            if (fsAppointmentRow.Completed == true)
            {
                var employeesLogs = graphAppointmentEntry.LogRecords.Select().RowCast<FSAppointmentLog>()
                                                                                 .Where(y => y.BAccountType == BAccountType.EmployeeType
                                                                                          && y.TrackTime == true);

                foreach (FSAppointmentLog fsAppointmentLogRow in employeesLogs)
                {
                    isEmployeeLines = true;
                    allLinesApprovedByTimeCard = allLinesApprovedByTimeCard && (bool)fsAppointmentLogRow.ApprovedTime;

                    if (!allLinesApprovedByTimeCard)
                    {
                        break;
                    }
                }

                if (allLinesApprovedByTimeCard && isEmployeeLines)
                {
                    fsAppointmentRow.TimeRegistered = true;
                }
            }
        }
    }
}
