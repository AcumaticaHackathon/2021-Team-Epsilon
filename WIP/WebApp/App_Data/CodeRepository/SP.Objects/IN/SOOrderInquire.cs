using System;
using System.Collections;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Data.EP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.SM;
using SP.Objects.CR;
using PX.Objects.GL.DAC;
using PX.Objects.SP.DAC;
using PX.Objects.Common.Discount;
using PX.Objects.Common.Discount.Mappers;

namespace SP.Objects.IN
{
    [DashboardType((int)DashboardTypeAttribute.Type.Default, TableAndChartDashboardTypeAttribute._AMCHARTS_DASHBOART_TYPE)]
    public class SOOrderInquire : PXGraph<SOOrderInquire>
    {
        #region Constructor
        public SOOrderInquire()
        {
            currentCustomer = ReadBAccount.ReadCurrentCustomerWithoutCheck();

            Actions["PrintOrder"].SetEnabled(currentCustomer != null);
            Actions["CancelOrder"].SetEnabled(currentCustomer != null);
            Actions["ViewShipment"].SetEnabled(currentCustomer != null);
            Actions["Reorder"].SetEnabled(currentCustomer != null);

            if (currentCustomer != null)
            {
                bool isActive = currentCustomer.Status == CustomerStatus.Active;
                Actions["reorder"].SetEnabled(isActive);
            }

            PXUIFieldAttribute.SetDisplayName<Contact.displayName>(this.Caches[typeof(Contact)], "Created By");
            PXUIFieldAttribute.SetDisplayName<SOOrderType.descr>(this.Caches[typeof(SOOrderType)], "Type");
            PXUIFieldAttribute.SetDisplayName<SOOrder.requestDate>(this.Caches[typeof(SOOrder)], "Delivery Date");

            PXDimensionAttribute.SuppressAutoNumbering<PortalCardLines.inventoryIDDescription>(CardLines.Cache, true);
        }
        #endregion

        [PXHidden]
        public PXSelect<Contact> Contact;
        public Customer currentCustomer;
        
        [PXFilterable]
        public PXSelectJoin<SOOrder,
            LeftJoin<Users, On<Users.pKID, Equal<SOOrder.createdByID>>,
            LeftJoin<SOOrderType, On<SOOrderType.orderType, Equal<SOOrder.orderType>>,
            LeftJoin<PX.Objects.GL.Branch, On<PX.Objects.GL.Branch.branchID, Equal<SOOrder.branchID>>,
            LeftJoin<Organization, On<Organization.organizationID, Equal<PX.Objects.GL.Branch.organizationID>, Or<Organization.organizationID, IsNull>>,
            LeftJoin<PortalSetup, On2<Where<PortalSetup.restrictByOrganizationID, IsNull, Or<Organization.organizationID, Equal<PortalSetup.restrictByOrganizationID>>>,
                 And<Where<PortalSetup.restrictByBranchID, IsNull, Or<PX.Objects.GL.Branch.branchID, Equal<PortalSetup.restrictByBranchID>>>>>>>>>>,
            Where2<MatchWithBAccount<SOOrder.customerID, Current<AccessInfo.userID>>,
                 And<PortalSetup.IsCurrentPortal>>,
            OrderBy<Desc<SOOrder.orderNbr>>> Items;

        public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<Batch.curyInfoID>>>> currencyinfo;

        [PXHidden]
        public PXSelect<PortalCardLine> CardLine;

        [PXHidden]
        public PXSelect<PortalCardLines> CardLines;

        public virtual IEnumerable cardLines()
        {
            foreach (var result in PXSelectJoin<PortalCardLines,
                InnerJoin<InventoryItem, On<PortalCardLines.inventoryID, Equal<InventoryItem.inventoryID>>>,
            Where<PortalCardLines.userID, Equal<Required<PortalCardLines.userID>>,
                And<
                Where<InventoryItem.itemStatus, Equal<InventoryItemStatus.active>,
                    Or<InventoryItem.itemStatus, Equal<InventoryItemStatus.noPurchases>,
                    Or<InventoryItem.itemStatus, Equal<InventoryItemStatus.noRequest>>>>>>>.Select(this, PXAccess.GetUserID()))
            {
                yield return result[typeof(PortalCardLines)];
            }
        }

        [PXUIField(DisplayName = "Created By", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [PXDependsOnFields(typeof(Contact.lastName), typeof(Contact.firstName), typeof(Contact.midName), typeof(Contact.title))]
        [ContactDisplayName(typeof(Contact.lastName), typeof(Contact.firstName), typeof(Contact.midName), typeof(Contact.title), true)]
        [PXFieldDescription]
        [PXDefault]
        [PXUIRequired(typeof(Where<Contact.contactType, Equal<ContactTypesAttribute.lead>, Or<Contact.contactType, Equal<ContactTypesAttribute.person>>>))]
        [PXNavigateSelector(typeof(Search<Contact.displayName,
            Where<Contact.contactType, Equal<ContactTypesAttribute.
            lead>,
                Or<Contact.contactType, Equal<ContactTypesAttribute.person>,
                Or<Contact.contactType, Equal<ContactTypesAttribute.employee>>>>>))]
        protected virtual void Contact_DisplayName_CacheAttached(PXCache sender)
        {

        }

        public PXAction<SOOrder> PrintOrder;
        [PXUIField(DisplayName = Objects.Messages.PrintOrder)]
        [PXButton(Tooltip = Objects.Messages.PrintOrder)]
        [PXActionRestriction(typeof(Where<CustomerEnabled.isEnabled, Equal<False>>), Objects.Messages.PrintOrderAccessError)]
        public virtual IEnumerable printOrder(PXAdapter adapter)
        {
            if (this.Items.Current != null)
            {
                SOOrder rowcurrent = this.Items.Current;
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters["SOOrder.OrderType"] = rowcurrent.OrderType;
                parameters["SOOrder.OrderNbr"] = rowcurrent.OrderNbr;
                throw new PXReportRequiredException(parameters, "SO641010", null);
            }
            return adapter.Get();
        }

        public PXAction<SOOrder> ViewShipment;
        [PXUIField(DisplayName = Objects.Messages.ViewShipments)]
        [PXButton(Tooltip = Objects.Messages.ViewShipments)]
        public virtual IEnumerable viewShipment(PXAdapter adapter)
        {
            if (this.Items.Current != null)
            {
                SOOrder rowcurrent = this.Items.Current;
                SOShipmentInquire graph = PXGraph.CreateInstance<SOShipmentInquire>();
                SOOrder currentline = graph.SOOrderView.Search<SOOrder.orderNbr>(rowcurrent.OrderNbr, rowcurrent.OrderType);
                if (currentline != null)
                {
                    graph.SOOrderView.Current = currentline;
                    PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Popup);
                }
            }
            return adapter.Get();
        }

        public PXAction<SOOrder> Reorder;
        [PXUIField(DisplayName = Objects.Messages.CopyOrdertoCart)]
        [PXButton(Tooltip = Objects.Messages.CopyOrdertoCart)]
        public virtual IEnumerable reorder(PXAdapter adapter)
        {
            if (this.Items.Current != null)
            {
                SOOrder rowcurrent = this.Items.Current;
                SOOrderEntry graph = PXGraph.CreateInstance<SOOrderEntry>();
                SOOrder currentline = graph.Document.Search<SOOrder.orderNbr>(rowcurrent.OrderNbr, rowcurrent.OrderType);

                if (currentline != null)
                {
                    // была ли корзина?
                    CardLine.Cache.Clear();
                    CardLine.Cache.ClearQueryCache();
                    CardLines.Cache.Clear();
                    CardLine.Cache.ClearQueryCache();

                    foreach (var lines in PXSelectJoin<SOLine,
                        InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<SOLine.inventoryID>>>,
                        Where<SOLine.orderType, Equal<Required<SOLine.orderType>>,
                            And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>>>>.Select(this, currentline.OrderType,
                                currentline.OrderNbr))
                    {
                        SOLine _soLine = lines[typeof(SOLine)] as SOLine;
                        InventoryItem _inventoryItem = lines[typeof(InventoryItem)] as InventoryItem;

                        PortalCardLines currentlineincart = PXSelectJoin<PortalCardLines,
                          InnerJoin<PortalSetup, On<PortalSetup.noteID, Equal<PortalCardLines.portalNoteID>>>,
                            Where<PortalCardLines.userID, Equal<Required<PortalCardLines.userID>>,
                                And<PortalCardLines.siteID, Equal<Required<PortalCardLines.siteID>>,
                                   And<PortalCardLines.inventoryID, Equal<Required<PortalCardLines.inventoryID>>,
                                     And<PortalCardLines.uOM, Equal<Required<PortalCardLines.uOM>>,
                                       And<PortalSetup.IsCurrentPortal>>>>>>.Select(this, PXAccess.GetUserID(), _soLine.SiteID, _soLine.InventoryID, _soLine.UOM);


                        if (currentlineincart == null)
                        {
                            PortalCardLines currentlineincart1 = CardLines.Cache.CreateInstance() as PortalCardLines;
                            currentlineincart1.UserID = PXAccess.GetUserID();
                            currentlineincart1.PortalNoteID = PortalSetup.Current.NoteID;
                            currentlineincart1.SiteID = _soLine.SiteID;
                            currentlineincart1.InventoryID = _soLine.InventoryID;
                            currentlineincart1.InventoryIDDescription = _inventoryItem.InventoryCD;
                            currentlineincart1.Descr = _inventoryItem.Descr;
                            currentlineincart1.StkItem = _inventoryItem.StkItem;

                            currentlineincart1.Qty = Convert.ToInt32(_soLine.Qty);
                            currentlineincart1.UOM = _soLine.UOM;
                            using (new PXReadBranchRestrictedScope())
                            {
                                currentlineincart1 = CardLines.Insert(currentlineincart1);
                            }
                            CalculateDiscountPriceCard(currentlineincart1, (decimal)_soLine.CuryUnitPrice);
                            CardLines.Cache.PersistInserted(currentlineincart1);
                        }

                        else
                        {
                            currentlineincart.Qty = currentlineincart.Qty + Convert.ToInt32(_soLine.Qty);
                            currentlineincart = CardLines.Update(currentlineincart);
                            CalculateDiscountPriceCard(currentlineincart, (decimal)_soLine.CuryUnitPrice);
                            CardLines.Cache.PersistUpdated(currentlineincart);
                        }
                    }
                    InventoryCardMaint graph1 = PXGraph.CreateInstance<InventoryCardMaint>();
                    PXRedirectHelper.TryRedirect(graph1, PXRedirectHelper.WindowMode.Same);
                }
            }
            return adapter.Get();
        }

        public PXAction<SOOrder> CancelOrder;
        [PXUIField(DisplayName = Objects.Messages.CancelOrder)]
        [PXButton(Tooltip = Objects.Messages.CancelOrder)]
        public virtual IEnumerable cancelOrder(PXAdapter adapter)
        {
            if (this.Items.Current != null)
            {
                SOOrder rowcurrent = this.Items.Current;
                SOOrderEntry graph = PXGraph.CreateInstance<SOOrderEntry>();
                SOOrder currentline = graph.Document.Search<SOOrder.orderNbr>(rowcurrent.OrderNbr, rowcurrent.OrderType);

                graph.Document.Current = currentline;

                if (currentline.Status.IsIn(SOOrderStatus.Open, SOOrderStatus.Hold, SOOrderStatus.CreditHold))
                    graph.cancelOrder.Press();
            }
           
            Items.Cache.Clear();
            Items.Cache.ClearQueryCache();

            Items.View.RequestRefresh();
            return adapter.Get();
        }

        public void CalculateDiscountPriceCard(PortalCardLines row, decimal price)
        {
            // цена
            Location location = PXSelect<Location,
                Where<Location.locationID, Equal<Required<Location.locationID>>,
                    And<Location.bAccountID, Equal<Required<Location.locationID>>>>>.Select(this, currentCustomer.DefLocationID, currentCustomer.BAccountID);

            CurrencyInfo info = new CurrencyInfo();
            info.CuryID = currentCustomer.CuryID;

            string curyRateTypeId = currentCustomer.CuryRateTypeID;
            if (String.IsNullOrEmpty(curyRateTypeId))
                curyRateTypeId = new ARSetupSelect(this).Current.DefaultRateTypeID;
            if (String.IsNullOrEmpty(curyRateTypeId))
                curyRateTypeId = new CMSetupSelect(this).Current.ARRateTypeDflt;

            info.CuryRateTypeID = curyRateTypeId;
            info = currencyinfo.Update(info);
            row.CuryInfoID = info.CuryInfoID;
            row.CustomerID = currentCustomer?.BAccountID; //CustomerID is needed to select correct customer-specific and customer price class-specific prices inside the discount engine.

	        DiscountEngine.SetLineDiscountOnly(CardLines.Cache, row,
                                new DiscountLineFields
                                    <DiscountLineFields.skipDisc,
                                    PortalCardLines.baseDiscountAmt,
                                    PortalCardLines.baseDiscountPct,
                                    PortalCardLines.baseDiscountID,
                                    PortalCardLines.baseDiscountSeq,
									DiscountLineFields.discountsAppliedToLine,
									DiscountLineFields.manualDisc,
                                    DiscountLineFields.manualPrice,
                                    DiscountLineFields.lineType,
                                    DiscountLineFields.isFree,
									DiscountLineFields.calculateDiscountsOnImport>(CardLines.Cache, row),
                                    row.BaseDiscountID,
                                    price,
                                    row.Qty * price,
                                    row.Qty,
                                    location.LocationID,
                                    currentCustomer.BAccountID,
                                    currentCustomer.CuryID,
                                    (DateTime)PXTimeZoneInfo.Now, null,
                                    row.InventoryID,
                                    false);

            currencyinfo.Cache.Clear();
            currencyinfo.Cache.ClearQueryCache();
        }
    }
}
