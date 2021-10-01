using PX.Data;

namespace PX.Objects.FA
{
    public class FASyncStatusAttribute : PXEventSubscriberAttribute, IPXRowUpdatedSubscriber
    {
        public void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            if (!sender.ObjectsEqual<FixedAsset.status>(e.Row, e.OldRow))
            {
                FixedAsset row = (FixedAsset)e.Row;
                FADetails details = PXSelect<FADetails, Where<FADetails.assetID,
                    Equal<Required<FADetails.assetID>>>>.Select(sender.Graph, row?.AssetID);
                sender.Graph.Caches[typeof(FADetails)].SetValue<FADetails.status>(details, row.Status);
                sender.Graph.Caches[typeof(FADetails)].MarkUpdated(details);
            }
        }
    }
}