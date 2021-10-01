using PX.Data;
using PX.Objects.Extensions;
using PX.Objects.PO;

namespace PX.Objects.PO.GraphExtensions
{
	public abstract class AffectedPOOrdersByPOLineUOpen<TSelf, TGraph> : ProcessAffectedEntitiesInPrimaryGraphBase<TSelf, TGraph, POOrder, POOrderEntry>
		where TGraph : PXGraph
		where TSelf : AffectedPOOrdersByPOLineUOpen<TSelf, TGraph>
	{
		protected override bool PersistInSameTransaction => true;

		protected override bool EntityIsAffected(POOrder entity)
		{
			var cache = Base.Caches<POOrder>();
			int? linesToCloseCntrOldValue = (int?)cache.GetValueOriginal<POOrder.linesToCloseCntr>(entity),
				linesToCompleteCntrOldValue = (int?)cache.GetValueOriginal<POOrder.linesToCompleteCntr>(entity);
			return
				(!Equals(linesToCloseCntrOldValue, entity.LinesToCloseCntr)
				|| !Equals(linesToCompleteCntrOldValue, entity.LinesToCompleteCntr))
				&& (linesToCloseCntrOldValue == 0 || linesToCompleteCntrOldValue == 0
				|| entity.LinesToCloseCntr == 0 || entity.LinesToCompleteCntr == 0);
		}

		protected override void ProcessAffectedEntity(POOrderEntry primaryGraph, POOrder entity)
		{
			primaryGraph.UpdateDocumentState(entity);
		}
	}
}
