using PX.Data;
using PX.Objects.CS;
using PX.SM;

namespace PX.Objects.PR
{
	public class PRxAccessWebUsers : PXGraphExtension<Access>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		protected virtual void _(Events.RowPersisted<UsersInRoles> e)
		{
			if (e.TranStatus == PXTranStatus.Completed)
			{
				MatchWithPayGroupHelper.ClearUserPayGroupIDsSlot();
			}
		}
	}
}
