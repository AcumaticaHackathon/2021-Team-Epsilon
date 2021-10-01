using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Commerce.Shopify.API.REST;
using PX.Data;
using System;
using System.Collections.Generic;
using PX.Objects.SO;
using System.Linq;
using PX.Common;
using PX.Objects.AR;
using PX.Api.ContractBased.Models;
using PX.Objects.CR;
using PX.Objects.CA;
using PX.Objects.GL;
using Newtonsoft.Json;

namespace PX.Commerce.Shopify
{
	public class SPRefundsBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary { get => Refunds; }
		public IMappedEntity[] Entities => new IMappedEntity[] { Refunds };
		public override IMappedEntity[] PostProcessors { get => new IMappedEntity[] { Order }; }

		public MappedRefunds Refunds;
		public MappedOrder Order;
	}

	public class SPRefundsRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			return null;
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			return base.Restrict<MappedRefunds>(mapped, delegate (MappedRefunds obj)
			{
				if (obj.Extern != null)
				{
					if (!obj.Extern.Refunds.Any(x => x.Transactions.Any(a => (a.Kind == TransactionType.Refund || a.Kind == TransactionType.Void) && a.Status == TransactionStatus.Success)))
					{
						return new FilterResult(FilterStatus.Filtered,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogRefundSkippedStatus, obj.Extern.Id));
					}
				}

				if (processor.SelectStatus(BCEntitiesAttribute.Order, obj.Extern.Id.ToString()) == null)
				{
					return new FilterResult(FilterStatus.Ignore,
						PXMessages.LocalizeNoPrefix(BCMessages.LogRefundSkippedOrderNotSynced));
				}

				return null;
			});
		}
	}

	[BCProcessor(typeof(SPConnector), BCEntitiesAttribute.OrderRefunds, BCCaptions.Refunds,
		IsInternal = false,
		Direction = SyncDirection.Import,
		PrimaryDirection = SyncDirection.Import,
		PrimarySystem = PrimarySystem.Extern,
		PrimaryGraph = typeof(PX.Objects.SO.SOOrderEntry),
		ExternTypes = new Type[] { },
		LocalTypes = new Type[] { },
		DetailTypes = new String[] { BCEntitiesAttribute.CustomerRefundOrder, BCCaptions.CustomerRefundOrder },
		AcumaticaPrimaryType = typeof(PX.Objects.SO.SOOrder),
		AcumaticaPrimarySelect = typeof(Search2<PX.Objects.SO.SOOrder.orderNbr,
			InnerJoin<BCBinding, On<BCBindingExt.orderType, Equal<SOOrder.orderType>>>,
			Where<BCBinding.connectorType, Equal<Current<BCSyncStatusEdit.connectorType>>,
				And<BCBinding.bindingID, Equal<Current<BCSyncStatusEdit.bindingID>>>>>),
		URL = "orders/{0}",
		Requires = new string[] { BCEntitiesAttribute.Order, BCEntitiesAttribute.Payment }
	)]
	[BCProcessorRealtime(PushSupported = false, HookSupported = true,
		 WebHookType = typeof(WebHookMessage),
		WebHooks = new String[]
		{
			"refunds/create"
		})]
	public class SPRefundsProcessor : SPOrderBaseProcessor<SPRefundsProcessor, SPRefundsBucket, MappedRefunds>
	{
		protected OrderRestDataProvider orderDataProvider;
		protected BCBindingShopify currentShopifySettings;

		#region Initialization
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			currentShopifySettings = GetBindingExt<BCBindingShopify>();
			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());
			orderDataProvider = new OrderRestDataProvider(client);
		}
		#endregion

		#region Pull
		public override MappedRefunds PullEntity(Guid? localID, Dictionary<string, object> fields)
		{
			SalesOrder impl = cbapi.GetByID<SalesOrder>(localID);
			if (impl == null) return null;

			MappedRefunds obj = new MappedRefunds(impl, impl.SyncID, impl.SyncTime);

			return obj;
		}
		public override MappedRefunds PullEntity(string externID, string jsonObject)
		{
			dynamic msg = JsonConvert.DeserializeObject(jsonObject);

			string orderId = (string)msg.order_id;
			if (orderId == null) return null;
			var orderData = orderDataProvider.GetByID(orderId);
			if (orderData == null) return null;
			if (orderData.Refunds == null || orderData.Refunds.Count == 0) return null;
			var date = orderData.Refunds.FirstOrDefault(x => x.Id.ToString() == externID)?.DateCreatedAt.ToDate(false);
			if (date == null) return null;
			MappedRefunds obj = new MappedRefunds(orderData, orderData.Id.ToString(), date);

			return obj;
		}
		#endregion

		public override IEnumerable<MappedRefunds> PullSimilar(IExternEntity entity, out string uniqueField)
		{
			uniqueField = ((OrderData)entity)?.Id?.ToString();
			if (string.IsNullOrEmpty(uniqueField))
				return null;
			uniqueField = APIHelper.ReferenceMake(uniqueField, currentBinding.BindingName);

			List<MappedRefunds> result = new List<MappedRefunds>();
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
				result.Add(new MappedRefunds(data, data.SyncID, data.SyncTime));
			}
			return result;
		}

		#region Export

		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{

		}
		public override EntityStatus GetBucketForExport(SPRefundsBucket bucket, BCSyncStatus syncstatus)
		{
			SalesOrder impl = cbapi.GetByID<SalesOrder>(syncstatus.LocalID, GetCustomFieldsForExport());
			if (impl == null) return EntityStatus.None;

			bucket.Refunds = bucket.Refunds.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(bucket.Refunds, SyncDirection.Export);


			return status;
		}

		public override void SaveBucketExport(SPRefundsBucket bucket, IMappedEntity existing, string operation)
		{
		}
		#endregion

		#region Import

		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			var currentBindingExt = GetBindingExt<BCBindingExt>();
			var minDate = minDateTime == null || (minDateTime != null && currentBindingExt.SyncOrdersFrom != null && minDateTime < currentBindingExt.SyncOrdersFrom) ? currentBindingExt.SyncOrdersFrom : minDateTime;
			FilterOrders filter = new FilterOrders { Status = OrderStatus.Any, Fields = "id,source_name,financial_status,updated_at,created_at,refunds" };
			if (minDate != null) filter.UpdatedAtMin = minDate.Value.ToLocalTime().AddSeconds(-GetBindingExt<BCBindingShopify>().ApiDelaySeconds ?? 0);
			if (maxDateTime != null) filter.UpdatedAtMax = maxDateTime.Value.ToLocalTime();
			IEnumerable<OrderData> datas = orderDataProvider.GetAll(filter);

			foreach (OrderData orderData in datas)
			{
				if (orderData.Refunds == null || orderData.Refunds.Count == 0) continue;

				SPRefundsBucket bucket = CreateBucket();
				var orderStatus = this.SelectStatus(BCEntitiesAttribute.Order, orderData.Id.ToString(), false);

				if (orderStatus == null) continue;
				var date = orderData.Refunds.Max(x => x.DateCreatedAt.ToDate(false));
				MappedRefunds obj = bucket.Refunds = bucket.Refunds.Set(orderData, orderData.Id.ToString(), date).With(_ => { _.ParentID = orderStatus.SyncID; return _; });
				EntityStatus status = EnsureStatus(obj, SyncDirection.Import, true);
			}
		}
		public override EntityStatus GetBucketForImport(SPRefundsBucket bucket, BCSyncStatus syncstatus)
		{
			OrderData orderData = orderDataProvider.GetByID(syncstatus.ExternID.KeySplit(0).ToString(), includedTransactions: true);
			if (orderData == null) return EntityStatus.None;
			EntityStatus status = EntityStatus.None;
			if (orderData.Refunds == null || orderData.Refunds.Count == 0) return status;
			var orderStatus = (BCSyncStatus)this.SelectStatus(BCEntitiesAttribute.Order, orderData.Id.ToString(), false);
			if (orderStatus == null) return status;

			if (orderStatus.LastOperation == BCSyncOperationAttribute.Skipped)
				throw new PXException(BCMessages.OrderStatusSkipped, orderData.Id);

			bucket.Order = bucket.Order.Set(orderData, orderData.Id?.ToString(), orderData.DateModifiedAt.ToDate(false));

			var date = orderData.Refunds.Max(x => x.DateCreatedAt.ToDate(false));
			MappedRefunds obj = bucket.Refunds = bucket.Refunds.Set(orderData, orderData.Id.ToString(), date);
			status = EnsureStatus(obj, SyncDirection.Import);

			return status;
		}
		public override void MapBucketImport(SPRefundsBucket bucket, IMappedEntity existing)
		{
			MappedRefunds obj = bucket.Refunds;
			OrderData orderData = obj.Extern;
			MappedRefunds mappedRefunds = existing as MappedRefunds;
			if (mappedRefunds?.Local == null) throw new PXException(BCMessages.OrderNotSyncronized, orderData.Id);
			if (mappedRefunds.Local.Status?.Value == PX.Objects.SO.Messages.Open || mappedRefunds.Local.Status?.Value == PX.Objects.SO.Messages.Hold)
			{
				bucket.Refunds.Local = new SalesOrder();
				bucket.Refunds.Local.EditSO = true;
				CreateRefundPayment(bucket, mappedRefunds);
			}
			else if (mappedRefunds.Local.Status?.Value == PX.Objects.SO.Messages.Cancelled && (orderData.FinancialStatus == OrderFinancialStatus.Refunded || orderData.FinancialStatus == OrderFinancialStatus.Voided))
			{
				bucket.Refunds.Local = new SalesOrder();
				CreateRefundPayment(bucket, mappedRefunds);
			}
			else if (mappedRefunds.Local.Status?.Value == PX.Objects.SO.Messages.Completed)
			{
				bucket.Refunds.Local = new SalesOrder();
				CreateRefundOrders(bucket, mappedRefunds);
				CreateRefundPayment(bucket, mappedRefunds);
			}
			else
				throw new PXException(BCMessages.OrderStatusNotValid, orderData.Id);

		}

		public virtual void CreateRefundOrders(SPRefundsBucket bucket, MappedRefunds existing)
		{
			SalesOrder origOrder = bucket.Refunds.Local;
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			OrderData orderData = bucket.Refunds.Extern;
			List<OrderRefund> refunds = orderData.Refunds;
			origOrder.RefundOrders = new List<SalesOrder>();
			origOrder.Payment = new List<Payment>();
			var branch = PX.Objects.GL.Branch.PK.Find(this, currentBinding.BranchID)?.BranchCD?.ValueField();
			IEnumerable<PXResult<PX.Objects.AR.ARPayment, BCSyncStatus>> result = PXSelectJoin<PX.Objects.AR.ARPayment,
					InnerJoin<BCSyncStatus, On<PX.Objects.AR.ARPayment.noteID, Equal<BCSyncStatus.localID>>>,
					Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						And<BCSyncStatus.parentSyncID, Equal<Required<BCSyncStatus.parentSyncID>>
					>>>>>.Select(this, BCEntitiesAttribute.Payment, bucket.Refunds.ParentID).Cast<PXResult<PX.Objects.AR.ARPayment, BCSyncStatus>>();
			foreach (OrderRefund data in refunds)
			{
				SalesOrder impl = new SalesOrder();
				impl.ExternalRef = APIHelper.ReferenceMake(data.Id, currentBinding.BindingName).ValueField();

				//Check if refund is already imported as CR Order
				var existingCR = cbapi.GetAll<SalesOrder>(new SalesOrder()
				{
					OrderType = GetBindingExt<BCBindingExt>()?.ReturnOrderType.SearchField(),
					ExternalRef = impl.ExternalRef.Value.SearchField(),
					Details = new List<SalesOrderDetail>() { new SalesOrderDetail() { InventoryID = new StringReturn() } },
					DiscountDetails = new List<SalesOrdersDiscountDetails>() { new SalesOrdersDiscountDetails() { ExternalDiscountCode = new StringReturn() } }
				},
				filters: GetFilter(Operation.EntityType).LocalFiltersRows.Cast<PXFilterRow>());
				if (existingCR.Count() > 1)
				{
					throw new PXException(BCMessages.MultipleEntitiesWithUniqueField,
						PXMessages.LocalizeNoPrefix(BCCaptions.SyncDirectionImport),
						PXMessages.LocalizeNoPrefix(Connector.GetEntities().First(e => e.EntityType == Operation.EntityType).EntityName),
						data.Id.ToString());
				}
				var presentCROrder = existingCR?.FirstOrDefault();

				// check if refund is already imported as CRPayment
				if (existing != null)
				{
					if (existing?.Details?.Count() > 0)
					{
						if (existing.Details.Any(d => d.EntityType == BCEntitiesAttribute.Payment && d.ExternID.KeySplit(0) == data.Id.ToString()) && presentCROrder == null) continue;
					}

					if (existing.Local.ExternalRefundRef?.Value != null)
					{
						if (existing.Local.ExternalRefundRef.Value.Split(new char[] { ';' }).Contains(data.Id.ToString())) continue;
					}
				}

				impl.Id = presentCROrder?.Id;

				origOrder.RefundOrders.Add(impl);

				impl.RefundID = data.Id.ToString();
				impl.OrderType = (presentCROrder?.OrderType?.Value ?? GetBindingExt<BCBindingExt>()?.ReturnOrderType).ValueField();
				impl.CustomerOrder = orderData.Id.ToString().ValueField();
				impl.FinancialSettings = new FinancialSettings();
				impl.FinancialSettings.Branch = branch;
				var date = data.DateCreatedAt.ToDate(false, PXTimeZoneInfo.FindSystemTimeZoneById(GetBindingExt<BCBindingExt>()?.OrderTimeZone));
				if (date.HasValue)
					impl.Date = (new DateTime(date.Value.Date.Ticks)).ValueField();
				impl.RequestedOn = impl.Date;
				impl.CustomerID = existing.Local.CustomerID;
				impl.LocationID = existing.Local.LocationID;
				var description = PXMessages.LocalizeFormat(ShopifyMessages.OrderDescription, currentBinding.BindingName, orderData.Id, orderData.FinancialStatus?.ToString());
				impl.Description = description.ValueField();
				impl.Details = new List<SalesOrderDetail>();
				impl.Totals = new Totals();
				impl.Totals.OverrideFreightAmount = existing.Local.Totals?.OverrideFreightAmount;
				List<OrderAdjustment> refundOrderAdjustments = null;
				List<RefundLineItem> refundItems = null;
				refundOrderAdjustments = data.OrderAdjustments;
				refundItems = data.RefundLineItems;

				decimal shippingrefundAmt = refundOrderAdjustments?.Where(x => x.Kind == OrderAdjustmentType.ShippingRefund)?.Sum(x => (-x.Amount) ?? 0m) ?? 0m;
				decimal shippingrefundAmtTax = refundOrderAdjustments?.Where(x => x.Kind == OrderAdjustmentType.ShippingRefund)?.Sum(x => (-x.TaxAmount) ?? 0m) ?? 0m;

				impl.ShipVia = existing.Local.ShipVia;
				if ((existing.Local.Totals?.Freight?.Value == null || existing.Local.Totals?.Freight?.Value == 0) && existing.Local.Totals?.PremiumFreight?.Value > 0)
				{
					impl.Totals.PremiumFreight = shippingrefundAmt.ValueField();
				}
				else
				{
					impl.Totals.Freight = shippingrefundAmt.ValueField();
				}
				var totalOrderRefundAmout = refundOrderAdjustments?.Where(x => x.Kind == OrderAdjustmentType.RefundDiscrepancy)?.Sum(y => (y.Amount)) ?? 0;

				//Add orderAdjustments
				if (totalOrderRefundAmout != 0)
				{
					var detail = InsertRefundAmountItem(-totalOrderRefundAmout, branch);
					if (presentCROrder?.Details != null)
						presentCROrder?.Details.FirstOrDefault(x => x.InventoryID.Value == detail.InventoryID.Value).With(e => detail.Id = e.Id);
					impl.Details.Add(detail);
				}

				if (existing.Local.TaxDetails?.Count > 0)
				{
					var taxes = PXSelect<SOTaxTran, Where<SOTaxTran.orderType, Equal<Required<SOTaxTran.orderType>>,
					And<SOTaxTran.orderNbr, Equal<Required<SOTaxTran.orderNbr>>>>>.Select(this, existing.Local.OrderType.Value, existing.Local.OrderNbr.Value).RowCast<SOTaxTran>();
					if (taxes?.Count() > 0)
					{
						impl.TaxDetails = new List<TaxDetail>();
						if (bucket.Refunds.Extern.TaxLines?.Count > 0)
						{

							impl.IsTaxValid = true.ValueField();
							foreach (var tax in bucket.Refunds.Extern.TaxLines)
							{
								//Third parameter set to tax name in order to simplify process (if tax names are equal and user don't want to fill lists)
								String mappedTaxName = GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxSubstitutionListID, tax.TaxName, tax.TaxName);
								mappedTaxName = helper.TrimAutomaticTaxNameForAvalara(mappedTaxName);
								decimal? taxable = 0m;
								if (tax.TaxRate != 0m)
								{
									var refundsItemWithTaxes = refundItems.Where(x => x.TotalTax != 0 && x.OrderLineItem.TaxLines?.Count > 0 && x.OrderLineItem.TaxLines.Any(t => t.TaxAmount > 0m && t.TaxName == tax.TaxName));
									taxable = refundsItemWithTaxes.Sum(x => x.SubTotal ?? 0m);
									var taxAmount = taxable * tax.TaxRate;
									if (bucket.Refunds.Extern.ShippingLines.Any(x => x.TaxLines?.Count > 0 && x.TaxLines.Any(t => t.TaxAmount > 0m && t.TaxName == tax.TaxName)))
									{
										taxAmount += shippingrefundAmt * tax.TaxRate;
										taxable += shippingrefundAmt;
									}
								}
								TaxDetail inserted = impl.TaxDetails.FirstOrDefault(i => i.TaxID.Value?.Equals(mappedTaxName, StringComparison.InvariantCultureIgnoreCase) == true);
								if (inserted == null)
								{
									if (String.IsNullOrEmpty(mappedTaxName)) throw new PXException(PX.Commerce.Objects.BCObjectsMessages.TaxNameDoesntExist);

									impl.TaxDetails.Add(new TaxDetail()
									{
										TaxID = mappedTaxName.ValueField(),
										TaxAmount = (taxable * tax.TaxRate).ValueField(),
										TaxRate = (tax.TaxRate * 100).ValueField(),
										TaxableAmount = (taxable).ValueField()
									});
								}
								else
								{
									if (inserted.TaxAmount != null)
									{
										inserted.TaxAmount.Value += tax.TaxAmount;
									}
								}
							}
						}
					}
				}

				if (impl.TaxDetails?.Count > 0)
				{
					impl.FinancialSettings.OverrideTaxZone = existing.Local.FinancialSettings.OverrideTaxZone;
					impl.FinancialSettings.CustomerTaxZone = existing.Local.FinancialSettings.CustomerTaxZone;
				}

				String[] tooLongTaxIDs = ((impl.TaxDetails ?? new List<TaxDetail>()).Select(x => x.TaxID?.Value).Where(x => (x?.Length ?? 0) > PX.Objects.TX.Tax.taxID.Length).ToArray());
				if (tooLongTaxIDs != null && tooLongTaxIDs.Length > 0)
				{
					throw new PXException(PX.Commerce.Objects.BCObjectsMessages.CannotFindSaveTaxIDs, String.Join(",", tooLongTaxIDs), PX.Objects.TX.Tax.taxID.Length);
				}


				decimal? totalDiscount = 0m;
				if (data.RefundLineItems?.Count > 0)
				{
					totalDiscount = AddSOLine(bucket, impl, data, existing, branch, presentCROrder);
				}

				#region Discounts
				if (GetBindingExt<BCBindingExt>()?.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount && totalDiscount > 0)
				{
					impl.DisableAutomaticDiscountUpdate = true.ValueField();
					impl.DiscountDetails = new List<SalesOrdersDiscountDetails>();

					SalesOrdersDiscountDetails discountDetail = new SalesOrdersDiscountDetails();
					discountDetail.Type = PX.Objects.Common.Discount.DiscountType.ExternalDocument.ValueField();
					discountDetail.DiscountAmount = totalDiscount.ValueField();
					discountDetail.Description = ShopifyMessages.RefundDiscount.ValueField();
					discountDetail.ExternalDiscountCode = ShopifyMessages.RefundDiscount.ValueField();
					impl.DiscountDetails.Add(discountDetail);
					if (presentCROrder != null)
					{
						presentCROrder.DiscountDetails?.ForEach(e => impl.DiscountDetails?.FirstOrDefault(n => n.ExternalDiscountCode.Value == e.ExternalDiscountCode.Value).With(n => n.Id = e.Id));
						impl.DiscountDetails?.AddRange(presentCROrder.DiscountDetails == null ? Enumerable.Empty<SalesOrdersDiscountDetails>()
					: presentCROrder.DiscountDetails.Where(e => impl.DiscountDetails == null || !impl.DiscountDetails.Any(n => e.Id == n.Id)).Select(n => new SalesOrdersDiscountDetails() { Id = n.Id, Delete = true })); ;
					}
				}
				#endregion

			}
		}

		public virtual void CreateRefundPayment(SPRefundsBucket bucket, MappedRefunds existing)
		{
			SalesOrder impl = bucket.Refunds.Local;
			OrderData orderData = bucket.Refunds.Extern;
			List<OrderRefund> refunds = orderData.Refunds;
			impl.Payment = new List<Payment>();
			var branch = PX.Objects.GL.Branch.PK.Find(this, currentBinding.BranchID)?.BranchCD?.ValueField();
			IEnumerable<PXResult<PX.Objects.AR.ARPayment, BCSyncStatus>> result = PXSelectJoin<PX.Objects.AR.ARPayment,
					InnerJoin<BCSyncStatus, On<PX.Objects.AR.ARPayment.noteID, Equal<BCSyncStatus.localID>>>,
					Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						And<BCSyncStatus.parentSyncID, Equal<Required<BCSyncStatus.parentSyncID>>
					>>>>>.Select(this, BCEntitiesAttribute.Payment, bucket.Refunds.ParentID).Cast<PXResult<PX.Objects.AR.ARPayment, BCSyncStatus>>();


			int refundsCount = refunds.Count(x => x.Transactions.Any(y => y.Status == TransactionStatus.Success));

			foreach (var refund in refunds)
			{

				foreach (var transaction in refund.Transactions)
				{
					if (transaction?.Status == TransactionStatus.Success)
					{
						var origPayment = orderData.Transactions.FirstOrDefault(x => x.Id == transaction.ParentId);
						Payment refundPayment = new Payment();
						BCPaymentMethods currentPayment = null;
						ARPayment arPayment = null;
						refundPayment.DocumentsToApply = new List<Core.API.PaymentDetail>();
						refundPayment.TransactionID = new object[] { refund.Id, transaction.Id.ToString() }.KeyCombine();
						var ccrefundTransactions = orderData.Transactions.Where(x => (x.Kind == TransactionType.Refund || x.Kind == TransactionType.Void) && x.Authorization != null && x.Status == TransactionStatus.Success);
						if ((orderData.FinancialStatus == OrderFinancialStatus.Refunded || orderData.FinancialStatus == OrderFinancialStatus.Voided) && (refundsCount == 1 && ccrefundTransactions?.Count() == 1)
							&& ((existing.Local?.Status?.Value != PX.Objects.SO.Messages.Completed) || (existing.Local?.Status?.Value == PX.Objects.SO.Messages.Completed && transaction.Kind == TransactionType.Void)) && origPayment.Authorization != null)
						{
							/*call voidCardPayment Action
							 * Incase fully refunded and open AC order with authorize/Captured(settled/unsettled) CC type payment or
							 * Incase fully refunded and completed AC order with authorize cctype payment
							*/
							refundPayment.Type = PX.Objects.AR.Messages.Prepayment.ValueField();
							currentPayment = helper.GetPaymentMethodMapping(transaction.Gateway, transaction.Currency, out String cashAcount, false);
							if (currentPayment?.ProcessRefunds != true)
							{
								LogInfo(Operation.LogScope(bucket.Refunds.SyncID), BCMessages.LogRefundPaymentSkipped, orderData.Id, refund.Id, transaction.Id, transaction.Gateway);
								continue; // void payment if only ProcessRefunds is checked
							}
							var parentID = (origPayment.Kind == TransactionType.Capture && origPayment.ParentId != null) ? origPayment.ParentId : transaction.ParentId;// to handle seperate capture transaction
							arPayment = result.FirstOrDefault(x => x.GetItem<BCSyncStatus>()?.ExternID.KeySplit(1) == parentID.ToString())?.GetItem<ARPayment>();
							if (transaction.Kind == TransactionType.Refund)
							{
								if (arPayment == null) throw new PXException(BCMessages.OriginalPaymentNotImported, parentID.ToString(), orderData.Id.ToString());
								if (arPayment?.Released != true) throw new PXException(BCMessages.OriginalPaymentNotReleased, parentID.ToString(), orderData.Id.ToString());
								if (existing != null)
								{
									PopulateNoteID(existing, refundPayment, ARPaymentType.VoidPayment, arPayment.RefNbr);
									if (refundPayment.NoteID != null)
									{
										impl.Payment.Add(refundPayment);
										continue;
									}
								}

							}
							else
							{
								if (arPayment == null) throw new PXException(BCMessages.OriginalPaymentNotImported, parentID.ToString(), orderData.Id.ToString());
								if (arPayment.IsCCCaptured == true) throw new PXException(BCMessages.OriginalPaymentStatusMismatch, parentID.ToString(), orderData.Id.ToString());
								if (arPayment.Voided == true)
								{
									refundPayment.NoteID = arPayment.NoteID.ValueField();
									impl.Payment.Add(refundPayment);
									continue;
								}

							}
							refundPayment.ReferenceNbr = arPayment.RefNbr.ValueField();
							refundPayment.VoidCardParameters = new VoidCardPayment();
							if (ccrefundTransactions.FirstOrDefault()?.Kind == TransactionType.Void)
							{
								refundPayment.VoidCardParameters.TranType = CCTranTypeCode.VoidTran.ValueField();
								refundPayment.VoidCardParameters.TranNbr = helper.ParseTransactionNumber(origPayment, out bool isCreditCardTran).ValueField();
							}
							else
							{
								refundPayment.VoidCardParameters.TranType = CCTranTypeCode.Unknown.ValueField();
								refundPayment.VoidCardParameters.TranNbr = helper.ParseTransactionNumber(ccrefundTransactions.FirstOrDefault(), out bool isCreditCardTran).ValueField();
							}

							impl.Payment.Add(refundPayment);

						}
						else// create CR payment
						{
							refundPayment.ExternalRef = transaction.Id.ToString().ValueField();
							refundPayment.PaymentRef = helper.ParseTransactionNumber(transaction, out bool isCreditCardTran).ValueField();

							//check if existing CR Payment
							if (existing != null)
							{
								PopulateNoteID(existing, refundPayment, ARPaymentType.Refund, refundPayment.ExternalRef.Value);
								if (refundPayment.NoteID != null)
								{
									impl.Payment.Add(refundPayment);
									continue;
								}
							}

							//mapy summary section
							refundPayment.Type = PX.Objects.AR.Messages.Refund.ValueField();
							refundPayment.CustomerID = existing.Local.CustomerID;
							refundPayment.CustomerLocationID = existing.Local.LocationID;
							var date = refund.DateCreatedAt.ToDate(false, PXTimeZoneInfo.FindSystemTimeZoneById(GetBindingExt<BCBindingExt>().OrderTimeZone));
							if (date.HasValue)
								refundPayment.ApplicationDate = (new DateTime(date.Value.Date.Ticks)).ValueField();
							refundPayment.BranchID = branch;
							var description = PXMessages.LocalizeFormat(ShopifyMessages.PaymentRefundDescription, currentBinding.BindingName, orderData?.Name, refund.Id.ToString(), transaction.Kind.ToString(), transaction.Status?.ToString(), transaction.Gateway);
							refundPayment.Description = description.ValueField();

							refundPayment.PaymentAmount = (transaction.Amount ?? 0).ValueField();

							//map paymentmethod
							currentPayment = helper.GetPaymentMethodMapping(transaction.Gateway, transaction.Currency, out String cashAcount, false);
							if (currentPayment?.ProcessRefunds != true)
							{
								LogInfo(Operation.LogScope(bucket.Refunds.SyncID), BCMessages.LogRefundPaymentSkipped, orderData.Id, refund.Id, transaction.Id, transaction.Gateway);
								continue; // create CR payment if only ProcessRefunds is checked
							}
							var parentID = (origPayment.Kind == TransactionType.Capture && origPayment.ParentId != null) ? origPayment.ParentId : transaction.ParentId;// to handle seperate capture transaction
							arPayment = result.FirstOrDefault(x => x.GetItem<BCSyncStatus>().ExternID.KeySplit(1) == parentID.ToString());
							if (currentPayment?.ProcessingCenterID != null && isCreditCardTran)
							{
								helper.AddCreditCardProcessingInfo(currentPayment, refundPayment, transaction.Kind);
								if (arPayment?.IsCCPayment == true)
								{
									refundPayment.OrigTransaction = ExternalTransaction.PK.Find(this, arPayment?.CCActualExternalTransactionID)?.TranNumber.ValueField();
								}
							}

							refundPayment.CashAccount = cashAcount?.Trim()?.ValueField();
							refundPayment.PaymentMethod = currentPayment?.PaymentMethodID?.ValueField();

							if (existing.Local?.Status?.Value == PX.Objects.SO.Messages.Completed) // do not apply payment just create in on hold status
							{
								refundPayment.Hold = true.ValueField();
								refundPayment.PaymentAmount = transaction.Amount.ValueField();
							}
							else
							{

								if (arPayment == null) throw new PXException(BCMessages.OriginalPaymentNotImported, parentID.ToString(), orderData.Id.ToString());
								if (arPayment?.Released != true) throw new PXException(BCMessages.OriginalPaymentNotReleased, parentID.ToString(), orderData.Id.ToString());
								ValidateCRPayment(arPayment?.RefNbr);
								Core.API.PaymentDetail paymentDetail = new Core.API.PaymentDetail();
								paymentDetail.ReferenceNbr = arPayment?.RefNbr.ValueField();
								paymentDetail.DocType = ARPaymentType.Prepayment.ValueField();
								paymentDetail.AmountPaid = (transaction.Amount ?? 0).ValueField();
								refundPayment.DocumentsToApply.Add(paymentDetail);
							}

							impl.Payment.Add(refundPayment);

						}
					}
				}
			}
		}

		public virtual void PopulateNoteID(MappedRefunds existing, Payment refundPayment, string docType, string reference)
		{
			if (existing?.Details?.Count() > 0)
			{
				existing?.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.Payment && d.ExternID == refundPayment.TransactionID).With(p => refundPayment.NoteID = p.LocalID.ValueField());
			}
			if (refundPayment.NoteID?.Value == null)
			{
				helper.GetExistingRefundPayment(refundPayment, docType, reference);

			}
		}

		public virtual void ValidateCRPayment(string adjgRefNbr)
		{

			var existinCRPayment = PXSelectJoin<PX.Objects.AR.ARPayment, InnerJoin<ARAdjust, On<ARPayment.refNbr, Equal<ARAdjust.adjgRefNbr>, And<ARAdjust.adjdRefNbr, Equal<Required<ARAdjust.adjdRefNbr>>>>>,
							Where<ARPayment.docType, Equal<Required<ARPayment.docType>>>>.Select(this, adjgRefNbr, ARPaymentType.Refund);
			if (existinCRPayment != null && existinCRPayment.Count > 0)
			{
				if (existinCRPayment.Any(x => x.GetItem<ARPayment>().Released == false))
					throw new PXException(BCMessages.UnreleasedCRPayment, adjgRefNbr, existinCRPayment.FirstOrDefault(x => x.GetItem<ARPayment>().Released == false).GetItem<ARPayment>().RefNbr);
			}
		}

		public virtual decimal? AddSOLine(SPRefundsBucket bucket, SalesOrder impl, OrderRefund data, MappedRefunds existing, StringValue branch, SalesOrder presentCROrder)
		{
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			decimal? totalDiscount = 0m;
			foreach (var item in data.RefundLineItems)
			{
				SalesOrderDetail detail = new SalesOrderDetail();
				String inventoryCD = helper.GetInventoryCDByExternID(
					item.OrderLineItem.ProductId?.ToString(),
					item.OrderLineItem.VariantId.ToString(),
					item.OrderLineItem.Sku,
					item.OrderLineItem.Name,
					item.OrderLineItem.IsGiftCard,
					out string uom);
				if (item.OrderLineItem.DiscountAllocations?.Count > 0)
				{
					var itemDiscount = item.OrderLineItem.DiscountAllocations.Sum(x => x.DiscountAmount);

					itemDiscount = itemDiscount + item.SubTotal - (item.OrderLineItem.Price * item.Quantity);
					totalDiscount += itemDiscount;
					if (bindingExt?.PostDiscounts == BCPostDiscountAttribute.LineDiscount)
					{
						detail.DiscountAmount = itemDiscount.ValueField();
					}
					else
					{
						detail.DiscountAmount = 0m.ValueField();
					}

				}

				if (string.IsNullOrWhiteSpace(bindingExt.ReasonCode))
					throw new PXException(ShopifyMessages.ReasonCodeRequired);

				detail.Branch = branch;
				detail.InventoryID = inventoryCD?.TrimEnd().ValueField();
				detail.OrderQty = ((decimal)item.Quantity).ValueField();
				detail.UOM = uom.ValueField();
				detail.UnitPrice = item.OrderLineItem.Price.ValueField();
				detail.ManualPrice = true.ValueField();
				detail.ReasonCode = bindingExt.ReasonCode?.ValueField();
				detail.ExternalRef = item.Id.ToString().ValueField();
				impl.Details.Add(detail);
				DetailInfo matchedDetail = existing?.Details?.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && item.Id.ToString() == d.ExternID.KeySplit(1) && data.Id.ToString() == d.ExternID.KeySplit(0));
				if (matchedDetail != null) detail.Id = matchedDetail.LocalID; //Search by Details
				else if (presentCROrder?.Details != null && presentCROrder.Details.Count > 0) //Serach by Existing line
				{
					SalesOrderDetail matchedLine = presentCROrder.Details.FirstOrDefault(x =>
						(x.ExternalRef?.Value != null && x.ExternalRef?.Value == item.Id.ToString())
						||
						(x.InventoryID?.Value == detail.InventoryID?.Value && (detail.UOM == null || detail.UOM.Value == x.UOM?.Value)));
					if (matchedLine != null) detail.Id = matchedLine.Id;
				}
			}

			return totalDiscount;
		}

		public override void SaveBucketImport(SPRefundsBucket bucket, IMappedEntity existing, string operation)
		{

			MappedRefunds obj = bucket.Refunds;
			// create CR payment and release it
			SalesOrder order = obj.Local;

			try
			{
				obj.ClearDetails();
				if (order.Payment != null)
				{
					List<Tuple<string, string>> addedRefNbr = new List<Tuple<string, string>>();
					foreach (var payment in order.Payment)
					{
						Guid? localId = payment.NoteID?.Value;
						Payment paymentResp = null;

						if (payment.VoidCardParameters != null)
						{
							paymentResp = cbapi.Invoke<Payment, VoidCardPayment>(payment, action: payment.VoidCardParameters);
							localId = paymentResp.Id;
						}
						else
						{
							foreach (var detail in payment.DocumentsToApply)
							{
								if (addedRefNbr.Any(x => x.Item1 == detail.ReferenceNbr.Value))
								{
									throw new SetSyncStatusException(BCMessages.UnreleasedCRPayment, detail?.ReferenceNbr?.Value, addedRefNbr.FirstOrDefault(x => x.Item1 == detail.ReferenceNbr.Value).Item2);
								}

							}

							if (payment.NoteID?.Value == null)
							{
								paymentResp = cbapi.Put<Payment>(payment);
								localId = paymentResp?.Id;
								foreach (var detail in payment.DocumentsToApply)
								{
									addedRefNbr.Add(new Tuple<string, string>(detail.ReferenceNbr.Value, paymentResp.ReferenceNbr.Value));
								}
							}
						}
						if (!obj.Details.Any(x => x.LocalID == localId))
						{
							obj.AddDetail(BCEntitiesAttribute.Payment, localId, payment.TransactionID.ToString());

						}

					}
				}

				if (order.RefundOrders != null)
				{
					foreach (var refundOrder in order.RefundOrders)
					{
						var details = refundOrder.Details;
						var localID = refundOrder.Id;
						if (refundOrder.Id == null)
						{

							#region Taxes
							//Logging for taxes
							helper.LogTaxDetails(obj.SyncID, refundOrder);
							#endregion

							SalesOrder impl = cbapi.Put<SalesOrder>(refundOrder, localID);
							localID = impl.Id;
							details = impl.Details;
							#region Taxes
							helper.ValidateTaxes(obj.SyncID, impl, refundOrder);
							#endregion
						}
						if (!obj.Details.Any(x => x.LocalID == localID))
						{
							obj.AddDetail(BCEntitiesAttribute.CustomerRefundOrder, localID, refundOrder.RefundID);
						}
						if (details != null)
							foreach (var lineitem in details)
							{
								if (!obj.Details.Any(x => x.LocalID == lineitem.Id))
								{
									if (lineitem.InventoryID.Value.Trim() == refundItem.InventoryCD.Trim())
										continue;
									else
									{
										var detail = obj.Extern.Refunds.FirstOrDefault(x => x.Id.ToString() == refundOrder.RefundID).RefundLineItems.FirstOrDefault(x => !obj.Details.Any(o => x.Id.ToString() == o.ExternID)
									 && x.OrderLineItem.Sku == lineitem.InventoryID.Value);
										if (detail != null)
											obj.AddDetail(BCEntitiesAttribute.OrderLine, lineitem.Id, new object[] { refundOrder.RefundID, detail.Id }.KeyCombine());
										else
											throw new PXException(BCMessages.CannotMapLines);
									}
								}

							}
					}

				}

				UpdateStatus(obj, operation);

				if (order.EditSO)
				{
					bucket.Order.ExternTimeStamp = DateTime.MaxValue;
					EnsureStatus(bucket.Order, SyncDirection.Import, false);
				}
				else
					bucket.Order = null;
			}
			catch (SetSyncStatusException)
			{
				throw;
			}
			catch
			{

				throw;
			}
		}

		#endregion
	}
}
