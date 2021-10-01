using System;
using System.Collections;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using SP.Objects.IN;
using PX.Objects.AM;
using PX.Objects.AM.Attributes;

namespace SP.Objects.AM.GraphExtensions
{
    /// <summary>
    /// Manufacturing extension to "My Cart" (SP700001)
    /// </summary>
    [Serializable]
    public class InventoryCardMaintAMExtension : PXGraphExtension<InventoryCardMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<PX.Objects.CS.FeaturesSet.manufacturingProductConfigurator>();
        }

        [PXHidden]
        [PXCopyPasteHiddenView]
        public PortalConfigurationSelect<
            Where<AMConfigurationResults.createdByID, Equal<Current<PortalCardLines.userID>>,
                And<AMConfigurationResults.inventoryID, Equal<Current<PortalCardLines.inventoryID>>,
                    And<AMConfigurationResults.siteID, Equal<Current<PortalCardLines.siteID>>,
                        And<AMConfigurationResults.uOM, Equal<Current<PortalCardLines.uOM>>,
                            And<AMConfigurationResults.ordNbrRef, IsNull,
                                And<Current<AMConfigurationResults.opportunityQuoteID>, IsNull>>>>>>> ItemConfiguration;


        [PXMergeAttributes(Method = MergeMethod.Append)]
        [PXParent(typeof(Select<PortalCardLines, 
            Where<PortalCardLines.userID, Equal<Current<AMConfigurationResults.createdByID>>,
                And<PortalCardLines.inventoryID, Equal<Current<AMConfigurationResults.inventoryID>>,
                    And<PortalCardLines.siteID, Equal<Current<AMConfigurationResults.siteID>>,
                        And<PortalCardLines.uOM, Equal<Current<AMConfigurationResults.uOM>>,
                            And<Current<AMConfigurationResults.ordNbrRef>, IsNull,
                            And<Current<AMConfigurationResults.opportunityQuoteID>, IsNull>>>>>>>))]
        protected virtual void AMConfigurationResults_SiteID_CacheAttached(PXCache sender)
        {
        }

        #region Proceed to Checkout - Button

        public PXAction<PortalCardLine> ProceedToCheckOut;
        /// <summary>
        /// Override to InventoryCardMaint.proceedToCheckOut
        /// </summary>
        [PXUIField(DisplayName = "Proceed to Checkout")]
        [PXButton]
        public virtual IEnumerable proceedToCheckOut(PXAdapter adapter)
        {
            string configFinishedMessages;
            if (!ConfiguraitonsFinished(out configFinishedMessages))
            {
                throw new PXException(configFinishedMessages);
            }
            return Base.proceedToCheckOut(adapter);
        }

        #endregion

        /// <summary>
        /// Check for the existing configured lines for configuations not complete
        /// </summary>
        protected virtual bool ConfiguraitonsFinished(out string message)
        {
            var sb = new System.Text.StringBuilder();
            foreach (PXResult<PortalCardLines, InventoryItem, INSite, AMConfigurationResults> result in PXSelectJoin<PortalCardLines,
                InnerJoin<InventoryItem, 
                    On<PortalCardLines.inventoryID, Equal<InventoryItem.inventoryID>>,
                InnerJoin<INSite,
                        On<PortalCardLines.siteID, Equal<INSite.siteID>>,
                InnerJoin<AMConfigurationResults, 
                    On<PortalCardLines.userID, Equal<AMConfigurationResults.createdByID>,
                        And<PortalCardLines.inventoryID, Equal<AMConfigurationResults.inventoryID>,
                        And<PortalCardLines.siteID, Equal<AMConfigurationResults.siteID>,
                        And<PortalCardLines.uOM, Equal<AMConfigurationResults.uOM>>>>>>>>, 
                Where<PortalCardLines.userID, Equal<Required<PortalCardLines.userID>>,
                    And<AMConfigurationResults.completed, Equal<False>,
                    And<AMConfigurationResults.ordNbrRef, IsNull,
                    And<AMConfigurationResults.opportunityQuoteID, IsNull>>>>>.Select(Base, PXAccess.GetUserID()))
            {
#if DEBUG
                sb.Append($"[{((AMConfigurationResults) result).ConfigResultsID}] ");
#endif
                if (PXAccess.FeatureInstalled<FeaturesSet.warehouse>())
                {
                    sb.AppendLine(PX.Objects.AM.Messages.GetLocal(PX.Objects.AM.Messages.ConfiguraitonIncompleteSitePortal,
                        ((InventoryItem)result).InventoryCD.TrimIfNotNullEmpty(),
                        UomHelper.FormatQty(((PortalCardLines)result).Qty.GetValueOrDefault()),
                        ((PortalCardLines)result).UOM.TrimIfNotNullEmpty(),
                        ((INSite)result).SiteCD.TrimIfNotNullEmpty()));
                }
                else
                {
                    sb.AppendLine(PX.Objects.AM.Messages.GetLocal(PX.Objects.AM.Messages.ConfiguraitonIncompletePortal,
                        ((InventoryItem)result).InventoryCD.TrimIfNotNullEmpty(),
                        UomHelper.FormatQty(((PortalCardLines)result).Qty.GetValueOrDefault()),
                        ((PortalCardLines)result).UOM.TrimIfNotNullEmpty()));
                }
            }

            message = sb.ToString();
            return string.IsNullOrWhiteSpace(message);
        }

        public PXAction<PortalCardLine> ConfigureEntry;
        [PXButton(OnClosingPopup = PXSpecialButtonType.Cancel, Tooltip = "Launch configuration entry")]
        [PXUIField(DisplayName = PX.Objects.AM.Messages.Configure, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        public virtual void configureEntry()
        {
            if (Base.DocumentDetails.Current == null)
            {
                return;
            }

            var ext = Base.DocumentDetails.Current.GetExtension<PortalCardLinesExt>();
            if (!ext.AMIsConfigurable.GetValueOrDefault())
            {
                throw new PXException(PX.Objects.AM.Messages.NotConfigurableItem);
            }

            Base.Actions.PressSave();
            AMConfigurationResults configuration = ItemConfiguration.SelectWindowed(0, 1);
            if (configuration != null)
            {
                var configGraph = PXGraph.CreateInstance<ConfigurationEntry>();
                configGraph.Results.Current =
                    configGraph.Results.Search<AMConfigurationResults.configResultsID>(configuration.ConfigResultsID);
#if DEBUG
                PXTrace.WriteInformation($"Opening configuration result {configuration.ConfigResultsID} [{configuration.ConfigurationID.TrimIfNotNullEmpty()} / {configuration.Revision.TrimIfNotNullEmpty()}]");
#endif
                PXRedirectHelper.TryRedirect(configGraph, PXRedirectHelper.WindowMode.Popup);
            }
        }

        //Including only to change call to use local CalculateInfoWorkAround which uses CalculatePriceCardOverride in place of Base.CalculatePriceCard  (as its not virtual)
        protected virtual void PortalCardLine_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected del)
        {
            del?.Invoke(sender, e);

            PortalCardLine row = e.Row as PortalCardLine;
            CalculateInfoOverride(row);
        }

        //Including only to change call to use CalculatePriceCardOverride in place of Base.CalculatePriceCard  (as its not virtual)
        protected virtual void PortalCardLines_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected del)
        {
            var row = e.Row as PortalCardLines;
            if (row == null)
            {
                ConfigureEntry.SetEnabled(false);
                return;
            }

            del?.Invoke(sender, e);

            Base.DocumentDetails.Cache.SetValueExt<PortalCardLines.curyUnitPrice>(row, CalculatePriceCardOverride(row));
        }

        // Does not work as an override because the base class CalculatePriceCard is not virtual
        // Receives this error:
        //      Method System.Decimal CalculatePriceCard(SP.Objects.IN.PortalCardLines, System.Func`2[SP.Objects.IN.PortalCardLines,System.Decimal]) 
        //      in graph extension is marked as [PXOverride], but the original method with such name has not been found in PXGraph
        //[PXOverride]
        //public virtual decimal CalculatePriceCard(PortalCardLines row, Func<PortalCardLines, decimal> del)
        // ...
        // As a work around we are including all base calls to get around the non virtual method...
        public virtual decimal CalculatePriceCardOverride(PortalCardLines row)
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
                    return AMConfigurationPriceAttribute.GetPriceExt<AMConfigurationResults.displayPrice>(ItemConfiguration.Cache, configResult, ConfigCuryType.Document).GetValueOrDefault() + configResult.CurySupplementalPriceTotal.GetValueOrDefault();
                }
            }
            
            // All non configured line items...
            return Base.CalculatePriceCard(row);
        }

        //Including only to change call to use CalculatePriceCardOverride in place of Base.CalculatePriceCard  (as its not virtual)
        public void CalculateInfoOverride(PortalCardLine currentcart)
        {
            if (currentcart == null)
            {
                return;
            }

            Decimal currentpacktotalitem = 0;
            Decimal currentpacktotal = 0;

            foreach (PortalCardLines lines in Base.DocumentDetails.Select(PXAccess.GetUserID()))
            {
                currentpacktotalitem = currentpacktotalitem + lines.Qty.GetValueOrDefault();
                lines.CuryUnitPrice = CalculatePriceCardOverride(lines);
                currentpacktotal = currentpacktotal + lines.TotalPrice.GetValueOrDefault();
            }

            currentcart.CurrencyStatus = Base.currentCustomer.CuryID;

            currentcart.ItemTotal = currentpacktotalitem;
            currentcart.AllTotalPrice = currentpacktotal;
        }
    }
}