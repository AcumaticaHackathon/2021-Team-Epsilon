using PX.Data;
using PX.Objects.CM;
using PX.Objects.IN;
using System;

namespace PX.Objects.FS
{
    [Serializable]
    public class FSProfitability : IBqlTable
    {
        #region LineRef
        public abstract class lineRef : PX.Data.BQL.BqlString.Field<lineRef> { }

        [PXString(4, IsFixed = true)]
        [PXUIField(DisplayName = "Ref. Nbr.")]
        public virtual string LineRef { get; set; }
        #endregion
        #region LineType
        public abstract class lineType : ListField_LineType_Profitability
        {
        }

        [PXString(5, IsFixed = true)]
        [PXUIField(DisplayName = "Line Type")]
        [lineType.ListAtrribute]
        [PXDefault]
        public virtual string LineType { get; set; }
        #endregion
        #region ItemID
        public abstract class itemID : PX.Data.BQL.BqlInt.Field<itemID> { }

        [InventoryIDByLineType(typeof(lineType))]
        public virtual int? ItemID { get; set; }
        #endregion
        #region Descr
        public abstract class descr : PX.Data.BQL.BqlString.Field<descr> { }

        [PXString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Description")]
        public virtual string Descr { get; set; }
        #endregion
        #region EmployeeID
        public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }

        [PXInt]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [FSSelector_StaffMember_ServiceOrderProjectID]
        [PXUIField(DisplayName = "Staff Member")]
        public virtual int? EmployeeID { get; set; }
        #endregion
        #region CuryInfoID
        public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
        [PXLong]
        [CurrencyInfo(typeof(FSAppointment.curyInfoID))]
        public virtual Int64? CuryInfoID { get; set; }
        #endregion
        #region UnitPrice
        public abstract class unitPrice : PX.Data.BQL.BqlDecimal.Field<unitPrice> { }

        [PXPriceCost]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Unit Price")]
        public virtual decimal? UnitPrice { get; set; }
        #endregion
        #region CuryUnitPrice
        public abstract class curyUnitPrice : PX.Data.BQL.BqlDecimal.Field<curyUnitPrice> { }

        [PXCurrency(typeof(curyInfoID), typeof(unitPrice))]
        [PXUIField(DisplayName = "Unit Price")]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual decimal? CuryUnitPrice { get; set; }
        #endregion
        #region EstimatedQty
        public abstract class estimatedQty : PX.Data.BQL.BqlDecimal.Field<estimatedQty> { }

        [PXQuantity]
        [PXDefault(TypeCode.Decimal, "1.0")]
        [PXUIField(DisplayName = "Estimated Quantity")]
        public virtual decimal? EstimatedQty { get; set; }
        #endregion
        #region EstimatedAmount
        public abstract class estimatedAmount : PX.Data.BQL.BqlDecimal.Field<estimatedAmount> { }

        [PXDecimal]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Estimated Amount")]
        public virtual decimal? EstimatedAmount { get; set; }
        #endregion
        #region CuryEstimatedAmount
        public abstract class curyEstimatedAmount : PX.Data.BQL.BqlDecimal.Field<curyEstimatedAmount> { }

        [PXCurrency(typeof(curyInfoID), typeof(estimatedAmount))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Estimated Amount")]
        public virtual decimal? CuryEstimatedAmount { get; set; }
        #endregion
        #region ActualDuration
        public abstract class actualDuration : PX.Data.BQL.BqlInt.Field<actualDuration> { }

        [FSDBTimeSpanLongAllowNegative]
        [PXUIField(DisplayName = "Actual Duration")]
        public virtual int? ActualDuration { get; set; }
        #endregion
        #region ActualQty
        public abstract class actualQty : PX.Data.BQL.BqlDecimal.Field<actualQty> { }

        [PXQuantity]
        [PXDefault(TypeCode.Decimal, "1.0")]
        [PXUIField(DisplayName = "Actual Quantity")]
        public virtual decimal? ActualQty { get; set; }
        #endregion
        #region ActualAmount
        public abstract class actualAmount : PX.Data.BQL.BqlDecimal.Field<actualAmount> { }

        [PXDecimal]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Actual Amount")]
        public virtual decimal? ActualAmount { get; set; }
        #endregion
        #region CuryActualAmount
        public abstract class curyActualAmount : PX.Data.BQL.BqlDecimal.Field<curyActualAmount> { }

        [PXCurrency(typeof(curyInfoID), typeof(actualAmount))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Actual Amount")]
        public virtual decimal? CuryActualAmount { get; set; }
        #endregion
        #region BillableQty
        public abstract class billableQty : PX.Data.BQL.BqlDecimal.Field<billableQty> { }

        [PXQuantity]
        [PXDefault(TypeCode.Decimal, "1.0")]
        [PXUIField(DisplayName = "Billable Quantity")]
        public virtual decimal? BillableQty { get; set; }
        #endregion
        #region BillableAmount
        public abstract class billableAmount : PX.Data.BQL.BqlDecimal.Field<billableAmount> { }

        [PXDecimal]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Billable Amount")]
        public virtual decimal? BillableAmount { get; set; }
        #endregion
        #region CuryBillableAmount
        public abstract class curyBillableAmount : PX.Data.BQL.BqlDecimal.Field<curyBillableAmount> { }

        [PXCurrency(typeof(curyInfoID), typeof(billableAmount))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Billable Amount")]
        public virtual decimal? CuryBillableAmount { get; set; }
        #endregion
        #region UnitCost
        public abstract class unitCost : PX.Data.BQL.BqlDecimal.Field<unitCost> { }

        [PXPriceCost()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Unit Cost")]
        public virtual Decimal? UnitCost { get; set; }
        #endregion
        #region CuryUnitCost
        public abstract class curyUnitCost : PX.Data.BQL.BqlDecimal.Field<curyUnitCost> { }

        [PXCurrency(typeof(curyInfoID), typeof(unitCost))]
        [PXUIField(DisplayName = "Unit Cost")]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual Decimal? CuryUnitCost { get; set; }
        #endregion
        #region EstimatedCost
        public abstract class estimatedCost : PX.Data.BQL.BqlDecimal.Field<estimatedCost> { }

        [PXPriceCost()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Estimated Cost")]
        public virtual Decimal? EstimatedCost { get; set; }
        #endregion
        #region CuryEstimatedCost
        public abstract class curyEstimatedCost : PX.Data.BQL.BqlDecimal.Field<curyEstimatedCost> { }

        [PXCurrency(typeof(curyInfoID), typeof(estimatedCost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Estimated Cost")]
        public virtual Decimal? CuryEstimatedCost { get; set; }
        #endregion
        #region ExtCost
        public abstract class extCost : PX.Data.BQL.BqlDecimal.Field<extCost> { }

        [PXPriceCost()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Ext. Cost")]
        public virtual Decimal? ExtCost { get; set; }
        #endregion
        #region CuryExtCost
        public abstract class curyExtCost : PX.Data.BQL.BqlDecimal.Field<curyExtCost> { }

        [PXCurrency(typeof(curyInfoID), typeof(extCost))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Ext. Cost")]
        public virtual Decimal? CuryExtCost { get; set; }
        #endregion
        
        #region Profit
        public abstract class profit : PX.Data.BQL.BqlDecimal.Field<profit> { }

        [PXDecimal]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Profit")]
        public virtual decimal? Profit { get; set; }
        #endregion
        #region CuryProfit
        public abstract class curyProfit : PX.Data.BQL.BqlDecimal.Field<curyProfit> { }

        [PXCurrency(typeof(curyInfoID), typeof(profit))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Profit")]
        public virtual Decimal? CuryProfit { get; set; }
        #endregion
        #region ProfitPercent
        public abstract class profitPercent : PX.Data.BQL.BqlDecimal.Field<profitPercent> { }

        [PXDecimal]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Profit (%)")]
        public virtual decimal? ProfitPercent { get; set; }
        #endregion

        #region Constructors

        public FSProfitability()
        {
        }

        public FSProfitability(FSAppointmentDet fsAppointmentDetRow)
        {
            this.LineRef = fsAppointmentDetRow.LineRef;
            this.LineType = fsAppointmentDetRow.LineType;
            this.ItemID = fsAppointmentDetRow.InventoryID;
            this.Descr = fsAppointmentDetRow.TranDesc;
            this.CuryInfoID = fsAppointmentDetRow.CuryInfoID;
            this.CuryUnitPrice = fsAppointmentDetRow.CuryUnitPrice;
            this.EstimatedQty = fsAppointmentDetRow.EstimatedQty;
            this.CuryEstimatedAmount = fsAppointmentDetRow.CuryEstimatedTranAmt;
            this.ActualDuration = fsAppointmentDetRow.IsService ? fsAppointmentDetRow.ActualDuration : 0;
            this.ActualQty = fsAppointmentDetRow.ActualQty;
            this.CuryActualAmount = fsAppointmentDetRow.CuryTranAmt;
            this.BillableQty = fsAppointmentDetRow.BillableQty;
            this.CuryBillableAmount = fsAppointmentDetRow.CuryBillableTranAmt;
            this.CuryUnitCost = fsAppointmentDetRow.CuryUnitCost;
            this.CuryEstimatedCost = this.EstimatedQty * this.CuryUnitCost;
            this.CuryExtCost = (fsAppointmentDetRow.IsLinkedItem == true ? fsAppointmentDetRow.CuryExtCost : this.ActualQty * this.CuryUnitCost);
            this.CuryProfit = Math.Round((decimal)this.CuryBillableAmount, 2) - Math.Round((decimal)this.CuryExtCost, 2);
            this.ProfitPercent = this.CuryExtCost == 0.0m ? 0.0m : (this.CuryProfit / this.CuryExtCost) * 100;
        }

        public FSProfitability(FSLog fsLogRow)
        {
            this.LineRef = fsLogRow.LineRef;
            this.LineType = ID.LineType_Profitability.LABOR_ITEM;
            this.CuryInfoID = fsLogRow.CuryInfoID;
            this.ItemID = fsLogRow.LaborItemID;
            this.EmployeeID = fsLogRow.BAccountID;
            this.ActualDuration = fsLogRow.TimeDuration;
            this.CuryUnitCost = fsLogRow.CuryUnitCost;
            this.ActualQty = ((decimal)fsLogRow.TimeDuration) / 60m;
            this.CuryActualAmount = this.ActualQty * this.CuryUnitCost;
            this.CuryExtCost = fsLogRow.CuryExtCost;
            this.BillableQty = 0m;
            this.CuryBillableAmount = 0m;

            if (fsLogRow.IsBillable == true)
            {
                this.BillableQty = ((decimal)fsLogRow.BillableTimeDuration) / 60m;
                this.CuryBillableAmount = this.BillableQty * this.CuryUnitCost;
            }

            this.CuryProfit = Math.Round((decimal)this.CuryBillableAmount, 2) - Math.Round((decimal)this.CuryExtCost, 2);
            this.ProfitPercent = this.CuryExtCost == 0.0m ? 0.0m : (this.CuryProfit / this.CuryExtCost) * 100;
        }

        #endregion
    }
}
