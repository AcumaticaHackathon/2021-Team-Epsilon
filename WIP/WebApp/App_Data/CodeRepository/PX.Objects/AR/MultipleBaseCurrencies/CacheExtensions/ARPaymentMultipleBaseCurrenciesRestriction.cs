using System;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.AR
{
	public sealed class ARPaymentMultipleBaseCurrenciesRestriction : PXCacheExtension<ARPaymentVisibilityRestriction, ARPayment>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0)]
		[PXFormula(typeof(Switch<
				Case<Where<ARPayment.customerLocationID, IsNull, And<UnattendedMode, Equal<True>>>, Null, 
				Case<Where<Selector<ARPayment.customerLocationID, Location.cBranchID>, IsNotNull>,
					Selector<ARPayment.customerLocationID, Location.cBranchID>,
				Case<Where<ARPayment.customerID, IsNotNull,
						And<Not<Selector<ARPayment.customerID, Customer.cOrgBAccountID>, RestrictByBranch<Current2<ARPayment.branchID>>>>>,
					Null,
				Case<Where<Current2<ARPayment.branchID>, IsNotNull>,
					Current2<ARPayment.branchID>,
				Case<Where<ARPayment.customerID, IsNotNull, 
						And<Selector<ARPayment.customerID, Customer.baseCuryID>, NotEqual<Current<AccessInfo.baseCuryID>>>>,
					Null>>>>>, 
				Current<AccessInfo.branchID>>))]
		public Int32? BranchID{get;	set;}
		#endregion

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current<branchBaseCuryID>, IsNull,
			Or<Customer.baseCuryID, Equal<Current<branchBaseCuryID>>>>), null)]
		public int? CustomerID { get; set; }
		#endregion

		#region BranchBaseCuryID
		public new abstract class branchBaseCuryID : PX.Data.BQL.BqlString.Field<branchBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<ARPayment.branchID, Branch.baseCuryID>))]
		public string BranchBaseCuryID { get; set; }
		#endregion
		#region CustomerBaseCuryID
		public new abstract class customerBaseCuryID : PX.Data.BQL.BqlString.Field<customerBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<ARPayment.customerID, Customer.baseCuryID>))]
		public string CustomerBaseCuryID { get; set; }
		#endregion
	}
}