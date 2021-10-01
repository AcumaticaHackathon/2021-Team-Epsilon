using System;
using PX.Data;

namespace PX.Objects.AR.CCPaymentProcessing
{
    public class ExpirationDateAttribute : PXEventSubscriberAttribute, IPXFieldUpdatingSubscriber
    {
        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);

            sender.Graph.FieldSelecting.AddHandler(sender.GetItemType(),
                _FieldName, FieldSelectingHandler);
        }

        public void FieldSelectingHandler(PXCache sender, PXFieldSelectingEventArgs e)
        {
            DateTime? dt = e.ReturnValue as DateTime?;
            if (dt != null)
            {
                e.ReturnValue = dt.Value.AddMonths(-1);
            }
        }

		public void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
            DateTime? dt = e.NewValue as DateTime?;
            if (dt != null)
            {
                e.NewValue = dt.Value.AddMonths(1);
            }
        }
	}
}