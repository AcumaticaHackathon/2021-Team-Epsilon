using PX.Objects.AM.Attributes;
using PX.Data;
using System.Linq;
using System.Collections.Generic;

namespace PX.Objects.AM
{
    /// <summary>
    /// Graph for building production details based on an Configuration
    /// </summary>
    public class ProductionConfigurationCopy : ProductionBomCopy
    {
        public PXSelect<AMConfigurationResults> ConfigResults;

        public override void CreateProductionDetails(AMProdItem amProdItem)
        {
            if (ConfigResults != null && ConfigResults.Current == null)
            {
                ConfigResults.Current = PXSelect<AMConfigurationResults,
                        Where<AMConfigurationResults.prodOrderType,
                            Equal<Required<AMConfigurationResults.prodOrderType>>,
                            And<AMConfigurationResults.prodOrderNbr,
                                Equal<Required<AMConfigurationResults.prodOrderNbr>>>>>
                    .Select(ProcessingGraph, amProdItem.OrderType, amProdItem.ProdOrdID);   
            }

            var config = ConfigResults?.Current;
            if (config == null || config.Completed != true)
            {
                DeleteProductionDetail(amProdItem);
                return;
            }

            base.CreateProductionDetails(amProdItem);
        }

        protected override void CopyMatl(OperationDetail operationDetail)
        {
            base.CopyMatl(operationDetail);

            if (!operationDetail.IsProdBom)
            {
                //we do not want to duplicate the material when finding phantom operations. 
                // Currently only material configured related to the configuration BOM should be copied.
                return;
            }

            // Before first save all config records are cache inserted and PXSelectJoin will not correctly return the records we need.
            if (ConfigResults?.Current?.ConfigResultsID < 0)
            {
                foreach (AMConfigResultsOption resultOption in ProcessingGraph.Caches<AMConfigResultsOption>().Cached.RowCast<AMConfigResultsOption>()
                                .Where(r => r.ConfigResultsID == ConfigResults?.Current?.ConfigResultsID && r.Included == true))
                {
                    if(!ProcessingGraph.Caches<AMConfigResultsOption>().IsRowInserted(resultOption))
                    {
                        continue;
                    }

                    foreach (AMConfigurationFeature feature in ProcessingGraph.Caches<AMConfigurationFeature>().Cached.RowCast<AMConfigurationFeature>()
                                .Where(r => r.ConfigurationID == resultOption.ConfigurationID && r.Revision == resultOption.Revision && r.LineNbr == resultOption.FeatureLineNbr)
                                .OrderBy(r => r.SortOrder))
                    {
                        foreach (AMConfigurationOption option in ProcessingGraph.Caches<AMConfigurationOption>().Cached.RowCast<AMConfigurationOption>()
                        .Where(r => r.ConfigurationID == feature.ConfigurationID && r.Revision == feature.Revision &&
                            r.ConfigFeatureLineNbr == feature.LineNbr && r.LineNbr == resultOption.OptionLineNbr &&
                            r.OperationID == operationDetail.BomOperationID)
                        .OrderBy(r => r.SortOrder).ThenBy(r => r.LineNbr))
                        {
                            CopyMatlInsertedRow(option, resultOption);
                        }
                    }
                }

                return;
            }

            var q = new PXSelectJoin<AMConfigurationResults,
                InnerJoin<AMConfigResultsOption,
                    On<AMConfigurationResults.configResultsID, Equal<AMConfigResultsOption.configResultsID>>,
                InnerJoin<AMConfigurationOption,
                    On<AMConfigResultsOption.configurationID, Equal<AMConfigurationOption.configurationID>,
                        And<AMConfigResultsOption.revision, Equal<AMConfigurationOption.revision>,
                        And<AMConfigResultsOption.featureLineNbr, Equal<AMConfigurationOption.configFeatureLineNbr>,
                        And<AMConfigResultsOption.optionLineNbr, Equal<AMConfigurationOption.lineNbr>>>>>,
                InnerJoin<AMConfigurationFeature,
                    On<AMConfigResultsOption.configurationID, Equal<AMConfigurationFeature.configurationID>,
                        And<AMConfigResultsOption.revision, Equal<AMConfigurationFeature.revision>,
                        And<AMConfigResultsOption.featureLineNbr, Equal<AMConfigurationFeature.lineNbr>>>>>>>,
                Where<AMConfigurationResults.configResultsID, Equal<Required<AMConfigurationResults.configResultsID>>,
                    And<AMConfigurationOption.operationID, Equal<Required<AMConfigurationOption.operationID>>>>,
                OrderBy<Asc<AMConfigurationFeature.sortOrder, Asc<AMConfigurationOption.sortOrder, Asc<AMConfigurationOption.lineNbr>>>>>(ProcessingGraph);

            foreach (PXResult<AMConfigurationResults,
                                AMConfigResultsOption,
                                    AMConfigurationOption,
                                        AMConfigurationFeature> configItem
                                        in q.Select(ConfigResults?.Current?.ConfigResultsID, operationDetail.BomOperationID))
            {
                var resultOption = ProcessingGraph.Caches<AMConfigResultsOption>().LocateElse((AMConfigResultsOption)configItem);
                CopyMatlInsertedRow(configItem, resultOption);
            }
        }

        protected virtual void CopyMatlInsertedRow(AMConfigurationOption option, AMConfigResultsOption resultOption)
        {
            if (option?.InventoryID == null || resultOption == null || resultOption.Included == false)
            {
                //Cannot import non inventory as material
                return;
            }

            if (option.MaterialType != AMMaterialType.Regular && option.MaterialType != AMMaterialType.Subcontract)
            {
                //Only regular material type.
                //  Phantoms loaded before operations built
                return;
            }

            var prodMatl = (AMProdMatl) ProcessingGraph.Caches<AMProdMatl>().Insert();
            if (prodMatl == null)
            {
                //var feature = (AMConfigurationFeature) configItem;
                PXTrace.WriteWarning(Messages.GetLocal(Messages.UnableToInsertProdMatlFromCfg, option?.Label, option.ConfigurationID, option.Revision, option.LineNbr));
                return;
            }

            prodMatl.Descr = option.Descr;
            prodMatl.InventoryID = option.InventoryID;
            prodMatl.SubItemID = option.SubItemID;
            prodMatl.MaterialType = option.MaterialType;
            prodMatl.PhantomRouting = option.PhantomRouting;
            prodMatl.BFlush = option.BFlush;
            prodMatl.ScrapFactor = resultOption.ScrapFactor;
            prodMatl.QtyReq = resultOption.Qty;
            prodMatl.UOM = option.UOM;
            prodMatl.QtyRoundUp = option.QtyRoundUp;
            prodMatl.BatchSize = option.BatchSize;

            if (option.SiteID != null)
            {
                prodMatl.SiteID = option.SiteID;
                prodMatl.WarehouseOverride = true;
                prodMatl.LocationID = option.LocationID;
            }

            if (prodMatl.SortOrder == null)
            {
                prodMatl.SortOrder = prodMatl.LineID;
                    
            }

            ProcessingGraph.Caches<AMProdMatl>().Update(prodMatl);
        }

        protected override void CopyBomsToProductionOrder()
        {
            base.CopyBomsToProductionOrder();
            CopyConfigurationAttributes(CurrentProdItem);
        }

        /// <summary>
        /// Copy configuration attributes to production attributes
        /// </summary>
        /// <param name="amProdItem">Production order record</param>
        protected virtual void CopyConfigurationAttributes(AMProdItem amProdItem)
        {
            if (amProdItem == null)
            {
                throw new PXArgumentException(nameof(amProdItem));
            }

            // Before first save all config records are cache inserted and PXSelectJoin will not correctly return the records we need.
            if (ConfigResults?.Current?.ConfigResultsID < 0)
            {
                foreach (AMConfigResultsAttribute attributeResult in ProcessingGraph.Caches<AMConfigResultsAttribute>().Cached.RowCast<AMConfigResultsAttribute>()
                                .Where(r => r.ConfigResultsID == ConfigResults?.Current?.ConfigResultsID))
                {
                    if(!ProcessingGraph.Caches<AMConfigResultsAttribute>().IsRowInserted(attributeResult))
                    {
                        continue;
                    }

                    foreach (AMConfigurationAttribute attribute in ProcessingGraph.Caches<AMConfigurationAttribute>().Cached.RowCast<AMConfigurationAttribute>()
                                .Where(r => r.ConfigurationID == attributeResult.ConfigurationID && r.Revision == attributeResult.Revision && 
                                    r.LineNbr == attributeResult.AttributeLineNbr)
                                .OrderBy(r => r.SortOrder))
                    {
                        CopyInsertedConfigurationAttributes(attribute, attributeResult);
                    }
                }

                return;
            }

            foreach (PXResult<AMConfigResultsAttribute, AMConfigurationAttribute, AMConfigurationResults> result in PXSelectJoin<
                AMConfigResultsAttribute,
                InnerJoin<AMConfigurationAttribute, 
                    On<AMConfigResultsAttribute.configurationID, Equal<AMConfigurationAttribute.configurationID>,
                    And<AMConfigResultsAttribute.revision, Equal<AMConfigurationAttribute.revision>,
                    And<AMConfigResultsAttribute.attributeLineNbr, Equal<AMConfigurationAttribute.lineNbr>>>>,
                LeftJoin<AMConfigurationResults, 
                    On<AMConfigResultsAttribute.configResultsID, Equal<AMConfigurationResults.configResultsID>>>>,
                Where<AMConfigurationResults.configResultsID, Equal<Required<AMConfigurationResults.configResultsID>>>>
                .Select(ProcessingGraph, ConfigResults?.Current?.ConfigResultsID))
            {
                var attributeResult = ProcessingGraph.Caches<AMConfigResultsAttribute>().LocateElse((AMConfigResultsAttribute) result);
                CopyInsertedConfigurationAttributes(result, attributeResult);
            }
        }

        protected virtual void CopyInsertedConfigurationAttributes(AMConfigurationAttribute attribute, AMConfigResultsAttribute attributeResult)
        {
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Label)
                    || attributeResult == null || string.IsNullOrWhiteSpace(attributeResult.AttributeID))
            {
                return;
            }

            var newProdAttribute = ProductionBomCopyMap.CopyAttributes(attribute, attributeResult);
            if (newProdAttribute == null
                || string.IsNullOrWhiteSpace(newProdAttribute.Label))
            {
                return;
            }

            newProdAttribute.OrderType = CurrentProdItem?.OrderType;
            newProdAttribute.ProdOrdID = CurrentProdItem?.ProdOrdID;
            TryInsertAMProdAttribute(newProdAttribute);
        }

        protected override int LoadingPhantomsFirstLevel(AMBomOper amBomOper, AMWC wc)
        {
            var index = base.LoadingPhantomsFirstLevel(amBomOper, wc);

            LoadConfigPhantoms(amBomOper, index);

            return index;
        }

        protected virtual void LoadConfigPhantoms(AMBomOper amBomOper, int currentIndex)
        {
            var q = new PXSelectJoin<AMConfigurationResults,
                            InnerJoin<AMConfigResultsOption,
                                On<AMConfigurationResults.configResultsID, Equal<AMConfigResultsOption.configResultsID>>,
                            InnerJoin<AMConfigurationOption,
                                On<AMConfigResultsOption.configurationID, Equal<AMConfigurationOption.configurationID>,
                                And<AMConfigResultsOption.revision, Equal<AMConfigurationOption.revision>,
                                And<AMConfigResultsOption.featureLineNbr, Equal<AMConfigurationOption.configFeatureLineNbr>,
                                And<AMConfigResultsOption.optionLineNbr, Equal<AMConfigurationOption.lineNbr>>>>>>>,
                                Where<AMConfigurationResults.configResultsID, Equal<Required<AMConfigurationResults.configResultsID>>,
                                    And<AMConfigResultsOption.included, Equal<True>,
                                    And<AMConfigurationOption.operationID, Equal<Required<AMConfigurationOption.operationID>>,
                                    And<AMConfigurationOption.materialType, Equal<AMMaterialType.phantom>>>>>, 
                                OrderBy<Asc<AMConfigurationOption.sortOrder, Asc<AMConfigurationOption.lineNbr>>>>(ProcessingGraph);

            foreach (PXResult<AMConfigurationResults,
                    AMConfigResultsOption,
                    AMConfigurationOption> result
                in q.Select(ConfigResults?.Current?.ConfigResultsID, amBomOper.OperationID))
            {
                BuildOperationByConfigPhantoms(result, result, amBomOper, currentIndex);
            }
        }

        protected virtual void BuildOperationByConfigPhantoms(AMConfigResultsOption resultOption, AMConfigurationOption option, AMBomOper bomOper, int currentIndex)
        {
            if (string.IsNullOrWhiteSpace(resultOption?.Revision)
                    || string.IsNullOrWhiteSpace(option?.Revision)
                    || option.InventoryID.GetValueOrDefault() == 0
                    || string.IsNullOrWhiteSpace(bomOper?.BOMID))
            {
                return;
            }

            if (CurrentProdItem?.SiteID == null)
            {
                throw new PXArgumentException("Current AMProdItem SiteID");
            }

            var bomId = new PrimaryBomIDManager(ProcessingGraph).GetPrimaryAllLevels(option.InventoryID, CurrentProdItem?.SiteID, resultOption.SubItemID);
            if (string.IsNullOrWhiteSpace(bomId))
            {
                return;
            }

            var bomItem = PrimaryBomIDManager.GetActiveRevisionBomItem(ProcessingGraph, bomId);

            if (bomItem?.RevisionID == null)
            {
                throw new PXException(Messages.NoActiveRevisionForBom, bomId);
            }

            BuildOperation(bomId,
                bomItem?.RevisionID, 
                option.ConfigurationID,
                option.Revision,
                option.LineNbr, 
                option.OperationID,
                resultOption.QtyRequired,
                option.PhantomRouting.GetValueOrDefault(), 
                1, 
                currentIndex);
        }
    }
}