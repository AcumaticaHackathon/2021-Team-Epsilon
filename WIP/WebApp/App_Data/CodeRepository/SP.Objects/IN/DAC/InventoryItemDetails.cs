using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.IN;
using SP.Objects.IN.Descriptor;

namespace SP.Objects.IN.DAC
{
    [Serializable]
    public class InventoryItemDetails : IBqlTable
    {
        #region InventoryID
        public abstract class inventoryID : PX.Data.IBqlField
        {
        }

        protected Int32? _InventoryID;
        [PXDBInt(IsKey = true)]
        [PXDefault()]
        [PXUIField(Enabled = false)]
        public virtual Int32? InventoryID
        {
            get { return this._InventoryID; }
            set { this._InventoryID = value; }
        }
        #endregion

        #region PictureNumber
        public abstract class pictureNumber : PX.Data.IBqlField
        {
        }

        protected Int32? _PictureNumber;
        [PXDBInt()]
        [PXDefault()]
        [PXUIField(Enabled = false)]
        public virtual Int32? PictureNumber
        {
            get { return this._PictureNumber; }
            set { this._PictureNumber = value; }
        }
        #endregion

        #region Description
        public abstract class description : IBqlField { }
        [PXDBString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Description", Enabled = false)]
        public virtual String Description { get; set; }
        #endregion

        #region InventoryDescription
        public abstract class inventoryDescription : IBqlField { }
        [PXDBString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Inventory Description", Enabled = false)]
        public virtual String InventoryDescription { get; set; }
        #endregion

        #region Qty
        public abstract class qty : PX.Data.IBqlField
        {
        }
        protected Decimal? _Qty;
        [PXDBQuantity(MinValue = 0, MaxValue = 9999999)]
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

        #region UOM
        public abstract class uOM : PX.Data.IBqlField
        {
        }
        protected String _UOM;
        [INUnit(typeof(inventoryID), DisplayName = "Unit")]
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

        #region SiteIDLabel
        public abstract class siteIDLabel : IBqlField { }

        [PXString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Warehouse", Enabled = false)]
        [PXDefault("Warehouse", PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual String SiteIDLabel
        {
            get { return "Warehouse"; }
        }

        #endregion

        #region QtyLabel
        public abstract class qtyLabel : IBqlField { }

        [PXString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Quantity", Enabled = false)]
        [PXDefault("Quantity", PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual String QtyLabel
        {
            get { return "Quantity"; }
        }

        #endregion

        #region UOMLabel
        public abstract class uOMLabel : IBqlField { }

        [PXString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "UOM", Enabled = false)]
        [PXDefault("UOM", PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual String UOMLabel
        {
            get { return "UOM"; }
        }

        #endregion

        #region SiteID
        public abstract class siteID : PX.Data.IBqlField
        {
        }
        protected Int32? _SiteID;
        [Attributes.PortalSiteDBAvailAttribute(typeof(inventoryID))]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
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
    }
}
