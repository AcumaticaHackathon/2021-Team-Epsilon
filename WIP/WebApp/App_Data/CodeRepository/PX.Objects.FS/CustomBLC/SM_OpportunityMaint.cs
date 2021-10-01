using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CM.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.IN;
using PX.SM;
using System;
using System.Collections;
using System.Linq;
using PX.Objects.CR.Workflows;

namespace PX.Objects.FS
{
    public class SM_OpportunityMaint : PXGraphExtension<CR.OpportunityMaint.Discount, OpportunityMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();
            InqueriesMenu.AddMenuAction(ViewServiceOrder);
        }

        [PXHidden]
        public PXSelect<FSSetup> SetupRecord;

        [PXHidden]
        public PXSelect<CRSetup> CRSetupRecord;

        public PXSetup<FSSrvOrdType>.Where<
               Where<
                   FSSrvOrdType.srvOrdType, Equal<Optional<FSxCROpportunity.srvOrdType>>>> ServiceOrderTypeSelected;

        [PXCopyPasteHiddenView]
        public PXFilter<FSCreateServiceOrderFilter> CreateServiceOrderFilter;

        #region CacheAttached

        #region CROpportunityProducts_CuryUnitPrice
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXDBCurrency(typeof(CommonSetup.decPlPrcCst), typeof(CROpportunityProducts.curyInfoID), typeof(CROpportunityProducts.unitPrice))]
        [PXUIField(DisplayName = "Unit Price", Visibility = PXUIVisibility.SelectorVisible)]
        [PXFormula(typeof(Switch<
                                Case<
                                    Where<
                                        Current<CROpportunityProducts.stockItemType>, Equal<INItemTypes.serviceItem>,
                                        And<
                                            FSxCROpportunityProducts.billingRule, Equal<FSxCROpportunityProducts.billingRule.None>>>,
                                    SharedClasses.decimal_0>,
                                CROpportunityProducts.curyUnitPrice>))]
        public virtual void CROpportunityProducts_CuryUnitPrice_CacheAttached(PXCache sender) { }
        #endregion

        #region CROpportunityProducts_CuryExtPrice
        [PXDBCurrency(typeof(CROpportunityProducts.curyInfoID), typeof(CROpportunityProducts.extPrice))]
        [PXUIField(DisplayName = "Ext. Price")]
        [PXFormula(typeof(Switch<
                                Case<
                                    Where<
                                        Current<CROpportunityProducts.stockItemType>, Equal<INItemTypes.serviceItem>,
                                        And<
                                            FSxCROpportunityProducts.billingRule, Equal<FSxCROpportunityProducts.billingRule.None>>>,
                                    SharedClasses.decimal_0>,
                                Mult<CROpportunityProducts.curyUnitPrice, CROpportunityProducts.quantity>>))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual void CROpportunityProducts_CuryExtPrice_CacheAttached(PXCache sender) { }
        #endregion
        #region CROpportunityProducts_UnitCost
        [PXDBPriceCost()]
        [PXDefault(TypeCode.Decimal, "0.0", typeof(Coalesce<
                                                        Search2<INItemSite.tranUnitCost,
                                                            InnerJoin<InventoryItem,
                                                                    On<InventoryItem.inventoryID, Equal<INItemSite.inventoryID>>>,
                                                            Where<INItemSite.inventoryID, Equal<Current<CROpportunityProducts.inventoryID>>,
                                                                And<INItemSite.siteID, Equal<Current<CROpportunityProducts.siteID>>,
                                                                    And<InventoryItem.stkItem, Equal<True>>>>>,
                                                        Search<InventoryItem.stdCost,
                                                            Where<InventoryItem.inventoryID, Equal<Current<CROpportunityProducts.inventoryID>>,
                                                                And<InventoryItem.stkItem, Equal<False>>>>>))]
        public virtual void CROpportunityProducts_UnitCost_CacheAttached(PXCache sender) { }
        #endregion
        #region CROpportunityProducts_CuryUnitCost
        [PXDBCurrency(typeof(Search<CommonSetup.decPlPrcCst>), typeof(CROpportunityProducts.curyInfoID), typeof(CROpportunityProducts.unitCost))]
        [PXUIField(DisplayName = "Unit Cost")]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXFormula(typeof(Default<CROpportunityProducts.pOCreate>))]
        public virtual void CROpportunityProducts_CuryUnitCost_CacheAttached(PXCache sender) { }
        #endregion

        #region FSCreateServiceOrderFilter_SrvOrdType
        #region SrvOrdType
        [PXDefault(typeof(Coalesce<
            Search<FSxUserPreferences.dfltSrvOrdType,
            Where<
                PX.SM.UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>>>,
            Search<FSSetup.dfltSrvOrdType>>), PersistingCheck = PXPersistingCheck.NullOrBlank)]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        protected virtual void FSCreateServiceOrderFilter_SrvOrdType_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion
        #endregion

        #region Workflow

        public override void Configure(PXScreenConfiguration configuration)
        {
            configuration
                .GetScreenConfigurationContext<OpportunityMaint, CROpportunity>()
                .UpdateScreenConfigurationFor(config => config
                    .WithActions(a =>
                    {
                        a.Add<SM_OpportunityMaint>(e => e.CreateServiceOrder, c => c.InFolder(FolderType.ActionsFolder).WithPersistOptions(ActionPersistOptions.ForcePersist));
                        a.Add<SM_OpportunityMaint>(e => e.OpenAppointmentBoard, c => c.InFolder(FolderType.ActionsFolder).WithPersistOptions(ActionPersistOptions.ForcePersist));
                    })
                    .UpdateDefaultFlow(flow =>
                    {
                        return flow.WithFlowStates(states =>
                        {
                            states.Update(OpportunityWorkflow.States.New,
                                state => state.WithActions(actions =>
                                {
                                    actions.Add<SM_OpportunityMaint>(e => e.CreateServiceOrder);
                                }));
                            states.Update(OpportunityWorkflow.States.Open,
                                state => state.WithActions(actions =>
                                {
                                    actions.Add<SM_OpportunityMaint>(e => e.CreateServiceOrder);
                                }));
                            states.Update(OpportunityWorkflow.States.Won,
                                state => state.WithActions(actions =>
                                {
                                    actions.Add<SM_OpportunityMaint>(e => e.CreateServiceOrder);
                                })
                            );
                        });
                    })
                );
        }

        #endregion

        #region Methods

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

        private CRSetup GetCRSetup()
        {
            if (CRSetupRecord.Current == null)
            {
                return CRSetupRecord.Select();
            }
            else
            {
                return CRSetupRecord.Current;
            }
        }

        public virtual void EnableDisableExtensionFields(PXCache cache, FSxCROpportunity fsxCROpportunityRow, FSServiceOrder fsServiceOrderRow)
        {
            if (fsxCROpportunityRow == null)
                return;

            bool isSMSetup = GetFSSetup() != null;

            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.sDEnabled>(cache, null, isSMSetup && fsServiceOrderRow == null);
            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.srvOrdType>(cache, null, isSMSetup && fsxCROpportunityRow.SDEnabled == true && fsServiceOrderRow == null);
            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.branchLocationID>(cache, null, isSMSetup && fsxCROpportunityRow.SDEnabled == true);
        }

        [Obsolete("Remove in major release")]
        public virtual void EnableDisableCustomFields(PXCache cache,
                                                      CROpportunity crOpportunityRow,
                                                      FSxCROpportunity fsxCROpportunityRow,
                                                      FSServiceOrder fsServiceOrderRow,
                                                      FSSrvOrdType fsSrvOrdTypeRow)
        {
            bool isSMSetup = GetFSSetup() != null;

            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.sDEnabled>(cache, null, isSMSetup && fsServiceOrderRow == null);
            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.srvOrdType>(cache, null, isSMSetup && fsxCROpportunityRow.SDEnabled == true && fsServiceOrderRow == null);
            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.branchLocationID>(cache, null, isSMSetup && fsxCROpportunityRow.SDEnabled == true);
        }

        public virtual void EnableDisableActions(PXCache cache,
                                                 CROpportunity crOpportunityRow,
                                                 FSxCROpportunity fsxCROpportunityRow,
                                                 FSServiceOrder fsServiceOrderRow,
                                                 FSSrvOrdType fsSrvOrdTypeRow)
        {
            bool isSMSetup = GetFSSetup() != null;
            bool insertedStatus = Base.Opportunity.Cache.GetStatus(Base.Opportunity.Current) == PXEntryStatus.Inserted;

            CreateServiceOrder.SetEnabled(isSMSetup && crOpportunityRow != null && insertedStatus == false && fsServiceOrderRow == null);
            ViewServiceOrder.SetEnabled(isSMSetup && crOpportunityRow != null && crOpportunityRow.OpportunityID != null && fsServiceOrderRow != null);
            OpenAppointmentBoard.SetEnabled(fsServiceOrderRow != null);

            if (fsServiceOrderRow != null)
            {
                Base.createSalesOrder.SetEnabled(false);
                Base.createInvoice.SetEnabled(false);
            }
        }

        public virtual void SetPersistingChecks(PXCache cache,
                                                CROpportunity crOpportunityRow,
                                                FSxCROpportunity fsxCROpportunityRow,
                                                FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsxCROpportunityRow.SDEnabled == true)
            {
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.srvOrdType>(cache, crOpportunityRow, PXPersistingCheck.NullOrBlank);
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.branchLocationID>(cache, crOpportunityRow, PXPersistingCheck.NullOrBlank);

                if (fsSrvOrdTypeRow == null)
                {
                    fsSrvOrdTypeRow = CRExtensionHelper.GetServiceOrderType(Base, fsxCROpportunityRow.SrvOrdType);
                }

                if (fsSrvOrdTypeRow != null
                        && fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
                {
                    PXDefaultAttribute.SetPersistingCheck<CROpportunity.bAccountID>(cache, crOpportunityRow, PXPersistingCheck.NullOrBlank);
                    PXDefaultAttribute.SetPersistingCheck<CROpportunity.locationID>(cache, crOpportunityRow, PXPersistingCheck.NullOrBlank);
                }
            }
            else
            {
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.srvOrdType>(cache, crOpportunityRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.branchLocationID>(cache, crOpportunityRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<CROpportunity.bAccountID>(cache, crOpportunityRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<CROpportunity.locationID>(cache, crOpportunityRow, PXPersistingCheck.Nothing);
            }
        }

        public virtual void SetBranchLocationID(PXGraph graph, CROpportunity crOpportunityRow, FSxCROpportunity fsxCROpportunityRow)
        {
            if (crOpportunityRow.BranchID != null)
            {
                UserPreferences userPreferencesRow =
                    PXSelect<UserPreferences,
                    Where<
                        UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>>>.Select(graph);

                if (userPreferencesRow != null
                        && userPreferencesRow.DefBranchID == crOpportunityRow.BranchID)
                {
                    FSxUserPreferences fsxUserPreferencesRow = PXCache<UserPreferences>.GetExtension<FSxUserPreferences>(userPreferencesRow);

                    if (fsxUserPreferencesRow != null)
                    {
                        fsxCROpportunityRow.BranchLocationID = fsxUserPreferencesRow.DfltBranchLocationID;
                    }
                }
                else
                {
                    fsxCROpportunityRow.BranchLocationID = null;
                }
            }
            else
            {
                fsxCROpportunityRow.BranchLocationID = null;
            }
        }

        public virtual void CreateServiceOrderDocument(OpportunityMaint graphOpportunityMaint, CROpportunity crOpportunityRow, FSCreateServiceOrderFilter fsCreateServiceOrderFilterRow)
        {
            if(graphOpportunityMaint == null
                    ||  crOpportunityRow == null
                        || fsCreateServiceOrderFilterRow == null)
            {
                return;
            }

            ServiceOrderEntry graphServiceOrderEntry = PXGraph.CreateInstance<ServiceOrderEntry>();

            FSServiceOrder newServiceOrderRow = CRExtensionHelper.InitNewServiceOrder(fsCreateServiceOrderFilterRow.SrvOrdType, ID.SourceType_ServiceOrder.OPPORTUNITY);

            CRSetup crSetupRow = GetCRSetup();

            graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords.Insert(newServiceOrderRow);

            UpdateServiceOrderHeader(graphServiceOrderEntry,
                                    Base.Opportunity.Cache,
                                    crOpportunityRow,
                                    fsCreateServiceOrderFilterRow,
                                    graphServiceOrderEntry.ServiceOrderRecords.Current,
                                    Base.Opportunity_Contact.Current,
                                    Base.Opportunity_Address.Current,
                                    true);

            SharedFunctions.CopyNotesAndFiles(Base.Opportunity.Cache,
                                              graphServiceOrderEntry.ServiceOrderRecords.Cache,
                                              crOpportunityRow, graphServiceOrderEntry.ServiceOrderRecords.Current,
                                              crSetupRow.CopyNotes,
                                              crSetupRow.CopyFiles);

            foreach (CROpportunityProducts crOpportunityProductsRow in graphOpportunityMaint.Products.Select())
            {
                if(crOpportunityProductsRow.InventoryID != null)
                { 
                    InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(graphServiceOrderEntry, crOpportunityProductsRow.InventoryID);

                    if (inventoryItemRow.StkItem == true
                            && graphServiceOrderEntry.ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.ACCOUNTS_RECEIVABLE_MODULE)
                    {
                        throw new PXException(TX.Error.STOCKITEM_NOT_HANDLED_BY_SRVORDTYPE, inventoryItemRow.InventoryCD);
                    }

                    FSxCROpportunityProducts fsxCROpportunityProductsRow = graphOpportunityMaint.Products.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);
                    InsertFSSODetFromOpportunity(graphServiceOrderEntry, graphOpportunityMaint.Products.Cache, crSetupRow, crOpportunityProductsRow, fsxCROpportunityProductsRow, inventoryItemRow);
                }
            }

            graphServiceOrderEntry.ServiceOrderRecords.Current.SourceRefNbr = crOpportunityRow.OpportunityID;

            if (!Base.IsContractBasedAPI)
            {
                throw new PXRedirectRequiredException(graphServiceOrderEntry, null);
            }
        }

        public virtual void HideOrShowFieldsActionsByInventoryFeature()
        {
            bool isInventoryFeatureInstalled = PXAccess.FeatureInstalled<FeaturesSet.inventory>();

            PXUIVisibility visibility = isInventoryFeatureInstalled ? PXUIVisibility.Visible : PXUIVisibility.Invisible;

            PXUIFieldAttribute.SetVisibility<CROpportunityProducts.pOCreate>(Base.Products.Cache, null, visibility);
            PXUIFieldAttribute.SetVisibility<CROpportunityProducts.vendorID>(Base.Products.Cache, null, visibility);
            PXUIFieldAttribute.SetVisibility<CROpportunityProducts.curyUnitCost>(Base.Products.Cache, null, visibility);
            PXUIFieldAttribute.SetVisibility<FSxCROpportunityProducts.vendorLocationID>(Base.Products.Cache, null, visibility);

            PXUIFieldAttribute.SetVisible<CROpportunityProducts.pOCreate>(Base.Products.Cache, null, isInventoryFeatureInstalled);
            PXUIFieldAttribute.SetVisible<CROpportunityProducts.vendorID>(Base.Products.Cache, null, isInventoryFeatureInstalled);
            PXUIFieldAttribute.SetVisible<CROpportunityProducts.curyUnitCost>(Base.Products.Cache, null, isInventoryFeatureInstalled);
            PXUIFieldAttribute.SetVisible<FSxCROpportunityProducts.vendorLocationID>(Base.Products.Cache, null, isInventoryFeatureInstalled);
        }

        public virtual void UpdateServiceOrderHeader(ServiceOrderEntry graphServiceOrderEntry,
                                                    PXCache cache,
                                                    CROpportunity crOpportunityRow,
                                                    FSCreateServiceOrderFilter fsCreateServiceOrderOnOpportunityFilterRow,
                                                    FSServiceOrder fsServiceOrderRow,
                                                    CRContact crContactRow,
                                                    CRAddress crAddressRow,
                                                    bool updatingExistingSO)
        {
            bool somethingChanged = false;

            FSSrvOrdType fsSrvOrdTypeRow = CRExtensionHelper.GetServiceOrderType(graphServiceOrderEntry, fsServiceOrderRow.SrvOrdType);

            if (fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
            {
                if (fsServiceOrderRow.CustomerID != crOpportunityRow.BAccountID)
                {
                    graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.customerID>(fsServiceOrderRow, crOpportunityRow.BAccountID);
                    somethingChanged = true;
                }

                if (fsServiceOrderRow.LocationID != crOpportunityRow.LocationID)
                {
                    graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.locationID>(fsServiceOrderRow, crOpportunityRow.LocationID);
                    somethingChanged = true;
                }
            }

            if (fsServiceOrderRow.CuryID != crOpportunityRow.CuryID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.curyID>(fsServiceOrderRow, crOpportunityRow.CuryID);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.BranchID != crOpportunityRow.BranchID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.branchID>(fsServiceOrderRow, crOpportunityRow.BranchID);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.BranchLocationID != fsCreateServiceOrderOnOpportunityFilterRow.BranchLocationID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.branchLocationID>(fsServiceOrderRow, fsCreateServiceOrderOnOpportunityFilterRow.BranchLocationID);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.ContactID != crOpportunityRow.ContactID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.contactID>(fsServiceOrderRow, crOpportunityRow.ContactID);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.DocDesc != crOpportunityRow.Subject)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.docDesc>(fsServiceOrderRow, crOpportunityRow.Subject);
                somethingChanged = true;
            }

            if (fsServiceOrderRow.ProjectID != crOpportunityRow.ProjectID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.projectID>(fsServiceOrderRow, crOpportunityRow.ProjectID);
                somethingChanged = true;
            }

            if (crOpportunityRow.OwnerID != null)
            {
                if (crOpportunityRow.OwnerID != (int?)cache.GetValueOriginal<CROpportunity.ownerID>(crOpportunityRow))
                {
                    int? salesPersonID = CRExtensionHelper.GetSalesPersonID(graphServiceOrderEntry, crOpportunityRow.OwnerID);

                    if (salesPersonID != null)
                    {
                        graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.salesPersonID>(fsServiceOrderRow, salesPersonID);
                        somethingChanged = true;
                    }
                }
            }

            if (fsServiceOrderRow.OrderDate != crOpportunityRow.CloseDate)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.orderDate>(fsServiceOrderRow, crOpportunityRow.CloseDate);
                somethingChanged = true;
            }

            FSAddress fsAddressRow = graphServiceOrderEntry.ServiceOrder_Address.Select();
            FSContact fsContactRow = graphServiceOrderEntry.ServiceOrder_Contact.Select();

            ApplyChangesfromContactInfo(graphServiceOrderEntry, crContactRow, fsContactRow, ref somethingChanged);
            ApplyChangesfromAddressInfo(graphServiceOrderEntry, crAddressRow, fsAddressRow, ref somethingChanged);

            if (fsServiceOrderRow.TaxZoneID != crOpportunityRow.TaxZoneID)
            {
                graphServiceOrderEntry.ServiceOrderRecords.SetValueExt<FSServiceOrder.taxZoneID>(fsServiceOrderRow, crOpportunityRow.TaxZoneID);
                somethingChanged = true;
            }

            if (somethingChanged && updatingExistingSO)
            {
                graphServiceOrderEntry.ServiceOrderRecords.Update(fsServiceOrderRow);
            }
        }

        public virtual void ApplyChangesfromAddressInfo(ServiceOrderEntry graphServiceOrderEntry, CRAddress crAddressRow, FSAddress fsAddressRow, ref bool somethingChanged)
        {
            if (crAddressRow == null)
            {
                return;
            }

            if (fsAddressRow.AddressLine1 != crAddressRow.AddressLine1)
            {
                graphServiceOrderEntry.ServiceOrder_Address.SetValueExt<FSAddress.addressLine1>(fsAddressRow, crAddressRow.AddressLine1);
                somethingChanged = true;
            }

            if (fsAddressRow.AddressLine2 != crAddressRow.AddressLine2)
            {
                graphServiceOrderEntry.ServiceOrder_Address.SetValueExt<FSAddress.addressLine2>(fsAddressRow, crAddressRow.AddressLine2);
                somethingChanged = true;
            }

            if (fsAddressRow.AddressLine3 != crAddressRow.AddressLine3)
            {
                graphServiceOrderEntry.ServiceOrder_Address.SetValueExt<FSAddress.addressLine3>(fsAddressRow, crAddressRow.AddressLine3);
                somethingChanged = true;
            }

            if (fsAddressRow.City != crAddressRow.City)
            {
                graphServiceOrderEntry.ServiceOrder_Address.SetValueExt<FSAddress.city>(fsAddressRow, crAddressRow.City);
                somethingChanged = true;
            }

            if (fsAddressRow.CountryID != crAddressRow.CountryID)
            {
                graphServiceOrderEntry.ServiceOrder_Address.SetValueExt<FSAddress.countryID>(fsAddressRow, crAddressRow.CountryID);
                somethingChanged = true;
            }

            if (fsAddressRow.State != crAddressRow.State)
            {
                graphServiceOrderEntry.ServiceOrder_Address.SetValueExt<FSAddress.state>(fsAddressRow, crAddressRow.State);
                somethingChanged = true;
            }

            if (fsAddressRow.PostalCode != crAddressRow.PostalCode)
            {
                graphServiceOrderEntry.ServiceOrder_Address.SetValueExt<FSAddress.postalCode>(fsAddressRow, crAddressRow.PostalCode);
                somethingChanged = true;
            }
        }

        public virtual void ApplyChangesfromContactInfo(ServiceOrderEntry graphServiceOrderEntry, CRContact crContactRow, FSContact fsContactRow, ref bool somethingChanged)
        {
            if (crContactRow == null)
            {
                return;
            }

            if (fsContactRow.Title != crContactRow.Title)
            {
                graphServiceOrderEntry.ServiceOrder_Contact.SetValueExt<FSContact.title>(fsContactRow, crContactRow.Title);
                somethingChanged = true;
            }

            if (fsContactRow.Attention != crContactRow.Attention)
            {
                graphServiceOrderEntry.ServiceOrder_Contact.SetValueExt<FSContact.attention>(fsContactRow, crContactRow.Attention);
                somethingChanged = true;
            }

            if (fsContactRow.Email != crContactRow.Email)
            {
                graphServiceOrderEntry.ServiceOrder_Contact.SetValueExt<FSContact.email>(fsContactRow, crContactRow.Email);
                somethingChanged = true;
            }

            if (fsContactRow.Phone1 != crContactRow.Phone1)
            {
                graphServiceOrderEntry.ServiceOrder_Contact.SetValueExt<FSContact.phone1>(fsContactRow, crContactRow.Phone1);
                somethingChanged = true;
            }

            if (fsContactRow.Phone2 != crContactRow.Phone2)
            {
                graphServiceOrderEntry.ServiceOrder_Contact.SetValueExt<FSContact.phone2>(fsContactRow, crContactRow.Phone2);
                somethingChanged = true;
            }

            if (fsContactRow.Phone3 != crContactRow.Phone3)
            {
                graphServiceOrderEntry.ServiceOrder_Contact.SetValueExt<FSContact.phone3>(fsContactRow, crContactRow.Phone3);
                somethingChanged = true;
            }

            if (fsContactRow.Fax != crContactRow.Fax)
            {
                graphServiceOrderEntry.ServiceOrder_Contact.SetValueExt<FSContact.fax>(fsContactRow, crContactRow.Fax);
                somethingChanged = true;
            }
        }

        public virtual void InsertFSSODetFromOpportunity(ServiceOrderEntry graphServiceOrder,
                                                        PXCache cacheOpportunityProducts,
                                                        CRSetup crSetupRow,
                                                        CROpportunityProducts crOpportunityProductRow,
                                                        FSxCROpportunityProducts fsxCROpportunityProductsRow,
                                                        InventoryItem inventoryItemRow)
        {
            if (graphServiceOrder == null
                    || crOpportunityProductRow == null
                    || fsxCROpportunityProductsRow == null
                    || inventoryItemRow == null)
            {
                return;
            }

            //Insert a new SODet line
            FSSODet fsSODetRow = new FSSODet();

            UpdateFSSODetFromOpportunity(graphServiceOrder.ServiceOrderDetails.Cache,
                                         fsSODetRow,
                                         crOpportunityProductRow,
                                         fsxCROpportunityProductsRow,
                                         GetLineTypeFromInventoryItem(inventoryItemRow));

            SharedFunctions.CopyNotesAndFiles(cacheOpportunityProducts,
                                                graphServiceOrder.ServiceOrderDetails.Cache,
                                                crOpportunityProductRow, graphServiceOrder.ServiceOrderDetails.Current,
                                                crSetupRow.CopyNotes,
                                                crSetupRow.CopyFiles);
        }

        public virtual void UpdateFSSODetFromOpportunity(PXCache soDetCache,
                                                        FSSODet fsSODetRow,
                                                        CROpportunityProducts crOpportunityProductRow,
                                                        FSxCROpportunityProducts fsxCROpportunityProductsRow,
                                                        string lineType)
        {
            if (crOpportunityProductRow == null || fsxCROpportunityProductsRow == null)
            {
                return;
            }

            fsSODetRow.SourceNoteID = crOpportunityProductRow.NoteID;
            soDetCache.Current = fsSODetRow = (FSSODet)soDetCache.Insert(fsSODetRow);

            fsSODetRow.LineType = lineType;
            fsSODetRow.InventoryID = crOpportunityProductRow.InventoryID;
            fsSODetRow.IsFree = crOpportunityProductRow.IsFree;

            soDetCache.Current = fsSODetRow = (FSSODet)soDetCache.Update(fsSODetRow);
            fsSODetRow = (FSSODet)soDetCache.CreateCopy(fsSODetRow);

            fsSODetRow.BillingRule = fsxCROpportunityProductsRow.BillingRule;
            fsSODetRow.TranDesc = crOpportunityProductRow.Descr;

            if (crOpportunityProductRow.SiteID != null)
            {
                fsSODetRow.SiteID = crOpportunityProductRow.SiteID;
            }

            fsSODetRow.EstimatedDuration = fsxCROpportunityProductsRow.EstimatedDuration;
            fsSODetRow.EstimatedQty = crOpportunityProductRow.Qty;
            soDetCache.Current = fsSODetRow = (FSSODet)soDetCache.Update(fsSODetRow);

            fsSODetRow.CuryUnitPrice = crOpportunityProductRow.CuryUnitPrice;
            fsSODetRow.ManualPrice = crOpportunityProductRow.ManualPrice;

            fsSODetRow.ProjectID = crOpportunityProductRow.ProjectID;
            fsSODetRow.ProjectTaskID = crOpportunityProductRow.TaskID;
            fsSODetRow.CostCodeID = crOpportunityProductRow.CostCodeID;

            fsSODetRow.CuryUnitCost = crOpportunityProductRow.CuryUnitCost;
            fsSODetRow.ManualCost = crOpportunityProductRow.POCreate;

            fsSODetRow.EnablePO = crOpportunityProductRow.POCreate;
            fsSODetRow.POVendorID = crOpportunityProductRow.VendorID;
            fsSODetRow.POVendorLocationID = fsxCROpportunityProductsRow.VendorLocationID;

            fsSODetRow.TaxCategoryID = crOpportunityProductRow.TaxCategoryID;

            fsSODetRow.DiscPct = crOpportunityProductRow.DiscPct;
            fsSODetRow.CuryDiscAmt = crOpportunityProductRow.CuryDiscAmt;
            fsSODetRow.CuryBillableExtPrice = crOpportunityProductRow.CuryExtPrice;

            soDetCache.Current = soDetCache.Update(fsSODetRow);
        }

        public virtual string GetLineTypeFromInventoryItem(InventoryItem inventoryItemRow)
        {
            return inventoryItemRow.StkItem == true ? ID.LineType_ALL.INVENTORY_ITEM :
                            inventoryItemRow.ItemType == INItemTypes.ServiceItem ? ID.LineType_ALL.SERVICE : ID.LineType_ALL.NONSTOCKITEM;
        }
        #endregion

        #region Actions
        public PXMenuInquiry<CROpportunity> InqueriesMenu;
        [PXButton(MenuAutoOpen = true, SpecialType = PXSpecialButtonType.InquiriesFolder)]
        [PXUIField(DisplayName = "Inquiries")]
        public virtual IEnumerable inqueriesMenu(PXAdapter adapter)
        {
            return adapter.Get();
        }

        public PXAction<CROpportunity> CreateServiceOrder;
        [PXButton]
        [PXUIField(DisplayName = "Create Service Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void createServiceOrder()
        {
            CROpportunity crOpportunityRow = Base.Opportunity.Current;
            FSxCROpportunity fsxCROpportunityRow = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(crOpportunityRow);

            var products = Base.Products.View.SelectMultiBound(new object[] { crOpportunityRow }).RowCast<CROpportunityProducts>();

            if (products.Any(_ => _.InventoryID == null))
            {
                    if (Base.OpportunityCurrent.Ask(TX.Messages.ASK_SALES_ORDER_HAS_NON_INVENTORY_LINES, MessageButtons.OKCancel) == WebDialogResult.Cancel)
                    {
                        return;
                    }
                }

            if (CreateServiceOrderFilter.AskExt() == WebDialogResult.OK)
            {
                fsxCROpportunityRow.SDEnabled = true;
                fsxCROpportunityRow.BranchLocationID = CreateServiceOrderFilter.Current.BranchLocationID;
                fsxCROpportunityRow.SrvOrdType = CreateServiceOrderFilter.Current.SrvOrdType;

                PXLongOperation.StartOperation(Base, delegate ()
                {
                    CreateServiceOrderDocument(Base, crOpportunityRow, CreateServiceOrderFilter.Current);
                });
            }
        }

        public PXAction<CROpportunity> ViewServiceOrder;
        [PXButton]
        [PXUIField(DisplayName = "View Service Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void viewServiceOrder()
        {
            if (Base.Opportunity == null || Base.Opportunity.Current == null)
            {
                return;
            }

            if (Base.IsDirty)
            {
                Base.Save.Press();
            }

            FSxCROpportunity fsxCROpportunityRow = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(Base.Opportunity.Current);

            if (fsxCROpportunityRow != null && fsxCROpportunityRow.SOID != null)
            {
                CRExtensionHelper.LaunchServiceOrderScreen(Base, fsxCROpportunityRow.SOID);
            }
        }

        public PXAction<CROpportunity> OpenAppointmentBoard;
        [PXButton]
        [PXUIField(DisplayName = "Schedule on the Calendar Board", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void openAppointmentBoard()
        {
            if (Base.Opportunity == null || Base.Opportunity.Current == null)
            {
                return;
            }

            if (Base.IsDirty)
            {
                Base.Save.Press();
            }

            FSxCROpportunity fsxCROpportunityRow = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(Base.Opportunity.Current);

            if (fsxCROpportunityRow != null && fsxCROpportunityRow.SOID != null)
            {
                CRExtensionHelper.LaunchEmployeeBoard(Base, fsxCROpportunityRow.SOID);
            }
        }

        #endregion

        #region Events

        #region CROpportunity

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<CROpportunity, FSxCROpportunity.sDEnabled> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunity crOpportunityRow = (CROpportunity)e.Row;
            FSxCROpportunity fsxCROpportunityRow = e.Cache.GetExtension<FSxCROpportunity>(crOpportunityRow);

            if (fsxCROpportunityRow.SDEnabled == true)
            {
                FSSetup fsSetupRow = GetFSSetup();

                if (fsSetupRow != null
                        && fsSetupRow.DfltOpportunitySrvOrdType != null)
                {
                    fsxCROpportunityRow.SrvOrdType = fsSetupRow.DfltOpportunitySrvOrdType;
                }

                SetBranchLocationID(Base, crOpportunityRow, fsxCROpportunityRow);
                Base.Opportunity.SetValueExt<CROpportunity.allowOverrideContactAddress>(crOpportunityRow, true);
            }
            else
            {
                fsxCROpportunityRow.BranchLocationID = null;
            }
        }

        protected virtual void _(Events.FieldUpdated<CROpportunity, CROpportunity.branchID> e)
        {
            if (e.Row == null)
            {
                return;
            }
            CROpportunity crOpportunityRow = (CROpportunity)e.Row;
            FSxCROpportunity fsxCROpportunityRow = e.Cache.GetExtension<FSxCROpportunity>(crOpportunityRow);
            SetBranchLocationID(Base, crOpportunityRow, fsxCROpportunityRow);
        }

        #endregion

        protected virtual void _(Events.RowSelecting<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowSelected<CROpportunity> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunity crOpportunityRow = (CROpportunity)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunity fsxCROpportunityRow = cache.GetExtension<FSxCROpportunity>(crOpportunityRow);

            FSServiceOrder fsServiceOrderRow = CRExtensionHelper.GetRelatedServiceOrder(Base, cache, crOpportunityRow, fsxCROpportunityRow.SOID);
            FSSrvOrdType fsSrvOrdTypeRow = null;

            if (fsServiceOrderRow != null)
            {
                fsSrvOrdTypeRow = CRExtensionHelper.GetServiceOrderType(Base, fsServiceOrderRow.SrvOrdType);
            }

            EnableDisableExtensionFields(cache, fsxCROpportunityRow, fsServiceOrderRow);
            EnableDisableActions(cache, crOpportunityRow, fsxCROpportunityRow, fsServiceOrderRow, fsSrvOrdTypeRow);
            SetPersistingChecks(cache, crOpportunityRow, fsxCROpportunityRow, fsSrvOrdTypeRow);

            HideOrShowFieldsActionsByInventoryFeature();
        }

        protected virtual void _(Events.RowInserting<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowInserted<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowUpdating<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowUpdated<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowDeleting<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowDeleted<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowPersisting<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowPersisted<CROpportunity> e)
        {
        }

        #endregion
        #region CROpportunityProducts

        #region FieldSelecting
        #endregion
        #region FieldDefaulting

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, CROpportunityProducts.quantity> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null
                && fsxCROpportunityProductsRow.BillingRule == ID.BillingRule.TIME
                && fsxCROpportunityProductsRow.EstimatedDuration != null
                && fsxCROpportunityProductsRow.EstimatedDuration > 0)
            {
                e.NewValue = fsxCROpportunityProductsRow.EstimatedDuration / 60m;
            }
            else
            {
                e.NewValue = 0m;
            }
        }

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, CROpportunityProducts.curyUnitCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (string.IsNullOrEmpty(crOpportunityProductsRow.UOM) == false && crOpportunityProductsRow.InventoryID != null && crOpportunityProductsRow.POCreate == true)
            {
                object unitcost;
                cache.RaiseFieldDefaulting<CROpportunityProducts.unitCost>(e.Row, out unitcost);

                if (unitcost != null && (decimal)unitcost != 0m)
                {
                    decimal newval = INUnitAttribute.ConvertToBase<CROpportunityProducts.inventoryID, CROpportunityProducts.uOM>(cache, crOpportunityProductsRow, (decimal)unitcost, INPrecision.NOROUND);

                    IPXCurrencyHelper currencyHelper = Base.FindImplementation<IPXCurrencyHelper>();

                    if (currencyHelper != null)
                    {
                        currencyHelper.CuryConvCury((decimal)unitcost, out newval);
                    }
                    else
                    {
                        CM.PXDBCurrencyAttribute.CuryConvCury(cache, crOpportunityProductsRow, newval, out newval, true);
                    }

                    e.NewValue = Math.Round(newval, CommonSetupDecPl.PrcCst, MidpointRounding.AwayFromZero);
                    e.Cancel = true;
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, FSxCROpportunityProducts.billingRule> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (crOpportunityProductsRow.InventoryID != null)
            {
                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(Base, crOpportunityProductsRow.InventoryID);

                if (inventoryItemRow.ItemType == INItemTypes.ServiceItem)
                {
                    FSxService fsxServiceRow = PXCache<InventoryItem>.GetExtension<FSxService>(inventoryItemRow);
                    e.NewValue = fsxServiceRow?.BillingRule;
                    e.Cancel = true;
                }
                else
                {
                    e.NewValue = ID.BillingRule.FLAT_RATE;
                    e.Cancel = true;
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, FSxCROpportunityProducts.vendorLocationID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (crOpportunityProductsRow.POCreate == false || crOpportunityProductsRow.InventoryID == null)
            {
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, FSxCROpportunityProducts.estimatedDuration> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null)
            {
                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(Base, crOpportunityProductsRow.InventoryID);

                if (inventoryItemRow != null)
                {
                    FSxService fsxServiceRow = PXCache<InventoryItem>.GetExtension<FSxService>(inventoryItemRow);

                    if (inventoryItemRow.ItemType == INItemTypes.ServiceItem
                            || inventoryItemRow.ItemType == INItemTypes.NonStockItem)
                    {

                        e.NewValue = fsxServiceRow?.EstimatedDuration;
                        e.Cancel = true;
                    }
                }
            }
        }

        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, FSxCROpportunityProducts.estimatedDuration> e)
        {
            if (e.Row == null || Base.IsImportFromExcel == true || Base.IsImport == true)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow.EstimatedDuration != null && fsxCROpportunityProductsRow.BillingRule == ID.BillingRule.TIME)
            {
                e.Cache.SetDefaultExt<CROpportunityProducts.quantity>(crOpportunityProductsRow);
            }
        }

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, FSxCROpportunityProducts.billingRule> e)
        {
            if (e.Row == null || Base.IsImportFromExcel == true || Base.IsImport == true)
            {
                return;
            }

            e.Cache.SetDefaultExt<CROpportunityProducts.quantity>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, CROpportunityProducts.uOM> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null && Base.IsImportFromExcel == false)
            {
                cache.SetDefaultExt<CROpportunityProducts.curyUnitCost>(e.Row);
            }
        }

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, CROpportunityProducts.siteID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null && Base.IsImportFromExcel == false)
            {
                cache.SetDefaultExt<CROpportunityProducts.curyUnitCost>(e.Row);
            }
        }

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, CROpportunityProducts.inventoryID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null)
            {
                if (Base.IsImportFromExcel == false)
                {
                    cache.SetDefaultExt<CROpportunityProducts.curyUnitCost>(e.Row);
                }

                cache.SetDefaultExt<FSxCROpportunityProducts.billingRule>(e.Row);
            }
        }

        #endregion

        protected virtual void _(Events.RowSelecting<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowSelected<CROpportunityProducts> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null)
            {
                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(Base, crOpportunityProductsRow.InventoryID);

                if (inventoryItemRow != null)
                {
                    PXUIFieldAttribute.SetEnabled<FSxCROpportunityProducts.billingRule>(cache, crOpportunityProductsRow, inventoryItemRow.ItemType == INItemTypes.ServiceItem);
                    PXUIFieldAttribute.SetEnabled<FSxCROpportunityProducts.estimatedDuration>(cache, crOpportunityProductsRow, inventoryItemRow.ItemType == INItemTypes.ServiceItem || inventoryItemRow.ItemType == INItemTypes.NonStockItem);
                    PXUIFieldAttribute.SetEnabled<CROpportunityProducts.curyUnitCost>(cache, crOpportunityProductsRow, crOpportunityProductsRow.POCreate == true);
                    PXUIFieldAttribute.SetEnabled<FSxCROpportunityProducts.vendorLocationID>(cache, crOpportunityProductsRow, crOpportunityProductsRow.POCreate == true);
                    PXUIFieldAttribute.SetEnabled<CROpportunityProducts.quantity>(cache, crOpportunityProductsRow, fsxCROpportunityProductsRow.BillingRule != ID.BillingRule.TIME);
                }
            }
        }

        protected virtual void _(Events.RowInserting<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowInserted<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowUpdating<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowUpdated<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowDeleting<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowDeleted<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowPersisting<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowPersisted<CROpportunityProducts> e)
        {
        }

        #endregion

        #endregion
    }
}