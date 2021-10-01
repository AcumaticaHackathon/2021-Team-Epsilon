using System;
using System.Collections.Generic;

using PX.Data;
using PX.BarcodeProcessing;

namespace PX.Objects.IN.WMS
{
	public static class AllWMSRedirects
	{
		public static IEnumerable<ScanRedirect<TScanExt>> CreateFor<TScanExt>()
			where TScanExt : PXGraphExtension, IBarcodeDrivenStateMachine
		{
			return new ScanRedirect<TScanExt>[]
			{
				new StoragePlaceLookup.RedirectFrom<TScanExt>(),
				new InventoryItemLookup.RedirectFrom<TScanExt>(),

				new INScanIssue.RedirectFrom<TScanExt>(),
				new INScanReceive.RedirectFrom<TScanExt>(),
				new INScanTransfer.RedirectFrom<TScanExt>(),
				new INScanCount.RedirectFrom<TScanExt>(),

				new PO.WMS.ReceivePutAway.ReceiveMode.RedirectFrom<TScanExt>(),
				new PO.WMS.ReceivePutAway.ReturnMode.RedirectFrom<TScanExt>(),
				new PO.WMS.ReceivePutAway.PutAwayMode.RedirectFrom<TScanExt>(),

				new SO.WMS.PickPackShip.PickMode.RedirectFrom<TScanExt>(),
				new SO.WMS.PickPackShip.PackMode.RedirectFrom<TScanExt>(),
				new SO.WMS.PickPackShip.ShipMode.RedirectFrom<TScanExt>(),
				new SO.WMS.PickPackShip.ReturnMode.RedirectFrom<TScanExt>(),
			};
		}
	}
}