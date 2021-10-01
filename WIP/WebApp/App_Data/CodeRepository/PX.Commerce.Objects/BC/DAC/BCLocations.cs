using System;
using PX.Commerce.Core;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.IN;

namespace PX.Commerce.Objects
{
    [Serializable]
	[PXCacheName("Locations")]
	public class BCLocations : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<BCLocations>.By<BCLocations.bCLocationsID>
		{
			public static BCLocations Find(PXGraph graph, int? bCLocationsID) => FindBy(graph, bCLocationsID);
		}
		public static class FK
		{
			public class Binding : BCBinding.BindingIndex.ForeignKeyOf<BCLocations>.By<bindingID> { }
			public class Site : INSite.PK.ForeignKeyOf<BCLocations>.By<siteID> { }
			public class Location : INLocation.PK.ForeignKeyOf<BCLocations>.By<locationID> { }
		}
		#endregion

		#region BCLocationsID
		[PXDBIdentity(IsKey = true)]
        public int? BCLocationsID { get; set; }
        public abstract class bCLocationsID : PX.Data.BQL.BqlInt.Field<bCLocationsID> { }
        #endregion

        #region BindingID
        [PXDBInt()]
		[PXUIField(DisplayName = "Store")]
		[PXSelector(typeof(BCBinding.bindingID),
					typeof(BCBinding.bindingName),
					SubstituteKey = typeof(BCBinding.bindingName))]
		[PXParent(typeof(Select<BCBinding,
			Where<BCBinding.bindingID, Equal<Current<BCLocations.bindingID>>>>))]
		[PXDBDefault(typeof(BCBinding.bindingID), 
			PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? BindingID { get; set; }
		public abstract class bindingID : PX.Data.BQL.BqlInt.Field<bindingID> { }
		#endregion

		#region SiteID
		[PXDBInt()]
		[PXUIField(DisplayName = "Warehouse")]
		[PXSelector(typeof(INSite.siteID),
					SubstituteKey = typeof(INSite.siteCD),
					DescriptionField = typeof(INSite.descr))]
		[PXRestrictor(typeof(Where<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>>),
							 PX.Objects.IN.Messages.TransitSiteIsNotAvailable)]
		[PXDefault()]
		public virtual int? SiteID { get; set; }
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		#endregion

		#region LocationID
		[PXDBInt()]
        [PXUIField(DisplayName = "Location ID")]
		[PXSelector(typeof(Search<INLocation.locationID, 
			Where<INLocation.siteID, Equal<Current<BCLocations.siteID>>>>),
			SubstituteKey = typeof(INLocation.locationCD),
			DescriptionField = typeof(INLocation.descr)
			)]
		public virtual int? LocationID { get; set; }
        public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		#endregion

		#region ExternalLocationID
		[PXDBString(20, IsUnicode = true)]
		[PXUIField(DisplayName = "External Location ID")]
		public virtual string ExternalLocationID { get; set; }
		public abstract class externalLocationID : PX.Data.BQL.BqlString.Field<externalLocationID> { }
		#endregion

		#region MappingDirection
		[PXDBString(1)]
		[PXUIField(DisplayName = "Mapping Direction", Visible = false)]
		[BCMappingDirection]
		[PXDefault()]
		public virtual string MappingDirection { get; set; }
		public abstract class mappingDirection : PX.Data.BQL.BqlString.Field<mappingDirection> { }
		#endregion

		#region SiteCD
		public virtual string SiteCD { get; set; }
		#endregion

		#region LocationCD
		public virtual string LocationCD { get; set; }
		#endregion
	}

	[Serializable]
	[PXCacheName("ExportLocations")]
	public class ExportBCLocations : BCLocations
	{
		#region MappingDirection
		[PXDBString(1)]
		[BCMappingDirection]
		[PXDefault(BCMappingDirectionAttribute.Export, PersistingCheck = PXPersistingCheck.Nothing)]
		public override string MappingDirection { get; set; }
		public abstract class mappingDirection : PX.Data.BQL.BqlString.Field<mappingDirection> { }
		#endregion

		#region SiteID
		[PXDBInt()]
		[PXUIField(DisplayName = "Warehouse")]
		[PXSelector(typeof(INSite.siteID),
					SubstituteKey = typeof(INSite.siteCD),
					DescriptionField = typeof(INSite.descr))]
		[PXRestrictor(typeof(Where<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>, And<INSite.active, Equal<True>>>),
							 PX.Objects.IN.Messages.TransitSiteIsNotAvailable)]
		[PXDefault()]
		public override int? SiteID { get; set; }
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		#endregion

		#region LocationID
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt()]
		[PXUIField(DisplayName = "Location ID")]
		[PXSelector(typeof(Search<INLocation.locationID,
			Where<INLocation.siteID, Equal<Current<ExportBCLocations.siteID>>, And<INLocation.active, Equal<True>, And<INLocation.salesValid, Equal<True>>>>>),
			SubstituteKey = typeof(INLocation.locationCD),
			DescriptionField = typeof(INLocation.descr)
			)]
		public override int? LocationID { get; set; }
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		#endregion
	}

	[Serializable]
	[PXCacheName("ImportLocations")]
	public class ImportBCLocations : BCLocations
	{
		#region MappingDirection
		[PXDBString(1)]
		[BCMappingDirection]
		[PXDefault(BCMappingDirectionAttribute.Import, PersistingCheck = PXPersistingCheck.NullOrBlank)]
		public override string MappingDirection { get; set; }
		public abstract class mappingDirection : PX.Data.BQL.BqlString.Field<mappingDirection> { }
		#endregion

		#region SiteID
		[PXDBInt()]
		[PXUIField(DisplayName = "Warehouse")]
		[PXSelector(typeof(INSite.siteID),
					SubstituteKey = typeof(INSite.siteCD),
					DescriptionField = typeof(INSite.descr))]
		[PXRestrictor(typeof(Where<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>, And<INSite.active, Equal<True>>>),
							 PX.Objects.IN.Messages.TransitSiteIsNotAvailable)]
		[PXDefault()]
		public override int? SiteID { get; set; }
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		#endregion

		#region LocationID
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt()]
		[PXUIField(DisplayName = "Location ID")]
		[PXSelector(typeof(Search<INLocation.locationID,
			Where<INLocation.siteID, Equal<Current<ImportBCLocations.siteID>>, And<INLocation.active, Equal<True>, And<INLocation.salesValid, Equal<True>>>>>),
			SubstituteKey = typeof(INLocation.locationCD),
			DescriptionField = typeof(INLocation.descr)
			)]
		public override int? LocationID { get; set; }
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		#endregion
	}
}