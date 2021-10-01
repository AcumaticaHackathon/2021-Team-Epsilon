using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using PX.Api;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.IN.DAC;
using PX.Objects.SO;
using PX.Objects.Common.Discount;
using PX.Objects.Common.Discount.Mappers;
using PX.Objects.SP.DAC;
using PX.SM;
using PX.TM;
using SP.Objects.IN.DAC;


namespace SP.Objects.IN
{
    [Serializable]
    public class InventoryCardMaint : PXGraph<InventoryCardMaint>
    {
        // Images
        public static readonly List<string> ImageTypes = new List<string>() { ".jpg", ".jpeg", ".jpe", ".gif", ".gif", ".png", ".bmp", ".tif", ".tiff", ".svg", ".ico" };
		protected ARSalesPriceMaint ARSalesPriceMaint => _arSalesPriceMaint.Value;
		private readonly Lazy<ARSalesPriceMaint> _arSalesPriceMaint = new Lazy<ARSalesPriceMaint>(CreateInstance<ARSalesPriceMaint>);

		public InventoryCardMaint()
        {
            if (PortalSetup.Current == null)
                throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

            currentCustomer = ReadBAccount.ReadCurrentCustomer();

            bool isActive = currentCustomer.Status == CustomerStatus.Active;

            if (!isActive && HttpContext.Current != null)
            {
                var url = string.Format("~/Frames/Error.aspx?exceptionID={0}&typeID={1}&errorcode={2}", HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.CustomerInactiveErrorMessage)),
                    HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ErrorType)), HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ConfigurationError)));
                var page = HttpContext.Current.CurrentHandler as Page;
                if (page != null && page.IsCallback)
                    throw new PXRedirectToUrlException(url, "");
                else
                    HttpContext.Current.Response.Redirect(url);
            }
           
            
            Actions["Cancel"].SetVisible(false);
        }

        #region Selects
        public PXSelect<PortalCardLines> DocumentDetails;
        public PXFilter<PortalCardLine> Document;
        public Customer currentCustomer;

        public PXSelect<CurrencyInfo> currencyinfo;

		public virtual IEnumerable documentDetails()
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
				.Select(this, PXAccess.GetUserID()))
			{
				yield return result[typeof(PortalCardLines)];
			}
		}
		#endregion

        #region Action
        public PXCancel<PortalCardLine> Cancel;

        public PXAction<PortalCardLine> ProceedToCheckOut;
        [PXUIField(DisplayName = "Proceed to Checkout")]
        [PXButton()]
        public virtual IEnumerable proceedToCheckOut(PXAdapter adapter)
        {
            this.DocumentDetails.Cache.Persist(PXDBOperation.Update);
            this.DocumentDetails.Cache.Persist(PXDBOperation.Insert);
            this.DocumentDetails.Cache.Persist(PXDBOperation.Delete);
            foreach (PXCache cache in Caches.Values)
            {
                cache.IsDirty = false;
            }

            SOOrderEntry graph = PXGraph.CreateInstance<SOOrderEntry>();
            SOOrder soorder = graph.Document.Cache.CreateInstance() as SOOrder;
            soorder = graph.Document.Insert() as SOOrder;

            graph.Document.Cache.SetValueExt<SOOrderExt.isSecondScreen>(soorder, 1);

            /*Customer currentCustomer = ReadBAccount.ReadCurrentCustomer();
            graph.Document.Cache.SetValueExt<SOOrder.orderType>(soorder, _setsetting.DefaultOrderType);
			graph.Document.Cache.Normalize();
            graph.Document.Cache.SetValueExt<SOOrder.customerID>(soorder, currentCustomer.BAccountID);
            graph.Document.Cache.SetValueExt<SOOrder.branchID>(soorder, PXAccess.GetBranchID());
            graph.Document.Cache.SetValueExt<SOOrder.curyID>(soorder, currentCustomer.CuryID);

            // давим  FieldVerifying на branchID так как такого бранча нет
            graph.FieldVerifying.AddHandler<SOLine.branchID>(
                (PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
            graph.FieldVerifying.AddHandler<SOOrder.branchID>(
                (PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
            graph.FieldVerifying.AddHandler<SOOrder.customerLocationID>(
                (PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });

            soorder = graph.Document.Cache.Update(soorder) as SOOrder;

            foreach (PortalCardLines lines in DocumentDetails.Select())
            {
                SOLine soline = graph.Transactions.Cache.CreateInstance() as SOLine;
                soline.InventoryID = lines.InventoryID;
                soline.SiteID = lines.SiteID;
                soline.Qty = lines.Qty;
                soline.UOM = lines.UOM;
                soline.BranchID = PXAccess.GetBranchID();
                if (PXAccess.FeatureInstalled<FeaturesSet.subItem>() && lines.StkItem == true)
                {
                    soline.SubItemID = _setsetting.DefaultSubItemID;
                }
                try
                {
                    soline = graph.Transactions.Cache.Insert(soline) as SOLine;
                }
                catch (Exception e)
                {
                    this.DocumentDetails.Cache.RaiseExceptionHandling<PortalCardLines.inventoryIDDescription>(lines, lines.InventoryIDDescription, new Exception(e.Message));
                    throw;
                }
            }*/

            PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
            return adapter.Get();
        }

        public PXAction<PortalCardLine> GoToCatalog;
        [PXUIField(DisplayName = "Continue Shopping")]
        [PXButton()]
        public virtual IEnumerable goToCatalog(PXAdapter adapter)
        {
            this.Actions.PressSave();
            InventoryLineMaint graph = PXGraph.CreateInstance<InventoryLineMaint>();
            PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
            return adapter.Get();
        }

        public PXAction<PortalCardLine> DeleteAllRow;
        [PXUIField(DisplayName = "Clear Cart")]
        [PXButton()]
        public virtual IEnumerable deleteAllRow(PXAdapter adapter)
        {
            if (Document.Ask(Messages.ClearCart, PXMessages.LocalizeNoPrefix(Objects.Messages.ClearCartMessage), MessageButtons.YesNo) ==
                WebDialogResult.Yes)
            {
                foreach (PortalCardLines lines in DocumentDetails.Select(PXAccess.GetUserID()))
                {
                    DocumentDetails.Cache.Delete(lines);
                }
                this.Actions.PressSave();
                InventoryLineMaint graph = PXGraph.CreateInstance<InventoryLineMaint>();
                PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
            }
            return adapter.Get();
        }

        public PXAction<PortalCardLine> DeleteRow;
        [PXUIField(DisplayName = "Delete")]
        [PXButton(Tooltip = Objects.Messages.DeletefromCart)]
        public virtual IEnumerable deleteRow(PXAdapter adapter)
        {
            DocumentDetails.Cache.Delete(DocumentDetails.Current);
            this.Actions.PressSave();
            return adapter.Get();
        }


        public PXAction<PortalCardLine> ShowPicture;
        [PXUIField(DisplayName = "Show Picture", Visible = false)]
        [PXButton()]
        public virtual IEnumerable showPicture(PXAdapter adapter)
        {
            this.Actions.PressSave();
            if (DocumentDetails.Current != null)
            {
                InventoryItem inventory = PXSelect<InventoryItem,
                    Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>.SelectSingleBound(
                        this, null, DocumentDetails.Current.InventoryID);

                ImageViewer graph = CreateInstance<ImageViewer>();
                InventoryItem inventoryitem = graph.InventoryItem.Search<InventoryItem.inventoryID>(DocumentDetails.Current.InventoryID);
                graph.InventoryItem.Cache.Current = inventoryitem;

                InventoryItemDetails inventoryitemdetails = graph.InventoryItemDetails.Search<InventoryItemDetails.inventoryID>(DocumentDetails.Current.InventoryID);
                if (inventoryitemdetails == null)
                {
                    inventoryitemdetails = graph.InventoryItemDetails.Insert();
                    inventoryitemdetails.InventoryID = inventoryitem.InventoryID;

					inventoryitemdetails.InventoryDescription = graph.InventoryItem.Cache.GetValueExt<InventoryItem.inventoryCD>(inventory).ToString().TrimEnd() + "  -  " + inventory.Descr;
					inventoryitemdetails.Description = "Item Description";
                    inventoryitemdetails.PictureNumber = 0;
                    inventoryitemdetails.Qty = 1;

                    inventoryitemdetails.SiteID = DocumentDetails.Current.SiteID;
                    inventoryitemdetails.UOM = DocumentDetails.Current.UOM;

                    graph.InventoryItemDetails.Cache.PersistInserted(inventoryitemdetails);
                }

                else
                {
					inventoryitemdetails.InventoryDescription = graph.InventoryItem.Cache.GetValueExt<InventoryItem.inventoryCD>(inventory).ToString().TrimEnd() + "  -  " + inventory.Descr;
					inventoryitemdetails.Description = "Item Description";
                    inventoryitemdetails.PictureNumber = 0;
                    inventoryitemdetails.Qty = 1;

                    inventoryitemdetails.SiteID = DocumentDetails.Current.SiteID;
                    inventoryitemdetails.UOM = DocumentDetails.Current.UOM;

                    inventoryitemdetails = graph.InventoryItemDetails.Cache.Update(inventoryitemdetails) as InventoryItemDetails;
                    graph.InventoryItemDetails.Cache.PersistUpdated(inventoryitemdetails);
                }
                graph.InventoryItemDetails.Cache.Current = inventoryitemdetails;
                PXRedirectHelper.TryRedirect(graph, inventoryitem, PXRedirectHelper.WindowMode.Popup);
                return adapter.Get();
            }
            return adapter.Get();
        }
        #endregion

        #region Handler
        protected virtual void PortalCardLines_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            PortalCardLines row = e.Row as PortalCardLines;

            InventoryLines _inventoryLines = PXSelect<InventoryLines,
                    Where<InventoryLines.inventoryID, Equal<Required<InventoryLines.inventoryID>>>>.SelectWindowed(this, 0, 1, row.InventoryID);
            if (_inventoryLines != null)
            {
                if (_inventoryLines.StkItem == true)
                {
                    DocumentDetails.Cache.SetValueExt<PortalCardLines.currentWarehouse>(row,
                        CalculateCurrentWarehouse(row));
                    DocumentDetails.Cache.SetValueExt<PortalCardLines.totalWarehouse>(row,
                        CalculateTotaltWarehouse(row));

                    // warning
                    if (PortalSetup.Current.AvailableQty == true)
                    {
                        PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Caches[typeof(PortalCardLines)], row, null);
                        
                        if (row.Qty > row.CurrentWarehouse)
                            PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Caches[typeof (PortalCardLines)], row,
                                Messages.deficiencyWarehouse);

                        if (row.Qty > row.TotalWarehouse)
                            PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Caches[typeof (PortalCardLines)], row,
                                Messages.deficiencyWarehouses);
                    }
                }
                else
                {
                    DocumentDetails.Cache.SetValueExt<PortalCardLines.currentWarehouse>(row, null);
                    DocumentDetails.Cache.SetValueExt<PortalCardLines.totalWarehouse>(row, null);
                }
            }

            CalculateDiscountPriceCard(row, (decimal)row.CuryUnitPrice);
            this.Actions.PressSave();
        }
        #endregion

        protected virtual void PortalCardLine_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            PortalCardLine row = e.Row as PortalCardLine;
            CalculateInfo(row);
            
            Actions["ProceedToCheckOut"].SetEnabled(DocumentDetails.Select().Count > 0);

            PXUIFieldAttribute.SetVisible<PortalCardLines.currentWarehouse>(DocumentDetails.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>() && PortalSetup.Current.AvailableQty == true);
            PXUIFieldAttribute.SetVisible<PortalCardLines.totalWarehouse>(DocumentDetails.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>() && PortalSetup.Current.AvailableQty == true);
            if (!PXAccess.FeatureInstalled<FeaturesSet.warehouse>())
            {
                PXUIFieldAttribute.SetDisplayName<PortalCardLines.totalWarehouse>(DocumentDetails.Cache, "Available Quantity");
            }
        }

        protected virtual void PortalCardLines_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            PortalCardLines row = e.Row as PortalCardLines;
            if (row != null)
            {
                // цена
                decimal price = CalculatePriceCard(row);
                DocumentDetails.Cache.SetValueExt<PortalCardLines.curyUnitPrice>(row, price);

                InventoryLines _inventoryLines = PXSelect<InventoryLines,
                Where<InventoryLines.inventoryID, Equal<Required<InventoryLines.inventoryID>>>>.SelectWindowed(this, 0, 1, row.InventoryID);
                if (_inventoryLines != null)
                {
                    if (_inventoryLines.StkItem == true)
                    {
                        DocumentDetails.Cache.SetValueExt<PortalCardLines.currentWarehouse>(row, CalculateCurrentWarehouse(row));
                        DocumentDetails.Cache.SetValueExt<PortalCardLines.totalWarehouse>(row, CalculateTotaltWarehouse(row));

                        // warning
                        if (PortalSetup.Current.AvailableQty == true)
                        {
                            PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Caches[typeof(PortalCardLines)], row, null);
                            if (row.Qty > row.CurrentWarehouse)
                                PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Caches[typeof(PortalCardLines)], row,
                                    Messages.deficiencyWarehouse);

                            if (row.Qty > row.TotalWarehouse)
                                PXUIFieldAttribute.SetWarning<PortalCardLines.qty>(Caches[typeof(PortalCardLines)], row,
                                    Messages.deficiencyWarehouses);
                        }
                    }
                    else
                    {
                        DocumentDetails.Cache.SetValueExt<PortalCardLines.currentWarehouse>(row, null);
                        DocumentDetails.Cache.SetValueExt<PortalCardLines.totalWarehouse>(row, null);
                    }
                }
            }
        }
        
        #region Private Help Function
        public decimal CalculatePriceCard(PortalCardLines row)
        {
            // цена
            string customerPriceClass = ARPriceClass.EmptyPriceClass;

            Location location = PXSelect<Location,
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

            decimal price =
                ARSalesPriceMaint.CalculateSalesPrice(Caches[typeof(PortalCardLines)], customerPriceClass,
                    currentCustomer.BAccountID,
                    row.InventoryID,
					row.SiteID,
                    info,
                    row.UOM, row.Qty, this.Accessinfo?.BusinessDate ?? DateTime.Now.Date, 0) ?? 0m;

            currencyinfo.Cache.Clear();
            currencyinfo.Cache.ClearQueryCache();
            return price;
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

	        DiscountEngine.SetLineDiscountOnly(DocumentDetails.Cache, row,
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
									DiscountLineFields.calculateDiscountsOnImport>(DocumentDetails.Cache, row),
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


        public decimal CalculateTotaltWarehouse(PortalCardLines row)
        {
            decimal total = 0;

            InventoryItem inventoryItem = PXSelect<InventoryItem,
                Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                .SelectWindowed(this, 0, 1, row.InventoryID);

            INUnit inUnit = PXSelect<INUnit,
                Where<INUnit.inventoryID, Equal<Required<INUnit.inventoryID>>,
                And<INUnit.fromUnit, Equal<Required<INUnit.fromUnit>>,
                And<INUnit.toUnit, Equal<Required<INUnit.toUnit>>>>>>
                .SelectWindowed(this, 0, 1, row.InventoryID, row.UOM, inventoryItem.BaseUnit);

            decimal unitRate = 1;
            if (inUnit != null)
                unitRate = (decimal)inUnit.UnitRate;

            foreach (INSiteStatus VARIABLE in
                PXSelect
                    <INSiteStatus, Where<INSiteStatus.inventoryID, Equal<Required<INSiteStatus.inventoryID>>>>
                    .Select(this, row.InventoryID))
            {
                WarehouseReference warehouse = PXSelect<WarehouseReference,
                  Where<WarehouseReference.siteID, Equal<Required<INSite.siteID>>,
                        And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>
                  .SelectWindowed(this, 0, 1, VARIABLE.SiteID);

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
			decimal qty = (decimal?)Caches[typeof(INSiteStatus)].GetValue(iNSiteStatus, GetQtyField().Name) ?? 0m;
			return (decimal)(qty / unitRate);
		}

        public decimal CalculateCurrentWarehouse(PortalCardLines row)
        {
            INSiteStatus iNSiteStatus = PXSelect<INSiteStatus,
                Where<INSiteStatus.siteID, Equal<Required<INSiteStatus.siteID>>,
                    And<INSiteStatus.inventoryID, Equal<Required<INSiteStatus.inventoryID>>>>>
                .SelectWindowed(this, 0, 1, row.SiteID, row.InventoryID);

             WarehouseReference warehouse = PXSelect<WarehouseReference,
                Where<WarehouseReference.siteID, Equal<Required<INSite.siteID>>,
                      And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>
                .SelectWindowed(this, 0, 1, row.SiteID);

            InventoryItem inventoryItem = PXSelect<InventoryItem,
                Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                .SelectWindowed(this, 0, 1, row.InventoryID);

            INUnit inUnit = PXSelect<INUnit,
                Where<INUnit.inventoryID, Equal<Required<INUnit.inventoryID>>,
                And<INUnit.fromUnit, Equal<Required<INUnit.fromUnit>>,
                And<INUnit.toUnit, Equal<Required<INUnit.toUnit>>>>>>
                .SelectWindowed(this, 0, 1, row.InventoryID, row.UOM, inventoryItem.BaseUnit);

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

        public void CalculateInfo(PortalCardLine currentcart)
        {
            // пересчитаем общую информацию
            if (currentcart != null)
            {
                Decimal currentpacktotalitem = 0;
                Decimal currentpacktotal = 0;

                foreach (PortalCardLines lines in DocumentDetails.Select(PXAccess.GetUserID()))
                {
                    currentpacktotalitem = currentpacktotalitem + (decimal)lines.Qty;
                    lines.CuryUnitPrice = CalculatePriceCard(lines);
                    currentpacktotal = currentpacktotal + (decimal)lines.TotalPrice;
                }

                currentcart.CurrencyStatus = currentCustomer.CuryID;

                currentcart.ItemTotal = currentpacktotalitem;
                currentcart.AllTotalPrice = currentpacktotal;
            }
        }

        private List<UploadFile> TakeImageFiles(Guid[] files)
        {
            UploadFile file = null;
            List<UploadFile> listfiles = new List<UploadFile>();
            foreach (Guid guidfile in files)
            {
                file =

                    PXSelect<UploadFile, Where<UploadFile.fileID, Equal<Required<UploadFile.fileID>>>>
                        .SelectWindowed(this, 0, 1, guidfile);

                int index = file.Name.LastIndexOf('.');
                if (index > -1)
                {
                    string currentType = file.Name.Remove(0, index);
                    if (ImageTypes.Contains(currentType))
                        listfiles.Add(file);
                }
            }

            if (listfiles.Count == 0)
            {
                Guid[] defaultfiles = PXNoteAttribute.GetFileNotes(this.Caches[typeof(PortalSetup)], PortalSetup.Current);
                if (defaultfiles != null && defaultfiles.Length > 0)
                {
                    file =
                       PXSelect<UploadFile, Where<UploadFile.fileID, Equal<Required<UploadFile.fileID>>>>
                           .SelectWindowed(this, 0, 1, defaultfiles[defaultfiles.Length - 1]);

                    listfiles.Add(file);
                }
            }
            return listfiles;
        }
        #endregion
    }
}
