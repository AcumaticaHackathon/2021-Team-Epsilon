using PX.Data;
using PX.Objects.CS;
using PX.Objects.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO.GraphExtensions.POReceiptEntryExt
{
	public class AffectedPOOrdersByPOReceipt : AffectedPOOrdersByPOLineUOpen<AffectedPOOrdersByPOReceipt, POReceiptEntry> 
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		protected override bool EntityIsAffected(POOrder entity)
		{
			if (entity.OrderType == POOrderType.RegularSubcontract)
				return false;
			return base.EntityIsAffected(entity);
		}
	}
}
