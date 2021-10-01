using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.Extensions;
using PX.Objects.CS;

namespace PX.Objects.SO.GraphExtensions.ARPaymentEntryExt
{
	public class AffectedSOOrdersWithPrepaymentRequirementsExt : ProcessAffectedEntitiesInPrimaryGraphBase<AffectedSOOrdersWithPrepaymentRequirementsExt, ARPaymentEntry, SOOrder, SOOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		private PXCache<SOOrder> orders => Base.Caches<SOOrder>();
		protected override bool PersistInSameTransaction => false;

		private HashSet<SOOrder> ordersChangedDuringPersist;

		public override void Persist(Action basePersist)
		{
			ordersChangedDuringPersist = new HashSet<SOOrder>(Base.Caches<SOOrder>().GetComparer());
			base.Persist(basePersist);
		}

		protected virtual void _(Events.RowUpdated<SOOrder> args)
		{
			if (ordersChangedDuringPersist != null && !args.Cache.ObjectsEqual<SOOrder.curyPrepaymentReqAmt, SOOrder.curyPaymentOverall>(args.Row, args.OldRow))
				ordersChangedDuringPersist.Add(args.Row);
		}

		protected override bool EntityIsAffected(SOOrder order)
		{
			return
				!Equals(orders.GetValueOriginal<SOOrder.curyPrepaymentReqAmt>(order), order.CuryPrepaymentReqAmt) ||
				!Equals(orders.GetValueOriginal<SOOrder.curyPaymentOverall>(order), order.CuryPaymentOverall);
		}

		protected override void ProcessAffectedEntity(SOOrderEntry orderEntry, SOOrder order)
		{
			if (order.CuryPaymentOverall >= order.CuryPrepaymentReqAmt)
				order.SatisfyPrepaymentRequirements(orderEntry);
			else
				order.ViolatePrepaymentRequirements(orderEntry);
		}

		protected override IEnumerable<SOOrder> GetLatelyAffectedEntities() => ordersChangedDuringPersist;
		protected override void OnProcessed(SOOrderEntry foreignGraph) => ordersChangedDuringPersist = null;

		protected override SOOrder ActualizeEntity(SOOrderEntry orderEntry, SOOrder order) => SOOrder.PK.Find(orderEntry, order);
	}
}