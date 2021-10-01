using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.IN.GraphExtensions.SOOrderEntryExt;
using PX.Objects.IN;
using PX.Objects.PO;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	public class Intercompany : PXGraphExtension<SOOrderEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.interBranch>()
			&& PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		protected virtual void _(Events.RowSelected<SOOrder> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			bool generatedFromPO =
				!string.IsNullOrEmpty(eventArgs.Row.IntercompanyPONbr)
				|| !string.IsNullOrEmpty(eventArgs.Row.IntercompanyPOReturnNbr);
			eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
				.For<SOOrder.intercompanyPOType>(a =>
				{
					a.Visible = eventArgs.Row.IsIntercompany == true && eventArgs.Row.Behavior.IsIn(SOBehavior.SO, SOBehavior.IN);
					a.Enabled = false;
				})
				.SameFor<SOOrder.intercompanyPONbr>()
				.For<SOOrder.intercompanyPOReturnNbr>(a =>
				{
					a.Visible = eventArgs.Row.IsIntercompany == true && eventArgs.Row.Behavior.IsIn(SOBehavior.RM, SOBehavior.CM);
					a.Enabled = false;
				})
				.For<SOOrder.shipSeparately>(a => a.Enabled = !generatedFromPO);

			if (generatedFromPO)
			{
				if (Base.sosetup.Current?.DisableAddingItemsForIntercompany == true)
				{
					Base.addInvBySite.SetEnabled(false);
					Base.addInvoice.SetEnabled(false);
					if (Base.Actions.Contains(nameof(MatrixEntryExt.showMatrixPanel)))
					{
						Base.Actions[nameof(MatrixEntryExt.showMatrixPanel)].SetEnabled(false);
					}
					Base.Transactions.Cache.AllowInsert = false;
					Base.Transactions.Cache.AllowDelete = false;
				}

				if (Base.sosetup.Current?.DisableEditingPricesDiscountsForIntercompany == true
					&& eventArgs.Row.Behavior.IsIn(SOBehavior.SO, SOBehavior.IN))
				{
					Base.recalculateDiscountsAction.SetEnabled(false);

					eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
						.For<SOOrder.curyDiscTot>(a => a.Enabled = false)
						.SameFor<SOOrder.disableAutomaticDiscountCalculation>();

					Base.DiscountDetails.Cache.AllowInsert = false;
					Base.DiscountDetails.Cache.AllowDelete = false;

					Base.DiscountDetails.Cache.Adjust<PXUIFieldAttribute>()
						.For<SOOrderDiscountDetail.curyDiscountAmt>(a => a.Enabled = false)
						.SameFor<SOOrderDiscountDetail.discountPct>();
				}

				eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
				.For<SOOrder.customerID>(a =>
				{
					a.Enabled = false;
				});
			}

			if (!string.IsNullOrEmpty(eventArgs.Row.IntercompanyPONbr))
			{
				// TODO: CrossCompanySales: make more stable mechanism of warnings and fields enablement
				POOrder intercompanyPO;
				using (new PXReadBranchRestrictedScope())
				{
					intercompanyPO = POOrder.PK.Find(Base,
						eventArgs.Row.IntercompanyPOType, eventArgs.Row.IntercompanyPONbr);
				}

				if (intercompanyPO != null)
				{
					if (intercompanyPO.Cancelled == true)
					{
						eventArgs.Cache.RaiseExceptionHandling<SOOrder.intercompanyPONbr>(
							eventArgs.Row, eventArgs.Row.IntercompanyPONbr,
							new PXSetPropertyException(Messages.IntercompanyPOCancelled, PXErrorLevel.Warning));
						return;
					}
					else if (eventArgs.Row.IntercompanyPOWithEmptyInventory == true)
					{
						eventArgs.Cache.RaiseExceptionHandling<SOOrder.intercompanyPONbr>(
							eventArgs.Row, eventArgs.Row.IntercompanyPONbr,
							new PXSetPropertyException(PO.Messages.IntercompanySOEmptyInventory, PXErrorLevel.Warning));
					}

					if (eventArgs.Row.CuryTaxTotal != intercompanyPO.CuryTaxTotal)
					{
						string warning = PXUIFieldAttribute.GetWarning<SOOrder.curyTaxTotal>(eventArgs.Cache, eventArgs.Row);
						if (string.IsNullOrEmpty(warning))
						{
							eventArgs.Cache.RaiseExceptionHandling<SOOrder.curyTaxTotal>(
								eventArgs.Row, eventArgs.Row.CuryTaxTotal,
								new PXSetPropertyException(Messages.IntercompanyTaxTotalDiffers, PXErrorLevel.Warning));
						}
					}
					if (eventArgs.Row.CuryOrderTotal != intercompanyPO.CuryOrderTotal)
					{
						string warning = PXUIFieldAttribute.GetWarning<SOOrder.curyOrderTotal>(eventArgs.Cache, eventArgs.Row);
						if (string.IsNullOrEmpty(warning))
						{
							eventArgs.Cache.RaiseExceptionHandling<SOOrder.curyOrderTotal>(
								eventArgs.Row, eventArgs.Row.CuryOrderTotal,
								new PXSetPropertyException(Messages.IntercompanyOrderTotalDiffers, PXErrorLevel.Warning));
						}
					}
				}
			}
		}

		protected virtual void _(Events.RowSelected<SOLine> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			if ((!string.IsNullOrEmpty(Base.Document.Current?.IntercompanyPONbr)
				|| !string.IsNullOrEmpty(Base.Document.Current?.IntercompanyPOReturnNbr))
				&& eventArgs.Row.IntercompanyPOLineNbr != null)
			{
				eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
					.For<SOLine.inventoryID>(a => a.Enabled = false)
					.SameFor<SOLine.isFree>()
					.SameFor<SOLine.orderQty>()
					.SameFor<SOLine.uOM>()
					.SameFor<SOLine.pOCreate>();

				if (Base.sosetup.Current?.DisableEditingPricesDiscountsForIntercompany == true
					&& eventArgs.Row.Behavior.IsIn(SOBehavior.SO, SOBehavior.IN))
				{
					eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
						.For<SOLine.curyUnitPrice>(a => a.Enabled = false)
						.SameFor<SOLine.manualPrice>()
						.SameFor<SOLine.curyExtPrice>()
						.SameFor<SOLine.discPct>()
						.SameFor<SOLine.curyDiscAmt>()
						.SameFor<SOLine.manualDisc>()
						.SameFor<SOLine.discountID>();
				}
			}
		}

		protected virtual void _(Events.RowSelected<SOLineSplit> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			if ((!string.IsNullOrEmpty(Base.Document.Current?.IntercompanyPONbr)
				|| !string.IsNullOrEmpty(Base.Document.Current?.IntercompanyPOReturnNbr))
				&& Base.Transactions.Current?.IntercompanyPOLineNbr != null)
			{
				eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
					.For<SOLineSplit.qty>(a => a.Enabled = false);
			}
		}

		protected virtual void SOLine_SalesAcctID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e,
			PXFieldDefaulting baseFunc)
		{
			SOLine line = (SOLine)e.Row;

			if (line != null && Base.IsTransferOrder == false && Base.customer.Current?.IsBranch == true)
			{
				InventoryItem item = InventoryItem.PK.Find(Base, line.InventoryID);
				if (item == null)
					return;

				switch (Base.soordertype.Current.IntercompanySalesAcctDefault)
				{
					case SOIntercompanyAcctDefault.MaskItem:
						e.NewValue = Base.GetValue<InventoryItem.salesAcctID>(item);
						e.Cancel = true;
						break;
					case SOIntercompanyAcctDefault.MaskLocation:
						CR.Location customerloc = Base.location.Current;
						e.NewValue = Base.GetValue<CR.Location.cSalesAcctID>(customerloc);
						e.Cancel = true;
						break;
				}
			}
			else
			{
				baseFunc(sender, e);
			}
		}
	}
}
