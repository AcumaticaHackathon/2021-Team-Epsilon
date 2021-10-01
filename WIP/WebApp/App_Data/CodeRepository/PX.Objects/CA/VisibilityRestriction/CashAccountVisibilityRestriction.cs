using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.CS;
using PX.Data.BQL;
using PX.Objects.CR;

namespace PX.Objects.CA
{
	public sealed class CashFlowEnqVisibilityRestriction : PXCacheExtension<CashFlowEnq.CashFlowFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.branch>();
		}

		#region AccountID
		[PXRestrictor(
			typeof(Where2<Where<CashAccount.restrictVisibilityWithBranch, Equal<True>, 
				And<CashAccount.branchID, InsideBranchesOf<Current<CashFlowEnq.CashFlowFilter.orgBAccountID>>>>, 
				Or<Where<CashAccount.restrictVisibilityWithBranch, Equal<False>, 
					And<CashAccount.baseCuryID, Equal<Current<CashFlowEnq.CashFlowFilter.organizationBaseCuryID>>>>>
				>),
			"",
			SuppressVerify = false
		)]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public int? CashAccountID
		{
			get;
			set;
		}
		#endregion
		#region DefaultAccountID
		[PXRestrictor(
			typeof(Where2<Where<CashAccount.restrictVisibilityWithBranch, Equal<True>,
				And<CashAccount.branchID, InsideBranchesOf<Current<CashFlowEnq.CashFlowFilter.orgBAccountID>>>>,
				Or<Where<CashAccount.restrictVisibilityWithBranch, Equal<False>,
					And<CashAccount.baseCuryID, Equal<Current<CashFlowEnq.CashFlowFilter.organizationBaseCuryID>>>>>
				>),
			"",
			SuppressVerify = false
		)]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public int? DefaultAccountID
		{
			get;
			set;
		}
		#endregion
	}
}
