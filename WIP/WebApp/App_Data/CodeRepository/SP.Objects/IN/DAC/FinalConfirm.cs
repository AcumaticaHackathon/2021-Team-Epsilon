using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.CM;
using PX.Objects.SO;

namespace SP.Objects.IN.DAC
{
    [Serializable]
    public class FinalConfirm : IBqlTable
    {
        #region Comment
        public abstract class comment : PX.Data.IBqlField
        {
        }
        protected String _Comment;
        [PXString(IsUnicode = true)]
        [PXUIField(DisplayName = "Comment")]
        public virtual String Comment
        {
            get
            {
                return this._Comment;
            }
            set
            {
                this._Comment = value;
            }
        }
        #endregion
        
        #region CuryLineTotal
        public abstract class curyLineTotal : PX.Data.IBqlField
        {
        }
        protected Decimal? _CuryLineTotal;
        [PXBaseCury()]
        [PXUIField(DisplayName = "Cart Total", Enabled = false)]
        public virtual Decimal? CuryLineTotal
        {
            get
            {
                return this._CuryLineTotal;
            }
            set
            {
                this._CuryLineTotal = value;
            }
        }
        #endregion

        #region CuryFreightAmt
        public abstract class curyFreightAmt : PX.Data.IBqlField
        {
        }
        protected Decimal? _CuryFreightAmt;
        [PXBaseCury()]
        [PXUIField(DisplayName = "Freight", Enabled = false)]
        public virtual Decimal? CuryFreightAmt
        {
            get
            {
                return this._CuryFreightAmt;
            }
            set
            {
                this._CuryFreightAmt = value;
            }
        }
        #endregion

        #region CuryTaxTotal
        public abstract class curyTaxTotal : PX.Data.IBqlField
        {
        }
        protected Decimal? _CuryTaxTotal;
        [PXBaseCury()]
        [PXUIField(DisplayName = "Tax Total", Enabled = false)]
        public virtual Decimal? CuryTaxTotal
        {
            get
            {
                return this._CuryTaxTotal;
            }
            set
            {
                this._CuryTaxTotal = value;
            }
        }
        #endregion

        #region CuryDiscTot
        public abstract class curyDiscTot : PX.Data.IBqlField
        {
        }
        protected Decimal? _CuryDiscTot;
        [PXBaseCury()]
        [PXUIField(DisplayName = "Discount Total", Enabled = false)]
        public virtual Decimal? CuryDiscTot
        {
            get
            {
                return this._CuryDiscTot;
            }
            set
            {
                this._CuryDiscTot = value;
            }
        }
        #endregion

        #region CuryOrderTotal
        public abstract class curyOrderTotal : PX.Data.IBqlField
        {
        }
        protected Decimal? _CuryOrderTotal;
        [PXBaseCury()]
        [PXUIField(DisplayName = "Total", Enabled = false)]
        public virtual Decimal? CuryOrderTotal
        {
            get
            {
                return this._CuryOrderTotal;
            }
            set
            {
                this._CuryOrderTotal = value;
            }
        }
        #endregion



        #region CurrencyStatus
        public abstract class currencyStatus : PX.Data.IBqlField
        {
        }
        protected String _CurrencyStatus;
        [PXString(IsUnicode = true)]
        [PXUIField()]
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

        #region Shipping Information
        public abstract class shippingInformation : PX.Data.IBqlField
        {
        }
        protected string _ShippingInformation;
        [PXString(IsUnicode = true)]
        [PXUIField(DisplayName = "Shipping Information", Enabled = false)]
        public virtual string ShippingInformation
        {
            get
            {
                return this._ShippingInformation;
            }
            set
            {
                this._ShippingInformation = value;
            }
        }
        #endregion
    }
}
