using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Api;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SP.DAC;
using PX.SM;

namespace SP.Objects.IN
{
    [Serializable]
    public class PortalCardLine : IBqlTable
    {
        #region ItemTotal
        public abstract class itemTotal : PX.Data.IBqlField
        {
        }

        protected Decimal? _ItemTotal;

        [PXDecimal()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Number of items")]
        public virtual Decimal? ItemTotal
        {
            get { return this._ItemTotal; }
            set { this._ItemTotal = value; }
        }

        #endregion

        #region AllTotalPrice
        public abstract class allTotalPrice : PX.Data.IBqlField
        {
        }

        protected Decimal? _AllTotalPrice;
        [PXBaseCury()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Subtotal", Enabled = false)]
        public virtual Decimal? AllTotalPrice
        {
            get { return this._AllTotalPrice; }
            set { this._AllTotalPrice = value; }
        }
        #endregion

        #region CurrencyStatus
        public abstract class currencyStatus : PX.Data.IBqlField
        {
        }
        protected String _CurrencyStatus;
        [PXString(IsUnicode = true)]
        [PXUIField(DisplayName = "Currency")]
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
    }

    [Serializable]
    public class PortalCardLines : IBqlTable
    {
		// для связи с юзером
		#region RecordID
		public abstract class recordID : PX.Data.IBqlField
		{
		}
		protected Int32? _RecordID;
		[PXDBIdentity(IsKey = true)]
		public virtual Int32? RecordID
		{
			get
			{
				return _RecordID;
			}
			set
			{
				_RecordID = value;
			}
		}
		#endregion

		#region UserID
		public abstract class userID : BqlGuid.Field<userID>
        {
        }
        protected Guid? _UserID;
        [PXDBGuid()]
        [PXDefault]
        [PXUIField(Visibility = PXUIVisibility.Invisible, Enabled = false)]
        public virtual Guid? UserID
        {
            get
            {
                return this._UserID;
            }
            set
            {
                this._UserID = value;
            }
        }
        #endregion

        #region InventoryID
        public abstract class inventoryID : BqlInt.Field<inventoryID>
        {
        }

        protected Int32? _InventoryID;
        [PXDBInt()]
        [PXDefault()]
        [PXUIField(Enabled = false)]
        public virtual Int32? InventoryID
        {
            get { return this._InventoryID; }
            set { this._InventoryID = value; }
        }
        #endregion

        #region InventoryIDDescription
        public abstract class inventoryIDDescription : PX.Data.IBqlField
        {
        }
        protected String _InventoryIDDescription;

		[PXDimension(InventoryRawAttribute.DimensionName)]
		[PXDBString]
		[PXUIField(DisplayName = "Inventory ID", Enabled = false)]
		public virtual String InventoryIDDescription
        {
            get
            {
                return this._InventoryIDDescription;
            }
            set
            {
                this._InventoryIDDescription = value;
            }
        }
        #endregion

        #region SiteIDDescription
        public abstract class siteIDDescription : PX.Data.IBqlField
        {
        }

        protected String _SiteIDDescription;
        [PXDBString]
        [PXUIField(DisplayName = "Warehouse", Enabled = false)]
        public virtual String SiteIDDescription
        {
            get { return this._SiteIDDescription; }
            set { this._SiteIDDescription = value; }
        }
        #endregion

        #region SiteID
        public abstract class siteID : BqlInt.Field<siteID>
        {
        }
        protected Int32? _SiteID;
        [Site(Enabled = false)]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIRequired(typeof(IIf<FeatureInstalled<FeaturesSet.inventory>, True, False>))]
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

		#region PortalNoteID
	    public abstract class portalNoteID : BqlGuid.Field<portalNoteID>
	    {
	    }
	    protected Guid? _PortalNoteID;
	    [PXNote()]
	    public virtual Guid? PortalNoteID
	    {
		    get
		    {
			    return this._PortalNoteID;
		    }
		    set
		    {
			    this._PortalNoteID = value;
		    }
	    }
	    #endregion

        #region Descr
        public abstract class descr : PX.Data.IBqlField
        {
        }
        protected String _Descr;
        [PXDBString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Description", Enabled = false)]
        public virtual String Descr
        {
            get
            {
                return this._Descr;
            }
            set
            {
                this._Descr = value;
            }
        }
        #endregion

        #region Qty
        public abstract class qty : PX.Data.IBqlField
        {
        }
        protected Decimal? _Qty;
        [PXDBQuantity()]
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
        public abstract class orderQty : PX.Data.IBqlField { }
        [PXDecimal()]
        public virtual Decimal? OrderQty
        {
            get { return _Qty; }
        }
        #endregion

        #region BaseOrderQty
        public abstract class baseOrderQty : PX.Data.IBqlField { }
        [PXDecimal()]
        public virtual Decimal? BaseOrderQty
        {
            get { return _Qty; }
        }
        #endregion
        #endregion

        #region UOM
        public abstract class uOM : BqlString.Field<uOM>
        {
        }
        protected String _UOM;
        //[PXDBString(255, IsUnicode = true)]
        [INUnit(typeof(inventoryID), DisplayName = "Unit")]
        [PXUIField(DisplayName = "Unit", Enabled = false)]
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
		[PXDecimal(typeof(Search<CommonSetup.decPlPrcCst>))]
        [PXUIField(DisplayName = "Price", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
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

        #region BaseDiscountAmt
        public abstract class baseDiscountAmt : IBqlField { }
        protected Decimal? _BaseDiscountAmt;
        [PXDBDecimal(6)]
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
        [PXDBDecimal(6)]
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

        #region TotalPrice
        public abstract class totalPrice : PX.Data.IBqlField
        {
        }
        protected Decimal? _TotalPrice;
        [PXBaseCury()]
        [PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Total", Enabled = false)]
        public virtual Decimal? TotalPrice
        {
            get { return Qty * CuryUnitPrice - BaseDiscountAmt; }
        }
        #endregion

        #region CurrentWarehouse
        public abstract class currentWarehouse : PX.Data.IBqlField
        {
        }
        protected Decimal? _CurrentWarehouse;
        [PXQuantity()]
        [PXUIField(DisplayName = "This Warehouse", Enabled = false)]
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
        [PXQuantity()]
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

        #region StkItem
        public abstract class stkItem : PX.Data.IBqlField
        {
        }
        protected Boolean? _StkItem;
        [PXDBBool()]
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

        #region CreatedByID
        public abstract class createdByID : PX.Data.IBqlField
        {
        }

        protected Guid? _CreatedByID;

        [PXDBCreatedByID()]
        public virtual Guid? CreatedByID
        {
            get { return this._CreatedByID; }
            set { this._CreatedByID = value; }
        }

        #endregion

        #region CreatedByScreenID

        public abstract class createdByScreenID : PX.Data.IBqlField
        {
        }

        protected String _CreatedByScreenID;

        [PXDBCreatedByScreenID()]
        public virtual String CreatedByScreenID
        {
            get { return this._CreatedByScreenID; }
            set { this._CreatedByScreenID = value; }
        }

        #endregion

        #region CreatedDateTime

        public abstract class createdDateTime : PX.Data.IBqlField
        {
        }

        protected DateTime? _CreatedDateTime;

        [PXDBCreatedDateTime()]
        public virtual DateTime? CreatedDateTime
        {
            get { return this._CreatedDateTime; }
            set { this._CreatedDateTime = value; }
        }

        #endregion

        #region LastModifiedByID

        public abstract class lastModifiedByID : PX.Data.IBqlField
        {
        }

        protected Guid? _LastModifiedByID;

        [PXDBLastModifiedByID()]
        public virtual Guid? LastModifiedByID
        {
            get { return this._LastModifiedByID; }
            set { this._LastModifiedByID = value; }
        }

        #endregion

        #region LastModifiedByScreenID

        public abstract class lastModifiedByScreenID : PX.Data.IBqlField
        {
        }

        protected String _LastModifiedByScreenID;

        [PXDBLastModifiedByScreenID()]
        public virtual String LastModifiedByScreenID
        {
            get { return this._LastModifiedByScreenID; }
            set { this._LastModifiedByScreenID = value; }
        }

        #endregion

        #region LastModifiedDateTime

        public abstract class lastModifiedDateTime : PX.Data.IBqlField
        {
        }

        protected DateTime? _LastModifiedDateTime;

        [PXDBLastModifiedDateTime()]
        public virtual DateTime? LastModifiedDateTime
        {
            get { return this._LastModifiedDateTime; }
            set { this._LastModifiedDateTime = value; }
        }

        #endregion

        #region tstamp

        public abstract class Tstamp : PX.Data.IBqlField
        {
        }

        protected Byte[] _tstamp;

        [PXDBTimestamp(RecordComesFirst = true)]
        public virtual Byte[] tstamp
        {
            get { return this._tstamp; }
            set { this._tstamp = value; }
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
    }

    [System.SerializableAttribute()]
	[CRPrimaryGraphRestricted(new[]
		{
			typeof(ImageViewer)
		},
		new[]
		{
			typeof(Select2<
					InventoryItem,
				InnerJoin<InventoryLines,
					On<InventoryLines.inventoryID, Equal<InventoryItem.inventoryID>>>,
			Where<
				InventoryItem.inventoryID, Equal<Current<InventoryItem.inventoryID>>>>)
		})]
	public class InventoryItemExt : PXCacheExtension<InventoryItem> { }
}


