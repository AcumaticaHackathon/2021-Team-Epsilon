using PX.Data;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;

namespace PX.Objects.PR
{
	public sealed class PRxGLTran : PXCacheExtension<GLTran>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		#region Keys
		public static class FK
		{
			public class EarningType : EPEarningType.PK.ForeignKeyOf<GLTran>.By<earningTypeCD> { }
			public class PayrollWorkLocation : PRLocation.PK.ForeignKeyOf<GLTran>.By<payrollWorkLocationID> { }
		}
		#endregion

		#region EarningTypeCD
		public abstract class earningTypeCD : PX.Data.BQL.BqlString.Field<earningTypeCD> { }
		[PXDBString(EPEarningType.typeCD.Length, IsUnicode = true)]
		public string EarningTypeCD { get; set; }
		#endregion
		#region PayrollWorkLocationID
		public abstract class payrollWorkLocationID : PX.Data.BQL.BqlInt.Field<payrollWorkLocationID> { }
		[PXDBInt]
		public int? PayrollWorkLocationID { get; set; }
		#endregion
	}
}
