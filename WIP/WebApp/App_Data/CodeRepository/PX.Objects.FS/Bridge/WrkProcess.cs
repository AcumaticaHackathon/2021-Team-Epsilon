using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.FS
{
    public class WrkProcess : PXGraph<WrkProcess, FSWrkProcess>
    {
        #region Public Members
        public const char SEPARATOR = ',';
        public bool LaunchTargetScreen = true;
        public int? processID;
        #endregion

        public PXSelect<FSWrkProcess> WrkProcessRecords;

        #region virtual Methods

        /// <summary>
        /// Split a string in several substrings by a separator character.
        /// </summary>
        /// <param name="parameters">String representing the whole parameters.</param>
        /// <param name="separator">Char representing the separation of parameters.</param>
        /// <returns>A string list.</returns>
        public virtual List<string> GetParameterList(string parameters, char separator)
        {
            List<string> parameterList = new List<string>();

            if (string.IsNullOrEmpty(parameters))
            {
                return parameterList;
            }

            parameterList = parameters.Split(separator).ToList();

            return parameterList;
        }
        public virtual void ValidateSrvOrdTypeNumberingSequence(PXGraph graph, string srvOrdType)
        {
            AppointmentEntry.ValidateSrvOrdTypeNumberingSequenceInt(graph, srvOrdType);
        }
        #endregion

        #region Private Functions

        /// <summary>
        /// Delete old records from database.
        /// </summary>
        private void DeleteOldRecords()
        {
            PXDatabase.Delete<FSWrkProcess>(
                new PXDataFieldRestrict<FSWrkProcess.createdDateTime>(PXDbType.DateTime, 8, DateTime.Now.AddDays(-2), PXComp.LE));
        }

        #region Service Order Methods

        /// <summary>
        /// Try to get the appropriate ServiceOrderType from this sources:
        /// a. <c>FSServiceOrder</c>
        /// b. <c>FSWrkProcessRow</c>
        /// c. <c>FSSetup</c>
        /// </summary>
        /// <param name="fsWrkProcessRow"><c>FSWrkProcess</c> row.</param>
        /// <param name="fsServiceOrderRow">FSServiceOrder row.</param>
        public virtual string GetSrvOrdType(PXGraph graph, FSWrkProcess fsWrkProcessRow, FSServiceOrder fsServiceOrderRow)
        {
            // a) Try to retrieve the ServiceOrderType from the ServiceOrder row
            if (fsWrkProcessRow.SOID != null
                    && fsServiceOrderRow != null
                      && !string.IsNullOrEmpty(fsServiceOrderRow.SrvOrdType))
            {
                return fsServiceOrderRow.SrvOrdType;
            }

            // b) Try to retrieve the ServiceOrderType from the WrkProcess row
            if (!string.IsNullOrEmpty(fsWrkProcessRow.SrvOrdType) &&
                !string.IsNullOrWhiteSpace(fsWrkProcessRow.SrvOrdType))
            {
                return fsWrkProcessRow.SrvOrdType;
            }

            // c) Try to retrieve the ServiceOrderType from the users preferences
            PX.SM.UserPreferences userPreferencesRow = PXSelect<PX.SM.UserPreferences,
                                                       Where<
                                                           PX.SM.UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>>>
                                                       .Select(graph);

            if (userPreferencesRow != null)
            {
                FSxUserPreferences fsxUserPreferencesRow = PXCache<PX.SM.UserPreferences>.GetExtension<FSxUserPreferences>(userPreferencesRow);

                if (!string.IsNullOrEmpty(fsxUserPreferencesRow.DfltSrvOrdType))
                {
                    return fsxUserPreferencesRow.DfltSrvOrdType;
                }
            }


            // d) Try to retrieve the Default ServiceOrderType from the Setup row
            FSSetup fsSetupRow = PXSetup<FSSetup>.Select(graph);

            if (fsSetupRow != null
                    && !string.IsNullOrEmpty(fsSetupRow.DfltSrvOrdType))
            {
                return fsSetupRow.DfltSrvOrdType;
            }

            return null;
        }

        /// <summary>
        /// Try to retrieve a ServiceOrder row associated to the supplied <c>WrkProcess</c> row.
        /// </summary>
        /// <param name="fsWrkProcessRow"><c>FSWrkProcess</c> row.</param>
        /// <returns><c>FSServiceOrder</c> row.</returns>
        public virtual FSServiceOrder GetServiceOrder(PXGraph graph, FSWrkProcess fsWrkProcessRow)
        {
            if (fsWrkProcessRow == null)
            {
                return null;
            }

            if (fsWrkProcessRow.SOID != null)
            {
                return PXSelect<FSServiceOrder,
                       Where<
                            FSServiceOrder.sOID, Equal<Required<FSServiceOrder.sOID>>>>
                       .Select(graph, fsWrkProcessRow.SOID);
            }

            return null;
        }

        #endregion

        #region LauncScreen Methods

        /// <summary>
        /// Launches the target screen specified in the <c>FSWrkProcess</c> row.
        /// </summary>
        /// <param name="processID"><c>Int</c> id of the process.</param>
        public virtual void LaunchScreen(int? processID)
        {
            DeleteOldRecords();

            FSWrkProcess fsWrkProcessRow = PXSelect<FSWrkProcess,
                                           Where<
                                               FSWrkProcess.processID, Equal<Required<FSWrkProcess.processID>>>>
                                           .Select(this, processID);

            if (fsWrkProcessRow == null)
            {
                return;
            }

            switch (fsWrkProcessRow.TargetScreenID)
            {
                case ID.ScreenID.APPOINTMENT:
                    this.LaunchAppointmentEntryScreen(fsWrkProcessRow);
                    break;

                default:
                    return;
            }
        }

        public virtual void AssignAppointmentRoom(AppointmentEntry graphAppointmentEntry, FSWrkProcess fsWrkProcessRow, FSServiceOrder fsServiceOrderRow = null)
        {
            if (string.IsNullOrEmpty(fsWrkProcessRow.RoomID) == false
                    && string.IsNullOrWhiteSpace(fsWrkProcessRow.RoomID) == false)
            {
                graphAppointmentEntry.ServiceOrderRelated.SetValueExt<FSServiceOrder.roomID>
                                        (graphAppointmentEntry.ServiceOrderRelated.Current, fsWrkProcessRow.RoomID);

                if (fsServiceOrderRow == null)
                {
                    fsServiceOrderRow = GetServiceOrder(graphAppointmentEntry, fsWrkProcessRow);
                }

                if (fsServiceOrderRow != null)
                {
                    graphAppointmentEntry.ServiceOrderRelated.Cache.SetStatus(graphAppointmentEntry.ServiceOrderRelated.Current, PXEntryStatus.Updated);
                }
            }
        }

        public virtual void AssignAppointmentEmployeeByList(AppointmentEntry graphAppointmentEntry, List<string> employeeList, List<string> soDetIDList)
        {
            employeeList.Reverse();

            if (employeeList.Count > 0 && soDetIDList.Count <= 0)
            {
                for (int i = 0; i < employeeList.Count; i++)
                {
                    FSAppointmentEmployee fsAppointmentEmployeeRow = new FSAppointmentEmployee();

                    fsAppointmentEmployeeRow = graphAppointmentEntry.AppointmentServiceEmployees.Insert(fsAppointmentEmployeeRow);
                    graphAppointmentEntry.AppointmentServiceEmployees.Cache.SetValueExt<FSAppointmentEmployee.employeeID>(fsAppointmentEmployeeRow, (int?)Convert.ToInt32(employeeList[i]));
                }
            }
        }

        /// <summary>
        /// Launches the AppointmentEntry screen with some preloaded values.
        /// </summary>
        /// <param name="fsWrkProcessRow"><c>FSWrkProcess</c> row.</param>
        public virtual int? LaunchAppointmentEntryScreen(FSWrkProcess fsWrkProcessRow, bool redirect = true)
        {
            AppointmentEntry graphAppointmentEntry = PXGraph.CreateInstance<AppointmentEntry>();

            List<string> soDetIDList = GetParameterList(fsWrkProcessRow.LineRefList, SEPARATOR);
            List<string> employeeList = GetParameterList(fsWrkProcessRow.EmployeeIDList, SEPARATOR);

            if (fsWrkProcessRow.AppointmentID != null)
            {
                graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Search<FSAppointment.appointmentID>(fsWrkProcessRow.AppointmentID, fsWrkProcessRow.SrvOrdType);
                AssignAppointmentRoom(graphAppointmentEntry, fsWrkProcessRow);
                AssignAppointmentEmployeeByList(graphAppointmentEntry, employeeList, soDetIDList);
            }
            else
            {
                FSAppointment fsAppointmentRow = new FSAppointment();
                FSServiceOrder fsServiceOrderRow = GetServiceOrder(this, fsWrkProcessRow);
                fsAppointmentRow.SrvOrdType = GetSrvOrdType(this, fsWrkProcessRow, fsServiceOrderRow);

                if (fsAppointmentRow.SrvOrdType == null)
                {
                    throw new PXException(TX.Error.DEFAULT_SERVICE_ORDER_TYPE_NOT_DEFINED);
                }

                if (fsAppointmentRow.SOID == null)
                {
                    ValidateSrvOrdTypeNumberingSequence(this, fsAppointmentRow.SrvOrdType);
                }

                graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Insert(fsAppointmentRow);

                #region ScheduleDateTime Fields
                // to know if we want to set false to the flag KeepTotalServicesDuration
                bool scheduleTimeFlag = true;

                if (fsWrkProcessRow.ScheduledDateTimeBegin.HasValue == true)
                {
                    graphAppointmentEntry.AppointmentRecords.SetValueExt<FSAppointment.scheduledDateTimeBegin>
                                                             (graphAppointmentEntry.AppointmentRecords.Current,
                                                              fsWrkProcessRow.ScheduledDateTimeBegin);
                }
                else
                {
                    scheduleTimeFlag = false;
                }

                if (fsWrkProcessRow.ScheduledDateTimeEnd.HasValue && scheduleTimeFlag)
                {
                    graphAppointmentEntry.AppointmentRecords.SetValueExt<FSAppointment.handleManuallyScheduleTime>
                                                             (graphAppointmentEntry.AppointmentRecords.Current, true);

                    if (fsWrkProcessRow.ScheduledDateTimeBegin != fsWrkProcessRow.ScheduledDateTimeEnd)
                    {
                        graphAppointmentEntry.AppointmentRecords.SetValueExt<FSAppointment.scheduledDateTimeEnd>
                                                                 (graphAppointmentEntry.AppointmentRecords.Current,
                                                                  fsWrkProcessRow.ScheduledDateTimeEnd);
                    }
                    else
                    {
                        graphAppointmentEntry.AppointmentRecords.SetValueExt<FSAppointment.scheduledDateTimeEnd>
                                                                 (graphAppointmentEntry.AppointmentRecords.Current,
                                                                  fsWrkProcessRow.ScheduledDateTimeBegin.Value.AddHours(1));
                    }
                }

                #endregion

                #region ServiceOrder Fields

                if (fsServiceOrderRow == null)
                {
                    if (fsWrkProcessRow.BranchID.HasValue == true)
                    {
                        graphAppointmentEntry.ServiceOrderRelated.SetValueExt<FSServiceOrder.branchID>
                                                                  (graphAppointmentEntry.ServiceOrderRelated.Current,
                                                                   fsWrkProcessRow.BranchID);
                    }

                    if (fsWrkProcessRow.BranchLocationID.HasValue == true && fsWrkProcessRow.BranchLocationID > 0)
                    {
                        graphAppointmentEntry.ServiceOrderRelated.SetValueExt<FSServiceOrder.branchLocationID>
                                                                  (graphAppointmentEntry.ServiceOrderRelated.Current,
                                                                   fsWrkProcessRow.BranchLocationID);
                    }

                    if (fsWrkProcessRow.CustomerID.HasValue == true && fsWrkProcessRow.CustomerID > 0)
                    {
                        graphAppointmentEntry.AppointmentRecords.SetValueExt<FSAppointment.customerID>
                                                                  (graphAppointmentEntry.AppointmentRecords.Current,
                                                                   fsWrkProcessRow.CustomerID);
                    }
                }
                else
                {
                    graphAppointmentEntry.AppointmentRecords.SetValueExt<FSAppointment.soRefNbr>
                                                             (graphAppointmentEntry.AppointmentRecords.Current,
                                                              fsServiceOrderRow.RefNbr);
                }

                AssignAppointmentRoom(graphAppointmentEntry, fsWrkProcessRow, fsServiceOrderRow);

                #endregion

                #region Get Appointment Values

                #region Services
                soDetIDList.Reverse();

                if (soDetIDList.Count > 0)
                {
                    var appointmentDetails = graphAppointmentEntry.AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                                                     .Where(x => x.IsInventoryItem == false);

                    foreach (FSAppointmentDet fsAppointmentDetRow in appointmentDetails)
                    {
                        if (soDetIDList.Contains(fsAppointmentDetRow.SODetID.ToString()) == false)
                        {
                            graphAppointmentEntry.AppointmentDetails.Delete(fsAppointmentDetRow);
                        }
                        else
                        {
                            InsertEmployeeLinkedToService(graphAppointmentEntry, employeeList, fsAppointmentDetRow.LineRef);
                        }
                    }

                    var employees = graphAppointmentEntry.AppointmentServiceEmployees.Select()
                                                         .RowCast<FSAppointmentEmployee>()
                                                         .Where(_ => _.ServiceLineRef != null);

                    foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in employees)
                    {
                        FSAppointmentDet fsAppointmentDetRow = (FSAppointmentDet)PXSelectorAttribute.Select<FSAppointmentEmployee.serviceLineRef>
                                                                                                     (graphAppointmentEntry.AppointmentServiceEmployees.Cache,
                                                                                                      fsAppointmentEmployeeRow);

                        if (fsAppointmentDetRow != null && soDetIDList.Contains(fsAppointmentDetRow.SODetID.ToString()) == false)
                        {
                            graphAppointmentEntry.AppointmentServiceEmployees.Delete(fsAppointmentEmployeeRow);
                        }
                    }
                }
                #endregion
                #region Employees

                AssignAppointmentEmployeeByList(graphAppointmentEntry, employeeList, soDetIDList);

                #endregion
                #region Equipment
                List<string> equipmentList = GetParameterList(fsWrkProcessRow.EquipmentIDList, SEPARATOR);
                equipmentList.Reverse();

                if (equipmentList.Count > 0)
                {
                    for (int i = 0; i < equipmentList.Count; i++)
                    {
                        FSAppointmentResource fsAppointmentResourceRow = new FSAppointmentResource();

                        fsAppointmentResourceRow.SMEquipmentID = (int?)Convert.ToInt32(equipmentList[i]);
                        graphAppointmentEntry.AppointmentResources.Insert(fsAppointmentResourceRow);
                    }
                }
                #endregion
                #endregion
            }

            if (fsWrkProcessRow.SMEquipmentID.HasValue == true)
            {
                graphAppointmentEntry.AppointmentRecords.SetValueExt<FSAppointment.mem_SMequipmentID>
                                                         (graphAppointmentEntry.AppointmentRecords.Current,
                                                          fsWrkProcessRow.SMEquipmentID);

                redirect = true;
            }

            if (redirect == true)
            {
                throw new PXRedirectRequiredException(graphAppointmentEntry, null) { Mode = PXBaseRedirectException.WindowMode.Same };
            }
            else
            {
                try
                {
                    graphAppointmentEntry.RecalculateExternalTaxesSync = true;
                    graphAppointmentEntry.Actions.PressSave();
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    graphAppointmentEntry.RecalculateExternalTaxesSync = false;
                }

                return graphAppointmentEntry.AppointmentRecords.Current?.AppointmentID;
            }
        }

        public virtual void InsertEmployeeLinkedToService(AppointmentEntry graphAppointmentEntry, List<string> employeeList, string lineRef)
        {
            foreach (string employeeID in employeeList)
            {
                int employeeIntID = -1;

                if (int.TryParse(employeeID, out employeeIntID))
                {
                    FSAppointmentEmployee fsAppointmentEmployeeRow = new FSAppointmentEmployee();
                    fsAppointmentEmployeeRow = graphAppointmentEntry.AppointmentServiceEmployees.Insert(fsAppointmentEmployeeRow);

                    graphAppointmentEntry.AppointmentServiceEmployees.Cache.SetValueExt<FSAppointmentEmployee.employeeID>(fsAppointmentEmployeeRow, employeeIntID);
                    graphAppointmentEntry.AppointmentServiceEmployees.Cache.SetValueExt<FSAppointmentEmployee.serviceLineRef>(fsAppointmentEmployeeRow, lineRef);
                }
            }
        }
        #endregion

        #endregion

        #region FSWrkProcess Events

        protected virtual void FSWrkProcess_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (LaunchTargetScreen == false)
            {
                return;
            }

            FSWrkProcess fsWrkProcessRow = (FSWrkProcess)e.Row;
            if (fsWrkProcessRow.ProcessID > 0 && processID == null)
            {
                processID = fsWrkProcessRow.ProcessID;
            }

            if (processID > 0)
            {
                LaunchScreen(processID);
                LaunchTargetScreen = false;
            }
        }

        #endregion
    }
}