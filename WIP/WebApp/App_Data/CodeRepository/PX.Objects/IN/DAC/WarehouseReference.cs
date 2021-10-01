using System;
using System.Web.UI.Design.WebControls;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.IN.DAC
{
	[Serializable]
	public class WarehouseReference : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<WarehouseReference>.By<siteID, portalSetupID>
		{
			public static WarehouseReference Find(PXGraph graph, int? siteID, string portalSetupID) => FindBy(graph, siteID, portalSetupID);
		}
		public static class FK
		{
			public class Site : INSite.PK.ForeignKeyOf<WarehouseReference>.By<siteID> { }
		}
		#endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }

		[PXDBInt(IsKey = true)]
		[PXUIField(Visibility = PXUIVisibility.SelectorVisible)]
		public virtual int? SiteID { get; set; }
		#endregion

		#region PortalSetupID
		public abstract class portalSetupID : PX.Data.BQL.BqlString.Field<portalSetupID> { }

		[PXDBString(256, IsKey = true)]
		[PXUIField(Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string PortalSetupID { get; set; }
		#endregion
	}
}
