using System;

namespace PX.Objects.PO
{
	public interface IPOReturnLineSource
	{
		string ReceiptNbr { get; }
		int? LineNbr { get; }
		string LineType { get; }
		string POType { get; }
		string PONbr { get; }
		int? POLineNbr { get; }
		int? InventoryID { get; }
		bool? AccrueCost { get; }
		int? SubItemID { get; }
		int? SiteID { get; }
		int? LocationID { get; }
		string LotSerialNbr { get; }
		DateTime? ExpireDate { get; }
		string UOM { get; }
		decimal? ReceiptQty { get; }
		decimal? BaseReceiptQty { get; }
		decimal? ReturnedQty { get; set; }
		decimal? BaseReturnedQty { get; }
		Int64? CuryInfoID { get; }
		int? ExpenseAcctID { get; }
		int? ExpenseSubID { get; }
		int? POAccrualAcctID { get; }
		int? POAccrualSubID { get; }
		string TranDesc { get; }
		int? CostCodeID { get; }
		int? ProjectID { get; }
		int? TaskID { get; }
		bool? AllowEditUnitCost { get; }
		bool? ManualPrice { get; }
		decimal? DiscPct { get; }
		decimal? CuryDiscAmt { get; }
		decimal? DiscAmt { get; }
		decimal? UnitCost { get; }
		decimal? CuryUnitCost { get; }
		decimal? ExtCost { get; }
		decimal? CuryExtCost { get; }
		decimal? TranCostFinal { get; }
		decimal? TranCost { get; }
		decimal? CuryTranCost { get; }
	}
}
