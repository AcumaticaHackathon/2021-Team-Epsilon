using PX.Commerce.Shopify.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.Common;
using PX.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PX.Api.ContractBased.Models;
using PX.Objects.IN;

namespace PX.Commerce.Shopify
{
	public class SPSalesOrderBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary { get => Order; }
		public IMappedEntity[] Entities => new IMappedEntity[] { Order };

		public override IMappedEntity[] PreProcessors { get => new IMappedEntity[] { Customer }; }
		public override IMappedEntity[] PostProcessors { get => Enumerable.Empty<IMappedEntity>().Concat(Payments).Concat(Shipments).ToArray(); }

		public MappedOrder Order;
		public MappedCustomer Customer;
		public MappedLocation Location;
		public List<MappedPayment> Payments = new List<MappedPayment>();
		public List<MappedShipment> Shipments = new List<MappedShipment>();
	}

	public class SPSalesOrderRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			#region Orders
			return base.Restrict<MappedOrder>(mapped, delegate (MappedOrder obj)
			{
				BCBindingExt bindingExt = processor.GetBindingExt<BCBindingExt>();
				Boolean syncCompleted = false;
				if (!syncCompleted && obj.Local != null && obj.Local.Status != null)
				{
					if (((obj.Local.Status.Value == PX.Objects.SO.Messages.Hold && obj.IsNew)
						|| obj.Local.Status.Value == PX.Objects.SO.Messages.Cancelled))
					{
						return new FilterResult(FilterStatus.Filtered,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogOrderSkippedStatusNotSupported, obj.Local.OrderNbr?.Value ?? obj.Local.SyncID.ToString(), obj.Local.Status.Value));
					}
				}
				var orderTypesArray = bindingExt.OtherSalesOrderTypes?.Split(',').Where(i => i != bindingExt.OrderType).ToArray();
				BCBindingShopify bindingShopify = processor.GetBindingExt<BCBindingShopify>();
				if (obj.Local?.OrderType != null && obj.Local?.OrderType?.Value != bindingShopify.POSDirectOrderType && obj.Local?.OrderType?.Value != bindingShopify.POSShippingOrderType &&
					obj.Local?.OrderType?.Value != bindingExt.OrderType && orderTypesArray?.Contains(obj.Local?.OrderType?.Value) == false)
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogOrderSkippedTypeNotSupported, obj.Local.OrderNbr?.Value ?? obj.Local.SyncID.ToString(), obj.Local?.OrderType?.Value));
				}

				//skip order that has only gift certificate in order line
				var guestID = PX.Objects.AR.Customer.PK.Find((PXGraph)processor, bindingExt.GuestCustomerID)?.AcctCD;
				if (!string.IsNullOrEmpty(obj.Local?.CustomerID?.Value.Trim()) && guestID.Trim().Equals(obj.Local?.CustomerID?.Value.Trim()))
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogOrderSkippedGuestOrder, obj.Local.OrderNbr?.Value ?? obj.Local.SyncID.ToString()));
				}

				if (!string.IsNullOrEmpty(obj.Local?.ExternalRef?.Value?.Trim()))
				{
					return new FilterResult(FilterStatus.Filtered,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogOrderSkippedWithExternalRef, obj.Local.OrderNbr?.Value ?? obj.Local.SyncID.ToString()));
				}

				return null;
			});
			#endregion
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			#region Orders
			return base.Restrict<MappedOrder>(mapped, delegate (MappedOrder obj)
			{
				Boolean syncCompleted = false;
				BCBindingShopify bindingShopify = processor.GetBindingExt<BCBindingShopify>();
				if (!syncCompleted && obj.Extern != null)
				{
					if (obj.IsNew && string.Equals(obj.Extern.SourceName, ShopifyConstants.POSSource, StringComparison.OrdinalIgnoreCase) && (PXAccess.FeatureInstalled<FeaturesSet.shopifyPOS>() != true || bindingShopify.ShopifyPOS != true))
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(ShopifyMessages.POSOrderNotSupported, $"{obj.Extern.Name}({obj.Extern.Id})"));
					}
				}

				if (!syncCompleted && obj.Extern != null)
				{
					if (obj.IsNew && obj.Extern.CancelledAt != null)
					{
						return new FilterResult(FilterStatus.Filtered,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogOrderSkippedExtStatusNotSupported, $"{obj.Extern.Name}({obj.Extern.Id})", "Cancelled"));
					}
				}

				if (!syncCompleted && obj.Extern != null)
				{
					if (obj.IsNew && obj.Extern.ClosedAt != null && !string.Equals(obj.Extern.SourceName, ShopifyConstants.POSSource, StringComparison.OrdinalIgnoreCase))
					{
						return new FilterResult(FilterStatus.Filtered,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogOrderSkippedExtStatusNotSupported, $"{obj.Extern.Name}({obj.Extern.Id})", "Closed/Archived"));
					}
				}


				return null;
			});
			#endregion
		}
	}

	[BCProcessor(typeof(SPConnector), BCEntitiesAttribute.Order, BCCaptions.Order,
		IsInternal = false,
		Direction = SyncDirection.Import,
		PrimaryDirection = SyncDirection.Import,
		PrimarySystem = PrimarySystem.Extern,
		PrimaryGraph = typeof(PX.Objects.SO.SOOrderEntry),
		ExternTypes = new Type[] { typeof(OrderData) },
		LocalTypes = new Type[] { typeof(SalesOrder) },
		DetailTypes = new String[] { BCEntitiesAttribute.OrderLine, BCCaptions.OrderLine, BCEntitiesAttribute.OrderAddress, BCCaptions.OrderAddress },
		AcumaticaPrimaryType = typeof(PX.Objects.SO.SOOrder),
		AcumaticaPrimarySelect = typeof(Search2<PX.Objects.SO.SOOrder.orderNbr,
			InnerJoin<BCBindingExt, On<BCBindingExt.orderType, Equal<SOOrder.orderType>>,
			InnerJoin<BCBinding, On<BCBindingExt.bindingID, Equal<BCBinding.bindingID>>>>,
			Where<BCBinding.connectorType, Equal<Current<BCSyncStatusEdit.connectorType>>,
				And<BCBinding.bindingID, Equal<Current<BCSyncStatusEdit.bindingID>>>>>),
		URL = "orders/{0}",
		Requires = new string[] { BCEntitiesAttribute.Customer },
		RequiresOneOf = new string[] { BCEntitiesAttribute.StockItem + "." + BCEntitiesAttribute.NonStockItem + "." + BCEntitiesAttribute.ProductWithVariant }
	)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = true,
		WebHookType = typeof(WebHookMessage),
		WebHooks = new String[]
		{
			"orders/create",
			"orders/cancelled",
			"orders/paid",
			"orders/updated"
		})]
	[BCProcessorExternCustomField(BCConstants.MetaFields, ShopifyCaptions.Metafields, nameof(OrderData.Metafields), typeof(OrderData))]
	public class SPSalesOrderProcessor : SPOrderBaseProcessor<SPSalesOrderProcessor, SPSalesOrderBucket, MappedOrder>
	{
		protected SPPaymentProcessor paymentProcessor = PXGraph.CreateInstance<SPPaymentProcessor>();

		protected OrderRestDataProvider orderDataProvider;
		protected StoreRestDataProvider storeDataProvider;
		protected List<ShippingZoneData> storeShippingZones;
		protected CustomerAddressRestDataProvider customerAddressRestDataProvider;
		protected BCBindingShopify currentShopifySettings;
		protected List<long> skipOrderItems;
		public PXSelect<BCShippingMappings, Where<BCShippingMappings.bindingID, Equal<Required<BCShippingMappings.bindingID>>,
			And<BCShippingMappings.shippingZone, Equal<Required<BCShippingMappings.shippingZone>>, And<BCShippingMappings.shippingMethod, Equal<Required<BCShippingMappings.shippingMethod>>>>>> bcShippingMappings;
		public PXSelect<State, Where<State.name, Equal<Required<State.name>>, Or<State.stateID, Equal<Required<State.stateID>>>>> states;

		#region Initialization
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			currentShopifySettings = GetBindingExt<BCBindingShopify>();

			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());

			orderDataProvider = new OrderRestDataProvider(client);
			storeDataProvider = new StoreRestDataProvider(client);
			customerAddressRestDataProvider = new CustomerAddressRestDataProvider(client);
			storeShippingZones = storeDataProvider.GetShippingZones().ToList();
			skipOrderItems = new List<long>();

			if (GetEntity(BCEntitiesAttribute.Payment)?.IsActive == true)
			{
				paymentProcessor.Initialise(iconnector, operation.Clone().With(_ => { _.EntityType = BCEntitiesAttribute.Payment; return _; }));
			}
		}
		#endregion

		#region Common
		public override List<Tuple<string, string>> GetExternCustomFieldList(BCEntity entity, EntityInfo entityInfo, ExternCustomFieldInfo customFieldInfo, PropertyInfo objectPropertyInfo = null)
		{
			List<Tuple<String, String>> fieldsList = new List<Tuple<String, String>>();
			string nameStr = "[Keyword]:[{0}]";
			if (objectPropertyInfo == null)
			{
				objectPropertyInfo = customFieldInfo.ExternEntityType.GetProperties().Where(x => x.Name == customFieldInfo.ObjectName).FirstOrDefault();
			}
			foreach (var prop in objectPropertyInfo.PropertyType.GetGenericArguments()[0].GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(System.ComponentModel.DescriptionAttribute))))
			{
				if ((prop.PropertyType == typeof(object) || Type.GetTypeCode(prop.PropertyType) != TypeCode.Object) ||
					(prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))))
				{
					fieldsList.Add(Tuple.Create(string.Format(nameStr, prop.Name), string.Format(nameStr, prop.Name)));
				}
			}
			return fieldsList;
		}
		public override string ValidateExternCustomField(BCEntity entity, EntityInfo entityInfo, ExternCustomFieldInfo customFieldInfo, string sourceObject, string sourceField, string targetObject, string targetField, EntityOperationType direction)
		{
			if (direction == EntityOperationType.ImportMapping)
			{
				//Validate the field format
				List<Tuple<String, String>> fields = GetExternCustomFieldList(entity, entityInfo, customFieldInfo);
				var fieldStrGroup = sourceField.Split(':');
				if (fieldStrGroup.Length == 2)
				{
					var keyFieldName = fieldStrGroup[0].Replace("[", "").Replace("]", "");
					var lastFieldName = fieldStrGroup[1].ToLower();
					var availableFields = fields.Select(x => x.Item1.Split(':')[1].ToLower()).ToList();
					if (!string.IsNullOrWhiteSpace(keyFieldName) && availableFields.Any(x => string.Equals(x, lastFieldName) || x.Substring(1, x.Length - 2).Equals(lastFieldName)))
						return null;
				}
				return string.Format(BCMessages.InvalidFilter, "Source", "[Keyword]:[Metafield Name]");
			}
			if (direction == EntityOperationType.ExportMapping)
			{
				return string.Format(BCMessages.NotSupportedMapping, "Target");
			}
			return null;
		}
		public override object GetExternCustomFieldValue(SPSalesOrderBucket entity, ExternCustomFieldInfo customFieldInfo, object sourceData, string sourceObject, string sourceField, out string displayName)
		{
			var sourceinfo = sourceField.Split(':');
			var metafields = GetPropertyValue(sourceData, sourceObject.Replace(" -> ", "."), out displayName);
			if (metafields != null && (metafields.GetType().IsGenericType && (metafields.GetType().GetGenericTypeDefinition() == typeof(List<>) || metafields.GetType().GetGenericTypeDefinition() == typeof(IList<>))))
			{
				var metafieldsList = (System.Collections.IList)metafields;
				if (sourceinfo.Length == 2)
				{
					var keyField = sourceinfo[0].Replace("[", "").Replace("]", "")?.Trim();
					var returnField = sourceinfo[1].Replace("[", "").Replace("]", "")?.Trim()?.ToLower();
					var findMatchedItem = false;
					foreach (object metaItem in metafieldsList)
					{
						object matchedMetaItem = null;
						object matchedMetaField = null;
						if (string.IsNullOrWhiteSpace(keyField))
							matchedMetaItem = metaItem;
						else
						{
							var metaKey = GetPropertyValue(metaItem, "Key", out displayName);
							if (metaKey != null && metaKey.ToString().Trim().Equals(keyField, StringComparison.InvariantCultureIgnoreCase))
							{
								matchedMetaItem = metaItem;
								findMatchedItem = true;
							}
						}
						if (matchedMetaItem != null)
						{
							matchedMetaField = GetPropertyValue(metaItem, returnField, out _);
							if (findMatchedItem == true)
								return matchedMetaField;
						}
					}

				}
			}
			return null;
		}
		public override void SetExternCustomFieldValue(SPSalesOrderBucket entity, ExternCustomFieldInfo customFieldInfo, object targetData, string targetObject, string targetField, string sourceObject, object value)
		{

		}
		public override MappedOrder PullEntity(Guid? localID, Dictionary<String, Object> fields)
		{
			SalesOrder impl = cbapi.GetByID<SalesOrder>(localID);
			if (impl == null) return null;

			MappedOrder obj = new MappedOrder(impl, impl.SyncID, impl.SyncTime);

			return obj;
		}
		public override MappedOrder PullEntity(string externID, string jsonObject)
		{
			OrderData data = orderDataProvider.GetByID(externID);
			if (data == null) return null;

			MappedOrder obj = new MappedOrder(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));

			return obj;
		}

		public override IEnumerable<MappedOrder> PullSimilar(IExternEntity entity, out string uniqueField)
		{
			uniqueField = ((OrderData)entity)?.Id?.ToString();
			if (string.IsNullOrEmpty(uniqueField))
				return null;
			uniqueField = APIHelper.ReferenceMake(uniqueField, currentBinding.BindingName);

			List<MappedOrder> result = new List<MappedOrder>();
			List<string> orderTypes = new List<string>() { GetBindingExt<BCBindingExt>()?.OrderType };
			if (string.Equals(((OrderData)entity)?.SourceName, ShopifyConstants.POSSource, StringComparison.OrdinalIgnoreCase))
			{
				//Support POS order type searching
				if (!string.IsNullOrEmpty(currentShopifySettings.POSDirectOrderType) && !orderTypes.Contains(currentShopifySettings.POSDirectOrderType))
					orderTypes.Add(currentShopifySettings.POSDirectOrderType);
				if (!string.IsNullOrEmpty(currentShopifySettings.POSShippingOrderType) && !orderTypes.Contains(currentShopifySettings.POSShippingOrderType))
					orderTypes.Add(currentShopifySettings.POSShippingOrderType);
			}
			helper.TryGetCustomOrderTypeMappings(ref orderTypes);

			foreach (SOOrder item in helper.OrderByTypesAndCustomerRefNbr.Select(orderTypes.ToArray(), uniqueField))
			{
				SalesOrder data = new SalesOrder() { SyncID = item.NoteID, SyncTime = item.LastModifiedDateTime, ExternalRef = item.CustomerRefNbr?.ValueField() };
				result.Add(new MappedOrder(data, data.SyncID, data.SyncTime));
			}
			return result;
		}
		public override IEnumerable<MappedOrder> PullSimilar(ILocalEntity entity, out string uniqueField)
		{
			uniqueField = ((SalesOrder)entity)?.ExternalRef?.Value;
			if (string.IsNullOrEmpty(uniqueField))
				return null;

			uniqueField = APIHelper.ReferenceParse(uniqueField, currentBinding.BindingName);

			IEnumerable<OrderData> similarOrders = orderDataProvider.GetAll(new FilterOrders() { IDs = uniqueField });
			if (similarOrders == null) return null;

			OrderData data = similarOrders.First();
			List<MappedOrder> result = new List<MappedOrder>();
			result.Add(new MappedOrder(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false)));
			return result;
		}

		public override Boolean ControlModification(IMappedEntity mapped, BCSyncStatus status, string operation)
		{
			if (mapped is MappedOrder)
			{
				MappedOrder order = mapped as MappedOrder;
				if (operation == BCSyncOperationAttribute.ExternChanged && !order.IsNew && order?.Extern != null && status?.PendingSync == false)
				{
					//We should prevent order from sync if it is updated by shipment
					if (order.Extern.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled || order.Extern.FulfillmentStatus == OrderFulfillmentStatus.Partial)
					{
						DateTime? orderdate = order.Extern.DateModifiedAt.ToDate(false);
						DateTime? shipmentDate = order.Extern.Fulfillments?.Max(x => x.DateModifiedAt)?.ToDate(false);

						if (orderdate != null && shipmentDate != null && Math.Abs((orderdate - shipmentDate).Value.TotalSeconds) < 5) //Modification withing 5 sec
							return false;
					}
				}
			}

			return base.ControlModification(mapped, status, operation);
		}

		public override bool ShouldFilter(SyncDirection direction, BCSyncStatus status)
		{
			return status != null && (status.ExternID == null || status.LocalID == null);
		}

		public override void ControlDirection(SPSalesOrderBucket bucket, BCSyncStatus status, ref bool shouldImport, ref bool shouldExport, ref bool skipSync, ref bool skipForce)
		{
			MappedOrder order = bucket.Order;

			if ((shouldImport || Operation.SyncMethod == SyncMode.Force) && !order.IsNew && order?.ExternID != null && order?.LocalID != null && (order.Local?.Status?.Value == PX.Objects.SO.Messages.Completed || order.Local?.Status?.Value == PX.Objects.SO.Messages.Cancelled))
			{
				if ((status.Status == BCSyncStatusAttribute.Synchronized || status.Status == BCSyncStatusAttribute.Pending || status.Status == BCSyncStatusAttribute.Failed) &&
						order?.Extern?.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)
				{
					skipForce = true;
					skipSync = true;
					status.LastOperation = BCSyncOperationAttribute.ManuallySynced;
					status.LastErrorMessage = null;
					UpdateStatus(order, status.LastOperation, status.LastErrorMessage);
					shouldImport = false;// if order is canceled or completed in ERP and Fullfilled in External System then skip import and mark order as synchronized
				}
				else if (order?.Extern?.FinancialStatus == OrderFinancialStatus.Refunded || order?.Extern?.FinancialStatus == OrderFinancialStatus.PartiallyRefunded)
				{
					var orderTotal = order.Extern.OrderTotal;
					var itemTotals = order.Extern?.LineItems?.Sum(x => x.Quantity) ?? 0;
					var refundStatus = SelectStatusChildren(order.SyncID).FirstOrDefault(x => x.EntityType == BCEntitiesAttribute.OrderRefunds);
					if (refundStatus != null)// to check if there were any refunds processed before order was completed
					{
						var details = SelectStatusDetails(refundStatus.SyncID);
						var refundsBeforeOrderCompleted = details?.Where(x => x.EntityType == BCEntitiesAttribute.Payment)?.Select(x => x.ExternID.KeySplit(0))?.ToList();
						if (refundsBeforeOrderCompleted != null)
						{
							var refunds = order.Extern.Refunds.Where(x => refundsBeforeOrderCompleted.Contains(x.Id.ToString()));
							orderTotal -= refunds.Sum(x => x.Transactions.Sum(s => s.Amount) ?? 0);
							itemTotals -= refunds.Sum(x => x.RefundLineItems?.Sum(s => s.Quantity) ?? 0);
						}
					}
					//We should prevent order from sync if it is updated by refunds
					if (orderTotal == order.Local.OrderTotal?.Value && itemTotals == (order.Local?.Details?.Where(x => x.InventoryID?.Value != refundItem?.InventoryCD?.Trim())?.Sum(x => x.OrderQty?.Value ?? 0) ?? 0))
					{
						DateTime? orderdate = order.Extern.DateModifiedAt.ToDate(false);
						DateTime? refundDate = order.Extern.Refunds.Max(x => x.DateCreatedAt).ToDate(false);

						if (orderdate != null && refundDate != null && Math.Abs((orderdate - refundDate).Value.TotalSeconds) == 0)
						{
							shouldImport = false;
							UpdateStatus(order, status.LastOperation, status.LastErrorMessage);
							skipForce = true;
							skipSync = true;//if order is in pending state because of refunds and if it is cancelled/completed than skip sync and let the syncstatus be same as before

						}
					}
				}
			}
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			BCBindingExt currentBindingExt = GetBindingExt<BCBindingExt>();
			var minDate = minDateTime == null || (minDateTime != null && currentBindingExt.SyncOrdersFrom != null && minDateTime < currentBindingExt.SyncOrdersFrom) ? currentBindingExt.SyncOrdersFrom : minDateTime;
			FilterOrders filter = new FilterOrders { Status = OrderStatus.Any, Fields = "id,name,source_name,financial_status,updated_at,created_at,cancelled_at,closed_at", Order = "updated_at asc" };
			if (minDateTime != null) filter.UpdatedAtMin = minDateTime.Value.ToLocalTime().AddSeconds(-GetBindingExt<BCBindingShopify>().ApiDelaySeconds ?? 0);
			if (maxDateTime != null) filter.UpdatedAtMax = maxDateTime.Value.ToLocalTime();

			IEnumerable<OrderData> datas = orderDataProvider.GetAll(filter);

			int countNum = 0;
			List<IMappedEntity> mappedList = new List<IMappedEntity>();
			try
			{
				foreach (OrderData data in datas)
				{
					IMappedEntity obj = new MappedOrder(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));

					mappedList.Add(obj);
					countNum++;
					if (countNum % BatchFetchCount == 0)
					{
						ProcessMappedListForImport(ref mappedList, true);

					}
				}
			}
			finally
			{
				if (mappedList.Any())
				{
					ProcessMappedListForImport(ref mappedList, true);
				}
			}
		}

		public override EntityStatus GetBucketForImport(SPSalesOrderBucket bucket, BCSyncStatus syncstatus)
		{
			bool? isPaymentActive = GetEntity(BCEntitiesAttribute.Payment)?.IsActive;
			OrderData data = orderDataProvider.GetByID(syncstatus.ExternID, true, isPaymentActive == true, true, GetBindingExt<BCBindingExt>()?.ImportOrderRisks ?? false);
			if (data == null) return EntityStatus.None;


			MappedOrder obj = bucket.Order = bucket.Order.Set(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));
			EntityStatus status = EnsureStatus(obj, SyncDirection.Import);

			if (status != EntityStatus.Pending && status != EntityStatus.Syncronized && Operation.SyncMethod != SyncMode.Force)
				return status;

			if (data.Customer != null && data.Customer.Id > 0 && (!string.IsNullOrEmpty(data.Customer.Email) || !string.IsNullOrEmpty(data.Customer.Phone)))
			{
				if (string.IsNullOrEmpty(data.Customer.FirstName) && string.IsNullOrEmpty(data.Customer.LastName))
				{
					LogWarning(Operation.LogScope(), BCMessages.CustomerNameIsEmpty, data.Customer.Id);
				}
				else
				{
					MappedCustomer customerObj = bucket.Customer = bucket.Customer.Set(data.Customer, data.Customer.Id?.ToString(), data.Customer.DateModifiedAt.ToDate(false));
					EntityStatus customerStatus = EnsureStatus(customerObj);

					if (data.ShippingAddress != null && GetEntity(BCEntitiesAttribute.Address)?.Direction != BCSyncDirectionAttribute.Export)
					{
						CustomerAddressData address = GetMatchingLocation(data);

						if (address != null)
						{
							bucket.Location = bucket.Location.Set(address, new Object[] { address.CustomerId, address.Id }.KeyCombine(), address.CalculateHash()).With(_ => { _.ParentID = customerObj.SyncID; return _; });
						}
						else
						{
							data.Customer.Addresses = customerAddressRestDataProvider.GetAll(data.Customer.Id.ToString()).ToList();// as  just recent 10 address are returned we need to make call to get all and retry
							address = GetMatchingLocation(data);
							if (address != null)
								bucket.Location = bucket.Location.Set(address, new Object[] { address.CustomerId, address.Id }.KeyCombine(), address.CalculateHash()).With(_ => { _.ParentID = customerObj.SyncID; return _; });
							else
								LogWarning(Operation.LogScope(syncstatus), BCMessages.LogOrderLocationUnidentified, $"{data.Name}|{data.Id}");
						}
					}
					BCExtensions.SetSharedSlot<CustomerData>(this.GetType(), data.Customer.Id?.ToString(), data.Customer);
				}
			}

			if (isPaymentActive == true && data.Transactions?.Count > 0)
			{
				foreach (OrderTransaction tranData in data.Transactions)
				{
					if ((tranData.Status == TransactionStatus.Success && tranData.Kind == TransactionType.Sale &&
						data.Transactions.Any(x => x.ParentId == tranData.Id && x.Status == TransactionStatus.Success && x.Kind == TransactionType.Refund && x.Amount == tranData.Amount &&
						x.ProcessedAt.HasValue && x.ProcessedAt.Value.Subtract(tranData.ProcessedAt.Value).TotalSeconds < 10)) ||
						(tranData.Status == TransactionStatus.Success && tranData.Kind == TransactionType.Refund && tranData.ParentId != null &&
						data.Transactions.Any(x => x.Id == tranData.ParentId && x.Status == TransactionStatus.Success && x.Kind == TransactionType.Sale && x.Amount == tranData.Amount &&
						tranData.ProcessedAt.HasValue && tranData.ProcessedAt.Value.Subtract(x.ProcessedAt.Value).TotalSeconds < 10)))
					{
						//Skip successful payment and its refund payment to avoid payment amount greater than order total amount issue if another payment is unsuccessful and system rolls back all payments 
						continue;
					}
					var lastKind = helper.PopulateAction(data.Transactions, tranData);

					MappedPayment paymentObj = new MappedPayment(tranData, new Object[] { data.Id, tranData.Id }.KeyCombine(), tranData.DateModifiedAt.ToDate(false), tranData.CalculateHash()).With(_ => { _.ParentID = obj.SyncID; return _; });
					EntityStatus paymentStatus = EnsureStatus(paymentObj, SyncDirection.Import);

					tranData.LastKind = lastKind;

					if (paymentStatus == EntityStatus.Pending)
					{
						bucket.Payments.Add(paymentObj);
					}
				}
			}
			BCExtensions.SetSharedSlot<OrderData>(this.GetType(), data.Id?.ToString(), data);
			return status;
		}

		protected virtual CustomerAddressData GetMatchingLocation(OrderData data)
		{
			//Find proper location by all fields.
			return data.Customer.Addresses?.FirstOrDefault(x => String.Equals(x.City ?? string.Empty, data.ShippingAddress.City ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.Company ?? string.Empty, data.ShippingAddress.Company ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.CountryCode ?? string.Empty, data.ShippingAddress.CountryCode ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.FirstName ?? string.Empty, data.ShippingAddress.FirstName ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.LastName ?? string.Empty, data.ShippingAddress.LastName ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.Phone ?? string.Empty, data.ShippingAddress.Phone ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.ProvinceCode ?? string.Empty, data.ShippingAddress.ProvinceCode ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.Address1 ?? string.Empty, data.ShippingAddress.Address1 ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.Address2 ?? string.Empty, data.ShippingAddress.Address2 ?? string.Empty, StringComparison.InvariantCultureIgnoreCase)
			&& String.Equals(x.PostalCode ?? string.Empty, data.ShippingAddress.PostalCode ?? string.Empty, StringComparison.InvariantCultureIgnoreCase));
		}

		public override void MapBucketImport(SPSalesOrderBucket bucket, IMappedEntity existing)
		{
			MappedOrder obj = bucket.Order;
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			OrderData data = obj.Extern;
			SalesOrder impl = obj.Local = new SalesOrder();
			SalesOrder presented = existing?.Local as SalesOrder;
			// we can update only open orders
			if (presented != null && presented.Status?.Value != PX.Objects.SO.Messages.Open && presented.Status?.Value != PX.Objects.SO.Messages.Hold
				 && presented.Status?.Value != BCObjectsMessages.RiskHold && presented.Status?.Value != PX.Objects.SO.Messages.CreditHold)
			{
				throw new PXException(BCMessages.OrderStatusDoesNotAllowModification, presented.OrderNbr?.Value);
			}

			bool cancelledOrder = obj.Extern?.CancelledAt != null ? true : false;
			var description = PXMessages.LocalizeFormat(ShopifyMessages.OrderDescription, currentBinding.BindingName, data.Name, data.FinancialStatus?.ToString());
			impl.Description = description.ValueField();
			if (presented != null && (cancelledOrder == true || data.FinancialStatus == OrderFinancialStatus.Refunded))
			{
				return;
			}
			impl.Custom = GetCustomFieldsForImport();

			#region SalesOrder
			//POS order
			if (string.Equals(data.SourceName, ShopifyConstants.POSSource, StringComparison.OrdinalIgnoreCase))
			{
				if (PXAccess.FeatureInstalled<FeaturesSet.shopifyPOS>() != true || currentShopifySettings.ShopifyPOS != true)
					throw new PXException(ShopifyMessages.POSOrderNotSupported, $"{obj.Extern.Name}({obj.Extern.Id})");
				if (data.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)
					impl.OrderType = currentShopifySettings.POSDirectOrderType.ValueField();
				else if (data.FulfillmentStatus == null || data.FulfillmentStatus == OrderFulfillmentStatus.Null)
					impl.OrderType = currentShopifySettings.POSShippingOrderType.ValueField();
			}
			else
				impl.OrderType = bindingExt?.OrderType.ValueField();
			impl.ExternalOrderOrigin = currentBinding.BindingName.ValueField();
			impl.ExternalOrderSource = data.SourceName.ValueField();
			var date = data.DateCreatedAt.ToDate(false, PXTimeZoneInfo.FindSystemTimeZoneById(bindingExt?.OrderTimeZone));
			if (date.HasValue)
				impl.Date = (new DateTime(date.Value.Date.Ticks)).ValueField();
			impl.RequestedOn = impl.Date;

			impl.CurrencyID = data.Currency.ValueField();
			//impl.CurrencyRate = data.CurrencyExchangeRate.ValueField();
			impl.CustomerOrder = data.Name.ValueField();
			impl.ExternalRef = APIHelper.ReferenceMake(data.Id, currentBinding.BindingName).ValueField();
			impl.Note = data.Note;

			impl.ExternalOrderOriginal = true.ValueField();

			PXResult<PX.Objects.CR.Address, PX.Objects.CR.Contact> billingResult = null;
			PXResult<PX.Objects.CR.Location, PX.Objects.CR.Address, PX.Objects.CR.Contact, BCSyncStatus> shippingResult = null;
			PX.Objects.CR.Address address = null;
			PX.Objects.CR.Contact contact = null;
			PX.Objects.CR.Location location = null;
			PX.Objects.AR.Customer customer = null;
			//Customer ID
			if (bucket.Customer != null && data.Customer.Id > 0 && (!string.IsNullOrEmpty(data.Customer.Email) || !string.IsNullOrEmpty(data.Customer.Phone)) &&
				(!string.IsNullOrEmpty(data.Customer.FirstName) || !string.IsNullOrEmpty(data.Customer.LastName)))
			{
				var result = PXSelectJoin<PX.Objects.AR.Customer,
					LeftJoin<PX.Objects.CR.Address, On<PX.Objects.AR.Customer.defBillAddressID, Equal<PX.Objects.CR.Address.addressID>>,
					LeftJoin<PX.Objects.CR.Contact, On<PX.Objects.AR.Customer.defBillContactID, Equal<PX.Objects.CR.Contact.contactID>>,
					LeftJoin<BCSyncStatus, On<PX.Objects.AR.Customer.noteID, Equal<BCSyncStatus.localID>>>>>,
					Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>.Select(this, BCEntitiesAttribute.Customer, data.Customer.Id).Cast<PXResult<PX.Objects.AR.Customer, PX.Objects.CR.Address, PX.Objects.CR.Contact, BCSyncStatus>>().FirstOrDefault();
				customer = result?.GetItem<PX.Objects.AR.Customer>();
				address = result?.GetItem<PX.Objects.CR.Address>();
				if (customer == null) throw new PXException(BCMessages.CustomerNotSyncronized, data.Customer.Id);
				if (customer.CuryID != impl.CurrencyID.Value && !customer.AllowOverrideCury.Value) throw new PXException(BCMessages.OrderCurrencyNotMathced, impl.CurrencyID.Value, customer.CuryID);
				impl.CustomerID = customer.AcctCD?.Trim().ValueField();
				billingResult = new PXResult<PX.Objects.CR.Address, PX.Objects.CR.Contact>(result?.GetItem<PX.Objects.CR.Address>(), result?.GetItem<PX.Objects.CR.Contact>());
			}
			else
			{
				PXResult<PX.Objects.AR.Customer, Contact2, PX.Objects.CR.Contact, PX.Objects.CR.Address> result = null;

				if (!string.IsNullOrEmpty(data.Email))
				{
					result = PXSelectJoin<PX.Objects.AR.Customer,
					   LeftJoin<Contact2, On<PX.Objects.AR.Customer.defContactID, Equal<Contact2.contactID>>,
					   LeftJoin<PX.Objects.CR.Contact, On<PX.Objects.AR.Customer.defBillContactID, Equal<PX.Objects.CR.Contact.contactID>>,
					   LeftJoin<PX.Objects.CR.Address, On<PX.Objects.AR.Customer.defBillAddressID, Equal<PX.Objects.CR.Address.addressID>>>>>,
					   Where<Contact2.eMail, Equal<Required<Contact2.eMail>>>>.Select(this, data.Email).Cast<PXResult<PX.Objects.AR.Customer, Contact2, PX.Objects.CR.Contact, PX.Objects.CR.Address>>().FirstOrDefault();
					customer = result?.GetItem<PX.Objects.AR.Customer>();
					address = result?.GetItem<PX.Objects.CR.Address>();
				}

				if (customer == null && !string.IsNullOrEmpty(data.Phone))
				{
					result = PXSelectJoin<PX.Objects.AR.Customer,
					   LeftJoin<Contact2, On<PX.Objects.AR.Customer.defContactID, Equal<Contact2.contactID>>,
					   LeftJoin<PX.Objects.CR.Contact, On<PX.Objects.AR.Customer.defBillContactID, Equal<PX.Objects.CR.Contact.contactID>>,
					   LeftJoin<PX.Objects.CR.Address, On<PX.Objects.AR.Customer.defBillAddressID, Equal<PX.Objects.CR.Address.addressID>>>>>,
					   Where<Contact2.phone1, Equal<Required<Contact2.phone1>>, Or<PX.Objects.CR.Contact.phone2, Equal<Required<PX.Objects.CR.Contact.phone2>>>>>.Select(this, data.Phone, data.Phone).Cast<PXResult<PX.Objects.AR.Customer, Contact2, PX.Objects.CR.Contact, PX.Objects.CR.Address>>().FirstOrDefault();
					customer = result?.GetItem<PX.Objects.AR.Customer>();
					address = result?.GetItem<PX.Objects.CR.Address>();
				}
				if (customer == null)
				{
					var guestCustomerResult = PXSelectJoin<PX.Objects.AR.Customer,
					   LeftJoin<PX.Objects.CR.Address, On<PX.Objects.AR.Customer.defBillAddressID, Equal<PX.Objects.CR.Address.addressID>>>,
					   Where<PX.Objects.AR.Customer.bAccountID, Equal<Required<PX.Objects.AR.Customer.bAccountID>>>>.Select(this, bindingExt.GuestCustomerID).Cast<PXResult<PX.Objects.AR.Customer, PX.Objects.CR.Address>>().FirstOrDefault();
					customer = guestCustomerResult?.GetItem<PX.Objects.AR.Customer>();
					address = guestCustomerResult?.GetItem<PX.Objects.CR.Address>();
					if (customer == null) throw new PXException(ShopifyMessages.NoGuestCustomer);
				}
				else
				{
					if (customer.CuryID != impl.CurrencyID.Value && !customer.AllowOverrideCury.Value) throw new PXException(BCMessages.OrderCurrencyNotMathced, impl.CurrencyID.Value, customer.CuryID);
					billingResult = new PXResult<PX.Objects.CR.Address, PX.Objects.CR.Contact>(result?.GetItem<PX.Objects.CR.Address>(), result?.GetItem<PX.Objects.CR.Contact>());

				}
				impl.CustomerID = customer.AcctCD?.Trim().ValueField();
			}
			//Location ID
			if (bucket.Location != null)
			{
				shippingResult = PXSelectJoin<PX.Objects.CR.Location,
					LeftJoin<PX.Objects.CR.Address, On<PX.Objects.CR.Location.defAddressID, Equal<PX.Objects.CR.Address.addressID>>,
					LeftJoin<PX.Objects.CR.Contact, On<PX.Objects.CR.Location.defContactID, Equal<PX.Objects.CR.Contact.contactID>>,
					LeftJoin<BCSyncStatus, On<PX.Objects.CR.Location.noteID, Equal<BCSyncStatus.localID>>>>>,
					Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>.Select(this, BCEntitiesAttribute.Address, bucket?.Location?.ExternID).Cast<PXResult<PX.Objects.CR.Location, PX.Objects.CR.Address, PX.Objects.CR.Contact, BCSyncStatus>>().FirstOrDefault();
				location = shippingResult?.GetItem<PX.Objects.CR.Location>();
				if (location != null)
				{
					impl.LocationID = location.LocationCD?.Trim().ValueField();
				}
			}
			impl.FinancialSettings = new FinancialSettings();
			var branch = Branch.PK.Find(this, currentBinding.BranchID)?.BranchCD?.Trim().ValueField();
			impl.FinancialSettings.Branch = branch;
			#endregion

			#region ShippingSettings
			//Freight
			impl.Totals = new Totals();
			impl.Totals.OverrideFreightAmount = new BooleanValue() { Value = true };
			impl.Totals.OrderWeight = data.TotalWeightInGrams.ValueField();

			List<OrderAdjustment> refundOrderAdjustments = null;
			List<RefundLineItem> refundItems = null;
			refundOrderAdjustments = data.Refunds?.Count > 0 ? data.Refunds.SelectMany(x => x.OrderAdjustments)?.ToList() : null;
			refundItems = data.Refunds?.Count > 0 ? data.Refunds.SelectMany(x => x.RefundLineItems)?.ToList() : null;

			if (data.Refunds?.Count > 0)
				impl.ExternalRefundRef = string.Join(";", data.Refunds.Select(x => x.Id)).ValueField();
			decimal shippingrefundAmt = refundOrderAdjustments?.Where(x => x.Kind == OrderAdjustmentType.ShippingRefund)?.Sum(x => (-x.Amount) ?? 0m) ?? 0m;
			decimal shippingrefundAmtTax = refundOrderAdjustments?.Where(x => x.Kind == OrderAdjustmentType.ShippingRefund)?.Sum(x => (-x.TaxAmount) ?? 0m) ?? 0m;
			//Included the shipping discount, if there is a free shipping discount applied, freight fee should be 0.
			//reduce the shipping refund amount from freight
			impl.Totals.Freight = (data.ShippingLines.Sum(x => x.ShippingCostExcludingTax) - data.ShippingLines.SelectMany(x => x.DiscountAllocations)?.Sum(x => x?.DiscountAmount ?? 0m) - shippingrefundAmt).ValueField();
			if (impl.Totals.Freight.Value < 0)
				throw new PXException(BCMessages.ValueCannotBeLessThenZero, data.ShippingLines.Sum(x => x.DiscountAllocations?.Count > 0 ? x.DiscountAllocations.Sum(d => d.DiscountAmount) ?? 0m : 0m), data.ShippingLines.Sum(x => x.ShippingCostExcludingTax));

			State state;
			//ShippingSettings
			impl.ShippingSettings = new ShippingSettings();
			PXCache cache = base.Caches[typeof(BCShippingMappings)];
			if (data.ShippingAddress != null)
			{
				var shippingLine = data.ShippingLines?.FirstOrDefault();
				bool hasMappingError = false;
				String zoneName = string.Empty;
				String shippingMethod = shippingLine?.Title ?? string.Empty;
				if (shippingLine != null)
				{
					storeShippingZones = storeShippingZones ?? storeDataProvider.GetShippingZones().ToList();
					BCShippingMappings mappingValue = null;

					//Compare ShippingZones data to find ShippingMapping record first, and then search wtih ShippingLine data again if no record is found.
					var shippingZone = storeShippingZones.FirstOrDefault(x => x.Countries?.Count > 0 && x.Countries.Any(c => string.Equals(c.Code, data.ShippingAddress.CountryCode, StringComparison.InvariantCultureIgnoreCase))) ??
											storeShippingZones.FirstOrDefault(x => x.Countries?.Count > 0 && x.Countries.Any(c => c.Code == "*"));
					zoneName = shippingZone != null ? shippingZone.Name : shippingLine.Code ?? string.Empty;
					mappingValue = bcShippingMappings.SelectSingle(Operation.Binding, zoneName, shippingMethod);
					if (mappingValue != null)
					{
						if (mappingValue.Active == true && mappingValue.CarrierID == null)
						{
							hasMappingError = true;
						}
						else if (mappingValue.Active == true && mappingValue.CarrierID != null)
						{
							impl.ShipVia = impl.ShippingSettings.ShipVia = mappingValue.CarrierID.ValueField();
							impl.ShippingSettings.ShippingZone = mappingValue.ZoneID.ValueField();
							impl.ShippingSettings.ShippingTerms = mappingValue.ShipTermsID.ValueField();
						}
					}
					else
					{
						hasMappingError = true;
						BCShippingMappings inserted = new BCShippingMappings() { BindingID = Operation.Binding, ShippingZone = zoneName, ShippingMethod = shippingMethod, Active = true };
						cache.Insert(inserted);
					}
				}

				if (cache.Inserted.Count() > 0)
					cache.Persist(PXDBOperation.Insert);
				if (hasMappingError)
				{
					throw new PXException(BCMessages.OrderShippingMappingIsMissing, zoneName, shippingMethod);
				}

				#region Ship-To Address && Contact

				impl.ShipToAddress = new Core.API.Address();
				impl.ShipToAddress.AddressLine1 = data.ShippingAddress.Address1.ValueField();
				impl.ShipToAddress.AddressLine2 = data.ShippingAddress.Address2.ValueField();
				impl.ShipToAddress.City = data.ShippingAddress.City.ValueField();
				impl.ShipToAddress.Country = data.ShippingAddress.CountryCode.ValueField();
				if (!string.IsNullOrEmpty(data.ShippingAddress.ProvinceCode))
				{
					state = states.Select(data.ShippingAddress.Province, data.ShippingAddress.ProvinceCode);
					if (state == null)
						impl.ShipToAddress.State = GetSubstituteLocalByExtern(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.State), data.ShippingAddress.Province, data.ShippingAddress.ProvinceCode).ValueField();
					else
						impl.ShipToAddress.State = state.StateID?.ValueField();
				}
				else
					impl.ShipToAddress.State = string.Empty.ValueField();
				impl.ShipToAddress.PostalCode = data.ShippingAddress.PostalCode?.ToUpperInvariant()?.ValueField();

				impl.ShipToContact = new DocContact();
				impl.ShipToContact.Phone1 = data.ShippingAddress.Phone.ValueField();
				impl.ShipToContact.Email = data.Email.ValueField();
				impl.ShipToContact.Attention = data.ShippingAddress.Name.ValueField();
				impl.ShipToContact.BusinessName = data.ShippingAddress.Company.ValueField();

				impl.ShipToAddressOverride = true.ValueField();
				impl.ShipToContactOverride = true.ValueField();
				if (customer.BAccountID != GetBindingExt<BCBindingExt>()?.GuestCustomerID)
				{
					address = shippingResult?.GetItem<PX.Objects.CR.Address>();
					if (address != null && CompareAddress(impl.ShipToAddress, address))
						impl.ShipToAddressOverride = false.ValueField();

					contact = shippingResult?.GetItem<PX.Objects.CR.Contact>();
					location = shippingResult?.GetItem<PX.Objects.CR.Location>();
					if (contact != null && CompareContact(impl.ShipToContact, contact, location))
						impl.ShipToContactOverride = false.ValueField();
				}
				#endregion

			}
			else
			{
				impl.ShipToAddress = new Core.API.Address();
				impl.ShipToAddress.AddressLine1 = string.Empty.ValueField();
				impl.ShipToAddress.AddressLine2 = string.Empty.ValueField();
				impl.ShipToAddress.City = string.Empty.ValueField();
				impl.ShipToAddress.State = string.Empty.ValueField();
				impl.ShipToAddress.PostalCode = string.Empty.ValueField();
				impl.ShipToContact = new DocContact();
				impl.ShipToContact.Phone1 = string.Empty.ValueField();
				impl.ShipToContact.Email = data.Email.ValueField();
				impl.ShipToContact.Attention = string.Empty.ValueField();
				impl.ShipToContact.BusinessName = string.Empty.ValueField();
				impl.ShipToAddressOverride = true.ValueField();
				impl.ShipToContactOverride = true.ValueField();
				impl.ShipToAddress.Country = (data.Customer?.DefaultAddress?.CountryCode ?? data.Customer?.Addresses?.FirstOrDefault()?.CountryCode ?? address?.CountryID)?.ValueField();
			}
			#endregion

			#region	Bill-To Address && Contact

			impl.BillToAddress = new Core.API.Address();
			impl.BillToContact = new DocContact();
			if (data.BillingAddress == null && data.ShippingAddress != null)
			{
				impl.BillToAddress.AddressLine1 = impl.ShipToAddress.AddressLine1;
				impl.BillToAddress.AddressLine2 = impl.ShipToAddress.AddressLine2;
				impl.BillToAddress.City = impl.ShipToAddress.City;
				impl.BillToAddress.Country = impl.ShipToAddress.Country;
				impl.BillToAddress.State = impl.ShipToAddress.State;
				impl.BillToAddress.PostalCode = impl.ShipToAddress.PostalCode;

				impl.BillToContact.Phone1 = impl.ShipToContact.Phone1;
				impl.BillToContact.Email = impl.ShipToContact.Email;
				impl.BillToContact.BusinessName = impl.ShipToContact.BusinessName;
				impl.BillToContact.Attention = impl.ShipToContact.Attention;
			}
			else if (data.BillingAddress != null)
			{
				impl.BillToAddress.AddressLine1 = data.BillingAddress.Address1.ValueField();
				impl.BillToAddress.AddressLine2 = data.BillingAddress.Address2.ValueField();
				impl.BillToAddress.City = data.BillingAddress.City.ValueField();
				impl.BillToAddress.Country = data.BillingAddress.CountryCode.ValueField();
				if (!string.IsNullOrEmpty(data.BillingAddress.ProvinceCode) && data.BillingAddress.ProvinceCode.Equals(data.ShippingAddress?.ProvinceCode))
				{
					impl.BillToAddress.State = impl.ShipToAddress.State;
				}
				else if (!string.IsNullOrEmpty(data.BillingAddress.ProvinceCode))
				{
					state = states.Select(data.BillingAddress.Province, data.BillingAddress.ProvinceCode);
					if (state == null)
					{
						impl.BillToAddress.State = GetSubstituteLocalByExtern(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.State), data.BillingAddress.Province, data.BillingAddress.ProvinceCode).ValueField();
					}
					else
						impl.BillToAddress.State = state.StateID?.ValueField();
				}
				else
					impl.BillToAddress.State = string.Empty.ValueField();
				impl.BillToAddress.PostalCode = data.BillingAddress.PostalCode?.ToUpperInvariant()?.ValueField();

				impl.BillToContact.Phone1 = data.BillingAddress.Phone.ValueField();
				impl.BillToContact.Email = data.Email.ValueField();
				impl.BillToContact.BusinessName = data.BillingAddress.Company.ValueField();
				impl.BillToContact.Attention = data.BillingAddress.Name.ValueField();
			}
			else if (data.BillingAddress == null && data.Customer?.DefaultAddress != null)
			{
				impl.BillToAddress.AddressLine1 = data.Customer?.DefaultAddress.Address1.ValueField();
				impl.BillToAddress.AddressLine2 = data.Customer?.DefaultAddress.Address2.ValueField();
				impl.BillToAddress.City = data.Customer?.DefaultAddress.City.ValueField();
				impl.BillToAddress.Country = data.Customer?.DefaultAddress.CountryCode.ValueField();
				if (!string.IsNullOrEmpty(data.Customer.DefaultAddress.ProvinceCode) && data.Customer.DefaultAddress.ProvinceCode.Equals(data.ShippingAddress?.ProvinceCode))
				{
					impl.BillToAddress.State = impl.ShipToAddress.State;
				}
				else if (!string.IsNullOrEmpty(data.Customer?.DefaultAddress.ProvinceCode))
				{
					state = states.Select(data.Customer?.DefaultAddress.Province, data.Customer?.DefaultAddress.ProvinceCode);
					if (state == null)
					{
						impl.BillToAddress.State = GetSubstituteLocalByExtern(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.State), data.Customer?.DefaultAddress.Province, data.Customer?.DefaultAddress.ProvinceCode).ValueField();
					}
					else
						impl.BillToAddress.State = state.StateID?.ValueField();
				}
				else
					impl.BillToAddress.State = string.Empty.ValueField();
				impl.BillToAddress.PostalCode = data.Customer?.DefaultAddress.PostalCode?.ToUpperInvariant()?.ValueField();

				impl.BillToContact.Phone1 = data.Customer?.DefaultAddress.Phone.ValueField();
				impl.BillToContact.Email = data.Email.ValueField();
				impl.BillToContact.BusinessName = data.Customer?.DefaultAddress.Company.ValueField();
				impl.BillToContact.Attention = data.Customer?.DefaultAddress.Name.ValueField();
			}
			impl.BillToContactOverride = true.ValueField();
			impl.BillToAddressOverride = true.ValueField();
			if (customer.BAccountID != GetBindingExt<BCBindingExt>()?.GuestCustomerID)
			{
				address = billingResult?.GetItem<PX.Objects.CR.Address>();
				if (address != null && CompareAddress(impl.BillToAddress, address))
					impl.BillToAddressOverride = false.ValueField();

				contact = billingResult?.GetItem<PX.Objects.CR.Contact>();
				if (contact != null && CompareContact(impl.BillToContact, contact))
					impl.BillToContactOverride = false.ValueField();
			}
			#endregion

			#region Products
			impl.Details = new List<SalesOrderDetail>();
			Decimal? totalDiscount = 0m;
			String orderLevelLocation = data.LocationId?.ToString();
			List<BCLocations> locationMappings = new List<BCLocations>();
			if (!string.IsNullOrWhiteSpace(orderLevelLocation))
			{
				foreach (PXResult<BCLocations, INSite, INLocation> result in PXSelectJoin<BCLocations,
					InnerJoin<INSite, On<INSite.siteID, Equal<BCLocations.siteID>>,
					InnerJoin<INLocation, On<INLocation.siteID, Equal<BCLocations.siteID>, And<BCLocations.locationID, IsNull, Or<BCLocations.locationID, Equal<INLocation.locationID>>>>>>,
					Where<BCLocations.bindingID, Equal<Required<BCLocations.bindingID>>, And<BCLocations.mappingDirection, Equal<BCMappingDirectionAttribute.import>>>>.Select(this, currentBinding.BindingID))
				{
					var bl = (BCLocations)result;
					var site = (INSite)result;
					var iNLocation = (INLocation)result;
					bl.SiteCD = site.SiteCD.Trim();
					bl.LocationCD = bl.LocationID == null ? null : iNLocation.LocationCD.Trim();
					locationMappings.Add(bl);
				}
			}
			foreach (var orderItem in data.LineItems)
			{
				decimal? quantity = orderItem.Quantity;
				decimal? subTotal = orderItem.Price * quantity;
				//Check refund data whether have this orderItem data
				List<RefundLineItem> matchedRefundItems = null;
				decimal? refundSubtotal = 0;
				decimal? refundQuantity = 0;
				SalesOrderDetail detail = new SalesOrderDetail();
				detail.DiscountAmount = 0m.ValueField();
				if (cancelledOrder == false && refundItems?.Count > 0 && refundItems.Any(x => x.LineItemId == orderItem.Id))
				{
					matchedRefundItems = refundItems.Where(x => x.LineItemId == orderItem.Id).ToList();
					refundQuantity = matchedRefundItems.Sum(x => x.Quantity);

					//If Admin modifies the item quantity and then changes back to original quantity, Shopify will keep the total quantity and use it to do the calculation in the item;
					//and add a new record to the refund item to keep the same amount. So we have to use this data to re-calculate the tax and discount if they applied.
					quantity = orderItem.Quantity - refundQuantity;
					subTotal = orderItem.Price * quantity;
					refundSubtotal = matchedRefundItems.Sum(x => x.SubTotal);
				}

				if (orderItem.DiscountAllocations?.Count > 0)
				{
					var itemDiscount = orderItem.DiscountAllocations.Sum(x => x.DiscountAmount);
					if (refundSubtotal != 0)
					{
						itemDiscount = itemDiscount + refundSubtotal - (orderItem.Price * refundQuantity);
					}
					totalDiscount += itemDiscount;
					if (bindingExt?.PostDiscounts == BCPostDiscountAttribute.LineDiscount)
					{
						detail.DiscountAmount = itemDiscount.ValueField();
					}

				}
				//If the refund item have the same quantity, that means this item have been removed from Order.
				if (quantity == 0)
				{
					skipOrderItems.Add(orderItem.Id.Value);
					continue;
				}

				String inventoryCD = GetInventoryCDByExternID(orderItem.ProductId?.ToString(), orderItem.VariantId.ToString(), orderItem.Sku ?? string.Empty, orderItem.Name, orderItem.IsGiftCard, out string uom);

				detail.Branch = impl.FinancialSettings.Branch;
				detail.InventoryID = inventoryCD?.TrimEnd().ValueField();
				detail.OrderQty = quantity.ValueField();
				detail.UOM = uom.ValueField();
				detail.UnitPrice = orderItem.Price.ValueField();
				detail.LineDescription = orderItem.Name.ValueField();
				detail.ExtendedPrice = subTotal.ValueField();
				detail.FreeItem = (orderItem.Price == 0m).ValueField();
				detail.ManualPrice = true.ValueField();
				detail.ExternalRef = orderItem.Id.ToString().ValueField();

				//Warehouse and Location mapping
				BCLocations matchedMapping = null;
				if (orderLevelLocation != null && locationMappings.Count > 0 && locationMappings.Any(x => x.ExternalLocationID == orderLevelLocation))
				{
					//Order Level location will be used first
					matchedMapping = locationMappings.First(x => x.ExternalLocationID == orderLevelLocation);
					detail.WarehouseID = matchedMapping.SiteCD?.ValueField();
					detail.Location = matchedMapping.LocationCD?.ValueField();
				}

				//Check for existing				
				DetailInfo matchedDetail = existing?.Details?.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && orderItem.Id.ToString() == d.ExternID);
				if (matchedDetail != null) detail.Id = matchedDetail.LocalID; //Search by Details
				else if (presented?.Details != null && presented.Details.Count > 0) //Serach by Existing line
				{
					SalesOrderDetail matchedLine = presented.Details.FirstOrDefault(x =>
						(x.ExternalRef?.Value != null && x.ExternalRef?.Value == orderItem.Id.ToString())
						||
						(x.InventoryID?.Value == detail.InventoryID?.Value && (detail.UOM == null || detail.UOM.Value == x.UOM?.Value)));
					if (matchedLine != null) detail.Id = matchedLine.Id;
				}

				impl.Details.Add(detail);
			}
			#endregion

			#region Add RefundItem Line
			var totalOrderRefundAmout = refundOrderAdjustments?.Where(x => x.Kind == OrderAdjustmentType.RefundDiscrepancy && x.Amount < 0)?.Sum(y => (y.Amount)) ?? 0;
			if (totalOrderRefundAmout != 0)
			{
				var detail = InsertRefundAmountItem(totalOrderRefundAmout, branch);

				if (presented != null && presented.Details?.Count > 0)
				{
					presented.Details.FirstOrDefault(x => x.InventoryID.Value == detail.InventoryID.Value).With(e => detail.Id = e.Id);
				}
				impl.Details.Add(detail);
			}
			#endregion

			#region Taxes
			impl.TaxDetails = new List<TaxDetail>();
			if (data.TaxLines?.Count > 0)
			{
				if (GetBindingExt<BCBindingExt>()?.TaxSynchronization == true)
				{
					impl.IsTaxValid = true.ValueField();
					foreach (OrderTaxLine tax in data.TaxLines)
					{
						//Third parameter set to tax name in order to simplify process (if tax names are equal and user don't want to fill lists)
						String mappedTaxName = GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxSubstitutionListID, tax.TaxName, tax.TaxName);
						mappedTaxName = helper.TrimAutomaticTaxNameForAvalara(mappedTaxName);
						if (string.IsNullOrEmpty(mappedTaxName)) throw new PXException(PX.Commerce.Objects.BCObjectsMessages.TaxNameDoesntExist);

						decimal? taxable = 0m, taxableExcludeRefundItems = 0m;
						decimal? taxAmount = 0m;
						if (tax.TaxRate != 0m)
						{
							var lineItemsWithTax = data.LineItems.Where(x => x.TaxLines?.Count > 0 && x.TaxLines.Any(t => t.TaxAmount > 0m && t.TaxName == tax.TaxName));
							var shippingItemsWithTax = data.ShippingLines.Where(x => x.TaxLines?.Count > 0 && x.TaxLines.Any(t => t.TaxAmount > 0m && t.TaxName == tax.TaxName));
							taxable = lineItemsWithTax.Sum(x => (x?.Price * x?.Quantity) ?? 0m) - lineItemsWithTax.SelectMany(x => x.DiscountAllocations)?.Sum(x => x.DiscountAmount ?? 0m);
							taxable += shippingItemsWithTax.Sum(x => x.ShippingCostExcludingTax ?? 0m) - shippingItemsWithTax.SelectMany(x => x.DiscountAllocations)?.Sum(x => x.DiscountAmount ?? 0m) - shippingrefundAmt;
							taxableExcludeRefundItems = taxable;
							taxAmount = tax.TaxAmount;
							if (cancelledOrder == false && refundItems?.Count > 0)
							{
								//If the line item shows in the Refunds field, we have to calculate the Tax manually.
								var lineItemIds = lineItemsWithTax.Select(x => x.Id).Distinct();
								var refundItemsWithTax = refundItems.Where(x => lineItemIds.Contains(x.LineItemId)).ToList();
								if (refundItemsWithTax?.Count > 0)
								{
									taxableExcludeRefundItems = taxable - refundItemsWithTax.Sum(x => x.SubTotal ?? 0m);
									taxAmount = Math.Round((decimal)(taxableExcludeRefundItems * tax.TaxRate), 2);
								}
							}
						}

						TaxDetail inserted = impl.TaxDetails.FirstOrDefault(i => i.TaxID.Value?.Equals(mappedTaxName, StringComparison.InvariantCultureIgnoreCase) == true);
						if (inserted == null)
						{
							impl.TaxDetails.Add(new TaxDetail()
							{
								TaxID = mappedTaxName.ValueField(),
								TaxAmount = taxAmount.ValueField(),
								TaxRate = (tax.TaxRate * 100).ValueField(),
								TaxableAmount = taxableExcludeRefundItems.ValueField()
							});
						}
						else if (inserted.TaxAmount != null && taxable == taxableExcludeRefundItems)
						{
							inserted.TaxAmount.Value += tax.TaxAmount;
						}
					}
				}
			}

			//Check for tax Ids with more than 30 characters
			String[] tooLongTaxIDs = ((impl.TaxDetails ?? new List<TaxDetail>()).Select(x => x.TaxID?.Value).Where(x => (x?.Length ?? 0) > PX.Objects.TX.Tax.taxID.Length).ToArray());
			if (tooLongTaxIDs != null && tooLongTaxIDs.Length > 0)
			{
				throw new PXException(PX.Commerce.Objects.BCObjectsMessages.CannotFindSaveTaxIDs, string.Join(",", tooLongTaxIDs), PX.Objects.TX.Tax.taxID.Length);
			}

			if (GetBindingExt<BCBindingExt>()?.TaxSynchronization == true && GetBindingExt<BCBindingExt>()?.DefaultTaxZoneID != null && GetBindingExt<BCBindingExt>()?.UseAsPrimaryTaxZone == true)
			{
				impl.FinancialSettings.OverrideTaxZone = true.ValueField();
				impl.FinancialSettings.CustomerTaxZone = GetBindingExt<BCBindingExt>()?.DefaultTaxZoneID.ValueField();
			}
			#endregion

			#region Discounts
			impl.DisableAutomaticDiscountUpdate = true.ValueField();
			impl.DiscountDetails = new List<SalesOrdersDiscountDetails>();
			if (data.DiscountApplications?.Count > 0)
			{
				SalesOrdersDiscountDetails itemDiscountDetail = null;
				var totalItemDiscounts = data.LineItems.SelectMany(x => x.DiscountAllocations).ToList();
				//If there is a shipping discount, it has been applied to the Freight fee calculation above.
				for (int i = 0; i < data.DiscountApplications.Count; i++)
				{
					var discountItem = data.DiscountApplications[i];
					SalesOrdersDiscountDetails detail = new SalesOrdersDiscountDetails();
					detail.Type = PX.Objects.Common.Discount.DiscountType.ExternalDocument.ValueField();
					detail.ExternalDiscountCode = discountItem.Type == DiscountApplicationType.DiscountCode ? discountItem.Code.ValueField() : (discountItem.Title ?? discountItem.Description).ValueField();
					detail.Description = (discountItem.Description ?? string.Empty).ValueField();
					if (discountItem.TargetType == DiscountTargetType.ShippingLine)
					{
						detail.Description = ShopifyMessages.DiscountAppliedToShippingItem.ValueField();
						detail.DiscountAmount = 0m.ValueField();
						impl.DiscountDetails.Add(detail);
					}
					else
					{
						var matchedDiscounts = totalItemDiscounts.Where(x => x.DiscountApplicationIndex == i);
						if (GetBindingExt<BCBindingExt>()?.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount)
						{
							detail.DiscountAmount = matchedDiscounts.Sum(x => x.DiscountAmount ?? 0m).ValueField();
						}
						else
						{
							detail.Description = ShopifyMessages.DiscountAppliedToLineItem.ValueField();
							detail.DiscountAmount = 0m.ValueField();
						}
						//If the refund items have discount, we cannot get the accurate discount amount, we have to combine all discounts to the order level.
						if (cancelledOrder == false && refundItems?.Count > 0 && refundItems.Any(x => x.OrderLineItem.DiscountAllocations?.Count > 0) && GetBindingExt<BCBindingExt>()?.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount)
						{
							itemDiscountDetail = detail;
							itemDiscountDetail.ExternalDiscountCode = ShopifyMessages.RefundDiscount.ValueField();
							itemDiscountDetail.DiscountAmount = totalDiscount.ValueField();
							break;
						}
						else
						{
							impl.DiscountDetails.Add(detail);
						}
					}
				}
				if (itemDiscountDetail != null)
				{
					impl.DiscountDetails.Add(itemDiscountDetail);
				}
			}
			#endregion

			#region Payment
			if (existing == null && GetEntity(BCEntitiesAttribute.Payment)?.IsActive == true && !paymentProcessor.ImportMappings.Select().Any() && data.FinancialStatus != OrderFinancialStatus.PartiallyRefunded && data.FinancialStatus != OrderFinancialStatus.Refunded)
			{
				impl.Payments = new List<SalesOrderPayment>();
				foreach (MappedPayment payment in bucket.Payments)
				{
					OrderTransaction dataPayment = payment.Extern;
					SalesOrderPayment implPament = new SalesOrderPayment();
					if (!payment.IsNew)
						continue;
					implPament.ExternalID = payment.ExternID;

					//Product
					implPament.DocType = PX.Objects.AR.Messages.Prepayment.ValueField();
					implPament.Currency = impl.CurrencyID;
					var appDate = dataPayment.DateCreatedAt.ToDate(false, PXTimeZoneInfo.FindSystemTimeZoneById(GetBindingExt<BCBindingExt>()?.OrderTimeZone));
					if (appDate.HasValue)
						implPament.ApplicationDate = (new DateTime(appDate.Value.Date.Ticks)).ValueField();
					implPament.PaymentAmount = ((decimal)dataPayment.Amount).ValueField();
					implPament.Hold = false.ValueField();

					implPament.AppliedToOrder = ((decimal)dataPayment.Amount).ValueField();

					BCPaymentMethods methodMapping = helper.GetPaymentMethodMapping(dataPayment.Gateway, dataPayment.Currency, out string cashAcount);
					if (methodMapping.ReleasePayments ?? false) continue; //don't save payment with the order if the require release.

					implPament.PaymentMethod = methodMapping?.PaymentMethodID.ValueField();
					implPament.CashAccount = cashAcount?.Trim()?.ValueField();
					implPament.ExternalRef = dataPayment.Id.ToString().ValueField();
					implPament.PaymentRef = helper.ParseTransactionNumber(dataPayment, out bool isCreditCardTran).ValueField();

					PX.Objects.AR.ARRegister existingPayment = PXSelect<PX.Objects.AR.ARRegister,
						Where<PX.Objects.AR.ARRegister.externalRef, Equal<Required<PX.Objects.AR.ARRegister.externalRef>>>>.Select(this, implPament.ExternalRef.Value);
					if (existingPayment != null) continue; //skip if payment with same ref nbr exists already.

					TransactionStatus? lastStatus = data.Transactions.LastOrDefault(x => x.ParentId == data.Id && x.Status == TransactionStatus.Success)?.Status ?? dataPayment.Status;
					var paymentDesc = PXMessages.LocalizeFormat(ShopifyMessages.PaymentDescription, currentBinding.BindingName, bucket.Order?.Extern?.Name, dataPayment.Kind.ToString(), lastStatus?.ToString(), dataPayment.Gateway);
					implPament.Description = paymentDesc.ValueField();

					//Credit Card:
					if (methodMapping?.ProcessingCenterID != null && isCreditCardTran)
					{
						//implPament.IsNewCard = true.ValueField();
						implPament.SaveCard = false.ValueField();
						implPament.ProcessingCenterID = methodMapping?.ProcessingCenterID?.ValueField();

						SalesOrderCreditCardTransactionDetail creditCardDetail = new SalesOrderCreditCardTransactionDetail();
						creditCardDetail.TranNbr = implPament.PaymentRef;
						if (appDate != null && appDate.HasValue)
							creditCardDetail.TranDate = date.ValueField();
						//creditCardDetail.ExtProfileId = dataPayment.PaymentInstrumentToken.ValueField();
						creditCardDetail.TranType = helper.GetTransactionType(dataPayment.LastKind);
						implPament.CreditCardTransactionInfo = new List<SalesOrderCreditCardTransactionDetail>(new[] { creditCardDetail });
					}

					impl.Payments.Add(implPament);
				}
			}
			#endregion

			#region Order Risks
			impl.OrderRisks = new List<OrderRisks>();
			impl.MaxRiskScore = new DecimalValue() { Value = null };
			obj.Local.OrderRisks?.AddRange(presented?.OrderRisks == null ? Enumerable.Empty<OrderRisks>()
				: presented.OrderRisks.Select(n => new OrderRisks() { Id = n.Id, Delete = true }));
			if (bindingExt.ImportOrderRisks == true && data.OrderRisks?.Count > 0)
			{
				foreach (var shopifyRisk in data.OrderRisks)
				{
					var risk = new OrderRisks()
					{
						Message = shopifyRisk.Message.ValueField(),
						Recommendation = shopifyRisk.Recommendation.ToString().ValueField(),
						Score = (shopifyRisk.Score * 100).ValueField(),
					};
					impl.OrderRisks.Add(risk);

				}

				impl.MaxRiskScore = (impl.OrderRisks?.Max(x => x.Score?.Value) ?? 0).ValueField();
				//Need to put onhold For workflow to begin with RiskHold State if condition statisfied
				if (bindingExt.HoldOnRiskStatus == BCRiskStatusAttribute.HighRisk && impl.MaxRiskScore.Value > 60m)
				{
					impl.Hold = true.ValueField();
				}
				else if (bindingExt.HoldOnRiskStatus == BCRiskStatusAttribute.MediumRiskorHighRisk && impl.MaxRiskScore.Value > 20m)
				{
					impl.Hold = true.ValueField();
				}
			}
			#endregion

			#region Adjust for Existing
			if (presented != null)
			{
				obj.Local.OrderType = presented.OrderType; //Keep the same order Type

				//remap entities if existing
				presented.DiscountDetails?.ForEach(e => obj.Local.DiscountDetails?.FirstOrDefault(n => n.ExternalDiscountCode.Value == e.ExternalDiscountCode.Value).With(n => n.Id = e.Id));
				presented.Payments?.ForEach(e => obj.Local.Payments?.FirstOrDefault(n => n.PaymentRef.Value == e.PaymentRef.Value).With(n => n.Id = e.Id));

				//delete unnecessary entities
				obj.Local.Details?.AddRange(presented.Details == null ? Enumerable.Empty<SalesOrderDetail>()
					: presented.Details.Where(e => obj.Local.Details == null || !obj.Local.Details.Any(n => e.Id == n.Id)).Select(n => new SalesOrderDetail() { Id = n.Id, Delete = true }));
				obj.Local.DiscountDetails?.AddRange(presented.DiscountDetails == null ? Enumerable.Empty<SalesOrdersDiscountDetails>()
					: presented.DiscountDetails.Where(e => obj.Local.DiscountDetails == null || !obj.Local.DiscountDetails.Any(n => e.Id == n.Id)).Select(n => new SalesOrdersDiscountDetails() { Id = n.Id, Delete = true }));
				obj.Local.Payments?.AddRange(presented.Payments == null ? Enumerable.Empty<SalesOrderPayment>()
					: presented.Payments.Where(e => obj.Local.Payments == null || !obj.Local.Payments.Any(n => e.Id == n.Id)).Select(n => new SalesOrderPayment() { Id = n.Id, Delete = true }));
			}
			#endregion
		}

		protected bool CompareAddress(Core.API.Address mappedAddress, PX.Objects.CR.Address address)
		{
			return Compare(mappedAddress.City?.Value, address.City)
											&& Compare(mappedAddress.Country?.Value, address.CountryID)
											&& Compare(mappedAddress.State?.Value, address.State)
											&& Compare(mappedAddress.AddressLine1?.Value, address.AddressLine1)
											&& Compare(mappedAddress.AddressLine2?.Value, address.AddressLine2)
											&& Compare(mappedAddress.PostalCode?.Value, address.PostalCode);
		}
		protected bool CompareContact(DocContact mappedContact, PX.Objects.CR.Contact contact, PX.Objects.CR.Location location = null)
		{
			return (Compare(mappedContact.BusinessName?.Value, contact.FullName) || Compare(mappedContact.BusinessName?.Value, location?.Descr))
										&& Compare(mappedContact.Attention?.Value, contact.Attention)
										&& Compare(mappedContact.Email?.Value, contact.EMail)
										&& Compare(mappedContact.Phone1?.Value, contact.Phone1);
		}
		protected bool Compare(string value1, string value2)
		{
			return string.Equals(value1?.Trim() ?? string.Empty, value2?.Trim() ?? string.Empty, StringComparison.InvariantCultureIgnoreCase);
		}

		public override void SaveBucketImport(SPSalesOrderBucket bucket, IMappedEntity existing, string operation)
		{
			MappedOrder obj = bucket.Order;
			SalesOrder local = obj.Local;
			SalesOrder presented = existing?.Local as SalesOrder;
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();

			// If custom mapped orderType, this will prevent attempt to modify existing SO type and following error
			if (existing != null)
				obj.Local.OrderType = ((MappedOrder)existing).Local.OrderType;

			SalesOrder impl;
			//If we need to cancel the order in Acumatica
			if ((obj.Extern?.CancelledAt != null || obj.Extern.FinancialStatus == OrderFinancialStatus.Refunded) && presented != null)
			{
				SalesOrder orderToCancel = presented == null ? null : new SalesOrder()
				{
					Description = local.Description,
					Id = presented.Id,
					Payments = presented.Payments?.Where(x => !string.Equals(x.Status.Value, PX.Objects.AR.Messages.Voided)).Select(x => new SalesOrderPayment() { Id = x.Id, AppliedToOrder = 0m.ValueField() }).ToList(),
				};
				impl = cbapi.Invoke<SalesOrder, CancelSalesOrder>(orderToCancel, obj.LocalID);
			}
			else
			{
				//sort solines by deleted =true first because of api bug  in case if lines are deleted
				obj.Local.Details = obj.Local.Details.OrderByDescending(o => o.Delete).ToList();
				obj.Local.DiscountDetails = obj.Local.DiscountDetails.OrderByDescending(o => o.Delete).ToList();


				#region Taxes
				helper.LogTaxDetails(obj.SyncID, obj.Local);
				#endregion

				impl = cbapi.Put<SalesOrder>(obj.Local, obj.LocalID);

				#region Taxes
				helper.ValidateTaxes(obj.SyncID, impl, obj.Local);
				#endregion


				//If we need to cancel the order in Acumatica
				if (obj.Extern?.CancelledAt != null || obj.Extern.FinancialStatus == OrderFinancialStatus.Refunded)
				{
					impl = cbapi.Invoke<SalesOrder, CancelSalesOrder>(null, impl.SyncID);
				}
			}
			obj.AddLocal(impl, impl.SyncID, impl.SyncTime);

			// Save Details
			DetailInfo[] oldDetails = obj.Details.ToArray();
			obj.ClearDetails();
			if (bucket.Location?.Extern != null && bucket.Location?.ExternID != null)
			{
				obj.AddDetail(BCEntitiesAttribute.OrderAddress, impl.SyncID, bucket.Location.ExternID); //Shipment ID detail	
			}
			foreach (OrderLineItem orderItem in obj.Extern.LineItems) //Line ID detail
			{
				if (orderItem.Quantity == 0 || skipOrderItems.Contains(orderItem.Id.Value)) continue;

				SalesOrderDetail detail = null;
				detail = impl.Details.FirstOrDefault(x => x.NoteID.Value == oldDetails.FirstOrDefault(o => o.ExternID == orderItem.Id.ToString())?.LocalID);
				if (detail == null) detail = impl.Details.FirstOrDefault(x => x.ExternalRef?.Value != null && x.ExternalRef?.Value == orderItem.Id.ToString());
				if (detail == null)
				{
					String inventoryCD = GetInventoryCDByExternID(
						orderItem.ProductId.ToString(),
						orderItem.VariantId.ToString(),
						orderItem.Sku ?? string.Empty,
						orderItem.Name,
						orderItem.IsGiftCard,
						out string uom);
					detail = impl.Details.FirstOrDefault(x => !obj.Details.Any(o => x.NoteID.Value == o.LocalID) && x.InventoryID.Value == inventoryCD);
				}
				if (detail != null)
				{
					obj.AddDetail(BCEntitiesAttribute.OrderLine, detail.NoteID.Value, orderItem.Id.ToString());
					continue;
				}
				throw new PXException(BCMessages.CannotMapLines);
			}

			#region Update Order To Shopify
			if (bindingExt.SyncOrderNbrToStore == true && obj.Extern?.Tags?.Contains(impl.OrderNbr?.Value, StringComparison.OrdinalIgnoreCase) != true)
			{
				OrderData exportOrderData = new OrderData() { Id = obj.ExternID.ToLong() };
				if (string.IsNullOrEmpty(obj.Extern.Tags) || !obj.Extern.Tags.Contains(ShopifyConstants.ImportedInAcumatica, StringComparison.OrdinalIgnoreCase))
					exportOrderData.Tags = string.IsNullOrEmpty(obj.Extern.Tags) ? ShopifyConstants.ImportedInAcumatica : $"{obj.Extern.Tags},{ShopifyConstants.ImportedInAcumatica}";
				else
					exportOrderData.Tags = obj.Extern.Tags;
				exportOrderData.Tags = string.IsNullOrEmpty(exportOrderData.Tags) ? impl.OrderNbr.Value : $"{exportOrderData.Tags},{impl.OrderNbr.Value}";
				var existedMetafield = obj.Extern.Metafields?.FirstOrDefault(x => x.Key == ShopifyConstants.ImportedInAcumatica);
				if (existedMetafield == null)
				{
					existedMetafield = new MetafieldData()
					{
						Key = ShopifyConstants.ImportedInAcumatica,
						Value = impl.OrderNbr.Value,
						ValueType = ShopifyConstants.ValueType_String,
						Namespace = ShopifyConstants.Namespace_Global
					};
					exportOrderData.Metafields = new List<MetafieldData>() { existedMetafield };
				}
				else if (existedMetafield.Value?.Contains(impl.OrderNbr.Value, StringComparison.OrdinalIgnoreCase) != true)
				{
					existedMetafield.Value = string.IsNullOrEmpty(existedMetafield.Value) ? impl.OrderNbr.Value : $"{existedMetafield.Value},{impl.OrderNbr.Value}";
					exportOrderData.Metafields = new List<MetafieldData>() { existedMetafield };
				}
				try
				{
					var returnOrderData = orderDataProvider.Update(exportOrderData, obj.ExternID);
					obj.ExternTimeStamp = returnOrderData?.DateModifiedAt.ToDate(false);
				}
				catch (Exception ex)
				{
					LogWarning(Operation.LogScope(), ex.Message);
				}

			}
			#endregion

			UpdateStatus(obj, operation);

			#region Payments
			if (existing == null && local.Payments != null && bucket.Payments != null)
			{
				for (int i = 0; i < local.Payments.Count; i++)
				{
					SalesOrderPayment sent = local.Payments[i];
					PX.Objects.AR.ARPayment payment = null;
					String docType = (new PX.Objects.AR.ARDocType()).ValueLabelPairs.First(p => p.Label == sent.DocType.Value).Value;
					string extRef = sent.PaymentRef.Value;
					payment = PXSelectJoin<PX.Objects.AR.ARPayment, InnerJoin<SOAdjust, On<SOAdjust.adjgRefNbr, Equal<PX.Objects.AR.ARPayment.refNbr>>>,
						Where<PX.Objects.AR.ARPayment.extRefNbr, Equal<Required<PX.Objects.AR.ARPayment.extRefNbr>>,
						And<PX.Objects.AR.ARPayment.docType, Equal<Required<PX.Objects.AR.ARPayment.docType>>,
						And<SOAdjust.adjdOrderNbr, Equal<Required<SOAdjust.adjdOrderNbr>>
						>>>>.Select(this, extRef, docType, impl.OrderNbr.Value);
					if (payment == null) continue;

					MappedPayment objPayment = bucket.Payments.FirstOrDefault(x => x.ExternID == sent.ExternalID);
					if (objPayment == null) continue;

					objPayment.AddLocal(null, payment.NoteID, payment.LastModifiedDateTime);
					UpdateStatus(objPayment, operation);
				}
			}
			#endregion
		}
		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			var bindingExt = GetBindingExt<BCBindingExt>();
			var minDate = minDateTime == null || (minDateTime != null && bindingExt.SyncOrdersFrom != null && minDateTime < bindingExt.SyncOrdersFrom) ? bindingExt.SyncOrdersFrom : minDateTime;
			SalesOrder[] impls = new SalesOrder[] { };
			List<string> orderTypesArray = new List<string> { bindingExt.OrderType };
			if (PXAccess.FeatureInstalled<FeaturesSet.userDefinedOrderTypes>() && bindingExt.OtherSalesOrderTypes != null && bindingExt.OtherSalesOrderTypes?.Count() > 0)
				orderTypesArray.AddRange(bindingExt.OtherSalesOrderTypes.Split(',').Where(i => i != bindingExt.OrderType).ToList());

			foreach (var orderType in orderTypesArray)
			{
				if (String.IsNullOrEmpty(orderType)) continue;

				var res = cbapi.GetAll<SalesOrder>(
						new SalesOrder()
						{
							OrderType = orderType.SearchField(),
							OrderNbr = new StringReturn(),
							Status = new StringReturn(),
							CustomerID = new StringReturn(),
							ExternalRef = new StringReturn(),
							Details = new List<SalesOrderDetail>() { new SalesOrderDetail() {
							ReturnBehavior = ReturnBehavior.OnlySpecified,
							InventoryID = new StringReturn() } }
						},
						minDate, maxDateTime, filters);
				if (res != null)
					impls = impls.Append(res.ToArray());
			}

			var invIDs = new List<string>();

			BCEntity entity = GetEntity(Operation.EntityType);
			if (impls != null && impls.Count() > 0)
			{
				List<BCSyncStatus> bcSyncStatusList = null;
				int countNum = 0;
				List<MappedOrder> mappedList = new List<MappedOrder>();
				foreach (SalesOrder impl in impls)
				{
					MappedOrder obj = new MappedOrder(impl, impl.SyncID, impl.SyncTime);

					mappedList.Add(obj);
					countNum++;
					if (countNum % BatchFetchCount == 0 || countNum == impls.Count())
					{
						var localIDs = mappedList.Select(x => x.LocalID.Value).ToArray();
						bcSyncStatusList = GetBCSyncStatusResult(entity.EntityType, null, localIDs, null).Select(x => x.GetItem<BCSyncStatus>()).ToList();
						EntityStatus status = 0;
						foreach (var oneMapped in mappedList)
						{
							status = EnsureStatusBulk(bcSyncStatusList, oneMapped, SyncDirection.Export, true);
						}
						if (status == EntityStatus.Pending) invIDs.Add(impl?.OrderNbr?.Value ?? "__");
						mappedList.Clear();
					}
				}
			}
		}
		public override EntityStatus GetBucketForExport(SPSalesOrderBucket bucket, BCSyncStatus syncstatus)
		{
			SalesOrder impl = cbapi.GetByID<SalesOrder>(syncstatus.LocalID);
			if (impl == null) return EntityStatus.None;

			MappedOrder obj = bucket.Order = bucket.Order.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(bucket.Order, SyncDirection.Export);

			if (status != EntityStatus.Pending && status != EntityStatus.Syncronized)
				return status;

			if (GetEntity(BCEntitiesAttribute.Shipment)?.IsActive == true && impl.Shipments != null)
			{
				foreach (SalesOrderShipment orderShipmentImpl in impl.Shipments)
				{
					if (orderShipmentImpl.ShippingNoteID?.Value == null) continue;

					BCShipments shipmentImpl = new BCShipments();
					shipmentImpl.ShippingNoteID = orderShipmentImpl.ShippingNoteID;
					shipmentImpl.OrderNoteIds = new List<Guid?>() { syncstatus.LocalID };
					shipmentImpl.ShipmentNumber = orderShipmentImpl.ShipmentNbr;
					shipmentImpl.ShipmentType = orderShipmentImpl.ShipmentType;
					shipmentImpl.Confirmed = (orderShipmentImpl.Status?.Value == BCAPICaptions.Confirmed).ValueField();
					MappedShipment shipmentObj = new MappedShipment(shipmentImpl, shipmentImpl.ShippingNoteID.Value, orderShipmentImpl.LastModifiedDateTime.Value);
					EntityStatus shipmentStatus = EnsureStatus(shipmentObj, SyncDirection.Export);

					if (shipmentStatus == EntityStatus.Pending)
						bucket.Shipments.Add(shipmentObj);
				}
			}

			BCSyncStatus customerStatus = PXSelectJoin<BCSyncStatus,
				InnerJoin<PX.Objects.AR.Customer, On<PX.Objects.AR.Customer.noteID, Equal<BCSyncStatus.localID>>>,
				Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
					And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
					And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
					And<PX.Objects.AR.Customer.acctCD, Equal<Required<PX.Objects.AR.Customer.acctCD>>>>>>>
				.Select(this, BCEntitiesAttribute.Customer, impl.CustomerID?.Value);
			if (customerStatus == null)
			{
				Core.API.Customer implCust = cbapi.Get<Core.API.Customer>(new Core.API.Customer() { CustomerID = new StringSearch() { Value = impl.CustomerID.Value } });
				if (implCust == null)
					throw new PXException(BCMessages.NoCustomerForOrder, obj.Local.OrderNbr.Value);
				MappedCustomer objCust = new MappedCustomer(implCust, implCust.SyncID, implCust.SyncTime);
				EntityStatus custStatus = EnsureStatus(objCust, SyncDirection.Export);

				if (custStatus == EntityStatus.Pending)
					bucket.Customer = objCust;
			}
			return status;
		}

		public override void MapBucketExport(SPSalesOrderBucket bucket, IMappedEntity existing)
		{

		}
		public override void SaveBucketExport(SPSalesOrderBucket bucket, IMappedEntity existing, string operation)
		{

		}
		#endregion

		#region Methods
		[Obsolete("Method will be removed in Acumatica 2021r2. Please use BCHelper.GetInventoryCDByExternID instead.", false)]
		public virtual String GetInventoryCDByExternID(String productID, String variantID, String sku, String description, bool? isGiftCard, out string uom)
		{
			return helper.GetInventoryCDByExternID(productID, variantID, sku, description, isGiftCard, out uom);
		}
		#endregion
	}
}