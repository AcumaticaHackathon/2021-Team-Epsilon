using PX.Data;
using SP.Objects.IN;
using PX.Objects.AM;

namespace SP.Objects.AM
{
    public class PortalConfigurationSelect<TWhere> : PortalConfigurationSelect
        where TWhere : IBqlWhere, new()
    {
        public PortalConfigurationSelect(PXGraph graph) : base(graph)
        {
            base.View.WhereNew(typeof(TWhere));
        }
    }

    public class PortalConfigurationSelect : ConfigurationSelect
    {
        public PortalConfigurationSelect(PXGraph graph) : base(graph)
        {
        }

        protected override void AMConfigurationResults_RowInserting(PXCache cache, PXRowInsertingEventArgs e)
        {
            //SKIPPING BASE CALL
        }

        public static AMConfigurationResults GetConfigurationResult(PXGraph graph, PortalCardLines row)
        {
            return PXSelect<AMConfigurationResults,
                Where<AMConfigurationResults.createdByID, Equal<Current<PortalCardLines.userID>>,
                    And<AMConfigurationResults.inventoryID, Equal<Current<PortalCardLines.inventoryID>>,
                        And<AMConfigurationResults.siteID, Equal<Current<PortalCardLines.siteID>>,
                            And<AMConfigurationResults.uOM, Equal<Current<PortalCardLines.uOM>>,
                                And<AMConfigurationResults.ordNbrRef, IsNull,
                                    And<AMConfigurationResults.opportunityQuoteID, IsNull>>>>>>>.SelectSingleBound(graph, new object[] { row });
        }
    }
}