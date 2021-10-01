using System;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.AR
{
	public sealed class ARInvoiceMultipleBaseCurrenciesRestriction : PXCacheExtension<ARInvoiceVisibilityRestriction,ARInvoice>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFormula(typeof(Switch<
				Case<Where<ARInvoice.customerLocationID, IsNull, And<UnattendedMode, Equal<True>>>, Null,
				Case<Where<Selector<ARInvoice.customerLocationID, Location.cBranchID>, IsNotNull>,
					Selector<ARInvoice.customerLocationID, Location.cBranchID>,
				Case<Where<ARInvoice.customerID, IsNotNull, 
						And<Not<Selector<ARInvoice.customerID, Customer.cOrgBAccountID>, RestrictByBranch<Current2<ARInvoice.branchID>>>>>,
					Null,
				Case<Where<Current2<ARInvoice.branchID>, IsNotNull>,
					Current2<ARInvoice.branchID>,
				Case<Where<ARInvoice.customerID, IsNotNull, 
						And<Selector<ARInvoice.customerID, Customer.baseCuryID>, NotEqual<Current<AccessInfo.baseCuryID>>>>,
					Null>>>>>, 
				Current<AccessInfo.branchID>>))]
		public Int32? BranchID{get;	set;}
		#endregion

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<branchBaseCuryID>, IsNull,
			Or<Customer.baseCuryID, Equal<Current2<branchBaseCuryID>>>>), null)]
		public int? CustomerID { get; set; }
		#endregion

		#region BranchBaseCuryID
		public new abstract class branchBaseCuryID : PX.Data.BQL.BqlString.Field<branchBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<ARInvoice.branchID, Branch.baseCuryID>))]
		public string BranchBaseCuryID { get; set; }
		#endregion
		#region CustomerBaseCuryID
		public new abstract class customerBaseCuryID : PX.Data.BQL.BqlString.Field<customerBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<ARInvoice.customerID, Customer.baseCuryID>))]
		public string CustomerBaseCuryID { get; set; }
		#endregion
	}
}