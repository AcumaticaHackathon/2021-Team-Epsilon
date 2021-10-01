using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce
{
	public class BCHelper : CommerceHelper
	{
		#region Inventory
		public virtual string GetInventoryCDByExternID(string productID, string variantID, string sku, OrdersProductsType type, out string uom)
		{
			if (type == OrdersProductsType.GiftCertificate)
			{
				BCBindingExt bindingExt = _processor.GetBindingExt<BCBindingExt>();
				PX.Objects.IN.InventoryItem inventory = bindingExt?.GiftCertificateItemID != null ? PX.Objects.IN.InventoryItem.PK.Find(this, bindingExt?.GiftCertificateItemID) : null;
				if (inventory?.InventoryCD == null)
					throw new PXException(BigCommerceMessages.NoGiftCertificateItem);

				uom = inventory.BaseUnit?.Trim();
				return inventory.InventoryCD.Trim();
			}

			String key = variantID != null ? new Object[] { productID, variantID }.KeyCombine() : productID;
			String priorityUOM = null;
			PX.Objects.IN.InventoryItem item = null;
			if (variantID != null)
			{
				item = PXSelectJoin<PX.Objects.IN.InventoryItem,
						InnerJoin<BCSyncDetail, On<PX.Objects.IN.InventoryItem.noteID, Equal<BCSyncDetail.localID>>,
						InnerJoin<BCSyncStatus, On<BCSyncStatus.syncID, Equal<BCSyncDetail.syncID>>>>,
							Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
								And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
								And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
								And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>,
								And<InventoryItem.inventoryCD, Equal<Required<InventoryItem.inventoryCD>>>>>>>>
						.Select(this, BCEntitiesAttribute.ProductWithVariant, productID, sku);
			}
			else
			{
				item = PXSelectJoin<PX.Objects.IN.InventoryItem,
					   LeftJoin<BCSyncStatus, On<PX.Objects.IN.InventoryItem.noteID, Equal<BCSyncStatus.localID>>>,
					   Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						   And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						   And2<Where<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
							   Or<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>>>,
						   And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>
					   .Select(this, BCEntitiesAttribute.StockItem, BCEntitiesAttribute.NonStockItem, key);
			}
			if (item == null) //Serch by SKU
			{
				item = PXSelect<PX.Objects.IN.InventoryItem,
							Where<InventoryItem.inventoryCD, Equal<Required<InventoryItem.inventoryCD>>>>
							.Select(this, sku);
			}
			if (item == null && sku != null) //Search by cross references
			{
				PX.Objects.IN.InventoryItem itemCandidate = null;
				PX.Objects.IN.INItemXRef crossrefCandidate = null;
				foreach (PXResult<PX.Objects.IN.INItemXRef, PX.Objects.IN.InventoryItem> result in PXSelectJoin<PX.Objects.IN.INItemXRef,
					InnerJoin<PX.Objects.IN.InventoryItem, On<PX.Objects.IN.INItemXRef.inventoryID, Equal<PX.Objects.IN.InventoryItem.inventoryID>>>,
					Where< PX.Objects.IN.INItemXRef.alternateType, Equal<INAlternateType.global>,
						And<PX.Objects.IN.INItemXRef.alternateID, Equal<Required<PX.Objects.IN.INItemXRef.alternateID>>>>>.Select(this, sku))
				{
					if (itemCandidate != null && itemCandidate.InventoryID != result.GetItem<PX.Objects.IN.InventoryItem>().InventoryID) 
						throw new PXException(BCMessages.InventoryMultipleAlternates, sku, key);

					itemCandidate = result.GetItem<PX.Objects.IN.InventoryItem>();
					crossrefCandidate = result.GetItem<PX.Objects.IN.INItemXRef>();
				}
				item = itemCandidate;
				priorityUOM = crossrefCandidate?.UOM;
			}

			if (item == null)
				throw new PXException(BCMessages.InvenotryNotFound, sku, key);
			if (item.ItemStatus == PX.Objects.IN.INItemStatus.Inactive)
				throw new PXException(BCMessages.InvenotryInactive, item.InventoryCD);

			uom = priorityUOM ?? item?.BaseUnit?.Trim();
			return item?.InventoryCD?.Trim();
		}
		#endregion

		#region Payment 
		public virtual BCPaymentMethods GetPaymentMethodMapping(string transactionMethod, string orderMethod, string currencyCode, out string cashAccount, bool throwError = true)
		{
			cashAccount = null;

			BCPaymentMethods result = null;
			//if order method(example in case of braintree payment method) is passed than check if found matching record, else just check with just payment method
			var results = PaymentMethods?.Where(x => x.StorePaymentMethod.Equals(transactionMethod, StringComparison.InvariantCultureIgnoreCase) &&
			   (!string.IsNullOrEmpty(orderMethod) && orderMethod.Equals(x.StoreOrderPaymentMethod, StringComparison.InvariantCultureIgnoreCase)));
			if (results != null && results.Any())
			{
				result = results.FirstOrDefault(x => x.Active == true);
			}
			else if (PaymentMethods != null && PaymentMethods.Any(x => x.StorePaymentMethod.Equals(transactionMethod, StringComparison.InvariantCultureIgnoreCase)))
			{
				result = PaymentMethods?.FirstOrDefault(x => x.StorePaymentMethod.Equals(transactionMethod, StringComparison.InvariantCultureIgnoreCase) && x.Active == true);
			}
			else
			{
				// if not found create entry and throw exception
				PXCache cache = base.Caches[typeof(BCPaymentMethods)];
				BCPaymentMethods entry = new BCPaymentMethods()
				{
					StorePaymentMethod = transactionMethod.ToUpper(),
					BindingID = _processor.Operation.Binding,
					Active = true
				};
				cache.Insert(entry);
				cache.Persist(PXDBOperation.Insert);

				throw new PXException(BCMessages.OrderPaymentMethodIsMissing, transactionMethod, orderMethod?.ToUpper(), currencyCode);
			}

			if (result != null)
			{
				CashAccount ca = null;
				Company baseCurrency = PXSelect<Company, Where<Company.companyCD, Equal<Required<Company.companyCD>>>>.Select(this, this.Accessinfo.CompanyName);
				var multiCurrency = PXAccess.FeatureInstalled<PX.Objects.CS.FeaturesSet.multicurrency>();
				if (baseCurrency?.BaseCuryID?.Trim() != currencyCode && multiCurrency)
				{
					BCMultiCurrencyPaymentMethod currency = PXSelect<BCMultiCurrencyPaymentMethod,
						Where<BCMultiCurrencyPaymentMethod.paymentMappingID, Equal<Required<BCPaymentMethods.paymentMappingID>>,
							And<BCMultiCurrencyPaymentMethod.curyID, Equal<Required<BCMultiCurrencyPaymentMethod.curyID>>,
							And<BCMultiCurrencyPaymentMethod.bindingID, Equal<Required<BCMultiCurrencyPaymentMethod.bindingID>>>>>>.Select(this, result.PaymentMappingID, currencyCode, result.BindingID);
					ca = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(this, currency?.CashAccountID);
				}
				else
					ca = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(this, result.CashAccountID);

				cashAccount = ca?.CashAccountCD;

				if (cashAccount == null || result?.PaymentMethodID == null)
				{
					throw new PXException(BCMessages.OrderPaymentMethodIsMissing, transactionMethod, orderMethod?.ToUpper(), currencyCode);
				}
			}
			else if (throwError)
			{
				// in case if payment is filetered and forced synced but paymentmethod mapping is not active or not mapped
				//Note in case of refunds passed as false we donot throw error 

				throw new PXException(BCMessages.OrderPaymentMethodIsMissing, transactionMethod, orderMethod?.ToUpper(), currencyCode);
			}

			return result;
		}

		public virtual string ParseTransactionNumber(OrdersTransactionData tran, out bool isCreditCardTran)
		{
			String paymentRef = tran?.GatewayTransactionId;
			isCreditCardTran = tran?.GatewayTransactionId != null;
			if (tran == null) return paymentRef;

			if (!String.IsNullOrWhiteSpace(paymentRef) && paymentRef.IndexOf("#") >= 0)
				paymentRef = paymentRef.Substring(0, paymentRef.IndexOf("#"));

			if (String.IsNullOrEmpty(paymentRef))
				paymentRef = tran.Id.ToString();

			return paymentRef;
		}
	
		public virtual string GetPaymentMethodName(OrdersTransactionData data)
		{
			if (data.PaymentMethod == BCConstants.Emulated)
				return data.Gateway?.ToUpper();
			return string.Format("{0} ({1})", data.Gateway, data.PaymentMethod ?? string.Empty)?.ToUpper();

		}
		public virtual bool CreatePaymentfromOrder(string method)
		{
			var paymentMethod = PaymentMethods.FirstOrDefault(x => String.Equals(x.StorePaymentMethod, method, StringComparison.InvariantCultureIgnoreCase) && x.CreatePaymentFromOrder == true && x.Active == true);
			return (paymentMethod != null);
		}

		public virtual void AddCreditCardProcessingInfo(BCPaymentMethods methodMapping, Payment payment, OrderPaymentEvent orderPaymentEvent,string paymentInstrumentToken)
		{
			payment.IsNewCard = true.ValueField();
			payment.SaveCard = (!String.IsNullOrWhiteSpace(paymentInstrumentToken)).ValueField();
			payment.ProcessingCenterID = methodMapping?.ProcessingCenterID?.ValueField();

			CreditCardTransactionDetail detail = new CreditCardTransactionDetail();
			detail.TranNbr = payment.PaymentRef;
			detail.TranDate = payment.ApplicationDate;
			detail.ExtProfileId = paymentInstrumentToken.ValueField();
			detail.TranType = GetTransactionType(orderPaymentEvent);

			payment.CreditCardTransactionInfo = new List<CreditCardTransactionDetail>(new[] { detail });
		}
		public virtual StringValue GetTransactionType(OrderPaymentEvent orderPaymentEvent)
		{
			switch (orderPaymentEvent)
			{
				case OrderPaymentEvent.Authorization:
					return CCTranTypeCode.Authorize.ValueField();
				case OrderPaymentEvent.Capture:
					return CCTranTypeCode.PriorAuthorizedCapture.ValueField();
				case OrderPaymentEvent.Purchase:
					return CCTranTypeCode.AuthorizeAndCapture.ValueField();
				case OrderPaymentEvent.Refund:
					return CCTranTypeCode.Credit.ValueField();
				default:
					return CCTranTypeCode.Unknown.ValueField();
			}
		}

		public virtual OrderPaymentEvent PopulateAction(IList<OrdersTransactionData> transactions, OrdersTransactionData data)
		{
			var lastEvent = transactions.LastOrDefault(x => x.Gateway == data.Gateway && x.Status != OrderPaymentStatus.Error && data.Event != x.Event
										  && (x.Event == OrderPaymentEvent.Authorization || x.Event == OrderPaymentEvent.Capture || x.Event == OrderPaymentEvent.Purchase))?.Event ?? data.Event;

			data.Action = lastEvent;
			//void Payment if payement was authorized only and voided in external system
			var voidTrans = transactions.FirstOrDefault(x => x.Gateway == data.Gateway && x.Status == OrderPaymentStatus.Ok && x.Event == OrderPaymentEvent.Void);
			if (voidTrans != null && lastEvent == OrderPaymentEvent.Authorization)
			{
				data.Action = voidTrans.Event;
			}

			return lastEvent;
		}

		#endregion
	}
}
