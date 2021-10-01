using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Commerce.Core;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.CS;
using PX.SM;

namespace PX.Commerce.Objects
{
	public class BCARPaymentEntryExt : PXGraphExtension<ARPaymentEntry>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }


		//Sync Time 
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PX.Commerce.Core.BCSyncExactTime()]
		public void ARPayment_LastModifiedDateTime_CacheAttached(PXCache sender) { }


		public PXAction<ARPayment> registerBCAuthTran;
		[PXUIField(DisplayName = "Register BC Auth Tran", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Visible = false)]
		[PXProcessButton]
		public virtual IEnumerable RegisterBCAuthTran(PXAdapter adapter)
		{
			return adapter.Get();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRemoveBaseAttribute(typeof(PXRestrictorAttribute))]
		public void SOAdjust_AdjdOrderNbr_CacheAttached(PXCache sender) { }

		protected virtual void SOAdjust_AdjdOrderNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e, PXFieldVerifying baseHandler)
		{
			
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			if (context != null && e.NewValue != null)
			{
				SOOrder order = PXSelect<SOOrder, Where<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>.Select(Base, e.NewValue.ToString());
				if (order != null && order.Status == SOOrderStatus.Cancelled)
					return;
			}

			baseHandler?.Invoke(sender, e);
		}
		protected virtual void SOOrder_Cancelled_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e, PXFieldVerifying baseHandler)
		{
			SOOrder order = (SOOrder)e.Row;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			if (context != null && order != null)
			{
				return;
			}
			baseHandler.Invoke(sender, e);
		}

	}
}
