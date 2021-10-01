using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PX.Commerce.Shopify.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Common;
using PX.Data.BQL;
using PX.Objects.SO;
using PX.Objects.AR;
using PX.Objects.GL;
using PX.Objects.Common;
using Newtonsoft.Json;
using PX.Data.PushNotifications;
using PX.Objects.CA;

namespace PX.Commerce.Shopify
{
	public class SPPaymentEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Payment;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };

		public MappedPayment Payment;
		public MappedOrder Order;
	}

	public class SPPaymentsRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			return null;
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			#region Payments
			return base.Restrict<MappedPayment>(mapped, delegate (MappedPayment obj)
			{
				if (obj.Extern != null)
				{
					if (obj.Extern.Kind == TransactionType.Void)
					{
						// we should skip void payments
						return new FilterResult(FilterStatus.Ignore,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedVoid, obj.Extern.Id));
					}

					if (string.IsNullOrWhiteSpace(obj.Extern.Gateway))
					{
						// we should skip payments without the payment gateway
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedMethodEmpty, obj.Extern.Id));
					}

					if (obj.Extern.Kind == TransactionType.Refund)
					{
						// we should skip refund payments now, and we will support later
						return new FilterResult(FilterStatus.Ignore,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedMethodNotSupported, obj.Extern.Id, TransactionType.Refund.ToString()));
					}

					if (obj.Extern.Kind == TransactionType.Capture)
					{
						// we should skip Capture to avoid the duplicated processing
						return new FilterResult(FilterStatus.Ignore,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedMethodNotSupported, obj.Extern.Id, TransactionType.Capture.ToString()));
					}

					if (obj.Extern.ParentId != null && processor.SelectStatus(BCEntitiesAttribute.Payment, new Object[] { obj.Extern.OrderId, obj.Extern.ParentId }.KeyCombine()) != null)
					{
						return new FilterResult(FilterStatus.Ignore,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedParentSynced, obj.Extern.Id, obj.Extern.ParentId));
					}

					if (obj.Extern.Status != TransactionStatus.Success)
					{
						// we should skip payments with error
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedError, obj.Extern.Id));
					}

					IEnumerable<BCPaymentMethods> paymentMethod = PXSelectReadonly<BCPaymentMethods,
										 Where<BCPaymentMethods.bindingID, Equal<Required<BCPaymentMethods.bindingID>>>>
										.Select((PXGraph)processor, processor.Operation.Binding).Select(x => x.GetItem<BCPaymentMethods>())
										.ToList().Where(x => x.StorePaymentMethod == obj.Extern.Gateway.ToUpper());
					if (paymentMethod != null && paymentMethod.Count() > 0 && paymentMethod.All(x => x.Active != true))
					{
						//skip if active is not true
						return new FilterResult(FilterStatus.Filtered,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedNotConfigured, obj.Extern.Id, obj.Extern.Gateway));
					}
				}

				if (processor.SelectStatus(BCEntitiesAttribute.Order, obj.Extern?.OrderId.ToString(), false) == null)
				{
					//Skip if order not synced. Bypass order 
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedOrderNotSynced, obj.Extern.Id, obj.Extern.OrderId));
				}

				return null;
			});
			#endregion
		}
	}

	[BCProcessor(typeof(SPConnector), BCEntitiesAttribute.Payment, BCCaptions.Payment,
		IsInternal = false,
		Direction = SyncDirection.Import,
		PrimaryDirection = SyncDirection.Import,
		PrimarySystem = PrimarySystem.Extern,
		PrimaryGraph = typeof(PX.Objects.AR.ARPaymentEntry),
		ExternTypes = new Type[] { typeof(OrderTransaction) },
		LocalTypes = new Type[] { typeof(Payment) },
		AcumaticaPrimaryType = typeof(PX.Objects.AR.ARPayment),
		AcumaticaPrimarySelect = typeof(Search<PX.Objects.AR.ARPayment.refNbr, Where<PX.Objects.AR.ARPayment.docType, Equal<ARDocType.payment>>>),
		URL = "orders/{0}",
		Requires = new string[] { BCEntitiesAttribute.Order })]
	[BCProcessorRealtime(PushSupported = false, HookSupported = false)]
	public class SPPaymentProcessor : BCProcessorSingleBase<SPPaymentProcessor, SPPaymentEntityBucket, MappedPayment>, IProcessor
	{
		public SPHelper helper = PXGraph.CreateInstance<SPHelper>();

		protected OrderRestDataProvider orderDataProvider;
		protected BCBinding currentBinding;
		protected BCBindingExt currentBindingExt;
		protected BCBindingShopify currentShopifySettings;

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			currentBinding = GetBinding();
			currentBindingExt = GetBindingExt<BCBindingExt>();
			currentShopifySettings = GetBindingExt<BCBindingShopify>();

			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());
			orderDataProvider = new OrderRestDataProvider(client);

			helper.Initialize(this);
		}

		public override void WithTransaction(Action action)
		{
			action();
		}
		#endregion

		public override IEnumerable<MappedPayment> PullSimilar(IExternEntity entity, out string uniqueField)
		{
			OrderTransaction externEntity = (OrderTransaction)entity;

			uniqueField = externEntity.Id.ToString();

			if (string.IsNullOrEmpty(uniqueField))
				return null;

			List<MappedPayment> result = new List<MappedPayment>();
			foreach (PX.Objects.AR.ARRegister item in helper.PaymentByExternalRef.Select(uniqueField))
			{
				Payment data = new Payment() { SyncID = item.NoteID, SyncTime = item.LastModifiedDateTime };
				result.Add(new MappedPayment(data, data.SyncID, data.SyncTime));
			}
			return result;
		}

		public override void ControlDirection(SPPaymentEntityBucket bucket, BCSyncStatus status, ref bool shouldImport, ref bool shouldExport, ref bool skipSync, ref bool skipForce)
		{
			MappedPayment payment = bucket.Payment;
			if (!payment.IsNew)
				if (payment.Local?.Status?.Value == PX.Objects.AR.Messages.Voided)
				{
					shouldImport = false;
					skipForce = true;// if payment is already voided cannot make any changes to it so skip force.
					skipSync = true;
					UpdateStatus(payment, status.LastOperation);// to update extern hash in case of Shopify payment if its voided or captured in acumatica
				}
				else if (payment.Local?.Status?.Value != PX.Objects.AR.Messages.CCHold && payment.Extern.Action == TransactionType.Capture)
				{
					shouldImport = false;
					skipSync = true;
					skipForce = true;// if payment is not cchold then it is already capture so skip force sync
					UpdateStatus(payment, status.LastOperation);// to update extern hash in case Shopify payment if its voided or captured in acumatica
				}
		}

		#region Pull
		public override MappedPayment PullEntity(Guid? localID, Dictionary<string, object> fields)
		{
			Payment impl = cbapi.GetByID<Payment>(localID);
			if (impl == null) return null;

			MappedPayment obj = new MappedPayment(impl, impl.SyncID, impl.SyncTime);

			return obj;
		}
		public override MappedPayment PullEntity(String externID, String jsonObject)
		{
			var data = orderDataProvider.GetOrderSingleTransaction(externID.KeySplit(0), externID.KeySplit(1));
			if (data == null) return null;

			MappedPayment obj = new MappedPayment(data, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateModifiedAt.ToDate(false), data.CalculateHash());

			return obj;
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			var minDate = minDateTime == null || (minDateTime != null && currentBindingExt.SyncOrdersFrom != null && minDateTime < currentBindingExt.SyncOrdersFrom) ? currentBindingExt.SyncOrdersFrom : minDateTime;
			FilterOrders filter = new FilterOrders { Status = OrderStatus.Any, Fields = "id,source_name,financial_status,updated_at,created_at,cancelled_at,closed_at" };
			if (minDate != null) filter.UpdatedAtMin = minDate.Value.ToLocalTime().AddSeconds(-GetBindingExt<BCBindingShopify>().ApiDelaySeconds ?? 0);
			if (maxDateTime != null) filter.UpdatedAtMax = maxDateTime.Value.ToLocalTime();

			IEnumerable<OrderData> datas = orderDataProvider.GetAll(filter);

			foreach (OrderData orderData in datas)
			{
				if (this.SelectStatus(BCEntitiesAttribute.Order, orderData.Id.ToString()) == null)
					continue; //Skip if order not synced

				var transactionList = orderDataProvider.GetOrderTransactions(orderData.Id.ToString());
				if (transactionList == null || transactionList.Count == 0) continue;
				//Only process the successful Transaction
				foreach (OrderTransaction data in transactionList)
				{
					if ((data.Status == TransactionStatus.Success && data.Kind == TransactionType.Sale &&
						transactionList.Any(x => x.ParentId == data.Id && x.Status == TransactionStatus.Success && x.Kind == TransactionType.Refund && x.Amount == data.Amount &&
						x.ProcessedAt.HasValue && x.ProcessedAt.Value.Subtract(data.ProcessedAt.Value).TotalSeconds < 10)) ||
						(data.Status == TransactionStatus.Success && data.Kind == TransactionType.Refund && data.ParentId != null &&
						transactionList.Any(x => x.Id == data.ParentId && x.Status == TransactionStatus.Success && x.Kind == TransactionType.Sale && x.Amount == data.Amount &&
						data.ProcessedAt.HasValue && data.ProcessedAt.Value.Subtract(x.ProcessedAt.Value).TotalSeconds < 10)))
					{
						//Skip successful payment and its refund payment to avoid payment amount greater than order total amount issue if another payment is unsuccessful and system rolls back all payments 
						continue;
					}
					SPPaymentEntityBucket bucket = CreateBucket();

					MappedOrder order = bucket.Order = bucket.Order.Set(orderData, orderData.Id?.ToString(), orderData.DateModifiedAt.ToDate(false));
					EntityStatus orderStatus = EnsureStatus(order);

					helper.PopulateAction(transactionList, data);
					MappedPayment obj = bucket.Payment = bucket.Payment.Set(data, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateModifiedAt.ToDate(false), data.CalculateHash()).With(_ => { _.ParentID = order.SyncID; return _; });
					EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
				}
			}
		}
		public override EntityStatus GetBucketForImport(SPPaymentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			OrderData orderData = BCExtensions.GetSharedSlot< OrderData>(syncstatus.ExternID.KeySplit(0)) ?? orderDataProvider.GetByID(syncstatus.ExternID.KeySplit(0), false, true, false);
			if (orderData == null || orderData.Transactions == null || !orderData.Transactions.Any(x => x?.Id.ToString() == syncstatus.ExternID.KeySplit(1)))
				return EntityStatus.None;

			OrderTransaction data = orderData.Transactions.FirstOrDefault(x => x?.Id.ToString() == syncstatus.ExternID.KeySplit(1));
			TransactionType lastKind = helper.PopulateAction(orderData.Transactions, data);

			MappedPayment obj = bucket.Payment = bucket.Payment.Set(data, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateModifiedAt.ToDate(false), data.CalculateHash());
			EntityStatus status = EnsureStatus(obj, SyncDirection.Import);

			//Used to determine transaction type in case of credit card
			data.LastKind = lastKind;

			MappedOrder order = bucket.Order = bucket.Order.Set(orderData, orderData.Id?.ToString(), orderData.DateModifiedAt.ToDate(false));
			EntityStatus orderStatus = EnsureStatus(order);

			return status;
		}



		public override void MapBucketImport(SPPaymentEntityBucket bucket, IMappedEntity existing)
		{
			MappedPayment obj = bucket.Payment;

			OrderTransaction data = obj.Extern;
			Payment impl = obj.Local = new Payment();
			Payment presented = existing?.Local as Payment;

			PXResult<PX.Objects.SO.SOOrder, PX.Objects.AR.Customer, PX.Objects.CR.Location, BCSyncStatus> result = PXSelectJoin<PX.Objects.SO.SOOrder,
				InnerJoin<PX.Objects.AR.Customer, On<PX.Objects.AR.Customer.bAccountID, Equal<SOOrder.customerID>>,
				InnerJoin<PX.Objects.CR.Location, On<PX.Objects.CR.Location.locationID, Equal<SOOrder.customerLocationID>>,
				InnerJoin<BCSyncStatus, On<PX.Objects.SO.SOOrder.noteID, Equal<BCSyncStatus.localID>>>>>,
				Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
					And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
					And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
					And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>
				.Select(this, BCEntitiesAttribute.Order, data.OrderId).Select(r => (PXResult<SOOrder, PX.Objects.AR.Customer, PX.Objects.CR.Location, BCSyncStatus>)r).FirstOrDefault();
			if (result == null) throw new PXException(BCMessages.OrderNotSyncronized, data.OrderId);
			PX.Objects.SO.SOOrder order = result.GetItem<PX.Objects.SO.SOOrder>();
			PX.Objects.AR.Customer customer = result.GetItem<PX.Objects.AR.Customer>();
			PX.Objects.CR.Location location = result.GetItem<PX.Objects.CR.Location>();

			//if payment already exists and then no need to map fields just call action
			if (presented?.Id != null && (obj.Extern.Action == TransactionType.Void || obj.Extern.Action == TransactionType.Capture)) return;

			impl.Type = PX.Objects.AP.Messages.Prepayment.ValueField();
			impl.CustomerID = customer.AcctCD.ValueField();
			impl.CustomerLocationID = location.LocationCD.ValueField();
			impl.CurrencyID = data.Currency.ValueField();
			var date = data.DateCreatedAt.ToDate(false, PXTimeZoneInfo.FindSystemTimeZoneById(currentBindingExt.OrderTimeZone));
			if (date.HasValue)
				impl.ApplicationDate = (new DateTime(date.Value.Date.Ticks)).ValueField();
			impl.PaymentAmount = ((decimal)data.Amount).ValueField();
			impl.BranchID = Branch.PK.Find(this, currentBinding.BranchID)?.BranchCD?.ValueField();
			impl.Hold = false.ValueField();

			BCPaymentMethods methodMapping = helper.GetPaymentMethodMapping(data.Gateway, data.Currency, out string cashAcount);
			if (((MappedPayment)existing)?.Local != null && ((MappedPayment)existing)?.Local.PaymentMethod != methodMapping?.PaymentMethodID?.Trim()?.ValueField())
				impl.PaymentMethod = ((MappedPayment)existing)?.Local.PaymentMethod;
			else
				impl.PaymentMethod = methodMapping?.PaymentMethodID?.Trim()?.ValueField();
			impl.CashAccount = cashAcount?.Trim()?.ValueField();
			impl.NeedRelease = methodMapping?.ReleasePayments ?? false;
			impl.ExternalRef = data.Id.ToString().ValueField();

			impl.PaymentRef = helper.ParseTransactionNumber(data, out bool isCreditCardTransaction).ValueField();

			TransactionStatus? lastStatus = bucket.Order.Extern.Transactions.LastOrDefault(x => x.ParentId == data.Id && x.Status == TransactionStatus.Success)?.Status ?? data.Status;

			var paymentDesc = PXMessages.LocalizeFormat(ShopifyMessages.PaymentDescription, currentBinding.BindingName, bucket.Order?.Extern?.Name, data.Kind.ToString(), lastStatus?.ToString(), data.Gateway);
			impl.Description = paymentDesc.ValueField();

			//Credit Card:
			if (methodMapping?.ProcessingCenterID != null && isCreditCardTransaction)
			{
				helper.AddCreditCardProcessingInfo(methodMapping, impl, data.LastKind);
			}

			//Calculated Unpaid Balance
			decimal curyUnpaidBalance = order.CuryOrderTotal ?? 0m;
			foreach (SOAdjust adj in PXSelect<SOAdjust,
							Where<SOAdjust.adjdOrderType, Equal<Required<SOOrder.orderType>>,
								And<SOAdjust.adjdOrderNbr, Equal<Required<SOOrder.orderNbr>>>>>.Select(this, order.OrderType, order.OrderNbr))
			{
				curyUnpaidBalance -= adj.CuryAdjdAmt ?? 0m;
			}

			//If we have applied already, than skip
			if ((existing as MappedPayment) == null || ((MappedPayment)existing).Local == null ||
				((MappedPayment)existing).Local.OrdersToApply == null || !((MappedPayment)existing).Local.OrdersToApply.Any(d => d.OrderType?.Value == order.OrderType && d.OrderNbr?.Value == order.OrderNbr))
			{
				decimal applicationAmount = 0m;
				if (bucket.Order.Extern.FinancialStatus != OrderFinancialStatus.Refunded)
					applicationAmount = (decimal)data.Amount > curyUnpaidBalance ? curyUnpaidBalance : (decimal)data.Amount;

				//Order to Apply
				PaymentOrderDetail detail = new PaymentOrderDetail();
				detail.OrderType = order.OrderType.ValueField();
				detail.OrderNbr = order.OrderNbr.ValueField();
				detail.AppliedToOrder = applicationAmount.ValueField();
				impl.OrdersToApply = new List<PaymentOrderDetail>(new[] { detail });
			}
		}

		public override void SaveBucketImport(SPPaymentEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedPayment obj = bucket.Payment;
			Boolean needRelease = obj.Local.NeedRelease;

			BCSyncStatus orderStatus = PXSelectJoin<BCSyncStatus,
				InnerJoin<SOOrder, On<SOOrder.noteID, Equal<BCSyncStatus.localID>,
					And<SOOrder.lastModifiedDateTime, Equal<BCSyncStatus.localTS>>>>,
				Where<BCSyncStatus.syncID, Equal<Required<BCSyncStatus.syncID>>>>.Select(this, bucket.Order.SyncID);
			
			Payment impl = null;
			WithTransaction(delegate ()
			{
				if (obj.Extern.Action == TransactionType.Void && existing?.Local != null)
				{
					impl = VoidTransaction(existing.Local as Payment, obj);
				}
				else if (obj.Extern.Action == TransactionType.Capture && existing?.Local != null)
				{
					impl = cbapi.Invoke<Payment, CardOperation>(existing.Local as Payment, action: new CardOperation()
					{
						TranType = CCTranTypeCode.PriorAuthorizedCapture.ValueField(),
						TranNbr = helper.ParseTransactionNumber(obj.Extern, out bool isCreditCardTran).ValueField()
					});
					bucket.Payment.AddLocal(null, obj.LocalID, impl.SyncTime);
				}
				else
				{
					impl = cbapi.Put<Payment>(obj.Local, obj.LocalID);
					bucket.Payment.AddLocal(impl, impl.SyncID, impl.SyncTime);

					if (obj.Extern.Action == TransactionType.Void)// need to call action as cannot create void payment directly
						impl = VoidTransaction(impl, obj);
				}
			});

			if (needRelease && impl.Status?.Value == PX.Objects.AR.Messages.Balanced)
			{
				try
				{
					impl = cbapi.Invoke<Payment, ReleasePayment>(null, obj.LocalID, ignoreResult: !WebConfig.ParallelProcessingDisabled);
					if (impl != null) bucket.Payment.AddLocal(null, impl.SyncID, impl.SyncTime);
				}
				catch (Exception ex) { LogError(Operation.LogScope(obj), ex); }
			}

			UpdateStatus(obj, operation);
			if (orderStatus?.LocalID != null) //Payment save updates the order, we need to change the saved timestamp.
			{
				orderStatus.LocalTS = BCSyncExactTimeAttribute.SelectDateTime<SOOrder.lastModifiedDateTime>(orderStatus.LocalID.Value);
				orderStatus = (BCSyncStatus)Statuses.Cache.Update(orderStatus);
			}
		}

		public virtual Payment VoidTransaction(Payment payment, MappedPayment obj)
		{
			Payment impl = cbapi.Invoke<Payment, VoidCardPayment>(payment, action: new VoidCardPayment()
			{
				TranType = CCTranTypeCode.VoidTran.ValueField(),
				TranNbr = helper.ParseTransactionNumber(obj.Extern, out bool isCreditCardTran).ValueField()
			});
			obj.AddLocal(null, impl.SyncID, impl.SyncTime);
			return impl;
		}
		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}
		public override EntityStatus GetBucketForExport(SPPaymentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			Payment impl = cbapi.GetByID<Payment>(syncstatus.LocalID);
			if (impl == null) return EntityStatus.None;

			MappedPayment obj = bucket.Payment = bucket.Payment.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(bucket.Payment, SyncDirection.Export);

			return status;
		}

		public override void MapBucketExport(SPPaymentEntityBucket bucket, IMappedEntity existing)
		{
		}
		public override void SaveBucketExport(SPPaymentEntityBucket bucket, IMappedEntity existing, String operation)
		{
		}
		#endregion
	}
}
