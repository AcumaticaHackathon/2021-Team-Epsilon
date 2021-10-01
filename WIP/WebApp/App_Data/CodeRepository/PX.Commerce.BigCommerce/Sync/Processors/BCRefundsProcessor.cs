using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce
{
	public class BCRefundsBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary { get => Refunds; }
		public IMappedEntity[] Entities => new IMappedEntity[] { Refunds };
		public override IMappedEntity[] PostProcessors { get => new IMappedEntity[] { Order }; }

		public MappedRefunds Refunds;
		public MappedOrder Order;
	}

	public class BCRefundsRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			return null;
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			return base.Restrict(mapped, delegate (MappedRefunds obj)
			{
				if (obj.Extern != null && obj.Extern.Refunds != null)
				{
					if (obj.Extern.Refunds.All(x => x.RefundPayments.All(a => a.IsDeclined)))
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

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.OrderRefunds, BCCaptions.Refunds,
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
	public class BCRefundsProcessor : BCOrderBaseProcessor<BCRefundsProcessor, BCRefundsBucket, MappedRefunds>
	{
		#region Initialization
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
		}

		#endregion

		#region Export

		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}

		public override EntityStatus GetBucketForExport(BCRefundsBucket bucket, BCSyncStatus syncstatus)
		{
			SalesOrder impl = cbapi.GetByID<SalesOrder>(syncstatus.LocalID, GetCustomFieldsForExport());
			if (impl == null) return EntityStatus.None;

			bucket.Refunds = bucket.Refunds.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(bucket.Refunds, SyncDirection.Export);

			return status;
		}

		public override void SaveBucketExport(BCRefundsBucket bucket, IMappedEntity existing, string operation)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Import
		public override IEnumerable<MappedRefunds> PullSimilar(IExternEntity entity, out string uniqueField)
		{
			var currentBinding = GetBinding();
			var currentBindingExt = GetBindingExt<BCBindingExt>();
			uniqueField = ((OrderData)entity)?.Id.ToString();
			if (string.IsNullOrEmpty(uniqueField))
				return null;
			uniqueField = APIHelper.ReferenceMake(uniqueField, currentBinding.BindingName);

			List<MappedRefunds> result = new List<MappedRefunds>();
			List<string> orderTypes = new List<string>() { currentBindingExt?.OrderType };
			if (!string.IsNullOrWhiteSpace(currentBindingExt.OtherSalesOrderTypes))
			{
				//Support exported order type searching
				var exportedOrderTypes = currentBindingExt.OtherSalesOrderTypes?.Split(',').Where(i => i != currentBindingExt.OrderType);
				if (exportedOrderTypes.Count() > 0)
					orderTypes.AddRange(exportedOrderTypes);
			}
			helper.TryGetCustomOrderTypeMappings(ref orderTypes);

			foreach (SOOrder item in helper.OrderByTypesAndCustomerRefNbr.Select(orderTypes.ToArray(), uniqueField))
			{
				SalesOrder data = new SalesOrder() { SyncID = item.NoteID, SyncTime = item.LastModifiedDateTime, ExternalRef = item.CustomerRefNbr?.ValueField() };
				result.Add(new MappedRefunds(data, data.SyncID, data.SyncTime));
			}
			return result;
		}

		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			FilterOrders filter = new FilterOrders { IsDeleted = "false", };
			if (minDateTime != null) filter.MinDateModified = minDateTime;
			if (maxDateTime != null) filter.MaxDateModified = maxDateTime;

			IEnumerable<OrderData> datas = orderDataProvider.GetAll(filter);

			foreach (OrderData data in datas)
			{
				if (data.StatusId == OrderStatuses.Refunded.GetHashCode() || data.StatusId == OrderStatuses.PartiallyRefunded.GetHashCode() || data.RefundedAmount > 0)
				{
					data.Refunds = orderRefundsRestDataProvider.Get(data.Id.ToString()) ?? new List<OrderRefund>();
					if (data.Refunds.Count == 0) continue;

					BCRefundsBucket bucket = CreateBucket();
					var orderStatus = this.SelectStatus(BCEntitiesAttribute.Order, data.Id.ToString(), false);

					if (orderStatus == null) continue;

					var date = data.Refunds.Max(x => x.DateCreated.ToDate());
					MappedRefunds obj = bucket.Refunds = bucket.Refunds.Set(data, data.Id.ToString(), date).With(_ => { _.ParentID = orderStatus.SyncID; return _; });
					EntityStatus status = EnsureStatus(obj, SyncDirection.Import, true);
				}
			}
		}

		public override EntityStatus GetBucketForImport(BCRefundsBucket bucket, BCSyncStatus syncstatus)
		{
			OrderData orderData = orderDataProvider.GetByID(syncstatus.ExternID.KeySplit(0).ToString());

			if (orderData == null || orderData.IsDeleted == true) return EntityStatus.None;

			EntityStatus status = EntityStatus.None;
			orderData.Refunds = orderRefundsRestDataProvider.Get(orderData.Id.ToString()) ?? new List<OrderRefund>();
			var orderStatus = (BCSyncStatus)this.SelectStatus(BCEntitiesAttribute.Order, orderData.Id.ToString(), false);
			if (orderStatus == null) return status;

			if (orderStatus.LastOperation == BCSyncOperationAttribute.Skipped)
				throw new PXException(BCMessages.OrderStatusSkipped, orderData.Id);

			bucket.Order = bucket.Order.Set(orderData, orderData.Id?.ToString(), orderData.DateModifiedUT.ToDate());
			orderData.Refunds = orderData.Refunds.Where(x => !x.RefundPayments.All(p => p.IsDeclined))?.ToList();
			var date = orderData.Refunds.Max(x => x.DateCreated.ToDate());
			MappedRefunds obj = bucket.Refunds = bucket.Refunds.Set(orderData, orderData.Id.ToString(), date);
			status = EnsureStatus(obj, SyncDirection.Import);

			orderData.OrdersCoupons = orderCouponsRestDataProvider.Get(syncstatus.ExternID) ?? new List<OrdersCouponData>();
			orderData.OrderProducts = orderProductsRestDataProvider.Get(syncstatus.ExternID) ?? new List<OrdersProductData>();
			orderData.Taxes = orderTaxesRestDataProvider.GetAll(syncstatus.ExternID).ToList() ?? new List<OrdersTaxData>();
			orderData.Transactions = orderTransactionsRestDataProvider.Get(syncstatus.ExternID) ?? new List<OrdersTransactionData>();

			return status;
		}

		public override void MapBucketImport(BCRefundsBucket bucket, IMappedEntity existing)
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
			else if (mappedRefunds.Local.Status?.Value == PX.Objects.SO.Messages.Cancelled && orderData.StatusId == OrderStatuses.Refunded.GetHashCode())
			{
				bucket.Refunds.Local = new SalesOrder();
				CreateRefundPayment(bucket, mappedRefunds);
			}
			else if (mappedRefunds?.Local?.Status?.Value == PX.Objects.SO.Messages.Completed)
			{
				bucket.Refunds.Local = new SalesOrder();
				CreateRefundOrders(bucket, mappedRefunds);
				CreateRefundPayment(bucket, mappedRefunds);
			}
			else
				throw new PXException(BCMessages.OrderStatusNotValid, orderData.Id);

		}

		#region Create CustomerRefund 

		public virtual void CreateRefundPayment(BCRefundsBucket bucket, MappedRefunds existing)
		{
			var currentBinding = GetBinding();
			var currentBindingExt = GetBindingExt<BCBindingExt>();
			SalesOrder impl = bucket.Refunds.Local;
			OrderData orderData = bucket.Refunds.Extern;
			List<OrderRefund> refunds = orderData.Refunds;
			impl.Payment = new List<Payment>();
			StringValue branch = null;

			List<PXResult<ARPayment, BCSyncStatus>> result = PXSelectJoin<ARPayment,
											   InnerJoin<BCSyncStatus, On<PX.Objects.AR.ARPayment.noteID, Equal<BCSyncStatus.localID>>>,
											   Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
													And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
												   And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
												   And<BCSyncStatus.parentSyncID, Equal<Required<BCSyncStatus.parentSyncID>>,
												   And<ARPayment.docType, Equal<ARDocType.prepayment>>
											>>>>>.Select(this, BCEntitiesAttribute.Payment, bucket.Refunds.ParentID).Cast<PXResult<ARPayment, BCSyncStatus>>().ToList();
			branch = PX.Objects.GL.Branch.PK.Find(this, currentBinding.BranchID)?.BranchCD?.ValueField();

			int refundsCount = refunds.Count(x => x.RefundPayments.Any(y => y.IsDeclined == false));
			List<string> usedTransactions = null;

			foreach (var refund in refunds)
			{
				foreach (var transaction in refund.RefundPayments)
				{
					if (transaction?.IsDeclined == false)
					{
						string transMethod = null;
						ARPayment aRPayment = null;
						BCPaymentMethods currentPayment = null;
						string cashAccount = null;
						Payment refundPayment = new Payment();
						refundPayment.DocumentsToApply = new List<Core.API.PaymentDetail>();
						refundPayment.TransactionID = new object[] { refund.Id, transaction.Id.ToString() }.KeyCombine();
						var parent = orderData.Transactions.FirstOrDefault(x => (x.Event == OrderPaymentEvent.Authorization || x.Event == OrderPaymentEvent.Purchase || x.Event == OrderPaymentEvent.Pending) && x.Gateway.Equals(transaction.ProviderId, StringComparison.InvariantCultureIgnoreCase));
						var ccrefundTransactions = orderData.Transactions.Where(x => x.Event == OrderPaymentEvent.Refund && x.GatewayTransactionId != null && x.Status == OrderPaymentStatus.Ok);
						if (existing.Local?.Status?.Value != PX.Objects.SO.Messages.Completed && orderData.StatusId == OrderStatuses.Refunded.GetHashCode() && (ccrefundTransactions?.Count() == 1 && refundsCount == 1 && refund.RefundPayments.Sum(x => x.Amount ?? 0) == (orderData.TotalIncludingTax)) && parent != null)
						{
							/*call voidCardPayment Action 
							 * if Ac order open and fully refunded with CC type payment method and captured(settled)
							*/
							#region VoidCardFlow
							refundPayment.Type = PX.Objects.AR.Messages.Prepayment.ValueField();
							if (!ValidateRefundTransaction(parent, bucket.Refunds.SyncID, orderData, refund.Id, transaction, out cashAccount, out currentPayment)) continue;
							var payment = result?.FirstOrDefault(x => x?.GetItem<BCSyncStatus>()?.ExternID.KeySplit(1) == parent.Id.ToString());
							aRPayment = payment?.GetItem<ARPayment>();
							if (aRPayment == null) throw new PXException(BCMessages.OriginalPaymentNotImported, parent.Id.ToString(), orderData.Id.ToString());
							if (aRPayment?.Released != true) throw new PXException(BCMessages.OriginalPaymentNotReleased, parent.Id.ToString(), orderData.Id.ToString());
							if (existing != null)
							{
								PopulateNoteID(existing, refundPayment, ARPaymentType.VoidPayment, aRPayment.RefNbr);
								if (refundPayment.NoteID != null)
								{
									impl.Payment.Add(refundPayment);
									continue;
								}
							}

							refundPayment.ReferenceNbr = aRPayment.RefNbr.ValueField();
							String paymentTran = helper.ParseTransactionNumber(ccrefundTransactions.FirstOrDefault(), out bool isCreditCardTran);
							refundPayment.VoidCardParameters = new VoidCardPayment()
							{
								TranType = CCTranTypeCode.Unknown.ValueField(),
								TranNbr = paymentTran.ValueField(),
							};

							impl.Payment.Add(refundPayment);
							#endregion
						}
						else
						{
							bool isCreditCardTran = false;

							refundPayment.ExternalRef = transaction.Id.ToString().ValueField();
							if (existing != null)
							{
								PopulateNoteID(existing, refundPayment, ARPaymentType.Refund, refundPayment.ExternalRef.Value);
								if (refundPayment.NoteID != null)
								{
									impl.Payment.Add(refundPayment);
									continue;
								}
							}
							OrdersTransactionData ccrefundTransaction = null;

							if (ccrefundTransactions?.Count() > 0)
							{
								var ccrefundTransactionList = ccrefundTransactions?.Where(x => (decimal)x.Amount == transaction.Amount.Value);
								//need this logic to get refund transaction. as there is no link between refund payment and refund transaction
								if (ccrefundTransactionList?.Count() > 1 && usedTransactions == null)
								{
									var ccTransactions = PXSelectJoin<BCSyncDetail, InnerJoin<ARPayment, On<BCSyncDetail.localID, Equal<ARPayment.noteID>>>,
									   Where<BCSyncDetail.syncID, Equal<Required<BCSyncDetail.syncID>>,
									   And<BCSyncDetail.entityType, Equal<Required<BCSyncDetail.entityType>>,
									   And<ARPayment.docType, Equal<ARDocType.refund>,
									   And<ARPayment.isCCPayment, Equal<True>>>>>>.Select(this, bucket.Refunds.SyncID, BCEntitiesAttribute.Payment)?.Cast<PXResult<BCSyncDetail, ARPayment>>().ToList();

									usedTransactions = usedTransactions ?? new List<string>();
									usedTransactions.AddRange(ccTransactions.Select(x => x.GetItem<ARPayment>().ExtRefNbr));
									ccrefundTransaction = ccrefundTransactionList.FirstOrDefault(x => usedTransactions != null && !usedTransactions.Contains(helper.ParseTransactionNumber(x, out bool iscc)));
								}
								else
									ccrefundTransaction = ccrefundTransactionList.FirstOrDefault();
							}

							var reference = helper.ParseTransactionNumber(ccrefundTransaction, out isCreditCardTran);
							if (reference != null)
							{
								usedTransactions = usedTransactions ?? new List<string>();
								usedTransactions.Add(reference);
							}
							refundPayment.PaymentRef = (reference ?? transaction.Id.ToString()).ValueField();

							if (parent == null)
							{
								// if refund gateway does not match the orignal payment
								transMethod = transaction.ProviderId.Equals("storecredit", StringComparison.InvariantCultureIgnoreCase) ? BCObjectsConstants.StoreCreditCode : transaction.ProviderId;
								currentPayment = helper.GetPaymentMethodMapping(transMethod, null, orderData.CurrencyCode, out cashAccount, false);
								if (currentPayment?.ProcessRefunds != true)
								{
									LogInfo(Operation.LogScope(bucket.Refunds.SyncID), BCMessages.LogRefundPaymentSkipped, orderData.Id, refund.Id, transaction.Id, currentPayment?.PaymentMethodID ?? transMethod);
									continue; // create CR payment if only ProcessRefunds is checked
								}
								if (ccrefundTransaction == null && currentPayment?.ProcessingCenterID != null) continue;// if there is processing center and no external refund transaction then skip
								if (existing.Local?.Status?.Value == PX.Objects.SO.Messages.Completed) // do not apply payment just create in on hold status
								{
									refundPayment.Hold = true.ValueField();
								}
								else
								{
									decimal? amount = transaction.Amount;

									foreach (var payment in result)
									{
										if (amount == 0) break;
										aRPayment = payment.GetItem<ARPayment>();
										decimal curyAdjdAmt = ((aRPayment.CuryOrigDocAmt ?? 0) - (aRPayment.CuryUnappliedBal ?? 0));
										if (curyAdjdAmt <= 0) continue;
										if (aRPayment == null) throw new PXException(BCMessages.OriginalPaymentNotImported, aRPayment?.ExtRefNbr, orderData.Id.ToString());
										if (aRPayment?.Released != true) throw new PXException(BCMessages.OriginalPaymentNotReleased, aRPayment?.ExtRefNbr, orderData.Id.ToString());
										ValidateCRPayment(aRPayment?.RefNbr);
										if (curyAdjdAmt >= amount)
										{
											CreatePaymentDetail(refundPayment, aRPayment, amount);
											amount = 0;
											break;
										}
										else
										{
											amount = amount - curyAdjdAmt;
											CreatePaymentDetail(refundPayment, aRPayment, amount);
										}
									}
									if (amount != 0) throw new PXException(BCMessages.OriginalPaymentNotReleased, null, orderData.Id.ToString());
								}
								refundPayment.PaymentAmount = transaction.Amount.ValueField();
							}
							else // if refund payment gateway matches original payment 
							{
								if (!ValidateRefundTransaction(parent, bucket.Refunds.SyncID, orderData, refund.Id, transaction, out cashAccount, out currentPayment)) continue;
								if (ccrefundTransaction == null && currentPayment?.ProcessingCenterID != null) continue;// if there is processing center and no external refund transaction then skip
								var payment = result.FirstOrDefault(x => x.GetItem<BCSyncStatus>().ExternID.KeySplit(1) == parent.Id.ToString());
								aRPayment = payment.GetItem<ARPayment>();
								if (currentPayment?.ProcessingCenterID != null && isCreditCardTran)
								{
									helper.AddCreditCardProcessingInfo(currentPayment, refundPayment, ccrefundTransaction.Event, ccrefundTransaction.PaymentInstrumentToken);
									if (aRPayment?.IsCCPayment == true)
									{
										refundPayment.OrigTransaction = ExternalTransaction.PK.Find(this, aRPayment?.CCActualExternalTransactionID)?.TranNumber.ValueField();
									}
								}
								if (existing.Local?.Status?.Value == PX.Objects.SO.Messages.Completed) // do not apply payment just create in on hold status
								{
									refundPayment.Hold = true.ValueField();
									refundPayment.PaymentAmount = transaction.Amount.ValueField();
								}
								else
								{

									if (aRPayment == null) throw new PXException(BCMessages.OriginalPaymentNotImported, parent.Id.ToString(), orderData.Id.ToString());
									if (aRPayment?.Released != true) throw new PXException(BCMessages.OriginalPaymentNotReleased, parent.Id.ToString(), orderData.Id.ToString());

									ValidateCRPayment(aRPayment?.RefNbr);
									var paymentDetail = CreatePaymentDetail(refundPayment, aRPayment, transaction.Amount);
									refundPayment.PaymentAmount = paymentDetail.AmountPaid;

								}
							}

							//map Sumary section
							refundPayment.Type = PX.Objects.AR.Messages.Refund.ValueField();
							refundPayment.CustomerID = existing.Local.CustomerID;
							refundPayment.CustomerLocationID = existing.Local.LocationID;
							var date = refund.DateCreated.ToDate(PXTimeZoneInfo.FindSystemTimeZoneById(currentBindingExt.OrderTimeZone));
							if (date.HasValue)
								refundPayment.ApplicationDate = (new DateTime(date.Value.Date.Ticks)).ValueField();
							refundPayment.BranchID = branch;
							refundPayment.TransactionID = new object[] { refund.Id, transaction.Id.ToString() }.KeyCombine();
							refundPayment.PaymentMethod = currentPayment?.PaymentMethodID?.ValueField();
							refundPayment.CashAccount = cashAccount?.Trim()?.ValueField();

							var desc = PXMessages.LocalizeFormat(BigCommerceMessages.PaymentRefundDescription, currentBinding.BindingName, orderData.Id, refund.Id, transaction.ProviderId);
							refundPayment.Description = desc.ValueField();
							impl.Payment.Add(refundPayment);
						}
					}
				}
			}
		}

		public virtual void PopulateNoteID(MappedRefunds existing, Payment refundPayment, string docType, string reference, string paymentRef = null)
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

		public virtual PaymentDetail CreatePaymentDetail(Payment cr, ARPayment aRPayment, decimal? amount)
		{
			Core.API.PaymentDetail paymentDetail = new Core.API.PaymentDetail();
			paymentDetail.DocType = ARPaymentType.Prepayment.ValueField();
			paymentDetail.AmountPaid = (amount ?? 0).ValueField();
			paymentDetail.ReferenceNbr = aRPayment?.RefNbr.ValueField();

			cr.DocumentsToApply.Add(paymentDetail);
			return paymentDetail;
		}

		//validates if existingCR payment is released or not
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

		//validates if processrefund checkbox is checked and credit card refund transaction is not offline
		public virtual bool ValidateRefundTransaction(OrdersTransactionData parent, int? syncID, OrderData orderData, int refundId, RefundPayment trans, out string cashAccount, out BCPaymentMethods methodMapping)
		{
			string transMethod = helper.GetPaymentMethodName(parent);
			methodMapping = helper.GetPaymentMethodMapping(transMethod, orderData.PaymentMethod, orderData.CurrencyCode, out cashAccount, false);
			if (methodMapping?.ProcessRefunds != true)
			{
				LogInfo(Operation.LogScope(syncID), BCMessages.LogRefundPaymentSkipped, orderData.Id, refundId, trans.Id, methodMapping?.PaymentMethodID ?? transMethod);
				return false; // process refund if only ProcessRefunds is checked
			}
			if (trans.Offline && parent.CreditCard != null)
			{
				LogInfo(Operation.LogScope(syncID), BCMessages.LogRefundCCPaymentSkipped, orderData.Id, refundId, trans.Id);
				return false;
			}
			return true;
		}
		#endregion

		#region CreateRefundOrders

		public virtual void CreateRefundOrders(BCRefundsBucket bucket, MappedRefunds existing)
		{
			var currentBinding = GetBinding();
			var currentBindingExt = GetBindingExt<BCBindingExt>();

			SalesOrder origOrder = bucket.Refunds.Local;
			OrderData orderData = bucket.Refunds.Extern;
			List<OrderRefund> refunds = orderData.Refunds;
			origOrder.RefundOrders = new List<SalesOrder>();
			var branch = PX.Objects.GL.Branch.PK.Find(this, currentBinding.BranchID)?.BranchCD?.ValueField();

			foreach (OrderRefund data in refunds)
			{
				if (data.RefundPayments.All(x => x.IsDeclined == true)) continue;
				SalesOrder impl = new SalesOrder();
				impl.ExternalRef = APIHelper.ReferenceMake(data.Id, currentBinding.BindingName).ValueField();
				//Check if refund is already imported as RC Order
				var existingRC = cbapi.GetAll<SalesOrder>(new SalesOrder()
				{
					OrderType = currentBindingExt.ReturnOrderType.SearchField(),
					ExternalRef = impl.ExternalRef.Value.SearchField(),
					Details = new List<SalesOrderDetail>() { new SalesOrderDetail() { InventoryID = new StringReturn() } },
					DiscountDetails = new List<SalesOrdersDiscountDetails>() { new SalesOrdersDiscountDetails() { ExternalDiscountCode = new StringReturn() } }
				},
				filters: GetFilter(Operation.EntityType).LocalFiltersRows.Cast<PXFilterRow>());
				if (existingRC.Count() > 1)
				{
					throw new PXException(BCMessages.MultipleEntitiesWithUniqueField, BCCaptions.SyncDirectionImport,
						  Connector.GetEntities().First(e => e.EntityType == Operation.EntityType).EntityName, data.Id.ToString());
				}
				var presentCROrder = existingRC?.FirstOrDefault();
				// skip refunds that were adjusted  before order completion
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
				impl.OrderType = currentBindingExt.ReturnOrderType.ValueField();
				impl.FinancialSettings = new FinancialSettings();
				impl.FinancialSettings.Branch = branch;
				var date = data.DateCreated.ToDate(PXTimeZoneInfo.FindSystemTimeZoneById(currentBindingExt.OrderTimeZone));
				if (date.HasValue)
					impl.Date = (new DateTime(date.Value.Date.Ticks)).ValueField();
				impl.RequestedOn = impl.Date;
				impl.CustomerOrder = orderData.Id.ToString().ValueField();
				impl.CustomerID = existing.Local.CustomerID;
				impl.LocationID = existing.Local.LocationID;
				impl.ExternalRef = APIHelper.ReferenceMake(data.Id, currentBinding.BindingName).ValueField();
				var description = PXMessages.LocalizeFormat(BigCommerceMessages.OrderDescription, currentBinding.BindingName, orderData.Id.ToString(), orderData.Status.ToString());
				impl.Description = description.ValueField();
				impl.Details = new List<SalesOrderDetail>();
				impl.Totals = new Totals();
				impl.Totals.OverrideFreightAmount = existing.Local.Totals?.OverrideFreightAmount;
				List<RefundedItem> refundItems = data.RefundedItems;
				decimal shippingrefundAmt = refundItems?.Where(x => x.ItemType == RefundItemType.Shipping || x.ItemType == RefundItemType.Handling)?.Sum(x => (x.RequestedAmount) ?? 0m) ?? 0m;

				impl.ShipVia = existing.Local.ShipVia;
				if ((existing.Local.Totals?.Freight?.Value == null || existing.Local.Totals?.Freight?.Value == 0) && existing.Local.Totals?.PremiumFreight?.Value > 0)
				{
					if (shippingrefundAmt > existing.Local.Totals?.PremiumFreight?.Value) throw new PXException(BCMessages.RefundShippingFeeInvalid, shippingrefundAmt, existing.Local.Totals?.PremiumFreight?.Value);
					impl.Totals.PremiumFreight = shippingrefundAmt.ValueField();
				}
				else
				{
					if (shippingrefundAmt > existing.Local.Totals?.Freight?.Value) throw new PXException(BCMessages.RefundShippingFeeInvalid, shippingrefundAmt, existing.Local.Totals?.Freight?.Value);
					impl.Totals.Freight = shippingrefundAmt.ValueField();
				}
				var totalOrderRefundAmout = refundItems?.Where(x => x.ItemType == RefundItemType.Order)?.Sum(y => (y.RequestedAmount)) ?? 0;
				//Add orderAdjustments
				if (totalOrderRefundAmout != 0)
				{
					var detail = InsertRefundAmountItem(totalOrderRefundAmout, branch);
					if (presentCROrder?.Details != null)
						presentCROrder?.Details.FirstOrDefault(x => x.InventoryID.Value == detail.InventoryID.Value).With(e => detail.Id = e.Id);
					impl.Details.Add(detail);
				}

				#region Taxes	
				impl.TaxDetails = new List<TaxDetail>();

				if (existing.Local.TaxDetails?.Count > 0 && data.TotalTax > 0 && orderData.Taxes.Count > 0)// if acumatica original SO has tax and Refunds has tax then process tax
				{
					impl.IsTaxValid = true.ValueField();
					foreach (OrdersTaxData tax in orderData.Taxes)
					{
						//Third parameter set to tax name in order to simplify process (if tax names are equal and user don't want to fill lists)

						string mappedTaxName = mappedTaxName = GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxSubstitutionListID, tax.Name, tax.Name);
						mappedTaxName = helper.TrimAutomaticTaxNameForAvalara(mappedTaxName);
						decimal? taxable = 0m;
						if (string.IsNullOrEmpty(mappedTaxName)) throw new PXException(PX.Commerce.Objects.BCObjectsMessages.TaxNameDoesntExist);
						TaxDetail inserted = impl.TaxDetails.FirstOrDefault(i => i.TaxID.Value?.Equals(mappedTaxName, StringComparison.InvariantCultureIgnoreCase) == true);
						var shippingItems = refundItems.Where(x => (x.ItemType == RefundItemType.Shipping || x.ItemType == RefundItemType.Handling));
						var trefundItems = refundItems.Where(y => y.ItemType == RefundItemType.Product && y.ItemId == tax.OrderProductId);
						var originalOrderProduct = orderData.OrderProducts.FirstOrDefault(i => i.Id == tax.OrderProductId);
						var quantity = trefundItems?.Sum(x => x.Quantity) ?? 0;
						taxable = CalculateTaxableRefundAmount(originalOrderProduct, shippingItems, quantity, tax.LineItemType);
						var taxAmount = Math.Round((decimal)(taxable * tax.Rate / 100), 2);
						if (inserted == null)
						{
							impl.TaxDetails.Add(new TaxDetail()
							{
								TaxID = mappedTaxName.ValueField(),
								TaxAmount = taxAmount.ValueField(),
								TaxRate = tax.Rate.ValueField(),
								TaxableAmount = taxable.ValueField()
							});
						}
						else if (inserted.TaxAmount != null)
						{
							inserted.TaxAmount.Value += taxAmount;
							inserted.TaxableAmount.Value += taxable;
						}
					}
				}
				else
				{
					if (existing.Local.TaxDetails?.Count > 0 && data.TotalTax > 0 && orderData.Taxes.Count == 0)// In case of full refunds order taxes count become zero
					{
						impl.IsTaxValid = true.ValueField();
						var taxRateTotal = existing.Local.TaxDetails.Sum(x => x.TaxRate.Value);
						foreach (TaxDetail tax in existing.Local.TaxDetails)
						{
							TaxDetail inserted = impl.TaxDetails.FirstOrDefault(i => i.TaxID.Value?.Equals(tax.TaxID.Value, StringComparison.InvariantCultureIgnoreCase) == true);
							var taxable = data.TotalAmount - data.TotalTax;
							var taxAmount = Math.Round((decimal)(data.TotalTax * tax.TaxRate.Value / taxRateTotal), 2); // just get tax amount based on totaltax inrefund and taxrate from Acumatica salesorder
							if (inserted == null)
							{
								impl.TaxDetails.Add(new TaxDetail()
								{
									TaxID = tax.TaxID,
									TaxAmount = taxAmount.ValueField(),
									TaxRate = tax.TaxRate,
									TaxableAmount = tax.TaxableAmount
								});
							}
							else if (inserted.TaxAmount != null)
							{
								inserted.TaxAmount.Value += taxAmount;
								inserted.TaxableAmount.Value += taxable;
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



				#endregion

				#region Discounts
				Dictionary<string, decimal?> totalDiscount = null;
				if (refundItems?.Where(x => x.ItemType == RefundItemType.Product).Count() > 0)
				{
					totalDiscount = AddSOLine(bucket, impl, data, existing, branch, presentCROrder);
				}

				if (currentBindingExt.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount && totalDiscount != null && totalDiscount?.Count > 0)
				{
					#region Coupons
					impl.DisableAutomaticDiscountUpdate = true.ValueField();
					impl.DiscountDetails = new List<SalesOrdersDiscountDetails>();
					foreach (OrdersCouponData couponData in orderData.OrdersCoupons)
					{
						SalesOrdersDiscountDetails disDetail = new SalesOrdersDiscountDetails();
						disDetail.ExternalDiscountCode = couponData.CouponCode.ValueField();
						disDetail.Description = string.Format(BCMessages.DiscountCouponDesctiption, couponData.CouponType.GetDescription(), couponData.Discount)?.ValueField();

						if (currentBindingExt.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount)
							disDetail.DiscountAmount = totalDiscount.GetValueOrDefault_<decimal>(couponData.CouponCode, 0).ValueField();
						else disDetail.DiscountAmount = 0m.ValueField();

						impl.DiscountDetails.Add(disDetail);
					}
					#endregion


					SalesOrdersDiscountDetails detail = new SalesOrdersDiscountDetails();
					detail.Type = PX.Objects.Common.Discount.DiscountType.ExternalDocument.ValueField();
					detail.ExternalDiscountCode = "Manual".ValueField();
					detail.DiscountAmount = (totalDiscount.GetValueOrDefault_<decimal>("Manual", 0)).ValueField();
					impl.DiscountDetails.Add(detail);
				}

				#endregion

				#region Adjust for Existing
				if (presentCROrder != null)
				{
					//Keep the same order Type
					impl.OrderType = presentCROrder.OrderType;

					//if Order already exists assign ID's 
					presentCROrder.DiscountDetails?.ForEach(e => impl.DiscountDetails?.FirstOrDefault(n => n.ExternalDiscountCode.Value == e.ExternalDiscountCode.Value).With(n => n.Id = e.Id));

					impl.DiscountDetails?.AddRange(presentCROrder.DiscountDetails == null ? Enumerable.Empty<SalesOrdersDiscountDetails>()
					: presentCROrder.DiscountDetails.Where(e => impl.DiscountDetails == null || !impl.DiscountDetails.Any(n => e.Id == n.Id)).Select(n => new SalesOrdersDiscountDetails() { Id = n.Id, Delete = true }));
				}
				#endregion
			}
		}
		public virtual Dictionary<string, decimal?> AddSOLine(BCRefundsBucket bucket, SalesOrder impl, OrderRefund data, MappedRefunds existing, StringValue branch, SalesOrder presentCROrder)
		{
			var currentBinding = GetBinding();
			var currentBindingExt = GetBindingExt<BCBindingExt>();
			OrderData salesOrder = bucket.Refunds.Extern;
			Dictionary<string, decimal?> totaldiscount = new Dictionary<string, decimal?>();
			foreach (var item in data.RefundedItems.Where(x => x.ItemType == RefundItemType.Product))
			{
				SalesOrderDetail detail = new SalesOrderDetail();
				var productData = salesOrder.OrderProducts.FirstOrDefault(x => x.Id == item.ItemId);
				if (productData == null) throw new PXException(BCMessages.RefundInventoryNotFound, item.ItemId);

				string inventoryCD = helper.GetInventoryCDByExternID(
					productData.ProductId.ToString(),
					productData.OptionSetId >= 0 ? productData.VariandId.ToString() : null,
					productData.Sku,
					productData.ProductType,
					out string uom);
				decimal discountPerItem = 0;
				if (productData.AppliedDiscounts != null)
				{
					discountPerItem = productData.AppliedDiscounts.Select(p => p.DiscountAmount).Sum() / productData.Quantity;
					detail.DiscountAmount = (discountPerItem * item.Quantity).ValueField();
					foreach (var discount in productData.AppliedDiscounts)
					{
						string key = discount.Code;
						if (discount.Id != "coupon")
							key = "Manual";
						if (totaldiscount.ContainsKey(key))
							totaldiscount[key] = totaldiscount[key].Value + ((discount.DiscountAmount / productData.Quantity) * item.Quantity);
						else
							totaldiscount.Add(key, ((discount.DiscountAmount / productData.Quantity) * item.Quantity));
					}
				}
				if (currentBindingExt.PostDiscounts != BCPostDiscountAttribute.LineDiscount)
					detail.DiscountAmount = 0m.ValueField();

				detail.InventoryID = inventoryCD?.TrimEnd().ValueField();

				if (item.Quantity > productData.Quantity)
					throw new PXException(BCMessages.RefundQuantityGreater);
				if (string.IsNullOrWhiteSpace(currentBindingExt.ReasonCode))
					throw new PXException(BigCommerceMessages.ReasonCodeRequired);

				detail.OrderQty = ((decimal)item.Quantity).ValueField();
				detail.UOM = uom.ValueField();
				detail.UnitPrice = productData.PriceExcludingTax.ValueField();
				detail.ManualPrice = true.ValueField();
				detail.ReasonCode = currentBindingExt.ReasonCode?.ValueField();
				detail.ExternalRef = item.ItemId.ToString().ValueField();
				impl.Details.Add(detail);

				DetailInfo matchedDetail = existing?.Details?.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && item.ItemId.ToString() == d.ExternID.KeySplit(1) && data.Id.ToString() == d.ExternID.KeySplit(0));
				if (matchedDetail != null) detail.Id = matchedDetail.LocalID; //Search by Details
				else if (presentCROrder?.Details != null && presentCROrder.Details.Count > 0) //Serach by Existing line
				{
					SalesOrderDetail matchedLine = presentCROrder.Details.FirstOrDefault(x =>
						(x.ExternalRef?.Value != null && x.ExternalRef?.Value == item.ItemId.ToString())
						||
						(x.InventoryID?.Value == detail.InventoryID?.Value && (detail.UOM == null || detail.UOM.Value == x.UOM?.Value)));
					if (matchedLine != null) detail.Id = matchedLine.Id;
				}

			}

			return totaldiscount;
		}
		#endregion

		public override void SaveBucketImport(BCRefundsBucket bucket, IMappedEntity existing, string operation)
		{
			MappedRefunds obj = bucket.Refunds;
			SalesOrder order = obj.Local;
			try
			{
				obj.ClearDetails();

				if (order.Payment != null)
				{
					List<Tuple<string, string>> addedRefNbr = new List<Tuple<string, string>>();
					foreach (var payment in order.Payment)
					{
						Payment paymentResp = null;
						Guid? localId = payment.NoteID?.Value;
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
								localId = paymentResp?.NoteID?.Value;
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
							if (impl.OrderTotal?.Value > obj.Extern.Refunds.FirstOrDefault(x => x.Id.ToString() == refundOrder.RefundID).TotalAmount)
								throw new PXException(BCMessages.RCOrderTotalGreater);
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
										RefundedItem detail = null;
										var externRefundItems = obj.Extern.Refunds.FirstOrDefault(x => x.Id.ToString() == refundOrder.RefundID).RefundedItems;
										detail = externRefundItems.FirstOrDefault(x => x.ItemId.ToString() == lineitem.ExternalRef?.Value);
										if (detail == null)
											detail = externRefundItems.FirstOrDefault(x => !obj.Details.Any(o => x.ItemId.ToString() == o.ExternID && x.ItemType == RefundItemType.Product)
									&& obj.Extern.OrderProducts.FirstOrDefault(o => o.Id == x.ItemId)?.Sku == lineitem.InventoryID.Value);
										if (detail != null)
											obj.AddDetail(BCEntitiesAttribute.OrderLine, lineitem.Id, new object[] { refundOrder.RefundID, detail.ItemId }.KeyCombine());
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
					EnsureStatus(bucket.Order, SyncDirection.Import);
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

		#region Pull
		public override MappedRefunds PullEntity(string externID, string externalInfo)
		{
			return null;
		}

		public override MappedRefunds PullEntity(Guid? localID, Dictionary<string, object> externalInfo)
		{
			return null;
		}

		#endregion
	}
}
