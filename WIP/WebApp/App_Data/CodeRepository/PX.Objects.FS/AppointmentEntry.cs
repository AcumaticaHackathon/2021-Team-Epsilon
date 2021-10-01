using Autofac;
using PX.Common;
using PX.Data;
using PX.Data.DependencyInjection;
using PX.Data.Reports;
using PX.FS;
using PX.LicensePolicy;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.EP;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.Extensions.SalesTax;
using PX.Objects.FS.ParallelProcessing;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;
using PX.Objects.TX;
using PX.Reports;
using PX.Reports.Controls;
using PX.Reports.Data;
using PX.SM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Compilation;
using TMEPEmployee = PX.Objects.CR.Standalone.EPEmployee;
using PX.Objects.PO;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;
using PX.Api;
using PX.Data.WorkflowAPI;

namespace PX.Objects.FS
{
    public class AppointmentEntry : ServiceOrderBase<AppointmentEntry, FSAppointment>, IGraphWithInitialization
    {
        public class ServiceRequirement
        {
            public int serviceID;
            public List<int?> requirementIDList = new List<int?>();
        }

        public enum DateFieldType
        {
            ScheduleField,
            ActualField
        }

        public static class FSMailing
        {
            public const string EMAIL_CONFIRMATION_TO_CUSTOMER = "NOTIFY CUSTOMER";
            public const string EMAIL_CONFIRMATION_TO_STAFF = "NOTIFY STAFF";
            public const string EMAIL_NOTIFICATION_TO_GEOZONE_STAFF = "NOTIFY SERVICE AREA STAFF";
            public const string EMAIL_NOTIFICATION_SIGNED_APPOINTMENT = "NOTIFY SIGNED APPOINTMENT";
        }

        public class FSNotificationContactType : NotificationContactType
        {
            public const string Customer = "U";
            public const string EmployeeStaff = "F";
            public const string VendorStaff = "X";
            public const string GeoZoneStaff = "G";
            public const string Salesperson = "L";
        }


        public enum SlotIsContained
        {
            NotContained = 1, Contained, PartiallyContained, ExceedsContainment
        }

        public enum ActionButton
        {
            PutOnHold = 1, ReleaseFromHold, StartAppointment, CompleteAppointment
                         , ReOpenAppointment, CancelAppointment, CloseAppointment
                         , UnCloseAppointment, InvoiceAppointment
        }

        public bool IsCloningAppointment = false;
        public bool NeedRecalculateRouteStats;
        public bool CalculateGoogleStats = true;
        public bool AvoidCalculateRouteStats = false;
        public bool SkipServiceOrderUpdate = false;
        public bool SkipTimeCardUpdate = false;
        public string UpdateSOStatusOnAppointmentDeleting = string.Empty;
        public bool IsGeneratingAppointment = false;
        public bool SkipChangingContract = false;
        public bool SkipManualTimeFlagUpdate = false;
        public bool SkipCallSOAction = false;
        public bool DisableServiceOrderUnboundFieldCalc = false;
        public string UncloseDialogMessage = TX.Messages.ASK_CONFIRM_APPOINTMENT_UNCLOSING;
        protected bool RetakeGeoLocation = false;
        public bool SkipLongOperation = false;
        public bool SkipLotSerialFieldVerifying = false;

        protected bool serviceOrderIsAlreadyUpdated = false;
        protected Exception CatchedServiceOrderUpdateException = null;
        protected bool serviceOrderRowPersistedPassedWithStatusAbort = false;
        protected bool insertingServiceOrder = false;
        public bool recalculateCuryID = false;

        protected bool UpdatingItemLinesBecauseOfDocStatusChange { get; set; }

        public bool RecalculateExternalTaxesSync { get; set; }
        protected virtual void RecalculateExternalTaxes()
        {
        }

        private PXGraph dummyGraph;

        #region Private Members
        private FSSelectorHelper fsSelectorHelper;

        private FSSelectorHelper GetFsSelectorHelperInstance
        {
            get
            {
                if (this.fsSelectorHelper == null)
                {
                    this.fsSelectorHelper = new FSSelectorHelper();
                }

                return this.fsSelectorHelper;
            }
        }

        private ServiceOrderEntry _ServiceOrderEntryGraph;

        private EmployeeActivitiesEntry _EmployeeActivitiesEntryGraph;

        private void RefreshServiceOrderRelated()
        {
            ServiceOrderRelated.Cache.Clear();
            ServiceOrderRelated.View.Clear();
            ServiceOrderRelated.View.RequestRefresh();
        }

        public void ClearServiceOrderEntry()
        {
            _ServiceOrderEntryGraph?.Clear(PXClearOption.ClearAll);
        }

        protected ServiceOrderEntry GetServiceOrderEntryGraph(bool clearGraph)
        {
            if (_ServiceOrderEntryGraph == null)
            {
                _ServiceOrderEntryGraph = PXGraph.CreateInstance<ServiceOrderEntry>();
            }
            else if (clearGraph == true && _ServiceOrderEntryGraph.RunningPersist == false)
            {
                _ServiceOrderEntryGraph.Clear();
            }

            if (_ServiceOrderEntryGraph.RunningPersist == false)
            {
            _ServiceOrderEntryGraph.RecalculateExternalTaxesSync = true;
            _ServiceOrderEntryGraph.GraphAppointmentEntryCaller = this;
            }

            return _ServiceOrderEntryGraph;
        }

        public void SetServiceOrderEntryGraph(ServiceOrderEntry soGraph)
        {
            this._ServiceOrderEntryGraph = soGraph;
        }

        protected EmployeeActivitiesEntry GetEmployeeActivitiesEntryGraph(bool clearGraph = true)
        {
            if (_EmployeeActivitiesEntryGraph == null)
            {
                _EmployeeActivitiesEntryGraph = PXGraph.CreateInstance<EmployeeActivitiesEntry>();
            }
            else if (clearGraph == true)
            {
                _EmployeeActivitiesEntryGraph.Clear(PXClearOption.PreserveTimeStamp);
            }

            SM_EmployeeActivitiesEntry extGraph = _EmployeeActivitiesEntryGraph.GetExtension<SM_EmployeeActivitiesEntry>();
            extGraph.GraphAppointmentEntryCaller = this;

            return _EmployeeActivitiesEntryGraph;
        }

        protected Dictionary<object, object> _oldRows = null;
        protected bool updateContractPeriod = false;

        private Dictionary<FSAppointmentDet, FSSODet> _ApptLinesWithSrvOrdLineUpdated = null;
        protected virtual Dictionary<FSAppointmentDet, FSSODet> ApptLinesWithSrvOrdLineUpdated
        {
            get
            {
                return _ApptLinesWithSrvOrdLineUpdated;
            }
            set
            {
                _ApptLinesWithSrvOrdLineUpdated = value;
            }
        }

        private Dictionary<FSApptLineSplit, FSSODetSplit> _ApptSplitsWithSrvOrdSplitUpdated = null;
        protected virtual Dictionary<FSApptLineSplit, FSSODetSplit> ApptSplitsWithSrvOrdSplitUpdated
        {
            get
            {
                return _ApptSplitsWithSrvOrdSplitUpdated;
            }
            set
            {
                _ApptSplitsWithSrvOrdSplitUpdated = value;
            }
        }
        #endregion

        public static bool IsReadyToBeUsed(PXGraph graph, string callerScreenID)
        {
            bool isSetupCompleted = PXSelect<FSSetup, Where<FSSetup.calendarID, IsNotNull>>.Select(graph).Count > 0;
            bool currentUserCanEnterTimeCards = TimeCardHelper.CanCurrentUserEnterTimeCards(graph, callerScreenID);

            return isSetupCompleted && currentUserCanEnterTimeCards;
        }

        public virtual void SkipTaxCalcAndSave()
        {
            var AppointmentEntryExternalTax = this.GetExtension<AppointmentEntryExternalTax>();

            if (AppointmentEntryExternalTax != null)
            {
                this.GetExtension<AppointmentEntryExternalTax>().SkipTaxCalcAndSave();
            }
            else
            {
                Save.Press();
            }
        }

        public virtual void SaveBeforeApplyAction(PXCache cache, FSAppointment row)
        {
            PXEntryStatus rowEntryStatus = cache.GetStatus(row);
            bool isInserted = cache.AllowInsert == true && rowEntryStatus == PXEntryStatus.Inserted;
            bool isUpdated = cache.AllowUpdate == true && rowEntryStatus == PXEntryStatus.Updated;

            if (isInserted || isUpdated)
            {
                SkipTaxCalcAndSave();
            }
        }

        public virtual void ChangeStatusSaveAndSkipExternalTaxCalc(FSAppointment fsAppointmentRow)
        {
            var AppointmentEntryExternalTax = this.GetExtension<AppointmentEntryExternalTax>();

            if (AppointmentEntryExternalTax != null)
            {
                bool previousValue = AppointmentEntryExternalTax.skipExternalTaxCalcOnSave;

                try
                {
                    AppointmentEntryExternalTax.skipExternalTaxCalcOnSave = true;
                    ForceUpdateCacheAndSave(AppointmentRecords.Cache, fsAppointmentRow);
                }
                finally
                {
                    AppointmentEntryExternalTax.skipExternalTaxCalcOnSave = previousValue;
                }
            }
            else
            {
                ForceUpdateCacheAndSave(AppointmentRecords.Cache, fsAppointmentRow);
            }
        }

        public AppointmentEntry()
            : base()
        {
            // Adding the start/complete/cancel buttons as part of the Action menu button
            FSSetup fsSetupRow = SetupRecord.Current;

            NeedRecalculateRouteStats = false;

            menuDetailActions.AddMenuAction(startItemLine);
            menuDetailActions.AddMenuAction(pauseItemLine);
            menuDetailActions.AddMenuAction(resumeItemLine);
            menuDetailActions.AddMenuAction(completeItemLine);
            menuDetailActions.AddMenuAction(cancelItemLine);

            menuStaffActions.AddMenuAction(startStaff);
            menuStaffActions.AddMenuAction(pauseStaff);
            menuStaffActions.AddMenuAction(resumeStaff);
            menuStaffActions.AddMenuAction(completeStaff);

            FieldUpdated.AddHandler(AppointmentRecords.Name,
                                    typeof(FSAppointment.scheduledDateTimeBegin).Name + PXDBDateAndTimeAttribute.DATE_FIELD_POSTFIX,
                                    FSAppointment_ScheduledDateTimeBegin_FieldUpdated);

            FieldUpdating.AddHandler(typeof(FSAppointment),
                                     typeof(FSAppointment.actualDateTimeBegin).Name + PXDBDateAndTimeAttribute.TIME_FIELD_POSTFIX,
                                     FSAppointment_ActualDateTimeBegin_Time_FieldUpdating);

            FieldUpdating.AddHandler(typeof(FSAppointment),
                                     typeof(FSAppointment.actualDateTimeEnd).Name + PXDBDateAndTimeAttribute.TIME_FIELD_POSTFIX,
                                     FSAppointment_ActualDateTimeEnd_Time_FieldUpdating);


            if (TimeCardHelper.CanCurrentUserEnterTimeCards(this, this.Accessinfo.ScreenID) == false)
            {
                if (IsExport || IsImport || System.Web.HttpContext.Current.Request.Form["__CALLBACKID"] != null)
                {
                    throw new PXException(PX.Objects.EP.Messages.MustBeEmployee);
                }
                else
                {
                    Redirector.Redirect(System.Web.HttpContext.Current,
                                        string.Format("~/Frames/Error.aspx?exceptionID={0}&typeID={1}",
                                                      EP.Messages.MustBeEmployee,
                                                      "error"));
                }
            }

            if (dummyGraph == null)
            {
                dummyGraph = new PXGraph();
            }

            PXUIFieldAttribute.SetDisplayName<FSAppointment.mapLatitude>(AppointmentRecords.Cache, TX.RouteLocationInfo.APPOINTMENT_LOCATION);
            PXUIFieldAttribute.SetDisplayName<FSAppointment.gPSLatitudeStart>(AppointmentRecords.Cache, TX.RouteLocationInfo.START_LOCATION);
            PXUIFieldAttribute.SetDisplayName<FSAppointment.gPSLatitudeComplete>(AppointmentRecords.Cache, TX.RouteLocationInfo.END_LOCATION);
            PXUIFieldAttribute.SetDisplayName<FSxService.actionType>(InventoryItemHelper.Cache, "Pickup/Deliver Items");
        }

        #region CacheAttached
        #region BAccount CacheAttached
        #region BAccount_AcctName
        [PXDBString(60, IsUnicode = true)]
        [PXUIField(DisplayName = "Account Name", Enabled = false)]
        protected virtual void BAccount_AcctName_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion

        #region ServiceOrder CacheAttached
        #region FSServiceOrder_CustomerID
        [PXDBInt]
        [PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
        [PXUIField(DisplayName = "Customer", Visibility = PXUIVisibility.SelectorVisible)]
        [PXRestrictor(typeof(Where<BAccountSelectorBase.status, IsNull,
                Or<BAccountSelectorBase.status, Equal<CustomerStatus.active>,
                Or<BAccountSelectorBase.status, Equal<CustomerStatus.oneTime>>>>),
                PX.Objects.AR.Messages.CustomerIsInStatus, typeof(BAccountSelectorBase.status))]
        [FSSelectorBAccountCustomerOrCombined]
        protected virtual void FSServiceOrder_CustomerID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region CustWorkOrderRefNbr
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "External Reference", Visible = false)]
        protected virtual void FSServiceOrder_CustWorkOrderRefNbr_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region CustPORefNbr
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Customer Order", Visible = false)]
        protected virtual void FSServiceOrder_CustPORefNbr_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region Priority
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Priority", Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
        protected virtual void FSServiceOrder_Priority_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region Severity
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Severity", Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
        protected virtual void FSServiceOrder_Severity_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region ProblemID
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Problem", Visible = false)]
        protected virtual void FSServiceOrder_ProblemID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region AssignedEmpID
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Supervisor", Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
        protected virtual void FSServiceOrder_AssignedEmpID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion

        #region FSBillingCycle_BillingCycleCD
        [PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">AAAAAAAAAAAAAAA")]
        [PXUIField(DisplayName = "Billing Cycle", Visibility = PXUIVisibility.SelectorVisible)]
        [PXSelector(typeof(FSBillingCycle.billingCycleCD))]
        [NormalizeWhiteSpace]
        protected virtual void FSBillingCycle_BillingCycleCD_CacheAttached(PXCache sender)
        {
        }
        #endregion

        #region ARPayment_CashAccountID
        [PXDBInt]
        [PXUIField(DisplayName = "Cash Account", Visibility = PXUIVisibility.Visible, Visible = false)]
        protected virtual void ARPayment_CashAccountID_CacheAttached(PXCache sender)
        {
        }
        #endregion

        #region FSAppointmentDetService CacheAttached
        [PopupMessage]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        protected virtual void FSAppointmentDet_InventoryID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion

        #region Actions

        #region InitialState
        public PXAutoAction<FSAppointment> initializeState;
        #endregion
        #region PutOnHold
        public PXAction<FSAppointment> putOnHold;
        [PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual IEnumerable PutOnHold(PXAdapter adapter) => adapter.Get();
        #endregion
        #region ReleaseFromHold
        public PXAction<FSAppointment> releaseFromHold;
        [PXButton(CommitChanges = true), PXUIField(DisplayName = "Remove Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual IEnumerable ReleaseFromHold(PXAdapter adapter) => adapter.Get();
        #endregion
        #region Cancel
        [PXCancelButton]
        [PXUIField(DisplayName = ActionsMessages.Cancel, MapEnableRights = PXCacheRights.Select)]
        protected new virtual IEnumerable Cancel(PXAdapter a)
        {
            InvoiceRecords.Cache.ClearQueryCache();
            return (new PXCancel<FSAppointment>(this, "Cancel")).Press(a);
        }
        #endregion
        #region StartTravel
        public PXAction<FSAppointment> startTravel;
        [PXUIField(DisplayName = "Depart", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable StartTravel(PXAdapter adapter)
        {
            SkipTaxCalcAndSave();
            if (LogActionFilter.Current != null)
            {
                if (LogActionFilter.View.Answer != WebDialogResult.OK)
                {
                    SetLogActionPanelDefaults(ID.LogActions.START, FSLogActionFilter.type.Values.Travel);
                }

                WebDialogResult result = LogActionFilter.AskExt(true);

                if (result == WebDialogResult.OK)
                {
                    if (LogActionFilter.Current.DetLineRef == null
                        && ServiceOrderTypeSelected.Current.DfltBillableTravelItem != null)
                    {
                        string travelItemLineRef = GetItemLineRef(this, AppointmentRecords.Current.AppointmentID, true);

                        if (travelItemLineRef == null)
                        {
                            FSAppointmentDet fsAppointmentDetRow = (FSAppointmentDet)AppointmentDetails.Cache.CreateInstance();
                            fsAppointmentDetRow.LineType = ID.LineType_ALL.SERVICE;
                            fsAppointmentDetRow.InventoryID = ServiceOrderTypeSelected.Current.DfltBillableTravelItem;
                            fsAppointmentDetRow = AppointmentDetails.Insert(fsAppointmentDetRow);

                            LogActionFilter.Current.DetLineRef = fsAppointmentDetRow.LineRef;
                        }
                        else
                        {
                            LogActionFilter.Current.DetLineRef = travelItemLineRef;
                        }
                    }

                    RunLogAction(LogActionFilter.Current.Action, LogActionFilter.Current.Type, null, null, null);
                }
            }

            return adapter.Get();
        }
        #endregion
        #region CompleteTravel
        public PXAction<FSAppointment> completeTravel;
        [PXUIField(DisplayName = "Arrive", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable CompleteTravel(PXAdapter adapter)
        {
            SkipTaxCalcAndSave();
            if (LogActionFilter.Current != null)
            {
                if (LogActionFilter.View.Answer != WebDialogResult.OK)
                {
                    SetLogActionPanelDefaults(ID.LogActions.COMPLETE, FSAppointmentLog.itemType.Values.Travel);
                }

                WebDialogResult result = LogActionFilter.AskExt(true);

                if (result == WebDialogResult.OK)
                {
                    RunLogAction(LogActionFilter.Current.Action, LogActionFilter.Current.Type, null, null, null);
                }
            }

            return adapter.Get();
        }
        #endregion
        #region ViewDirectionOnMap
        public PXAction<FSAppointment> viewDirectionOnMap;
        [PXUIField(DisplayName = CR.Messages.ViewOnMap, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual void ViewDirectionOnMap()
        {
            var address = ServiceOrder_Address.SelectSingle();

            if (address != null)
            {
                CR.BAccountUtility.ViewOnMap<FSAddress, FSAddress.countryID>(address);
            }
        }
        #endregion
        #region ViewStartGPSOnMap
        public PXAction<FSAppointment> viewStartGPSOnMap;
        [PXUIField(DisplayName = "View on Map", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable ViewStartGPSOnMap(PXAdapter adapter)
        {
            if (AppointmentSelected.Current != null && AppointmentSelected.Current.SOID != null)
            {
                var googleMap = new PX.Data.GoogleMapLatLongRedirector();

                googleMap.ShowAddressByLocation(AppointmentSelected.Current.GPSLatitudeStart, AppointmentSelected.Current.GPSLongitudeStart);
            }

            return adapter.Get();
        }
        #endregion
        #region ViewCompleteGPSOnMap
        public PXAction<FSAppointment> viewCompleteGPSOnMap;
        [PXUIField(DisplayName = "View on Map", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable ViewCompleteGPSOnMap(PXAdapter adapter)
        {
            if (AppointmentSelected.Current != null && AppointmentSelected.Current.SOID != null)
            {
                var googleMap = new GoogleMapLatLongRedirector();

                googleMap.ShowAddressByLocation(AppointmentSelected.Current.GPSLatitudeComplete, AppointmentSelected.Current.GPSLongitudeComplete);
            }

            return adapter.Get();
        }
        #endregion

        #region CloneAppointment
        public PXAction<FSAppointment> cloneAppointment;
        [PXUIField(DisplayName = "Clone", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Cancel)]
        public virtual IEnumerable CloneAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);

                if (!string.IsNullOrEmpty(list[0].RefNbr))
                {
                    if (ServiceOrderRelated.Current != null
                            && ServiceOrderRelated.Current.Completed == true)
                    {
                        ServiceOrderEntry graphServiceOrderEntry = GetServiceOrderEntryGraph(true);

                        graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords
                                    .Search<FSServiceOrder.refNbr>(ServiceOrderRelated.Current.RefNbr, ServiceOrderRelated.Current.SrvOrdType);

                        graphServiceOrderEntry.reopenOrder.Press();
                    }

                    CloneAppointmentProcess gCloneAppt = PXGraph.CreateInstance<CloneAppointmentProcess>();

                    gCloneAppt.Filter.Current.SrvOrdType = list[0].SrvOrdType;
                    gCloneAppt.Filter.Current.RefNbr = list[0].RefNbr;

                    gCloneAppt.AppointmentSelected.Current = gCloneAppt.AppointmentSelected.Select();

                    gCloneAppt.cancel.Press();

                    throw new PXRedirectRequiredException(gCloneAppt, null);
                }
            }

            return list;
        }
        #endregion

        #region MenuActions
        public PXMenuAction<FSAppointment> menuActions;
        [PXButton(MenuAutoOpen = true, SpecialType = PXSpecialButtonType.ActionsFolder)]
        [PXUIField(DisplayName = "Actions")]
        public virtual IEnumerable MenuActions(PXAdapter adapter)
        {
            return adapter.Get();
        }

        public PXAction<FSAppointment> report;
        [PXButton(SpecialType = PXSpecialButtonType.ReportsFolder, MenuAutoOpen = true)]
        [PXUIField(DisplayName = "Reports")]
        public virtual IEnumerable Report(PXAdapter adapter,
            [PXString(8, InputMask = "CC.CC.CC.CC")]
            string reportID
            )
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!String.IsNullOrEmpty(reportID))
            {
                Save.Press();
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                string actualReportID = null;

                PXReportRequiredException ex = null;
                Dictionary<PX.SM.PrintSettings, PXReportRequiredException> reportsToPrint = new Dictionary<PX.SM.PrintSettings, PXReportRequiredException>();

                foreach (FSAppointment appt in list)
                {
                    parameters = new Dictionary<string, string>();
                    parameters["FSAppointment.SrvOrdType"] = appt.SrvOrdType;
                    parameters["FSAppointment.RefNbr"] = appt.RefNbr;

                    object cstmr = PXSelectorAttribute.Select<FSServiceOrder.customerID>(ServiceOrderRelated.Cache, ServiceOrderRelated.Current);
                    actualReportID = new NotificationUtility(this).SearchReport(SONotificationSource.Customer, cstmr, reportID, appt.BranchID);
                    ex = PXReportRequiredException.CombineReport(ex, actualReportID, parameters);

                    reportsToPrint = PX.SM.SMPrintJobMaint.AssignPrintJobToPrinter(reportsToPrint, parameters, adapter, new NotificationUtility(this).SearchPrinter, SONotificationSource.Customer, reportID, actualReportID, appt.BranchID);
                }

                if (ex != null)
                {
                    PX.SM.SMPrintJobMaint.CreatePrintJobGroups(reportsToPrint);

                    throw ex;
                }
            }
            return adapter.Get();
        }

        public PXMenuAction<FSAppointment> menuDetailActions;
        [PXButton(MenuAutoOpen = true)]
        [PXUIField(DisplayName = "Actions")]
        public virtual IEnumerable MenuDetailActions(PXAdapter adapter)
        {
            return adapter.Get();
        }

        public PXMenuAction<FSAppointment> menuStaffActions;
        [PXButton(MenuAutoOpen = true)]
        [PXUIField(DisplayName = "Actions")]
        public virtual IEnumerable MenuStaffActions(PXAdapter adapter)
        {
            return adapter.Get();
        }
        #endregion

        #region Validate Address
        public PXAction<FSAppointment> validateAddress;
        [PXUIField(DisplayName = "Validate Address", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, FieldClass = CS.Messages.ValidateAddress)]
        [PXButton]
        public virtual IEnumerable ValidateAddress(PXAdapter adapter)
        {
            foreach (FSAppointment current in adapter.Get<FSAppointment>())
            {
                if (current != null)
                {
                    FSAddress address = this.ServiceOrder_Address.Select();
                    if (address != null && address.IsDefaultAddress == false && address.IsValidated == false)
                    {
                        PXAddressValidator.Validate<FSAddress>(this, address, true);
                    }
                }
                yield return current;
            }
        }
        #endregion

        #region StartAppointment
        public PXAction<FSAppointment> startAppointment;
        [PXUIField(DisplayName = "Start", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable StartAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (list.Count > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);

                foreach (FSAppointment fsAppointmentRow in list)
                {
                    FSAppointment apptRow = fsAppointmentRow;

                    try
                    {
                        AppointmentRecords.Current = apptRow;

                        int serviceDetailCount = AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(x => x.IsService).Count();

                        if (serviceDetailCount == 0)
                        {
                            throw new PXException(TX.Error.APPOINTMENT_START_VALIDATE_SERVICE, PXErrorLevel.Error);
                        }

                        using (var ts = new PXTransactionScope())
                        {
                            apptRow.HandleManuallyActualTime = false;
                            apptRow = AppointmentRecords.Update(apptRow);

                            ForceAppointmentDetActualFieldsUpdate(false);

                            try
                            {
                                SkipManualTimeFlagUpdate = true;

                                DateTime? dateTimeBegin = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                                if (ServiceOrderTypeSelected.Current?.OnStartApptSetStartTimeInHeader == true || this.IsMobile == true)
                                {
                                    SetHeaderActualDateTimeBegin(AppointmentRecords.Cache, apptRow, dateTimeBegin);
                                }
                                else
                                {
                                    AppointmentRecords.Cache.SetDefaultExt<FSAppointment.executionDate>(apptRow);
                                }

                                apptRow = AppointmentRecords.Update(apptRow);

                                if (ServiceOrderTypeSelected.Current?.OnStartApptStartUnassignedStaff == true)
                                {
                                    IEnumerable<FSAppointmentStaffExtItemLine> createLogitems = PXSelect<FSAppointmentStaffExtItemLine,
                                                                                         Where<
                                                                                             FSAppointmentStaffExtItemLine.detLineRef, IsNull,
                                                                                         And<
                                                                                             FSAppointmentStaffExtItemLine.docID, Equal<Required<FSAppointmentStaffExtItemLine.docID>>>>>
                                                                                         .Select(this, apptRow.AppointmentID)
                                                                                         .RowCast<FSAppointmentStaffExtItemLine>();

                                    StartStaffAction(createLogitems, dateTimeBegin);
                                }

                                if (ServiceOrderTypeSelected.Current?.OnStartApptStartServiceAndStaff == true)
                                {
                                    IEnumerable<FSDetailFSLogAction> createLogitems = PXSelectJoin<FSDetailFSLogAction,
                                                                                      InnerJoin<InventoryItem,
                                                                                      On<
                                                                                          InventoryItem.inventoryID, Equal<FSDetailFSLogAction.inventoryID>>>,
                                                                                      Where<
                                                                                          FSDetailFSLogAction.appointmentID, Equal<Required<FSDetailFSLogAction.appointmentID>>,
                                                                                      And<
                                                                                          FSxService.isTravelItem, Equal<False>>>>
                                                                                      .Select(this, apptRow.AppointmentID)
                                                                                      .RowCast<FSDetailFSLogAction>();

                                    StartServiceBasedOnAssignmentAction(createLogitems, dateTimeBegin);
                                }

                                if (ServiceOrderTypeSelected.Current?.OnStartApptSetNotStartItemInProcess == true)
                                {
                                    foreach (FSAppointmentDet apptDet in AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                                                           .Where(r => r.Status != ID.Status_AppointmentDet.CANCELED
                                                                                                  && r.Status != ID.Status_AppointmentDet.WaitingForPO
                                                                                                  && r.Status != ID.Status_AppointmentDet.RequestForPO
                                                                                                  && r.IsTravelItem == false
                                                                                                  && r.IsLinkedItem == false))
                                    {
                                        ChangeItemLineStatus(apptDet, ID.Status_AppointmentDet.IN_PROCESS);
                                    }
                                }

                                if (ServiceOrderTypeSelected.Current?.OnCompleteApptSetEndTimeInHeader == false
                                        && ServiceOrderTypeSelected.Current?.SetTimeInHeaderBasedOnLog == false)
                                {
                                    AppointmentSelected.Cache.SetValueExtIfDifferent<FSAppointment.handleManuallyActualTime>(apptRow, true);
                                }
                            }
                            finally
                            {
                                SkipManualTimeFlagUpdate = false;
                            }

                            if (IsMobile == true
                                    && SetupRecord.Current != null
                                        && SetupRecord.Current.TrackAppointmentLocation == true)
                            {
                                FSGPSTrackingHistory lastLocation = PXSelectJoin<FSGPSTrackingHistory,
                                                                    InnerJoin<FSGPSTrackingRequest,
                                                                    On<
                                                                        FSGPSTrackingRequest.trackingID, Equal<FSGPSTrackingHistory.trackingID>>>,
                                                                    Where<
                                                                        FSGPSTrackingRequest.userName, Equal<Required<AccessInfo.userName>>,
                                                                    And<
                                                                        FSGPSTrackingHistory.executionDate, GreaterEqual<Required<FSGPSTrackingHistory.executionDate>>>>,
                                                                    OrderBy<
                                                                        Desc<FSGPSTrackingHistory.executionDate>>>
                                                                    .Select(this, Accessinfo.UserName, apptRow.ExecutionDate)
                                                                    .FirstOrDefault();

                                if (lastLocation != null
                                        && lastLocation.Longitude != null
                                            && lastLocation.Latitude != null)
                                {
                                    apptRow.GPSLatitudeStart = lastLocation.Latitude;
                                    apptRow.GPSLongitudeStart = lastLocation.Longitude;
                                }
                            }

                            AppointmentRecords.Update(apptRow);
                            SkipTaxCalcAndSave();

                            ts.Complete();
                            RefreshServiceOrderRelated();
                        }

                        LoadServiceOrderRelatedAfterStatusChange(apptRow);
                    }
                    finally
                    {
                        apptRow.StartActionRunning = false;
                    }
                }
            }

            return list;
        }
        #endregion
        #region PauseAppointment
        public PXAction<FSAppointment> pauseAppointment;
        [PXUIField(DisplayName = "Pause", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable PauseAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();
            if (list.Count > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);

                foreach (FSAppointment fsAppointmentRow in list)
                {
                    FSAppointment apptRow = fsAppointmentRow;

                    try
                    {
                        AppointmentRecords.Current = apptRow;

                        using (var ts = new PXTransactionScope())
                        {
                            List<FSAppointmentLog> logs = PXSelect<FSAppointmentLog,
                                                          Where<
                                                              FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                                                              And<FSAppointmentLog.status, Equal<FSAppointmentLog.status.InProcess>,
                                                              And<FSAppointmentLog.itemType, NotEqual<FSAppointmentLog.itemType.Values.travel>>>>>
                                                          .Select(this, AppointmentRecords.Current?.AppointmentID)
                                                          .RowCast<FSAppointmentLog>()
                                                          .ToList();

                    DateTime? dateTimeEnd = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
                            CompletePauseMultipleLogs(dateTimeEnd, ID.Status_AppointmentDet.IN_PROCESS, ID.Status_Log.PAUSED, false, logs);

                            SkipTaxCalcAndSave();
                            ts.Complete();
                            RefreshServiceOrderRelated();
                        }

                        LoadServiceOrderRelatedAfterStatusChange(apptRow);
                    }
                    finally
                    {
                        apptRow.PauseActionRunning = false;
                    }
                }
            }

            return list;
        }
        #endregion
        #region ResumeAppointment
        public PXAction<FSAppointment> resumeAppointment;
        [PXUIField(DisplayName = "Resume", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable ResumeAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (list.Count > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);
                foreach (FSAppointment fsAppointmentRow in list)
                {
                    FSAppointment apptRow = fsAppointmentRow;
                    try
                    {

                        AppointmentRecords.Current = apptRow;

                        using (var ts = new PXTransactionScope())
                        {
                            PXSelectBase<FSAppointmentLog> logs = null;
                            object[] logSelectArgs = null;

                            logs = new PXSelect<FSAppointmentLog,
                                       Where<
                                           FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                                           And<FSAppointmentLog.status, Equal<FSAppointmentLog.status.Paused>>>>(this);

                            logSelectArgs = new object[] { apptRow.AppointmentID };

                            ResumeMultipleLogs(logs, logSelectArgs);
                            SkipTaxCalcAndSave();

                            ts.Complete();
                            RefreshServiceOrderRelated();
                        }

                        LoadServiceOrderRelatedAfterStatusChange(apptRow);
                    }
                    finally
                    {
                        apptRow.ResumeActionRunning = false;
                    }
                }
            }

            return list;
        }
        #endregion
        #region CancelAppointment
        public PXAction<FSAppointment> cancelAppointment;
        [PXUIField(DisplayName = "Cancel", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable CancelAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();
            if (list.Count > 0)
            {
                foreach (FSAppointment fsAppointmentRow in list)
                {
                    FSAppointment apptRow = fsAppointmentRow;

                    try
                    {
                        AppointmentRecords.Current = apptRow;

                        FSAppointmentDet errorApptDet = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                                          .Where(r => r.Status != ID.Status_AppointmentDet.CANCELED && CanItemLineBeCanceled(r) == false)
                                                                          .FirstOrDefault();

                        if (errorApptDet != null)
                        {
                            throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_CANCELED,
                                                  PXLocalizer.Localize(cancelItemLine.GetCaption()),
                                                  GetLineDisplayHint(this, errorApptDet.LineRef, errorApptDet.TranDesc, errorApptDet.InventoryID),
                                                  PXStringListAttribute.GetLocalizedLabel<FSAppointment.status>(AppointmentDetails.Cache, errorApptDet, errorApptDet.Status));
                        }

                        FSAppointmentLog log = LogRecords.Select().RowCast<FSAppointmentLog>()
                                                         .Where(r => r.Status == ID.Status_Log.IN_PROCESS || r.Status == ID.Status_Log.COMPLETED)
                                                         .FirstOrDefault();

                        if (log != null)
                        {
                            throw new PXException(TX.Error.CANNOT_CANCEL_APPOINTMENT_WITH_LOG);
                        }

                        string soDetFinalStatus = ID.Status_AppointmentDet.CANCELED;

                        string soFinalStatus = ServiceOrderRelated.Current.Status;

                        if (WebDialogResult.Yes == AppointmentRecords.Ask(
                                                    TX.WebDialogTitles.ConfirmCancelAppointmentDetails,
                                                    TX.Messages.ConfirmCancelAppointmentDetails,
                                                    MessageButtons.YesNo))
                        {
                            soDetFinalStatus = ID.Status_AppointmentDet.NOT_PERFORMED;
                        }
                        else
                        {
                            soFinalStatus = GetFinalServiceOrderStatus(ServiceOrderRelated.Current, apptRow);
                        }

                        using (var ts = new PXTransactionScope())
                        {
                            foreach (FSAppointmentDet apptDet in AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(r => r.Status != ID.Status_AppointmentDet.CANCELED
                                                                                                                                    && r.IsLinkedItem == false))
                            {
                                ChangeItemLineStatus(apptDet, soDetFinalStatus);
                            }

                            apptRow = (FSAppointment)AppointmentRecords.Cache.CreateCopy(apptRow);
                            apptRow.BillServiceContractID = null;
                            AppointmentRecords.Cache.Update(apptRow);

                            updateContractPeriod = true;

                            SkipTaxCalcAndSave();

                            if (!string.IsNullOrEmpty(soFinalStatus))
                            {
                                SetLatestServiceOrderStatusBaseOnAppointmentStatus(ServiceOrderRelated.Current, soFinalStatus);
                            }

                            ts.Complete();
                            RefreshServiceOrderRelated();
                        }

                        LoadServiceOrderRelatedAfterStatusChange(apptRow);
                    }
                    finally
                    {
                        apptRow.CancelActionRunning = false;
                    }
                }
            }

            return list;
        }

        #endregion
        #region ReopenAppointment
        public PXAction<FSAppointment> reopenAppointment;
        [PXUIField(DisplayName = "Reopen", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable ReopenAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();
            if (list.Count > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);
                foreach (FSAppointment fsAppointmentRow in list)
                {
                    FSAppointment apptRow = fsAppointmentRow;
                    try
                    {
                        AppointmentRecords.Current = apptRow;

                        if (fsAppointmentRow.Canceled == true)
                        {
                            AppointmentRecords.Cache.AllowUpdate = true;
                        }

                        using (var ts = new PXTransactionScope())
                        {
                            AppointmentRecords.Cache.SetDefaultExt<FSAppointment.billContractPeriodID>(apptRow);

                            ForceAppointmentDetActualFieldsUpdate(true);

                            apptRow = (FSAppointment)AppointmentRecords.Cache.CreateCopy(apptRow);
                            apptRow.CuryLineTotal = apptRow.CuryEstimatedLineTotal;

                            SetServiceOrderStatusFromAppointment(ServiceOrderRelated.Current, apptRow, ActionButton.ReOpenAppointment);
                            ClearAppointmentLog();

                            apptRow.HandleManuallyActualTime = false;
                            apptRow.ActualDateTimeBegin = null;
                            apptRow.ActualDateTimeEnd = null;
                            AppointmentRecords.Cache.Update(apptRow);

                            SkipTaxCalcAndSave();
                            LogRecords.Cache.Clear();

                            ts.Complete();
                            RefreshServiceOrderRelated();
                        }

                        LoadServiceOrderRelatedAfterStatusChange(apptRow);
                    }
                    finally
                    {
                        apptRow.ReopenActionRunning = false;
                    }
                }
            }

            return list;
        }
        #endregion
        #region CompleteAppointment
        public PXAction<FSAppointment> completeAppointment;
        [PXUIField(DisplayName = "Complete", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable CompleteAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (list.Count > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);
                foreach (FSAppointment fsAppointmentRow in list)
                {
                    FSAppointment apptRow = fsAppointmentRow;

                    try
                    {
                        AppointmentRecords.Current = apptRow;

                        if (ServiceOrderTypeSelected.Current?.OnCompleteApptRequireLog == true)
                        {
                            VerifyOnCompleteApptRequireLog(AppointmentSelected.Cache);
                        }

                        DateTime? dateTimeEnd = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                        if (ServiceOrderTypeSelected.Current?.OnCompleteApptSetEndTimeInHeader == true)
                        {
                            SetHeaderActualDateTimeEnd(AppointmentSelected.Cache, AppointmentRecords.Current, dateTimeEnd);
                        }
                        else if (this.IsMobile)
                        {
                            AppointmentSelected.Cache.SetValueExtIfDifferent<FSAppointment.actualDateTimeEnd>(apptRow, PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now));
                        }

                        if (IsMobile)
                        {
                            ValidateSignatureFields(AppointmentSelected.Cache,
                                                    AppointmentRecords.Current,
                                                    GetRequireCustomerSignature(this, ServiceOrderTypeSelected.Current, ServiceOrderRelated.Current));
                        }

                        int serviceDetailCount = AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(x => x.IsService).Count();

                        if (serviceDetailCount == 0)
                        {
                            throw new PXException(TX.Error.APPOINTMENT_COMPLETE_VALIDATE_SERVICE, PXErrorLevel.Error);
                        }

                        PXSetPropertyException apptDetException = null;

                        var errorApptDetRows = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                                 .Where(r => r.IsTravelItem == false
                                                                             && ((r.Status == ID.Status_AppointmentDet.NOT_STARTED
                                                                                    && ServiceOrderTypeSelected.Current?.OnCompleteApptSetNotStartedItemsAs == FSSrvOrdType.onCompleteApptSetNotStartedItemsAs.Values.DoNothing)
                                                                                  || (r.Status == ID.Status_AppointmentDet.IN_PROCESS && ServiceOrderTypeSelected.Current?.OnCompleteApptSetInProcessItemsAs == FSSrvOrdType.onCompleteApptSetInProcessItemsAs.Values.DoNothing)));

                        foreach (FSAppointmentDet errorApptDet in errorApptDetRows)
                        {
                            if (apptDetException == null)
                            {
                                apptDetException = new PXSetPropertyException(TX.Error.CANNOT_COMPLETE_APPOINTMENT_WITH_NOTSTARTED_INPROCESS_ITEM_LINES);
                            }

                            AppointmentDetails.Cache.RaiseExceptionHandling<FSAppointmentDet.status>(errorApptDet, errorApptDet.Status, apptDetException);
                        }

                        var errorWaitingPORows = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                                    .Where(r => r.EnablePO == true
                                                                            && r.POCompleted != true
                                                                            && r.Status == ListField_Status_AppointmentDet.WaitingForPO);

                        foreach (FSAppointmentDet errorApptDet in errorWaitingPORows)
                        {
                            if (apptDetException == null)
                            {
                                apptDetException = new PXSetPropertyException(TX.Error.CannotCompleteAppointmentWithWaitingPOLines);
                            }

                            AppointmentDetails.Cache.RaiseExceptionHandling<FSAppointmentDet.status>(errorApptDet, errorApptDet.Status, apptDetException);
                        }

                        PXSetPropertyException logException = null;

                        var errorLogRows = LogRecords.Select().RowCast<FSAppointmentLog>()
                                                     .Where(r => r.Travel == false
                                                                    && r.Status == ID.Status_Log.IN_PROCESS
                                                                        && ServiceOrderTypeSelected.Current?.OnCompleteApptSetInProcessItemsAs == FSSrvOrdType.onCompleteApptSetInProcessItemsAs.Values.DoNothing);

                        foreach (FSAppointmentLog errorLog in errorLogRows)
                        {
                            if (logException == null)
                            {
                                logException = new PXSetPropertyException(TX.Error.CANNOT_COMPLETE_APPOINTMENT_WITH_INPROCESS_LOG_LINES);
                            }

                            LogRecords.Cache.RaiseExceptionHandling<FSAppointmentLog.status>(errorLog, errorLog.Status, logException);
                        }

                        if (apptDetException != null || logException != null)
                        {
                            throw new PXException(apptDetException?.Message + Environment.NewLine + logException?.Message);
                        }

                        int rowsAffected = 0;

                        if (ServiceOrderTypeSelected.Current?.OnCompleteApptSetInProcessItemsAs != FSSrvOrdType.onCompleteApptSetInProcessItemsAs.Values.DoNothing)
                        {
                            List<FSAppointmentLog> logs = PXSelect<FSAppointmentLog,
                                                        Where<
                                                            FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                                                            And<FSAppointmentLog.itemType, NotEqual<ListField_LogAction_Type.travel>,
                                                            And<
                                                                Where<
                                                                    FSAppointmentLog.status, Equal<FSAppointmentLog.status.InProcess>,
                                                                    Or<FSAppointmentLog.status, Equal<FSAppointmentLog.status.Paused>>>>>>>
                                                        .Select(this, AppointmentRecords.Current?.AppointmentID)
                                                        .RowCast<FSAppointmentLog>()
                                                        .ToList();

                            string newApptDetStatus = ServiceOrderTypeSelected.Current?.OnCompleteApptSetInProcessItemsAs == FSSrvOrdType.onCompleteApptSetInProcessItemsAs.Values.Completed
                                ? ID.Status_AppointmentDet.COMPLETED : ID.Status_AppointmentDet.NOT_FINISHED;

                            rowsAffected += CompletePauseMultipleLogs(dateTimeEnd, newApptDetStatus, ID.Status_Log.COMPLETED, true, logs);

                            var itemLines = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                              .Where(r => r.IsTravelItem == false && r.Status == ID.Status_AppointmentDet.IN_PROCESS);

                            SplitAppoinmentLogLinesByDays();

                            foreach (FSAppointmentDet itemLine in itemLines)
                            {
                                ChangeItemLineStatus(itemLine, newApptDetStatus);
                                rowsAffected++;
                            }
                        }

                        if (ServiceOrderTypeSelected.Current?.OnCompleteApptSetNotStartedItemsAs != FSSrvOrdType.onCompleteApptSetNotStartedItemsAs.Values.DoNothing)
                        {
                            string newApptDetStatus = ServiceOrderTypeSelected.Current?.OnCompleteApptSetNotStartedItemsAs == FSSrvOrdType.onCompleteApptSetNotStartedItemsAs.Values.Completed
                                ? ID.Status_AppointmentDet.COMPLETED : ID.Status_AppointmentDet.NOT_PERFORMED;

                            var itemLines = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                              .Where(r => r.Status == ID.Status_AppointmentDet.NOT_STARTED
                                                                          && r.IsTravelItem == false
                                                                          && r.IsLinkedItem == false);

                            foreach (FSAppointmentDet itemLine in itemLines)
                            {
                                ChangeItemLineStatus(itemLine, newApptDetStatus);
                                rowsAffected++;
                            }
                        }

                        // This validation should be after the log completion because it may update actualDateTimeEnd
                        if (ActualDateAndTimeValidation(AppointmentRecords.Current) == false)
                        {
                            throw new PXException(TX.Error.ACTUAL_DATES_APPOINTMENT_MISSING);
                        }

                        if (IsMobile)
                        {
                            ValidateSignatureFields(AppointmentSelected.Cache,
                                                    AppointmentRecords.Current,
                                                    GetRequireCustomerSignature(this, ServiceOrderTypeSelected.Current, ServiceOrderRelated.Current));
                        }

                        apptRow = AppointmentRecords.Current;

                        if (rowsAffected > 0)
                        {
                            SkipTaxCalcAndSave();
                        }

                        using (var ts = new PXTransactionScope())
                        {
                            SetServiceOrderStatusFromAppointment(ServiceOrderRelated.Current, fsAppointmentRow, ActionButton.CompleteAppointment);

                            if (ServiceOrderRelated.Current.SourceType == ID.SourceType_ServiceOrder.SALES_ORDER)
                            {
                                UpdateSalesOrderByCompletingAppointment(this, ServiceOrderRelated.Current.SourceDocType, ServiceOrderRelated.Current.SourceRefNbr);
                            }

                            if (IsMobile == true
                                    && SetupRecord.Current != null
                                        && SetupRecord.Current.TrackAppointmentLocation == true)
                            {
                                FSGPSTrackingHistory lastLocation = PXSelectJoin<FSGPSTrackingHistory,
                                                                    InnerJoin<FSGPSTrackingRequest,
                                                                    On<
                                                                        FSGPSTrackingRequest.trackingID, Equal<FSGPSTrackingHistory.trackingID>>>,
                                                                    Where<
                                                                        FSGPSTrackingRequest.userName, Equal<Required<AccessInfo.userName>>,
                                                                    And<
                                                                        FSGPSTrackingHistory.executionDate, GreaterEqual<Required<FSGPSTrackingHistory.executionDate>>>>,
                                                                    OrderBy<
                                                                        Desc<FSGPSTrackingHistory.executionDate>>>
                                                                    .Select(this, Accessinfo.UserName, AppointmentRecords.Current.ExecutionDate)
                                                                    .FirstOrDefault();

                                if (lastLocation != null
                                        && lastLocation.Longitude != null
                                            && lastLocation.Latitude != null)
                                {
                                    apptRow.GPSLatitudeComplete = lastLocation.Latitude;
                                    apptRow.GPSLongitudeComplete = lastLocation.Longitude;
                                }
                            }

                            CalculateCosts();
                            SkipTaxCalcAndSave();

                            ts.Complete();
                            RefreshServiceOrderRelated();
                        }

                        LoadServiceOrderRelatedAfterStatusChange(apptRow);
                    }
                    finally
                    {
                        apptRow.CompleteActionRunning = false;
                    }
                }
            }

            return list;
        }
        #endregion
        #region CloseAppointment
        public PXAction<FSAppointment> closeAppointment;
        [PXUIField(DisplayName = "Close", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable CloseAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();
            List<FSAppointment> listToReturn = new List<FSAppointment>();

            if (list.Count > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);
                foreach (FSAppointment fsAppointmentRow in list)
                {
                    FSAppointment apptRow = fsAppointmentRow;
                    apptRow = AppointmentRecords.Current = AppointmentRecords.Update(apptRow);

                    try
                    {
                        if (apptRow.TimeRegistered == false && ServiceOrderTypeSelected.Current.RequireTimeApprovalToInvoice == true)
                        {
                            throw new PXException(TX.Error.CANNOT_CLOSED_APPOINTMENT_BECAUSE_TIME_REGISTERED_OR_ACTUAL_TIMES);
                        }

                        using (var ts = new PXTransactionScope())
                        {
                            updateContractPeriod = true;
                            SetServiceOrderStatusFromAppointment(ServiceOrderRelated.Current, apptRow, ActionButton.CloseAppointment);

                            SkipTaxCalcAndSave();
                            ts.Complete();
                            RefreshServiceOrderRelated();
                        }

                        LoadServiceOrderRelatedAfterStatusChange(apptRow);
                        listToReturn.Add(apptRow);
                    }
                    finally 
                    {
                        apptRow.CloseActionRunning = false;
                    }
                }
            }

            return listToReturn;
        }
        #endregion
        #region Unclose Appointment
        public PXAction<FSAppointment> uncloseAppointment;
        [PXUIField(DisplayName = "Unclose", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable UncloseAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();
            if (list.Count > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);

                foreach (FSAppointment fsAppointmentRow in list)
                {
                    FSAppointment apptRow = fsAppointmentRow;
                    AppointmentRecords.Current = apptRow;
                    try
                    {
                        apptRow.UserConfirmedUnclosing = true;

                        if (adapter.MassProcess == false)
                        {
                            if (WebDialogResult.Yes == AppointmentRecords.Ask(TX.WebDialogTitles.CONFIRM_APPOINTMENT_UNCLOSING,
                                                                              UncloseDialogMessage,
                                                                              MessageButtons.YesNo))
                            {
                                apptRow.UserConfirmedUnclosing = true;
                            }
                            else
                            {
                                apptRow.UserConfirmedUnclosing = false;
                            }
                        }

                        if (apptRow.UserConfirmedUnclosing == true)
                        {
                            using (var ts = new PXTransactionScope())
                            {
                                apptRow = AppointmentRecords.Update(apptRow);
                                SetServiceOrderStatusFromAppointment(ServiceOrderRelated.Current, apptRow, ActionButton.UnCloseAppointment);

                                updateContractPeriod = true;

                                SkipTaxCalcAndSave();
                                ts.Complete();
                                RefreshServiceOrderRelated();
                            }
                        }
                    }
                    finally
                    {
                        apptRow.UnCloseActionRunning = false;
                    }
                }
            }

            return list;
        }
        #endregion
        #region InvoiceAppointment
        public PXAction<FSAppointment> invoiceAppointment;
        [PXButton]
        [PXUIField(DisplayName = "Run Billing", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public IEnumerable InvoiceAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();
            List<AppointmentToPost> rows = new List<AppointmentToPost>();

            if (!adapter.MassProcess)
            {
                SaveWithRecalculateExternalTaxesSync();
            }

            foreach (FSAppointment fsAppointmentRow in list)
            {
                PXLongOperation.StartOperation(
                this,
                delegate ()
                {
                    SetServiceOrderStatusFromAppointment(ServiceOrderRelated.Current, fsAppointmentRow, ActionButton.InvoiceAppointment);

                    CreateInvoiceByAppointmentPost graphCreateInvoiceByAppointmentPost = PXGraph.CreateInstance<CreateInvoiceByAppointmentPost>();
                    graphCreateInvoiceByAppointmentPost.Filter.Current.PostTo = ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.ACCOUNTS_RECEIVABLE_MODULE ? ID.Batch_PostTo.AR_AP : ServiceOrderTypeSelected.Current.PostTo;
                    graphCreateInvoiceByAppointmentPost.Filter.Current.IgnoreBillingCycles = true;
                    graphCreateInvoiceByAppointmentPost.Filter.Current.BranchID = fsAppointmentRow.BranchID;
                    graphCreateInvoiceByAppointmentPost.Filter.Current.LoadData = true;
                    graphCreateInvoiceByAppointmentPost.Filter.Current.IsGenerateInvoiceScreen = false;

                    if (fsAppointmentRow.ActualDateTimeEnd > Accessinfo.BusinessDate)
                    {
                        graphCreateInvoiceByAppointmentPost.Filter.Current.UpToDate = fsAppointmentRow.ActualDateTimeEnd;
                        graphCreateInvoiceByAppointmentPost.Filter.Current.InvoiceDate = fsAppointmentRow.ActualDateTimeEnd;
                    }

                    graphCreateInvoiceByAppointmentPost.Filter.Insert(graphCreateInvoiceByAppointmentPost.Filter.Current);

                    AppointmentToPost appointmentToPostRow = graphCreateInvoiceByAppointmentPost.PostLines.Current =
                                graphCreateInvoiceByAppointmentPost.PostLines.Search<AppointmentToPost.refNbr>(fsAppointmentRow.RefNbr, fsAppointmentRow.SrvOrdType);

                    rows = new List<AppointmentToPost>
                    {
                        appointmentToPostRow
                    };

                    var jobExecutor = new JobExecutor<InvoicingProcessStepGroupShared>(processorCount: 1);
                    Guid currentProcessID = graphCreateInvoiceByAppointmentPost.CreateInvoices(graphCreateInvoiceByAppointmentPost, rows, graphCreateInvoiceByAppointmentPost.Filter.Current, null, jobExecutor, adapter.QuickProcessFlow);

                    if (graphCreateInvoiceByAppointmentPost.Filter.Current.PostTo == ID.SrvOrdType_PostTo.SALES_ORDER_MODULE
                        || graphCreateInvoiceByAppointmentPost.Filter.Current.PostTo == ID.SrvOrdType_PostTo.SALES_ORDER_INVOICE)
                    {
                        foreach (PXResult<FSPostBatch> result in SharedFunctions.GetPostBachByProcessID(this, currentProcessID))
                        {
                            FSPostBatch fSPostBatchRow = (FSPostBatch)result;

                            graphCreateInvoiceByAppointmentPost.ApplyPrepayments(fSPostBatchRow);
                        }
                    }

                    AppointmentEntry apptGraph = PXGraph.CreateInstance<AppointmentEntry>();
                    apptGraph.AppointmentRecords.Current =
                            apptGraph.AppointmentRecords.Search<FSAppointment.refNbr>
                                                (fsAppointmentRow.RefNbr, fsAppointmentRow.SrvOrdType);

                    if (ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.PROJECTS)
                    {
                        UpdateUnitCostsAndUnitPrices(apptGraph,
                                                     apptGraph.AppointmentDetails.Cache,
                                                     apptGraph.AppointmentDetails.Select().RowCast<FSAppointmentDet>(),
                                                     apptGraph.AppointmentRecords.Current.AppointmentID);

                        if (apptGraph.AppointmentDetails.Cache.IsDirty == true)
                        {
                            apptGraph.Save.Press();
                        }
                    }

                    if (!adapter.MassProcess || this.IsMobile == true)
                    {
                        using (new PXTimeStampScope(null))
                        {
                            apptGraph.AppointmentPostedIn.Current = apptGraph.AppointmentPostedIn.SelectWindowed(0, 1);
                            apptGraph.openPostingDocument();
                        }
                    }
                });
            }

            return list;
        }
        #endregion

        #region OpenEmployeeBoard
        public PXAction<FSAppointment> openEmployeeBoard;
        [PXUIField(DisplayName = TX.CalendarBoardAccess.MULTI_EMP_CALENDAR, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable OpenEmployeeBoard(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess && list.Count() > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);
                throw new PXRedirectToBoardRequiredException(Paths.ScreenPaths.MULTI_EMPLOYEE_DISPATCH,
                                                             GetAppointmentUrlArguments(AppointmentRecords.Current),
                                                             PXBaseRedirectException.WindowMode.Same);
            }

            return list;
        }
        #endregion
        #region OpenRoomBoard
        public PXAction<FSAppointment> openRoomBoard;
        [PXUIField(DisplayName = TX.CalendarBoardAccess.ROOM_CALENDAR, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable OpenRoomBoard(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess && list.Count() > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);
                throw new PXRedirectToBoardRequiredException(Paths.ScreenPaths.MULTI_ROOM_DISPATCH,
                                                             GetAppointmentUrlArguments(AppointmentRecords.Current),
                                                             PXBaseRedirectException.WindowMode.Same);
            }

            return list;
        }
        #endregion
        #region OpenUserCalendar
        public PXAction<FSAppointment> openUserCalendar;
        [PXUIField(DisplayName = TX.CalendarBoardAccess.SINGLE_EMP_CALENDAR, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable OpenUserCalendar(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess && list.Count() > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);
                throw new PXRedirectToBoardRequiredException(Paths.ScreenPaths.SINGLE_EMPLOYEE_DISPATCH,
                                                             GetAppointmentUrlArguments(AppointmentRecords.Current),
                                                             PXBaseRedirectException.WindowMode.Same);
            }

            return list;
        }
        #endregion
        #region OpenSource
        public PXAction<FSAppointment> openSourceDocument;
        [PXUIField(DisplayName = "Open Source", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable OpenSourceDocument(PXAdapter adapter)
        {
            FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.Current;

            if (fsServiceOrderRow != null
                    && string.IsNullOrEmpty(fsServiceOrderRow.SourceType) == false
                        && string.IsNullOrEmpty(fsServiceOrderRow.SourceRefNbr) == false)
            {
                switch (fsServiceOrderRow.SourceType)
                {
                    case ID.SourceType_ServiceOrder.CASE:
                        var graphCRCase = PXGraph.CreateInstance<CRCaseMaint>();
                        CRCase crCase = (CRCase)PXSelect<CRCase,
                                                Where<CRCase.caseCD,
                                                    Equal<Required<CRCase.caseCD>>>>
                                                .Select(graphCRCase, fsServiceOrderRow.SourceRefNbr);
                        if (crCase != null)
                        {
                            graphCRCase.Case.Current = crCase;
                            throw new PXRedirectRequiredException(graphCRCase, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                        }

                        break;

                    case ID.SourceType_ServiceOrder.OPPORTUNITY:
                        var graphOpportunityMaint = PXGraph.CreateInstance<OpportunityMaint>();
                        CROpportunity crOpportunityRow = (CROpportunity)
                                                         PXSelect<CROpportunity,
                                                         Where<
                                                             CROpportunity.opportunityID, Equal<Required<CROpportunity.opportunityID>>>>
                                                         .Select(graphOpportunityMaint, fsServiceOrderRow.SourceRefNbr);

                        if (crOpportunityRow != null)
                        {
                            graphOpportunityMaint.Opportunity.Current = crOpportunityRow;
                            throw new PXRedirectRequiredException(graphOpportunityMaint, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                        }

                        break;

                    case ID.SourceType_ServiceOrder.SALES_ORDER:

                        var graphSOOrder = PXGraph.CreateInstance<SOOrderEntry>();
                        SOOrder soOrder = (SOOrder)PXSelect<SOOrder,
                                                   Where<
                                                       SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
                                                       And<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>
                                                   .Select(graphSOOrder, fsServiceOrderRow.SourceDocType, fsServiceOrderRow.SourceRefNbr);
                        if (soOrder != null)
                        {
                            graphSOOrder.Document.Current = soOrder;
                            throw new PXRedirectRequiredException(graphSOOrder, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                        }

                        break;

                    case ID.SourceType_ServiceOrder.SERVICE_DISPATCH:

                        var graphServiceOrder = PXGraph.CreateInstance<ServiceOrderEntry>();

                        graphServiceOrder.ServiceOrderRecords.Current = graphServiceOrder.ServiceOrderRecords
                                    .Search<FSServiceOrder.refNbr>(fsServiceOrderRow.SourceRefNbr, fsServiceOrderRow.SourceDocType);

                        throw new PXRedirectRequiredException(graphServiceOrder, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
            }

            return adapter.Get();
        }

        #endregion

        #region CreateNewCustomer
        public PXAction<FSAppointment> createNewCustomer;
        [PXUIField(DisplayName = "Create New Customer", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual void CreateNewCustomer()
        {
            var graph = PXGraph.CreateInstance<CustomerMaint>();
            Customer customer = new Customer();
            graph.CurrentCustomer.Insert(customer);
            throw new PXRedirectRequiredException(graph, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
        }
        #endregion

        #region EmailConfirmationToStaffMember
        public PXAction<FSAppointment> emailConfirmationToStaffMember;
        [PXUIField(DisplayName = "Email Confirmation to Staff", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable EmailConfirmationToStaffMember(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            foreach (FSAppointment fsAppointmentRow in list)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, fsAppointmentRow);
                SendNotification(AppointmentRecords.Cache, fsAppointmentRow, FSMailing.EMAIL_CONFIRMATION_TO_STAFF, ServiceOrderRelated.Current.BranchID);
            }

            return list;
        }
        #endregion

        #region EmailConfirmationToCustomer
        public PXAction<FSAppointment> emailConfirmationToCustomer;
        [PXUIField(DisplayName = "Email Confirmation to Customer", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable EmailConfirmationToCustomer(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            foreach (FSAppointment fsAppointmentRow in list)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, fsAppointmentRow);
                SendNotification(AppointmentRecords.Cache, fsAppointmentRow, FSMailing.EMAIL_CONFIRMATION_TO_CUSTOMER, ServiceOrderRelated.Current.BranchID);
            }

            return list;
        }
        #endregion

        #region EmailConfirmationToGeoZoneStaff
        public PXAction<FSAppointment> emailConfirmationToGeoZoneStaff;
        [PXUIField(DisplayName = "Email Notification to Service Area Staff", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable EmailConfirmationToGeoZoneStaff(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            foreach (FSAppointment fsAppointmentRow in list)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, fsAppointmentRow);
                SendNotification(AppointmentRecords.Cache, fsAppointmentRow, FSMailing.EMAIL_NOTIFICATION_TO_GEOZONE_STAFF, ServiceOrderRelated.Current.BranchID);
            }

            return list;
        }
        #endregion

        #region EmailSignedAppointment
        public PXAction<FSAppointment> emailSignedAppointment;
        [PXUIField(DisplayName = "Email Signed Appointment", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable EmailSignedAppointment(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            foreach (FSAppointment fsAppointmentRow in list)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, fsAppointmentRow);

                List<Guid?> attachments = new List<Guid?>();

                if (AppointmentRecords.Current.CustomerSignedReport != null)
                {
                    attachments.Add(AppointmentRecords.Current.CustomerSignedReport);
                }

                SendNotification(AppointmentRecords.Cache, fsAppointmentRow, FSMailing.EMAIL_NOTIFICATION_SIGNED_APPOINTMENT, ServiceOrderRelated.Current.BranchID, attachments);
            }

            return list;
        }
        #endregion

        #region OpenPostingDocument
        public PXAction<FSAppointment> OpenPostingDocument;
        [PXButton]
        [PXUIField(DisplayName = "Open Document", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void openPostingDocument()
        {
            FSPostDet fsPostDetRow = AppointmentPostedIn.SelectWindowed(0, 1);

            if (fsPostDetRow == null)
            {
                return;
            }

            if (fsPostDetRow.SOPosted == true)
            {
                if (PXAccess.FeatureInstalled<FeaturesSet.distributionModule>())
                {
                    SOOrderEntry graphSOOrderEntry = PXGraph.CreateInstance<SOOrderEntry>();
                    graphSOOrderEntry.Document.Current = graphSOOrderEntry.Document.Search<SOOrder.orderNbr>(fsPostDetRow.SOOrderNbr, fsPostDetRow.SOOrderType);
                    throw new PXRedirectRequiredException(graphSOOrderEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
            }
            else if (fsPostDetRow.ARPosted == true && IsMobile == false)
            {
                ARInvoiceEntry graphARInvoiceEntry = PXGraph.CreateInstance<ARInvoiceEntry>();
                graphARInvoiceEntry.Document.Current = graphARInvoiceEntry.Document.Search<ARInvoice.refNbr>(fsPostDetRow.ARRefNbr, fsPostDetRow.ARDocType);
                throw new PXRedirectRequiredException(graphARInvoiceEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
            else if (fsPostDetRow.SOInvPosted == true)
            {
                SOInvoiceEntry graphSOInvoiceEntry = PXGraph.CreateInstance<SOInvoiceEntry>();
                graphSOInvoiceEntry.Document.Current = graphSOInvoiceEntry.Document.Search<ARInvoice.refNbr>(fsPostDetRow.SOInvRefNbr, fsPostDetRow.SOInvDocType);
                throw new PXRedirectRequiredException(graphSOInvoiceEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
            else if (fsPostDetRow.APPosted == true)
            {
                APInvoiceEntry graphAPInvoiceEntry = PXGraph.CreateInstance<APInvoiceEntry>();
                graphAPInvoiceEntry.Document.Current = graphAPInvoiceEntry.Document.Search<APInvoice.refNbr>(fsPostDetRow.APRefNbr, fsPostDetRow.APDocType);
                throw new PXRedirectRequiredException(graphAPInvoiceEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
            else if (fsPostDetRow.PMPosted == true)
            {
                RegisterEntry graphRegisterEntry = PXGraph.CreateInstance<RegisterEntry>();
                graphRegisterEntry.Document.Current = graphRegisterEntry.Document.Search<PMRegister.refNbr>(fsPostDetRow.PMRefNbr, fsPostDetRow.PMDocType);
                throw new PXRedirectRequiredException(graphRegisterEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
            else if (fsPostDetRow.INPosted == true)
            {
                if (fsPostDetRow.INDocType.Trim() == INDocType.Receipt)
                {
                    INReceiptEntry graphINReceiptEntry = PXGraph.CreateInstance<INReceiptEntry>();
                    graphINReceiptEntry.receipt.Current = graphINReceiptEntry.receipt.Search<INRegister.refNbr>(fsPostDetRow.INRefNbr);
                    throw new PXRedirectRequiredException(graphINReceiptEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
                else
                {
                    INIssueEntry graphINIssueEntry = PXGraph.CreateInstance<INIssueEntry>();
                    graphINIssueEntry.issue.Current = graphINIssueEntry.issue.Search<INRegister.refNbr>(fsPostDetRow.INRefNbr);
                    throw new PXRedirectRequiredException(graphINIssueEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
            }
        }
        #endregion

        #region OpenBillDocument
        public PXAction<FSAppointment> OpenBillDocument;
        [PXButton]
        [PXUIField(DisplayName = "Open Document", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual IEnumerable openBillDocument(PXAdapter adapter)
        {
            FSBillHistory fsBillHistoryRow = InvoiceRecords.Current;

            if (fsBillHistoryRow != null)
            {
                if (fsBillHistoryRow.ChildEntityType == FSEntityType.SalesOrder && PXAccess.FeatureInstalled<FeaturesSet.distributionModule>())
                {
                    SOOrderEntry graphSOOrderEntry = PXGraph.CreateInstance<SOOrderEntry>();
                    graphSOOrderEntry.Document.Current = graphSOOrderEntry.Document.Search<SOOrder.orderNbr>(fsBillHistoryRow.ChildRefNbr, fsBillHistoryRow.ChildDocType);
                    throw new PXRedirectRequiredException(graphSOOrderEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.ARInvoice)
                {
                ARInvoiceEntry graphARInvoiceEntry = PXGraph.CreateInstance<ARInvoiceEntry>();
                    graphARInvoiceEntry.Document.Current = graphARInvoiceEntry.Document.Search<ARInvoice.refNbr>(fsBillHistoryRow.ChildRefNbr, fsBillHistoryRow.ChildDocType);
                throw new PXRedirectRequiredException(graphARInvoiceEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.SOInvoice)
            {
                SOInvoiceEntry graphSOInvoiceEntry = PXGraph.CreateInstance<SOInvoiceEntry>();
                    graphSOInvoiceEntry.Document.Current = graphSOInvoiceEntry.Document.Search<ARInvoice.refNbr>(fsBillHistoryRow.ChildRefNbr, fsBillHistoryRow.ChildDocType);
                throw new PXRedirectRequiredException(graphSOInvoiceEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.APInvoice)
            {
                APInvoiceEntry graphAPInvoiceEntry = PXGraph.CreateInstance<APInvoiceEntry>();
                    graphAPInvoiceEntry.Document.Current = graphAPInvoiceEntry.Document.Search<APInvoice.refNbr>(fsBillHistoryRow.ChildRefNbr, fsBillHistoryRow.ChildDocType);
                throw new PXRedirectRequiredException(graphAPInvoiceEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.PMRegister)
            {
                RegisterEntry graphRegisterEntry = PXGraph.CreateInstance<RegisterEntry>();
                    graphRegisterEntry.Document.Current = graphRegisterEntry.Document.Search<PMRegister.refNbr>(fsBillHistoryRow.ChildRefNbr, fsBillHistoryRow.ChildDocType);
                throw new PXRedirectRequiredException(graphRegisterEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.INReceipt)
                {
                    INReceiptEntry graphINReceiptEntry = PXGraph.CreateInstance<INReceiptEntry>();
                    graphINReceiptEntry.receipt.Current = graphINReceiptEntry.receipt.Search<INRegister.refNbr>(fsBillHistoryRow.ChildRefNbr);
                    throw new PXRedirectRequiredException(graphINReceiptEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.INIssue)
                {
                    INIssueEntry graphINIssueEntry = PXGraph.CreateInstance<INIssueEntry>();
                    graphINIssueEntry.issue.Current = graphINIssueEntry.issue.Search<INRegister.refNbr>(fsBillHistoryRow.ChildRefNbr);
                    throw new PXRedirectRequiredException(graphINIssueEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
            }

            return adapter.Get();
        }
        #endregion

        #region AppointmentsReports
        public PXAction<FSAppointment> printAppointmentReport;
        [PXUIField(DisplayName = "Print Appointment", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual IEnumerable PrintAppointmentReport(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);

                Dictionary<string, string> parameters = new Dictionary<string, string>();

                string srvOrdTypeFieldName = SharedFunctions.GetFieldName<FSAppointment.srvOrdType>();
                string refNbrFieldName = SharedFunctions.GetFieldName<FSAppointment.refNbr>();

                parameters[srvOrdTypeFieldName] = list[0].SrvOrdType;
                parameters[refNbrFieldName] = list[0].RefNbr;

                throw new PXReportRequiredException(parameters, ID.ReportID.APPOINTMENT, string.Empty);
            }

            return list;
        }

        public PXAction<FSAppointment> printServiceTimeActivityReport;
        [PXUIField(DisplayName = "Print Service Time Activity", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual IEnumerable PrintServiceTimeActivityReport(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);

                Dictionary<string, string> parameters = new Dictionary<string, string>();

                string srvOrdTypeFieldName = SharedFunctions.GetFieldName<FSAppointment.srvOrdType>();
                string appRefNbrFieldName = SharedFunctions.GetFieldName<FSAppointment.refNbr>();
                string soRefNbrFieldName = SharedFunctions.GetFieldName<FSAppointment.soRefNbr>();
                ////This two parameters are for the Report ServiceTimeActivity
                string DateFromFieldName = "DateFrom";
                string DateToFieldName = "DateTo";

                parameters[srvOrdTypeFieldName] = list[0].SrvOrdType;
                parameters[appRefNbrFieldName] = list[0].RefNbr;
                parameters[soRefNbrFieldName] = list[0].SORefNbr;
                parameters[DateFromFieldName] = list[0].ExecutionDate.ToString();
                parameters[DateToFieldName] = list[0].ExecutionDate.ToString();

                throw new PXReportRequiredException(parameters, ID.ReportID.SERVICE_TIME_ACTIVITY, string.Empty);
            }

            return list;
        }
        #endregion

        #region StartItemLine
        public PXAction<FSAppointment> startItemLine;
        [PXUIField(DisplayName = "Start", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable StartItemLine(PXAdapter adapter)
        {
            FSAppointmentDet apptDet = AppointmentDetails.Current;

            if (apptDet != null)
            {
                if (CanLogBeStarted(apptDet) == false)
                {
                    throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_STARTED,
                                          PXLocalizer.Localize(startItemLine.GetCaption()),
                                          GetLineDisplayHint(this, apptDet.LineRef, apptDet.TranDesc, apptDet.InventoryID),
                                          PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status));
                }
            }

            if (apptDet != null &&
                (apptDet.LineType == ID.LineType_ALL.INVENTORY_ITEM ||
                 apptDet.LineType == ID.LineType_ALL.COMMENT ||
                 apptDet.LineType == ID.LineType_ALL.INSTRUCTION))
            {
                ChangeItemLineStatus(apptDet, ID.Status_AppointmentDet.IN_PROCESS);
                this.Actions.PressSave();
            }
            else
            {
                LogActionBase(adapter, ID.LogActions.START, PXLocalizer.Localize(startItemLine.GetCaption()), AppointmentDetails.Current, null);
            }

            return adapter.Get();
        }

        #endregion
        #region PauseItemLine
        public PXAction<FSAppointment> pauseItemLine;
        [PXUIField(DisplayName = "Pause", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable PauseItemLine(PXAdapter adapter)
        {
            FSAppointmentDet apptDet = AppointmentDetails.Current;

            if (apptDet != null)
            {
                if (CanLogBePaused(apptDet) == false)
                {
                    throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_PAUSED,
                                          PXLocalizer.Localize(pauseItemLine.GetCaption()),
                                          GetLineDisplayHint(this, apptDet.LineRef, apptDet.TranDesc, apptDet.InventoryID),
                                          PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status));
                }
            }

            if (apptDet != null &&
                (apptDet.LineType == ID.LineType_ALL.INVENTORY_ITEM
                 || apptDet.LineType == ID.LineType_ALL.COMMENT
                 || apptDet.LineType == ID.LineType_ALL.INSTRUCTION))
            {
                return adapter.Get();
            }
            else
            {
                PXSelectBase<FSAppointmentLog> logSelect = null;
                object[] logSelectArgs = null;

                if (GetLogType(apptDet) == FSAppointmentLog.itemType.Values.NonStock)
                {
                    logSelect = new PXSelect<FSAppointmentLog,
                                    Where<
                                        FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                                    And<
                                        FSAppointmentLog.detLineRef, Equal<Required<FSAppointmentLog.detLineRef>>,
                                    And<
                                        FSAppointmentLog.itemType, Equal<FSAppointmentLog.itemType.Values.nonStock>,
                                    And<
                                        FSAppointmentLog.status, Equal<FSAppointmentLog.status.InProcess>>>>>>(this);

                    logSelectArgs = new object[] { AppointmentRecords.Current?.AppointmentID, AppointmentDetails.Current?.LineRef };
                }

                LogActionBase(adapter, ID.LogActions.PAUSE, PXLocalizer.Localize(pauseItemLine.GetCaption()), apptDet, logSelect, logSelectArgs);
            }

            return adapter.Get();
        }

        #endregion
        #region ResumeItemLine
        public PXAction<FSAppointment> resumeItemLine;
        [PXUIField(DisplayName = "Resume", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable ResumeItemLine(PXAdapter adapter)
        {
            FSAppointmentDet apptDet = AppointmentDetails.Current;

            if (apptDet != null)
            {
                if (CanLogBeResumed(apptDet) == false)
                {
                    throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_RESUMED,
                                          PXLocalizer.Localize(resumeItemLine.GetCaption()),
                                          GetLineDisplayHint(this, apptDet.LineRef, apptDet.TranDesc, apptDet.InventoryID),
                                          PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status));
                }
            }

            if (apptDet != null &&
                (apptDet.LineType == ID.LineType_ALL.INVENTORY_ITEM
                 || apptDet.LineType == ID.LineType_ALL.COMMENT
                 || apptDet.LineType == ID.LineType_ALL.INSTRUCTION))
            {
                return adapter.Get();
            }
            else
            {
                PXSelectBase<FSAppointmentLog> logSelect = null;
                object[] logSelectArgs = null;

                if (GetLogType(apptDet) == FSAppointmentLog.itemType.Values.NonStock)
                {
                    logSelect = new PXSelect<FSAppointmentLog,
                                    Where<
                                        FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                                    And<
                                        FSAppointmentLog.detLineRef, Equal<Required<FSAppointmentLog.detLineRef>>,
                                    And<
                                        FSAppointmentLog.itemType, Equal<FSAppointmentLog.itemType.Values.nonStock>,
                                    And<
                                        FSAppointmentLog.status, Equal<FSAppointmentLog.status.Paused>>>>>>(this);

                    logSelectArgs = new object[] { AppointmentRecords.Current?.AppointmentID, apptDet.LineRef };
                }

                LogActionBase(adapter, ID.LogActions.RESUME, PXLocalizer.Localize(resumeItemLine.GetCaption()), apptDet, logSelect, logSelectArgs);
            }

            return adapter.Get();
        }

        #endregion
        #region CompleteItemLine
        public PXAction<FSAppointment> completeItemLine;
        [PXUIField(DisplayName = "Complete", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable CompleteItemLine(PXAdapter adapter)
        {
            FSAppointmentDet apptDet = AppointmentDetails.Current;

            if (apptDet != null)
            {
                if (CanItemLineBeCompleted(apptDet) == false)
                {
                    throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_COMPLETED,
                                          PXLocalizer.Localize(completeItemLine.GetCaption()),
                                          GetLineDisplayHint(this, apptDet.LineRef, apptDet.TranDesc, apptDet.InventoryID),
                                          PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status));
                }
            }

            if (apptDet != null &&
                (apptDet.LineType == ID.LineType_ALL.INVENTORY_ITEM ||
                 apptDet.LineType == ID.LineType_ALL.COMMENT ||
                 apptDet.LineType == ID.LineType_ALL.INSTRUCTION))
            {
                ChangeItemLineStatus(apptDet, ID.Status_AppointmentDet.COMPLETED);
                this.Actions.PressSave();
            }
            else
            {
                PXSelectBase<FSAppointmentLog> logSelect = null;
                object[] logSelectArgs = null;

                if (GetLogType(AppointmentDetails.Current) == FSAppointmentLog.itemType.Values.NonStock)
                {
                    logSelect = new PXSelect<FSAppointmentLog,
                        Where<FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                            And<FSAppointmentLog.detLineRef, Equal<Required<FSAppointmentLog.detLineRef>>,
                            And<FSAppointmentLog.itemType, Equal<FSAppointmentLog.itemType.Values.nonStock>,
                            And<
                                Where<FSAppointmentLog.status, Equal<FSAppointmentLog.status.InProcess>,
                                   Or<FSAppointmentLog.status, Equal<FSAppointmentLog.status.Paused>>>>>>>>(this);

                    logSelectArgs = new object[] { AppointmentRecords.Current?.AppointmentID, AppointmentDetails.Current?.LineRef };
                }

                LogActionBase(adapter, ID.LogActions.COMPLETE, PXLocalizer.Localize(completeItemLine.GetCaption()), AppointmentDetails.Current, logSelect, logSelectArgs);
            }

            return adapter.Get();
        }
        #endregion
        #region CancelItemLine
        public PXAction<FSAppointment> cancelItemLine;
        [PXUIField(DisplayName = "Cancel", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable CancelItemLine(PXAdapter adapter)
        {
            FSAppointmentDet apptDet = AppointmentDetails.Current;
            if (apptDet == null)
                return adapter.Get();

            ChangeItemLineStatus(apptDet, ID.Status_AppointmentDet.CANCELED);

            Save.Press();

            return adapter.Get();
        }
        #endregion

        public virtual void LogActionBase(PXAdapter adapter, string logActionID, string logActionLabel, FSAppointmentDet apptDet, PXSelectBase<FSAppointmentLog> logSelect, params object[] logSelectArgs)
        {
            if (LogActionFilter.Current == null)
                return;

            bool openDialogBox = true;

            string logType = GetLogType(AppointmentDetails.Current);

            if (logType == FSAppointmentLog.itemType.Values.NonStock)
            {
                openDialogBox = false;
                LogActionFilter.Current.Type = FSLogActionFilter.type.Values.NonStock;
            }
            else
            {
                apptDet = null;
            }

            if (openDialogBox == true && LogActionFilter.View.Answer != WebDialogResult.OK)
            {
                SetLogActionPanelDefaults(logActionID, logType);
            }

            VerifyLogActionWithAppointmentStatus(logActionID, logActionLabel, logType, AppointmentRecords.Current);

            WebDialogResult result = WebDialogResult.None;

            if (openDialogBox == true)
            {
                result = LogActionFilter.AskExt(true);
            }

            if (openDialogBox == false || result == WebDialogResult.OK)
            {
                RunLogAction(logActionID, LogActionFilter.Current.Type, apptDet, logSelect, logSelectArgs);
            }
        }

        public virtual string GetDfltLogTypeForStaffAction(FSAppointmentEmployee staffRow, string defaultLogType)
        {
            FSAppointment appt = AppointmentRecords.Current;

            if (appt == null || appt.Status == null)
                return null;

            if (appt.NotStarted == true || appt.Completed == true)
            {
                return FSLogActionFilter.type.Values.Travel;
            }

            if (staffRow == null || string.IsNullOrEmpty(staffRow.ServiceLineRef) == true)
                return defaultLogType;

            FSAppointmentDet detRow =
                    (FSAppointmentDet)PXSelectorAttribute.Select<FSAppointmentEmployee.serviceLineRef>
                                                                (AppointmentServiceEmployees.Cache, staffRow);

            string logType = GetLogTypeCheckingTravelWithLogFormula(LogRecords.Cache, detRow);

            return logType == FSLogActionFilter.type.Values.Travel ? FSLogActionFilter.type.Values.Travel : defaultLogType;
        }

        #region StartStaff
        public PXAction<FSAppointment> startStaff;
        [PXUIField(DisplayName = "Start", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable StartStaff(PXAdapter adapter)
        {
            if (LogActionFilter.Current != null)
            {
                string logType = GetDfltLogTypeForStaffAction(AppointmentServiceEmployees.Current, FSLogActionFilter.type.Values.Staff);

                if (LogActionFilter.View.Answer != WebDialogResult.OK)
                {
                    SetLogActionPanelDefaults(ID.LogActions.START, logType);
                }

                VerifyLogActionWithAppointmentStatus(ID.LogActions.START, PXLocalizer.Localize(startStaff.GetCaption()), logType, AppointmentRecords.Current);

                WebDialogResult result = LogActionFilter.AskExt(true);

                if (result == WebDialogResult.OK)
                {
                    RunLogAction(LogActionFilter.Current.Action, LogActionFilter.Current.Type, null, null, null);
                }
            }

            return adapter.Get();
        }
        #endregion
        #region PauseStaff
        public PXAction<FSAppointment> pauseStaff;
        [PXUIField(DisplayName = "Pause", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable PauseStaff(PXAdapter adapter)
        {
            if (LogActionFilter.Current != null)
            {
                string logType = GetDfltLogTypeForStaffAction(AppointmentServiceEmployees.Current, FSLogActionFilter.type.Values.Service);

                if (LogActionFilter.View.Answer != WebDialogResult.OK)
                {
                    SetLogActionPanelDefaults(ID.LogActions.PAUSE, logType, true);
                }

                WebDialogResult result = LogActionFilter.AskExt(true);

                if (result == WebDialogResult.OK)
                {
                    RunLogAction(LogActionFilter.Current.Action, LogActionFilter.Current.Type, null, null, null);
                }
            }

            return adapter.Get();
        }

        #endregion
        #region ResumeStaff
        public PXAction<FSAppointment> resumeStaff;
        [PXUIField(DisplayName = "Resume", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable ResumeStaff(PXAdapter adapter)
        {
            if (LogActionFilter.Current != null)
            {
                string logType = GetDfltLogTypeForStaffAction(AppointmentServiceEmployees.Current, FSLogActionFilter.type.Values.Service);

                if (LogActionFilter.View.Answer != WebDialogResult.OK)
                {
                    SetLogActionPanelDefaults(ID.LogActions.RESUME, logType, true);
                }

                VerifyLogActionWithAppointmentStatus(ID.LogActions.RESUME, PXLocalizer.Localize(resumeStaff.GetCaption()), logType, AppointmentRecords.Current);

                WebDialogResult result = LogActionFilter.AskExt(true);

                if (result == WebDialogResult.OK)
                {
                    RunLogAction(LogActionFilter.Current.Action, LogActionFilter.Current.Type, null, null, null);
                }
            }

            return adapter.Get();
        }
        #endregion
        #region CompleteStaff
        public PXAction<FSAppointment> completeStaff;
        [PXUIField(DisplayName = "Complete", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable CompleteStaff(PXAdapter adapter)
        {
            if (LogActionFilter.Current != null)
            {
                string logType = GetDfltLogTypeForStaffAction(AppointmentServiceEmployees.Current, FSLogActionFilter.type.Values.Service);

                if (LogActionFilter.View.Answer != WebDialogResult.OK)
                {
                    SetLogActionPanelDefaults(ID.LogActions.COMPLETE, logType, true);
                }

                VerifyLogActionWithAppointmentStatus(ID.LogActions.COMPLETE, PXLocalizer.Localize(completeStaff.GetCaption()), logType, AppointmentRecords.Current);

                WebDialogResult result = LogActionFilter.AskExt(true);

                if (result == WebDialogResult.OK)
                {
                    RunLogAction(LogActionFilter.Current.Action, LogActionFilter.Current.Type, null, null, null);
                }
            }

            return adapter.Get();
        }
        #endregion

        #region ViewPayment
        public PXAction<FSAppointment> viewPayment;
        [PXUIField(DisplayName = "View Payment", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXLookupButton]
        public virtual void ViewPayment()
        {
            if (ServiceOrderRelated.Current != null && AppointmentRecords.Current != null && Adjustments.Current != null)
            {
                ARPaymentEntry graphARPaymentEntry = PXGraph.CreateInstance<ARPaymentEntry>();
                graphARPaymentEntry.Document.Current = graphARPaymentEntry.Document.Search<ARPayment.refNbr>(Adjustments.Current.RefNbr, Adjustments.Current.DocType);

                throw new PXRedirectRequiredException(graphARPaymentEntry, true, "Payment") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
        }
        #endregion
        #region CreatePrepayment
        public PXAction<FSAppointment> createPrepayment;
        [PXUIField(DisplayName = "Create Prepayment", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Cancel)]
        protected virtual void CreatePrepayment()
        {
            if (ServiceOrderRelated.Current != null && AppointmentRecords.Current != null)
            {
                this.Save.Press();

                PXGraph target;

                CreatePrepaymentDocument(ServiceOrderRelated.Current, AppointmentRecords.Current, out target, ARPaymentType.Prepayment);

                throw new PXPopupRedirectException(target, "New Payment", true);
            }
        }
        #endregion

        #region QuickProcessMobile
        public PXAction<FSAppointment> quickProcessMobile;
        [PXButton]
        [PXUIField(DisplayName = "Quick Process", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public IEnumerable QuickProcessMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Cache.GetStatus(AppointmentRecords.Current) != PXEntryStatus.Inserted)
            {
                FSAppointment fsAppointmentRow = AppointmentRecords.Current;
                fsAppointmentRow.IsCalledFromQuickProcess = true;
                AppointmentEntry.AppointmentQuickProcess.InitQuickProcessPanel(this, "");

                // This is to verify the applicability of each option.
                AppointmentQuickProcessExt.QuickProcessParameters.Cache.RaiseRowSelected(AppointmentQuickProcessExt.QuickProcessParameters.Current);

                PXQuickProcess.Start(this, fsAppointmentRow, this.AppointmentQuickProcessExt.QuickProcessParameters.Current);
            }

            return adapter.Get();
        }
        #endregion

        #region OpenScheduleScreen
        public PXAction<FSAppointment> openScheduleScreen;
        [PXButton(OnClosingPopup = PXSpecialButtonType.Cancel)]
        [PXUIField(MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
        protected virtual void OpenScheduleScreen()
        {
            if (ServiceOrderRelated.Current != null && ServiceOrderTypeSelected.Current != null)
            {
                if (ServiceOrderTypeSelected.Current.Behavior == FSSrvOrdType.behavior.Values.RouteAppointment)
                {
                    var graphRouteServiceContractScheduleEntry = PXGraph.CreateInstance<RouteServiceContractScheduleEntry>();

                    graphRouteServiceContractScheduleEntry.ContractScheduleRecords.Current = graphRouteServiceContractScheduleEntry
                                                                                             .ContractScheduleRecords.Search<FSRouteContractSchedule.scheduleID>
                                                                                             (ServiceOrderRelated.Current.ScheduleID,
                                                                                              ServiceOrderRelated.Current.ServiceContractID,
                                                                                              ServiceOrderRelated.Current.CustomerID);

                    throw new PXRedirectRequiredException(graphRouteServiceContractScheduleEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
                else
                {
                    var graphServiceContractScheduleEntry = PXGraph.CreateInstance<ServiceContractScheduleEntry>();

                    graphServiceContractScheduleEntry.ContractScheduleRecords.Current = graphServiceContractScheduleEntry
                                                                                        .ContractScheduleRecords.Search<FSContractSchedule.scheduleID>
                                                                                        (ServiceOrderRelated.Current.ScheduleID,
                                                                                         ServiceOrderRelated.Current.ServiceContractID,
                                                                                         ServiceOrderRelated.Current.CustomerID);

                    throw new PXRedirectRequiredException(graphServiceContractScheduleEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
            }
        }
        #endregion

        // Travel Time Actions Mobile
        #region TravelTimeActionsMobile
        #region StartTravelMobile
        public PXAction<FSAppointment> startTravelMobile;
        [PXUIField(DisplayName = "Depart", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable StartTravelMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                var graph = PXGraph.CreateInstance<AppStartTravelServiceEntry>();

                graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                graph.Filter.Current.Type = FSLogActionFilter.type.Values.Travel;
                graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
                graph.Filter.Current.DetLineRef = GetItemLineRef(this, AppointmentRecords.Current.AppointmentID, true);

                throw new PXPopupRedirectException(graph, null);
            }

            return adapter.Get();
        }
        #endregion
        #region CompleteTravelMobile
        public PXAction<FSAppointment> completeTravelMobile;
        [PXUIField(DisplayName = "Arrive", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable CompleteTravelMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                var graph = PXGraph.CreateInstance<AppCompleteTravelEntry>();

                graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                throw new PXPopupRedirectException(graph, null);
            }

            return adapter.Get();
        }
        #endregion
        #region StartServiceMobile
        public PXAction<FSAppointment> startServiceMobile;
        [PXUIField(DisplayName = "Start", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable StartServiceMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                FSAppointmentDet apptDet = AppointmentDetails.Current;

                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                if (CanLogBeStarted(apptDet) == false)
                {
                    throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_STARTED,
                                          PXLocalizer.Localize(startItemLine.GetCaption()),
                                          GetLineDisplayHint(this, apptDet.LineRef, apptDet.TranDesc, apptDet.InventoryID),
                                          PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status));
                }

                if (apptDet != null && apptDet.LineType != ID.LineType_ALL.SERVICE)
                {
                    this.startItemLine.Press();
                }
                else
                {
                    var graph = PXGraph.CreateInstance<AppStartTravelServiceEntry>();

                    graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                    graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                    graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                    graph.Filter.Current.Type = apptDet?.IsTravelItem == true ? FSLogActionMobileFilter.type.Values.Travel : FSLogActionMobileFilter.type.Values.Service;
                    graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
                    graph.Filter.Current.DetLineRef = apptDet?.LineType == ID.LineType_ALL.SERVICE ? apptDet.LineRef : null;

                    throw new PXPopupRedirectException(graph, null);
                }
            }

            return adapter.Get();
        }
        #endregion
        #region StartForAssignedStaffMobile
        public PXAction<FSAppointment> startForAssignedStaffMobile;
        [PXUIField(DisplayName = "Start for Assigned Staff", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable StartForAssignedStaffMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                var graph = PXGraph.CreateInstance<AppStartServiceAssignedStaffEntry>();

                graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                throw new PXPopupRedirectException(graph, null);
            }

            return adapter.Get();
        }
        #endregion
        #region CompleteServiceMobile
        public PXAction<FSAppointment> completeServiceMobile;
        [PXUIField(DisplayName = "Complete", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable CompleteServiceMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                FSAppointmentDet apptDet = AppointmentDetails.Current;

                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                if (CanItemLineBeCompleted(apptDet) == false)
                {
                    throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_COMPLETED,
                                          PXLocalizer.Localize(completeItemLine.GetCaption()),
                                          GetLineDisplayHint(this, apptDet.LineRef, apptDet.TranDesc, apptDet.InventoryID),
                                          PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status));
                }

                if (apptDet != null && apptDet.LineType != ID.LineType_ALL.SERVICE)
                {
                    this.completeItemLine.Press();
                }
                else
                {
                    var graph = PXGraph.CreateInstance<AppCompleteServiceEntry>();

                    graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                    graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                    graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                    graph.Filter.Current.Type = apptDet?.IsTravelItem == true ? FSLogActionFilter.type.Values.Travel  : FSLogActionFilter.type.Values.Service;
                    graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                    throw new PXPopupRedirectException(graph, null);
                }
            }

            return adapter.Get();
        }
        #endregion
        #region PauseServiceMobile
        public PXAction<FSAppointment> pauseServiceMobile;
        [PXUIField(DisplayName = "Pause", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable PauseServiceMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                FSAppointmentDet apptDet = AppointmentDetails.Current;

                if (apptDet != null)
                {
                    if (CanLogBePaused(apptDet) == false)
                    {
                        throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_PAUSED,
                                          PXLocalizer.Localize(pauseItemLine.GetCaption()),
                                          GetLineDisplayHint(this, apptDet.LineRef, apptDet.TranDesc, apptDet.InventoryID),
                                          PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status));
                    }
                }

                if (apptDet != null && apptDet.LineType != ID.LineType_ALL.SERVICE)
                {
                    this.pauseItemLine.Press();
                }
                else
                {
                    var graph = PXGraph.CreateInstance<AppPauseServiceEntry>();

                    graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                    graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                    graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                    graph.Filter.Current.Type = apptDet?.IsTravelItem == true ? FSLogActionMobileFilter.type.Values.Travel : FSLogActionMobileFilter.type.Values.Service;
                    graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                    throw new PXPopupRedirectException(graph, null);
                }
            }

            return adapter.Get();
        }
        #endregion
        #region ResumeServiceMobile
        public PXAction<FSAppointment> resumeServiceMobile;
        [PXUIField(DisplayName = "Resume", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable ResumeServiceMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);
                FSAppointmentDet apptDet = AppointmentDetails.Current;

                if (CanLogBeResumed(apptDet) == false)
                {
                    throw new PXException(TX.Error.DETAIL_LINE_CANNOT_BE_RESUMED,
                                            PXLocalizer.Localize(resumeItemLine.GetCaption()),
                                            GetLineDisplayHint(this, apptDet.LineRef, apptDet.TranDesc, apptDet.InventoryID),
                                            PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status));
                }

                if (apptDet != null && apptDet.LineType != ID.LineType_ALL.SERVICE)
                {
                    this.resumeItemLine.Press();
                }
                else
                {

                    var graph = PXGraph.CreateInstance<AppResumeServiceEntry>();

                    graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                    graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                    graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                    graph.Filter.Current.Type = apptDet?.IsTravelItem == true ? FSLogActionMobileFilter.type.Values.Travel : FSLogActionMobileFilter.type.Values.Service;
                    graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                    throw new PXPopupRedirectException(graph, null);
                }
            }

            return adapter.Get();
        }
        #endregion
        #region StartStaffMobile
        public PXAction<FSAppointment> startStaffMobile;
        [PXUIField(DisplayName = "Start", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable StartStaffMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                var graph = PXGraph.CreateInstance<AppStartStaffAndServiceEntry>();

                graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
                graph.Filter.Current.EmployeeLineRef = AppointmentServiceEmployees.Current?.LineRef;

                throw new PXPopupRedirectException(graph, null);
            }

            return adapter.Get();
        }
        #endregion
        #region PauseStaffMobile
        public PXAction<FSAppointment> pauseStaffMobile;
        [PXUIField(DisplayName = "Pause", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable PauseStaffMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                var graph = PXGraph.CreateInstance<AppPauseServiceEntry>();

                graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                graph.Filter.Current.Type = FSLogActionMobileFilter.type.Values.Staff;
                graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                throw new PXPopupRedirectException(graph, null);
            }

            return adapter.Get();
        }
        #endregion
        #region ResumeStaffMobile
        public PXAction<FSAppointment> resumeStaffMobile;
        [PXUIField(DisplayName = "Resume", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
        public virtual IEnumerable ResumeStaffMobile(PXAdapter adapter)
        {
            if (AppointmentRecords.Current != null)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, AppointmentRecords.Current);

                var graph = PXGraph.CreateInstance<AppResumeServiceEntry>();

                graph.Filter.Current.SrvOrdType = AppointmentRecords.Current.SrvOrdType;
                graph.Filter.Current.AppointmentID = AppointmentRecords.Current.AppointmentID;
                graph.Filter.Current.SOID = AppointmentRecords.Current.SOID;
                graph.Filter.Current.Type = FSLogActionMobileFilter.type.Values.Staff;
                graph.Filter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);

                throw new PXPopupRedirectException(graph, null);
            }

            return adapter.Get();
        }
        #endregion
        #endregion

        public PXAction<FSAppointment> addInvBySite;
        [PXUIField(DisplayName = "Add Items", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXLookupButton]
        public virtual IEnumerable AddInvBySite(PXAdapter adapter)
        {
            sitestatusfilter.Cache.Clear();
            if (sitestatus.AskExt() == WebDialogResult.OK)
            {
                return AddInvSelBySite(adapter);
            }
            sitestatusfilter.Cache.Clear();
            sitestatus.Cache.Clear();
            return adapter.Get();
        }

        public PXAction<FSAppointment> addInvSelBySite;
        [PXUIField(DisplayName = "Add", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
        [PXLookupButton]
        public virtual IEnumerable AddInvSelBySite(PXAdapter adapter)
        {
            AppointmentDetails.Cache.ForceExceptionHandling = true;

            foreach (FSSiteStatusSelected line in sitestatus.Cache.Cached)
            {
                if (line.Selected == true
                    && (line.QtySelected > 0 || line.DurationSelected > 0))
                {
                    InventoryItem inventoryItem =
                        PXSelect<InventoryItem,
                        Where<
                            InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                        .Select(this, line.InventoryID);

                    FSAppointmentDet newline = PXCache<FSAppointmentDet>.CreateCopy(AppointmentDetails.Insert(new FSAppointmentDet()));
                    if (inventoryItem.StkItem == true)
                    {
                        newline.LineType = ID.LineType_ALL.INVENTORY_ITEM;
                    }
                    else
                    {
                        newline.LineType = inventoryItem.ItemType == INItemTypes.ServiceItem ? ID.LineType_ALL.SERVICE : ID.LineType_ALL.NONSTOCKITEM;
                    }

                    newline.SiteID = line.SiteID ?? newline.SiteID;
                    newline.InventoryID = line.InventoryID;
                    newline.SubItemID = line.SubItemID;
                    newline.UOM = line.SalesUnit;
                    newline = PXCache<FSAppointmentDet>.CreateCopy(AppointmentDetails.Update(newline));

                    if (line.BillingRule == ID.BillingRule.TIME)
                    {
                        newline.EstimatedDuration = line.DurationSelected;
                    }
                    else
                    {
                        newline.EstimatedQty = line.QtySelected;
                    }

                    AppointmentDetails.Update(newline);
                }
            }

            sitestatus.Cache.Clear();
            return adapter.Get();
        }

        #region ViewLinkedDoc
        public ViewLinkedDoc<FSAppointment, FSAppointmentDet> viewLinkedDoc;
        #endregion

        #region AddReceipt
        public PXAction<FSAppointment> addReceipt;
        [PXUIField(DisplayName = "Create Expense Receipt", MapEnableRights = PXCacheRights.Select)]
        [PXButton()]
        protected virtual IEnumerable AddReceipt(PXAdapter adapter)
        {
            FSAppointment fsAppointmentRow = AppointmentRecords.Current;
            FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.Current;
            FSSrvOrdType fsSrvOrdTypeRow = ServiceOrderTypeSelected.Current;

            if (fsAppointmentRow != null && fsServiceOrderRow != null)
            {
                Save.Press();

                ExpenseClaimDetailEntry graph = PXGraph.CreateInstance<ExpenseClaimDetailEntry>();
                EPExpenseClaimDetails claimDetails = (EPExpenseClaimDetails)graph.ClaimDetails.Cache.CreateInstance();

                claimDetails = graph.ClaimDetails.Insert(claimDetails);

                claimDetails.ExpenseDate = fsAppointmentRow.ExecutionDate;
                claimDetails.BranchID = fsAppointmentRow.BranchID;
                claimDetails.CustomerID = fsServiceOrderRow.BillCustomerID;
                claimDetails.CustomerLocationID = fsServiceOrderRow.BillLocationID;
                claimDetails.ContractID = fsAppointmentRow.ProjectID;
                claimDetails.TaskID = fsAppointmentRow.DfltProjectTaskID;

                if (fsSrvOrdTypeRow != null
                   && !ProjectDefaultAttribute.IsNonProject(fsAppointmentRow.ProjectID)
                   && PXAccess.FeatureInstalled<FeaturesSet.costCodes>())
                {
                    claimDetails.CostCodeID = fsSrvOrdTypeRow.DfltCostCodeID;
                }

                claimDetails = graph.ClaimDetails.Update(claimDetails);

                FSxEPExpenseClaimDetails row = graph.ClaimDetails.Cache.GetExtension<FSxEPExpenseClaimDetails>(claimDetails);

                graph.ClaimDetails.Cache.SetValueExt<FSxEPExpenseClaimDetails.fsEntityTypeUI>(claimDetails, ID.FSEntityType.Appointment);
                graph.ClaimDetails.Cache.SetValueExt<FSxEPExpenseClaimDetails.fsEntityNoteID>(claimDetails, fsAppointmentRow.NoteID);

                claimDetails = graph.ClaimDetails.Update(claimDetails);

                PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.InlineWindow);
            }


            return adapter.Get();
        }
        #endregion
        #region AddBill
        public PXAction<FSAppointment> addBill;
        [PXUIField(DisplayName = "Create AP Bill", MapEnableRights = PXCacheRights.Select)]
        [PXButton()]
        protected virtual IEnumerable AddBill(PXAdapter adapter)
        {
            FSAppointment fsAppointmentRow = AppointmentRecords.Current;
            FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.Current;
            FSSrvOrdType fsSrvOrdTypeRow = ServiceOrderTypeSelected.Current;

            if (fsAppointmentRow != null && fsServiceOrderRow != null)
            {
                Save.Press();

                APInvoiceEntry graph = PXGraph.CreateInstance<APInvoiceEntry>();
                APInvoice ap = (APInvoice)graph.Document.Cache.CreateInstance();
                ap = graph.Document.Insert(ap);

                var graphExt = graph.GetExtension<SM_APInvoiceEntry>();

                ap.BranchID = fsAppointmentRow.BranchID;
                ap.DocDate = fsAppointmentRow.ExecutionDate;

                graphExt.apFilter.Current.RelatedEntityType = ID.FSEntityType.Appointment;
                graphExt.apFilter.Current.RelatedDocNoteID = fsAppointmentRow.NoteID;
                graphExt.apFilter.Current.RelatedDocDate = fsAppointmentRow.ExecutionDate;
                graphExt.apFilter.Current.RelatedDocCustomerID = fsAppointmentRow.CustomerID;
                graphExt.apFilter.Current.RelatedDocCustomerLocationID = fsServiceOrderRow.LocationID;
                graphExt.apFilter.Current.RelatedDocProjectID = fsServiceOrderRow.ProjectID;
                graphExt.apFilter.Current.RelatedDocProjectTaskID = fsServiceOrderRow.DfltProjectTaskID;
                graphExt.apFilter.Current.RelatedDocCostCodeID = fsSrvOrdTypeRow?.DfltCostCodeID;

                graph.Document.Update(ap);
                PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.InlineWindow);
            }

            return adapter.Get();
        }
        #endregion

        #region CreatePurchaseOrder
        public PXAction<FSAppointment> createPurchaseOrder;
        [PXUIField(DisplayName = "Purchase", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, FieldClass = "DISTINV")]
        [PXButton]
        public virtual IEnumerable CreatePurchaseOrder(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);

                POCreate graphPOCreate = PXGraph.CreateInstance<POCreate>();
                FSxPOCreateFilter fSxPOCreateFilterRow = graphPOCreate.Filter.Cache.GetExtension<FSxPOCreateFilter>(graphPOCreate.Filter.Current);
                fSxPOCreateFilterRow.SrvOrdType = list[0].SrvOrdType;
                fSxPOCreateFilterRow.ServiceOrderRefNbr = list[0].SORefNbr;

                throw new PXRedirectRequiredException(graphPOCreate, null);
            }

            return list;
        }

        public PXAction<FSAppointment> createPurchaseOrderMobile;
        [PXUIField(DisplayName = "Purchase Mobile", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, FieldClass = "DISTINV")]
        [PXButton]
        public virtual IEnumerable CreatePurchaseOrderMobile(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess && list.Count > 0)
            {
                SaveBeforeApplyAction(AppointmentRecords.Cache, list[0]);

                PXLongOperation.StartOperation(this, delegate ()
                {
                    POCreate graphPOCreate = PXGraph.CreateInstance<POCreate>();
                    FSxPOCreateFilter fSxPOCreateFilterRow = graphPOCreate.Filter.Cache.GetExtension<FSxPOCreateFilter>(graphPOCreate.Filter.Current);
                    fSxPOCreateFilterRow.SrvOrdType = list[0].SrvOrdType;
                    fSxPOCreateFilterRow.ServiceOrderRefNbr = list[0].SORefNbr;
                    fSxPOCreateFilterRow.AppointmentRefNbr = list[0].RefNbr;

                    List<POFixedDemand> processList = graphPOCreate.FixedDemand.Select().RowCast<POFixedDemand>().ToList();

                    POCreate.CreateProc(processList, graphPOCreate.Filter.Current.PurchDate, false);
                });
            }

            return list;
        }
        #endregion

        #region BillReversal
        public PXAction<FSAppointment> billReversal;
        [PXUIField(DisplayName = "Run Appointment Bill Reversal", MapEnableRights = PXCacheRights.Select)]
        [PXButton()]
        protected virtual IEnumerable BillReversal(PXAdapter adapter)
        {
            List<FSAppointment> list = adapter.Get<FSAppointment>().ToList();

            if (!adapter.MassProcess && list.Count > 0)
            {
                SaveWithRecalculateExternalTaxesSync();

                PXLongOperation.StartOperation(
                this,
                delegate ()
                {
                    RevertInvoiceDocument(list.FirstOrDefault(), AppointmentPostedIn.Select().RowCast<FSPostDet>().ToList());

                    AppointmentEntry apptGraph = PXGraph.CreateInstance<AppointmentEntry>();
                    apptGraph.AppointmentRecords.Current =
                            apptGraph.AppointmentRecords.Search<FSAppointment.refNbr>
                                                (AppointmentRecords.Current.RefNbr, AppointmentRecords.Current.SrvOrdType);

                    if (apptGraph.AppointmentRecords.Current != null
                        && apptGraph.AppointmentRecords.Current.Closed == true)
                    {
                        apptGraph.AppointmentRecords.View.Answer = WebDialogResult.Yes;
                        apptGraph.uncloseAppointment.Press();
                    }
                });
            }

            return list;
        }
        #endregion
        #region AddNewContact
        public PXAction<FSAppointment> addNewContact;
        [PXUIField(DisplayName = CR.Messages.AddNewContact, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable AddNewContact(PXAdapter adapter)
        {
            if (ServiceOrderRelated.Current != null && ServiceOrderRelated.Current.CustomerID != null)
            {
                ContactMaint target = PXGraph.CreateInstance<ContactMaint>();
                target.Clear();
                Contact maincontact = target.Contact.Insert();
                maincontact.BAccountID = ServiceOrderRelated.Current.CustomerID;

                CRContactClass ocls = PXSelect<CRContactClass, Where<CRContactClass.classID, Equal<Current<Contact.classID>>>>
                    .SelectSingleBound(this, new object[] { maincontact });

                maincontact = target.Contact.Update(maincontact);
                throw new PXRedirectRequiredException(target, true, "Contact") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
            return adapter.Get();
        }
        #endregion

        #region OpenPostBatch
        public ViewPostBatch<FSAppointment> openPostBatch;
        #endregion

        #endregion

        #region Selects / Views

        [PXHidden]
        public PXSelect<FSRouteSetup> RouteSetupRecord;

        [PXHidden]
        public PXSelect<FSSODet> FSSODets;

        [PXCopyPasteHiddenView]
        public PXFilter<FSLogActionFilter> LogActionFilter;


        [PXCopyPasteHiddenFields(typeof(FSAppointment.soRefNbr))]
        public PXSelectJoin<FSAppointment,
                LeftJoinSingleTable<Customer,
                    On<Customer.bAccountID, Equal<FSAppointment.customerID>>>,
                Where<
                    FSAppointment.srvOrdType, Equal<Optional<FSAppointment.srvOrdType>>,
                    And<
                        Where<
                            Customer.bAccountID, IsNull,
                            Or<Match<Customer, Current<AccessInfo.userName>>>>>>> AppointmentRecords;

        [PXViewName(TX.FriendlyViewName.Appointment.APPOINTMENT_SELECTED)]
        [PXCopyPasteHiddenFields(
            typeof(FSAppointment.soRefNbr),
            typeof(FSAppointment.fullNameSignature),
            typeof(FSAppointment.CustomerSignaturePath),
            typeof(FSAppointment.serviceContractID),
            typeof(FSAppointment.scheduleID),
            typeof(FSAppointment.logLineCntr))]
        public AppointmentSelected_View AppointmentSelected;


        [PXHidden]
        public PXSelect<FSSODet, Where<FSSODet.sOID, Equal<Current<FSServiceOrder.sOID>>>> ServiceOrderDetails;

        [PXHidden]
        public PXSelect<FSPostInfo, Where<FSPostInfo.appointmentID, Equal<Current<FSAppointment.appointmentID>>>> PostInfoDetails;

        [PXViewName(TX.FriendlyViewName.Common.SERVICEORDERTYPE_SELECTED)]
        public PXSetup<FSSrvOrdType>.Where<
               Where<
                   FSSrvOrdType.srvOrdType, Equal<Optional<FSAppointment.srvOrdType>>>> ServiceOrderTypeSelected;

        [PXHidden]
        public PXSetup<SOOrderType>.Where<
                Where<
                   SOOrderType.orderType, Equal<Current<FSSrvOrdType.postOrderType>>>> postSOOrderTypeSelected;

        [PXHidden]
        public PXSetup<SOOrderType>.Where<
                Where<
                    SOOrderType.orderType, Equal<Current<FSSrvOrdType.allocationOrderType>>>> AllocationSOOrderTypeSelected;

        [PXHidden]
        public PXSetup<FSBranchLocation>.Where<
               Where<
                   FSBranchLocation.branchLocationID, Equal<Current<FSServiceOrder.branchLocationID>>>> CurrentBranchLocation;

        [PXHidden]
        public PXSetup<Customer>.Where<
               Where<
                   Customer.bAccountID, Equal<Optional<FSServiceOrder.billCustomerID>>>> TaxCustomer;

        [PXHidden]
        public PXSetup<Location>.Where<
               Where<
                   Location.bAccountID, Equal<Current<FSServiceOrder.billCustomerID>>,
                   And<Location.locationID, Equal<Optional<FSServiceOrder.billLocationID>>>>> TaxLocation;

        [PXHidden]
        public PXSetup<TaxZone>.Where<
               Where<TaxZone.taxZoneID, Equal<Current<FSAppointment.taxZoneID>>>> TaxZone;

        public class ServiceOrderRelated_View : PXSelect<FSServiceOrder,
                                                Where<
                                                    FSServiceOrder.sOID, Equal<Optional<FSAppointment.sOID>>>>
        {
            public ServiceOrderRelated_View(PXGraph graph) : base(graph)
            {
            }

            public ServiceOrderRelated_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }
        [PXViewName(TX.FriendlyViewName.Appointment.SERVICEORDER_RELATED)]
        public ServiceOrderRelated_View ServiceOrderRelated;

        [PXViewName(TX.TableName.FSCONTACT)]
        public FSContact_View ServiceOrder_Contact;

        [PXViewName(TX.TableName.FSADDRESS)]
        public FSAddress_View ServiceOrder_Address;

        [PXHidden]
        public PXSelect<CurrencyInfo,
               Where<CurrencyInfo.curyInfoID, Equal<Current<FSAppointment.curyInfoID>>>> currencyInfoView;

        [PXFilterable]
        [PXImport(typeof(FSAppointment))]
        [PXViewName(TX.FriendlyViewName.Appointment.APPOINTMENT_DETAILS)]
        [PXCopyPasteHiddenFields(typeof(FSAppointmentDet.sODetID),
                                 typeof(FSAppointmentDet.lotSerialNbr),
                                 typeof(FSAppointmentDet.status),
                                 typeof(FSAppointmentDet.uiStatus))]
        public AppointmentDetails_View AppointmentDetails;


        public class AppointmentServiceEmployees_View : PXSelectJoin<FSAppointmentEmployee,
                                                        LeftJoin<BAccount,
                                                            On<FSAppointmentEmployee.employeeID, Equal<BAccount.bAccountID>>,
                                                        LeftJoin<FSAppointmentServiceEmployee,
                                                            On<
                                                                FSAppointmentServiceEmployee.lineRef, Equal<FSAppointmentEmployee.serviceLineRef>,
                                                                And<FSAppointmentServiceEmployee.appointmentID, Equal<FSAppointmentEmployee.appointmentID>>>>>,
                                                        Where<
                                                            FSAppointmentEmployee.appointmentID, Equal<Current<FSAppointment.appointmentID>>,
                                                            And<
                                                                Where<
                                                                    FSAppointmentEmployee.serviceLineRef, IsNull,
                                                                Or<
                                                                    FSAppointmentServiceEmployee.lineType, Equal<ListField_LineType_ALL.Service>>>>>,
                                                        OrderBy<
                                                                Asc<FSAppointmentEmployee.lineRef>>>
        {
            public AppointmentServiceEmployees_View(PXGraph graph) : base(graph)
            {
            }

            public AppointmentServiceEmployees_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }
        [PXFilterable]
        [PXViewName(TX.FriendlyViewName.Appointment.APPOINTMENT_EMPLOYEES)]
        [PXCopyPasteHiddenFields(
            typeof(FSAppointmentEmployee.lineNbr),
            typeof(FSAppointmentEmployee.lineRef),
            typeof(FSAppointmentEmployee.serviceLineRef))]
        public AppointmentServiceEmployees_View AppointmentServiceEmployees;

        [PXViewName(TX.FriendlyViewName.Appointment.APPOINTMENT_RESOURCES)]
        public AppointmentResources_View AppointmentResources;

        [PXCopyPasteHiddenView()]
        public PXSelectReadonly2<ARPayment,
               InnerJoin<FSAdjust,
               On<
                   ARPayment.docType, Equal<FSAdjust.adjgDocType>,
                   And<ARPayment.refNbr, Equal<FSAdjust.adjgRefNbr>>>>,
               Where<
                   FSAdjust.adjdOrderType, Equal<Current<FSServiceOrder.srvOrdType>>,
                   And<FSAdjust.adjdOrderNbr, Equal<Current<FSServiceOrder.refNbr>>>>> Adjustments;

        [PXCopyPasteHiddenView]
        public PXSelectJoin<FSPostDet,
               InnerJoin<FSSODet,
                   On<FSSODet.postID, Equal<FSPostDet.postID>>,
               InnerJoin<FSPostBatch,
                   On<FSPostBatch.batchID, Equal<FSPostDet.batchID>>>>,
               Where2<
                   Where<
                    FSSODet.sOID, Equal<Current<FSAppointment.sOID>>>,
                    And<
                       Where<FSPostDet.aRPosted, Equal<True>,
                       Or<FSPostDet.aPPosted, Equal<True>,
                       Or<FSPostDet.sOPosted, Equal<True>,
                       Or<FSPostDet.pMPosted, Equal<True>,
                       Or<FSPostDet.sOInvPosted, Equal<True>,
                       Or<FSPostDet.iNPosted, Equal<True>>>>>>>>>,
               OrderBy<
                   Desc<FSPostDet.batchID,
                   Desc<FSPostDet.aRPosted,
                   Desc<FSPostDet.aPPosted,
                   Desc<FSPostDet.sOPosted,
                   Desc<FSPostDet.aPPosted,
                   Desc<FSPostDet.pMPosted,
                   Desc<FSPostDet.sOInvPosted,
                   Desc<FSPostDet.iNPosted>>>>>>>>>> ServiceOrderPostedIn;

        [PXFilterable]
        [PXImport(typeof(FSAppointment))]
        [PXCopyPasteHiddenView]
        public AppointmentLog_View LogRecords;

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public PXSelect<FSAppointmentLogExtItemLine,
            Where2<
                Where<
                   FSAppointmentLogExtItemLine.docID, Equal<Current<FSAppointment.appointmentID>>,
                And<
                    Where<Current<FSLogActionFilter.me>, Equal<False>,
                    Or<FSAppointmentLogExtItemLine.userID, Equal<Current<AccessInfo.userID>>>>>>,
            And2<
                Where2<
                        Where<Current<FSLogActionFilter.type>, Equal<FSLogTypeAction.Travel>,
                        And<FSAppointmentLogExtItemLine.itemType, Equal<ListField_LogAction_Type.travel>>>,
                    Or<
                        Where<Current<FSLogActionFilter.type>, NotEqual<FSLogTypeAction.Travel>,
                        And<FSAppointmentLogExtItemLine.itemType, NotEqual<ListField_LogAction_Type.travel>>>>>,
                And<
                    Where2<
                        Where<
                            Current<FSLogActionFilter.action>, Equal<FSLogActionFilter.action.Pause>,
                            And<FSAppointmentLogExtItemLine.status, Equal<FSAppointmentLogExtItemLine.status.InProcess>>>,
                        Or<
                            Where2<
                                Where<
                                    Current<FSLogActionFilter.action>, Equal<FSLogActionFilter.action.Resume>,
                                    And<FSAppointmentLogExtItemLine.status, Equal<FSAppointmentLogExtItemLine.status.Paused>>>,
                                Or<
                                    Where<
                                        Current<FSLogActionFilter.action>, Equal<FSLogActionFilter.action.Complete>,
                                        And<
                                            Where<FSAppointmentLogExtItemLine.status, Equal<FSAppointmentLogExtItemLine.status.InProcess>,
                                            Or<FSAppointmentLogExtItemLine.status, Equal<FSAppointmentLogExtItemLine.status.Paused>>>>>>>>>>>>,
            OrderBy<
                   Desc<FSAppointmentLogExtItemLine.selected>>> LogActionLogRecords;

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public PXSelect<FSAppointmentStaffExtItemLine,
               Where<
                   FSAppointmentStaffExtItemLine.docID, Equal<Current<FSAppointment.appointmentID>>,
                   And<
                       Where<
                           Current<FSLogActionFilter.me>, Equal<True>,
                           And<FSAppointmentStaffExtItemLine.userID, Equal<Current<AccessInfo.userID>>,
                           Or<Current<FSLogActionFilter.me>, Equal<False>>>>>>,
               OrderBy<
                   Desc<FSAppointmentStaffExtItemLine.selected>>> LogActionStaffRecords;

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public PXSelect<FSAppointmentStaffDistinct,
               Where<
                   FSAppointmentStaffDistinct.docID, Equal<Current<FSAppointment.appointmentID>>>,
               OrderBy<
                   Desc<FSAppointmentStaffDistinct.selected>>> LogActionStaffDistinctRecords;

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public PXSelect<FSDetailFSLogAction,
               Where<
                   FSDetailFSLogAction.appointmentID, Equal<Current<FSAppointment.appointmentID>>>> ServicesLogAction;

        public PXSetup<FSServiceContract>.Where<
               Where<
                   FSServiceContract.serviceContractID, Equal<Current<FSAppointment.billServiceContractID>>>> StandardContractRelated;

        // TODO: move the billingBy condition to the selector.
        public PXSetup<FSContractPeriod>.Where<
               Where<
                   FSContractPeriod.serviceContractID, Equal<Current<FSAppointment.billServiceContractID>>,
                   And<FSContractPeriod.contractPeriodID, Equal<Current<FSAppointment.billContractPeriodID>>,
                   And<Current<FSBillingCycle.billingBy>, Equal<FSBillingCycle.billingBy.Appointment>>>>> StandardContractPeriod;

        public PXSelect<FSContractPeriodDet,
               Where<
                   FSContractPeriodDet.serviceContractID, Equal<Current<FSContractPeriod.serviceContractID>>,
                   And<FSContractPeriodDet.contractPeriodID, Equal<Current<FSContractPeriod.contractPeriodID>>>>> StandardContractPeriodDetail;

        [PXHidden]
        public PXSelect<Contract,
               Where<
                   Contract.contractID, Equal<Required<Contract.contractID>>>> ContractRelatedToProject;

        [PXViewName(TX.FriendlyViewName.Appointment.APPOINTMENT_POSTED_IN)]
        [PXCopyPasteHiddenView]
        public PXSelectJoin<FSPostDet,
               LeftJoin<FSAppointmentDet,
               On<
                   FSAppointmentDet.postID, Equal<FSPostDet.postID>>,
               LeftJoin<FSPostBatch,
               On<
                   FSPostBatch.batchID, Equal<FSPostDet.batchID>>>>,
               Where2<
                   Where<
                   FSAppointmentDet.appointmentID, Equal<Current<FSAppointment.appointmentID>>>,
                   And<
                       Where<FSPostDet.aRPosted, Equal<True>,
                       Or<FSPostDet.aPPosted, Equal<True>,
                       Or<FSPostDet.sOPosted, Equal<True>,
                       Or<FSPostDet.pMPosted, Equal<True>,
                       Or<FSPostDet.sOInvPosted, Equal<True>,
                       Or<FSPostDet.iNPosted, Equal<True>>>>>>>>>,
               OrderBy<
                   Desc<FSPostDet.batchID,
                   Desc<FSPostDet.aRPosted,
                   Desc<FSPostDet.aPPosted,
                   Desc<FSPostDet.sOPosted,
                   Desc<FSPostDet.pMPosted,
                   Desc<FSPostDet.sOInvPosted,
                   Desc<FSPostDet.iNPosted>>>>>>>>> AppointmentPostedIn;

        [PXCopyPasteHiddenView]
        public PXSelect<FSBillHistory,
                Where<
                    FSBillHistory.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>,
                    And<FSBillHistory.appointmentRefNbr, Equal<Current<FSAppointment.refNbr>>>>,
               OrderBy<
                   Desc<FSBillHistory.createdDateTime>>> InvoiceRecords;

        [PXCopyPasteHiddenView]
        public PXSelect<FSSchedule,
               Where<
                   FSSchedule.scheduleID, Equal<Current<FSAppointment.scheduleID>>>> ScheduleRecord;

        [PXViewName(CR.Messages.MainContact)]
        public PXSelect<Contact> DefaultCompanyContact;

        [PXCopyPasteHiddenView]
        public PXSelectReadonly<FSProfitability> ProfitabilityRecords;

        [PXViewName(CR.Messages.Answers)]
        public FSAttributeList<FSAppointment> Answers;

        public PXFilter<FSSiteStatusFilter> sitestatusfilter;
        [PXFilterable]
        [PXCopyPasteHiddenView]
        public FSSiteStatusLookup<FSSiteStatusSelected, FSSiteStatusFilter> sitestatus;

        [PXCopyPasteHiddenView()]
        [PXFilterable]
        public PXSelect<FSApptLineSplit,
               Where<
                   FSApptLineSplit.srvOrdType, Equal<Current<FSAppointmentDet.srvOrdType>>,
                   And<FSApptLineSplit.apptNbr, Equal<Current<FSAppointmentDet.refNbr>>,
                   And<FSApptLineSplit.lineNbr, Equal<Current<FSAppointmentDet.lineNbr>>>>>> Splits;

        public LSFSApptLine lsselect;

        #region Tax Extension Views
        [PXCopyPasteHiddenView]
        public PXSelect<FSAppointmentTax,
               Where<
                   FSAppointmentTax.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>,
                   And<FSAppointmentTax.refNbr, Equal<Current<FSAppointment.refNbr>>>>,
               OrderBy<
                   Asc<FSAppointmentTax.taxID>>> TaxLines;

        [PXViewName(TX.Messages.AppointmentTax)]
        [PXCopyPasteHiddenView]
        public PXSelectJoin<FSAppointmentTaxTran,
               LeftJoin<Tax,
                   On<Tax.taxID, Equal<FSAppointmentTaxTran.taxID>>>,
               Where<
                   FSAppointmentTaxTran.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>,
                   And<FSAppointmentTaxTran.refNbr, Equal<Current<FSAppointment.refNbr>>>>,
               OrderBy<
                   Asc<FSAppointmentTaxTran.taxID,
                   Asc<FSAppointmentTaxTran.recordID>>>> Taxes;
        #endregion

        [InjectDependency]
        protected ILicenseLimitsService _licenseLimits { get; set; }
        #endregion

        #region Overrides
        public virtual void MyPersist()
        {
            serviceOrderIsAlreadyUpdated = false;
            CatchedServiceOrderUpdateException = null;
            serviceOrderRowPersistedPassedWithStatusAbort = false;
            insertingServiceOrder = false;
            FSAppointment currentAppt = AppointmentRecords.Current;

            try
            {
                using (PXTransactionScope ts = new PXTransactionScope())
                {
                    try
                    {
                        base.Persist(typeof(FSServiceOrder), PXDBOperation.Insert);
                        base.Persist(typeof(FSServiceOrder), PXDBOperation.Update);
                    }
                    catch
                    {
                        Caches[typeof(FSServiceOrder)].Persisted(true);
                        throw;
                    }

                    try
                    {
                        if (RecalculateExternalTaxesSync && currentAppt?.SkipExternalTaxCalculation == false)
                        {
                            RecalculateExternalTaxes();
                            this.SelectTimeStamp();
                        }

                        SplitAppoinmentLogLinesByDays();

                        base.Persist();

                        if (!RecalculateExternalTaxesSync && currentAppt?.SkipExternalTaxCalculation == false) //When the calling process is the 'UI' thread.
                            RecalculateExternalTaxes();
                    }
                    catch
                    {
                        if (serviceOrderRowPersistedPassedWithStatusAbort == false)
                        {
                            Caches[typeof(FSServiceOrder)].Persisted(true);
                        }

                        throw;
                    }

                    ts.Complete();
                }
            }
            finally
            {
                serviceOrderIsAlreadyUpdated = false;
                CatchedServiceOrderUpdateException = null;
                serviceOrderRowPersistedPassedWithStatusAbort = false;
                insertingServiceOrder = false;
            }
        }

        public override void Persist()
        {
            MyPersist();
        }

        #endregion
        void IGraphWithInitialization.Initialize()
        {
            if (_licenseLimits != null)
            {
                OnBeforeCommit += _licenseLimits.GetCheckerDelegate<FSAppointment>(
                new TableQuery(TransactionTypes.LinesPerMasterRecord, typeof(FSAppointmentDet), (graph) =>
                {
                    return new PXDataFieldValue[]
                    {
                        new PXDataFieldValue<FSAppointmentDet.srvOrdType>(PXDbType.Char, ((AppointmentEntry)graph).AppointmentRecords.Current?.SrvOrdType),
                        new PXDataFieldValue<FSAppointmentDet.refNbr>(((AppointmentEntry)graph).AppointmentRecords.Current?.RefNbr)
                    };
                }));
            }

            lsselect.VerifySrvOrdQtyMethod = this.VerifySrvOrdLineQty;
        }

        #region Workflow Stages tree
        public TreeWFStageHelper.TreeWFStageView TreeWFStages;

        protected virtual IEnumerable treeWFStages(
            [PXInt]
            int? wFStageID)
        {
            if (AppointmentRecords.Current == null)
            {
                return null;
            }


            return TreeWFStageHelper.treeWFStages(this, AppointmentRecords.Current.SrvOrdType, wFStageID);
        }
        #endregion

        /// <summary>
        /// Gets the license types related for the given appointment services. Also sets a list with the License Type identifiers 
        /// related to the appointment services.
        /// </summary>
        /// <param name="bqlResultSet">Set of appointment detail services.</param>
        /// <param name="serviceLicenseIDs">This list contains the union of all license types related to the given appointment services.</param>
        /// <returns>List of services with their respective related license types.</returns>
        public virtual List<ServiceRequirement> GetAppointmentDetServiceRowLicenses(List<FSAppointmentDet> appointmentServiceDetails,
                                                                                    ref List<int?> serviceLicenseIDs)
        {
            List<ServiceRequirement> serviceLicensesList = new List<ServiceRequirement>();
            List<object> args = new List<object>();

            BqlCommand fsServiceLicenseRows = new Select2<FSServiceLicenseType,
                                                  InnerJoin<InventoryItem,
                                                  On<
                                                      FSServiceLicenseType.serviceID, Equal<InventoryItem.inventoryID>>>,
                                                  Where<True, Equal<True>>>();

            fsServiceLicenseRows = fsServiceLicenseRows.WhereAnd(InHelper<InventoryItem.inventoryID>.Create(appointmentServiceDetails.Count));

            foreach (FSAppointmentDet fsAppointmentDetRow in appointmentServiceDetails)
            {
                args.Add(fsAppointmentDetRow.InventoryID);
            }

            PXView serviceLicensesView = new PXView(this, true, fsServiceLicenseRows);
            var fsServiceLicenseTypeSet = serviceLicensesView.SelectMulti(args.ToArray());

            if (fsServiceLicenseTypeSet.Count == 0)
            {
                return serviceLicensesList;
            }

            foreach (PXResult<FSServiceLicenseType, InventoryItem> bqlResult in fsServiceLicenseTypeSet)
            {
                InventoryItem fsServiceRow = (InventoryItem)bqlResult;
                FSServiceLicenseType fsServiceLicenseTypeRow = (FSServiceLicenseType)bqlResult;
                serviceLicenseIDs.Add(fsServiceLicenseTypeRow.LicenseTypeID);

                var serviceLicenses = serviceLicensesList.Where(list => list.serviceID == fsServiceRow.InventoryID).FirstOrDefault();

                if (serviceLicenses != null)
                {
                    serviceLicenses.requirementIDList.Add((int)fsServiceLicenseTypeRow.LicenseTypeID);
                }
                else
                {
                    ServiceRequirement newServiceLicenses = new ServiceRequirement()
                    {
                        serviceID = (int)fsServiceRow.InventoryID
                    };
                    newServiceLicenses.requirementIDList.Add(fsServiceLicenseTypeRow.LicenseTypeID);
                    serviceLicensesList.Add(newServiceLicenses);
                }
            }

            return serviceLicensesList;
        }

        /// <summary>
        /// Gets the license types related for the given appointment employees.  
        /// </summary>
        /// <param name="bqlResultSet">Set of appointment employees.</param>
        /// <returns>List of unexpired license identifiers owned by the given appointment employees.</returns>
        public virtual List<int?> GetAppointmentEmpoyeeLicenseIDs(PXResultset<FSAppointmentEmployee> bqlResultSet)
        {
            List<int?> appointmentEmployeeLicenseIDList = new List<int?>();
            List<object> args = new List<object>();
            DateTime tempDate = new DateTime(AppointmentSelected.Current.ScheduledDateTimeBegin.Value.Year,
                                             AppointmentSelected.Current.ScheduledDateTimeBegin.Value.Month,
                                             AppointmentSelected.Current.ScheduledDateTimeBegin.Value.Day);

            BqlCommand fsAppointmentEmployeeLicenseRows = new Select4<FSLicense,
                                                              Where<
                                                                  FSLicense.expirationDate, GreaterEqual<Required<FSAppointment.scheduledDateTimeBegin>>,
                                                                  Or<FSLicense.expirationDate, IsNull>>,
                                                              Aggregate<GroupBy<FSLicense.licenseTypeID>>,
                                                              OrderBy<Asc<FSLicense.licenseID>>>();

            args.Add(tempDate);

            fsAppointmentEmployeeLicenseRows = fsAppointmentEmployeeLicenseRows.WhereAnd(InHelper<FSLicense.employeeID>.Create(bqlResultSet.Count));

            foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in bqlResultSet)
            {
                args.Add(fsAppointmentEmployeeRow.EmployeeID);
            }

            PXView appointmentEmployeeLicenseView = new PXView(this, true, fsAppointmentEmployeeLicenseRows);
            var fsLicenseSet = appointmentEmployeeLicenseView.SelectMulti(args.ToArray());

            foreach (FSLicense fsLicenseRow in fsLicenseSet)
            {
                appointmentEmployeeLicenseIDList.Add(fsLicenseRow.LicenseTypeID);
            }

            return appointmentEmployeeLicenseIDList;
        }

        /// <summary>
        /// Gets the skills related for the given appointment services. Also sets a list with the skills identifiers 
        /// related to the appointment services.
        /// </summary>
        /// <param name="bqlResultSet">Set of appointment detail services.</param>
        /// <param name="serviceSkillIDs">This list contains the union of all skills related to the given appointment services.</param>
        /// <returns>List of services with their respective related skills.</returns>
        public virtual List<ServiceRequirement> GetAppointmentDetServiceRowSkills(List<FSAppointmentDet> appointmentServiceDetails,
                                                                                  ref List<int?> serviceSkillIDs)
        {
            List<ServiceRequirement> serviceSkillsList = new List<ServiceRequirement>();
            List<object> args = new List<object>();

            BqlCommand fsServiceSkillsRows = new Select2<FSServiceSkill,
                                                 InnerJoin<InventoryItem,
                                                    On<FSServiceSkill.serviceID, Equal<InventoryItem.inventoryID>>>,
                                                 Where<True, Equal<True>>>();

            fsServiceSkillsRows = fsServiceSkillsRows.WhereAnd(InHelper<InventoryItem.inventoryID>.Create(appointmentServiceDetails.Count));

            foreach (FSAppointmentDet fsAppointmentDetRow in appointmentServiceDetails)
            {
                args.Add(fsAppointmentDetRow.InventoryID);
            }

            PXView serviceSkillsView = new PXView(this, true, fsServiceSkillsRows);
            var fsServiceSkillSet = serviceSkillsView.SelectMulti(args.ToArray());

            if (fsServiceSkillSet.Count == 0)
            {
                return serviceSkillsList;
            }

            foreach (PXResult<FSServiceSkill, InventoryItem> bqlResult in fsServiceSkillSet)
            {
                InventoryItem fsServiceRow = (InventoryItem)bqlResult;
                FSServiceSkill fsServiceSkillRow = (FSServiceSkill)bqlResult;
                serviceSkillIDs.Add((int)fsServiceSkillRow.SkillID);

                var serviceSkills = serviceSkillsList.Where(list => list.serviceID == fsServiceRow.InventoryID).FirstOrDefault();
                if (serviceSkills != null)
                {
                    serviceSkills.requirementIDList.Add((int)fsServiceSkillRow.SkillID);
                }
                else
                {
                    ServiceRequirement newServiceSkills = new ServiceRequirement()
                    {
                        serviceID = (int)fsServiceRow.InventoryID
                    };

                    newServiceSkills.requirementIDList.Add((int)fsServiceSkillRow.SkillID);
                    serviceSkillsList.Add(newServiceSkills);
                }
            }

            return serviceSkillsList;
        }

        /// <summary>
        /// Gets the skills related for the given appointment employees.  
        /// </summary>
        /// <param name="bqlResultSet">Set of appointment employees.</param>
        /// <returns>List of skill identifiers owned by the given appointment employees.</returns>
        public virtual List<int?> GetAppointmentEmpoyeeSkillIDs(PXResultset<FSAppointmentEmployee> bqlResultSet)
        {
            List<int?> appointmentEmployeeSkillIDList = new List<int?>();
            List<object> args = new List<object>();

            BqlCommand fsAppointmentEmployeeSkillRows = new Select4<FSEmployeeSkill,
                                                            Where<True, Equal<True>>,
                                                            Aggregate<GroupBy<FSEmployeeSkill.skillID>>,
                                                            OrderBy<Asc<FSEmployeeSkill.skillID>>>();

            fsAppointmentEmployeeSkillRows = fsAppointmentEmployeeSkillRows.WhereAnd(InHelper<FSEmployeeSkill.employeeID>.Create(bqlResultSet.Count));
            foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in bqlResultSet)
            {
                args.Add(fsAppointmentEmployeeRow.EmployeeID);
            }

            PXView appointmentEmployeeSkillView = new PXView(this, true, fsAppointmentEmployeeSkillRows);
            var fsEmployeeSkillSet = appointmentEmployeeSkillView.SelectMulti(args.ToArray());

            foreach (FSEmployeeSkill fsEmployeeSkillRow in fsEmployeeSkillSet)
            {
                appointmentEmployeeSkillIDList.Add(fsEmployeeSkillRow.SkillID);
            }

            return appointmentEmployeeSkillIDList;
        }

        /// <summary>
        /// Updates ProjectID in the Lines of the Appointment using the project in the <c>fsServiceOrderRow</c>. Also, sets ProjectTaskID to null.
        /// </summary>
        public virtual void UpdateDetailsFromProjectID(int? projectID)
        {
            if (projectID == null)
            {
                return;
            }

            if (AppointmentDetails != null)
            {
                foreach (FSAppointmentDet fsAppointmentDetRow in AppointmentDetails.Select())
                {
                    fsAppointmentDetRow.ProjectID = projectID;
                    fsAppointmentDetRow.ProjectTaskID = null;
                    AppointmentDetails.Update(fsAppointmentDetRow);
                }
            }
        }

        /// <summary>
        /// Updates BranchID in the Lines of the Appointment using the branch in the <c>fsServiceOrderRow</c>.
        /// </summary>
        public virtual void UpdateDetailsFromBranchID(FSServiceOrder fsServiceOrderRow)
        {
            if (fsServiceOrderRow.BranchID == null)
            {
                return;
            }

            if (AppointmentDetails != null)
            {
                foreach (FSAppointmentDet fsAppointmentDetRow in AppointmentDetails.Select())
                {
                    fsAppointmentDetRow.BranchID = fsServiceOrderRow.BranchID;
                    AppointmentDetails.Update(fsAppointmentDetRow);
                }
            }
        }

        public virtual void CalculateLaborCosts()
        {
            object unitcost;

            var employeeLogRecords = LogRecords.Select().RowCast<FSAppointmentLog>().Where(x => x.BAccountID != null);

            foreach (FSAppointmentLog fsAppointmentLogRow in employeeLogRecords)
            {
                LogRecords.Cache.RaiseFieldDefaulting<FSAppointmentLog.unitCost>(fsAppointmentLogRow, out unitcost);
                
                fsAppointmentLogRow.UnitCost = (decimal)unitcost;
                decimal newval = (decimal)unitcost;
                CM.PXDBCurrencyAttribute.CuryConvCury(LogRecords.Cache, fsAppointmentLogRow, newval, out newval, CommonSetupDecPl.PrcCst);
                fsAppointmentLogRow.CuryUnitCost = newval;

                LogRecords.Update(fsAppointmentLogRow);
            }
        }

        public virtual void CalculateCosts()
        {
            object unitcost;

            var nonStockItemRows = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                     .Where(x => x.LineType == ID.LineType_ALL.NONSTOCKITEM
                                                              && x.IsCanceledNotPerformed != true
                                                              && x.IsLinkedItem == false);

            foreach (FSAppointmentDet fsAppointmentDetRow in nonStockItemRows)
            {
                AppointmentDetails.Cache.RaiseFieldDefaulting<FSAppointmentDet.unitCost>(fsAppointmentDetRow, out unitcost);
                fsAppointmentDetRow.UnitCost = (decimal)unitcost;

                decimal newval = INUnitAttribute.ConvertToBase<FSAppointmentDet.inventoryID, FSAppointmentDet.uOM>(AppointmentDetails.Cache, fsAppointmentDetRow, (decimal)unitcost, INPrecision.NOROUND);
                CM.PXDBCurrencyAttribute.CuryConvCury(AppointmentDetails.Cache, fsAppointmentDetRow, newval, out newval, CommonSetupDecPl.PrcCst);
                fsAppointmentDetRow.CuryUnitCost = newval;

                AppointmentDetails.Update(fsAppointmentDetRow);
            }

            CalculateLaborCosts();
        }

        public virtual decimal? CalculateLaborCost(PXCache cache, FSAppointmentLog fsAppointmentLogRow, FSAppointment fsAppointmentRow)
        {
            if (fsAppointmentLogRow.LaborItemID == null)
            {
                return null;
            }

            PMLaborCostRate laborCostRate = null;

            laborCostRate = PXSelect<PMLaborCostRate,
                            Where<
                                PMLaborCostRate.type, Equal<PMLaborCostRateType.employee>,
                            And<
                                PMLaborCostRate.employeeID, Equal<Required<PMLaborCostRate.employeeID>>,
                            And<
                                PMLaborCostRate.inventoryID, Equal<Required<PMLaborCostRate.inventoryID>>,
                            And<
                                PMLaborCostRate.employmentType, Equal<EP.RateTypesAttribute.hourly>,
                            And<
                                PMLaborCostRate.curyID, Equal<Required<PMLaborCostRate.curyID>>,
                            And<
                                PMLaborCostRate.effectiveDate, LessEqual<Required<PMLaborCostRate.effectiveDate>>>>>>>>,
                            OrderBy<
                                Desc<PMLaborCostRate.effectiveDate>>>
                            .Select(this, fsAppointmentLogRow.BAccountID, fsAppointmentLogRow.LaborItemID, fsAppointmentRow.CuryID, fsAppointmentRow.ExecutionDate)
                            .AsEnumerable()
                            .FirstOrDefault();

            if (laborCostRate == null)
            {
                laborCostRate = PXSelect<PMLaborCostRate,
                                Where<
                                    PMLaborCostRate.type, Equal<PMLaborCostRateType.item>,
                                And<
                                    PMLaborCostRate.inventoryID, Equal<Required<PMLaborCostRate.inventoryID>>,
                                And<
                                    PMLaborCostRate.employmentType, Equal<EP.RateTypesAttribute.hourly>,
                                And<
                                    PMLaborCostRate.curyID, Equal<Required<PMLaborCostRate.curyID>>,
                                And<
                                    PMLaborCostRate.effectiveDate, LessEqual<Required<PMLaborCostRate.effectiveDate>>>>>>>,
                                OrderBy<
                                    Desc<PMLaborCostRate.effectiveDate>>>
                                .Select(this, fsAppointmentLogRow.LaborItemID, fsAppointmentRow.CuryID, fsAppointmentRow.ExecutionDate)
                                .AsEnumerable()
                                .FirstOrDefault();
            }

            return laborCostRate != null ? laborCostRate.Rate : null;
        }

        public virtual IEnumerable profitabilityRecords()
        {
            return Delegate_ProfitabilityRecords(AppointmentRecords.Cache, AppointmentRecords.Current);
        }

        public virtual List<FSProfitability> Delegate_ProfitabilityRecords(PXCache cache, FSAppointment fsAppointmentRow)
        {
            List<FSProfitability> returnList = new List<FSProfitability>();

            if (fsAppointmentRow == null)
            {
                return returnList;
            }

            var itemsWithProfit = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                    .Where(x => (x.IsInventoryItem
                                                                    || x.LineType == ID.LineType_ALL.NONSTOCKITEM
                                                                    || (x.LineType == ID.LineType_ALL.SERVICE && x.CuryUnitCost != 0m))
                                                              && x.IsCanceledNotPerformed != true);

            foreach (FSAppointmentDet fsAppDetRow in itemsWithProfit)
            {
                FSProfitability fsProfitabilityRow = new FSProfitability(fsAppDetRow);
                returnList.Add(fsProfitabilityRow);
            }

            foreach (FSAppointmentLog fsLogRow in LogRecords.Select().RowCast<FSAppointmentLog>()
                                                            .Where(x => x.BAccountID != null && x.CuryUnitCost != 0m))
            {
                FSProfitability fsProfitabilityRow = new FSProfitability(fsLogRow);
                InventoryItem inventoyItemRow = SharedFunctions.GetInventoryItemRow(this, fsLogRow.LaborItemID);
                fsProfitabilityRow.Descr = inventoyItemRow.Descr;
                returnList.Add(fsProfitabilityRow);
            }

            return returnList;
        }

        #region Virtual Methods
        /// <summary>
        /// Sends Mail.
        /// </summary>
        public virtual void SendNotification(AppointmentEntry graph, PXCache cache, string mailing, int? branchID, IList<Guid?> attachments = null)
        {
            SendNotification(graph, cache, mailing, branchID, null, attachments);
        }

        public virtual void FillDocDesc(FSAppointment fsAppointmentRow)
        {
            FSAppointmentDet fsAppointmentDetRow_InDB = PXSelect<FSAppointmentDet,
                                                        Where<
                                                            FSAppointmentDet.appointmentID, Equal<Required<FSAppointmentDet.appointmentID>>>,
                                                        OrderBy<
                                                            Asc<FSAppointmentDet.sODetID>>>
                                                        .SelectWindowed(this, 0, 1, fsAppointmentRow.AppointmentID);

            fsAppointmentRow.DocDesc = fsAppointmentDetRow_InDB?.TranDesc;
        }

        /// <summary>
        /// Sets the TimeRegister depending on <c>Setup.RequireTimeApprovalToInvoice</c> and ActualTime.
        /// </summary>
        public virtual void SetTimeRegister(FSAppointment fsAppointmentRow, FSSrvOrdType fsSrvOrdTypeRow, PXDBOperation operation)
        {
            bool? timeRegisteredNewValue = true;

            if (fsSrvOrdTypeRow.RequireTimeApprovalToInvoice == true
                    && operation == PXDBOperation.Update)
            {
                var result = PXSelect<FSAppointmentLog,
                             Where<
                                 FSAppointmentLog.approvedTime, Equal<False>,
                                 And<FSAppointmentLog.trackTime, Equal<True>,
                                 And<FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>>>>>
                             .SelectWindowed(this, 0, 1, AppointmentSelected.Current.AppointmentID);

                if (result.Count > 0 || LogRecords.Select().Count == 0)
                {
                    timeRegisteredNewValue = false;
                }
            }
            else if (fsAppointmentRow.ActualDateTimeBegin == null)
            {
                timeRegisteredNewValue = false;
            }

            fsAppointmentRow.TimeRegistered = timeRegisteredNewValue;
        }

        public virtual void CalculateEndTimeWithLinesDuration(PXCache cache,
                                                              FSAppointment fsAppointmentRow,
                                                              DateFieldType dateFieldType,
                                                              bool forceUpdate = false)
        {
            if (forceUpdate == false
                    && (AppointmentRecords.Current == null || AppointmentRecords.Current.isBeingCloned == true)
            )
            {
                return;
            }

            bool handleTimeManually = (bool)fsAppointmentRow.HandleManuallyScheduleTime;
            DateTime? dateTimeBegin = fsAppointmentRow.ScheduledDateTimeBegin;
            int duration = (int)fsAppointmentRow.EstimatedDurationTotal;

            if (dateTimeBegin != null
                    && (forceUpdate == true || handleTimeManually == false)
            )
            {
                DateTime? dateTimeEnd = dateTimeBegin.Value.AddMinutes(duration);

                bool originalFlag = SkipManualTimeFlagUpdate;

                try
                {
                    SkipManualTimeFlagUpdate = true;

                    if (dateFieldType == DateFieldType.ScheduleField)
                    {
                        cache.SetValueExtIfDifferent<FSAppointment.scheduledDateTimeEnd>(fsAppointmentRow, dateTimeEnd);
                        cache.SetValuePending(fsAppointmentRow, typeof(FSAppointment.scheduledDateTimeEnd).Name, PXCache.NotSetValue);
                        cache.SetValuePending(fsAppointmentRow, typeof(FSAppointment.scheduledDateTimeEnd).Name + PXDBDateAndTimeAttribute.TIME_FIELD_POSTFIX, PXCache.NotSetValue);
                    }
                }
                finally
                {
                    SkipManualTimeFlagUpdate = originalFlag;
                }
            }
        }

        #region EnableDisable
        /// <summary>
        /// Check the ManageRooms value on Setup to check/hide the Rooms Values options.
        /// </summary>
        public virtual void HideRooms(FSAppointment fsAppointmentRow, FSSetup fSSetupRow)
        {
            bool isRoomManagementActive = ServiceManagementSetup.IsRoomManagementActive(this, fSSetupRow);

            FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.SelectSingle();
            PXUIFieldAttribute.SetVisible<FSServiceOrder.roomID>(this.ServiceOrderRelated.Cache, fsServiceOrderRow, isRoomManagementActive);
            openRoomBoard.SetVisible(isRoomManagementActive);
        }
        #endregion

        public virtual void SetServiceOrderStatusFromAppointment(FSServiceOrder fsServiceOrderRow, FSAppointment fsAppointmentRow, ActionButton action)
        {
            if (this.SkipCallSOAction == true
                || (action == ActionButton.CompleteAppointment && ServiceOrderTypeSelected.Current.CompleteSrvOrdWhenSrvDone == false)
                || (action == ActionButton.CloseAppointment && ServiceOrderTypeSelected.Current.CloseSrvOrdWhenSrvDone == false))
            { 
                return;
            }

            if (IsUpdatingTheLatestActiveAppointmentOfServiceOrder(fsAppointmentRow, considerCompletedStatus: fsAppointmentRow.Closed == true))
            {
                ServiceOrderEntry graphServiceOrderEntry = GetServiceOrderEntryGraph(true);
                FSServiceOrder order = graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords
                                                             .Search<FSServiceOrder.refNbr>(fsServiceOrderRow.RefNbr, fsServiceOrderRow.SrvOrdType);

                if (action == ActionButton.CompleteAppointment)
                {
                    if (fsServiceOrderRow.OpenDoc == true)
                    {
                        order.CompleteAppointments = false;
                        
                        FSServiceOrder.Events.Select(ev => ev.LastAppointmentCompleted).FireOn(graphServiceOrderEntry, order);
                        graphServiceOrderEntry.ServiceOrderRecords.Update(order);
                        graphServiceOrderEntry.SkipTaxCalcAndSave();
                    }

                    if (fsServiceOrderRow.Closed == true)
                    {
                        FSServiceOrder.Events.Select(ev => ev.AppointmentUnclosed).FireOn(graphServiceOrderEntry, order);
                        graphServiceOrderEntry.ServiceOrderRecords.Update(order);
                        graphServiceOrderEntry.SkipTaxCalcAndSave();
                    }
                }
                else if (action == ActionButton.CloseAppointment
                            || action == ActionButton.InvoiceAppointment)
                {
                    if (fsServiceOrderRow.Completed == true)
                    {
                        order.CloseAppointments = false;

                        FSServiceOrder.Events.Select(ev => ev.LastAppointmentClosed).FireOn(graphServiceOrderEntry, order);
                        graphServiceOrderEntry.ServiceOrderRecords.Update(order);
                        graphServiceOrderEntry.SkipTaxCalcAndSave();
                    }
                }
                else if (action == ActionButton.UnCloseAppointment)
                {
                    if (fsServiceOrderRow.Closed == true)
                    {
                        FSServiceOrder.Events.Select(ev => ev.AppointmentUnclosed).FireOn(graphServiceOrderEntry, order);
                        graphServiceOrderEntry.ServiceOrderRecords.Update(order);
                        graphServiceOrderEntry.SkipTaxCalcAndSave();
                    }
                }
                else if (action == ActionButton.PutOnHold
                            || action == ActionButton.ReOpenAppointment
                            || action == ActionButton.ReleaseFromHold)
                {
                    if (fsServiceOrderRow.Canceled == true
                            || fsServiceOrderRow.Completed == true)
                    {
                        FSServiceOrder.Events.Select(ev => ev.AppointmentReOpened).FireOn(graphServiceOrderEntry, order);
                        graphServiceOrderEntry.ServiceOrderRecords.Update(order);
                        graphServiceOrderEntry.SkipTaxCalcAndSave();
                    }
                }
            }
        }

        public virtual void SetLatestServiceOrderStatusBaseOnAppointmentStatus(FSServiceOrder fsServiceOrderRow, string latestServiceOrderStatus)
        {
            if (this.SkipCallSOAction == true || string.IsNullOrEmpty(latestServiceOrderStatus))
            {
                return;
            }

            ServiceOrderEntry graphServiceOrderEntry = GetServiceOrderEntryGraph(true);

            FSServiceOrder order = graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords
                                            .Search<FSServiceOrder.refNbr>(fsServiceOrderRow.RefNbr, fsServiceOrderRow.SrvOrdType);

            if (graphServiceOrderEntry.ServiceOrderRecords.Current != null
                && graphServiceOrderEntry.ServiceOrderRecords.Current.Status == latestServiceOrderStatus)
                return;

            switch (latestServiceOrderStatus)
            {
                case FSServiceOrder.status.Values.Canceled:
                    FSServiceOrder.Events.Select(ev => ev.LastAppointmentCanceled).FireOn(graphServiceOrderEntry, order);
                    graphServiceOrderEntry.ServiceOrderRecords.Update(order);
                    graphServiceOrderEntry.Save.Press();
                    break;
                case FSServiceOrder.status.Values.Completed:
                    FSServiceOrder.Events.Select(ev => ev.LastAppointmentCompleted).FireOn(graphServiceOrderEntry, order);
                    graphServiceOrderEntry.ServiceOrderRecords.Update(order);
                    graphServiceOrderEntry.Save.Press();
                    break;
            }
        }

        public virtual bool IsUpdatingTheLatestActiveAppointmentOfServiceOrder(FSAppointment changingAppointment, bool considerCompletedStatus = false)
        {
            if ((changingAppointment.Completed == true && ServiceOrderTypeSelected.Current.CompleteSrvOrdWhenSrvDone == false)
                || (changingAppointment.Closed == true && ServiceOrderTypeSelected.Current.CloseSrvOrdWhenSrvDone == false))
            {
                return false;
            }

            BqlCommand otherApptCommand = new Select<FSAppointment,
                                            Where<
                                                FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>,
                                                And<FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>>();
            List<object> args = new List<object>();

            args.Add(changingAppointment.AppointmentID);
            args.Add(changingAppointment.SOID);

            if (considerCompletedStatus == false)
            {
                otherApptCommand = otherApptCommand.WhereAnd(typeof(Where<FSAppointment.notStarted, Equal<True>, Or<FSAppointment.inProcess, Equal<True>>>));
            } else
            {
                otherApptCommand = otherApptCommand.WhereAnd(typeof(Where<FSAppointment.notStarted, Equal<True>, Or<FSAppointment.inProcess, Equal<True>,
                                                                        Or<Where<FSAppointment.completed, Equal<True>, And<FSAppointment.closed, Equal<False>>>>>>));
            }

            PXView otherApptView = new PXView(this, true, otherApptCommand);

            var otherAppts = otherApptView.SelectMulti(args.ToArray());

            if (otherAppts != null && otherAppts.Count > 0)
            {
                return false;
            }

            if ((changingAppointment.Completed == true || changingAppointment.CompleteActionRunning == true) && ServiceOrderTypeSelected.Current.CompleteSrvOrdWhenSrvDone == true)
            {
                int scheduleNeededCount = PXSelect<FSSODet,
                    Where<FSSODet.sOID, Equal<Required<FSSODet.sOID>>,
                        And<FSSODet.status, Equal<FSSODet.status.ScheduleNeeded>,
                        And<FSSODet.lineType, NotEqual<ListField_LineType_ALL.Instruction>,
                        And<FSSODet.lineType, NotEqual<ListField_LineType_ALL.Comment>>>>>>.
                    Select(this, changingAppointment.SOID).Count();

                if (scheduleNeededCount > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public virtual string GetFinalServiceOrderStatus(FSServiceOrder fsServiceOrderRow, FSAppointment fsAppointmentRow)
        {
            bool lastAppointment = false;

            lastAppointment = IsUpdatingTheLatestActiveAppointmentOfServiceOrder(fsAppointmentRow);

            if (lastAppointment == true)
            {
                var fsAppointmentRow_tmp = PXSelect<FSAppointment,
                            Where<
                                FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>,
                                And<
                                    FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>,
                                And<
                                    Where<
                                        FSAppointment.canceled, Equal<False>>>>>>
                            .SelectWindowed(this, 0, 1, fsAppointmentRow.AppointmentID, fsAppointmentRow.SOID);

                int notCanceledCount = fsAppointmentRow_tmp.Count;

                if (notCanceledCount == 0)
                {
                    return FSServiceOrder.status.Values.Canceled;
                }

                if (fsServiceOrderRow.Completed == false
                     && fsServiceOrderRow.Closed == false)
                {
                    var pendingLines = PXSelect<FSSODet,
                            Where<
                                FSSODet.sOID, Equal<Required<FSSODet.sOID>>,
                                And<Where<
                                    FSSODet.status, Equal<FSSODet.status.ScheduleNeeded>,
                                    Or<FSSODet.status, Equal<FSSODet.status.Scheduled>>>>>>
                            .SelectWindowed(this, 0, 1, fsAppointmentRow.SOID);

                    if (pendingLines.Count == 0)
                    {
                        var response = AppointmentRecords.Ask(
                                                     TX.WebDialogTitles.CONFIRM_SERVICE_ORDER_COMPLETING,
                                                     TX.Messages.ASK_CONFIRM_SERVICE_ORDER_COMPLETING,
                                                     MessageButtons.YesNo);
                        if (response == WebDialogResult.Yes)
                        {
                            return FSServiceOrder.status.Values.Completed;
                        }
                    }
                }
            }

            return string.Empty;
        }

        public virtual void CheckScheduledDateTimes(PXCache cache,
                                                    FSAppointment fsAppointmentRow)
        {
            if (fsAppointmentRow.ScheduledDateTimeBegin == null
                || fsAppointmentRow.ScheduledDateTimeEnd == null
                    || fsAppointmentRow.HandleManuallyScheduleTime == false)
            {
                return;
            }

            PXSetPropertyException exception = CheckDateTimes((DateTime)fsAppointmentRow.ScheduledDateTimeBegin,
                                                              (DateTime)fsAppointmentRow.ScheduledDateTimeEnd,
                                                              true);

            if (exception != null)
            {
                cache.RaiseExceptionHandling<FSAppointment.scheduledDateTimeEnd>(fsAppointmentRow,
                                                                                 fsAppointmentRow.ScheduledDateTimeEnd,
                                                                                 exception);
            }
            else
            {
                cache.RaiseExceptionHandling<FSAppointment.scheduledDateTimeEnd>(fsAppointmentRow,
                                                                                 fsAppointmentRow.ScheduledDateTimeEnd,
                                                                                 null);
            }
        }

        public virtual PXSetPropertyException CheckDateTimes(DateTime actualDateTimeBegin,
                                                             DateTime actualDateTimeEnd,
                                                             bool isScheduled)
        {
            PXSetPropertyException exception = null;

            if (actualDateTimeBegin > actualDateTimeEnd)
            {
                if (isScheduled == true)
                {
                    exception = new PXSetPropertyException(TX.Error.APPOINTMENT_SCHEDULED_END_DATE_CANNOT_BE_PRIOR_SCHEDULED_START_DATE, PXErrorLevel.RowError);
                }
                else
                {
                    exception = new PXSetPropertyException(TX.Error.APPOINTMENT_ACTUAL_END_DATE_CANNOT_BE_PRIOR_ACTUAL_START_DATE, PXErrorLevel.RowError);
                }
            }

            return exception;
        }

        public virtual void CheckActualDateTimes(PXCache cache, FSAppointment fsAppointmentRow)
        {
            if (fsAppointmentRow.ActualDateTimeBegin == null
                || fsAppointmentRow.ActualDateTimeEnd == null)
            {
                return;
            }

            PXSetPropertyException exception = CheckDateTimes((DateTime)fsAppointmentRow.ActualDateTimeBegin,
                                                              (DateTime)fsAppointmentRow.ActualDateTimeEnd,
                                                              false);

            PXFieldState actualDateTimeEndField = (PXFieldState)cache.GetStateExt<FSAppointment.actualDateTimeEnd>(fsAppointmentRow);

            if (actualDateTimeEndField.Error == null)
            {
                cache.RaiseExceptionHandling<FSAppointment.actualDateTimeEnd>(fsAppointmentRow, fsAppointmentRow.ActualDateTimeEnd, exception);
            }

            if (fsAppointmentRow.BillContractPeriodID != null
                        && StandardContractPeriod.Current != null)
            {
                if (StandardContractPeriod.Current.StartPeriodDate.HasValue
                    && fsAppointmentRow.ActualDateTimeBegin < StandardContractPeriod.Current.StartPeriodDate)
                {
                    cache.RaiseExceptionHandling<FSAppointment.executionDate>(fsAppointmentRow,
                                                                              fsAppointmentRow.ExecutionDate,
                                                                              new PXSetPropertyException(TX.Error.APPOINTMENT_ACTUAL_START_DATE_CANNOT_BE_PRIOR_CONTRACT_PERIOD_START_DATE,
                                                                              PXErrorLevel.RowError));

                    cache.RaiseExceptionHandling<FSAppointment.actualDateTimeBegin>(fsAppointmentRow,
                                                                                    fsAppointmentRow.ActualDateTimeBegin,
                                                                                    new PXSetPropertyException(TX.Error.APPOINTMENT_ACTUAL_START_DATE_CANNOT_BE_PRIOR_CONTRACT_PERIOD_START_DATE,
                                                                                    PXErrorLevel.RowError));
                }

                if (StandardContractPeriod.Current.EndPeriodDate.HasValue
                    && fsAppointmentRow.ActualDateTimeEnd >= StandardContractPeriod.Current.EndPeriodDate.Value.AddDays(1))
                {
                    cache.RaiseExceptionHandling<FSAppointment.actualDateTimeEnd>(fsAppointmentRow,
                                                                                  fsAppointmentRow.ActualDateTimeEnd,
                                                                                  new PXSetPropertyException(TX.Error.APPOINTMENT_ACTUAL_END_DATE_CANNOT_BE_GREATER_CONTRACT_PERDIO_END_DATE,
                                                                                  PXErrorLevel.RowError));
                }
            }
        }

        public virtual void CheckMinMaxActualDateTimes(PXCache cache, FSAppointment fsAppointmentRow)
        {
            if (fsAppointmentRow.MinLogTimeBegin < fsAppointmentRow.ActualDateTimeBegin)
            {
                cache.RaiseExceptionHandling<FSAppointment.actualDateTimeBegin>(
                                    fsAppointmentRow,
                                    fsAppointmentRow.ActualDateTimeBegin,
                                    new PXException(TX.Error.APPOINTMENT_START_CANNOT_BE_GREATER_MIN_LOG_START));
            }

            if (fsAppointmentRow.MaxLogTimeEnd > fsAppointmentRow.ActualDateTimeEnd)
            {
                cache.RaiseExceptionHandling<FSAppointment.actualDateTimeEnd>(
                                    fsAppointmentRow,
                                    fsAppointmentRow.ActualDateTimeEnd,
                                    new PXException(TX.Error.APPOINTMENT_END_CANNOT_BE_PRIOR_MAX_LOG_END));
            }
        }

        public virtual void AutoConfirm(FSAppointment fsAppointmentRow)
        {
            if (SetupRecord.Current != null
                    && SetupRecord.Current.AppAutoConfirmGap != null
                        && SetupRecord.Current.AppAutoConfirmGap > 0)
            {
                TimeSpan? diffTimeToStart = fsAppointmentRow.ScheduledDateTimeBegin - PXTimeZoneInfo.Now;

                if (diffTimeToStart.Value.TotalMinutes <= SetupRecord.Current.AppAutoConfirmGap)
                {
                    fsAppointmentRow.Confirmed = true;
                }
            }
        }

        /// <summary>
        /// Validates if the required information in the Signature tab is complete.
        /// </summary>
        /// <param name="cache">PXCache instance.</param>
        /// <param name="fsAppointmentRow">Current FSAppointment object.</param>
        /// <param name="mustValidateSignature">Indicates if the validation process will be applied.</param>
        public virtual void ValidateSignatureFields(PXCache cache, FSAppointment fsAppointmentRow, bool mustValidateSignature)
        {
            if (mustValidateSignature
                && IsAnySignatureAttached(cache, fsAppointmentRow) == false
                && (fsAppointmentRow.CustomerSignedReport == null
                    || fsAppointmentRow.CustomerSignedReport == Guid.Empty))
            {
                throw new PXException(TX.Error.CUSTOMER_SIGNATURE_MISSING);
            }
        }

        public virtual void ValidateLicenses<fieldType>(PXCache currentCache, object currentRow)
            where fieldType : IBqlField
        {
            if (SetupRecord.Current.DenyWarnByLicense == ID.ValidationType.NOT_VALIDATE)
            {
                return;
            }

            var fsAppointmentDetServiceSet = AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(x => x.IsService);

            if (fsAppointmentDetServiceSet.Count() == 0)
            {
                return;
            }

            var fsAppointmentEmployeeSet = AppointmentServiceEmployees.Select();
            List<int?> serviceLicenseIDs = new List<int?>();
            List<int?> employeeLicenseIDs = GetAppointmentEmpoyeeLicenseIDs(fsAppointmentEmployeeSet);
            List<ServiceRequirement> serviceLicensesList = GetAppointmentDetServiceRowLicenses(fsAppointmentDetServiceSet.ToList(), ref serviceLicenseIDs);

            //verify if appointmentDetLicenseIDs list is a subset of employeeLicenseIDs list
            serviceLicenseIDs = serviceLicenseIDs.Distinct().ToList();
            bool isSubset = !serviceLicenseIDs.Except(employeeLicenseIDs).Any();

            if (!isSubset)
            {
                List<int?> missingLicenseIDs = serviceLicenseIDs.Except(employeeLicenseIDs).ToList();
                bool throwException = false;
                PXErrorLevel errorLevel;
                bool licenseIsContained = false;

                foreach (FSAppointmentDet fsAppointmentDetRow in fsAppointmentDetServiceSet)
                {
                    licenseIsContained = false;

                    ServiceRequirement serviceLicenses = serviceLicensesList.Where(list => list.serviceID == fsAppointmentDetRow.InventoryID).FirstOrDefault();

                    if (serviceLicenses != null)
                    {
                        licenseIsContained = missingLicenseIDs.Intersect(serviceLicenses.requirementIDList).Any();
                    }

                    if (licenseIsContained)
                    {
                        errorLevel = PXErrorLevel.Warning;

                        if (SetupRecord.Current.DenyWarnByLicense == ID.ValidationType.PREVENT)
                        {
                            throwException = true;
                            errorLevel = PXErrorLevel.RowError;
                        }

                        currentCache.RaiseExceptionHandling<fieldType>(currentRow,
                                                                       null,
                                                                       new PXSetPropertyException(TX.Error.SERVICE_LICENSE_TYPES_REQUIREMENTS_MISSING, errorLevel));
                    }
                }

                if (throwException)
                {
                    throw new PXException(TX.Error.SERVICE_LICENSE_TYPES_REQUIREMENTS_MISSING, PXErrorLevel.Error);
                }
            }
        }

        public virtual void ValidateSkills<fieldType>(PXCache currentCache, object currentRow)
            where fieldType : IBqlField
        {
            if (SetupRecord.Current.DenyWarnBySkill == ID.ValidationType.NOT_VALIDATE)
            {
                return;
            }

            var fsAppointmentDetServicesSet = AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(x => x.IsService);

            if (fsAppointmentDetServicesSet.Count() == 0)
            {
                return;
            }

            var fsAppointmentEmployeeSet = AppointmentServiceEmployees.Select();
            List<object> args = new List<object>();
            List<int?> serviceSkillIDs = new List<int?>();

            List<int?> employeeSkillIDs = GetAppointmentEmpoyeeSkillIDs(fsAppointmentEmployeeSet);
            List<ServiceRequirement> serviceSkillsList = GetAppointmentDetServiceRowSkills(fsAppointmentDetServicesSet.ToList(), ref serviceSkillIDs);

            //verify if appointmentDetSkillIDs list is a subset of employeeSkillIDs list
            serviceSkillIDs = serviceSkillIDs.Distinct().ToList();
            bool isSubset = !serviceSkillIDs.Except(employeeSkillIDs).Any();

            if (!isSubset)
            {
                List<int?> missingSkillIDs = serviceSkillIDs.Except(employeeSkillIDs).ToList();
                bool throwException = false;
                PXErrorLevel errorLevel;
                bool skillIsContained = false;

                foreach (FSAppointmentDet fsAppointmentDetRow in fsAppointmentDetServicesSet)
                {
                    skillIsContained = false;

                    ServiceRequirement serviceSkills = serviceSkillsList.Where(list => list.serviceID == fsAppointmentDetRow.InventoryID).FirstOrDefault();

                    if (serviceSkills != null)
                    {
                        skillIsContained = missingSkillIDs.Intersect(serviceSkills.requirementIDList).Any();
                    }

                    if (skillIsContained)
                    {
                        errorLevel = PXErrorLevel.Warning;

                        if (SetupRecord.Current.DenyWarnBySkill == ID.ValidationType.PREVENT)
                        {
                            throwException = true;
                            errorLevel = PXErrorLevel.RowError;
                        }

                        currentCache.RaiseExceptionHandling<fieldType>(currentRow,
                                                                       null,
                                                                       new PXSetPropertyException(TX.Error.SERVICE_SKILL_REQUIREMENTS_MISSING_GENERAL, errorLevel));
                    }
                }

                if (throwException)
                {
                    throw new PXException(TX.Error.SERVICE_SKILL_REQUIREMENTS_MISSING_GENERAL, PXErrorLevel.Error);
                }
            }
        }

        public virtual void ValidateGeoZones<fieldType>(PXCache currentCache, object currentRow)
            where fieldType : IBqlField
        {
            if (SetupRecord.Current.DenyWarnByGeoZone == ID.ValidationType.NOT_VALIDATE)
            {
                return;
            }

            FSAddress fsAddressRow = ServiceOrder_Address.Select();

            if (fsAddressRow == null
                    || string.IsNullOrEmpty(fsAddressRow.PostalCode) == true)
            {
                return;
            }

            var fsAppointmentEmployeeSet = AppointmentServiceEmployees.Select();
            bool throwException = false;
            PXErrorLevel errorLevel;
            List<object> args = new List<object>();
            List<int?> employeeIDList = new List<int?>();
            List<int?> employeesBelongingToTheGeozone = new List<int?>();

            BqlCommand fsGeoZoneEmpBql = new Select2<FSGeoZoneEmp,
                                             InnerJoin<FSGeoZonePostalCode,
                                             On<
                                                 FSGeoZonePostalCode.geoZoneID, Equal<FSGeoZoneEmp.geoZoneID>>>>();

            fsGeoZoneEmpBql = fsGeoZoneEmpBql.WhereAnd(InHelper<FSGeoZoneEmp.employeeID>.Create(fsAppointmentEmployeeSet.Count));

            foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in fsAppointmentEmployeeSet)
            {
                args.Add(fsAppointmentEmployeeRow.EmployeeID);
                employeeIDList.Add(fsAppointmentEmployeeRow.EmployeeID);
            }

            PXView fsGeoZoneEmpView = new PXView(this, true, fsGeoZoneEmpBql);
            var fsGeoZoneEmpSet = fsGeoZoneEmpView.SelectMulti(args.ToArray());

            foreach (PXResult<FSGeoZoneEmp, FSGeoZonePostalCode> bqlResult in fsGeoZoneEmpSet)
            {
                FSGeoZoneEmp fsGeoZoneEmpRow = (FSGeoZoneEmp)bqlResult;
                FSGeoZonePostalCode fsGeoZonePostalCodeRow = (FSGeoZonePostalCode)bqlResult;

                if (Regex.Match(fsAddressRow.PostalCode.Trim(), @fsGeoZonePostalCodeRow.PostalCode.Trim()).Success)
                {
                    employeesBelongingToTheGeozone.Add((int?)fsGeoZoneEmpRow.EmployeeID);
                }
            }

            List<int?> employeesMissingFromGeozone = employeeIDList.Except(employeesBelongingToTheGeozone).ToList();

            if (employeesMissingFromGeozone.Count > 0)
            {
                foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in fsAppointmentEmployeeSet)
                {
                    if (employeesMissingFromGeozone.IndexOf(fsAppointmentEmployeeRow.EmployeeID) != -1)
                    {
                        errorLevel = PXErrorLevel.Warning;

                        if (SetupRecord.Current.DenyWarnByGeoZone == ID.ValidationType.PREVENT)
                        {
                            throwException = true;
                            errorLevel = PXErrorLevel.RowError;
                        }

                        currentCache.RaiseExceptionHandling<fieldType>(
                                currentRow,
                                null,
                                new PXSetPropertyException(TX.Error.APPOINTMENT_EMPLOYEE_MISMATCH_GEOZONE, errorLevel));
                    }
                }

                if (throwException)
                {
                    throw new PXException(TX.Error.APPOINTMENT_EMPLOYEE_MISMATCH_GEOZONE, PXErrorLevel.Error);
                }
            }
        }

        public virtual void ClearEmployeesGrid()
        {
            foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in AppointmentServiceEmployees.Select())
            {
                AppointmentServiceEmployees.Delete(fsAppointmentEmployeeRow);
            }
        }

        public virtual void GetEmployeesFromServiceOrder(FSAppointment fsAppointmentRow)
        {
            ClearEmployeesGrid();

            var fsSOEmployeeSet = PXSelectJoin<FSSOEmployee,
                                  InnerJoin<FSServiceOrder,
                                  On<
                                      FSServiceOrder.sOID, Equal<FSSOEmployee.sOID>>>,
                                  Where<
                                      FSServiceOrder.sOID, Equal<Required<FSServiceOrder.sOID>>>>
                                  .Select(this, fsAppointmentRow.SOID);

            foreach (FSSOEmployee fsSOEmployeeRow in fsSOEmployeeSet)
            {
                FSAppointmentEmployee fsAppointmentEmployeeRow = new FSAppointmentEmployee();

                fsAppointmentEmployeeRow.EmployeeID = fsSOEmployeeRow.EmployeeID;
                fsAppointmentEmployeeRow.ServiceLineRef = fsSOEmployeeRow.ServiceLineRef;

                AppointmentServiceEmployees.Insert(fsAppointmentEmployeeRow);
            }
        }

        public virtual void ClearResourceGrid()
        {
            foreach (FSAppointmentResource fsAppointmentResourceRow in AppointmentResources.Select())
            {
                AppointmentResources.Delete(fsAppointmentResourceRow);
            }
        }

        public virtual void ClearPrepayment(FSAppointment fsAppointmentRow)
        {
            ARPaymentEntry graphARPaymentEntry = PXGraph.CreateInstance<ARPaymentEntry>();
            SM_ARPaymentEntry graphSM_ARPaymentEntry = graphARPaymentEntry.GetExtension<SM_ARPaymentEntry>();

            var adjustments = PXSelect<FSAdjust,
                              Where<
                                  FSAdjust.adjdOrderType, Equal<Required<FSAdjust.adjdOrderType>>,
                                  And<FSAdjust.adjdOrderNbr, Equal<Required<FSAdjust.adjdOrderNbr>>,
                                  And<FSAdjust.adjdAppRefNbr, Equal<Required<FSAdjust.adjdAppRefNbr>>>>>>
                              .Select(graphARPaymentEntry, fsAppointmentRow.SrvOrdType, fsAppointmentRow.SORefNbr, fsAppointmentRow.RefNbr);

            foreach (FSAdjust fsAdjustRow in adjustments)
            {
                graphARPaymentEntry.Document.Current = graphARPaymentEntry.Document.Search<ARPayment.refNbr>(fsAdjustRow.AdjgRefNbr, fsAdjustRow.AdjgDocType);

                if (graphARPaymentEntry.Document.Current != null)
                {
                    fsAdjustRow.AdjdAppRefNbr = String.Empty;

                    graphSM_ARPaymentEntry.FSAdjustments.Update(fsAdjustRow);
                    graphARPaymentEntry.Save.Press();
                }
            }
        }

        public virtual void GetResourcesFromServiceOrder(FSAppointment fsAppointmentRow)
        {
            ClearResourceGrid();

            var fsSOResourceSet = PXSelectJoin<FSSOResource,
                                  InnerJoin<FSServiceOrder,
                                  On<
                                      FSServiceOrder.sOID, Equal<FSSOResource.sOID>>>,
                                  Where<
                                      FSServiceOrder.sOID, Equal<Required<FSServiceOrder.sOID>>>>
                                  .Select(this, fsAppointmentRow.SOID);

            foreach (FSSOResource fsSOResourceRow in fsSOResourceSet)
            {
                FSAppointmentResource fsAppointmentResourceRow = new FSAppointmentResource();

                fsAppointmentResourceRow.SMEquipmentID = fsSOResourceRow.SMEquipmentID;
                fsAppointmentResourceRow.Comment = fsSOResourceRow.Comment;

                AppointmentResources.Insert(fsAppointmentResourceRow);
            }
        }

        public virtual void UncheckUnreachedCustomerByScheduledDate(DateTime? oldValue, DateTime? currentValue, FSAppointment fsAppointmentRow)
        {
            if (currentValue != oldValue)
            {
                fsAppointmentRow.UnreachedCustomer = false;
            }
        }

        public virtual void ValidateEmployeeAvailability<fieldType>(FSAppointment fsAppointmentRow, PXCache currentCache, object currentRow)
            where fieldType : IBqlField
        {
            if (SetupRecord.Current.DenyWarnByAppOverlap == ID.ValidationType.NOT_VALIDATE)
            {
                return;
            }

            var fsAppointmentEmployeeSet = AppointmentServiceEmployees.Select();

            if (fsAppointmentEmployeeSet.Count == 0)
            {
                return;
            }

            List<object> args = new List<object>();
            List<int?> employeeIDList = new List<int?>();
            List<int?> notAvailableEmployees = new List<int?>();

            BqlCommand fsAppointmentBql = new Select2<FSAppointment,
                                              InnerJoin<FSAppointmentEmployee,
                                              On<
                                                  FSAppointmentEmployee.appointmentID, Equal<FSAppointment.appointmentID>>>,
                                              Where2<
                                                  Where<
                                                      FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>,
                                                      And<FSAppointment.canceled, Equal<False>>>,
                                                  And<FSAppointment.completed, Equal<False>,
                                                  And<FSAppointment.closed, Equal<False>,
                                                  And<
                                                      Where<
                                                          FSAppointment.scheduledDateTimeEnd, Greater<Required<FSAppointment.scheduledDateTimeBegin>>,
                                                          And<FSAppointment.scheduledDateTimeBegin, Less<Required<FSAppointment.scheduledDateTimeEnd>>>>>>>>>();

            args.Add(fsAppointmentRow.AppointmentID);
            args.Add(fsAppointmentRow.ScheduledDateTimeBegin);
            args.Add(fsAppointmentRow.ScheduledDateTimeEnd);

            fsAppointmentBql = fsAppointmentBql.WhereAnd(InHelper<FSAppointmentEmployee.employeeID>.Create(fsAppointmentEmployeeSet.Count));

            foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in fsAppointmentEmployeeSet)
            {
                args.Add(fsAppointmentEmployeeRow.EmployeeID);
                employeeIDList.Add(fsAppointmentEmployeeRow.EmployeeID);
            }

            PXView fsAppointmentView = new PXView(this, true, fsAppointmentBql);
            var fsAppointmentSet = fsAppointmentView.SelectMulti(args.ToArray());

            foreach (PXResult<FSAppointment, FSAppointmentEmployee> bqlResult in fsAppointmentSet)
            {
                FSAppointmentEmployee fsAppointmentEmployeeRow = (FSAppointmentEmployee)bqlResult;
                notAvailableEmployees.Add((int?)fsAppointmentEmployeeRow.EmployeeID);
            }

            if (fsAppointmentSet.Count > 0)
            {
                bool throwException = false;
                PXErrorLevel errorLevel;

                foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in fsAppointmentEmployeeSet)
                {
                    if (notAvailableEmployees.IndexOf(fsAppointmentEmployeeRow.EmployeeID) != -1)
                    {
                        errorLevel = PXErrorLevel.Warning;

                        if (SetupRecord.Current.DenyWarnByAppOverlap == ID.ValidationType.PREVENT)
                        {
                            throwException = true;
                            errorLevel = PXErrorLevel.RowError;
                        }

                        currentCache.RaiseExceptionHandling<fieldType>(currentRow,
                                                                       null,
                                                                       new PXSetPropertyException(TX.Error.EMPLOYEE_NOT_AVAILABLE_WITH_APPOINTMENTS, errorLevel));
                    }
                }

                if (throwException)
                {
                    throw new PXException(TX.Error.EMPLOYEE_NOT_AVAILABLE_WITH_APPOINTMENTS, PXErrorLevel.Error);
                }
            }
        }

        public virtual void ValidateRoomAvailability(PXCache cache, FSAppointment fsAppointmentRow)
        {
            //TODO AC-142850 SD-6208 need to be reimplemented taking now in consideration the roomID in FSServiceOrder Table
            return;
            /*
            if (SetupRecord.Current.DenyWarnEmpAvailability == ID.ValidationType.NONE)
            {
                return;
            }
            
            // IF there are no rooms assigned to the appointment 
            // THEN return
            if (!String.IsNullOrEmpty(fsAppointmentRow.RoomID))
            {
                var fsAppointmentRows = PXSelectJoin<FSAppointment,
                                                    InnerJoin<
                                                        FSServiceOrder,
                                                            On<FSServiceOrder.sOID, Equal<FSAppointment.sOID>>
                                                    >,
                                                    Where2<
                                                         Where<
                                                             FSServiceOrder.cpnyLocationID, Equal<Required<FSServiceOrder.cpnyLocationID>>,
                                                                And<
                                                                    FSAppointment.roomID, Equal<Required<FSAppointment.roomID>>,
                                                                And<
                                                                    FSAppointment.appointmentID,NotEqual<Required<FSAppointment.appointmentID>>>>>,
                                                         And<
                                                            Where<
                                                                    FSAppointment.scheduledDateTimeBegin, Less<Required<FSAppointment.scheduledDateTimeBegin>>,
                                                                And<
                                                                    FSAppointment.scheduledDateTimeEnd, Greater<Required<FSAppointment.scheduledDateTimeEnd>>>>>>>
                                                    .Select(this,
                                                            ServiceOrderRelated.Current.CpnyLocationID,
                                                            fsAppointmentRow.RoomID,
                                                            fsAppointmentRow.AppointmentID,
                                                            fsAppointmentRow.ScheduledDateTimeEnd,
                                                            fsAppointmentRow.ScheduledDateTimeBegin);

                if (fsAppointmentRows.Count() > 0)
                {
                    if (SetupRecord.Current.DenyWarnEmpAvailability == ID.ValidationType.DENY)
                    {
                        messages.ErrorMessages.Add(TX.Error.ROOM_NOT_AVAILABLE_WITH_APPOINTMENTS);
                            
                        AppointmentRecords.Cache.RaiseExceptionHandling<FSAppointment.roomID>(fsAppointmentRow,
                                fsAppointmentRow.RoomID,
                                new PXSetPropertyException(TX.Error.ROOM_NOT_AVAILABLE_WITH_APPOINTMENTS, PXErrorLevel.RowError));

                        throw new PXException(TX.Error.ROOM_NOT_AVAILABLE_WITH_APPOINTMENTS, PXErrorLevel.Error);
                    }
                    else
                    {
                        messages.WarningMessages.Add(TX.Error.ROOM_NOT_AVAILABLE_WITH_APPOINTMENTS);

                        AppointmentRecords.Cache.RaiseExceptionHandling<FSAppointmentEmployee.employeeID>(fsAppointmentRow,
                                fsAppointmentRow.RoomID,
                                new PXSetPropertyException(TX.Error.ROOM_NOT_AVAILABLE_WITH_APPOINTMENTS, PXErrorLevel.Warning));
                    }
                }
            }   */
        }

        public virtual void ValidateRoom(FSAppointment fsAppointmentRow)
        {
            FSSrvOrdType fsSrvOrdTypeRow = this.ServiceOrderTypeSelected.SelectSingle(fsAppointmentRow.SrvOrdType);
            FSServiceOrder fsServiceOrder = this.ServiceOrderRelated.Current;

            if (fsSrvOrdTypeRow.RequireRoom == true && string.IsNullOrEmpty(fsServiceOrder.RoomID))
            {
                this.ServiceOrderRelated.Cache.RaiseExceptionHandling<FSServiceOrder.roomID>(fsServiceOrder,
                                                                                             fsServiceOrder.RoomID,
                                                                                             new PXSetPropertyException(TX.Error.ROOM_REQUIRED_FOR_THIS_SRVORDTYPE, PXErrorLevel.Error));
            }
        }

        /// <summary>
        /// Validates if the maximum amount of appointments it is exceed for a specific route.
        /// </summary>
        public virtual void ValidateMaxAppointmentQty(FSAppointment fsAppointmentRow)
        {
            if (fsAppointmentRow.RouteID == null)
            {
                return;
            }

            FSRoute fsRouteRow = FSRoute.PK.Find(this, fsAppointmentRow.RouteID);

            if (fsRouteRow != null
                    && fsRouteRow.NoAppointmentLimit == false)
            {
                DateTime? appointmentScheduledDate = fsAppointmentRow.ScheduledDateTimeBegin;
                DateTime? begin = new DateTime(appointmentScheduledDate.Value.Year, appointmentScheduledDate.Value.Month, appointmentScheduledDate.Value.Day, 0, 0, 0); //12:00 AM
                DateTime? end = new DateTime(appointmentScheduledDate.Value.Year, appointmentScheduledDate.Value.Month, appointmentScheduledDate.Value.Day, 23, 59, 59); //23:59 AM

                PXResultset<FSAppointment> bqlResultSet = PXSelectReadonly<FSAppointment,
                                                          Where<
                                                              FSAppointment.routeID, Equal<Required<FSAppointment.routeID>>,
                                                              And<FSAppointment.scheduledDateTimeBegin, Between<Required<FSAppointment.scheduledDateTimeBegin>, Required<FSAppointment.scheduledDateTimeBegin>>>>>
                                                          .Select(this, fsAppointmentRow.RouteID, begin, end);

                if (bqlResultSet.Count >= fsRouteRow.MaxAppointmentQty)
                {
                    throw new PXException(TX.Error.ROUTE_MAX_APPOINTMENT_QTY_EXCEEDED, PXErrorLevel.Error);
                }
            }
        }

        /// <summary>
        /// Validates if the appointment Week Code is valid with the <c>datetime</c> of the appointment.
        /// </summary>
        public virtual void ValidateWeekCode(FSAppointment fsAppointmentRow)
        {
            if (fsAppointmentRow.ScheduleID == null)
            {
                return;
            }

            FSSchedule fsScheduleRow = PXSelect<FSSchedule,
                                       Where<
                                           FSSchedule.scheduleID, Equal<Required<FSSchedule.scheduleID>>>>
                                       .Select(this, fsAppointmentRow.ScheduleID);

            DateTime? scheduleTimeModified = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                                          fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                                          fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                                          0,
                                                          0,
                                                          0); //12:00 AM

            if (fsScheduleRow != null && fsScheduleRow.WeekCode != null)
            {
                if (!SharedFunctions.WeekCodeIsValid(fsScheduleRow.WeekCode, scheduleTimeModified, this))
                {
                    throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.WEEKCODE_NOT_MATCH_WITH_SCHEDULE, fsScheduleRow.RefNbr, scheduleTimeModified.Value.ToShortDateString()), PXErrorLevel.Error);
                }
            }

            PXResult<FSScheduleRoute, FSRoute> bqlResult = (PXResult<FSScheduleRoute, FSRoute>)
                                                            PXSelectJoin<FSScheduleRoute,
                                                            InnerJoin<FSRoute,
                                                            On<
                                                                FSRoute.routeID, Equal<FSScheduleRoute.dfltRouteID>>>,
                                                            Where<
                                                                FSScheduleRoute.scheduleID, Equal<Required<FSScheduleRoute.scheduleID>>>>
                                                            .Select(this, fsScheduleRow.ScheduleID);

            if (bqlResult != null)
            {
                FSRoute fsRouteRow = (FSRoute)bqlResult;

                if (fsRouteRow != null && fsRouteRow.WeekCode != null)
                {
                    if (!SharedFunctions.WeekCodeIsValid(fsRouteRow.WeekCode, scheduleTimeModified, this))
                    {
                        throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.WEEKCODE_NOT_MATCH_WITH_ROUTE_SCHEDULE, fsScheduleRow.RefNbr, scheduleTimeModified.Value.ToShortDateString()), PXErrorLevel.Error);
                    }
                }
            }

            if (fsAppointmentRow.RouteID != null)
            {
                FSRoute fsRouteRow = FSRoute.PK.Find(this, fsAppointmentRow.RouteID);

                if (fsRouteRow != null && fsRouteRow.WeekCode != null)
                {
                    if (!SharedFunctions.WeekCodeIsValid(fsRouteRow.WeekCode, scheduleTimeModified, this))
                    {
                        throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.WEEKCODE_NOT_MATCH_WITH_ROUTE, fsScheduleRow.RefNbr, scheduleTimeModified.Value.ToShortDateString()), PXErrorLevel.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Assign the [fsAppointmentRow] position on the current [fsRouteDocumentRow].
        /// </summary>
        public virtual void SetRoutePosition(FSRouteDocument fsRouteDocumentRow, FSAppointment fsAppointmentRow)
        {
            if (RouteSetupRecord.Current == null)
            {
                return;
            }

            if (fsAppointmentRow.RoutePosition == null || fsAppointmentRow.RoutePosition < 0)
            {
                int? newRoutePosition;
                FSAppointment fsAppointmentRow_local = PXSelectReadonly<FSAppointment,
                                                       Where<
                                                           FSAppointment.routeDocumentID, Equal<Required<FSAppointment.routeDocumentID>>>,
                                                       OrderBy<Desc<FSAppointment.routePosition>>>
                                                       .Select(this, fsRouteDocumentRow.RouteDocumentID);

                if (fsAppointmentRow_local == null || fsAppointmentRow_local.RoutePosition == null)
                {
                    newRoutePosition = 1;
                }
                else
                {
                    if ((fsAppointmentRow.IsReassigned == true
                                || fsAppointmentRow.NotStarted == true)
                                            && RouteSetupRecord.Current.SetFirstManualAppointment == true)
                    {
                        newRoutePosition = 1;
                        UpdateRouteAppointmentsOrder(this, fsRouteDocumentRow.RouteDocumentID, fsAppointmentRow.AppointmentID, newRoutePosition + 1);
                    }
                    else
                    {
                        newRoutePosition = fsAppointmentRow_local.RoutePosition + 1;
                    }
                }

                fsAppointmentRow.RoutePosition = newRoutePosition;
            }
        }

        /// <summary>
        /// Updates the appointments' order in a route in ascending order setting the initial order.
        /// </summary>
        public virtual void UpdateRouteAppointmentsOrder(PXGraph graph, int? routeDocumentID, int? appointmentID, int? firstPositionToSet)
        {
            PXResultset<FSAppointment> fsAppointmentsInRoute = PXSelectReadonly<FSAppointment,
                                                               Where<
                                                                   FSAppointment.routeDocumentID, Equal<Required<FSAppointment.routeDocumentID>>,
                                                                   And<FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>>>,
                                                               OrderBy<Asc<FSAppointment.routePosition>>>
                                                               .Select(graph, routeDocumentID, appointmentID);

            foreach (FSAppointment fsAppointmentInRoute in fsAppointmentsInRoute)
            {
                PXUpdate<
                    Set<FSAppointment.routePosition, Required<FSAppointment.routePosition>>,
                FSAppointment,
                Where<
                    FSAppointment.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>
                .Update(this,
                        firstPositionToSet++,
                        fsAppointmentInRoute.AppointmentID);
            }
        }

        /// <summary>
        /// Set the route info necessary to the [fsAppointmentRow] using the [fsAppointmentRow].RouteID, [fsAppointmentRow].RouteDocumentID and [fsServiceOrderRow].BranchID.
        /// </summary>
        public virtual void SetAppointmentRouteInfo(PXCache cache, FSAppointment fsAppointmentRow, FSServiceOrder fsServiceOrderRow)
        {
            FSRouteDocument fsRouteDocumentRow = GetOrGenerateRoute(fsAppointmentRow.RouteID, fsAppointmentRow.RouteDocumentID, fsAppointmentRow.ScheduledDateTimeBegin, fsServiceOrderRow.BranchID);
            SetRoutePosition(fsRouteDocumentRow, fsAppointmentRow);
            fsAppointmentRow.RouteDocumentID = fsRouteDocumentRow.RouteDocumentID;

            PXUpdate<
                Set<FSAppointment.routeDocumentID, Required<FSAppointment.routeDocumentID>,
                Set<FSAppointment.routeID, Required<FSAppointment.routeID>,
                Set<FSAppointment.routePosition, Required<FSAppointment.routePosition>>>>,
            FSAppointment,
            Where<
                FSAppointment.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>
            .Update(this,
                    fsRouteDocumentRow.RouteDocumentID,
                    fsRouteDocumentRow.RouteID,
                    fsAppointmentRow.RoutePosition,
                    fsAppointmentRow.AppointmentID);

            cache.Graph.SelectTimeStamp();
        }

        /// <summary>
        /// Set schedule times to the [fsAppointmentRow] using Route and Schedule.
        /// </summary>
        public virtual void SetScheduleTimesByRouteAndContract(FSRouteDocument fsRouteDocumentRow, FSAppointment fsAppointmentRow)
        {
            DateTime? routeBegin, routeEnd, slotBegin, slotEnd;

            bool timeBeginHasValue = fsRouteDocumentRow.TimeBegin.HasValue;

            routeBegin = slotBegin = new DateTime(fsRouteDocumentRow.Date.Value.Year,
                                                  fsRouteDocumentRow.Date.Value.Month,
                                                  fsRouteDocumentRow.Date.Value.Day,
                                                  timeBeginHasValue ? fsRouteDocumentRow.TimeBegin.Value.Hour : 0,
                                                  timeBeginHasValue ? fsRouteDocumentRow.TimeBegin.Value.Minute : 0,
                                                  0);

            routeEnd = fsRouteDocumentRow.TimeEnd;

            List<object> appointmentsArgs = new List<object>();
            PXResultset<FSAppointment> bqlResultSet;
            SlotIsContained isContained = SlotIsContained.NotContained;

            DateTime dayBegin;
            DateTime dayEnd;

            dayBegin = routeBegin.Value.Date;

            if (routeEnd != null)
            {
                dayEnd = routeEnd.Value.Date.AddDays(1);
            }
            else
            {
                dayEnd = dayBegin.Date.AddDays(1);
            }

            double slotDuration = (double)fsAppointmentRow.EstimatedDurationTotal;
            slotEnd = slotBegin.Value.AddMinutes(slotDuration);

            PXSelectBase<FSAppointment> appointmentsBase =
                                    new PXSelectReadonly<FSAppointment,
                                        Where<
                                            FSAppointment.routeDocumentID, Equal<Required<FSAppointment.routeDocumentID>>,
                                            And<FSAppointment.scheduledDateTimeBegin, GreaterEqual<Required<FSAppointment.scheduledDateTimeBegin>>,
                                            And<FSAppointment.scheduledDateTimeEnd, LessEqual<Required<FSAppointment.scheduledDateTimeEnd>>,
                                            And<FSAppointment.scheduledDateTimeEnd, Greater<Required<FSAppointment.scheduledDateTimeBegin>>,
                                            And<FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>>>>>>,
                                        OrderBy<Asc<FSAppointment.routePosition>>>(this);

            appointmentsArgs.Add(fsRouteDocumentRow.RouteDocumentID);
            appointmentsArgs.Add(dayBegin);
            appointmentsArgs.Add(dayEnd);
            appointmentsArgs.Add(routeBegin);
            appointmentsArgs.Add(fsAppointmentRow.AppointmentID);

            bqlResultSet = appointmentsBase.Select(appointmentsArgs.ToArray());

            //Resolve collision with other appointments
            foreach (FSAppointment fsAppointmentRow_local in bqlResultSet)
            {
                if (fsAppointmentRow_local != null && fsAppointmentRow_local.AppointmentID != null)
                {
                    if (fsAppointmentRow_local.ScheduledDateTimeEnd > slotBegin)
                    {
                        isContained = SlotIsContainedInSlot(slotBegin,
                                                                            slotEnd,
                                                                            fsAppointmentRow_local.ScheduledDateTimeBegin,
                                                                            fsAppointmentRow_local.ScheduledDateTimeEnd);

                        if (isContained == SlotIsContained.Contained
                                || isContained == SlotIsContained.PartiallyContained
                                    || isContained == SlotIsContained.ExceedsContainment)
                        {
                            slotBegin = fsAppointmentRow_local.ScheduledDateTimeEnd;
                            slotEnd = slotBegin.Value.AddMinutes(slotDuration);
                        }

                        if (isContained == SlotIsContained.NotContained)
                        {
                            break;
                        }
                    }
                }
            }

            //Set Times
            fsAppointmentRow.ScheduledDateTimeBegin = slotBegin;
            fsAppointmentRow.ScheduledDateTimeEnd = slotEnd;

            //Time Restriction Verification
            if (fsAppointmentRow.ScheduleID != null)
            {
                FSContractSchedule fsContractScheduleRow = PXSelect<FSContractSchedule, Where<FSContractSchedule.scheduleID, Equal<Required<FSContractSchedule.scheduleID>>>>.Select(this, fsAppointmentRow.ScheduleID);
                if (fsContractScheduleRow != null && (fsContractScheduleRow.RestrictionMax == true || fsContractScheduleRow.RestrictionMin == true) && IsAppointmentInValidRestriction(fsAppointmentRow, fsContractScheduleRow) == false)
                {
                    if (fsContractScheduleRow.RestrictionMin == true)
                    {
                        fsAppointmentRow.ScheduledDateTimeBegin = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                                                               fsContractScheduleRow.RestrictionMinTime.Value.Hour,
                                                                               fsContractScheduleRow.RestrictionMinTime.Value.Minute,
                                                                               fsContractScheduleRow.RestrictionMinTime.Value.Second);
                    }
                    else
                    {
                        fsAppointmentRow.ScheduledDateTimeBegin = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                                                               fsContractScheduleRow.RestrictionMaxTime.Value.Hour,
                                                                               fsContractScheduleRow.RestrictionMaxTime.Value.Minute,
                                                                               fsContractScheduleRow.RestrictionMaxTime.Value.Second);
                    }

                    fsAppointmentRow.ScheduledDateTimeEnd = fsAppointmentRow.ScheduledDateTimeBegin.Value.AddMinutes(slotDuration);
                }
            }
        }

        /// <summary>
        /// Set schedule times to the [fsAppointmentRow] using Contract and Schedule.
        /// </summary>
        public virtual void SetScheduleTimesByContract(FSAppointment fsAppointmentRow)
        {
            DateTime? slotBegin, slotEnd;
            List<object> appointmentsArgs = new List<object>();
            PXResultset<FSAppointment> bqlResultSet;
            SlotIsContained isContained = SlotIsContained.NotContained;
            DateTime dayBegin = fsAppointmentRow.ScheduledDateTimeBegin.Value.Date;
            DateTime dayEnd = fsAppointmentRow.ScheduledDateTimeEnd.Value.Date.AddDays(1);

            FSContractSchedule fsContractScheduleRow = PXSelect<FSContractSchedule, Where<FSContractSchedule.scheduleID, Equal<Required<FSContractSchedule.scheduleID>>>>.Select(this, fsAppointmentRow.ScheduleID);

            //The appointment should be created in the time restriction
            if (fsContractScheduleRow != null && (fsContractScheduleRow.RestrictionMax == true || fsContractScheduleRow.RestrictionMin == true))
            {
                if (fsContractScheduleRow.RestrictionMin == true)
                {
                    slotBegin = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                             fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                             fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                             fsContractScheduleRow.RestrictionMinTime.Value.Hour,
                                             fsContractScheduleRow.RestrictionMinTime.Value.Minute,
                                             fsContractScheduleRow.RestrictionMinTime.Value.Second);
                }
                else
                {
                    slotBegin = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                             fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                             fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                             fsContractScheduleRow.RestrictionMaxTime.Value.Hour,
                                             fsContractScheduleRow.RestrictionMaxTime.Value.Minute,
                                             fsContractScheduleRow.RestrictionMaxTime.Value.Second);
                }
            }
            else
            {
                slotBegin = dayBegin;
            }

            double slotDuration = (double)fsAppointmentRow.EstimatedDurationTotal;

            slotEnd = slotBegin.Value.AddMinutes(slotDuration);

            PXSelectBase<FSAppointment> appointmentsBase = new PXSelectReadonly<FSAppointment,
                                                               Where<
                                                                   FSAppointment.scheduledDateTimeBegin, Less<Required<FSAppointment.scheduledDateTimeBegin>>,
                                                                   And<FSAppointment.scheduledDateTimeEnd, Greater<Required<FSAppointment.scheduledDateTimeEnd>>>>,
                                                               OrderBy<Asc<FSAppointment.scheduledDateTimeBegin>>>(this);
            appointmentsArgs.Add(dayEnd);
            appointmentsArgs.Add(dayBegin);

            bqlResultSet = appointmentsBase.Select(appointmentsArgs.ToArray());

            //Resolve collision with other appointments
            foreach (FSAppointment fsAppointmentRow_local in bqlResultSet)
            {
                if (fsAppointmentRow_local != null && fsAppointmentRow_local.AppointmentID != null)
                {
                    if (fsAppointmentRow_local.ScheduledDateTimeEnd > slotBegin)
                    {
                        isContained = SlotIsContainedInSlot(slotBegin,
                                                                            slotEnd,
                                                                            fsAppointmentRow_local.ScheduledDateTimeBegin,
                                                                            fsAppointmentRow_local.ScheduledDateTimeEnd);

                        if (isContained == SlotIsContained.Contained
                                || isContained == SlotIsContained.PartiallyContained
                                    || isContained == SlotIsContained.ExceedsContainment)
                        {
                            slotBegin = fsAppointmentRow_local.ScheduledDateTimeEnd;
                            slotEnd = slotBegin.Value.AddMinutes(slotDuration);
                        }

                        if (isContained == SlotIsContained.NotContained)
                        {
                            break;
                        }
                    }
                }
            }

            //Set Times
            fsAppointmentRow.ScheduledDateTimeBegin = slotBegin;
            fsAppointmentRow.ScheduledDateTimeEnd = slotEnd;

            //Time Restriction verifcation
            if (fsAppointmentRow.ScheduleID != null)
            {
                if (fsContractScheduleRow != null && (fsContractScheduleRow.RestrictionMax == true || fsContractScheduleRow.RestrictionMin == true) && IsAppointmentInValidRestriction(fsAppointmentRow, fsContractScheduleRow) == false)
                {
                    if (fsContractScheduleRow.RestrictionMin == true)
                    {
                        fsAppointmentRow.ScheduledDateTimeBegin = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                                                               fsContractScheduleRow.RestrictionMinTime.Value.Hour,
                                                                               fsContractScheduleRow.RestrictionMinTime.Value.Minute,
                                                                               fsContractScheduleRow.RestrictionMinTime.Value.Second);
                    }
                    else
                    {
                        fsAppointmentRow.ScheduledDateTimeBegin = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                                                               fsContractScheduleRow.RestrictionMaxTime.Value.Hour,
                                                                               fsContractScheduleRow.RestrictionMaxTime.Value.Minute,
                                                                               fsContractScheduleRow.RestrictionMaxTime.Value.Second);
                    }

                    fsAppointmentRow.ScheduledDateTimeEnd = fsAppointmentRow.ScheduledDateTimeBegin.Value.AddMinutes(slotDuration);
                }
            }
        }

        /// <summary>
        /// Verifies if the [fsAppointmentRow].ScheduleTimeBegin and [fsAppointmentRow].ScheduleTimeEnd are valid in the fsContractScheduleRow restrictions.
        /// </summary>
        public virtual bool IsAppointmentInValidRestriction(FSAppointment fsAppointmentRow, FSContractSchedule fsContractScheduleRow)
        {
            DateTime compareDateTime;

            if (fsContractScheduleRow.RestrictionMax == true)
            {
                compareDateTime = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                               fsContractScheduleRow.RestrictionMaxTime.Value.Hour,
                                               fsContractScheduleRow.RestrictionMaxTime.Value.Minute,
                                               fsContractScheduleRow.RestrictionMaxTime.Value.Second);

                if (fsAppointmentRow.ScheduledDateTimeBegin > compareDateTime)
                {
                    return false;
                }
            }

            if (fsContractScheduleRow.RestrictionMin == true)
            {
                compareDateTime = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                               fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                               fsContractScheduleRow.RestrictionMinTime.Value.Hour,
                                               fsContractScheduleRow.RestrictionMinTime.Value.Minute,
                                               fsContractScheduleRow.RestrictionMinTime.Value.Second);

                if (fsAppointmentRow.ScheduledDateTimeBegin < compareDateTime)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get the specific route in the Routes Module using the [routeID], [routeDocumentID] and [appointmentScheduledDate].
        /// </summary>
        public virtual FSRouteDocument GetOrGenerateRoute(int? routeID, int? routeDocumentID, DateTime? appointmentScheduledDate, int? branchID)
        {
            FSRouteDocument fsRouteDocumentRow;

            DateTime? begin = new DateTime(appointmentScheduledDate.Value.Year, appointmentScheduledDate.Value.Month, appointmentScheduledDate.Value.Day, 0, 0, 0); //12:00 AM
            DateTime? end = new DateTime(appointmentScheduledDate.Value.Year, appointmentScheduledDate.Value.Month, appointmentScheduledDate.Value.Day, 23, 59, 59); //23:59 AM

            if (routeDocumentID == null)
            {
                fsRouteDocumentRow = PXSelect<FSRouteDocument,
                                     Where<
                                        FSRouteDocument.routeID, Equal<Required<FSRouteDocument.routeID>>,
                                        And<FSRouteDocument.timeBegin, Between<Required<FSRouteDocument.timeBegin>, Required<FSRouteDocument.timeBegin>>>>>
                                     .Select(this, routeID, begin, end);
            }
            else
            {
                fsRouteDocumentRow = PXSelect<FSRouteDocument,
                                     Where<
                                        FSRouteDocument.routeID, Equal<Required<FSRouteDocument.routeID>>,
                                        And<FSRouteDocument.routeDocumentID, Equal<Required<FSRouteDocument.routeDocumentID>>>>,
                                     OrderBy<Desc<FSRouteDocument.routeDocumentID>>>
                                     .Select(this, routeID, routeDocumentID);
            }

            if (fsRouteDocumentRow == null)
            {
                FSRoute fsRouteRow = PXSelect<FSRoute, Where<FSRoute.routeID, Equal<Required<FSRoute.routeID>>>>.Select(this, routeID);

                DateTime? beginTimeOnWeekDay = new DateTime();

                SharedFunctions.ValidateExecutionDay(fsRouteRow, appointmentScheduledDate.Value.DayOfWeek, ref beginTimeOnWeekDay);

                if (!beginTimeOnWeekDay.HasValue)
                {
                    beginTimeOnWeekDay = new DateTime(appointmentScheduledDate.Value.Year, appointmentScheduledDate.Value.Month, appointmentScheduledDate.Value.Day, 0, 0, 0);
                }

                RouteDocumentMaint graphRouteMaint = PXGraph.CreateInstance<RouteDocumentMaint>();
                FSRouteDocument fsRouteDocumentRow_Local = new FSRouteDocument();

                fsRouteDocumentRow_Local.GeneratedBySystem = true;
                fsRouteDocumentRow_Local.RouteID = routeID;
                fsRouteDocumentRow_Local.BranchID = branchID;
                fsRouteDocumentRow_Local.Date = new DateTime(appointmentScheduledDate.Value.Year, appointmentScheduledDate.Value.Month, appointmentScheduledDate.Value.Day, 0, 0, 0);
                fsRouteDocumentRow_Local.TimeBegin = new DateTime(appointmentScheduledDate.Value.Year, appointmentScheduledDate.Value.Month, appointmentScheduledDate.Value.Day, beginTimeOnWeekDay.Value.Hour, beginTimeOnWeekDay.Value.Minute, 0);
                fsRouteDocumentRow_Local.TimeEnd = new DateTime(appointmentScheduledDate.Value.Year, appointmentScheduledDate.Value.Month, appointmentScheduledDate.Value.Day, 23, 59, 59);
                fsRouteDocumentRow_Local.Status = ID.Status_Route.OPEN;

                graphRouteMaint.RouteRecords.Insert(fsRouteDocumentRow_Local);
                graphRouteMaint.Save.Press();
                fsRouteDocumentRow = graphRouteMaint.RouteRecords.Current;
            }

            return fsRouteDocumentRow;
        }

        /// <summary>
        /// Calculate all the statistics for the routes involving the given appointment.
        /// </summary>
        /// <param name="graph">Context graph instance.</param>
        /// <param name="fsAppointmentRow">FSAppointment Row.</param>
        /// <param name="simpleStatsOnly">Boolean flag that controls whereas only single statistics need to be calculated or not.</param>
        public virtual void CalculateRouteStats(FSAppointment fsAppointmentRow, string apiKey, bool simpleStatsOnly = false)
        {
            PXGraph graph = this;
            RouteDocumentMaint graphRouteMaint = PXGraph.CreateInstance<RouteDocumentMaint>();

            FSRouteDocument fsRouteDocumentRow_Current = PXSelect<FSRouteDocument,
                                                         Where<
                                                             FSRouteDocument.routeDocumentID, Equal<Required<FSRouteDocument.routeDocumentID>>>>
                                                         .Select(graph, fsAppointmentRow.RouteDocumentID);

            FSRouteDocument fsRouteDocumentRow_Previous = PXSelect<FSRouteDocument,
                                                          Where<
                                                              FSRouteDocument.routeDocumentID, Equal<Required<FSRouteDocument.routeDocumentID>>>>
                                                          .Select(graph, fsAppointmentRow.Mem_LastRouteDocumentID);

            if (fsRouteDocumentRow_Current != null)
            {
                fsRouteDocumentRow_Current.RouteStatsUpdated = simpleStatsOnly == false;
                SetRouteSimpleStats(graph, fsAppointmentRow.RouteDocumentID, ref fsRouteDocumentRow_Current);

                if (simpleStatsOnly == false)
                {
                    SetRouteMapStats(graph, fsAppointmentRow, fsAppointmentRow.RouteDocumentID, ref fsRouteDocumentRow_Current, apiKey);
                }

                graphRouteMaint.RouteRecords.Update(fsRouteDocumentRow_Current);
                graphRouteMaint.Save.Press();
            }

            if (fsRouteDocumentRow_Previous != null)
            {
                SetRouteSimpleStats(graph, fsRouteDocumentRow_Previous.RouteDocumentID, ref fsRouteDocumentRow_Previous);

                if (simpleStatsOnly != true)
                {
                    SetRouteMapStats(graph, fsAppointmentRow, fsRouteDocumentRow_Previous.RouteDocumentID, ref fsRouteDocumentRow_Previous, apiKey);
                }

                graphRouteMaint.RouteRecords.Update(fsRouteDocumentRow_Previous);
                graphRouteMaint.Save.Press();
            }
        }

        /// <summary>
        /// Return the total duration of the services within a given route.
        /// </summary>
        public virtual int? CalculateRouteTotalServicesDuration(PXResultset<FSAppointmentDet> bqlResultSet)
        {
            int? servicesDurationTotal = 0;

            foreach (FSAppointmentDet fsAppointmentDetRow in bqlResultSet)
            {
                servicesDurationTotal += fsAppointmentDetRow.EstimatedDuration;
            }

            return servicesDurationTotal;
        }

        /// <summary>
        /// Return the total duration of the appointments within a given route.
        /// </summary>
        /// <param name="graph">Context graph instance.</param>
        /// <param name="routeDocumentID">Id for Route Document.</param>
        /// <param name="fsAppointmentRow">FSAppointment object.</param>
        /// <returns>RowCount of appointments.</returns>
        public virtual int? CalculateRouteTotalAppointmentsDuration(PXGraph graph, int? routeDocumentID, FSAppointment fsAppointmentRow)
        {
            int? appointmentsDurationTotal = 0;
            appointmentsDurationTotal += fsAppointmentRow.EstimatedDurationTotal;

            var appointmentsInRoute = PXSelectReadonly<FSAppointment,
                                      Where<
                                          FSAppointment.routeDocumentID, Equal<Required<FSAppointment.routeDocumentID>>,
                                          And<FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>>>>
                                      .Select(graph, routeDocumentID, fsAppointmentRow.AppointmentID);

            foreach (FSAppointment fsAppointmentRowInRoute in appointmentsInRoute)
            {
                appointmentsDurationTotal += fsAppointmentRowInRoute.ScheduledDuration;
            }

            return appointmentsDurationTotal;
        }

        /// <summary>
        /// Return the total number of appointments for a given route.
        /// </summary>
        public virtual int? GetRouteTotalAppointments(PXGraph graph, int? routeDocumentID)
        {
            var fsAppointmentSet = PXSelectGroupBy<FSAppointment,
                                   Where<
                                       FSAppointment.routeDocumentID, Equal<Required<FSAppointment.routeDocumentID>>>,
                                   Aggregate<Count<FSAppointment.appointmentID>>>
                                   .Select(graph, routeDocumentID);

            return fsAppointmentSet.RowCount;
        }

        /// <summary>
        /// Return the services for a given route.
        /// </summary>
        public virtual PXResultset<FSAppointmentDet> GetRouteServices(PXGraph graph, int? routeDocumentID)
        {
            PXResultset<FSAppointmentDet> bqlResultSet = PXSelectJoin<FSAppointmentDet,
                                                         InnerJoin<FSAppointment,
                                                         On<
                                                             FSAppointmentDet.appointmentID, Equal<FSAppointment.appointmentID>>,
                                                         InnerJoin<FSSODet,
                                                         On<
                                                             FSSODet.sODetID, Equal<FSAppointmentDet.sODetID>>>>,
                                                         Where<
                                                             FSSODet.lineType, Equal<FSLineType.Service>,
                                                             And<FSAppointment.routeDocumentID, Equal<Required<FSAppointment.routeDocumentID>>>>>
                                                         .Select(graph, routeDocumentID);

            return bqlResultSet;
        }

        /// <summary>
        /// Split an array [geoLocationArray] in a list of array of [length] element.
        /// </summary>
        public virtual List<GLocation[]> SplitArrayInList(GLocation[] geoLocationArray, int length)
        {
            List<GLocation[]> listGeoLocationArray = new List<GLocation[]>();
            int totalSplit = 1 + (int)Math.Ceiling(((float)geoLocationArray.Length - (float)length) / ((float)length - 1));
            int totalLength = 0;
            int totalElement = 0;

            if (totalSplit == 1)
            {
                listGeoLocationArray.Add(geoLocationArray);

                return listGeoLocationArray;
            }

            for (int i = 0; i < totalSplit; i++)
            {
                totalLength = length;
                GLocation[] locationAuxiliar;

                if (i == totalSplit - 1 && i != 0)
                {
                    totalLength = geoLocationArray.Length - totalElement;
                    locationAuxiliar = new GLocation[totalLength + 1];
                }
                else
                {
                    locationAuxiliar = new GLocation[totalLength];
                }

                for (int j = 0; j < totalLength; j++)
                {
                    if (i != 0)
                    {
                        locationAuxiliar[j] = geoLocationArray[totalElement - 1];
                        if (j != totalLength - 1)
                        {
                            totalElement++;
                        }
                    }
                    else
                    {
                        locationAuxiliar[j] = geoLocationArray[totalElement];
                        totalElement++;
                    }
                }

                if (i == totalSplit - 1 && i != 0)
                {
                    locationAuxiliar[totalLength] = geoLocationArray[geoLocationArray.Length - 1];
                }

                listGeoLocationArray.Add(locationAuxiliar);
            }

            return listGeoLocationArray;
        }

        /// <summary>
        /// Calculate the google map statistics for a given route.
        /// </summary>
        /// <param name="graph">Context graph instance.</param>
        /// <param name="routeDocumentID">ID for the route.</param>
        /// <param name="totalDistance">Total driving distance in meters.</param>
        /// <param name="totalDistanceFriendly">Total driving distance user friendly.</param>
        /// <param name="totalDuration">Total driving duration in seconds.</param>
        public virtual void CalculateRouteMapStats(PXGraph graph,
                                                   int? routeDocumentID,
                                                   ref decimal? totalDistance,
                                                   ref string totalDistanceFriendly,
                                                   ref int? totalDuration,
                                                   string apiKey)
        {
            FSRouteDocument fsRouteDocumentRow = PXSelect<FSRouteDocument,
                                                 Where<
                                                     FSRouteDocument.routeDocumentID, Equal<Required<FSRouteDocument.routeDocumentID>>>>
                                                 .Select(graph, routeDocumentID);

            var fsAppointmentSet = PXSelectReadonly2<FSAppointment,
                                   InnerJoin<FSServiceOrder,
                                        On<FSAppointment.sOID, Equal<FSServiceOrder.sOID>>,
                                   InnerJoin<FSContact,
                                        On<FSContact.contactID, Equal<FSServiceOrder.serviceOrderContactID>>,
                                   InnerJoin<FSAddress,
                                        On<FSAddress.addressID, Equal<FSServiceOrder.serviceOrderAddressID>>>>>,
                                   Where<
                                        FSAppointment.routeDocumentID, Equal<Required<FSAppointment.routeDocumentID>>>,
                                   OrderBy<
                                        Asc<FSAppointment.routePosition>>>
                                   .Select(graph, routeDocumentID);

            List<GLocation> geoLocationList = new List<GLocation>();
            string nodeAddress;

            //Gets the Begin Branch Location Row
            FSBranchLocation fsBranchLocationRow_Begin = PXSelectJoin<FSBranchLocation,
                                                         InnerJoin<FSRoute,
                                                         On<
                                                             FSRoute.beginBranchLocationID, Equal<FSBranchLocation.branchLocationID>>>,
                                                         Where<
                                                             FSRoute.routeID, Equal<Required<FSRoute.routeID>>>>
                                                         .Select(graph, fsRouteDocumentRow.RouteID);

            //Gets the End Branch Location Row
            FSBranchLocation fsBranchLocationRow_End = PXSelectJoin<FSBranchLocation,
                                                       InnerJoin<FSRoute,
                                                       On<
                                                           FSRoute.endBranchLocationID, Equal<FSBranchLocation.branchLocationID>>>,
                                                       Where<
                                                           FSRoute.routeID, Equal<Required<FSRoute.routeID>>>>
                                                       .Select(graph, fsRouteDocumentRow.RouteID);

            //setting fsBeginBranchLocationRow as first address
            nodeAddress = SharedFunctions.GetBranchLocationAddress(graph, fsBranchLocationRow_Begin);

            if (!string.IsNullOrEmpty(nodeAddress))
            {
                geoLocationList.Add(new GLocation(nodeAddress));
            }

            var graphAppointmentMaint = PXGraph.CreateInstance<AppointmentEntry>();
            graphAppointmentMaint.CalculateGoogleStats = false;

            List<FSAppointment> routeAppointmentList = new List<FSAppointment>();

            foreach (PXResult<FSAppointment, FSServiceOrder, FSContact, FSAddress> bqlResult in fsAppointmentSet)
            {
                routeAppointmentList.Add((FSAppointment)bqlResult);
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)bqlResult;
                FSAddress fsAddressRow = (FSAddress)bqlResult;

                nodeAddress = SharedFunctions.GetAppointmentAddress(fsAddressRow);

                if (!string.IsNullOrEmpty(nodeAddress))
                {
                    geoLocationList.Add(new GLocation(nodeAddress));
                }
            }

            nodeAddress = SharedFunctions.GetBranchLocationAddress(graph, fsBranchLocationRow_End);

            //setting fsBeginBranchLocationRow as first address
            if (!string.IsNullOrEmpty(nodeAddress))
            {
                geoLocationList.Add(new GLocation(nodeAddress));
            }

            GLocation[] geoLocationArray = geoLocationList.ToArray();

            List<GLocation[]> geoLocationArrayList = SplitArrayInList(geoLocationArray, 10);

            // Ignoring appointments without address
            try
            {
                int i = 0;
                double distanceTotal = 0;
                totalDuration = 0;
                totalDistance = 0;
                FSAppointment fsAppointmentRow;
                double servicesDuration;
                DateTime? lastAppoinmentEndTime = DateTime.Now;

                foreach (GLocation[] glocationArray in geoLocationArrayList)
                {
                    Route route = RouteDirections.GetRoute("distance", apiKey, glocationArray);

                    if (route != null)
                    {
                        string firstLegDistanceDescr = route.Legs[0].DistanceDescr;
                        int indexOfBlank = firstLegDistanceDescr.IndexOf(" ");
                        string metric = firstLegDistanceDescr.Substring(indexOfBlank);

                        foreach (RouteLeg leg in route.Legs)
                        {
                            if (routeAppointmentList.ElementAtOrDefault(i) != null)
                            {
                                graphAppointmentMaint.AppointmentRecords.Current = graphAppointmentMaint.AppointmentRecords.Search<FSAppointment.refNbr>(
                                routeAppointmentList.ElementAt(i).RefNbr, routeAppointmentList.ElementAt(i).SrvOrdType);
                                fsAppointmentRow = graphAppointmentMaint.AppointmentRecords.Current;

                                double estimatedDurationTotal = (double)fsAppointmentRow.EstimatedDurationTotal;

                                // assign one minute to the services duration if the appointment has NO services.
                                servicesDuration = (estimatedDurationTotal == 0) ? 1 : estimatedDurationTotal;

                                if (i == 0)
                                {
                                    fsAppointmentRow.ScheduledDateTimeBegin = new DateTime(fsAppointmentRow.ScheduledDateTimeBegin.Value.Year,
                                                                                           fsAppointmentRow.ScheduledDateTimeBegin.Value.Month,
                                                                                           fsAppointmentRow.ScheduledDateTimeBegin.Value.Day,
                                                                                           fsRouteDocumentRow.TimeBegin.Value.Hour,
                                                                                           fsRouteDocumentRow.TimeBegin.Value.Minute,
                                                                                           fsRouteDocumentRow.TimeBegin.Value.Second);

                                    fsAppointmentRow.ScheduledDateTimeBegin = fsAppointmentRow.ScheduledDateTimeBegin.Value.AddSeconds(leg.Duration);
                                }
                                else
                                {
                                    fsAppointmentRow.ScheduledDateTimeBegin = lastAppoinmentEndTime;
                                    fsAppointmentRow.ScheduledDateTimeBegin = fsAppointmentRow.ScheduledDateTimeBegin.Value.AddSeconds(leg.Duration);
                                }

                                fsAppointmentRow.ScheduledDateTimeEnd = fsAppointmentRow.ScheduledDateTimeBegin;
                                fsAppointmentRow.ScheduledDateTimeEnd = fsAppointmentRow.ScheduledDateTimeEnd.Value.AddMinutes(servicesDuration);
                                TimeSpan? diffTime = fsAppointmentRow.ScheduledDateTimeEnd - fsAppointmentRow.ScheduledDateTimeEnd;

                                PXUpdate<
                                    Set<FSAppointment.scheduledDateTimeBegin, Required<FSAppointment.scheduledDateTimeBegin>,
                                    Set<FSAppointment.scheduledDateTimeEnd, Required<FSAppointment.scheduledDateTimeEnd>,
                                    Set<FSAppointment.routePosition, Required<FSAppointment.routePosition>>>>,
                                FSAppointment,
                                Where<
                                    FSAppointment.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>
                                .Update(graph,
                                        fsAppointmentRow.ScheduledDateTimeBegin,
                                        fsAppointmentRow.ScheduledDateTimeEnd,
                                        fsAppointmentRow.RoutePosition,
                                        fsAppointmentRow.AppointmentID);

                                lastAppoinmentEndTime = fsAppointmentRow.ScheduledDateTimeEnd;
                            }

                            indexOfBlank = leg.DistanceDescr.IndexOf(" ");
                            distanceTotal += Convert.ToDouble(leg.DistanceDescr.Substring(0, indexOfBlank));
                            i++;
                        }

                        totalDistance += (decimal)route.Distance;
                        totalDistanceFriendly = distanceTotal.ToString() + " " + metric;
                        totalDuration += route.Duration / 60;
                    }
                }

                graph.SelectTimeStamp();
            }
            catch (Exception)
            {
                //@TODO AC-142850 SD-5806 Handle google maps exceptions
            }
        }

        /// <summary>
        /// Set the simple stats for a given route.
        /// </summary>
        /// <param name="graph">Context graph instance.</param>
        /// <param name="routeDocumentID">ID of the route.</param>
        /// <param name="fsRouteDocumentRow">FSRoute object.</param>
        public virtual void SetRouteSimpleStats(PXGraph graph, int? routeDocumentID, ref FSRouteDocument fsRouteDocumentRow)
        {
            fsRouteDocumentRow.TotalNumAppointments = GetRouteTotalAppointments(graph, routeDocumentID);

            PXResultset<FSAppointmentDet> bqlResultSet = GetRouteServices(graph, routeDocumentID);
            fsRouteDocumentRow.TotalServices = bqlResultSet.Count;
            fsRouteDocumentRow.TotalServicesDuration = CalculateRouteTotalServicesDuration(bqlResultSet);
        }

        public virtual void SetRouteMapStats(PXGraph graph, FSAppointment fsAppointmentRow, int? routeDocumentID, ref FSRouteDocument fsRouteDocumentRow, string apiKey)
        {
            decimal? totalDistance = null;
            string totalDistanceFriendly = null;
            int? totalDuration = null;

            using (var ts = new PXTransactionScope())
            {
                try
                {
                    CalculateRouteMapStats(graph, routeDocumentID, ref totalDistance, ref totalDistanceFriendly, ref totalDuration, apiKey);
                    ts.Complete();
                }
                catch
                {
                    ts.Dispose();
                }
            }

            if (totalDistance != null)
            {
                fsRouteDocumentRow.TotalDistance = totalDistance;
            }

            if (totalDistanceFriendly != null)
            {
                fsRouteDocumentRow.TotalDistanceFriendly = totalDistanceFriendly;
            }

            if (totalDuration != null)
            {
                fsRouteDocumentRow.TotalDuration = totalDuration;
                fsRouteDocumentRow.TotalTravelTime = CalculateRouteTotalAppointmentsDuration(graph, routeDocumentID, fsAppointmentRow) + fsRouteDocumentRow.TotalDuration;
            }
        }

        public virtual void ResetLatLong(FSSrvOrdType fsSrvOrdTypeRow)
        {
            FSAppointment fsAppointmentRow = AppointmentSelected.Current;

            if (fsSrvOrdTypeRow != null
                    && fsAppointmentRow != null
                        && fsSrvOrdTypeRow.Behavior == FSSrvOrdType.behavior.Values.RouteAppointment)
            {
                fsAppointmentRow.MapLatitude = null;
                fsAppointmentRow.MapLongitude = null;
            }
        }

        private void SetGeoLocation(FSAddress fsAddressRow, FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsSrvOrdTypeRow != null
                    && fsSrvOrdTypeRow.Behavior == FSSrvOrdType.behavior.Values.RouteAppointment)
            {
                FSAppointment fsAppointmentRow = AppointmentSelected.Current;

                if (fsAppointmentRow != null
                    && (fsAppointmentRow.MapLatitude == null
                        || fsAppointmentRow.MapLongitude == null))
                {
                    try
                    {
                        GLocation[] results = Geocoder.Geocode(SharedFunctions.GetAppointmentAddress(fsAddressRow), SetupRecord.Current.MapApiKey);

                        if (results != null
                            && results.Length > 0)
                        {
                            //If there are many locations, we just pick first one
                            fsAppointmentRow.MapLatitude = (decimal)results[0].LatLng.Latitude;
                            fsAppointmentRow.MapLongitude = (decimal)results[0].LatLng.Longitude;
                        }
                    }
                    catch
                    {
                        // Do nothing.
                    }
                }
            }
        }

        /// <summary>
        /// Return true if the current appointment has at least one <c>FSAppointmentEmployee</c> row with employee or employee combined type.
        /// </summary>
        public virtual bool AreThereAnyEmployees()
        {
            FSAppointmentEmployee fsAppointmentEmployeeRow = PXSelect<FSAppointmentEmployee,
                                                             Where<
                                                                 Where<
                                                                     FSAppointmentEmployee.appointmentID, Equal<Current<FSAppointment.appointmentID>>,
                                                                     And<
                                                                         Where<
                                                                             FSAppointmentEmployee.type, Equal<BAccountType.employeeType>,
                                                                             Or<FSAppointmentEmployee.type, Equal<BAccountType.empCombinedType>>>>>>>
                                                             .SelectWindowed(this, 0, 1);

            return fsAppointmentEmployeeRow != null;
        }

        /// <summary>
        /// Hides or shows Appointments, Pickup Delivery Items Tabs & Invoice Info section.
        /// </summary>
        /// <param name="fsAppointmentRow">Appointment Row.</param>
        public virtual void HideOrShowTabs(FSAppointment fsAppointmentRow)
        {
            var relatedServiceOrder = ServiceOrderRelated.Current;

            bool isBillingByApp = BillingCycleRelated.Current != null && relatedServiceOrder?.BillingBy == ID.Billing_By.APPOINTMENT ? true : false;
            fsAppointmentRow.ShowInvoicesTab = isBillingByApp;
        }

        public virtual void HideOrShowRouteInfo(FSAppointment fsAppointmentRow)
        {
            PXUIFieldAttribute.SetVisible<FSAppointment.routeID>(AppointmentRecords.Cache, fsAppointmentRow, fsAppointmentRow.IsRouteAppoinment == true);
            PXUIFieldAttribute.SetVisible<FSAppointment.routeDocumentID>(AppointmentRecords.Cache, fsAppointmentRow, fsAppointmentRow.IsRouteAppoinment == true);
        }

        /// <summary>
        /// Hides or shows fields related to the Employee Time Cards Integration.
        /// </summary>
        public virtual void HideOrShowTimeCardsIntegration(PXCache cache, FSAppointment fsAppointmentRow)
        {
            if (this.SetupRecord.Current != null
                    && ServiceOrderTypeSelected.Current != null)
            {
                bool enableLogTEIntegration = SetupRecord.Current.EnableEmpTimeCardIntegration == true
                                                        && ServiceOrderTypeSelected.Current.CreateTimeActivitiesFromAppointment == true;

                bool projectAndTimeReportingEnabled = PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() && PXAccess.FeatureInstalled<FeaturesSet.timeReportingModule>();

                PXUIFieldAttribute.SetVisible<FSAppointmentLog.timeCardCD>(LogRecords.Cache, null, enableLogTEIntegration);
                PXUIFieldAttribute.SetVisible<FSAppointmentLog.approvedTime>(LogRecords.Cache, null, enableLogTEIntegration);

                PXUIFieldAttribute.SetVisibility<FSAppointmentLog.isBillable>(LogRecords.Cache, null, enableLogTEIntegration && projectAndTimeReportingEnabled == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
                PXUIFieldAttribute.SetVisibility<FSAppointmentLog.billableTimeDuration>(LogRecords.Cache, null, enableLogTEIntegration && projectAndTimeReportingEnabled == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
                PXUIFieldAttribute.SetVisibility<FSAppointmentLog.curyBillableTranAmount>(LogRecords.Cache, null, enableLogTEIntegration && projectAndTimeReportingEnabled == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);

                PXUIFieldAttribute.SetVisible<FSAppointmentLog.earningType>(LogRecords.Cache, null, enableLogTEIntegration);
                PXUIFieldAttribute.SetVisible<FSAppointmentLog.trackTime>(LogRecords.Cache, null, enableLogTEIntegration);
            }
        }

        /// <summary>
        /// Sets the BillServiceContractID field from the ServiceOrder's ServiceContractID field for prepaid contracts.
        /// </summary>
        public virtual void SetBillServiceContractIDFromSO(PXCache cache, FSAppointment fsAppointmentRow, int? serviceContractID)
        {
            if (serviceContractID == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = FSServiceContract.PK.Find(cache.Graph, serviceContractID);

            if (fsServiceContractRow != null)
            {
                bool isPrepaidContract = fsServiceContractRow.BillingType == ID.Contract_BillingType.STANDARDIZED_BILLINGS;

                var relatedServiceOrder = ServiceOrderRelated.Current;

                if (isPrepaidContract == true
                        && BillingCycleRelated.Current != null
                            && relatedServiceOrder?.BillingBy == ID.Billing_By.APPOINTMENT)
                {
                    cache.SetValueExt<FSAppointment.billServiceContractID>(fsAppointmentRow, fsServiceContractRow.ServiceContractID);
                }
            }
        }

        protected virtual void SetServiceOrderRelatedBySORefNbr(PXCache cache, FSAppointment fsAppointmentRow)
        {
            // Loading an existing FSServiceOrder record
            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)PXSelectorAttribute.Select<FSAppointment.soRefNbr>(cache, fsAppointmentRow);

            fsAppointmentRow.SOID = fsServiceOrderRow.SOID;

            LoadServiceOrderRelated(fsAppointmentRow);

            cache.SetValueExt<FSAppointment.curyID>(fsAppointmentRow, fsServiceOrderRow.CuryID);
            cache.SetValueExt<FSAppointment.taxZoneID>(fsAppointmentRow, fsServiceOrderRow.TaxZoneID);
            cache.SetValueExt<FSAppointment.taxCalcMode>(fsAppointmentRow, fsServiceOrderRow.TaxCalcMode);
            cache.SetValueExt<FSAppointment.projectID>(fsAppointmentRow, fsServiceOrderRow.ProjectID);
            cache.SetValueExt<FSAppointment.dfltProjectTaskID>(fsAppointmentRow, fsServiceOrderRow.DfltProjectTaskID);
            cache.SetDefaultExt<FSAppointment.salesPersonID>(fsAppointmentRow);
            cache.SetDefaultExt<FSAppointment.commissionable>(fsAppointmentRow);

            SetBillServiceContractIDFromSO(cache, fsAppointmentRow, fsServiceOrderRow.ServiceContractID);

            if (fsAppointmentRow.DocDesc == null)
            {
                fsAppointmentRow.DocDesc = fsServiceOrderRow.DocDesc;
            }
        }

        protected virtual bool CanExecuteAppointmentRowPersisting(PXCache cache, PXRowPersistingEventArgs e)
        {
            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            //SrvOrdType is key field
            if (string.IsNullOrWhiteSpace(fsAppointmentRow.SrvOrdType))
            {
                GraphHelper.RaiseRowPersistingException<FSAppointment.srvOrdType>(cache, e.Row);
            }

            LoadServiceOrderRelated(fsAppointmentRow);

            if (ServiceOrderRelated.Current == null)
            {
                throw new PXException(TX.Error.SERVICE_ORDER_SELECTED_IS_NULL);
            }

            BackupOriginalValues(cache, e);

            if ((e.Operation & PXDBOperation.Command) != PXDBOperation.Delete)
            {
                if (fsAppointmentRow.SOID < 0)
                {
                    fsAppointmentRow.SOID = ServiceOrderRelated.Current.SOID;
                    fsAppointmentRow.SORefNbr = ServiceOrderRelated.Current.RefNbr;
                }
            }

            return true;
        }

        // TODO:
        [Obsolete("This method will be deleted in the next major release.")]
        public virtual bool UpdateServiceOrder(FSAppointment fsAppointmentRow,
                                               AppointmentEntry graphAppointmentEntry,
                                               object rowInProcessing,
                                               PXDBOperation operation,
                                               PXTranStatus? tranStatus,
                                               bool throwExceptions,
                                               out Exception exception)
        {
            exception = null;

            return UpdateServiceOrder(fsAppointmentRow,
                                       graphAppointmentEntry,
                                       rowInProcessing,
                                       operation,
                                       tranStatus);
        }

        public virtual bool UpdateServiceOrder(FSAppointment fsAppointmentRow,
                                       AppointmentEntry graphAppointmentEntry,
                                       object rowInProcessing,
                                       PXDBOperation operation,
                                       PXTranStatus? tranStatus)
        {
            if (CatchedServiceOrderUpdateException != null)
            {
                return false;
            }

            try
            {
                return UpdateServiceOrderWithoutErrorHandler(fsAppointmentRow,
                                                   graphAppointmentEntry,
                                                   rowInProcessing,
                                                   operation,
                                                   tranStatus);
            }
            catch (ServiceOrderUpdateException ex)
            {
                CatchedServiceOrderUpdateException = ex;
                return false;
            }
            catch (ServiceOrderUpdateException2 ex)
            {
                CatchedServiceOrderUpdateException = ex;
                return false;
            }
        }

        public virtual bool UpdateServiceOrderWithoutErrorHandler(FSAppointment fsAppointmentRow,
                                               AppointmentEntry graphAppointmentEntry,
                                               object rowInProcessing,
                                               PXDBOperation operation,
                                               PXTranStatus? tranStatus)
        {
            if (serviceOrderIsAlreadyUpdated == true || SkipServiceOrderUpdate == true || fsAppointmentRow == null || fsAppointmentRow.MustUpdateServiceOrder != true)
            {
                return true;
            }

            // tranStatus is null when the caller is a RowPersisting event.
            if (tranStatus != null && tranStatus == PXTranStatus.Aborted)
            {
                return false;
            }

            bool deletingAppointment = false;
            bool forceAppointmentCheckings = false;
            PXEntryStatus appointmentRowEntryStatus = graphAppointmentEntry.AppointmentRecords.Cache.GetStatus(fsAppointmentRow);

            if (appointmentRowEntryStatus == PXEntryStatus.Deleted)
            {
                // When the Appointment is being deleted, the ServiceOrder is not updated in any RowPersisting event
                // but in the RowPersisted event of FSAppointment.
                if (tranStatus == null
                    || tranStatus != PXTranStatus.Completed
                    || operation != PXDBOperation.Delete
                    || (rowInProcessing is FSAppointment) == false)
                {
                    return true;
                }
                else
                {
                    deletingAppointment = true;
                }
            }

            ServiceOrderEntry graphServiceOrderEntry = GetServiceOrderEntryGraph(true);

            // Variables with short names
            ServiceOrderEntry soGraph = graphServiceOrderEntry;
            AppointmentEntry apptGraph = graphAppointmentEntry;

            FSServiceOrder fsServiceOrderRow = null;

            if (graphServiceOrderEntry.RunningPersist == false)
            {
                graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords
                        .Search<FSServiceOrder.refNbr>(fsAppointmentRow.SORefNbr, fsAppointmentRow.SrvOrdType);
            }

            fsServiceOrderRow = graphServiceOrderEntry.ServiceOrderRecords.Current;

            if (fsServiceOrderRow == null
                || fsServiceOrderRow.SrvOrdType != fsAppointmentRow.SrvOrdType
                || fsServiceOrderRow.RefNbr != fsAppointmentRow.SORefNbr)
            {
                throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSServiceOrder)));
            }

            if (deletingAppointment == false)
            {
                if (appointmentRowEntryStatus != PXEntryStatus.Inserted)
                {
                    bool? oldNotStarted = (bool?)graphAppointmentEntry.AppointmentRecords.Cache.GetValueOriginal<FSAppointment.notStarted>(fsAppointmentRow);
                    bool? oldInProcess = (bool?)graphAppointmentEntry.AppointmentRecords.Cache.GetValueOriginal<FSAppointment.inProcess>(fsAppointmentRow);

                    forceAppointmentCheckings = (oldNotStarted == true || oldInProcess == true) != (fsAppointmentRow.NotStarted == true || fsAppointmentRow.InProcess == true);

                    if (fsAppointmentRow.Finished != (bool?)graphAppointmentEntry.AppointmentRecords.Cache.GetValueOriginal<FSAppointment.finished>(fsAppointmentRow))
                    {
                        forceAppointmentCheckings = true;
                    }
                }
                else
                {
                    if ((fsAppointmentRow.NotStarted == true || fsAppointmentRow.InProcess == true) && fsAppointmentRow.Finished == false)
                    {
                        forceAppointmentCheckings = true;
                    }
                }

                if (forceAppointmentCheckings == false)
                {
                    forceAppointmentCheckings = IsThereAnySODetReferenceBeingDeleted<FSAppointmentDet.sODetID>(graphAppointmentEntry.AppointmentDetails.Cache);
                }
            }

            if (graphServiceOrderEntry.ServiceOrderRecords.Current.CuryInfoID < 0)
            {
                graphServiceOrderEntry.ServiceOrderRecords.Cache.SetValueExt<FSServiceOrder.curyInfoID>(graphServiceOrderEntry.ServiceOrderRecords.Current, fsAppointmentRow.CuryInfoID);
            }

            if (deletingAppointment == true
                || forceAppointmentCheckings == true
                || ServiceOrderRelated.Cache.GetStatus(ServiceOrderRelated.Current) != PXEntryStatus.Notchanged)
            {
                // There is not need to copy ServiceOrderRelated's current values
                // to graphServiceOrderEntry.ServiceOrderRecords' current
                // because this graph (AppointmentEntry) just finished persisting the ServiceOrderRelated record.
                // However we mark the graphServiceOrderEntry.ServiceOrderRecords' current record as Updated
                // in order to graphServiceOrderEntry runs all its validations.
                graphServiceOrderEntry.ServiceOrderRecords.Cache.SetStatus(fsServiceOrderRow, PXEntryStatus.Updated);
                graphServiceOrderEntry.ServiceOrderRecords.Cache.IsDirty = true;
            }

            UpdateRelatedApptSummaryFieldsByDeletedLines(soGraph, graphAppointmentEntry);

            if (deletingAppointment == true)
            {
                // Save the ServiceOrder to persist the new related appointment summary values.
                try
                {
                    graphServiceOrderEntry.GraphAppointmentEntryCaller = null;
                    graphServiceOrderEntry.ForceAppointmentCheckings = true;

                    graphServiceOrderEntry.Save.Press();

                    serviceOrderIsAlreadyUpdated = true;
                }
                catch (Exception ex)
                {
                    ReplicateServiceOrderExceptions(graphAppointmentEntry, graphServiceOrderEntry, ex);

                    VerifyIfTransactionWasAborted(graphServiceOrderEntry, ex);

                    return false;
                }
                finally
                {
                    graphServiceOrderEntry.ForceAppointmentCheckings = false;
                }
            }
            else
            {
                PXResultset<FSAppointmentDet> apptLines = graphAppointmentEntry.AppointmentDetails.Select();
                List<FSApptLineSplit> processedApptSplits = new List<FSApptLineSplit>();
                graphServiceOrderEntry.SkipTaxCalcTotals = true;

                foreach (FSAppointmentDet fsAppointmentDetRow in apptLines.Where(x => ((FSAppointmentDet)x).LineType != ID.LineType_ALL.PICKUP_DELIVERY))
                {
                    PXEntryStatus lineStatus = graphAppointmentEntry.AppointmentDetails.Cache.GetStatus(fsAppointmentDetRow);

                    if (lineStatus != PXEntryStatus.Inserted
                            && lineStatus != PXEntryStatus.Updated)
                    {
                        continue;
                    }

                    apptGraph.AppointmentDetails.Current = fsAppointmentDetRow;

                    InsertUpdateSODet(graphAppointmentEntry.AppointmentDetails.Cache, fsAppointmentDetRow, graphServiceOrderEntry.ServiceOrderDetails, fsAppointmentRow);

                    List<FSApptLineSplit> apptSplits = apptGraph.Splits.Select().RowCast<FSApptLineSplit>().Where(r => string.IsNullOrEmpty(r.LotSerialNbr) == false).ToList();

                    UpdateSrvOrdSplits(apptGraph, fsAppointmentDetRow, apptSplits, soGraph);

                    processedApptSplits.AddRange(apptSplits);

                    graphServiceOrderEntry.UpdateRelatedApptSummaryFields(AppointmentDetails.Cache, fsAppointmentDetRow, graphServiceOrderEntry.ServiceOrderDetails.Cache, fsAppointmentDetRow.FSSODetRow);
                }

                try
                {
                    graphServiceOrderEntry.GraphAppointmentEntryCaller = graphAppointmentEntry;
                    graphServiceOrderEntry.ForceAppointmentCheckings = forceAppointmentCheckings;

                    if (insertingServiceOrder == true)
                    {
                        graphServiceOrderEntry.Answers.Select();
                        graphServiceOrderEntry.Answers.CopyAttributes(graphServiceOrderEntry, graphServiceOrderEntry.ServiceOrderRecords.Current, graphAppointmentEntry, graphAppointmentEntry.AppointmentRecords.Current, true);
                        insertingServiceOrder = false;
                    }

                    if (graphServiceOrderEntry.ForceAppointmentCheckings == true || graphServiceOrderEntry.IsDirty == true)
                    {
                        graphServiceOrderEntry.SkipTaxCalcTotals = false;
                        ServiceOrderEntry.SalesTax salesExtSrvOrd = graphServiceOrderEntry.GetExtension<ServiceOrderEntry.SalesTax>();
                        salesExtSrvOrd.CalcTaxes();

                        if (graphServiceOrderEntry.RunningPersist == false)
                        {
                        graphServiceOrderEntry.SelectTimeStamp();
                        graphServiceOrderEntry.SkipTaxCalcAndSave();
                        graphServiceOrderEntry.RecalculateExternalTaxes();
                    }
                    }

                    serviceOrderIsAlreadyUpdated = true;
                }
                catch (Exception ex)
                {
                    ReplicateServiceOrderExceptions(graphAppointmentEntry, graphServiceOrderEntry, ex);
                    VerifyIfTransactionWasAborted(graphServiceOrderEntry, ex);
                    return false;
                }
                finally
                {
                    graphServiceOrderEntry.GraphAppointmentEntryCaller = null;
                    graphServiceOrderEntry.ForceAppointmentCheckings = false;
                    graphServiceOrderEntry.SkipTaxCalcTotals = false;
                }

                // Fill the dictionary to update FSAppointmentDet with FSSODet values in its RowPersisting event
                FillDictionaryWithUpdatedFSSODets(apptLines);

                // Fill the dictionary to update FSApptLineSplit with FSSODetSplit values in its RowPersisting event
                FillDictionaryWithUpdatedFSSODetSplits(processedApptSplits);
            }

            if (deletingAppointment == false)
            {
                // Update the ServiceOrderRelated values with the new Service Order values.
                if (ServiceOrderRelated.Current != null && graphServiceOrderEntry.ServiceOrderRecords.Current != null
                    && ServiceOrderRelated.Current.SOID == graphServiceOrderEntry.ServiceOrderRecords.Current.SOID)
                {
                    foreach (var fieldName in ServiceOrderRelated.Cache.Fields)
                    {
                        if (fieldName == nameof(ServiceOrderRelated.Current.AppointmentsCompletedCntr)
                                            || fieldName == nameof(ServiceOrderRelated.Current.AppointmentsCompletedOrClosedCntr))
                        {
                            continue;
                        }

                        ServiceOrderRelated.Cache.SetValue(ServiceOrderRelated.Current, fieldName,
                                graphServiceOrderEntry.ServiceOrderRecords.Cache.GetValue(graphServiceOrderEntry.ServiceOrderRecords.Current, fieldName));
                    }
                }
            }

            return true;
        }

        public virtual void VerifyIfTransactionWasAborted(PXGraph graph, Exception exception)
        {
            string recordRaisedErrors = PXMessages.LocalizeNoPrefix(PX.Data.ErrorMessages.RecordRaisedErrors);
            recordRaisedErrors = recordRaisedErrors.Replace("{0}", "");
            recordRaisedErrors = recordRaisedErrors.Replace("{1}", "");
            recordRaisedErrors = recordRaisedErrors.Replace("''", "");

            if (exception.Message.Contains(recordRaisedErrors.Trim()) == true)
            {
                // This is to avoid the message:
                // NotValidTransaction = "The transaction has been silently rolled back before a database update operation.";
                throw PXException.PreserveStack(exception);
            }
        }

        public virtual void UpdateRelatedApptSummaryFieldsByDeletedLines(ServiceOrderEntry soGraph, AppointmentEntry graphAppointmentEntry)
        {
            foreach (FSAppointmentDet apptLine in graphAppointmentEntry.AppointmentDetails.Cache.Deleted)
            {
                if (apptLine.LineType == ID.LineType_ALL.PICKUP_DELIVERY || apptLine.SODetID == null || apptLine.SODetID < 0)
                {
                    continue;
                }

                apptLine.FSSODetRow = FSSODet.UK.Find(soGraph, apptLine.SODetID);

                soGraph.UpdateRelatedApptSummaryFields(AppointmentDetails.Cache, apptLine, soGraph.ServiceOrderDetails.Cache, apptLine.FSSODetRow);
            }
        }

        private void UpdateSrvOrdSplits(AppointmentEntry apptGraph, FSAppointmentDet apptLine, List<FSApptLineSplit> apptSplits, ServiceOrderEntry soGraph)
        {
            if (apptLine.SODetID != null && apptLine.SODetID != apptLine.FSSODetRow.SODetID)
            {
                throw new PXArgumentException();
            }

            FSSODet soLine = soGraph.ServiceOrderDetails.Search<FSSODet.sODetID>(apptLine.FSSODetRow.SODetID);
            if (soLine == null || soLine.SODetID != apptLine.FSSODetRow.SODetID)
            {
                throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODet)));
            }

            apptLine.FSSODetRow = soGraph.ServiceOrderDetails.Current = soLine;

            decimal origEstimatedQty = (decimal)soLine.EstimatedQty;
            int insertedSplitCount = 0;
            FSSODetSplit firstInsertedSplit = null;

            // Insert splits in Service Order with new LotSerialNbrs
            foreach (FSApptLineSplit apptSplit in apptSplits)
            {
                FSSODetSplit soSplit = soGraph.Splits.Select().RowCast<FSSODetSplit>().Where(r => r.LotSerialNbr == apptSplit.LotSerialNbr).FirstOrDefault();
                if (soSplit == null)
                {
                    var newSOSplit = new FSSODetSplit();

                    newSOSplit = (FSSODetSplit)soGraph.Splits.Cache.CreateCopy(soGraph.Splits.Insert(newSOSplit));

                    newSOSplit.LotSerialNbr = apptSplit.LotSerialNbr;
                    newSOSplit.Qty = apptSplit.Qty;
                    newSOSplit.BaseQty = INUnitAttribute.ConvertToBase(soGraph.Splits.Cache, newSOSplit.InventoryID, newSOSplit.UOM, newSOSplit.Qty.Value, INPrecision.QUANTITY);

                    newSOSplit = soGraph.Splits.Update(newSOSplit);

                    PropagateServiceOrderErrors(soGraph.Splits.Cache, newSOSplit,
                                                apptGraph.Splits.Cache, apptSplit,
                                                TX.Action.InsertingLotSerialInServiceOrder, apptSplit.LotSerialNbr, apptLine.LineRef);

                    insertedSplitCount++;

                    soSplit = newSOSplit;
                }

                apptSplit.FSSODetSplitRow = soSplit;
                if (firstInsertedSplit == null)
                {
                    firstInsertedSplit = soSplit;
                }
            }

            // Decrease the Qty on the uncompleted splits without LotSerialNbr to restore the original EstimatedQty
            decimal surplusQuantity = (decimal)soLine.EstimatedQty > origEstimatedQty ? (decimal)soLine.EstimatedQty - origEstimatedQty : 0m;
            while (surplusQuantity > 0m)
            {
                FSSODetSplit soSplit = soGraph.Splits.Select().RowCast<FSSODetSplit>().Where(r => string.IsNullOrEmpty(r.LotSerialNbr) == true && r.Completed == false).FirstOrDefault();
                if (soSplit != null)
                {
                    FSSODetSplit soSplitCopy = (FSSODetSplit)soGraph.Splits.Cache.CreateCopy(soSplit);

                    if (soSplitCopy.Qty >= surplusQuantity)
                    {
                        soSplitCopy.Qty -= surplusQuantity;
                        surplusQuantity = 0m;
                    }
                    else
                    {
                        surplusQuantity -= (decimal)soSplitCopy.Qty;
                        soSplitCopy.Qty = 0m;
                    }

                    if (soSplitCopy.Qty == 0m)
                    {
                        soGraph.Splits.Delete(soSplit);
                    }
                    else
                    {
                        soSplitCopy.BaseQty = INUnitAttribute.ConvertToBase(soGraph.Splits.Cache, soSplitCopy.InventoryID, soSplitCopy.UOM, soSplitCopy.Qty.Value, INPrecision.QUANTITY);
                        soSplitCopy = soGraph.Splits.Update(soSplitCopy);
                    }
                }
                else
                {
                    break;
                }
            }

            if (origEstimatedQty != (decimal)soLine.EstimatedQty)
            {
                Exception exception = new PXSetPropertyException(TX.Error.UpdatingTheServiceOrderLotSerialsEndedInAnAttemptToIncreaseTheLineQty, PXErrorLevel.Error);

                apptGraph.AppointmentDetails.Cache.RaiseExceptionHandling<FSAppointmentDet.lotSerialNbr>(apptLine, null, exception);
                throw new ServiceOrderUpdateException2(TX.Error.UpdatingTheServiceOrderLotSerialsEndedInAnAttemptToIncreaseTheLineQty);
            }

            apptLine.FSSODetRow = soGraph.ServiceOrderDetails.Current;
        }

        // TODO: refactor this method to use ReplicateCacheExceptions.
        // - Update ReplicateCacheExceptions with this code.
        protected virtual void PropagateServiceOrderErrors(PXCache errorSourceCache, object errorSourceRow,
                                                        PXCache mappingCache, object mappingRow,
                                                        string actionMessage, params string[] messageParams)
        {
            Dictionary<string, string> errors;
            errors = PXUIFieldAttribute.GetErrors(errorSourceCache, errorSourceRow, PXErrorLevel.Error, PXErrorLevel.RowError, PXErrorLevel.Undefined);

            if (errors == null)
            {
                return;
            }

            string localizedActionMessage = PXMessages.LocalizeFormatNoPrefix(actionMessage, messageParams);

            List<string> uiFields = SharedFunctions.GetUIFields(mappingCache, mappingRow);
            bool fieldWithoutMapping = false;

            foreach (KeyValuePair<string, string> entry in errors)
            {
                Exception exception = new PXSetPropertyException(TX.Error.XErrorOccurredDuringActionY, PXErrorLevel.Error,
                                                                entry.Value, localizedActionMessage);

                if (uiFields.Any(e => e.Equals(entry.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    mappingCache.RaiseExceptionHandling(entry.Key, mappingRow, null, exception);
                }
                else
                {
                    fieldWithoutMapping = true;
                }
            }

            if (errors.Count > 0)
            {
                if (fieldWithoutMapping == false)
                {
                    throw new ServiceOrderUpdateException(errors,
                                                          mappingCache.Graph.GetType(),
                                                          mappingRow,
                                                          actionMessage,
                                                          messageParams);
                }
                else
                {
                    throw new PXOuterException(errors,
                                               mappingCache.Graph.GetType(),
                                               mappingRow,
                                               actionMessage,
                                               messageParams);
                }
            }
        }

        private void FillDictionaryWithUpdatedFSSODets(PXResultset<FSAppointmentDet> apptLines)
        {
            if (ApptLinesWithSrvOrdLineUpdated == null)
            {
                ApptLinesWithSrvOrdLineUpdated = new Dictionary<FSAppointmentDet, FSSODet>();
            }
            else
            {
                ApptLinesWithSrvOrdLineUpdated.Clear();
            }

            foreach (FSAppointmentDet fsAppointmentDetRow in apptLines)
            {
                if (fsAppointmentDetRow.FSSODetRow != null)
                {
                    ApptLinesWithSrvOrdLineUpdated[fsAppointmentDetRow] = fsAppointmentDetRow.FSSODetRow;
                }
            }
        }

        private void FillDictionaryWithUpdatedFSSODetSplits(List<FSApptLineSplit> apptSplits)
        {
            if (ApptSplitsWithSrvOrdSplitUpdated == null)
            {
                ApptSplitsWithSrvOrdSplitUpdated = new Dictionary<FSApptLineSplit, FSSODetSplit>();
            }
            else
            {
                ApptSplitsWithSrvOrdSplitUpdated.Clear();
            }

            foreach (FSApptLineSplit apptSplit in apptSplits)
            {
                if (apptSplit.FSSODetSplitRow != null)
                {
                    ApptSplitsWithSrvOrdSplitUpdated[apptSplit] = apptSplit.FSSODetSplitRow;
                }
            }
        }

        protected virtual void InsertUpdateSODet(PXCache cacheAppointmentDet, FSAppointmentDet fsAppointmentDetRow, PXSelectBase<FSSODet> viewSODet, FSAppointment apptRow)
        {
            PXEntryStatus lineStatus = cacheAppointmentDet.GetStatus(fsAppointmentDetRow);

            if (lineStatus != PXEntryStatus.Inserted
                    && lineStatus != PXEntryStatus.Updated)
            {
                return;
            }

            FSSODet fsSODetRow = null;

            if (fsAppointmentDetRow.SODetID != null)
            {
                fsSODetRow = FSSODet.UK.Find(viewSODet.Cache.Graph, fsAppointmentDetRow.SODetID);

                if (fsSODetRow == null || fsSODetRow.SODetID != fsAppointmentDetRow.SODetID)
                {
                    throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODet)));
                }
            }

            bool insertedUpdated = false;

            if (fsSODetRow == null)
            {
                fsSODetRow = new FSSODet();

                try
                {
                    fsSODetRow.SkipUnitPriceCalc = true;
                    fsSODetRow.AlreadyCalculatedUnitPrice = fsAppointmentDetRow.CuryUnitPrice;

                    fsSODetRow = InsertDetailLine<FSSODet, FSAppointmentDet>(viewSODet.Cache,
                                                                                 fsSODetRow,
                                                                                 cacheAppointmentDet,
                                                                                 fsAppointmentDetRow,
                                                                                 noteID: null,
                                                                                 soDetID: null,
                                                                                 copyTranDate: true,
                                                                                 tranDate: fsAppointmentDetRow.TranDate,
                                                                                 SetValuesAfterAssigningSODetID: true,
                                                                                 copyingFromQuote: false);

                    fsAppointmentDetRow.SODetCreate = true;
                    insertedUpdated = true;
                }
                finally
                {
                    fsSODetRow.SkipUnitPriceCalc = false;
                    fsSODetRow.AlreadyCalculatedUnitPrice = null;
                }

                fsAppointmentDetRow.FSSODetRow = fsSODetRow;
            }
            else
            {
                fsSODetRow = (FSSODet)viewSODet.Cache.CreateCopy(fsSODetRow);

                if (fsSODetRow.BranchID != fsAppointmentDetRow.BranchID)
                {
                    viewSODet.Cache.SetValue<FSSODet.branchID>(fsSODetRow, fsAppointmentDetRow.BranchID);
                    insertedUpdated = true;
                }

                if (fsSODetRow.SiteID != fsAppointmentDetRow.SiteID)
                {
                    fsSODetRow.SiteID = fsAppointmentDetRow.SiteID;
                    insertedUpdated = true;
                }

                if (fsSODetRow.SiteLocationID != fsAppointmentDetRow.SiteLocationID)
                {
                    fsSODetRow.SiteLocationID = fsAppointmentDetRow.SiteLocationID;
                    insertedUpdated = true;
                }

                if (CanEditSrvOrdLineValues(cacheAppointmentDet, fsAppointmentDetRow, fsSODetRow) == true)
                {
                    if (fsSODetRow.POCreate != fsAppointmentDetRow.EnablePO)
                    {
                        fsSODetRow.POCreate = fsAppointmentDetRow.EnablePO;
                        insertedUpdated = true;
                    }

                    if (fsSODetRow.POSource != fsAppointmentDetRow.POSource)
                    {
                        fsSODetRow.POSource = fsAppointmentDetRow.POSource;
                        insertedUpdated = true;
                    }

                    if (fsSODetRow.POVendorID != fsAppointmentDetRow.POVendorID)
                    {
                        fsSODetRow.POVendorID = fsAppointmentDetRow.POVendorID;
                        insertedUpdated = true;
                    }

                    if (fsSODetRow.POVendorLocationID != fsAppointmentDetRow.POVendorLocationID)
                    {
                        fsSODetRow.POVendorLocationID = fsAppointmentDetRow.POVendorLocationID;
                        insertedUpdated = true;
                    }

                    if (fsSODetRow.CuryUnitCost != fsAppointmentDetRow.CuryUnitCost)
                    {
                        fsSODetRow.CuryUnitCost = fsAppointmentDetRow.CuryUnitCost;
                        insertedUpdated = true;
                    }

                    if (fsSODetRow.ManualCost != fsAppointmentDetRow.ManualCost)
                    {
                        fsSODetRow.ManualCost = fsAppointmentDetRow.ManualCost;
                        insertedUpdated = true;
                    }

                    if (fsSODetRow.EstimatedQty != fsAppointmentDetRow.EstimatedQty)
                    {
                        fsSODetRow.EstimatedQty = fsAppointmentDetRow.EstimatedQty;
                        insertedUpdated = true;
                    }
                }

                if (insertedUpdated)
                {
                    fsSODetRow = viewSODet.Update(fsSODetRow);
                }
            }

            if (fsSODetRow.LineType == ID.LineType_ALL.SERVICE
                && ID.Status_SODet.CanBeScheduled(fsSODetRow.Status) == true
                    &&
                        (apptRow.NotStarted == true 
                        || apptRow.InProcess == true)
                    )
            {
                // Inserting or updating, this SODet line is being Scheduled in this Appointment.
                if (fsSODetRow.Scheduled != true)
                {
                    viewSODet.Cache.SetValue<FSSODet.scheduled>(fsSODetRow, true);
                    insertedUpdated = true;
                }
            }

            if (insertedUpdated == true)
            {
                fsSODetRow = viewSODet.Update(fsSODetRow);
            }

            fsAppointmentDetRow.FSSODetRow = fsSODetRow;
        }

        public virtual bool CanEditSrvOrdLineValues(PXCache cacheAppointmentDet, FSAppointmentDet fsAppointmentDetRow, FSSODet fsSODetRow)
        {
            // TODO: add verification of posting status
            return (fsAppointmentDetRow.EnablePO == true
                        || (bool?)cacheAppointmentDet.GetValueOriginal<FSAppointmentDet.enablePO>(fsAppointmentDetRow) == true)
                    && fsAppointmentDetRow.CanChangeMarkForPO == true
                    && fsSODetRow.ApptCntr <= 1
                    && fsSODetRow.IsPrepaid == false;
        }

        protected virtual bool IsThereAnySODetReferenceBeingDeleted<SODetIDType>(PXCache cache)
            where SODetIDType : IBqlField
        {
            // Check if some line is being deleted.
            foreach (object row in cache.Deleted)
            {
                return true;
            }

            // Check if some line is changing its SODet reference.
            foreach (object row in cache.Updated)
            {
                if ((int?)cache.GetValue<SODetIDType>(row) != (int?)cache.GetValueOriginal<SODetIDType>(row))
                {
                    return true;
                }
            }

            // Check if some line is changing its SODet reference.
            foreach (object row in cache.Inserted)
            {
                if ((int?)cache.GetValue<SODetIDType>(row) != (int?)cache.GetValueOriginal<SODetIDType>(row))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual bool IsAppointmentBeingDeleted(int? appointmentID, PXCache cache)
        {
            foreach (FSAppointment fsAppointmentRow in cache.Deleted)
            {
                if (fsAppointmentRow.AppointmentID == appointmentID)
                {
                    return true;
                }
            }

            return false;
        }

        [Obsolete("This method will be deleted in the next major release. Use the version that receives the parameter of type Exception.")]
        protected virtual int ReplicateServiceOrderExceptions(AppointmentEntry graphAppointmentEntry, ServiceOrderEntry graphServiceOrderEntry)
        {
            Exception exception = new PXException(TX.Error.ErrorUpdatingTheServiceOrderWithTheMessageX, string.Empty);
            return ReplicateServiceOrderExceptions(graphAppointmentEntry, graphServiceOrderEntry, exception);
        }

        protected virtual int ReplicateServiceOrderExceptions(AppointmentEntry graphAppointmentEntry, ServiceOrderEntry graphServiceOrderEntry, Exception exception)
        {
            int errorCount = 0;

            errorCount += SharedFunctions.ReplicateCacheExceptions(graphAppointmentEntry.AppointmentRecords.Cache,
                                                                   graphAppointmentEntry.AppointmentRecords.Current,
                                                                   graphAppointmentEntry.ServiceOrderRelated.Cache,
                                                                   graphAppointmentEntry.ServiceOrderRelated.Current,
                                                                   graphServiceOrderEntry.ServiceOrderRecords.Cache,
                                                                   graphServiceOrderEntry.ServiceOrderRecords.Current);

            foreach (FSAppointmentDet fsAppointmentDetRow in graphAppointmentEntry.AppointmentDetails.Select())
            {
                if (fsAppointmentDetRow.FSSODetRow != null)
                {
                    errorCount += SharedFunctions.ReplicateCacheExceptions(graphAppointmentEntry.AppointmentDetails.Cache,
                                                                           fsAppointmentDetRow,
                                                                           graphServiceOrderEntry.ServiceOrderDetails.Cache,
                                                                           fsAppointmentDetRow.FSSODetRow);
                }
            }

            if (errorCount == 0)
            {
                throw PXException.PreserveStack(exception);
            }

            return errorCount;
        }

        protected void RestoreOriginalValues(PXCache cache, PXRowPersistedEventArgs e)
        {
            if (_oldRows == null)
            {
                return;
            }

            if ((e.Operation & PXDBOperation.Command) != PXDBOperation.Delete && e.TranStatus == PXTranStatus.Aborted)
            {
                object oldRow;

                if (_oldRows.TryGetValue(e.Row, out oldRow) && e.Row.GetType() == oldRow.GetType())
                {
                    cache.RestoreCopy(e.Row, oldRow);
                }
            }
        }

        protected void BackupOriginalValues(PXCache cache, PXRowPersistingEventArgs e)
        {
            if ((e.Operation & PXDBOperation.Command) != PXDBOperation.Delete)
            {
                if (_oldRows == null)
                {
                    _oldRows = new Dictionary<object, object>();
                }

                object oldRow;

                // This is to avoid create multiple DAC instances for the same e.Row.
                if (_oldRows.TryGetValue(e.Row, out oldRow) && oldRow.GetType() == e.Row.GetType())
                {
                    cache.RestoreCopy(oldRow, e.Row);
                }
                else
                {
                    _oldRows[e.Row] = cache.CreateCopy(e.Row);
                }
            }
        }

        public virtual void ValidateRouteDriverDeletionFromRouteDocument(FSAppointmentEmployee fsAppointmentEmployeeRow)
        {
            if (IsAppointmentBeingDeleted(fsAppointmentEmployeeRow.AppointmentID, AppointmentRecords.Cache))
            {
                return;
            }

            if (fsAppointmentEmployeeRow.IsDriver == true
                    && Accessinfo.ScreenID == SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT)
                         && AppointmentSelected.Current.RouteDocumentID != null)
            {
                FSRouteDocument fsRouteDocumentRow = PXSelect<FSRouteDocument,
                                                     Where<
                                                         FSRouteDocument.routeDocumentID, Equal<Required<FSRouteDocument.routeDocumentID>>>>
                                                     .Select(this, AppointmentSelected.Current.RouteDocumentID);

                throw new PXException(TX.Error.DRIVER_DELETION_ERROR, fsRouteDocumentRow.RefNbr);
            }
        }

        public virtual void SetRequireSerialWarning(PXCache cache, FSAppointmentDet fsAppointmentDetServiceRow)
        {
            if (fsAppointmentDetServiceRow.SMEquipmentID != null)
            {
                FSEquipmentComponent fsEquipmentComponentRows = PXSelect<FSEquipmentComponent,
                                                                Where<
                                                                    FSEquipmentComponent.requireSerial, Equal<True>,
                                                                    And<FSEquipmentComponent.serialNumber, IsNull,
                                                                    And<FSEquipmentComponent.SMequipmentID, Equal<Required<FSEquipmentComponent.SMequipmentID>>>>>>
                                                                .Select(cache.Graph, fsAppointmentDetServiceRow.SMEquipmentID);

                if (fsEquipmentComponentRows != null)
                {
                    cache.RaiseExceptionHandling<FSAppointmentDet.SMequipmentID>(fsAppointmentDetServiceRow,
                                                                                 null,
                                                                                 new PXSetPropertyException(TX.Warning.REQUIRES_SERIAL_NUMBER, PXErrorLevel.Warning));
                }
            }
        }

        public virtual void UpdateAppointmentDetService_StaffID(string serviceLineRef, string oldServiceLineRef)
        {
            //Process current lineRef selection
            if (string.IsNullOrEmpty(serviceLineRef) == false)
            {
                var empReferencingLineRef = AppointmentServiceEmployees.Select()
                                                                       .RowCast<FSAppointmentEmployee>()
                                                                       .Where(y => y.ServiceLineRef == serviceLineRef);

                var fsAppointmentDetRows = AppointmentDetails.Select()
                                                             .RowCast<FSAppointmentDet>()
                                                             .Where(y => y.IsService && y.LineRef == serviceLineRef);

                if (fsAppointmentDetRows.Count() == 1)
                {
                    FSAppointmentDet fsAppointmentDetRow = fsAppointmentDetRows.ElementAt(0);
                    int? numEmployeeLinkedToService = empReferencingLineRef.Count();

                    fsAppointmentDetRow.StaffID = numEmployeeLinkedToService == 1 ? empReferencingLineRef.ElementAt(0).EmployeeID : null;
                    AppointmentDetails.Update(fsAppointmentDetRow);

                    UpdateStaffRelatedUnboundFields(AppointmentDetails.Current, AppointmentServiceEmployees, LogRecords, numEmployeeLinkedToService);
                }
            }

            //Clean old lineRef selection
            if (string.IsNullOrEmpty(oldServiceLineRef) == false)
            {
                var empReferencingOldLineRef = AppointmentServiceEmployees.Select()
                                                                          .RowCast<FSAppointmentEmployee>()
                                                                          .Where(y => y.ServiceLineRef == oldServiceLineRef);

                var serviceOldLineRef = AppointmentDetails.Select()
                                                          .RowCast<FSAppointmentDet>()
                                                          .Where(y => y.IsService && y.LineRef == oldServiceLineRef);

                if (serviceOldLineRef.Count() == 1)
                {
                    FSAppointmentDet fsAppointmentDetRow = serviceOldLineRef.ElementAt(0);
                    int? numOldEmployeeLinkedToService = empReferencingOldLineRef.Count();

                    fsAppointmentDetRow.StaffID = numOldEmployeeLinkedToService == 1 ? empReferencingOldLineRef.ElementAt(0).EmployeeID : null;
                    AppointmentDetails.Update(fsAppointmentDetRow);

                    UpdateStaffRelatedUnboundFields(AppointmentDetails.Current, AppointmentServiceEmployees, LogRecords, numOldEmployeeLinkedToService);
                }
            }
        }

        public virtual bool IsAnySignatureAttached(PXCache cache, FSAppointment fsAppointmentRow)
        {
            Guid[] files = PXNoteAttribute.GetFileNotes(cache, fsAppointmentRow);
            var uploadFileMaintenance = PXGraph.CreateInstance<UploadFileMaintenance>();

            foreach (Guid fileID in files)
            {
                FileInfo fileInfoRow = uploadFileMaintenance.GetFileWithNoData(fileID);
                if (fileInfoRow != null && fileInfoRow.FullName.Contains("signature") == true)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void GenerateSignedReport(PXCache cache, FSAppointment fsAppointmentRow)
        {
            if (IsAnySignatureAttached(cache, fsAppointmentRow) == false)
            {
                return;
            }

            string reportID = ID.ReportID.APPOINTMENT;

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            string srvOrdTypeFieldName = SharedFunctions.GetFieldName<FSAppointment.srvOrdType>();
            string refNbrFieldName = SharedFunctions.GetFieldName<FSAppointment.refNbr>();

            parameters[srvOrdTypeFieldName] = fsAppointmentRow.SrvOrdType;
            parameters[refNbrFieldName] = fsAppointmentRow.RefNbr;

            using (PXTransactionScope ts = new PXTransactionScope())
            {
                using (Report report = PXReportTools.LoadReport(reportID, null))
                {
                    if (report == null)
                    {
                        throw new Exception("Unable to call Acumatica report writter for specified report : " + reportID);
                    }

                    PXReportTools.InitReportParameters(report, parameters, PXSettingProvider.Instance.Default);
                    ReportNode repNode = ReportProcessor.ProcessReport(report);
                    IRenderFilter renderFilter = ReportProcessor.GetRenderer(ReportProcessor.FilterPdf);

                    using (StreamManager streamMgr = new StreamManager())
                    {
                        renderFilter.Render(repNode, null, streamMgr);
                        UploadFileMaintenance graphUploadFile = PXGraph.CreateInstance<UploadFileMaintenance>();

                        string name = string.Format("{0} - {1} - {2}.pdf", fsAppointmentRow.RefNbr, "Signed", DateTime.Now.ToString("MM_dd_yy_hh_mm_ss"));

                        var file = new FileInfo(Guid.NewGuid(), name, null, streamMgr.MainStream.GetBytes());
                        graphUploadFile.SaveFile(file, FileExistsAction.CreateVersion);

                        PXCache<NoteDoc> noteDocCache = new PXCache<NoteDoc>(this);

                        var fileNote = new NoteDoc { NoteID = fsAppointmentRow.NoteID, FileID = file.UID };

                        noteDocCache.Insert(fileNote);
                        noteDocCache.Persist(PXDBOperation.Insert);

                        Guid[] files = PXNoteAttribute.GetFileNotes(cache, fsAppointmentRow);
                        foreach (Guid fileID in files)
                        {
                            FileInfo fileInfoRow = graphUploadFile.GetFileWithNoData(fileID);

                            if (fileInfoRow != null && (fileInfoRow.UID == fsAppointmentRow.CustomerSignedReport || fileInfoRow.FullName.Contains("signature") == true))
                            {
                                UploadFileMaintenance.DeleteFile(fileInfoRow.UID);
                            }
                        }

                        fsAppointmentRow.CustomerSignedReport = file.UID;

                        PXUpdate<
                            Set<FSAppointment.customerSignedReport, Required<FSAppointment.customerSignedReport>>,
                        FSAppointment,
                        Where<
                            FSAppointment.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>
                        .Update(this,
                                fsAppointmentRow.CustomerSignedReport,
                                fsAppointmentRow.AppointmentID);

                        cache.Graph.SelectTimeStamp();
                    }
                }

                ts.Complete();
            }
        }

        public virtual void HandleServiceLineStatusChange(ref List<FSAppointmentLog> deleteReleatedTimeActivity,
                                                          ref List<FSAppointmentLog> createReleatedTimeActivity)
        {
            var serviceUpdatedLines = AppointmentDetails.Cache.Updated.RowCast<FSAppointmentDet>().Where(x => x.IsService);

            foreach (FSAppointmentDet fsAppointmentDetRow in serviceUpdatedLines)
            {
                var originalValue = AppointmentDetails.Cache.GetValueOriginal<FSAppointmentDet.status>(fsAppointmentDetRow);

                string oldServiceStatus = originalValue != null ? (string)originalValue : string.Empty;

                if (oldServiceStatus == fsAppointmentDetRow.Status)
                {
                    continue;
                }

                if (fsAppointmentDetRow.IsCanceledNotPerformed == true)
                {
                    this.FillRelatedTimeActivityList(fsAppointmentDetRow.LineRef, ref deleteReleatedTimeActivity);
                }
                else if (oldServiceStatus == ID.Status_AppointmentDet.CANCELED)
                {
                    this.FillRelatedTimeActivityList(fsAppointmentDetRow.LineRef, ref createReleatedTimeActivity);
                }
            }
        }

        public virtual void FillRelatedTimeActivityList(string lineRef,
                                                        ref List<FSAppointmentLog> fsAppointmentDetEmployeeList)
        {
            var fsAppointmentLogRecords = LogRecords.Select().RowCast<FSAppointmentLog>()
                                                                       .Where(y => y.DetLineRef == lineRef);

            foreach (FSAppointmentLog fsAppointmentLogRow in fsAppointmentLogRecords)
            {
                fsAppointmentDetEmployeeList.Add(fsAppointmentLogRow);
            }
        }

        public virtual void SetCurrentAppointmentSalesPersonID(FSServiceOrder fsServiceOrderRow)
        {
            if (fsServiceOrderRow == null)
            {
                return;
            }

            FSSrvOrdType fsSrvOrdTypeRow = ServiceOrderTypeSelected.Current;

            if (fsSrvOrdTypeRow != null
                    && fsSrvOrdTypeRow.SalesPersonID == null)
            {
                CustDefSalesPeople custDefSalesPeopleRow = PXSelect<CustDefSalesPeople,
                                                           Where<
                                                               CustDefSalesPeople.bAccountID, Equal<Required<CustDefSalesPeople.bAccountID>>,
                                                               And<CustDefSalesPeople.locationID, Equal<Required<CustDefSalesPeople.locationID>>,
                                                               And<CustDefSalesPeople.isDefault, Equal<True>>>>>
                                                           .Select(this, fsServiceOrderRow.CustomerID, fsServiceOrderRow.LocationID);

                if (custDefSalesPeopleRow != null)
                {
                    AppointmentRecords.Current.SalesPersonID = custDefSalesPeopleRow.SalesPersonID;
                    AppointmentRecords.Current.Commissionable = false;
                }
                else
                {
                    AppointmentRecords.Current.SalesPersonID = null;
                    AppointmentRecords.Current.Commissionable = false;
                }
            }
        }

        public virtual bool GetSrvOrdLineBalance(PXGraph graphToQuery, int? soDetID, int? apptDetID, out decimal srvOrdAllocatedQty, out decimal otherAppointmentsUsedQty)
        {
            if (soDetID == null || soDetID < 0)
            {
                srvOrdAllocatedQty = 0m;
                otherAppointmentsUsedQty = 0m;

                return false;
            }

            FSSODet fsSODetRow = FSSODet.UK.Find(graphToQuery, soDetID);

            srvOrdAllocatedQty = (decimal)fsSODetRow.EstimatedQty;

            FSAppointmentDet otherApptLinesSum = PXSelectGroupBy<FSAppointmentDet,
                                                 Where<
                                                     FSAppointmentDet.lineType, Equal<FSLineType.Inventory_Item>,
                                                     And<FSAppointmentDet.isCanceledNotPerformed, Equal<False>,
                                                     And<FSAppointmentDet.sODetID, Equal<Required<FSAppointmentDet.sODetID>>,
                                                     And<FSAppointmentDet.appDetID, NotEqual<Required<FSAppointmentDet.appDetID>>>>>>,
                                                 Aggregate<
                                                     GroupBy<FSAppointmentDet.sODetID,
                                                     Sum<FSAppointmentDet.effTranQty>>>>
                                                 .Select(graphToQuery, soDetID, apptDetID);

            decimal? usedQty = otherApptLinesSum != null ? otherApptLinesSum.EffTranQty : 0m;
            otherAppointmentsUsedQty = usedQty != null ? (decimal)usedQty : 0m;

            return true;
        }

        public virtual void VerifySrvOrdLineQty(PXCache cache, FSAppointmentDet apptLine, object newValue, Type QtyField, bool runningFieldVerifying)
        {
            if (newValue == null 
                || (newValue is decimal) == false 
                || (decimal)newValue == 0 
                || apptLine.IsInventoryItem == false 
                || apptLine.Status == FSAppointmentDet.status.RequestForPO
                || apptLine.Status == FSAppointmentDet.status.CANCELED)
            {
                return;
            }

            if (apptLine.SODetID != null && apptLine.SODetID > 0)
            {
                FSSODet fsSODetRow = FSSODet.UK.Find(cache.Graph, apptLine.SODetID);

                if (CanEditSrvOrdLineValues(cache, apptLine, fsSODetRow) == true)
                {
                    return;
                }

                PXGraph graphToQuery = runningFieldVerifying == true ? cache.Graph : dummyGraph;
                decimal srvOrdAllocatedQty;
                decimal otherAppointmentsUsedQty;

                GetSrvOrdLineBalance(graphToQuery, apptLine.SODetID, apptLine.AppDetID, out srvOrdAllocatedQty, out otherAppointmentsUsedQty);

                if (otherAppointmentsUsedQty + (decimal)newValue > srvOrdAllocatedQty)
                {
                    PXSetPropertyException exception = null;

                    if (otherAppointmentsUsedQty == 0)
                    {
                        exception = new PXSetPropertyException(TX.Error.AppointmentQtyGreaterThanServiceOrderQty,
                                                PXErrorLevel.Error,
                                                ((decimal)newValue).ToString("0.00"), srvOrdAllocatedQty.ToString("0.00"));
                    }
                    else
                    {
                        exception = new PXSetPropertyException(TX.Error.AppointmentQtyWithOtherAppointmentsGreaterThanServiceOrderQty,
                                                PXErrorLevel.Error,
                                                ((decimal)newValue).ToString("0.00"), otherAppointmentsUsedQty.ToString("0.00"), srvOrdAllocatedQty.ToString("0.00"));
                    }

                    if (runningFieldVerifying == true)
                    {
                        throw exception;
                    }
                    else
                    {
                        cache.RaiseExceptionHandling(QtyField.Name, apptLine, newValue, exception);
                    }
                }
            }
        }

        public virtual void LoadServiceOrderRelatedAfterStatusChange(FSAppointment fsAppointmentRow)
        {
            LoadServiceOrderRelated(fsAppointmentRow);
            UpdateServiceOrderUnboundFields(this, ServiceOrderRelated.Current, BillingCycleRelated.Current, dummyGraph, fsAppointmentRow, DisableServiceOrderUnboundFieldCalc);
        }

        protected virtual void SetUnitCostByLotSerialNbr(PXCache cache, FSAppointmentDet fsAppointmentDetRow, string oldLotSerialNbr)
        {
            if (fsAppointmentDetRow.EnablePO == true)
            {
                return;
            }

            UnitCostHelper.UnitCostPair unitCostPair;

            if (string.IsNullOrEmpty(fsAppointmentDetRow.LotSerialNbr) == true)
            {
                unitCostPair = UnitCostHelper.CalculateCuryUnitCost<FSAppointmentDet.unitCost,
                                                                    FSAppointmentDet.inventoryID,
                                                                    FSAppointmentDet.uOM>
                                                                    (cache, fsAppointmentDetRow, true, 0m);

                cache.SetValueExt<FSAppointmentDet.unitCost>(fsAppointmentDetRow, unitCostPair.unitCost);
                cache.SetValueExt<FSAppointmentDet.curyUnitCost>(fsAppointmentDetRow, unitCostPair.curyUnitCost);
            }
            else
            {
                if (fsAppointmentDetRow.LotSerialNbr != oldLotSerialNbr
                    && ServiceOrderRelated.Current?.PostedBy != null)
                {
                    var unitCostRow = PXSelectJoin<FSSODet,
                                      InnerJoin<FSPostDet,
                                        On<FSPostDet.postDetID, Equal<FSSODet.postID>>,
                                      InnerJoin<INTran,
                                        On<INTran.sOOrderType, Equal<FSPostDet.sOOrderType>,
                                        And<INTran.sOOrderNbr, Equal<FSPostDet.sOOrderNbr>,
                                        And<INTran.sOOrderLineNbr, Equal<FSPostDet.sOLineNbr>>>>>>,
                                      Where<
                                          FSSODet.lineType, Equal<FSLineType.Inventory_Item>,
                                      And<
                                          FSSODet.srvOrdType, Equal<Required<FSSODet.srvOrdType>>,
                                      And<
                                          FSSODet.refNbr, Equal<Required<FSSODet.refNbr>>,
                                      And<
                                          FSSODet.inventoryID, Equal<Required<FSSODet.inventoryID>>,
                                      And<
                                          INTran.lotSerialNbr, Equal<Required<INTran.lotSerialNbr>>>>>>>>
                                      .Select(this, fsAppointmentDetRow?.SrvOrdType, ServiceOrderRelated.Current?.RefNbr, fsAppointmentDetRow?.InventoryID, fsAppointmentDetRow?.LotSerialNbr)
                                      .FirstOrDefault();

                    if (unitCostRow != null)
                    {
                        PXResult<FSSODet, FSPostDet, INTran> result = (PXResult<FSSODet, FSPostDet, INTran>)unitCostRow;
                        INTran inTranRow = (INTran)result;

                        unitCostPair = UnitCostHelper.CalculateCuryUnitCost<FSAppointmentDet.unitCost,
                                                                            FSAppointmentDet.inventoryID,
                                                                            FSAppointmentDet.uOM>
                                                                            (cache, fsAppointmentDetRow, false, inTranRow.UnitCost);

                        cache.SetValueExt<FSAppointmentDet.unitCost>(fsAppointmentDetRow, unitCostPair.unitCost);
                        cache.SetValueExt<FSAppointmentDet.curyUnitCost>(fsAppointmentDetRow, unitCostPair.curyUnitCost);
                    }
                }
            }
        }

        protected virtual void UpdateManualFlag(PXCache cache,
                                                PXFieldUpdatingEventArgs e,
                                                DateTime? currentDateTime,
                                                ref bool? manualFlag)
        {
            if (SkipManualTimeFlagUpdate == false)
            {
                // Turning on ManualFlag after any DateTime edition.
                // This is done in the FieldUpdating event instead of the FieldUpdated one
                // to ensure the ManualFlag update before the processing of
                // another FieldUpdated event triggered by PXFormula(typeof(Default<...>)).

                DateTime? newTime = SharedFunctions.TryParseHandlingDateTime(cache, e.NewValue);

                if (newTime != null
                        && currentDateTime != null
                        && newTime != currentDateTime)
                {
                    manualFlag = true;
                }
            }
        }

        public virtual void UpdateUnitCostsAndUnitPrices(PXGraph graph, PXCache appDetailsCache, IEnumerable<FSAppointmentDet> appDetails, int? appointmentID)
        {
            FSAppointmentDet appDetRow;
            PMTran pmTranRow;
            int? postID;    // This is needed to avoid the bug related to UpdatePendingPostFlags() method called during the RowPersisting

            if (appointmentID != null)
            {
                var postedRows = PXSelectJoin<PMTran,
                                 InnerJoin<FSPostDet,
                                 On<
                                     FSPostDet.pMDocType, Equal<PMTran.tranType>,
                                     And<FSPostDet.pMRefNbr, Equal<PMTran.refNbr>,
                                     And<FSPostDet.pMTranID, Equal<PMTran.tranID>>>>,
                                 InnerJoin<FSAppointmentDet,
                                 On<
                                     FSAppointmentDet.postID, Equal<FSPostDet.postID>>>>,
                                 Where<
                                     FSAppointmentDet.appointmentID, Equal<Required<FSAppointmentDet.appointmentID>>>>
                                 .Select(graph, appointmentID);

                foreach (PXResult<PMTran, FSPostDet, FSAppointmentDet> postedRow in postedRows)
                {
                    pmTranRow = (PMTran)postedRow;
                    appDetRow = (FSAppointmentDet)postedRow;
                    postID = appDetRow.PostID;

                    appDetRow = appDetails.Where(x => x.AppDetID == appDetRow.AppDetID).FirstOrDefault();

                    if (appDetRow != null)
                    {
                        FSAppointmentDet appDetRowCopy = (FSAppointmentDet)appDetailsCache.CreateCopy(appDetRow);
                        appDetRowCopy.CuryUnitCost = pmTranRow.TranCuryUnitRate;
                        appDetRowCopy.PostID = postID;
                        appDetailsCache.Update(appDetRowCopy);
                    }
                }
            }
        }

        public virtual string GetLineDisplayHint(PXGraph graph, string lineRefNbr, string lineDescr, int? inventoryID)
        {
            return MessageHelper.GetLineDisplayHint(graph, lineRefNbr, lineDescr, inventoryID);
        }

        public virtual void InitServiceOrderRelated(FSAppointment fsAppointmentRow)
        {
            // Inserting FSServiceOrder record
            if (fsAppointmentRow.SOID == null)
            {
                var oldServiceOrderDirty = ServiceOrderRelated.Cache.IsDirty;

                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)ServiceOrderRelated.Cache.CreateInstance();
                fsServiceOrderRow.SrvOrdType = fsAppointmentRow.SrvOrdType;
                fsServiceOrderRow.DocDesc = fsAppointmentRow.DocDesc;
                fsServiceOrderRow = ServiceOrderRelated.Insert(fsServiceOrderRow);
                fsAppointmentRow.SOID = fsServiceOrderRow.SOID;

                ServiceOrderRelated.Cache.IsDirty = oldServiceOrderDirty;
            }
            else
            {
                LoadServiceOrderRelated(fsAppointmentRow);
            }
        }

        #region Appointment Status Change methods

        public virtual void ForceUpdateCacheAndSave(PXCache cache, object row)
        {
            cache.AllowUpdate = true;
            cache.SetStatus(row, PXEntryStatus.Updated);
            this.GetSaveAction().Press();
        }

        /// <summary>
        /// Force calculate external taxes.
        /// When changing status is a good practice to calculate again the taxes. This is because line Qty can be modified.
        /// Also, new lines can be inserted on Details or Logs.
        /// </summary>
        public virtual void ForceExternalTaxCalc()
        {
            this.AppointmentRecords.Cache.SetValueExt<FSAppointment.isTaxValid>(this.AppointmentRecords.Current, false);
            RecalculateExternalTaxes();
        }
        #endregion
        #region Item Line Status Change methods
        public virtual void SetItemLineUIStatusList(PXCache cache, FSAppointmentDet row)
        {
            var valueLabelList = new List<Tuple<string, string>>();

            foreach (Tuple<string, string> t in FSAppointmentDet.status.ListAttribute.FullList)
            {
                if (row.UIStatus != t.Item1
                    && (t.Item1 == FSAppointmentDet.status.RequestForPO
                        || t.Item1 == FSAppointmentDet.status.WaitingForPO)) 
                {
                    continue;
                }

                if (row.UIStatus == t.Item1
                    || IsNewItemLineStatusValid(row, t.Item1) == true)
                {
                    valueLabelList.Add(t);
                }
            }

            PXStringListAttribute.SetList<FSAppointmentDet.uiStatus>(cache, row, valueLabelList.ToArray());
        }

        public virtual int ChangeItemLineStatus(FSAppointmentDet apptDet, string newStatus)
        {
            if (apptDet.Status == newStatus)
            {
                return 0;
            }

            if (IsItemLineStatusChangeValid(apptDet, newStatus) == false)
            {
                throw new PXException(TX.Error.InvalidStatusChangeForItemLine,
                                      apptDet.LineRef,
                                      PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, apptDet.Status),
                                      PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.status>(AppointmentDetails.Cache, apptDet, newStatus));
            }

            FSAppointmentDet copy = (FSAppointmentDet)AppointmentDetails.Cache.CreateCopy(apptDet);

            object status = newStatus;
            AppointmentDetails.Cache.RaiseFieldUpdating<FSAppointmentDet.status>(copy, ref status);

            copy.Status = (string)status;
            AppointmentDetails.Update(copy);

            return 1;
        }

        /// <summary>
        /// This method does not consider the current item line status.
        /// This performs the basic validation of the new status for the given item line.
        /// </summary>
        /// <param name="apptDet"></param>
        /// <param name="newStatus"></param>
        /// <returns></returns>
        public virtual bool IsNewItemLineStatusValid(FSAppointmentDet apptDet, string newStatus)
        {
            if (newStatus == null || AppointmentSelected.Current == null)
                return false;

            var apptRow = AppointmentSelected.Current;

            var lineIsCanceled = apptDet.Status == FSAppointmentDet.status.CANCELED;

            bool retValue = false;

            if (apptDet.ShouldBeWaitingPO == true)
            {
                if (newStatus == FSAppointmentDet.status.WaitingForPO)
                    return true;
                else if (newStatus != FSAppointmentDet.status.CANCELED && lineIsCanceled == false)
                    return false;
            }

            if (apptDet.ShouldBeRequestPO == true)
            {
                if (newStatus == FSAppointmentDet.status.RequestForPO)
                    return true;
                else if (newStatus != FSAppointmentDet.status.CANCELED && lineIsCanceled == false)
                    return false;
            }

            switch (newStatus)
            {
                #region NotStarted
                case FSAppointmentDet.status.NOT_STARTED:
                    if (apptRow != null
                        &&
                        (
                            (
                                apptRow.NotStarted == true
                                || apptRow.Hold == true
                                || apptRow.InProcess == true
                                || apptRow.Paused == true
                                || apptRow.ReopenActionRunning == true
                            )
                            ||
                            (
                                apptDet.IsTravelItem == true
                                && apptRow.Completed == true
                            )
                        )
                    )
                    {
                        retValue = true;
                    }
                    break;
                #endregion
                #region InProcess
                case FSAppointmentDet.status.IN_PROCESS:
                    if (apptRow != null
                        &&
                        (
                            (
                                apptRow.Hold == true
                                || apptRow.InProcess == true
                                || apptRow.Paused == true
                                || apptRow.StartActionRunning == true
                            )
                            ||
                            (
                                apptDet.IsTravelItem == true
                                &&
                                (
                                    apptRow.NotStarted == true
                                    || apptRow.Completed == true
                                )
                            )
                        )
                    )
                    {
                        retValue = true;
                    }
                    break;
                #endregion
                #region Completed
                case FSAppointmentDet.status.COMPLETED:
                    if (apptRow != null
                        &&
                        (
                            (
                                apptRow.Hold == true
                                || apptRow.InProcess == true
                                || apptRow.Paused == true
                                || apptRow.Completed == true
                                || apptRow.Closed == true
                            )
                            ||
                            (
                                apptDet.IsTravelItem == true
                                &&
                                (
                                    apptRow.NotStarted == true
                                    || apptRow.Completed == true
                                )
                            )
                        )
                    )
                    {
                        retValue = true;
                    }
                    break;
                #endregion
                #region NotFinished
                case FSAppointmentDet.status.NOT_FINISHED:
                    if (apptRow != null
                        &&
                        (
                            apptRow.Hold == true
                            || apptRow.InProcess == true
                            || apptRow.Paused == true
                            || apptRow.Completed == true
                            || apptRow.Closed == true
                        )
                    )
                    {
                        retValue = true;
                    }
                    break;
                #endregion
                #region NotPerformed
                case FSAppointmentDet.status.NOT_PERFORMED:
                    if (apptRow != null
                        &&
                        (
                            apptRow.Hold == true
                            || apptRow.InProcess == true
                            || apptRow.Paused == true
                            || apptRow.Completed == true
                            || apptRow.Closed == true
                            || apptRow.Canceled == true
                            || apptRow.CancelActionRunning == true
                        )
                    )
                    {
                        retValue = true;
                    }
                    break;
                #endregion
                #region Canceled
                case FSAppointmentDet.status.CANCELED:
                    if ((apptRow != null
                        && 
                        (
                            apptRow.NotStarted == true
                            || apptRow.Hold == true
                            || apptRow.InProcess == true
                            || apptRow.Paused == true
                            || apptRow.Completed == true
                            || apptRow.Closed == true
                            || apptRow.Canceled == true
                        )
                        )
                        || apptDet.ShouldBeWaitingPO == true 
                        || apptDet.ShouldBeRequestPO == true
                    )
                    {
                        retValue = true;
                    }
                    break;
                #endregion
            }

            return retValue;
        }

        /// <summary>
        /// This method considers the current item line status
        /// and it's used into the actions Start, Complete, Cancel, etc.
        /// The idea with this method is to force the normal workflow.
        /// </summary>
        /// <param name="apptDet"></param>
        /// <param name="newStatus"></param>
        /// 
        /// <returns></returns>
        public virtual bool IsItemLineStatusChangeValid(FSAppointmentDet apptDet, string newStatus)
        {
            if (IsNewItemLineStatusValid(apptDet, newStatus) == false)
            {
                return false;
            }

            switch (newStatus)
            {
                case FSAppointmentDet.status.IN_PROCESS:
                    return CanLogBeStarted(apptDet) == true;

                case FSAppointmentDet.status.COMPLETED:
                    return CanItemLineBeCompleted(apptDet) == true;

                case FSAppointmentDet.status.CANCELED:
                    return CanItemLineBeCanceled(apptDet) == true;

                case FSAppointmentDet.status.NOT_STARTED:
                case FSAppointmentDet.status.NOT_FINISHED:
                case FSAppointmentDet.status.NOT_PERFORMED:
                    return true;
            }

            return false;
        }

        public virtual bool CanItemLineBeCompleted(FSAppointmentDet row)
        {
            if (row == null)
            {
                return false;
            }

            if (IsNewItemLineStatusValid(row, FSAppointmentDet.status.COMPLETED) == false)
            {
                return false;
            }

            if (row.Status != null
                && row.Status.IsNotIn(
                                FSAppointmentDet.status.COMPLETED,
                                FSAppointmentDet.status.CANCELED,
                                FSAppointmentDet.status.NOT_FINISHED,
                                FSAppointmentDet.status.NOT_PERFORMED)
            )
            {
                return true;
            }

            return false;
        }

        public virtual bool CanItemLineBeCanceled(FSAppointmentDet row)
        {
            if (row == null)
            {
                return false;
            }

            if (IsNewItemLineStatusValid(row, FSAppointmentDet.status.CANCELED) == false)
            {
                return false;
            }

            if (row.Status != null
                && row.Status.IsNotIn(
                                FSAppointmentDet.status.COMPLETED,
                                FSAppointmentDet.status.CANCELED,
                                FSAppointmentDet.status.NOT_FINISHED,
                                FSAppointmentDet.status.IN_PROCESS)
            )
            {
                return true;
            }

            return false;
        }

        public virtual PXSetPropertyException ValidateItemLineStatus(PXCache cache, FSAppointmentDet apptDet, FSAppointment appt)
        {
            if (apptDet.IsTravelItem == true)
                return null;

            if (appt.Completed == true && appt.ReopenActionRunning == false)
            {
                if (apptDet.Status == ID.Status_AppointmentDet.NOT_STARTED
                    || apptDet.Status == ID.Status_AppointmentDet.IN_PROCESS)
                {
                    var ex = new PXSetPropertyException(TX.Error.COMPLETED_APPOINTMENT_CAN_ONLY_HAVE_COMPLETED_NON_TRAVEL_LINES);
                    cache.RaiseExceptionHandling<FSAppointmentDet.status>(apptDet, apptDet.Status, ex);

                    return ex;
                }
            }

            return null;
        }
        #endregion
        #region Log methods
        #region Log Action Validation methods
        public virtual bool CanLogBeStarted(FSAppointmentDet row)
        {
            if (row == null)
            {
                return false;
            }

            if (IsNewItemLineStatusValid(row, FSAppointmentDet.status.IN_PROCESS) == false)
            {
                return false;
            }

            if (row.Status != null
                && row.Status.IsNotIn(
                                FSAppointmentDet.status.COMPLETED,
                                FSAppointmentDet.status.CANCELED,
                                FSAppointmentDet.status.NOT_FINISHED,
                                FSAppointmentDet.status.NOT_PERFORMED)
            )
            {
                return true;
            }

            return false;
        }

        public virtual bool CanLogBePausedResumed(FSAppointmentDet row)
        {
            if (row == null)
            {
                return false;
            }

            if (row.Status == FSAppointmentDet.status.IN_PROCESS
                && row.LineType != null
                && row.LineType.IsNotIn(ID.LineType_ALL.COMMENT,
                                        ID.LineType_ALL.INSTRUCTION,
                                        ID.LineType_ALL.INVENTORY_ITEM)
            )
            {
                return true;
            }

            return false;
        }

        public virtual bool CanLogBePaused(FSAppointmentDet row)
        {
            return CanLogBePausedResumed(row);
        }

        public virtual bool CanLogBeResumed(FSAppointmentDet row)
        {
            if (CanLogBePausedResumed(row) == false)
            {
                return false;
            }

            if (IsNewItemLineStatusValid(row, FSAppointmentDet.status.IN_PROCESS) == false)
            {
                return false;
            }

            return true;
        }

        public virtual PXSetPropertyException ValidateLogStatus(PXCache cache, FSAppointmentLog log, FSAppointment appt)
        {
            if (log.Travel == true)
                return null;

            if (appt.Completed == true && appt.ReopenActionRunning == false)
            {
                if (log.Status == ID.Status_Log.IN_PROCESS)
                {
                    var ex = new PXSetPropertyException(TX.Error.COMPLETED_APPOINTMENT_CAN_ONLY_HAVE_COMPLETED_NON_TRAVEL_LINES);
                    cache.RaiseExceptionHandling<FSAppointmentLog.status>(log, log.Status, ex);

                    return ex;
                }
            }

            return null;
        }
        #endregion
        #region Log Split methods
        public virtual void SplitAppoinmentLogLinesByDays()
        {
            foreach (FSAppointmentLog row in LogRecords.Select()
                                          .RowCast<FSAppointmentLog>()
                                          .Where(_ => _.TrackTime == true
                                                   && (_.Status == ID.Status_Log.COMPLETED
                                                        || _.Status == ID.Status_Log.PAUSED)
                                                   && string.IsNullOrEmpty(_.TimeCardCD) == true))
            {
                if (row.TimeDuration < 0)
                {
                    SplitNegativeAppoinmentLogLinesByDays(LogRecords.Cache, row);
                }
                else
                {
                SplitAppointmentLogLineByDays(LogRecords.Cache, row);
            }
        }
        }

        public virtual void SplitAppointmentLogLineByDays(PXCache cache, FSAppointmentLog row)
        {
            if (row.DateTimeBegin.HasValue == false || row.DateTimeEnd.HasValue == false)
                return;

            if (row.DateTimeBegin.Value.Date == row.DateTimeEnd.Value.Date)
                return;

            DateTime startDate = row.DateTimeBegin.Value;
            DateTime originalStartDate = startDate;
            DateTime originalEndDate = row.DateTimeEnd.Value;

            FSAppointmentDet fsAppointmentDetRow = (FSAppointmentDet)PXSelectorAttribute.Select<FSAppointmentLog.detLineRef>(cache, row);
            string _origItemStatus = fsAppointmentDetRow?.Status;
            string _origLogStatus = row.Status;

            while (startDate < originalEndDate)
            {
                DateTime nextDay = startDate.AddDays(1);
                nextDay = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, 0, 0, 0);

                ////Avoids PXRestrictor exception in FSAppointmentLog.detLineRef
                if (fsAppointmentDetRow != null)
                    fsAppointmentDetRow.Status = ID.Status_AppointmentDet.IN_PROCESS;

                if (originalStartDate.Date == startDate.Date)
            {
                    if (row.DateTimeEnd != nextDay)
                {
                        FSAppointmentLog fsAppointmentLogRow = (FSAppointmentLog)cache.CreateCopy(row);
                        fsAppointmentLogRow.DateTimeEnd = nextDay;

                        if (fsAppointmentLogRow.Status == ID.Status_Log.PAUSED)
                {
                            fsAppointmentLogRow.Status = ID.Status_Log.COMPLETED;
                }

                        cache.Update(fsAppointmentLogRow);
            }
                }
                else
            {
                    FSAppointmentLog insertLogRow = (FSAppointmentLog)cache.CreateCopy(row);

                    insertLogRow.LineNbr = null;
                    insertLogRow.LineRef = null;
                    insertLogRow.LogID = null;
                    insertLogRow.NoteID = null;
                    insertLogRow.tstamp = null;
                    insertLogRow.TimeDuration = null;

                    if (_origLogStatus == ID.Status_Log.PAUSED && nextDay < originalEndDate)
                {
                        insertLogRow.Status = ID.Status_Log.COMPLETED;
                }
                else
                {
                        insertLogRow.Status = _origLogStatus;
                }

                    insertLogRow.DateTimeBegin = startDate;
                    insertLogRow.DateTimeEnd = originalEndDate.Date == startDate.Date ? originalEndDate : nextDay;

                    LogRecords.Cache.Insert(insertLogRow);
                }

                startDate = nextDay;
            }

            if (fsAppointmentDetRow != null)
                fsAppointmentDetRow.Status = _origItemStatus;
        }

        public virtual void SplitNegativeAppoinmentLogLinesByDays(PXCache cache, FSAppointmentLog row)
        {
            if (row.TimeDuration.HasValue == false || row.DateTimeBegin.HasValue == false)
            {
                return;
            }

            DateTime startDateTime = row.DateTimeBegin.Value;
            int limitByDay = (startDateTime.Hour*60 + startDateTime.Minute) - 1440;

            if (row.TimeDuration.HasValue == true && row.TimeDuration < limitByDay)
            {
                int timeDuration = row.TimeDuration.Value + (-limitByDay);

                FSAppointmentLog fsAppointmentLogRow = (FSAppointmentLog)cache.CreateCopy(row);
                fsAppointmentLogRow.TimeDuration = limitByDay;
                cache.Update(fsAppointmentLogRow);

                int numberOfDays = 1;

                while (timeDuration < 0)
                {
                    FSAppointmentLog insertLogRow = (FSAppointmentLog)cache.CreateCopy(row);

                    insertLogRow.LineNbr = null;
                    insertLogRow.LineRef = null;
                    insertLogRow.LogID = null;
                    insertLogRow.NoteID = null;
                    insertLogRow.tstamp = null;
                    insertLogRow.TimeDuration = null;

                    insertLogRow = (FSAppointmentLog)LogRecords.Cache.Insert(insertLogRow);

                    insertLogRow.DateTimeBegin = startDateTime.Date.AddDays(numberOfDays);
                    insertLogRow.TimeDuration = timeDuration < -1440 ? -1440 : timeDuration;

                    LogRecords.Cache.Update(insertLogRow);

                    timeDuration += 1440;
                    numberOfDays++;
                }
            }
        }
        #endregion

        public virtual void ClearAppointmentLog()
        {
            foreach (FSAppointmentLog fsAppointmentLogRow in LogRecords.Select().RowCast<FSAppointmentLog>().Where(_ => _.ItemType != FSAppointmentLog.itemType.Values.Travel))
            {
                LogRecords.Delete(fsAppointmentLogRow);
            }
        }

        public virtual void SetLogInfoFromDetails(PXCache cache, FSAppointmentLog fsLogRow)
        {
            FSAppointmentDet apptDet = null;

            if (string.IsNullOrWhiteSpace(fsLogRow.DetLineRef) == false)
            {
                apptDet = AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(_ => _.LineRef == fsLogRow.DetLineRef).FirstOrDefault();
            }

            if (fsLogRow.BAccountID == null)
            {
                fsLogRow.EarningType = null;
                fsLogRow.LaborItemID = null;

                if (apptDet != null
                    && (apptDet.LineType == ID.LineType_ALL.SERVICE ||
                        apptDet.LineType == ID.LineType_ALL.NONSTOCKITEM))
                {
                    fsLogRow.ProjectID = apptDet.ProjectID;
                    fsLogRow.ProjectTaskID = apptDet.ProjectTaskID;
                    fsLogRow.CostCodeID = apptDet.CostCodeID;
                }
            }

            if (apptDet != null)
            {
                fsLogRow.Descr = apptDet.TranDesc;
            }

            string logType = null;

            if (apptDet == null && fsLogRow.Travel == true)
            {
                logType = FSAppointmentLog.itemType.Values.Travel;
            }
            else
            {
                logType = GetLogTypeCheckingTravelWithLogFormula(cache, apptDet);
            }

            cache.SetValueExt<FSAppointmentLog.itemType>(fsLogRow, logType);
        }

        public virtual void SetLogActionDefaultSelection(string type, bool fromStaffTab)
        {
            IEnumerable<FSAppointmentLogExtItemLine> logRows = null;

            if (type == FSAppointmentLogExtItemLine.itemType.Values.Travel || type == FSAppointmentLogExtItemLine.itemType.Values.Service)
            {
                logRows = LogActionLogRecords.Select().RowCast<FSAppointmentLogExtItemLine>();
            }

            if (type == FSAppointmentLogExtItemLine.itemType.Values.Travel)
            {
                if (AppointmentDetails.Current?.IsTravelItem == true
                    && CanLogBeStarted(AppointmentDetails.Current) == true)
                {
                    LogActionFilter.Current.DetLineRef = AppointmentDetails.Current.LineRef;
                }
                else
                {
                    LogActionFilter.Current.DetLineRef = GetItemLineRef(this, AppointmentRecords.Current.AppointmentID, true);
                }

                foreach (FSAppointmentLogExtItemLine row in logRows)
                {
                    row.Selected = row.DetLineRef == LogActionFilter.Current.DetLineRef || LogActionFilter.Current.DetLineRef == null;
                    LogActionLogRecords.Update(row);
                }
            }
            else if (type == FSAppointmentLogExtItemLine.itemType.Values.Service)
            {
                LogActionFilter.Current.DetLineRef = AppointmentDetails.Current?.LineType == ID.LineType_ALL.SERVICE ? AppointmentDetails.Current.LineRef : null;

                foreach (FSAppointmentLogExtItemLine row in logRows)
                {
                    if (fromStaffTab == false)
                    {
                        row.Selected = row.DetLineRef == LogActionFilter.Current.DetLineRef;
                    }
                    else
                    {
                        row.Selected = row.BAccountID == AppointmentServiceEmployees.Current?.EmployeeID;
                    }

                    LogActionLogRecords.Update(row);
                }
            }
            else if (type == FSAppointmentLogExtItemLine.itemType.Values.Staff)
            {
                FSAppointmentStaffExtItemLine row = LogActionStaffRecords.Select()
                                                                  .RowCast<FSAppointmentStaffExtItemLine>()
                                                                  .Where(_ => _.LineRef == AppointmentServiceEmployees.Current.LineRef)
                                                                  .FirstOrDefault();

                if (row != null)
                {
                    foreach (FSAppointmentStaffExtItemLine selectedRow in LogActionStaffRecords.Select()
                                                                                    .RowCast<FSAppointmentStaffExtItemLine>()
                                                                                    .Where(_ => _.Selected == true))
                    {
                        selectedRow.Selected = false;
                        LogActionStaffRecords.Update(row);
                    }

                    row.Selected = row.BAccountID == AppointmentServiceEmployees.Current.EmployeeID;
                    LogActionStaffRecords.Update(row);
                }
            }
        }

        public virtual void SetLogActionPanelDefaults(string dfltAction, string dfltLogType, bool fromStaffTab = false)
        {
            ClearLogActionsViewCaches();
            LogActionFilter.Cache.Clear();
            LogActionFilter.Cache.ClearQueryCache();

            LogActionFilter.Current.Action = dfltAction;
            LogActionFilter.Current.Type = dfltLogType;

            SetLogActionDefaultSelection(dfltLogType, fromStaffTab);

            LogActionFilter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
        }

        public virtual void RunLogActionBase(string action, string logType, FSAppointmentDet apptDet, PXSelectBase<FSAppointmentLog> logSelect, params object[] logSelectArgs)
        {
            if (action == ID.LogActions.START)
            {
                bool saveDocument = false;

                if (logType == FSLogActionFilter.type.Values.Travel)
                {
                    StartTravelAction();
                    saveDocument = LogRecords.Cache.IsDirty == true || AppointmentServiceEmployees.Cache.IsDirty;
                }
                else if (logType == FSLogActionFilter.type.Values.Service)
                {
                    StartServiceAction();
                    saveDocument = LogRecords.Cache.IsDirty == true;
                }
                else if (logType == FSLogActionFilter.type.Values.NonStock)
                {
                    StartNonStockAction(apptDet);
                    saveDocument = true;
                }
                else if (logType == FSLogActionFilter.type.Values.Staff)
                {
                    StartStaffAction();
                    saveDocument = LogRecords.Cache.IsDirty == true || AppointmentServiceEmployees.Cache.IsDirty == true;
                }
                else if (logType == FSLogActionFilter.type.Values.ServBasedAssignment)
                {
                    StartServiceBasedOnAssignmentAction();
                    saveDocument = LogRecords.Cache.IsDirty == true || AppointmentServiceEmployees.Cache.IsDirty == true;
                }

                if (saveDocument == true)
                {
                    if (SkipLongOperation == true)
                    {
                        SaveWithRecalculateExternalTaxesSync();
                    }
                    else
                    {
                        Actions.PressSave();
                    }
                }
            }
            else if (action == ID.LogActions.COMPLETE || action == ID.LogActions.PAUSE || action == ID.LogActions.RESUME)
            {
                List<FSAppointmentLog> logList = null;

                if (logSelect != null)
                {
                    logList = new List<FSAppointmentLog>();

                    foreach (FSAppointmentLog logRow in logSelect.Select(logSelectArgs))
                    {
                        logList.Add(logRow);
                    }
                }
                else
                {
                    logList = LogActionLogRecords.Select()
                                                     .RowCast<FSAppointmentLogExtItemLine>()
                                                     .Where(_ => _.Selected == true)
                                                     .ToList<FSAppointmentLog>();
                }

                if (action == ID.LogActions.RESUME)
                {
                    foreach (FSAppointmentLog logRow in logList)
                    {
                        FSAppointmentLog fsAppointmentLogRow = new FSAppointmentLog()
                        {
                            ItemType = logRow.ItemType,
                            Status = ID.Status_Log.IN_PROCESS,
                            BAccountID = logRow.BAccountID,
                            DetLineRef = logRow.DetLineRef,
                            DateTimeBegin = LogActionFilter.Current?.LogDateTime
                        };

                        fsAppointmentLogRow = (FSAppointmentLog)LogRecords.Cache.Insert(fsAppointmentLogRow);
                    }
                }

                CompletePauseAction(action, LogActionFilter.Current.LogDateTime, apptDet, logList);

                if (logType == FSLogActionFilter.type.Values.Travel
                        && ServiceOrderTypeSelected.Current.OnTravelCompleteStartAppt == true
                            && (AppointmentRecords.Current?.NotStarted == true))
                {
                    startAppointment.PressImpl();
                }
            }
        }

        public virtual void RunLogAction(string action, string type, FSAppointmentDet apptDet, PXSelectBase<FSAppointmentLog> logSCanChangePOOptionselect, params object[] logSelectArgs)
        {
            if (type != FSLogActionFilter.type.Values.NonStock)
            {
                VerifyBeforeAction(action, type);
            }

            if (this.SkipLongOperation == false)
            {
                PXLongOperation.StartOperation(this,
                delegate ()
                {
                    RunLogActionBase(action, type, apptDet, logSCanChangePOOptionselect, logSelectArgs);
                });
            }
            else
            {
                RunLogActionBase(action, type, apptDet, logSCanChangePOOptionselect, logSelectArgs);
            }
        }

        public virtual void SetVisibleCompletePauseLogActionGrid(FSLogActionFilter filter)
        {
            LogActionLogRecords.Cache.AllowSelect = filter.Action == ID.LogActions.COMPLETE || filter.Action == ID.LogActions.PAUSE || filter.Action == ID.LogActions.RESUME;
        }

        public virtual void ClearLogActionsViewCaches()
        {
            LogActionLogRecords.Cache.Clear();
            LogActionLogRecords.Cache.ClearQueryCache();

            LogActionStaffRecords.Cache.Clear();
            LogActionStaffRecords.Cache.ClearQueryCache();

            LogActionStaffDistinctRecords.Cache.Clear();
            LogActionStaffDistinctRecords.Cache.ClearQueryCache();
        }

        public virtual void VerifyOnCompleteApptRequireLog(PXCache cache)
        {
            int? servicesWithoutLog = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                        .Where(_ => _.LineType == ID.LineType_ALL.SERVICE
                                                    && _.IsTravelItem == false
                                                    && _.IsCanceledNotPerformed == false
                                                    && _.LogRelatedCount == 0).Count();

            if (servicesWithoutLog > 0)
            {
                throw new PXException(TX.Error.LOG_DATE_TIMES_ARE_REQUIRED_FOR_SERVICES);
            }
        }

        public virtual void VerifyLogActionWithAppointmentStatus(string logActionID, string logActionLabel, string logType, FSAppointment appointment)
        {
            if (appointment.NotStarted == true
                || appointment.Completed == true)
            {
                if (logType != FSLogActionFilter.type.Values.Travel)
                {
                    throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.LogActionNotAllowedWithCurrentAppointmentStatus,
                                            logActionLabel,
                                            TX.Status_Appointment.NotStarted,
                                            TX.Status_Appointment.COMPLETED)
                        );
                }
            }
        }

        public virtual string GetItemLineStatusFromLog(FSAppointmentDet appointmentDet)
        {
            if (appointmentDet?.Status == ID.Status_AppointmentDet.NOT_FINISHED)
            {
                return ID.Status_AppointmentDet.NOT_FINISHED;
            }

            string itemLineStatus = ID.Status_AppointmentDet.NOT_STARTED;

            if (appointmentDet != null &&
                (appointmentDet.LineType == ID.LineType_ALL.SERVICE ||
                 appointmentDet.LineType == ID.LineType_ALL.NONSTOCKITEM))
            {
                IEnumerable<FSAppointmentLog> itemLogRecords = LogRecords.Select().RowCast<FSAppointmentLog>().Where(_ => _.DetLineRef == appointmentDet.LineRef);
                bool anyInProcessPaused = itemLogRecords.Where(_ => _.Status == ID.Status_Log.IN_PROCESS || _.Status == ID.Status_Log.PAUSED).Any();

                if (anyInProcessPaused == false)
                {
                    if (itemLogRecords.Count() > 0)
                    {
                        if (appointmentDet.Status == ID.Status_AppointmentDet.COMPLETED ||
                            appointmentDet.Status == ID.Status_AppointmentDet.NOT_FINISHED)
                        {
                            return appointmentDet.Status;
                        }
                        else
                        {
                            itemLineStatus = ID.Status_AppointmentDet.COMPLETED;
                        }
                    }
                }
                else
                {
                    itemLineStatus = ID.Status_AppointmentDet.IN_PROCESS;
                }
            }

            if (appointmentDet != null && IsNewItemLineStatusValid(appointmentDet, itemLineStatus) == false)
            {
                itemLineStatus = ID.Status_AppointmentDet.NOT_STARTED;
            }

            return itemLineStatus;
        }

        public virtual string GetLogType(FSAppointmentDet apptDet)
        {
            if (apptDet == null)
                return FSAppointmentLog.itemType.Values.Staff;

            return apptDet.IsTravelItem == true ? FSAppointmentLog.itemType.Values.Travel
                        : apptDet.LineType == ID.LineType_ALL.NONSTOCKITEM ? FSAppointmentLog.itemType.Values.NonStock
                        : FSAppointmentLog.itemType.Values.Service;
        }
        public virtual string GetLogTypeCheckingTravelWithLogFormula(PXCache logCache, FSAppointmentDet apptDet)
        {
            object dfltType = null;
            logCache.RaiseFieldDefaulting<FSAppointmentLog.itemType>(null, out dfltType);

            if (dfltType != null)
            {
                string strDfltType = (string)dfltType;
                if (strDfltType == FSAppointmentLog.itemType.Values.Travel)
                    return strDfltType;
            }

            return GetLogType(apptDet);
        }
        #endregion
        #region Travel Time methods

        public virtual void PrimaryDriver_FieldUpdated_Handler(PXCache cache, FSAppointmentEmployee fsAppointmentEmployeeRow)
        {
            PXResultset<FSAppointmentEmployee> employeeRows = AppointmentServiceEmployees.Select();
            foreach (FSAppointmentEmployee row in employeeRows.RowCast<FSAppointmentEmployee>()
                                                        .Where(_ => _.EmployeeID == fsAppointmentEmployeeRow.EmployeeID))
            {
                row.PrimaryDriver = fsAppointmentEmployeeRow.PrimaryDriver;
                if (cache.GetStatus(row) == PXEntryStatus.Notchanged)
                {
                    cache.SetStatus(row, PXEntryStatus.Updated);
                }
            }

            if (fsAppointmentEmployeeRow.PrimaryDriver == true)
            {
                foreach (FSAppointmentEmployee row in employeeRows.RowCast<FSAppointmentEmployee>()
                                                        .Where(_ => _.EmployeeID != fsAppointmentEmployeeRow.EmployeeID &&
                                                                    _.PrimaryDriver == true))
                {
                    row.PrimaryDriver = false;
                    if (cache.GetStatus(row) == PXEntryStatus.Notchanged)
                    {
                        cache.SetStatus(row, PXEntryStatus.Updated);
                    }
                }
            }

            AppointmentServiceEmployees.View.RequestRefresh();
        }

        public virtual void PrimaryDriver_RowDeleting_Handler(PXCache cache, FSAppointmentEmployee fsAppointmentEmployeeRow)
        {
            if (fsAppointmentEmployeeRow.PrimaryDriver == true)
            {
                PXResultset<FSAppointmentEmployee> fsAppointmentEmployeeRows = AppointmentServiceEmployees.Select();

                if (fsAppointmentEmployeeRows.RowCast<FSAppointmentEmployee>()
                                             .Where(_ => _.EmployeeID == fsAppointmentEmployeeRow.EmployeeID).Any() == false)
                {
                    IEnumerable<FSAppointmentEmployee> firstAppointmentEmployeeRow = fsAppointmentEmployeeRows.RowCast<FSAppointmentEmployee>()
                                                                                       .OrderBy(_ => _.LineNbr);
                    if (firstAppointmentEmployeeRow.Any() == true)
                    {
                        cache.SetValueExt<FSAppointmentEmployee.primaryDriver>(firstAppointmentEmployeeRow.First(), true);
                    }

                    AppointmentRecords.Current.PrimaryDriver = firstAppointmentEmployeeRow.FirstOrDefault()?.EmployeeID;
                }
            }
        }

        public virtual void ValidatePrimaryDriver()
        {
            PXResultset<FSAppointmentEmployee> fsAppointmentEmployeeRow = AppointmentServiceEmployees.Select();
            if (fsAppointmentEmployeeRow.Count > 0 &&
                fsAppointmentEmployeeRow.RowCast<FSAppointmentEmployee>().Where(_ => _.PrimaryDriver == true).Any() == false)
            {
                throw new PXException(TX.Messages.MISSING_PRIMARY_DRIVER);
            }
        }

        public virtual void VerifyBeforeAction(string action, string type)
        {
            int itemsSelectedCount = 0;
            bool canPerformAction = false;

            if (action == ID.LogActions.START)
            {
                if (type == FSLogActionFilter.type.Values.Travel || type == FSLogActionFilter.type.Values.Service)
                {
                    itemsSelectedCount = LogActionStaffDistinctRecords.Select().RowCast<FSAppointmentStaffDistinct>().Where(x => x.Selected == true).Count();
                    canPerformAction = LogActionFilter.Current.Me == true || itemsSelectedCount > 0;

                    if (type == FSLogActionFilter.type.Values.Service && LogActionFilter.Current.DetLineRef == null)
                    {
                        LogActionFilter.Cache.RaiseExceptionHandling<FSLogActionFilter.detLineRef>(LogActionFilter.Current,
                                                                                                   LogActionFilter.Current.DetLineRef,
                                                                                                   new PXSetPropertyException(PXMessages.LocalizeFormatNoPrefix(PX.Data.ErrorMessages.FieldIsEmpty,
                                                                                                                                         PXUIFieldAttribute.GetDisplayName<FSLogActionFilter.detLineRef>(LogActionFilter.Cache))));

                        canPerformAction = false;
                    }
                }
                else if (type == FSLogActionFilter.type.Values.Staff)
                {
                    itemsSelectedCount = LogActionStaffRecords.Select().RowCast<FSAppointmentStaffExtItemLine>().Where(x => x.Selected == true).Count();
                    bool isLogMobile = Accessinfo.ScreenID == SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.LOG_ACTION_START_STAFF_MOBILE);
                    canPerformAction = itemsSelectedCount > 0 || (isLogMobile && LogActionFilter.Current.Me == true);
                }
                else if (type == FSLogActionFilter.type.Values.ServBasedAssignment)
                {
                    itemsSelectedCount = ServicesLogAction.Select().RowCast<FSDetailFSLogAction>().Where(x => x.Selected == true).Count();
                    canPerformAction = itemsSelectedCount > 0;
                }
            }
            else if (action == ID.LogActions.COMPLETE || action == ID.LogActions.PAUSE || action == ID.LogActions.RESUME)
            {
                itemsSelectedCount = LogActionLogRecords.Select().RowCast<FSAppointmentLogExtItemLine>().Where(x => x.Selected == true).Count();
                canPerformAction = itemsSelectedCount > 0;
            }

            if (canPerformAction == false)
            {
                LogActionFilter.Cache.RaiseExceptionHandling<FSLogActionFilter.action>(LogActionFilter.Current,
                                                                                       LogActionFilter.Current.Action,
                                                                                       new PXSetPropertyException(TX.Error.CANNOT_PERFORM_LOG_ACTION_RECORD_NOT_SELECTED));

                throw new PXRowPersistingException(null, null, TX.Error.CANNOT_PERFORM_LOG_ACTION_RECORD_NOT_SELECTED);
            }
        }

        public virtual void StartTravelAction(IEnumerable<FSAppointmentStaffDistinct> createLogItems = null)
        {
            IEnumerable<FSAppointmentStaffDistinct> createLogItemsLocal = null;
            FSAppointmentLog fsAppointmentLogRow;
            string detLineRef = null;
            DateTime? dateTimeBegin = null;

            if (createLogItems == null)
            {
                detLineRef = LogActionFilter.Current?.DetLineRef;
                dateTimeBegin = LogActionFilter.Current?.LogDateTime;

                if (LogActionFilter.Current.Me == true)
                {
                    EPEmployee employeeByUserID = PXSelect<EPEmployee,
                                                  Where<
                                                      EPEmployee.userID, Equal<Current<AccessInfo.userID>>>>
                                                  .Select(this);

                    if (employeeByUserID != null)
                    {
                        bool isEmployeeInGrid = AppointmentServiceEmployees.Select().RowCast<FSAppointmentEmployee>()
                                                                           .Where(x => x.EmployeeID == employeeByUserID.BAccountID)
                                                                           .Count() > 0;

                        bool isTherePrimaryDriver = AppointmentServiceEmployees.Select().RowCast<FSAppointmentEmployee>()
                                                                               .Where(x => x.PrimaryDriver == true)
                                                                               .Count() > 0;

                        if (isEmployeeInGrid == false)
                        {
                            FSAppointmentEmployee fsAppointmentEmployeeRow = new FSAppointmentEmployee()
                            {
                                EmployeeID = employeeByUserID.BAccountID,
                            };

                            if (isTherePrimaryDriver == false)
                            {
                                fsAppointmentEmployeeRow.PrimaryDriver = true;
                            }

                            AppointmentServiceEmployees.Cache.Insert(fsAppointmentEmployeeRow);
                        }


                        fsAppointmentLogRow = new FSAppointmentLog()
                        {
                            BAccountID = employeeByUserID.BAccountID,
                            ItemType = FSAppointmentLog.itemType.Values.Travel,
                            DetLineRef = detLineRef,
                            DateTimeBegin = dateTimeBegin
                        };

                        LogRecords.Cache.Insert(fsAppointmentLogRow);
                    }
                }
                else
                {
                    createLogItemsLocal = LogActionStaffDistinctRecords.Select().RowCast<FSAppointmentStaffDistinct>()
                                                                   .Where(x => x.Selected == true);
                }
            }
            else
            {
                detLineRef = null;
                dateTimeBegin = PXDBDateAndTimeAttribute.CombineDateTime(AppointmentRecords.Current.ExecutionDate, PXTimeZoneInfo.Now);
                createLogItemsLocal = createLogItems;
            }

            if (createLogItemsLocal != null)
            {
                foreach (FSAppointmentStaffDistinct row in createLogItemsLocal)
                {
                    fsAppointmentLogRow = new FSAppointmentLog()
                    {
                        BAccountID = row.BAccountID,
                        ItemType = FSAppointmentLog.itemType.Values.Travel,
                        DetLineRef = detLineRef,
                        DateTimeBegin = dateTimeBegin
                    };

                    LogRecords.Cache.Insert(fsAppointmentLogRow);
                }
            }
        }

        public virtual void StartServiceAction(IEnumerable<FSAppointmentStaffDistinct> createLogItems = null)
        {
            IEnumerable<FSAppointmentStaffDistinct> createLogItemsLocal = null;
            FSAppointmentLog fsAppointmentLogRow;
            string detLineRef = null;
            DateTime? dateTimeBegin = null;

            if (LogActionFilter.Current?.DetLineRef == null)
                return;

            if (createLogItems == null)
            {
                detLineRef = LogActionFilter.Current?.DetLineRef;
                dateTimeBegin = LogActionFilter.Current?.LogDateTime;

                if (LogActionFilter.Current.Me == true)
                {
                    EPEmployee employeeByUserID = PXSelect<EPEmployee,
                                                  Where<
                                                      EPEmployee.userID, Equal<Current<AccessInfo.userID>>>>
                                                  .Select(this);

                    if (employeeByUserID != null)
                    {
                        bool isEmployeeInGrid = AppointmentServiceEmployees.Select().RowCast<FSAppointmentEmployee>()
                                                                           .Where(x => x.EmployeeID == employeeByUserID.BAccountID)
                                                                           .Count() > 0;

                        bool isTherePrimaryDriver = AppointmentServiceEmployees.Select().RowCast<FSAppointmentEmployee>()
                                                                               .Where(x => x.PrimaryDriver == true)
                                                                               .Count() > 0;

                        if (isEmployeeInGrid == false)
                        {
                            FSAppointmentEmployee fsAppointmentEmployeeRow = new FSAppointmentEmployee()
                            {
                                EmployeeID = employeeByUserID.BAccountID,
                            };

                            if (isTherePrimaryDriver == false)
                            {
                                fsAppointmentEmployeeRow.PrimaryDriver = true;
                            }

                            AppointmentServiceEmployees.Cache.Insert(fsAppointmentEmployeeRow);
                        }

                        fsAppointmentLogRow = new FSAppointmentLog()
                        {
                            ItemType = FSAppointmentLog.itemType.Values.Service,
                            BAccountID = employeeByUserID.BAccountID,
                            DetLineRef = detLineRef,
                            DateTimeBegin = dateTimeBegin
                        };

                        fsAppointmentLogRow = (FSAppointmentLog)LogRecords.Cache.Insert(fsAppointmentLogRow);
                    }
                }
                else
                {
                    createLogItemsLocal = LogActionStaffDistinctRecords.Select().RowCast<FSAppointmentStaffDistinct>()
                                                                   .Where(x => x.Selected == true);
                }
            }
            else
            {
                detLineRef = null;
                dateTimeBegin = PXDBDateAndTimeAttribute.CombineDateTime(AppointmentRecords.Current.ExecutionDate, PXTimeZoneInfo.Now);
                createLogItemsLocal = createLogItems;
            }

            if (createLogItemsLocal != null)
            {
                foreach (FSAppointmentStaffDistinct row in createLogItemsLocal)
                {
                    fsAppointmentLogRow = new FSAppointmentLog()
                    {
                        ItemType = FSAppointmentLog.itemType.Values.Service,
                        BAccountID = row.BAccountID,
                        DetLineRef = detLineRef,
                        DateTimeBegin = dateTimeBegin
                    };

                    LogRecords.Cache.Insert(fsAppointmentLogRow);
                }
            }
        }

        public virtual void StartNonStockAction(FSAppointmentDet fsAppointmentDet)
        {
            if (fsAppointmentDet == null)
                return;

            FSAppointmentLog fsAppointmentLogRow = new FSAppointmentLog()
            {
                ItemType = FSAppointmentLog.itemType.Values.NonStock,
                BAccountID = null,
                DetLineRef = fsAppointmentDet.LineRef,
                DateTimeBegin = PXDBDateAndTimeAttribute.CombineDateTime(AppointmentRecords.Current.ExecutionDate, PXTimeZoneInfo.Now),
                TimeDuration = fsAppointmentDet.EstimatedDuration ?? 0,
                Status = ID.Status_Log.IN_PROCESS,
                TrackTime = false,
                Descr = fsAppointmentDet.TranDesc,
                TrackOnService = true,
                ProjectID = fsAppointmentDet.ProjectID,
                ProjectTaskID = fsAppointmentDet.ProjectTaskID,
                CostCodeID = fsAppointmentDet.CostCodeID
            };

            LogRecords.Cache.Insert(fsAppointmentLogRow);
        }

        public virtual void CompletePauseAction(string logAction, DateTime? dateTimeEnd, FSAppointmentDet apptDet, List<FSAppointmentLog> logList)
        {
            string logStatus = logAction == ID.LogActions.PAUSE ? ID.Status_Log.PAUSED : ID.Status_Log.COMPLETED;
            bool isPauseOrResumeAction = logAction == ID.LogActions.PAUSE || logAction == ID.LogActions.RESUME;
            apptDet = isPauseOrResumeAction ? null : apptDet;

            int rowsAffected = CompletePauseMultipleLogs(dateTimeEnd, ID.Status_AppointmentDet.COMPLETED, logStatus, apptDet != null ? false : true, logList);

            if (apptDet != null)
            {
                rowsAffected += ChangeItemLineStatus(apptDet, ID.Status_AppointmentDet.COMPLETED);
            }

            if (rowsAffected > 0)
            {
                if (SkipLongOperation == true)
                {
                    SaveWithRecalculateExternalTaxesSync();
                }
                else
                {
                    Actions.PressSave();
                }
            }
        }

        public virtual int CompletePauseMultipleLogs(DateTime? dateTimeEnd, string newAppDetStatus, string logStatus, bool completeRelatedItemLines, List<FSAppointmentLog> logList)
        {
            if (dateTimeEnd == null)
            {
                dateTimeEnd = PXDBDateAndTimeAttribute.CombineDateTime(PXTimeZoneInfo.Now, PXTimeZoneInfo.Now);
            }

            int rowsAffected = 0;
            List<FSAppointmentDet> apptDetRows = null;

            if (completeRelatedItemLines == true)
            {
                apptDetRows = AppointmentDetails.Select().RowCast<FSAppointmentDet>().ToList();
            }

            if (logList != null && logList.Count > 0)
            {
                IEnumerable<FSAppointmentLog> viewRows = LogRecords.Select().RowCast<FSAppointmentLog>();

                foreach (FSAppointmentLog listRow in logList)
                {
                    FSAppointmentLog viewRow = viewRows.Where(_ => _.LineRef == listRow.LineRef).FirstOrDefault();

                    ChangeLogAndRelatedItemLinesStatus(viewRow, logStatus, dateTimeEnd.Value, newAppDetStatus, apptDetRows);
                    rowsAffected++;
                }
            }

            return rowsAffected;
        }

        public virtual void ResumeMultipleLogs(PXSelectBase<FSAppointmentLog> logSelect, params object[] logSelectArgs)
        {
            LogActionFilter.Current.LogDateTime = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
            RunLogActionBase(ID.LogActions.RESUME, null, null, logSelect, logSelectArgs);
        }

        public virtual FSAppointmentLog ChangeLogAndRelatedItemLinesStatus(FSAppointmentLog logRow, string newLogStatus, DateTime newDateTimeEnd, string newApptDetStatus, List<FSAppointmentDet> apptDetRows)
        {
            if (logRow == null)
                return null;

            bool keepPausedDateTime = logRow.Status == ID.Status_Log.PAUSED
                                                && newLogStatus == ID.Status_Log.COMPLETED;

            logRow = PXCache<FSAppointmentLog>.CreateCopy(logRow);
            logRow.Status = newLogStatus;

            if (logRow.KeepDateTimes == false && keepPausedDateTime == false)
            {
                logRow.DateTimeEnd = newDateTimeEnd;
            }

            if (apptDetRows != null && string.IsNullOrWhiteSpace(logRow.DetLineRef) == false)
            {
                FSAppointmentDet apptDet = apptDetRows.Where(r => r.LineRef == logRow.DetLineRef).FirstOrDefault();

                if (apptDet != null)
                {
                    ChangeItemLineStatus(apptDet, newApptDetStatus);
                }
            }

            return (FSAppointmentLog)LogRecords.Cache.Update(logRow);
        }

        public virtual void StartStaffAction(IEnumerable<FSAppointmentStaffExtItemLine> createLogItems = null, DateTime? dateTimeBegin = null)
        {
            IEnumerable<FSAppointmentStaffExtItemLine> createLogItemsLocal = null;

            if (createLogItems == null)
            {
                if (LogActionFilter.Current.Me == true)
                {
                    EPEmployee employeeByUserID = PXSelect<EPEmployee,
                                                  Where<
                                                      EPEmployee.userID, Equal<Current<AccessInfo.userID>>>>
                                                  .Select(this);

                    if (employeeByUserID != null)
                    {
                        bool isEmployeeInGrid = AppointmentServiceEmployees.Select().RowCast<FSAppointmentEmployee>()
                                                                           .Where(x => x.EmployeeID == employeeByUserID.BAccountID)
                                                                           .Count() > 0;

                        bool isTherePrimaryDriver = AppointmentServiceEmployees.Select().RowCast<FSAppointmentEmployee>()
                                                                               .Where(x => x.PrimaryDriver == true)
                                                                               .Count() > 0;

                        if (isEmployeeInGrid == false)
                        {
                            FSAppointmentEmployee fsAppointmentEmployeeRow = new FSAppointmentEmployee()
                            {
                                EmployeeID = employeeByUserID.BAccountID
                            };

                            if (isTherePrimaryDriver == false)
                            {
                                fsAppointmentEmployeeRow.PrimaryDriver = true;
                            }

                            AppointmentServiceEmployees.Cache.Insert(fsAppointmentEmployeeRow);

                            FSAppointmentLog fsAppointmentLogRow = new FSAppointmentLog()
                            {
                                ItemType = FSAppointmentLog.itemType.Values.Staff,
                                BAccountID = employeeByUserID.BAccountID,
                                DetLineRef = null,
                                DateTimeBegin = dateTimeBegin ?? LogActionFilter.Current.LogDateTime
                            };

                            LogRecords.Cache.Insert(fsAppointmentLogRow);
                        }
                        else
                        {
                            createLogItemsLocal = LogActionStaffRecords.Select().RowCast<FSAppointmentStaffExtItemLine>()
                                                                       .Where(x => x.Selected == true);
                        }
                    }
                }
                else
                {
                    createLogItemsLocal = LogActionStaffRecords.Select().RowCast<FSAppointmentStaffExtItemLine>()
                                                               .Where(x => x.Selected == true);
                }
            }
            else
            {
                createLogItemsLocal = createLogItems;
            }

            if (createLogItemsLocal != null)
            {
                foreach (FSAppointmentStaffExtItemLine row in createLogItemsLocal)
                {
                    int? timeDuration = row != null && row.EstimatedDuration != null ? row.EstimatedDuration : 0;

                    FSAppointmentLog fsAppointmentLogRow = new FSAppointmentLog()
                    {
                        ItemType = FSAppointmentLog.itemType.Values.Staff,
                        BAccountID = row.BAccountID,
                        DetLineRef = row.DetLineRef,
                        DateTimeBegin = dateTimeBegin ?? LogActionFilter.Current.LogDateTime,
                    };

                    LogRecords.Cache.Insert(fsAppointmentLogRow);
                }
            }
        }

        public virtual void StartServiceBasedOnAssignmentAction(IEnumerable<FSDetailFSLogAction> createLogItems = null, DateTime? dateTimeBegin = null)
        {
            IEnumerable<FSDetailFSLogAction> createLogItemsLocal = null;
            FSAppointmentLog fsAppointmentLogRow;

            if (createLogItems == null)
            {
                createLogItemsLocal = ServicesLogAction.Select().RowCast<FSDetailFSLogAction>().Where(x => x.Selected == true);
            }
            else
            {
                createLogItemsLocal = createLogItems;
            }

            if (createLogItemsLocal != null)
            {
                foreach (FSDetailFSLogAction fsDetailLogActionRow in createLogItemsLocal)
                {
                    var employeesRelatedToService = AppointmentServiceEmployees.Select().RowCast<FSAppointmentEmployee>()
                                                                               .Where(x => x.ServiceLineRef == fsDetailLogActionRow.LineRef);

                    if (employeesRelatedToService.Count() > 0)
                    {
                        foreach (FSAppointmentEmployee employeeRow in employeesRelatedToService)
                        {
                            fsAppointmentLogRow = new FSAppointmentLog()
                            {
                                ItemType = FSAppointmentLog.itemType.Values.Staff,
                                BAccountID = employeeRow.EmployeeID,
                                DetLineRef = employeeRow.ServiceLineRef,
                                DateTimeBegin = dateTimeBegin ?? LogActionFilter.Current.LogDateTime,
                            };

                            LogRecords.Cache.Insert(fsAppointmentLogRow);
                        }
                    }
                    else
                    {
                        fsAppointmentLogRow = new FSAppointmentLog()
                        {
                            ItemType = FSAppointmentLog.itemType.Values.Service,
                            BAccountID = null,
                            DetLineRef = fsDetailLogActionRow.LineRef,
                            DateTimeBegin = dateTimeBegin ?? LogActionFilter.Current.LogDateTime,
                        };

                        LogRecords.Cache.Insert(fsAppointmentLogRow);
                    }
                }
            }
        }
        #endregion
        #region Purchase Order PO methods

        public virtual void POCreateVerifyValue(PXCache sender, FSAppointmentDet row, bool? value)
        {
            ServiceOrderEntry.POCreateVerifyValueInt<FSAppointmentDet.enablePO>(sender, row, row.InventoryID, value);
        }

        #endregion
        #region AppointmentHeaderFunctions
        public virtual void SetHeaderActualDateTimeBegin(PXCache cache, FSAppointment fsAppointmentRow, DateTime? dateTimeBegin)
        {
            if (fsAppointmentRow != null && fsAppointmentRow.HandleManuallyActualTime == false)
            {
                cache.SetValueExtIfDifferent<FSAppointment.executionDate>(fsAppointmentRow, dateTimeBegin.Value.Date);
                cache.SetValueExtIfDifferent<FSAppointment.actualDateTimeBegin>(fsAppointmentRow, dateTimeBegin);
            }
        }

        public virtual void SetHeaderActualDateTimeEnd(PXCache cache, FSAppointment fsAppointmentRow, DateTime? dateTimeEnd)
        {
            if (fsAppointmentRow != null && fsAppointmentRow.HandleManuallyActualTime == false)
            {
                cache.SetValueExtIfDifferent<FSAppointment.actualDateTimeEnd>(fsAppointmentRow, dateTimeEnd);
            }
        }

        public virtual bool ActualDateAndTimeValidation(FSAppointment fsAppointmentRow)
        {
            return fsAppointmentRow.ActualDateTimeBegin != null && fsAppointmentRow.ActualDateTimeEnd != null;
        }
        #endregion
        #region AppointmentDet methods
        public virtual string GetValidAppDetStatus(FSAppointmentDet row, string newStatus)
        {
            if (newStatus != ListField_Status_AppointmentDet.CANCELED
                && newStatus != ListField_Status_AppointmentDet.WaitingForPO
                && newStatus != ListField_Status_AppointmentDet.RequestForPO)
            {
                if (row.ShouldBeWaitingPO == true)
                {
                    return ListField_Status_AppointmentDet.WaitingForPO;
                }
                else if (row.ShouldBeRequestPO == true)
                {
                    return ListField_Status_AppointmentDet.RequestForPO;
                }
            }

            return newStatus;
        }

        public virtual void ForceAppointmentDetActualFieldsUpdate(bool reopeningAppointment)
        {
            foreach (FSAppointmentDet row in AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(r => r.IsLinkedItem == false))
            {
                if (reopeningAppointment == true
                    && row.Status != FSAppointmentDet.status.NOT_STARTED
                    && row.Status != FSAppointmentDet.status.WaitingForPO
                    && row.Status != FSAppointmentDet.status.RequestForPO)
                {
                    ChangeItemLineStatus(row, FSAppointmentDet.status.NOT_STARTED);
                }

                FSAppointmentDet copy = (FSAppointmentDet)AppointmentDetails.Cache.CreateCopy(row);

                AppointmentDetails.Cache.SetDefaultExt<FSAppointmentDet.areActualFieldsActive>(copy);

                if (AppointmentDetails.Cache.ObjectsEqual<FSAppointmentDet.curyEstimatedTranAmt,
                        FSAppointmentDet.actualDuration, FSAppointmentDet.actualQty, FSAppointmentDet.curyTranAmt,
                        FSAppointmentDet.curyExtPrice>(row, copy) == false)
                {
                    AppointmentDetails.Update(copy);
                }
            }
        }

        public virtual void OnApptStartTimeChangeUpdateLogStartTime(FSAppointment fsAppointmentRow, FSSrvOrdType fsSrvOrdTypeRow,
                                                                        AppointmentLog_View logRecords)
        {
            if (fsAppointmentRow != null && fsSrvOrdTypeRow?.OnStartTimeChangeUpdateLogStartTime == true)
            {
                foreach (FSAppointmentLog fsAppointmentLogRow in logRecords.Select().RowCast<FSAppointmentLog>()
                                                                            .Where(_ => _.ItemType != FSAppointmentLog.itemType.Values.Travel
                                                                                     && _.DateTimeBegin.HasValue == true)
                                                                            .GroupBy(_ => new { _.BAccountID, _.DetLineRef })
                                                                            .Select(_ => _.OrderBy(d => d.DateTimeBegin).First()))
                {
                    if (fsAppointmentLogRow.DateTimeBegin == fsAppointmentRow.ActualDateTimeBegin)
                        continue;

                    if (fsAppointmentLogRow.KeepDateTimes == true)
                        continue;

                    FSAppointmentLog copy = (FSAppointmentLog)logRecords.Cache.CreateCopy(fsAppointmentLogRow);
                    copy.DateTimeBegin = fsAppointmentRow.ActualDateTimeBegin;

                    if (copy.DateTimeEnd < copy.DateTimeBegin)
                        copy.DateTimeEnd = copy.DateTimeBegin;

                    logRecords.Cache.Update(copy);
                }
            }
        }

        public virtual void OnApptEndTimeChangeUpdateLogEndTime(FSAppointment fsAppointmentRow, FSSrvOrdType fsSrvOrdTypeRow,
                                                                        AppointmentLog_View logRecords)
        {
            if (fsAppointmentRow != null && fsSrvOrdTypeRow?.OnEndTimeChangeUpdateLogEndTime == true)
            {
                foreach (FSAppointmentLog fsAppointmentLogRow in logRecords.Select().RowCast<FSAppointmentLog>()
                                                                            .Where(_ => _.ItemType != FSAppointmentLog.itemType.Values.Travel
                                                                                     && _.DateTimeEnd.HasValue == true)
                                                                            .GroupBy(_ => new { _.BAccountID, _.DetLineRef })
                                                                            .Select(_ => _.OrderByDescending(d => d.DateTimeEnd).First()))
                {
                    if (fsAppointmentLogRow.DateTimeEnd == fsAppointmentRow.ActualDateTimeEnd)
                        continue;

                    if (fsAppointmentLogRow.KeepDateTimes == true)
                        continue;

                    FSAppointmentLog copy = (FSAppointmentLog)logRecords.Cache.CreateCopy(fsAppointmentLogRow);
                    copy.DateTimeEnd = fsAppointmentRow.ActualDateTimeEnd;

                    if (copy.DateTimeBegin > copy.DateTimeEnd)
                        copy.DateTimeBegin = copy.DateTimeEnd;

                    logRecords.Cache.Update(copy);
                }
            }
        }
        #endregion

        protected void DeleteUnpersistedServiceOrderRelated(FSAppointment fsAppointmentRow)
        {
            // Deleting unpersisted FSServiceOrder record
            if (fsAppointmentRow.SOID < 0)
            {
                FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.SelectSingle();
                ServiceOrderRelated.Delete(fsServiceOrderRow);
                fsAppointmentRow.SOID = null;
            }
        }

        protected virtual void LoadServiceOrderRelated(FSAppointment fsAppointmentRow)
        {
            if (fsAppointmentRow.ReloadServiceOrderRelated == true)
            {
                ServiceOrderRelated.Cache.ClearQueryCache();
                ServiceOrderRelated.Cache.Clear();
                ServiceOrderRelated.Current = null;
                fsAppointmentRow.ReloadServiceOrderRelated = false;
            }

            if (fsAppointmentRow.SrvOrdType != null && fsAppointmentRow.SOID != null &&
                (ServiceOrderRelated.Current == null
                    || (ServiceOrderRelated.Current.SOID != fsAppointmentRow.SOID
                            && fsAppointmentRow.SOID > 0)
                )
               )
            {
                ServiceOrderRelated.Current = ServiceOrderRelated.SelectSingle(fsAppointmentRow.SOID);
                fsAppointmentRow.CustomerID = ServiceOrderRelated.Current?.CustomerID;
                fsAppointmentRow.BillCustomerID = ServiceOrderRelated.Current?.BillCustomerID;
                fsAppointmentRow.BranchID = ServiceOrderRelated.Current?.BranchID;
                fsAppointmentRow.CuryID = ServiceOrderRelated.Current?.CuryID;
            }
        }

        protected virtual void VerifyIsAlreadyPosted<Field>(PXCache cache, FSAppointmentDet fsAppointmentDetRow, FSBillingCycle billingCycleRow)
            where Field : class, IBqlField
        {
            if (fsAppointmentDetRow == null || ServiceOrderRelated.Current == null || billingCycleRow == null)
            {
                return;
            }

            IFSSODetBase row = null;
            int? pivot = -1;

            if (fsAppointmentDetRow.IsInventoryItem)
            {
                row = fsAppointmentDetRow;
                pivot = fsAppointmentDetRow.SODetID;
            }
            else if (fsAppointmentDetRow.IsPickupDelivery)
            {
                row = fsAppointmentDetRow;
                pivot = fsAppointmentDetRow.AppDetID > 0 ? fsAppointmentDetRow.AppDetID : null;
            }

            PXEntryStatus status = ServiceOrderRelated.Cache.GetStatus(ServiceOrderRelated.Current);
            bool needsVerify = status == PXEntryStatus.Updated || status == PXEntryStatus.Notchanged;
            bool isSOAlreadyPosted = ServiceOrderPostedIn.SelectWindowed(0, 1).Count > 0;

            var relatedServiceOrder = ServiceOrderRelated.Current;

            if (needsVerify == true
                    && pivot == null
                        && IsInstructionOrComment(row) == false
                            && relatedServiceOrder?.BillingBy == ID.Billing_By.SERVICE_ORDER
                                && isSOAlreadyPosted == true)
            {
                cache.RaiseExceptionHandling<Field>(row,
                                                    row.InventoryID,
                                                    new PXSetPropertyException(PXMessages.LocalizeFormat(TX.Error.CANNOT_ADD_INVENTORY_TYPE_LINES_BECAUSE_SO_POSTED, GetLineType(row.LineType, true)),
                                                                               PXErrorLevel.RowError));
            }
        }

        protected virtual bool IsInstructionOrComment(object eRow)
        {
            if (eRow == null)
            {
                return false;
            }

            var row = (IFSSODetBase)eRow;

            return row.LineType == ID.LineType_ALL.COMMENT
                || row.LineType == ID.LineType_ALL.INSTRUCTION;
        }

        // TODO:
        [Obsolete("This method will be deleted in the next major release. Use the version with the parm justWarn.")]
        public virtual void ValidateCustomerBillingCycle(PXCache cache, Object row, int? customerID, FSSrvOrdType fsSrvOrdTypeRow, FSSetup setupRecordRow)
        {
            ValidateCustomerBillingCycle(cache, (FSServiceOrder)row, AppointmentRecords.Cache, AppointmentRecords.Current, customerID, fsSrvOrdTypeRow, setupRecordRow, justWarn: false);
        }

        public virtual bool ValidateCustomerBillingCycle(PXCache serviceOrderCache, FSServiceOrder serviceOrder, PXCache appointmentCache, FSAppointment appointment, int? billCustomerID, FSSrvOrdType fsSrvOrdTypeRow, FSSetup setupRecordRow, bool justWarn)
        {
            return ValidateCustomerBillingCycle<FSServiceOrder.billCustomerID>(serviceOrderCache, serviceOrder, billCustomerID, fsSrvOrdTypeRow, setupRecordRow, justWarn);
        }

        public virtual void ClearAPBillReferences(FSAppointment fsAppointmentRow)
        {
            ServiceOrderEntry soGraph = null;

            foreach (FSAppointmentDet row in AppointmentDetails.Cache.Deleted)
            {
                if (soGraph == null)
                {
                    soGraph = GetServiceOrderEntryGraph(true);

                    if (soGraph.RunningPersist == false)
                    {
                        soGraph.ServiceOrderRecords.Current = soGraph.ServiceOrderRecords
                            .Search<FSServiceOrder.refNbr>(fsAppointmentRow.SORefNbr, fsAppointmentRow.SrvOrdType);
                    }
                }

                if (row.IsAPBillItem == true)
                {
                    FSSODet soDetRow = PXSelect<FSSODet,
                        Where<FSSODet.sODetID, Equal<Required<FSSODet.sODetID>>>>
                        .Select(soGraph, row.SODetID);

                    soGraph.ServiceOrderDetails.Cache.Delete(soDetRow);
                }
            }

            if (soGraph != null && soGraph.IsDirty == true && soGraph.RunningPersist == false)
            {
                soGraph.Actions.PressSave();
            }
        }

        public virtual void SaveWithRecalculateExternalTaxesSync() 
        {
            try
            {
                RecalculateExternalTaxesSync = true;
                Save.Press();
            }
            finally
            {
                RecalculateExternalTaxesSync = false;
            }
        }

        public virtual bool CanChangePOOptions(FSAppointmentDet apptLine, ref FSSODet soLine, string fieldName, out PXException exception)
        {
            return CanChangePOOptions(apptLine, false, ref soLine, fieldName, out exception);
        }

        public virtual bool CanChangePOOptions(FSAppointmentDet apptLine, bool runningRowSelecting, ref FSSODet soLine, string fieldName, out PXException exception)
        {
            exception = null;

            if (apptLine == null)
                return false;

            if (apptLine.SODetID == null || apptLine.SODetID < 0)
                return true;

            if (soLine == null)
            {
                soLine = FSSODet.UK.Find(this, apptLine.SODetID);
                if (soLine == null)
                {
                    return false;
                }
            }

            // TODO: Add bound Posted flag to FSSODet and add verification Posted == false
            if (soLine.ApptCntr > 1 || soLine.IsPrepaid == true)
            {
                return false;
            }

            if (soLine.POType != null || soLine.PONbr != null)
            {
                if (fieldName == typeof(FSAppointmentDet.enablePO).Name)
                {
                    exception = new PXSetPropertyException(TX.Error.CannotUnselectMarkForPOBecausePOIsAlreadyCreated, PXErrorLevel.Error);
                }

                if (fieldName == typeof(FSAppointmentDet.pOSource).Name)
                {
                    exception = new PXSetPropertyException(TX.Error.CannotChangePOSourceBecausePOIsAlreadyCreated, PXErrorLevel.Error);
                }

                return false;
            }

            decimal apptCntrIncludingRequestPOLine = (decimal)soLine.ApptCntr;
            if (apptLine.AppDetID > 0 && apptLine.SODetCreate == true)
            {
                string originalStatus = null;

                if (runningRowSelecting == true)
                {
                    originalStatus = apptLine.Status;
                }
                else
                {
                    originalStatus = (string)this.Caches[typeof(FSAppointmentDet)].GetValueOriginal<FSAppointmentDet.status>(apptLine);
                }

                if (originalStatus == FSAppointmentDet.status.RequestForPO)
                {
                    apptCntrIncludingRequestPOLine++;
                }
            }

            if (
                (apptLine.AppDetID > 0 && apptCntrIncludingRequestPOLine > 1)
                || (apptLine.AppDetID < 0 && apptCntrIncludingRequestPOLine > 0)
            )
            {
                if (fieldName == typeof(FSAppointmentDet.enablePO).Name)
                {
                    exception = new PXSetPropertyException(TX.Error.CannotChangeMarkForPOInApptLine, PXErrorLevel.Error);
                }

                if (fieldName == typeof(FSAppointmentDet.pOSource).Name)
                {
                    exception = new PXSetPropertyException(TX.Error.CannotChangePOSourcePOInApptLine, PXErrorLevel.Error);
                }

                return false;
            }

            // This is to avoid changing MarkForPo on the Service Order, because if the appointment does not create the line
            // Qty may not be the same on the Service Order and you will cancel items that the appointment does not need.
            if (apptLine.SODetCreate == false)
            {
                if (fieldName == typeof(FSAppointmentDet.enablePO).Name)
                {
                    exception = new PXSetPropertyException(TX.Error.CannotUnselectMarkForPOBecauseItWasRequestedFromServiceOrder, PXErrorLevel.Error);
                }

                if (fieldName == typeof(FSAppointmentDet.pOSource).Name)
                {
                    exception = new PXSetPropertyException(TX.Error.CannotChangePOSourceBecauseItWasRequestedFromServiceOrder, PXErrorLevel.Error);
                }

                return false;
            }

            return true;
        }

        public virtual DateTime? GetDateTimeEnd(DateTime? dateTimeBegin, int hour = 0, int minute = 0, int second = 0, int milisecond = 0)
        {
            return AppointmentEntry.GetDateTimeEndInt(dateTimeBegin, hour, minute, second, milisecond);
        }
        public virtual FSSODet GetSODetFromAppointmentDet(PXGraph graph, FSAppointmentDet fsAppointmentDetRow)
        {
            return GetSODetFromAppointmentDetInt(graph, fsAppointmentDetRow);
        }

        /// <summary>
        /// Evaluates whether the Employee's slot can contain the Appointment's duration.
        /// </summary>
        /// <param name="slotBegin">DateTime of Start of the Employee Schedule.</param>
        /// <param name="slotEnd">DateTime of End of the Employee Schedule.</param>
        /// <param name="beginTime">Begin DateTime of the possible overlap Slot.</param>
        /// <param name="endTime">End DateTime of the possible overlap Slot.</param>
        /// <returns><c>Enum</c> indicating if the appointment is contained, partially contained or not contained in the Employee's work slot.</returns>
        public virtual SlotIsContained SlotIsContainedInSlot(DateTime? slotBegin, DateTime? slotEnd, DateTime? beginTime, DateTime? endTime)
        {
            return SlotIsContainedInSlotInt(slotBegin, slotEnd, slotEnd, beginTime);
        }

        public virtual string GetItemLineRef(PXGraph graph, int? appointmentID, bool isTravel = false)
        {
            var appoinmentDetRow = (FSAppointmentDet)
                                   PXSelect<FSAppointmentDet,
                                   Where<
                                       FSAppointmentDet.isTravelItem, Equal<Required<FSAppointmentDet.isTravelItem>>,
                                   And<
                                       FSAppointmentDet.appointmentID, Equal<Required<FSAppointment.appointmentID>>,
                                       And<
                                           Where<FSAppointmentDet.status, Equal<FSAppointmentDet.status.NotStarted>,
                                           Or<FSAppointmentDet.status, Equal<FSAppointmentDet.status.InProcess>>>>>>>
                                   .Select(graph, isTravel, appointmentID);

            return appoinmentDetRow != null && appoinmentDetRow.LineType == ID.LineType_ALL.SERVICE ? appoinmentDetRow.LineRef : null;
        }
        public virtual void ValidateSrvOrdTypeNumberingSequence(PXGraph graph, string srvOrdType)
        {
            ValidateSrvOrdTypeNumberingSequenceInt(graph, srvOrdType);
        }

        public virtual bool GetRequireCustomerSignature(PXGraph graph, FSSrvOrdType fsSrvOrdTypeRow, FSServiceOrder fsServiceOrderRow)
        {
            if (fsSrvOrdTypeRow == null || fsServiceOrderRow == null)
            {
                return false;
            }

            Customer customerRow = PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.Select(graph, fsServiceOrderRow.CustomerID);
            FSxCustomer fsxCustomerRow = customerRow != null ? PXCache<Customer>.GetExtension<FSxCustomer>(customerRow) : null;

            return fsSrvOrdTypeRow.RequireCustomerSignature == true
                        || (fsxCustomerRow != null
                            && fsxCustomerRow.RequireCustomerSignature == true);
        }

        public virtual NotificationSource GetSource(PXGraph graph, string classID, Guid setupID, int? branchID)
        {
            var notificationSourceSet = PXSelect<NotificationSource,
                                        Where<
                                            NotificationSource.setupID, Equal<Required<NotificationSource.setupID>>,
                                            And<NotificationSource.classID, Equal<Required<NotificationSource.classID>>,
                                            And<NotificationSource.active, Equal<True>>>>>
                                        .Select(graph, setupID, classID);

            NotificationSource result = null;

            foreach (NotificationSource rec in notificationSourceSet)
            {
                if (rec.NBranchID == branchID)
                {
                    return rec;
                }

                if (rec.NBranchID == null)
                {
                    result = rec;
                }
            }

            return result;
        }

        /// <summary>
        /// Add the EmailSource.
        /// </summary>
        public virtual void AddEmailSource(PXGraph graph, int? sourceEmailID, RecipientList recipients)
        {
            NotificationRecipient recipient = null;

            EMailAccount emailAccountRow = PXSelect<EMailAccount,
                                           Where<
                                                 EMailAccount.emailAccountID, Equal<Required<EMailAccount.emailAccountID>>>>
                                           .Select(graph, sourceEmailID);

            if (emailAccountRow != null && emailAccountRow.Address != null)
            {
                recipient = new NotificationRecipient()
                {
                    Active = true,
                    Email = emailAccountRow.Address,
                    AddTo = RecipientAddToAttribute.To,
                    Format = "H"
                };

                if (recipient != null)
                {
                    recipients.Add(recipient);
                }
            }
        }

        /// <summary>
        /// Add the Customer info as a recipient in the Email template generated by Appointment.
        /// </summary>
        public virtual void AddCustomerRecipient(AppointmentEntry graphAppointmentEntry, NotificationRecipient recSetup, RecipientList recipients)
        {
            NotificationRecipient recipient = null;

            Customer customerRow = PXSelect<Customer,
                                   Where<
                                         Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                   .Select(graphAppointmentEntry, graphAppointmentEntry.ServiceOrderRelated.Current.CustomerID);

            if (customerRow == null)
            {
                return;
            }

            FSContact fsContactRow = graphAppointmentEntry.ServiceOrder_Contact.SelectSingle();

            if (fsContactRow != null && fsContactRow.Email != null)
            {
                recipient = new NotificationRecipient()
                {
                    Active = true,
                    Email = fsContactRow.Email,
                    AddTo = recSetup.AddTo,
                    Format = recSetup.Format
                };
            }
            else
            {
                Contact srvOrdContactRow = PXSelect<Contact,
                                           Where<
                                               Contact.contactID, Equal<Required<Contact.contactID>>>>
                                           .Select(graphAppointmentEntry, graphAppointmentEntry.ServiceOrderRelated.Current.ContactID);

                if (srvOrdContactRow != null && srvOrdContactRow.EMail != null)
                {
                    recipient = new NotificationRecipient()
                    {
                        Active = true,
                        Email = srvOrdContactRow.EMail,
                        AddTo = recSetup.AddTo,
                        Format = recSetup.Format
                    };
                }
            }

            if (recipient != null)
            {
                recipients.Add(recipient);
            }
        }

        [Obsolete("This method will be deleted in the next major release.")]
        public virtual void AddEmployeeStaffRecipient(AppointmentEntry graphAppointmentEntry,
                                                      int? bAccountID,
                                                      string type,
                                                      NotificationRecipient recSetup,
                                                      RecipientList recipients)
        {
            AddStaffRecipient(graphAppointmentEntry, bAccountID, type, recSetup, recipients);
        }

        private static void AddStaffRecipient(AppointmentEntry graphAppointmentEntry,
                                              int? bAccountID,
                                              string type,
                                              NotificationRecipient recSetup,
                                              RecipientList recipients)
        {
            NotificationRecipient recipient = null;
            Contact contactRow = null;

            if (type == BAccountType.EmployeeType)
            {
                contactRow = PXSelectJoin<Contact,
                             InnerJoin<BAccount,
                             On<
                                 BAccount.parentBAccountID, Equal<Contact.bAccountID>,
                                 And<BAccount.defContactID, Equal<Contact.contactID>>>>,
                             Where<
                                 BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>,
                             And<
                                 BAccount.type, Equal<Required<BAccount.type>>>>>
                             .Select(graphAppointmentEntry, bAccountID, type);
            }
            else if (type == BAccountType.VendorType)
            {
                contactRow = PXSelectJoin<Contact,
                             InnerJoin<BAccount,
                             On<
                                 Contact.contactID, Equal<BAccount.defContactID>>>,
                             Where<
                                 BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>,
                             And<
                                 BAccount.type, Equal<Required<BAccount.type>>>>>
                             .Select(graphAppointmentEntry, bAccountID, type);
            }

            if (contactRow != null && contactRow.EMail != null)
            {
                recipient = new NotificationRecipient()
                {
                    Active = true,
                    Email = contactRow.EMail,
                    AddTo = recSetup.AddTo,
                    Format = recSetup.Format
                };

                if (recipient != null)
                {
                    recipients.Add(recipient);
                }
            }
        }

        [Obsolete("This method will be deleted in the next major release.")]
        public virtual void AddVendorStaffRecipient(AppointmentEntry graphAppointmentEntry,
                                                    FSAppointmentEmployee fsAppointmentEmployee,
                                                    NotificationRecipient recSetup,
                                                    RecipientList recipients)
        {
            throw new PXInvalidOperationException();
            //TODO: AC-142850 6082 validate Vendor's AppNotification flag
            /*
                select * from Contact
                inner join BAccount on (BAccount.BAccountID = Contact.BAccountID
                            and BAccount.DefContactID = Contact.ContactID)
                where
                BAccount.BAccountID = xx
                and BAccount.Type = 'VE'
            */

            //BAccountType.VendorType
        }

        /// <summary>
        /// Add the Employee info defined in the Notification tab defined in the <c>SrvOrdType</c> as a recipient(s) in the Email template generated by Appointment.
        /// </summary>
        public virtual void AddEmployeeRecipient(PXGraph graph, NotificationRecipient recSetup, RecipientList recipients)
        {
            NotificationRecipient recipient = null;

            PXResult<Contact, BAccount, EPEmployee> bqlResult =
                                        (PXResult<Contact, BAccount, EPEmployee>)
                                        PXSelectJoin<Contact,
                                        InnerJoin<BAccount,
                                        On<
                                            Contact.bAccountID, Equal<BAccount.parentBAccountID>,
                                            And<Contact.contactID, Equal<BAccount.defContactID>>>,
                                        InnerJoin<EPEmployee,
                                        On<
                                            EPEmployee.bAccountID, Equal<BAccount.bAccountID>>>>,
                                        Where<
                                            Contact.contactID, Equal<Required<Contact.contactID>>,
                                        And<
                                            BAccount.type, Equal<Required<BAccount.type>>>>>
                                        .Select(graph, recSetup.ContactID, BAccountType.EmployeeType);

            Contact contactRow = (Contact)bqlResult;
            BAccount baccountRow = (BAccount)bqlResult;
            EPEmployee epEmployeeRow = (EPEmployee)bqlResult;

            if (epEmployeeRow != null)
            {
                if (contactRow != null && contactRow.EMail != null)
                {
                    recipient = new NotificationRecipient()
                    {
                        Active = true,
                        Email = contactRow.EMail,
                        AddTo = recSetup.AddTo,
                        Format = recSetup.Format
                    };

                    if (recipient != null)
                    {
                        recipients.Add(recipient);
                    }
                }
            }
        }

        /// <summary>
        /// Add the Billing Customer info as a recipient(s) in the Email template generated by Appointment.
        /// </summary>
        public virtual void AddBillingRecipient(AppointmentEntry graphAppointmentEntry, NotificationRecipient recSetup, RecipientList recipients)
        {
            NotificationRecipient recipient = null;

            if (graphAppointmentEntry.ServiceOrderRelated.Current.BillCustomerID != null)
            {
                Customer customerRow = PXSelect<Customer,
                                       Where<
                                            Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                       .Select(graphAppointmentEntry, graphAppointmentEntry.ServiceOrderRelated.Current.BillCustomerID);

                if (customerRow == null)
                {
                    return;
                }

                Contact contactRow = PXSelectJoin<Contact,
                                     InnerJoin<Customer,
                                     On<
                                         Contact.bAccountID, Equal<Customer.bAccountID>,
                                         And<Contact.contactID, Equal<Customer.defBillContactID>>>>,
                                     Where<
                                         Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                     .Select(graphAppointmentEntry, graphAppointmentEntry.ServiceOrderRelated.Current.BillCustomerID);

                if (contactRow != null && contactRow.EMail != null)
                {
                    recipient = new NotificationRecipient()
                    {
                        Active = true,
                        Email = contactRow.EMail,
                        AddTo = recSetup.AddTo,
                        Format = recSetup.Format
                    };
                }
            }

            if (recipient != null)
            {
                recipients.Add(recipient);
            }
        }

        /// <summary>
        /// Adds the Employee(s) belonging to the Appointment's Service Area as recipients in the Email template generated by Appointment.
        /// </summary>
        public virtual void AddGeoZoneStaffRecipient(AppointmentEntry graphAppointmentEntry, NotificationRecipient recSetup, RecipientList recipients)
        {
            List<FSGeoZoneEmp> geoZoneEmpList = new List<FSGeoZoneEmp>();

            FSAddress fsAddressRow = graphAppointmentEntry.ServiceOrder_Address.SelectSingle();

            if (fsAddressRow != null && fsAddressRow.PostalCode != null)
            {
                FSGeoZonePostalCode fsGeoZoneRow = StaffSelectionHelper.GetMatchingGeoZonePostalCode(graphAppointmentEntry, fsAddressRow.PostalCode);

                if (fsGeoZoneRow != null)
                {
                    var fsGeoZonePostalCodeSet = PXSelectJoin<FSGeoZonePostalCode,
                                                 InnerJoin<FSGeoZoneEmp,
                                                 On<
                                                     FSGeoZoneEmp.geoZoneID, Equal<FSGeoZonePostalCode.geoZoneID>>>,
                                                 Where<
                                                     FSGeoZonePostalCode.postalCode, Equal<Required<FSGeoZonePostalCode.postalCode>>>>
                                                 .Select(graphAppointmentEntry, fsGeoZoneRow.PostalCode);

                    foreach (PXResult<FSGeoZonePostalCode, FSGeoZoneEmp> bqlResult in fsGeoZonePostalCodeSet)
                    {
                        geoZoneEmpList.Add((FSGeoZoneEmp)bqlResult);
                    }
                }
            }

            List<FSGeoZoneEmp> fsGeoZoneEmpGroupByEmployeeID = geoZoneEmpList.GroupBy(x => x.EmployeeID).Select(grp => grp.First()).ToList();

            if (fsGeoZoneEmpGroupByEmployeeID.Count > 0)
            {
                foreach (FSGeoZoneEmp fsGeoZoneEmpRow in fsGeoZoneEmpGroupByEmployeeID)
                {
                    AddStaffRecipient(graphAppointmentEntry, fsGeoZoneEmpRow.EmployeeID, BAccountType.EmployeeType, recSetup, recipients);
                }
            }
        }

        /// <summary>
        /// Add the Employee email that has assigned the salesperson as a recipient in the Email template generated by Appointment.
        /// </summary>
        public virtual void AddSalespersonRecipient(AppointmentEntry graphAppointmentEntry, NotificationRecipient recSetup, RecipientList recipients)
        {
            NotificationRecipient recipient = null;

            PXResult<SalesPerson, EPEmployee, BAccount, Contact> bqlResult =
                                            (PXResult<SalesPerson, EPEmployee, BAccount, Contact>)
                                            PXSelectJoin<SalesPerson,
                                            InnerJoin<EPEmployee,
                                            On<
                                                EPEmployee.salesPersonID, Equal<SalesPerson.salesPersonID>>,
                                            InnerJoin<BAccount,
                                            On<
                                                BAccount.bAccountID, Equal<EPEmployee.bAccountID>>,
                                            InnerJoin<Contact,
                                            On<
                                                BAccount.parentBAccountID, Equal<Contact.bAccountID>,
                                                And<BAccount.defContactID, Equal<Contact.contactID>>>>>>,
                                            Where<
                                                SalesPerson.salesPersonID, Equal<Required<FSAppointment.salesPersonID>>>>
                                            .Select(graphAppointmentEntry, graphAppointmentEntry.AppointmentRecords.Current.SalesPersonID);

            Contact contactRow = (Contact)bqlResult;
            BAccount baccountRow = (BAccount)bqlResult;
            EPEmployee epEmployeeRow = (EPEmployee)bqlResult;
            SalesPerson SalespersonRow = (SalesPerson)bqlResult;

            if (epEmployeeRow != null && SalespersonRow != null)
            {
                if (contactRow != null && contactRow.EMail != null)
                {
                    recipient = new NotificationRecipient()
                    {
                        Active = true,
                        Email = contactRow.EMail,
                        AddTo = recSetup.AddTo,
                        Format = recSetup.Format
                    };

                    if (recipient != null)
                    {
                        recipients.Add(recipient);
                    }
                }
            }
        }

        public virtual RecipientList GetRecipients(AppointmentEntry graphAppointmentEntry, int? sourceID, int? sourceEmailID)
        {
            RecipientList verifiedRecipients = new RecipientList();
            RecipientList unverifiedRecipients = new RecipientList();

            bool allEmailsBCC = true;

            PXResultset<NotificationRecipient> notificationRecipientSet = PXSelect<NotificationRecipient,
                                                                          Where<
                                                                              NotificationRecipient.sourceID, Equal<Required<NotificationRecipient.sourceID>>,
                                                                          And<
                                                                              NotificationRecipient.active, Equal<True>>>,
                                                                          OrderBy<
                                                                              Asc<NotificationRecipient.notificationID>>>
                                                                          .Select(graphAppointmentEntry, sourceID);

            //This loop can't be included in the following one because if all fields are BCC, the origin email account has to be placed in the first position of the array
            //so Acumatica can use it as the "To:" email account
            foreach (NotificationRecipient notificationRecipientRow in notificationRecipientSet)
            {
                if (notificationRecipientRow.AddTo != RecipientAddToAttribute.Bcc)
                {
                    allEmailsBCC = false;
                    break;
                }
            }

            if (allEmailsBCC)
            {
                AddEmailSource(graphAppointmentEntry, sourceEmailID, unverifiedRecipients);
                VerifyRecipientsAndAddToList(unverifiedRecipients, verifiedRecipients, false, null);
            }

            var staffMemberRowsGrouped = graphAppointmentEntry.AppointmentServiceEmployees.Select().AsEnumerable().GroupBy(
                                                                    p => ((FSAppointmentEmployee)p).EmployeeID,
                                                                    (key, group) => new
                                                                    {
                                                                        Group = (FSAppointmentEmployee)group.First()
                                                                    })
                                                                    .Select(g => g.Group).ToList();

            foreach (NotificationRecipient notificationRecipientRow in notificationRecipientSet)
            {
                switch (notificationRecipientRow.ContactType)
                {
                    case FSNotificationContactType.Customer:
                        if (graphAppointmentEntry.ServiceOrderRelated.Current.CustomerID != null)
                        {
                            AddCustomerRecipient(graphAppointmentEntry, notificationRecipientRow, unverifiedRecipients);
                            VerifyRecipientsAndAddToList(unverifiedRecipients, verifiedRecipients, true, TX.Error.EmailNotificationEmailBlankForCurrentCustomer);
                        }

                        break;
                    case FSNotificationContactType.EmployeeStaff:
                        foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in staffMemberRowsGrouped)
                        {
                            if (fsAppointmentEmployeeRow.Type == BAccountType.EmployeeType)
                            {
                                AddStaffRecipient(graphAppointmentEntry, fsAppointmentEmployeeRow.EmployeeID, fsAppointmentEmployeeRow.Type, notificationRecipientRow, unverifiedRecipients);
                                VerifyRecipientsAndAddToList(unverifiedRecipients, verifiedRecipients, true, TX.Error.EmailNotificationEmailBlankForCurrentEmployeeStaff);
                            }
                        }

                        break;
                    case FSNotificationContactType.VendorStaff:
                        foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in staffMemberRowsGrouped)
                        {
                            if (fsAppointmentEmployeeRow.Type == BAccountType.VendorType)
                            {
                                AddStaffRecipient(graphAppointmentEntry, fsAppointmentEmployeeRow.EmployeeID, fsAppointmentEmployeeRow.Type, notificationRecipientRow, unverifiedRecipients);
                                VerifyRecipientsAndAddToList(unverifiedRecipients, verifiedRecipients, true, TX.Error.EmailNotificationEmailBlankForCurrentVendorStaff);
                            }
                        }

                        break;
                    case FSNotificationContactType.Employee:
                        AddEmployeeRecipient(graphAppointmentEntry, notificationRecipientRow, unverifiedRecipients);
                        VerifyRecipientsAndAddToList(unverifiedRecipients, verifiedRecipients, true, TX.Error.EmailNotificationEmailBlankForEmployeeTypeNotificationRecipient);
                        break;
                    case FSNotificationContactType.Billing:
                        AddBillingRecipient(graphAppointmentEntry, notificationRecipientRow, unverifiedRecipients);
                        VerifyRecipientsAndAddToList(unverifiedRecipients, verifiedRecipients, true, TX.Error.EmailNotificationEmailBlankForCurrentBillingCustomer);
                        break;
                    case FSNotificationContactType.GeoZoneStaff:
                        AddGeoZoneStaffRecipient(graphAppointmentEntry, notificationRecipientRow, unverifiedRecipients);
                        VerifyRecipientsAndAddToList(unverifiedRecipients, verifiedRecipients, true, TX.Error.EmailNotificationEmailBlankForCurrentGeoZoneStaff);
                        break;
                    case FSNotificationContactType.Salesperson:
                        AddSalespersonRecipient(graphAppointmentEntry, notificationRecipientRow, unverifiedRecipients);
                        VerifyRecipientsAndAddToList(unverifiedRecipients, verifiedRecipients, true, TX.Error.EmailNotificationEmailBlankForCurrentSalesperson);
                        break;
                    default:
                        break;
                }
            }

            //The only element in the list is the From and the email shouldn't be sent
            if (verifiedRecipients.Count() == 1 && allEmailsBCC == true)
            {
                verifiedRecipients = null;
            }

            return verifiedRecipients;
        }

        public virtual void VerifyRecipientsAndAddToList(RecipientList unverifiedRecipients, RecipientList verifiedRecipients, bool throwError, string errorMessage)
        {
            foreach (NotificationRecipient recipient in unverifiedRecipients)
            {
                if (string.IsNullOrWhiteSpace(recipient.Email) == true)
                {
                    if (throwError)
                    {
                        throw new PXException(errorMessage);
                    }
                }
                else
                {
                    verifiedRecipients.Add(recipient);
                }
            }
        }

        /// <summary>
        /// Returns the emails address for the "To" and "BCC" sections.
        /// </summary>
        public virtual void GetsRecipientsFields(IEnumerable<NotificationRecipient> notificationRecipientSet, ref string emailToAccounts, ref string emailBCCAccounts)
                {
            bool firstToElement = true;
            bool firstBCCElement = true;

            foreach (NotificationRecipient notificationRecipientRow in notificationRecipientSet)
            {
                if (notificationRecipientRow.AddTo != RecipientAddToAttribute.Bcc)
                {
                    if (firstToElement == true)
                    {
                        firstToElement = false;
                        emailToAccounts = notificationRecipientRow.Email;
                    }
                    else
                    {
                        emailToAccounts = emailToAccounts + "; " + notificationRecipientRow.Email;
                    }
                }
                else
                {
                    if (firstBCCElement == true)
                    {
                        firstBCCElement = false;
                        emailBCCAccounts = notificationRecipientRow.Email;
                    }
                    else
                    {
                        emailBCCAccounts = emailBCCAccounts + "; " + notificationRecipientRow.Email;
                    }
                }
                }

            return;
            }

        public virtual void SendNotification(AppointmentEntry graphAppointmentEntry, PXCache sourceCache, string notificationCD, int? branchID, IDictionary<string, string> parameters, IList<Guid?> attachments = null)
        {
            if (sourceCache.Current == null)
            {
                throw new PXException(CR.Messages.EmailNotificationObjectNotFound);
            }

            Guid? setupID = new NotificationUtility(graphAppointmentEntry).SearchSetupID(FSNotificationSource.Appointment, notificationCD);

            if (setupID == null)
            {
                throw new PXException(CR.Messages.EmailNotificationSetupNotFound, notificationCD);
            }

            if (branchID == null)
            {
                branchID = graphAppointmentEntry.Accessinfo.BranchID;
            }

            if (parameters == null)
            {
                parameters = new Dictionary<string, string>();

                foreach (string key in sourceCache.Keys)
                {
                        object value = sourceCache.GetValueExt(sourceCache.Current, key);
                        parameters[key] = value != null ? value.ToString() : null;
                }
            }

            Send(graphAppointmentEntry, sourceCache, (Guid)setupID, branchID, parameters, attachments);
        }

        public virtual void Send(AppointmentEntry graphAppointmentEntry, PXCache sourceCache, Guid setupID, int? branchID, IDictionary<string, string> reportParams, IList<Guid?> attachments = null)
        {
            string emailToAccounts = string.Empty;
            string emailBCCAccounts = string.Empty;

            FSAppointment fsAppointmentRow = graphAppointmentEntry.AppointmentRecords.Current;

            Guid? refNoteId = fsAppointmentRow.NoteID;
            Guid? parentNoteId = null;

            string srvOrdType = fsAppointmentRow.SrvOrdType;

            NotificationSetup notificationSetup = PXSelect<NotificationSetup,
                Where<NotificationSetup.setupID, Equal<Required<NotificationSetup.setupID>>>>.
                Select(graphAppointmentEntry, setupID);
            string notificationCD = notificationSetup?.NotificationCD;

            NotificationSource source = GetSource(graphAppointmentEntry, srvOrdType, setupID, branchID);

            if (source == null)
            {
                throw new PXException(TX.Error.EmailNotificationSetupNotFoundForTheSrvOrdTypeX, notificationCD, srvOrdType);
            }

            var accountId = source.EMailAccountID ?? Data.EP.MailAccountManager.DefaultMailAccountID;

            if (accountId == null)
            {
                throw new PXException(TX.Warning.EMAIL_ACCOUNT_NOT_CONFIGURED_FOR_MAILING,
                                notificationCD,
                                srvOrdType,
                                PX.Data.ActionsMessages.PreferencesEmailMaint);
            }

            RecipientList recipients = GetRecipients(graphAppointmentEntry, source.SourceID, accountId);

            if (recipients == null || recipients.Count() == 0)
            {
                throw new PXException(TX.Error.EmailNotificationRecipientListEmpty);
            }

            GetsRecipientsFields(recipients, ref emailToAccounts, ref emailBCCAccounts);

            var sent = false;
            
            if (source.NotificationID != null)
            {
                var sender = TemplateNotificationGenerator.Create(fsAppointmentRow, (int)source.NotificationID);

                if (source.EMailAccountID != null)
                {
                    sender.MailAccountId = accountId;
                }

                string notificationBody = sender.Body;
                FSAppointment.ReplaceWildCards(graphAppointmentEntry, ref notificationBody, fsAppointmentRow);

                sender.Body = notificationBody;
                sender.BodyFormat = source.Format;
                sender.RefNoteID = refNoteId;
                sender.ParentNoteID = parentNoteId;
                sender.To = emailToAccounts;
                sender.Bcc = emailBCCAccounts;

                if (source.ReportID != null)
                {
                    var _report = PXReportTools.LoadReport(source.ReportID, null);

                    if (_report == null)
                    {
                        throw new ArgumentException(PXMessages.LocalizeFormatNoPrefixNLA(EP.Messages.ReportCannotBeFound, source.ReportID), "reportId");
                    }

                    PXReportTools.InitReportParameters(_report, reportParams, SettingsProvider.Instance.Default);

                    _report.MailSettings.Format = ReportNotificationGenerator.ConvertFormat(source.Format);

                    var reportNode = ReportProcessor.ProcessReport(_report);

                    reportNode.SendMailMode = true;

                    Reports.Mail.Message message = (from msg in reportNode.Groups.Select(g => g.MailSettings)
                                                    where msg != null && msg.ShouldSerialize()
                                                    select new Reports.Mail.Message(msg, reportNode, msg))
                                                   .FirstOrDefault();

                    if (message == null)
                    {
                        throw new InvalidOperationException(PXMessages.LocalizeFormatNoPrefixNLA(EP.Messages.EmailFromReportCannotBeCreated, source.ReportID));
                    }

                    foreach (var attachment in message.Attachments)
                    {
                        if (sender.Body == null && sender.BodyFormat == NotificationFormat.Html && attachment.MimeType == "text/html")
                        {
                            sender.Body = attachment.Encoding.GetString(attachment.GetBytes());
                        }
                        else
                        {
                            sender.AddAttachment(attachment.Name, attachment.GetBytes(), attachment.CID);
                        }
                    }
                }

                if (attachments != null)
                {
                    foreach (var attachment in attachments)
                    {
                        if (attachment != null)
                        {
                            sender.AddAttachmentLink(attachment.Value);
                        }
                    }
                }

                sent |= sender.Send().Any();
            }
            else if (source.ReportID != null)
            {
                var sender = new ReportNotificationGenerator(source.ReportID)
                {
                    MailAccountId = accountId,
                    Format = source.Format,
                    AdditionalRecipents = recipients,
                    Parameters = reportParams,
                    NotificationID = source.NotificationID
                };

                sent |= sender.Send().Any();
            }

            if (!sent)
            {
                throw new PXException(CR.Messages.EmailNotificationError);
            }
        }

        public virtual bool IsEnableEmailSignedAppointment()
        {
            FSAppointment fsAppointmentRow = AppointmentRecords.Current;

            return fsAppointmentRow != null
                    && fsAppointmentRow.Hold == false
                    && fsAppointmentRow.Awaiting == false
                    && fsAppointmentRow.CustomerSignedReport != null;
        }

        public virtual bool IsItemLineUpdateRequired(PXCache cache, FSAppointment row, FSAppointment oldRow)
        {
            return cache.ObjectsEqual<FSAppointment.areActualFieldsActive>(row, oldRow) == false;
        }

        public virtual void UpdateItemLinesBecauseOfDocStatusChange()
        {
            PXCache cache = AppointmentDetails.Cache;

            foreach (FSAppointmentDet row in AppointmentDetails.Select())
            {
                // This is to trigger the recalculation of the dependent fields
                // only when the value changes.

                object newAreActualFieldsActive;
                cache.RaiseFieldDefaulting<FSAppointmentDet.areActualFieldsActive>(row, out newAreActualFieldsActive);

                if ((bool?)newAreActualFieldsActive != row.AreActualFieldsActive)
                {
                    if (row.IsLinkedItem == true)
                    {
                        cache.SetValue<FSAppointmentDet.areActualFieldsActive>(row, newAreActualFieldsActive);
                    }
                    else
                    {
                        FSAppointmentDet copy = (FSAppointmentDet)cache.CreateCopy(row);

                        cache.SetValueExt<FSAppointmentDet.areActualFieldsActive>(copy, newAreActualFieldsActive);

                        cache.Update(copy);
                    }
                }
            }
        }

        public virtual void RecalculateAreActualFieldsActive(PXCache cache, FSAppointment row)
        {
            if (row == null)
            {
                return;
            }

            // This method is to trigger the recalculation of the dependent fields
            // only when the value changes.

            object newAreActualFieldsActive;
            cache.RaiseFieldDefaulting<FSAppointment.areActualFieldsActive>(row, out newAreActualFieldsActive);

            if ((bool?)newAreActualFieldsActive != row.AreActualFieldsActive)
            {
                cache.SetValueExt<FSAppointment.areActualFieldsActive>(row, newAreActualFieldsActive);
            }
        }
        #endregion

        #region Static Methods
        public static List<FSAppointmentDet> GetRelatedApptLinesInt(PXGraph graph, int? soDetID, bool excludeSpecificApptLine, int? apptDetID, bool onlyMarkForPOLines, bool sortResult)
        {
            BqlCommand bqlCommand = new Select<FSAppointmentDet,
                    Where<
                        FSAppointmentDet.sODetID, Equal<Required<FSAppointmentDet.sODetID>>,
                        And<FSAppointmentDet.status, NotEqual<FSAppointmentDet.status.Canceled>>>>();

            List<object> parameters = new List<object>();
            parameters.Add(soDetID);

            if (excludeSpecificApptLine == true)
            {
                if (apptDetID == null)
                {
                    throw new ArgumentException();
                }

                bqlCommand = bqlCommand.WhereAnd(typeof(Where<FSAppointmentDet.appDetID, NotEqual<Required<FSAppointmentDet.appDetID>>>));
                parameters.Add(apptDetID);
            }

            if (onlyMarkForPOLines == true)
            {
                bqlCommand = bqlCommand.WhereAnd(typeof(
                        Where<
                            FSAppointmentDet.status, Equal<FSAppointmentDet.status.waitingForPO>,
                            Or<FSAppointmentDet.status, Equal<FSAppointmentDet.status.NotStarted>>>
                    ));
            }

            if (sortResult == true)
            {
                bqlCommand = bqlCommand.OrderByNew(typeof(
                        OrderBy<
                            Asc<FSAppointmentDet.tranDate,
                            Asc<FSAppointmentDet.srvOrdType,
                            Asc<FSAppointmentDet.refNbr,
                            Asc<FSAppointmentDet.sortOrder>>>>>
                    ));
            }

            return new PXView(graph, false, bqlCommand).SelectMulti(parameters.ToArray()).RowCast<FSAppointmentDet>().ToList();
        }

        public static decimal GetCuryDocTotal(decimal? curyLineTotal, decimal? curyLogBillableTranAmountTotal, decimal? curyDiscTotal, decimal? curyTaxTotal, decimal? curyInclTaxTotal)
        {
            return (curyLineTotal ?? 0) + (curyLogBillableTranAmountTotal ?? 0) - (curyDiscTotal ?? 0) + (curyTaxTotal ?? 0) - (curyInclTaxTotal ?? 0);
        }

        public static void UpdateCanceledNotPerformed(PXCache cache, FSAppointmentDet row, FSAppointment appointmentRow, string oldStatusValue)
        {
            bool apptIsInProcessPaused = appointmentRow?.InProcess == true || appointmentRow?.Paused == true;

            object newIsCanceledNotPerformed;
            cache.RaiseFieldDefaulting<FSAppointmentDet.isCanceledNotPerformed>(row, out newIsCanceledNotPerformed);

            if ((bool?)newIsCanceledNotPerformed != row.IsCanceledNotPerformed
                || (
                    row.Status == FSAppointmentDet.status.NOT_STARTED
                    && apptIsInProcessPaused == true
                )
            )
            {
                // This is to trigger the recalculation of the dependent fields.
                cache.SetValueExt<FSAppointmentDet.isCanceledNotPerformed>(row, newIsCanceledNotPerformed);
            }
            else
            {
                // This is to avoid triggering the recalculation of dependent fields.
                row.IsCanceledNotPerformed = newIsCanceledNotPerformed == null ? false : (bool?)newIsCanceledNotPerformed;
            }
        }
        /// <summary>
        /// Gets the corresponding Service Order Detail from the <c>fsAppointmentDetRow.SODetID</c>.
        /// </summary>
        public static FSSODet GetSODetFromAppointmentDetInt(PXGraph graph, FSAppointmentDet fsAppointmentDetRow)
        {
            FSSODet fsSODetRow = new FSSODet();

            if (fsAppointmentDetRow != null)
            {
                fsSODetRow = FSSODet.UK.Find(graph, fsAppointmentDetRow.SODetID);
            }

            return fsSODetRow;
        }

        #endregion

        #region Time Card Methods

        public virtual bool ValidateTimeIntegration(PXGraph graph)
        {
            return TimeCardHelper.IsTheTimeCardIntegrationEnabled(graph) == true
                    && PXAccess.FeatureInstalled<FeaturesSet.timeReportingModule>() == true;
        }

        public virtual void VerifyTimeActivityUpdate(PXCache cache, FSAppointmentLog logRow, string fieldName)
        {
            if (ValidateTimeIntegration(this) == false
                   || logRow.BAccountID == null
                   || logRow.BAccountType != BAccountType.EmployeeType)
            {
                return;
            }

            TMEPEmployee epEmployeeRow = FindTMEmployee(this, logRow.BAccountID);
            EPActivityApprove epActivityApproveRow =  FindEPActivityApprove(this, logRow, epEmployeeRow);

            if (epActivityApproveRow != null
                    && ValidateInsertUpdateTimeActivity(epActivityApproveRow) == false)
            {
                throw new PXSetPropertyException(TX.Error.FieldCannotBeUpdatedBecauseRelatedReleasedTimeActivity,
                                                 PXUIFieldAttribute.GetDisplayName(cache, fieldName));
            }
        }

        public virtual void InsertUpdateDeleteTimeActivities(FSAppointment fsAppointmentRow,
                                                             FSServiceOrder fsServiceOrderRow,
                                                             FSAppointmentLog fsAppointmentLogRow,
                                                             PXCache cache)
        {
            if (ValidateTimeIntegration(this) == false)
                return;

            if (fsAppointmentLogRow.BAccountType != BAccountType.EmployeeType)
                return;

            EmployeeActivitiesEntry employeeActivitiesEntryGraph = null;

            if (LogRecords.Cache.GetStatus(fsAppointmentLogRow) == PXEntryStatus.Inserted
                    || LogRecords.Cache.GetStatus(fsAppointmentLogRow) == PXEntryStatus.Updated)
            {
                TMEPEmployee epEmployeeRow = FindTMEmployee(this, fsAppointmentLogRow.BAccountID);
                EPActivityApprove epActivityApproveRow = FindEPActivityApprove(this, fsAppointmentLogRow, epEmployeeRow);

                if (fsAppointmentLogRow.TrackTime == true
                    && (fsAppointmentLogRow.Status == ID.Status_Log.COMPLETED || fsAppointmentLogRow.Status == ID.Status_Log.PAUSED))
                {
                    employeeActivitiesEntryGraph = employeeActivitiesEntryGraph ?? GetEmployeeActivitiesEntryGraph(clearGraph: true);

                    int? oldBAccountID = (int?)cache.GetValueOriginal<FSAppointmentLog.bAccountID>(fsAppointmentLogRow);

                    if (fsAppointmentLogRow.BAccountID != oldBAccountID)
                    {
                        TMEPEmployee oldEPEmployeeRow = FindTMEmployee(this, oldBAccountID);
                        EPActivityApprove oldEPActivityApproveRow = FindEPActivityApprove(this, fsAppointmentLogRow, oldEPEmployeeRow);

                        if (oldEPActivityApproveRow != null)
                        {
                            DeleteEPActivityApprove(employeeActivitiesEntryGraph, oldEPActivityApproveRow, oldEPEmployeeRow);
                        }
                    }

                    InsertUpdateEPActivityApprove(this, employeeActivitiesEntryGraph, fsAppointmentLogRow, fsAppointmentRow, fsServiceOrderRow, epActivityApproveRow, epEmployeeRow);
                }
                else
                {
                    if (epActivityApproveRow != null)
                    {
                        DeleteEPActivityApprove(GetEmployeeActivitiesEntryGraph(), epActivityApproveRow, epEmployeeRow);
                    }
                }
            }
            else if (LogRecords.Cache.GetStatus(fsAppointmentLogRow) == PXEntryStatus.Deleted)
            {
                SearchAndDeleteEPActivity(fsAppointmentLogRow, GetEmployeeActivitiesEntryGraph());
            }
        }

        public virtual void InsertUpdateDeleteTimeActivities(PXCache appointmentCache,
                                                             FSAppointment fsAppointmentRow,
                                                             FSServiceOrder fsServiceOrderRow,
                                                             List<FSAppointmentLog> deleteReleatedTimeActivity,
                                                             List<FSAppointmentLog> createReleatedTimeActivity)
        {
            if (ValidateTimeIntegration(this) == false)
                return;

            EmployeeActivitiesEntry employeeActivitiesEntryGraph = null;

            if ((string)appointmentCache.GetValueOriginal<FSAppointment.status>(fsAppointmentRow) == FSAppointment.status.Values.Completed
                    && fsAppointmentRow.Status == FSAppointment.status.Values.NotStarted)
            {
                employeeActivitiesEntryGraph = employeeActivitiesEntryGraph ?? GetEmployeeActivitiesEntryGraph();

                foreach (FSAppointmentLog fsAppointmentLogRow in LogRecords.Select().RowCast<FSAppointmentLog>().Where(row => row.BAccountType == BAccountType.EmployeeType))
                {
                    SearchAndDeleteEPActivity(fsAppointmentLogRow, employeeActivitiesEntryGraph);
                }

                foreach (FSAppointmentLog fsAppointmentLogRow in LogRecords.Cache.Deleted.RowCast<FSAppointmentLog>().Where(row => row.BAccountType == BAccountType.EmployeeType))
                {
                    SearchAndDeleteEPActivity(fsAppointmentLogRow, employeeActivitiesEntryGraph);
                }
            }

            if (deleteReleatedTimeActivity != null && deleteReleatedTimeActivity.Count > 0)
            {
                employeeActivitiesEntryGraph = employeeActivitiesEntryGraph ?? GetEmployeeActivitiesEntryGraph();

                //Deleting time activities related with canceled service lines
                foreach (FSAppointmentLog fsAppointmentLogRow in deleteReleatedTimeActivity)
                {
                    if (fsAppointmentLogRow.BAccountType == BAccountType.EmployeeType)
                    {
                        SearchAndDeleteEPActivity(fsAppointmentLogRow, employeeActivitiesEntryGraph);
                    }
                }
            }

            if (createReleatedTimeActivity != null && createReleatedTimeActivity.Count > 0)
            {
                employeeActivitiesEntryGraph = employeeActivitiesEntryGraph ?? GetEmployeeActivitiesEntryGraph();

                //Creating time activities related with re-opened service lines
                foreach (FSAppointmentLog fsAppointmentLogRow in createReleatedTimeActivity)
                {
                    if (fsAppointmentLogRow.BAccountType == BAccountType.EmployeeType)
                    {
                        TMEPEmployee epEmployeeRow = FindTMEmployee(this, fsAppointmentLogRow.BAccountID);
                        InsertUpdateEPActivityApprove(this, employeeActivitiesEntryGraph, fsAppointmentLogRow, fsAppointmentRow, fsServiceOrderRow, null, epEmployeeRow);
                    }
                }
            }
        }

        public virtual void SearchAndDeleteEPActivity(FSAppointmentLog fsAppointmentLogRow,
                                                         EmployeeActivitiesEntry graphEmployeeActivitiesEntry)
        {
            TMEPEmployee epEmployeeRow = FindTMEmployee(this, fsAppointmentLogRow.BAccountID);
            EPActivityApprove epActivityApproveRow = FindEPActivityApprove(this, fsAppointmentLogRow, epEmployeeRow);

            if (epActivityApproveRow != null)
            {
                DeleteEPActivityApprove(graphEmployeeActivitiesEntry, epActivityApproveRow, epEmployeeRow);
            }
        }

        public virtual void DeleteEPActivityApprove(EmployeeActivitiesEntry graphEmployeeActivitiesEntry,
                                                       EPActivityApprove epActivityApproveRow,
                                                       TMEPEmployee epEmployeeRow)
        {
            if (epActivityApproveRow != null)
            {
                if (ValidateInsertUpdateTimeActivity(epActivityApproveRow) == false)
                {
                    throw new PXSetPropertyException(TX.Error.ReleasedTimeActivityCannotBeDeleted, epEmployeeRow?.AcctCD, PXErrorLevel.Error);
                }

                graphEmployeeActivitiesEntry.Activity.Delete(epActivityApproveRow);
                graphEmployeeActivitiesEntry.Save.Press();
            }
        }

        [Obsolete("Remove in major release")]
        public virtual void DeleteEPActivityApprove(EmployeeActivitiesEntry graphEmployeeActivitiesEntry,
                                                       EPActivityApprove epActivityApproveRow)
        {
            if (epActivityApproveRow != null)
            {
                if (epActivityApproveRow.ApprovalStatus == ActivityStatusListAttribute.Approved
                    || epActivityApproveRow.ApprovalStatus == ActivityStatusListAttribute.Released
                    || epActivityApproveRow.TimeCardCD != null)
                {
                    return;
                }

                graphEmployeeActivitiesEntry.Activity.Delete(epActivityApproveRow);
                graphEmployeeActivitiesEntry.Save.Press();
            }
        }

        public virtual TMEPEmployee FindTMEmployee(PXGraph graph, int? employeeID)
        {
            TMEPEmployee epEmployeeRow = PXSelect<TMEPEmployee,
                            Where<
                                TMEPEmployee.bAccountID, Equal<Required<TMEPEmployee.bAccountID>>>>
                            .Select(graph, employeeID);

            if (epEmployeeRow == null)
            {
                throw new Exception(TX.Error.MISSING_LINK_ENTITY_STAFF_MEMBER);
            }

            return epEmployeeRow;
        }

        public virtual EPActivityApprove FindEPActivityApprove(PXGraph graph,
                                                 FSAppointmentLog fsAppointmentLogRow,
                                                 TMEPEmployee epEmployeeRow)
        {
            if (fsAppointmentLogRow == null || epEmployeeRow == null)
                return null;

            return PXSelect<EPActivityApprove,
                                   Where<
                                       EPActivityApprove.ownerID, Equal<Required<EPActivityApprove.ownerID>>,
                                       And<FSxPMTimeActivity.appointmentID, Equal<Required<FSxPMTimeActivity.appointmentID>>,
                                       And<FSxPMTimeActivity.logLineNbr, Equal<Required<FSxPMTimeActivity.logLineNbr>>>>>>
                                   .Select(graph, epEmployeeRow.DefContactID, fsAppointmentLogRow.DocID, fsAppointmentLogRow.LineNbr);
        }

        public virtual bool ValidateInsertUpdateTimeActivity(EPActivityApprove epActivityApproveRow)
        {
            return epActivityApproveRow == null
                    || (epActivityApproveRow.ApprovalStatus != ActivityStatusListAttribute.Approved
                        && epActivityApproveRow.ApprovalStatus != ActivityStatusListAttribute.Released
                        && epActivityApproveRow.Released == false
                        && epActivityApproveRow.TimeCardCD == null);
        }

        public virtual void InsertUpdateEPActivityApprove(PXGraph graph,
                                                         EmployeeActivitiesEntry graphEmployeeActivitiesEntry,
                                                         FSAppointmentLog fsAppointmentLogRow,
                                                         FSAppointment fsAppointmentRow,
                                                         FSServiceOrder fsServiceOrderRow,
                                                         EPActivityApprove epActivityApproveRow,
                                                         TMEPEmployee epEmployeeRow)
        {
            if (ValidateInsertUpdateTimeActivity(epActivityApproveRow) == false)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetServiceRow =
                                        PXSelect<FSAppointmentDet,
                                        Where<
                                            FSAppointmentDet.lineRef, Equal<Required<FSAppointmentDet.lineRef>>,
                                            And<FSAppointmentDet.appointmentID, Equal<Required<FSAppointmentDet.appointmentID>>>>>
                                        .Select(graph, fsAppointmentLogRow.DetLineRef, fsAppointmentRow.AppointmentID);

            if (fsAppointmentDetServiceRow != null &&
                    fsAppointmentDetServiceRow.IsCanceledNotPerformed == true)
            {
                return;
            }

            if (epActivityApproveRow == null)
            {
                epActivityApproveRow = new EPActivityApprove();
                epActivityApproveRow.OwnerID = epEmployeeRow.DefContactID;
                epActivityApproveRow = graphEmployeeActivitiesEntry.Activity.Insert(epActivityApproveRow);
            }

            graphEmployeeActivitiesEntry.Activity.SetValueExt<EPActivityApprove.hold>(epActivityApproveRow, false);
            epActivityApproveRow.WorkgroupID = fsAppointmentLogRow.WorkgroupID;
            epActivityApproveRow.Date = fsAppointmentLogRow.DateTimeBegin;
            epActivityApproveRow.EarningTypeID = fsAppointmentLogRow.EarningType;
            epActivityApproveRow.TimeSpent = fsAppointmentLogRow.TimeDuration;
            epActivityApproveRow.Summary = GetDescriptionToUseInEPActivityApprove(fsAppointmentRow, fsAppointmentLogRow, fsAppointmentDetServiceRow);
            epActivityApproveRow.CostCodeID = fsAppointmentLogRow?.CostCodeID;

            FSxPMTimeActivity fsxPMTimeActivityRow = PXCache<PMTimeActivity>.GetExtension<FSxPMTimeActivity>((PMTimeActivity)epActivityApproveRow);

            fsxPMTimeActivityRow.SOID = fsAppointmentRow.SOID;
            fsxPMTimeActivityRow.AppointmentID = fsAppointmentRow.AppointmentID;
            fsxPMTimeActivityRow.AppointmentCustomerID = fsServiceOrderRow.CustomerID;
            fsxPMTimeActivityRow.LogLineNbr = fsAppointmentLogRow.LineNbr;
            fsxPMTimeActivityRow.ServiceID = fsAppointmentLogRow.DetLineRef != null ? fsAppointmentDetServiceRow?.InventoryID : null;

            epActivityApproveRow = graphEmployeeActivitiesEntry.Activity.Update(epActivityApproveRow);

            graphEmployeeActivitiesEntry.Activity.SetValueExt<EPActivityApprove.projectID>(epActivityApproveRow, fsServiceOrderRow.ProjectID);
            graphEmployeeActivitiesEntry.Activity.SetValueExt<EPActivityApprove.projectTaskID>(epActivityApproveRow, fsAppointmentLogRow.ProjectTaskID);
            graphEmployeeActivitiesEntry.Activity.SetValueExt<EPActivityApprove.isBillable>(epActivityApproveRow, fsAppointmentLogRow.IsBillable);
            graphEmployeeActivitiesEntry.Activity.SetValueExt<EPActivityApprove.timeBillable>(epActivityApproveRow, fsAppointmentLogRow.BillableTimeDuration);
            graphEmployeeActivitiesEntry.Activity.SetValueExt<EPActivityApprove.approvalStatus>(epActivityApproveRow, GetStatusToUseInEPActivityApprove());
            graphEmployeeActivitiesEntry.Activity.SetValueExt<EPActivityApprove.labourItemID>(epActivityApproveRow, fsAppointmentLogRow.LaborItemID);

            graphEmployeeActivitiesEntry.Save.Press();
        }

        public virtual string GetDescriptionToUseInEPActivityApprove(FSAppointment fsAppointmentRow,
                                                             FSAppointmentLog fsAppointmentLogRow,
                                                             FSAppointmentDet fsAppointmentDetRow)
        {
            if (fsAppointmentLogRow != null)
            {
                if (fsAppointmentLogRow.ItemType == FSAppointmentLog.itemType.Values.Travel)
                {
                    return fsAppointmentLogRow.Descr;
                }
                else if (fsAppointmentLogRow.DetLineRef != null &&
                        fsAppointmentDetRow != null)
                {
                    return fsAppointmentDetRow.TranDesc;
                }
            }

            return fsAppointmentRow.DocDesc;
        }

        public virtual void EnableDisableTravelTimeMobileActions(PXCache cache, object row)
        {
            if (this.IsMobile == true)
            {
                FSAppointment appointment = AppointmentRecords.Current;

                if (appointment == null)
                {
                    return;
                }

                bool enabled = appointment.Canceled == false
                                && appointment.Closed == false
                                && appointment.Hold == false;

                if (row is FSAppointmentDet)
                {
                    FSAppointmentDet fsAppointmentDetRow = row as FSAppointmentDet;

                    startServiceMobile.SetEnabled(enabled);
                    completeServiceMobile.SetEnabled(enabled);
                    startForAssignedStaffMobile.SetEnabled(enabled);
                    cancelItemLine.SetEnabled(enabled);
                }
            }
        }

        [Obsolete("Remove in major release")]
        public virtual object GetProjectTaskIDToUseInEPActivityApprove(FSAppointment fsAppointmentRow,
                                                               FSAppointmentLog fsAppointmentLogRow,
                                                               FSAppointmentDet fsAppointmentDetRow)
        {
            if (fsAppointmentLogRow != null && fsAppointmentLogRow.DetLineRef != null && fsAppointmentDetRow != null)
            {
                return fsAppointmentDetRow.ProjectTaskID;
            }
            else
            {
                return fsAppointmentRow.DfltProjectTaskID;
            }
        }

        public virtual string GetStatusToUseInEPActivityApprove()
        {
            return ActivityStatusListAttribute.Completed;
        }
        public virtual Int32? GetPreferedSiteID()
        {
            int? siteID = null;
            FSAppointmentDet site = PXSelectJoin<FSAppointmentDet,
                InnerJoin<INSite, On<INSite.siteID, Equal<FSAppointmentDet.siteID>>>,
                Where<FSAppointmentDet.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>,
                    And<FSAppointmentDet.refNbr, Equal<Current<FSAppointment.refNbr>>,
                        And<Match<INSite, Current<AccessInfo.userName>>>>>>.Select(this);

            if (site != null)
            {
                siteID = site.SiteID;
            }

            return siteID;
        }

        public virtual DateTime? GetShipDate(FSServiceOrder serviceOrder, FSAppointment appointment)
        {
            return GetShipDateInt(this, serviceOrder, appointment);
        }

        public virtual bool ShouldUpdateAppointmentLogBillableFieldsFromTimeCard()
        {
            FSSrvOrdType fsSrvOrdTypeRow = ServiceOrderTypeSelected.Current;

            return (fsSrvOrdTypeRow != null
                    && fsSrvOrdTypeRow.PostTo == ID.SrvOrdType_PostTo.PROJECTS
                    && fsSrvOrdTypeRow.BillingType == ID.SrvOrdType_BillingType.COST_AS_COST
                    && fsSrvOrdTypeRow.CreateTimeActivitiesFromAppointment == true);
        }
        #endregion

        #region Static Methods
        public static DateTime? GetShipDateInt(PXGraph graph, FSServiceOrder serviceOrder, FSAppointment appointment)
        {
            DateTime? shipDate = null;

            if (appointment != null)
            {
                shipDate = appointment.ActualDateTimeBegin?.Date;
            }
            else if (serviceOrder != null)
            {
                shipDate = serviceOrder.OrderDate;
            }

            return graph.Accessinfo.BusinessDate > shipDate ? graph.Accessinfo.BusinessDate : shipDate;
        }
        public static DateTime? GetDateTimeEndInt(DateTime? dateTimeBegin, int hour = 0, int minute = 0, int second = 0, int milisecond = 0)
        {
            if (dateTimeBegin != null)
            {
                return new DateTime(dateTimeBegin.Value.Year,
                                    dateTimeBegin.Value.Month,
                                    dateTimeBegin.Value.Day,
                                    hour,
                                    minute,
                                    second,
                                    milisecond);
            }

            return null;
        }
        public static SlotIsContained SlotIsContainedInSlotInt(DateTime? slotBegin, DateTime? slotEnd, DateTime? beginTime, DateTime? endTime)
        {
            if (beginTime <= slotBegin && endTime >= slotEnd)
            {
                return SlotIsContained.ExceedsContainment;
            }

            if (beginTime >= slotBegin && endTime <= slotEnd)
            {
                return SlotIsContained.Contained;
            }

            if ((beginTime < slotBegin && endTime > slotBegin) || (beginTime < slotEnd && endTime > slotEnd))
            {
                return SlotIsContained.PartiallyContained;
            }

            return SlotIsContained.NotContained;
        }
        public static void ValidateSrvOrdTypeNumberingSequenceInt(PXGraph graph, string srvOrdType)
        {
            FSSrvOrdType fsSrvOrdTypeRow = PXSelect<FSSrvOrdType,
                                           Where<
                                               FSSrvOrdType.srvOrdType, Equal<Required<FSSrvOrdType.srvOrdType>>>>
                                           .Select(graph, srvOrdType);

            Numbering numbering = PXSelect<Numbering, Where<Numbering.numberingID, Equal<Required<Numbering.numberingID>>>>.Select(graph, fsSrvOrdTypeRow?.SrvOrdNumberingID);

            if (numbering == null)
            {
                throw new PXSetPropertyException(PX.Objects.CS.Messages.NumberingIDNull);
            }

            if (numbering.UserNumbering == true)
            {
                throw new PXSetPropertyException(TX.Error.SERVICE_ORDER_TYPE_DOES_NOT_ALLOW_AUTONUMBERING, srvOrdType);
            }
        }
        #endregion

        #region Entity Event Handlers
        public PXWorkflowEventHandler<FSAppointment> OnServiceContractCleared;
        public PXWorkflowEventHandler<FSAppointment> OnServiceContractPeriodAssigned;
        // TODO: Delete in the next major release
        public PXWorkflowEventHandler<FSAppointment> OnServiceContractPeriodCleared;
        public PXWorkflowEventHandler<FSAppointment> OnRequiredServiceContractPeriodCleared;
        public PXWorkflowEventHandler<FSAppointment> OnAppointmentUnposted;
        public PXWorkflowEventHandler<FSAppointment> OnAppointmentPosted;
        #endregion

        #region Event Handlers

        #region FSServiceOrder
        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<FSServiceOrder, FSServiceOrder.branchID> e)
        {
            if (e.Row == null || AppointmentSelected.Current == null)
            {
                return;
            }

            AppointmentSelected.Cache.SetValueExt<FSAppointment.branchID>(AppointmentSelected.Current, e.Row.BranchID);
            UpdateDetailsFromBranchID(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSServiceOrder, FSServiceOrder.branchLocationID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder_BranchLocationID_FieldUpdated_Handler(this,
                                                                                  e.Args,
                                                                                  ServiceOrderTypeSelected.Current,
                                                                                  ServiceOrderRelated);
        }

        protected virtual void _(Events.FieldUpdated<FSServiceOrder, FSServiceOrder.locationID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder_LocationID_FieldUpdated_Handler(e.Cache, e.Args);

            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;
            FSSrvOrdType fsSrvOrdTypeRow = ServiceOrderTypeSelected.Current;

            SetCurrentAppointmentSalesPersonID(fsServiceOrderRow);
        }

        protected virtual void _(Events.FieldUpdated<FSServiceOrder, FSServiceOrder.contactID> e)
        {
            FSServiceOrder_ContactID_FieldUpdated_Handler(this, e.Args, ServiceOrderTypeSelected.Current);
        }

        protected virtual void _(Events.FieldUpdated<FSServiceOrder, FSServiceOrder.billCustomerID> e)
        {
            if (e.Row == null || AppointmentSelected.Current == null)
            {
                return;
            }

            var fsServiceOrderRow = e.Row;

            try
            {
                if (e.ExternalCall == true)
                    this.recalculateCuryID = true;

                AppointmentSelected.Cache.SetValueExt<FSAppointment.billCustomerID>(AppointmentSelected.Current, fsServiceOrderRow.BillCustomerID);
            }
            finally
            {
                this.recalculateCuryID = false;
            }

            FSServiceOrder_BillCustomerID_FieldUpdated_Handler(e.Cache, e.Args);

            ValidateCustomerBillingCycle(e.Cache, e.Row, AppointmentRecords.Cache, AppointmentRecords.Current, e.Row.BillCustomerID, ServiceOrderTypeSelected.Current, SetupRecord.Current, justWarn: true);

            if (SkipChangingContract == false)
            {
                AppointmentSelected.Cache.SetDefaultExt<FSAppointment.billServiceContractID>(AppointmentSelected.Current);
                AppointmentSelected.Cache.SetDefaultExt<FSAppointment.billContractPeriodID>(AppointmentSelected.Current);
            }
            SkipChangingContract = false;
        }

        protected virtual void _(Events.FieldUpdated<FSServiceOrder, FSServiceOrder.billLocationID> e)
        {
            if (AppointmentSelected.Current == null)
            {
                return;
            }

            AppointmentSelected.Cache.SetDefaultExt<FSAppointment.taxZoneID>(AppointmentSelected.Current);
            AppointmentSelected.Cache.SetDefaultExt<FSAppointment.taxCalcMode>(AppointmentSelected.Current);
        }
        #endregion

        protected virtual void _(Events.RowSelecting<FSServiceOrder> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;

            using (new PXConnectionScope())
            {
                UpdateServiceOrderUnboundFields(this, fsServiceOrderRow, BillingCycleRelated.Current, dummyGraph, AppointmentRecords.Current, DisableServiceOrderUnboundFieldCalc);

                PXResultset<FSAppointment> fsAppointmentSet = PXSelectReadonly<FSAppointment,
                                                              Where2<
                                                                  Where<
                                                                      FSAppointment.closed, Equal<True>,
                                                                      Or<FSAppointment.completed, Equal<True>>>,
                                                                  And<FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>>
                                                              .Select(dummyGraph, fsServiceOrderRow.SOID);

                FSAppointment currentAppointment = AppointmentRecords.Current;
                fsServiceOrderRow.AppointmentsCompletedOrClosedCntr = 0;
                fsServiceOrderRow.AppointmentsCompletedCntr = 0;

                foreach (FSAppointment fsAppointmentRow in fsAppointmentSet)
                {
                    if (currentAppointment == null ||
                        (currentAppointment != null
                            && currentAppointment.AppointmentID != fsAppointmentRow.AppointmentID))
                    {
                        fsServiceOrderRow.AppointmentsCompletedOrClosedCntr += 1;

                        if (fsAppointmentRow.Completed == true && fsAppointmentRow.Closed == false)
                        {
                            fsServiceOrderRow.AppointmentsCompletedCntr += 1;
                        }
                    }
                }

                if (currentAppointment != null)
                {
                    if (currentAppointment.Completed == true && currentAppointment.Closed == false)
                    {
                        fsServiceOrderRow.AppointmentsCompletedOrClosedCntr += 1;
                        fsServiceOrderRow.AppointmentsCompletedCntr += 1;
                    }
                    else if (currentAppointment.Closed == true)
                    {
                        fsServiceOrderRow.AppointmentsCompletedOrClosedCntr += 1;
                    }
                }
            }
        }

        protected virtual void _(Events.RowSelected<FSServiceOrder> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;
            PXCache cache = e.Cache;

            if (string.IsNullOrEmpty(fsServiceOrderRow.SrvOrdType))
            {
                return;
            }

            if (ContractRelatedToProject?.Current == null)
            {
                ContractRelatedToProject.Current = ContractRelatedToProject.Select(fsServiceOrderRow.ProjectID);
            }

            int appointmentCount = PXSelect<FSAppointment,
                                   Where<
                                       FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>
                                   .SelectWindowed(cache.Graph, 0, 2, fsServiceOrderRow.SOID).Count;

            FSServiceOrder_RowSelected_PartialHandler(cache.Graph,
                                                                       cache,
                                                                       fsServiceOrderRow,
                                                                       AppointmentRecords.Current,
                                                                       ServiceOrderTypeSelected.Current,
                                                                       BillingCycleRelated.Current,
                                                                       ContractRelatedToProject.Current,
                                                                       appointmentCount,
                                                                       AppointmentDetails.Select().Count,
                                                                       null,
                                                                       null,
                                                                       null,
                                                                       null,
                                                                       null,
                                                                       null,
                                                                       null);
        }

        protected virtual void _(Events.RowInserting<FSServiceOrder> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSServiceOrder> e)
        {
            if (e.Row == null)
            {
                return;
            }

            SharedFunctions.InitializeNote(e.Cache, e.Args);
        }

        protected virtual void _(Events.RowUpdating<FSServiceOrder> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSServiceOrder> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSServiceOrder> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSServiceOrder> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSServiceOrder> e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert)
            {
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;

                // SrvOrdType is key field
                if (string.IsNullOrWhiteSpace(fsServiceOrderRow.SrvOrdType))
                {
                    GraphHelper.RaiseRowPersistingException<FSAppointment.srvOrdType>(AppointmentRecords.Cache, AppointmentRecords.Current);
                }

                // Initial values from appointment
                fsServiceOrderRow.CustomerID = AppointmentRecords.Current.CustomerID;
                fsServiceOrderRow.CuryID = AppointmentRecords.Current.CuryID;
                fsServiceOrderRow.TaxZoneID = AppointmentRecords.Current.TaxZoneID;
                fsServiceOrderRow.TaxCalcMode = AppointmentRecords.Current.TaxCalcMode;
                fsServiceOrderRow.ProjectID = AppointmentRecords.Current.ProjectID;
                fsServiceOrderRow.DfltProjectTaskID = AppointmentRecords.Current.DfltProjectTaskID;
                fsServiceOrderRow.DocDesc = AppointmentRecords.Current.DocDesc;
                fsServiceOrderRow.OrderDate = AppointmentRecords.Current.ScheduledDateTimeBegin;
                fsServiceOrderRow.SalesPersonID = AppointmentRecords.Current.SalesPersonID;
                fsServiceOrderRow.Commissionable = AppointmentRecords.Current.Commissionable;
                fsServiceOrderRow.WFStageID = null;
                fsServiceOrderRow.CutOffDate = GetCutOffDate(this, fsServiceOrderRow.CBID, fsServiceOrderRow.OrderDate, fsServiceOrderRow.SrvOrdType);

                ValidateSrvOrdTypeNumberingSequence(this, fsServiceOrderRow.SrvOrdType);

                insertingServiceOrder = true;
            }
        }

        protected virtual void _(Events.RowPersisted<FSServiceOrder> e)
        {
            if (e.TranStatus == PXTranStatus.Aborted)
            {
                serviceOrderRowPersistedPassedWithStatusAbort = true;
            }
        }

        #endregion

        #region FSAppointment

        #region FieldSelecting
        #endregion
        #region FieldDefaulting

        protected virtual void _(Events.FieldDefaulting<FSAppointment, FSAppointment.billContractPeriodID> e)
        {
            if (e.Row == null || e.NewValue != null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = e.Row;
            FSContractPeriod fsContractPeriodRow = PXSelect<FSContractPeriod,
                                                   Where2<
                                                       Where<
                                                           FSContractPeriod.startPeriodDate, LessEqual<Required<FSContractPeriod.startPeriodDate>>,
                                                           And<FSContractPeriod.endPeriodDate, GreaterEqual<Required<FSContractPeriod.startPeriodDate>>>>,
                                                       And<
                                                           FSContractPeriod.serviceContractID, Equal<Current<FSAppointment.billServiceContractID>>,
                                                           And2<
                                                               Where<FSContractPeriod.status, Equal<FSContractPeriod.status.Active>,
                                                               Or<FSContractPeriod.status, Equal<FSContractPeriod.status.Pending>>>,
                                                               And<Current<FSBillingCycle.billingBy>, Equal<FSBillingCycle.billingBy.Appointment>>>>>>
                                                   .Select(this, fsAppointmentRow.ScheduledDateTimeBegin?.Date, fsAppointmentRow.ScheduledDateTimeEnd?.Date);

            e.NewValue = fsContractPeriodRow?.ContractPeriodID;
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointment, FSAppointment.scheduledDateTimeBegin> e)
        {
            e.NewValue = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointment, FSAppointment.scheduledDateTimeEnd> e)
        {
            e.NewValue = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
        }

        #endregion
        #region FieldUpdating

        // Custom declared in ctor
        protected virtual void FSAppointment_ActualDateTimeBegin_Time_FieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
        {
            if (e.Row == null || e.NewValue == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            DateTime? newTime = SharedFunctions.TryParseHandlingDateTime(cache, e.NewValue);

            if (newTime != null)
            {
                // Set the date part equal to Execution date
                e.NewValue = PXDBDateAndTimeAttribute.CombineDateTime(fsAppointmentRow.ExecutionDate, newTime);
                cache.SetValuePending(e.Row, typeof(FSAppointment.actualDateTimeBegin).Name + "headerNewTime", e.NewValue);
            }
        }

        // Custom declared in ctor
        protected virtual void FSAppointment_ActualDateTimeEnd_Time_FieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
        {
            if (e.Row == null || e.NewValue == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            DateTime? newTime = SharedFunctions.TryParseHandlingDateTime(cache, e.NewValue);

            if (newTime != null)
            {
                if (fsAppointmentRow.ActualDateTimeEnd == null)
                {
                    newTime = PXDBDateAndTimeAttribute.CombineDateTime(fsAppointmentRow.ActualDateTimeBegin, newTime);
                }
                else
                {
                    newTime = PXDBDateAndTimeAttribute.CombineDateTime(fsAppointmentRow.ActualDateTimeEnd, newTime);
                }

                e.NewValue = newTime;
                cache.SetValuePending(e.Row, typeof(FSAppointment.actualDateTimeEnd).Name + "newTime", newTime);
            }
        }

        protected virtual void _(Events.FieldUpdating<FSAppointment, FSAppointment.executionDate> e)
        {
            if (e.Row == null || e.NewValue != null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = e.Row;

            if (fsAppointmentRow.ScheduledDateTimeBegin.HasValue)
            {
                e.NewValue = fsAppointmentRow.ScheduledDateTimeBegin.Value.Date;
            }
        }

        #endregion
        #region FieldVerifying
        // This event can not be change to new format.
        protected virtual void FSAppointment_NoteFiles_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            if (e.NewValue is Guid[] && ((Guid[])e.NewValue).Length > 0 &&
                (fsAppointmentRow.CustomerSignedReport == null || fsAppointmentRow.CustomerSignedReport == Guid.Empty))
            {
                GenerateSignedReport(cache, fsAppointmentRow);
            }
        }
        #endregion
        #region FieldUpdated

        // Custom declared in ctor
        protected virtual void FSAppointment_ScheduledDateTimeBegin_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            DateTime? oldDate = (DateTime?)e.OldValue;

            if (oldDate != fsAppointmentRow.ScheduledDateTimeBegin)
            {
                UncheckUnreachedCustomerByScheduledDate(oldDate,
                                                        fsAppointmentRow.ScheduledDateTimeBegin,
                                                        fsAppointmentRow);

                // If the date part changed
                if (oldDate == null
                    || fsAppointmentRow.ScheduledDateTimeBegin == null
                    || oldDate.Value.Date != fsAppointmentRow.ScheduledDateTimeBegin.Value.Date)
                {
                    cache.SetDefaultExt<FSAppointment.executionDate>(e.Row);
                    cache.SetDefaultExt<FSAppointment.billContractPeriodID>(e.Row);

                    RefreshSalesPricesInTheWholeDocument(AppointmentDetails);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.soRefNbr> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            if (string.IsNullOrEmpty(fsAppointmentRow.SORefNbr))
            {
                if (fsAppointmentRow.SOID == null || fsAppointmentRow.SOID >= 0)
                {
                    fsAppointmentRow.SOID = null;
                    InitServiceOrderRelated(fsAppointmentRow);
                }
            }
            else
            {
                DeleteUnpersistedServiceOrderRelated(fsAppointmentRow);
                SetServiceOrderRelatedBySORefNbr(e.Cache, fsAppointmentRow);
            }

            FSAddress fsAddressInserted = ServiceOrder_Address.Cache.Inserted?.RowCast<FSAddress>().FirstOrDefault();
            FSContact fsContactInserted = ServiceOrder_Contact.Cache.Inserted?.RowCast<FSContact>().FirstOrDefault();

            if (fsAddressInserted != null && fsAddressInserted.AddressID < 0)
                ServiceOrder_Address.Delete(fsAddressInserted);

            if (fsContactInserted != null && fsContactInserted.ContactID < 0)
                ServiceOrder_Contact.Delete(fsContactInserted);


            if (IsCloningAppointment == false && IsGeneratingAppointment == false && fsAppointmentRow.SORefNbr != null)
            {
                Helper.Current = GetFsSelectorHelperInstance;

                PXResultset<FSSODet> bqlResultSet_SODetU = new PXResultset<FSSODet>();
                GetPendingLines(this, fsAppointmentRow.SOID, ref bqlResultSet_SODetU);
                InsertServiceOrderDetailsInAppointment(bqlResultSet_SODetU, AppointmentDetails.Cache);

                GetEmployeesFromServiceOrder(fsAppointmentRow);
                GetResourcesFromServiceOrder(fsAppointmentRow);

                Answers.Current = Answers.Select();
                Answers.CopyAllAttributes(AppointmentRecords.Current, ServiceOrderRelated.Current);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.executionDate> e)
        {
            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            PXCache cache = e.Cache;

            if ((DateTime?)e.OldValue != fsAppointmentRow.ExecutionDate)
            {
                if (fsAppointmentRow.ActualDateTimeBegin != null)
                {
                    // Assign the date part to ActualDateTimeBegin
                    DateTime? newBegin = PXDBDateAndTimeAttribute.CombineDateTime(fsAppointmentRow.ExecutionDate, fsAppointmentRow.ActualDateTimeBegin);

                    cache.SetValueExtIfDifferent<FSAppointment.actualDateTimeBegin>(e.Row, newBegin);
                }

                var appDetails = AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(x => x.IsPickupDelivery == false);

                foreach (FSAppointmentDet fsAppointmentDetRow in appDetails)
                {
                    UpdateWarrantyFlag(cache, fsAppointmentDetRow, AppointmentRecords.Current.ExecutionDate);

                    AppointmentDetails.Update(fsAppointmentDetRow);
                }

                CalculateLaborCosts();
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.routeDocumentID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            fsAppointmentRow.Mem_LastRouteDocumentID = (int?)e.OldValue;
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.handleManuallyScheduleTime> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            CalculateEndTimeWithLinesDuration(e.Cache, fsAppointmentRow, DateFieldType.ScheduleField);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.handleManuallyActualTime> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            // TODO: Change this to SetDefaultExt of actualDateTimeEnd
            if (ServiceOrderTypeSelected?.Current.SetTimeInHeaderBasedOnLog == true
                    && fsAppointmentRow.HandleManuallyActualTime == false)
            {
                e.Cache.SetValueExt<FSAppointment.actualDateTimeEnd>(fsAppointmentRow, fsAppointmentRow.MaxLogTimeEnd);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.actualDateTimeBegin> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            PXCache cache = e.Cache;

            var valuePending = cache.GetValuePending(e.Row, typeof(FSAppointment.actualDateTimeBegin).Name + "headerNewTime");

            if (PXCache.NotSetValue != valuePending)
            {
                DateTime? newTime = (DateTime?)valuePending;
                if (newTime != null)
                {
                    fsAppointmentRow.ActualDateTimeBegin = newTime;
                }
            }

            OnApptStartTimeChangeUpdateLogStartTime(fsAppointmentRow, ServiceOrderTypeSelected.Current, LogRecords);

            cache.SetDefaultExt<FSAppointment.actualDuration>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.actualDateTimeEnd> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            PXCache cache = e.Cache;

            var valuePending = cache.GetValuePending(e.Row, typeof(FSAppointment.actualDateTimeEnd).Name + "newTime");

            if (PXCache.NotSetValue != valuePending)
            {
                DateTime? newTime = (DateTime?)valuePending;
                if (newTime != null)
                {
                    fsAppointmentRow.ActualDateTimeEnd = newTime;
                }
            }

            if (SkipManualTimeFlagUpdate == false)
            {
                OnApptEndTimeChangeUpdateLogEndTime(fsAppointmentRow, ServiceOrderTypeSelected.Current, LogRecords);
            }

            cache.SetDefaultExt<FSAppointment.actualDuration>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.confirmed> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            #region Confirmed & UnreachedCustomer flag exlusivity
            if (fsAppointmentRow.Confirmed == true && fsAppointmentRow.UnreachedCustomer == true)
            {
                fsAppointmentRow.UnreachedCustomer = false;
            }
            #endregion
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.unreachedCustomer> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            #region Confirmed & UnreachedCustomer flag exclusivity
            if (fsAppointmentRow.UnreachedCustomer == true && fsAppointmentRow.Confirmed == true)
            {
                fsAppointmentRow.Confirmed = false;
            }
            #endregion
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.scheduledDateTimeEnd> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            UncheckUnreachedCustomerByScheduledDate((DateTime?)e.OldValue,
                                                    fsAppointmentRow.ScheduledDateTimeEnd,
                                                    fsAppointmentRow);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.billContractPeriodID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = e.Row;

            if (fsAppointmentRow.BillServiceContractID != null)
            {
                if (fsAppointmentRow.BillContractPeriodID != null)
                {
                    FSAppointment.Events.Select(ev => ev.ServiceContractPeriodAssigned).FireOn(this, e.Row);
                }
                else
                {
                    FSAppointment.Events.Select(ev => ev.RequiredServiceContractPeriodCleared).FireOn(this, e.Row);
                }
            }

            if ((int?)e.OldValue == fsAppointmentRow.BillContractPeriodID)
            {
                return;
            }

            StandardContractPeriod.Current = StandardContractPeriod.Select();
            StandardContractPeriodDetail.Current = StandardContractPeriodDetail.Select();

            var appDetails = AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(x => x.IsPickupDelivery == false);

            foreach (FSAppointmentDet fsAppointmentDetRow in appDetails)
            {
                AppointmentDetails.Cache.SetDefaultExt<FSAppointmentDet.contractRelated>(fsAppointmentDetRow);
                AppointmentDetails.Cache.Update(fsAppointmentDetRow);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.billServiceContractID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            if (fsAppointmentRow.BillServiceContractID == null)
            {
                fsAppointmentRow.BillContractPeriodID = null;
                FSAppointment.Events.Select(ev => ev.ServiceContractCleared).FireOn(this, e.Row);
                return;
            }
            
            e.Cache.SetDefaultExt<FSAppointment.billContractPeriodID>(fsAppointmentRow);

            FSServiceContract fsServiceContractRow = FSServiceContract.PK.Find(e.Cache.Graph, fsAppointmentRow.BillServiceContractID);

            // TODO: review this if and fix the necessary code to delete it!!!
            if (fsServiceContractRow != null && fsServiceContractRow.ServiceContractID != null)
            {
                SkipChangingContract = true;

                FSServiceOrder serviceOrder = ServiceOrderRelated.Current;

                if (fsServiceContractRow.BillCustomerID != serviceOrder.BillCustomerID
                    || fsServiceContractRow.BillLocationID != serviceOrder.BillLocationID)
                {
                    ServiceOrderRelated.Cache.SetValueExt<FSServiceOrder.billCustomerID>(serviceOrder, fsServiceContractRow.BillCustomerID);
                    ServiceOrderRelated.Cache.SetValueExt<FSServiceOrder.billLocationID>(serviceOrder, fsServiceContractRow.BillLocationID);
                }
            }

            if (IsCopyPasteContext == false
                && fsServiceContractRow != null)
            {
                e.Cache.SetValueExt<FSAppointment.projectID>(fsAppointmentRow, fsServiceContractRow.ProjectID);
                e.Cache.SetValueExt<FSAppointment.dfltProjectTaskID>(fsAppointmentRow, fsServiceContractRow.DfltProjectTaskID);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.curyBillableLineTotal> e)
        {
            if (e.Row != null && ServiceOrderTypeSelected.Current != null && ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.PROJECTS)
                e.Cache.SetValueExt<FSAppointment.curyDocTotal>(e.Row, e.Row.CuryBillableLineTotal + e.Row.CuryLogBillableTranAmountTotal - e.Row.CuryDiscTot);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.curyLogBillableTranAmountTotal> e)
        {
            if (e.Row != null && ServiceOrderTypeSelected.Current != null && ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.PROJECTS)
                e.Cache.SetValueExt<FSAppointment.curyDocTotal>(e.Row, e.Row.CuryBillableLineTotal + e.Row.CuryLogBillableTranAmountTotal - e.Row.CuryDiscTot);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.curyDiscTot> e)
        {
            if (e.Row != null && ServiceOrderTypeSelected.Current != null && ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.PROJECTS)
                e.Cache.SetValueExt<FSAppointment.curyDocTotal>(e.Row, e.Row.CuryBillableLineTotal + e.Row.CuryLogBillableTranAmountTotal - e.Row.CuryDiscTot);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.minLogTimeBegin> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            if (ServiceOrderTypeSelected?.Current.SetTimeInHeaderBasedOnLog == true)
            {
                e.Cache.SetValueExt<FSAppointment.actualDateTimeBegin>(fsAppointmentRow, fsAppointmentRow.MinLogTimeBegin);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.maxLogTimeEnd> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            // TODO: Change this to FieldDefaulting of actualDateTimeEnd
            if (ServiceOrderTypeSelected?.Current.SetTimeInHeaderBasedOnLog == true
                && fsAppointmentRow.HandleManuallyActualTime == false)
            {
                e.Cache.SetValueExt<FSAppointment.actualDateTimeEnd>(fsAppointmentRow, fsAppointmentRow.MaxLogTimeEnd);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.projectID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            UpdateDetailsFromProjectID(e.Row.ProjectID);
            ContractRelatedToProject.Current = ContractRelatedToProject.Select(e.Row.ProjectID);
        }

        // TODO: delete this event for the next major version
        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.status> e)
        {
        }

        protected virtual void _(Events.FieldUpdated<FSAppointment, FSAppointment.customerID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            PXCache cache = e.Cache;

            if (cache.Graph.IsCopyPasteContext == true)
            {
                AppointmentDetails.Cache.AllowInsert = true;
                AppointmentDetails.Cache.AllowUpdate = true;
            }

            FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.Current;

            if (fsServiceOrderRow != null && fsServiceOrderRow.CustomerID != e.Row.CustomerID)
            {
                ServiceOrderRelated.Cache.SetValueExtIfDifferent<FSServiceOrder.customerID>(fsServiceOrderRow, e.Row.CustomerID);

                PXResultset<FSAppointment> bqlResultSet = PXSelect<FSAppointment,
                                                          Where<
                                                                FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>
                                                          .Select(this, fsServiceOrderRow.SOID);

                FSServiceOrder_CustomerID_FieldUpdated_Handler(ServiceOrderRelated.Cache,
                                                                                fsServiceOrderRow,
                                                                                ServiceOrderTypeSelected.Current,
                                                                                null,
                                                                                AppointmentDetails,
                                                                                bqlResultSet,
                                                                                (int?)e.Args.OldValue,
                                                                                AppointmentRecords.Current.ScheduledDateTimeBegin,
                                                                                allowCustomerChange: false,
                                                                                customerRow: TaxCustomer.Current);

                SetCurrentAppointmentSalesPersonID(fsServiceOrderRow);
            }
        }
        #endregion

        protected virtual void _(Events.RowSelecting<FSAppointment> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            using (new PXConnectionScope())
            {
                UpdateServiceOrderUnboundFields(this, ServiceOrderRelated.Current, BillingCycleRelated.Current, dummyGraph, fsAppointmentRow, DisableServiceOrderUnboundFieldCalc);
                if (AppointmentSelected.Current != null)
                {
                    AppointmentSelected.Current.AppCompletedBillableTotal = fsAppointmentRow.AppCompletedBillableTotal;
                }
            }
        }

        protected virtual void _(Events.RowSelected<FSAppointment> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            PXCache cache = e.Cache;

            LoadServiceOrderRelated(fsAppointmentRow);

            if (ServiceOrderDetails?.Current == null)
            {
                ServiceOrderDetails.Current = ServiceOrderDetails.Select();
            }

            if (PostInfoDetails?.Current == null)
            {
                PostInfoDetails.Current = PostInfoDetails.Select();
            }

            if (cache.GetStatus(fsAppointmentRow) == PXEntryStatus.Updated && fsAppointmentRow.AppointmentID < 0)
            {
                //// TODO: AC-142850 Review this. When the Row is marked as updated the Autonumber is not setting the new number.
                cache.SetStatus(fsAppointmentRow, PXEntryStatus.Inserted);
            }

            PXDefaultAttribute.SetPersistingCheck<FSAppointment.soRefNbr>(cache, fsAppointmentRow, PXPersistingCheck.Nothing);
            PXDefaultAttribute.SetPersistingCheck<FSAppointment.soRefNbr>(cache, fsAppointmentRow, PXPersistingCheck.Nothing);

            EnableDisable_Document(fsAppointmentRow,
                    ServiceOrderRelated.Current,
                    SetupRecord.Current,
                    BillingCycleRelated.Current,
                    ServiceOrderTypeSelected.Current,
                    this.SkipTimeCardUpdate,
                    this.AppointmentRecords.Current.IsCalledFromQuickProcess);

            PXUIFieldAttribute.SetVisible<FSAppointmentDet.equipmentAction>(AppointmentDetails.Cache, null, ServiceOrderTypeSelected.Current?.PostToSOSIPM == true);

            //The IsRouteAppoinment flag shows/hides the delivery notes tab
            if (ServiceOrderTypeSelected != null && ServiceOrderTypeSelected.Current != null)
            {
                fsAppointmentRow.IsRouteAppoinment = ServiceOrderTypeSelected.Current.Behavior == FSSrvOrdType.behavior.Values.RouteAppointment;
            }
            else
            {
                fsAppointmentRow.IsRouteAppoinment = false;
            }

            if (fsAppointmentRow != null
                    && ServiceOrderRelated.Current != null
                        && fsAppointmentRow.ExecutionDate > ServiceOrderRelated.Current.SLAETA)
            {
                cache.RaiseExceptionHandling<FSAppointment.executionDate>(fsAppointmentRow, 
                                                                          fsAppointmentRow.ExecutionDate, 
                                                                          new PXSetPropertyException(TX.Warning.ACTUAL_DATE_AFTER_SLA, 
                                                                                                     PXErrorLevel.Warning));
            }

            if (fsAppointmentRow.WaitingForParts == true
                    && fsAppointmentRow.InProcess == true)
            {
                cache.RaiseExceptionHandling<FSAppointment.waitingForParts>(fsAppointmentRow,
                                                                            fsAppointmentRow.WaitingForParts,
                                                                            new PXSetPropertyException(TX.Warning.WAITING_FOR_PARTS,
                                                                                                       PXErrorLevel.Warning));
            }

            if (fsAppointmentRow.Finished == false
                    && fsAppointmentRow.Completed == true)
            {
                cache.RaiseExceptionHandling<FSAppointment.finished>(fsAppointmentRow,
                                                                     fsAppointmentRow.Finished,
                                                                     new PXSetPropertyException(TX.Warning.APPOINTMENT_WAS_NOT_FINISHED,
                                                                                                PXErrorLevel.Warning));
            }

            HideRooms(fsAppointmentRow, SetupRecord?.Current);
            HideOrShowTabs(fsAppointmentRow);
            HideOrShowTimeCardsIntegration(cache, fsAppointmentRow);
            HideOrShowRouteInfo(fsAppointmentRow);

            HidePrepayments(Adjustments.View, ServiceOrderRelated.Cache, ServiceOrderRelated.Current, fsAppointmentRow, ServiceOrderTypeSelected.Current);

            if (RouteSetupRecord.Current == null)
            {
                RouteSetupRecord.Current = RouteSetupRecord.Select();
            }

            Caches[typeof(FSContact)].AllowUpdate = ServiceOrderRelated.Current?.AllowOverrideContactAddress == true && Caches[typeof(FSContact)].AllowUpdate == true;
            Caches[typeof(FSAddress)].AllowUpdate = ServiceOrderRelated.Current?.AllowOverrideContactAddress == true && Caches[typeof(FSContact)].AllowUpdate == true;

            PXUIFieldAttribute.SetEnabled<FSManufacturer.allowOverrideContactAddress>(ServiceOrderRelated.Cache, ServiceOrderRelated.Current, !(ServiceOrderRelated.Current?.CustomerID == null && ServiceOrderRelated.Current?.ContactID == null));

            PXUIFieldAttribute.SetVisible<FSAppointmentDet.pickupDeliveryAppLineRef>(AppointmentDetails.Cache, null, fsAppointmentRow.IsRouteAppoinment == true);
            PXUIFieldAttribute.SetVisible<FSAppointmentDet.pickupDeliveryServiceID>(AppointmentDetails.Cache, null, fsAppointmentRow.IsRouteAppoinment == true);
            PXUIFieldAttribute.SetVisible<FSAppointmentDet.serviceType>(AppointmentDetails.Cache, null, fsAppointmentRow.IsRouteAppoinment == true);

            bool inventoryAndEquipmentModuleEnabled = PXAccess.FeatureInstalled<FeaturesSet.inventory>()
                                                            && PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>();

            PXUIFieldAttribute.SetVisibility<FSAppointmentDet.comment>(AppointmentDetails.Cache, null, inventoryAndEquipmentModuleEnabled ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
            PXUIFieldAttribute.SetVisibility<FSAppointmentDet.equipmentAction>(AppointmentDetails.Cache, null, inventoryAndEquipmentModuleEnabled ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
            PXUIFieldAttribute.SetVisibility<FSAppointmentDet.newTargetEquipmentLineNbr>(AppointmentDetails.Cache, null, inventoryAndEquipmentModuleEnabled ? PXUIVisibility.Visible : PXUIVisibility.Invisible);

            bool showMarkForPO = ShouldShowMarkForPOFields(AllocationSOOrderTypeSelected.Current);
            PXUIFieldAttribute.SetVisible<FSAppointmentDet.enablePO>(AppointmentDetails.Cache, null, showMarkForPO);
            PXUIFieldAttribute.SetVisible<FSApptLineSplit.pOCreate>(Splits.Cache, null, showMarkForPO);
        }

        protected virtual void _(Events.RowInserting<FSAppointment> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSAppointment> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            if (string.IsNullOrEmpty(fsAppointmentRow.SrvOrdType) == false)
            {
                InitServiceOrderRelated(fsAppointmentRow);
                SharedFunctions.InitializeNote(e.Cache, e.Args);
            }

            if (fsAppointmentRow.AppointmentID < 0)
            {
                UpdateServiceOrderUnboundFields(this, ServiceOrderRelated.Current, BillingCycleRelated.Current, dummyGraph, fsAppointmentRow, DisableServiceOrderUnboundFieldCalc);
            }

            fsAppointmentRow.MustUpdateServiceOrder = true;

            RecalculateAreActualFieldsActive(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowUpdating<FSAppointment> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (this.IsMobile == true)
            {
                FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
                FSAppointment newFSAppointmentRow = (FSAppointment)e.NewRow;

                if (fsAppointmentRow.SrvOrdType != newFSAppointmentRow.SrvOrdType &&
                    (fsAppointmentRow.SOID < 0 ||
                        fsAppointmentRow.SOID == null))
                {
                    if (ServiceOrderRelated.Current != null)
                    {
                        ServiceOrderRelated.Delete(ServiceOrderRelated.Current);
                    }

                    fsAppointmentRow.SOID = null;
                    newFSAppointmentRow.SOID = null;
                    InitServiceOrderRelated(newFSAppointmentRow);
                }
            }
        }

        protected virtual void _(Events.RowUpdated<FSAppointment> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (!e.Cache.ObjectsEqual<FSAppointment.srvOrdType>(e.Row, e.OldRow) && e.OldRow.SrvOrdType == null)
            {
                InitServiceOrderRelated(e.Row);
            }

            CalculateEndTimeWithLinesDuration(e.Cache, e.Row, DateFieldType.ScheduleField);

            RecalculateAreActualFieldsActive(e.Cache, e.Row);

            if (UpdatingItemLinesBecauseOfDocStatusChange != true)
            {
                try
                {
                    UpdatingItemLinesBecauseOfDocStatusChange = true;

                    if (IsItemLineUpdateRequired(e.Cache, e.Row, e.OldRow) == true)
                    {
                        UpdateItemLinesBecauseOfDocStatusChange();
                    }
                }
                finally
                {
                    UpdatingItemLinesBecauseOfDocStatusChange = false;
                }
            }
        }

        protected virtual void _(Events.RowDeleting<FSAppointment> e)
        {
            if (AppointmentRecords == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = AppointmentRecords.Current;

            if (fsAppointmentRow.APBillLineCntr > 0
                    && Accessinfo.ScreenID == SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT)
                    && AppointmentRecords.Ask(TX.Warning.FSDocumentLinkedToAPLines, MessageButtons.OKCancel) != WebDialogResult.OK)
            {
                e.Cancel = true;
            }

            PXResultset<FSAppointment> bqlResultSet = PXSelect<FSAppointment,
                                                      Where<
                                                          FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>,
                                                          And<FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>>>>
                                                      .Select(this, fsAppointmentRow.SOID, fsAppointmentRow.AppointmentID);

            if (bqlResultSet.Count > 0 && !CanDeleteServiceOrder(this, ServiceOrderRelated.Current))
            {
                UpdateSOStatusOnAppointmentDeleting = GetFinalServiceOrderStatus(ServiceOrderRelated.Current, fsAppointmentRow);
            }
        }

        protected virtual void _(Events.RowDeleted<FSAppointment> e)
        {
            if (e.Row == null)
            {
                return;
            }

            DeleteUnpersistedServiceOrderRelated(e.Row);
            e.Row.MustUpdateServiceOrder = true;
        }

        protected virtual void _(Events.RowPersisting<FSAppointment> e)
        {
            PXCache cache = e.Cache;

            if (CanExecuteAppointmentRowPersisting(cache, e.Args) == false)
            {
                return;
            }

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;
            FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.Current;

            if (fsServiceOrderRow != null) 
            {
                bool isEnabledCustomerID = AllowEnableCustomerID(fsServiceOrderRow);

                PXDefaultAttribute.SetPersistingCheck<FSAppointment.customerID>(AppointmentRecords.Cache,
                                                                                 fsAppointmentRow,
                                                                                 fsServiceOrderRow.BAccountRequired != false && isEnabledCustomerID ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
            }

            if (e.Operation == PXDBOperation.Insert)
            {
                AutoConfirm(fsAppointmentRow);
            }

            if (e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update)
            {
                if (AreThereAnyEmployees())
                {
                    ValidateLicenses<FSAppointment.docDesc>(cache, e.Row);
                    ValidateSkills<FSAppointment.docDesc>(cache, e.Row);
                    ValidateGeoZones<FSAppointment.docDesc>(cache, e.Row);
                }

                ValidateRoom(fsAppointmentRow);
                ValidateMaxAppointmentQty(fsAppointmentRow);
                ValidateWeekCode(fsAppointmentRow);

                ValidateCustomerBillingCycle(ServiceOrderRelated.Cache, fsServiceOrderRow, e.Cache, e.Row, fsServiceOrderRow.BillCustomerID, ServiceOrderTypeSelected.Current, SetupRecord.Current, justWarn: false);

                if (UpdateBillingInfo(ServiceOrderRelated.Cache, fsServiceOrderRow) == true)
                {
                    // In AppointmentEntry we need to cancel the persist operation
                    // because the persist of FSServiceOrder was already done at this moment.
                    // So we need a second persist to persist the changes made by UpdateBillingInfo
                    throw new PXSetPropertyException(TX.Warning.SaveCanceledBecauseTheBillingInfoWasUpdated, PXErrorLevel.Error);
                }

                if (UpdateServiceOrder(fsAppointmentRow, this, e.Row, e.Operation, null) == false)
                {
                    return;
                }

                //Validate Execution Day
                if (fsAppointmentRow.RouteID != null)
                {
                    FSRoute fsRouteRow = FSRoute.PK.Find(this, fsAppointmentRow.RouteID);
                    DateTime? dummyDateTime = DateTime.Now;
                    bool runValidation = false;

                    if (e.Operation == PXDBOperation.Update)
                    {
                        int? oldRouteID = (int?)cache.GetValueOriginal<FSAppointment.routeID>(fsAppointmentRow);
                        runValidation = oldRouteID != fsAppointmentRow.RouteID;
                    }

                    if (runValidation || e.Operation == PXDBOperation.Insert)
                    {
                        SharedFunctions.ValidateExecutionDay(fsRouteRow, fsAppointmentRow.ScheduledDateTimeBegin.Value.DayOfWeek, ref dummyDateTime);
                    }
                }

                CheckActualDateTimes(cache, fsAppointmentRow);
                CheckMinMaxActualDateTimes(cache, fsAppointmentRow);
                CheckScheduledDateTimes(cache, fsAppointmentRow);

                if (string.IsNullOrEmpty(fsAppointmentRow.DocDesc))
                {
                    FillDocDesc(fsAppointmentRow);
                }

                CalculateEndTimeWithLinesDuration(cache, fsAppointmentRow, DateFieldType.ScheduleField);

                //When updating from Service Order the ServiceOrderRelated view contains not saved BranchID
                if (SkipServiceOrderUpdate == false)
                {
                    fsAppointmentRow.BranchID = ServiceOrderRelated.Current?.BranchID;
                }
                SetTimeRegister(fsAppointmentRow, ServiceOrderTypeSelected.Current, e.Operation);

                ValidateRoomAvailability(cache, fsAppointmentRow);
                ValidateEmployeeAvailability<FSAppointment.docDesc>(fsAppointmentRow, cache, e.Row);

                TimeCardHelper.CheckTimeCardAppointmentApprovalsAndComplete(this, cache, fsAppointmentRow);

                if (SkipTimeCardUpdate == false)
                {
                    var deleteReleatedTimeActivity = new List<FSAppointmentLog>();
                    var createReleatedTimeActivity = new List<FSAppointmentLog>();

                    if (TimeCardHelper.IsTheTimeCardIntegrationEnabled(this))
                    {
                        this.HandleServiceLineStatusChange(ref deleteReleatedTimeActivity, ref createReleatedTimeActivity);
                    }

                    InsertUpdateDeleteTimeActivities(cache,
                                                    fsAppointmentRow,
                                                    fsServiceOrderRow,
                                                    deleteReleatedTimeActivity,
                                                    createReleatedTimeActivity);
                }

                bool updateCutOffDate = false;

                if (e.Operation == PXDBOperation.Insert)
                {
                    //Appointment generation by schedule
                    if (ServiceOrderTypeSelected.Current.Behavior == FSSrvOrdType.behavior.Values.RouteAppointment
                            && fsAppointmentRow.ScheduleID != null
                                && fsAppointmentRow.RouteID == null)
                    {
                        SetScheduleTimesByContract(fsAppointmentRow);
                    }

                    if (ServiceOrderRelated.Current.ScheduleID != null || ServiceOrderRelated.Current.ServiceContractID != null)
                    {
                        fsAppointmentRow.ScheduleID = ServiceOrderRelated.Current.ScheduleID;
                        fsAppointmentRow.ServiceContractID = ServiceOrderRelated.Current.ServiceContractID;
                    }

                    updateCutOffDate = true;
                }

                if (e.Operation == PXDBOperation.Update)
                {
                    if ((DateTime?)cache.GetValueOriginal<FSAppointment.actualDateTimeEnd>(fsAppointmentRow) != fsAppointmentRow.ActualDateTimeEnd)
                    {
                        updateCutOffDate = true;
                    }
                }

                if (updateCutOffDate)
                {
                    fsAppointmentRow.CutOffDate = GetCutOffDate(this, ServiceOrderRelated.Current.CBID, fsAppointmentRow.ActualDateTimeEnd, fsAppointmentRow.SrvOrdType);
                }

                if (AppointmentRecords.Cache.GetStatus(fsAppointmentRow) == PXEntryStatus.Updated)
                {
                    string lastStatus = (string)AppointmentRecords.Cache.GetValueOriginal<FSAppointment.status>(fsAppointmentRow);
                    if (lastStatus != fsAppointmentRow.Status)
                    {
                        AppointmentRecords.Cache.AllowUpdate = true;
                        ServiceOrderRelated.Cache.AllowUpdate = true;
                    }
                }

                UpdatePendingPostFlags(e.Cache, AppointmentDetails, PostInfoDetails, fsAppointmentRow, ServiceOrderRelated.Current, ServiceOrderTypeSelected.Current);

                ValidatePrimaryDriver();
            }

            if ((e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update || e.Operation == PXDBOperation.Delete) && AvoidCalculateRouteStats == false)
            {
                int? routeIDOriginal = (int?)cache.GetValueOriginal<FSAppointment.routeID>(fsAppointmentRow);
                int? routeID = (int?)cache.GetValue<FSAppointment.routeID>(fsAppointmentRow);
                int? routePositionOriginal = (int?)cache.GetValueOriginal<FSAppointment.routePosition>(fsAppointmentRow);
                int? routePosition = (int?)cache.GetValue<FSAppointment.routePosition>(fsAppointmentRow);
                int? routeDocumentOriginal = (int?)cache.GetValueOriginal<FSAppointment.routeDocumentID>(fsAppointmentRow);
                int? routeDocument = (int?)cache.GetValue<FSAppointment.routeDocumentID>(fsAppointmentRow);
                int? estimatedTotalDurationOriginal = (int?)cache.GetValueOriginal<FSAppointment.estimatedDurationTotal>(fsAppointmentRow);
                int? estimatedTotalDuration = (int?)cache.GetValue<FSAppointment.estimatedDurationTotal>(fsAppointmentRow);
                DateTime? scheduleDateTimeBegin = (DateTime?)cache.GetValue<FSAppointment.scheduledDateTimeBegin>(fsAppointmentRow);
                DateTime? scheduleDateTimeBeginOriginal = (DateTime?)cache.GetValueOriginal<FSAppointment.scheduledDateTimeBegin>(fsAppointmentRow);
                DateTime? scheduleDateTimeEnd = (DateTime?)cache.GetValue<FSAppointment.scheduledDateTimeEnd>(fsAppointmentRow);
                DateTime? scheduleDateTimeEndOriginal = (DateTime?)cache.GetValueOriginal<FSAppointment.scheduledDateTimeEnd>(fsAppointmentRow);

                if (routeIDOriginal != routeID
                    || routePositionOriginal != routePosition
                    || routeDocumentOriginal != routeDocument
                    || scheduleDateTimeBeginOriginal != scheduleDateTimeBegin
                    || scheduleDateTimeEndOriginal != scheduleDateTimeEnd
                    || estimatedTotalDurationOriginal != estimatedTotalDuration
                    || (e.Operation == PXDBOperation.Delete && routeIDOriginal != null))
                {
                    NeedRecalculateRouteStats = true;
                }
            }

            if (e.Operation == PXDBOperation.Insert)
            {
                SharedFunctions.CopyNotesAndFiles(cache,
                                                  ServiceOrderTypeSelected.Current,
                                                  fsAppointmentRow,
                                                  ServiceOrderRelated.Current.CustomerID,
                                                  ServiceOrderRelated.Current.LocationID);
            }

            if ((e.Operation & PXDBOperation.Command) != PXDBOperation.Delete)
            {
                if (RetakeGeoLocation == true)
                {
                    fsAppointmentRow.ROOptimizationStatus = ID.Status_ROOptimization.NOT_OPTIMIZED;

                    if (fsAppointmentRow.MapLatitude == null
                        || fsAppointmentRow.MapLongitude == null)
                    {
                        RetakeGeoLocation = false;
                        ResetLatLong(ServiceOrderTypeSelected.Current);

                        if (ServiceOrder_Address.Current != null
                            && ServiceOrder_Address.Current.AddressID < 0
                            && ServiceOrderRelated.Current != null
                            && ServiceOrderRelated.Current.ServiceOrderAddressID != ServiceOrder_Address.Current.AddressID)
                        {
                            ServiceOrder_Address.Current = ServiceOrder_Address.Select();
                        }

                        SetGeoLocation(ServiceOrder_Address.Current, ServiceOrderTypeSelected.Current);
                    }
                }
            }

            if (e.Row != null && e.Operation != PXDBOperation.Delete)
            {
                ValidateDuplicateLineNbr(null, AppointmentDetails);

                PXSetPropertyException apptDetException = null;

                foreach (FSAppointmentDet apptDet in AppointmentDetails.Select())
                {
                    var ex = ValidateItemLineStatus(AppointmentDetails.Cache, apptDet, AppointmentRecords.Current);
                    if (ex != null && apptDetException == null)
                    {
                        apptDetException = ex;
                    }
                }

                PXSetPropertyException logException = null;

                foreach (FSAppointmentLog log in LogRecords.Select())
                {
                    var ex = ValidateLogStatus(LogRecords.Cache, log, AppointmentRecords.Current);
                    if (ex != null && logException == null)
                    {
                        logException = ex;
                    }
                }

                if (apptDetException != null)
                {
                    throw new PXException(apptDetException?.Message);
                }
                else if (logException != null)
                {
                    throw new PXException(logException?.Message);
                }
            }
        }

        protected virtual void _(Events.RowPersisted<FSAppointment> e)
        {
            PXCache cache = e.Cache;

            RestoreOriginalValues(cache, e.Args);

            FSAppointment fsAppointmentRow = (FSAppointment)e.Row;

            if (e.TranStatus == PXTranStatus.Completed && e.Operation == PXDBOperation.Delete)
            {
                GetServiceOrderEntryGraph(false).ClearFSDocExpenseReceipts(fsAppointmentRow.NoteID);
            }

            if (e.TranStatus == PXTranStatus.Completed)
            {
                if (e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update || e.Operation == PXDBOperation.Delete)
                {
                    //Route generation or assignment
                    if (fsAppointmentRow.RouteID != null && (fsAppointmentRow.RouteDocumentID == null || fsAppointmentRow.RoutePosition == null))
                    {
                        SetAppointmentRouteInfo(cache, fsAppointmentRow, ServiceOrderRelated.Current);
                    }

                    if (e.Operation != PXDBOperation.Delete)
                    {
                        GenerateSignedReport(cache, fsAppointmentRow);
                    }
                }

                if (ServiceOrderTypeSelected.Current != null && ServiceOrderTypeSelected.Current.Behavior == FSSrvOrdType.behavior.Values.RouteAppointment)
                {
                    if (NeedRecalculateRouteStats)
                    {
                        NeedRecalculateRouteStats = false;

                        bool singleStatsOnly = (RouteSetupRecord.Current != null
                                                    && RouteSetupRecord.Current.AutoCalculateRouteStats == false)
                                                        || CalculateGoogleStats == false;

                        CalculateRouteStats(fsAppointmentRow, SetupRecord.Current.MapApiKey, singleStatsOnly);
                    }
                }
            }

            if (e.Operation == PXDBOperation.Delete && e.TranStatus == PXTranStatus.Completed)
            {
                bool serviceOrderDeleted = false;

                if (ServiceOrderRelated.Current == null)
                {
                    throw new PXException(TX.Error.SERVICE_ORDER_SELECTED_IS_NULL);
                }

                if (ServiceOrderRelated.Current.CreatedByScreenID == fsAppointmentRow.CreatedByScreenID
                        && (fsAppointmentRow.CreatedByScreenID == ID.ScreenID.APPOINTMENT
                            || fsAppointmentRow.CreatedByScreenID == ID.ScreenID.GENERATE_SERVICE_CONTRACT_APPOINTMENT
                            || fsAppointmentRow.CreatedByScreenID == ID.ScreenID.ROUTE_DOCUMENT_DETAIL
                            || fsAppointmentRow.CreatedByScreenID == ID.ScreenID.WRKPROCESS))
                {
                    PXResultset<FSAppointment> bqlResultSet = PXSelect<FSAppointment,
                                                              Where<
                                                                  FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>
                                                              .Select(this, fsAppointmentRow.SOID);

                    if (bqlResultSet.Count == 0 && CanDeleteServiceOrder(this, ServiceOrderRelated.Current))
                    {
                        DeleteServiceOrder(ServiceOrderRelated.Current, GetServiceOrderEntryGraph(true));
                        serviceOrderDeleted = true;
                    }
                }

                if (serviceOrderDeleted == false)
                {
                    ClearPrepayment(fsAppointmentRow);

                    if (UpdateServiceOrder(fsAppointmentRow, this, e.Row, e.Operation, e.TranStatus) == false)
                    {
                        throw PXRowPersistedException.PreserveStack(CatchedServiceOrderUpdateException);
                    }

                    if (string.IsNullOrEmpty(UpdateSOStatusOnAppointmentDeleting) == false)
                    {
                        SetLatestServiceOrderStatusBaseOnAppointmentStatus(ServiceOrderRelated.Current, UpdateSOStatusOnAppointmentDeleting);
                        UpdateSOStatusOnAppointmentDeleting = string.Empty;
                    }
                }
            }

            if (e.TranStatus == PXTranStatus.Open)
            {
                if (updateContractPeriod == true && fsAppointmentRow.BillContractPeriodID != null)
                {
                    int multSign = (fsAppointmentRow.Closed == true || fsAppointmentRow.CloseActionRunning == true) && fsAppointmentRow.UnCloseActionRunning == false ? 1 : -1;
                    FSServiceContract currentStandardContract = StandardContractRelated.Current;
                    if (currentStandardContract != null && currentStandardContract.RecordType == ID.RecordType_ServiceContract.SERVICE_CONTRACT)
                    {
                        ServiceContractEntry graphServiceContract = PXGraph.CreateInstance<ServiceContractEntry>();
                        graphServiceContract.ServiceContractRecords.Current = graphServiceContract.ServiceContractRecords.Search<FSServiceContract.serviceContractID>(fsAppointmentRow.BillServiceContractID, ServiceOrderRelated.Current.BillCustomerID);
                        graphServiceContract.ContractPeriodFilter.Cache.SetDefaultExt<FSContractPeriodFilter.contractPeriodID>(graphServiceContract.ContractPeriodFilter.Current);

                        if (graphServiceContract.ContractPeriodFilter.Current != null
                                && graphServiceContract.ContractPeriodFilter.Current.ContractPeriodID != fsAppointmentRow.BillContractPeriodID)
                        {
                            graphServiceContract.ContractPeriodFilter.Cache.SetValueExt<FSContractPeriodFilter.contractPeriodID>(graphServiceContract.ContractPeriodFilter.Current, fsAppointmentRow.BillContractPeriodID);
                        }

                        FSContractPeriodDet fsContractPeriodDetRow;
                        decimal? usedQty = 0;
                        int? usedTime = 0;

                        var serviceLines = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                             .Where(x => x.IsService
                                                                      && x.ContractRelated == true
                                                                      && x.IsCanceledNotPerformed != true);

                        foreach (FSAppointmentDet fsAppointmentDetRow in serviceLines)
                        {
                            fsContractPeriodDetRow = graphServiceContract.ContractPeriodDetRecords.Search<FSContractPeriodDet.inventoryID,
                                                                                                          FSContractPeriodDet.SMequipmentID,
                                                                                                          FSContractPeriodDet.billingRule>
                                                                                                          (fsAppointmentDetRow.InventoryID,
                                                                                                           fsAppointmentDetRow.SMEquipmentID,
                                                                                                           fsAppointmentDetRow.BillingRule)
                                                                                                  .AsEnumerable()
                                                                                                  .FirstOrDefault();

                            StandardContractPeriodDetail.Cache.Clear();
                            StandardContractPeriodDetail.Cache.ClearQueryCacheObsolete();
                            StandardContractPeriodDetail.View.Clear();
                            StandardContractPeriodDetail.Select();

                            if (fsContractPeriodDetRow != null)
                            {
                                usedQty = fsContractPeriodDetRow.UsedQty + (multSign * fsAppointmentDetRow.CoveredQty)
                                            + (multSign * fsAppointmentDetRow.ExtraUsageQty);

                                usedTime = fsContractPeriodDetRow.UsedTime + (int?)(multSign * fsAppointmentDetRow.CoveredQty * 60)
                                            + (int?)(multSign * fsAppointmentDetRow.ExtraUsageQty * 60);

                                fsContractPeriodDetRow.UsedQty = fsContractPeriodDetRow.BillingRule == ID.BillingRule.FLAT_RATE ? usedQty : 0m;
                                fsContractPeriodDetRow.UsedTime = fsContractPeriodDetRow.BillingRule == ID.BillingRule.TIME ? usedTime : 0;
                            }

                            graphServiceContract.ContractPeriodDetRecords.Update(fsContractPeriodDetRow);
                        }

                        graphServiceContract.Save.PressButton();
                    }
                    else if (currentStandardContract != null && currentStandardContract.RecordType == ID.RecordType_ServiceContract.ROUTE_SERVICE_CONTRACT)
                    {
                        RouteServiceContractEntry graphRouteServiceContractEntry = PXGraph.CreateInstance<RouteServiceContractEntry>();
                        graphRouteServiceContractEntry.ServiceContractRecords.Current = graphRouteServiceContractEntry.ServiceContractRecords.Search<FSServiceContract.serviceContractID>(fsAppointmentRow.BillServiceContractID, ServiceOrderRelated.Current.BillCustomerID);
                        graphRouteServiceContractEntry.ContractPeriodFilter.Cache.SetDefaultExt<FSContractPeriodFilter.contractPeriodID>(graphRouteServiceContractEntry.ContractPeriodFilter.Current);

                        FSContractPeriodDet fsContractPeriodDetRow;
                        decimal? usedQty = 0;
                        int? usedTime = 0;

                        var serviceLines = AppointmentDetails.Select().RowCast<FSAppointmentDet>()
                                                             .Where(x => x.IsService
                                                                      && x.ContractRelated == true
                                                                      && x.IsCanceledNotPerformed != true);

                        foreach (FSAppointmentDet fsAppointmentDetRow in serviceLines)
                        {
                            fsContractPeriodDetRow = graphRouteServiceContractEntry.ContractPeriodDetRecords.Search<FSContractPeriodDet.inventoryID,
                                                                                                                    FSContractPeriodDet.SMequipmentID,
                                                                                                                    FSContractPeriodDet.billingRule>
                                                                                                                    (fsAppointmentDetRow.InventoryID,
                                                                                                                     fsAppointmentDetRow.SMEquipmentID,
                                                                                                                     fsAppointmentDetRow.BillingRule)
                                                                                                            .AsEnumerable()
                                                                                                            .FirstOrDefault();

                            StandardContractPeriodDetail.Cache.Clear();
                            StandardContractPeriodDetail.Cache.ClearQueryCacheObsolete();
                            StandardContractPeriodDetail.View.Clear();
                            StandardContractPeriodDetail.Select();

                            if (fsContractPeriodDetRow != null)
                            {
                                usedQty = fsContractPeriodDetRow.UsedQty + (multSign * fsAppointmentDetRow.CoveredQty)
                                            + (multSign * fsAppointmentDetRow.ExtraUsageQty);

                                usedTime = fsContractPeriodDetRow.UsedTime + (int?)(multSign * fsAppointmentDetRow.CoveredQty * 60)
                                            + (int?)(multSign * fsAppointmentDetRow.ExtraUsageQty * 60);

                                fsContractPeriodDetRow.UsedQty = fsContractPeriodDetRow.BillingRule == ID.BillingRule.FLAT_RATE ? usedQty : 0m;
                                fsContractPeriodDetRow.UsedTime = fsContractPeriodDetRow.BillingRule == ID.BillingRule.TIME ? usedTime : 0;
                            }

                            graphRouteServiceContractEntry.ContractPeriodDetRecords.Update(fsContractPeriodDetRow);
                        }

                        graphRouteServiceContractEntry.Save.PressButton();
                    }

                    AppointmentEntry graphAppointmentEntry = PXGraph.CreateInstance<AppointmentEntry>();

                    var appointmentsRelatedToSameBillingPeriod = PXSelectJoin<FSAppointment,
                                                                 InnerJoin<FSServiceOrder,
                                                                     On<FSServiceOrder.sOID, Equal<FSAppointment.sOID>>>,
                                                                 Where<
                                                                     FSServiceOrder.billCustomerID, Equal<Required<FSServiceOrder.billCustomerID>>,
                                                                 And<
                                                                     FSAppointment.billServiceContractID, Equal<Required<FSAppointment.billServiceContractID>>,
                                                                 And<
                                                                     FSAppointment.billContractPeriodID, Equal<Required<FSAppointment.billContractPeriodID>>,
                                                                 And<
                                                                     FSAppointment.closed, Equal<False>,
                                                                 And<
                                                                     FSAppointment.canceled, Equal<False>,
                                                                 And<
                                                                     FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>>>>>>>>
                                                                 .Select(graphAppointmentEntry,
                                                                         ServiceOrderRelated.Current?.BillCustomerID,
                                                                         fsAppointmentRow.BillServiceContractID,
                                                                         fsAppointmentRow.BillContractPeriodID,
                                                                         fsAppointmentRow.AppointmentID);

                    foreach (FSAppointment fsRelatedAppointmentRow in appointmentsRelatedToSameBillingPeriod)
                    {
                        graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords
                                                                                                .Search<FSServiceOrder.refNbr>
                                                                                                (fsRelatedAppointmentRow.RefNbr, fsRelatedAppointmentRow.SrvOrdType);
                        graphAppointmentEntry.AppointmentRecords.Cache.SetDefaultExt<FSAppointment.billContractPeriodID>(fsRelatedAppointmentRow);
                        graphAppointmentEntry.Save.PressButton();
                    }
                }
                updateContractPeriod = false;
            }

            if (e.TranStatus == PXTranStatus.Completed
                    && (e.Operation == PXDBOperation.Insert
                        || e.Operation == PXDBOperation.Update))
            {
                UpdateServiceOrderUnboundFields(this, ServiceOrderRelated.Current, BillingCycleRelated.Current, dummyGraph, fsAppointmentRow, DisableServiceOrderUnboundFieldCalc);
            }

            if (e.TranStatus == PXTranStatus.Completed
                    && (e.Operation == PXDBOperation.Update || e.Operation == PXDBOperation.Delete))
            {
                ClearAPBillReferences(e.Row);
            }
        }

        #endregion

        #region FSAddress
        protected virtual void _(Events.FieldUpdated<FSAddress, FSAddress.countryID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAddress fsAddressRow = (FSAddress)e.Row;

            if (fsAddressRow.CountryID != (string)e.OldValue)
            {
                fsAddressRow.State = null;
                fsAddressRow.PostalCode = null;
            }
        }

        protected virtual void _(Events.RowPersisting<FSAddress> e)
        {
            if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
            {
                FSAddress fsAddressRow = (FSAddress)e.Row;

                string postalCode = (string)ServiceOrder_Address.Cache.GetValueOriginal<FSAddress.postalCode>(fsAddressRow);
                string addressLine1 = (string)ServiceOrder_Address.Cache.GetValueOriginal<FSAddress.addressLine1>(fsAddressRow);
                string addressLine2 = (string)ServiceOrder_Address.Cache.GetValueOriginal<FSAddress.addressLine2>(fsAddressRow);
                string city = (string)ServiceOrder_Address.Cache.GetValueOriginal<FSAddress.city>(fsAddressRow);
                string state = (string)ServiceOrder_Address.Cache.GetValueOriginal<FSAddress.state>(fsAddressRow);
                string countryID = (string)ServiceOrder_Address.Cache.GetValueOriginal<FSAddress.countryID>(fsAddressRow);

                if (fsAddressRow.PostalCode != postalCode
                        || fsAddressRow.AddressLine1 != addressLine1
                        || fsAddressRow.AddressLine2 != addressLine2
                        || fsAddressRow.City != city
                        || fsAddressRow.State != state
                        || fsAddressRow.CountryID != countryID
                )
                {
                    RetakeGeoLocation = true;
                }
            }
        }

        #endregion

        #region FSAppointmentEmployee

        #region FieldSelecting
        #endregion
        #region FieldDefaulting

        protected virtual void _(Events.FieldDefaulting<FSAppointmentEmployee, FSAppointmentEmployee.primaryDriver> e)
        {
            if (e.Row == null)
            {
                return;
            }

            PXResultset<FSAppointmentEmployee> employeeRows = AppointmentServiceEmployees.Select();
            e.NewValue = employeeRows.Count == 0;

            if (employeeRows.Count > 0)
            {
                e.NewValue = employeeRows.RowCast<FSAppointmentEmployee>()
                                         .Where(_ => _.PrimaryDriver == true && _.EmployeeID == e.Row.EmployeeID)
                                         .Any();
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentEmployee, FSAppointmentEmployee.earningType> e)
        {
            if (e.Row == null || e.Row.Type != BAccountType.EmployeeType)
            {
                return;
            }

            string dfltValue = string.Empty;

            FSAppointmentDet det = PXSelect<FSAppointmentDet,
                                        Where<FSAppointmentDet.appointmentID, Equal<Required<FSAppointmentDet.appointmentID>>,
                                        And<FSAppointmentDet.lineRef, Equal<Required<FSAppointmentDet.lineRef>>>>>
                                    .Select(this, e.Row.AppointmentID, e.Row.ServiceLineRef);

            if (det != null)
            {
                InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<FSAppointmentDet.inventoryID>(AppointmentDetails.Cache, det);
                FSxService itemExt = PXCache<InventoryItem>.GetExtension<FSxService>(item);

                if (itemExt?.DfltEarningType != null)
                {
                    dfltValue = itemExt.DfltEarningType;
                }
            }

            if (string.IsNullOrEmpty(dfltValue) && ServiceOrderTypeSelected.Current?.DfltEarningType != null)
            {
                dfltValue = ServiceOrderTypeSelected.Current?.DfltEarningType;
            }

            if (string.IsNullOrEmpty(dfltValue) == false)
            {
                e.NewValue = dfltValue;
            }
        }
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<FSAppointmentEmployee, FSAppointmentEmployee.serviceLineRef> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentEmployee fsAppointmentEmployeeRow = (FSAppointmentEmployee)e.Row;
            FSAppointment fsAppointmentRow = (FSAppointment)AppointmentRecords.Current;
            PXCache cache = e.Cache;

            string oldServiceLineRef = (string)e.OldValue;

            string pivot = fsAppointmentEmployeeRow.ServiceLineRef;

            FSAppointmentDet fsAppointmentDetRow = PXSelect<FSAppointmentDet,
                                                   Where<
                                                        FSAppointmentDet.lineRef, Equal<Required<FSAppointmentDet.lineRef>>,
                                                        And<FSAppointmentDet.appointmentID, Equal<Current<FSAppointmentDet.appointmentID>>>>>
                                                   .Select(cache.Graph, pivot);

            fsAppointmentEmployeeRow.DfltProjectID = fsAppointmentDetRow?.ProjectID;
            fsAppointmentEmployeeRow.DfltProjectTaskID = fsAppointmentDetRow?.ProjectTaskID;

            fsAppointmentEmployeeRow.CostCodeID = fsAppointmentDetRow?.CostCodeID;

            if (e.ExternalCall == true)
            {
                UpdateAppointmentDetService_StaffID(fsAppointmentEmployeeRow.ServiceLineRef, oldServiceLineRef);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentEmployee, FSAppointmentEmployee.employeeID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fsAppointmentRow = AppointmentRecords.Current;
            FSAppointmentEmployee fsAppointmentEmployeeRow = (FSAppointmentEmployee)e.Row;
            fsAppointmentEmployeeRow.Type = SharedFunctions.GetBAccountType(this, fsAppointmentEmployeeRow.EmployeeID);
            fsAppointmentEmployeeRow.DfltProjectTaskID = fsAppointmentRow?.DfltProjectTaskID;

            e.Cache.SetDefaultExt<FSAppointmentEmployee.trackTime>(e.Row);
            e.Cache.SetDefaultExt<FSAppointmentEmployee.earningType>(e.Row);
            e.Cache.SetDefaultExt<FSAppointmentEmployee.laborItemID>(e.Row);
            e.Cache.SetDefaultExt<FSAppointmentEmployee.primaryDriver>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentEmployee, FSAppointmentEmployee.primaryDriver> e)
        {
            if (e.Row == null)
            {
                return;
            }

            PrimaryDriver_FieldUpdated_Handler(e.Cache, e.Row);
        }
        #endregion

        protected virtual void _(Events.RowSelecting<FSAppointmentEmployee> e)
        {
        }

        protected virtual void _(Events.RowSelected<FSAppointmentEmployee> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentEmployee fsAppointmentEmployeeRow = (FSAppointmentEmployee)e.Row;
            FSAppointment fsAppointmentRow = AppointmentSelected.Current;
            PXCache cache = e.Cache;

            EnableDisable_StaffRelatedFields(cache, fsAppointmentEmployeeRow);
            EnableDisable_TimeRelatedFields(cache, SetupRecord.Current, ServiceOrderTypeSelected.Current, AppointmentRecords.Current, fsAppointmentEmployeeRow);
            SetVisible_TimeRelatedFields(cache, ServiceOrderTypeSelected.Current);
            SetPersisting_TimeRelatedFields(cache, SetupRecord.Current, ServiceOrderTypeSelected.Current, AppointmentRecords.Current, fsAppointmentEmployeeRow);
        }

        protected virtual void _(Events.RowInserting<FSAppointmentEmployee> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentEmployee fsAppointmentEmployeeRow = (FSAppointmentEmployee)e.Row;

            if (fsAppointmentEmployeeRow.LineRef == null)
            {
                fsAppointmentEmployeeRow.LineRef = fsAppointmentEmployeeRow.LineNbr.Value.ToString("000");
            }
        }

        protected virtual void _(Events.RowInserted<FSAppointmentEmployee> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentEmployee fsAppointmentEmployeeRow = (FSAppointmentEmployee)e.Row;
            UpdateAppointmentDetService_StaffID(fsAppointmentEmployeeRow.ServiceLineRef, null);
        }

        protected virtual void _(Events.RowUpdating<FSAppointmentEmployee> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSAppointmentEmployee> e)
        {
            MarkHeaderAsUpdated(e.Cache, e.Row);

            if (e.Row.PrimaryDriver == true)
            {
                AppointmentRecords.Cache.SetValueExt<FSAppointment.primaryDriver>(AppointmentRecords.Current, e.Row.EmployeeID);
            }
        }

        protected virtual void _(Events.RowDeleting<FSAppointmentEmployee> e)
        {

            if (e.Row == null)
            {
                return;
            }

            FSAppointmentEmployee fsAppointmentEmployeeRow = (FSAppointmentEmployee)e.Row;
            ValidateRouteDriverDeletionFromRouteDocument(fsAppointmentEmployeeRow);
            UpdateAppointmentDetService_StaffID(fsAppointmentEmployeeRow.ServiceLineRef, null);

            PrimaryDriver_RowDeleting_Handler(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowDeleted<FSAppointmentEmployee> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSAppointmentEmployee> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentEmployee fsAppointmentEmployeeRow = (FSAppointmentEmployee)e.Row;

            SetPersisting_TimeRelatedFields(e.Cache, SetupRecord.Current, ServiceOrderTypeSelected.Current, AppointmentRecords.Current, fsAppointmentEmployeeRow);
        }

        protected virtual void _(Events.RowPersisted<FSAppointmentEmployee> e)
        {
        }

        #endregion

        #region AppointmentResourceEventHandlers

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated
        #endregion

        protected virtual void _(Events.RowSelecting<FSAppointmentResource> e)
        {
        }

        protected virtual void _(Events.RowSelected<FSAppointmentResource> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentResource fsAppointmentResourceRow = (FSAppointmentResource)e.Row;
            PXCache cache = e.Cache;

            bool enableDisableFields = fsAppointmentResourceRow.SMEquipmentID == null;

            PXUIFieldAttribute.SetEnabled<FSAppointmentResource.SMequipmentID>(cache, fsAppointmentResourceRow, enableDisableFields);
            PXUIFieldAttribute.SetEnabled<FSAppointmentResource.qty>(cache, fsAppointmentResourceRow, !enableDisableFields);
            PXUIFieldAttribute.SetEnabled<FSAppointmentResource.comment>(cache, fsAppointmentResourceRow, !enableDisableFields);
        }

        protected virtual void _(Events.RowInserting<FSAppointmentResource> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSAppointmentResource> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSAppointmentResource> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSAppointmentResource> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSAppointmentResource> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSAppointmentResource> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSAppointmentResource> e)
        {
        }

        protected virtual void _(Events.RowPersisted<FSAppointmentResource> e)
        {
        }

        #endregion

        #region FSAppointmentDet
        #region FieldSelecting
        #endregion
        #region FieldDefaulting

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.acctID> e)
        {
            X_AcctID_FieldDefaulting<FSAppointmentDet>(e.Cache, e.Args, ServiceOrderTypeSelected.Current, ServiceOrderRelated.Current);
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.subID> e)
        {
            X_SubID_FieldDefaulting<FSAppointmentDet>(e.Cache, e.Args, ServiceOrderTypeSelected.Current, ServiceOrderRelated.Current);
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.uOM> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsService || fsAppointmentDetRow.IsInventoryItem)
            {
                X_UOM_FieldDefaulting<FSAppointmentDet>(e.Cache, e.Args);
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.unitCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.Row.IsInventoryItem)
            {
                INItemSite inItemSiteRow = PXSelect<INItemSite,
                                           Where<
                                               INItemSite.inventoryID, Equal<Required<FSAppointmentDet.inventoryID>>,
                                               And<
                                                   INItemSite.siteID, Equal<Required<FSAppointmentDet.siteID>>>>>
                                           .Select(this, e.Row.InventoryID, e.Row.SiteID);

                e.NewValue = inItemSiteRow?.TranUnitCost;
            }
            else
            {
                InventoryItem inventoryItemRow = InventoryItem.PK.Find(this, e.Row.InventoryID);
                DateTime? docdate = (AppointmentRecords.Current == null ? false : AppointmentRecords.Current.NotStarted == true)
                                        ? AppointmentRecords.Current?.ScheduledDateTimeBegin : AppointmentRecords.Current?.ActualDateTimeBegin;

                if (inventoryItemRow != null)
                {
                    if (inventoryItemRow.StdCostDate <= docdate)
                        e.NewValue = inventoryItemRow.StdCost;
                    else
                        e.NewValue = inventoryItemRow.LastStdCost;
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.curyUnitCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (string.IsNullOrEmpty(fsAppointmentDetRow.UOM) == false && fsAppointmentDetRow.InventoryID != null)
            {
                object unitcost;
                e.Cache.RaiseFieldDefaulting<FSAppointmentDet.unitCost>(e.Row, out unitcost);

                if (unitcost != null && (decimal)unitcost != 0m)
                {
                    decimal newval = INUnitAttribute.ConvertToBase<FSAppointmentDet.inventoryID, FSAppointmentDet.uOM>(e.Cache, fsAppointmentDetRow, (decimal)unitcost, INPrecision.NOROUND);
                    CM.PXDBCurrencyAttribute.CuryConvCury(e.Cache, fsAppointmentDetRow, newval, out newval, CommonSetupDecPl.PrcCst);
                    e.NewValue = newval;
                    e.Cancel = true;
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.costCodeID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;
            SetCostCodeDefault(fsAppointmentDetRow, AppointmentRecords.Current?.ProjectID, ServiceOrderTypeSelected.Current, e.Args);
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.contractRelated> e)
        {
            if (e.Row == null || ServiceOrderTypeSelected.Current == null || AppointmentSelected.Current == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsService == false)
            {
                return;
            }

            var relatedServiceOrder = ServiceOrderRelated.Current;

            if (BillingCycleRelated.Current == null
                || relatedServiceOrder?.BillingBy != ID.Billing_By.APPOINTMENT
                || AppointmentRecords.Current.BillServiceContractID == null
                || AppointmentRecords.Current.BillContractPeriodID == null)
            {
                e.NewValue = false;
                return;
            }

            FSAppointmentDet fsSODetDuplicatedByContract = AppointmentDetails.Search<FSAppointmentDet.inventoryID,
                                                                                     FSAppointmentDet.SMequipmentID,
                                                                                     FSAppointmentDet.billingRule,
                                                                                     FSAppointmentDet.contractRelated>
                                                                                     (fsAppointmentDetRow.InventoryID,
                                                                                      fsAppointmentDetRow.SMEquipmentID,
                                                                                      fsAppointmentDetRow.BillingRule,
                                                                                      true);

            bool duplicatedContractLine = fsSODetDuplicatedByContract != null && fsSODetDuplicatedByContract.LineID != fsAppointmentDetRow.LineID;

            e.NewValue = duplicatedContractLine == false
                            && StandardContractPeriodDetail.Select().AsEnumerable().RowCast<FSContractPeriodDet>()
                                                           .Where(x => x.InventoryID == fsAppointmentDetRow.InventoryID
                                                                       && (x.SMEquipmentID == fsAppointmentDetRow.SMEquipmentID || x.SMEquipmentID == null)
                                                                       && x.BillingRule == fsAppointmentDetRow.BillingRule)
                                                           .Count() == 1;
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.coveredQty> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsService == false)
            {
                return;
            }

            var relatedServiceOrder = ServiceOrderRelated.Current;

            if (BillingCycleRelated.Current == null
                || relatedServiceOrder?.BillingBy != ID.Billing_By.APPOINTMENT
                || fsAppointmentDetRow.ContractRelated == false)
            {
                e.NewValue = 0m;
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = StandardContractPeriodDetail.Select().RowCast<FSContractPeriodDet>()
                                                                                     .Where(x => x.InventoryID == fsAppointmentDetRow.InventoryID
                                                                                              && (x.SMEquipmentID == fsAppointmentDetRow.SMEquipmentID || x.SMEquipmentID == null)
                                                                                              && x.BillingRule == fsAppointmentDetRow.BillingRule)
                                                                                     .FirstOrDefault();

            if (fsContractPeriodDetRow != null)
            {
                int? pivotDuration = AppointmentSelected.Current.NotStarted == true && AppointmentSelected.Current.StartActionRunning == false
                                        ? fsAppointmentDetRow.EstimatedDuration : fsAppointmentDetRow.ActualDuration;

                decimal? pivotQty = AppointmentSelected.Current.NotStarted == true && AppointmentSelected.Current.StartActionRunning == false
                                        ? fsAppointmentDetRow.EstimatedQty : fsAppointmentDetRow.ActualQty;

                if (fsAppointmentDetRow.BillingRule == ID.BillingRule.TIME)
                {
                    e.NewValue = fsContractPeriodDetRow.RemainingTime - pivotDuration >= 0 ? pivotQty : fsContractPeriodDetRow.RemainingTime / 60;
                }
                else
                {
                    e.NewValue = fsContractPeriodDetRow.RemainingQty - pivotQty >= 0 ? pivotQty : fsContractPeriodDetRow.RemainingQty;
                }
            }
            else
            {
                e.NewValue = 0m;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.curyExtraUsageUnitPrice> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsService == false)
            {
                return;
            }

            var relatedServiceOrder = ServiceOrderRelated.Current;

            if (BillingCycleRelated.Current == null
                || relatedServiceOrder?.BillingBy != ID.Billing_By.APPOINTMENT
                || fsAppointmentDetRow.ContractRelated == false)
            {
                e.NewValue = 0m;
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = StandardContractPeriodDetail.Select().RowCast<FSContractPeriodDet>()
                                                                                     .Where(x => x.InventoryID == fsAppointmentDetRow.InventoryID
                                                                                              && (x.SMEquipmentID == fsAppointmentDetRow.SMEquipmentID || x.SMEquipmentID == null)
                                                                                              && x.BillingRule == fsAppointmentDetRow.BillingRule)
                                                                                     .FirstOrDefault();

            if (fsContractPeriodDetRow != null)
            {
                e.NewValue = fsContractPeriodDetRow.OverageItemPrice;
            }
            else
            {
                e.NewValue = 0m;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.curyUnitPrice> e)
        {
            if (e.Row == null ||  e.Row.IsLinkedItem == true)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;
            FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.Current;

            DateTime? docDate = AppointmentSelected.Current.ScheduledDateTimeBegin;

            bool unitPriceEqualsToUnitCost = ServiceOrderTypeSelected.Current?.PostTo == ID.SrvOrdType_PostTo.PROJECTS
                                                    && ServiceOrderTypeSelected.Current?.BillingType == ID.SrvOrdType_BillingType.COST_AS_COST;

            if (docDate != null)
            {
                // Remove the time part
                docDate = new DateTime(docDate.Value.Year, docDate.Value.Month, docDate.Value.Day);
            }

            if (unitPriceEqualsToUnitCost == false)
            {

                var currencyInfo = ExtensionHelper.SelectCurrencyInfo(currencyInfoView, AppointmentSelected.Current.CuryInfoID);

                // Appointment Service and Inventory lines handle EstimatedQty so the price is based on EstimatedQty and not on Qty.
                // Appointment PickupDelivery lines don't handle EstimatedQty so the price is based on Qty.
                decimal? qty = fsAppointmentDetRow.BillableQty;

                X_CuryUnitPrice_FieldDefaulting<FSAppointmentDet,
                                                                                FSAppointmentDet.curyUnitPrice>
                                                                                (e.Cache, e.Args, qty, docDate, fsServiceOrderRow, AppointmentRecords.Current, currencyInfo);
            }
            else
            {
                e.NewValue = fsAppointmentDetRow.CuryUnitCost ?? 0m;
                e.Cancel = fsAppointmentDetRow.CuryUnitCost != null;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.siteID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDet = (FSAppointmentDet)e.Row;

            if (IsInventoryLine(fsAppointmentDet.LineType) == false
                || fsAppointmentDet.InventoryID == null)
            {
                e.NewValue = null;
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.effTranQty> e)
        {
            if (e.Row.AreActualFieldsActive == true)
            {
                e.NewValue = e.Row.ActualQty;
            }
            else
            {
                e.NewValue = e.Row.EstimatedQty;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.enablePO> e)
        {
            if (e.Row == null
                || ServiceOrderTypeSelected.Current == null
                || ServiceOrderTypeSelected.Current.PostTo != ID.SrvOrdType_PostTo.SALES_ORDER_MODULE)
            {
                return;
            }

            SOOrderType soOrderType = postSOOrderTypeSelected.Current;

            if (soOrderType != null && !(soOrderType.RequireShipping == true && soOrderType.RequireLocation == false && soOrderType.RequireAllocation == false))
            {
                e.NewValue = false;
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.poVendorID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDet = (FSAppointmentDet)e.Row;

            if (fsAppointmentDet.EnablePO == false || fsAppointmentDet.InventoryID == null)
            {
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.poVendorLocationID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDet = (FSAppointmentDet)e.Row;

            if (fsAppointmentDet.EnablePO == false || fsAppointmentDet.InventoryID == null || fsAppointmentDet.POVendorID == null)
            {
                e.Cancel = true;
            }
        }
        #endregion
        #region FieldUpdating
        protected virtual void _(Events.FieldUpdating<FSAppointmentDet, FSAppointmentDet.actualQty> e)
        {
            if (e.NewValue == null || e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsPickupDelivery)
            {
                decimal newValue = Convert.ToDecimal(e.NewValue);

                if (newValue < (decimal?)0.0)
                {
                    e.Cache.RaiseExceptionHandling<FSAppointmentDet.actualQty>(e.Row, fsAppointmentDetRow.ActualQty, new PXSetPropertyException(TX.Error.POSITIVE_QTY, PXErrorLevel.Warning));
                    e.NewValue = fsAppointmentDetRow.ActualQty;
                }
            }
        }

        protected virtual void _(Events.FieldUpdating<FSAppointmentDet, FSAppointmentDet.uiStatus> e)
        {
            if (e.NewValue == null || e.Row == null)
            {
                return;
            }

            e.NewValue = GetValidAppDetStatus(e.Row, (string)e.NewValue);
        }

        protected virtual void _(Events.FieldUpdating<FSAppointmentDet, FSAppointmentDet.status> e)
        {
            if (e.NewValue == null || e.Row == null)
            {
                return;
            }

            e.NewValue = GetValidAppDetStatus(e.Row, (string)e.NewValue);
        }

        #endregion
        #region FieldVerifying

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.billingRule> e)
        {
            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (e.Row != null && fsAppointmentDetRow.IsService == false)
            {
                return;
            }

            X_BillingRule_FieldVerifying<FSAppointmentDet>(e.Cache, e.Args);
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.lotSerialNbr> e)
        {
            if (e.Row == null)
            {
                return;
            }
            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsInventoryItem)
            {

                int? serialRepeated = PXSelectJoin<FSAppointmentDet,
                                      InnerJoin<FSAppointment, On<FSAppointment.appointmentID, Equal<FSAppointmentDet.appointmentID>>>,
                                      Where<
                                            FSAppointmentDet.lineType, Equal<FSLineType.Inventory_Item>,
                                            And<FSAppointment.canceled, Equal<False>,
                                            And<FSAppointmentDet.isCanceledNotPerformed, NotEqual<True>,
                                            And<FSAppointmentDet.sODetID, Equal<Required<FSAppointmentDet.sODetID>>,
                                            And<FSAppointmentDet.appDetID, NotEqual<Required<FSAppointmentDet.appDetID>>,
                                            And<FSAppointmentDet.lotSerialNbr, Equal<Required<FSAppointmentDet.lotSerialNbr>>>>>>>>>
                                      .Select(new PXGraph(), fsAppointmentDetRow.SODetID, fsAppointmentDetRow.AppDetID, (string)e.NewValue)
                                      .Count();

                if (serialRepeated != null && serialRepeated > 0)
                {
                    e.Cache.RaiseExceptionHandling<FSAppointmentDet.lotSerialNbr>(fsAppointmentDetRow, null, new PXSetPropertyException(TX.Error.REPEATED_APPOINTMENT_SERIAL_ERROR, PXErrorLevel.Error));
                    e.NewValue = null;
                }
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.uiStatus> e)
        {
            if (e.Row == null || IsCopyPasteContext)
            {
                return;
            }

            string newItemLineStatus = (string)e.NewValue;

            if (e.ExternalCall == false)
            {
                return;
            }

            if (newItemLineStatus == null)
            {
                throw new PXSetPropertyException(PX.Data.ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<FSAppointmentDet.uiStatus>(e.Cache));
            }

            if (IsNewItemLineStatusValid(e.Row, newItemLineStatus) == false)
            {
                FSAppointment appointment = AppointmentSelected.Current;

                throw new PXSetPropertyException(TX.Error.InvalidItemLineStatusForCurrentAppointmentStatus,
                    PXStringListAttribute.GetLocalizedLabel<FSAppointmentDet.uiStatus>(AppointmentDetails.Cache, e.Row, newItemLineStatus),
                    PXStringListAttribute.GetLocalizedLabel<FSAppointment.status>(AppointmentSelected.Cache, appointment, appointment.Status));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.curyUnitPrice> e)
        {
            if (e.Row == null)
                return;

            if (e.Row.BillingRule == ID.BillingRule.NONE
                    && e.Row.InventoryID != null)
            {
                e.Row.ManualPrice = false;
                e.NewValue = 0m;
                return;
            }

            if (GetServiceOrderEntryGraph(false).IsManualPriceFlagNeeded(e.Cache, e.Row))
                e.Row.ManualPrice = true;
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.curyExtPrice> e)
        {
            if (e.Row == null)
                return;

            if (GetServiceOrderEntryGraph(false).IsManualPriceFlagNeeded(e.Cache, e.Row))
                e.Row.ManualPrice = true;
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.estimatedQty> e)
        {
            decimal srvOrdAllocatedQty;
            decimal otherAppointmentsQty;

            bool soDetExists = GetSrvOrdLineBalance(e.Cache.Graph, e.Row.SODetID, e.Row.AppDetID, out srvOrdAllocatedQty, out otherAppointmentsQty);

            if (soDetExists == true)
            {
                decimal balance = srvOrdAllocatedQty - otherAppointmentsQty;
                e.NewValue = balance < 1m ? balance : 1m;
            }
            else
            {
                e.NewValue = 1m;
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.estimatedQty> e)
        {
            VerifySrvOrdLineQty(e.Cache, (FSAppointmentDet)e.Row, e.NewValue, typeof(FSAppointmentDet.estimatedQty), true);
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.actualQty> e)
        {
            VerifySrvOrdLineQty(e.Cache, (FSAppointmentDet)e.Row, e.NewValue, typeof(FSAppointmentDet.actualQty), true);
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.enablePO> e)
        {
            if (e.Row.SODetID > 0)
            {
                FSSODet soDetLine = FSSODet.UK.Find(this, e.Row.SODetID);
                if (soDetLine == null)
                {
                    throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODet)));
                }

                if ((bool)soDetLine.EnablePO != (bool)e.NewValue)
                {
                    if (e.Row.CanChangeMarkForPO != true)
                    {
                        PXException exception;
                        CanChangePOOptions(e.Row, ref soDetLine, typeof(FSAppointmentDet.enablePO).Name, out exception);

                        if (exception != null)
                        {
                            throw exception;
                        }
                    }
                }
            }

            POCreateVerifyValue(e.Cache, e.Row, (bool?)e.NewValue);
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.pOSource> e)
        {
            if (e.ExternalCall == false)
                return;

            if (e.Row.SODetID > 0)
            {
                FSSODet soDetLine = FSSODet.UK.Find(this, e.Row.SODetID);
                if (soDetLine == null)
                {
                    throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODet)));
                }

                if (e.Row.CanChangeMarkForPO != true)
                {
                    PXException exception;
                    CanChangePOOptions(e.Row, ref soDetLine, typeof(FSAppointmentDet.pOSource).Name, out exception);

                    if (exception != null)
                    {
                        throw exception;
                    }
                }
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.curyBillableExtPrice> e)
        {
            if (e.Row == null)
                return;

            if (e.Row.BillingRule == ID.BillingRule.NONE)
                e.NewValue = 0m;
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentDet, FSAppointmentDet.discPct> e)
        {
            if (e.Row == null)
                return;

            if (e.Row.BillingRule == ID.BillingRule.NONE)
                e.NewValue = 0m;
        }
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.isPrepaid> e)
        {
            X_IsPrepaid_FieldUpdated<FSAppointmentDet,
                                                                     FSAppointmentDet.manualPrice,
                                                                     FSAppointmentDet.isFree,
                                                                     FSAppointmentDet.estimatedDuration,
                                                                     FSAppointmentDet.actualDuration>
                                                                     (e.Cache, e.Args, useActualField: true);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.manualPrice> e)
        {
            X_ManualPrice_FieldUpdated<FSAppointmentDet, FSAppointmentDet.curyUnitPrice, FSAppointmentDet.curyBillableExtPrice>(e.Cache, e.Args);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.inventoryID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;
            FSServiceOrder fsServiceOrderRow = ServiceOrderRelated.Current;

            X_InventoryID_FieldUpdated<FSAppointmentDet,
                                                                       FSAppointmentDet.acctID,
                                                                       FSAppointmentDet.subItemID,
                                                                       FSAppointmentDet.siteID,
                                                                       FSAppointmentDet.siteLocationID,
                                                                       FSAppointmentDet.uOM,
                                                                       FSAppointmentDet.estimatedDuration,
                                                                       FSAppointmentDet.estimatedQty,
                                                                       FSAppointmentDet.billingRule,
                                                                       FSAppointmentDet.actualDuration,
                                                                       FSAppointmentDet.actualQty>
                                                                       (e.Cache,
                                                                        e.Args,
                                                                        fsServiceOrderRow.BranchLocationID,
                                                                        TaxCustomer.Current,
                                                                        useActualFields: true);

            if (fsAppointmentDetRow.IsService == true)
            {
                fsAppointmentDetRow.ServiceType = null;

                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(e.Cache.Graph, fsAppointmentDetRow.InventoryID);

                if (inventoryItemRow != null)
                {
                    FSxService fsxServiceRow = PXCache<InventoryItem>.GetExtension<FSxService>(inventoryItemRow);
                    fsAppointmentDetRow.ServiceType = fsxServiceRow?.ActionType;
                }

                if (fsAppointmentDetRow.ContractRelated == true)
                {
                    FSContractPeriodDet fsContractPeriodDetRow = StandardContractPeriodDetail.Select().RowCast<FSContractPeriodDet>()
                                                                                             .Where(x => x.InventoryID == fsAppointmentDetRow.InventoryID
                                                                                                      && (x.SMEquipmentID == fsAppointmentDetRow.SMEquipmentID || x.SMEquipmentID == null)
                                                                                                      && x.BillingRule == fsAppointmentDetRow.BillingRule)
                                                                                             .FirstOrDefault();

                    e.Cache.SetValueExt<FSAppointmentDet.projectTaskID>(fsAppointmentDetRow, fsContractPeriodDetRow.ProjectTaskID);
                    e.Cache.SetValueExt<FSAppointmentDet.costCodeID>(fsAppointmentDetRow, fsContractPeriodDetRow.CostCodeID);
                }

                e.Cache.SetDefaultExt<FSAppointmentDet.curyUnitCost>(e.Row);
                e.Cache.SetDefaultExt<FSAppointmentDet.enablePO>(e.Row);
            }
            else if (fsAppointmentDetRow.IsInventoryItem == true)
            {
                SharedFunctions.UpdateEquipmentFields(this, e.Cache, fsAppointmentDetRow, fsAppointmentDetRow.InventoryID, AppointmentRecords.Current?.NotStarted ==  false);
                e.Cache.SetDefaultExt<FSAppointmentDet.curyUnitCost>(e.Row);
                e.Cache.SetDefaultExt<FSAppointmentDet.enablePO>(e.Row);
            }

            if (e.ExternalCall == false
                    && fsAppointmentDetRow.InventoryID != null)
            {
                //In case ExternalCall == false (any change not coming from Appointment's UI), ShowPopupMessage = false to avoid
                //showing the Popup Message for the InventoryID field. By default it's set to true.
                foreach (PXSelectorAttribute s in e.Cache.GetAttributes(typeof(FSAppointmentDet.inventoryID).Name).OfType<PXSelectorAttribute>())
                {
                    s.ShowPopupMessage = false;
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.tranDesc> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (!String.IsNullOrEmpty(fsAppointmentDetRow.TranDesc) && fsAppointmentDetRow.LineType == null)
            {
                e.Cache.SetValueExt<FSAppointmentDet.lineType>(fsAppointmentDetRow, ID.LineType_ALL.INSTRUCTION);
            }

            if (fsAppointmentDetRow.IsService == true && fsAppointmentDetRow.StaffRelated == true)
            {
                AppointmentServiceEmployees.Cache.SetStatus(AppointmentServiceEmployees.Current, PXEntryStatus.Updated);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.uiStatus> e)
        {
            if (e.Row == null || IsCopyPasteContext)
            {
                return;
            }

            if (e.ExternalCall == false)
            {
                return;
            }

            if (e.Row.Status != e.Row.UIStatus)
            {
                e.Cache.SetValueExt<FSAppointmentDet.status>(e.Row, e.Row.UIStatus);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.status> e)
        {
            if (e.Row == null)
            {
                return;
            }

            string oldStatus = (string)e.OldValue;

            UpdateCanceledNotPerformed(e.Cache, e.Row, AppointmentSelected.Current, oldStatus);

            if (e.Row.Status != oldStatus)
            {
                if (e.Row.Status == ListField_Status_AppointmentDet.RequestForPO
                    || e.Row.Status == ListField_Status_AppointmentDet.WaitingForPO)
                {
                    SetItemLineUIStatusList(e.Cache, e.Row);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.isCanceledNotPerformed> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.Row.IsService == true && e.Row.IsCanceledNotPerformed == true)
            {
                foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in AppointmentServiceEmployees.Select().AsEnumerable().Where(y => ((FSAppointmentEmployee)y).ServiceLineRef == e.Row.LineRef))
                {
                    fsAppointmentEmployeeRow.TrackTime = false;
                    AppointmentServiceEmployees.Update(fsAppointmentEmployeeRow);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.projectTaskID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsService == true && fsAppointmentDetRow.StaffRelated == true)
            {
                AppointmentServiceEmployees.Cache.SetStatus(AppointmentServiceEmployees.Current, PXEntryStatus.Updated);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.sODetID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsPickupDelivery == false)
            {
                FSSODet soDetLine = FSSODet.UK.Find(this, e.Row.SODetID);
                GetSODetValues<FSAppointmentDet, FSSODet>(e.Cache, fsAppointmentDetRow, ServiceOrderRelated.Current, AppointmentRecords.Current, soDetLine);

                PXException exception;
                fsAppointmentDetRow.CanChangeMarkForPO = CanChangePOOptions(fsAppointmentDetRow, ref soDetLine, typeof(FSAppointmentDet.enablePO).Name, out exception);
            }
            else
            {
                string oldServiceLineRef = (string)e.OldValue;
                string pivot = fsAppointmentDetRow.LineRef ?? oldServiceLineRef;

                FSAppointmentDet fsAppointmentDetRowRef = PXSelectJoin<FSAppointmentDet,
                                                          InnerJoin<FSSODet,
                                                               On<FSSODet.sODetID, Equal<FSAppointmentDet.sODetID>>>,
                                                          Where<
                                                               FSSODet.lineRef, Equal<Required<FSSODet.lineRef>>,
                                                          And<
                                                               FSSODet.sOID, Equal<Current<FSAppointment.sOID>>>>>
                                                          .Select(e.Cache.Graph, pivot);

                fsAppointmentDetRow.ProjectTaskID = fsAppointmentDetRowRef?.ProjectTaskID;
                fsAppointmentDetRow.CostCodeID = fsAppointmentDetRowRef?.CostCodeID;
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.estimatedQty> e)
        {
            if ((decimal?)e.OldValue != e.Row.EstimatedQty)
            {
                if (e.Row.IsLinkedItem)
                {
                    e.Cache.SetValueExt<FSAppointmentDet.actualQty>(e.Row, e.Row.EstimatedQty);
                }
                e.Cache.SetDefaultExt<FSAppointmentDet.effTranQty>(e.Row);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.contractRelated> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsPickupDelivery == false && (bool?)e.OldValue != fsAppointmentDetRow.ContractRelated)
            {
                e.Cache.SetDefaultExt<FSAppointmentDet.curyUnitPrice>(e.Row);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.billableQty> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsService || fsAppointmentDetRow.IsInventoryItem)
            {
                if ((decimal?)e.OldValue != ((FSAppointmentDet)e.Row).BillableQty)
                {
                    X_Qty_FieldUpdated<FSAppointmentDet.curyUnitPrice>(e.Cache, e.Args);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.actualQty> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet row = e.Row;

            if (row.IsService || row.IsPickupDelivery)
            {
                X_Qty_FieldUpdated<FSAppointmentDet.curyUnitPrice>(e.Cache, e.Args);
            }

            if ((decimal?)e.OldValue != row.ActualQty)
            {
                e.Cache.SetDefaultExt<FSAppointmentDet.effTranQty>(row);
            }

            FSAppointment appointmentRow = AppointmentRecords.Current;

            if (e.ExternalCall == true
                && row.AppDetID < 0
                && appointmentRow != null
                && (appointmentRow.InProcess == true
                    || appointmentRow.Completed == true)) 
            {
                e.Cache.SetValueExtIfDifferent<FSAppointmentDet.estimatedQty>(row, row.ActualQty);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.actualDuration> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsService)
            {
                X_Duration_FieldUpdated<FSAppointmentDet, FSAppointmentDet.actualQty>(e.Cache, e.Args, fsAppointmentDetRow.ActualDuration);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.estimatedDuration> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsService)
            {
                X_Duration_FieldUpdated<FSAppointmentDet, FSAppointmentDet.estimatedQty>(e.Cache, e.Args, fsAppointmentDetRow.EstimatedDuration);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.lineType> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsPickupDelivery == false && e.ExternalCall == true)
            {
                X_LineType_FieldUpdated<FSAppointmentDet>(e.Cache, e.Args);
            }

            if (fsAppointmentDetRow.LineType == ID.LineType_ALL.PICKUP_DELIVERY && (string)e.OldValue != ID.LineType_ALL.PICKUP_DELIVERY)
            {
                var serviceRows = AppointmentDetails.Select().AsEnumerable().RowCast<FSAppointmentDet>()
                                                    .Where(x => x.LineType == ID.LineType_ALL.SERVICE
                                                             && x.AppDetID > 0);

                if (serviceRows.Count() == 1)
                {
                    FSAppointmentDet fsServiceAppointmentDetRow = (FSAppointmentDet)serviceRows.First();
                    e.Cache.SetValueExt<FSAppointmentDet.pickupDeliveryAppLineRef>(fsAppointmentDetRow, fsServiceAppointmentDetRow.LineRef);
                }
            }
        }

        [Obsolete("Remove in major release 2020R2")]
        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.staffID> e)
        {
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.SMequipmentID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            UpdateWarrantyFlag(e.Cache, fsAppointmentDetRow, AppointmentRecords.Current.ExecutionDate);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.componentID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            UpdateWarrantyFlag(e.Cache, fsAppointmentDetRow, AppointmentRecords.Current.ExecutionDate);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.equipmentLineRef> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            UpdateWarrantyFlag(e.Cache, fsAppointmentDetRow, AppointmentRecords.Current.ExecutionDate);

            if (fsAppointmentDetRow.ComponentID == null)
            {
                fsAppointmentDetRow.ComponentID = SharedFunctions.GetEquipmentComponentID(this, fsAppointmentDetRow.SMEquipmentID, fsAppointmentDetRow.EquipmentLineRef);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.equipmentAction> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsInventoryItem)
            {
                SharedFunctions.ResetEquipmentFields(e.Cache, fsAppointmentDetRow);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.lotSerialNbr> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsInventoryItem)
            {
                SetUnitCostByLotSerialNbr(e.Cache, fsAppointmentDetRow, (string)e.OldValue);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.billingRule> e)
        {
            X_BillingRule_FieldUpdated<FSAppointmentDet,
                                                                       FSAppointmentDet.estimatedDuration,
                                                                       FSAppointmentDet.actualDuration,
                                                                       FSAppointmentDet.curyUnitPrice,
                                                                       FSAppointmentDet.isFree>
                                                                       (e.Cache, e.Args, useActualField: true);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.uOM> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            X_UOM_FieldUpdated<FSAppointmentDet.curyUnitPrice>(e.Cache, e.Args);
            e.Cache.SetDefaultExt<FSAppointmentDet.curyUnitCost>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.siteID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            X_SiteID_FieldUpdated<FSAppointmentDet.curyUnitPrice, FSAppointmentDet.acctID, FSAppointmentDet.subID>(e.Cache, e.Args);

            e.Cache.SetDefaultExt<FSAppointmentDet.curyUnitCost>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.curyUnitCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSrvOrdType srvOrdType = ServiceOrderTypeSelected.Current;

            if (srvOrdType != null
                    && srvOrdType.PostTo == ID.SrvOrdType_PostTo.PROJECTS
                    && srvOrdType.BillingType == ID.SrvOrdType_BillingType.COST_AS_COST)
            {
                e.Cache.SetDefaultExt<FSAppointmentDet.curyUnitPrice>(e.Row);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.isFree> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.Row.IsFree == true)
            {
                e.Cache.SetValueExt<FSAppointmentDet.curyUnitPrice>(e.Row, 0m);
                e.Cache.SetValueExt<FSAppointmentDet.curyBillableExtPrice>(e.Row, 0m);
                e.Cache.SetValueExt<FSAppointmentDet.discPct>(e.Row, 0m);
                e.Cache.SetValueExt<FSAppointmentDet.curyDiscAmt>(e.Row, 0m);
                if (e.ExternalCall)
                {
                    e.Cache.SetValueExt<FSAppointmentDet.manualDisc>(e.Row, true);
                }
            }
            else
            {
                e.Cache.SetDefaultExt<FSAppointmentDet.curyUnitPrice>(e.Row);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.isBillable> e)
        {
            if (e.Row == null || e.Row.IsBillable == (bool?)e.OldValue)
            {
                return;
            }

            if (e.Row.IsBillable == false || e.Row.BillingRule == ID.BillingRule.NONE)
            {
                if (e.Row.IsCanceledNotPerformed == true)
                {
                    e.Cache.SetValueExt<FSAppointmentDet.curyBillableExtPrice>(e.Row, 0m);
                    e.Cache.SetValueExt<FSAppointmentDet.discPct>(e.Row, 0m);
                    e.Cache.SetValueExt<FSAppointmentDet.curyDiscAmt>(e.Row, 0m);
                }
                else
                {
                    e.Cache.SetValueExt<FSAppointmentDet.isFree>(e.Row, true);
                }

                e.Cache.SetValueExt<FSAppointmentDet.contractRelated>(e.Row, false);
            }
            else
            {
                e.Cache.SetValueExt<FSAppointmentDet.isFree>(e.Row, false);
                e.Cache.SetValueExt<FSAppointmentDet.manualPrice>(e.Row, e.Row.IsExpenseReceiptItem == true);

                if (e.Row.IsExpenseReceiptItem == true)
                {
                    e.Cache.SetValueExt<FSAppointmentDet.curyUnitPrice>(e.Row, e.Row.CuryUnitCost);
                }

                if(e.Row.IsLinkedItem == true)
                {
                    e.Cache.SetValueExt<FSAppointmentDet.curyBillableExtPrice>(e.Row, e.Row.CuryExtCost);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.effTranQty> e)
        {
            if (e.Row.AreActualFieldsActive == true)
            {
                if (e.Row.ActualQty != e.Row.EffTranQty)
                {
                    e.Cache.SetValueExt<FSAppointmentDet.actualQty>(e.Row, e.Row.EffTranQty);
                }
            }
            else
            {
                if (e.Row.EstimatedQty != e.Row.EffTranQty)
                {
                    e.Cache.SetValueExt<FSAppointmentDet.estimatedQty>(e.Row, e.Row.EffTranQty);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.enablePO> e)
        {
            if (e.Row == null)
                return;

            if (e.Row.ShouldBeRequestPO == true)
            {
                e.Cache.SetValueExt<FSAppointmentDet.status>(e.Row, FSAppointmentDet.status.RequestForPO);
            }
            else if (e.Row.ShouldBeWaitingPO == true)
            {
                e.Cache.SetValueExt<FSAppointmentDet.status>(e.Row, FSAppointmentDet.status.WaitingForPO);
            }
            else
            {
                e.Cache.SetValueExt<FSAppointmentDet.status>(e.Row, FSAppointmentDet.status.NOT_STARTED);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.pOSource> e)
        {
            if (e.Row == null)
                return;

            if (e.Row.ShouldBeWaitingPO == true)
            {
                e.Cache.SetValueExt<FSAppointmentDet.status>(e.Row, FSAppointmentDet.status.WaitingForPO);
            }
            else if (e.Row.ShouldBeRequestPO == true)
            {
                e.Cache.SetValueExt<FSAppointmentDet.status>(e.Row, FSAppointmentDet.status.RequestForPO);
            }
            else
            {
                e.Cache.SetValueExt<FSAppointmentDet.status>(e.Row, FSAppointmentDet.status.NOT_STARTED);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentDet, FSAppointmentDet.linkedDocRefNbr> e)
        {
            if (e.Row == null)
                return;

            if (e.Row.IsAPBillItem == true)
            {
                e.Cache.SetValueExt<FSAppointmentDet.actualDuration>(e.Row, 0);
                e.Cache.SetValueExt<FSAppointmentDet.actualQty>(e.Row, e.Row.EstimatedQty);
            }
        }
        #endregion
        #region ExceptionHandling
        protected virtual void _(Events.ExceptionHandling<FSAppointmentDet.status> e)
        {
            Exception ex = e.Exception as PXSetPropertyException;
            if (ex != null)
            {
                var apptDet = (FSAppointmentDet)e.Row;

                e.Cache.RaiseExceptionHandling<FSAppointmentDet.uiStatus>(apptDet, apptDet.Status, ex);
            }
        }

        protected virtual void _(Events.ExceptionHandling<FSAppointmentDet.effTranQty> e)
        {
            Exception ex = e.Exception as PXSetPropertyException;
            if (ex != null)
            {
                var apptDet = (FSAppointmentDet)e.Row;

                if (apptDet.AreActualFieldsActive == true)
                {
                    e.Cache.RaiseExceptionHandling<FSAppointmentDet.actualQty>(apptDet, apptDet.EffTranQty, ex);
                }
                else
                {
                    e.Cache.RaiseExceptionHandling<FSAppointmentDet.estimatedQty>(apptDet, apptDet.EffTranQty, ex);
                }
            }
        }

        protected virtual PXSetPropertyException GetSetPropertyException<TField>(PXCache cache, object row, Exception currentException)
            where TField : IBqlField
        {
            PXFieldState fieldState;
            PXErrorLevel errorLevel = PXErrorLevel.Error;

            try
            {
                fieldState = (PXFieldState)cache.GetStateExt<TField>(row);
            }
            catch
            {
                fieldState = null;
            }

            if (fieldState != null)
            {
                if (fieldState.ErrorLevel != PXErrorLevel.Undefined)
                {
                    errorLevel = fieldState.ErrorLevel;
                }
                else
                {
                    errorLevel = PXErrorLevel.Warning;
                }
            }

            return new PXSetPropertyException(currentException, errorLevel, currentException.Message);
        }
        #endregion

        protected virtual void _(Events.RowSelecting<FSAppointmentDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;
            fsAppointmentDetRow.UIStatus = fsAppointmentDetRow.Status;

            if (fsAppointmentDetRow.IsService)
            {
                UpdateStaffRelatedUnboundFields(fsAppointmentDetRow, AppointmentServiceEmployees, LogRecords, null);
            }

            using (new PXConnectionScope())
            {
                FSSODet soDetLine = null;

                PXException exception;
                fsAppointmentDetRow.CanChangeMarkForPO = CanChangePOOptions(fsAppointmentDetRow, true, ref soDetLine, typeof(FSAppointmentDet.enablePO).Name, out exception);
            }
        }

        protected virtual void _(Events.RowSelected<FSAppointmentDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            bool includeIN = PXAccess.FeatureInstalled<FeaturesSet.distributionModule>()
                                && PXAccess.FeatureInstalled<FeaturesSet.inventory>()
                                    && ServiceOrderTypeSelected.Current.PostToSOSIPM == true;

            FSLineType.SetLineTypeList<FSAppointmentDet.lineType>(AppointmentDetails.Cache,
                                                                  fsAppointmentDetRow,
                                                                  includeIN,
                                                                  false,
                                                                  ServiceOrderTypeSelected.Current.Behavior == FSSrvOrdType.behavior.Values.RouteAppointment,
                                                                  true,
                                                                  false);

            if (fsAppointmentDetRow.IsPickupDelivery == false)
            {
                FSAppointmentDet_RowSelected_PartialHandler(e.Cache,
                                                                            fsAppointmentDetRow,
                                                                            SetupRecord.Current,
                                                                            ServiceOrderTypeSelected.Current,
                                                                            ServiceOrderRelated.Current,
                                                                            AppointmentSelected.Current);
            }

            if (fsAppointmentDetRow.IsService)
            {
                SetRequireSerialWarning(e.Cache, fsAppointmentDetRow);
            }

            bool disableSODetReferenceFields = fsAppointmentDetRow.IsPickupDelivery ? false : fsAppointmentDetRow.AppDetID > 0;

            // Move the old code of SetEnabled and SetPersistingCheck in previous methods to this new generic method
            // keeping the generic convention.
            X_RowSelected<FSAppointmentDet>(e.Cache,
                                                                            e.Args,
                                                                            ServiceOrderRelated.Current,
                                                                            ServiceOrderTypeSelected.Current,
                                                                            disableSODetReferenceFields: disableSODetReferenceFields,
                                            docAllowsActualFieldEdition: AppointmentRecords.Current?.NotStarted == false);

            POCreateVerifyValue(e.Cache, fsAppointmentDetRow, fsAppointmentDetRow.EnablePO);

            if (ServiceOrderTypeSelected.Current != null && ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.PROJECTS)
            {
                PXUIFieldAttribute.SetEnabled<FSScheduleDet.equipmentAction>(e.Cache, null, false);
            }

            bool callDisableField = false;
            List<Type> ignoreFieldList = new List<Type>();

            if (fsAppointmentDetRow.Status == ID.Status_AppointmentDet.CANCELED
                || fsAppointmentDetRow.Status == ID.Status_AppointmentDet.NOT_PERFORMED)
            {
                ignoreFieldList.Add(typeof(FSAppointmentDet.uiStatus));
                callDisableField = true;
            }
            else if (fsAppointmentDetRow.Status == ID.Status_AppointmentDet.RequestForPO)
            {
                ignoreFieldList.Add(typeof(FSAppointmentDet.uiStatus));

                if (fsAppointmentDetRow.CanChangeMarkForPO == true)
                {
                    ignoreFieldList.Add(typeof(FSAppointmentDet.enablePO));
                }

                if (fsAppointmentDetRow.EnablePO == true && fsAppointmentDetRow.CanChangeMarkForPO == true)
                {
                    ignoreFieldList.Add(typeof(FSAppointmentDet.pOSource));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.poVendorID));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.poVendorLocationID));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.curyUnitCost));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.estimatedQty));
                }

                callDisableField = true;
            }
            else if (fsAppointmentDetRow.IsLinkedItem == true)
            {
                ignoreFieldList.Add(typeof(FSAppointmentDet.SMequipmentID));
                ignoreFieldList.Add(typeof(FSAppointmentDet.newTargetEquipmentLineNbr));
                ignoreFieldList.Add(typeof(FSAppointmentDet.componentID));
                ignoreFieldList.Add(typeof(FSAppointmentDet.equipmentLineRef));

                if (ProjectDefaultAttribute.IsNonProject(AppointmentRecords.Current.ProjectID))
                {
                    ignoreFieldList.Add(typeof(FSAppointmentDet.isBillable));
                }

                if (fsAppointmentDetRow.IsBillable == true)
                {
                    ignoreFieldList.Add(typeof(FSAppointmentDet.curyUnitPrice));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.manualPrice));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.manualDisc));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.curyBillableExtPrice));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.curyExtCost));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.discPct));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.curyDiscAmt));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.isFree));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.taxCategoryID));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.acctID));
                    ignoreFieldList.Add(typeof(FSAppointmentDet.subID));
                }

                callDisableField = true;
            }

            if (callDisableField)
            {
                this.DisableAllDACFields(e.Cache, fsAppointmentDetRow, ignoreFieldList);
            }

            SetItemLineUIStatusList(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowInserting<FSAppointmentDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.LineRef == null)
            {
                fsAppointmentDetRow.LineRef = fsAppointmentDetRow.LineNbr.Value.ToString("0000");
            }

            if (fsAppointmentDetRow.SODetID != null)
            {
                return;
            }

            if (fsAppointmentDetRow.IsPickupDelivery)
            {
                var fsAppointmentDetServiceRows = AppointmentDetails.Select();

                if (fsAppointmentDetServiceRows.Count == 1)
                {
                    FSAppointmentDet fsAppointmentDetServiceRow = fsAppointmentDetServiceRows[0];
                    e.Cache.SetValueExt<FSAppointmentDet.sODetID>(fsAppointmentDetRow, fsAppointmentDetServiceRow.SODetID);
                }
            }
        }

        protected virtual void _(Events.RowInserted<FSAppointmentDet> e)
        {
            if (e.Row == null || AppointmentSelected.Current == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;
            fsAppointmentDetRow.UIStatus = fsAppointmentDetRow.Status;

            if (fsAppointmentDetRow.IsPickupDelivery)
            {
                return;
            }

            MarkHeaderAsUpdated(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowUpdating<FSAppointmentDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsInventoryItem)
            {
                EquipmentHelper.CheckReplaceComponentLines<FSAppointmentDet, FSAppointmentDet.equipmentLineRef>(e.Cache, AppointmentDetails.Select(), (FSAppointmentDet)e.NewRow);
            }
        }

        protected virtual void _(Events.RowUpdated<FSAppointmentDet> e)
        {
            if (e.Row == null || AppointmentSelected.Current == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;
            FSAppointmentDet fsAppointmentDetOldRow = e.OldRow;
            FSAppointment appt = AppointmentSelected.Current;

            fsAppointmentDetRow.UIStatus = fsAppointmentDetRow.Status;

            MarkHeaderAsUpdated(e.Cache, e.Row);

            if (this.IsCopyPasteContext 
                && (e.Row.LinkedEntityType == ListField_Linked_Entity_Type.APBill
                    || e.Row.LinkedEntityType == ListField_Linked_Entity_Type.ExpenseReceipt))
            {
                e.Cache.Delete(e.Row);
                return;
            }

            if (appt.NotStarted == true)
            {
                CheckIfManualPrice<FSAppointmentDet, FSAppointmentDet.estimatedQty>(e.Cache, e.Args);
            }
            else
            {
                CheckIfManualPrice<FSAppointmentDet, FSAppointmentDet.actualQty>(e.Cache, e.Args);
            }

            CheckSOIfManualCost(e.Cache, e.Args);

            if (e.ExternalCall == true && fsAppointmentDetRow?.StaffID != fsAppointmentDetOldRow?.StaffID)
            {
                InsertUpdateDelete_AppointmentDetService_StaffID(e.Cache, fsAppointmentDetRow, AppointmentServiceEmployees, fsAppointmentDetOldRow?.StaffID);
            }

            if (fsAppointmentDetRow.IsInventoryItem || fsAppointmentDetRow.IsPickupDelivery)
            {
                VerifyIsAlreadyPosted<FSAppointmentDet.inventoryID>(e.Cache, fsAppointmentDetRow, BillingCycleRelated.Current);
            }

            if (fsAppointmentDetRow.Status != fsAppointmentDetOldRow.Status 
                && fsAppointmentDetRow.Status == ID.Status_AppointmentDet.NOT_FINISHED
                && fsAppointmentDetRow.IsService == true)
            {
                DateTime? endTime = PXDBDateAndTimeAttribute.CombineDateTime(AppointmentRecords.Current.ExecutionDate, PXTimeZoneInfo.Now);

                var inProcessRelatedLogs = LogRecords.Select()
                                                     .AsEnumerable()
                                                     .RowCast<FSAppointmentLog>()
                                                     .Where(y => y.DetLineRef == fsAppointmentDetRow.LineRef && y.Status == ID.Status_Log.IN_PROCESS);

                foreach (FSAppointmentLog fsAppointmentLogRow in inProcessRelatedLogs)
                {
                    ChangeLogAndRelatedItemLinesStatus(fsAppointmentLogRow, ID.Status_Log.COMPLETED, endTime.Value, null, null);
                }
            }
        }

        protected virtual void _(Events.RowDeleting<FSAppointmentDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (e.ExternalCall == true
                    && fsAppointmentDetRow.IsAPBillItem == true
                    && AppointmentRecords.Ask(TX.Warning.FSLineLinkedToAPLine, MessageButtons.OKCancel) != WebDialogResult.OK)
            {
                e.Cancel = true;
            }

            if (fsAppointmentDetRow.IsService)
            {
                if (IsAppointmentBeingDeleted(fsAppointmentDetRow.AppointmentID, AppointmentRecords.Cache) == false
                    && ServiceLinkedToPickupDeliveryItem(this, fsAppointmentDetRow, AppointmentRecords.Current) == true)
                {
                    throw new PXException(TX.Error.SERVICE_LINKED_TO_PICKUP_DELIVERY_ITEMS);
                }

                foreach (FSAppointmentEmployee fsAppointmentEmployeeRow in AppointmentServiceEmployees.Select().AsEnumerable().Where(y => ((FSAppointmentEmployee)y).ServiceLineRef == fsAppointmentDetRow.LineRef))
                {
                    AppointmentServiceEmployees.Delete(fsAppointmentEmployeeRow);
                }

                foreach (FSAppointmentLog fsAppointmentLogRow in LogRecords.Select().AsEnumerable().Where(y => ((FSAppointmentLog)y).DetLineRef == fsAppointmentDetRow.LineRef))
                {
                    if (fsAppointmentLogRow.BAccountID != null)
                    {
                        fsAppointmentLogRow.DetLineRef = null;
                        LogRecords.Update(fsAppointmentLogRow);
                    }
                    else
                    {
                        LogRecords.Delete(fsAppointmentLogRow);
                    }
                }
            }
        }

        protected virtual void _(Events.RowDeleted<FSAppointmentDet> e)
        {
            if (e.Row == null || AppointmentSelected.Current == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsPickupDelivery)
            {
                return;
            }

            MarkHeaderAsUpdated(e.Cache, e.Row);
            ClearTaxes(AppointmentSelected.Current);
        }

        protected virtual void _(Events.RowPersisting<FSAppointmentDet> e)
        {
            if (e.Row == null || AppointmentRecords.Current == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;

            if (fsAppointmentDetRow.IsInventoryItem || fsAppointmentDetRow.IsPickupDelivery)
            {
                VerifyIsAlreadyPosted<FSAppointmentDet.inventoryID>(e.Cache, fsAppointmentDetRow, BillingCycleRelated.Current);
            }

            if (fsAppointmentDetRow.IsPickupDelivery == false)
            {
                BackupOriginalValues(e.Cache, e.Args);

                if (UpdateServiceOrder(AppointmentRecords.Current, this, e.Row, e.Operation, null) == false)
                {
                    return;
                }

                if (ApptLinesWithSrvOrdLineUpdated != null)
                {
                    FSSODet soLine;

                    if (ApptLinesWithSrvOrdLineUpdated.TryGetValue(fsAppointmentDetRow, out soLine))
                    {
                        fsAppointmentDetRow.SODetID = soLine.SODetID;

                        fsAppointmentDetRow.OrigSrvOrdNbr = soLine.RefNbr;
                        fsAppointmentDetRow.OrigLineNbr = soLine.LineNbr;
                    }
                }

                FSAppointmentDet_RowPersisting_PartialHandler(e.Cache,
                                                                              fsAppointmentDetRow,
                                                                              AppointmentRecords.Current,
                                                                              ServiceOrderTypeSelected.Current);
                if (fsAppointmentDetRow.IsInventoryItem)
                {
                    string errorMessage = string.Empty;

                    if (e.Operation != PXDBOperation.Delete
                            && !SharedFunctions.AreEquipmentFieldsValid(e.Cache, fsAppointmentDetRow.InventoryID, fsAppointmentDetRow.SMEquipmentID, fsAppointmentDetRow.NewTargetEquipmentLineNbr, fsAppointmentDetRow.EquipmentAction, ref errorMessage))
                    {
                        e.Cache.RaiseExceptionHandling<FSAppointmentDet.equipmentAction>(fsAppointmentDetRow, fsAppointmentDetRow.EquipmentAction, new PXSetPropertyException(errorMessage));
                    }

                    if (EquipmentHelper.CheckReplaceComponentLines<FSAppointmentDet, FSAppointmentDet.equipmentLineRef>(e.Cache, AppointmentDetails.Select(), fsAppointmentDetRow) == false)
                    {
                        return;
                    }
                }
            }

            VerifySrvOrdLineQty(e.Cache, e.Row, e.Row.EffTranQty, typeof(FSAppointmentDet.effTranQty), false);
            X_SetPersistingCheck<FSAppointmentDet>(e.Cache, e.Args, ServiceOrderRelated.Current, ServiceOrderTypeSelected.Current);

            if (e.Operation != PXDBOperation.Delete)
            {
                ValidateItemLineStatus(e.Cache, e.Row, AppointmentRecords.Current);
            }
        }

        protected virtual void _(Events.RowPersisted<FSAppointmentDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentDet fsAppointmentDetRow = e.Row;
            PXCache cache = e.Cache;

            if (Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.ExpenseReceipt)
                && e.TranStatus == PXTranStatus.Completed
                && e.Operation == PXDBOperation.Delete
                && this.AppointmentRecords.Cache.GetStatus(AppointmentSelected.Current) != PXEntryStatus.Deleted
                && fsAppointmentDetRow.IsExpenseReceiptItem == true)
            {
                GetServiceOrderEntryGraph(false).ClearFSDocExpenseReceipts(fsAppointmentDetRow.LinkedDocRefNbr);
            }

            if (fsAppointmentDetRow.IsPickupDelivery == false)
            {
                RestoreOriginalValues(e.Cache, e.Args);

                if (fsAppointmentDetRow.IsService)
                {
                    UpdateStaffRelatedUnboundFields(fsAppointmentDetRow, AppointmentServiceEmployees, LogRecords, null);
                }
            }
        }

        #endregion

        #region FSApptLineSplit
        protected virtual void _(Events.RowInserted<FSApptLineSplit> e)
        {
            MarkApptLineAsUpdated(e.Cache, e.Row);
        }
        protected virtual void _(Events.RowUpdated<FSApptLineSplit> e)
        {
            MarkApptLineAsUpdated(e.Cache, e.Row);
        }
        protected virtual void _(Events.RowDeleted<FSApptLineSplit> e)
        {
            MarkApptLineAsUpdated(e.Cache, e.Row);
        }
        protected virtual void _(Events.RowPersisting<FSApptLineSplit> e)
        {
            if (e.Row == null || AppointmentRecords.Current == null)
            {
                return;
            }

            FSApptLineSplit apptSplit = e.Row;

            if (UpdateServiceOrder(AppointmentRecords.Current, this, e.Row, e.Operation, null) == false)
            {
                return;
            }

            if (ApptSplitsWithSrvOrdSplitUpdated != null)
            {
                FSSODetSplit soSplit;

                if (ApptSplitsWithSrvOrdSplitUpdated.TryGetValue(apptSplit, out soSplit))
                {
                    apptSplit.OrigSrvOrdType = soSplit.SrvOrdType;
                    apptSplit.OrigSrvOrdNbr = soSplit.RefNbr;
                    apptSplit.OrigLineNbr = soSplit.LineNbr;
                    apptSplit.OrigSplitLineNbr = soSplit.SplitLineNbr;
                }
            }
        }
        #endregion

        #region FSPostDet

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated
        #endregion

        protected virtual void _(Events.RowSelecting<FSPostDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSPostDet fsPostDetRow = e.Row as FSPostDet;
            PXCache cache = e.Cache;

            if (fsPostDetRow.SOPosted == true)
            {
                using (new PXConnectionScope())
                {
                    var soOrderShipment = (SOOrderShipment)PXSelectReadonly<SOOrderShipment,
                                          Where<
                                              SOOrderShipment.orderNbr, Equal<Required<SOOrderShipment.orderNbr>>,
                                          And<
                                              SOOrderShipment.orderType, Equal<Required<SOOrderShipment.orderType>>>>>
                                          .Select(cache.Graph, fsPostDetRow.SOOrderNbr, fsPostDetRow.SOOrderType);

                    fsPostDetRow.InvoiceRefNbr = soOrderShipment?.InvoiceNbr;
                    fsPostDetRow.InvoiceDocType = soOrderShipment?.InvoiceType;
                }
            }
            else if (fsPostDetRow.ARPosted == true || fsPostDetRow.SOInvPosted == true)
            {
                fsPostDetRow.InvoiceRefNbr = fsPostDetRow.Mem_DocNbr;
                fsPostDetRow.InvoiceDocType = fsPostDetRow.ARDocType;
            }
            else if (fsPostDetRow.APPosted == true)
            {
                fsPostDetRow.InvoiceRefNbr = fsPostDetRow.Mem_DocNbr;
                fsPostDetRow.InvoiceDocType = fsPostDetRow.APDocType;
            }

            using (new PXConnectionScope())
            {
                FSPostBatch fsPostBatchRow = PXSelect<FSPostBatch,
                                             Where<
                                                 FSPostBatch.batchID, Equal<Required<FSPostBatch.batchID>>>>
                                             .Select(cache.Graph, fsPostDetRow.BatchID);
                fsPostDetRow.BatchNbr = fsPostBatchRow?.BatchNbr;
            }
        }

        protected virtual void _(Events.RowSelected<FSPostDet> e)
        {
        }

        protected virtual void _(Events.RowInserting<FSPostDet> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSPostDet> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSPostDet> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSPostDet> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSPostDet> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSPostDet> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSPostDet> e)
        {
        }

        protected virtual void _(Events.RowPersisted<FSPostDet> e)
        {
        }

        #endregion

        #region ARPaymentEvents

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated
        #endregion

        protected virtual void _(Events.RowSelecting<ARPayment> e)
        {
            if (e.Row == null)
            {
                return;
            }

            ARPayment arPaymentRow = (ARPayment)e.Row;

            using (new PXConnectionScope())
            {
                RecalcSOApplAmounts(this, arPaymentRow);
            }
        }

        protected virtual void _(Events.RowSelected<ARPayment> e)
        {
        }

        protected virtual void _(Events.RowInserting<ARPayment> e)
        {
        }

        protected virtual void _(Events.RowInserted<ARPayment> e)
        {
        }

        protected virtual void _(Events.RowUpdating<ARPayment> e)
        {
        }

        protected virtual void _(Events.RowUpdated<ARPayment> e)
        {
        }

        protected virtual void _(Events.RowDeleting<ARPayment> e)
        {
        }

        protected virtual void _(Events.RowDeleted<ARPayment> e)
        {
        }

        protected virtual void _(Events.RowPersisting<ARPayment> e)
        {
        }

        protected virtual void _(Events.RowPersisted<ARPayment> e)
        {
        }

        #endregion

        #region FSAppointmentLog

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        protected virtual void _(Events.FieldDefaulting<FSAppointmentLog, FSAppointmentLog.unitCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentLog fsAppointmentLogRow = e.Row;
            decimal? laborCost = CalculateLaborCost(e.Cache, fsAppointmentLogRow, this.AppointmentRecords.Current);

            if (laborCost != null)
            {
                e.NewValue = laborCost;
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentLog, FSAppointmentLog.curyUnitCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            object unitcost;
            e.Cache.RaiseFieldDefaulting<FSAppointmentLog.unitCost>(e.Row, out unitcost);

            if (unitcost != null && (decimal)unitcost != 0m)
            {
                decimal newval = (decimal)unitcost;
                CM.PXDBCurrencyAttribute.CuryConvCury(e.Cache, e.Row, newval, out newval, CommonSetupDecPl.PrcCst);
                e.NewValue = newval;
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentLog, FSAppointmentLog.trackOnService> e)
        {
            if (e.Row == null)
            {
                return;
            }

            e.NewValue = e.Row.DetLineRef != null;

            if (e.Row.Travel == true)
            {
                if (e.Row.BAccountID != null)
                {
                    e.NewValue = (bool)e.NewValue &&
                                 AppointmentServiceEmployees.Select()
                                                    .RowCast<FSAppointmentEmployee>()
                                                    .Where(_ => _.EmployeeID == e.Row.BAccountID && _.PrimaryDriver == true)
                                                    .Any();
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentLog, FSAppointmentLog.descr> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.Row.DetLineRef != null)
            {
                e.NewValue = e.Row.Descr;
            }
            else if (e.Row.Travel == true)
            {
                e.NewValue = PXMessages.LocalizeNoPrefix(TX.Type_Log.TRAVEL);
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentLog, FSAppointmentLog.projectID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.NewValue == null)
            {
                e.NewValue = AppointmentRecords.Current?.ProjectID;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentLog, FSAppointmentLog.billableQty> e)
        {
            if (e.Row == null)
            {
                return;
            }

            e.NewValue = PXDBQuantityAttribute.Round(decimal.Divide((decimal)(e.Row.BillableTimeDuration ?? 0), 60));
        }

        #endregion
        #region FieldUpdating
        protected virtual void _(Events.FieldUpdating<FSAppointmentLog, FSAppointmentLog.trackTime> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.Row.BAccountID != null)
            {
                bool allowTrackTime = SharedFunctions.GetBAccountType(this, e.Row.BAccountID) == BAccountType.EmployeeType;

                if (allowTrackTime == false)
                {
                    e.NewValue = false;
                }
            }
            else
            {
                e.NewValue = false;
            }
        }
        #endregion
        #region FieldVerifying
        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.detLineRef> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((string)e.NewValue != e.Row.DetLineRef)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.detLineRef));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.dateTimeBegin> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((DateTime?)e.NewValue != e.Row.DateTimeBegin)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.dateTimeBegin));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.dateTimeEnd> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((DateTime?)e.NewValue != e.Row.DateTimeEnd)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.dateTimeEnd));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.timeDuration> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((int?)e.NewValue != e.Row.TimeDuration)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.timeDuration));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.earningType> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((string)e.NewValue != e.Row.EarningType)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.earningType));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.costCodeID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((int?)e.NewValue != e.Row.CostCodeID)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.costCodeID));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.laborItemID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((int?)e.NewValue != e.Row.LaborItemID)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.laborItemID));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.trackTime> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((bool?)e.NewValue != e.Row.TrackTime)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.trackTime));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.isBillable> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((bool?)e.NewValue != e.Row.IsBillable)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.isBillable));
            }
        }

        protected virtual void _(Events.FieldVerifying<FSAppointmentLog, FSAppointmentLog.workgroupID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((int?)e.NewValue != e.Row.WorkgroupID)
            {
                VerifyTimeActivityUpdate(e.Cache, e.Row, nameof(FSAppointmentLog.workgroupID));
            }
        }
        #endregion
        #region FieldUpdated
        protected virtual void _(Events.FieldUpdated<FSAppointmentLog, FSAppointmentLog.bAccountID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            e.Cache.SetValueExt<FSAppointmentLog.bAccountType>(e.Row, SharedFunctions.GetBAccountType(this, e.Row.BAccountID));

            SetLogInfoFromDetails(e.Cache, e.Row);

            e.Cache.SetDefaultExt<FSAppointmentLog.earningType>(e.Row);
            e.Cache.SetDefaultExt<FSAppointmentLog.trackTime>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentLog, FSAppointmentLog.timeDuration> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.Row.TimeDuration == (int?)e.OldValue)
            {
                return;
            }

                int duration = 0;

                if (e.Row.TimeDuration != null)
                {
                    duration = (int)e.Row.TimeDuration;
                }

            if (duration >= 0)
            {
                if (e.Row.DateTimeBegin.HasValue
                        && (duration > 0 || e.Row.DateTimeEnd.HasValue))
                {
                    DateTime newEnd = e.Row.DateTimeBegin.Value.AddMinutes((double)duration);

                    if (e.Row.DateTimeEnd != newEnd)
                    {
                        e.Cache.SetValueExt<FSAppointmentLog.dateTimeEnd>(e.Row, newEnd);
                    }
                }
            }
            else
            {
                if (e.Row.TrackTime == true)
                {
                    e.Cache.SetValue<FSAppointmentLog.status>(e.Row, ID.Status_Log.COMPLETED);
                    e.Cache.SetValue<FSAppointmentLog.dateTimeEnd>(e.Row, null);
                }
                else
                {
                    e.Cache.SetValueExt<FSAppointmentLog.timeDuration>(e.Row, duration*(-1));
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentLog, FSAppointmentLog.detLineRef> e)
        {
            if (e.Row == null)
            {
                return;
            }

            SetLogInfoFromDetails(e.Cache, e.Row);

            e.Cache.SetDefaultExt<FSAppointmentLog.earningType>(e.Row);
            e.Cache.SetDefaultExt<FSAppointmentLog.trackTime>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentLog, FSAppointmentLog.laborItemID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointmentLog fsAppointmentLogRow = e.Row;
            e.Cache.SetDefaultExt<FSAppointmentLog.curyUnitCost>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentLog, FSAppointmentLog.travel> e)
        {
            if (e.Row == null)
                return;

            FSAppointmentDet apptDet = null;

            if (string.IsNullOrWhiteSpace(e.Row.DetLineRef) == false)
            {
                apptDet = AppointmentDetails.Select().RowCast<FSAppointmentDet>().
                    Where(r => r.LineRef == e.Row.DetLineRef && r.IsTravelItem == e.Row.Travel).FirstOrDefault();

                if (apptDet == null)
                {
                    e.Cache.SetValueExt<FSAppointmentLog.detLineRef>(e.Row, null);
                }
            }

            string logType = null;
            if (e.Row.Travel == true)
            {
                logType = FSAppointmentLog.itemType.Values.Travel;
            }
            else
            {
                logType = GetLogTypeCheckingTravelWithLogFormula(e.Cache, apptDet);
                
                if (logType == FSAppointmentLog.itemType.Values.Travel)
                {
                    logType = FSAppointmentLog.itemType.Values.Staff;
                }
            }

            if (logType != e.Row.ItemType)
                e.Cache.SetValueExt<FSAppointmentLog.itemType>(e.Row, logType);

            if (e.Row.Travel != (bool?)e.OldValue)
                e.Cache.SetDefaultExt<FSAppointmentLog.descr>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<FSAppointmentLog, FSAppointmentLog.trackTime> e)
        {
            if (e.Row == null || e.Row.TrackTime == (bool?)e.OldValue)
            {
                return;
            }

            if (e.Row.TrackTime == false && e.Row.TimeDuration < 0)
            {
                e.Cache.SetValueExt<FSAppointmentLog.timeDuration>(e.Row, 0);
            }
        }
        #endregion

        protected virtual void _(Events.RowSelecting<FSAppointmentLog> e)
        {
        }

        protected virtual void _(Events.RowSelected<FSAppointmentLog> e)
        {
            if (e.Row == null)
            {
                return;
            }

            EnableDisable_TimeRelatedLogFields(
                                                               e.Cache,
                                                               e.Row,
                                                               SetupRecord.Current,
                                                               ServiceOrderTypeSelected.Current,
                                                               AppointmentRecords.Current);
        }

        protected virtual void _(Events.RowInserting<FSAppointmentLog> e)
        {
            if (e.Row == null)
            {
                return;
            }
        }

        protected virtual void _(Events.RowInserted<FSAppointmentLog> e)
        {
            if (e.Row == null)
            {
                return;
            }

            OnRowInsertedFSAppointmentLog(e.Row);
        }

        protected virtual void _(Events.RowUpdating<FSAppointmentLog> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSAppointmentLog> e)
        {
            MarkHeaderAsUpdated(e.Cache, e.Row);

            if (e.OldRow != null && e.Row != null
                && e.Cache.ObjectsEqual<FSAppointmentLog.detLineRef>(e.Row, e.OldRow)
                && e.Cache.ObjectsEqual<FSAppointmentLog.timeDuration>(e.Row, e.OldRow) 
                && e.Cache.ObjectsEqual<FSAppointmentLog.trackOnService>(e.Row, e.OldRow) 
                && e.Cache.ObjectsEqual<FSAppointmentLog.status>(e.Row, e.OldRow))
            {
                return;
            }

            if ((e.ExternalCall || e.Cache.Graph.IsImport)
                && ServiceOrderTypeSelected.Current.AllowManualLogTimeEdition == true
                && (!e.Cache.ObjectsEqual<FSAppointmentLog.timeDuration>(e.Row, e.OldRow)
                    || !e.Cache.ObjectsEqual<FSAppointmentLog.dateTimeEnd>(e.Row, e.OldRow)))
            {
                e.Row.KeepDateTimes = true;
            }

            OnRowDeletedFSAppointmentLog(e.OldRow);
            OnRowInsertedFSAppointmentLog(e.Row);
        }

        protected virtual void _(Events.RowDeleting<FSAppointmentLog> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSAppointmentLog> e)
        {
            if (e.Row == null)
            {
                return;
            }

            OnRowDeletedFSAppointmentLog(e.Row);
        }

        protected virtual void _(Events.RowPersisting<FSAppointmentLog> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSAppointment fSAppointmentRow = AppointmentRecords.Current;

            if (e.Operation != PXDBOperation.Delete)
            {
                ValidateLogStatus(e.Cache, e.Row, fSAppointmentRow);

                if (fSAppointmentRow != null
                    && e.Row.ItemType != FSAppointmentLog.itemType.Values.Travel)
                {
                    if (fSAppointmentRow.ActualDateTimeBegin > e.Row.DateTimeBegin)
                    {
                        e.Cache.RaiseExceptionHandling<FSAppointmentLog.dateTimeBegin>(
                                            e.Row,
                                            e.Row.DateTimeBegin,
                                            new PXException(TX.Error.LOG_START_CANNOT_BE_PRIOR_APPOINTMENT_START));
                    }

                    if (fSAppointmentRow.ActualDateTimeEnd < e.Row.DateTimeEnd)
                    {
                        e.Cache.RaiseExceptionHandling<FSAppointmentLog.dateTimeEnd>(
                                            e.Row,
                                            e.Row.DateTimeEnd,
                                            new PXException(TX.Error.LOG_END_CANNOT_BE_GREATER_APPOINTMENT_END));
                    }
                }
            }
        }

        protected virtual void _(Events.RowPersisted<FSAppointmentLog> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.TranStatus == PXTranStatus.Open)
            {
                if (SkipTimeCardUpdate == false)
                {
                    InsertUpdateDeleteTimeActivities(AppointmentRecords.Current, ServiceOrderRelated.Current, e.Row, e.Cache);
                }
            }
        }
        #endregion

        #region FSLogActionFilter
        protected virtual void _(Events.FieldDefaulting<FSLogActionFilter, FSLogActionFilter.logDateTime> e)
        {
            if (AppointmentRecords.Current == null)
            {
                return;
            }

            e.NewValue = PXDBDateAndTimeAttribute.CombineDateTime(Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
        }

        protected virtual void _(Events.RowSelected<FSLogActionFilter> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSLogActionFilter filterRow = e.Row;

            FSLogTypeAction.SetLineTypeList<FSLogActionFilter.type>(e.Cache, filterRow, filterRow.Action);

            SetVisibleCompletePauseLogActionGrid(filterRow);
        }

        protected virtual void _(Events.RowUpdated<FSLogActionFilter> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (ServiceOrderTypeSelected.Current?.SetTimeInHeaderBasedOnLog == false)
            {
                if (e.Row.Type != FSLogActionFilter.type.Values.Travel
                    && e.Row.Action == ID.LogActions.START
                    && AppointmentRecords.Current.ActualDateTimeBegin > e.Row.LogDateTime)
                {
                    e.Cache.RaiseExceptionHandling<FSLogActionFilter.logDateTime>(
                                        e.Row,
                                        e.Row.LogDateTime,
                                        new PXException(TX.Error.LOG_START_CANNOT_BE_PRIOR_APPOINTMENT_START));
                }

                if (e.Row.Type != FSLogActionFilter.type.Values.Travel
                    && e.Row.Action == ID.LogActions.COMPLETE
                    && AppointmentRecords.Current.ActualDateTimeEnd < e.Row.LogDateTime)
                {
                    e.Cache.RaiseExceptionHandling<FSLogActionFilter.logDateTime>(
                                        e.Row,
                                        e.Row.LogDateTime,
                                        new PXException(TX.Error.LOG_END_CANNOT_BE_GREATER_APPOINTMENT_END));
                }
            }
        }
        #endregion

        #region FSSiteStatusFilter
        protected virtual void _(Events.RowSelected<FSSiteStatusFilter> e)
        {
            FSSiteStatusFilter row = (FSSiteStatusFilter)e.Row;

            bool includeIN = PXAccess.FeatureInstalled<FeaturesSet.distributionModule>()
                                && PXAccess.FeatureInstalled<FeaturesSet.inventory>()
                                    && ServiceOrderTypeSelected.Current?.PostToSOSIPM == true;
            row.IncludeIN = includeIN;

            if (row != null && !includeIN)
            {
                row.OnlyAvailable = false;
                PXUIFieldAttribute.SetVisible<FSSiteStatusFilter.onlyAvailable>(e.Cache, row, includeIN);
                PXUIFieldAttribute.SetVisible<FSSiteStatusFilter.inventory_Wildcard>(e.Cache, row, includeIN);
                PXUIFieldAttribute.SetVisible<FSSiteStatusFilter.mode>(e.Cache, row, includeIN);
                PXUIFieldAttribute.SetVisible<FSSiteStatusFilter.barCode>(e.Cache, row, includeIN);
                PXUIFieldAttribute.SetVisible<FSSiteStatusFilter.barCodeWildcard>(e.Cache, row, includeIN);
                PXUIFieldAttribute.SetVisible<FSSiteStatusFilter.siteID>(e.Cache, row, includeIN);
            }

            FSLineType.SetLineTypeList<FSSiteStatusFilter.lineType>(e.Cache,
                                                                  row,
                                                                  includeIN,
                                                                  false,
                                                                  false,
                                                                  false,
                                                                  true);
        }
        #endregion

        #region FSBillHistory
        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated
        #endregion

        protected virtual void _(Events.RowSelecting<FSBillHistory> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSBillHistory fsBillHistoryRow = (FSBillHistory)e.Row;
            CalculateBillHistoryUnboundFields(e.Cache, fsBillHistoryRow);
        }

        protected virtual void _(Events.RowSelected<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowInserting<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowPersisted<FSBillHistory> e)
        {
        }
        #endregion

        #endregion

        #region Enable/Disable methods
        public virtual void EnableDisable_Document(FSAppointment fsAppointmentRow,
                                          FSServiceOrder fsServiceOrderRow,
                                          FSSetup fsSetupRow,
                                          FSBillingCycle fsBillingCycleRow,
                                          FSSrvOrdType fsSrvOrdTypeRow,
                                          bool skipTimeCardUpdate,
                                          bool? isBeingCalledFromQuickProcess)
        {
            bool enableServicesTab = true;
            bool enablePickupTab = false;

            if (fsServiceOrderRow != null && fsSrvOrdTypeRow != null)
            {
                if (fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
                {
                    enableServicesTab = fsServiceOrderRow.CustomerID != null;
                }
            }

            bool? initialAllowUpdateValue = AppointmentRecords.Cache.AllowUpdate;
            bool enableInsertUpdate = CanUpdateAppointment(fsAppointmentRow, fsSrvOrdTypeRow) || skipTimeCardUpdate || (isBeingCalledFromQuickProcess ?? false);
            bool enableDelete = CanDeleteAppointment(fsAppointmentRow, fsServiceOrderRow, fsSrvOrdTypeRow);

            // This is needed for Apppointment Closing Screen because the navigation functionality fails there 
            // if the caches for these views are enable/disable.
            if (this.Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.ROUTE_CLOSING))
            {
                AppointmentSelected.Cache.AllowInsert = enableInsertUpdate;
                AppointmentSelected.Cache.AllowUpdate = enableInsertUpdate || this.IsMobile == true || fsAppointmentRow.Awaiting == true;
                AppointmentSelected.Cache.AllowDelete = enableDelete;

                AppointmentRecords.Cache.AllowInsert = true;
                AppointmentRecords.Cache.AllowUpdate = enableInsertUpdate || this.IsMobile == true || fsAppointmentRow.Awaiting == true;
                AppointmentRecords.Cache.AllowDelete = enableDelete;
            }

            if (initialAllowUpdateValue != AppointmentRecords.Cache.AllowUpdate)
            {
                PXUIFieldAttribute.SetEnabled(AppointmentRecords.Cache, fsAppointmentRow, AppointmentRecords.Cache.AllowUpdate);
            }

            AppointmentDetails.Cache.AllowInsert = enableInsertUpdate && enableServicesTab;
            AppointmentDetails.Cache.AllowUpdate = enableInsertUpdate && enableServicesTab;
            AppointmentDetails.Cache.AllowDelete = enableInsertUpdate && enableServicesTab;

            var fsLogCache = this.LogRecords.Cache;

            if (fsLogCache != null)
            {
                fsLogCache.AllowInsert = enableInsertUpdate;
                fsLogCache.AllowUpdate = enableInsertUpdate;
                fsLogCache.AllowDelete = enableInsertUpdate;
            }

            AppointmentServiceEmployees.Cache.AllowInsert = enableInsertUpdate;
            AppointmentServiceEmployees.Cache.AllowUpdate = enableInsertUpdate;
            AppointmentServiceEmployees.Cache.AllowDelete = enableInsertUpdate;

            ServiceOrder_Contact.Cache.AllowInsert = enableInsertUpdate;
            ServiceOrder_Contact.Cache.AllowUpdate = enableInsertUpdate;
            ServiceOrder_Contact.Cache.AllowDelete = enableInsertUpdate;

            ServiceOrder_Address.Cache.AllowInsert = enableInsertUpdate;
            ServiceOrder_Address.Cache.AllowUpdate = enableInsertUpdate;
            ServiceOrder_Address.Cache.AllowDelete = enableInsertUpdate;

            AppointmentResources.Cache.AllowInsert = enableInsertUpdate;
            AppointmentResources.Cache.AllowUpdate = enableInsertUpdate;
            AppointmentResources.Cache.AllowDelete = enableInsertUpdate;

            PXUIFieldAttribute.SetEnabled<FSAppointment.customerID>(AppointmentRecords.Cache,
                                                                    fsAppointmentRow,
                                                                    ServiceOrderRelated.Cache.GetStatus(fsServiceOrderRow) == PXEntryStatus.Inserted
                                                                        && fsSrvOrdTypeRow.BAccountRequired == true
                                                                        && (fsAppointmentRow.MaxLineNbr == 0
                                                                            || fsAppointmentRow.MaxLineNbr == null));

            if (fsServiceOrderRow != null)
            { 
                bool isEnabledCustomerID = AllowEnableCustomerID(fsServiceOrderRow);

                PXDefaultAttribute.SetPersistingCheck<FSAppointment.customerID>(AppointmentRecords.Cache,
                                                                                 fsAppointmentRow,
                                                                                 fsServiceOrderRow.BAccountRequired != false && isEnabledCustomerID ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
            }

            enablePickupTab = fsSrvOrdTypeRow?.Behavior == FSSrvOrdType.behavior.Values.RouteAppointment;

            EnableDisable_ScheduleDateTimes(AppointmentRecords.Cache, fsAppointmentRow, enableInsertUpdate && !enablePickupTab);
            EnableDisable_UnreachedCustomer(AppointmentRecords.Cache, fsAppointmentRow, enableInsertUpdate);
            EnableDisable_AppointmentActualDateTimes(AppointmentRecords.Cache, fsSetupRow, fsAppointmentRow, fsSrvOrdTypeRow);

            if (fsServiceOrderRow != null)
            {
                bool nonProject = ProjectDefaultAttribute.IsNonProject(fsAppointmentRow.ProjectID);
                PXUIFieldAttribute.SetVisible<FSAppointment.dfltProjectTaskID>(AppointmentRecords.Cache, fsAppointmentRow, !nonProject);
                PXUIFieldAttribute.SetEnabled<FSAppointment.dfltProjectTaskID>(AppointmentRecords.Cache, fsAppointmentRow, enableInsertUpdate && !nonProject);
                PXUIFieldAttribute.SetRequired<FSAppointment.dfltProjectTaskID>(AppointmentRecords.Cache, !nonProject);
                PXDefaultAttribute.SetPersistingCheck<FSAppointment.dfltProjectTaskID>(AppointmentRecords.Cache, fsAppointmentRow, !nonProject ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
            }

            if (fsAppointmentRow != null)
            {
                PXUIFieldAttribute.SetEnabled<FSAppointment.soRefNbr>(AppointmentRecords.Cache, fsAppointmentRow, fsAppointmentRow.SOID != null && fsAppointmentRow.SOID < 0);
            }

            PXUIFieldAttribute.SetEnabled<FSAppointment.routeDocumentID>(AppointmentRecords.Cache, fsAppointmentRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointment.executionDate>(AppointmentRecords.Cache, fsAppointmentRow, enableInsertUpdate);

            bool enableHandleManuallyActualTime = fsSrvOrdTypeRow?.OnCompleteApptSetEndTimeInHeader == true || fsSrvOrdTypeRow?.SetTimeInHeaderBasedOnLog == true;

            PXUIFieldAttribute.SetEnabled<FSAppointment.handleManuallyActualTime>(AppointmentRecords.Cache, fsAppointmentRow, enableHandleManuallyActualTime);

            bool enableServiceContractFields = fsBillingCycleRow != null
                                               && fsServiceOrderRow?.BillingBy == ID.Billing_By.APPOINTMENT
                                               && (PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>()
                                                   || PXAccess.FeatureInstalled<FeaturesSet.routeManagementModule>());

            PXUIFieldAttribute.SetVisible<FSAppointment.billServiceContractID>(AppointmentRecords.Cache, fsAppointmentRow, enableServiceContractFields);
            PXUIFieldAttribute.SetVisible<FSAppointment.billContractPeriodID>(AppointmentRecords.Cache, fsAppointmentRow, enableServiceContractFields && fsAppointmentRow.BillServiceContractID != null);
        }

        public virtual void EnableDisable_ScheduleDateTimes(PXCache cache, FSAppointment fsAppointmentRow, bool masterEnable)
        {
            PXUIFieldAttribute.SetEnabled<FSAppointment.scheduledDateTimeBegin>(cache, fsAppointmentRow, masterEnable);
            PXUIFieldAttribute.SetEnabled<FSAppointment.scheduledDateTimeEnd>(cache, fsAppointmentRow, masterEnable);
        }

        public virtual void EnableDisable_UnreachedCustomer(PXCache cache, FSAppointment fsAppointmentRow, bool masterEnable)
        {
            bool enable = false;

            if (fsAppointmentRow != null)
            {
                enable = fsAppointmentRow.NotStarted == true;
            }

            PXUIFieldAttribute.SetEnabled<FSAppointment.unreachedCustomer>(cache, fsAppointmentRow, enable && masterEnable);
        }

        public virtual void EnableDisable_AppointmentActualDateTimes(PXCache appointmentCache, FSSetup fsSetupRow, FSAppointment fsAppointmentRow, FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsSetupRow == null || fsAppointmentRow == null || fsSrvOrdTypeRow == null)
            {
                return;
            }

            bool enableActualStartDateTime = fsAppointmentRow == null ? false : fsAppointmentRow.NotStarted == false && fsAppointmentRow.Hold == false;

            bool enableActualEndDateTime = enableActualStartDateTime && fsAppointmentRow.ActualDateTimeBegin.HasValue;

            PXUIFieldAttribute.SetEnabled<FSAppointment.actualDateTimeBegin>(appointmentCache, fsAppointmentRow, enableActualStartDateTime);
            PXUIFieldAttribute.SetEnabled<FSAppointment.actualDateTimeEnd>(appointmentCache, fsAppointmentRow, enableActualEndDateTime);
        }
        #endregion

        #region Selector Methods

        #region Staff Selector

        [PXCopyPasteHiddenView]
        public PXFilter<StaffSelectionFilter> StaffSelectorFilter;
        [PXCopyPasteHiddenView]
        public StaffSelectionHelper.SkillRecords_View SkillGridFilter;
        [PXCopyPasteHiddenView]
        public StaffSelectionHelper.LicenseTypeRecords_View LicenseTypeGridFilter;
        [PXCopyPasteHiddenView]
        public StaffSelectionHelper.StaffRecords_View StaffRecords;

        public IEnumerable skillGridFilter()
        {
            return StaffSelectionHelper.SkillFilterDelegate(this, AppointmentDetails, StaffSelectorFilter, SkillGridFilter); 
        }

        public IEnumerable licenseTypeGridFilter()
        {
            return StaffSelectionHelper.LicenseTypeFilterDelegate(this, AppointmentDetails, StaffSelectorFilter, LicenseTypeGridFilter);
        }

        protected virtual IEnumerable staffRecords()
        {
            return StaffSelectionHelper.StaffRecordsDelegate(AppointmentServiceEmployees,
                                                             SkillGridFilter,
                                                             LicenseTypeGridFilter,
                                                             StaffSelectorFilter);
        }

        protected virtual void _(Events.FieldUpdated<StaffSelectionFilter, StaffSelectionFilter.serviceLineRef> e)
        {
            if (e.Row == null)
            {
                return;
            }

            SkillGridFilter.Cache.Clear();
            LicenseTypeGridFilter.Cache.Clear();
            StaffRecords.Cache.Clear();
        }

        protected virtual void _(Events.RowUpdated<BAccountStaffMember> e)
        {
            BAccountStaffMember bAccountStaffMemberRow = (BAccountStaffMember)e.Row;
            PXCache cache = e.Cache;

            if (StaffSelectorFilter.Current != null)
            {
                if (bAccountStaffMemberRow.Selected == true)
                {
                    if (AppointmentDetails.Current != null)
                    {
                        if (AppointmentDetails.Current.LineRef != StaffSelectorFilter.Current.ServiceLineRef)
                        {
                            AppointmentDetails.Current = AppointmentDetails.Search<FSAppointmentDet.lineRef>(StaffSelectorFilter.Current.ServiceLineRef);
                        }

                        if (AppointmentServiceEmployees.Select().RowCast<FSAppointmentEmployee>().Where(_ => _.ServiceLineRef == StaffSelectorFilter.Current.ServiceLineRef).Any() == false)
                        {
                            AppointmentDetails.Current.StaffID = bAccountStaffMemberRow.BAccountID;
                        }
                        else
                        {
                            AppointmentDetails.Current.StaffID = null;
                        }
                    }

                    FSAppointmentEmployee fsFSAppointmentEmployeeRow = new FSAppointmentEmployee
                    {
                        EmployeeID = bAccountStaffMemberRow.BAccountID,
                        ServiceLineRef = StaffSelectorFilter.Current.ServiceLineRef
                    };

                    AppointmentServiceEmployees.Insert(fsFSAppointmentEmployeeRow);
                }
                else
                {
                    FSAppointmentEmployee fsFSAppointmentEmployeeRow = PXSelectJoin<FSAppointmentEmployee,
                                                                       LeftJoin<FSAppointment,
                                                                       On<
                                                                           FSAppointment.appointmentID, Equal<FSAppointmentEmployee.appointmentID>>,
                                                                       LeftJoin<FSSODet,
                                                                       On<
                                                                           FSSODet.sOID, Equal<FSAppointment.sOID>,
                                                                           And<FSSODet.lineRef, Equal<FSAppointmentEmployee.serviceLineRef>>>>>,
                                                                       Where2<
                                                                           Where<
                                                                               FSSODet.lineRef, Equal<Required<FSSODet.lineRef>>,
                                                                               Or<
                                                                                   Where<
                                                                                       FSSODet.lineRef, IsNull,
                                                                                       And<Required<FSSODet.lineRef>, IsNull>>>>,
                                                                           And<
                                                                               Where<
                                                                                   FSAppointmentEmployee.appointmentID, Equal<Current<FSAppointment.appointmentID>>,
                                                                                   And<FSAppointmentEmployee.employeeID, Equal<Required<FSAppointmentEmployee.employeeID>>>>>>>
                                                                       .Select(cache.Graph, StaffSelectorFilter.Current.ServiceLineRef, StaffSelectorFilter.Current.ServiceLineRef, bAccountStaffMemberRow.BAccountID);

                    fsFSAppointmentEmployeeRow = (FSAppointmentEmployee)AppointmentServiceEmployees.Cache.Locate(fsFSAppointmentEmployeeRow);

                    if (fsFSAppointmentEmployeeRow != null)
                    {
                        AppointmentServiceEmployees.Delete(fsFSAppointmentEmployeeRow);
                    }
                }
            }

            StaffRecords.View.RequestRefresh();
        }

        #region OpenStaffSelectorFromServiceTab
        public PXAction<FSAppointment> openStaffSelectorFromServiceTab;
        [PXButton]
        [PXUIField(DisplayName = "Add Staff", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void OpenStaffSelectorFromServiceTab()
        {
            ServiceOrder_Contact.Current = ServiceOrder_Contact.SelectSingle();
            ServiceOrder_Address.Current = ServiceOrder_Address.SelectSingle();

            if (ServiceOrderRelated.Current != null)
            {
                StaffSelectorFilter.Current.PostalCode = ServiceOrder_Address.Current.PostalCode;
                StaffSelectorFilter.Current.ProjectID = ServiceOrderRelated.Current.ProjectID;
            }

            if (AppointmentSelected.Current != null)
            {
                StaffSelectorFilter.Current.ScheduledDateTimeBegin = AppointmentSelected.Current.ScheduledDateTimeBegin;
            }

            FSAppointmentDet fsAppointmentDetRow = AppointmentDetails.Current;

            if (fsAppointmentDetRow != null && fsAppointmentDetRow.LineType == ID.LineType_ALL.SERVICE)
            {
                StaffSelectorFilter.Current.ServiceLineRef = fsAppointmentDetRow.LineRef;
            }
            else
            {
                StaffSelectorFilter.Current.ServiceLineRef = null;
            }

            SkillGridFilter.Cache.Clear();
            LicenseTypeGridFilter.Cache.Clear();
            StaffRecords.Cache.Clear();

            StaffSelectionHelper appStaffSelector = new StaffSelectionHelper();
            appStaffSelector.LaunchStaffSelector(this, StaffSelectorFilter);
        }
        #endregion

        #region OpenStaffSelectorFromStaffTab
        public PXAction<FSAppointment> openStaffSelectorFromStaffTab;
        [PXButton]
        [PXUIField(DisplayName = "Add Staff", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void OpenStaffSelectorFromStaffTab()
        {
            ServiceOrder_Contact.Current = ServiceOrder_Contact.SelectSingle();
            ServiceOrder_Address.Current = ServiceOrder_Address.SelectSingle();

            if (ServiceOrderRelated.Current != null)
            {
                StaffSelectorFilter.Current.PostalCode = ServiceOrder_Address.Current.PostalCode;
                StaffSelectorFilter.Current.ProjectID = ServiceOrderRelated.Current.ProjectID;
            }

            if (AppointmentRecords.Current != null)
            {
                StaffSelectorFilter.Current.ScheduledDateTimeBegin = AppointmentRecords.Current.ScheduledDateTimeBegin;
            }

            StaffSelectorFilter.Current.ServiceLineRef = null;

            SkillGridFilter.Cache.Clear();
            LicenseTypeGridFilter.Cache.Clear();
            StaffRecords.Cache.Clear();

            StaffSelectionHelper appStaffSelector = new StaffSelectionHelper();
            appStaffSelector.LaunchStaffSelector(this, StaffSelectorFilter);
        }
        #endregion

        #endregion

        #endregion

        protected virtual void MarkHeaderAsUpdated(PXCache cache, object row)
        {
            if (row == null || AppointmentSelected.Current == null)
            {
                return;
            }

            if (AppointmentSelected.Cache.GetStatus(AppointmentSelected.Current) == PXEntryStatus.Notchanged && AppointmentSelected.Current.RefNbr != null)
            {
                AppointmentSelected.Cache.SetStatus(AppointmentSelected.Current, PXEntryStatus.Updated);
            }

            AppointmentSelected.Current.MustUpdateServiceOrder = true;
        }

        public virtual void MarkApptLineAsUpdated(PXCache cache, FSApptLineSplit lineSplit)
        {
            if (lineSplit == null)
            {
                return;
            }

            FSAppointmentDet apptLine = (FSAppointmentDet)PXParentAttribute.SelectParent(cache, lineSplit, typeof(FSAppointmentDet));

            if (apptLine == null)
            {
                return;
            }

            if (AppointmentDetails.Cache.GetStatus(apptLine) == PXEntryStatus.Notchanged)
            {
                AppointmentDetails.Cache.SetStatus(apptLine, PXEntryStatus.Updated);
            }
        }

        #region Methods to handle line inserts and updates

        public virtual void InsertServiceOrderDetailsInAppointment(PXResultset<FSSODet> bqlResultSet_FSSODet, PXCache cacheAppDetails)
        {
            foreach (FSSODet fsSODetRow in bqlResultSet_FSSODet)
            {
                var fsAppointmentDetRow = new FSAppointmentDet();

                AppointmentEntry.InsertDetailLine<FSAppointmentDet, FSSODet>(this.AppointmentDetails.Cache,
                                                                             fsAppointmentDetRow,
                                                                             this.Caches[typeof(FSSODet)],
                                                                             fsSODetRow,
                                                                             null,
                                                                             fsSODetRow.SODetID,
                                                                             copyTranDate: false,
                                                                             tranDate: fsSODetRow.TranDate,
                                                                             SetValuesAfterAssigningSODetID: false,
                                                                             copyingFromQuote: false);
            }
        }

        public virtual void CopyAppointmentLineValues<TargetRowType, SourceRowType>(PXCache targetCache,
                                                                                    object objTargetRow,
                                                                                    PXCache sourceCache,
                                                                                    object objSourceRow,
                                                                                    bool copyTranDate,
                                                                                    DateTime? tranDate,
                                                                                    bool ForceFormulaCalculation)
            where TargetRowType : class, IBqlTable, IFSSODetBase, new()
            where SourceRowType : class, IBqlTable, IFSSODetBase, new()
        {
            var targetRow = (TargetRowType)objTargetRow;
            var sourceRow = (SourceRowType)objSourceRow;
            FSSODet fsSODetRow = null;
            FSAppointmentDet fsAppointmentDetRow = null;
            TargetRowType oldRow = null;

            if (ForceFormulaCalculation == true)
            {
                // This row copy is to be used with cache.RaiseRowUpdated in order to accumulate totals with Formulas.
                oldRow = (TargetRowType)targetCache.CreateCopy(targetRow);
            }

            if (targetRow is FSSODet)
            {
                fsSODetRow = (FSSODet)objTargetRow;

                if (copyTranDate)
                {
                    fsSODetRow.TranDate = tranDate;
                }
            }
            else
            {
                fsAppointmentDetRow = (FSAppointmentDet)objTargetRow;

                if (copyTranDate)
                {
                    fsAppointmentDetRow.TranDate = tranDate;
                }
            }

            // **********************************************************************************************************
            // The sequence of fields in this method is VERY IMPORTANT because
            // it determines the proper validation and assignment of values
            // **********************************************************************************************************

            targetCache.SetValueExtIfDifferent<FSSODet.lineType>(targetRow, sourceRow.LineType);


            targetRow = CopyDependentFieldsOfSODet<TargetRowType, SourceRowType>(targetCache,
                                                                                 targetRow,
                                                                                 sourceCache,
                                                                                 sourceRow,
                                                                                 copyingFromQuote: false);

            if (ForceFormulaCalculation == true)
            {
                // This cache.RaiseRowUpdated is required to accumulate totals with Formulas
                // ONLY when this method is called from outside any row event.
                targetCache.RaiseRowUpdated(targetRow, oldRow);
            }
        }

        public static NewRowType InsertDetailLine<NewRowType, SourceRowType>(PXCache newRowCache,
                                                                             object objNewRow,
                                                                             PXCache sourceCache,
                                                                             object objSourceRow,
                                                                             Guid? noteID,
                                                                             int? soDetID,
                                                                             bool copyTranDate,
                                                                             DateTime? tranDate,
                                                                             bool SetValuesAfterAssigningSODetID,
                                                                             bool copyingFromQuote)
            where NewRowType : class, IBqlTable, IFSSODetBase, new()
            where SourceRowType : class, IBqlTable, IFSSODetBase, new()
        {
            var newRow = (NewRowType)objNewRow;
            var sourceRow = (SourceRowType)objSourceRow;
            FSSODet fsSODetRow = null;
            FSAppointmentDet fsAppointmentDetRow = null;

            if (newRow is FSSODet)
            {
                fsSODetRow = (FSSODet)objNewRow;
            }
            else if (newRow is FSAppointmentDet)
            {
                fsAppointmentDetRow = (FSAppointmentDet)objNewRow;
            }

            //*****************************************************************************************
            // You can specify before cache.Insert only fields that are not calculated from other ones.
            newRow.LineType = sourceRow.LineType;

            if (fsSODetRow != null)
            {
                // Insert the new row with the key fields cleared
                fsSODetRow.RefNbr = null;
                fsSODetRow.SOID = null;
                fsSODetRow.LineRef = null;
                fsSODetRow.SODetID = null;
                fsSODetRow.LotSerialNbr = null;

                if (copyTranDate)
                {
                    fsSODetRow.TranDate = tranDate;
                }

                fsSODetRow.NoteID = noteID;
            }
            else
            {
                // Insert the new row with the key fields cleared
                fsAppointmentDetRow.RefNbr = null;
                fsAppointmentDetRow.AppointmentID = null;
                fsAppointmentDetRow.LineRef = null;
                fsAppointmentDetRow.AppDetID = null;
                fsAppointmentDetRow.LineNbr = null;

                // Insert the new row with special fields cleared
                fsAppointmentDetRow.LotSerialNbr = null;

                if (copyTranDate)
                {
                    fsAppointmentDetRow.TranDate = tranDate;
                }

                fsAppointmentDetRow.NoteID = noteID;
            }

            newRow = (NewRowType)newRowCache.Insert(newRow);
            //*****************************************************************************************

            // This row copy is to be used with cache.RaiseRowUpdated in order to accumulate totals with Formulas
            var oldRow = (NewRowType)newRowCache.CreateCopy(newRow);


            if (fsAppointmentDetRow != null)
            {
                newRowCache.SetValueExtIfDifferent<FSAppointmentDet.sODetID>(newRow, soDetID);
            }

            if (SetValuesAfterAssigningSODetID == true || fsSODetRow != null)
            {
                newRow = CopyDependentFieldsOfSODet<NewRowType, SourceRowType>(newRowCache,
                                                                               newRow,
                                                                               sourceCache,
                                                                               sourceRow,
                                                                               copyingFromQuote);
            }

            if (fsAppointmentDetRow != null
                    && sourceCache.Graph.Accessinfo.ScreenID == SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.CLONE_APPOINTMENT)
            )
            {
                string equipmentAction = fsAppointmentDetRow.EquipmentAction ?? string.Empty;

                if (equipmentAction.IsNotIn(ID.Equipment_Action.NONE,
                                            ID.Equipment_Action.SELLING_TARGET_EQUIPMENT))
                {
                    fsAppointmentDetRow.EquipmentAction = ID.Equipment_Action.NONE;
                    fsAppointmentDetRow.SMEquipmentID = null;
                    fsAppointmentDetRow.NewTargetEquipmentLineNbr = null;
                    fsAppointmentDetRow.ComponentID = null;
                    fsAppointmentDetRow.EquipmentLineRef = null;
                }
            }

            // This cache.RaiseRowUpdated is required to accumulate totals with Formulas
            newRowCache.RaiseRowUpdated(newRow, oldRow);


            return newRow;
        }

        protected static TargetRowType CopyDependentFieldsOfSODet<TargetRowType, SourceRowType>(PXCache targetCache,
                                                                                                object objTargetRow,
                                                                                                PXCache sourceCache,
                                                                                                object objSourceRow,
                                                                                                bool copyingFromQuote)
            where TargetRowType : class, IBqlTable, IFSSODetBase, new()
            where SourceRowType : class, IBqlTable, IFSSODetBase, new()
        {
            var targetRow = (TargetRowType)objTargetRow;
            var sourceRow = (SourceRowType)objSourceRow;

            // **********************************************************************************************************
            // The sequence of fields in this method is VERY IMPORTANT because
            // it determines the proper validation and assignment of values
            // **********************************************************************************************************

            targetCache.SetValueExtIfDifferent<FSSODet.branchID>(targetRow, sourceRow.BranchID);
            targetCache.SetValueExtIfDifferent<FSSODet.inventoryID>(targetRow, sourceRow.InventoryID);

            if (targetRow.InventoryID != null)
            {
                targetCache.SetValueExtIfDifferent<FSSODet.isPrepaid>(targetRow, sourceRow.IsPrepaid);

                if (sourceRow.IsBillable == false && sourceRow.Status == ID.Status_AppointmentDet.RequestForPO)
                {
                    targetCache.SetValueExtIfDifferent<FSSODet.isBillable>(targetRow, true);
                }
                else
                {
                    targetCache.SetValueExtIfDifferent<FSSODet.isBillable>(targetRow, sourceRow.IsBillable);
                }
                
                targetCache.SetValueExtIfDifferent<FSSODet.billingRule>(targetRow, sourceRow.BillingRule);
                targetCache.SetValueExtIfDifferent<FSSODet.manualPrice>(targetRow, sourceRow.ManualPrice);
                targetCache.SetValueExtIfDifferent<FSSODet.isFree>(targetRow, sourceRow.IsFree);

                targetCache.SetValueExtIfDifferent<FSSODet.subItemID>(targetRow, sourceRow.SubItemID);
                targetCache.SetValueExtIfDifferent<FSSODet.uOM>(targetRow, sourceRow.UOM);

                targetCache.SetValueExtIfDifferent<FSSODet.siteID>(targetRow, sourceRow.SiteID);
                targetCache.SetValueExtIfDifferent<FSSODet.siteLocationID>(targetRow, sourceRow.SiteLocationID);

                if (sourceRow.GetQty(FieldType.EstimatedField) > sourceRow.GetApptQty())
                {
                    targetCache.SetValueExtIfDifferent<FSSODet.estimatedQty>(targetRow, sourceRow.GetQty(FieldType.EstimatedField) - sourceRow.GetApptQty());

                    if (sourceRow.LineType == ID.LineType_AppSrvOrd.SERVICE && sourceRow.BillingRule == ID.BillingRule.TIME)
                    {
                        targetCache.SetValueExtIfDifferent<FSSODet.estimatedDuration>(targetRow, sourceRow.GetDuration(FieldType.EstimatedField) - sourceRow.GetApptDuration());
                    }
                    else
                    {
                        targetCache.SetValueExtIfDifferent<FSSODet.estimatedDuration>(targetRow, sourceRow.GetDuration(FieldType.EstimatedField));
                    }
                }
                else
                {
                    switch (sourceRow.LineType)
                    {
                        case ID.LineType_AppSrvOrd.INVENTORY_ITEM:
                            targetCache.SetValueExtIfDifferent<FSSODet.estimatedDuration>(targetRow, 0);
                            targetCache.SetValueExtIfDifferent<FSSODet.estimatedQty>(targetRow, 0m);
                            break;
                        case ID.LineType_AppSrvOrd.SERVICE:
                            if (sourceRow.BillingRule == ID.BillingRule.TIME)
                            {
                                targetCache.SetValueExtIfDifferent<FSSODet.estimatedDuration>(targetRow, 1);
                            }
                            else
                            {
                                targetCache.SetValueExtIfDifferent<FSSODet.estimatedDuration>(targetRow, sourceRow.GetDuration(FieldType.EstimatedField));
                                targetCache.SetValueExtIfDifferent<FSSODet.estimatedQty>(targetRow, 1m);
                            }
                            break;
                        case ID.LineType_AppSrvOrd.NONSTOCKITEM:
                            targetCache.SetValueExtIfDifferent<FSSODet.estimatedDuration>(targetRow, sourceRow.GetDuration(FieldType.EstimatedField));
                            targetCache.SetValueExtIfDifferent<FSSODet.estimatedQty>(targetRow, 1m);
                            break;
                        default:
                            targetCache.SetValueExtIfDifferent<FSSODet.estimatedDuration>(targetRow, 0);
                            targetCache.SetValueExtIfDifferent<FSSODet.estimatedQty>(targetRow, 0m);
                            break;
                    }
                }

                if (targetRow.ManualPrice == true)
                {
                    decimal targetCuryUnitPrice = 0m;
                    decimal targetCuryBillableExtPrice = 0m;

                    if (sourceRow.UnitPrice != null)
                    {
                        targetCache.Graph.FindImplementation<IPXCurrencyHelper>().CuryConvCury((decimal)(sourceRow.UnitPrice), out targetCuryUnitPrice);
                    }

                    if (sourceRow.BillableExtPrice != null)
                    {
                        targetCache.Graph.FindImplementation<IPXCurrencyHelper>().CuryConvCury((decimal)(sourceRow.BillableExtPrice), out targetCuryBillableExtPrice);
                    }

                    targetCache.SetValueExtIfDifferent<FSSODet.curyUnitPrice>(targetRow, targetCuryUnitPrice);

                    if (targetCuryUnitPrice != 0)
                    {
                        PXUIFieldAttribute.SetWarning<FSSODet.curyUnitPrice>(targetCache, targetRow, null);
                    }

                    targetCache.SetValueExtIfDifferent<FSSODet.curyBillableExtPrice>(targetRow, targetCuryBillableExtPrice);

                    if (targetCuryBillableExtPrice != 0)
                    {
                        PXUIFieldAttribute.SetWarning<FSSODet.curyBillableExtPrice>(targetCache, targetRow, null);
                    }
                }

                if (sourceRow.EnablePO == true && sourceRow.POStatus != POOrderStatus.Completed)
                {
                    if (objTargetRow is FSAppointmentDet)
                    {
                        FSAppointmentDet apptDetRow = (FSAppointmentDet)objTargetRow;

                        if (objSourceRow is FSSODet)
                        {
                            apptDetRow.CanChangeMarkForPO = false;
                        }
                    }
                    
                    targetRow.POType = sourceRow.POType;
                    targetRow.PONbr = sourceRow.PONbr;
                    targetRow.POCompleted = sourceRow.POCompleted;
                    targetRow.POStatus = sourceRow.POStatus;
                    targetRow.POSource = sourceRow.POSource;
                    targetRow.POVendorID = sourceRow.POVendorID;
                    targetRow.POVendorLocationID = sourceRow.POVendorLocationID;

                    targetCache.SetValueExtIfDifferent<FSSODet.enablePO>(targetRow, sourceRow.EnablePO);
                    targetCache.SetValueExt<FSSODet.pOSource>(targetRow, sourceRow.POSource);
                    targetCache.SetValueExt<FSSODet.poVendorID>(targetRow, sourceRow.POVendorID);
                    targetCache.SetValueExt<FSSODet.poVendorLocationID>(targetRow, sourceRow.POVendorLocationID);

                    decimal targetCuryUnitCost = 0m;

                    if (sourceRow.UnitCost != null)
                    {
                        targetCache.Graph.FindImplementation<IPXCurrencyHelper>().CuryConvCury((decimal)(sourceRow.UnitCost), out targetCuryUnitCost);
                    }

                    targetCache.SetValueExtIfDifferent<FSSODet.curyUnitCost>(targetRow, targetCuryUnitCost);

                    if (targetCuryUnitCost != 0)
                    {
                        PXUIFieldAttribute.SetWarning<FSSODet.curyUnitCost>(targetCache, targetRow, null);
                    }
                }

                targetCache.SetValueExtIfDifferent<FSSODet.taxCategoryID>(targetRow, sourceRow.TaxCategoryID);
                targetCache.SetValueExtIfDifferent<FSSODet.projectID>(targetRow, sourceRow.ProjectID);
                targetCache.SetValueExtIfDifferent<FSSODet.projectTaskID>(targetRow, sourceRow.ProjectTaskID);

                if (sourceRow.AcctID != null || copyingFromQuote == false)
                {
                    targetCache.SetValueExtIfDifferent<FSSODet.acctID>(targetRow, sourceRow.AcctID);

                    if (sourceRow.SubID != null || copyingFromQuote == false)
                    {
                        targetCache.SetValueExtIfDifferent<FSSODet.subID>(targetRow, sourceRow.SubID);
                    }
                }

                targetCache.SetValueExtIfDifferent<FSSODet.costCodeID>(targetRow, sourceRow.CostCodeID);
                
                if (objSourceRow is FSSODet)
                {
                    FSSODet soDetRow = (FSSODet)objSourceRow;
                    targetCache.SetValueExtIfDifferent<FSSODet.manualDisc>(targetRow, soDetRow.ManualDisc);
                    targetCache.SetValueExtIfDifferent<FSSODet.manualCost>(targetRow, soDetRow.ManualCost);
                }
                else if(objSourceRow is FSAppointmentDet)
                {
                    FSAppointmentDet apptDetRow = (FSAppointmentDet)objSourceRow;
                    targetCache.SetValueExtIfDifferent<FSSODet.manualDisc>(targetRow, apptDetRow.ManualDisc);
                    targetCache.SetValueExtIfDifferent<FSSODet.manualCost>(targetRow, apptDetRow.ManualCost);
                }
                
                targetCache.SetValueExtIfDifferent<FSSODet.discPct>(targetRow, sourceRow.DiscPct);
                targetCache.SetValueExtIfDifferent<FSSODet.curyDiscAmt>(targetRow, sourceRow.CuryDiscAmt);

                if (sourceRow.IsLinkedItem == true)
                {
                    targetCache.SetValueExtIfDifferent<FSSODet.linkedEntityType>(targetRow, sourceRow.LinkedEntityType);
                    targetCache.SetValueExtIfDifferent<FSSODet.linkedDocType>(targetRow, sourceRow.LinkedDocType);
                    targetCache.SetValueExtIfDifferent<FSSODet.linkedDocRefNbr>(targetRow, sourceRow.LinkedDocRefNbr);
                    targetCache.SetValueExtIfDifferent<FSSODet.linkedLineNbr>(targetRow, sourceRow.LinkedLineNbr);
                }
            }

            bool copyEquipmentFields = true;

            if (objTargetRow is FSAppointmentDet
                    && sourceCache.Graph.Accessinfo.ScreenID == SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.CLONE_APPOINTMENT)
            )
            {
                FSAppointmentDet apptDetRow = (FSAppointmentDet)objTargetRow;

                string equipmentAction = apptDetRow.EquipmentAction ?? string.Empty;

                if (equipmentAction.IsNotIn(ID.Equipment_Action.NONE,
                            ID.Equipment_Action.SELLING_TARGET_EQUIPMENT))
                {
                    copyEquipmentFields = false;
                }
            }

            if (copyEquipmentFields == true)
            {
                targetCache.SetValueExtIfDifferent<FSSODet.equipmentAction>(targetRow, sourceRow.EquipmentAction);
                targetCache.SetValueExtIfDifferent<FSSODet.SMequipmentID>(targetRow, sourceRow.SMEquipmentID);
                targetCache.SetValueExtIfDifferent<FSSODet.newTargetEquipmentLineNbr>(targetRow, sourceCache.GetValue<FSAppointmentDet.newTargetEquipmentLineNbr>(sourceRow));
                targetCache.SetValueExtIfDifferent<FSSODet.componentID>(targetRow, sourceRow.ComponentID);
                targetCache.SetValueExtIfDifferent<FSSODet.equipmentLineRef>(targetRow, sourceRow.EquipmentLineRef);
            }
            else
            {
                targetCache.SetValueExt<FSSODet.equipmentAction>(targetRow, ID.Equipment_Action.NONE);
                targetCache.SetValueExt<FSSODet.SMequipmentID>(targetRow, null);
                targetCache.SetValueExt<FSSODet.newTargetEquipmentLineNbr>(targetRow, null);
                targetCache.SetValueExt<FSSODet.componentID>(targetRow, null);
                targetCache.SetValueExt<FSSODet.equipmentLineRef>(targetRow, null);
            }

            targetCache.SetValueExtIfDifferent<FSSODet.tranDesc>(targetRow, sourceRow.TranDesc);

            if (objTargetRow is FSAppointmentDet && objSourceRow is FSSODet)
            {
                var soLine = (FSSODet)objSourceRow;
                var apptLine = (FSAppointmentDet)objTargetRow;


                apptLine.OrigSrvOrdNbr = soLine.RefNbr;
                apptLine.OrigLineNbr = soLine.LineNbr;
            }

            return targetRow;
        }

        protected virtual void OnRowInsertedFSAppointmentLog(FSAppointmentLog row)
        {
            if (row == null || row.DetLineRef == null)
                return;

            var appointmentDet = AppointmentDetails.Select().AsEnumerable().RowCast<FSAppointmentDet>()
                                                     .Where(r => r.LineRef == row.DetLineRef).FirstOrDefault();

            if (appointmentDet != null)
            {
                var copy = (FSAppointmentDet)AppointmentDetails.Cache.CreateCopy(appointmentDet);

                if (row.TrackOnService == true)
                {
                    if (copy.LogRelatedCount == null)
                    {
                        copy.LogRelatedCount = 0;
                    }

                    copy.LogRelatedCount++;
                    copy.LogActualDuration += row.TimeDuration;
                }

                if (copy.IsLinkedItem == false)
                {
                    copy.Status = GetItemLineStatusFromLog(appointmentDet);
                }

                if (row.TrackOnService == true)
                {
                    SetDefaultActualDuration(copy);
                }

                appointmentDet = AppointmentDetails.Update(copy);
            }
        }

        protected virtual void OnRowDeletedFSAppointmentLog(FSAppointmentLog row)
        {
            if (row == null || row.DetLineRef == null)
                return;

            var appointmentDet = AppointmentDetails.Select().AsEnumerable().RowCast<FSAppointmentDet>()
                                                     .Where(r => r.LineRef == row.DetLineRef).FirstOrDefault();

            if (appointmentDet != null)
            {
                var copy = (FSAppointmentDet)AppointmentDetails.Cache.CreateCopy(appointmentDet);

                if (row.TrackOnService == true)
                {
                    if (copy.LogRelatedCount == null)
                    {
                        copy.LogRelatedCount = 0;
                    }

                    copy.LogRelatedCount--;
                    copy.LogActualDuration -= row.TimeDuration;
                }

                if (copy.IsLinkedItem == false)
                {
                    copy.Status = GetItemLineStatusFromLog(appointmentDet);
                }

                if (row.TrackOnService == true)
                {
                    SetDefaultActualDuration(copy);
                }

                appointmentDet = AppointmentDetails.Update(copy);
            }
        }

        public virtual void SetDefaultActualDuration(FSAppointmentDet appointmentDetCopy)
        {
            if (appointmentDetCopy == null)
                return;

            var relatedLogs = LogRecords.Select().RowCast<FSAppointmentLog>()
                                .Where(r => r.DetLineRef == appointmentDetCopy.LineRef
                                    && r.TrackOnService == true)
                                .FirstOrDefault();

            if (relatedLogs == null)
            {
                AppointmentDetails.Cache.SetDefaultExt<FSAppointmentDet.actualDuration>(appointmentDetCopy);
            }
        }
        #endregion

        #region Avalara Tax

        #region External Tax Provider

        public virtual bool IsExternalTax(string taxZoneID)
        {
            return false;
        }

        public virtual FSAppointment CalculateExternalTax(FSAppointment fsAppointmentRow)
        {
            return fsAppointmentRow;
        }
        #endregion

        public void ClearTaxes(FSAppointment appointmentRow)
        {
            if (appointmentRow == null)
                return;

            if (IsExternalTax(appointmentRow.TaxZoneID))
            {
                foreach (PXResult<FSAppointmentTaxTran, Tax> res in Taxes.View.SelectMultiBound(new object[] { appointmentRow }))
                {
                    FSAppointmentTaxTran taxTran = (FSAppointmentTaxTran)res;
                    Taxes.Delete(taxTran);
                }

                appointmentRow.CuryTaxTotal = 0;
                appointmentRow.CuryDocTotal = GetCuryDocTotal(appointmentRow.CuryBillableLineTotal, appointmentRow.CuryLogBillableTranAmountTotal, appointmentRow.CuryDiscTot,
                                                0, 0);
            }
        }
        #endregion
        #region Well-known extensions
        #region QuickProcess
        public AppointmentQuickProcess AppointmentQuickProcessExt => GetExtension<AppointmentQuickProcess>();
        public class AppointmentQuickProcess : PXGraphExtension<AppointmentEntry>
        {
            public static bool IsActive() => true;

            public PXSelect<SOOrderTypeQuickProcess, Where<SOOrderTypeQuickProcess.orderType, Equal<Current<FSSrvOrdType.postOrderType>>>> currentSOOrderType;
            public static bool isSOInvoice;

            public PXQuickProcess.Action<FSAppointment>.ConfiguredBy<FSAppQuickProcessParams> quickProcess;

            [PXButton(CommitChanges = true), PXUIField(DisplayName = "Quick Process")]
            protected virtual IEnumerable QuickProcess(PXAdapter adapter)
            {
                QuickProcessParameters.AskExt(InitQuickProcessPanel);

                if (Base.AppointmentRecords.AllowUpdate == true)
                {
                    Base.SkipTaxCalcAndSave();
                }

                PXQuickProcess.Start(Base, Base.AppointmentRecords.Current, QuickProcessParameters.Current);

                return new[] { Base.AppointmentRecords.Current };
            }

            public PXAction<FSAppointment> quickProcessOk;
            [PXButton, PXUIField(DisplayName = "OK")]
            public virtual IEnumerable QuickProcessOk(PXAdapter adapter)
            {
                Base.AppointmentRecords.Current.IsCalledFromQuickProcess = true;
                return adapter.Get();
            }

            public PXFilter<FSAppQuickProcessParams> QuickProcessParameters;

            protected virtual void _(Events.RowSelected<FSAppointment> e)
            {
                if (e.Row == null)
                {
                    return;
                }

                if (currentSOOrderType.Current == null)
                {
                    currentSOOrderType.Current = currentSOOrderType.Select();
                }

                isSOInvoice = Base.ServiceOrderTypeSelected.Current?.PostTo == ID.SrvOrdType_PostTo.SALES_ORDER_INVOICE;
                quickProcess.SetEnabled(Base.ServiceOrderTypeSelected.Current?.AllowQuickProcess == true);
            }

            protected virtual void _(Events.RowSelected<FSAppQuickProcessParams> e)
            {
                if (e.Row == null)
                {
                    return;
                }

                quickProcessOk.SetEnabled(true);

                FSAppQuickProcessParams fsQuickProcessParametersRow = (FSAppQuickProcessParams)e.Row;
                PXCache cache = e.Cache;

                SetQuickProcessSettingsVisibility(cache, Base.ServiceOrderTypeSelected.Current, Base.AppointmentRecords.Current, fsQuickProcessParametersRow);

                if (isSOInvoice == true)
                {
                    PXUIFieldAttribute.SetEnabled<FSAppQuickProcessParams.generateInvoiceFromAppointment>(cache, fsQuickProcessParametersRow, true);
                    PXUIFieldAttribute.SetEnabled<FSAppQuickProcessParams.releaseInvoice>(cache, fsQuickProcessParametersRow, fsQuickProcessParametersRow.GenerateInvoiceFromAppointment == true);
                    PXUIFieldAttribute.SetEnabled<FSAppQuickProcessParams.emailInvoice>(cache, fsQuickProcessParametersRow, fsQuickProcessParametersRow.GenerateInvoiceFromAppointment == true);
                }
            }

            protected virtual void _(Events.FieldUpdated<FSAppQuickProcessParams, FSAppQuickProcessParams.sOQuickProcess> e)
            {
                if (e.Row == null)
                {
                    return;
                }

                FSAppQuickProcessParams fsAppQuickProcessParamsRow = (FSAppQuickProcessParams)e.Row;

                if (fsAppQuickProcessParamsRow.SOQuickProcess != (bool?)e.OldValue)
                {
                    SetQuickProcessOptions(Base, e.Cache, fsAppQuickProcessParamsRow, true);
                }
            }

            protected virtual void _(Events.FieldUpdated<FSAppQuickProcessParams, FSAppQuickProcessParams.generateInvoiceFromAppointment> e)
            {
                if (e.Row == null)
                {
                    return;
                }

                FSAppQuickProcessParams fsAppQuickProcessParamsRow = (FSAppQuickProcessParams)e.Row;

                if (isSOInvoice
                            && fsAppQuickProcessParamsRow.GenerateInvoiceFromAppointment != (bool?)e.OldValue)
                {
                    e.Cache.SetValueExt<FSAppQuickProcessParams.prepareInvoice>(fsAppQuickProcessParamsRow, fsAppQuickProcessParamsRow.GenerateInvoiceFromAppointment == true);
                }
            }

            protected virtual void _(Events.FieldUpdated<FSAppQuickProcessParams, FSAppQuickProcessParams.prepareInvoice> e)
            {
                if (e.Row == null)
                {
                    return;
                }

                FSAppQuickProcessParams fsAppQuickProcessParamsRow = (FSAppQuickProcessParams)e.Row;

                if (isSOInvoice
                        && fsAppQuickProcessParamsRow.PrepareInvoice != (bool?)e.OldValue
                            && fsAppQuickProcessParamsRow.PrepareInvoice == false)
                {
                    fsAppQuickProcessParamsRow.ReleaseInvoice = false;
                    fsAppQuickProcessParamsRow.EmailInvoice = false;
                }
            }

            private void SetQuickProcessSettingsVisibility(PXCache cache, FSSrvOrdType fsSrvOrdTypeRow, FSAppointment fsAppointmentRow, FSAppQuickProcessParams fsQuickProcessParametersRow)
            {
                if (fsSrvOrdTypeRow != null)
                {
                    bool isInvoiceBehavior = false;
                    bool orderTypeQuickProcessIsEnabled = false;
                    bool postToSO = fsSrvOrdTypeRow.PostTo == ID.SrvOrdType_PostTo.SALES_ORDER_MODULE;
                    bool postToSOInvoice = fsSrvOrdTypeRow.PostTo == ID.SrvOrdType_PostTo.SALES_ORDER_INVOICE;
                    bool enableSOQuickProcess = postToSO;

                    if (postToSO && currentSOOrderType.Current?.AllowQuickProcess != null)
                    {
                        isInvoiceBehavior = currentSOOrderType.Current.Behavior == SOBehavior.IN;
                        orderTypeQuickProcessIsEnabled = (bool)currentSOOrderType.Current.AllowQuickProcess;
                    }
                    else if (postToSOInvoice)
                    {
                        isInvoiceBehavior = true;
                    }

                    enableSOQuickProcess = orderTypeQuickProcessIsEnabled
                                                && fsQuickProcessParametersRow.GenerateInvoiceFromAppointment == true
                                                    && (fsQuickProcessParametersRow.PrepareInvoice == false || fsQuickProcessParametersRow.SOQuickProcess == true);

                    PXUIFieldAttribute.SetVisible<FSAppQuickProcessParams.sOQuickProcess>(cache, fsQuickProcessParametersRow, postToSO && orderTypeQuickProcessIsEnabled);
                    PXUIFieldAttribute.SetVisible<FSAppQuickProcessParams.emailSalesOrder>(cache, fsQuickProcessParametersRow, postToSO);
                    PXUIFieldAttribute.SetVisible<FSAppQuickProcessParams.prepareInvoice>(cache, fsQuickProcessParametersRow, postToSO && isInvoiceBehavior && fsQuickProcessParametersRow.SOQuickProcess == false);
                    PXUIFieldAttribute.SetVisible<FSAppQuickProcessParams.releaseInvoice>(cache, fsQuickProcessParametersRow, (postToSO || postToSOInvoice) && isInvoiceBehavior && fsQuickProcessParametersRow.SOQuickProcess == false);
                    PXUIFieldAttribute.SetVisible<FSAppQuickProcessParams.emailInvoice>(cache, fsQuickProcessParametersRow, (postToSO || postToSOInvoice) && isInvoiceBehavior && fsQuickProcessParametersRow.SOQuickProcess == false);

                    PXUIFieldAttribute.SetEnabled<FSAppQuickProcessParams.sOQuickProcess>(cache, fsQuickProcessParametersRow, enableSOQuickProcess);
                    PXUIFieldAttribute.SetEnabled<FSAppQuickProcessParams.emailSignedAppointment>(cache, fsQuickProcessParametersRow, Base.IsEnableEmailSignedAppointment());

                    if (fsQuickProcessParametersRow.ReleaseInvoice == false
                        && fsQuickProcessParametersRow.EmailInvoice == false
                            && fsQuickProcessParametersRow.SOQuickProcess == false
                                && fsQuickProcessParametersRow.GenerateInvoiceFromAppointment == true)
                    {
                        PXUIFieldAttribute.SetEnabled<FSAppQuickProcessParams.prepareInvoice>(cache, fsQuickProcessParametersRow, true);
                    }

                    if (Base.IsEnableEmailSignedAppointment() == false 
                        && Base.Accessinfo.ScreenID == SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT))
                    {
                        cache.RaiseExceptionHandling<FSQuickProcessParameters.emailSignedAppointment>(fsQuickProcessParametersRow,
                                                                                                      false,
                                                                                                      new PXSetPropertyException(TX.Warning.SIGNED_APP_EMAIL_ACTION_IS_DISABLED, PXErrorLevel.Warning));
                    }
                }
            }

            [Obsolete("This method will be deleted in the next major release. Use GetExcludedFields instead.")]
            public virtual string[] GetExcludeFiels()
            {
                return GetExcludedFields();
            }

            public static string[] GetExcludedFields()
            {
                string[] excludeFields = {
                    SharedFunctions.GetFieldName<FSQuickProcessParameters.closeAppointment>(),
                    SharedFunctions.GetFieldName<FSQuickProcessParameters.generateInvoiceFromAppointment>(),
                    SharedFunctions.GetFieldName<FSQuickProcessParameters.sOQuickProcess>(),
                    SharedFunctions.GetFieldName<FSQuickProcessParameters.emailSalesOrder>(),
                    SharedFunctions.GetFieldName<FSQuickProcessParameters.srvOrdType>()
                };

                return excludeFields;
            }

            public static void SetQuickProcessOptions(PXGraph graph, PXCache targetCache, FSAppQuickProcessParams fsAppQuickProcessParamsRow, bool ignoreUpdateSOQuickProcess)
            {
                var ext = ((AppointmentEntry)graph).AppointmentQuickProcessExt;

                if (string.IsNullOrEmpty(ext.QuickProcessParameters.Current.OrderType))
                {
                    ext.QuickProcessParameters.Cache.Clear();
                    ResetSalesOrderQuickProcessValues(ext.QuickProcessParameters.Current);
                }

                if (fsAppQuickProcessParamsRow != null)
                {
                    ResetSalesOrderQuickProcessValues(fsAppQuickProcessParamsRow);
                }

                FSQuickProcessParameters fsQuickProcessParamsRow = PXSelectReadonly<FSQuickProcessParameters,
                                                                   Where<
                                                                       FSQuickProcessParameters.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>>>
                                                                   .Select(ext.Base);

                bool isSOQuickProcess = (fsAppQuickProcessParamsRow != null && fsAppQuickProcessParamsRow.SOQuickProcess == true)
                                            || (fsAppQuickProcessParamsRow == null && fsQuickProcessParamsRow?.SOQuickProcess == true);

                var cache = targetCache ?? ext.QuickProcessParameters.Cache;
                FSAppQuickProcessParams row = fsAppQuickProcessParamsRow ?? ext.QuickProcessParameters.Current;

                if (ext.currentSOOrderType.Current?.AllowQuickProcess == true && isSOQuickProcess)
                {
                    var cacheFSAppQuickProcessParams = new PXCache<FSAppQuickProcessParams>(ext.Base);

                    FSAppQuickProcessParams fsAppQuickProcessParamsFromDB = PXSelectReadonly<FSAppQuickProcessParams,
                                                                            Where<
                                                                                FSAppQuickProcessParams.orderType, Equal<Current<FSSrvOrdType.postOrderType>>>>
                                                                            .Select(ext.Base);

                    SharedFunctions.CopyCommonFields(cache,
                                                     row,
                                                     cacheFSAppQuickProcessParams,
                                                     fsAppQuickProcessParamsFromDB,
                                                     GetExcludedFields());

                    if (row.CreateShipment == true)
                    {
                        ext.EnsureSiteID(cache, row);

                        DateTime? shipDate = ext.Base.GetShipDate(ext.Base.ServiceOrderRelated.Current, ext.Base.AppointmentRecords.Current);
                        SOQuickProcessParametersShipDateExt.SetDate(cache, row, shipDate.Value);
                    }
                }
                else
                {
                    SetCommonValues(row, fsQuickProcessParamsRow);
                }

                if (ignoreUpdateSOQuickProcess == false)
                {
                    SetServiceOrderTypeValues(graph, row, fsQuickProcessParamsRow);
                }
            }

            public static void InitQuickProcessPanel(PXGraph graph, string viewName)
            {
                SetQuickProcessOptions(graph, null, null, false);
            }

            public static void ResetSalesOrderQuickProcessValues(FSAppQuickProcessParams fsAppQuickProcessParamsRow)
            {
                fsAppQuickProcessParamsRow.CreateShipment = false;
                fsAppQuickProcessParamsRow.ConfirmShipment = false;
                fsAppQuickProcessParamsRow.UpdateIN = false;
                fsAppQuickProcessParamsRow.PrepareInvoiceFromShipment = false;
                fsAppQuickProcessParamsRow.PrepareInvoice = false;
                fsAppQuickProcessParamsRow.EmailInvoice = false;
                fsAppQuickProcessParamsRow.ReleaseInvoice = false;
                fsAppQuickProcessParamsRow.AutoRedirect = false;
                fsAppQuickProcessParamsRow.AutoDownloadReports = false;
            }

            public static void SetCommonValues(FSAppQuickProcessParams fsAppQuickProcessParamsRowTarget, FSQuickProcessParameters FSQuickProcessParametersRowSource)
            {
                if (isSOInvoice && fsAppQuickProcessParamsRowTarget.GenerateInvoiceFromAppointment == true)
                {
                    fsAppQuickProcessParamsRowTarget.PrepareInvoice = false;
                }
                else
                {
                    fsAppQuickProcessParamsRowTarget.PrepareInvoice = FSQuickProcessParametersRowSource.GenerateInvoiceFromAppointment.Value ? FSQuickProcessParametersRowSource.PrepareInvoice : false;
                }

                fsAppQuickProcessParamsRowTarget.ReleaseInvoice = FSQuickProcessParametersRowSource.GenerateInvoiceFromAppointment.Value ? FSQuickProcessParametersRowSource.ReleaseInvoice : false;
                fsAppQuickProcessParamsRowTarget.EmailInvoice = FSQuickProcessParametersRowSource.GenerateInvoiceFromAppointment.Value ? FSQuickProcessParametersRowSource.EmailInvoice : false;
            }

            public static void SetServiceOrderTypeValues(PXGraph graph, FSAppQuickProcessParams fsAppQuickProcessParamsRowTarget, FSQuickProcessParameters FSQuickProcessParametersRowSource)
            {
                fsAppQuickProcessParamsRowTarget.CloseAppointment = FSQuickProcessParametersRowSource.CloseAppointment;
                fsAppQuickProcessParamsRowTarget.EmailSignedAppointment = ((AppointmentEntry)graph).IsEnableEmailSignedAppointment() && (FSQuickProcessParametersRowSource.EmailSignedAppointment.HasValue && FSQuickProcessParametersRowSource.EmailSignedAppointment.Value);
                fsAppQuickProcessParamsRowTarget.GenerateInvoiceFromAppointment = FSQuickProcessParametersRowSource.GenerateInvoiceFromAppointment;
                fsAppQuickProcessParamsRowTarget.EmailSalesOrder = FSQuickProcessParametersRowSource.GenerateInvoiceFromAppointment.Value ? FSQuickProcessParametersRowSource.EmailSalesOrder : false;
                fsAppQuickProcessParamsRowTarget.SOQuickProcess = FSQuickProcessParametersRowSource.SOQuickProcess;
                fsAppQuickProcessParamsRowTarget.SrvOrdType = FSQuickProcessParametersRowSource.SrvOrdType;

                if (isSOInvoice == true && fsAppQuickProcessParamsRowTarget.GenerateInvoiceFromAppointment == true)
                {
                    fsAppQuickProcessParamsRowTarget.PrepareInvoice = false;
                }
            }

            protected virtual void EnsureSiteID(PXCache sender, FSAppQuickProcessParams row)
            {
                if (row.SiteID == null)
                {
                    Int32? preferedSiteID = Base.GetPreferedSiteID();
                    if (preferedSiteID != null)
                        sender.SetValueExt<FSAppQuickProcessParams.siteID>(row, preferedSiteID);
                }
            }
        }
        #endregion

        #region Multi Currency
        public class MultiCurrency : SMMultiCurrencyGraph<AppointmentEntry, FSAppointment>
        {
            protected override PXSelectBase[] GetChildren()
            {
                return new PXSelectBase[]
                {
                    Base.AppointmentRecords,
                    Base.AppointmentDetails,
                    Base.ServiceOrderRelated
                };
            }

            protected override DocumentMapping GetDocumentMapping()
            {
                return new DocumentMapping(typeof(FSAppointment))
                {
                    BAccountID = typeof(FSAppointment.billCustomerID),
                    DocumentDate = typeof(FSAppointment.executionDate)
                };
            }

            protected override void _(Events.FieldUpdated<Extensions.MultiCurrency.Document, Extensions.MultiCurrency.Document.bAccountID> e)
            {
                if (e.Row == null) return;

                var doc = Documents.Cache.GetMain<Extensions.MultiCurrency.Document>(e.Row);
                
                if (doc is FSAppointment)
                {
                    AppointmentEntry graph = (AppointmentEntry)Documents.Cache.Graph;

                    if (e.ExternalCall || e.Row?.CuryID == null || graph.recalculateCuryID == true)
                    {
                        SourceFieldUpdated<Extensions.MultiCurrency.Document.curyInfoID, Extensions.MultiCurrency.Document.curyID, Extensions.MultiCurrency.Document.documentDate>(e.Cache, e.Row);
                    }
                }
                else
                {
                    base._(e);
                }
            }
        }
        #endregion

        #region Sales Taxes
        public class SalesTax : TaxGraph<AppointmentEntry, FSAppointment>
        {
            protected override bool CalcGrossOnDocumentLevel { get => true; set => base.CalcGrossOnDocumentLevel = value; }

            protected override DocumentMapping GetDocumentMapping()
            {
                return new DocumentMapping(typeof(FSAppointment))
                {
                    DocumentDate = typeof(FSAppointment.executionDate),
                    CuryDocBal = typeof(FSAppointment.curyDocTotal),
                    CuryDiscountLineTotal = typeof(FSAppointment.curyLineDocDiscountTotal),
                    CuryDiscTot = typeof(FSAppointment.curyDiscTot),
                    BranchID = typeof(FSAppointment.branchID),
                    FinPeriodID = typeof(FSAppointment.finPeriodID),
                    TaxZoneID = typeof(FSAppointment.taxZoneID),
                    CuryLinetotal = typeof(FSAppointment.curyBillableLineTotal),
                    CuryTaxTotal = typeof(FSAppointment.curyTaxTotal),
                    TaxCalcMode = typeof(FSAppointment.taxCalcMode)
                };
            }

            protected override DetailMapping GetDetailMapping()
            {
                return new DetailMapping(typeof(FSAppointmentDet))
                {
                    CuryTranAmt = typeof(FSAppointmentDet.curyBillableTranAmt),
                    TaxCategoryID = typeof(FSAppointmentDet.taxCategoryID),
                    DocumentDiscountRate = typeof(FSAppointmentDet.documentDiscountRate),
                    GroupDiscountRate = typeof(FSAppointmentDet.groupDiscountRate),
                    CuryTranDiscount = typeof(FSAppointmentDet.curyDiscAmt),
                    CuryTranExtPrice = typeof(FSAppointmentDet.curyBillableExtPrice)
                };
            }

            protected override void CurrencyInfo_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
            {
                TaxCalc taxCalc = CurrentDocument.TaxCalc ?? TaxCalc.NoCalc;
                if (taxCalc == TaxCalc.Calc || taxCalc == TaxCalc.ManualLineCalc)
                {
                    if (e.Row != null && ((CurrencyInfo)e.Row).CuryRate != null && (e.OldRow == null || !sender.ObjectsEqual<CurrencyInfo.curyRate, CurrencyInfo.curyMultDiv>(e.Row, e.OldRow)))
                    {
                        if (Base.AppointmentDetails.SelectSingle() != null)
                        {
                            CalcTaxes(null);
                        }
                    }
                }
            }

            protected override TaxDetailMapping GetTaxDetailMapping()
            {
                return new TaxDetailMapping(typeof(FSAppointmentTax), typeof(FSAppointmentTax.taxID));
            }

            protected override TaxTotalMapping GetTaxTotalMapping()
            {
                return new TaxTotalMapping(typeof(FSAppointmentTaxTran), typeof(FSAppointmentTaxTran.taxID));
            }

            protected virtual void Document_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
            {
                var row = sender.GetExtension<Extensions.SalesTax.Document>(e.Row);
                if (row == null)
                    return;

                if (row.TaxCalc == null)
                    row.TaxCalc = TaxCalc.Calc;

                if (Base.ServiceOrderTypeSelected.Current != null && Base.ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.PROJECTS)
                    row.TaxCalc = TaxCalc.NoCalc;
            }

            protected override void CalcDocTotals(object row, decimal CuryTaxTotal, decimal CuryInclTaxTotal, decimal CuryWhTaxTotal)
            {
                base.CalcDocTotals(row, CuryTaxTotal, CuryInclTaxTotal, CuryWhTaxTotal);
                FSAppointment doc = (FSAppointment)this.Documents.Cache.GetMain<Extensions.SalesTax.Document>(this.Documents.Current);

                decimal CuryLineTotal = (decimal)(ParentGetValue<FSAppointment.curyBillableLineTotal>() ?? 0m);
                decimal CuryLogBillableTranAmountTotal = (decimal)(ParentGetValue<FSAppointment.curyLogBillableTranAmountTotal>() ?? 0m);
                decimal CuryDiscTotal = (decimal)(ParentGetValue<FSAppointment.curyDiscTot>() ?? 0m);

                decimal CuryDocTotal = GetCuryDocTotal(CuryLineTotal, CuryLogBillableTranAmountTotal, CuryDiscTotal,
                                                CuryTaxTotal, CuryInclTaxTotal);

                if (object.Equals(CuryDocTotal, (decimal)(ParentGetValue<FSAppointment.curyDocTotal>() ?? 0m)) == false)
                {
                    ParentSetValue<FSAppointment.curyDocTotal>(CuryDocTotal);
                }
            }

            protected override string GetExtCostLabel(PXCache sender, object row)
            {
                return ((PXDecimalState)sender.GetValueExt<FSAppointmentDet.curyBillableExtPrice>(row)).DisplayName;
            }

            protected override void SetExtCostExt(PXCache sender, object child, decimal? value)
            {
                var row = child as PX.Data.PXResult<PX.Objects.Extensions.SalesTax.Detail>;
                if (row != null)
                {
                    var det = PXResult.Unwrap<PX.Objects.Extensions.SalesTax.Detail>(row);
                    var line = (FSAppointmentDet)det.Base;
                    line.CuryBillableExtPrice = value;
                    sender.Update(row);
                }
            }

            protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
            {
                Dictionary<string, PXResult<Tax, TaxRev>> tail = new Dictionary<string, PXResult<Tax, TaxRev>>();
				IComparer<Tax> taxComparer = GetTaxByCalculationLevelComparer();
				taxComparer.ThrowOnNull(nameof(taxComparer));

				var currents = new[]
                {
                    row != null && row is Extensions.SalesTax.Detail ? Details.Cache.GetMain((Extensions.SalesTax.Detail)row):null,
                    ((AppointmentEntry)graph).AppointmentSelected.Current
                };

                foreach (PXResult<Tax, TaxRev> record in PXSelectReadonly2<Tax,
                                                         LeftJoin<TaxRev,
                                                         On<
                                                             TaxRev.taxID, Equal<Tax.taxID>,
                                                             And<TaxRev.outdated, Equal<False>,
                                                             And<TaxRev.taxType, Equal<TaxType.sales>,
                                                             And<Tax.taxType, NotEqual<CSTaxType.withholding>,
                                                             And<Tax.taxType, NotEqual<CSTaxType.use>,
                                                             And<Tax.reverseTax, Equal<False>,
                                                             And<Current<FSAppointment.executionDate>, Between<TaxRev.startDate, TaxRev.endDate>>>>>>>>>,
                                                         Where>
                                                         .SelectMultiBound(graph, currents, parameters))
                {
                    tail[((Tax)record).TaxID] = record;
                }

                List<object> ret = new List<object>();
                switch (taxchk)
                {
                    case PXTaxCheck.Line:
                        foreach (FSAppointmentTax record in PXSelect<FSAppointmentTax,
                                                            Where<
                                                                FSAppointmentTax.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>,
                                                                And<FSAppointmentTax.refNbr, Equal<Current<FSAppointment.refNbr>>,
                                                                And<FSAppointmentTax.lineNbr, Equal<Current<FSAppointmentDet.lineNbr>>>>>>
                                                            .SelectMultiBound(graph, currents))
                        {
                            PXResult<Tax, TaxRev> line;

                            if (tail.TryGetValue(record.TaxID, out line))
                            {
                                int idx;
                                for (idx = ret.Count;
                                    (idx > 0) && taxComparer.Compare((PXResult<FSAppointmentTax, Tax, TaxRev>)ret[idx - 1], line) > 0;
                                    idx--) ;

                                Tax adjdTax = AdjustTaxLevel((Tax)line);
                                ret.Insert(idx, new PXResult<FSAppointmentTax, Tax, TaxRev>(record, adjdTax, (TaxRev)line));
                            }
                        }
                        return ret;
                    case PXTaxCheck.RecalcLine:
                        foreach (FSAppointmentTax record in PXSelect<FSAppointmentTax,
                                                            Where<
                                                                FSAppointmentTax.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>,
                                                                And<FSAppointmentTax.refNbr, Equal<Current<FSAppointment.refNbr>>>>>
                                                            .SelectMultiBound(graph, currents))
                        {
                            PXResult<Tax, TaxRev> line;

                            if (tail.TryGetValue(record.TaxID, out line))
                            {
                                int idx;
                                for (idx = ret.Count;
                                    (idx > 0)
                                    && ((FSAppointmentTax)(PXResult<FSAppointmentTax, Tax, TaxRev>)ret[idx - 1]).LineNbr == record.LineNbr
                                    && taxComparer.Compare((PXResult<FSAppointmentTax, Tax, TaxRev>)ret[idx - 1], line) > 0;
                                    idx--) ;

                                Tax adjdTax = AdjustTaxLevel((Tax)line);
                                ret.Insert(idx, new PXResult<FSAppointmentTax, Tax, TaxRev>(record, adjdTax, (TaxRev)line));
                            }
                        }
                        return ret;
                    case PXTaxCheck.RecalcTotals:
                        foreach (FSAppointmentTaxTran record in PXSelect<FSAppointmentTaxTran,
                                                                Where<
                                                                    FSAppointmentTaxTran.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>,
                                                                    And<FSAppointmentTaxTran.refNbr, Equal<Current<FSAppointment.refNbr>>>>,
                                                                OrderBy<
                                                                    Asc<FSAppointmentTaxTran.srvOrdType,
                                                                    Asc<FSAppointmentTaxTran.refNbr,
                                                                    Asc<FSAppointmentTaxTran.taxID>>>>>
                                                                .SelectMultiBound(graph, currents))
                        {
                            PXResult<Tax, TaxRev> line;

                            if (record.TaxID != null && tail.TryGetValue(record.TaxID, out line))
                            {
                                int idx;
                                for (idx = ret.Count;
                                    (idx > 0) && taxComparer.Compare((PXResult<FSAppointmentTaxTran, Tax, TaxRev>)ret[idx - 1], line) > 0;
                                    idx--) ;

                                Tax adjdTax = AdjustTaxLevel((Tax)line);
                                ret.Insert(idx, new PXResult<FSAppointmentTaxTran, Tax, TaxRev>(record, adjdTax, (TaxRev)line));
                            }
                        }
                        return ret;
                    default:
                        return ret;
                }
            }

            protected override List<Object> SelectDocumentLines(PXGraph graph, object row)
            {
                var res = PXSelect<FSAppointmentDet,
                            Where<FSAppointmentDet.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>,
                                And<FSAppointmentDet.refNbr, Equal<Current<FSAppointment.refNbr>>>>>
                            .SelectMultiBound(graph, new object[] { row })
                            .RowCast<FSAppointmentDet>()
                            .Select(_ => (object)_)
                            .ToList();
                return res;
            }

            #region FSAppointmentTaxTran
            protected virtual void _(Events.RowSelected<FSAppointmentTaxTran> e)
            {
                if (e.Row == null)
                    return;

                PXUIFieldAttribute.SetEnabled<FSAppointmentTaxTran.taxID>(e.Cache, e.Row, e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted);
            }

            protected virtual void _(Events.RowPersisting<FSAppointmentTaxTran> e)
            {
                FSAppointmentTaxTran row = e.Row as FSAppointmentTaxTran;

                if (row == null) return;

                if (e.Operation == PXDBOperation.Delete)
                {
                    FSAppointmentTax tax = (FSAppointmentTax)Base.TaxLines.Cache.Locate(FindFSAppointmentTax(row));

                    if (Base.TaxLines.Cache.GetStatus(tax) == PXEntryStatus.Deleted ||
                         Base.TaxLines.Cache.GetStatus(tax) == PXEntryStatus.InsertedDeleted)
                        e.Cancel = true;
                }
                if (e.Operation == PXDBOperation.Update)
                {
                    FSAppointmentTax tax = (FSAppointmentTax)Base.TaxLines.Cache.Locate(FindFSAppointmentTax(row));

                    if (Base.TaxLines.Cache.GetStatus(tax) == PXEntryStatus.Updated)
                        e.Cancel = true;
                }
            }

            internal static FSAppointmentTax FindFSAppointmentTax(FSAppointmentTaxTran tran)
            {
                var list = PXSelect<FSAppointmentTax,
                           Where<
                               FSAppointmentTax.srvOrdType, Equal<Required<FSAppointmentTax.srvOrdType>>,
                               And<FSAppointmentTax.refNbr, Equal<Required<FSAppointmentTax.refNbr>>,
                               And<FSAppointmentTax.lineNbr, Equal<Required<FSAppointmentTax.lineNbr>>,
                               And<FSAppointmentTax.taxID, Equal<Required<FSAppointmentTax.taxID>>>>>>>
                           .SelectSingleBound(new PXGraph(), new object[] { })
                           .RowCast<FSAppointmentTax>();

                return list.FirstOrDefault();
            }
            #endregion

            #region FSAppointment
            protected virtual void _(Events.FieldDefaulting<FSAppointment, FSAppointment.taxZoneID> e)
            {
                var row = e.Row as FSAppointment;
                PXCache cache = e.Cache;

                if (row == null) return;

                FSServiceOrder fsServiceOrderRow = ((AppointmentEntry)cache.Graph).ServiceOrderRelated.Current;

                if (fsServiceOrderRow == null)
                {
                    return;
                }

                var customerLocation = (Location)PXSelect<Location,
                                                 Where<
                                                     Location.bAccountID, Equal<Required<Location.bAccountID>>,
                                                     And<Location.locationID, Equal<Required<Location.locationID>>>>>
                                                 .Select(cache.Graph, row.BillCustomerID, fsServiceOrderRow.BillLocationID);

                if (customerLocation != null)
                {
                    if (!string.IsNullOrEmpty(customerLocation.CTaxZoneID))
                    {
                        e.NewValue = customerLocation.CTaxZoneID;
                    }
                    else
                    {
                        var address = (Address)PXSelect<Address,
                                               Where<
                                                   Address.addressID, Equal<Required<Address.addressID>>>>
                                               .Select(cache.Graph, customerLocation.DefAddressID);

                        if (address != null && !string.IsNullOrEmpty(address.PostalCode))
                        {
                            e.NewValue = TaxBuilderEngine.GetTaxZoneByZip(cache.Graph, address.PostalCode);
                        }
                    }
                }
                if (e.NewValue == null)
                {
                    var branchLocationResult = (PXResult<GL.Branch, BAccount, Location>)
                                               PXSelectJoin<GL.Branch,
                                               InnerJoin<BAccount,
                                               On<
                                                   BAccount.bAccountID, Equal<GL.Branch.bAccountID>>,
                                               InnerJoin<Location,
                                               On<
                                                   Location.locationID, Equal<BAccount.defLocationID>>>>,
                                               Where<
                                                   GL.Branch.branchID, Equal<Required<GL.Branch.branchID>>>>
                                               .Select(cache.Graph, row.BranchID);

                    var branchLocation = (Location)branchLocationResult;

                    if (branchLocation != null && branchLocation.VTaxZoneID != null)
                        e.NewValue = branchLocation.VTaxZoneID;
                    else
                        e.NewValue = row.TaxZoneID;
                }
            }
            #endregion
        }
        #endregion

        #region Contact/Address
        public class ContactAddress : SrvOrdContactAddressGraph<AppointmentEntry>
        {
        }
        #endregion

        #region Service Registration
        public class ExtensionSorting : Module
        {
            protected override void Load(ContainerBuilder builder) => builder.RunOnApplicationStart(() =>
                PXBuildManager.SortExtensions += list => PXBuildManager.PartialSort(list, _order)
                );

            private static readonly Dictionary<Type, int> _order = new Dictionary<Type, int>
            {
                {typeof(ContactAddress), 1},
                {typeof(MultiCurrency), 2},
                /*{typeof(SalesPrice), 3},
                    {typeof(Discount), 4},*/
            { typeof(SalesTax), 5},
            };
        }
        #endregion

        #region Address Lookup Extension
        /// <exclude/>
        public class AppointmentEntryAddressLookupExtension : CR.Extensions.AddressLookupExtension<AppointmentEntry, FSAppointment, FSAddress>
        {
            protected override string AddressView => nameof(Base.ServiceOrder_Address);
            protected override string ViewOnMap => nameof(Base.viewDirectionOnMap);
        }
        #endregion
        #endregion
    }
}

