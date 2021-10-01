using PX.Api.ContractBased.Models;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Commerce.Shopify.API.REST;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.Shopify
{
	public class SPHelper : CommerceHelper
	{
		#region Inventory
		public virtual String GetInventoryCDByExternID(String productID, String variantID, String sku, String description, bool? isGiftCard, out string uom)
		{
			if (isGiftCard == true)
			{
				Int32? giftCertItem = _processor.GetBindingExt<BCBindingExt>().GiftCertificateItemID;
				PX.Objects.IN.InventoryItem inventory = giftCertItem != null ? PX.Objects.IN.InventoryItem.PK.Find(this, giftCertItem) : null;
				if (inventory?.InventoryCD == null)
					throw new PXException(ShopifyMessages.NoGiftCertificateItem);

				uom = inventory.BaseUnit?.Trim();
				return inventory.InventoryCD.Trim();
			}

			String priorityUOM = null;
			PX.Objects.IN.InventoryItem item = null;
			if (!string.IsNullOrWhiteSpace(sku))
			{
				item = SelectFrom<PX.Objects.IN.InventoryItem>.
					LeftJoin<BCSyncDetail>.On<PX.Objects.IN.InventoryItem.noteID.IsEqual<BCSyncDetail.localID>>.
					InnerJoin<BCSyncStatus>.On<BCSyncDetail.syncID.IsEqual<BCSyncStatus.syncID>>.
					Where<BCSyncStatus.connectorType.IsEqual<BCEntity.connectorType.FromCurrent>.
					And<BCSyncStatus.bindingID.IsEqual<BCEntity.bindingID.FromCurrent>.
					And<Brackets<BCSyncStatus.entityType.IsEqual<@P.AsString>.Or<BCSyncStatus.entityType.IsEqual<@P.AsString>.Or<BCSyncStatus.entityType.IsEqual<@P.AsString>>>>.
					And<BCSyncDetail.externID.IsEqual<@P.AsString>.
					And<BCSyncStatus.externID.IsEqual<@P.AsString>.
					And<InventoryItem.inventoryCD.IsEqual<@P.AsString>>>>>>>.View.
					Select(this, BCEntitiesAttribute.StockItem, BCEntitiesAttribute.NonStockItem, BCEntitiesAttribute.ProductWithVariant, variantID, productID, sku);
			}
			if (item == null) //Serch by SKU
			{
				item = PXSelect<PX.Objects.IN.InventoryItem,
								Where<InventoryItem.inventoryCD, Equal<Required<InventoryItem.inventoryCD>>>>
								.Select(this, string.IsNullOrEmpty(sku) ? description : sku);
			}
			if (item == null && (sku != null || description != null)) //Search by cross references
			{
				PX.Objects.IN.InventoryItem itemCandidate = null;
				PX.Objects.IN.INItemXRef crossrefCandidate = null;
				foreach (PXResult<PX.Objects.IN.INItemXRef, PX.Objects.IN.InventoryItem> result in PXSelectJoin<PX.Objects.IN.INItemXRef,
					InnerJoin<PX.Objects.IN.InventoryItem, On<PX.Objects.IN.INItemXRef.inventoryID, Equal<PX.Objects.IN.InventoryItem.inventoryID>>>,
					Where<PX.Objects.IN.INItemXRef.alternateType, Equal<INAlternateType.global>,
						And<PX.Objects.IN.INItemXRef.alternateID, Equal<Required<PX.Objects.IN.INItemXRef.alternateID>>>>>.Select(this, string.IsNullOrEmpty(sku) ? description : sku))
				{
					if (itemCandidate != null && itemCandidate.InventoryID != result.GetItem<PX.Objects.IN.InventoryItem>().InventoryID)
						throw new PXException(BCMessages.InventoryMultipleAlternates, string.IsNullOrEmpty(sku) ? description : sku, new Object[] { productID ?? description, variantID }.KeyCombine());

					itemCandidate = result.GetItem<PX.Objects.IN.InventoryItem>();
					crossrefCandidate = result.GetItem<PX.Objects.IN.INItemXRef>();
				}
				item = itemCandidate;
				priorityUOM = crossrefCandidate?.UOM;
			}

			if (item == null)
				throw new PXException(BCMessages.InvenotryNotFound, string.IsNullOrEmpty(sku) ? description : sku, new Object[] { productID ?? description, variantID }.KeyCombine());
			if (item.ItemStatus == PX.Objects.IN.INItemStatus.Inactive)
				throw new PXException(BCMessages.InvenotryInactive, item.InventoryCD);

			uom = priorityUOM ?? item?.BaseUnit?.Trim();
			return item?.InventoryCD?.Trim();
		}
		#endregion

		#region Payment 
		public virtual BCPaymentMethods GetPaymentMethodMapping(string gateway, string currency, out string cashAccount, bool throwError = true)
		{
			cashAccount = null;
			BCPaymentMethods result = null;
			if (!PaymentMethods.Any(x => x.StorePaymentMethod.Equals(gateway.ToUpper(), StringComparison.OrdinalIgnoreCase)))
			{
				PXCache cache = base.Caches[typeof(BCPaymentMethods)];
				BCPaymentMethods newMapping = new BCPaymentMethods()
				{
					BindingID = _processor.Operation.Binding,
					StorePaymentMethod = gateway.ToUpper(),
					Active = true,
				};
				newMapping = (BCPaymentMethods)cache.Insert(newMapping);
				cache.Persist(newMapping, PXDBOperation.Insert);

				throw new PXException(BCMessages.OrderPaymentMethodIsMissing, gateway?.ToUpper(), null, currency);
			}
			else if (!PaymentMethods.Any(x => x.StorePaymentMethod.Equals(gateway.ToUpper(), StringComparison.OrdinalIgnoreCase) && x.Active == true))
			{
				if (throwError)// note this parameter is passed as false for refunds
					throw new PXException(BCMessages.OrderPaymentMethodIsMissing, gateway?.ToUpper(), null, currency);
				return null;
			}
			else
			{
				result = PaymentMethods.FirstOrDefault(x => x.StorePaymentMethod.Equals(gateway.ToUpper(), StringComparison.OrdinalIgnoreCase) && x.Active == true);

				CashAccount ca = null;
				BCMultiCurrencyPaymentMethod bCMultiCurrency = null;
				Company baseCurrency = PXSelect<Company, Where<Company.companyCD, Equal<Required<Company.companyCD>>>>.Select(this, this.Accessinfo.CompanyName);
				var multiCurrency = PXAccess.FeatureInstalled<PX.Objects.CS.FeaturesSet.multicurrency>();
				if (baseCurrency?.BaseCuryID?.Trim() != currency && multiCurrency)
				{
					bCMultiCurrency = PXSelect<BCMultiCurrencyPaymentMethod, Where<BCMultiCurrencyPaymentMethod.paymentMappingID, Equal<Required<BCPaymentMethods.storePaymentMethod>>,
						And<BCMultiCurrencyPaymentMethod.curyID, Equal<Required<BCMultiCurrencyPaymentMethod.curyID>>,
						And<BCMultiCurrencyPaymentMethod.bindingID, Equal<Required<BCMultiCurrencyPaymentMethod.bindingID>>>>>>.Select(this, result?.PaymentMappingID, currency, result?.BindingID);
					ca = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(this, bCMultiCurrency?.CashAccountID);
				}
				else
					ca = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(this, result?.CashAccountID);

				cashAccount = ca?.CashAccountCD;

				if (cashAccount == null || result?.PaymentMethodID == null)
				{
					throw new PXException(BCMessages.OrderPaymentMethodIsMissing, gateway?.ToUpper(), null, currency);
				}
				return result;
			}
		}

		public virtual string ParseTransactionNumber(OrderTransaction tran, out bool isCreditCardTran)
		{
			String paymentRef = tran?.Authorization;
			isCreditCardTran = tran?.Authorization != null;
			if (tran == null) return paymentRef;

			if (!String.IsNullOrWhiteSpace(paymentRef) && paymentRef.IndexOf("#") >= 0)
				paymentRef = paymentRef.Substring(0, paymentRef.IndexOf("#"));
			if (String.Equals(tran.Gateway, ShopifyConstants.Bogus, StringComparison.InvariantCultureIgnoreCase))// only for bogus gateway as transaction id is always same
			{
				paymentRef = $"{tran.Id}#{tran.Authorization ?? string.Empty}";
				isCreditCardTran = false;
			}
			if (String.Equals(tran.Gateway, ShopifyConstants.ShopifyPayments, StringComparison.InvariantCultureIgnoreCase))//Shopify Paymenents use shopify ID as primary transaction ID
			{
				paymentRef = tran.Id.ToString();
				isCreditCardTran = true;
			}

			if (String.IsNullOrEmpty(paymentRef))
				paymentRef = tran.Id.ToString();

			return paymentRef;
		}

		public virtual void AddCreditCardProcessingInfo(BCPaymentMethods methodMapping, Payment payment, TransactionType transactionType)
		{
			payment.IsNewCard = true.ValueField();
			payment.SaveCard = false.ValueField();
			payment.ProcessingCenterID = methodMapping?.ProcessingCenterID.ValueField();
			CreditCardTransactionDetail detail = new CreditCardTransactionDetail();
			detail.TranNbr = payment.PaymentRef;
			detail.TranDate = payment.ApplicationDate;
			detail.TranType = GetTransactionType(transactionType);

			payment.CreditCardTransactionInfo = new List<CreditCardTransactionDetail>(new[] { detail });
		}

		public virtual StringValue GetTransactionType(TransactionType transactionType)
		{
			switch (transactionType)
			{
				case TransactionType.Authorization:
					return CCTranTypeCode.Authorize.ValueField();
				case TransactionType.Capture:
					return CCTranTypeCode.PriorAuthorizedCapture.ValueField();
				case TransactionType.Sale:
					return CCTranTypeCode.AuthorizeAndCapture.ValueField();
				case TransactionType.Refund:
					return CCTranTypeCode.Credit.ValueField();
				default:
					return CCTranTypeCode.Unknown.ValueField();

			}
		}
		public virtual TransactionType PopulateAction(List<OrderTransaction> transactions, OrderTransaction data)
		{
			var lastKind = transactions.LastOrDefault(x => x.ParentId == data.Id && x.Status == TransactionStatus.Success
							 && (x.Kind == TransactionType.Authorization || x.Kind == TransactionType.Capture || x.Kind == TransactionType.Sale))?.Kind ?? data.Kind;
			data.Action = lastKind;
			var voidTrans = transactions.FirstOrDefault(x => x.ParentId == data.Id && x.Status == TransactionStatus.Success && x.Kind == TransactionType.Void);
			if (voidTrans != null && lastKind == TransactionType.Authorization)
			{
				data.Action = voidTrans.Kind;
			}

			return lastKind;
		}

		#endregion
	}
}
