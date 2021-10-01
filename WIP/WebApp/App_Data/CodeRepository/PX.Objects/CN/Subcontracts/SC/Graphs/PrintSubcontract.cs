using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.CN.Common.Descriptor;
using PX.Objects.CN.Common.Helpers;
using PX.Objects.CS;
using PX.Objects.PO;
using Messages = PX.Objects.CN.Subcontracts.SC.Descriptor.Messages;

namespace PX.Objects.CN.Subcontracts.SC.Graphs
{
    public class PrintSubcontract : POPrintOrder
    {
        public PXAction<POPrintOrderFilter> ViewSubcontractDetails;

        public PrintSubcontract()
        {
            FeaturesSetHelper.CheckConstructionFeature();
            Records.WhereAnd<Where<POPrintOrderOwned.orderType.IsEqual<POOrderType.regularSubcontract>>>();
        }

        [PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute),
            Constants.AttributeProperties.DisplayName, Messages.Subcontract.SubcontractNumber)]
        public virtual void _(Events.CacheAttached<POPrintOrderOwned.orderNbr> e)
        {
        }

        [PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXEditDetailButton]
        public override IEnumerable Details(PXAdapter adapter)
        {
            if (Records.Current != null && Filter.Current != null)
            {
                OpenSubcontractDetails();
            }
            return adapter.Get();
        }

        [PXUIField(MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select, Enabled = false)]
        [PXButton]
        public virtual void viewSubcontractDetails()
        {
            if (Records.Current != null)
            {
                OpenSubcontractDetails();
            }
        }

        private void OpenSubcontractDetails()
        {
            var graph = CreateInstance<SubcontractEntry>();
            graph.Document.Current = Records.Current;
            throw new PXRedirectRequiredException(graph, true, Messages.ViewSubcontract)
            {
                Mode = PXBaseRedirectException.WindowMode.NewWindow
            };
        }

        public override bool IsPrintingAllowed(POPrintOrderFilter filter)
        {
            const string printSubcontract = "SC301000" + "$" + nameof(SubcontractEntry.printSubcontract);
            return PXAccess.FeatureInstalled<FeaturesSet.deviceHub>()
                && filter?.Action == printSubcontract;
        }
    }
}
