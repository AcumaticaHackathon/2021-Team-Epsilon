using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Commerce.Core;
using PX.Commerce.Objects;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Commerce.BigCommerce.API.REST;
using PX.Objects.SO;
using PX.Commerce.Core.API;
using PX.Objects.CA;

namespace PX.Commerce.BigCommerce
{
	public class OrderValidator : BCBaseValidator, ISettingsValidator, ILocalValidator
	{
		public int Priority { get { return 0; } }

		public virtual void Validate(IProcessor iproc)
		{
			Validate<BCSalesOrderProcessor>(iproc, (processor) =>
			{
				BCEntity entity = processor.GetEntity();
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();

				//Branch
				if (store.BranchID == null)
					throw new PXException(BigCommerceMessages.NoBranch);

				ARSetup arSetup = PXSelect<ARSetup>.Select(processor);
				if (arSetup?.MigrationMode == true)
					throw new PXException(BCMessages.MigrationModeOnSO);

				//Numberings
				SOOrderType type = PXSelect<SOOrderType, Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>>>.Select(processor, storeExt.OrderType);
				//OrderType
				if (type == null || type.Active != true)
					throw new PXException(BigCommerceMessages.NoSalesOrderType);
				//Order Numberings
				BCAutoNumberAttribute.CheckAutoNumbering(processor, type.OrderNumberingID);
			});
			Validate<BCPaymentProcessor>(iproc, (processor) =>
			{
				BCEntity entity = processor.GetEntity();
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();

				//Branch
				if (store.BranchID == null)
					throw new PXException(BigCommerceMessages.NoBranch);

				ARSetup type = PXSelect<ARSetup>.Select(processor);
				if(type?.MigrationMode == true)
					throw new PXException(BCMessages.MigrationModeOn);

				//Numberings
				BCAutoNumberAttribute.CheckAutoNumbering(processor, type.PaymentNumberingID);
			});
		}

		public void Validate(IProcessor iproc, ILocalEntity ilocal)
		{
			Validate<BCSalesOrderProcessor, SalesOrder>(iproc, ilocal, (processor, entity) =>
			{
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();
				if (storeExt.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount && entity.DiscountDetails?.Count > 0 && PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() == false)
					throw new PXException(BCMessages.DocumentDiscountSOMsg);
			});
			Validate<BCRefundsProcessor, SalesOrder>(iproc, ilocal, (processor, entity) =>
			{
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();
				if (storeExt.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount && (entity.RefundOrders != null && entity.RefundOrders.Any(x => x.DiscountDetails?.Count > 0)) && PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() == false)
					throw new PXException(BCMessages.DocumentDiscountSOMsg);
			});
		}
	}
}
