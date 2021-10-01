using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.GL;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce
{
	public class BCPaymentEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Payment;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };

		public MappedPayment Payment;
		public MappedOrder Order;
	}

	public class BCPaymentsRestrictor : BCBaseRestrictor, IRestrictor
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
					if (obj.Extern.Event != OrderPaymentEvent.Authorization && obj.Extern.Event != OrderPaymentEvent.Purchase && obj.Extern.Event != OrderPaymentEvent.Pending)
					{
						// we should skip payment transactions except Authorized or Purchase
						return new FilterResult(FilterStatus.Ignore,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedEventNotSupported, obj.Extern.Id, obj.Extern.Event));
					}

					if (obj.Extern.Status == OrderPaymentStatus.Error)
					{
						// we should skip payments with error
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedError, obj.Extern.Id));
					}

					if (String.IsNullOrEmpty(obj.Extern.PaymentMethod))
					{
						// we should skip custom payments
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedMethodEmpty, obj.Extern.Id));
					}
					var paymentMethods = PXSelectReadonly<BCPaymentMethods,
										 Where<BCPaymentMethods.bindingID, Equal<Required<BCPaymentMethods.bindingID>>>>
										.Select((PXGraph)processor, processor.Operation.Binding).Select(x => x.GetItem<BCPaymentMethods>())
										.ToList();
					string method;
					IEnumerable<BCPaymentMethods> paymentMethod = null;
					if (obj.Extern.PaymentMethod == BCConstants.Emulated)
					{
						method = obj.Extern.Gateway;
						paymentMethod = paymentMethods.Where(x => x.StorePaymentMethod == method?.ToUpper());
					}
					else
					{
						method = string.Format("{0} ({1})", obj.Extern.Gateway, obj.Extern.PaymentMethod);
						paymentMethod = paymentMethods.Where(x => x.StorePaymentMethod == method.ToUpper() && x.StoreOrderPaymentMethod?.ToUpper() == obj.Extern.OrderPaymentMethod?.ToUpper());
						if (paymentMethod == null || paymentMethod?.Count() == 0)
							paymentMethod = paymentMethods.Where(x => x.StorePaymentMethod == method.ToUpper());

					}
					//skip if payment method not present at all or ProcessPayment is not true
					if (paymentMethod != null && paymentMethod.Count() > 0 && paymentMethod.All(x => x.Active != true))
					{
						return new FilterResult(FilterStatus.Filtered,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedNotConfigured, obj.Extern.Id, obj.Extern.Gateway));
					}
				}

				if (processor.SelectStatus(BCEntitiesAttribute.Order, obj.Extern?.OrderId, false) == null)
				{
					//Skip if order not synced
					return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogPaymentSkippedOrderNotSynced, obj.Extern.Id, obj.Extern.OrderId));
				}

				return null;
			});

			#endregion
		}
	}

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.Payment, BCCaptions.Payment,
		IsInternal = false,
		Direction = SyncDirection.Import,
		PrimaryDirection = SyncDirection.Import,
		PrimarySystem = PrimarySystem.Extern,
		PrimaryGraph = typeof(PX.Objects.AR.ARPaymentEntry),
		ExternTypes = new Type[] { typeof(OrdersTransactionData) },
		LocalTypes = new Type[] { typeof(Payment) },
		AcumaticaPrimaryType = typeof(PX.Objects.AR.ARPayment),
		AcumaticaPrimarySelect = typeof(Search<PX.Objects.AR.ARPayment.refNbr, Where<PX.Objects.AR.ARPayment.docType, Equal<ARDocType.payment>>>),
		URL = "orders?keywords={0}&searchDeletedOrders=no",
		Requires = new string[] { BCEntitiesAttribute.Order })]
	[BCProcessorRealtime(PushSupported = false, HookSupported = false)]
	public class BCPaymentProcessor : BCProcessorSingleBase<BCPaymentProcessor, BCPaymentEntityBucket, MappedPayment>, IProcessor
	{
		protected OrderRestDataProvider orderDataProvider;
		protected IChildReadOnlyRestDataProvider<OrdersTransactionData> orderTransactionsRestDataProvider;

		public BCHelper helper = PXGraph.CreateInstance<BCHelper>();

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			var client = BCConnector.GetRestClient(GetBindingExt<BCBindingBigCommerce>());
			orderDataProvider = new OrderRestDataProvider(client);
			orderTransactionsRestDataProvider = new OrderTransactionsRestDataProvider(client);

			helper.Initialize(this);
		}

		public override void WithTransaction(Action action)
		{
			action();
		}
		#endregion

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
			OrdersTransactionData data = orderTransactionsRestDataProvider.GetByID(externID.KeySplit(1), externID.KeySplit(0));
			if (data == null) return null;

			MappedPayment obj = new MappedPayment(data, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedUT.ToDate(), data.CalculateHash());

			return obj;
		}
		public override IEnumerable<MappedPayment> PullSimilar(IExternEntity entity, out string uniqueField)
		{
			var externEntity = (OrdersTransactionData)entity;

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
		#endregion

		public override void ControlDirection(BCPaymentEntityBucket bucket, BCSyncStatus status, ref bool shouldImport, ref bool shouldExport, ref bool skipSync, ref bool skipForce)
		{
			MappedPayment payment = bucket.Payment;
			if (!payment.IsNew)
				if (payment.Local?.Status?.Value == PX.Objects.AR.Messages.Voided)
				{
					shouldImport = false;
					skipForce = true;// if payment is already voided cannot make any changes to it so skip force sync
					skipSync = true;
					UpdateStatus(payment, status.LastOperation);// if manually voided in acumattica
				}
				else if (payment.Local?.Status?.Value != PX.Objects.AR.Messages.CCHold && payment.Extern.Action == OrderPaymentEvent.Capture)
				{
					shouldImport = false;
					skipForce = true;// if payment is not cchold then it is already capture so skip force sync
					skipSync = true;
					UpdateStatus(payment, status.LastOperation);//if manually captured in acumatica
				}
		}

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			var minDate = minDateTime == null || (minDateTime != null && bindingExt.SyncOrdersFrom != null && minDateTime < bindingExt.SyncOrdersFrom) ? bindingExt.SyncOrdersFrom : minDateTime;
			FilterOrders filter = new FilterOrders { MinDateModified = minDate == null ? null : minDate, MaxDateModified = maxDateTime == null ? null : maxDateTime };

			IEnumerable<OrderData> orders = orderDataProvider.GetAll(filter);

			foreach (OrderData orderData in orders)
			{
				if (this.SelectStatus(BCEntitiesAttribute.Order, orderData.Id.ToString()) == null)
					continue; //Skip if order not synced

				var transactions = orderTransactionsRestDataProvider.Get(orderData.Id.ToString()) ?? new List<OrdersTransactionData>();
				foreach (OrdersTransactionData data in transactions)
				{
					BCPaymentEntityBucket bucket = CreateBucket();

					MappedOrder order = bucket.Order = bucket.Order.Set(orderData, orderData.Id?.ToString(), orderData.DateModifiedUT.ToDate());
					EntityStatus orderStatus = EnsureStatus(order);

					helper.PopulateAction(transactions, data);

					MappedPayment obj = bucket.Payment = bucket.Payment.Set(data, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedUT.ToDate(), data.CalculateHash()).With(_ => { _.ParentID = order.SyncID; return _; });
					EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
				}
				if (helper.CreatePaymentfromOrder(orderData.PaymentMethod))
				{
					OrdersTransactionData data = CreateOrderTransactionData(orderData);
					BCPaymentEntityBucket bucket = CreateBucket();

					MappedOrder order = bucket.Order = bucket.Order.Set(orderData, orderData.Id?.ToString(), orderData.DateModifiedUT.ToDate());
					EntityStatus orderStatus = EnsureStatus(order);

					MappedPayment obj = bucket.Payment = bucket.Payment.Set(data, data.Id.ToString(), orderData.DateModifiedUT.ToDate(), data.CalculateHash());
					EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
				}
			}
		}
		public override EntityStatus GetBucketForImport(BCPaymentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			if (syncstatus.ExternID.HasParent())
			{
				List<OrdersTransactionData> transactions = orderTransactionsRestDataProvider.Get(syncstatus.ExternID.KeySplit(0));

				OrdersTransactionData data = transactions.FirstOrDefault(x => x.Id.ToString() == syncstatus.ExternID.KeySplit(1));
				if (data == null) return EntityStatus.None;

				OrderData orderData = orderDataProvider.GetByID(data.OrderId);
				data.OrderPaymentMethod = orderData.PaymentMethod;
				OrderPaymentEvent lastEvent = helper.PopulateAction(transactions, data);

				MappedPayment obj = bucket.Payment = bucket.Payment.Set(data, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedUT.ToDate(), data.CalculateHash());
				EntityStatus status = EnsureStatus(obj, SyncDirection.Import);

				data.LastEvent = lastEvent;

				MappedOrder order = bucket.Order = bucket.Order.Set(orderData, orderData.Id?.ToString(), orderData.DateModifiedUT.ToDate());
				EntityStatus orderStatus = EnsureStatus(order);

				return status;
			}
			else
			{
				OrderData orderData = orderDataProvider.GetByID(syncstatus.ExternID);
				if (helper.CreatePaymentfromOrder(orderData.PaymentMethod))
				{
					OrdersTransactionData data = CreateOrderTransactionData(orderData);
					MappedPayment obj = bucket.Payment = bucket.Payment.Set(data, data.Id.ToString(), orderData.DateModifiedUT.ToDate(), data.CalculateHash());
					EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
					MappedOrder order = bucket.Order = bucket.Order.Set(orderData, orderData.Id?.ToString(), orderData.DateModifiedUT.ToDate());
					EntityStatus orderStatus = EnsureStatus(order);
					return status;
				}
				return EntityStatus.None;
			}
		}

		public override void MapBucketImport(BCPaymentEntityBucket bucket, IMappedEntity existing)
		{
			MappedPayment obj = bucket.Payment;

			OrdersTransactionData data = obj.Extern;
			Payment impl = obj.Local = new Payment();
			BCBinding binding = GetBinding();
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
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

			//if payment already exists and then no need to map fields just call action
			if (presented?.Id != null && (obj.Extern.Action == OrderPaymentEvent.Void || obj.Extern.Action == OrderPaymentEvent.Capture)) return;


			PX.Objects.SO.SOOrder order = result.GetItem<PX.Objects.SO.SOOrder>();
			PX.Objects.AR.Customer customer = result.GetItem<PX.Objects.AR.Customer>();
			PX.Objects.CR.Location location = result.GetItem<PX.Objects.CR.Location>();

			//Product
			impl.Type = PX.Objects.AR.Messages.Prepayment.ValueField();
			impl.CustomerID = customer.AcctCD.ValueField();
			impl.CustomerLocationID = location.LocationCD.ValueField();
			impl.CurrencyID = data.Currency.ValueField();
			var date = data.DateCreatedUT.ToDate(PXTimeZoneInfo.FindSystemTimeZoneById(bindingExt.OrderTimeZone));
			if (date.HasValue)
				impl.ApplicationDate = (new DateTime(date.Value.Date.Ticks)).ValueField();

			impl.PaymentAmount = ((decimal)data.Amount).ValueField();
			impl.BranchID = Branch.PK.Find(this, binding.BranchID)?.BranchCD?.ValueField();
			impl.Hold = false.ValueField();

			BCPaymentMethods methodMapping = helper.GetPaymentMethodMapping(helper.GetPaymentMethodName(data), bucket.Order?.Extern?.PaymentMethod, data.Currency, out string cashAcount);
			if (((MappedPayment)existing)?.Local != null && ((MappedPayment)existing)?.Local.PaymentMethod != methodMapping?.PaymentMethodID?.Trim()?.ValueField())
				impl.PaymentMethod = ((MappedPayment)existing)?.Local.PaymentMethod;
			else
				impl.PaymentMethod = methodMapping?.PaymentMethodID?.Trim()?.ValueField();
			impl.CashAccount = cashAcount?.Trim()?.ValueField();
			impl.NeedRelease = methodMapping?.ReleasePayments ?? false;

			if (methodMapping.StorePaymentMethod == BCObjectsConstants.GiftCertificateCode)
				impl.Description = PXMessages.LocalizeFormat(BigCommerceMessages.PaymentDescriptionGC, binding.BindingName, methodMapping.StorePaymentMethod, data.OrderId, data.Id, data.GiftCertificate?.Code).ValueField();
			else
				impl.Description = PXMessages.LocalizeFormat(BigCommerceMessages.PaymentDescription, binding.BindingName, methodMapping.StorePaymentMethod, data.OrderId, data.Id).ValueField();

			impl.ExternalRef = data.Id.ToString().ValueField();
			impl.PaymentRef = helper.ParseTransactionNumber(data, out bool isCreditCardTransaction).ValueField();

			//Credit Card:
			if (methodMapping?.ProcessingCenterID != null && isCreditCardTransaction)
			{
				helper.AddCreditCardProcessingInfo(methodMapping, impl, data.LastEvent, data.PaymentInstrumentToken);
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
				if (bucket.Order.Extern.StatusId != OrderStatuses.Refunded.GetHashCode())
					applicationAmount = (decimal)data.Amount > curyUnpaidBalance ? curyUnpaidBalance : (decimal)data.Amount;

				//Order to Apply
				PaymentOrderDetail detail = new PaymentOrderDetail();
				detail.OrderType = order.OrderType.ValueField();
				detail.OrderNbr = order.OrderNbr.ValueField();
				detail.AppliedToOrder = applicationAmount.ValueField();
				impl.OrdersToApply = new List<PaymentOrderDetail>(new[] { detail });
			}
		}
		public override void SaveBucketImport(BCPaymentEntityBucket bucket, IMappedEntity existing, String operation)
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
				if (obj.Extern.Action == OrderPaymentEvent.Void && existing?.Local != null)
				{
					impl = VoidTransaction(existing.Local as Payment, obj);
				}
				else if (obj.Extern.Action == OrderPaymentEvent.Capture && existing?.Local != null)
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

					if (obj.Extern.Action == OrderPaymentEvent.Void)// need to call action as cannot create void payment directly
						impl = VoidTransaction(impl, obj);
				}
			});

			if (needRelease && impl.Status?.Value == PX.Objects.AR.Messages.Balanced)
			{
				try
				{ 
					impl = cbapi.Invoke<Payment, ReleasePayment>(null, obj.LocalID, ignoreResult: !WebConfig.ParallelProcessingDisabled);
					bucket.Payment.AddLocal(null, obj.LocalID, impl.SyncTime);
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
			obj.AddLocal(null, obj.LocalID, impl.SyncTime);
			return impl;
		}

		public virtual OrdersTransactionData CreateOrderTransactionData(OrderData orderData)
		{
			OrdersTransactionData data = new OrdersTransactionData();
			data.Id = orderData.Id.Value;
			data.OrderId = orderData.Id.ToString();
			data.Gateway = orderData.PaymentMethod;
			data.Currency = orderData.CurrencyCode;
			data.DateCreatedUT = orderData.DateCreatedUT;
			data.PaymentMethod = BCConstants.Emulated;
			data.Amount = Convert.ToDouble(orderData.TotalIncludingTax);
			return data;
		}
		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}
		public override EntityStatus GetBucketForExport(BCPaymentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			Payment impl = cbapi.GetByID<Payment>(syncstatus.LocalID);
			if (impl == null) return EntityStatus.None;

			MappedPayment obj = bucket.Payment = bucket.Payment.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(bucket.Payment, SyncDirection.Export);

			return status;
		}

		public override void MapBucketExport(BCPaymentEntityBucket bucket, IMappedEntity existing)
		{
		}
		public override void SaveBucketExport(BCPaymentEntityBucket bucket, IMappedEntity existing, String operation)
		{
		}
		#endregion

	}
}
