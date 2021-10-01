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
using PX.Objects.PO;
using PX.Data.BQL.Fluent;
using PX.Api.ContractBased.Models;
using System.Reflection;
using PX.Objects.IN;

namespace PX.Commerce.Shopify
{
	public class SPShipmentEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Shipment;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary }.Concat(Orders).ToArray();

		public MappedShipment Shipment;
		public List<MappedOrder> Orders = new List<MappedOrder>();
	}

	public class SPShipmentsRestrictor : BCBaseRestrictor, IRestrictor
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

					if (obj.Local.OrderNoteIds != null)
					{
						BCBindingExt binding = processor.GetBindingExt<BCBindingExt>();

						Boolean anyFound = false;
						foreach (var orderNoeId in obj.Local?.OrderNoteIds)
						{
							if (processor.SelectStatus(BCEntitiesAttribute.Order, orderNoeId) == null) continue;

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

	[BCProcessor(typeof(SPConnector), BCEntitiesAttribute.Shipment, BCCaptions.Shipment,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		ExternTypes = new Type[] { typeof(FulfillmentData) },
		LocalTypes = new Type[] { typeof(BCShipments) },
		GIScreenID = BCConstants.GenericInquiryShipmentDetails,
		GIResult = typeof(BCShipmentsResult),
		DetailTypes = new String[] { BCEntitiesAttribute.ShipmentLine, BCCaptions.ShipmentLine, BCEntitiesAttribute.ShipmentBoxLine, BCCaptions.ShipmentLineBox },
		AcumaticaPrimaryType = typeof(PX.Objects.SO.SOShipment),
		AcumaticaPrimarySelect = typeof(PX.Objects.SO.SOShipment.shipmentNbr),
		URL = "orders/{0}",
		Requires = new string[] { BCEntitiesAttribute.Order }
	)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-Shipments" })]
	public class SPShipmentProcessor : BCProcessorSingleBase<SPShipmentProcessor, SPShipmentEntityBucket, MappedShipment>, IProcessor
	{
		protected OrderRestDataProvider orderDataProvider;
		protected FulfillmentRestDataProvider fulfillmentDataProvider;
		protected IEnumerable<InventoryLocationData> inventoryLocations;
		protected List<BCShippingMappings> shippingMappings;
		protected BCBinding currentBinding;
		protected BCBindingExt currentBindingExt;
		protected BCBindingShopify currentShopifySettings;
		private long? defaultLocationId;

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			currentBinding = GetBinding();
			currentBindingExt = GetBindingExt<BCBindingExt>();
			currentShopifySettings = GetBindingExt<BCBindingShopify>();

			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());

			orderDataProvider = new OrderRestDataProvider(client);
			fulfillmentDataProvider = new FulfillmentRestDataProvider(client);

			shippingMappings = PXSelectReadonly<BCShippingMappings,
				Where<BCShippingMappings.bindingID, Equal<Required<BCShippingMappings.bindingID>>>>
				.Select(this, Operation.Binding).Select(x => x.GetItem<BCShippingMappings>()).ToList();
			inventoryLocations = ConnectorHelper.GetConnector(currentBinding.ConnectorType)?.GetExternalInfo<InventoryLocationData>(BCObjectsConstants.BCInventoryLocation, currentBinding.BindingID)?.Where(x => x.Active == true);
			if (inventoryLocations == null || inventoryLocations.Count() == 0)
			{
				throw new PXException(ShopifyMessages.InventoryLocationNotFound);
			}
			else
				defaultLocationId = inventoryLocations.First().Id;
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
			BCShipments giResult = (new BCShipments()
			{
				BindingID = currentBinding.BindingID.ValueField(),
				ShippingNoteID = localID.ValueField()
			});
			giResult.Results = cbapi.GetGIResult<BCShipmentsResult>(giResult, BCConstants.GenericInquiryShipmentDetails).ToList();

			if (giResult?.Results == null) return null;
			MapFilterFields(giResult?.Results, giResult);
			GetOrderShipment(giResult);
			if (giResult.Shipment == null && giResult.POReceipt == null) return null;
			MappedShipment obj = new MappedShipment(giResult, giResult.ShippingNoteID.Value, giResult.LastModified?.Value);
			return obj;


		}
		public override MappedShipment PullEntity(String externID, String externalInfo)
		{
			FulfillmentData data = fulfillmentDataProvider.GetByID(externID.KeySplit(0), externID.KeySplit(1));
			if (data == null) return null;

			MappedShipment obj = new MappedShipment(data, new Object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedAt.ToDate(false), data.CalculateHash());

			return obj;
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}
		public override EntityStatus GetBucketForImport(SPShipmentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			bucket.Shipment = bucket.Shipment.Set(new FulfillmentData(), syncstatus.ExternID, syncstatus.ExternTS);

			return EntityStatus.None;
		}

		public override void MapBucketImport(SPShipmentEntityBucket bucket, IMappedEntity existing)
		{
		}
		public override void SaveBucketImport(SPShipmentEntityBucket bucket, IMappedEntity existing, String operation)
		{
		}
		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			var minDate = minDateTime == null || (minDateTime != null && currentBindingExt.SyncOrdersFrom != null && minDateTime < currentBindingExt.SyncOrdersFrom) ? currentBindingExt.SyncOrdersFrom : minDateTime;
			var giResult = cbapi.GetGIResult<BCShipmentsResult>(new BCShipments()
			{
				BindingID = currentBinding.BindingID.ValueField(),
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

		protected virtual void MapFilterFields(List<BCShipmentsResult> results, BCShipments impl)
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

		public override EntityStatus GetBucketForExport(SPShipmentEntityBucket bucket, BCSyncStatus syncstatus)
		{
			SOOrderShipments impl = new SOOrderShipments();
			BCShipments giResult = new BCShipments()
			{
				ShippingNoteID = syncstatus.LocalID.ValueField(),
				BindingID = currentBinding.BindingID.ValueField()
			};
			giResult.Results = cbapi.GetGIResult<BCShipmentsResult>(giResult, BCConstants.GenericInquiryShipmentDetails).ToList();
			if (giResult?.Results == null) return EntityStatus.None;

			MapFilterFields(giResult?.Results, giResult);
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


		public override void MapBucketExport(SPShipmentEntityBucket bucket, IMappedEntity existing)
		{
			MappedShipment obj = bucket.Shipment;
			if (obj.Local?.Confirmed?.Value == false) throw new PXException(BCMessages.ShipmentNotConfirmed);
			List<BCLocations> locationMappings = new List<BCLocations>();
			if (currentBindingExt.WarehouseMode == BCWarehouseModeAttribute.SpecificWarehouse)
			{
				foreach (PXResult<BCLocations, INSite, INLocation> result in PXSelectJoin<BCLocations,
					InnerJoin<INSite, On<INSite.siteID, Equal<BCLocations.siteID>>,
					InnerJoin<INLocation, On<INLocation.siteID, Equal<BCLocations.siteID>, And<BCLocations.locationID, IsNull, Or<BCLocations.locationID, Equal<INLocation.locationID>>>>>>,
					Where<BCLocations.bindingID, Equal<Required<BCLocations.bindingID>>, And<BCLocations.mappingDirection, Equal<BCMappingDirectionAttribute.import>>>,
					OrderBy<Desc<BCLocations.mappingDirection>>>.Select(this, currentBinding.BindingID))
				{
					var bl = (BCLocations)result;
					var site = (INSite)result;
					var iNLocation = (INLocation)result;
					bl.SiteCD = site.SiteCD.Trim();
					bl.LocationCD = bl.LocationID == null ? null : iNLocation.LocationCD.Trim();
					locationMappings.Add(bl);
				}
			}
			if (obj.Local.ShipmentType.Value == SOShipmentType.DropShip)
			{
				PurchaseReceipt impl = obj.Local.POReceipt;
				MapDropShipment(bucket, obj, impl, locationMappings);
			}
			else if (obj.Local.ShipmentType.Value == SOShipmentType.Issue)
			{
				Shipment impl = obj.Local.Shipment;
				MapShipment(bucket, obj, impl, locationMappings);
			}
			else
			{

				Shipment impl = obj.Local.Shipment;
				MapInvoice(bucket, obj, impl, locationMappings);
			}
		}

		public override CustomField GetLocalCustomField(SPShipmentEntityBucket bucket, string viewName, string fieldName)
		{
			MappedShipment obj = bucket.Shipment;
			BCShipments impl = obj.Local;
			if (impl?.Results?.Count() > 0)
				return impl.Results[0].Custom?.Where(x => x.ViewName == viewName && x.FieldName == fieldName).FirstOrDefault();
			else return null;
		}

		public override void SaveBucketExport(SPShipmentEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedShipment obj = bucket.Shipment;

			StringBuilder key = new StringBuilder();
			String origExternId = obj.ExternID;
			var ShipmentItems = obj.Extern.LineItems;
			string errorMsg = string.Empty;
			List<DetailInfo> existingDetails = new List<DetailInfo>(obj.Details);

			if (existingDetails != null)
			{
				if (ShipmentItems.All(x => x.PackageId != null))
				{
					foreach (var detail in existingDetails)
					{
						CancelFullfillment(bucket, detail);
					}
				}
				else
				{
					foreach (var detail in existingDetails)
					{
						bool orderExist = bucket.Orders.Any(x => x.LocalID == detail.LocalID);
						if (detail.EntityType == BCEntitiesAttribute.ShipmentBoxLine || !orderExist)
						{
							CancelFullfillment(bucket, detail);
						}
					}
				}
			}
			obj.ClearDetails();

			Dictionary<MappedOrder, (string EntitiesAttribute, List<FulfillmentData> OrderFulfillmentData)> shipmentsToCreate = new Dictionary<MappedOrder, (string EntitiesAttribute, List<FulfillmentData> OrderFulfillmentData)>();
			foreach (MappedOrder order in bucket.Orders)
			{
				FulfillmentData shipmentData = obj.Extern.Сlone();
				shipmentData.LineItems = ShipmentItems?.Where(x => x.OrderId == order.ExternID.ToLong())?.ToList();

				if (shipmentData.LineItems.All(x => x.PackageId != null))
				{
					var externOrder = orderDataProvider.GetByID(order.ExternID, false, false, false);
					if (externOrder?.CancelledAt != null || externOrder?.FinancialStatus == OrderFinancialStatus.Refunded)
						throw new PXException(BCMessages.InvalidOrderStatusforShipment, externOrder?.Id, externOrder?.CancelledAt != null ? OrderStatus.Cancelled.ToString() : OrderFinancialStatus.Refunded.ToString());
					shipmentData.TrackingNumbers.Clear();
					var packages = shipmentData.LineItems.GroupBy(x => x.PackageId).ToDictionary(x => x.Key, x => x.ToList());
					List<FulfillmentData> packageShipmentOfSameOrder = new List<FulfillmentData>();

					foreach (var package in packages)
					{
						shipmentData.LineItems = package.Value;
						shipmentData.TrackingNumbers = new List<string>();
						var trackingNumber = obj.Local.Shipment.Packages.FirstOrDefault(x => x.NoteID?.Value == package.Key)?.TrackingNbr?.Value;
						if (!string.IsNullOrEmpty(trackingNumber)) shipmentData.TrackingNumbers.Add(trackingNumber);
						//Check the fulfillments in the extern order
						packageShipmentOfSameOrder.Add(shipmentData.Сlone(true));

					}
					shipmentsToCreate.Add(order, (BCEntitiesAttribute.ShipmentBoxLine, packageShipmentOfSameOrder));

				}
				else
				{
					shipmentsToCreate.Add(order, (BCEntitiesAttribute.ShipmentLine, new List<FulfillmentData>()
						{
							{ shipmentData }
						}));
				}
			}

			//Validation: tracking numbers matching and delitiong of matched shipments in BC
			Dictionary<string, (bool Fulfilled, bool Errored, string ErrorMsg)> fulfillmentStatuses = ValidateAllShipmentsCanExport(shipmentsToCreate, existingDetails);

			//Create all shipments for given order
			foreach (KeyValuePair<MappedOrder, (string EntitiesAttribute, List<FulfillmentData> OrderFulfillmentData)> pair in shipmentsToCreate)
			{
				if (fulfillmentStatuses[pair.Key.ExternID].Errored == true)
					continue;
				DateTime lastModifiedOrderAt = new DateTime();
				foreach (FulfillmentData shipmentData in pair.Value.OrderFulfillmentData)
				{
					FulfillmentData data = SaveFullfillment(pair.Key, shipmentData);
					if (fulfillmentStatuses[pair.Key.ExternID].Fulfilled != true && lastModifiedOrderAt < data.DateModifiedAt)
						lastModifiedOrderAt = (DateTime)data.DateModifiedAt;
					obj.With(_ => { _.ExternID = null; return _; }).AddExtern(data, new object[] { data.OrderId, data.Id }.KeyCombine(), data.DateCreatedAt.ToDate());
					obj.AddDetail(pair.Value.EntitiesAttribute, pair.Key.LocalID, new object[] { data.OrderId, data.Id }.KeyCombine());
					key.Append(key.Length > 0 ? "|" + obj.ExternID : obj.ExternID);
				}
				//If all items shipped and order changed status we need to take the timestamp again
				if (fulfillmentStatuses[pair.Key.ExternID].Fulfilled == true)
					lastModifiedOrderAt = (DateTime)orderDataProvider.GetByID(pair.Key.ExternID, false, false, false).DateModifiedAt;
				pair.Key.AddExtern(null, pair.Key.ExternID?.ToString(), lastModifiedOrderAt.ToDate(false));
				UpdateStatus(pair.Key, null);
			}

			obj.ExternID = key.ToString()?.TrimExternID();
			if (fulfillmentStatuses.Any(i => i.Value.Errored == true))
				UpdateStatus(obj, BCSyncOperationAttribute.ExternFailed, String.Join("", fulfillmentStatuses.Values.Where(i => i.Errored == true).Select(i => i.ErrorMsg)));
			else
				UpdateStatus(obj, operation);
		}

		private void CancelFullfillment(SPShipmentEntityBucket bucket, DetailInfo detail)
		{
			if (detail.ExternID.HasParent())
			{
				try
				{
					fulfillmentDataProvider.CancelFulfillment(detail.ExternID.KeySplit(0), detail.ExternID.KeySplit(1));
				}
				catch (Exception ex)
				{
					Log(bucket?.Primary?.SyncID, SyncDirection.Export, ex);
				}
			}

		}
		public virtual Dictionary<string, (bool Fulfilled, bool Errored, string ErrorMsg)> ValidateAllShipmentsCanExport(
			Dictionary<MappedOrder, (string EntitiesAttribute, List<FulfillmentData> OrderFulfillmentData)> shipmentsToCreate,
			List<DetailInfo> existingDetails)
		{
			//To track if order entirely fulfilled (for ts update of order) and 
			Dictionary<string, (bool Fulfilled, bool Errored, string ErrorMsg)> fulFillmentStatuses = new Dictionary<string, (bool Fulfilled, bool Errored, string ErrorMsg)>();
			foreach (KeyValuePair<MappedOrder, (string EntitiesAttribute, List<FulfillmentData> OrderFulfillmentData)> pair in shipmentsToCreate)
			{
				string errorMsg = string.Empty;
				bool fulfillmentStatus = false, errored = false;
				try
				{
					var externOrder = orderDataProvider.GetByID(pair.Key.ExternID, false, false, false);
					List<FulfillmentData> existingFulfillments = externOrder.Fulfillments.Where(i => i.Status == FulfillmentStatus.Success).ToList();
					//qtyByItem for all items of order quantities and qtyUsedOnOrder is for the amount of objects processed by external shipments, refunds and used to prevent overshipping
					Dictionary<long, int> qtyByItem = externOrder.LineItems.Where(i => i.RequiresShipping == true).ToDictionary(x => (long)x.Id, x => x.Quantity ?? 0);
					Dictionary<long, int> qtyUsedOnOrder = qtyByItem.ToDictionary(item => (long)item.Key, item =>
						externOrder.Fulfillments.Where(i => i.Status == FulfillmentStatus.Success).SelectMany(i => i.LineItems).Where(i => i.Id == item.Key).Sum(i => i.Quantity ?? 0) +
						externOrder.Refunds.SelectMany(i => i.RefundLineItems).Where(i => i.LineItemId == item.Key).Sum(i => i.Quantity ?? 0));

					List<long> idsToCancel = new List<long>();
					//We separately track not matched by code fulFillments to attemp matching them with existing fulfillments by item lines
					List<FulfillmentData> notTrackingCodeMatchedFulFillments = new List<FulfillmentData>();
					foreach (FulfillmentData fulfillmentData in pair.Value.OrderFulfillmentData)
					{
						FulfillmentData matchingFulfillment = existingFulfillments?.FirstOrDefault(i => !String.IsNullOrEmpty(i.TrackingNumbers.FirstOrDefault()) && i.TrackingNumbers.FirstOrDefault() == fulfillmentData.TrackingNumbers.FirstOrDefault());
						//If the fulfillment is not matched by lines and quantities, but with tracking number, we cannot modify it externally and need to cancel it first
						if (matchingFulfillment != null)
						{
							existingFulfillments.Remove(matchingFulfillment);
							if (matchingFulfillment.LineItems.All(i => fulfillmentData.LineItems.Any(x => x.Id == i.Id && x.Quantity == i.Quantity)) &&
								fulfillmentData.LineItems.All(i => matchingFulfillment.LineItems.Any(x => x.Id == i.Id && x.Quantity == i.Quantity)))
							{
								fulfillmentData.NotifyCustomer = false;
								fulfillmentData.Id = matchingFulfillment.Id;
								continue;
							}
							//Must cancel fulfillment because changing number of lines/item quantities impossible
							idsToCancel.Add((long)matchingFulfillment.Id);
							foreach (long id in qtyByItem.Keys)
								qtyUsedOnOrder[id] = qtyUsedOnOrder[id]
									- (matchingFulfillment?.LineItems?.FirstOrDefault(i => i.Id == id)?.Quantity ?? 0)
									+ (fulfillmentData.LineItems.FirstOrDefault(i => i.Id == id)?.Quantity ?? 0);
						}
						else
							notTrackingCodeMatchedFulFillments.Add(fulfillmentData);
					}

					foreach (FulfillmentData fulfillmentData in notTrackingCodeMatchedFulFillments)
					{
						var matchingFulfillments = existingFulfillments?.Where(i =>
							i.LineItems.All(x => fulfillmentData.LineItems.Any(item => item.Id == x.Id && item.Quantity == x.Quantity && x.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)) &&
							fulfillmentData.LineItems.All(x => i.LineItems.Any(item => item.Id == x.Id && item.Quantity == x.Quantity && item.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)));
						if (matchingFulfillments != null && matchingFulfillments.Count() > 1 && existingDetails != null)
                        {
							List<FulfillmentData> matches = new List<FulfillmentData>();
							foreach (var detail in existingDetails)
							{
								var ids = detail.ExternID?.Split(';');
								if (ids != null && ids.Count() == 2)
									matches.AddRange(matchingFulfillments.Where(mf => String.Equals(ids[1], mf.Id.ToString(), StringComparison.InvariantCultureIgnoreCase))?.ToList() ?? new List<FulfillmentData>());
							}
							matchingFulfillments = matches;
						}
						if (matchingFulfillments != null && matchingFulfillments.Count() == 1)
						{
							fulfillmentData.Id = matchingFulfillments.FirstOrDefault()?.Id;
							fulfillmentData.NotifyCustomer = false;
							existingFulfillments.Remove(matchingFulfillments.FirstOrDefault());
						}
						else
							foreach (long id in qtyByItem.Keys)
								qtyUsedOnOrder[id] = qtyUsedOnOrder[id] + (fulfillmentData.LineItems.FirstOrDefault(i => i.Id == id)?.Quantity ?? 0);
					}
					//verify that after exporting we will not exceed item quantity on all fulfillments and also predict if order will be entirely fulfilled
					fulfillmentStatus = qtyUsedOnOrder.Select(x =>
				   {
					   if (x.Value > qtyByItem[x.Key])
						   throw new PXException(BCMessages.ShipmentCannotBeExported, externOrder.LineItems.FirstOrDefault(i => i.Id == x.Key)?.Sku);
					   return x.Value < qtyByItem[x.Key] ? false : true;
				   }).All(i => i == true);
					if (idsToCancel.Count > 0)
						idsToCancel.ForEach(x => fulfillmentDataProvider.CancelFulfillment(pair.Key.ExternID.ToString(), x.ToString()));
				}
				catch (Exception ex)
				{
					Log(pair.Key?.SyncID, SyncDirection.Export, ex);
					errored = true;
					errorMsg = $"{ex.InnerException?.Message ?? ex.Message} \n";
				}
				fulFillmentStatuses.Add(pair.Key.ExternID, (fulfillmentStatus, errored, errorMsg));
			}
			return fulFillmentStatuses;
		}

		private FulfillmentData SaveFullfillment(MappedOrder order, FulfillmentData ordersShipmentData)
		{
			if (ordersShipmentData.Id != null)
				return fulfillmentDataProvider.Update(ordersShipmentData, order.ExternID, ordersShipmentData.Id.ToString());
			else
				return fulfillmentDataProvider.Create(ordersShipmentData, order.ExternID);
		}

		protected virtual void GetOrderShipment(BCShipments bCShipments)
		{
			if (bCShipments.ShipmentType?.Value == SOShipmentType.DropShip)
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

			foreach (PXResult<ARTran, SOOrder> item in PXSelectJoin<ARTran, InnerJoin<SOOrder, On<ARTran.sOOrderNbr, Equal<SOOrder.orderNbr>>>,
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

		protected virtual EntityStatus GetDropShipment(SPShipmentEntityBucket bucket, BCShipments bCShipments)
		{
			if (bCShipments.ShipmentNumber == null) return EntityStatus.None;
			GetDropShipmentByShipmentNbr(bCShipments);
			if (bCShipments.POReceipt == null || bCShipments.POReceipt?.Details?.Count == 0)
				return EntityStatus.None;

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
		protected virtual EntityStatus GetShipment(SPShipmentEntityBucket bucket, BCShipments bCShipment)
		{
			if (bCShipment.ShippingNoteID == null || bCShipment.ShippingNoteID.Value == Guid.Empty) return EntityStatus.None;
			bCShipment.Shipment = cbapi.GetByID<Shipment>(bCShipment.ShippingNoteID.Value);
			if (bCShipment.Shipment == null || bCShipment.Shipment?.Details?.Count == 0)
				return EntityStatus.None;

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
		protected virtual EntityStatus GetInvoice(SPShipmentEntityBucket bucket, BCShipments bCShipment)
		{
			if (bCShipment.ShipmentNumber == null) return EntityStatus.None;
			GetInvoiceByShipmentNbr(bCShipment);
			if (bCShipment.Shipment?.Details?.Count == 0) return EntityStatus.None;

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

		protected virtual void MapDropShipment(SPShipmentEntityBucket bucket, MappedShipment obj, PurchaseReceipt impl, List<BCLocations> locationMappings)
		{
			FulfillmentData shipmentData = obj.Extern = new FulfillmentData();
			shipmentData.LineItems = new List<OrderLineItem>();
			shipmentData.LocationId = defaultLocationId;
			var shipvia = impl.Details.FirstOrDefault(x => !string.IsNullOrEmpty(x.ShipVia?.Value))?.ShipVia?.Value ?? string.Empty;
			shipmentData.TrackingCompany = GetCarrierName(shipvia);
			shipmentData.TrackingNumbers = new List<string>() { impl.VendorRef?.Value };

			foreach (MappedOrder order in bucket.Orders)
			{
				foreach (PurchaseReceiptDetail line in impl.Details ?? new List<PurchaseReceiptDetail>())
				{
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.SOOrderType.Value && order.Local.OrderNbr.Value == line.SOOrderNbr.Value && d.LineNbr.Value == line.SOLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) throw new PXException(BCMessages.OrderShippingLineSyncronized, orderLine.InventoryID?.Value, order.Local.OrderNbr.Value, order?.ExternID);

					OrderLineItem shipItem = new OrderLineItem();
					shipItem.Id = lineInfo.ExternID.ToLong();
					shipItem.Quantity = (int)line.ReceiptQty.Value;
					shipItem.OrderId = order.ExternID.ToLong();

					shipmentData.LineItems.Add(shipItem);
				}
			}
		}

		protected virtual void MapInvoice(SPShipmentEntityBucket bucket, MappedShipment obj, Shipment impl, List<BCLocations> locationMappings)
		{
			FulfillmentData shipmentData = obj.Extern = new FulfillmentData();
			shipmentData.LineItems = new List<OrderLineItem>();
			shipmentData.LocationId = GetMappedExternalLocation(locationMappings, impl.WarehouseID.Value, impl.Details.FirstOrDefault()?.LocationID.Value);
			foreach (MappedOrder order in bucket.Orders)
			{
				foreach (ShipmentDetail line in impl.Details ?? new List<ShipmentDetail>())
				{

					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.OrderType.Value && order.Local.OrderNbr.Value == line.OrderNbr.Value && d.LineNbr.Value == line.OrderLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) throw new PXException(BCMessages.OrderShippingLineSyncronized, orderLine.InventoryID?.Value, order.Local.OrderNbr.Value, order?.ExternID);

					OrderLineItem shipItem = new OrderLineItem();
					shipItem.Id = lineInfo.ExternID.ToLong();
					shipItem.Quantity = (int)line.ShippedQty.Value;
					shipItem.OrderId = order.ExternID.ToLong();
					shipmentData.LineItems.Add(shipItem);
				}
			}
		}

		protected virtual void MapShipment(SPShipmentEntityBucket bucket, MappedShipment obj, Shipment impl, List<BCLocations> locationMappings)
		{
			FulfillmentData shipmentData = obj.Extern = new FulfillmentData();
			shipmentData.LineItems = new List<OrderLineItem>();
			shipmentData.LocationId = GetMappedExternalLocation(locationMappings, impl.WarehouseID.Value, impl.Details.FirstOrDefault()?.LocationID?.Value);
			shipmentData.TrackingCompany = GetCarrierName(impl.ShipVia?.Value ?? string.Empty);
			shipmentData.TrackingNumbers = new List<string>();
			bool ignorePackages = false;
			var PackageDetails = PXSelect<SOShipLineSplitPackage,
			Where<SOShipLineSplitPackage.shipmentNbr, Equal<Required<SOShipLineSplitPackage.shipmentNbr>>
			>>.Select(this, impl.ShipmentNbr?.Value).RowCast<SOShipLineSplitPackage>().ToList();
			var packages = impl.Packages ?? new List<ShipmentPackage>();
			impl.Details = impl.Details ?? new List<ShipmentDetail>();
			if (packages.Count == 1)
			{
				var trackingNumber = packages.FirstOrDefault()?.TrackingNbr?.Value;
				if (!string.IsNullOrEmpty(trackingNumber)) shipmentData.TrackingNumbers.Add(trackingNumber);
			}
			else
			{
				foreach (ShipmentPackage package in packages)
				{
					var detail = PackageDetails.Where(x => x.PackageLineNbr == package.LineNbr?.Value && x.PackedQty != 0)?.ToList() ?? new List<SOShipLineSplitPackage>();
					if (detail.Count == 0)
					{
						//if any box does not conatin item then ignore boxes and just create single shipment with all the trackingnumbers
						ignorePackages = true;
						break;
					}
					package.ShipmentLineNbr.AddRange(detail.Select(x => new Tuple<int?, decimal?>(x.ShipmentLineNbr, x.PackedQty)));
				}
				if (!ignorePackages)
				{
					// check if all item are place in boxes if not then ignore packages
					var result = packages.SelectMany(x => x.ShipmentLineNbr).GroupBy(g => g.Item1).Select(y => new { ShipmentLineNbr = y.Key, Qty = y.Sum(z => z.Item2 ?? 0) });
					if (impl.Details?.Count() != result?.Count())
						ignorePackages = true;
					if (impl.Details.Any(x => result?.FirstOrDefault(r => r.ShipmentLineNbr == x.LineNbr.Value)?.Qty != x.ShippedQty.Value))
						ignorePackages = true;
				}
				if (ignorePackages)
					shipmentData.TrackingNumbers.AddRange(packages.Where(x => x.TrackingNbr?.Value != null).Select(y => y.TrackingNbr?.Value));
			}

			foreach (MappedOrder order in bucket.Orders)
			{
				foreach (ShipmentDetail line in impl.Details)
				{
					List<ShipmentPackage> details = null;
					if (packages.Count > 1 && !ignorePackages)
					{
						details = packages.Where(x => (x.ShipmentLineNbr.Select(y => y.Item1)).Contains(line.LineNbr?.Value))?.ToList();
						if (details == null || (details != null && (details.SelectMany(x => x.ShipmentLineNbr)?.Where(x => x.Item1 == line.LineNbr.Value && x.Item2 != null)?.Sum(x => x.Item2) ?? 0) != line.ShippedQty?.Value))//  check if shipped item quatity matches the  quantity of item in package 
							throw new PXException(BCMessages.ItemsWithoutBoxes, impl.ShipmentNbr.Value, line.InventoryID?.Value);
					}
					SalesOrderDetail orderLine = order.Local.Details.FirstOrDefault(d =>
						order.Local.OrderType.Value == line.OrderType.Value && order.Local.OrderNbr.Value == line.OrderNbr.Value && d.LineNbr.Value == line.OrderLineNbr.Value);
					if (orderLine == null) continue; //skip shipment that is not from this order

					DetailInfo lineInfo = order.Details.FirstOrDefault(d => d.EntityType == BCEntitiesAttribute.OrderLine && d.LocalID == orderLine.NoteID.Value);
					if (lineInfo == null) lineInfo = MatchOrderLineFromExtern(order?.ExternID, orderLine.InventoryID.Value); //Try to fetch line data from external system in case item was extra added but not synced to ERP
					if (lineInfo == null) continue;// if dont find line item then ignore it
					if (details != null)
					{
						foreach (var detail in details)
						{
							OrderLineItem shipItem = new OrderLineItem();
							shipItem.Id = lineInfo.ExternID.ToLong();
							shipItem.OrderId = order.ExternID.ToLong();
							shipItem.Quantity = (int)(detail?.ShipmentLineNbr.Where(x => x.Item1 == line.LineNbr?.Value)?.Sum(x => x.Item2) ?? 0);
							shipItem.PackageId = detail?.NoteID?.Value;
							shipmentData.LineItems.Add(shipItem);
						}
					}
					else
					{
						OrderLineItem shipItem = new OrderLineItem();
						shipItem.Id = lineInfo.ExternID.ToLong();
						shipItem.Quantity = (int)line.ShippedQty.Value;
						shipItem.OrderId = order.ExternID.ToLong();
						shipmentData.LineItems.Add(shipItem);
					}
				}

			}
		}

		protected virtual string GetCarrierName(string shipVia)
		{
			string company = null;
			if (!string.IsNullOrEmpty(shipVia))
			{
				PX.Objects.CS.Carrier carrierData = SelectFrom<PX.Objects.CS.Carrier>.Where<PX.Objects.CS.Carrier.carrierID.IsEqual<@P.AsString>>.View.Select(this, shipVia);
				if (!string.IsNullOrEmpty(carrierData?.CarrierPluginID))
				{
					company = carrierData?.CarrierPluginID;
				}
				else
					company = shipVia;
				company = GetSubstituteExternByLocal(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.Carriers), company, company);
			}

			return company;
		}

		protected virtual DetailInfo MatchOrderLineFromExtern(string externalOrderId, string identifyKey)
		{
			DetailInfo lineInfo = null;
			if (string.IsNullOrEmpty(externalOrderId) || string.IsNullOrEmpty(identifyKey))
				return lineInfo;
			var orderLineDetails = orderDataProvider.GetByID(externalOrderId, includedMetafields: false, includedTransactions: false, includedCustomer: false, includedOrderRisk: false)?.LineItems;
			var matchedLine = orderLineDetails?.FirstOrDefault(x => string.Equals(x?.Sku, identifyKey, StringComparison.OrdinalIgnoreCase));
			if (matchedLine != null && matchedLine?.Id.HasValue == true)
			{
				lineInfo = new DetailInfo(BCEntitiesAttribute.OrderLine, null, matchedLine.Id.ToString());
			}
			return lineInfo;
		}

		protected virtual long? GetMappedExternalLocation(List<BCLocations> locationMappings, string siteCD, string locationCD)
		{
			if (locationMappings?.Count == 0 || string.IsNullOrEmpty(siteCD))
				return defaultLocationId;
			var matchedItem = locationMappings.FirstOrDefault(l => !string.IsNullOrEmpty(l.ExternalLocationID) && string.Equals(l.SiteCD, siteCD, StringComparison.OrdinalIgnoreCase) && (l.LocationID == null || (l.LocationID != null && string.Equals(l.LocationCD, locationCD, StringComparison.OrdinalIgnoreCase))));
			if (matchedItem != null)
				return inventoryLocations.Any(x => x.Id?.ToString() == matchedItem.ExternalLocationID) ? matchedItem.ExternalLocationID?.ToLong() : defaultLocationId;
			else
				return defaultLocationId;
		}
		#endregion
	}
}