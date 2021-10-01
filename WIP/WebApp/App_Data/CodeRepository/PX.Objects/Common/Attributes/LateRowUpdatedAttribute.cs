using System;
using PX.Data;

namespace PX.Objects.Common
{
	/// <summary>
	/// Represents an event handler for the RowUpdated event that subscribes as late as possible.
	/// </summary>
	public abstract class LateRowUpdatedAttribute : PXEventSubscriberAttribute
	{
		public Type TargetTable { get; set; }

		public override void CacheAttached(PXCache cache)
		{
			base.CacheAttached(cache);

			cache.Graph.Initialized -= LateSubscription;
			cache.Graph.Initialized += LateSubscription;
		}

		private void LateSubscription(PXGraph graph)
		{
			graph.RowUpdated.RemoveHandler(TargetTable ?? BqlTable, LateRowUpdated);
			graph.RowUpdated.AddHandler(TargetTable ?? BqlTable, LateRowUpdated);
		}

		protected abstract void LateRowUpdated(PXCache cache, PXRowUpdatedEventArgs args);
	}
}