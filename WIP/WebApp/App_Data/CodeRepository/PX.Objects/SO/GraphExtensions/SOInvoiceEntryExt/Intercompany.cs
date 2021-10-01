using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN.GraphExtensions.SOOrderEntryExt;
using PX.Objects.PO;

namespace PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt
{
	public class Intercompany : PXGraphExtension<SOInvoiceEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.interBranch>()
			&& PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		protected virtual void _(Events.RowSelected<ARTran> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			ARTran row = eventArgs.Row;

			if (DisableIntercompanyOrderAmountsModification(row.SOOrderType, row.SOOrderNbr))
			{
				eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
							.For<ARTran.curyUnitPrice>(a => a.Enabled = false)
							.SameFor<ARTran.manualPrice>()
							.SameFor<ARTran.curyExtPrice>()
							.SameFor<ARTran.discPct>()
							.SameFor<ARTran.curyDiscAmt>()
							.SameFor<ARTran.manualDisc>()
							.SameFor<ARTran.discountID>();
			}
		}

		protected virtual void _(Events.RowDeleting<ARInvoiceDiscountDetail> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			ARInvoiceDiscountDetail row = eventArgs.Row;
			
			if (eventArgs.ExternalCall == true && Base.Document.Current != null && 
				Base.Document.Cache.GetStatus(Base.Document.Current).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted) &&
				DisableIntercompanyOrderAmountsModification(row.OrderType, row.OrderNbr))
			{
				throw new PXSetPropertyException(Messages.DiscountDetailLineCannotBeDeleted, errorLevel: PXErrorLevel.RowError);
			}
		}

		protected virtual bool DisableIntercompanyOrderAmountsModification(string orderType, string orderNbr)
		{
			if (Base.customer.Current?.IsBranch == true && orderNbr != null &&
					Base.sosetup.Current?.DisableEditingPricesDiscountsForIntercompany == true)
			{
				SOOrder soOrder = PXSelect<SOOrder, Where<SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
									And<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>.Select(Base, orderType, orderNbr);

				if (soOrder != null && !string.IsNullOrEmpty(soOrder.IntercompanyPONbr) && soOrder.Behavior.IsIn(SOBehavior.SO, SOBehavior.IN))
				{
					return true;
				}
			}

			return false;
		}
	}
}
