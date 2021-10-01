using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Common;
using PX.Data.BQL;
using PX.Objects.SO;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.PO;
using PX.Api.ContractBased.Models;
using Serilog.Context;
using static PX.Objects.SO.SOShipmentEntry;
using PX.Objects.CS;

namespace PX.Commerce.BigCommerce
{
	public class BCShipmentEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Shipment;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary }.Concat(Orders).ToArray();

		public MappedShipment Shipment;
		public List<MappedOrder> Orders = new List<MappedOrder>();
	}

	public class BCShipmentsRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			#region Shipments
			return base.Restrict<MappedShipment>(mapped, delegate (MappedShipment obj)
			{
				if (obj.Local != null)
				{
					if (obj.Local.Confirmed?.Value == false)
					{
						return new FilterResult(FilterStatus.Invalid,
								PXMessages.Localize(BCMessages.LogShipmentSkippedNotConfirmed));
					}
					if (obj.Local?.OrderNoteIds != null)
					{
						BCBindingExt binding = processor.GetBindingExt<BCBindingExt>();

						Boolean anyFound = false;
						foreach (var orderNoteID in obj.Local?.OrderNoteIds)
						{
							if (processor.SelectStatus(BCEntitiesAttribute.Order, orderNoteID) == null) continue;

							anyFound = true;
						}
						if (!anyFound)
						{
							return new FilterResult(FilterStatus.Ignore,
								PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogShipmentSkippedNoOrder, obj.Local.ShipmentNumber?.Value ?? obj.Local.SyncID.ToString()));
						}
					}

				}

				return null;
			});
			#endregion
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			return null;
		}
	}

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.Shipment, BCCaptions.Shipment,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		ExternTypes = new Type[] { typeof(OrdersShipmentData) },
		LocalTypes = new Type[] { typeof(BCShipments) },
		GIScreenID = BCConstants.GenericInquiryShipmentDetails,
		GIResult = typeof(BCShipmentsResult),
		DetailTypes = new String[] { BCEntitiesAttribute.ShipmentLine, BCCaptions.ShipmentLine, BCEntitiesAttribute.ShipmentBoxLine, BCCaptions.ShipmentLineBox },
		AcumaticaPrimaryType = typeof(PX.Objects.SO.SOShipment),
		AcumaticaPrimarySelect = typeof(PX.Objects.SO.SOShipment.shipmentNbr),
		URL = "orders?keywords={0}&searchDeletedOrders=no",
		Requires = new string[] { BCEntitiesAttribute.Order }
	)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-Shipments" })]
	public class BCShipmentProcessor : BCProcessorSingleBase<BCShipmentProcessor, BCShipmentEntityBucket, MappedShipment>, IProcessor
	{
		protected OrderRestDataProvider orderDataProvider;
		protected IChildRestDataProvider<OrdersShipmentData> orderShipmentRestDataProvider;
		protected IChildRestDataProvider<OrdersProductData> orderProductsRestDataProvider;

		protected List<BCShippingMappings> shippingMappings;

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			var client = BCConnector.GetRestClient(GetBindingExt<BCBindingBigCommerce>());

			orderDataProvider = new OrderRestDataProvider(client);
			orderShipmentRestDataProvider = new OrderShipmentsRestDataProvider(client);
			orderProductsRestDataProvider = new OrderProductsRestDataProvider(client);

			shippingMappings = PXSelectReadonly<BCShippingMappings,
				Where<BCShippingMappings.bindingID, Equal<Required<BCShippingMappings.bindingID>>>>
				.Select(this, Operation.Binding).Select(x => x.GetItem<BCShippingMappings>()).ToList();
		}
		#endregion

		public override void NavigateLocal(IConnector connector, ISyncStatus status)
		{
			SOOrderShipment orderShipment = PXSelect<SOOrderShipment, Where<SOOrderShipment.shippingRefNoteID, Equal<Required<SOOrderShipment.shippingRefNoteID>>>>.Select(this, status?.LocalID);
			if (orderShipment.ShipmentType == SOShipmentType.DropShip)//dropshipment
			{
				POReceiptEntry extGraph = PXGraph.CreateInstance<POReceiptEntry>();
				EntityHelper helper = new EntityHelper(extGraph);
				helper.NavigateToRow(extGraph.GetPrimaryCache().GetItemType().FullName, status.LocalID, PXRedirectHelper.WindowMode.NewWindow);

			}
			if (orderShipment.ShipmentType == SOShipmentType.Issue && orderShipment.ShipmentNoteID == null) //Invoice
			{
				POReceiptEntry extGraph = PXGraph.CreateInstance<POReceiptEntry>();
				EntityHelper helper = new EntityHelper(extGraph);
				helper.NavigateToRow(extGraph.GetPrimaryCache().GetItemType().FullName, status.LocalID, PXRedirectHelper.WindowMode.NewWindow);

			}
			else//shipment
			{
				SOShipmentEntry extGraph = PXGraph.CreateInstance<SOShipmentEntry>();
				EntityHelper helper = new EntityHelper(extGraph);
				helper.NavigateToRow(extGraph.GetPrimaryCache().GetItemType().FullName, status.LocalID, PXRedirectHelper.WindowMode.NewWindow);

			}

		}

		#region Pull
		public override MappedShipment PullEntity(Guid? localID, Dictionary<string, object> externalInfo)
		{
			BCBindingExt binding = GetBindingExt<BCBindingExt>();
			BCShipments giResult = (new BCShipments()
			{
				BindingID = GetBinding().BindingID.ValueField(),
				ShippingNoteID = localID.ValueField()
			});
			giResult.Results = cbapi.GetGIResult<BCShipmentsResult>(giResult, BCConstants.GenericInquiryShipmentDetails).ToList();

			if (giResult?.Results == null) return null;
			MapFilterFields(binding, giResult?.Results, giResult);
			GetOrderShipment(giResult);
			if (giResult.Shipment == null && giResult.POReceipt == null) return null;
			MappedShipment obj = new MappedShipment(giResult, giResult.ShippingNoteID.Value, giResult.LastModified.Value);
			return obj;


		}
		public override MappedShipment PullEntity(String externID, String externalInfo)
		{
			OrdersShipmentData data = orderShipmentRestDataProvider.GetByID(externID.KeySplit(1), externID.KeySplit(0));
			if (data == null) return null;

			MappedShipment obj = new MappedShipment(data, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedUT.ToDate(), data.CalculateHash());

			return obj;
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}
		public override EntityStatus GetBucketForImport(BCShipmentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			bucket.Shipment = bucket.Shipment.Set(new OrdersShipmentData(), syncstatus.ExternID, syncstatus.ExternTS);

			return EntityStatus.None;
		}

		public override void MapBucketImport(BCShipmentEntityBucket bucket, IMappedEntity existing)
		{
		}
		public override void SaveBucketImport(BCShipmentEntityBucket bucket, IMappedEntity existing, String operation)
		{
		}
		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			BCBindingExt binding = GetBindingExt<BCBindingExt>();
			var minDate = minDateTime == null || (minDateTime != null && binding.SyncOrdersFrom != null && minDateTime < binding.SyncOrdersFrom) ? binding.SyncOrdersFrom : minDateTime;
			IEnumerable<BCShipmentsResult> giResult = cbapi.GetGIResult<BCShipmentsResult>(new BCShipments()
			{
				BindingID = GetBinding().BindingID.ValueField(),
				LastModified = minDate.ValueField()
			}, BCConstants.GenericInquiryShipment);

			foreach (var result in giResult)
			{
				if (result.NoteID?.Value == null)
					continue;
				BCShipments bCShipments = new BCShipments() { ShippingNoteID = result.NoteID, LastModified = result.LastModifiedDateTime };
				MappedShipment obj = new MappedShipment(bCShipments, bCShipments.ShippingNoteID.Value, bCShipments.LastModified.Value);
				EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

			}
		}

		protected virtual void MapFilterFields(BCBindingExt binding, List<BCShipmentsResult> results, BCShipments impl)
		{
			impl.OrderNoteIds = new List<Guid?>();
			foreach (var result in results)
			{
				impl.ShippingNoteID = result.NoteID;
				impl.VendorRef = result.InvoiceNbr;
				impl.ShipmentNumber = result.ShipmentNumber;
				impl.ShipmentType = result.ShipmentType;
				impl.LastModified = result.LastModifiedDateTime;
				impl.Confirmed = result.Confirmed;
				impl.OrderNoteIds.Add(result.OrderNoteID.Value);

			}
		}

		public override EntityStatus GetBucketForExport(BCShipmentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			BCBindingExt binding = GetBindingExt<BCBindingExt>();
			SOOrderShipments impl = new SOOrderShipments();

			BCShipments giResult = (new BCShipments()
			{
				BindingID = GetBinding().BindingID.ValueField(),
				ShippingNoteID = syncstatus.LocalID.ValueField()
			});
			giResult.Results = cbapi.GetGIResult<BCShipmentsResult>(giResult, BCConstants.GenericInquiryShipmentDetails).ToList();

			if (giResult?.Results == null) return EntityStatus.None;

			MapFilterFields(binding, giResult?.Results, giResult);

			if (giResult.ShipmentType.Value == SOShipmentType.DropShip)
			{
				return GetDropShipment(bucket, giResult);
			}
			else if (giResult.ShipmentType.Value == SOShipmentType.Invoice)
			{

				return GetInvoice(bucket, giResult);
			}
			else
			{
				return GetShipment(bucket, giResult);
			}

		}

		public override void MapBucketExport(BCShipmentEntityBucket bucket, IMappedEntity existing)
		{
			MappedShipment obj = bucket.Shipment;
			if (obj.Local?.Confirmed?.Value == false) throw new PXException(BCMessages.ShipmentNotConfirmed);
			if (obj.Local.ShipmentType.Value == SOShipmentType.DropShip)
			{
				PurchaseReceipt impl = obj.Local.POReceipt;
				MapDropShipment(bucket, obj, impl);
			}
			else if (obj.Local.ShipmentType.Value == SOShipmentType.Issue)
			{
				Shipment impl = obj.Local.Shipment;
				MapShipment(bucket, obj, impl);
			}
			else
			{
				OrdersShipmentData shipmentData = obj.Extern = new OrdersShipmentData();
			}
		}

		public override CustomField GetLocalCustomField(BCShipmentEntityBucket bucket, string viewName, string fieldName)
		{
			MappedShipment obj = bucket.Shipment;
			BCShipments impl = obj.Local;
			if (impl?.Results?.Count() > 0)
				return impl.Results[0].Custom?.Where(x => x.ViewName == viewName && x.FieldName == fieldName).FirstOrDefault();
			else return null;
		}

		public override void SaveBucketExport(BCShipmentEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedShipment obj = bucket.Shipment;

			StringBuilder key = new StringBuilder();
			String origExternId = obj.ExternID;
			var ShipmentItems = obj.Extern.ShipmentItems;
			OrdersShipmentData ordersShipmentData = obj.Extern;
			List<DetailInfo> existingDetails = new List<DetailInfo>(obj.Details);
			List<string> notFound = new List<string>();
			obj.ClearDetails();
			//Delete all shipments for given BCSyncDetails
			if (existingDetails != null && existingDetails.Count > 0)
			{
				foreach (var externShipment in existingDetails.Where(d => d.EntityType == BCEntitiesAttribute.ShipmentLine || d.EntityType == BCEntitiesAttribute.ShipmentBoxLine))
				{
					var orderExist = bucket.Orders.FirstOrDefault(x => x.LocalID == externShipment.LocalID);
					if (externShipment.ExternID.HasParent() || orderExist != null)
					{
						orderShipmentRestDataProvider.Delete(externShipment.ExternID.HasParent() ? externShipment.ExternID.KeySplit(1) : externShipment.ExternID, externShipment.ExternID.HasParent() ? externShipment.ExternID.KeySplit(0) : orderExist.ExternID);
					}
					else
						notFound.Add(externShipment.ExternID);
					if (externShipment.ExternID.HasParent() && orderExist == null)
					{
						//if order is removed from shipment then delete the shipment from BC and change status back to Awaiting Full fillment
						OrderStatus orderStatus = new OrderStatus();
						orderStatus.StatusId = OrderStatuses.AwaitingFulfillment.GetHashCode();
						orderStatus = orderDataProvider.Update(orderStatus, externShipment.ExternID.KeySplit(1));
					}
				}
			}

			Dictionary<MappedOrder, (string EntitiesAttribute, List<OrdersShipmentData> OrderShipmentData)> shipmentsToCreate = new Dictionary<MappedOrder, (string EntitiesAttribute, List<OrdersShipmentData> OrderShipmentData)>();
			foreach (MappedOrder order in bucket.Orders)
			{
				DetailInfo addressInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderAddress && d.LocalID == order.LocalID);
				if (addressInfo != null)
				{
					var externOrder = orderDataProvider.GetByID(order.ExternID);
					if (externOrder?.StatusId == OrderStatuses.Cancelled.GetHashCode() || externOrder?.StatusId == OrderStatuses.Completed.GetHashCode() || externOrder?.StatusId == OrderStatuses.Refunded.GetHashCode())
						throw new PXException(BCMessages.InvalidOrderStatusforShipment, order?.ExternID, externOrder?.Status);

					//If for some reason existing shipment is not dleted try deleting 
					if (notFound.Count > 0)
					{
						var existingshipments = orderShipmentRestDataProvider.Get(order.ExternID);
						if (existingshipments?.Count > 0)
						{
							existingshipments.Where(x => notFound.Contains(x.Id.ToString()))?.ForEach(y =>
							{
								orderShipmentRestDataProvider.Delete(y.Id.ToString(), y.OrderId.ToString());
								notFound.Remove(y.Id.ToString());
							});
						}
					}

					OrdersShipmentData shipmentData = ordersShipmentData.Сlone();
					shipmentData.OrderAddressId = addressInfo.ExternID.ToInt().Value;
					shipmentData.ShipmentItems = ShipmentItems?.Where(x => x.OrderID == order.ExternID)?.ToList();

					if (ordersShipmentData.ShipmentItems?.Count > 0 && (shipmentData.ShipmentItems?.All(x => x.PackageId != null) ?? false))
					{
						var packages = shipmentData.ShipmentItems.GroupBy(x => x.PackageId).ToDictionary(x => x.Key, x => x.ToList());
						List<OrdersShipmentData> packageShipmentOfSameOrder = new List<OrdersShipmentData>();
						foreach (var package in packages)
						{
							shipmentData.ShipmentItems = package.Value;
							shipmentData.TrackingNumber = obj.Local.Shipment.Packages.FirstOrDefault(x => x.NoteID?.Value == package.Key)?.TrackingNbr?.Value;
							packageShipmentOfSameOrder.Add(shipmentData.Сlone(true));
						}
						shipmentsToCreate.Add(order, (BCEntitiesAttribute.ShipmentBoxLine, packageShipmentOfSameOrder));
					}
					else
					{
						shipmentsToCreate.Add(order, (BCEntitiesAttribute.ShipmentLine, new List<OrdersShipmentData>()
						{
							{ shipmentData }
						}));
					}
				}
			}

			//Validation: tracking numbers matching and delitiong of matched shipments in BC
			TryDeleteExistingShipments(shipmentsToCreate);


			//Create all shipments for given order
			foreach (KeyValuePair<MappedOrder, (string EntitiesAttribute, List<OrdersShipmentData> OrderShipmentData)> pair in shipmentsToCreate)
			{
				foreach (OrdersShipmentData shipmentData in pair.Value.OrderShipmentData)
				{
					OrdersShipmentData data = orderShipmentRestDataProvider.Create(shipmentData, pair.Key.ExternID);
					obj.With(_ => { _.ExternID = null; return _; }).AddExtern(data, new object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedUT.ToDate());
					obj.AddDetail(pair.Value.EntitiesAttribute, pair.Key.LocalID, new object[] { data.OrderId, data.Id }.KeyCombine());
					key.Append(key.Length > 0 ? "|" + obj.ExternID : obj.ExternID);
				}

				OrderStatus orderStatus = new OrderStatus();
				if (obj.Local.ShipmentType.Value == SOShipmentType.Invoice)
					orderStatus.StatusId = OrderStatuses.Completed.GetHashCode();
				else
					orderStatus.StatusId = BCSalesOrderProcessor.ConvertStatus(pair.Key.Local.Status?.Value).GetHashCode();
				orderStatus = orderDataProvider.Update(orderStatus, pair.Key.ExternID);

				pair.Key.AddExtern(null, orderStatus.Id?.ToString(), orderStatus.DateModifiedUT.ToDate());
				UpdateStatus(pair.Key, null);
			}

			if (obj.Extern.OrderAddressId != 0)
				obj.ExternID = key.ToString().TrimExternID();

			UpdateStatus(obj, operation);
		}

		#region ShipmentSavingSection
		public virtual void TryDeleteExistingShipments(Dictionary<MappedOrder, (string EntitiesAttribute, List<OrdersShipmentData> OrderShipmentData)> shipmentsToCreate)
		{
			List<Tuple<string, string>> iDsToBeDeleted = new List<Tuple<string, string>>();
			foreach (KeyValuePair<MappedOrder, (string EntitiesAttribute, List<OrdersShipmentData> OrderShipmentData)> pair in shipmentsToCreate)
			{
				//Validation items lists
				List<OrdersShipmentItem> itemsToDelete = new List<OrdersShipmentItem>();
				List<OrdersShipmentItem> itemsToShip = new List<OrdersShipmentItem>();
				List<OrdersShipmentItem> itemsToRemainExternal = new List<OrdersShipmentItem>();

				List<OrdersShipmentData> existingshipments = orderShipmentRestDataProvider.Get(pair.Key.ExternID);
				List<OrdersProductData> orderProducts = orderProductsRestDataProvider.Get(pair.Key.ExternID);

				foreach (OrdersShipmentData shipmentData in pair.Value.OrderShipmentData)
				{
					itemsToShip.AddRange(shipmentData.ShipmentItems);
					OrdersShipmentData matchingShipment = existingshipments?.FirstOrDefault(i => i.TrackingNumber == shipmentData.TrackingNumber);
					if (matchingShipment != null)
					{
						iDsToBeDeleted.Add(Tuple.Create(pair.Key.ExternID, matchingShipment.Id.ToString()));
						itemsToDelete.AddRange(matchingShipment.ShipmentItems);
					}
				}
				foreach (var shipment in existingshipments ?? new List<OrdersShipmentData>())
				{
					if (!pair.Value.OrderShipmentData.Any(i => String.Equals(shipment.TrackingNumber, i.TrackingNumber, StringComparison.InvariantCultureIgnoreCase) && !String.IsNullOrEmpty(shipment.TrackingNumber)))
						itemsToRemainExternal.AddRange(shipment.ShipmentItems);
				}
				ValidateOrderShipments(itemsToShip, itemsToDelete, itemsToRemainExternal, orderProducts);
			}
			foreach (var idsPair in iDsToBeDeleted)
				orderShipmentRestDataProvider.Delete(idsPair.Item2, idsPair.Item1);
		}

		public virtual void ValidateOrderShipments(IList<OrdersShipmentItem> itemsToShip, IList<OrdersShipmentItem> itemsToDelete, IList<OrdersShipmentItem> itemsToRemainExternal, List<OrdersProductData> orderDetails)
		{
			Dictionary<int, int> itemsToShipDict = itemsToShip.GroupBy(i => i.OrderProductId).ToDictionary(group => (int)group.Key, group => group.Sum(k => k.Quantity));
			Dictionary<int, int> itemsToDeleteDict = itemsToDelete.GroupBy(i => i.OrderProductId).ToDictionary(group => (int)group.Key, group => group.Sum(k => k.Quantity));
			Dictionary<int, int> itemsToRemainExternalDict = itemsToRemainExternal.GroupBy(i => i.OrderProductId).ToDictionary(group => (int)group.Key, group => group.Sum(k => k.Quantity));
			foreach (var detail in orderDetails)
				if ((itemsToShipDict.ContainsKey((int)detail.Id) ? itemsToShipDict[(int)detail.Id] : 0)
					- (itemsToDeleteDict.ContainsKey((int)detail.Id) ? itemsToDeleteDict[(int)detail.Id] : 0)
					+ (itemsToRemainExternalDict.ContainsKey((int)detail.Id) ? itemsToRemainExternalDict[(int)detail.Id] : 0) > detail.Quantity)
					throw new PXException(BCMessages.ShipmentCannotBeExported, detail.Sku);
		}
		#endregion

		#region ShipmentGetSection
		protected virtual void GetOrderShipment(BCShipments bCShipments)
		{
			if (bCShipments.ShipmentType.Value == SOShipmentType.DropShip)
				GetDropShipmentByShipmentNbr(bCShipments);
			else if (bCShipments.ShipmentType.Value == SOShipmentType.Invoice)
				GetInvoiceByShipmentNbr(bCShipments);
			else
				bCShipments.Shipment = cbapi.GetByID<Shipment>(bCShipments.ShippingNoteID.Value);
		}

		protected virtual void GetInvoiceByShipmentNbr(BCShipments bCShipment)
		{
			bCShipment.Shipment = new Shipment();
			bCShipment.Shipment.Details = new List<ShipmentDetail>();

			foreach (PXResult<ARTran, SOOrder> item in PXSelectJoin<ARTran, 
				InnerJoin<SOOrder, On<ARTran.sOOrderType, Equal<SOOrder.orderType>, And<ARTran.sOOrderNbr, Equal<SOOrder.orderNbr>>>>,
				Where<ARTran.refNbr, Equal<Required<ARTran.refNbr>>, And<ARTran.sOOrderType, Equal<Required<ARTran.sOOrderType>>>>>
				.Select(this, bCShipment.ShipmentNumber.Value, bCShipment.OrderType.Value))
			{
				ARTran line = item.GetItem<ARTran>();
				ShipmentDetail detail = new ShipmentDetail();
				detail.OrderNbr = line.SOOrderNbr.ValueField();
				detail.OrderLineNbr = line.SOOrderLineNbr.ValueField();
				detail.OrderType = line.SOOrderType.ValueField();
				bCShipment.Shipment.Details.Add(detail);
			}
		}
		protected virtual void GetDropShipmentByShipmentNbr(BCShipments bCShipments)
		{
			bCShipments.POReceipt = new PurchaseReceipt();
			bCShipments.POReceipt.ShipmentNbr = bCShipments.ShipmentNumber;
			bCShipments.POReceipt.VendorRef = bCShipments.VendorRef;
			bCShipments.POReceipt.Details = new List<PurchaseReceiptDetail>();

			foreach (PXResult<SOLineSplit, POOrder, SOOrder> item in PXSelectJoin<SOLineSplit,
				InnerJoin<POOrder, On<POOrder.orderNbr, Equal<SOLineSplit.pONbr>>,
				InnerJoin<SOOrder, On<SOLineSplit.orderNbr, Equal<SOOrder.orderNbr>>>>,
				Where<SOLineSplit.pOReceiptNbr, Equal<Required<SOLineSplit.pOReceiptNbr>>>>
			.Select(this, bCShipments.ShipmentNumber.Value))
			{
				SOLineSplit lineSplit = item.GetItem<SOLineSplit>();
				SOOrder line = item.GetItem<SOOrder>();
				POOrder poOrder = item.GetItem<POOrder>();
				PurchaseReceiptDetail detail = new PurchaseReceiptDetail();
				detail.SOOrderNbr = lineSplit.OrderNbr.ValueField();
				detail.SOLineNbr = lineSplit.LineNbr.ValueField();
				detail.SOOrderType = lineSplit.OrderType.ValueField();
				detail.ReceiptQty = lineSplit.ShippedQty.ValueField();
				detail.ShipVia = poOrder.ShipVia.ValueField();
				detail.SONoteID = line.NoteID.ValueField();
				bCShipments.POReceipt.Details.Add(detail);
			}
		}
		protected virtual EntityStatus GetDropShipment(BCShipmentEntityBucket bucket, BCShipments bCShipments)
		{
			if (bCShipments.ShipmentNumber == null) return EntityStatus.None;
			GetDropShipmentByShipmentNbr(bCShipments);
			if (bCShipments.POReceipt == null) return EntityStatus.None;

			MappedShipment obj = bucket.Shipment = bucket.Shipment.Set(bCShipments, bCShipments.ShippingNoteID.Value, bCShipments.LastModified.Value);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

			IEnumerable<PurchaseReceiptDetail> lines = bCShipments.POReceipt.Details
				.GroupBy(r => new { OrderType = r.SOOrderType.Value, OrderNbr = r.SOOrderNbr.Value })
				.Select(r => r.First());
			foreach (PurchaseReceiptDetail line in lines)
			{
				SalesOrder orderImpl = cbapi.Get<SalesOrder>(new SalesOrder() { OrderType = line.SOOrderType.Value.SearchField(), OrderNbr = line.SOOrderNbr.Value.SearchField() });
				if (orderImpl == null) throw new PXException(BCMessages.OrderNotFound, bCShipments.POReceipt.ShipmentNbr.Value);
				MappedOrder orderObj = new MappedOrder(orderImpl, orderImpl.SyncID, orderImpl.SyncTime);
				EntityStatus orderStatus = EnsureStatus(orderObj);

				if (orderObj.ExternID == null) throw new PXException(BCMessages.OrderNotSyncronized, orderImpl.OrderNbr.Value);
				bucket.Orders.Add(orderObj);
			}
			return status;
		}
		protected virtual EntityStatus GetShipment(BCShipmentEntityBucket bucket, BCShipments bCShipment)
		{
			if (bCShipment.ShippingNoteID == null || bCShipment.ShippingNoteID.Value == Guid.Empty) return EntityStatus.None;
			bCShipment.Shipment = cbapi.GetByID<Shipment>(bCShipment.ShippingNoteID.Value);
			if (bCShipment.Shipment == null) return EntityStatus.None;

			MappedShipment obj = bucket.Shipment = bucket.Shipment.Set(bCShipment, bCShipment.ShippingNoteID.Value, bCShipment.LastModified.Value);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

			IEnumerable<ShipmentDetail> lines = bCShipment.Shipment.Details
				.GroupBy(r => new { OrderType = r.OrderType.Value, OrderNbr = r.OrderNbr.Value })
				.Select(r => r.First());
			foreach (ShipmentDetail line in lines)
			{
				SalesOrder orderImpl = cbapi.Get<SalesOrder>(new SalesOrder() { OrderType = line.OrderType.Value.SearchField(), OrderNbr = line.OrderNbr.Value.SearchField() });
				if (orderImpl == null) throw new PXException(BCMessages.OrderNotFound, bCShipment.Shipment.ShipmentNbr.Value);
				MappedOrder orderObj = new MappedOrder(orderImpl, orderImpl.SyncID, orderImpl.SyncTime);
				EntityStatus orderStatus = EnsureStatus(orderObj);

				if (orderObj.ExternID == null) throw new PXException(BCMessages.OrderNotSyncronized, orderImpl.OrderNbr.Value);
				bucket.Orders.Add(orderObj);
			}
			return status;
		}
		protected virtual EntityStatus GetInvoice(BCShipmentEntityBucket bucket, BCShipments bCShipment)
		{
			if (bCShipment.ShipmentNumber == null) return EntityStatus.None;
			GetInvoiceByShipmentNbr(bCShipment);

			MappedShipment obj = bucket.Shipment = bucket.Shipment.Set(bCShipment, bCShipment.ShippingNoteID.Value, bCShipment.LastModified.Value);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);

			IEnumerable<ShipmentDetail> lines = bCShipment.Shipment.Details
				.GroupBy(r => new { OrderType = r.OrderType.Value, OrderNbr = r.OrderNbr.Value })
				.Select(r => r.First());
			foreach (ShipmentDetail line in lines)
			{
				SalesOrder orderImpl = cbapi.Get<SalesOrder>(new SalesOrder() { OrderType = line.OrderType.Value.SearchField(), OrderNbr = line.OrderNbr.Value.SearchField() });
				if (orderImpl == null) throw new PXException(BCMessages.OrderNotFound, bCShipment.Shipment.ShipmentNbr.Value);
				MappedOrder orderObj = new MappedOrder(orderImpl, orderImpl.SyncID, orderImpl.SyncTime);
				EntityStatus orderStatus = EnsureStatus(orderObj);

				if (orderObj.ExternID == null) throw new PXException(BCMessages.OrderNotSyncronized, orderImpl.OrderNbr.Value);
				bucket.Orders.Add(orderObj);
			}
			return status;
		}
		#endregion

		#region ShipmentMappingSection

		protected virtual void MapDropShipment(BCShipmentEntityBucket bucket, MappedShipment obj, PurchaseReceipt impl)
		{
			OrdersShipmentData shipmentData = obj.Extern = new OrdersShipmentData();
			shipmentData.ShipmentItems = new List<OrdersShipmentItem>();
			shipmentData.ShippingProvider = string.Empty;
			var shipvia = impl.Details.FirstOrDefault(x => !string.IsNullOrEmpty(x.ShipVia?.Value))?.ShipVia?.Value ?? string.Empty;
			shipmentData.ShippingMethod = shipvia;

			if (!string.IsNullOrEmpty(shipvia))
			{
				var shippingmethods = shippingMappings.Where(x => x.CarrierID == shipvia)?.ToList();
				if (shippingmethods?.Count == 1)
					shipmentData.ShippingMethod = shippingmethods.FirstOrDefault().ShippingMethod;
			}
			shipmentData.TrackingNumber = impl.VendorRef?.Value;

			foreach (MappedOrder order in bucket.Orders)
			{
				DetailInfo addressInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderAddress && d.LocalID == order.LocalID);
				if (addressInfo != null)
					shipmentData.OrderAddressId = addressInfo.ExternID.ToInt().Value;
				foreach (PurchaseReceiptDetail line in impl.Details ?? new List<PurchaseReceiptDetail>())
				{
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.SOOrderType.Value && order.Local.OrderNbr.Value == line.SOOrderNbr.Value && d.LineNbr.Value == line.SOLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) throw new PXException(BCMessages.OrderShippingLineSyncronized, orderLine.InventoryID?.Value, order.Local.OrderNbr.Value, order?.ExternID);


					OrdersShipmentItem shipItem = new OrdersShipmentItem();
					shipItem.OrderProductId = lineInfo.ExternID.ToInt();
					shipItem.Quantity = (int)line.ReceiptQty.Value;
					shipItem.OrderID = order.ExternID;

					shipmentData.ShipmentItems.Add(shipItem);
				}
			}
		}
		protected virtual void MapShipment(BCShipmentEntityBucket bucket, MappedShipment obj, Shipment impl)
		{
			OrdersShipmentData shipmentData = obj.Extern = new OrdersShipmentData();
			shipmentData.ShipmentItems = new List<OrdersShipmentItem>();
			shipmentData.ShippingProvider = string.Empty;
			shipmentData.ShippingMethod = impl.ShipVia?.Value ?? string.Empty;
			if (!string.IsNullOrEmpty(impl.ShipVia?.Value))
			{
				var shippingmethods = shippingMappings.Where(x => x.CarrierID == impl.ShipVia?.Value)?.ToList();
				if (shippingmethods?.Count == 1)
					shipmentData.ShippingMethod = shippingmethods.FirstOrDefault().ShippingMethod;

			}
			var PackageDetails = PXSelect<SOShipLineSplitPackage,
			Where<SOShipLineSplitPackage.shipmentNbr, Equal<Required<SOShipLineSplitPackage.shipmentNbr>>
			>>.Select(this, impl.ShipmentNbr?.Value).RowCast<SOShipLineSplitPackage>().ToList();
			var packages = impl.Packages ?? new List<ShipmentPackage>();
			if (packages.Count == 1)
				shipmentData.TrackingNumber = packages.FirstOrDefault()?.TrackingNbr?.Value;
			else
				foreach (ShipmentPackage package in packages)
				{
					var detail = PackageDetails.Where(x => x.PackageLineNbr == package.LineNbr?.Value && x.PackedQty != 0)?.ToList() ?? new List<SOShipLineSplitPackage>();
					if (detail.Count == 0)//if box is emty
						throw new PXException(BCMessages.BoxesWithoutItems, impl.ShipmentNbr.Value, package.TrackingNbr?.Value);
					package.ShipmentLineNbr.AddRange(detail.Select(x => new Tuple<int?, decimal?>(x.ShipmentLineNbr, x.PackedQty)));
				}

			foreach (MappedOrder order in bucket.Orders)
			{
				DetailInfo addressInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderAddress && d.LocalID == order.LocalID);
				if (addressInfo != null)
					shipmentData.OrderAddressId = addressInfo.ExternID.ToInt().Value;

				foreach (ShipmentDetail line in impl.Details ?? new List<ShipmentDetail>())
				{
					List<ShipmentPackage> details = null;
					if (packages.Count > 1)
					{
						details = packages.Where(x => x.ShipmentLineNbr.Select(y => y.Item1).Contains(line.LineNbr?.Value))?.ToList();
						if (details == null || (details != null && (details.SelectMany(x => x.ShipmentLineNbr)?.Where(x => x.Item1 == line.LineNbr.Value && x.Item2 != null)?.Sum(x => x.Item2) ?? 0) != line.ShippedQty?.Value))//  check if shipped item quatity matches the  quantity of item in package
							throw new PXException(BCMessages.ItemsWithoutBoxes, impl.ShipmentNbr.Value, line.InventoryID?.Value);
					}
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.OrderType.Value && order.Local.OrderNbr.Value == line.OrderNbr.Value && d.LineNbr.Value == line.OrderLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) throw new PXException(BCMessages.OrderShippingLineSyncronized, orderLine.InventoryID?.Value, order.Local.OrderNbr.Value, order?.ExternID);

					if (details != null)
					{
						foreach (var detail in details)
						{
							OrdersShipmentItem shipItem = new OrdersShipmentItem();
							shipItem.OrderProductId = lineInfo.ExternID.ToInt();
							shipItem.OrderID = order.ExternID;
							shipItem.Quantity = (int)(detail?.ShipmentLineNbr.Where(x => x.Item1 == line.LineNbr?.Value)?.Sum(x => x.Item2) ?? 0);
							shipItem.PackageId = detail?.NoteID?.Value;
							shipmentData.ShipmentItems.Add(shipItem);
						}
					}
					else
					{
						OrdersShipmentItem shipItem = new OrdersShipmentItem();
						shipItem.OrderProductId = lineInfo.ExternID.ToInt();
						shipItem.OrderID = order.ExternID;
						shipItem.Quantity = (int)line.ShippedQty.Value;
						shipmentData.ShipmentItems.Add(shipItem);
					}
				}

			}
		}

		protected DetailInfo MatchOrderLineFromExtern(string externalOrderId, string identifyKey)
		{
			DetailInfo lineInfo = null;
			if (string.IsNullOrEmpty(externalOrderId) || string.IsNullOrEmpty(identifyKey))
				return lineInfo;
			var orderLineDetails = orderProductsRestDataProvider.GetAll(externalOrderId).ToList();
			var matchedLine = orderLineDetails?.FirstOrDefault(x => string.Equals(x?.Sku, identifyKey, StringComparison.OrdinalIgnoreCase));
			if (matchedLine != null && matchedLine?.Id.HasValue == true)
			{
				lineInfo = new DetailInfo(BCEntitiesAttribute.OrderLine, null, matchedLine.Id.ToString());
			}
			return lineInfo;
		}
		#endregion
		#endregion
	}
}