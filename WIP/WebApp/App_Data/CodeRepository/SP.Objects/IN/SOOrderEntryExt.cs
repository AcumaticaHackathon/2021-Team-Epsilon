using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Util;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.EP;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.IN.DAC;
using PX.Objects.PO;
using PX.Objects.SO;
using PX.Objects.SP.DAC;
using SP.Objects.IN.DAC;

namespace SP.Objects.IN
{
    public class SOOrderEntryExt : PXGraphExtension<SOOrderEntry>
    {
	    protected ARSalesPriceMaint ARSalesPriceMaint => _arSalesPriceMaint.Value;
		private readonly Lazy<ARSalesPriceMaint> _arSalesPriceMaint = new Lazy<ARSalesPriceMaint>(PXGraph.CreateInstance<ARSalesPriceMaint>);
		public override void Initialize()
        {
            if (PortalSetup.Current == null)
                throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

            FinalConfirm finalConfirm = finalFilter.Current;
            currentCustomer = ReadBAccount.ReadCurrentCustomer();
            finalConfirm.CurrencyStatus = currentCustomer.CuryID;

            PXUIFieldAttribute.SetDisplayName<SOOrder.requestDate>(Base.Caches[typeof(SOOrder)], Objects.Messages.DeliveryDate);

            PXDimensionAttribute.SuppressAutoNumbering<PortalCardLines.inventoryIDDescription>(DocumentCardDetails.Cache, true);
        }

        #region Select
        public PXFilter<FinalConfirm> finalFilter;
        public PXSelect<PortalCardLines> DocumentCardDetails;
        public Customer currentCustomer;

        #endregion

		#region Delegate
		public virtual IEnumerable documentCardDetails()
		{
			foreach (var result in SelectFrom<PortalCardLines>
				.InnerJoin<InventoryItem>
					.On<PortalCardLines.inventoryID.IsEqual<InventoryItem.inventoryID>>
				.InnerJoin<PortalSetup>
					.On<PortalSetup.noteID.IsEqual<PortalCardLines.portalNoteID>>
				.Where<
					PortalCardLines.userID.IsEqual<@P.AsGuid>
					.And<PortalSetup.IsCurrentPortal>
					.And<InventoryItem.itemStatus.IsIn<
						InventoryItemStatus.active,
						InventoryItemStatus.noPurchases,
						InventoryItemStatus.noRequest>>>
				.View
				.Select(Base, PXAccess.GetUserID()))
			{
				yield return result[typeof(PortalCardLines)];
			}
		}
		#endregion

        #region Action
        public PXAction<SOOrder> ProceedtoCheckout;
        [PXUIField(DisplayName = "Continue", Visible = true)]
        [PXButton()]
        public virtual IEnumerable proceedtoCheckout(PXAdapter adapter)
        {
            SOOrder rowcurrent = Base.Document.Cache.Current as SOOrder;
            Base.Document.Cache.SetValueExt<SOOrderExt.isSecondScreen>(rowcurrent, 2);

            SOOrderExt rowcurrentext;
            rowcurrentext = PXCache<SOOrder>.GetExtension<SOOrderExt>(rowcurrent);

            SOShippingContact contactrowcurrent = Base.Shipping_Contact.Cache.Current as SOShippingContact;
            SOShippingAddress addressrowcurrent = Base.Shipping_Address.Cache.Current as SOShippingAddress;            
            

            FinalConfirm finalConfirm = finalFilter.Current;
            finalConfirm.CuryFreightAmt = rowcurrent.CuryFreightAmt;
            finalConfirm.CuryLineTotal = rowcurrentext.PortalLineTotal;
            finalConfirm.CuryOrderTotal = rowcurrent.CuryOrderTotal;
            finalConfirm.CuryTaxTotal = rowcurrent.CuryTaxTotal;
            finalConfirm.CuryDiscTot = rowcurrent.CuryDiscTot;

            finalConfirm.ShippingInformation = contactrowcurrent.Attention +
                                               "\n" + addressrowcurrent.AddressLine1 +
                                               "\n" + (string.IsNullOrEmpty(addressrowcurrent.City) ? string.Empty : (addressrowcurrent.City + ", "))
													+ (string.IsNullOrEmpty(addressrowcurrent.State) ? string.Empty : (addressrowcurrent.State + ", ")) 
													+ (string.IsNullOrEmpty(addressrowcurrent.PostalCode) ? string.Empty : (addressrowcurrent.PostalCode + ", ")) 
													+ addressrowcurrent.CountryID +
                                               "\n\n" + contactrowcurrent.FullName +
                                               "\n" + contactrowcurrent.Phone1 +
                                               "\n" + contactrowcurrent.Email +

                                               "\n\n " + PXMessages.LocalizeNoPrefix(Messages.ShipVia) + ": " + rowcurrent.ShipVia +
                                               "\n " + PXMessages.LocalizeNoPrefix(Messages.DeliveryDate) + ": " + (((DateTime)rowcurrent.RequestDate).Date).ToShortDateString();


            finalConfirm.CurrencyStatus = currentCustomer.CuryID;
            
			string warning = PXUIFieldAttribute.GetError<SOOrder.curyTaxTotal>(Base.Document.Cache, rowcurrent);
			if (!string.IsNullOrEmpty(warning))
			{
				PXUIFieldAttribute.SetWarning<FinalConfirm.curyTaxTotal>(finalFilter.Cache, finalFilter.Current, warning);
			}

            DocumentCardDetails.Select();
            return adapter.Get();
        }



        #region Action

        public PXAction<SOOrder> SubmitOrder;
        [PXUIField(DisplayName = "Submit Order", Visible = false)]
        [PXButton()]
        public virtual IEnumerable submitOrder(PXAdapter adapter)
        {
            SOOrder rowcurrent = Base.Document.Cache.Current as SOOrder;
            Contact contact = ReadBAccount.ReadCurrentContact();
            // почистим корзину
            foreach (PortalCardLines lines in PXSelectJoin<PortalCardLines, 
                InnerJoin<PortalSetup, On<PortalSetup.noteID, Equal<PortalCardLines.portalNoteID>>>,
                Where<PortalCardLines.userID, Equal<Required<PortalCardLines.userID>>,
                       And<PortalSetup.IsCurrentPortal>>>.Select(Base, PXAccess.GetUserID()))
            {
                DocumentCardDetails.Cache.Delete(lines);
                DocumentCardDetails.Cache.PersistDeleted(lines);
            }

            foreach (SOOrder row in Base.Document.Cache.Inserted)
            {
                SOOrderExt rowcurrentext;
                rowcurrentext = PXCache<SOOrder>.GetExtension<SOOrderExt>(row);
                PXNoteAttribute.SetNote(Base.Document.Cache, row, rowcurrentext.Comment);
                row.OrderDesc = Messages.DefaultOrderFromPortal + " by " + contact.DisplayName;
                Base.Document.Cache.SetValueExt<SOOrder.curyControlTotal>(row, row.CuryOrderTotal);
                Base.Document.Cache.Update(row);
				Base.RecalculateExternalTaxesSync = true;
                Base.Actions.PressSave();
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters["SOOrder.OrderType"] = row.OrderType;
                parameters["SOOrder.OrderNbr"] = row.OrderNbr;
                throw new PXReportRequiredException(parameters, "SO641010", null);
            }
            return adapter.Get();
        }

        public PXAction<SOOrder> ShippingDetail;
        [PXUIField(DisplayName = "Return To Shipping Detail")]
        [PXButton()]
        public virtual IEnumerable shippingDetail(PXAdapter adapter)
        {
            SOOrder rowcurrent = Base.Document.Cache.Current as SOOrder;
            Base.Document.Cache.SetValueExt<SOOrderExt.isSecondScreen>(rowcurrent, 1);
            
            /*SOOrder rowcurrent = Base.Document.Cache.Current as SOOrder;
            Base.Document.Cache.SetValueExt<SOOrderExt.isSecondScreen>(rowcurrent, 1);*/
            /*PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Base.Caches[typeof(PortalCardLines)], DocumentCardDetails.Current,
                        null);
            PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Base.Caches[typeof(PortalCardLines)], DocumentCardDetails.Current,
                        null);
            */

            return adapter.Get();
        }
        #endregion

        public PXAction<SOOrder> GoBack;
        [PXUIField(DisplayName = "Go Back to Cart")]
        [PXButton()]
        public virtual IEnumerable goBack(PXAdapter adapter)
        {
            foreach (PXCache cache in Base.Caches.Values)
            {
                cache.IsDirty = false;
            }
            
            InventoryCardMaint graph = PXGraph.CreateInstance<InventoryCardMaint>();
            PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
            return adapter.Get();
        }

        /*public PXAction<SOOrder> calculateFreight;
        [PXUIField(DisplayName = "Calculate", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXProcessButton]
        public virtual IEnumerable CalculateFreight(PXAdapter adapter)
        {
            //IEnumerable ret = null;
            //try
            //{
            //    ret = Base.Actions["CalculateFreight"].Press(adapter);
            //}
            //catch (PXException e)
            //{
            //    if (e.Message == PX.Objects.SO.Messages.AtleastOnePackageIsRequired)
            //        throw new PXException(Messages.AtleastOnePackageIsRequired);
            //}
            //return ret;
            Base.Actions["CalculateFreight"].Press(adapter);
        }*/
        #endregion

        #region Cache Attach

        [PXSelector(typeof (Branch.branchID))]
        [PXDBInt]
        protected virtual void SOOrder_BranchID_CacheAttached(PXCache sender)
        {
        }

        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Ship Via")]
        [PXSelector(typeof (Search<Carrier.carrierID>), typeof (Carrier.carrierID), typeof (Carrier.description),
            DescriptionField = typeof (Carrier.description), CacheGlobal = true)]
        [PXDefault(
            typeof (
                Search
                    <Location.cCarrierID,
                        Where
                            <Location.bAccountID, Equal<Current<SOOrder.customerID>>,
                                And<Location.locationID, Equal<Current<SOOrder.customerLocationID>>>>>),
            PersistingCheck = PXPersistingCheck.Nothing)]
        protected virtual void SOOrder_ShipVia_CacheAttached(PXCache sender)
        {
        }

        [PXSelector(typeof(Branch.branchID))]
        [PXDBInt]
		[PXDefault(typeof(SOOrder.branchID))]
        protected virtual void SOLine_BranchID_CacheAttached(PXCache sender)
        {
        }

		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDBDefault(typeof(SOOrder.shipComplete))]
        protected virtual void SOLine_ShipComplete_CacheAttached(PXCache sender) { }
        #endregion

        #region Handler

		protected virtual void SOOrder_OrderType_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e,
            PXFieldDefaulting def)
        {
			e.NewValue = PortalSetup.Current.DefaultOrderType;
			e.Cancel = true;
		}

        protected virtual void SOOrder_RowInserted(PXCache sender, PXRowInsertedEventArgs e,
            PXRowInserted ins)
        {
            if (ins != null)
                ins(sender, e);

            SOOrder row = e.Row as SOOrder;
            if (row != null && row.CustomerID == null)
            {
                var _setsetting = PortalSetup.Current;
                SOOrder soorder = Base.Document.Current;
                Base.Document.Cache.SetValueExt<SOOrderExt.isSecondScreen>(soorder, 1);
                Base.Document.Cache.SetValueExt<SOOrder.customerID>(soorder, currentCustomer.BAccountID);
                Base.Document.Cache.SetValueExt<SOOrder.branchID>(soorder, _setsetting.SellingBranchID);
                Base.Document.Cache.SetValueExt<SOOrder.curyID>(soorder, currentCustomer.CuryID);

                // давим  FieldVerifying на branchID так как такого бранча нет
                Base.FieldVerifying.AddHandler<SOLine.branchID>(
                    (PXCache sender1, PXFieldVerifyingEventArgs e1) => { e1.Cancel = true; });
                Base.FieldVerifying.AddHandler<SOOrder.branchID>(
                    (PXCache sender1, PXFieldVerifyingEventArgs e1) => { e1.Cancel = true; });
                Base.FieldVerifying.AddHandler<SOOrder.customerLocationID>(
                    (PXCache sender1, PXFieldVerifyingEventArgs e1) => { e1.Cancel = true; });

				using (Base.GetPriceCalculationScope().AppendContext<SOLine.inventoryID>())
				{
                foreach (PortalCardLines lines in DocumentCardDetails.Select(this))
                {
                    SOLine soline = Base.Transactions.Cache.CreateInstance() as SOLine;
                    soline.InventoryID = lines.InventoryID;
                    soline.SiteID = lines.SiteID;
                    soline.Qty = lines.Qty;
                    soline.UOM = lines.UOM;
                    soline.BranchID = _setsetting.SellingBranchID;
                    if (PXAccess.FeatureInstalled<FeaturesSet.subItem>())
                    {
                        if (lines.StkItem == true)
                            soline.SubItemID = _setsetting.DefaultSubItemID;
                    }
                    using (new PXReadBranchRestrictedScope())
                    {
                        soline = Base.Transactions.Cache.Insert(soline) as SOLine;
                    }
                }
				}
                Base.Caches[typeof(INSiteStatus)].Clear();
				SOOrder currentOrderOld = (SOOrder)Base.Document.Cache.CreateCopy(Base.Document.Current);
				currentOrderOld.FreightTaxCategoryID = null;
				Base.Document.Cache.RaiseRowUpdated(Base.Document.Current, currentOrderOld);

				if(Base.Document.Current.ShipVia != null)
					ReCalculateFreight();

            }
        }

        protected virtual void SOOrder_RequestDate_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e,
            PXFieldVerifying sel)
        {
            SOOrder rowcurrent = e.Row as SOOrder;
            if (rowcurrent != null)
                if (e.NewValue != null)
                {
                    if ((DateTime)e.NewValue < PXTimeZoneInfo.Today)
                    {
                        e.NewValue = PXTimeZoneInfo.Today;
                    }
                }
                else
                {
                    e.NewValue = PXTimeZoneInfo.Today;
                }
        }

        

        protected virtual void SOOrder_OverrideShipment_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e, PXFieldUpdated sel)
        {
            SOOrder rowcurrent = e.Row as SOOrder;
            SOOrderExt rowcurrentext;
            rowcurrentext = PXCache<SOOrder>.GetExtension<SOOrderExt>(rowcurrent);
            
            SOShippingContact row = Base.Shipping_Contact.Current as SOShippingContact;
            Base.Shipping_Contact.Current.IsDefaultContact = !rowcurrentext.OverrideShipment;
            Base.Caches[typeof (SOShippingContact)].Update(row);

            SOShippingAddress row1 = Base.Shipping_Address.Current as SOShippingAddress;
            Base.Shipping_Address.Current.IsDefaultAddress = !rowcurrentext.OverrideShipment;
            Base.Caches[typeof(SOShippingAddress)].Update(row1);
        }

        protected virtual void SOOrder_ShipVia_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e, PXFieldUpdated sel)
        {
            SOOrder row = e.Row as SOOrder;
            if (row.ShipVia != null)
            {
                try
                {
                    if (sel != null)
                        sel(sender, e);
                    Base.Actions["CalculateFreight"].Press();
                    Carrier _carrier = PXSelect<Carrier, Where<Carrier.carrierID, Equal<Required<Carrier.carrierID>>>>.SelectSingleBound(Base, null, row.ShipVia);
                    if (_carrier != null)
                    {
                        if (_carrier.IsExternal != true)
                        {
                            sender.SetValueExt<SOOrder.useCustomerAccount>(row, false);
                        }
                        PXUIFieldAttribute.SetEnabled<SOOrder.useCustomerAccount>(sender, row, _carrier.IsExternal == true);
                    }
                }
                catch (PXException)
                {
                    row = (SOOrder)sender.Locate(row) ?? row;
                    row.FreightCost = 0;
                    PXResultset<SOLine> res = Base.Transactions.Select();
                    FreightCalculator fc = Base.CreateFreightCalculator();
                    fc.CalcFreight<SOOrder, SOOrder.curyFreightCost, SOOrder.curyFreightAmt>(sender, row, res.Count);
                    PXUIFieldAttribute.SetWarning<SOOrder.curyFreightAmt>(sender, row, Messages.AtleastOnePackageIsRequired);
                }
            }
            else
            {
                sender.SetValueExt<SOOrder.useCustomerAccount>(row, false);
                PXUIFieldAttribute.SetEnabled<SOOrder.useCustomerAccount>(sender, row, false);
            }
        }

        protected virtual void SOOrder_CustomerID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e, PXFieldUpdated sel)
        {
            string customerrules = Base.customer.Current.CreditRule;

            if (sel != null)
                sel(sender, e);

            Base.customer.Current.CreditRule = customerrules;
        }


        protected virtual void SOOrder_Resedential_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e, PXFieldUpdated sel)
        {
            SOOrder row = e.Row as SOOrder;
            if (row.ShipVia != null)
            {
                    if (sel != null)
                        sel(sender, e);

				ReCalculateFreight();
            }
        }

        protected virtual void SOOrder_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
        {
            if (sel != null)
                sel(sender, e);

            SOOrder row = e.Row as SOOrder;
            if (row != null)
            {
                CalculateVisible(row);
                SOOrderType rowtype = PXSelect<SOOrderType,
                    Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>>>.SelectWindowed(Base, 0, 1,
                        row.OrderType);

                if (rowtype != null)
                {
                    PXUIFieldAttribute.SetVisible<SOOrder.shipVia>(Base.Caches[typeof(SOOrder)], null,
                        rowtype.CalculateFreight == true);
                    PXUIFieldAttribute.SetVisible<SOOrder.curyFreightAmt>(Base.Caches[typeof(SOOrder)], null,
                        rowtype.CalculateFreight == true);
                    PXUIFieldAttribute.SetVisible<FinalConfirm.curyFreightAmt>(Base.Caches[typeof(FinalConfirm)], null,
                        rowtype.CalculateFreight == true);
                    PXUIFieldAttribute.SetVisible<SOOrder.useCustomerAccount>(Base.Caches[typeof(SOOrder)], null,
                      rowtype.CalculateFreight == true);
                    PXUIFieldAttribute.SetEnabled<PortalCardLines.qty>(Base.Caches[typeof(PortalCardLines)], null, false);
                }
                PXUIFieldAttribute.SetVisible<PortalCardLines.currentWarehouse>(DocumentCardDetails.Cache, null, PortalSetup.Current.AvailableQty == true);
                PXUIFieldAttribute.SetVisible<PortalCardLines.totalWarehouse>(DocumentCardDetails.Cache, null, PortalSetup.Current.AvailableQty == true);

                string warning = PXUIFieldAttribute.GetError<SOOrder.customerID>(sender, row);
                if (warning != null || row.CreditHold == true)
                {
                    PXUIFieldAttribute.SetWarning<SOOrder.curyOrderTotal>(sender, row, Messages.DefaultCreditLimit);
                    PXUIFieldAttribute.SetWarning<FinalConfirm.curyOrderTotal>(Base.Caches[typeof(FinalConfirm)], finalFilter.Current, Messages.DefaultCreditLimit);
                }

                if (row.ShipVia != null)
                {
                    Carrier _carrier = PXSelect<Carrier, Where<Carrier.carrierID, Equal<Required<Carrier.carrierID>>>>.SelectSingleBound(Base, null, row.ShipVia);
                    if (_carrier != null)
                    {
                        if (_carrier.IsExternal != true)
                        {
                            sender.SetValueExt<SOOrder.useCustomerAccount>(row, false);
                        }
                        PXUIFieldAttribute.SetEnabled<SOOrder.useCustomerAccount>(sender, row, _carrier.IsExternal == true);
                    }
                }
                else
                {
                    sender.SetValueExt<SOOrder.useCustomerAccount>(row, false);
                    PXUIFieldAttribute.SetEnabled<SOOrder.useCustomerAccount>(sender, row, false);
                }
            }
        }

        protected virtual void PortalCardLines_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            PortalCardLines row = e.Row as PortalCardLines;
            SOOrder rowcurrent = Base.Document.Cache.Current as SOOrder;
            if (row != null)
            {
                // цена
                decimal price = CalculatePriceCard(row, rowcurrent);
                DocumentCardDetails.Cache.SetValueExt<PortalCardLines.curyUnitPrice>(row, price);

                InventoryLines _inventoryLines = PXSelect<InventoryLines,
                Where<InventoryLines.inventoryID, Equal<Required<InventoryLines.inventoryID>>>>.SelectWindowed(Base, 0, 1, row.InventoryID);

                if (_inventoryLines != null)
                {
                    if (_inventoryLines.StkItem == true)
                    {
                        DocumentCardDetails.Cache.SetValueExt<PortalCardLines.currentWarehouse>(row, CalculateCurrentWarehouse(row));
                        DocumentCardDetails.Cache.SetValueExt<PortalCardLines.totalWarehouse>(row, CalculateTotaltWarehouse(row));

                        // warning
                        if (PortalSetup.Current.AvailableQty == true)
                        {
                            if (row.Qty > row.CurrentWarehouse)
                                PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Base.Caches[typeof(PortalCardLines)], row,
                                    Messages.deficiencyWarehouse);

                            if (row.Qty > row.TotalWarehouse)
                                PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Base.Caches[typeof(PortalCardLines)], row,
                                    Messages.deficiencyWarehouses);
                        }
                    }
                    else
                    {
                        DocumentCardDetails.Cache.SetValueExt<PortalCardLines.currentWarehouse>(row, null);
                        DocumentCardDetails.Cache.SetValueExt<PortalCardLines.totalWarehouse>(row, null);
                    }
                }
            }
        }

        // В базовом графе происходит сброс установленного бранча.
        protected virtual void SOOrder_CustomerLocationID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e, PXFieldUpdated sel)
        {
            if (sel != null)
                sel(sender, e);

            sender.SetValueExt<SOOrder.branchID>(e.Row, PortalSetup.Current.SellingBranchID);

            if (e.ExternalCall)
            {
                SOOrder rowcurrent = e.Row as SOOrder;
                SOOrderExt rowcurrentext;
                rowcurrentext = PXCache<SOOrder>.GetExtension<SOOrderExt>(rowcurrent);
                rowcurrentext.OverrideShipment = false;
            }
        }
        #endregion

        #region Private Help Function
        public decimal CalculatePriceCard(PortalCardLines row, SOOrder soOrder)
        {
            // цена
            string customerPriceClass = ARPriceClass.EmptyPriceClass;

            if (soOrder != null && soOrder.CustomerLocationID != null)
            {
                 Location location = PXSelect<Location,
                Where<Location.locationID, Equal<Required<Location.locationID>>,
                And<Location.bAccountID, Equal<Required<Location.bAccountID>>>>>.Select(Base, soOrder.CustomerLocationID, currentCustomer.BAccountID);

				if (!string.IsNullOrEmpty(location?.CPriceClassID))
				{
					customerPriceClass = location.CPriceClassID;
				}

				CurrencyInfo info = new CurrencyInfo();
                info.CuryID = currentCustomer.CuryID;

                string curyRateTypeId = currentCustomer.CuryRateTypeID;
                if (String.IsNullOrEmpty(curyRateTypeId))
                    curyRateTypeId = new ARSetupSelect(Base).Current.DefaultRateTypeID;
                if (String.IsNullOrEmpty(curyRateTypeId))
                    curyRateTypeId = new CMSetupSelect(Base).Current.ARRateTypeDflt;

                info.CuryRateTypeID = curyRateTypeId;
                info = Base.currencyinfo.Update(info);

                decimal price =
                    ARSalesPriceMaint.CalculateSalesPrice(Base.Caches[typeof(PortalCardLines)], customerPriceClass,
                        currentCustomer.BAccountID,
                        row.InventoryID,
						row.SiteID,
                        info,
                        row.UOM, row.Qty, Base.Accessinfo?.BusinessDate ?? DateTime.Now.Date, 0) ?? 0m;

                    return price;
            }
            return 0;
        }

        public decimal CalculateTotaltWarehouse(PortalCardLines row)
        {
            decimal total = 0;

            InventoryItem inventoryItem = PXSelect<InventoryItem,
                Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                .SelectWindowed(Base, 0, 1, row.InventoryID);

            INUnit inUnit = PXSelect<INUnit,
                Where<INUnit.inventoryID, Equal<Required<INUnit.inventoryID>>,
                And<INUnit.fromUnit, Equal<Required<INUnit.fromUnit>>,
                And<INUnit.toUnit, Equal<Required<INUnit.toUnit>>>>>>
                .SelectWindowed(Base, 0, 1, row.InventoryID, row.UOM, inventoryItem.BaseUnit);

            decimal unitRate = 1;
            if (inUnit != null)
                unitRate = (decimal)inUnit.UnitRate;

            foreach (INSiteStatus VARIABLE in
                PXSelect<INSiteStatus,
                    Where<INSiteStatus.inventoryID, Equal<Required<INSiteStatus.inventoryID>>, And<INSiteStatus.siteID, Equal<Required<INSiteStatus.siteID>>>>>
                    .Select(Base, row.InventoryID, row.SiteID))
            {
                WarehouseReference warehouse = PXSelect<WarehouseReference,
                    Where<WarehouseReference.siteID, Equal<Required<INSite.siteID>>,
                          And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>
                    .SelectWindowed(Base, 0, 1, VARIABLE.SiteID);

                if (unitRate != 0 && warehouse != null)
                {
					total = total + GetQty(VARIABLE, unitRate);
                }
            }
            return total;
        }

		protected virtual Type GetQtyField()
		{
			return typeof(INSiteStatus.qtyAvail);
		}

		protected virtual decimal GetQty(INSiteStatus iNSiteStatus, decimal unitRate)
		{
			decimal qty = (decimal?)Base.Caches<INSiteStatus>().GetValue(iNSiteStatus, GetQtyField().Name) ?? 0m;
			return (decimal)(qty / unitRate);
		}

        public decimal CalculateCurrentWarehouse(PortalCardLines row)
        {
            INSiteStatus iNSiteStatus = PXSelect<INSiteStatus,
                Where<INSiteStatus.siteID, Equal<Required<INSiteStatus.siteID>>,
                    And<INSiteStatus.inventoryID, Equal<Required<INSiteStatus.inventoryID>>>>>
                .SelectWindowed(Base, 0, 1, row.SiteID, row.InventoryID);

            WarehouseReference warehouse = PXSelect<WarehouseReference,
                Where<WarehouseReference.siteID, Equal<Required<INSite.siteID>>,
                     And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>
                .SelectWindowed(Base, 0, 1, row.SiteID);

            InventoryItem inventoryItem = PXSelect<InventoryItem,
                Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                .SelectWindowed(Base, 0, 1, row.InventoryID);

            INUnit inUnit = PXSelect<INUnit,
                Where<INUnit.inventoryID, Equal<Required<INUnit.inventoryID>>,
                And<INUnit.fromUnit, Equal<Required<INUnit.fromUnit>>,
                And<INUnit.toUnit, Equal<Required<INUnit.toUnit>>>>>>
                .SelectWindowed(Base, 0, 1, row.InventoryID, row.UOM, inventoryItem.BaseUnit);

            decimal unitRate = 1;
            if (inUnit != null)
                unitRate = (decimal)inUnit.UnitRate;

            decimal qty = 0;
            if (unitRate != 0 && iNSiteStatus != null && warehouse != null)
            {
                    qty = GetQty(iNSiteStatus, unitRate);
            }
            return (decimal)qty;
        }

        public void CalculateVisible(SOOrder rowcurrent)
        {
            SOOrderExt rowcurrentext;
            rowcurrentext = PXCache<SOOrder>.GetExtension<SOOrderExt>(rowcurrent);


            if (rowcurrentext != null)
            {
                /*PXUIFieldAttribute.SetVisible<PortalCardLines.inventoryID>(Base.Caches[typeof(PortalCardLines)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<PortalCardLines.qty>(Base.Caches[typeof(PortalCardLines)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<PortalCardLines.curyUnitPrice>(Base.Caches[typeof(PortalCardLines)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<PortalCardLines.uOM>(Base.Caches[typeof(PortalCardLines)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<PortalCardLines.totalPrice>(Base.Caches[typeof(PortalCardLines)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<PortalCardLines.inventoryIDDescription>(Base.Caches[typeof(PortalCardLines)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<PortalCardLines.descr>(Base.Caches[typeof(PortalCardLines)], null, rowcurrentext.IsSecondScreen);

                //Base.Caches[typeof(PortalCardLines)].AllowSelect = rowcurrentext.IsSecondScreen;


                PXUIFieldAttribute.SetVisible<SOOrder.curyFreightAmt>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrder.curyLineTotal>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrder.curyOrderTotal>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrder.curyTaxTotal>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);

                PXUIFieldAttribute.SetVisible<SOShippingContact.overrideContact>(Base.Caches[typeof(SOShippingContact)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingContact.fullName>(Base.Caches[typeof(SOShippingContact)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingContact.salutation>(Base.Caches[typeof(SOShippingContact)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingContact.phone1>(Base.Caches[typeof(SOShippingContact)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingContact.email>(Base.Caches[typeof(SOShippingContact)], null, !rowcurrentext.IsSecondScreen);

                PXUIFieldAttribute.SetVisible<SOShippingAddress.overrideAddress>(Base.Caches[typeof(SOShippingAddress)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingAddress.isValidated>(Base.Caches[typeof(SOShippingAddress)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingAddress.addressLine1>(Base.Caches[typeof(SOShippingAddress)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingAddress.addressLine2>(Base.Caches[typeof(SOShippingAddress)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingAddress.city>(Base.Caches[typeof(SOShippingAddress)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingAddress.countryID>(Base.Caches[typeof(SOShippingAddress)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingAddress.state>(Base.Caches[typeof(SOShippingAddress)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOShippingAddress.postalCode>(Base.Caches[typeof(SOShippingAddress)], null, !rowcurrentext.IsSecondScreen);

                PXUIFieldAttribute.SetVisible<SOOrder.curyLineTotal>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrder.curyFreightAmt>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrder.curyTaxTotal>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrder.curyOrderTotal>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrder.shipVia>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);

                PXUIFieldAttribute.SetVisible<SOOrder.requestDate>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);

                PXUIFieldAttribute.SetVisible<SOOrder.resedential>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrder.useCustomerAccount>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<SOOrderExt.comment>(Base.Caches[typeof(SOOrder)], null, !rowcurrentext.IsSecondScreen);


                PXUIFieldAttribute.SetVisible<FinalConfirm.currencyStatus>(Base.Caches[typeof(FinalConfirm)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<FinalConfirm.curyFreightAmt>(Base.Caches[typeof(FinalConfirm)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<FinalConfirm.curyLineTotal>(Base.Caches[typeof(FinalConfirm)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<FinalConfirm.curyOrderTotal>(Base.Caches[typeof(FinalConfirm)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<FinalConfirm.curyTaxTotal>(Base.Caches[typeof(FinalConfirm)], null, rowcurrentext.IsSecondScreen);
                PXUIFieldAttribute.SetVisible<FinalConfirm.shippingInformation>(Base.Caches[typeof(FinalConfirm)], null, rowcurrentext.IsSecondScreen);*/

                Base.Actions["ProceedtoCheckout"].SetVisible(rowcurrentext.IsSecondScreen == 1);
                Base.Actions["GoBack"].SetVisible(rowcurrentext.IsSecondScreen == 1);

                Base.Actions["SubmitOrder"].SetVisible(rowcurrentext.IsSecondScreen == 2);
                Base.Actions["ShippingDetail"].SetVisible(rowcurrentext.IsSecondScreen == 2);
            }
        }
        #endregion
        
		#region Override

		[PXOverride]
		public virtual void SOOrder_ShipDate_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e, PXFieldUpdated baseMethod)
		{
			DateTime? oldDate = (DateTime?)e.OldValue;
			if (oldDate != ((SOOrder)e.Row).ShipDate)
			{
				if ((SOLine)Base.Transactions.Select() != null && (Base.Document.View.Answer == WebDialogResult.None && !Base.IsMobile) &&
					((SOOrder)e.Row).ShipComplete == SOShipComplete.BackOrderAllowed)
				{
					Base.Document.View.Answer = WebDialogResult.Yes; // Answer is always yes for ConfirmShipDateRecalc
				}
			}
			
			baseMethod(cache, e);
		}

		#endregion

		public virtual void ReCalculateFreight()
		{
			try
			{
				Base.calculateFreight.Press();
			}
			catch (PXException)
			{
				SOOrder doc = Base.Document.Current;
				var cache = Base.Document.Cache;
				cache.SetValueExt<SOOrder.curyFreightAmt>(doc, 0m);
				cache.SetValueExt<SOOrder.freightAmt>(doc, 0m);
				PXUIFieldAttribute.SetError<SOOrder.curyFreightAmt>(cache, doc, Messages.AtleastOnePackageIsRequired);
			}
		}
    }
}
