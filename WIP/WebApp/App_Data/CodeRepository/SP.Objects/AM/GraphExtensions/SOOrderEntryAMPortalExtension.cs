using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.AM;
using PX.Objects.AM.Attributes;
using PX.Objects.AM.CacheExtensions;
using PX.Objects.CM;
using PX.Objects.SO;
using SP.Objects.IN;

namespace SP.Objects.AM.GraphExtensions
{
    /// <summary>
    /// Portal extension for Manufacturing
    /// </summary>
    [Serializable]
    public class SOOrderEntryAMPortalExtension : PXGraphExtension<SOOrderEntryExt, SOOrderEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<PX.Objects.CS.FeaturesSet.manufacturingProductConfigurator>();
        }

        public PXSelect<AMConfigurationResults> ConfigResults;

        // Removing default Formula
        [PXDBCurrency(typeof(AMConfigurationResults.curyInfoID), typeof(AMConfigurationResults.bOMPriceTotal))]
        [PXUIField(DisplayName = "BOM Price Total", Visibility = PXUIVisibility.Visible, Enabled = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        protected virtual void _(Events.CacheAttached<AMConfigurationResults.curyBOMPriceTotal> e) { }

        // Removing default Formula
        [PXDBCurrency(typeof(AMConfigurationResults.curyInfoID), typeof(AMConfigurationResults.fixedPriceTotal))]
        [PXUIField(DisplayName = "Parent Price", Visibility = PXUIVisibility.Visible, Enabled = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        protected virtual void _(Events.CacheAttached<AMConfigurationResults.curyFixedPriceTotal> e) { }

        protected SOOrderEntryExt PortalExtension => Base.GetExtension<SOOrderEntryExt>();

        public PXAction<SOOrder> SubmitOrder;
        /// <summary>
        /// Override to SOOrderEntryExt.submitOrder
        /// </summary>
        [PXUIField(DisplayName = "Submit Order", Visible = false)]
        [PXButton]
        public virtual IEnumerable submitOrder(PXAdapter adapter)
        {
            // Get config detail...
            var configsToUpdate = GetConfiguraitonResults();

            var portalExt = Base.GetExtension<SOOrderEntryExt>();

            if (portalExt != null)
            {
                try
                {
                    return portalExt.submitOrder(adapter);
                }
                catch (PXReportRequiredException)
                {
                    // Here we will take the time to get the order number before re-throwing the report
                    UpdateConfiguraitonResults(Base.Document.Current, configsToUpdate);
                    Base.Actions.PressSave();

                    throw;
                }
            }
            return adapter.Get();
        }

        protected virtual List<AMConfigurationResults> GetConfiguraitonResults()
        {
            var list = new List<AMConfigurationResults>();
            foreach (PXResult<PortalCardLines, AMConfigurationResults> result in PXSelectJoin<PortalCardLines,
                        InnerJoin<AMConfigurationResults,
                            On<PortalCardLines.userID, Equal<AMConfigurationResults.createdByID>,
                                And<PortalCardLines.inventoryID, Equal<AMConfigurationResults.inventoryID>,
                                    And<PortalCardLines.siteID, Equal<AMConfigurationResults.siteID>,
                                        And<PortalCardLines.uOM, Equal<AMConfigurationResults.uOM>>>>>>,
                Where<PortalCardLines.userID, Equal<Required<PortalCardLines.userID>>,
                        And<AMConfigurationResults.ordNbrRef, IsNull,
                            And<AMConfigurationResults.opportunityQuoteID, IsNull>>>>.Select(Base, PXAccess.GetUserID()))
            {
                list.Add(LocateConfigResult(result));
            }
            return list;
        }

        protected AMConfigurationResults LocateConfigResult(AMConfigurationResults result)
        {
            return ConfigResults.Locate(result) ?? result;
        }

        protected SOLine LocateSOLine(SOLine soLine)
        {
            return Base.Transactions.Locate(soLine) ?? soLine;
        }

        protected virtual void UpdateConfiguraitonResults(SOOrder order, List<AMConfigurationResults> configs)
        {
            foreach (var config in configs)
            {
                config.OrdTypeRef = order.OrderType;
                config.OrdNbrRef = order.OrderNbr;
                config.CustomerID = order.CustomerID;
                config.CustomerLocationID = order.CustomerLocationID;
                config.CuryID = order.CuryID;
                config.CuryInfoID = order.CuryInfoID;
                ConfigResults.Update(config);
            }
        }

        public virtual void SOLine_RowInserted(PXCache sender, PXRowInsertedEventArgs e, PXRowInserted del)
        {
            del?.Invoke(sender, e);

            UpdateConfigResultReferences(sender, (SOLine)e.Row);
        }

        protected virtual void UpdateConfigResultReferences(PXCache sender, SOLine soLine)
        {
            if (soLine?.InventoryID == null || soLine.SiteID == null)
            {
                return;
            }

            var soLineExt = soLine.GetExtension<SOLineExt>();
            if (soLineExt == null || soLineExt.AMIsSupplemental.GetValueOrDefault())
            {
                return;
            }

            soLineExt.AMParentLineNbr = soLine.LineNbr;

            var configResult = GetPortalConfigurationResults(soLine);

            if (configResult == null)
            {
                Base.Transactions.Update(soLine);
                return;
            }

            configResult.OrdTypeRef = soLine.OrderType;
            configResult.OrdLineRef = soLine.LineNbr;
            configResult.Qty = soLine.Qty.GetValueOrDefault();

            ConfigResults.Update(configResult);

            soLineExt.AMConfigurationID = configResult.ConfigurationID;
            Base.Transactions.Update(soLine);

            InsertSupplementalItems(soLine, configResult);
        }

        protected void InsertSupplementalItems(SOLine parentConfigSOLine, AMConfigurationResults configResult)
        {
            if (parentConfigSOLine == null || configResult == null)
            {
                return;
            }

            ConfigSupplementalItemsHelper.AddSupplementalLineItems(parentConfigSOLine, Base, configResult);
        }

        protected virtual AMConfigurationResults GetPortalConfigurationResults(SOLine soLine)
        {
            return PXSelect<AMConfigurationResults,
                Where<AMConfigurationResults.createdByID, Equal<Required<AMConfigurationResults.createdByID>>,
                    And<AMConfigurationResults.inventoryID, Equal<Required<AMConfigurationResults.inventoryID>>,
                        And<AMConfigurationResults.siteID, Equal<Required<AMConfigurationResults.siteID>>,
                            And<AMConfigurationResults.uOM, Equal<Required<AMConfigurationResults.uOM>>,
                                And<AMConfigurationResults.ordNbrRef, IsNull,
                                    And<Current<AMConfigurationResults.opportunityQuoteID>, IsNull>>
                            >>>>>.Select(Base, PXAccess.GetUserID(), soLine.InventoryID, soLine.SiteID, soLine.UOM);
        }

        protected virtual void SOLine_CuryUnitPrice_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e, PXFieldVerifying del)
        {
            del?.Invoke(sender, e);

            var row = (SOLine)e.Row;
            var rowExt = row.GetExtension<SOLineExt>();
            if (row != null && rowExt != null)
            {
                var configResult = GetPortalConfigurationResults(row);
                if (configResult != null)
                {
                    var price = AMConfigurationPriceAttribute.GetPriceExt<AMConfigurationResults.displayPrice>(ConfigResults.Cache, configResult, ConfigCuryType.Document);
                    e.NewValue = price;
                }
            }
        }

        //Including only to change call to use CalculatePriceCardOverride in place of Base.CalculatePriceCard  (as its not virtual)
        //  Copy of same override limitation in InventoryCardMaintAMExtension
        protected virtual void PortalCardLines_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected del)
        {
            del?.Invoke(sender, e);

            PortalExtension?.DocumentCardDetails.Cache.SetValueExt<PortalCardLines.curyUnitPrice>(e.Row, CalculatePriceCardOverride((PortalCardLines)e.Row, Base.Document.Cache.Current as SOOrder));
        }

        // Does not work as an override because the base class CalculatePriceCard is not virtual
        // Receives this error:
        //      Method System.Decimal CalculatePriceCard(SP.Objects.IN.PortalCardLines, System.Func`2[SP.Objects.IN.PortalCardLines,System.Decimal]) 
        //      in graph extension is marked as [PXOverride], but the original method with such name has not been found in PXGraph
        //[PXOverride]
        //public virtual decimal CalculatePriceCard(PortalCardLines row, Func<PortalCardLines, decimal> del)
        // ...
        // As a work around we are including all base calls to get around the non virtual method...
        //  Copy of same override limitation in InventoryCardMaintAMExtension
        public virtual decimal CalculatePriceCardOverride(PortalCardLines row, SOOrder soOrder)
        {
            if (row == null)
            {
                return 0m;
            }

            var rowExt = row.GetExtension<PortalCardLinesExt>();
            if (rowExt != null && rowExt.AMIsConfigurable.GetValueOrDefault())
            {
                // For configured line items only...
                var configResult = PortalConfigurationSelect.GetConfigurationResult(Base, row);
                if (configResult != null)
                {
                    return AMConfigurationPriceAttribute.GetPriceExt<AMConfigurationResults.displayPrice>(ConfigResults.Cache, configResult, ConfigCuryType.Document).GetValueOrDefault() + configResult.CurySupplementalPriceTotal.GetValueOrDefault();
                }
            }

            // All non configured line items...
            return PortalExtension?.CalculatePriceCard(row, soOrder) ?? 0m;
        }
    }
}