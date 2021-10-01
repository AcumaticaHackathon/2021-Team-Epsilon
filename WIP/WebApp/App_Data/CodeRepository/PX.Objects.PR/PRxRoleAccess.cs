using PX.Data;
using PX.Objects.CS;
using PX.SM;

namespace PX.Objects.PR
{
	public class PRxRoleAccess : PXGraphExtension<RoleAccess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		protected virtual void _(Events.RowPersisted<PX.SM.UsersInRoles> e, PXRowPersisted baseHandler)
		{
			baseHandler?.Invoke(e.Cache, e.Args);

			if (e.TranStatus == PXTranStatus.Completed)
			{
				MatchWithPayGroupHelper.ClearUserPayGroupIDsSlot();
			}
		}
	}
}
