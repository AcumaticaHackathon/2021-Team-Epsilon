using PX.Common;
using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.DAC.Unbound
{
	[PXCacheName(Messages.INDeadStockEnqFilter)]
	public class INDeadStockEnqFilter : IBqlTable
	{
		#region SiteID
		[Site(DescriptionField = typeof(INSite.descr), DisplayName = "Warehouse", Required = true)]
		public virtual int? SiteID
		{
			get;
			set;
		}
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		#endregion

		#region ItemClassID
		[PXInt]
		[PXUIField(DisplayName = "Item Class", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDimensionSelector(INItemClass.Dimension, typeof(Search<INItemClass.itemClassID>),
			typeof(INItemClass.itemClassCD), DescriptionField = typeof(INItemClass.descr), CacheGlobal = true)]
		public virtual int? ItemClassID
		{
			get;
			set;
		}
		public abstract class itemClassID : PX.Data.BQL.BqlInt.Field<itemClassID> { }
		#endregion

		#region InventoryID
		[AnyInventory(typeof(Search<InventoryItem.inventoryID,
			Where<InventoryItem.stkItem, NotEqual<boolFalse>,
				And<Where<Match<Current<AccessInfo.userName>>>>>>),
			typeof(InventoryItem.inventoryCD), typeof(InventoryItem.descr))]
		public virtual int? InventoryID
		{
			get;
			set;
		}
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		#endregion

		#region SelectBy
		[PXString]
		[selectBy.List]
		[PXDefault(selectBy.Days, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Select by", Required = true)]
		public virtual string SelectBy
		{
			get;
			set;
		}
		public abstract class selectBy : PX.Data.BQL.BqlString.Field<selectBy>
		{
			public const string Days = nameof(Days);
			public const string Date = nameof(Date);

			public class days : PX.Data.BQL.BqlString.Constant<days>
			{
				public days() : base(Days) { }
			}

			public class date : PX.Data.BQL.BqlString.Constant<days>
			{
				public date() : base(Date) { }
			}

			[PXLocalizable]
			public class Messages
			{
				public const string Days = "Days";
				public const string Date = "Date";
			}

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base(new string[] { Days, Date },
					new string[] { Messages.Days, Messages.Date })
				{
				}
			}
		}
		#endregion // SelectBy

		#region InStockDays
		[PXInt]
		[PXUIField(DisplayName = "In Stock For")]
		[PXDefault(30, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIVisible(typeof(Where<selectBy, Equal<selectBy.days>>))]
		public virtual int? InStockDays
		{
			get;
			set;
		}
		public abstract class inStockDays : PX.Data.BQL.BqlInt.Field<inStockDays> { }
		#endregion

		#region InStockSince
		[PXDate]
		[PXUIField(DisplayName = "In Stock Since")]
		[PXUIVisible(typeof(Where<selectBy, Equal<selectBy.date>>))]
		public virtual DateTime? InStockSince
		{
			get;
			set;
		}
		public abstract class inStockSince : PX.Data.BQL.BqlDateTime.Field<inStockSince> { }
		#endregion

		#region NoSalesDays
		[PXInt]
		[PXUIField(DisplayName = "No Sales For")]
		[PXUIVisible(typeof(Where<selectBy, Equal<selectBy.days>>))]
		public virtual int? NoSalesDays
		{
			get;
			set;
		}
		public abstract class noSalesDays : PX.Data.BQL.BqlInt.Field<noSalesDays> { }
		#endregion

		#region NoSalesSince
		[PXDate]
		[PXUIField(DisplayName = "No Sales Since")]
		[PXUIVisible(typeof(Where<selectBy, Equal<selectBy.date>>))]
		public virtual DateTime? NoSalesSince
		{
			get;
			set;
		}
		public abstract class noSalesSince : PX.Data.BQL.BqlDateTime.Field<noSalesSince> { }
		#endregion
	}
}
