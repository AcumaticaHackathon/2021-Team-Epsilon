using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;

namespace PX.Objects.IN
{
	[PXProjection(typeof(Select4<INSiteStatus, Where<boolTrue, Equal<boolTrue>>, Aggregate<GroupBy<INSiteStatus.inventoryID, GroupBy<INSiteStatus.siteID, Sum<INSiteStatus.qtyOnHand, Sum<INSiteStatus.qtyAvail, Sum<INSiteStatus.qtyNotAvail>>>>>>>))]
	[Serializable]
	[PXCacheName(Messages.INSiteStatusSummary)]
	public partial class INSiteStatusSummary : INSiteStatus
	{
		#region Keys
		public new class PK : PrimaryKeyOf<INSiteStatusSummary>.By<inventoryID, siteID>
		{
			public static INSiteStatusSummary Find(PXGraph graph, int? inventoryID, int? siteID)
				=> FindBy(graph, inventoryID, siteID);
		}
		public new static class FK
		{
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INSiteStatusSummary>.By<inventoryID> { }
			public class Site : INSite.PK.ForeignKeyOf<INSiteStatusSummary>.By<siteID> { }
			public class ItemSite : INItemSite.PK.ForeignKeyOf<INSiteStatusSummary>.By<inventoryID, siteID> { }
		}
		#endregion
		#region InventoryID
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault]
		public override Int32? InventoryID
		{
			get
			{
				return this._InventoryID;
			}
			set
			{
				this._InventoryID = value;
			}
		}
		#endregion
		#region SubItemID
		public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		[SubItem] // not a part of the key anymore
		[PXDefault]
		public override Int32? SubItemID
		{
			get
			{
				return this._SubItemID;
			}
			set
			{
				this._SubItemID = value;
			}
		}
		#endregion
		#region SiteID
		public new abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		#endregion
		#region QtyOnHand
		public new abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
		#endregion
	}
}
