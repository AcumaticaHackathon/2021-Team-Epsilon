using PX.Data;
using PX.Objects.CS;
using PX.Objects.Extensions;
using PX.Objects.PO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PODropShipLinksExt = PX.Objects.PO.GraphExtensions.POOrderEntryExt.DropShipLinksExt;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	public class AffectedDropShipOrdersBySOOrderEntry : ProcessAffectedEntitiesInPrimaryGraphBase<AffectedDropShipOrdersBySOOrderEntry, SOOrderEntry, SupplyPOOrder, POOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();
		}

		protected override bool PersistInSameTransaction => true;

		protected override bool EntityIsAffected(SupplyPOOrder entity)
		{
			if (entity.OrderType != POOrderType.DropShip || entity.IsLegacyDropShip == true)
				return false;

			var cache = Base.Caches<SupplyPOOrder>();
			int? activeLinksCountOldValue = (int?)cache.GetValueOriginal<SupplyPOOrder.dropShipActiveLinksCount>(entity);

			return entity.DropShipActiveLinksCount != activeLinksCountOldValue;
		}

		protected override void ProcessAffectedEntity(POOrderEntry primaryGraph, SupplyPOOrder entity)
		{
			POOrder order = POOrder.PK.Find(primaryGraph, entity.OrderType, entity.OrderNbr);
			PODropShipLinksExt ext = primaryGraph.GetExtension<PODropShipLinksExt>();
			ext.UpdateDocumentState(order);
		}

		protected override void ClearCaches(PXGraph graph)
		{
			graph.Caches<SupplyPOOrder>().Clear();
		}
	}
}
