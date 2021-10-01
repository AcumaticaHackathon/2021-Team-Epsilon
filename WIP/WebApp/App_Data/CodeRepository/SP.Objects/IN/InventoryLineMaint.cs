using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PX.Api;
using PX.Common;
using PX.CS;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.IN.DAC;
using PX.Objects.Common.Discount;
using PX.Objects.Common.Discount.Mappers;
using PX.Objects.SP.DAC;
using PX.SM;
using SP.Objects.IN.DAC;

namespace SP.Objects.IN
{
    [Serializable]
    public class InventoryLineMaint : PXGraph<InventoryLineMaint>
	{
		protected ARSalesPriceMaint ARSalesPriceMaint => _arSalesPriceMaint.Value;
		private readonly Lazy<ARSalesPriceMaint> _arSalesPriceMaint = new Lazy<ARSalesPriceMaint>(CreateInstance<ARSalesPriceMaint>);
		private readonly string _inventoryLinesSiteIdViewName = string.Format("_Cache#{0}_{1}_{2}_", typeof(InventoryLines).FullName, nameof(InventoryLines.SiteID),  typeof(INSite.siteID).FullName);

		#region SelectCategoriesTree
		public class PXSelectCategoriesTree : PXSelectBase<INCategory>
        {
            public PXSelectCategoriesTree(PXGraph graph)
            {
                this.View = CreateView(graph, new PXSelectDelegate<int?>(categories));
            }

            public PXSelectCategoriesTree(PXGraph graph, Delegate handler)
			{
				this.View = CreateView(graph, handler);
			}
            
            private PXView CreateView(PXGraph graph, Delegate handler)
            {
                return new PXView(graph, false,
					new Select<INCategory,
						Where<INCategory.parentID, Equal<Argument<int?>>>,
						OrderBy<Asc<INCategory.sortOrder>>>(),
                    handler);
            }

            internal IEnumerable categories([PXInt] int? CategoryID)
            {
                if (CategoryID == null)
                    CategoryID = 0;
                
				foreach (var ret in PXSelect<INCategory,
					Where<INCategory.parentID, Equal<Required<INCategory.parentID>>>,
					OrderBy<Asc<INCategory.sortOrder>>>
					.Select(new PXGraph(), CategoryID))
                {
                    yield return ret;
                }
            }
        }
        #endregion
        
        #region Ctor
        public InventoryLineMaint()
        {
            if (PortalSetup.Current == null)
                throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

            //PortalSetup setting = portalSetup.Current;
            currentCustomer = ReadBAccount.ReadCurrentCustomer();

            bool isActive = currentCustomer.Status == CustomerStatus.Active;
            
            Actions["addToCart"].SetEnabled(isActive);
            Actions["openCart"].SetEnabled(isActive);

            PXSelectorAttribute.SetColumns<InventoryLines.uOM>(Caches[typeof(InventoryLines)], 
            new Type[]
			{
			    typeof (INUnit.fromUnit), 
                typeof (INUnitExt.convertionFactor
                )
			}, null );
            var setting = PortalSetup.Current;
            if (setting != null)
            {
                SetWarehouseColumn(setting.AvailableQty == true);
            }
            Actions["Cancel"].SetVisible(false);

            PXDimensionAttribute.SuppressAutoNumbering<PortalCardLines.inventoryIDDescription>(CardLines.Cache, true);
        }

        bool timestampSelected = false;
        #endregion

        #region Selects
        public PXFilter<InventoryLineFilter> Filter;

        [PXFilterable]
        public PXSelectOrderBy<InventoryLines, OrderBy<Asc<InventoryLines.inventoryCD>>> FilteredItems;

        public PXFilter<InventoryLineInfo> ItemsInfo;

        public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<Batch.curyInfoID>>>> currencyinfo;

        public PXSelect<PortalCardLine> CardLine;


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

		public SelectFrom<PortalCardLines>
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
			SimpleCardLines;

		public CRAttributeList<InventoryLines> Answers;

        public PXSelectCategoriesTree Categories;
        public PXSelect<INSiteStatus> InSiteStatus;
        public PXSelect<INSite> InSite;
		[PXHidden]
		public PXSelect<INSite> InSite_dummy_for_search;

        public Customer currentCustomer;
        #endregion

        #region Select Delegate
        public class LineIdentity
        {
            public int siteID;
            public int inventoryID;
        }

		[PXInternalUseOnly]
		public virtual BqlCommand GetFilteredItemsCommand()
		{
			BqlCommand cmd;

			if (Filter.Current.CategoryID == null)
			{
				cmd = BqlTemplate.OfCommand<Select<InventoryLines,
					Where<Current<InventoryLineFilter.isShowAvailableItem>, IsNull,
						Or<Current<InventoryLineFilter.isShowAvailableItem>, Equal<False>,
							Or<InventoryLines.stkItem, Equal<False>,
								Or<BqlPlaceholder.A, Greater<decimal0>>>>>>>
						.Replace<BqlPlaceholder.A>(GetQtyField())
						.ToCommand();
			}
			else
			{
				cmd = BqlTemplate.OfCommand<Select2<InventoryLines,
					InnerJoin<INItemCategory, On<INItemCategory.inventoryID, Equal<InventoryLines.inventoryID>>>,
					Where<INItemCategory.categoryID, Equal<Current<InventoryLineFilter.categoryID>>,
						And<
							Where<Current<InventoryLineFilter.isShowAvailableItem>, IsNull,
								Or<Current<InventoryLineFilter.isShowAvailableItem>, Equal<False>,
									Or<InventoryLines.stkItem, Equal<False>,
										Or<BqlPlaceholder.A, Greater<decimal0>>>>>>>>>
						.Replace<BqlPlaceholder.A>(GetQtyField())
						.ToCommand();
			}
			return cmd;
		}


		public virtual IEnumerable filteredItems()
        {
			BqlCommand cmd = GetFilteredItemsCommand();

			var startRow = PXView.StartRow;
            int totalRows = 0;

            var list = new PXView(PXView.CurrentGraph, false, cmd).
                Select(null, null, PXView.Searches, PXView.SortColumns, PXView.Descendings,
                    PXView.Filters,
                    ref startRow, PXView.MaximumRows, ref totalRows);
            
            PXView.StartRow = 0;

            return list;
        }
        
        #endregion

        #region Action
        public PXCancel<InventoryLineFilter> Cancel;

        public PXAction<InventoryLineFilter> AddToCart;
        [PXUIField(DisplayName = "Add To Cart")]
        [PXButton()]
        public virtual IEnumerable addToCart(PXAdapter adapter)
        {
			bool isInserted = false;
			bool isUpdated = false;

			var selectedLines = FilteredItems.Cache.Updated
				.OfType<InventoryLines>()
				.Where(x => x.Selected == true && x.Qty > 0)
				.ToList();

			foreach (InventoryLines line1 in selectedLines)
                {
					if(line1.SiteID == null)
						throw new PXException(Messages.NoAccessToWarehouseData);

					PortalCardLines cardLine = AddLineToCart(line1);
					if (cardLine == null)
						continue;
					var status = CardLines.Cache.GetStatus(cardLine);
					isInserted |= status == PXEntryStatus.Inserted;
					isUpdated |= status == PXEntryStatus.Updated;
					ClearRow(line1);
                }

			CalculateInfo(Filter.Current, ItemsInfo.Current);
			ItemsInfo.Current.Info = CalculateCardInfo(Filter.Current);

			PersistCardLines(isInserted, isUpdated);

			return adapter.Get();
        }

        public PXAction<InventoryLineFilter> OpenCart;
        [PXUIField(DisplayName = "Open Cart")]
        [PXButton()]
        public virtual IEnumerable openCart(PXAdapter adapter)
        {
            this.Clear();

            InventoryCardMaint graph = PXGraph.CreateInstance<InventoryCardMaint>();
            PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
            return adapter.Get();
        }

        public PXAction<InventoryLineFilter> ShowPicture;
        [PXUIField(DisplayName = "Show Picture", Visible = false)]
        [PXButton()]
        public virtual IEnumerable showPicture(PXAdapter adapter)
        {
            if (FilteredItems.Current != null)
				OpenInventory(FilteredItems.Current.InventoryID, FilteredItems.Current.SiteID, FilteredItems.Current.UOM);
            return adapter.Get();
        }

		public virtual void OpenInventory(int? inventoryID, int? siteID, string uom)
		{
			InventoryItem inventory = PXSelect<InventoryItem,
					Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>.SelectSingleBound(
						this, null, inventoryID);

			ImageViewer graph = CreateInstance<ImageViewer>();
			InventoryItem inventoryitem = graph.InventoryItem.Search<InventoryItem.inventoryID>(inventoryID);
			graph.InventoryItem.Cache.Current = inventoryitem;

			InventoryItemDetails inventoryitemdetails = graph.InventoryItemDetails.Search<InventoryItemDetails.inventoryID>(inventoryID);
			if (inventoryitemdetails == null)
			{
				inventoryitemdetails = graph.InventoryItemDetails.Insert();
				inventoryitemdetails.InventoryID = inventoryitem.InventoryID;

				inventoryitemdetails.InventoryDescription = graph.InventoryItem.Cache.GetValueExt<InventoryItem.inventoryCD>(inventory).ToString().TrimEnd() + "  -  " + inventory.Descr;
				inventoryitemdetails.Description = "Item Description";
				inventoryitemdetails.PictureNumber = 0;
				inventoryitemdetails.Qty = 1m;

				inventoryitemdetails.SiteID = siteID;
				inventoryitemdetails.UOM = uom;

				graph.InventoryItemDetails.Cache.PersistInserted(inventoryitemdetails);
			}

			else
			{
				inventoryitemdetails.InventoryDescription = graph.InventoryItem.Cache.GetValueExt<InventoryItem.inventoryCD>(inventory).ToString().TrimEnd() + "  -  " + inventory.Descr;
				inventoryitemdetails.Description = "Item Description";
				inventoryitemdetails.PictureNumber = 0;
				inventoryitemdetails.Qty = 1m;

				inventoryitemdetails.SiteID = siteID;
				inventoryitemdetails.UOM = uom;

				inventoryitemdetails = graph.InventoryItemDetails.Cache.Update(inventoryitemdetails) as InventoryItemDetails;
				graph.InventoryItemDetails.Cache.PersistUpdated(inventoryitemdetails);
			}
			graph.InventoryItemDetails.Cache.Current = inventoryitemdetails;
			PXRedirectHelper.TryRedirect(graph, inventoryitem, PXRedirectHelper.WindowMode.Popup);
		}

		public PXAction<InventoryLineFilter> RefreshHeader;
		[PXButton()]
		public virtual IEnumerable refreshHeader(PXAdapter adapter)
		{
			Filter.View.RequestRefresh();
			return adapter.Get();
		}

        #endregion



        #region Handler
        protected virtual void InventoryLineFilter_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
        {
            InventoryLineFilter row = e.Row as InventoryLineFilter;
            if (row != null)
            {
                row.CurrencyStatus = currentCustomer.CuryID;
            }
        }

        protected virtual void InventoryLineFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var setting = PortalSetup.Current;

            PXUIFieldAttribute.SetVisible<InventoryLineFilter.isShowAvailableItem>(Filter.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.inventory>() && setting.AvailableQty == true);
            
            foreach (PortalCardLines row1 in SimpleCardLines.Select(PXAccess.GetUserID()))
            {
                if (PriceTimeStamp != row1.PriceTimestamp)
                {
                    decimal price = CalculatePriceCard(row1);
                    Caches[typeof(PortalCardLines)].SetValueExt<PortalCardLines.curyUnitPrice>(row1, price);
                    Caches[typeof(PortalCardLines)].SetValueExt<PortalCardLines.priceTimestamp>(row1, PriceTimeStamp);
                }
            }
            CalculateInfo(Filter.Current, ItemsInfo.Current);
            ItemsInfo.Current.Info = CalculateCardInfo(Filter.Current);
        }

        protected virtual void InventoryLines_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            InventoryLines row = e.Row as InventoryLines;
            var setting = PortalSetup.Current;
            
            if (row != null)
            {
				if (row.SiteID == null)
				{
					object newValue;
					FilteredItems.Cache.RaiseFieldDefaulting<InventoryLines.siteID>(row, out newValue);
					if (newValue != null)
						sender.SetValue<InventoryLines.siteID>(row, (int)newValue);
				}

                if (row.Qty == null)
                {
                    object newValue1;
                    FilteredItems.Cache.RaiseFieldDefaulting<InventoryLines.qty>(row, out newValue1);
                    if (newValue1 != null)
						sender.SetValue<InventoryLines.qty>(row, (decimal)newValue1);
                }
				string qtyFieldName = GetQtyField().Name;
				if (sender.GetValue(row, qtyFieldName) == null && row.StkItem == true)
                {
                    object newValue1;
                    FilteredItems.Cache.RaiseFieldDefaulting(qtyFieldName, row, out newValue1);
					if (newValue1 != null)
						sender.SetValue(row, qtyFieldName, (decimal)newValue1);
                }
                if (row.UOM == null)
                {
                    object newValue2;
                    FilteredItems.Cache.RaiseFieldDefaulting<InventoryLines.uOM>(row, out newValue2);
                    if (newValue2 != null)
						sender.SetValue<InventoryLines.uOM>(row, (string)newValue2);
                }

                if (PriceTimeStamp != row.PriceTimestamp)
                {
                    decimal price = CalculatePrice(row);
                    CalculateDiscount(row, price);
                    FilteredItems.Cache.SetValueExt<InventoryLines.curyUnitPrice>(row, price);
                    FilteredItems.Cache.SetValueExt<InventoryLines.priceTimestamp>(row, PriceTimeStamp);
                }

				PXUIFieldAttribute.SetEnabled<InventoryLines.qty>(Caches[typeof(InventoryLines)], row, row.CuryUnitPrice != 0);
				PXUIFieldAttribute.SetEnabled<InventoryLines.selected>(Caches[typeof(InventoryLines)], row, row.CuryUnitPrice != 0);

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
					PXUIFieldAttribute.SetWarning<PortalCardLines.curyUnitPrice>(Caches[typeof(InventoryLines)], row,
					row.CuryUnitPrice == 0 ? message : null);
				}

				if (row.StkItem == true)
                {
                    if (setting != null && setting.AvailableQty == true)
                    {
                        PXUIFieldAttribute.SetWarning<InventoryLines.qty>(Caches[typeof(InventoryLines)], row, null);
                        // warning
                        if (row.Qty > row.CurrentWarehouse)
                            PXUIFieldAttribute.SetWarning<InventoryLines.qty>(Caches[typeof(InventoryLines)], row, Messages.deficiencyWarehouse);

                        if (row.Qty > row.TotalWarehouse)
                            PXUIFieldAttribute.SetWarning<InventoryLines.qty>(Caches[typeof(InventoryLines)], row, Messages.deficiencyWarehouses);
                    }
                }
            }

            PXUIVisibility viscurrent = PXUIVisibility.Invisible;
            PXUIVisibility vistotal = PXUIVisibility.Invisible;
            if (PXAccess.FeatureInstalled<FeaturesSet.warehouse>() && setting.AvailableQty == true)
                viscurrent = PXUIVisibility.Visible;
            if (setting.AvailableQty == true)
                vistotal = PXUIVisibility.Visible;
            

            PXUIFieldAttribute.SetVisible<InventoryLines.currentWarehouse>(FilteredItems.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>() && setting.AvailableQty == true);
            PXUIFieldAttribute.SetVisibility<InventoryLines.currentWarehouse>(FilteredItems.Cache, null, viscurrent);
            PXUIFieldAttribute.SetVisible<InventoryLines.totalWarehouse>(FilteredItems.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.inventory>() && setting.AvailableQty == true);
            PXUIFieldAttribute.SetVisibility<InventoryLines.totalWarehouse>(FilteredItems.Cache, null, vistotal);
            
            if (!PXAccess.FeatureInstalled<FeaturesSet.warehouse>())
            {
                PXUIFieldAttribute.SetDisplayName<InventoryLines.totalWarehouse>(FilteredItems.Cache, "Available Quantity");
            }
            
            PXUIFieldAttribute.SetEnabled<InventoryLines.inventoryID>(FilteredItems.Cache, null, false);
            PXUIFieldAttribute.SetEnabled<InventoryLines.currentWarehouse>(FilteredItems.Cache, null, false);

            PXUIFieldAttribute.SetEnabled<InventoryItem.inventoryID>(this.Caches[typeof(InventoryItem)], null, false);
            PXUIFieldAttribute.SetEnabled<InventoryItem.inventoryCD>(this.Caches[typeof(InventoryItem)], null, false);
            PXUIFieldAttribute.SetEnabled<INItemCategory.categoryID>(this.Caches[typeof(INItemCategory)], null, false);

            if (setting != null)
            {
                PXUIFieldAttribute.SetEnabled<InventoryLines.uOM>(this.Caches[typeof(InventoryLines)], null, (setting.BaseUOM != true && PXAccess.FeatureInstalled<FeaturesSet.multipleUnitMeasure>()));
            }

            foreach (PXCache _cache in this.Caches.Values)
            {
                _cache.IsDirty = false;
            }
        }

        protected virtual void InventoryLines_Selected_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            InventoryLines row = e.Row as InventoryLines;
            if (row != null)
            {
				if(row.Selected == true)
				{
					if((row.Qty ?? 0) == 0)
						sender.SetValue<InventoryLines.qty>(row, 1m);
				}
				else
					sender.SetValue<InventoryLines.qty>(row, 0m);
            }
        }

        protected virtual void InventoryLines_Qty_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            InventoryLines row = e.Row as InventoryLines;
            if (row != null)
				sender.SetValue<InventoryLines.selected>(row, row.Qty > 0);
        }

        protected virtual void InventoryLines_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            var setting = PortalSetup.Current;
            
            InventoryLines row = e.Row as InventoryLines;
            if (row != null)
            {
                decimal price = CalculatePrice(row);
                CalculateDiscount(row, price);
                sender.SetValueExt<InventoryLines.curyUnitPrice>(e.Row, price);              

                if (row.StkItem == true)
                {
                    if (setting != null && setting.AvailableQty == true)
                    {
                        PXUIFieldAttribute.SetWarning<InventoryLines.qty>(Caches[typeof(InventoryLines)], row, null);
                        // warning
                        if (row.Qty > row.CurrentWarehouse)
                            PXUIFieldAttribute.SetWarning<InventoryLines.qty>(Caches[typeof(InventoryLines)], row, Messages.deficiencyWarehouse);

                        if (row.Qty > row.TotalWarehouse)
                            PXUIFieldAttribute.SetWarning<InventoryLines.qty>(Caches[typeof(InventoryLines)], row, Messages.deficiencyWarehouses);

                    }
               }
            }
            CalculateInfo(Filter.Current, ItemsInfo.Current);
        }

		public virtual IEnumerable inSite_dummy_for_search()
		{
			List<int?> deletedBranches;
			using (new PXReadDeletedScope(true))
			{
				deletedBranches = PXDatabase.SelectMulti(typeof(PX.Objects.GL.Branch), new PXDataField("BranchID")).Select(b => b.GetInt32(0)).ToList();
			}
			IEnumerable<object> filteredItemsExt = null;
			using (new PXReadBranchRestrictedScope())
			{
				filteredItemsExt = new PXView(this, true, this.Views[_inventoryLinesSiteIdViewName].BqlSelect).SelectMulti();
			}
			
			return filteredItemsExt.Cast<PXResult>().Where(_ => deletedBranches.All(branchId => branchId != PXResult.Unwrap<INSite>(_).BranchID)).ToList<object>();
		}

		public override IEnumerable ExecuteSelect(string viewName, object[] parameters, object[] searches, string[] sortcolumns, bool[] descendings, PXFilterRow[] filters, ref int startRow, int maximumRows, ref int totalRows)
		{
			if (viewName.Equals(_inventoryLinesSiteIdViewName))
			{
				return InSite_dummy_for_search.View.Select(null, parameters, searches, sortcolumns, descendings, filters, ref startRow,
					maximumRows, ref totalRows);

			}
			return base.ExecuteSelect(viewName, parameters, searches, sortcolumns, descendings, filters, ref startRow, maximumRows, ref totalRows);
		}

		public override int ExecuteUpdate(string viewName, IDictionary keys, IDictionary values, params object[] parameters)
		{
			if (viewName.Equals(nameof(FilteredItems)))
			{
				using (new PXReadBranchRestrictedScope())
				{
					return base.ExecuteUpdate(viewName, keys, values, parameters);
				}
			}
			return base.ExecuteUpdate(viewName, keys, values, parameters);
		}
        #endregion

        #region Private Help Function

        private void ClearRow(InventoryLines row)
        {
			var copy = (InventoryLines)FilteredItems.Cache.CreateCopy(row);
            FilteredItems.Cache.SetDefaultExt<InventoryLines.selected>(copy);
            FilteredItems.Cache.SetDefaultExt<InventoryLines.qty>(copy);
			if (PXAccess.FeatureInstalled<FeaturesSet.inventory>())
			{
				object newValue;
				FilteredItems.Cache.RaiseFieldDefaulting<InventoryLines.siteID>(copy, out newValue);
				FilteredItems.Cache.SetValue<InventoryLines.siteID>(copy, (int)newValue);
			}
			FilteredItems.Update(copy);
		}

        public decimal CalculatePrice(InventoryLines row)
        {
            // цена
            Location location = PXSelect<Location, 
                Where<Location.locationID, Equal<Required<Location.locationID>>,
                    And<Location.bAccountID, Equal<Required<Location.locationID>>>>>.Select(this, currentCustomer.DefLocationID, currentCustomer.BAccountID);

            string customerPriceClass = ARPriceClass.EmptyPriceClass;
			if (!string.IsNullOrEmpty(location?.CPriceClassID))
			{
				customerPriceClass = location.CPriceClassID;
			}
			CurrencyInfo info = GetCurrencyInfo();

            decimal price = 
                ARSalesPriceMaint.CalculateSalesPrice(FilteredItems.Cache, customerPriceClass,
                    currentCustomer.BAccountID,
                    row.InventoryID,
					row.SiteID,
                    info,
                    row.UOM, row.Qty, this.Accessinfo?.BusinessDate ?? DateTime.Now.Date, 0) ?? 0m;

            return price;
        }

		public virtual CurrencyInfo GetCurrencyInfo()
		{
			CurrencyInfo info = new CurrencyInfo();
			info.CuryID = currentCustomer.CuryID;

			string curyRateTypeId = currentCustomer.CuryRateTypeID;
			if (string.IsNullOrEmpty(curyRateTypeId))
				curyRateTypeId = new ARSetupSelect(this).Current.DefaultRateTypeID;
			if (string.IsNullOrEmpty(curyRateTypeId))
				curyRateTypeId = new CMSetupSelect(this).Current.ARRateTypeDflt;

			info.CuryRateTypeID = curyRateTypeId;
			info = currencyinfo.Update(info);
			currencyinfo.Cache.Clear();
			currencyinfo.Cache.ClearQueryCache();
			return info;
		}


		public virtual void CalculateDiscount(InventoryLines row, decimal price)
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

	        DiscountEngine.SetLineDiscountOnly(FilteredItems.Cache, row,
                                new DiscountLineFields
                                    <DiscountLineFields.skipDisc, 
                                    InventoryLines.baseDiscountAmt, 
                                    InventoryLines.baseDiscountPct, 
                                    InventoryLines.baseDiscountID,
                                    InventoryLines.baseDiscountSeq,
									DiscountLineFields.discountsAppliedToLine,
									DiscountLineFields.manualDisc, 
                                    DiscountLineFields.manualPrice, 
                                    DiscountLineFields.lineType, 
                                    DiscountLineFields.isFree,
									DiscountLineFields.calculateDiscountsOnImport>(FilteredItems.Cache, row), 
                                    row.BaseDiscountID,
                                    price, 
                                    row.Qty * price, 
                                    row.Qty, 
                                    location?.LocationID, 
                                    currentCustomer.BAccountID, 
                                    currentCustomer.CuryID,
                                    (DateTime)PXTimeZoneInfo.Now, null, 
                                    row.InventoryID, 
                                    false);

            currencyinfo.Cache.Clear();
            currencyinfo.Cache.ClearQueryCache();
        }

        public decimal CalculatePriceCard(PortalCardLines row)
        {
            // цена
            Location location = PXSelect<Location,
                Where<Location.locationID, Equal<Required<Location.locationID>>,
                    And<Location.bAccountID, Equal<Required<Location.bAccountID>>>>>.Select(this, currentCustomer.DefLocationID, currentCustomer.BAccountID);

            string customerPriceClass = ARPriceClass.EmptyPriceClass;
			if (!string.IsNullOrEmpty(location?.CPriceClassID))
			{
				customerPriceClass = location.CPriceClassID;
			}

			CurrencyInfo info = GetCurrencyInfo();

            decimal price = 
                ARSalesPriceMaint.CalculateSalesPrice(Caches[typeof(PortalCardLines)], customerPriceClass,
                    currentCustomer.BAccountID,
                    row.InventoryID,
					row.SiteID,
                    info,
                    row.UOM, row.Qty, this.Accessinfo?.BusinessDate ?? DateTime.Now.Date, 0) ?? 0m;

            return price;
        }

        public virtual void CalculateDiscountPriceCard(PortalCardLines row, decimal price)
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

        public virtual string CalculateCardInfo(InventoryLineFilter row)
        {
            Decimal currentpacktotalitem = 0;
            Decimal currentpacktotal = 0;
            foreach (PortalCardLines lines in SimpleCardLines.Select(PXAccess.GetUserID()))
            {
                currentpacktotalitem = currentpacktotalitem + (decimal)lines.Qty;
                currentpacktotal = currentpacktotal + (decimal)lines.TotalPrice;
            }
            if (currentpacktotalitem == 0)
            {
                return PXMessages.LocalizeNoPrefix(Objects.Messages.EmptyCart);
            }
            else
            {
                // Общая информация
                Currency _currency = PXSelect<Currency,
                    Where<Currency.curyID, Equal<Required<Currency.curyID>>>>.SelectWindowed(this, 0, 1,
                        currentCustomer.CuryID);

                string stringformat = "";
                if (_currency != null)
                {
                    short format = (short)(_currency.DecimalPlaces);
                    while (format > 0)
                    {
                        format--;
                        stringformat = stringformat + '0';
                    }
                }
                stringformat = "{0:0." + stringformat + "}";
                string stringcurrentpacktotal = string.Format(stringformat, (double)currentpacktotal);
				return PXMessages.LocalizeFormatNoPrefix(Messages.YourCartContainsItemsFor, 
					(double)currentpacktotalitem, stringcurrentpacktotal, Filter.Current.CurrencyStatus);
            }
        }

        public virtual void CalculateInfo(InventoryLineFilter row, InventoryLineInfo ItemsInfo)
        {
            // Selection Total
            decimal total = 0;
            foreach (InventoryLines line in FilteredItems.Cache.Cached)
            {
                if (line != null)
                {
                    if (line.Selected == true)
                    {
                        if (line.TotalPrice != null)
                        {
                            total = (decimal)(total + line.TotalPrice);
                        }
                    }
                }
            }
            row.SelectionTotal = total;
            row.CurrencyStatus = currentCustomer.CuryID;
        }    

        public virtual void SetWarehouseColumn(bool needWarehouse)
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
                selHeaders[3] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)], typeof(Address.addressLine1).Name);

                selFields[4] = typeof(Address).Name + "__" + typeof(Address.addressLine2).Name;
                selHeaders[4] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)], typeof(Address.addressLine2).Name);

                selFields[5] = typeof(Address).Name + "__" + typeof(Address.city).Name;
                selHeaders[5] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)], typeof(Address.city).Name);

                selFields[6] = typeof(Country).Name + "__" + typeof(Country.description).Name;
                selHeaders[6] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Country)], typeof(Country.description).Name);

                selFields[7] = typeof(State).Name + "__" + typeof(State.name).Name;
                selHeaders[7] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(State)], typeof(State.name).Name);
            }
            else
            {
                selFields[1] = typeof(INSite.descr).Name;
                selHeaders[1] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(INSite)], typeof(INSite.descr).Name);

                selFields[2] = typeof(Address).Name + "__" + typeof(Address.addressLine1).Name;
                selHeaders[2] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)], typeof(Address.addressLine1).Name);

                selFields[3] = typeof(Address).Name + "__" + typeof(Address.addressLine2).Name;
                selHeaders[3] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)], typeof(Address.addressLine2).Name);

                selFields[4] = typeof(Address).Name + "__" + typeof(Address.city).Name;
                selHeaders[4] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Address)], typeof(Address.city).Name);

                selFields[5] = typeof(Country).Name + "__" + typeof(Country.description).Name;
                selHeaders[5] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(Country)], typeof(Country.description).Name);

                selFields[6] = typeof(State).Name + "__" + typeof(State.name).Name;
                selHeaders[6] = PXUIFieldAttribute.GetDisplayName(Caches[typeof(State)], typeof(State.name).Name);
            }
            PXSelectorAttribute.SetColumns(Caches[typeof(InventoryLines)], typeof(InventoryLines.siteID).Name, selFields, selHeaders);
        }

		private void PersistCardLines(bool inserted, bool updated)
		{
			if (inserted || updated)
			{
				using (PXTransactionScope ts = new PXTransactionScope())
				{
					if (inserted)
						CardLines.Cache.Persist(PXDBOperation.Insert);
					if (updated)
						CardLines.Cache.Persist(PXDBOperation.Update);
					ts.Complete(this);
				}

				CardLines.Cache.Persisted(false);
			}
		}

		public virtual PortalCardLines AddLineToCart(InventoryLines item)
		{
			if (item.Selected != true || (item.Qty ?? 0) <= 0 || (item.CuryUnitPrice ?? 0) == 0)
				return null;
			// предупредить если какой то из ключей пустой
			PortalCardLines currentlineincart = CardLines.Select(PXAccess.GetUserID(), item.SiteID,
				item.InventoryID, item.UOM);
			if (currentlineincart == null)
			{
				currentlineincart = CardLines.Cache.CreateInstance() as PortalCardLines;
				currentlineincart.UserID = PXAccess.GetUserID();
				currentlineincart.SiteID = item.SiteID;
				currentlineincart.PortalNoteID = PortalSetup.Current.NoteID;
				currentlineincart.InventoryID = item.InventoryID;
				currentlineincart.InventoryIDDescription = item.InventoryCD;
				currentlineincart.Descr = item.Descr;
				currentlineincart.Qty = item.Qty;
				currentlineincart.UOM = item.UOM;
				currentlineincart.CuryUnitPrice = (decimal)item.CuryUnitPrice;
				currentlineincart.CurrentWarehouse = item.CurrentWarehouse - item.Qty;
				currentlineincart.TotalWarehouse = item.TotalWarehouse - item.Qty;
				currentlineincart.StkItem = item.StkItem;
				using (new PXReadBranchRestrictedScope())
				{
					currentlineincart = CardLines.Insert(currentlineincart);
				}
			}
			else
			{
				currentlineincart.Qty = currentlineincart.Qty + item.Qty;
				currentlineincart.CurrentWarehouse = item.CurrentWarehouse - item.Qty;
				currentlineincart.TotalWarehouse = item.TotalWarehouse - item.Qty;
				currentlineincart = CardLines.Update(currentlineincart);
			}
			CalculateDiscountPriceCard(currentlineincart, (decimal)currentlineincart.CuryUnitPrice);
			return currentlineincart;
		}

		public virtual void MoveToCart(InventoryLines item)
		{
			PortalCardLines cardLine = AddLineToCart(item);
			if (cardLine == null)
				return;
			var status = CardLines.Cache.GetStatus(cardLine);
			var inserted = status == PXEntryStatus.Inserted;
			var updated = status == PXEntryStatus.Updated;
			if (!updated && !inserted)
				return;
			CalculateInfo(Filter.Current, ItemsInfo.Current);
			ItemsInfo.Current.Info = CalculateCardInfo(Filter.Current);

			PersistCardLines(inserted, updated);
		}
		#endregion

		protected virtual decimal? GetInventoryQtyOnSite(int? InventoryID, int? SiteID)
        {
            if (InventoryID == null || SiteID == null)
                return null;

            var inSiteStatus =
                new PXSelectGroupBy
                    <INSiteStatus,
                        Where<INSiteStatus.inventoryID, Equal<Required<INSiteStatus.inventoryID>>,
                            And<INSiteStatus.siteID, Equal<Required<INSiteStatus.siteID>>>>,
                        Aggregate<GroupBy<INSiteStatus.inventoryID, GroupBy<INSiteStatus.siteID,Sum<INSiteStatus.qtyOnHand, Sum<INSiteStatus.qtyAvail, Sum<INSiteStatus.qtyHardAvail>>>>>>>(this).Select(
                            InventoryID, SiteID);

            if (inSiteStatus.Count == 0)
                return null;
            if (inSiteStatus.Count == 1)
				return (decimal?)InSiteStatus.Cache.GetValue((INSiteStatus)inSiteStatus, GetQtyField().Name);

            throw new Exception("Query returned more than one row");
        }

		protected virtual Type GetQtyField()
		{
			return typeof(InventoryLines.qtyAvail);
		}

        private String PriceTimeStamp
        {
            get
            {
                
                if(!timestampSelected)
                {
                    PXDatabase.SelectTimeStamp();
                    timestampSelected = true;
                }
                Definition defs = PX.Common.PXContext.GetSlot<Definition>();
                if (defs == null)
                {
                    PXContext.SetSlot<Definition>(
                    defs = PXDatabase.GetSlot<Definition, PXGraph>("PriceDefinition", this,
                        new Type[]
                        {
                            typeof(ARSalesPrice),
                            typeof(InventoryItem)
                        })
                    );
                }
                return defs.PriceTimeStamp;
            }
        }

        public class Definition : IPrefetchable<PXGraph>
        {
            private String _PriceTimeStamp;
            public String PriceTimeStamp
            {
                get { return _PriceTimeStamp; }
            }

            public void Prefetch(PXGraph graph)
            {
                graph.Caches[typeof(InventoryItem)].ClearQueryCache();
                graph.Caches[typeof(ARSalesPrice)].ClearQueryCache();
                _PriceTimeStamp = System.Text.Encoding.Default.GetString(PXDatabase.Provider.SelectTimeStamp());
            }
        }
    }
}
