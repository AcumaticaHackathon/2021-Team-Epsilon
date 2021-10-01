using System;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.AP
{
	public sealed class APPaymentMultipleBaseCurrenciesRestriction : PXCacheExtension<APPaymentVisibilityRestriction, APPayment>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFormula(typeof(Switch<
				Case<Where<APPayment.vendorLocationID, IsNull, And<UnattendedMode, Equal<True>>>, Null,
					Case<Where<Selector<APPayment.vendorLocationID, Location.vBranchID>, IsNotNull>,
					Selector<APPayment.vendorLocationID, Location.vBranchID>,
				Case<Where<APPayment.vendorID, IsNotNull,
						And<Not<Selector<APPayment.vendorID, Vendor.vOrgBAccountID>, RestrictByBranch<Current2<APPayment.branchID>>>>>,
					Null,
				Case<Where<Current2<APPayment.branchID>, IsNotNull>,
					Current2<APPayment.branchID>,
				Case<Where<APPayment.vendorID, IsNotNull, 
						And<Selector<APPayment.vendorID, Vendor.baseCuryID>, NotEqual<Current<AccessInfo.baseCuryID>>>>,
					Null>>>>>, 
				Current<AccessInfo.branchID>>))]
		public Int32? BranchID{get;	set;}
		#endregion

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current<branchBaseCuryID>, IsNull,
			Or<Vendor.baseCuryID, Equal<Current<branchBaseCuryID>>>>), null)]
		public int? VendorID { get; set; }
		#endregion

		#region BranchBaseCuryID
		public new abstract class branchBaseCuryID : PX.Data.BQL.BqlString.Field<branchBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<APPayment.branchID, Branch.baseCuryID>))]
		public string BranchBaseCuryID { get; set; }
		#endregion
		#region VendorBaseCuryID
		public new abstract class vendorBaseCuryID : PX.Data.BQL.BqlString.Field<vendorBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<APPayment.vendorID, Vendor.baseCuryID>))]
		public string VendorBaseCuryID { get; set; }
		#endregion
	}
}