using PX.Data;
using PX.Objects.CS;
using PX.Objects.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PO.GraphExtensions.POOrderEntryExt
{
	using POOrderR = POOrderEntry.POOrderR;

	public class AffectedBlanketOrdersByOriginalOrders : ProcessAffectedEntitiesInPrimaryGraphBase<AffectedBlanketOrdersByOriginalOrders, POOrderEntry, POOrderR, POOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		private ILookup<POOrderR, POLine> _affectedBlanketOrders;

		protected override bool ClearAffectedCaches => false;
		protected override bool PersistInSameTransaction => true;
		
		protected override IEnumerable<POOrderR> GetLatelyAffectedEntities() => _affectedBlanketOrders.Select(x => x.Key).ToArray();

		[PXOverride]
		public override void Persist(Action basePersist)
		{
			_affectedBlanketOrders = Base.Transactions.Cache
				.Updated
				.RowCast<POLine>()
				.Where(x => LineIsAffected(x))
				.Select(x => new POLine
				{
					OrderType = x.POType,
					OrderNbr = x.PONbr,
					LineNbr = x.LineNbr,
					Closed = x.Closed
				})
				.ToLookup(x => new POOrderR { OrderType = x.OrderType, OrderNbr = x.OrderNbr }, Base.Caches<POOrderR>().GetComparer());

			base.Persist(basePersist);
		}

		protected override bool EntityIsAffected(POOrderR entity)
		{
			if (entity.OrderType != POOrderType.Blanket)
				return false;
			var cache = Base.Caches<POOrderR>();
			int? linesToCompleteCntrOldValue = (int?)cache.GetValueOriginal<POOrderR.linesToCompleteCntr>(entity);
			return
				!Equals(linesToCompleteCntrOldValue, entity.LinesToCompleteCntr)
				&& (linesToCompleteCntrOldValue == 0 || entity.LinesToCompleteCntr == 0);
		}

		protected virtual bool LineIsAffected(POLine line)
        {
			if (line.POType != POOrderType.Blanket)
				return false;
			if (line.Closed != (bool?)Base.Transactions.Cache.GetValueOriginal<POLine.closed>(line))
				return true;
			if (line.Closed == true)
            {
				var lineR = Base.poLiner.Cache.Updated
					.RowCast<POLineR>()
					.FirstOrDefault(x => x.OrderType == line.POType 
						&& x.OrderNbr == line.PONbr
						&& x.Completed != (bool?)Base.poLiner.Cache.GetValueOriginal<POLineR.completed>(x));
				return lineR != null;
            }
			return false;
		}

		protected override void ProcessAffectedEntity(POOrderEntry primaryGraph, POOrderR entity)
		{
			var order = POOrder.PK.Find(primaryGraph, entity.OrderType, entity.OrderNbr);
			var affectedBlanketLines = _affectedBlanketOrders[entity]
				.OrderBy(x => x.Closed)
				.ToArray();

			if (affectedBlanketLines.Any())
			{
				primaryGraph.Document.Search<POOrder.orderNbr>(order.OrderNbr, order.OrderType);

				foreach (var blanketLine in affectedBlanketLines)
					primaryGraph.UpdateClosedState(blanketLine);
			}

			primaryGraph.UpdateDocumentState(order);
		}
	}
}
