using System;
using PX.Data;
using PX.Objects.AM;
using SP.Objects.IN;

namespace SP.Objects.AM.GraphExtensions
{
    [Serializable]
    public class InventoryLineMaintAMExtension : PXGraphExtension<InventoryLineMaint>
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

        [PXOverride]
        public virtual PortalCardLines AddLineToCart(InventoryLines item, Func<InventoryLines, PortalCardLines> del)
        {
            var cardLine = del?.Invoke(item);

            // really no other good place to do this until the base code is cleaned up allowing for better persist in transaction of private call PersistCardLines
            if (ItemConfiguration.IsDirty && IsCardLineInsertUpdated(cardLine))
            {
                ItemConfiguration.ConfigPersistInsertUpdate();
            }

            return cardLine;
        }

        protected virtual bool IsCardLineInsertUpdated(PortalCardLines cardLine)
        {
            if (cardLine == null)
            {
                return false;
            }

            var status = Base.CardLines.Cache.GetStatus(cardLine);
            return status == PXEntryStatus.Inserted || status == PXEntryStatus.Updated;
        }

        protected virtual void PortalCardLines_RowInserting(PXCache sender, PXRowInsertingEventArgs e, PXRowInserting del)
        {
            del?.Invoke(sender, e);

            PortalCardLines row = (PortalCardLines) e.Row;
            if (row == null)
            {
                return;
            }
            InsertConfigurationResult(sender, row);
        }
        
        private void InsertConfigurationResult(PXCache sender, PortalCardLines row)
        {
            if (row == null)
            {
                throw new PXArgumentException(nameof(row));
            }

            var rowExt = row.GetExtension<PortalCardLinesExt>();

            string configurationID = rowExt?.AMConfigurationID;
            if (string.IsNullOrWhiteSpace(configurationID))
            {
                if (!ConfigurationSelect.TryGetDefaultConfigurationID(Base, row.InventoryID, row.SiteID,
                    out configurationID))
                {
                    return;
                }

                if (rowExt != null)
                {
                    sender.SetValueExt<PortalCardLinesExt.aMConfigurationID>(row, configurationID);
                }
            }

            ItemConfiguration.Insert(new AMConfigurationResults
            {
                ConfigurationID = configurationID,
                // Using "createdByID" as the "UserID" key
                InventoryID = row.InventoryID,
                SiteID = row.SiteID,
                UOM = row.UOM,
                CuryInfoID = row.CuryInfoID,
                Qty = row.Qty.GetValueOrDefault(),
                CustomerID = Base.currentCustomer.BAccountID,
                CustomerLocationID = Base.currentCustomer.DefLocationID
            });
        }
    }
}