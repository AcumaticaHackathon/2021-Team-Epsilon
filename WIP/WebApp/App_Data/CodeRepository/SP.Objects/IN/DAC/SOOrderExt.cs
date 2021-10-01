using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Api;
using PX.Data;
using PX.Data.Wiki.Parser.Rtf;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.SO;
using PX.SM;
using SP.Objects.CR;
using SP.Objects.IN.Descriptor;

namespace SP.Objects.IN.DAC
{
    [Serializable]
	[CRPrimaryGraphRestricted(
		new[]{
			typeof(SOOrderEntry)
		},
		new[]{
			typeof(Select<
				SOOrder,
				Where<
					SOOrder.orderType, Equal<Current<SOOrder.orderType>>,
					And<SOOrder.orderNbr, Equal<Current<SOOrder.orderNbr>>,
					And<MatchWithBAccountNotNull<SOOrder.customerID>>>>>)
		})]
	public class SOOrderExt : PXCacheExtension<SOOrder>
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

        #region IsEnabledCancelOrder
        public abstract class isEnabledCancelOrder : PX.Data.IBqlField
        {
        }
        protected Boolean? _IsEnabledCancelOrder;
        [PXBool()]
        [PXUIField(DisplayName = "Is Enabled Cancel Order", Visible = false, Visibility = PXUIVisibility.Invisible)]
        public virtual Boolean? IsEnabledCancelOrder
        {
            get
            {
                return Base.Status == SOOrderStatus.Hold || Base.Status == SOOrderStatus.Open || Base.Status == SOOrderStatus.CreditHold;
            }
        }
        #endregion

        #region OverrideShipment
        public abstract class overrideShipment : PX.Data.IBqlField
        {
        }
        protected Boolean? _OverrideShipment;
        [PXBool()]
        [PXUIField(DisplayName = "Edit Address", Visibility = PXUIVisibility.Invisible)]
        public virtual Boolean? OverrideShipment
        {
            get
            {
                return this._OverrideShipment;
            }
            set
            {
                this._OverrideShipment = value;
            }
        }
        #endregion

        #region IsSecondScreen
        public abstract class isSecondScreen : PX.Data.IBqlField
        {
        }
        protected Int32 _IsSecondScreen;
        [PXInt()]
        [PXUIField(DisplayName = "Is First Screen", Visible = false)]
        [PXDefault(1)]
        public virtual Int32 IsSecondScreen
        {
            get
            {
                return this._IsSecondScreen;
            }
            set
            {
                this._IsSecondScreen = value;
            }
        }
        #endregion

        #region CreatedByID
        public abstract class createdByIDForFilter : PX.Data.IBqlField
        {
        }
        protected Guid? _CreatedByIDForFilter;
        [PXGuid()]
        [PXUIField(Visibility = PXUIVisibility.Invisible, Visible = false)]
        public virtual Guid? CreatedByIDForFilter
        {
            get
            {
                return Base.CreatedByID;
            }
        }
        #endregion

        #region CustomerLocationID
        public abstract class customerLocationID : PX.Data.IBqlField
        {
        }
        protected Int32? _CustomerLocationID;
        [LocationID(typeof(Where<Location.bAccountID, Equal<Current<SOOrder.customerID>>,
            And<Location.isActive, Equal<True>>>), DescriptionField = typeof(Location.descr), Visibility = PXUIVisibility.SelectorVisible)]
        [PXDefault(typeof(Coalesce<Search2<BAccountR.defLocationID,
            InnerJoin<Location, On<Location.bAccountID, Equal<BAccountR.bAccountID>, And<Location.locationID, Equal<BAccountR.defLocationID>>>>,
            Where<BAccountR.bAccountID, Equal<Current<SOOrder.customerID>>,
                And<Location.isActive, Equal<True>>>>,
            Search<Location.locationID,
            Where<Location.bAccountID, Equal<Current<SOOrder.customerID>>,
            And<Location.isActive, Equal<True>>>>>))]
        public virtual Int32? CustomerLocationID
        {
            get
            {
                return this._CustomerLocationID;
            }
            set
            {
                this._CustomerLocationID = value;
            }
        }
        #endregion

        #region ShipComplete
        public abstract class shipComplete : PX.Data.IBqlField
        {
        }
        protected String _ShipComplete;
        [PXDBString(1, IsFixed = true)]
        [PXDefault(Attributes.SOShipCompletePortal.CancelRemainder)]
        [Attributes.SOShipCompletePortal.List()]
        [PXUIField(DisplayName = "Ship Complete")]
        public virtual String ShipComplete
        {
            get
            {
                return this._ShipComplete;
            }
            set
            {
                this._ShipComplete = value;
            }
        }
        #endregion

        #region CuryMiscTot + CuryLineTotal
        public abstract class portalLineTotal : PX.Data.IBqlField
        {
        }
        [PXBaseCury()]
        [PXUIField(DisplayName = "Line Total", Enabled = false)]
        [PXDependsOnFields(typeof(SOOrder.curyMiscTot), typeof(SOOrder.curyLineTotal))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual Decimal? PortalLineTotal
        {
            get
            {
                return Base.CuryMiscTot + Base.CuryLineTotal;
            }
        }
        #endregion
    }
}
