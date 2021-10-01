using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.FS.Scheduler;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Configuration;
using PX.Common;

namespace PX.Objects.FS
{
    // Acuminator disable once PX1018 NoPrimaryViewForPrimaryDac [Justification]
    public class ServiceOrderBase<TGraph, TPrimary> : PXGraph<TGraph, TPrimary>
        where TGraph : PX.Data.PXGraph
        where TPrimary : class, PX.Data.IBqlTable, new()
    {
        public abstract class fakeField : PX.Data.IBqlField { }

        public enum EventType
        {
            RowSelectedEvent,
            RowPersistingEvent
        }

        #region Selects
        [PXHidden]
        public PXSetup<FSSetup> SetupRecord;

        [PXHidden]
        public PXSelect<BAccount> BAccounts;

        [PXHidden]
        public PXSelect<BAccountSelectorBase> BAccountSelectorBaseView;

        [PXHidden]
        public PXSelect<Vendor> Vendors;

        [PXHidden]
        public PXSetup<FSSelectorHelper> Helper;

        [PXHidden]
        public PXSelect<InventoryItem> InventoryItemHelper;

        [PXCopyPasteHiddenView]
        public PXSetup<FSBillingCycle,
               InnerJoin<FSCustomerBillingSetup,
               On<
                   FSBillingCycle.billingCycleID, Equal<FSCustomerBillingSetup.billingCycleID>>>,
               Where<
                   FSCustomerBillingSetup.cBID, Equal<Current<FSServiceOrder.cBID>>>> BillingCycleRelated;
        #endregion

        #region ServiceOrderViewTypes

        public class CurrentServiceOrder_View : PXSelect<FSServiceOrder,
                                                Where<
                                                    FSServiceOrder.srvOrdType, Equal<Current<FSServiceOrder.srvOrdType>>,
                                                    And<FSServiceOrder.refNbr, Equal<Current<FSServiceOrder.refNbr>>>>>
        {
            public CurrentServiceOrder_View(PXGraph graph) : base(graph)
            {
            }

            public CurrentServiceOrder_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }
        public class ServiceOrderAppointments_View : PXSelect<FSAppointment,
                                                     Where<
                                                         FSAppointment.sOID, Equal<Current<FSServiceOrder.sOID>>>,
                                                     OrderBy<
                                                         Asc<FSAppointment.refNbr>>>
        {
            public ServiceOrderAppointments_View(PXGraph graph) : base(graph)
            {
            }

            public ServiceOrderAppointments_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        public class ServiceOrderEquipment_View : PXSelectJoin<FSSOResource,
                                                  LeftJoin<FSEquipment,
                                                  On<
                                                      FSEquipment.SMequipmentID, Equal<FSSOResource.SMequipmentID>>>,
                                                  Where<
                                                      FSSOResource.sOID, Equal<Current<FSServiceOrder.sOID>>>>
        {
            public ServiceOrderEquipment_View(PXGraph graph) : base(graph)
            {
            }

            public ServiceOrderEquipment_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        public class RelatedServiceOrders_View : PXSelectReadonly<RelatedServiceOrder,
                                                 Where<
                                                     RelatedServiceOrder.sourceDocType, Equal<Current<FSServiceOrder.srvOrdType>>,
                                                     And<RelatedServiceOrder.sourceRefNbr, Equal<Current<FSServiceOrder.refNbr>>>>>
        {
            public RelatedServiceOrders_View(PXGraph graph) : base(graph)
            {
            }

            public RelatedServiceOrders_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        public class FSContact_View : PXSelect<FSContact,
                                      Where<
                                          FSContact.contactID, Equal<Current<FSServiceOrder.serviceOrderContactID>>>>
        {
            public FSContact_View(PXGraph graph) : base(graph)
            {
            }

            public FSContact_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        public class FSAddress_View : PXSelect<FSAddress,
                                      Where<
                                          FSAddress.addressID, Equal<Current<FSServiceOrder.serviceOrderAddressID>>>>
        {
            public FSAddress_View(PXGraph graph) : base(graph)
            {
            }

            public FSAddress_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        [PXDynamicButton(
            new string[] { DetailsPasteLineCommand, DetailsResetOrderCommand },
            new string[] { ActionsMessages.PasteLine, ActionsMessages.ResetOrder },
            TranslationKeyType = typeof(PX.Objects.Common.Messages))]
        public class ServiceOrderDetailsOrdered : PXOrderedSelect<FSServiceOrder, FSSODet,
                                                  Where<
                                                      FSSODet.sOID, Equal<Current<FSSODet.sOID>>>,
                                                  OrderBy<
                                                      Asc<FSSODet.srvOrdType,
                                                      Asc<FSSODet.refNbr,
                                                      Asc<FSSODet.sortOrder,
                                                      Asc<FSSODet.lineNbr>>>>>>
        {
            public ServiceOrderDetailsOrdered(PXGraph graph) : base(graph) { }

            public ServiceOrderDetailsOrdered(PXGraph graph, Delegate handler) : base(graph, handler) { }

            public const string DetailsPasteLineCommand = "DetailsPasteLine";
            public const string DetailsResetOrderCommand = "DetailsResetOrder";

            protected override void AddActions(PXGraph graph)
            {
                AddAction(graph, DetailsPasteLineCommand, ActionsMessages.PasteLine, PasteLine);
                AddAction(graph, DetailsResetOrderCommand, ActionsMessages.ResetOrder, ResetOrder);
            }
        }

        #endregion

        #region AppointmentViewTypes

        public class AppointmentRecords_View : PXSelectJoin<FSAppointment,
                LeftJoinSingleTable<Customer,
                    On<Customer.bAccountID, Equal<FSAppointment.customerID>>>,
                Where<
                    FSAppointment.srvOrdType, Equal<Optional<FSAppointment.srvOrdType>>,
                    And<
                        Where<
                            Customer.bAccountID, IsNull,
                            Or<Match<Customer, Current<AccessInfo.userName>>>>>>>
        {
            public AppointmentRecords_View(PXGraph graph) : base(graph)
            {
            }

            public AppointmentRecords_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        public class AppointmentSelected_View : PXSelect<FSAppointment,
                                                Where<
                                                    FSAppointment.appointmentID, Equal<Current<FSAppointment.appointmentID>>>>
        {
            public AppointmentSelected_View(PXGraph graph) : base(graph)
            {
            }

            public AppointmentSelected_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        [PXDynamicButton(new string[] { DetailsPasteLineCommand, DetailsResetOrderCommand },
                         new string[] { ActionsMessages.PasteLine, ActionsMessages.ResetOrder },
                         TranslationKeyType = typeof(Common.Messages))]
        public class AppointmentDetails_View : PXOrderedSelect<FSAppointment, FSAppointmentDet,
                                               Where<
                                                    FSAppointmentDet.appointmentID, Equal<Current<FSAppointment.appointmentID>>>,
                                               OrderBy<
                                                    Asc<FSAppointmentDet.srvOrdType,
                                                    Asc<FSAppointmentDet.refNbr,
                                                    Asc<FSAppointmentDet.sortOrder,
                                                    Asc<FSAppointmentDet.lineNbr>>>>>>
        {
            public AppointmentDetails_View(PXGraph graph) : base(graph) { }

            public AppointmentDetails_View(PXGraph graph, Delegate handler) : base(graph, handler) { }

            public const string DetailsPasteLineCommand = "DetailsPasteLine";
            public const string DetailsResetOrderCommand = "DetailsResetOrder";

            protected override void AddActions(PXGraph graph)
            {
                AddAction(graph, DetailsPasteLineCommand, ActionsMessages.PasteLine, PasteLine);
                AddAction(graph, DetailsResetOrderCommand, ActionsMessages.ResetOrder, ResetOrder);
            }
        }
        public class AppointmentResources_View : PXSelectJoin<FSAppointmentResource,
                                                 LeftJoin<FSEquipment,
                                                     On<FSEquipment.SMequipmentID, Equal<FSAppointmentResource.SMequipmentID>>>,
                                                 Where<
                                                     FSAppointmentResource.appointmentID, Equal<Current<FSAppointment.appointmentID>>>>
        {
            public AppointmentResources_View(PXGraph graph) : base(graph)
            {
            }

            public AppointmentResources_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        public class AppointmentLog_View : PXSelectJoin<FSAppointmentLog,
                                            LeftJoin<FSAppointmentDet,
                                                On<FSAppointmentDet.lineRef, Equal<FSAppointmentLog.detLineRef>,
                                                And<FSAppointmentDet.appointmentID, Equal<FSAppointmentLog.docID>>>>,
                                            Where<FSAppointmentLog.docID, Equal<Current<FSAppointment.appointmentID>>>>
        {
            public AppointmentLog_View(PXGraph graph) : base(graph)
            {
            }

            public AppointmentLog_View(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }
        #endregion

        #region Event Handlers
        protected virtual void _(Events.FieldUpdated<FSServiceOrder, FSServiceOrder.cBID> e)
        {
            if (e.NewValue == null)
            {
                BillingCycleRelated.Current = null;
            }
            else
            {
                BillingCycleRelated.Current = BillingCycleRelated.Select();
            }
        }
        #endregion

        #region Public Virtual Methods
        public virtual void FSServiceOrder_BranchLocationID_FieldUpdated_Handler(PXGraph graph,
                                                                                PXFieldUpdatedEventArgs e,
                                                                                FSSrvOrdType fsSrvOrdTypeRow,
                                                                                PXSelectBase<FSServiceOrder> serviceOrderRelated)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;
        }

        public virtual void FSServiceOrder_LocationID_FieldUpdated_Handler(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;
            if (cache.Graph.IsCopyPasteContext == false)
                SetBillCustomerAndLocationID(cache, fsServiceOrderRow);
        }

        public virtual void FSServiceOrder_ContactID_FieldUpdated_Handler(PXGraph graph, PXFieldUpdatedEventArgs e, FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;
        }

        public virtual void FSServiceOrder_BillCustomerID_FieldUpdated_Handler(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;

            if (cache.Graph.IsCopyPasteContext == false)
                cache.SetDefaultExt<FSServiceOrder.projectID>(e.Row);

            cache.SetValueExt<FSServiceOrder.billLocationID>(e.Row, GetDefaultLocationID(cache.Graph, fsServiceOrderRow.BillCustomerID));

            UpdateBillingInfo(cache, fsServiceOrderRow);
        }

        public virtual bool UpdateBillingInfo(PXCache cache, FSServiceOrder fsServiceOrderRow)
        {
            bool billingInfoUpdated = false;

            var billingInformation = GetBillingInformationByCustomer(cache.Graph, fsServiceOrderRow.BillCustomerID, fsServiceOrderRow.SrvOrdType);

            if (billingInformation != null && billingInformation.Count > 0)
            {
                var row = (PXResult<FSCustomerBillingSetup, FSBillingCycle, FSSetup>)billingInformation.First();

                FSCustomerBillingSetup fsCustomerBillingSetupRow = row;
                FSBillingCycle fsBillingCycle = row;

                if (fsServiceOrderRow.CBID != fsCustomerBillingSetupRow.CBID)
                {
                    cache.SetValueExt<FSServiceOrder.cBID>(fsServiceOrderRow, fsCustomerBillingSetupRow.CBID);
                    billingInfoUpdated = true;
                }

                if (fsServiceOrderRow.BillingBy != fsBillingCycle.BillingBy)
                {
                    cache.SetValueExt<FSServiceOrder.billingBy>(fsServiceOrderRow, fsBillingCycle.BillingBy);
                    billingInfoUpdated = true;
                }

                if (fsServiceOrderRow.BillOnlyCompletedClosed != fsBillingCycle.InvoiceOnlyCompletedServiceOrder)
                {
                    cache.SetValueExt<FSServiceOrder.billOnlyCompletedClosed>(fsServiceOrderRow, fsBillingCycle.InvoiceOnlyCompletedServiceOrder);
                    billingInfoUpdated = true;
                }
            }

            return billingInfoUpdated;
        }

        public virtual void FSServiceOrder_RowPersisting_Handler(ServiceOrderEntry graphServiceOrderEntry,
                                                                PXCache cacheServiceOrder,
                                                                PXRowPersistingEventArgs e,
                                                                FSSrvOrdType fsSrvOrdTypeRow,
                                                                PXSelectBase<FSSODet> serviceOrderDetails,
                                                                ServiceOrderAppointments_View serviceOrderAppointments,
                                                                AppointmentEntry graphAppointmentEntryCaller,
                                                                bool forceAppointmentCheckings)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = (FSServiceOrder)e.Row;

            FSAppointment fsAppointmentRowBeingSaved = null;

            if (graphAppointmentEntryCaller != null)
            {
                fsAppointmentRowBeingSaved = graphAppointmentEntryCaller.AppointmentRecords.Current;
            }

            if (e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update)
            {
                if (fsSrvOrdTypeRow == null)
                {
                    throw new PXException(TX.Error.SERVICE_ORDER_TYPE_X_NOT_FOUND, fsServiceOrderRow.SrvOrdType);
                }

                if (string.IsNullOrWhiteSpace(fsServiceOrderRow.DocDesc))
                {
                    SetDocDesc(graphServiceOrderEntry, fsServiceOrderRow);
                }

                if (fsServiceOrderRow.ProjectID != (int?)cacheServiceOrder.GetValueOriginal<FSServiceOrder.projectID>(fsServiceOrderRow)
                    || fsServiceOrderRow.BranchID != (int?)cacheServiceOrder.GetValueOriginal<FSServiceOrder.branchID>(fsServiceOrderRow))
                {
                    if (serviceOrderAppointments.Select().Count() > 0)
                    {
                        AppointmentEntry graphAppointmentEntry = PXGraph.CreateInstance<AppointmentEntry>();

                        foreach (FSAppointment fsAppointmentRow in serviceOrderAppointments.Select())
                        {
                            if (fsAppointmentRowBeingSaved == null || fsAppointmentRowBeingSaved.AppointmentID != fsAppointmentRow.AppointmentID)
                            {
                                FSAppointment fsAppointmentRow_local = graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Search<FSAppointment.refNbr>(fsAppointmentRow.RefNbr, fsAppointmentRow.SrvOrdType);
                                fsAppointmentRow_local.BranchID = fsServiceOrderRow.BranchID;
                                graphAppointmentEntry.AppointmentRecords.Update(fsAppointmentRow_local);

                                graphAppointmentEntry.UpdateDetailsFromProjectID(fsServiceOrderRow.ProjectID);
                                graphAppointmentEntry.UpdateDetailsFromBranchID(fsServiceOrderRow);

                                try
                                {
                                    graphAppointmentEntry.SkipServiceOrderUpdate = true;
                                    graphAppointmentEntry.Save.Press();
                                }
                                finally
                                {
                                    graphAppointmentEntry.SkipServiceOrderUpdate = false;
                                }
                            }
                        }

                        graphServiceOrderEntry.SelectTimeStamp();
                    }
                }

                if (fsSrvOrdTypeRow.RequireContact == true && fsServiceOrderRow.ContactID == null)
                {
                    throw new PXException(TX.Error.REQUIRED_CONTACT_MISSING);
                }

                IEnumerable<FSSODet> fsSODetList = serviceOrderDetails.Select().RowCast<FSSODet>();
                IEnumerable<FSSODet> serviceDetails = fsSODetList.Where(x => x.IsService == true);
                IEnumerable<FSSODet> partDetails = fsSODetList.Where(x => x.IsInventoryItem == true);

                UpdateServiceCounts(fsServiceOrderRow, serviceDetails);
                UpdatePendingPostFlags(graphServiceOrderEntry, fsServiceOrderRow, serviceOrderDetails);

                if (fsServiceOrderRow.Quote != true)
                {
                    fsServiceOrderRow.AppointmentsNeeded = fsServiceOrderRow.ApptNeededLineCntr > 0;
                }

                bool updateCutOffDate = false;

                if (e.Operation == PXDBOperation.Insert)
                {
                    updateCutOffDate = true;

                    SharedFunctions.CopyNotesAndFiles(cacheServiceOrder,
                                                      fsSrvOrdTypeRow,
                                                      fsServiceOrderRow,
                                                      fsServiceOrderRow.CustomerID,
                                                      fsServiceOrderRow.LocationID);
                }
                else if (e.Operation == PXDBOperation.Update)
                {
                    if ((DateTime?)cacheServiceOrder.GetValueOriginal<FSServiceOrder.orderDate>(fsServiceOrderRow) != fsServiceOrderRow.OrderDate)
                    {
                        updateCutOffDate = true;
                    }
                }

                if (updateCutOffDate)
                {
                    fsServiceOrderRow.CutOffDate = GetCutOffDate(graphServiceOrderEntry, fsServiceOrderRow.CBID, fsServiceOrderRow.OrderDate, fsServiceOrderRow.SrvOrdType);
                }
            }
            else
            {
                if (CanDeleteServiceOrder(graphServiceOrderEntry, fsServiceOrderRow) == false)
                {
                    throw new PXException(TX.Error.SERVICE_ORDER_CANNOT_BE_DELETED_BECAUSE_OF_ITS_STATUS);
                }
            }
        }

        public virtual void FSServiceOrder_CustomerID_FieldUpdated_Handler(PXCache cacheServiceOrder,
                                                                          FSServiceOrder fsServiceOrderRow,
                                                                          FSSrvOrdType fsSrvOrdTypeRow,
                                                                          PXSelectBase<FSSODet> serviceOrderDetails,
                                                                          PXSelectBase<FSAppointmentDet> appointmentDetails,
                                                                          PXResultset<FSAppointment> bqlResultSet_Appointment,
                                                                          int? oldCustomerID,
                                                                          DateTime? itemDateTime,
                                                                          bool allowCustomerChange,
                                                                          Customer customerRow)
        {
            if (fsServiceOrderRow == null)
            {
                return;
            }

            if (allowCustomerChange == false && CheckCustomerChange(cacheServiceOrder, fsServiceOrderRow, oldCustomerID, bqlResultSet_Appointment) == false)
            {
                return;
            }

            fsServiceOrderRow.ContactID = null;

            int? defaultLocationID = GetDefaultLocationID(cacheServiceOrder.Graph, fsServiceOrderRow.CustomerID);
            if (fsServiceOrderRow.LocationID != defaultLocationID)
            {
                cacheServiceOrder.SetValueExt<FSServiceOrder.locationID>(fsServiceOrderRow, defaultLocationID);
            }

            SetBillCustomerAndLocationID(cacheServiceOrder, fsServiceOrderRow);

            if (serviceOrderDetails != null)
            {
                RefreshSalesPricesInTheWholeDocument(serviceOrderDetails);
            }
            else if (appointmentDetails != null)
            {
                RefreshSalesPricesInTheWholeDocument(appointmentDetails);
            }

            if (cacheServiceOrder.Graph.IsCopyPasteContext == false)
            {
                // clear the ProjectID if it's not the default
                if (fsServiceOrderRow.ProjectID != 0)
                {
                    fsServiceOrderRow.ProjectID = null;
                }
            }
        }

        public virtual void FSServiceOrder_RowSelected_PartialHandler(PXGraph graph,
                                                                     PXCache cacheServiceOrder,
                                                                     FSServiceOrder fsServiceOrderRow,
                                                                     FSAppointment fsAppointmentRow,
                                                                     FSSrvOrdType fsSrvOrdTypeRow,
                                                                     FSBillingCycle fsBillingCycleRow,
                                                                     Contract contractRow,
                                                                     int appointmentCount,
                                                                     int detailsCount,
                                                                     PXCache cacheServiceOrderDetails,
                                                                     PXCache cacheServiceOrderAppointments,
                                                                     PXCache cacheServiceOrderEquipment,
                                                                     PXCache cacheServiceOrderEmployees,
                                                                     PXCache cacheServiceOrder_Contact,
                                                                     PXCache cacheServiceOrder_Address,
                                                                     bool? isBeingCalledFromQuickProcess,
                                                                     bool allowCustomerChange = false)
        {
            if (cacheServiceOrder.GetStatus(fsServiceOrderRow) == PXEntryStatus.Inserted)
            {
                if (fsSrvOrdTypeRow == null)
                {
                    throw new PXException(TX.Error.SERVICE_ORDER_TYPE_X_NOT_FOUND, fsServiceOrderRow.SrvOrdType);
                }
            }

            EnableDisable_Document(graph,
                                   cacheServiceOrder,
                                   fsServiceOrderRow,
                                   fsAppointmentRow,
                                   fsSrvOrdTypeRow,
                                   fsBillingCycleRow,
                                   appointmentCount,
                                   detailsCount,
                                   cacheServiceOrderDetails,
                                   cacheServiceOrderAppointments,
                                   cacheServiceOrderEquipment,
                                   cacheServiceOrderEmployees,
                                   cacheServiceOrder_Contact,
                                   cacheServiceOrder_Address,
                                   isBeingCalledFromQuickProcess,
                                   allowCustomerChange);

            Exception customerException = CheckIfCustomerBelongsToProject(graph, cacheServiceOrder, fsServiceOrderRow, contractRow);
            if (graph is ServiceOrderEntry && fsServiceOrderRow != null)
            {
                cacheServiceOrder.RaiseExceptionHandling<FSServiceOrder.projectID>
                                            (fsServiceOrderRow, fsServiceOrderRow.ProjectID, customerException);
            }
            else if (graph is AppointmentEntry && fsAppointmentRow != null)
            {
                ((AppointmentEntry)graph).AppointmentRecords.Cache.RaiseExceptionHandling<FSAppointment.projectID>
                                            (fsAppointmentRow, fsAppointmentRow.ProjectID, customerException);
            }
        }

        public virtual Exception CheckIfCustomerBelongsToProject(PXGraph graph, PXCache cache, FSServiceOrder fsServiceOrderRow, Contract ContractRow)
        {
            if (fsServiceOrderRow == null)
            {
                return null;
            }

            int? customerID = ContractRow?.CustomerID;

            Exception customerException = null;

            if (customerID != null
                    && fsServiceOrderRow.CustomerID != null
                        && customerID != fsServiceOrderRow.CustomerID)
            {
                customerException = new PXSetPropertyException(TX.Warning.CUSTOMER_DOES_NOT_MATCH_PROJECT, PXErrorLevel.Warning);
            }

            return customerException;
        }

        public virtual void RefreshSalesPricesInTheWholeDocument(PXSelectBase<FSSODet> serviceOrderDetails)
        {
            // TODO:
            // This method should run and depend on BillCustomerID changes, and not on CustomerID changes
            // Besides that, check if this is necessary using the Sales-Price graph extension

            foreach (FSSODet row in serviceOrderDetails.Select())
            {
                serviceOrderDetails.Cache.SetDefaultExt<FSSODet.curyUnitPrice>(row);
                serviceOrderDetails.Cache.Update(row);
            }
        }

        public virtual void FSServiceOrder_ProjectID_FieldUpdated_PartialHandler(FSServiceOrder fsServiceOrderRow, PXSelectBase<FSSODet> serviceOrderDetails)
        {
            if (fsServiceOrderRow.ProjectID == null)
            {
                return;
            }

            if (serviceOrderDetails != null)
            {
                foreach (FSSODet fsSODetRow in serviceOrderDetails.Select())
                {
                    fsSODetRow.ProjectID = fsServiceOrderRow.ProjectID;
                    fsSODetRow.ProjectTaskID = null;
                    serviceOrderDetails.Update(fsSODetRow);
                }
            }
        }

        public virtual void FSServiceOrder_BranchID_FieldUpdated_PartialHandler(FSServiceOrder fsServiceOrderRow, PXSelectBase<FSSODet> serviceOrderDetails)
        {
            if (fsServiceOrderRow.BranchID == null)
            {
                return;
            }

            if (serviceOrderDetails != null)
            {
                foreach (FSSODet fsSODetRow in serviceOrderDetails.Select())
                {
                    fsSODetRow.BranchID = fsServiceOrderRow.BranchID;
                    serviceOrderDetails.Update(fsSODetRow);
                }
            }
        }

        /// <summary>
        /// Calls SetEnabled and SetPersistingCheck for the specified field depending on the event that is running.
        /// </summary>
        /// <typeparam name="Field">The field to set properties.</typeparam>
        /// <param name="cache">The cache that is executing the event.</param>
        /// <param name="row">The row for which the event is executed.</param>
        /// <param name="eventType">The type of the event that is running.</param>
        /// <param name="isEnabled">True to enable the field. False to disable it.</param>
        /// <param name="persistingCheck">
        /// <para>The type of PersistingCheck for the field.</para>
        /// <para>Pass NULL if you don't want to set the PersistingCheck property for the field.</para>
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetEnabledAndPersistingCheck<Field>(PXCache cache,
                                                               object row,
                                                               EventType eventType,
                                                               bool isEnabled,
                                                               PXPersistingCheck? persistingCheck)
            where Field : class, IBqlField
        {
            if (eventType == EventType.RowSelectedEvent)
            {
                PXUIFieldAttribute.SetEnabled<Field>(cache, row, isEnabled);
            }

            if (persistingCheck != null)
            {
                PXDefaultAttribute.SetPersistingCheck<Field>(cache, row, (PXPersistingCheck)persistingCheck);
            }
        }

        public virtual void X_RowSelected<DAC>(PXCache cache,
                                              PXRowSelectedEventArgs e,
                                              FSServiceOrder fsServiceOrderRow,
                                              FSSrvOrdType fsSrvOrdTypeRow,
                                              bool disableSODetReferenceFields,
                                              bool docAllowsActualFieldEdition)
            where DAC : class, IBqlTable, IFSSODetBase, new()
        {
            X_RowSelected<DAC>(cache,
                               e.Row,
                               EventType.RowSelectedEvent,
                               fsServiceOrderRow,
                               fsSrvOrdTypeRow,
                               disableSODetReferenceFields,
                               docAllowsActualFieldEdition);
        }

        public virtual void X_SetPersistingCheck<DAC>(PXCache cache,
                                                     PXRowPersistingEventArgs e,
                                                     FSServiceOrder fsServiceOrderRow,
                                                     FSSrvOrdType fsSrvOrdTypeRow)
            where DAC : class, IBqlTable, IFSSODetBase, new()
        {
            X_RowSelected<DAC>(cache,
                               e.Row,
                               EventType.RowPersistingEvent,
                               fsServiceOrderRow,
                               fsSrvOrdTypeRow,
                               disableSODetReferenceFields: true,
                               docAllowsActualFieldEdition: false);
        }

        public virtual void X_LineType_FieldUpdated<DAC>(PXCache cache, PXFieldUpdatedEventArgs e)
            where DAC : class, IBqlTable, IFSSODetBase, new()
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            string newValue = row.LineType;
            string oldValue = (string)e.OldValue;

            Type dacType = typeof(DAC);
            FSSODet fsSODetRow = null;
            FSAppointmentDet fsAppointmentDetRow = null;

            if (dacType == typeof(FSAppointmentDet))
            {
                fsAppointmentDetRow = (FSAppointmentDet)e.Row;
            }
            else
            {
                fsSODetRow = (FSSODet)e.Row;
            }

            if (newValue != oldValue)
            {
                if ((newValue == ID.LineType_ALL.COMMENT || newValue == ID.LineType_ALL.INSTRUCTION)
                        || (oldValue == ID.LineType_ALL.COMMENT || oldValue == ID.LineType_ALL.INSTRUCTION))
                {
                    cache.SetDefaultExt<FSSODet.isBillable>(e.Row);
                }
            }

            cache.SetDefaultExt<FSSODet.isFree>(e.Row);

            if (IsInventoryLine(row.LineType) == false)
            {
                // Clear fields for non-inventory lines
                row.ManualPrice = false;

                row.InventoryID = null;
                row.SubItemID = null;

                row.BillingRule = ID.BillingRule.NONE;
                cache.SetDefaultExt<FSSODet.uOM>(e.Row);

                row.CuryUnitPrice = 0;
                row.SetDuration(FieldType.EstimatedField, 0, cache, false);
                row.SetQty(FieldType.EstimatedField, 0, cache, false);
                row.SiteID = null;
                row.SiteLocationID = null;

                row.ProjectTaskID = null;

                row.AcctID = null;
                row.SubID = null;

                if (fsAppointmentDetRow != null)
                {
                    fsAppointmentDetRow.ActualDuration = 0;
                    fsAppointmentDetRow.ActualQty = 0;
                }

                if (fsSODetRow != null)
                {
                    fsSODetRow.EnablePO = false;
                    fsSODetRow.POVendorID = null;
                    fsSODetRow.POVendorLocationID = null;
                }

                return;
            }

            // Set default values for common fields of FSSODet and FSAppointmentDet
            cache.SetDefaultExt<FSSODet.projectTaskID>(e.Row);

            // Set default values for specific fields of FSSODet
            if (fsSODetRow != null)
            {
                cache.SetDefaultExt<FSSODet.enablePO>(e.Row);
            }

            // Set default values for specific fields of FSAppointmentDet
            if (fsAppointmentDetRow != null)
            {
            }
        }

        public virtual void X_RowSelected<DAC>(PXCache cache,
                                               object eRow,
                                               EventType eventType,
                                               FSServiceOrder fsServiceOrderRow,
                                               FSSrvOrdType fsSrvOrdTypeRow,
                                               bool disableSODetReferenceFields,
                                               bool docAllowsActualFieldEdition)
                where DAC : class, IBqlTable, IFSSODetBase, new()
        {
            if (eRow == null)
            {
                return;
            }

            var row = (DAC)eRow;

            Type dacType = typeof(DAC);
            FSSODet fsSODetRow = null;
            bool calledFromSO = false;
            bool calledFromAPP = false;
            FSAppointmentDet fsAppointmentDetRow = null;

            if (dacType == typeof(FSAppointmentDet))
            {
                fsAppointmentDetRow = (FSAppointmentDet)eRow;
                calledFromAPP = true;
            }
            else
            {
                fsSODetRow = (FSSODet)eRow;
                calledFromSO = true;
            }

            bool isEnabled;
            PXPersistingCheck persistingCheck;
            bool isInventoryLine = IsInventoryLine(row.LineType);
            bool isStockItem;
            bool isPickupDeliveryItem;
            bool disabledByHeaderStatus = calledFromSO == true && fsServiceOrderRow?.AllowInvoice == true;

            if (isInventoryLine == false)
            {
                isStockItem = false;
            }
            else
            {
                if (row.LineType != ID.LineType_ALL.SERVICE
                        && row.LineType != ID.LineType_ALL.NONSTOCKITEM)
                {
                    isStockItem = true;
                }
                else
                {
                    isStockItem = false;
                }
            }

            isPickupDeliveryItem = row.LineType == ID.LineType_ALL.PICKUP_DELIVERY;

            // Enable/Disable SODetID
            SetEnabledAndPersistingCheck<FSSODet.sODetID>(cache, eRow, eventType,
                                            isEnabled: !disableSODetReferenceFields && !isPickupDeliveryItem, persistingCheck: null);

            // Enable/Disable LineType
            SetEnabledAndPersistingCheck<FSSODet.lineType>(cache, eRow, eventType,
                                            isEnabled: !disableSODetReferenceFields, persistingCheck: null);

            // Set InventoryID properties (SetEnabled and SetPersistingCheck)
            isEnabled = true;
            persistingCheck = PXPersistingCheck.NullOrBlank;

            if (isInventoryLine == false
                    && row.LineType != null)
            {
                isEnabled = false;
                persistingCheck = PXPersistingCheck.Nothing;
            }
            else if (row.IsPrepaid == true
                    || (disableSODetReferenceFields == true && isPickupDeliveryItem == false)
                    || (calledFromSO == true && fsServiceOrderRow.AllowInvoice == true)
                    || (calledFromSO == true && string.IsNullOrEmpty(fsSODetRow.Mem_LastReferencedBy) == false))
            {
                isEnabled = false;
            }

            SetEnabledAndPersistingCheck<FSSODet.inventoryID>(cache, eRow, eventType, isEnabled, persistingCheck);

            if (PXAccess.FeatureInstalled<FeaturesSet.subItem>())
            {
                // Set SubItemID properties BASED ON InventoryID and LineType
                SetEnabledAndPersistingCheck<FSSODet.subItemID>(cache, eRow, eventType,
                                                isEnabled: isEnabled && isStockItem,
                                                persistingCheck: isStockItem == true ? persistingCheck : PXPersistingCheck.Nothing);
            }

            // Set UOM properties SAME AS InventoryID
            SetEnabledAndPersistingCheck<FSSODet.uOM>(cache, eRow, eventType, isEnabled, persistingCheck);

            // Enable/Disable billingRule
            isEnabled = false;

            if ((row.LineType == ID.LineType_ALL.SERVICE)
                && row.IsPrepaid == false
                && (fsSODetRow == null || fsSODetRow.Mem_LastReferencedBy == null)
                && ((calledFromAPP == true && cache.GetStatus(fsAppointmentDetRow) == PXEntryStatus.Inserted && fsAppointmentDetRow.SODetID == null)
                    || (calledFromSO == true && disabledByHeaderStatus == false)))
            {
                isEnabled = true;
            }

            SetEnabledAndPersistingCheck<FSSODet.billingRule>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            // Enable/Disable ManualPrice
            isEnabled = true;

            if (row.IsPrepaid == true
                || isInventoryLine == false
                || row.InventoryID == null
                || row.BillingRule == ID.BillingRule.NONE
                || row.IsBillable == false
                || disabledByHeaderStatus == true)
            {
                isEnabled = false;
            }

            isEnabled = isEnabled && (fsSrvOrdTypeRow?.PostTo != ID.SrvOrdType_PostTo.PROJECTS || (fsSrvOrdTypeRow?.PostTo == ID.SrvOrdType_PostTo.PROJECTS && fsSrvOrdTypeRow.BillingType != ID.SrvOrdType_BillingType.COST_AS_COST));

            SetEnabledAndPersistingCheck<FSSODet.manualPrice>(cache, eRow, eventType,
                                            isEnabled, persistingCheck: null);

            // Set IsFree properties (SetEnabled and SetPersistingCheck)
            // Enable/Disable IsFree Same as ManualPrice
            SetEnabledAndPersistingCheck<FSSODet.isFree>(cache, eRow, eventType,
                                            isEnabled && row.ContractRelated == false, persistingCheck: null);

            // Enable/Disable isBillable
            isEnabled = true;

            if (row.IsPrepaid == true
                || isStockItem == true
                || row.Status == ID.Status_SODet.CANCELED
                || fsAppointmentDetRow?.Status == ID.Status_AppointmentDet.NOT_PERFORMED
                || isInventoryLine == false
                || row.ContractRelated == true
                || disabledByHeaderStatus == true)
            {
                isEnabled = false;
            }

            isEnabled = isEnabled && (fsSrvOrdTypeRow?.PostTo != ID.SrvOrdType_PostTo.PROJECTS || (fsSrvOrdTypeRow.PostTo == ID.SrvOrdType_PostTo.PROJECTS && fsSrvOrdTypeRow?.BillingType != ID.SrvOrdType_BillingType.COST_AS_COST));

            SetEnabledAndPersistingCheck<FSSODet.isBillable>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            // Enable/Disable Discount Fields
            isEnabled = true;

            if (row.BillingRule == ID.BillingRule.NONE == true
                || row.IsPrepaid == true
                || row.IsBillable == false
                || row.ContractRelated == true
                || isInventoryLine == false
                || row.InventoryID == null
                || disabledByHeaderStatus == true)
            {
                isEnabled = false;
            }

            isEnabled = isEnabled && (fsSrvOrdTypeRow?.PostTo != ID.SrvOrdType_PostTo.PROJECTS || (fsSrvOrdTypeRow?.PostTo == ID.SrvOrdType_PostTo.PROJECTS && fsSrvOrdTypeRow?.BillingType != ID.SrvOrdType_BillingType.COST_AS_COST));

            SetEnabledAndPersistingCheck<FSSODet.discPct>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            SetEnabledAndPersistingCheck<FSSODet.curyBillableExtPrice>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            SetEnabledAndPersistingCheck<FSSODet.curyDiscAmt>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            // Enable/Disable CuryUnitPrice
            isEnabled = false;

            if (row.BillingRule != ID.BillingRule.NONE
                && row.IsPrepaid == false
                && row.ContractRelated == false
                && row.InventoryID != null
                && row.IsFree != true
                && disabledByHeaderStatus == false
                && (fsSrvOrdTypeRow?.PostTo != ID.SrvOrdType_PostTo.PROJECTS
                    || (fsSrvOrdTypeRow?.PostTo == ID.SrvOrdType_PostTo.PROJECTS
                        && fsSrvOrdTypeRow?.BillingType != ID.SrvOrdType_BillingType.COST_AS_COST)))
            {
                isEnabled = true;
            }

            SetEnabledAndPersistingCheck<FSSODet.curyUnitPrice>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            // Enable/Disable EstimatedDuration and ActualDuration
            isEnabled = false;

            if ((row.LineType == ID.LineType_ALL.SERVICE || row.LineType == ID.LineType_ALL.NONSTOCKITEM)
                && row.InventoryID != null
                && disabledByHeaderStatus == false)
            {
                isEnabled = true;
            }

            SetEnabledAndPersistingCheck<FSSODet.estimatedDuration>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            if (fsAppointmentDetRow != null)
            {
                //Enable when there is only 1 or 0 staff related to the service
                bool enableByLogRelated = fsAppointmentDetRow.LogRelatedCount == 0;
                SetEnabledAndPersistingCheck<FSAppointmentDet.actualDuration>(cache, eRow, eventType,
                                            isEnabled: isEnabled && docAllowsActualFieldEdition && enableByLogRelated, persistingCheck: null);
            }


            // Enable/Disable EstimatedQty and ActualQty
            isEnabled = false;

            if (isInventoryLine == true
                && row.BillingRule != ID.BillingRule.TIME
                && row.IsPrepaid == false
                && row.InventoryID != null
                && disabledByHeaderStatus == false)
            {
                isEnabled = true;
            }

            SetEnabledAndPersistingCheck<FSSODet.estimatedQty>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            if (fsAppointmentDetRow != null)
            {
                SetEnabledAndPersistingCheck<FSAppointmentDet.actualQty>(cache, eRow, eventType,
                                            isEnabled: isEnabled && docAllowsActualFieldEdition, persistingCheck: null);
            }

            // Set SiteID properties (SetEnabled and SetPersistingCheck)
            isEnabled = false;
            persistingCheck = PXPersistingCheck.Nothing;

            if (row.InventoryID != null
                    && fsSrvOrdTypeRow?.PostTo != ID.SrvOrdType_PostTo.ACCOUNTS_RECEIVABLE_MODULE
                    && disabledByHeaderStatus == false)
            {
                if (row.IsPrepaid == false)
                {
                    if (fsAppointmentDetRow != null)
                    {
                        //Disable if line exists in more than 1 appointment. 
                        FSSODet fsSODetSelectorRow = FSSODet.UK.Find(cache.Graph, fsAppointmentDetRow.SODetID);
                        isEnabled = fsSODetSelectorRow == null
                                        || (fsSODetSelectorRow.ApptCntr == 1 && fsAppointmentDetRow.AppDetID > 0);
                    }
                    else
                    {
                        isEnabled = true;
                    }

                    persistingCheck = PXPersistingCheck.NullOrBlank;
                }
            }

            if (PXAccess.FeatureInstalled<FeaturesSet.inventory>())
            {
                SetEnabledAndPersistingCheck<FSSODet.siteID>(cache, eRow, eventType, isEnabled, persistingCheck);

                // LocationID is always disabled for non-stocks items
                // in other case is enabled/disabled as SiteID
                SetEnabledAndPersistingCheck<FSSODet.siteLocationID>(cache, eRow, eventType, isStockItem && isEnabled, persistingCheck);
            }

            if (PXAccess.FeatureInstalled<FeaturesSet.projectModule>())
            {
                // Set ProjectID properties (SetEnabled and SetPersistingCheck)
                isEnabled = false;
                persistingCheck = PXPersistingCheck.Nothing;

                if (isInventoryLine == true && row.InventoryID != null
                    && disabledByHeaderStatus == false)
                {
                    isEnabled = true;
                    persistingCheck = PXPersistingCheck.NullOrBlank;
                }

                SetEnabledAndPersistingCheck<FSSODet.projectID>(cache, eRow, eventType, isEnabled, persistingCheck);

                // Set ProjectTaskID properties (SetEnabled and SetPersistingCheck)
                isEnabled = true;
                persistingCheck = PXPersistingCheck.Nothing;

                if (isInventoryLine == false
                    || row.InventoryID == null
                    || row.ContractRelated == true
                    || disabledByHeaderStatus == true)
                {
                    isEnabled = false;
                }
                else if (ProjectDefaultAttribute.IsProject(cache.Graph, row.ProjectID) == true)
                {
                    persistingCheck = PXPersistingCheck.NullOrBlank;
                }

                SetEnabledAndPersistingCheck<FSSODet.projectTaskID>(cache, eRow, eventType, isEnabled, persistingCheck);
            }

            // Set CostCodeID properties (SetEnabled and SetPersistingCheck)
            isEnabled = true;
            persistingCheck = PXPersistingCheck.Nothing;

            if (isInventoryLine == false
                || row.InventoryID == null
                || row.ContractRelated == true
                || disabledByHeaderStatus == true)
            {
                isEnabled = false;
            }
            else if (ProjectDefaultAttribute.IsProject(cache.Graph, row.ProjectID) == true)
            {
                persistingCheck = PXPersistingCheck.NullOrBlank;
            }

            SetEnabledAndPersistingCheck<FSSODet.costCodeID>(cache, eRow, eventType, isEnabled, persistingCheck);

            // Set AcctID properties (SetEnabled and SetPersistingCheck)
            isEnabled = false;
            persistingCheck = PXPersistingCheck.Nothing;

            if (isInventoryLine == true
                && row.InventoryID != null
                && fsServiceOrderRow?.Quote == false
                && disabledByHeaderStatus == false
                && fsSrvOrdTypeRow?.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
            {
                isEnabled = true;
                persistingCheck = PXPersistingCheck.NullOrBlank;
            }

            SetEnabledAndPersistingCheck<FSSODet.acctID>(cache, eRow, eventType, isEnabled, persistingCheck);

            // Set SubID properties SAME AS AcctID
            SetEnabledAndPersistingCheck<FSSODet.subID>(cache, eRow, eventType, isEnabled, persistingCheck);

            // Set PickupDeliveryServiceID properties (SetEnabled and SetPersistingCheck)
            if (fsAppointmentDetRow != null)
            {
                isEnabled = false;
                persistingCheck = PXPersistingCheck.Nothing;

                if (isPickupDeliveryItem)
                {
                    isEnabled = true;
                    persistingCheck = PXPersistingCheck.NullOrBlank;
                }

                SetEnabledAndPersistingCheck<FSAppointmentDet.pickupDeliveryAppLineRef>(cache, eRow, eventType, isEnabled: isEnabled, persistingCheck: persistingCheck);
            }

            // Set LotSerial properties (SetEnabled and SetPersistingCheck)
            if (fsAppointmentDetRow != null)
            {
                isEnabled = false;
                persistingCheck = PXPersistingCheck.Nothing;

                if (row.LineType == ID.LineType_ALL.INVENTORY_ITEM)
                {
                    isEnabled = true;
                }

                SetEnabledAndPersistingCheck<FSSODet.lotSerialNbr>(cache, eRow, eventType, isEnabled, persistingCheck);
            }

            // Set Tax Category (SetEnabled and SetPersistingCheck)
            isEnabled = true;

            if (row.IsPrepaid == true
                || disabledByHeaderStatus == true)
            {
                isEnabled = false;
            }

            SetEnabledAndPersistingCheck<FSSODet.taxCategoryID>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            // Disable field in Pickup Delivery item
            if (isPickupDeliveryItem == true)
            {
                isEnabled = false;
                persistingCheck = PXPersistingCheck.Nothing;

                // Set StaffID (SetEnabled and SetPersistingCheck)
                SetEnabledAndPersistingCheck<FSSODet.staffID>(cache, eRow, eventType, isEnabled, persistingCheck: null);
            }

            isEnabled = false;

            if (row.LineType == ID.LineType_ALL.SERVICE
                && (fsSODetRow == null
                    || fsSODetRow.EnableStaffID == true))
            {
                isEnabled = true;
            }

            SetEnabledAndPersistingCheck<FSSODet.staffID>(cache, eRow, eventType, isEnabled, persistingCheck: null);


            // Set TranDesc properties (SetEnabled and SetPersistingCheck)
            isEnabled = true;
            persistingCheck = PXPersistingCheck.Nothing;

            if (isInventoryLine == false)
            {
                persistingCheck = PXPersistingCheck.NullOrBlank;
            }

            SetEnabledAndPersistingCheck<FSSODet.tranDesc>(cache, eRow, eventType, isEnabled, persistingCheck);

            // Set TaxCategory properties (SetEnabled and SetPersistingCheck)
            isEnabled = false;

            if (row.InventoryID != null
                && isInventoryLine == true
                && disabledByHeaderStatus == false)
            {
                isEnabled = true;
            }

            SetEnabledAndPersistingCheck<FSSODet.taxCategoryID>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            isEnabled = false;

            if (row.EnablePO == true
                && (fsAppointmentDetRow == null
                    || fsAppointmentDetRow.CanChangeMarkForPO == true)
                && (fsSODetRow == null
                    || fsSODetRow.POSource == ListField_FSPOSource.PurchaseToServiceOrder))
            {
                isEnabled = true;
            }

            isEnabled = isEnabled
                && (fsSrvOrdTypeRow.PostTo != ID.SrvOrdType_PostTo.PROJECTS
                    || (fsSrvOrdTypeRow.PostTo == ID.SrvOrdType_PostTo.PROJECTS
                        && fsSrvOrdTypeRow.BillingType != ID.SrvOrdType_BillingType.COST_AS_COST));

            SetEnabledAndPersistingCheck<FSSODet.curyUnitCost>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            isEnabled = false;

            if (row.InventoryID != null
                && row.LineType == ID.LineType_ALL.INVENTORY_ITEM
                && disabledByHeaderStatus == false)
            {
                isEnabled = true;
            }

            SetEnabledAndPersistingCheck<FSSODet.equipmentAction>(cache, eRow, eventType, isEnabled, persistingCheck: null);

            SharedFunctions.SetEquipmentFieldEnablePersistingCheck(cache, eRow);

            // Additional Enable/Disable for Equipment fields
            if (disabledByHeaderStatus == true)
            {
                isEnabled = false;
                SetEnabledAndPersistingCheck<FSSODet.SMequipmentID>(cache, eRow, eventType, isEnabled, persistingCheck: null);
            }

            // Enable/Disable PO fields
            isEnabled = false;
            string postTo = fsSrvOrdTypeRow?.PostTo ?? string.Empty;
            string rowStatus = row.Status ?? string.Empty;

            if (isInventoryLine == true
                    && row.IsPrepaid == false
                    && postTo.IsIn(ID.Batch_PostTo.SO,
                                    ID.Batch_PostTo.SI,
                                    ID.Batch_PostTo.PM)
                    && (fsAppointmentDetRow == null
                            || rowStatus.IsIn(ID.Status_AppointmentDet.NOT_STARTED,
                                                ID.Status_AppointmentDet.COMPLETED,
                                                ID.Status_AppointmentDet.RequestForPO)
                            || fsAppointmentDetRow.CanChangeMarkForPO == true
                    )
                    && (fsSODetRow == null
                            || fsSODetRow.EnablePO == false
                            || fsSODetRow.POSource == ListField_FSPOSource.PurchaseToServiceOrder
                    )
            )
            {
                isEnabled = true;
            }

            SetEnabledAndPersistingCheck<FSSODet.enablePO>(cache, eRow, eventType, isEnabled, persistingCheck: null);
            SetEnabledAndPersistingCheck<FSSODet.pOCreate>(cache, eRow, eventType, isEnabled, persistingCheck: null);
            SetEnabledAndPersistingCheck<FSSODet.poVendorID>(cache, eRow, eventType, isEnabled && row.EnablePO == true, persistingCheck: null);
            SetEnabledAndPersistingCheck<FSSODet.poVendorLocationID>(cache, eRow, eventType, isEnabled && row.EnablePO == true, persistingCheck: null);
        }

        public virtual void X_IsPrepaid_FieldUpdated<DAC,
                                                    ManualPrice,
                                                    IsFree,
                                                    EstimatedDuration,
                                                    ActualDuration>(PXCache cache,
                                                                    PXFieldUpdatedEventArgs e,
                                                                    bool useActualField)
            where DAC : class, IBqlTable, IFSSODetBase, new()
            where ManualPrice : class, IBqlField
            where IsFree : class, IBqlField
            where EstimatedDuration : class, IBqlField
            where ActualDuration : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            if (row.IsPrepaid == true)
            {
                cache.SetValueExt<IsFree>(e.Row, true);

                cache.RaiseFieldUpdated<EstimatedDuration>(e.Row, 0);

                if (useActualField == true)
                {
                    cache.RaiseFieldUpdated<ActualDuration>(e.Row, 0);
                }
            }
        }

        public virtual void X_InventoryID_FieldUpdated<DAC,
                                                      AcctID,
                                                      SubItemID,
                                                      SiteID,
                                                      SiteLocationID,
                                                      UOM,
                                                      EstimatedDuration,
                                                      EstimatedQty,
                                                      BillingRule,
                                                      ActualDuration,
                                                      ActualQty>(PXCache cache,
                                                                 PXFieldUpdatedEventArgs e,
                                                                 int? branchLocationID,
                                                                 Customer billCustomerRow,
                                                                 bool useActualFields)
            where DAC : class, IBqlTable, IFSSODetBase, new()
            where AcctID : class, IBqlField
            where SubItemID : class, IBqlField
            where SiteID : class, IBqlField
            where SiteLocationID : class, IBqlField
            where UOM : class, IBqlField
            where EstimatedDuration : class, IBqlField
            where EstimatedQty : class, IBqlField
            where BillingRule : class, IBqlField
            where ActualDuration : class, IBqlField
            where ActualQty : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;
            InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(cache.Graph, row.InventoryID);

            if (inventoryItemRow != null && row.LineType == null)
            {
                if (inventoryItemRow.StkItem == true)
                {
                    row.LineType = ID.LineType_ALL.INVENTORY_ITEM;
                }
                else if (inventoryItemRow.ItemType == INItemTypes.ServiceItem)
                {
                    row.LineType = ID.LineType_ALL.SERVICE;
                }
                else
                {
                    row.LineType = ID.LineType_ALL.NONSTOCKITEM;
                }

                cache.SetDefaultExt<AcctID>(row);
            }

            // This is required in Inventory FieldUpdated events
            if (e.ExternalCall)
            {
                row.CuryUnitPrice = 0m;
            }

            if (IsInventoryLine(row.LineType) == false
                    || (row.InventoryID == null && row.LineType != ID.LineType_ALL.PICKUP_DELIVERY))
            {
                // Clear fields for non-inventory lines
                row.IsFree = true;
                row.ManualPrice = false;

                row.SubItemID = null;
                row.SiteID = null;
                row.SiteLocationID = null;

                cache.SetDefaultExt<FSSODet.uOM>(e.Row);

                cache.RaiseExceptionHandling<UOM>(e.Row, null, null);

                row.SetDuration(FieldType.EstimatedField, 0, cache, false);
                row.SetQty(FieldType.EstimatedField, 0, cache, false);

                if (useActualFields == true)
                {
                    row.SetDuration(FieldType.ActualField, 0, cache, false);
                    row.SetQty(FieldType.ActualField, 0, cache, false);
                }

                row.BillingRule = ID.BillingRule.NONE;

                return;
            }

            row.TranDesc = null;
            if (inventoryItemRow != null)
            {
                row.TranDesc = PXDBLocalizableStringAttribute.GetTranslation(cache.Graph.Caches[typeof(InventoryItem)], inventoryItemRow, "Descr", billCustomerRow?.LocaleName);
            }

            FSxService fsxServiceRow = null;

            // UOM is assigned to null here to avoid price calculation while duration and qty fields are defaulted.
            row.UOM = null;
            cache.RaiseExceptionHandling<UOM>(e.Row, null, null);

            if (row.LineType == ID.LineType_ALL.SERVICE && inventoryItemRow != null)
            {
                fsxServiceRow = PXCache<InventoryItem>.GetExtension<FSxService>(inventoryItemRow);

                cache.SetValueExt<BillingRule>(e.Row, fsxServiceRow.BillingRule);
            }
            else
            {
                cache.SetValueExt<BillingRule>(e.Row, ID.BillingRule.FLAT_RATE);
            }

            cache.SetDefaultExt<SubItemID>(e.Row);

            object newValue = null;
            cache.RaiseFieldDefaulting<SiteID>(e.Row, out newValue);
            int? defaultSiteID = (int?)newValue;
            bool validateSiteID = true;

            if (inventoryItemRow != null && row.SiteLocationID == null && row.LineType == ID.LineType_ALL.INVENTORY_ITEM)
            {
                if (defaultSiteID == null)
                {
                    defaultSiteID = inventoryItemRow.DfltSiteID;
                }

                if (defaultSiteID == null)
                {
                    //TODO: this query may return multiple records
                    INItemSite inItemSiteRow = PXSelectJoin<INItemSite,
                                                    InnerJoin<INSite,
                                                        On<INSite.siteID, Equal<INItemSite.siteID>>>,
                                               Where<
                                                    INItemSite.inventoryID, Equal<Required<INItemSite.inventoryID>>,
                                                    And<Match<INSite, Current<AccessInfo.userName>>>>>
                                               .Select(cache.Graph, inventoryItemRow.InventoryID);
                    if (inItemSiteRow != null)
                    {
                        defaultSiteID = inItemSiteRow.SiteID;
                        validateSiteID = false;
                    }
                }
            }

            int? defaultSubItemID = row.SubItemID;
            newValue = null;
            cache.RaiseFieldDefaulting<UOM>(e.Row, out newValue);
            string defaultUOM = (string)newValue;

            CompleteItemInfoUsingBranchLocation(cache.Graph,
                                                branchLocationID,
                                                inventoryItemRow != null ? inventoryItemRow.DefaultSubItemOnEntry : false,
                                                ref defaultSubItemID,
                                                ref defaultUOM,
                                                ref defaultSiteID);
            row.SubItemID = defaultSubItemID;

            string postTo = string.Empty;
            if (cache.Graph is ServiceOrderEntry)
            {
                ServiceOrderEntry graph = cache.Graph as ServiceOrderEntry;
                postTo = graph.ServiceOrderTypeSelected.Current?.PostTo;
            }
            else if (cache.Graph is AppointmentEntry)
            {
                AppointmentEntry graph = cache.Graph as AppointmentEntry;
                postTo = graph.ServiceOrderTypeSelected.Current?.PostTo;
            }

            if (validateSiteID)
            {
                defaultSiteID = GetValidatedSiteID(cache.Graph, defaultSiteID);
            }

            if (defaultSiteID != null && postTo != ID.SrvOrdType_PostTo.ACCOUNTS_RECEIVABLE_MODULE)
            {
                cache.SetValueExt<SiteID>(e.Row, defaultSiteID);
            }

            if (row.SiteLocationID == null && inventoryItemRow != null && postTo != ID.SrvOrdType_PostTo.ACCOUNTS_RECEIVABLE_MODULE)
            {
                if (row.SiteID == inventoryItemRow.DfltSiteID && row.LineType == ID.LineType_ALL.INVENTORY_ITEM)
                {
                    row.SiteLocationID = inventoryItemRow.DfltShipLocationID;
                }
            }

            cache.SetValueExt<UOM>(e.Row, defaultUOM);

            row.SetDuration(FieldType.EstimatedField, 0, cache, false);

            // EstimatedQty MUST be assigned after BillingRule BUT before EstimatedDuration.
            bool isFromInsert = (cache.GetStatus(row) == PXEntryStatus.Notchanged) && cache.Locate(row) == null;

            if (!isFromInsert || cache.Graph.IsContractBasedAPI == false)
            {
                cache.SetDefaultExt<EstimatedQty>(e.Row);
            }

            if (row.LineType == ID.LineType_ALL.SERVICE && fsxServiceRow != null)
            {
                cache.SetValueExt<EstimatedDuration>(e.Row, fsxServiceRow.EstimatedDuration ?? 0);
            }

            if (useActualFields == true)
            {
                cache.SetDefaultExt<ActualQty>(e.Row);
                cache.SetDefaultExt<ActualDuration>(e.Row);
            }
        }

        public virtual void X_BillingRule_FieldVerifying<DAC>(PXCache cache, PXFieldVerifyingEventArgs e)
            where DAC : class, IBqlTable, IFSSODetBase, new()
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            if (IsInventoryLine(row.LineType) == false
                || row.InventoryID == null)
            {
                e.NewValue = ID.BillingRule.NONE;
            }
            else if (row.LineType == ID.LineType_ALL.NONSTOCKITEM
                    || row.LineType == ID.LineType_ALL.INVENTORY_ITEM
                    || row.LineType == ID.LineType_ALL.PICKUP_DELIVERY)
            {
                e.NewValue = ID.BillingRule.FLAT_RATE;
            }
        }

        public virtual void X_BillingRule_FieldUpdated<DAC,
                                                      EstimatedDuration,
                                                      ActualDuration,
                                                      CuryUnitPrice,
                                                      IsFree>(PXCache cache,
                                                                     PXFieldUpdatedEventArgs e,
                                                                     bool useActualField)
            where DAC : class, IBqlTable, IFSSODetBase, new()
            where EstimatedDuration : class, IBqlField
            where ActualDuration : class, IBqlField
            where CuryUnitPrice : class, IBqlField
            where IsFree : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;
            string newValue = row.BillingRule;
            string oldValue = (string)e.OldValue;

            if (newValue != oldValue
                 && (newValue == ID.BillingRule.NONE || oldValue == ID.BillingRule.NONE))
            {
                cache.SetDefaultExt<IsFree>(e.Row);
            }

            if (row.LineType == ID.LineType_ALL.SERVICE && row.BillingRule == ID.BillingRule.TIME)
            {
                cache.RaiseFieldUpdated<EstimatedDuration>(e.Row, 0);

                if (useActualField == true)
                {
                    cache.RaiseFieldUpdated<ActualDuration>(e.Row, 0);
                }
            }
            else
            {
                cache.SetDefaultExt<CuryUnitPrice>(e.Row);
            }
        }

        public virtual void X_UOM_FieldUpdated<CuryUnitPrice>(PXCache cache, PXFieldUpdatedEventArgs e)
            where CuryUnitPrice : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            cache.SetDefaultExt<CuryUnitPrice>(e.Row);
        }

        public virtual void X_SiteID_FieldUpdated<CuryUnitPrice, AcctID, SubID>(PXCache cache, PXFieldUpdatedEventArgs e)
                where CuryUnitPrice : class, IBqlField
                where AcctID : class, IBqlField
                where SubID : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            cache.SetDefaultExt<CuryUnitPrice>(e.Row);
            cache.SetDefaultExt<AcctID>(e.Row);

            try
            {
                cache.SetDefaultExt<SubID>(e.Row);
            }
            catch (PXSetPropertyException)
            {
                cache.SetValue<SubID>(e.Row, null);
            }
        }

        public virtual void X_Qty_FieldUpdated<CuryUnitPrice>(PXCache cache, PXFieldUpdatedEventArgs e)
            where CuryUnitPrice : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            cache.SetDefaultExt<CuryUnitPrice>(e.Row);
        }

        public virtual void X_ManualPrice_FieldUpdated<DAC, CuryUnitPrice, CuryBillableExtPrice>(PXCache cache, PXFieldUpdatedEventArgs e)
            where DAC : class, IFSSODetBase, new()
            where CuryUnitPrice : class, IBqlField
            where CuryBillableExtPrice : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            cache.SetDefaultExt<CuryUnitPrice>(e.Row);
            if (row.IsFree == true
                   && (bool?)e.OldValue == true
                   && row.ManualPrice == false)
            {
                cache.SetValueExt<CuryBillableExtPrice>(e.Row, 0m);
            }
        }

        public virtual void X_CuryUnitPrice_FieldDefaulting<DAC, CuryUnitPrice>(PXCache cache,
                                                                               PXFieldDefaultingEventArgs e,
                                                                               decimal? qty,
                                                                               DateTime? docDate,
                                                                               FSServiceOrder fsServiceOrderRow,
                                                                               FSAppointment fsAppointmentRow,
                                                                               CurrencyInfo currencyInfo)
            where DAC : class, IBqlTable, IFSSODetBase, new()
            where CuryUnitPrice : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            // TODO: AC-142850 AC-97482
            // FSSODet does not have PriceType nor PriceCode.
            FSAppointmentDet fsAppointmentDetRow = null;
            Type dacType = typeof(DAC);

            if (dacType == typeof(FSAppointmentDet))
            {
                fsAppointmentDetRow = (FSAppointmentDet)e.Row;
            }

            if (row.InventoryID == null ||
                row.UOM == null ||
                    (row.BillingRule == ID.BillingRule.NONE && row.ManualPrice != true))
            {
                // Special cases with price 0
                PXUIFieldAttribute.SetWarning<CuryUnitPrice>(cache, row, null);
                e.NewValue = 0m;

                // TODO: AC-142850 AC-97482
                if (fsAppointmentDetRow != null)
                {
                    fsAppointmentDetRow.PriceType = null;
                    fsAppointmentDetRow.PriceCode = null;
                }
            }
            else if (row.ManualPrice != true && !cache.Graph.IsCopyPasteContext && row.IsFree != true)
            {
                SalesPriceSet salesPriceSet = FSPriceManagement.CalculateSalesPriceWithCustomerContract(
                                                        cache,
                                                        fsServiceOrderRow.ServiceContractID,
                                                        fsAppointmentRow != null ? fsAppointmentRow.BillServiceContractID : fsServiceOrderRow.BillServiceContractID,
                                                        fsAppointmentRow != null ? fsAppointmentRow.BillContractPeriodID : fsServiceOrderRow.BillContractPeriodID,
                                                        fsServiceOrderRow.BillCustomerID,
                                                        fsServiceOrderRow.BillLocationID,
                                                        row.ContractRelated,
                                                        row.InventoryID,
                                                        row.SiteID,
                                                        qty,
                                                        row.UOM,
                                                        (DateTime)(docDate ?? cache.Graph.Accessinfo.BusinessDate),
                                                        row.CuryUnitPrice,
                                                        alwaysFromBaseCurrency: false,
                                                        currencyInfo: currencyInfo.GetCM(),
                                                        catchSalesPriceException: false);

                if (salesPriceSet.ErrorCode == ID.PriceErrorCode.UOM_INCONSISTENCY)
                {
                    InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(cache.Graph, row.InventoryID);
                    throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.INVENTORY_ITEM_UOM_INCONSISTENCY, inventoryItemRow.InventoryCD), PXErrorLevel.Error);
                }

                e.NewValue = salesPriceSet.Price ?? 0m;

                if (fsAppointmentDetRow != null)
                {
                    // These fields are just report fields so they wouldn't have associated events
                    // and therefore they don't need be assigned with SetValueExt.
                    fsAppointmentDetRow.PriceType = salesPriceSet.PriceType;
                    fsAppointmentDetRow.PriceCode = salesPriceSet.PriceCode;
                }

                ARSalesPriceMaint.CheckNewUnitPrice<DAC, CuryUnitPrice>(cache, row, salesPriceSet.Price);
            }
            else
            {
                e.NewValue = row.CuryUnitPrice ?? 0m;
                e.Cancel = row.CuryUnitPrice != null;
                return;
            }
        }

        public virtual void X_Duration_FieldUpdated<DAC, Qty>(PXCache cache, PXFieldUpdatedEventArgs e, int? duration)
            where DAC : class, IBqlTable, IFSSODetBase, new()
            where Qty : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            if (row.LineType == ID.LineType_ALL.SERVICE && row.BillingRule == ID.BillingRule.TIME
                    && row.IsPrepaid == false)
            {
                cache.SetValueExt<Qty>(e.Row, PXDBQuantityAttribute.Round(decimal.Divide((decimal)(duration ?? 0), 60)));
            }
        }

        public virtual void CompleteItemInfoUsingBranchLocation(PXGraph graph,
                                                                int? branchLocationID,
                                                                bool? defaultSubItemOnEntry,
                                                                ref int? SubItemID,
                                                                ref string UOM,
                                                                ref int? SiteID)
        {
            if (branchLocationID == null)
            {
                return;
            }

            if ((SubItemID == null && defaultSubItemOnEntry == true)
                    || string.IsNullOrEmpty(UOM)
                    || SiteID == null)
            {
                FSBranchLocation fsBranchLocationRow = FSBranchLocation.PK.Find(graph, branchLocationID);

                if (fsBranchLocationRow != null)
                {
                    if (SubItemID == null && defaultSubItemOnEntry == true)
                    {
                        SubItemID = fsBranchLocationRow.DfltSubItemID;
                    }

                    if (string.IsNullOrEmpty(UOM))
                    {
                        UOM = fsBranchLocationRow.DfltUOM;
                    }

                    if (SiteID == null)
                    {
                        SiteID = fsBranchLocationRow.DfltSiteID;
                    }
                }
            }
        }

        public virtual bool IsInventoryLine(string lineType)
        {
            if (lineType == null
                || lineType == ID.LineType_ALL.COMMENT
                || lineType == ID.LineType_ALL.INSTRUCTION
                || lineType == ID.LineType_ALL.SERVICE_TEMPLATE)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public virtual void CheckIfManualPrice<DAC, Qty>(PXCache cache, PXRowUpdatedEventArgs e)
            where DAC : class, IBqlTable, IFSSODetBase, new()
            where Qty : class, IBqlField
        {
            if (e.Row == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            if ((string)cache.GetValue<FSSODet.billingRule>(e.Row) == ID.BillingRule.NONE
                || (string)cache.GetValue<FSSODet.billingRule>(e.OldRow) == ID.BillingRule.NONE)
            {
                return;
            }

            if ((e.ExternalCall || cache.Graph.IsImport)
                && cache.ObjectsEqual<FSSODet.branchID>(e.Row, e.OldRow)
                && cache.ObjectsEqual<FSSODet.inventoryID>(e.Row, e.OldRow)
                && cache.ObjectsEqual<FSSODet.uOM>(e.Row, e.OldRow)
                && cache.ObjectsEqual<Qty>(e.Row, e.OldRow)
                && cache.ObjectsEqual<FSSODet.siteID>(e.Row, e.OldRow)
                && cache.ObjectsEqual<FSSODet.manualPrice>(e.Row, e.OldRow)
                && (!cache.ObjectsEqual<FSSODet.curyUnitPrice>(e.Row, e.OldRow) || !cache.ObjectsEqual<FSSODet.curyBillableExtPrice>(e.Row, e.OldRow))
                && cache.ObjectsEqual<FSSODet.status>(e.Row, e.OldRow)
            )
            {
                row.ManualPrice = true;
            }
        }

        public virtual void CheckSOIfManualCost(PXCache cache, PXRowUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            if ((e.ExternalCall || cache.Graph.IsImport)
                && cache.ObjectsEqual<FSSODet.branchID>(e.Row, e.OldRow)
                && cache.ObjectsEqual<FSSODet.inventoryID>(e.Row, e.OldRow)
                && cache.ObjectsEqual<FSSODet.uOM>(e.Row, e.OldRow)
                && cache.ObjectsEqual<FSSODet.siteID>(e.Row, e.OldRow)
                && cache.ObjectsEqual<FSSODet.manualCost>(e.Row, e.OldRow)
                && !cache.ObjectsEqual<FSSODet.curyUnitCost>(e.Row, e.OldRow))
            {
                if (e.Row is FSSODet)
                {
                    var rowSODet = (FSSODet)e.Row;
                    rowSODet.ManualCost = true;
                }
                else if (e.Row is FSAppointmentDet)
                {
                    var rowAppDet = (FSAppointmentDet)e.Row;
                    rowAppDet.ManualCost = true;
                }
            }
        }

        public virtual void X_AcctID_FieldDefaulting<DAC>(PXCache cache,
                                                         PXFieldDefaultingEventArgs e,
                                                         FSSrvOrdType fsSrvOrdTypeRow,
                                                         FSServiceOrder fsServiceOrderRow)
            where DAC : class, IBqlTable, IFSSODetBase, new()
        {
            if (e.Row == null || fsSrvOrdTypeRow == null || fsServiceOrderRow == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            if (IsInventoryLine(row.LineType) == false)
            {
                e.NewValue = null;
            }
            else
            {
                e.NewValue = Get_TranAcctID_DefaultValue(cache.Graph, fsSrvOrdTypeRow.SalesAcctSource, row.InventoryID, row.SiteID, fsServiceOrderRow);
            }
        }

        public virtual void X_SubID_FieldDefaulting<DAC>(PXCache cache,
                                                        PXFieldDefaultingEventArgs e,
                                                        FSSrvOrdType fsSrvOrdTypeRow,
                                                        FSServiceOrder fsServiceOrderRow)
            where DAC : class, IBqlTable, IFSSODetBase, new()
        {
            if (e.Row == null || fsSrvOrdTypeRow == null || fsServiceOrderRow == null)
            {
                return;
            }

            var row = (DAC)e.Row;

            if (row.AcctID == null)
            {
                return;
            }

            e.NewValue = Get_IFSSODetBase_SubID_DefaultValue(cache, row, fsServiceOrderRow, fsSrvOrdTypeRow);
        }

        public virtual void X_UOM_FieldDefaulting<DAC>(PXCache cache, PXFieldDefaultingEventArgs e)
            where DAC : class, IBqlTable, IFSSODetBase, new()
        {
            if (e.Row == null)
            {
                return;
            }

            IFSSODetBase fsSODetBaseRow = (IFSSODetBase)e.Row;

            string returnUOM = ((CommonSetup)PXSelect<CommonSetup>.Select(cache.Graph))?.WeightUOM;

            if (fsSODetBaseRow.InventoryID != null)
            {
                returnUOM = InventoryItem.PK.Find(cache.Graph, fsSODetBaseRow.InventoryID)?.SalesUnit;
            }

            e.NewValue = returnUOM;
        }

        /// <summary>
        /// If the given line is prepaid then disable all its editable fields.
        /// </summary>
        /// <param name="cacheAppointmentDet">Cache of the Appointment Detail.</param>
        /// <param name="fsAppointmentDetRow">Appointment Detail row.</param>
        public virtual void DisablePrepaidLine(PXCache cacheAppointmentDet, FSAppointmentDet fsAppointmentDetRow)
        {
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.tranDesc>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.lineType>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.inventoryID>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.billingRule>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.isFree>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.actualQty>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.estimatedQty>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.curyUnitPrice>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.projectTaskID>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.siteID>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.siteLocationID>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.acctID>(cacheAppointmentDet, fsAppointmentDetRow, false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.subID>(cacheAppointmentDet, fsAppointmentDetRow, false);
        }

        public virtual void FSAppointmentDet_RowSelected_PartialHandler(PXCache cacheAppointmentDet,
                                                                       FSAppointmentDet fsAppointmentDetRow,
                                                                       FSSetup fsSetupRow,
                                                                       FSSrvOrdType fsSrvOrdTypeRow,
                                                                       FSServiceOrder fsServiceOrderRow,
                                                                       FSAppointment fsAppointmentRow = null)
        {
            // @TODO: AC-142850 Evaluate if usage of deprecated function can be avoided already
            EnableDisable_LineType(cacheAppointmentDet, fsAppointmentDetRow, fsSetupRow, fsAppointmentRow, fsSrvOrdTypeRow);

            if (fsAppointmentDetRow.IsPrepaid == true)
            {
                DisablePrepaidLine(cacheAppointmentDet, fsAppointmentDetRow);
            }
            else
            {
                EnableDisable_Acct_Sub(cacheAppointmentDet, fsAppointmentDetRow, fsSrvOrdTypeRow, fsServiceOrderRow);
            }
        }

        public virtual void FSAppointmentDet_RowPersisting_PartialHandler(PXCache cacheAppointmentDet,
                                                                         FSAppointmentDet fsAppointmentDetRow,
                                                                         FSAppointment fsAppointmentRow,
                                                                         FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsAppointmentDetRow.LineType == ID.LineType_ServiceTemplate.INVENTORY_ITEM
                && fsSrvOrdTypeRow.PostTo == ID.SourceType_ServiceOrder.SALES_ORDER
                && fsAppointmentDetRow.LastModifiedByScreenID != ID.ScreenID.GENERATE_SERVICE_CONTRACT_APPOINTMENT
                && fsAppointmentDetRow.SiteID == null)
            {
                cacheAppointmentDet.RaiseExceptionHandling<FSAppointmentDet.siteID>(fsAppointmentDetRow, null, new PXSetPropertyException(TX.Error.DATA_REQUIRED_FOR_LINE_TYPE, PXErrorLevel.Error));
            }

            ValidateQty(cacheAppointmentDet, fsAppointmentDetRow);
        }

        public virtual void RefreshSalesPricesInTheWholeDocument(PXSelectBase<FSAppointmentDet> appointmentDetails)
        {
            // TODO: AC-142850
            // This method should run and depend on BillCustomerID changes, and not on CustomerID changes
            // Besides that, check if this is necessary using the Sales-Price graph extension
            foreach (FSAppointmentDet row in appointmentDetails.Select())
            {
                appointmentDetails.Cache.SetDefaultExt<FSAppointmentDet.curyUnitPrice>(row);
                appointmentDetails.Cache.Update(row);
            }
        }

        public virtual bool ShouldShowMarkForPOFields(SOOrderType currentSOOrderType)
        {
            return currentSOOrderType?.RequireShipping ?? false;
        }

        public virtual void OpenEmployeeBoard_Handler(PXGraph graph,
                                                     FSServiceOrder fsServiceOrder)
        {
            if (fsServiceOrder == null)
            {
                return;
            }

            if (fsServiceOrder.OpenDoc == false)
            {
                throw new PXException(TX.Error.INVALID_ACTION_FOR_CURRENT_SERVICE_ORDER_STATUS);
            }

            graph.GetSaveAction().Press();

            PXResultset<FSSODet> bqlResultSet_SODet = new PXResultset<FSSODet>();

            GetPendingLines(graph, (int?)fsServiceOrder.SOID, ref bqlResultSet_SODet);

            if (bqlResultSet_SODet.Count > 0)
            {
                throw new PXRedirectToBoardRequiredException(Paths.ScreenPaths.MULTI_EMPLOYEE_DISPATCH, GetServiceOrderUrlArguments(fsServiceOrder));
            }
            else
            {
                throw new PXException(TX.Error.CURRENT_DOCUMENT_NOT_SERVICES_TO_SCHEDULE);
            }
        }

        /// <summary>
        /// Returns the url arguments for a Service Order [fsServiceOrderRow].
        /// </summary>
        public virtual KeyValuePair<string, string>[] GetServiceOrderUrlArguments(FSServiceOrder fsServiceOrderRow)
        {
            KeyValuePair<string, string>[] urlArgs = new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>(typeof(FSServiceOrder.refNbr).Name, fsServiceOrderRow.RefNbr),
                new KeyValuePair<string, string>("Date", fsServiceOrderRow.OrderDate.Value.ToString())
            };

            return urlArgs;
        }

        public virtual void SetServiceOrderRecord_AsUpdated_IfItsNotchanged(PXCache cacheServiceOrder, FSServiceOrder fsServiceOrderRow)
        {
            if (cacheServiceOrder.GetStatus(fsServiceOrderRow) == PXEntryStatus.Notchanged)
            {
                cacheServiceOrder.SetStatus(fsServiceOrderRow, PXEntryStatus.Updated);
            }
        }

        public virtual void DeleteServiceOrder(FSServiceOrder fsServiceOrderRow, ServiceOrderEntry graphServiceOrderEntry)
        {
            graphServiceOrderEntry.Clear();

            graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords
                                    .Search<FSServiceOrder.refNbr>(fsServiceOrderRow.RefNbr, fsServiceOrderRow.SrvOrdType);

            graphServiceOrderEntry.Delete.Press();
        }

        public virtual PXResultset<FSAppointment> GetEditableAppointments(PXGraph graph, int? sOID, int? appointmentID)
        {
            return PXSelect<FSAppointment,
                   Where2<
                        Where<
                            FSAppointment.notStarted, Equal<True>>,
                        And<FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>,
                        And<FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>>>>>
                   .Select(graph, sOID, appointmentID);
        }

        /// <summary>
        /// Enable / Disable the document depending of the Status of the Appointment [fsAppointmentRow] and ServiceOrder [fsServiceOrderRow].
        /// </summary>
        public virtual void EnableDisable_Document(PXGraph graph,
                                                   PXCache cacheServiceOrder,
                                                   FSServiceOrder fsServiceOrderRow,
                                                   FSAppointment fsAppointmentRow,
                                                   FSSrvOrdType fsSrvOrdTypeRow,
                                                   FSBillingCycle fsBillingCycleRow,
                                                   int appointmentCount,
                                                   int detailsCount,
                                                   PXCache cacheServiceOrderDetails,
                                                   PXCache cacheServiceOrderAppointments,
                                                   PXCache cacheServiceOrderEquipment,
                                                   PXCache cacheServiceOrderEmployees,
                                                   PXCache cacheServiceOrder_Contact,
                                                   PXCache cacheServiceOrder_Address,
                                                   bool? isBeingCalledFromQuickProcess,
                                                   bool allowCustomerChange = false)
        {
            bool enableDetailsTab = true;

            if (fsServiceOrderRow != null
                && fsSrvOrdTypeRow != null)
            {
                if (fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
                {
                    enableDetailsTab = fsServiceOrderRow.CustomerID != null;
                }
            }

            bool enableDelete;
            bool enableInsertUpdate;
            bool isQuote = fsServiceOrderRow.Quote == true;

            if (fsAppointmentRow != null)
            {
                enableInsertUpdate = CanUpdateAppointment(fsAppointmentRow, fsSrvOrdTypeRow);
                enableDelete = CanDeleteAppointment(fsAppointmentRow, fsServiceOrderRow, fsSrvOrdTypeRow);
            }
            else
            {
                enableDelete = CanDeleteServiceOrder(graph, fsServiceOrderRow);
                enableInsertUpdate = CanUpdateServiceOrder(fsServiceOrderRow, fsSrvOrdTypeRow);
            }

            //Enable/Disable all view buttons
            cacheServiceOrder.AllowInsert = true;
            cacheServiceOrder.AllowUpdate = enableInsertUpdate || allowCustomerChange || (isBeingCalledFromQuickProcess ?? false);

            if (fsServiceOrderRow.Canceled == true)
            {
                cacheServiceOrder.AllowUpdate = false;
            }

            if (fsServiceOrderRow.Completed == true)
            {
                cacheServiceOrder.AllowUpdate = fsSrvOrdTypeRow.Active == true;
            }

            cacheServiceOrder.AllowDelete = enableDelete;

            if (cacheServiceOrderDetails != null)
            {
                cacheServiceOrderDetails.AllowInsert = enableInsertUpdate && enableDetailsTab && fsServiceOrderRow.AllowInvoice == false;
                cacheServiceOrderDetails.AllowUpdate = enableInsertUpdate && enableDetailsTab;
                cacheServiceOrderDetails.AllowDelete = enableInsertUpdate && enableDetailsTab && fsServiceOrderRow.AllowInvoice == false;
            }

            if (graph is ServiceOrderEntry)
            {
                var SplitCache = ((ServiceOrderEntry)graph).Splits.Cache;

                if (SplitCache != null)
                {
                    SplitCache.AllowInsert = enableInsertUpdate && enableDetailsTab && fsServiceOrderRow.AllowInvoice == false;
                    SplitCache.AllowUpdate = enableInsertUpdate && enableDetailsTab;
                    SplitCache.AllowDelete = enableInsertUpdate && enableDetailsTab && fsServiceOrderRow.AllowInvoice == false;
                }
            }

            if (cacheServiceOrder_Contact != null)
            {
                cacheServiceOrder_Contact.AllowInsert = enableInsertUpdate && fsServiceOrderRow.AllowInvoice == false;
                cacheServiceOrder_Contact.AllowUpdate = enableInsertUpdate && fsServiceOrderRow.AllowInvoice == false;
                cacheServiceOrder_Contact.AllowDelete = enableInsertUpdate && fsServiceOrderRow.AllowInvoice == false;
            }

            if (cacheServiceOrder_Address != null)
            {
                cacheServiceOrder_Address.AllowInsert = enableInsertUpdate && fsServiceOrderRow.AllowInvoice == false;
                cacheServiceOrder_Address.AllowUpdate = enableInsertUpdate && fsServiceOrderRow.AllowInvoice == false;
                cacheServiceOrder_Address.AllowDelete = enableInsertUpdate && fsServiceOrderRow.AllowInvoice == false;
            }

            if (cacheServiceOrderAppointments != null)
            {
                cacheServiceOrderAppointments.AllowInsert = enableInsertUpdate;
                cacheServiceOrderAppointments.AllowUpdate = enableInsertUpdate;
                cacheServiceOrderAppointments.AllowDelete = enableInsertUpdate;
            }

            if (cacheServiceOrderEquipment != null)
            {
                cacheServiceOrderEquipment.AllowSelect = !isQuote;
                cacheServiceOrderEquipment.AllowInsert = enableInsertUpdate && !isQuote;
                cacheServiceOrderEquipment.AllowUpdate = enableInsertUpdate && !isQuote;
                cacheServiceOrderEquipment.AllowDelete = enableInsertUpdate && !isQuote;
            }

            if (cacheServiceOrderEmployees != null)
            {
                cacheServiceOrderEmployees.AllowSelect = !isQuote;
                cacheServiceOrderEmployees.AllowInsert = enableInsertUpdate && !isQuote;
                cacheServiceOrderEmployees.AllowUpdate = enableInsertUpdate && !isQuote;
                cacheServiceOrderEmployees.AllowDelete = enableInsertUpdate && !isQuote;
            }

            bool customerRequired = (bool)fsServiceOrderRow.BAccountRequired;
            bool contactRequired = (bool)fsSrvOrdTypeRow.RequireContact;
            bool internalSOType = fsSrvOrdTypeRow.Behavior == FSSrvOrdType.behavior.Values.InternalAppointment;
            bool enableServiceContractFields = fsServiceOrderRow.BillingBy == ID.Billing_By.SERVICE_ORDER
                                                    && (PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>()
                                                        || PXAccess.FeatureInstalled<FeaturesSet.routeManagementModule>());

            bool isEnabledCustomerID = AllowEnableCustomerID(fsServiceOrderRow);

            PXUIFieldAttribute.SetEnabled<FSServiceOrder.customerID>(cacheServiceOrder,
                                                                        fsServiceOrderRow,
                                                                        customerRequired && isEnabledCustomerID);

            PXUIFieldAttribute.SetRequired<FSServiceOrder.contactID>(cacheServiceOrder, customerRequired && contactRequired);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.locationID>(cacheServiceOrder, fsServiceOrderRow, customerRequired);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.billCustomerID>(cacheServiceOrder, fsServiceOrderRow, customerRequired);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.billLocationID>(cacheServiceOrder, fsServiceOrderRow, customerRequired);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.billServiceContractID>(cacheServiceOrder, fsServiceOrderRow, enableServiceContractFields
                                                                                                                        && fsServiceOrderRow.AllowInvoice == false);
            PXUIFieldAttribute.SetVisible<FSServiceOrder.billServiceContractID>(cacheServiceOrder, fsServiceOrderRow, enableServiceContractFields);
            PXUIFieldAttribute.SetVisible<FSServiceOrder.billContractPeriodID>(cacheServiceOrder, fsServiceOrderRow, enableServiceContractFields && fsServiceOrderRow.BillServiceContractID != null);

            PXDefaultAttribute.SetPersistingCheck<FSServiceOrder.customerID>(cacheServiceOrder,
                                                                                fsServiceOrderRow,
                                                                                customerRequired && isEnabledCustomerID ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

            PXDefaultAttribute.SetPersistingCheck<FSServiceOrder.contactID>(cacheServiceOrder, fsServiceOrderRow, customerRequired && contactRequired ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
            PXDefaultAttribute.SetPersistingCheck<FSServiceOrder.locationID>(cacheServiceOrder, fsServiceOrderRow, customerRequired ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

            EnableDisable_SLAETA(cacheServiceOrder, fsServiceOrderRow);

            bool noClosedOrCompletedAppointments = true;

            if (fsServiceOrderRow.AppointmentsCompletedOrClosedCntr > 0)
            {
                noClosedOrCompletedAppointments = false;
            }

            // Editing an Appointment, if there is more than 1 Appointment for the ServiceOrder, the fields that affect other Appointments are disabled.
            bool unrestrictedAppointmentEdition = fsAppointmentRow == null || appointmentCount <= 1;

            bool enableAllowInvoice = fsServiceOrderRow.BillingBy == ID.Billing_By.SERVICE_ORDER
                                        && fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment
                                        && fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.Quote
                                        && fsServiceOrderRow.IsBilledOrClosed == false
                                        && (fsServiceOrderRow.Status == FSServiceOrder.status.Values.Open
                                            || fsServiceOrderRow.Status == FSServiceOrder.status.Values.Completed);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.allowInvoice>(cacheServiceOrder, fsServiceOrderRow, enableAllowInvoice);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.customerID>(cacheServiceOrder, fsServiceOrderRow, cacheServiceOrder.GetStatus(fsServiceOrderRow) == PXEntryStatus.Inserted && customerRequired && detailsCount == 0);

            PXUIFieldAttribute.SetEnabled<FSServiceOrder.billCustomerID>(cacheServiceOrder, fsServiceOrderRow, noClosedOrCompletedAppointments && unrestrictedAppointmentEdition && !internalSOType);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.billLocationID>(cacheServiceOrder, fsServiceOrderRow, noClosedOrCompletedAppointments && unrestrictedAppointmentEdition && !internalSOType);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.branchID>(cacheServiceOrder, fsServiceOrderRow, noClosedOrCompletedAppointments && unrestrictedAppointmentEdition);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.branchLocationID>(cacheServiceOrder, fsServiceOrderRow, noClosedOrCompletedAppointments && unrestrictedAppointmentEdition);
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.projectID>(cacheServiceOrder, fsServiceOrderRow, noClosedOrCompletedAppointments && unrestrictedAppointmentEdition);
            PXUIFieldAttribute.SetVisible<FSServiceOrder.dfltProjectTaskID>(cacheServiceOrder, fsServiceOrderRow, !ProjectDefaultAttribute.IsNonProject(fsServiceOrderRow.ProjectID));
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.dfltProjectTaskID>(cacheServiceOrder, fsServiceOrderRow, noClosedOrCompletedAppointments && unrestrictedAppointmentEdition && !ProjectDefaultAttribute.IsNonProject(fsServiceOrderRow.ProjectID));
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.roomID>(cacheServiceOrder, fsServiceOrderRow, noClosedOrCompletedAppointments && unrestrictedAppointmentEdition);

            PXUIFieldAttribute.SetRequired<FSServiceOrder.contactID>(cacheServiceOrder, noClosedOrCompletedAppointments && customerRequired && contactRequired);

            PXUIFieldAttribute.SetEnabled<FSServiceOrder.taxZoneID>(cacheServiceOrder, fsServiceOrderRow, fsServiceOrderRow.AllowInvoice == false);
        }

        public virtual bool AreThereAnyItemsForPO(ServiceOrderEntry graph, FSServiceOrder fsServiceOrderRow)
        {
            if (fsServiceOrderRow == null)
            {
                return false;
            }

            return fsServiceOrderRow.PendingPOLineCntr > 0;
        }

        public virtual void EnableDisable_Acct_Sub(PXCache cache, IFSSODetBase fsSODetRow, FSSrvOrdType fsSrvOrdTypeRow, FSServiceOrder fsServiceOrderRow)
        {
            bool enableAcctSub = fsSrvOrdTypeRow != null && fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment
                                    && fsServiceOrderRow != null && fsServiceOrderRow.Quote == false
                                        && (fsSODetRow.LineType == ID.LineType_ServiceTemplate.NONSTOCKITEM
                                            || fsSODetRow.LineType == ID.LineType_ServiceTemplate.SERVICE
                                                || fsSODetRow.LineType == ID.LineType_ServiceTemplate.INVENTORY_ITEM);

            if (fsSODetRow is FSSODet)
            {
                PXUIFieldAttribute.SetEnabled<FSSODet.acctID>(cache, fsSODetRow, enableAcctSub);
                PXUIFieldAttribute.SetEnabled<FSSODet.subID>(cache, fsSODetRow, enableAcctSub);
            }
            else if (fsSODetRow is FSAppointmentDet)
            {
                PXUIFieldAttribute.SetEnabled<FSAppointmentDet.acctID>(cache, fsSODetRow, enableAcctSub);
                PXUIFieldAttribute.SetEnabled<FSAppointmentDet.subID>(cache, fsSODetRow, enableAcctSub);
            }

            if (enableAcctSub == false)
            {
                if (fsSODetRow is FSSODet)
                {
                    cache.SetValueExt<FSSODet.acctID>(fsSODetRow, null);
                    cache.SetValueExt<FSSODet.subID>(fsSODetRow, null);
                }
                else if (fsSODetRow is FSAppointmentDet)
                {
                    cache.SetValueExt<FSAppointmentDet.acctID>(fsSODetRow, null);
                    cache.SetValueExt<FSAppointmentDet.subID>(fsSODetRow, null);
                }
            }
        }

        /// <summary>
        /// Returns true if a Service order [fsServiceOrderRow] can be updated based on its status and its SrvOrdtype's status.
        /// </summary>
        public virtual bool CanUpdateServiceOrder(FSServiceOrder fsServiceOrderRow, FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsServiceOrderRow == null
                    || fsSrvOrdTypeRow == null)
            {
                return false;
            }

            if ((fsServiceOrderRow.Closed == true
                        || fsServiceOrderRow.Canceled == true)
                        || fsSrvOrdTypeRow.Active == false)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if a Service order [fsServiceOrderRow] can be deleted based based in its status.
        /// </summary>
        public virtual bool CanDeleteServiceOrder(PXGraph graph, FSServiceOrder fsServiceOrderRow)
        {
            if (fsServiceOrderRow == null
                    || fsServiceOrderRow.Mem_Invoiced == true
                    || fsServiceOrderRow.AllowInvoice == true
                    || (fsServiceOrderRow.OpenDoc == false
                        && fsServiceOrderRow.Hold == false
                        && fsServiceOrderRow.Quote == false))
            {
                return false;
            }

            if (fsServiceOrderRow.AppointmentsCompletedCntr > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if a Service order [fsServiceOrderRow] has an appointment assigned.
        /// </summary>
        public virtual bool ServiceOrderHasAppointment(PXGraph graph, FSServiceOrder fsServiceOrderRow)
        {
            PXResultset<FSAppointment> fsAppointmentSet = PXSelect<FSAppointment,
                                                          Where<
                                                              FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>
                                                          .Select(graph, fsServiceOrderRow.SOID);

            if (fsAppointmentSet.Count == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if a Service in the Service Order <c>fsSODetServiceRow</c> is linked with an appointment.
        /// </summary>
        public virtual bool FSSODetLinkedToAppointments(PXGraph graph, FSSODet fsSODetRow)
        {
            PXResultset<FSAppointmentDet> fsAppointmentDetSet = PXSelect<FSAppointmentDet,
                                                                Where<
                                                                    FSAppointmentDet.sODetID, Equal<Required<FSSODet.sODetID>>>>
                                                                .Select(graph, fsSODetRow.SODetID);
            return fsAppointmentDetSet.Count > 0;
        }

        public virtual void EnableDisable_SLAETA(PXCache cacheServiceOrder, FSServiceOrder fsServiceOrderRow)
        {
            PXUIFieldAttribute.SetEnabled<FSServiceOrder.sLAETA>(cacheServiceOrder, fsServiceOrderRow, fsServiceOrderRow.SourceType != ID.SourceType_ServiceOrder.CASE);
        }

        public virtual int? GetDefaultLocationID(PXGraph graph, int? bAccountID)
        {
            return ServiceOrderEntry.GetDefaultLocationIDInt(graph, bAccountID);
        }

        public virtual PXResultset<FSCustomerBillingSetup> GetBillingInformationByCustomer(PXGraph graph, int? bAccountID, string srvOrdType)
        {
            if (graph == null || bAccountID == null || srvOrdType == null)
            {
                return null;
            }

            var fsCustomerBillingSetupRow = PXSelectJoin<FSCustomerBillingSetup,
                                            LeftJoin<FSBillingCycle,
                                                On<FSBillingCycle.billingCycleID, Equal<FSCustomerBillingSetup.billingCycleID>>,
                                            CrossJoinSingleTable<FSSetup>>,
                                            Where2<
                                                Where<
                                                    FSCustomerBillingSetup.customerID, Equal<Required<FSCustomerBillingSetup.customerID>>>,
                                                And<
                                                    Where2<
                                                        Where<
                                                            FSSetup.customerMultipleBillingOptions, Equal<False>,
                                                            And<FSCustomerBillingSetup.srvOrdType, IsNull,
                                                            And<FSCustomerBillingSetup.active, Equal<True>>>>,
                                                        Or<
                                                            Where<
                                                                FSSetup.customerMultipleBillingOptions, Equal<True>,
                                                                And<FSCustomerBillingSetup.srvOrdType, Equal<Required<FSServiceOrder.srvOrdType>>,
                                                                And<FSCustomerBillingSetup.active, Equal<True>>>>>>>>>
                                            .Select(graph, bAccountID, srvOrdType);
            return fsCustomerBillingSetupRow;
        }

        public virtual DateTime? GetCutOffDate(PXGraph graph, int? CBID, DateTime? docDate, string srvOrdType)
        {
            return ServiceOrderEntry.GetCutOffDateInt(graph, CBID, docDate, srvOrdType);
        }

        public virtual void SetDocDesc(PXGraph graph, FSServiceOrder fsServiceOrderRow)
        {
            FSSODet fsSODetRow = PXSelect<FSSODet,
                                 Where<
                                     FSSODet.sOID, Equal<Required<FSSODet.sOID>>>,
                                 OrderBy<
                                     Asc<FSSODet.sODetID>>>
                                 .Select(graph, fsServiceOrderRow.SOID);

            if (fsSODetRow != null)
            {
                fsServiceOrderRow.DocDesc = fsSODetRow.TranDesc;
            }
        }

        public virtual void SetBillCustomerAndLocationID(PXCache cache, FSServiceOrder fsServiceOrderRow)
        {
            BAccount bAccountRow = PXSelect<BAccount,
                                   Where<
                                       BAccount.bAccountID, Equal<Required<FSServiceOrder.customerID>>>>
                                   .Select(cache.Graph, fsServiceOrderRow.CustomerID);

            int? billCustomerID = null;
            int? billLocationID = null;

            if (bAccountRow == null || bAccountRow.Type != BAccountType.ProspectType)
            {
                Customer customerRow = SharedFunctions.GetCustomerRow(cache.Graph, fsServiceOrderRow.CustomerID);
                FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(customerRow);

                switch (fsxCustomerRow.DefaultBillingCustomerSource)
                {
                    case ID.Default_Billing_Customer_Source.SERVICE_ORDER_CUSTOMER:
                        billCustomerID = fsServiceOrderRow.CustomerID;
                        billLocationID = fsServiceOrderRow.LocationID;
                        break;

                    case ID.Default_Billing_Customer_Source.DEFAULT_CUSTOMER:
                        billCustomerID = fsServiceOrderRow.CustomerID;
                        billLocationID = GetDefaultLocationID(cache.Graph, fsServiceOrderRow.CustomerID);
                        break;

                    case ID.Default_Billing_Customer_Source.SPECIFIC_CUSTOMER:
                        billCustomerID = fsxCustomerRow.BillCustomerID;
                        billLocationID = fsxCustomerRow.BillLocationID;
                        break;
                }
            }

            cache.SetValueExtIfDifferent<FSServiceOrder.billCustomerID>(fsServiceOrderRow, billCustomerID);
            cache.SetValueExtIfDifferent<FSServiceOrder.billLocationID>(fsServiceOrderRow, billLocationID);
        }

        public virtual bool AllowEnableCustomerID(FSServiceOrder fsServiceOrderRow)
        {
            if (fsServiceOrderRow == null)
            {
                return false;
            }

            return fsServiceOrderRow.SourceType == ID.SourceType_ServiceOrder.SERVICE_DISPATCH;
        }

        /// <summary>
        /// Search for <c>FSSODet</c> lines that are NOT in Status "Canceled" and return those rows in <c>bqlResultSet_FSSODet</c>.
        /// </summary>
        public virtual void GetPendingLines(PXGraph graph, int? sOID, ref PXResultset<FSSODet> bqlResultSet_FSSODet)
        {
            bqlResultSet_FSSODet = PXSelect<FSSODet,
                                   Where<
                                        FSSODet.sOID, Equal<Required<FSSODet.sOID>>,
                                   And<
                                        FSSODet.status, Equal<FSSODet.status.ScheduleNeeded>>>,
                                   OrderBy<
                                        Asc<FSSODet.sortOrder>>>
                                   .Select(graph, sOID);
        }

        public virtual bool CheckCustomerChange(PXCache cacheServiceOrder,
                                               FSServiceOrder fsServiceOrderRow,
                                               int? oldCustomerID,
                                               PXResultset<FSAppointment> bqlResultSet)
        {
            if (fsServiceOrderRow.OpenDoc == false
                    && fsServiceOrderRow.Hold == false
                    && fsServiceOrderRow.Quote == false)
            {
                fsServiceOrderRow.CustomerID = oldCustomerID;

                cacheServiceOrder.RaiseExceptionHandling<FSServiceOrder.customerID>(fsServiceOrderRow,
                                                                                    oldCustomerID,
                                                                                    new PXSetPropertyException(TX.Error.CUSTOMER_CHANGE_NOT_ALLOWED_SO_STATUS, PXErrorLevel.Warning));

                return false;
            }

            foreach (FSAppointment fsAppointmentRow in bqlResultSet)
            {
                if (fsAppointmentRow == null ? false : fsAppointmentRow.NotStarted == false)
                {
                    fsServiceOrderRow.CustomerID = (int?)oldCustomerID;

                    cacheServiceOrder.RaiseExceptionHandling<FSServiceOrder.customerID>(fsServiceOrderRow,
                                                                                        oldCustomerID,
                                                                                        new PXSetPropertyException(TX.Error.CUSTOMER_CHANGE_NOT_ALLOWED_APP_STATUS, PXErrorLevel.Warning));

                    return false;
                }
            }

            return true;
        }

        public virtual void _EnableDisableActionButtons(PXAction<FSServiceOrder>[] pxActions, bool enable)
        {
            foreach (PXAction<FSServiceOrder> pxAction in pxActions)
            {
                    pxAction.SetEnabled(enable);
                }
            }

        public virtual FSServiceOrder CreateServiceOrderCleanCopy(FSServiceOrder fsServiceOrderRow)
        {
            FSServiceOrder fsServiceOrderRow_Copy = PXCache<FSServiceOrder>.CreateCopy(fsServiceOrderRow);

            // Key fields are cleared to prevent bad references and calculations
            fsServiceOrderRow_Copy.SrvOrdType = null;
            fsServiceOrderRow_Copy.RefNbr = null;
            fsServiceOrderRow_Copy.SOID = null;

            fsServiceOrderRow_Copy.NoteID = null;
            fsServiceOrderRow_Copy.CuryInfoID = null;

            fsServiceOrderRow_Copy.BranchID = null;
            fsServiceOrderRow_Copy.BranchLocationID = null;
            fsServiceOrderRow_Copy.LocationID = null;
            fsServiceOrderRow_Copy.ContactID = null;
            fsServiceOrderRow_Copy.Status = null;

            fsServiceOrderRow_Copy.ProjectID = null;
            fsServiceOrderRow_Copy.DfltProjectTaskID = null;

            fsServiceOrderRow_Copy.AllowOverrideContactAddress = false;
            fsServiceOrderRow_Copy.ServiceOrderContactID = null;
            fsServiceOrderRow_Copy.ServiceOrderAddressID = null;

            fsServiceOrderRow_Copy.AllowInvoice = false;
            fsServiceOrderRow_Copy.Quote = false;
            fsServiceOrderRow_Copy.OpenDoc = false;
            fsServiceOrderRow_Copy.Hold = false;
            fsServiceOrderRow_Copy.Billed = false;
            fsServiceOrderRow_Copy.Awaiting = false;
            fsServiceOrderRow_Copy.Completed = false;
            fsServiceOrderRow_Copy.Canceled = false;
            fsServiceOrderRow_Copy.Copied = false;
            fsServiceOrderRow_Copy.Confirmed = false;
            fsServiceOrderRow_Copy.WorkflowTypeID = null;

            //Clean total fields
            fsServiceOrderRow_Copy.EstimatedDurationTotal = 0;
            fsServiceOrderRow_Copy.ApptDurationTotal = 0;

            fsServiceOrderRow_Copy.CuryEstimatedOrderTotal = 0;
            fsServiceOrderRow_Copy.CuryApptOrderTotal = 0;
            fsServiceOrderRow_Copy.CuryBillableOrderTotal = 0;

            fsServiceOrderRow_Copy.EstimatedOrderTotal = 0;
            fsServiceOrderRow_Copy.ApptOrderTotal = 0;
            fsServiceOrderRow_Copy.BillableOrderTotal = 0;

            fsServiceOrderRow_Copy.LineCntr = 0;
            fsServiceOrderRow_Copy.PendingPOLineCntr = 0;
            fsServiceOrderRow_Copy.SplitLineCntr = 0;

            return fsServiceOrderRow_Copy;
        }

        public virtual int? Get_TranAcctID_DefaultValue(PXGraph graph, string salesAcctSource, int? inventoryID, int? siteID, FSServiceOrder fsServiceOrderRow)
        {
            return ServiceOrderEntry.Get_TranAcctID_DefaultValueInt(graph, salesAcctSource, inventoryID, siteID, fsServiceOrderRow);
        }

        public virtual object Get_IFSSODetBase_SubID_DefaultValue(PXCache cache, IFSSODetBase fsSODetRow, FSServiceOrder fsServiceOrderRow, FSSrvOrdType fsSrvOrdTypeRow, FSAppointment fsAppointmentRow = null)
        {
            int? inventoryID = fsSODetRow.IsService ? fsSODetRow.InventoryID : fsSODetRow.InventoryID;
            int? salesPersonID = fsAppointmentRow == null ? fsServiceOrderRow.SalesPersonID : fsAppointmentRow.SalesPersonID;

            SharedClasses.SubAccountIDTupla subAcctIDs = SharedFunctions.GetSubAccountIDs(cache.Graph,
                                                                                          fsSrvOrdTypeRow,
                                                                                          inventoryID,
                                                                                          fsServiceOrderRow.BranchID,
                                                                                          fsServiceOrderRow.LocationID,
                                                                                          fsServiceOrderRow.BranchLocationID,
                                                                                          salesPersonID,
                                                                                          fsSODetRow.IsService);

            if (subAcctIDs == null)
            {
                return null;
            }

            object value = null;

            try
            {
                value = SubAccountMaskAttribute.MakeSub<FSSrvOrdType.combineSubFrom>(
                            cache.Graph,
                            fsSrvOrdTypeRow.CombineSubFrom,
                            new object[] { subAcctIDs.branchLocation_SubID, subAcctIDs.branch_SubID, subAcctIDs.inventoryItem_SubID, subAcctIDs.customerLocation_SubID, subAcctIDs.postingClass_SubID, subAcctIDs.salesPerson_SubID, subAcctIDs.srvOrdType_SubID, subAcctIDs.warehouse_SubID },
                            new Type[] { typeof(FSBranchLocation.subID), typeof(Location.cMPSalesSubID), typeof(InventoryItem.salesSubID), typeof(Location.cSalesSubID), typeof(INPostClass.salesSubID), typeof(SalesPerson.salesSubID), typeof(FSSrvOrdType.subID), fsSODetRow.IsService ? typeof(INSite.salesSubID) : typeof(InventoryItem.salesSubID) });

                if (fsSODetRow is FSSODet)
                {
                    cache.RaiseFieldUpdating<FSSODet.subID>(fsSODetRow, ref value);
                }

                if (fsSODetRow is FSAppointmentDet)
                {
                    cache.RaiseFieldUpdating<FSAppointmentDet.subID>(fsSODetRow, ref value);
                }
            }
            catch (PXMaskArgumentException ex)
            {
                if (fsSODetRow is FSSODet)
                {
                    cache.RaiseExceptionHandling<FSSODet.subID>(fsSODetRow, null, new PXSetPropertyException(ex.Message));
                }

                if (fsSODetRow is FSAppointmentDet)
                {
                    cache.RaiseExceptionHandling<FSAppointmentDet.subID>(fsSODetRow, null, new PXSetPropertyException(ex.Message));
                }

                value = null;
            }
            catch (PXSetPropertyException ex)
            {
                if (fsSODetRow is FSSODet)
                {
                    cache.RaiseExceptionHandling<FSSODet.subID>(fsSODetRow, value, ex);
                }

                if (fsSODetRow is FSAppointmentDet)
                {
                    cache.RaiseExceptionHandling<FSAppointmentDet.subID>(fsSODetRow, value, ex);
                }

                value = null;
            }

            return value;
        }

        public virtual void UpdateServiceCounts(FSServiceOrder fsServiceOrderRow, IEnumerable<FSSODet> serviceDetails)
        {
            fsServiceOrderRow.ServiceCount = 0;
            fsServiceOrderRow.CompleteServiceCount = 0;
            fsServiceOrderRow.ScheduledServiceCount = 0;

            fsServiceOrderRow.ServiceCount = serviceDetails.Where(_ => _.Status != ID.Status_SODet.CANCELED).Count();
            fsServiceOrderRow.CompleteServiceCount = serviceDetails.Where(_ => _.Status == ID.Status_SODet.COMPLETED).Count();
            fsServiceOrderRow.ScheduledServiceCount = serviceDetails.Where(_ => _.Status == ID.Status_SODet.SCHEDULED).Count();
        }

        public virtual void PropagateSODetStatusToAppointmentLines(PXGraph graph, FSSODet fsSODetServiceRow, FSAppointment fsAppointmentRow)
        {
            int? appointmentID = fsAppointmentRow?.AppointmentID;

            PXUpdateJoin<
                Set<FSAppointmentDet.status, Required<FSAppointmentDet.status>>,
            FSAppointmentDet,
            InnerJoin<FSAppointment,
            On<
                FSAppointment.appointmentID, Equal<FSAppointmentDet.appointmentID>>>,
            Where<
                FSAppointmentDet.sODetID, Equal<Required<FSAppointmentDet.sODetID>>,
            And2<
                Where<
                    FSAppointmentDet.status, NotEqual<Required<FSAppointmentDet.status>>,
                And<
                    Where<
                        FSAppointmentDet.appointmentID, NotEqual<Required<FSAppointmentDet.appointmentID>>,
                    Or<
                        Required<FSAppointmentDet.appointmentID>, IsNull>>>>,
                And<
                    Where<
                        FSAppointment.notStarted, Equal<True>,
                    Or<
                        FSAppointment.inProcess, Equal<True>>>>>>>
            .Update(graph, fsSODetServiceRow.Status, fsSODetServiceRow.SODetID, fsSODetServiceRow.Status, appointmentID, appointmentID);
        }

        [Obsolete("Delete this method in the next major release")]
        public virtual void UpdatePendingPostFlags(FSServiceOrder fsServiceOrderRow, PXSelectBase<FSSODet> serviceDetails)
        {
        }

        public virtual void UpdatePendingPostFlags(ServiceOrderEntry graphServiceOrderEntry, FSServiceOrder fsServiceOrderRow, PXSelectBase<FSSODet> serviceDetails)
        {
            int? linesToPost = 0;

            if (fsServiceOrderRow.PostedBy == null)
            {
                foreach (FSSODet det in serviceDetails.Select())
                {
                    if (det.needToBePosted())
                    {
                        if (det.IsBillable == false)
                        {
                            // Currently stock item lines can not be marked as non-billable
                            // because we post directly to IN without generating a billing document
                            // only for pick-up and delivery items
                            continue;
                        }

                        FSPostInfo postInfo = graphServiceOrderEntry.PostInfoDetails.Select().RowCast<FSPostInfo>().Where(x => x.PostID == det.PostID).FirstOrDefault();

                        if (postInfo == null || postInfo.isPosted() == false)
                        {
                            linesToPost++;
                            break;
                        }
                    }
                }

                fsServiceOrderRow.PendingAPARSOPost = linesToPost > 0;
            }
            else
            {
                fsServiceOrderRow.PendingAPARSOPost = false;
            }

            // Currently we post directly to IN without generating a billing document
            // only for pick-up and delivery items
            fsServiceOrderRow.PendingINPost = false;
        }

        public virtual void UpdateWarrantyFlag(PXCache cache, IFSSODetBase fsSODetRow, DateTime? docDate)
        {
            fsSODetRow.Warranty = false;

            if (docDate == null || fsSODetRow.SMEquipmentID == null)
            {
                return;
            }

            if (fsSODetRow.EquipmentAction != ID.Equipment_Action.REPLACING_TARGET_EQUIPMENT
                && fsSODetRow.EquipmentAction != ID.Equipment_Action.REPLACING_COMPONENT
                && fsSODetRow.EquipmentAction != ID.Equipment_Action.NONE
                && fsSODetRow.LineType != ID.LineType_ALL.SERVICE)
            {
                return;
            }

            if (fsSODetRow.EquipmentAction == ID.Equipment_Action.REPLACING_COMPONENT
                && fsSODetRow.EquipmentLineRef == null)
            {
                return;
            }

            FSEquipment equipmentRow = PXSelect<FSEquipment,
                                       Where<
                                           FSEquipment.SMequipmentID, Equal<Required<FSEquipment.SMequipmentID>>>>
                                       .Select(cache.Graph, fsSODetRow.SMEquipmentID);

            if (fsSODetRow.LineType != ID.LineType_ALL.SERVICE
                    && fsSODetRow.LineType != ID.LineType_ALL.NONSTOCKITEM
                        && fsSODetRow.LineType != ID.LineType_ALL.COMMENT
                            && fsSODetRow.LineType != ID.LineType_ALL.INSTRUCTION
                                && fsSODetRow.EquipmentAction != ID.Equipment_Action.NONE)
            {
                InventoryItem inventoryItemRow = InventoryItem.PK.Find(cache.Graph, fsSODetRow.InventoryID);

                FSxEquipmentModel fSxEquipmentModelRow = null;

                if (inventoryItemRow != null)
                {
                    fSxEquipmentModelRow = PXCache<InventoryItem>.GetExtension<FSxEquipmentModel>(inventoryItemRow);
                }

                if (inventoryItemRow == null || fSxEquipmentModelRow == null || (fSxEquipmentModelRow != null && (fSxEquipmentModelRow.EquipmentItemClass == ID.Equipment_Item_Class.MODEL_EQUIPMENT
                                                                                        || fSxEquipmentModelRow.EquipmentItemClass == ID.Equipment_Item_Class.COMPONENT)))
                {
                    UpdateWarrantyFlagByTargetEquipment(cache, fsSODetRow, docDate, equipmentRow);
                }
                else if (fSxEquipmentModelRow.EquipmentItemClass == ID.Equipment_Item_Class.PART_OTHER_INVENTORY)
                {
                    if (equipmentRow.CpnyWarrantyEndDate >= docDate
                                || equipmentRow.VendorWarrantyEndDate >= docDate)
                    {
                        fsSODetRow.Warranty = true;
                    }
                }
            }
            else
            {
                UpdateWarrantyFlagByTargetEquipment(cache, fsSODetRow, docDate, equipmentRow);
            }
        }

        public virtual void UpdateWarrantyFlagByTargetEquipment(PXCache cache, IFSSODetBase fsSODetRow, DateTime? docDate, FSEquipment equipmentRow)
        {
            if (equipmentRow == null)
                return;

            if (fsSODetRow.EquipmentLineRef == null)
            {
                if (equipmentRow.CpnyWarrantyEndDate >= docDate
                     || equipmentRow.VendorWarrantyEndDate >= docDate)
                {
                    fsSODetRow.Warranty = true;
                }
            }
            else
            {
                FSEquipmentComponent fsEquipmentComponentRow = PXSelect<FSEquipmentComponent,
                                                               Where<
                                                                    FSEquipmentComponent.SMequipmentID, Equal<Required<FSEquipmentComponent.SMequipmentID>>,
                                                                    And<FSEquipmentComponent.lineNbr, Equal<Required<FSEquipmentComponent.lineNbr>>>>>
                                                               .Select(cache.Graph, fsSODetRow.SMEquipmentID, fsSODetRow.EquipmentLineRef);

                if (fsEquipmentComponentRow.CpnyWarrantyEndDate != null
                    && fsEquipmentComponentRow.CpnyWarrantyEndDate >= docDate)
                {
                    fsSODetRow.Warranty = true;
                }
                else if (fsEquipmentComponentRow.VendorWarrantyEndDate != null
                            && fsEquipmentComponentRow.VendorWarrantyEndDate >= docDate)
                {
                    fsSODetRow.Warranty = true;
                }
                else if (fsEquipmentComponentRow.CpnyWarrantyEndDate == null
                            && fsEquipmentComponentRow.VendorWarrantyEndDate == null
                            && (equipmentRow.CpnyWarrantyEndDate >= docDate
                                    || equipmentRow.VendorWarrantyEndDate >= docDate))
                {
                    fsSODetRow.Warranty = true;
                }
            }
        }

        public virtual bool AccountIsAProspect(PXGraph graph, int? bAccountID)
        {
            BAccount bAccountRow = PXSelect<BAccount,
                                   Where<
                                       BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>
                                   .Select(graph, bAccountID);

            return bAccountRow != null && bAccountRow.Type == BAccountType.ProspectType;
        }

        public virtual bool CustomerHasBillingCycle(PXGraph graph, FSSetup setupRecordRow, int? customerID, string srvOrdType)
        {
            Customer customerRow = PXSelect<Customer,
                                   Where<
                                       Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                   .Select(graph, customerID);

            if (customerRow != null)
            {
                FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(customerRow);
                if (setupRecordRow != null
                                   && setupRecordRow.CustomerMultipleBillingOptions == true)
                {
                    var multipleBillingOptions = PXSelect<FSCustomerBillingSetup,
                                                 Where<
                                                     FSCustomerBillingSetup.customerID, Equal<Required<FSCustomerBillingSetup.customerID>>,
                                                 And<
                                                     FSCustomerBillingSetup.srvOrdType, Equal<Required<FSCustomerBillingSetup.srvOrdType>>>>>
                                                 .Select(graph, customerID, srvOrdType);

                    return multipleBillingOptions.Count() > 0;
                }
                else
                {
                    return fsxCustomerRow.BillingCycleID != null;
                }
            }

            return false;
        }

        // TODO:
        [Obsolete("This method will be deleted in the next major release. Use the virtual version of each graph instead.")]
        public virtual void ValidateCustomerBillingCycle(PXCache cache, FSSetup setupRecordRow, FSSrvOrdType fsSrvOrdTypeRow, FSServiceOrder fsServiceOrderRow)
        {
            ValidateCustomerBillingCycle<FSServiceOrder.billCustomerID>(cache, fsServiceOrderRow, fsServiceOrderRow.CustomerID, fsSrvOrdTypeRow, setupRecordRow, justWarn: false);
        }

        // TODO:
        [Obsolete("This method will be deleted in the next major release. Use the version with the parm justWarn.")]
        public virtual void ValidateCustomerBillingCycle<TField>(PXCache cache, Object row, int? customerID, FSSrvOrdType fsSrvOrdTypeRow, FSSetup setupRecordRow)
            where TField : IBqlField
        {
            ValidateCustomerBillingCycle<TField>(cache, row, customerID, fsSrvOrdTypeRow, setupRecordRow, justWarn: false);
        }

        public virtual bool ValidateCustomerBillingCycle<TField>(PXCache cache, Object row, int? billCustomerID, FSSrvOrdType fsSrvOrdTypeRow, FSSetup setupRecordRow, bool justWarn)
            where TField : IBqlField
        {
            if (row == null
                    || fsSrvOrdTypeRow == null
                        || fsSrvOrdTypeRow.Behavior == FSSrvOrdType.behavior.Values.Quote
                            || fsSrvOrdTypeRow.Behavior == FSSrvOrdType.behavior.Values.InternalAppointment)
            {
                return true;
            }

            if (AccountIsAProspect(cache.Graph, billCustomerID) == false
                    && CustomerHasBillingCycle(cache.Graph, setupRecordRow, billCustomerID, fsSrvOrdTypeRow.SrvOrdType) == false)
            {
                PXException exception = new PXSetPropertyException(TX.Error.MISSING_CUSTOMER_BILLING_CYCLE, PXErrorLevel.Warning);
                cache.RaiseExceptionHandling<TField>(row, billCustomerID, exception);

                if (justWarn == false)
                {
                    throw exception;
                }

                return false;
            }

            return true;
        }

        public virtual void CreatePrepaymentDocument(FSServiceOrder fsServiceOrderRow, FSAppointment fsAppointmentRow, out PXGraph target, string paymentType = ARPaymentType.Payment)
        {
            ARPaymentEntry graphARPaymentEntry = PXGraph.CreateInstance<ARPaymentEntry>();
            target = graphARPaymentEntry;

            graphARPaymentEntry.Clear();

            ARPayment arPaymentRow = new ARPayment()
            {
                DocType = paymentType,
            };

            AROpenPeriodAttribute.SetThrowErrorExternal<ARPayment.adjFinPeriodID>(graphARPaymentEntry.Document.Cache, true);
            arPaymentRow = PXCache<ARPayment>.CreateCopy(graphARPaymentEntry.Document.Insert(arPaymentRow));
            AROpenPeriodAttribute.SetThrowErrorExternal<ARPayment.adjFinPeriodID>(graphARPaymentEntry.Document.Cache, false);

            arPaymentRow.CustomerID = fsServiceOrderRow.BillCustomerID;
            arPaymentRow.CustomerLocationID = fsServiceOrderRow.BillLocationID;

            decimal CuryDocTotal;

            if (string.Equals(fsServiceOrderRow.CuryID, arPaymentRow.CuryID))
            {
                CuryDocTotal = fsServiceOrderRow.CuryDocTotal ?? 0m;
            }
            else
            {
                CurrencyInfo so_info = PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<ARPayment.curyInfoID>>>>.Select(graphARPaymentEntry, arPaymentRow.CuryInfoID);

                if (graphARPaymentEntry.FindImplementation<IPXCurrencyHelper>() != null)
                {
                    graphARPaymentEntry.FindImplementation<IPXCurrencyHelper>().CuryConvCury((decimal)fsServiceOrderRow.DocTotal, out CuryDocTotal);
                }
                else
                {
                    CM.PXDBCurrencyAttribute.CuryConvCury(graphARPaymentEntry.Document.Cache, so_info, (decimal)fsServiceOrderRow.DocTotal, out CuryDocTotal);
                }
            }

            arPaymentRow.CuryOrigDocAmt = CuryDocTotal;

            arPaymentRow.ExtRefNbr = fsServiceOrderRow.CustWorkOrderRefNbr;
            arPaymentRow.DocDesc = fsServiceOrderRow.DocDesc;
            arPaymentRow = graphARPaymentEntry.Document.Update(arPaymentRow);

            InsertSOAdjustments(fsServiceOrderRow, fsAppointmentRow, graphARPaymentEntry, arPaymentRow);
        }

        public virtual void InsertSOAdjustments(FSServiceOrder fsServiceOrderRow, FSAppointment fSAppointmentRow, ARPaymentEntry arPaymentGraph, ARPayment arPaymentRow)
        {
            FSAdjust fsAdjustRow = new FSAdjust()
            {
                AdjdOrderType = fsServiceOrderRow.SrvOrdType,
                AdjdOrderNbr = fsServiceOrderRow.RefNbr,
                AdjdAppRefNbr = fSAppointmentRow != null ? fSAppointmentRow.RefNbr : null,
                SOCuryCompletedBillableTotal = fsServiceOrderRow.SOCuryCompletedBillableTotal
            };

            SM_ARPaymentEntry sm_ARPaymentEntry = arPaymentGraph.GetExtension<SM_ARPaymentEntry>();

            try
            {
                sm_ARPaymentEntry.FSAdjustments.Insert(fsAdjustRow);
            }
            catch (PXSetPropertyException)
            {
                arPaymentRow.CuryOrigDocAmt = 0m;
            }
        }

        public virtual void RecalcSOApplAmounts(PXGraph graph, ARPayment row)
        {
            ServiceOrderEntry.RecalcSOApplAmountsInt(graph, row);
        }

        public virtual void HidePrepayments(PXView fsAdjustmentsView, PXCache cache, FSServiceOrder fsServiceOrderRow, FSAppointment fsAppointmentRow, FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsServiceOrderRow == null
                || fsSrvOrdTypeRow == null)
            {
                return;
            }

            bool showPrepayment = fsSrvOrdTypeRow.PostToSOSIPM == true;

            if (fsServiceOrderRow != null)
            {
                showPrepayment = showPrepayment && fsServiceOrderRow.BillServiceContractID == null;
                fsServiceOrderRow.IsPrepaymentEnable = showPrepayment;
            }

            if (fsAppointmentRow != null)
            {
                showPrepayment = showPrepayment && fsAppointmentRow.BillServiceContractID == null;
                fsAppointmentRow.IsPrepaymentEnable = showPrepayment;
            }

            fsAdjustmentsView.AllowSelect = showPrepayment;

            PXUIFieldAttribute.SetVisible<FSServiceOrder.sOPrepaymentApplied>(cache, fsServiceOrderRow, showPrepayment);
            PXUIFieldAttribute.SetVisible<FSServiceOrder.sOPrepaymentReceived>(cache, fsServiceOrderRow, showPrepayment);
            PXUIFieldAttribute.SetVisible<FSServiceOrder.sOPrepaymentRemaining>(cache, fsServiceOrderRow, showPrepayment);
            PXUIFieldAttribute.SetVisible<FSServiceOrder.sOCuryUnpaidBalanace>(cache, fsServiceOrderRow, showPrepayment);
            PXUIFieldAttribute.SetVisible<FSServiceOrder.sOCuryBillableUnpaidBalanace>(cache, fsServiceOrderRow, showPrepayment);
        }

        public virtual void UpdateServiceOrderUnboundFields(PXGraph graph, FSServiceOrder fsServiceOrderRow, FSBillingCycle fsBillingCycleRow, PXGraph appointmentGraph, FSAppointment fsAppointmentRow, bool disableServiceOrderUnboundFieldCalc, bool calcPrepaymentAmount = true)
        {
            ServiceOrderEntry.UpdateServiceOrderUnboundFieldsInt(graph, fsServiceOrderRow, fsBillingCycleRow, appointmentGraph, fsAppointmentRow, disableServiceOrderUnboundFieldCalc, calcPrepaymentAmount);
        }
        public virtual void SetCostCodeDefault(IFSSODetBase fsSODetRow, int? projectID, FSSrvOrdType fsSrvOrdTypeRow, PXFieldDefaultingEventArgs e)
        {
            if (fsSrvOrdTypeRow != null
                && !ProjectDefaultAttribute.IsNonProject(projectID)
                && PXAccess.FeatureInstalled<FeaturesSet.costCodes>()
                && fsSODetRow.InventoryID != null
                && fsSODetRow.IsPrepaid == false
                && fsSODetRow.ContractRelated == false)
            {
                e.NewValue = fsSrvOrdTypeRow.DfltCostCodeID;
            }
        }

        public virtual void SetVisiblePODetFields(PXCache cache, bool showPOFields)
        {
            PXUIFieldAttribute.SetVisible<FSSODet.poNbr>(cache, null, showPOFields);
            PXUIFieldAttribute.SetVisible<FSSODet.pOCreate>(cache, null, showPOFields);
            PXUIFieldAttribute.SetVisible<FSSODet.poStatus>(cache, null, showPOFields);
            PXUIFieldAttribute.SetVisible<FSSODet.poVendorID>(cache, null, showPOFields);
            PXUIFieldAttribute.SetVisible<FSSODet.poVendorLocationID>(cache, null, showPOFields);
            PXUIFieldAttribute.SetVisible<FSSODet.enablePO>(cache, null, showPOFields);
            PXUIFieldAttribute.SetVisible<FSSODet.curyUnitCost>(cache, null, showPOFields);
        }

        public virtual Int32? GetValidatedSiteID(PXGraph graph, int? siteID)
        {
            if (siteID != null)
            {
                INSite inSite = PXSelect<INSite,
                                    Where<INSite.siteID, Equal<Required<INSite.siteID>>,
                                        And<Match<INSite, Current<AccessInfo.userName>>>>>.Select(graph, siteID);
                if (inSite != null)
                {
                    return inSite.SiteID;
                }
            }

            return null;
        }

        public virtual KeyValuePair<string, string>[] GetAppointmentUrlArguments(FSAppointment fsAppointmentRow)
        {
            KeyValuePair<string, string>[] urlArgs = new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>(typeof(FSAppointment.refNbr).Name, fsAppointmentRow.RefNbr),
                new KeyValuePair<string, string>("Date", fsAppointmentRow.ScheduledDateBegin.ToString()),
                new KeyValuePair<string, string>("AppSource", "1")
            };

            return urlArgs;
        }

        #region EnableDisable
        public virtual void EnableDisable_ServiceActualDateTimes(PXCache appointmentDetCache,
                                                                FSAppointment fsAppointmentRow,
                                                                FSAppointmentDet fsAppointmentDetRow,
                                                                bool enableByLineType)
        {
            if (fsAppointmentRow == null || fsAppointmentDetRow == null)
            {
                return;
            }

            //Grouping conditions that affect Service Actual Duration's enable/disable behavior.

            //Enable by Time Behavior
            bool enableByTimeBehavior = fsAppointmentRow == null ? false : fsAppointmentRow.NotStarted == false;

            //Enable by Log Related
            bool enableByLogRelated = (fsAppointmentDetRow.LogRelatedCount ?? 0) == 0;

            bool enableActualDuration = enableByLineType && enableByLogRelated && enableByTimeBehavior;

            PXUIFieldAttribute.SetEnabled<FSAppointmentDet.actualDuration>(appointmentDetCache, fsAppointmentDetRow, enableActualDuration);
        }

        public virtual void EnableDisable_TimeRelatedFields(PXCache appointmentEmployeeCache,
                                                           FSSetup fsSetupRow,
                                                           FSSrvOrdType fsSrvOrdType,
                                                           FSAppointment fsAppointmentRow,
                                                           FSAppointmentEmployee fsAppointmentEmployeeRow)
        {
            if (fsAppointmentRow == null || fsAppointmentEmployeeRow == null || fsSetupRow == null)
            {
                return;
            }

            bool enableByTimeBehavior = fsAppointmentRow == null ? false : fsAppointmentRow.NotStarted == false;

            bool enableActualStartDateTime = enableByTimeBehavior;

            bool enableTrackTime = fsSetupRow.EnableEmpTimeCardIntegration.Value
                                   && fsSrvOrdType.CreateTimeActivitiesFromAppointment.Value
                                        && fsAppointmentEmployeeRow.Type == BAccountType.EmployeeType
                                            && fsAppointmentEmployeeRow.EmployeeID != null;

            PXUIFieldAttribute.SetEnabled<FSAppointmentEmployee.trackTime>(appointmentEmployeeCache,
                                                                           fsAppointmentEmployeeRow,
                                                                           enableTrackTime);

            PXUIFieldAttribute.SetEnabled<FSAppointmentEmployee.earningType>(appointmentEmployeeCache,
                                                                             fsAppointmentEmployeeRow,
                                                                             enableTrackTime);
        }

        public virtual void SetVisible_TimeRelatedFields(PXCache appointmentEmployeeCache, FSSrvOrdType fsSrvOrdType)
        {
            PXUIFieldAttribute.SetVisible<FSAppointmentEmployee.trackTime>(appointmentEmployeeCache,
                                                                           null,
                                                                           fsSrvOrdType.CreateTimeActivitiesFromAppointment.Value);

            PXUIFieldAttribute.SetVisible<FSAppointmentEmployee.earningType>(appointmentEmployeeCache,
                                                                             null,
                                                                             fsSrvOrdType.CreateTimeActivitiesFromAppointment.Value);
        }

        public virtual void SetPersisting_TimeRelatedFields(PXCache appointmentEmployeeCache,
                                                           FSSetup fsSetupRow,
                                                           FSSrvOrdType fsSrvOrdType,
                                                           FSAppointment fsAppointmentRow,
                                                           FSAppointmentEmployee fsAppointmentEmployeeRow)
        {
            if (fsSetupRow == null)
            {
                throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSetup)));
            }

            bool isTAIntegrationActive = fsSetupRow.EnableEmpTimeCardIntegration.Value
                                            && fsSrvOrdType.CreateTimeActivitiesFromAppointment.Value
                                            && fsAppointmentEmployeeRow.Type == BAccountType.EmployeeType;

            PXPersistingCheck persistingCheckTAIntegration = isTAIntegrationActive ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing;

            PXDefaultAttribute.SetPersistingCheck<FSAppointmentEmployee.trackTime>(appointmentEmployeeCache,
                                                                                   fsAppointmentEmployeeRow,
                                                                                   persistingCheckTAIntegration);

            PXDefaultAttribute.SetPersistingCheck<FSAppointmentEmployee.earningType>(appointmentEmployeeCache,
                                                                                     fsAppointmentEmployeeRow,
                                                                                     persistingCheckTAIntegration);
        }

        public virtual void EnableDisable_StaffRelatedFields(PXCache appointmentEmployeeCache,
                                                            FSAppointmentEmployee fsAppointmentEmployeeRow)
        {
            if (fsAppointmentEmployeeRow == null)
            {
                return;
            }

            bool enableEmployeeRelatedFields = fsAppointmentEmployeeRow.EmployeeID != null;

            PXUIFieldAttribute.SetEnabled<FSAppointmentEmployee.employeeID>
                    (appointmentEmployeeCache, fsAppointmentEmployeeRow, !enableEmployeeRelatedFields);
        }

        public virtual void EnableDisable_TimeRelatedLogFields(
                                                              PXCache cache,
                                                              FSAppointmentLog fsLogRow,
                                                              FSSetup fsSetupRow,
                                                              FSSrvOrdType fsSrvOrdType,
                                                              FSAppointment fsAppointmentRow)
        {
            if (fsSetupRow == null)
            {
                return;
            }

            bool noEmployee = fsLogRow.BAccountID == null
                                || fsLogRow.BAccountType != BAccountType.EmployeeType;

            bool enableTEFields = fsSetupRow.EnableEmpTimeCardIntegration == true
                                    && fsSrvOrdType.CreateTimeActivitiesFromAppointment == true
                                    && noEmployee == false;

            bool enableDescription = fsLogRow.ItemType == FSAppointmentLog.itemType.Values.Travel;

            bool isTimeDurationNegative = fsLogRow.TimeDuration < 0;

            if (cache.GetStatus(fsLogRow) != PXEntryStatus.Inserted)
            {
                PXUIFieldAttribute.SetEnabled<FSAppointmentLog.bAccountID>(cache, fsLogRow, noEmployee == false);
            }

            bool enableTravel = fsAppointmentRow == null ? false : fsAppointmentRow.NotStarted == false;

            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.travel>(cache, fsLogRow, enableTravel && fsLogRow.ItemType != FSAppointmentLog.itemType.Values.NonStock);
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.laborItemID>(cache, fsLogRow, noEmployee == false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.projectTaskID>(cache, fsLogRow, noEmployee == false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.costCodeID>(cache, fsLogRow, noEmployee == false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.trackTime>(cache, fsLogRow, enableTEFields);
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.earningType>(cache, fsLogRow, enableTEFields);
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.descr>(cache, fsLogRow, enableDescription);
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.trackOnService>(cache, fsLogRow, fsLogRow.ItemType != FSAppointmentLog.itemType.Values.NonStock);
            
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.dateTimeEnd>(cache, fsLogRow, isTimeDurationNegative == false);
            PXUIFieldAttribute.SetEnabled<FSAppointmentLog.status>(cache, fsLogRow, isTimeDurationNegative == false);
        }

        public virtual void UpdateStaffRelatedUnboundFields(FSAppointmentDet fsAppointmentDetServiceRow,
                                                            AppointmentEntry.AppointmentServiceEmployees_View appointmentEmployees,
                                                            PXSelectBase<FSAppointmentLog> logView,
                                                            int? numEmployeeLinkedToService)
        {
            if (numEmployeeLinkedToService == null || fsAppointmentDetServiceRow.LogRelatedCount == null)
            {
                using (new PXConnectionScope())
                {
                    if (numEmployeeLinkedToService == null)
                    {
                        numEmployeeLinkedToService = appointmentEmployees.Select().AsEnumerable().RowCast<FSAppointmentEmployee>()
                                                     .Where(y => y.ServiceLineRef == fsAppointmentDetServiceRow.LineRef)
                                                     .Count();
                    }

                    if (fsAppointmentDetServiceRow.LogRelatedCount == null)
                    {
                        fsAppointmentDetServiceRow.LogRelatedCount = 0;

                        if (RunningSelectingScope<FSAppointmentLog>.IsRunningSelecting(logView.Cache.Graph) == false)
                        {
                            using (new RunningSelectingScope<FSAppointmentLog>(logView.Cache.Graph))
                            {
                                fsAppointmentDetServiceRow.LogRelatedCount = logView.Select().AsEnumerable().RowCast<FSAppointmentLog>()
                                                                .Where(y => y.DetLineRef == fsAppointmentDetServiceRow.LineRef
                                                                    && y.TrackOnService == true)
                                                                .Count();
                            }
                        }
                    }
                }
            }

            fsAppointmentDetServiceRow.StaffRelatedCount = numEmployeeLinkedToService;

            if (numEmployeeLinkedToService == 1)
            {
                fsAppointmentDetServiceRow.StaffRelated = true;
            }
            else
            {
                fsAppointmentDetServiceRow.StaffRelated = numEmployeeLinkedToService != 0;
            }
        }

        public virtual void InsertUpdateDelete_AppointmentDetService_StaffID(PXCache cache,
                                                                            FSAppointmentDet fsAppointmentDetRow,
                                                                            AppointmentEntry.AppointmentServiceEmployees_View appointmentEmployees,
                                                                            int? oldStaffID)
        {
            if (fsAppointmentDetRow.StaffID != null && oldStaffID != null)
            {
                FSAppointmentEmployee fsAppointmentEmployeeRow =
                                            appointmentEmployees.Select()
                                                                .RowCast<FSAppointmentEmployee>()
                                                                .Where(_ => _.ServiceLineRef == fsAppointmentDetRow.LineRef
                                                                         && _.EmployeeID == oldStaffID).FirstOrDefault();
                if (fsAppointmentEmployeeRow != null)
                {
                    fsAppointmentEmployeeRow.EmployeeID = fsAppointmentDetRow.StaffID;
                    appointmentEmployees.Update(fsAppointmentEmployeeRow);
                }
            }
            else if (fsAppointmentDetRow.StaffID != null && oldStaffID == null)
            {
                FSAppointmentEmployee fsAppointmentEmployeeRow = new FSAppointmentEmployee()
                {
                    ServiceLineRef = fsAppointmentDetRow.LineRef,
                    EmployeeID = fsAppointmentDetRow.StaffID
                };

                appointmentEmployees.Insert(fsAppointmentEmployeeRow);
            }
            else
            {
                FSAppointmentEmployee fsAppointmentEmployeeRow =
                            appointmentEmployees.Select()
                                                .RowCast<FSAppointmentEmployee>()
                                                .Where(_ => _.ServiceLineRef == fsAppointmentDetRow.LineRef
                                                         && _.EmployeeID == oldStaffID).FirstOrDefault();

                appointmentEmployees.Delete(fsAppointmentEmployeeRow);
            }
        }

        [Obsolete("EnableDisable_LineType is deprecated, please use the generic methods X_RowSelected and X_SetPersistingCheck instead.")]
        public virtual void EnableDisable_LineType(PXCache cache, FSAppointmentDet fsAppointmentDetRow, FSSetup fsSetupRow, FSAppointment fsAppointmentRow, FSSrvOrdType fsSrvOrdTypeRow)
        {
            // TODO: AC-142850
            // Move all these SetEnabled and SetPersistingCheck calls to the new generic method X_RowSelected.
            // Verify if each field is handled by the generic method before moving it.
            // If the generic method already handles a field, check if the conditions to enable/disable
            // and PersistingCheck are the same.
            // DELETE this method when all fields are moved.

            if (fsAppointmentRow == null)
            {
                return;
            }

            bool enable = fsSrvOrdTypeRow.RequireTimeApprovalToInvoice == false;
            bool equipmentOrRouteModuleEnabled = PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>()
                                                        || PXAccess.FeatureInstalled<FeaturesSet.routeManagementModule>();
            bool showStandardContractColumns = fsAppointmentRow.BillServiceContractID != null
                                                && equipmentOrRouteModuleEnabled;

            switch (fsAppointmentDetRow.LineType)
            {
                case ID.LineType_ServiceTemplate.SERVICE:
                case ID.LineType_ServiceTemplate.NONSTOCKITEM:

                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.inventoryID>(cache, fsAppointmentDetRow, true);
                    PXDefaultAttribute.SetPersistingCheck<FSAppointmentDet.inventoryID>(cache, fsAppointmentDetRow, PXPersistingCheck.NullOrBlank);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.isFree>(cache, fsAppointmentDetRow, true);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.actualQty>(cache, fsAppointmentDetRow, fsAppointmentRow == null ? false : fsAppointmentRow.NotStarted == false);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.estimatedQty>(cache, fsAppointmentDetRow, true);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.projectTaskID>(cache, fsAppointmentDetRow, true);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.estimatedDuration>(cache, fsAppointmentDetRow, true);
                    PXUIFieldAttribute.SetVisible<FSSODet.contractRelated>(cache, null, showStandardContractColumns);
                    PXUIFieldAttribute.SetVisible<FSSODet.coveredQty>(cache, null, showStandardContractColumns);
                    PXUIFieldAttribute.SetVisible<FSSODet.extraUsageQty>(cache, null, showStandardContractColumns);
                    PXUIFieldAttribute.SetVisible<FSSODet.curyExtraUsageUnitPrice>(cache, null, showStandardContractColumns);
                    PXUIFieldAttribute.SetVisibility<FSSODet.contractRelated>(cache, null, equipmentOrRouteModuleEnabled ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
                    PXUIFieldAttribute.SetVisibility<FSSODet.coveredQty>(cache, null, equipmentOrRouteModuleEnabled ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
                    PXUIFieldAttribute.SetVisibility<FSSODet.extraUsageQty>(cache, null, equipmentOrRouteModuleEnabled ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
                    PXUIFieldAttribute.SetVisibility<FSSODet.curyExtraUsageUnitPrice>(cache, null, equipmentOrRouteModuleEnabled ? PXUIVisibility.Visible : PXUIVisibility.Invisible);

                    break;

                case ID.LineType_ServiceTemplate.COMMENT:
                case ID.LineType_ServiceTemplate.INSTRUCTION:

                    PXDefaultAttribute.SetPersistingCheck<FSAppointmentDet.tranDesc>(cache, fsAppointmentDetRow, PXPersistingCheck.NullOrBlank);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.inventoryID>(cache, fsAppointmentDetRow, false);
                    PXDefaultAttribute.SetPersistingCheck<FSAppointmentDet.inventoryID>(cache, fsAppointmentDetRow, PXPersistingCheck.Nothing);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.isFree>(cache, fsAppointmentDetRow, false);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.actualQty>(cache, fsAppointmentDetRow, false);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.estimatedQty>(cache, fsAppointmentDetRow, false);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.projectTaskID>(cache, fsAppointmentDetRow, false);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.staffID>(cache, fsAppointmentDetRow, false);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.billingRule>(cache, fsAppointmentDetRow, false);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.estimatedDuration>(cache, fsAppointmentDetRow, false);

                    enable = false;

                    break;

                case ID.LineType_ServiceTemplate.INVENTORY_ITEM:
                default:
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.inventoryID>(cache, fsAppointmentDetRow, true);
                    PXDefaultAttribute.SetPersistingCheck<FSAppointmentDet.inventoryID>(cache, fsAppointmentDetRow, PXPersistingCheck.NullOrBlank);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.isFree>(cache, fsAppointmentDetRow, true);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.actualQty>(cache, fsAppointmentDetRow, fsAppointmentRow == null ? false : fsAppointmentRow.NotStarted == false);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.estimatedQty>(cache, fsAppointmentDetRow, true);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.projectTaskID>(cache, fsAppointmentDetRow, true);
                    PXUIFieldAttribute.SetEnabled<FSAppointmentDet.staffID>(cache, fsAppointmentDetRow, false);

                    break;
            }

            EnableDisable_ServiceActualDateTimes(cache, fsAppointmentRow, fsAppointmentDetRow, enable);
        }
        #endregion

        /// <summary>
        /// Returns true if an Appointment [fsAppointmentRow] can be updated based in its status and the status of the Service Order [fsServiceOrderRow].
        /// </summary>
        public virtual bool CanUpdateAppointment(FSAppointment fsAppointmentRow, FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsAppointmentRow == null || fsSrvOrdTypeRow == null)
            {
                return false;
            }

            if (fsAppointmentRow.Closed == true
                || fsAppointmentRow.Canceled == true
                || fsSrvOrdTypeRow.Active == false
                || fsAppointmentRow.Billed == true)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if an Appointment [fsAppointmentRow] can be deleted based in its status and the status of the Service Order [fsServiceOrderRow].
        /// </summary>
        public virtual bool CanDeleteAppointment(FSAppointment fsAppointmentRow, FSServiceOrder fsServiceOrderRow, FSSrvOrdType fsSrvOrdTypeRow)
        {
            bool canDeleteServiceOrder = true;

            if (fsServiceOrderRow != null)
            {
                canDeleteServiceOrder = CanUpdateServiceOrder(fsServiceOrderRow, fsSrvOrdTypeRow);
            }

            if (fsAppointmentRow == null
                || (fsAppointmentRow.NotStarted == false
                    && fsAppointmentRow.InProcess == false
                        && fsAppointmentRow.Hold == false))
            {
                return false;
            }

            return canDeleteServiceOrder;
        }

        public virtual void GetSODetValues<AppointmentDetType, SODetType>(PXCache cacheAppointmentDet,
                                                                         AppointmentDetType fsAppointmentDetRow,
                                                                         FSServiceOrder fsServiceOrderRow,
                                                                         FSAppointment fsAppointmentRow,
                                                                         FSSODet fsSODetRow)
            where AppointmentDetType : FSAppointmentDet, new()
            where SODetType : FSSODet, new()
        {
            if (fsAppointmentDetRow.SODetID == null)
            {
                return;
            }

            PXCache cacheSODet = new PXCache<FSSODet>(cacheAppointmentDet.Graph);

            if (fsSODetRow == null)
            {
                fsSODetRow = FSSODet.UK.Find(cacheAppointmentDet.Graph, fsAppointmentDetRow.SODetID);
            }

            if (fsSODetRow != null)
            {
                var graphAppointment = (AppointmentEntry)cacheAppointmentDet.Graph;

                graphAppointment.CopyAppointmentLineValues<AppointmentDetType, FSSODet>(cacheAppointmentDet,
                                                                                        fsAppointmentDetRow,
                                                                                        cacheSODet,
                                                                                        fsSODetRow,
                                                                                        false,
                                                                                        fsSODetRow.TranDate,
                                                                                        ForceFormulaCalculation: false);

                if (fsAppointmentRow == null ? false : fsAppointmentRow.NotStarted == true)
                {
                    cacheAppointmentDet.SetValueExtIfDifferent<FSAppointmentDet.actualDuration>(fsAppointmentDetRow, 0);
                    cacheAppointmentDet.SetValueExtIfDifferent<FSAppointmentDet.actualQty>(fsAppointmentDetRow, 0m);
                }

                if (fsServiceOrderRow.SourceRefNbr != null
                        && fsServiceOrderRow.SourceType == ID.SourceType_ServiceOrder.SALES_ORDER
                            && fsAppointmentDetRow.IsPrepaid == true)
                {
                    fsAppointmentDetRow.LinkedDocRefNbr = fsServiceOrderRow.SourceRefNbr;
                    fsAppointmentDetRow.LinkedDocType = fsServiceOrderRow.SourceDocType;
                    fsAppointmentDetRow.LinkedEntityType = FSAppointmentDet.linkedEntityType.Values.SalesOrder;
                }
            }
        }

        public virtual void ValidateQty(PXCache cache, FSAppointmentDet fsAppointmentDetRow, PXErrorLevel errorLevel = PXErrorLevel.Error)
        {
            if (fsAppointmentDetRow.ActualQty < 0)
            {
                PXUIFieldAttribute.SetEnabled<FSAppointmentDet.actualQty>(cache, fsAppointmentDetRow, true);
                cache.RaiseExceptionHandling<FSAppointmentDet.actualQty>(fsAppointmentDetRow,
                                                                   null,
                                                                   new PXSetPropertyException(TX.Error.NEGATIVE_QTY, errorLevel));
            }
        }

        /// <summary>
        /// Determines if a Service line has at least one pickup/delivery item related.
        /// </summary>
        public virtual bool ServiceLinkedToPickupDeliveryItem(PXGraph graph, FSAppointmentDet fsAppointmentDetRow, FSAppointment fsAppointmentRow)
        {
            int srvLinkedCount = PXSelect<FSAppointmentDet,
                                 Where<
                                     FSAppointmentDet.lineType, Equal<ListField_LineType_Pickup_Delivery.Pickup_Delivery>,
                                 And<
                                     FSAppointmentDet.sODetID, Equal<Required<FSAppointmentDet.sODetID>>,
                                 And<
                                     FSAppointmentDet.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>>>
                                 .Select(graph, fsAppointmentDetRow.SODetID, fsAppointmentRow.AppointmentID)
                                 .Count;

            return srvLinkedCount > 0;
        }

        [Obsolete("Delete this method in the next major release")]
        public virtual void UpdatePendingPostFlags(FSAppointment fsAppointmentRow,
                                                  FSServiceOrder fsServiceOrder,
                                                  AppointmentDetails_View appointmentDetails)
        {
        }

        [Obsolete("This method will be deleted in the next major release.")]
        public void UpdatePendingPostFlags(FSAppointment fsAppointmentRow,
                                                  FSServiceOrder fsServiceOrder)
        {
        }

        [Obsolete("This method will be deleted in the next major release.")]
        public virtual void UpdatePendingPostFlags(PXCache cache,
                                                   AppointmentDetails_View appointmentDetails,
                                                   FSAppointment fsAppointmentRow,
                                                   FSServiceOrder fsServiceOrder,
                                                   FSSrvOrdType srvOrdType)
        {
        }

        [Obsolete("This method will be deleted in the next major release.")]
        public void UpdatePendingPostFlags(FSAppointment fsAppointmentRow,
                                       FSServiceOrder fsServiceOrder,
                                       PXSelectBase<FSAppointmentDet> appointmentDetails,
                                       PXSelectBase<FSPostInfo> postInfoDetails)
        {
        }

        public virtual void UpdatePendingPostFlags(PXCache cache,
                                                   AppointmentDetails_View appointmentDetails,
                                                   PXSelectBase<FSPostInfo> PostInfoDetails,
                                                   FSAppointment fsAppointmentRow,
                                                   FSServiceOrder fsServiceOrder,
                                                   FSSrvOrdType srvOrdType)
        {
            bool setAsNotPending = false;

            if ((fsServiceOrder.PostedBy == null || fsServiceOrder.PostedBy == ID.Billing_By.APPOINTMENT)
                    && fsAppointmentRow.PostingStatusAPARSO != ID.Status_Posting.POSTED)
            {
                int? servicesToPost = 0;
                int? partsToPost = 0;
                int? pickUpDeliveryToPost = 0;
                bool acceptPickUpDelivery = srvOrdType?.EnableINPosting == true
                                            && fsAppointmentRow.IsRouteAppoinment == true;

                foreach (FSAppointmentDet det in appointmentDetails.Select())
                {
                    FSPostInfo postInfo = PostInfoDetails.Select().RowCast<FSPostInfo>().Where(x => x.PostID == det.PostID).FirstOrDefault();

                    if (det.IsService && det.needToBePosted())
                    {
                        if (det.IsBillable == false)
                        {
                            // Currently stock item lines can not be marked as non-billable
                            // because we post directly to IN without generating a billing document
                            // only for pick-up and delivery items
                            continue;
                        }

                        if (postInfo == null || postInfo.isPosted() == false)
                        {
                            servicesToPost++;
                        }
                    }

                    if (det.IsInventoryItem && det.needToBePosted())
                    {
                        if (det.IsBillable == false)
                        {
                            // Currently stock item lines can not be marked as non-billable
                            // because we post directly to IN without generating a billing document
                            // only for pick-up and delivery items
                            continue;
                        }

                        if (postInfo == null || postInfo.isPosted() == false)
                        {
                            partsToPost++;
                        }
                    }

                    if (det.IsPickupDelivery)
                    {
                        if (postInfo == null || postInfo.isPosted() == false)
                        {
                            pickUpDeliveryToPost++;
                        }
                    }

                    if ((servicesToPost > 0 || partsToPost > 0)
                        && (acceptPickUpDelivery == false
                            || (acceptPickUpDelivery == true && pickUpDeliveryToPost > 0)
                        ))
                    {
                        break;
                    }
                }

                if (servicesToPost > 0 || partsToPost > 0 || pickUpDeliveryToPost > 0)
                {
                    fsAppointmentRow.PendingAPARSOPost = true;
                    fsAppointmentRow.PostingStatusAPARSO = ID.Status_Posting.PENDING_TO_POST;

                    if (pickUpDeliveryToPost > 0)
                    {
                        fsAppointmentRow.PendingINPost = true;
                        //// @TODO: AC-142850 When IN posting is done, uncomment this line
                        ////fsAppointmentRow.PostingStatusIN = ID.Status_Posting.PENDING_TO_POST;
                    }
                    else
                    {
                        fsAppointmentRow.PendingINPost = false;
                        fsAppointmentRow.PostingStatusIN = ID.Status_Posting.NOTHING_TO_POST;
                    }
                }
                else
                {
                    setAsNotPending = true;
                }
            }
            else
            {
                setAsNotPending = true;
            }

            if (setAsNotPending == true)
            {
                fsAppointmentRow.PendingAPARSOPost = false;
                fsAppointmentRow.PendingINPost = false;
                fsAppointmentRow.PostingStatusAPARSO = fsAppointmentRow.PostingStatusAPARSO != ID.Status_Posting.POSTED ? ID.Status_Posting.NOTHING_TO_POST : ID.Status_Posting.POSTED;
                fsAppointmentRow.PostingStatusIN = fsAppointmentRow.PostingStatusIN != ID.Status_Posting.POSTED ? ID.Status_Posting.NOTHING_TO_POST : ID.Status_Posting.POSTED;
            }
        }

        // TODO:
        [Obsolete("Delete this method in the next major release")]
        public virtual void UpdateApptUsageStatisticsOnSrvOrdLine<SODetType>(PXCache appCache,
                                                                                FSAppointment fsAppointmentRow,
                                                                                PXCache detCache,
                                                                                FSAppointmentDet fsAppointmentDetRow,
                                                                                PXSelectBase<SODetType> viewSODet)
            where SODetType : FSSODet, new()
        {
            //Validating line status
            PXEntryStatus detStatus = detCache.GetStatus(fsAppointmentDetRow);

            if (detStatus != PXEntryStatus.Inserted
                && detStatus != PXEntryStatus.Updated)
            {
                return;
            }

            //Validating Line type
            if (fsAppointmentDetRow.LineType != ID.LineType_ALL.SERVICE
                    && fsAppointmentDetRow.LineType != ID.LineType_ALL.NONSTOCKITEM
                        && fsAppointmentDetRow.LineType != ID.LineType_ALL.INVENTORY_ITEM)
            {
                return;
            }

            SODetType fsSODetRow = (SODetType)fsAppointmentDetRow.FSSODetRow;

            if (fsSODetRow == null)
            {
                return;
            }

            bool? oldNotStarted = (bool?)appCache.GetValueOriginal<FSAppointment.notStarted>(fsAppointmentRow);
            string oldLineStatus = (string)detCache.GetValueOriginal<FSAppointmentDet.status>(fsAppointmentDetRow);

            decimal? _CuryBillableTranAmt = 0m;
            decimal? _ApptQty = 0m;
            int? _ApptDuration = 0;
            int? _ApptEstimatedDuration = 0;
            int? _ApptNumber = 0;

            decimal? _OriCuryBillableTranAmt = (decimal?)detCache.GetValueOriginal<FSAppointmentDet.curyBillableTranAmt>(fsAppointmentDetRow) ?? 0;

            if (_OriCuryBillableTranAmt != fsAppointmentDetRow.CuryBillableTranAmt)
            {
                _CuryBillableTranAmt = fsAppointmentDetRow.CuryBillableTranAmt - (_OriCuryBillableTranAmt ?? 0);
            }

            decimal? _OriApptQty = (decimal?)detCache.GetValueOriginal<FSAppointmentDet.billableQty>(fsAppointmentDetRow) ?? 0;

            if (_OriApptQty != fsAppointmentDetRow.BillableQty)
            {
                _ApptQty = fsAppointmentDetRow.BillableQty - (_OriApptQty ?? 0);
            }

            int? _OriApptDuration = (int?)detCache.GetValueOriginal<FSAppointmentDet.actualDuration>(fsAppointmentDetRow) ?? 0;

            if (_OriApptDuration != fsAppointmentDetRow.ActualDuration)
            {
                _ApptDuration = fsAppointmentDetRow.ActualDuration - (_OriApptDuration ?? 0);
            }

            int? _OriApptEstimatedDuration = (int?)detCache.GetValueOriginal<FSAppointmentDet.estimatedDuration>(fsAppointmentDetRow) ?? 0;

            if (_OriApptEstimatedDuration != fsAppointmentDetRow.EstimatedDuration)
            {
                _ApptEstimatedDuration = fsAppointmentDetRow.EstimatedDuration - (_OriApptEstimatedDuration ?? 0);
            }

            if (fsSODetRow != null)
            {
                bool updateServiceOrder = false;

                switch (detStatus)
                {
                    case PXEntryStatus.Inserted:

                        if (fsAppointmentDetRow.IsCanceledNotPerformed != true)
                        {
                            _CuryBillableTranAmt = fsAppointmentDetRow.CuryBillableTranAmt;
                            _ApptQty = fsAppointmentDetRow.BillableQty;
                            _ApptDuration = fsAppointmentDetRow.ActualDuration;
                            _ApptEstimatedDuration = fsAppointmentDetRow.EstimatedDuration;
                            _ApptNumber = 1;

                            updateServiceOrder = true;
                        }
                        break;
                    case PXEntryStatus.Updated:

                        //Appointment line status change --> CANCELED
                        if (oldLineStatus != fsAppointmentDetRow.Status
                                && fsAppointmentDetRow.IsCanceledNotPerformed == true
                                && (oldLineStatus != ID.Status_AppointmentDet.CANCELED
                                    && oldLineStatus != ID.Status_AppointmentDet.NOT_PERFORMED
                                    && oldLineStatus != ID.Status_AppointmentDet.RequestForPO))
                        {
                            _CuryBillableTranAmt = -_OriCuryBillableTranAmt;
                            _ApptQty = -_OriApptQty;
                            _ApptDuration = -_OriApptDuration;
                            _ApptEstimatedDuration = -_OriApptEstimatedDuration;
                            _ApptNumber = -1;
                        }

                        //Appointment line status change: CANCELED --> REOPEN
                        if (oldLineStatus != fsAppointmentDetRow.Status
                                && fsAppointmentDetRow.IsCanceledNotPerformed == false
                                && (oldLineStatus == ID.Status_AppointmentDet.CANCELED
                                    || oldLineStatus == ID.Status_AppointmentDet.NOT_PERFORMED
                                    || oldLineStatus == ID.Status_AppointmentDet.RequestForPO))
                        {
                            _CuryBillableTranAmt = fsAppointmentDetRow.CuryBillableTranAmt;
                            _ApptQty = fsAppointmentDetRow.BillableQty;
                            _ApptDuration = fsAppointmentDetRow.ActualDuration;
                            _ApptEstimatedDuration = fsAppointmentDetRow.EstimatedDuration;
                            _ApptNumber = 1;
                        }

                        updateServiceOrder = true;

                        break;
                }

                //Appointment status change: XXXX --> REOPEN
                if (oldNotStarted != fsAppointmentRow.NotStarted
                    && fsAppointmentRow.NotStarted == true
                        && oldLineStatus == fsAppointmentDetRow.Status)
                {
                    _CuryBillableTranAmt = -_OriCuryBillableTranAmt + fsAppointmentDetRow.CuryBillableTranAmt;
                    _ApptQty = -_OriApptQty + fsAppointmentDetRow.BillableQty;
                    _ApptDuration = -_OriApptDuration;
                    _ApptEstimatedDuration = -_OriApptEstimatedDuration + fsAppointmentDetRow.EstimatedDuration;

                    updateServiceOrder = true;
                }

                if (updateServiceOrder)
                {
                    decimal? curyApptTranAmt = fsSODetRow.CuryApptTranAmt + _CuryBillableTranAmt;
                    decimal? apptQty = fsSODetRow.ApptQty + _ApptQty;
                    int? apptDuration = fsSODetRow.ApptDuration + _ApptDuration;
                    int? apptEstimatedDuration = fsSODetRow.ApptEstimatedDuration + _ApptEstimatedDuration;
                    int? apptNumber = fsSODetRow.ApptCntr + _ApptNumber;

                    // Updating Service Order's detail line
                    fsSODetRow.CuryApptTranAmt = curyApptTranAmt;
                    fsSODetRow.ApptQty = apptQty;
                    fsSODetRow.ApptDuration = apptDuration;
                    fsSODetRow.ApptEstimatedDuration = apptEstimatedDuration;
                    fsSODetRow.ApptCntr = apptNumber;

                    viewSODet.Cache.Update(fsSODetRow);

                    if (fsAppointmentDetRow.Status != ID.Status_AppointmentDet.NOT_FINISHED
                            && fsSODetRow.Status != ID.Status_SODet.COMPLETED)
                    {
                        viewSODet.Cache.SetDefaultExt<FSSODet.status>(fsSODetRow);
                    }
                }
            }
        }

        public virtual bool IsAppointmentReadyToBeInvoiced(FSAppointment fsAppointmentRow, FSServiceOrder fsServiceOrderRow, FSBillingCycle fsBillingCycle, FSSrvOrdType fsSrvOrdTypeRow)
        {
            // @TODO: AC-142850 Improve this, is completely unreadable
            return fsAppointmentRow != null
                    && fsServiceOrderRow != null
                    && fsBillingCycle != null
                    && fsSrvOrdTypeRow != null
                    && fsAppointmentRow.PendingAPARSOPost == true
                    && fsAppointmentRow.BillContractPeriodID == null
                    && fsAppointmentRow.BillContractPeriodID == null
                    && fsServiceOrderRow.BillContractPeriodID == null
                    && fsServiceOrderRow.BillContractPeriodID == null
                    && fsServiceOrderRow.CBID != null
                    && fsServiceOrderRow.BillingBy == ID.Billing_By.APPOINTMENT
                    && (fsAppointmentRow.Closed == true
                            || (fsAppointmentRow.Completed == true
                                    && fsSrvOrdTypeRow.AllowInvoiceOnlyClosedAppointment == false
                                    && fsAppointmentRow.TimeRegistered == true))
                    && (fsBillingCycle.InvoiceOnlyCompletedServiceOrder == false
                            || (fsBillingCycle.InvoiceOnlyCompletedServiceOrder == true
                                    && (fsServiceOrderRow.Completed == true
                                            || fsServiceOrderRow.Closed == true)));
        }

        public virtual void UpdateSalesOrderByCompletingAppointment(PXGraph graph, string sourceDocType, string sourceRefNbr)
        {
            SOOrder sOOrderRow = PXSelect<SOOrder,
                                 Where<
                                    SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
                                 And<
                                    SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>
                                 .Select(graph, sourceDocType, sourceRefNbr);

            if (sOOrderRow == null)
            {
                throw new PXException(TX.Error.SERVICE_ORDER_SOORDER_INCONSISTENCY);
            }

            //Installed flag lift for Sales Order
            PXUpdate<
                Set<FSxSOOrder.installed, True>,
            SOOrder,
            Where<
                SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
            And<
                SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>
            .Update(graph, sourceDocType, sourceRefNbr);

            PXResultset<SOOrderShipment> bqlResultSet = PXSelect<SOOrderShipment,
                                                        Where<
                                                            SOOrderShipment.orderType, Equal<Required<SOOrderShipment.orderType>>,
                                                        And<
                                                            SOOrderShipment.orderNbr, Equal<Required<SOOrderShipment.orderNbr>>>>>
                                                        .Select(graph, sOOrderRow.OrderType, sOOrderRow.OrderNbr);

            foreach (SOOrderShipment soOrderShipmentRow in bqlResultSet)
            {
                //Installed flag lift for the Shipment
                PXUpdate<
                    Set<FSxSOShipment.installed, True>,
                SOShipment,
                Where
                    <SOShipment.shipmentNbr, Equal<Required<SOShipment.shipmentNbr>>>>
                .Update(graph, soOrderShipmentRow.ShipmentNbr);
            }
        }

        public virtual void SendNotification(PXCache cache, FSAppointment fsAppointmentRow, string mailing, int? branchID, IList<Guid?> attachments = null)
        {
            AppointmentEntry graphAppointmentEntry = PXGraph.CreateInstance<AppointmentEntry>();

            graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Search<FSAppointment.refNbr>(fsAppointmentRow.RefNbr, fsAppointmentRow.SrvOrdType);

            PXLongOperation.StartOperation(cache.Graph,
            delegate
            {
                graphAppointmentEntry.SendNotification(graphAppointmentEntry, cache, mailing, branchID, attachments);
            });
        }

        public virtual List<FSAppointmentDet> GetRelatedApptLines(PXGraph graph, int? soDetID, bool excludeSpecificApptLine, int? apptDetID, bool onlyMarkForPOLines, bool sortResult)
        {
            return AppointmentEntry.GetRelatedApptLinesInt(graph, soDetID, excludeSpecificApptLine, apptDetID, onlyMarkForPOLines, sortResult);
        }

        public virtual void DisableAllDACFields(PXCache cache, object row, List<Type> fieldsToIgnore)
        {
            var namesToIgnore = new List<string>();

            foreach (Type field in fieldsToIgnore)
            {
                namesToIgnore.Add(field.Name.ToLower());
            }

            foreach (string fieldName in cache.Fields)
            {
                if (fieldName.Contains("_"))
                    continue;

                if (namesToIgnore.Contains(fieldName.ToLower()))
                    continue;

                IPXInterfaceField interfaceField = cache.GetAttributesReadonly(row, fieldName).OfType<IPXInterfaceField>().FirstOrDefault();
                if (interfaceField == null) // if the field is not in the interface
                    continue;

                PXUIFieldAttribute.SetEnabled(cache, row, fieldName, false);
            }
        }

        public virtual string GetLineType(string lineType, bool lower = false)
        {
            string lineTypeTX = "";

            switch (lineType)
            {
                case ID.LineType_ALL.SERVICE: lineTypeTX = TX.LineType_ALL.SERVICE; break;
                case ID.LineType_ALL.NONSTOCKITEM: lineTypeTX = TX.LineType_ALL.NONSTOCKITEM; break;
                case ID.LineType_ALL.INVENTORY_ITEM: lineTypeTX = TX.LineType_ALL.INVENTORY_ITEM; break;
                case ID.LineType_ALL.PICKUP_DELIVERY: lineTypeTX = TX.LineType_ALL.PICKUP_DELIVERY; break;
                case ID.LineType_ALL.COMMENT: lineTypeTX = TX.LineType_ALL.COMMENT; break;
                case ID.LineType_ALL.INSTRUCTION: lineTypeTX = TX.LineType_ALL.INSTRUCTION; break;
            }

            if (lower)
            {
                lineTypeTX.ToLower();
            }

            return lineTypeTX;
        }

        public virtual void ValidateDuplicateLineNbr(PXSelectBase<FSSODet> srvOrdDetails, PXSelectBase<FSAppointmentDet> apptDetails)
        {
            var lineNbrs = new List<int?>();

            if (srvOrdDetails != null)
            {
                ValidateDuplicateLineNbr<FSSODet>(lineNbrs,
                                                  srvOrdDetails.Select().ToList().Select(e => (FSSODet)e).ToList(),
                                                  srvOrdDetails.Cache);
            }

            if (apptDetails != null)
            {
                ValidateDuplicateLineNbr<FSAppointmentDet>(lineNbrs,
                                                           apptDetails.Select().ToList().Select(e => (FSAppointmentDet)e).ToList(),
                                                           apptDetails.Cache);
            }
        }

        public virtual void ValidateDuplicateLineNbr<DetailType>(List<int?> lineNbrs, List<DetailType> list, PXCache cache)
            where DetailType : IBqlTable, IFSSODetBase
        {
            foreach (DetailType row in list)
            {
                if (lineNbrs.Find(lineNbr => lineNbr == row.LineNbr) == default(int?))
                {
                    lineNbrs.Add(row.LineNbr);
                }
                else
                {
                    PXFieldState state = cache.GetValueExt<FSSODet.lineNbr>(row) as PXFieldState;
                    cache.RaiseExceptionHandling<FSSODet.lineNbr>(
                        row, state != null ? state.Value : cache.GetValue<FSSODet.lineNbr>(row),
                        new PXSetPropertyException(ErrorMessages.DuplicateEntryAdded));

                    throw new PXRowPersistingException(typeof(FSSODet.lineNbr).Name, null, ErrorMessages.DuplicateEntryAdded);
                }
            }
        }

        public virtual void RevertInvoiceDocument(object currentRow, List<FSPostDet> postDetList) 
        {
            if (currentRow == null 
                || postDetList == null
                || postDetList.Count == 0) 
            {
                return;
            }

            using (PXTransactionScope ts = new PXTransactionScope())
            {
                PXCache cacheFSBillHistory = this.Caches[typeof(FSBillHistory)];

                string SrvOrdType = null;
                string ServiceOrderRefNbr = null;
                string AppointmentRefNbr = null;

                if (currentRow is FSAppointment) 
                {
                    FSAppointment row = (FSAppointment)currentRow;
                    SrvOrdType = row.SrvOrdType;
                    ServiceOrderRefNbr = row.SORefNbr;
                    AppointmentRefNbr = row.RefNbr;
                }
                else if(currentRow is FSServiceOrder)
                {
                    FSServiceOrder row = (FSServiceOrder)currentRow;
                    SrvOrdType = row.SrvOrdType;
                    ServiceOrderRefNbr = row.RefNbr;
                    AppointmentRefNbr = null;
                }

                FSPostDet pmPostDetRow = postDetList.Where(x => ((FSPostDet)x).PMPosted == true).FirstOrDefault();
                FSPostDet inPostDetRow = postDetList.Where(x => ((FSPostDet)x).INPosted == true).FirstOrDefault();

                RegisterEntry registerEntry = null;
                INIssueEntry inIssueEntry = null;

                if (pmPostDetRow != null
                    && pmPostDetRow.PMPosted == true)
                {
                    registerEntry = PXGraph.CreateInstance<RegisterEntry>();
                    registerEntry.Document.Current = registerEntry.Document.Search<PMRegister.refNbr>(pmPostDetRow.PMRefNbr, pmPostDetRow.PMDocType);

                    if (registerEntry.Document.Current != null
                        && registerEntry.Document.Current.Status != PMRegister.status.Released)
                    {
                        throw new PXException(TX.Error.PMReverseCannotBeCreatedNotRelease, PXErrorLevel.Error);
                    }
                }

                if (inPostDetRow != null
                    && inPostDetRow.INPosted == true)
                {
                    inIssueEntry = PXGraph.CreateInstance<INIssueEntry>();
                    inIssueEntry.issue.Current = inIssueEntry.issue.Search<INRegister.refNbr>(inPostDetRow.INRefNbr, inPostDetRow.INDocType);

                    if (inIssueEntry.issue.Current != null
                        && inIssueEntry.issue.Current.Status != INDocStatus.Released)
                    {
                        throw new PXException(TX.Error.INReverseCannotBeCreatedNotRelease, PXErrorLevel.Error);
                    }
                }

                if (registerEntry != null
                    && registerEntry.Document.Current != null)
                {
                    SM_RegisterEntry registerEntryExt = registerEntry.GetExtension<SM_RegisterEntry>();
                    PMRegister reverseDoc = registerEntryExt.RevertInvoice();

                    FSBillHistory fsBillHistoryRow = new FSBillHistory();

                    fsBillHistoryRow.SrvOrdType = SrvOrdType;
                    fsBillHistoryRow.ServiceOrderRefNbr = ServiceOrderRefNbr;
                    fsBillHistoryRow.AppointmentRefNbr = AppointmentRefNbr;
                    fsBillHistoryRow.ChildEntityType = FSEntityType.PMRegister;
                    fsBillHistoryRow.ChildDocType = reverseDoc.Module;
                    fsBillHistoryRow.ChildRefNbr = reverseDoc.RefNbr;
                    fsBillHistoryRow.ParentEntityType = FSEntityType.PMRegister;
                    fsBillHistoryRow.ParentDocType = registerEntry.Document.Current.Module;
                    fsBillHistoryRow.ParentRefNbr = registerEntry.Document.Current.RefNbr;

                    cacheFSBillHistory.Insert(fsBillHistoryRow);

                    registerEntryExt.CleanPostingInfoLinkedToDoc(registerEntry.Document.Current);
                }

                if (inIssueEntry != null
                    && inIssueEntry.issue.Current != null)
                {
                    SM_INIssueEntry inIssueEntryExt = inIssueEntry.GetExtension<SM_INIssueEntry>();
                    INRegister reverseDoc = inIssueEntryExt.RevertInvoice();

                    FSBillHistory fsBillHistoryRow = new FSBillHistory();

                    fsBillHistoryRow.SrvOrdType = SrvOrdType;
                    fsBillHistoryRow.ServiceOrderRefNbr = ServiceOrderRefNbr;
                    fsBillHistoryRow.AppointmentRefNbr = AppointmentRefNbr;
                    fsBillHistoryRow.ChildEntityType = FSEntityType.INIssue;
                    fsBillHistoryRow.ChildDocType = reverseDoc.DocType;
                    fsBillHistoryRow.ChildRefNbr = reverseDoc.RefNbr;
                    fsBillHistoryRow.ParentEntityType = FSEntityType.INIssue;
                    fsBillHistoryRow.ParentDocType = inIssueEntry.issue.Current.DocType;
                    fsBillHistoryRow.ParentRefNbr = inIssueEntry.issue.Current.RefNbr;

                    cacheFSBillHistory.Insert(fsBillHistoryRow);

                    var requiredAllocationList = FSAllocationProcess.GetRequiredAllocationList(inIssueEntry, inIssueEntry.issue.Current);

                    FSAllocationProcess.ReallocateServiceOrderSplits(requiredAllocationList);

                    inIssueEntryExt.CleanPostingInfoLinkedToDoc(inIssueEntry.issue.Current);
                }

                cacheFSBillHistory.Persist(PXDBOperation.Insert);

                ts.Complete();
            }
        }

        public virtual void CalculateBillHistoryUnboundFields(PXCache cache, FSBillHistory fsBillHistoryRow)
        {
            using (new PXConnectionScope())
            {
                CalculateBillHistoryUnboundFieldsInt(cache, fsBillHistoryRow);
            }
        }

        public static void CalculateBillHistoryUnboundFieldsInt(PXCache cache, FSBillHistory fsBillHistoryRow)
        {
            if (fsBillHistoryRow.ChildEntityType == FSEntityType.SalesOrder)
            {
                SOOrder sOOrderRow = PXSelect<SOOrder,
                                     Where<
                                         SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
                                     And<
                                         SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>
                                     .Select(cache.Graph, fsBillHistoryRow.ChildDocType, fsBillHistoryRow.ChildRefNbr);

                if (sOOrderRow != null)
                {
                    fsBillHistoryRow.ChildDocDate = sOOrderRow.OrderDate;
                    fsBillHistoryRow.ChildDocDesc = sOOrderRow.OrderDesc;
                    fsBillHistoryRow.ChildAmount = sOOrderRow.CuryOrderTotal;
                    fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<SOOrder.status>(new PXCache<SOOrder>(cache.Graph), sOOrderRow, sOOrderRow.Status);
                }
            }
            else if (fsBillHistoryRow.ChildEntityType == FSEntityType.ARInvoice || fsBillHistoryRow.ChildEntityType == FSEntityType.ARCreditMemo)
            {
                ARInvoice arInvoiceRow = PXSelect<ARInvoice,
                                         Where<
                                             ARInvoice.docType, Equal<Required<ARInvoice.docType>>,
                                         And<
                                             ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>>
                                         .Select(cache.Graph, fsBillHistoryRow.ChildDocType, fsBillHistoryRow.ChildRefNbr);

                if (arInvoiceRow != null)
                {
                    fsBillHistoryRow.ChildDocDate = arInvoiceRow.DocDate;
                    fsBillHistoryRow.ChildDocDesc = arInvoiceRow.DocDesc;
                    fsBillHistoryRow.ChildAmount = arInvoiceRow.CuryOrigDocAmt;
                    fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<ARInvoice.status>(new PXCache<ARInvoice>(cache.Graph), arInvoiceRow, arInvoiceRow.Status);
                }
            }
            else if (fsBillHistoryRow.ChildEntityType == FSEntityType.SOInvoice || fsBillHistoryRow.ChildEntityType == FSEntityType.SOCreditMemo)
            {
                SOInvoice soInvoiceRow = PXSelect<SOInvoice,
                                         Where<
                                             SOInvoice.docType, Equal<Required<SOInvoice.docType>>,
                                         And<
                                             SOInvoice.refNbr, Equal<Required<SOInvoice.refNbr>>>>>
                                         .Select(cache.Graph, fsBillHistoryRow.ChildDocType, fsBillHistoryRow.ChildRefNbr);

                if (soInvoiceRow != null)
                {
                    fsBillHistoryRow.ChildDocDate = soInvoiceRow.DocDate;
                    fsBillHistoryRow.ChildDocDesc = soInvoiceRow.DocDesc;
                    fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<SOInvoice.status>(new PXCache<SOInvoice>(cache.Graph), soInvoiceRow, soInvoiceRow.Status);
                }
            }
            else if (fsBillHistoryRow.ChildEntityType == FSEntityType.APInvoice)
            {
                APInvoice apInvoiceRow = PXSelect<APInvoice,
                                         Where<
                                             APInvoice.docType, Equal<Required<APInvoice.docType>>,
                                         And<
                                             APInvoice.refNbr, Equal<Required<APInvoice.refNbr>>>>>
                                         .Select(cache.Graph, fsBillHistoryRow.ChildDocType, fsBillHistoryRow.ChildRefNbr);

                if (apInvoiceRow != null)
                {
                    fsBillHistoryRow.ChildDocDate = apInvoiceRow.DocDate;
                    fsBillHistoryRow.ChildDocDesc = apInvoiceRow.DocDesc;
                    fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<APInvoice.status>(new PXCache<APInvoice>(cache.Graph), apInvoiceRow, apInvoiceRow.Status);
                }
            }
            else if (fsBillHistoryRow.ChildEntityType == FSEntityType.PMRegister)
            {
                PMRegister pmRegisterRow = PXSelect<PMRegister,
                                           Where<
                                               PMRegister.module, Equal<Required<PMRegister.module>>,
                                           And<
                                               PMRegister.refNbr, Equal<Required<PMRegister.refNbr>>>>>
                                           .Select(cache.Graph, fsBillHistoryRow.ChildDocType, fsBillHistoryRow.ChildRefNbr);

                if (pmRegisterRow != null)
                {
                    fsBillHistoryRow.ChildDocDesc = pmRegisterRow.Description;
                    fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<PMRegister.status>(new PXCache<PMRegister>(cache.Graph), pmRegisterRow, pmRegisterRow.Status);
                }
            }
            else if (fsBillHistoryRow.ChildEntityType == FSEntityType.INIssue || fsBillHistoryRow.ChildEntityType == FSEntityType.INReceipt)
            {
                INRegister inRegisterRow = PXSelect<INRegister,
                                           Where<
                                               INRegister.docType, Equal<Required<INRegister.docType>>,
                                           And<
                                               INRegister.refNbr, Equal<Required<INRegister.refNbr>>>>>
                                           .Select(cache.Graph, fsBillHistoryRow.ChildDocType, fsBillHistoryRow.ChildRefNbr);

                if (inRegisterRow != null)
                {
                    fsBillHistoryRow.ChildDocDate = inRegisterRow.TranDate;
                    fsBillHistoryRow.ChildDocDesc = inRegisterRow.TranDesc;
                    fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<INRegister.status>(new PXCache<INRegister>(cache.Graph), inRegisterRow, inRegisterRow.Status);
                }
            }

            if (string.IsNullOrEmpty(fsBillHistoryRow.ChildDocStatus))
            {
                fsBillHistoryRow.ChildDocStatus = EP.Messages.Deleted;
            }
        }
        #endregion
    }
}
