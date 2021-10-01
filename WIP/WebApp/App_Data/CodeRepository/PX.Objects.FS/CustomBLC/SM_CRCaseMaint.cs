using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.EP;
using PX.Objects.IN;
using PX.SM;
using System;
using CRLocation = PX.Objects.CR.Standalone.Location;

namespace PX.Objects.FS
{
    public class SM_CRCaseMaint : PXGraphExtension<CRCaseMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();
            Base.Action.AddMenuAction(CreateServiceOrder);
            Base.Action.AddMenuAction(OpenAppointmentBoard);
            Base.Inquiry.AddMenuAction(ViewServiceOrder);
        }

        [PXHidden]
        public PXSelect<FSSetup> SetupRecord;

        [PXCopyPasteHiddenView]
        public PXFilter<FSCreateServiceOrderOnCaseFilter> CreateServiceOrderFilter;

        #region CacheAttached
        #region CRCase_ContactID
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXMergeAttributes(Method = MergeMethod.Append)]
        protected virtual void CRCase_ContactID_CacheAttached(PXCache sender)
        {
        }
        #endregion

        #region CRCase_LocationID
        [PXDefault(typeof(Coalesce<Search2<BAccountR.defLocationID,
                                   InnerJoin<CRLocation,
                                   On<
                                       CRLocation.bAccountID, Equal<BAccountR.bAccountID>,
                                       And<CRLocation.locationID, Equal<BAccountR.defLocationID>>>>,
                                   Where<
                                       BAccountR.bAccountID, Equal<Current<CRCase.customerID>>,
                                       And<CRLocation.isActive, Equal<True>,
                                           And<MatchWithBranch<CRLocation.cBranchID>>>>>,
                                   Search<CRLocation.locationID,
                                   Where<
                                       CRLocation.bAccountID, Equal<Current<CRCase.customerID>>,
                                       And<CRLocation.isActive, Equal<True>,
                                       And<MatchWithBranch<CRLocation.cBranchID>>>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
        [LocationID(typeof(Where<Location.bAccountID, Equal<Current<CRCase.customerID>>>), DescriptionField = typeof(Location.descr))]
        [PXFormula(typeof(Switch<
                                Case<Where<Current<CRCase.locationID>, IsNull, And<Current<CRCase.contractID>, IsNotNull>>,
                                        IsNull<Selector<CRCase.contractID, Selector<ContractBillingSchedule.locationID, Location.locationCD>>,
                                                     Selector<CRCase.contractID, Selector<Contract.locationID, Location.locationCD>>>,
                              Case<Where<Current<CRCase.locationID>, IsNull, And<Current<CRCase.customerID>, IsNotNull>>,
                                         Selector<CRCase.customerID, Selector<BAccount.defLocationID, Location.locationCD>>,
                                Case<Where<Current<CRCase.customerID>, IsNull>, Null>>>,
                              CRCase.locationID>))]
        [PXFormula(typeof(Default<CRCase.customerID>))]
        protected virtual void CRCase_LocationID_CacheAttached(PXCache sender)
        {
        }
        #endregion

        #endregion

        #region Actions

        public PXAction<CRCase> CreateServiceOrder;
        [PXButton]
        [PXUIField(DisplayName = "Create Service Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void createServiceOrder()
        {
            CRCase crCaseRow = Base.Case.Current;
            FSxCRCase fsxCRCaseRow = Base.Case.Cache.GetExtension<FSxCRCase>(crCaseRow);

            if (CreateServiceOrderFilter.AskExt() == WebDialogResult.OK)
            {
                fsxCRCaseRow.SDEnabled = true;
                fsxCRCaseRow.BranchLocationID = CreateServiceOrderFilter.Current.BranchLocationID;
                fsxCRCaseRow.SrvOrdType = CreateServiceOrderFilter.Current.SrvOrdType;
                fsxCRCaseRow.AssignedEmpID = CreateServiceOrderFilter.Current.AssignedEmpID;
                fsxCRCaseRow.ProblemID = CreateServiceOrderFilter.Current.ProblemID;

                PXLongOperation.StartOperation(Base, delegate ()
                {
                    CreateServiceOrderDocument(Base, crCaseRow, CreateServiceOrderFilter.Current);
                });
            }
        }

        public PXAction<CRCase> OpenAppointmentBoard;
        [PXButton]
        [PXUIField(DisplayName = "Schedule on the Calendar Board", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void openAppointmentBoard()
        {
            if (Base.Case == null || Base.Case.Current == null)
            {
                return;
            }

            if (Base.IsDirty)
            {
                Base.Save.Press();
            }

            FSxCRCase fsxCRCaseRow = Base.Case.Cache.GetExtension<FSxCRCase>(Base.Case.Current);

            if (fsxCRCaseRow != null && fsxCRCaseRow.SOID != null)
            {
                CRExtensionHelper.LaunchEmployeeBoard(Base, fsxCRCaseRow.SOID);
            }
        }

        public PXAction<CRCase> ViewServiceOrder;
        [PXButton]
        [PXUIField(DisplayName = "View Service Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void viewServiceOrder()
        {
            if (Base.Case == null || Base.Case.Current == null)
            {
                return;
            }

            if (Base.IsDirty)
            {
                Base.Save.Press();
            }

            FSxCRCase fsxCRCaseRow = Base.Case.Cache.GetExtension<FSxCRCase>(Base.Case.Current);

            if (fsxCRCaseRow != null && fsxCRCaseRow.SOID != null)
            {
                CRExtensionHelper.LaunchServiceOrderScreen(Base, fsxCRCaseRow.SOID);
            }
        }

        #endregion

        #region Virtual Methods

        private FSSetup GetFSSetup()
        {
            if (SetupRecord.Current == null)
            {
                return SetupRecord.Select();
            }
            else
            {
                return SetupRecord.Current;
            }
        }

        public virtual void SetPersistingChecks(PXCache cache,
                                                CRCase crCaseRow,
                                                FSxCRCase fsxCRCaseRow,
                                                FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsxCRCaseRow.SDEnabled == true)
            {
                PXDefaultAttribute.SetPersistingCheck<FSxCRCase.srvOrdType>(cache, crCaseRow, PXPersistingCheck.NullOrBlank);
                PXDefaultAttribute.SetPersistingCheck<FSxCRCase.branchLocationID>(cache, crCaseRow, PXPersistingCheck.NullOrBlank);

                if (fsSrvOrdTypeRow == null)
                {
                    fsSrvOrdTypeRow = CRExtensionHelper.GetServiceOrderType(Base, fsxCRCaseRow.SrvOrdType);
                }

                if (fsSrvOrdTypeRow != null
                        && fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
                {
                    PXDefaultAttribute.SetPersistingCheck<CROpportunity.bAccountID>(cache, crCaseRow, PXPersistingCheck.NullOrBlank);
                    if (PXAccess.FeatureInstalled<FeaturesSet.accountLocations>() == true)
                    {
                        PXDefaultAttribute.SetPersistingCheck<CROpportunity.locationID>(cache, crCaseRow, PXPersistingCheck.NullOrBlank);
                    }
                }
            }
            else
            {
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.srvOrdType>(cache, crCaseRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.branchLocationID>(cache, crCaseRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<CROpportunity.bAccountID>(cache, crCaseRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<CROpportunity.locationID>(cache, crCaseRow, PXPersistingCheck.Nothing);
            }
        }

        public virtual void EnableDisableCustomFields(PXCache cache,
                                                      CRCase crCaseRow,
                                                      FSxCRCase fsxCRCaseRow,
                                                      FSServiceOrder fsServiceOrderRow,
                                                      FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsSrvOrdTypeRow != null)
            {
                bool isAnInternalSrvOrdType = fsSrvOrdTypeRow.Behavior == FSSrvOrdType.behavior.Values.InternalAppointment;

                PXUIFieldAttribute.SetEnabled<CRCase.customerID>(cache, null, isAnInternalSrvOrdType || (!isAnInternalSrvOrdType && fsServiceOrderRow == null));
            }
            else
            {
                PXUIFieldAttribute.SetEnabled<CRCase.customerID>(cache, null, true);
            }
        }

        public virtual void EnableDisableActions(PXCache cache,
                                                 CRCase crCaseRow,
                                                 FSxCRCase fsxCRCaseRow,
                                                 FSServiceOrder fsServiceOrderRow,
                                                 FSSrvOrdType fsSrvOrdTypeRow)
        {
            bool isSMSetup = GetFSSetup() != null;

            CreateServiceOrder.SetEnabled(isSMSetup && crCaseRow != null && !string.IsNullOrWhiteSpace(crCaseRow.CaseCD) && fsServiceOrderRow == null);
            ViewServiceOrder.SetEnabled(isSMSetup && crCaseRow != null && !string.IsNullOrWhiteSpace(crCaseRow.CaseCD) && fsServiceOrderRow != null);
            OpenAppointmentBoard.SetEnabled(fsServiceOrderRow != null);
        }

        public virtual void SetBranchLocationID(PXGraph graph, FSxCRCase fsxCaseRow)
        {
            UserPreferences userPreferencesRow = PXSelect<UserPreferences,
                                                 Where<
                                                     UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>>>
                                                 .Select(graph);

            if (userPreferencesRow != null)
            {
                FSxUserPreferences fsxUserPreferencesRow = PXCache<UserPreferences>.GetExtension<FSxUserPreferences>(userPreferencesRow);

                if (fsxUserPreferencesRow != null)
                {
                    fsxCaseRow.BranchLocationID = fsxUserPreferencesRow.DfltBranchLocationID;
                }
            }
            else
            {
                fsxCaseRow.BranchLocationID = null;
            }
        }

        public virtual void CreateServiceOrderDocument(CRCaseMaint graphCRCaseMaint, CRCase crCaseRow, FSCreateServiceOrderOnCaseFilter fsCreateServiceOrderOnCaseFilterRow)
        {
            if (graphCRCaseMaint == null
                    || crCaseRow == null
                        || fsCreateServiceOrderOnCaseFilterRow == null)
            {
                return;
            }

            ServiceOrderEntry graphServiceOrderEntry = PXGraph.CreateInstance<ServiceOrderEntry>();

            FSServiceOrder newServiceOrderRow = CRExtensionHelper.InitNewServiceOrder(CreateServiceOrderFilter.Current.SrvOrdType, ID.SourceType_ServiceOrder.CASE);

            graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords.Insert(newServiceOrderRow);

            UpdateServiceOrderHeader(graphServiceOrderEntry,
                                    Base.Case.Cache,
                                    crCaseRow,
                                    fsCreateServiceOrderOnCaseFilterRow,
                                    graphServiceOrderEntry.ServiceOrderRecords.Current,
                                    true);

            graphServiceOrderEntry.ServiceOrderRecords.Current.SourceRefNbr = crCaseRow.CaseCD;

            if (!Base.IsContractBasedAPI)
            {
                throw new PXRedirectRequiredException(graphServiceOrderEntry, null);
            }
        }

        public virtual void UpdateServiceOrderHeader(ServiceOrderEntry graphServiceOrderEntry,
                                                    PXCache cache,
                                                    CRCase crCaseRow,
                                                    FSCreateServiceOrderOnCaseFilter fsCreateServiceOrderOnCaseFilterRow,
                                                    FSServiceOrder fsServiceOrderRow,
                                                    bool updatingExistingSO)
        {
            if (fsServiceOrderRow.Closed == true)
            {
                return;
            }

            bool somethingChanged = false;

            FSSrvOrdType fsSrvOrdTypeRow = CRExtensionHelper.GetServiceOrderType(graphServiceOrderEntry, fsServiceOrderRow.SrvOrdType);

            if (fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
            {
                if (fsServiceOrderRow.CustomerID != crCaseRow.CustomerID)
                {
                    graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.customerID>(fsServiceOrderRow, crCaseRow.CustomerID);
                    somethingChanged = true;
                }

                if (fsServiceOrderRow.LocationID != crCaseRow.LocationID)
                {
                    graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.locationID>(fsServiceOrderRow, crCaseRow.LocationID);
                    somethingChanged = true;
                }
            }

            if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
            {
                Customer customer = (Customer)PXSelect<Customer,
                                                Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                            .Select(graphServiceOrderEntry, crCaseRow.CustomerID);

                if (customer != null
                    && fsServiceOrderRow.CuryID != customer.CuryID)
                {
                    graphServiceOrderEntry.ServiceOrderRecords.Cache.SetValueExt<FSServiceOrder.curyID>(fsServiceOrderRow, customer.CuryID);
                    somethingChanged = true;
                }
            }

            if (fsServiceOrderRow.BranchLocationID != fsCreateServiceOrderOnCaseFilterRow.BranchLocationID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.branchLocationID>(fsServiceOrderRow, fsCreateServiceOrderOnCaseFilterRow.BranchLocationID);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.ContactID != crCaseRow.ContactID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.contactID>(fsServiceOrderRow, crCaseRow.ContactID);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.DocDesc != crCaseRow.Subject)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.docDesc>(fsServiceOrderRow, crCaseRow.Subject);
                somethingChanged = true;
            }

            if (crCaseRow.OwnerID != null)
            {
                if (crCaseRow.OwnerID != (int?)cache.GetValueOriginal<CROpportunity.ownerID>(crCaseRow))
                {
                    int? salesPersonID = CRExtensionHelper.GetSalesPersonID(graphServiceOrderEntry, crCaseRow.OwnerID);

                    if (salesPersonID != null)
                    {
                        graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.salesPersonID>(fsServiceOrderRow, salesPersonID);
                        somethingChanged = true;
                    }
                }
            }

            if (crCaseRow.CreatedDateTime.HasValue
                    && fsServiceOrderRow.OrderDate != crCaseRow.CreatedDateTime.Value.Date)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.orderDate>(fsServiceOrderRow, crCaseRow.CreatedDateTime.Value.Date);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.SLAETA != crCaseRow.SLAETA)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.sLAETA>(fsServiceOrderRow, crCaseRow.SLAETA);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.AssignedEmpID != fsCreateServiceOrderOnCaseFilterRow.AssignedEmpID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.assignedEmpID>(fsServiceOrderRow, fsCreateServiceOrderOnCaseFilterRow.AssignedEmpID);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.ProblemID != fsCreateServiceOrderOnCaseFilterRow.ProblemID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.problemID>(fsServiceOrderRow, fsCreateServiceOrderOnCaseFilterRow.ProblemID);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.LongDescr != crCaseRow.Description)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.longDescr>(fsServiceOrderRow, crCaseRow.Description);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.Priority != crCaseRow.Priority)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.priority>(fsServiceOrderRow, crCaseRow.Priority);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.Severity != crCaseRow.Severity)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.severity>(fsServiceOrderRow, crCaseRow.Severity);
                somethingChanged = true;
            }

            if (somethingChanged && updatingExistingSO)
            {
                graphServiceOrderEntry.ServiceOrderRecords.Update(fsServiceOrderRow);
            }
        }

        #endregion

        #region Events

        #region CRCase
        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated
        protected virtual void _(Events.FieldUpdated<CRCase, FSxCRCase.sDEnabled> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CRCase crCaseRow = (CRCase)e.Row;
            FSxCRCase fsxCRCaseRow = e.Cache.GetExtension<FSxCRCase>(crCaseRow);

            if (fsxCRCaseRow.SDEnabled == true)
            {
                FSSetup fsSetupRow = GetFSSetup();

                if (fsSetupRow != null
                        && fsSetupRow.DfltCasesSrvOrdType != null)
                {
                    fsxCRCaseRow.SrvOrdType = fsSetupRow.DfltCasesSrvOrdType;
                }

                SetBranchLocationID(Base, fsxCRCaseRow);
            }
            else
            {
                fsxCRCaseRow.SrvOrdType = null;
                fsxCRCaseRow.BranchLocationID = null;
            }
        }

        #endregion

        protected virtual void _(Events.RowSelecting<CRCase> e)
        {
        }

        protected virtual void _(Events.RowSelected<CRCase> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CRCase crCaseRow = (CRCase)e.Row;
            PXCache cache = e.Cache;

            FSxCRCase fsxCRCaseRow = cache.GetExtension<FSxCRCase>(crCaseRow);

            FSServiceOrder fsServiceOrderRow = CRExtensionHelper.GetRelatedServiceOrder(Base, cache, crCaseRow, fsxCRCaseRow.SOID);
            FSSrvOrdType fsSrvOrdTypeRow = null;

            if (fsServiceOrderRow != null)
            {
                fsSrvOrdTypeRow = CRExtensionHelper.GetServiceOrderType(Base, fsServiceOrderRow.SrvOrdType);
            }

            EnableDisableCustomFields(cache, crCaseRow, fsxCRCaseRow, fsServiceOrderRow, fsSrvOrdTypeRow);
            EnableDisableActions(cache, crCaseRow, fsxCRCaseRow, fsServiceOrderRow, fsSrvOrdTypeRow);
            SetPersistingChecks(cache, crCaseRow, fsxCRCaseRow, fsSrvOrdTypeRow);
        }

        protected virtual void _(Events.RowInserting<CRCase> e)
        {
        }

        protected virtual void _(Events.RowInserted<CRCase> e)
        {
        }

        protected virtual void _(Events.RowUpdating<CRCase> e)
        {
        }

        protected virtual void _(Events.RowUpdated<CRCase> e)
        {
        }

        protected virtual void _(Events.RowDeleting<CRCase> e)
        {
        }

        protected virtual void _(Events.RowDeleted<CRCase> e)
        {
        }

        protected virtual void _(Events.RowPersisting<CRCase> e)
        {
        }

        protected virtual void _(Events.RowPersisted<CRCase> e)
        {
        }

        #endregion

        #endregion
    }
}
