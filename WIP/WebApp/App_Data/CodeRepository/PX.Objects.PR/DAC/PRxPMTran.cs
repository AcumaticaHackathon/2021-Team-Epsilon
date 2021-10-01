using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PM;

namespace PX.Objects.PR
{
	public sealed class PRxPMTran : PXCacheExtension<PMTran>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		#region Keys
		public static class FK
		{
			public class PayrollWorkLocation : PRLocation.PK.ForeignKeyOf<PMTran>.By<payrollWorkLocationID> { }
		}
		#endregion

		#region PayrollWorkLocationID
		public abstract class payrollWorkLocationID : PX.Data.BQL.BqlInt.Field<payrollWorkLocationID> { }
		[PXDBInt]
		public int? PayrollWorkLocationID { get; set; }
		#endregion
	}
}
