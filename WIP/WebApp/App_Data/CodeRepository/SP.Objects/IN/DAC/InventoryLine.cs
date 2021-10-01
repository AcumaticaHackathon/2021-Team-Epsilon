using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.IN.DAC;
using PX.Objects.SO;
using PX.Objects.SP.DAC;
using PX.Objects.AR;
using PX.SM;
using SP.Objects.IN.Descriptor;

namespace SP.Objects.IN
{
    [Serializable]
    public class INSiteStatusExt : PXCacheExtension<INSiteStatus>
    {
        #region Selected

        public abstract class selected : IBqlField
        {
        }

        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Add")]
        public virtual bool? Selected { get; set; }

        #endregion

        #region Qty

        public abstract class qty : PX.Data.IBqlField
        {
        }

        protected Int32? _Qty;

        [PXInt()]
        [PXDefault(TypeCode.Int32, "0")]
        [PXUIField(DisplayName = "Qty")]
        public virtual Int32? Qty
        {
            get { return this._Qty; }
            set { this._Qty = value; }
        }

        #endregion

        #region TotalPrice
        public abstract class totalPrice : PX.Data.IBqlField
        {
        }

        protected Decimal? _TotalPrice;
        [PXDecimal()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Total", Enabled = false)]
        public virtual Decimal? TotalPrice
        {
            get { return Qty * CuryUnitPrice; }
        }

        #endregion

        #region UOM
        public abstract class uOM : PX.Data.IBqlField
        {
        }
        protected String _UOM;
        [Attributes.InUnitPortalAttribute(typeof(INSiteStatus.inventoryID), DisplayName = "Unit")]
        [PXDefault(typeof(Search<InventoryItem.salesUnit, Where<InventoryItem.inventoryID, Equal<Current<INSiteStatus.inventoryID>>>>))]
        public virtual String UOM
        {
            get
            {
                return this._UOM;
            }
            set
            {
                this._UOM = value;
            }
        }
        #endregion

        #region CuryUnitPrice
        public abstract class curyUnitPrice : PX.Data.IBqlField
        {
        }
        protected Decimal? _CuryUnitPrice;
        [PXCurrency(typeof(Search<CommonSetup.decPlPrcCst>), typeof(CurrencyInfo.curyInfoID), typeof(unitPrice), MinValue = 0)]
        [PXUIField(DisplayName = "Price", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual Decimal? CuryUnitPrice
        {
            get
            {
                return this._CuryUnitPrice;
            }
            set
            {
                this._CuryUnitPrice = value;
            }
        }
        #endregion
        
        #region UnitPrice
        public abstract class unitPrice : PX.Data.IBqlField
        {
        }
        protected Decimal? _UnitPrice;
        [PXPriceCost()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Price", Enabled = false)]
        public virtual Decimal? UnitPrice
        {
            get
            {
                return this._UnitPrice;
            }
            set
            {
                this._UnitPrice = value;
            }
        }
        #endregion

        #region TotalWarehouse
        public abstract class totalWarehouse : PX.Data.IBqlField
        {
        }
        protected Decimal? _TotalWarehouse;
        [PXQuantity()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "All Warehouses", Enabled = false)]
        public virtual Decimal? TotalWarehouse
        {
            get
            {
                return this._TotalWarehouse;
            }
            set
            {
                this._TotalWarehouse = value;
            }
        }
        #endregion

        #region Order

        public abstract class order : PX.Data.IBqlField
        {
        }

        protected Int32? _Order;

        [PXInt()]
        [PXUIField(DisplayName = "Qty")]
        public virtual Int32? Order
        {
            get { return this._Order; }
            set { this._Order = value; }
        }

        #endregion
    }

    [Serializable]
    public class InventoryLineFilter : IBqlTable
    {
        #region Find Item
        public abstract class findItem : PX.Data.IBqlField
        {
        }
        protected String _FindItem;
        [PXString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Find Item")]
        public virtual String FindItem
        {
            get
            {
                return this._FindItem;
            }
            set
            {
                this._FindItem = value;
            }
        }
        #endregion
        
        #region SiteID

        public abstract class siteID : PX.Data.IBqlField
        {
        }

        protected Int32? _SiteID;

        [Site()]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Int32? SiteID
        {
            get { return this._SiteID; }
            set { this._SiteID = value; }
        }

        #endregion
        
        #region CategoryID

        public abstract class categoryID : PX.Data.IBqlField
        {
        }

        protected int? _CategoryID;
        [PXInt()]
        [PXSelector(typeof (INCategory.categoryID), DescriptionField = typeof (INCategory.description))]
        [PXUIField(DisplayName = "Category")]
        public virtual int? CategoryID
        {
            get { return this._CategoryID; }
            set { this._CategoryID = value; }
        }
        #endregion

        #region SelectionTotal
        public abstract class selectionTotal : PX.Data.IBqlField
        {
        }
        protected Decimal? _SelectionTotal;
        [PXBaseCury()]
        [PXUIField(DisplayName = "Selection Total", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual Decimal? SelectionTotal
        {
            get
            {
                return this._SelectionTotal;
            }
            set
            {
                this._SelectionTotal = value;
            }
        }
        #endregion

        #region CurrencyStatus
        public abstract class currencyStatus : PX.Data.IBqlField
        {
        }
        protected String _CurrencyStatus;
        [PXString(IsUnicode = true)]
        [PXUIField(DisplayName = "Prices are shown in")]
        [PXDefault("USD", PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual String CurrencyStatus
        {
            get
            {
                return this._CurrencyStatus;
            }
            set
            {
                this._CurrencyStatus = value;
            }
        }
        #endregion

        #region Available Item

        public abstract class isShowAvailableItem : IBqlField
        {
        }
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Show Available Items Only")]
        public virtual bool? IsShowAvailableItem { get; set; }
        #endregion
    }

    [Serializable]
    public class InventoryLineInfo : IBqlTable
    {
        #region Info
        public abstract class info : PX.Data.IBqlField
        {
        }
        protected String _Info;
        [PXString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Info", Enabled = false)]
        public virtual String Info
        {
            get
            {
                return this._Info;
            }
            set
            {
                this._Info = value;
            }
        }
        #endregion
    }

    [PXProjection(typeof(
        Select2<InventoryItem,
			InnerJoin<PortalSetup, 
				On<PortalSetup.IsCurrentPortal>,
			LeftJoin<INUnit, 
				On<INUnit.inventoryID, Equal<InventoryItem.inventoryID>,
				And<INUnit.fromUnit, Equal<InventoryItem.salesUnit>,
				And<INUnit.toUnit, Equal<InventoryItem.baseUnit>>>>,
			LeftJoin<InventoryAvailability,
				On<InventoryItem.inventoryID, Equal<InventoryAvailability.inventoryID>>>>>,
		Where<InventoryItem.itemStatus, In3<InventoryItemStatus.active, InventoryItemStatus.noPurchases, InventoryItemStatus.noRequest>,
			And2<Exists<Select<INItemCategory, Where<INItemCategory.inventoryID, Equal<InventoryItem.inventoryID>>>>,
			And<Where<CurrentMatch<InventoryItem, AccessInfo.userName>>>>>
			>), Persistent = false)]
    [Serializable]
    public class InventoryLines : IBqlTable
    {
        #region InventoryID
        public abstract class inventoryID : BqlInt.Field<inventoryID>
        {
        }

        protected Int32? _InventoryID;
        [PXDBInt(IsKey = true, BqlField = typeof(InventoryItem.inventoryID))]
        [PXDefault()]
        [PXUIField(Enabled = false)]
        public virtual Int32? InventoryID
        {
            get { return this._InventoryID; }
            set { this._InventoryID = value; }
        }
        #endregion

        #region NoteID
        public abstract class noteID : PX.Data.IBqlField
        {
        }
        protected Guid? _NoteID;
        [PXNote(BqlField = typeof(InventoryItem.noteID))]
        public virtual Guid? NoteID
        {
            get
            {
                return this._NoteID;
            }
            set
            {
                this._NoteID = value;
            }
        }
        #endregion

        #region InventoryCD
        public abstract class inventoryCD : PX.Data.IBqlField
        {
        }
        protected String _InventoryCD;
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBString(BqlField = typeof(InventoryItem.inventoryCD))]
		[PXUIField(DisplayName = "Inventory ID")]
		[PXDimension(InventoryRawAttribute.DimensionName)]
		[PXSelector(typeof(Search<InventoryLines.inventoryCD, Where2<Match<Current<AccessInfo.userName>>, And<InventoryLines.itemStatus, NotEqual<InventoryItemStatus.unknown>>>>))]
		public virtual String InventoryCD
        {
            get
            {
                return this._InventoryCD;
            }
            set
            {
                this._InventoryCD = value;
            }
        }
        #endregion

		#region ItemStatus
		public abstract class itemStatus : PX.Data.BQL.BqlString.Field<itemStatus> { }
		protected String _ItemStatus;

		/// <summary>
		/// The status of the Inventory Item.
		/// </summary>
		/// <value>
		/// Possible values are:
		/// <c>"AC"</c> - Active (can be used in inventory operations, such as issues and receipts),
		/// <c>"NS"</c> - No Sales (cannot be sold),
		/// <c>"NP"</c> - No Purchases (cannot be purchased),
		/// <c>"NR"</c> - No Request (cannot be used on requisition requests),
		/// <c>"IN"</c> - Inactive,
		/// <c>"DE"</c> - Marked for Deletion.
		/// Defaults to Active (<c>"AC"</c>).
		/// </value>
		[PXDBString(2, IsFixed = true, BqlField = typeof(InventoryItem.itemStatus))]
		[PXDefault("AC")]
		[InventoryItemStatus.List]
		public virtual String ItemStatus
		{
			get
			{
				return this._ItemStatus;
			}
			set
			{
				this._ItemStatus = value;
			}
		}
		#endregion

        #region SiteID
        public abstract class siteID : PX.Data.IBqlField
        {
        }
        protected Int32? _SiteID;
        [Attributes.PortalSiteAvailAttribute(typeof(inventoryID))]
        [PXDBDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Int32? SiteID
        {
            get
            {
                return this._SiteID;
            }
            set
            {
                this._SiteID = value;
            }
        }
        #endregion
        #region QtyOnHand
        public abstract class qtyOnHand : PX.Data.IBqlField
        {
        }
        protected Decimal? _QtyOnHand;
        [PXDBQuantity(BqlField = typeof(InventoryAvailability.qtyOnHand))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Qty. On Hand", Visibility = PXUIVisibility.Visible)]
        public virtual Decimal? QtyOnHand
        {
            get
            {
                return this._QtyOnHand;
            }
            set
            {
                this._QtyOnHand = value;
            }
        }
		#endregion

		#region QtyAvail
		public abstract class qtyAvail : PX.Data.IBqlField
		{
		}
		[PXDBQuantity(BqlField = typeof(InventoryAvailability.qtyAvail))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Available", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? QtyAvail
		{
			get;
			set;
		}
		#endregion

		#region QtyHardAvail
		public abstract class qtyHardAvail : PX.Data.IBqlField
		{
		}
		[PXDBQuantity(BqlField = typeof(InventoryAvailability.qtyHardAvail))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. Hard Available", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? QtyHardAvail
		{
			get;
			set;
		}
		#endregion


		#region QtyDflt
		public abstract class qtyDflt : PX.Data.IBqlField
        {
        }
        protected Decimal? _QtyDflt;
        [PXDefault(typeof(Search<INSiteStatus.qtyOnHand, Where<INSiteStatus.inventoryID, Equal<Current<inventoryID>>,
                                                               And<INSiteStatus.siteID, Equal<Current<siteID>>>>>))]
        public virtual Decimal? QtyDflt
        {
            get
            {
                return this._QtyDflt;
            }
            set
            {
                this._QtyDflt = value;
            }
        }
		#endregion

		#region QtyAvailDflt
		public abstract class qtyAvailDflt : PX.Data.IBqlField
		{
		}
		[PXDefault(typeof(Search<INSiteStatus.qtyAvail, Where<INSiteStatus.inventoryID, Equal<Current<inventoryID>>,
															   And<INSiteStatus.siteID, Equal<Current<siteID>>>>>))]
		public virtual Decimal? QtyAvailDflt
		{
			get;
			set;
		}
		#endregion

		#region QtyHardAvailDflt
		public abstract class qtyHardAvailDflt : PX.Data.IBqlField
		{
		}
		[PXDefault(typeof(Search<INSiteStatus.qtyHardAvail, Where<INSiteStatus.inventoryID, Equal<Current<inventoryID>>,
															   And<INSiteStatus.siteID, Equal<Current<siteID>>>>>))]
		public virtual Decimal? QtyHardAvailDflt
		{
			get;
			set;
		}
		#endregion



        #region Descr
        public abstract class descr : PX.Data.IBqlField
        {
        }
        [PXDBLocalizableString(255, IsUnicode = true, BqlField = typeof(InventoryItem.descr), IsProjection = true)]
        [PXUIField(DisplayName = "Description", Enabled = false)]
        public virtual String Descr { get; set; }
        #endregion

        #region StkItem
        public abstract class stkItem : PX.Data.IBqlField
        {
        }
        protected Boolean? _StkItem;
        [PXDBBool(BqlField = typeof(InventoryItem.stkItem))]
        [PXDefault(true)]
        [PXUIField(DisplayName = "Stock Item")]
        public virtual Boolean? StkItem
        {
            get
            {
                return this._StkItem;
            }
            set
            {
                this._StkItem = value;
            }
        }
        #endregion


        // virtual fields
        #region SiteIDDescription
        public abstract class siteIDDescription : PX.Data.IBqlField
        {
        }

        protected String _SiteIDDescription;
        [PXString]
        [PXUIField(DisplayName = "Warehouse", Enabled = false)]
        public virtual String SiteIDDescription
        {
            get { return this._SiteIDDescription; }
            set { this._SiteIDDescription = value; }
        }
        #endregion

        #region CurrentWarehouse
        public abstract class currentWarehouse : PX.Data.IBqlField
        {
        }
        protected Decimal? _CurrentWarehouse;
   
        [PXUIField(DisplayName = "This Warehouse", Enabled = false)]
        [PXQuantity]
		[PXFormula(typeof(Switch<Case<Where<InventoryLines.stkItem, Equal<True>>, Div<InventoryLines.qtyAvailDflt, InventoryLines.unitRate>>, Null>))]
		public virtual Decimal? CurrentWarehouse
        {
            get
            {
                return this._CurrentWarehouse;
            }
            set
            {
                this._CurrentWarehouse = value;
            }
        }
        #endregion

        #region TotalWarehouse
        public abstract class totalWarehouse : PX.Data.IBqlField
        {
        }
        protected Decimal? _TotalWarehouse;
        [PXQuantity]
        [PXUIField(DisplayName = "All Warehouses", Enabled = false)]
        [PXFormula(typeof(Switch<
			Case<Where<InventoryLines.qtyAvail, IsNull>, Zero,
			Case<Where<InventoryLines.stkItem, Equal<True>>, Div<IsNull<InventoryLines.qtyAvail, Zero>, InventoryLines.unitRate>>>,
			Null>))]
		public virtual Decimal? TotalWarehouse
        {
            get
            {
                return this._TotalWarehouse;
            }
            set
            {
                this._TotalWarehouse = value;
            }
        }
        #endregion

        #region Qty
        public abstract class qty : PX.Data.IBqlField
        {
        }
        protected Decimal? _Qty;
        [PXQuantity(MinValue = 0, MaxValue = 9999999)]
        [PXDefault(TypeCode.Decimal, "0.00")]
        [PXUIField(DisplayName = "Qty")]
        public virtual Decimal? Qty
        {
            get
            {
                return this._Qty;
            }
            set
            {
                this._Qty = value;
            }
        }
        #endregion

        #region Virtual fields for discount engine
        //This fields are needed for correct calculation of customer-specific and customer price class-specific prices inside the discount engine. 
        //See GetUnitPrice(), AmountLineFields.GetMapFor() and LineEntitiesFields.GetMapFor() methods (PX.Objects.Common.Discount.DiscountEngine).
        #region CustomerID
        public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
        [PXInt()]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Int32? CustomerID
        {
            get;
            set;
        }
        #endregion

        #region OrderQty
        public abstract class orderQty : PX.Data.BQL.BqlDecimal.Field<orderQty> { }
        [PXDecimal()]
        public virtual Decimal? OrderQty
        {
            get { return _Qty; }
        }
        #endregion

        #region BaseOrderQty
        public abstract class baseOrderQty : PX.Data.BQL.BqlDecimal.Field<baseOrderQty> { }
        [PXDecimal()]
        public virtual Decimal? BaseOrderQty
        {
            get { return _Qty; }
        }
        #endregion
        #endregion

        #region UOM
        public abstract class uOM : PX.Data.IBqlField
        {
        }
        protected String _UOM;
        [INUnit(typeof(InventoryLines.inventoryID), DisplayName = "Unit")]
        [PXDefault(typeof(Search<InventoryItem.salesUnit, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>))]
        public virtual String UOM
        {
            get
            {
                return this._UOM;
            }
            set
            {
                this._UOM = value;
            }
        }
        #endregion

        #region CuryUnitPrice
        public abstract class curyUnitPrice : PX.Data.IBqlField
        {
        }
        protected Decimal? _CuryUnitPrice;
		[PXCurrency(typeof(Search<CommonSetup.decPlPrcCst>), typeof(InventoryLines.curyInfoID), typeof(unitPrice), MinValue = 0)]
        [PXUIField(DisplayName = "Price", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual Decimal? CuryUnitPrice
        {
            get
            {
                return this._CuryUnitPrice;
            }
            set
            {
                this._CuryUnitPrice = value;
            }
        }
        #endregion

        #region UnitPrice
        public abstract class unitPrice : PX.Data.IBqlField
        {
        }
        protected Decimal? _UnitPrice;
        [PXPriceCost()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Price", Enabled = false)]
        public virtual Decimal? UnitPrice
        {
            get
            {
                return this._UnitPrice;
            }
            set
            {
                this._UnitPrice = value;
            }
        }
        #endregion

        #region BaseDiscountAmt
        public abstract class baseDiscountAmt : IBqlField { }
        protected Decimal? _BaseDiscountAmt;
        [PXDecimal(6)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Discount Amt", Enabled = false)]
        public decimal? BaseDiscountAmt
        {
            get 
            {
                return _BaseDiscountAmt ?? 0;
            }
            set
            {
                _BaseDiscountAmt = value;
            } 
        }
        #endregion

        #region BaseDiscountPct
        public abstract class baseDiscountPct : IBqlField { }
        [PXDecimal(6)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Discount Pct,%", Enabled = false)]
        public decimal? BaseDiscountPct
        {
            get;
            set;
        }
        #endregion

        #region BaseDiscountID
        public abstract class baseDiscountID : PX.Data.IBqlField
        {
        }
        [PXString(10, IsUnicode = true)]
        public virtual String BaseDiscountID
        {
            get;
            set;
        }
        #endregion

        #region BaseDiscountSeq
        public abstract class baseDiscountSeq : PX.Data.IBqlField
        {
        }
        [PXString(10, IsUnicode = true)]
        public virtual String BaseDiscountSeq
        {
            get;
            set;
        }
        #endregion

        #region CuryInfoID
        public abstract class curyInfoID : PX.Data.IBqlField
        {
        }
        [PXLong()]
        public virtual Int64? CuryInfoID
        {
            get;
            set;
        }
        #endregion

        #region Selected
        public abstract class selected : IBqlField
        {
        }
        protected Boolean? _Selected;
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Add To Cart")]
        public virtual bool? Selected
        {
            get
            {
                return this._Selected;
            }
            set
            {
                this._Selected = value;
            }
        }
        #endregion

        #region TotalPrice
        public abstract class totalPrice : PX.Data.IBqlField
        {
        }

        protected Decimal? _TotalPrice;
        [PXBaseCury()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Total", Enabled = false)]
        public virtual Decimal? TotalPrice
        {
            get
            {
                return Qty * CuryUnitPrice - BaseDiscountAmt;
            }
        }

        #endregion

        #region DfltSiteID
        public abstract class dfltSiteID : PX.Data.IBqlField
        {
        }
        protected Int32? _DfltSiteID;
        [PXDBInt(BqlField = typeof(InventoryItem.dfltSiteID))]
        public virtual Int32? DfltSiteID
        {
            get
            {
                return this._DfltSiteID;
            }
            set
            {
                this._DfltSiteID = value;
            }
        }
        #endregion

        #region PriceTimestamp
        public abstract class priceTimestamp : PX.Data.IBqlField
        {
        }
        protected String _PriceTimestamp;
        [PXString(30, IsUnicode = true)]
        public virtual String PriceTimestamp
        {
            get
            {
                return this._PriceTimestamp;
            }
            set
            {
                this._PriceTimestamp = value;
            }
        }
        #endregion

        #region UnitRate
        public abstract class unitRate : PX.Data.IBqlField
        {
        }
        protected Decimal? _UnitRate;
        [PXDBQuantity(BqlField = typeof(INUnit.unitRate))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        //[PXUIField(DisplayName = "Qty. On Hand")]
        public virtual Decimal? UnitRate
        {
            get
            {
                return this._UnitRate;
            }
            set
            {
                this._UnitRate = value;
            }
        }
        #endregion
    }

	[PXProjection(typeof(
	   Select2<InventoryItem,
	   InnerJoin<InventoryItemsWithCategories, On<InventoryItemsWithCategories.inventoryID,
		   Equal<InventoryItem.inventoryID>>,
	   InnerJoin<PortalSetup, On<PortalSetup.IsCurrentPortal>,
	   LeftJoin<INSiteStatus, On<INSiteStatus.inventoryID, Equal<InventoryItem.inventoryID>,
								 And<INSiteStatus.subItemID, Equal<PortalSetup.defaultSubItemID>>>,
	   LeftJoin<WarehouseReference, On<WarehouseReference.siteID, Equal<INSiteStatus.siteID>,
									 And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSetupID>>>,
	   LeftJoin<INUnit, On<INUnit.inventoryID, Equal<InventoryItem.inventoryID>,
				And<INUnit.fromUnit, Equal<InventoryItem.salesUnit>,
				And<INUnit.toUnit, Equal<InventoryItem.baseUnit>>>>>>>>>,
	   Where2<
		   Where<InventoryItem.itemStatus, Equal<InventoryItemStatus.active>,
		   Or<InventoryItem.itemStatus, Equal<InventoryItemStatus.noPurchases>,
		   Or<InventoryItem.itemStatus, Equal<InventoryItemStatus.noRequest>>>>,
		   And<Where<CurrentMatch<InventoryItem, AccessInfo.userName>>>>>), Persistent = false)]
	[Serializable]
	public class InventoryLinesWithoutGroup : IBqlTable
	{
		#region InventoryID
		public abstract class inventoryID : PX.Data.IBqlField
		{
		}

		protected Int32? _InventoryID;
		[PXDBInt(IsKey = true, BqlField = typeof(InventoryItem.inventoryID))]
		[PXDefault()]
		[PXUIField(Enabled = false)]
		public virtual Int32? InventoryID
		{
			get { return this._InventoryID; }
			set { this._InventoryID = value; }
		}
		#endregion
		
		#region NoteID
		public abstract class noteID : PX.Data.IBqlField
		{
		}
		protected Guid? _NoteID;
		[PXNote(BqlField = typeof(InventoryItem.noteID))]
		public virtual Guid? NoteID
		{
			get
			{
				return this._NoteID;
			}
			set
			{
				this._NoteID = value;
			}
		}
		#endregion

		#region InventoryCD
		public abstract class inventoryCD : PX.Data.IBqlField
		{
		}
		protected String _InventoryCD;
		[PXDBString(BqlField = typeof(InventoryItem.inventoryCD))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDimension(InventoryRawAttribute.DimensionName)]
		//Type SearchType = typeof(Search<InventoryItem.inventoryCD, Where2<Match<Current<AccessInfo.userName>>, And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.unknown>>>>);
		//[PXSelector(typeof(Search<InventoryItem.inventoryCD, Where2<Match<Current<AccessInfo.userName>>, And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.unknown>>>>))]
		public virtual String InventoryCD
		{
			get
			{
				return this._InventoryCD;
			}
			set
			{
				this._InventoryCD = value;
			}
		}
		#endregion

		#region ItemStatus
		public abstract class itemStatus : PX.Data.BQL.BqlString.Field<itemStatus> { }
		protected String _ItemStatus;

		/// <summary>
		/// The status of the Inventory Item.
		/// </summary>
		/// <value>
		/// Possible values are:
		/// <c>"AC"</c> - Active (can be used in inventory operations, such as issues and receipts),
		/// <c>"NS"</c> - No Sales (cannot be sold),
		/// <c>"NP"</c> - No Purchases (cannot be purchased),
		/// <c>"NR"</c> - No Request (cannot be used on requisition requests),
		/// <c>"IN"</c> - Inactive,
		/// <c>"DE"</c> - Marked for Deletion.
		/// Defaults to Active (<c>"AC"</c>).
		/// </value>
		[PXDBString(2, IsFixed = true, BqlField = typeof(InventoryItem.itemStatus))]
		[PXDefault("AC")]
		[PXUIField(DisplayName = "Item Status", Visibility = PXUIVisibility.SelectorVisible)]
		[InventoryItemStatus.List]
		public virtual String ItemStatus
		{
			get
			{
				return this._ItemStatus;
			}
			set
			{
				this._ItemStatus = value;
			}
		}
		#endregion

		#region SiteID
		public abstract class siteID : PX.Data.IBqlField
		{
		}
		protected Int32? _SiteID;
		[Attributes.PortalSiteAvailAttribute(typeof(inventoryID))]
		[PXDBDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? SiteID
		{
			get
			{
				return this._SiteID;
			}
			set
			{
				this._SiteID = value;
			}
		}
		#endregion

		#region QtyOnHandExt
		public abstract class qtyOnHandExt : PX.Data.IBqlField
		{
		}
		protected Decimal? _QtyOnHandExt;
		[PXDBCalced(typeof(Switch<Case<Where<WarehouseReference.portalSetupID, IsNotNull>, INSiteStatus.qtyOnHand>, Zero>), typeof(decimal))]
		public virtual Decimal? QtyOnHandExt
		{
			get
			{
				return this._QtyOnHandExt;
			}
			set
			{
				this._QtyOnHandExt = value;
			}
		}
		#endregion

		#region QtyAvailExt
		public abstract class qtyAvailExt : PX.Data.IBqlField
		{
		}
		[PXDBCalced(typeof(Switch<Case<Where<WarehouseReference.portalSetupID, IsNotNull>, INSiteStatus.qtyAvail>, Zero>), typeof(decimal))]
		public virtual Decimal? QtyAvailExt
		{
			get;
			set;
		}
		#endregion

		#region QtyHardAvailExt
		public abstract class qtyHardAvailExt : PX.Data.IBqlField
		{
		}
		[PXDBCalced(typeof(Switch<Case<Where<WarehouseReference.portalSetupID, IsNotNull>, INSiteStatus.qtyHardAvail>, Zero>), typeof(decimal))]
		public virtual Decimal? QtyHardAvailExt
		{
			get;
			set;
		}
		#endregion


		#region QtyOnHand
		public abstract class qtyOnHand : PX.Data.IBqlField
		{
		}
		protected Decimal? _QtyOnHand;
		[PXDBQuantity(BqlField = typeof(INSiteStatus.qtyOnHand))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. On Hand", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? QtyOnHand
		{
			get
			{
				return this._QtyOnHand;
			}
			set
			{
				this._QtyOnHand = value;
			}
		}
		#endregion

		#region Descr
		public abstract class descr : PX.Data.IBqlField
		{
		}
		[PXDBLocalizableString(255, IsUnicode = true, BqlField = typeof(InventoryItem.descr), IsProjection = true)]
		[PXUIField(DisplayName = "Description", Enabled = false)]
		public virtual String Descr { get; set; }
		#endregion

		#region StkItem
		public abstract class stkItem : PX.Data.IBqlField
		{
		}
		protected Boolean? _StkItem;
		[PXDBBool(BqlField = typeof(InventoryItem.stkItem))]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Stock Item")]
		public virtual Boolean? StkItem
		{
			get
			{
				return this._StkItem;
			}
			set
			{
				this._StkItem = value;
			}
		}
		#endregion

		#region PortalSetupID
		public abstract class portalSetupID : PX.Data.IBqlField
		{
		}
		protected String _PortalSetupID;
		[PXDBString(BqlField = typeof(WarehouseReference.portalSetupID))]
		public virtual String PortalSetupID
		{
			get
			{
				return this._PortalSetupID;
			}
			set
			{
				this._PortalSetupID = value;
			}
		}
		#endregion

		// virtual fields
		#region CuryUnitPrice
		public abstract class curyUnitPrice : PX.Data.IBqlField
		{
		}
		protected Decimal? _CuryUnitPrice;
		[PXCurrency(typeof(Search<CommonSetup.decPlPrcCst>), typeof(InventoryLinesWithoutGroup.curyInfoID), typeof(unitPrice), MinValue = 0)]
		[PXUIField(DisplayName = "Price", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryUnitPrice
		{
			get
			{
				return this._CuryUnitPrice;
			}
			set
			{
				this._CuryUnitPrice = value;
			}
		}
		#endregion

		#region UnitPrice
		public abstract class unitPrice : PX.Data.IBqlField
		{
		}
		protected Decimal? _UnitPrice;
		[PXPriceCost()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Price", Enabled = false)]
		public virtual Decimal? UnitPrice
		{
			get
			{
				return this._UnitPrice;
			}
			set
			{
				this._UnitPrice = value;
			}
		}
		#endregion

		#region BaseDiscountAmt
		public abstract class baseDiscountAmt : IBqlField { }
		protected Decimal? _BaseDiscountAmt;
		[PXDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Discount Amt", Enabled = false)]
		public decimal? BaseDiscountAmt
		{
			get
			{
				return _BaseDiscountAmt ?? 0;
			}
			set
			{
				_BaseDiscountAmt = value;
			}
		}
		#endregion

		#region BaseDiscountPct
		public abstract class baseDiscountPct : IBqlField { }
		[PXDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Discount Pct,%", Enabled = false)]
		public decimal? BaseDiscountPct
		{
			get;
			set;
		}
		#endregion

		#region BaseDiscountID
		public abstract class baseDiscountID : PX.Data.IBqlField
		{
		}
		[PXString(10, IsUnicode = true)]
		public virtual String BaseDiscountID
		{
			get;
			set;
		}
		#endregion

		#region BaseDiscountSeq
		public abstract class baseDiscountSeq : PX.Data.IBqlField
		{
		}
		[PXString(10, IsUnicode = true)]
		public virtual String BaseDiscountSeq
		{
			get;
			set;
		}
		#endregion

		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.IBqlField
		{
		}
		[PXLong()]
		public virtual Int64? CuryInfoID
		{
			get;
			set;
		}
		#endregion

		#region DfltSiteID
		public abstract class dfltSiteID : PX.Data.IBqlField
		{
		}
		protected Int32? _DfltSiteID;
		[PXDBInt(BqlField = typeof(InventoryItem.dfltSiteID))]
		public virtual Int32? DfltSiteID
		{
			get
			{
				return this._DfltSiteID;
			}
			set
			{
				this._DfltSiteID = value;
			}
		}
		#endregion

		#region UnitRate
		public abstract class unitRate : PX.Data.IBqlField
		{
		}
		protected Decimal? _UnitRate;
		[PXDBQuantity(BqlField = typeof(INUnit.unitRate))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Qty. On Hand")]
		public virtual Decimal? UnitRate
		{
			get
			{
				return this._UnitRate;
			}
			set
			{
				this._UnitRate = value;
			}
		}
		#endregion
	}

	[PXProjection(typeof(
		Select5<INSiteStatus, 
			InnerJoin<PortalSetup,
				On<PortalSetup.defaultSubItemID, Equal<INSiteStatus.subItemID>, 
				And<PortalSetup.IsCurrentPortal>>,
			InnerJoin<WarehouseReference, 
				On<WarehouseReference.siteID, Equal<INSiteStatus.siteID>,
				And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSetupID>>>>>,
			Aggregate<
				GroupBy<INSiteStatus.inventoryID,
				Sum<INSiteStatus.qtyOnHand,
				Sum<INSiteStatus.qtyAvail,
				Sum<INSiteStatus.qtyHardAvail>>>>>>), Persistent = false)]
	public class InventoryAvailability: IBqlTable
	{
		#region InventoryID
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		[PXDBInt(IsKey = true, BqlField = typeof(INSiteStatus.inventoryID))]
		[PXDefault]
		public virtual int? InventoryID
		{
			get;
			set;
		}
		#endregion

		#region QtyOnHand
		public abstract class qtyOnHand : BqlDecimal.Field<qtyOnHand> { }
		[PXDBQuantity(BqlField = typeof(INSiteStatus.qtyOnHand))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyOnHand
		{
			get;
			set;
		}
		#endregion
		#region QtyAvail
		public abstract class qtyAvail : BqlDecimal.Field<qtyAvail> { }
		[PXDBQuantity(BqlField = typeof(INSiteStatus.qtyAvail))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyAvail
		{
			get;
			set;
		}
		#endregion
		#region QtyHardAvail
		public abstract class qtyHardAvail : BqlDecimal.Field<qtyHardAvail> { }
		[PXDBQuantity(BqlField = typeof(INSiteStatus.qtyHardAvail))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? QtyHardAvail
		{
			get;
			set;
		}
		#endregion
	}

	[PXProjection(typeof(
        Select5<InventoryItem,
        InnerJoin<INItemCategory, On<INItemCategory.inventoryID, Equal<InventoryItem.inventoryID>>>,
          Aggregate<GroupBy<InventoryItem.inventoryID>>>), Persistent = false)]
    [Serializable]
    public class InventoryItemsWithCategories : IBqlTable
    {
        #region InventoryID
        public abstract class inventoryID : PX.Data.IBqlField
        {
        }

        protected Int32? _InventoryID;
        [PXDBInt(IsKey = true, BqlField = typeof(InventoryItem.inventoryID))]
        [PXDefault()]
        [PXUIField(Enabled = false)]
        public virtual Int32? InventoryID
        {
            get { return this._InventoryID; }
            set { this._InventoryID = value; }
        }
        #endregion
    }
}


