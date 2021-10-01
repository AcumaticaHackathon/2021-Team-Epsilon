using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.SP.DAC;
using PX.Objects.Common.Discount;
using PX.Objects.Common.Discount.Mappers;
using PX.SM;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using SP.Objects.IN.DAC;

namespace SP.Objects.IN
{
    public class ImageViewer : PXGraph<ImageViewer>
	{
		protected ARSalesPriceMaint ARSalesPriceMaint => _arSalesPriceMaint.Value;
		private readonly Lazy<ARSalesPriceMaint> _arSalesPriceMaint = new Lazy<ARSalesPriceMaint>(CreateInstance<ARSalesPriceMaint>);
		private readonly string _inventoryItemDetailsSiteIdViewName = string.Concat("_", nameof(DAC.InventoryItemDetails), nameof(DAC.InventoryItemDetails.SiteID), "_", typeof(INSite.siteID).FullName, "_");
		#region Ctor
		public ImageViewer()
        {
            if (PortalSetup.Current == null)
                throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

            currentCustomer = ReadBAccount.ReadCurrentCustomer();

            bool isActive = currentCustomer.Status == CustomerStatus.Active;

            Actions["addToCart"].SetEnabled(isActive);

            PXUIFieldAttribute.SetVisible<InventoryLines.currentWarehouse>(InventoryItemDetails.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>());
            PXUIFieldAttribute.SetVisible<InventoryItemDetails.siteIDLabel>(InventoryItemDetails.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>());

            PXSelectorAttribute.SetColumns<InventoryItemDetails.uOM>(Caches[typeof(InventoryItemDetails)],
            new Type[]
			{
			    typeof (INUnit.fromUnit), 
                typeof (INUnitExt.convertionFactor
                )
			}, null);

            var setting = PortalSetup.Current;
            if (setting != null)
            {
                SetWarehouseColumn(setting.AvailableQty == true);
            }

            PXDimensionAttribute.SuppressAutoNumbering<PortalCardLines.inventoryIDDescription>(CardLines.Cache, true);
        }
        #endregion
        public PXSelect<InventoryItem> InventoryItem;
        public PXSelectJoin<InventoryItemDetails,
            InnerJoin<InventoryItem, On<InventoryItemDetails.inventoryID, Equal<InventoryItem.inventoryID>>>, 
            Where<InventoryItemDetails.inventoryID, Equal<Current<InventoryItem.inventoryID>>>> InventoryItemDetails;

        public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<Batch.curyInfoID>>>> currencyinfo;

        public PXSelect<InventoryLines,
                            Where<InventoryLines.inventoryID, Equal<Required<InventoryLines.inventoryID>>>> InventoryLine;

        public CRAttributeList<InventoryItem> Answers;

		[PXHidden]
		public SelectFrom<PortalCardLines>
			.InnerJoin<PortalSetup>
				.On<PortalSetup.noteID.IsEqual<PortalCardLines.portalNoteID>>
			.Where<
				PortalCardLines.userID.IsEqual<@P.AsGuid>
				.And<PortalCardLines.siteID.IsEqual<@P.AsInt>>
				.And<PortalCardLines.inventoryID.IsEqual<@P.AsInt>>
				.And<PortalCardLines.uOM.IsEqual<@P.AsString>>
				.And<PortalSetup.IsCurrentPortal>>
			.View
			CardLines;

        [PXHidden]
        public PXSelect<INSite> InSite_dummy_for_search;

        public Customer currentCustomer;

        public virtual IEnumerable inSite_dummy_for_search()
        {
            IEnumerable<object> filteredItemsExt = null;
            using (new PXReadBranchRestrictedScope())
            {
                filteredItemsExt = new PXView(this, true, this.Views[_inventoryItemDetailsSiteIdViewName].BqlSelect).SelectMulti();
            }
            return filteredItemsExt;
        }

        #region Action
        public PXAction<InventoryItem> AddToCart;
        [PXUIField(DisplayName = "Add To Cart")]
        [PXButton()]
        public virtual IEnumerable addToCart(PXAdapter adapter)
        {
            InventoryLines line = InventoryLine.Select(InventoryItemDetails.Current.InventoryID);
            
            if (line != null)
            {
                string customerPriceClass = ARPriceClass.EmptyPriceClass;
                Customer currentCustomer = ReadBAccount.ReadCurrentCustomer();

				Location location = PXSelectReadonly<Location,
				Where<Location.locationID, Equal<Required<Location.locationID>>,
					And<Location.bAccountID, Equal<Required<Location.bAccountID>>>>>.Select(this, currentCustomer.DefLocationID, currentCustomer.BAccountID);

				if (!string.IsNullOrEmpty(location?.CPriceClassID))
				{
					customerPriceClass = location.CPriceClassID;
				}

                CurrencyInfo info = new CurrencyInfo();
                info.CuryID = currentCustomer.CuryID;

                string curyRateTypeId = currentCustomer.CuryRateTypeID;
                if (String.IsNullOrEmpty(curyRateTypeId))
                    curyRateTypeId = new ARSetupSelect(this).Current.DefaultRateTypeID;
                if (String.IsNullOrEmpty(curyRateTypeId))
                    curyRateTypeId = new CMSetupSelect(this).Current.ARRateTypeDflt;
                info.CuryRateTypeID = curyRateTypeId;

                info = currencyinfo.Update(info);
                currencyinfo.Cache.Clear();
                currencyinfo.Cache.ClearQueryCache();
                decimal price =
                        ARSalesPriceMaint.CalculateSalesPrice(InventoryLine.Cache, customerPriceClass, currentCustomer.BAccountID,
                        InventoryItemDetails.Current.InventoryID,
						InventoryItemDetails.Current.SiteID,
                        info,
                        InventoryItemDetails.Current.UOM, InventoryItemDetails.Current.Qty, this.Accessinfo?.BusinessDate ?? DateTime.Now.Date, line.CuryUnitPrice) ?? 0m;


                var setting = PortalSetup.Current;
                if (setting != null)
                {
                    string message = null;
                    if (setting.BaseUOM != true && PXAccess.FeatureInstalled<FeaturesSet.multipleUnitMeasure>())
                    {
                        message = Messages.noUnitCost;
                    }
                    else
                    {
                        message = Messages.noCost;
                    }

                    if (price == 0)
                        throw new PXException(message);
                }

                InventoryLine.Cache.SetValueExt<InventoryLines.curyUnitPrice>(line, price);

                // есть ли уже такой товар в корзине для этого клиента?
                CardLines.Cache.Clear();
                PortalCardLines currentlineincart = CardLines.Select(PXAccess.GetUserID(), InventoryItemDetails.Current.SiteID,
                    InventoryItemDetails.Current.InventoryID, InventoryItemDetails.Current.UOM);
                
                WebDialogResult temp = InventoryItemDetails.Ask(Messages.ItemSuccessfullyAdded, PXMessages.LocalizeFormat(Messages.ItemAddedToCart, line.Descr), MessageButtons.YesNo);
                switch (temp)
                {
                    case WebDialogResult.Yes:
                        if (currentlineincart == null)
                        {
                            currentlineincart = CardLines.Cache.CreateInstance() as PortalCardLines;
                            currentlineincart.UserID = PXAccess.GetUserID();
                            currentlineincart.PortalNoteID = PortalSetup.Current.NoteID;
                            currentlineincart.SiteID = InventoryItemDetails.Current.SiteID;
                            currentlineincart.InventoryID = InventoryItemDetails.Current.InventoryID;
                            currentlineincart.InventoryIDDescription = line.InventoryCD;
                            currentlineincart.Descr = line.Descr;
                            currentlineincart.Qty = InventoryItemDetails.Current.Qty;
                            currentlineincart.UOM = InventoryItemDetails.Current.UOM;
                            currentlineincart.CuryUnitPrice = price;
                            currentlineincart.StkItem = line.StkItem;
                            using (new PXReadBranchRestrictedScope())
                            {
                                currentlineincart = CardLines.Insert(currentlineincart);
                            }
                            CalculateDiscountPriceCard(currentlineincart, (decimal)price);
                            CardLines.Cache.PersistInserted(currentlineincart);
                        }
                        else
                        {
                            currentlineincart.Qty = currentlineincart.Qty + InventoryItemDetails.Current.Qty;
                            currentlineincart = CardLines.Update(currentlineincart);
                            CalculateDiscountPriceCard(currentlineincart, (decimal)price);
                            CardLines.Cache.PersistUpdated(currentlineincart);
                        }
                        throw new PXRedirectToLastUrlException(PXGraph.CreateInstance<InventoryCardMaint>());

                    case WebDialogResult.No:
                        if (currentlineincart == null)
                        {
                            currentlineincart = CardLines.Cache.CreateInstance() as PortalCardLines;
                            currentlineincart.UserID = PXAccess.GetUserID();
                            currentlineincart.PortalNoteID = PortalSetup.Current.NoteID;
                            currentlineincart.SiteID = InventoryItemDetails.Current.SiteID;
                            currentlineincart.InventoryID = InventoryItemDetails.Current.InventoryID;
                            currentlineincart.InventoryIDDescription = line.InventoryCD;
                            currentlineincart.Descr = line.Descr;
                            currentlineincart.Qty = InventoryItemDetails.Current.Qty;
                            currentlineincart.UOM = InventoryItemDetails.Current.UOM;
                            currentlineincart.CuryUnitPrice = price;
                            currentlineincart.StkItem = line.StkItem;
                            using (new PXReadBranchRestrictedScope())
                            {
                                currentlineincart = CardLines.Insert(currentlineincart);
                            }
                            CalculateDiscountPriceCard(currentlineincart, (decimal)price);
                            CardLines.Cache.PersistInserted(currentlineincart);
                        }
                        else
                        {
                            currentlineincart.Qty = currentlineincart.Qty + InventoryItemDetails.Current.Qty;
                            currentlineincart = CardLines.Update(currentlineincart);
                            CalculateDiscountPriceCard(currentlineincart, (decimal)price);
                            CardLines.Cache.PersistUpdated(currentlineincart);
                        }
                        break;

                    default:
                        break;
                }
            }
            return adapter.Get();
        }
        #endregion
        
        #region Event Handlers
        protected virtual void InventoryItemDetails_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var setting = PortalSetup.Current;
            if (setting != null)
            {
                PXUIFieldAttribute.SetEnabled<InventoryItemDetails.uOM>(this.Caches[typeof(InventoryItemDetails)], null, (setting.BaseUOM != true && PXAccess.FeatureInstalled<FeaturesSet.multipleUnitMeasure>()));
            }
        }
        #endregion

		protected virtual Type GetQtyField()
		{
			return typeof(INSiteStatus.qtyAvail);
		}

        public void SetWarehouseColumn(bool needWarehouse)
        {
            string[] selFields = new string[8];
            string[] selHeaders = new string[8];
            selFields[0] = typeof(INSite.siteCD).Name;
            selHeaders[0] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(INSite)], typeof(INSite.siteCD).Name);

            if (needWarehouse)
            {
				var qtyField = GetQtyField().Name;
				selFields[1] = typeof(INSiteStatus).Name + "__" + qtyField;
				selHeaders[1] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(INSiteStatus)], qtyField);

                selFields[2] = typeof(INSite.descr).Name;
                selHeaders[2] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(INSite)], typeof(INSite.descr).Name);

                selFields[3] = typeof(Address).Name + "__" + typeof(Address.addressLine1).Name;
                selHeaders[3] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)],
                    typeof(Address.addressLine1).Name);

                selFields[4] = typeof(Address).Name + "__" + typeof(Address.addressLine2).Name;
                selHeaders[4] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)],
                    typeof(Address.addressLine2).Name);

                selFields[5] = typeof(Address).Name + "__" + typeof(Address.city).Name;
                selHeaders[5] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)], typeof(Address.city).Name);

                selFields[6] = typeof(Country).Name + "__" + typeof(Country.description).Name;
                selHeaders[6] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Country)],
                    typeof(Country.description).Name);

                selFields[7] = typeof(State).Name + "__" + typeof(State.name).Name;
                selHeaders[7] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(State)], typeof(State.name).Name);
            }
            else
            {
                selFields[1] = typeof(INSite.descr).Name;
                selHeaders[1] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(INSite)], typeof(INSite.descr).Name);

                selFields[2] = typeof(Address).Name + "__" + typeof(Address.addressLine1).Name;
                selHeaders[2] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)],
                    typeof(Address.addressLine1).Name);

                selFields[3] = typeof(Address).Name + "__" + typeof(Address.addressLine2).Name;
                selHeaders[3] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)],
                    typeof(Address.addressLine2).Name);

                selFields[4] = typeof(Address).Name + "__" + typeof(Address.city).Name;
                selHeaders[4] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)], typeof(Address.city).Name);

                selFields[5] = typeof(Country).Name + "__" + typeof(Country.description).Name;
                selHeaders[5] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Country)],
                    typeof(Country.description).Name);

                selFields[6] = typeof(State).Name + "__" + typeof(State.name).Name;
                selHeaders[6] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(State)], typeof(State.name).Name);
            }
            PXSelectorAttribute.SetColumns(Caches[typeof(InventoryItemDetails)], typeof(InventoryItemDetails.siteID).Name,
                selFields, selHeaders);
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

        public override IEnumerable ExecuteSelect(string viewName, object[] parameters, object[] searches, string[] sortcolumns, bool[] descendings, PXFilterRow[] filters, ref int startRow, int maximumRows, ref int totalRows)
        {
            if (viewName.Equals(_inventoryItemDetailsSiteIdViewName))
            {
                return InSite_dummy_for_search.View.Select(null, parameters, searches, sortcolumns, descendings, filters, ref startRow,
                    maximumRows, ref totalRows);
            }
            return base.ExecuteSelect(viewName, parameters, searches, sortcolumns, descendings, filters, ref startRow, maximumRows, ref totalRows);
        }
    }
}