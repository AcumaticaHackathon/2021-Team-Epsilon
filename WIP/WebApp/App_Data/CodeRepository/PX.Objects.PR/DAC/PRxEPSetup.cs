using PX.Data;
using PX.Objects.CS;
using PX.Objects.EP;
using System;

namespace PX.Objects.PR
{
	public sealed class PRxEPSetup : PXCacheExtension<EPSetup>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		#region PostingOption
		public abstract class postingOption : PX.Data.BQL.BqlString.Field<postingOption> { }
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Posting Option for Non-Payroll Employees")]
		public string PostingOption { get; set; }
		#endregion
	}
}