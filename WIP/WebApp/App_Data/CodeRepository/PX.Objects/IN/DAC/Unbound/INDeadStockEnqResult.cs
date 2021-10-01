using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.DAC.Unbound
{
	[PXCacheName(Messages.INDeadStockEnqResult)]
	public class INDeadStockEnqResult : IBqlTable
	{
		#region SiteID
		[Site(DescriptionField = typeof(INSite.descr), DisplayName = "Warehouse", IsKey = true)]
		public virtual int? SiteID
		{
			get;
			set;
		}
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		#endregion

		#region InventoryID
		[AnyInventory(typeof(Search<InventoryItem.inventoryID,
			Where<InventoryItem.stkItem, NotEqual<boolFalse>,
				And<Where<Match<Current<AccessInfo.userName>>>>>>),
			typeof(InventoryItem.inventoryCD), typeof(InventoryItem.descr), IsKey = true)]
		public virtual int? InventoryID
		{
			get;
			set;
		}
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		#endregion

		#region SubItemID
		[SubItem(Visibility = PXUIVisibility.SelectorVisible, DisplayName = "Subitem", IsKey = true)]
		public virtual Int32? SubItemID
		{
			get;
			set;
		}
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		#endregion

		#region InStockQty
		[PXQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "In Stock Qty.")]
		public virtual decimal? InStockQty
		{
			get;
			set;
		}
		public abstract class inStockQty : PX.Data.BQL.BqlDecimal.Field<inStockQty> { }
		#endregion

		#region DeadStockQty
		[PXQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Dead Stock Qty.")]
		public virtual decimal? DeadStockQty
		{
			get;
			set;
		}
		public abstract class deadStockQty : PX.Data.BQL.BqlDecimal.Field<deadStockQty> { }
		#endregion

		#region InDeadStockDays
		[PXQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "In Dead Stock (days)")]
		public virtual decimal? InDeadStockDays
		{
			get;
			set;
		}
		public abstract class inDeadStockDays : PX.Data.BQL.BqlDecimal.Field<inDeadStockDays> { }
		#endregion

		#region LastSaleDate
		[PXDate]
		[PXUIField(DisplayName = "Last Sale Date")]
		public virtual DateTime? LastSaleDate
		{
			get;
			set;
		}
		public abstract class lastSaleDate : PX.Data.BQL.BqlDateTime.Field<lastSaleDate> { }
		#endregion

		#region LastCost
		[PXPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Last Cost")]
		public virtual decimal? LastCost
		{
			get;
			set;
		}
		public abstract class lastCost : PX.Data.BQL.BqlDecimal.Field<lastCost> { }
		#endregion

		#region TotalDeadStockCost
		[PXPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Total Dead Stock Cost")]
		public virtual decimal? TotalDeadStockCost
		{
			get;
			set;
		}
		public abstract class totalDeadStockCost : PX.Data.BQL.BqlDecimal.Field<totalDeadStockCost	> { }
		#endregion

		#region AverageItemCost
		[PXPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Dead Stock Item Average Cost")]
		public virtual decimal? AverageItemCost
		{
			get;
			set;
		}
		public abstract class averageItemCost : PX.Data.BQL.BqlDecimal.Field<averageItemCost> { }
		#endregion

		#region BaseCuryID
		public abstract class baseCuryID : PX.Data.BQL.BqlString.Field<baseCuryID> { }
		/// <summary>
		/// The base <see cref="Currency"/>.
		/// </summary>
		[PXString(5, IsUnicode = true)]
		[PXSelector(typeof(Search<CM.CurrencyList.curyID>))]
		[PXUIField(DisplayName = "Currency")]
		public virtual String BaseCuryID { get; set; }
		#endregion
	}
}
