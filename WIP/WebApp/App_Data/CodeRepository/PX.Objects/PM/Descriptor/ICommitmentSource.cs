using CommonServiceLocator;
using PX.Data;
using PX.Objects.CA.Descriptor;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    interface ICommitmentSource 
	{ 
		Guid? CommitmentID { get; }
		int? ExpenseAcctID { get; }
		int? ProjectID { get; }
		int? TaskID { get; }
		int? InventoryID { get; }
		int? CostCodeID { get; }
		int? BranchID { get; }
		string UOM { get; }
		decimal? OrigExtCost { get; }
		decimal? OrigOrderQty { get; }
		decimal? CuryExtCost { get; }
		decimal? ExtCost { get; }
		decimal? OrderQty { get; }
		decimal? CuryRetainageAmt { get; }
		decimal? CompletedQty { get; }
		decimal? ReceivedQty { get; }
		decimal? BilledQty { get; }
		decimal? CuryBilledAmt { get; }
		decimal? BilledAmt { get; }
		decimal? RetainageAmt { get; }
		bool? Completed { get; }
		bool? Closed { get; }
		bool? Cancelled { get; }
		string CompletePOLine { get; }
	}

}