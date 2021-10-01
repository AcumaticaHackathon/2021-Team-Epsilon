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
using PX.Data.BQL;
using PX.Objects.IN;
using PX.Objects.Common;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;

namespace PX.Commerce.Shopify
{
	public class SPAvailabilityEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Product;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };
		public MappedAvailability Product;
		public Dictionary<string, List<StorageDetailsResult>> LocationMappings = new Dictionary<string, List<StorageDetailsResult>>();
		public ProductVariantData ExternalVariant { get; set; }
	}

	[BCProcessor(typeof(SPConnector), BCEntitiesAttribute.ProductAvailability, BCCaptions.ProductAvailability,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		PrimaryGraph = typeof(PX.Objects.IN.InventorySummaryEnq),
		ExternTypes = new Type[] { },
		LocalTypes = new Type[] { },
		GIScreenID = BCConstants.GenericInquiryAvailability,
		GIResult = typeof(StorageDetails),
		AcumaticaPrimaryType = typeof(InventoryItem),
		RequiresOneOf = new string[] { BCEntitiesAttribute.StockItem + "." + BCEntitiesAttribute.ProductWithVariant },
		URL = "products/{0}"
		)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-AvailabilityStockItem", "BC-PUSH-AvailabilityTemplates" })]
	public class SPAvailabilityProcessor : BCProcessorBulkBase<SPAvailabilityProcessor, SPAvailabilityEntityBucket, MappedAvailability>, IProcessor
	{
		protected InventoryLevelRestDataProvider levelProvider;
		protected ProductVariantRestDataProvider productVariantDataProvider;
		protected IEnumerable<InventoryLocationData> inventoryLocations;
		protected BCBinding currentBinding;
		protected List<BCLocations> locationMappings;
		protected string defaultExtLocation = null;
		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			currentBinding = GetBinding();

			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());

			levelProvider = new InventoryLevelRestDataProvider(client);
			productVariantDataProvider = new ProductVariantRestDataProvider(client);
			inventoryLocations = ConnectorHelper.GetConnector(currentBinding.ConnectorType)?.GetExternalInfo<InventoryLocationData>(BCObjectsConstants.BCInventoryLocation, currentBinding.BindingID)?.Where(x => x.Active == true);
			if (inventoryLocations == null || inventoryLocations.Count() == 0)
			{
				throw new PXException(ShopifyMessages.InventoryLocationNotFound);
			}
		}
		#endregion

		#region Common
		public override void NavigateLocal(IConnector connector, ISyncStatus status)
		{
			PX.Objects.IN.InventorySummaryEnq extGraph = PXGraph.CreateInstance<PX.Objects.IN.InventorySummaryEnq>();
			InventorySummaryEnqFilter filter = extGraph.Filter.Current;
			InventoryItem item = PXSelect<InventoryItem, Where<InventoryItem.noteID, Equal<Required<InventoryItem.noteID>>>>.Select(this, status.LocalID);
			filter.InventoryID = item.InventoryID;

			if (filter.InventoryID != null)
				throw new PXRedirectRequiredException(extGraph, "Navigation") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
		}

		public override MappedAvailability PullEntity(Guid? localID, Dictionary<string, object> fields)
		{
			if (localID == null) return null;
			DateTime? timeStamp = fields.Where(f => f.Key.EndsWith(nameof(BCEntity.LastModifiedDateTime), StringComparison.InvariantCultureIgnoreCase)).Select(f => f.Value).LastOrDefault()?.ToDate();
			int? parentID = fields.Where(f => f.Key.EndsWith(nameof(BCSyncStatus.SyncID), StringComparison.InvariantCultureIgnoreCase)).Select(f => f.Value).LastOrDefault()?.ToInt();
			localID = fields.Where(f => f.Key.EndsWith("TemplateItem_noteID", StringComparison.InvariantCultureIgnoreCase)).Select(f => f.Value).LastOrDefault()?.ToGuid() ?? localID;
			return new MappedAvailability(new StorageDetailsResult(), localID, timeStamp, parentID);
		}
		#endregion

		#region Import
		public override List<SPAvailabilityEntityBucket> FetchBucketsImport(List<BCSyncStatus> ids)
		{
			return null;
		}
		public override void MapBucketImport(SPAvailabilityEntityBucket bucket, IMappedEntity existing)
		{
			throw new NotImplementedException();
		}
		public override void SaveBucketsImport(List<SPAvailabilityEntityBucket> buckets)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Export
		public override List<SPAvailabilityEntityBucket> FetchBucketsExport(List<BCSyncStatus> syncIDs)
		{
			List<SPAvailabilityEntityBucket> buckets = new List<SPAvailabilityEntityBucket>();
			BCEntityStats entityStats = GetEntityStats();
			BCBinding binding = GetBinding();
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			var invIDs = new List<string>();

			var warehouses = new Dictionary<string, INSite>();
			var locations = new Dictionary<string, string>();
			var locationMappings = new List<BCLocations>();
			string[] mappedLocations = null;
			var allVariantsData = productVariantDataProvider.GetAllWithoutParent(new FilterWithFields() { Fields = "id,product_id,sku,inventory_item_id,compare_at_price,price" }).ToList();
			if (allVariantsData == null && allVariantsData.Count() == 0) return buckets;

			if (bindingExt.WarehouseMode == BCWarehouseModeAttribute.SpecificWarehouse &&
				(PXAccess.FeatureInstalled<FeaturesSet.warehouse>() == true || PXAccess.FeatureInstalled<FeaturesSet.warehouseLocation>() == true))
			{
				foreach (PXResult<BCLocations, INSite, INLocation> result in PXSelectJoin<BCLocations,
					InnerJoin<INSite, On<INSite.siteID, Equal<BCLocations.siteID>>,
					InnerJoin<INLocation, On<INLocation.siteID, Equal<BCLocations.siteID>, And<BCLocations.locationID, IsNull, Or<BCLocations.locationID, Equal<INLocation.locationID>>>>>>,
					Where<BCLocations.bindingID, Equal<Required<BCLocations.bindingID>>, And<BCLocations.mappingDirection, Equal<BCMappingDirectionAttribute.export>>>>.Select(this, currentBinding.BindingID))
				{
					var bl = (BCLocations)result;
					var site = (INSite)result;
					var location = (INLocation)result;
					warehouses[site.SiteCD.Trim()] = site;
					bl.SiteCD = site.SiteCD.Trim();
					bl.LocationCD = location.LocationCD.Trim();
					if (location != null && bl.LocationID != null)
					{
						locations[site.SiteCD.Trim()] = locations.ContainsKey(site.SiteCD.Trim()) ? (locations[site.SiteCD.Trim()] + "," + location.LocationCD) : location.LocationCD;
					}
					//If customer specifies the warehouse but not specifis the location, should include all locations and all items that not assigned to any location.
					if (bl.LocationID == null)
					{
						locations[site.SiteCD.Trim()] = string.Empty;
						bl.LocationCD = null;
					}
					if (!string.IsNullOrEmpty(bl.ExternalLocationID) && inventoryLocations.Any(x => x.Id?.ToString() == bl.ExternalLocationID) == false)
					{
						throw new PXException(ShopifyMessages.ExternalLocationNotFound);
					}
					locationMappings.Add(bl);
				}
				if (locationMappings?.Count > 0)
				{
					mappedLocations = locationMappings.Select(x => x.ExternalLocationID).Distinct().ToArray();
					defaultExtLocation = mappedLocations.Length > 1 ? null : (mappedLocations.Length == 1 ? mappedLocations[0] : inventoryLocations.First().Id?.ToString());
				}
				else
					defaultExtLocation = inventoryLocations.First().Id?.ToString();
			}
			else
			{
				defaultExtLocation = inventoryLocations.First().Id?.ToString();
			}
			Boolean anyLocation = warehouses.Any() && locations.Any(x => x.Value != string.Empty);

			List<StorageDetailsResult> response = FetchStorageDetails(syncIDs, warehouses.Keys.ToArray(), anyLocation);

			//Remove all items that should not be updated, based on combination of Item + Store settings
			string storeAvailability = BCItemAvailabilities.Convert(bindingExt.Availability);
			response?.RemoveAll(c => c.Availability.Value == BCCaptions.StoreDefault && storeAvailability == BCCaptions.DoNotUpdate);
			if (syncIDs != null && syncIDs.Count > 0 && (Operation.PrepareMode == PrepareMode.None))
			{
				var localIds = syncIDs.Select(x => x.LocalID);
				response = response?.Where(s => localIds.Contains(s.InventoryNoteID.Value))?.ToList();
				if (response == null || response?.Count() == 0) return buckets;
			}

			List<dynamic> entitiesData = new List<dynamic>();
			foreach (PXResult<BCSyncStatus, BCSyncDetail> result in SelectFrom<BCSyncStatus>.
				LeftJoin<BCSyncDetail>.On<BCSyncStatus.syncID.IsEqual<BCSyncDetail.syncID>>.
				Where<BCSyncStatus.connectorType.IsEqual<@P.AsString>.
				And<BCSyncStatus.bindingID.IsEqual<@P.AsInt>.
				And<Brackets<BCSyncStatus.entityType.IsEqual<@P.AsString>.Or<BCSyncStatus.entityType.IsEqual<@P.AsString>.Or<BCSyncStatus.entityType.IsEqual<@P.AsString>>>>>>>.
				View.Select(this, currentBinding.ConnectorType, currentBinding.BindingID, BCEntitiesAttribute.StockItem, BCEntitiesAttribute.NonStockItem, BCEntitiesAttribute.ProductWithVariant))
			{
				var syncRecord = (BCSyncStatus)result;
				var recordDetail = (BCSyncDetail)result;

				if (syncRecord != null && syncRecord.PendingSync != true && syncRecord.Deleted != true)
				{
					var variantData = allVariantsData.FirstOrDefault(x => x.ProductId.ToString() == syncRecord.ExternID && !string.IsNullOrEmpty(recordDetail.ExternID) && x.Id.ToString() == recordDetail.ExternID) ??
						allVariantsData.FirstOrDefault(x => x.ProductId.ToString() == syncRecord.ExternID && (string.IsNullOrEmpty(recordDetail.ExternID) || x.Id.ToString() == recordDetail.ExternID));
					if (variantData == null) continue;
					entitiesData.Add(new
					{
						PSyncID = syncRecord.SyncID,
						PLocalID = syncRecord.LocalID,
						PExternID = variantData.ProductId,
						PEntityType = syncRecord.EntityType,
						CSyncID = recordDetail.SyncID,
						CLocalID = recordDetail.LocalID,
						CExternID = variantData.Id,
						InventoryItemID = variantData.InventoryItemId.ToString(),
						ProductVariant = variantData
					});
				}
			}
			List<StorageDetailsResult> results = new List<StorageDetailsResult>();
			if (response != null && response?.Count > 0)
			{
				foreach (var detailsGroup in response.GroupBy(r => new { InventoryID = r.InventoryCD?.Value?.Trim(), /*SiteID = r.SiteID?.Value*/ }))
				{
					StorageDetailsResult result = null;
					if (defaultExtLocation == null)
					{
						result = new StorageDetailsResult()
						{
							InventoryDescription = detailsGroup.First().InventoryDescription,
							InventoryCD = detailsGroup.First().InventoryCD,
							InventoryNoteID = detailsGroup.First().InventoryNoteID,
							Availability = detailsGroup.First().Availability,
							NotAvailMode = detailsGroup.First().NotAvailMode,
							ItemStatus = detailsGroup.First().ItemStatus,
							TemplateItemID = detailsGroup.First().TemplateItemID,
							IsTemplate = detailsGroup.First().IsTemplate,
							InventoryLastModifiedDate = detailsGroup.First().InventoryLastModifiedDate,
							ParentSyncId= detailsGroup.First().ParentSyncId
						};
						//If defaultExtLocation is null, that means there are multiple shopify locations in the mapping, we need to recalculate the Inventory by Location
						result.InventoryDetails = detailsGroup.ToList();
					}
					else
						result = detailsGroup.First();
					result.SiteLastModifiedDate = detailsGroup.Where(d => d.SiteLastModifiedDate != null).Select(d => d.SiteLastModifiedDate.Value).Max().ValueField();
					result.LocationLastModifiedDate = detailsGroup.Where(d => d.LocationLastModifiedDate != null).Select(d => d.LocationLastModifiedDate.Value).Max().ValueField();
					result.SiteOnHand = detailsGroup.Sum(k => k.SiteOnHand?.Value ?? 0m).ValueField();
					result.SiteAvailable = detailsGroup.Sum(k => k.SiteAvailable?.Value ?? 0m).ValueField();
					result.SiteAvailableforIssue = detailsGroup.Sum(k => k.SiteAvailableforIssue?.Value ?? 0m).ValueField();
					result.SiteAvailableforShipping = detailsGroup.Sum(k => k.SiteAvailableforShipping?.Value ?? 0m).ValueField();
					if (bindingExt.WarehouseMode == BCWarehouseModeAttribute.SpecificWarehouse && !warehouses.Any())//if warehouse is specific but nothing is configured in table
					{
						result.LocationOnHand = result.LocationAvailable = result.LocationAvailableforIssue = result.LocationAvailableforShipping = 0m.ValueField();
					}
					else
					{
						if (detailsGroup.Any(i => i.SiteID?.Value != null))
						{
							result.LocationOnHand = anyLocation ? detailsGroup.Where(k => warehouses.Count <= 0 || (locations.ContainsKey(k.SiteID?.Value?.Trim()) && (locations[k.SiteID?.Value?.Trim()] == string.Empty || (k.LocationID?.Value != null && locations[k.SiteID?.Value?.Trim()].Contains(k.LocationID?.Value))))).Sum(k => k.LocationOnHand?.Value ?? 0m).ValueField() : null;
							result.LocationAvailable = anyLocation ? detailsGroup.Where(k => warehouses.Count <= 0 || (locations.ContainsKey(k.SiteID?.Value?.Trim()) && (locations[k.SiteID?.Value?.Trim()] == string.Empty || (k.LocationID?.Value != null && locations[k.SiteID?.Value?.Trim()].Contains(k.LocationID?.Value))))).Sum(k => k.LocationAvailable?.Value ?? 0m).ValueField() : null;
							result.LocationAvailableforIssue = anyLocation ? detailsGroup.Where(k => warehouses.Count <= 0 || (locations.ContainsKey(k.SiteID?.Value?.Trim()) && (locations[k.SiteID?.Value?.Trim()] == string.Empty || (k.LocationID?.Value != null && locations[k.SiteID?.Value?.Trim()].Contains(k.LocationID?.Value))))).Sum(k => k.LocationAvailableforIssue?.Value ?? 0m).ValueField() : null;
							result.LocationAvailableforShipping = anyLocation ? detailsGroup.Where(k => warehouses.Count <= 0 || (locations.ContainsKey(k.SiteID?.Value?.Trim()) && (locations[k.SiteID?.Value?.Trim()] == string.Empty || (k.LocationID?.Value != null && locations[k.SiteID?.Value?.Trim()].Contains(k.LocationID?.Value))))).Sum(k => k.LocationAvailableforShipping?.Value ?? 0m).ValueField() : null;
						}
						else
							result.LocationOnHand = result.LocationAvailable = result.LocationAvailableforIssue = result.LocationAvailableforShipping = null;
					}
					results.Add(result);
				}
			}
			if (results == null || results.Count() == 0) return buckets;

			foreach (StorageDetailsResult line in results)
			{
				Guid? noteID = line.InventoryNoteID?.Value;
				var productSyncRecord = entitiesData.FirstOrDefault(p => (p.PEntityType == BCEntitiesAttribute.ProductWithVariant && ((Guid?)p.CLocalID) == noteID) || (p.PEntityType != BCEntitiesAttribute.ProductWithVariant && ((Guid?)p.PLocalID) == noteID));
				if (productSyncRecord == null) continue;
				DateTime? lastModified;
				lastModified = new DateTime?[] { line.LocationLastModifiedDate?.Value, line.SiteLastModifiedDate?.Value, line.InventoryLastModifiedDate.Value }.Where(d => d != null).Select(d => d.Value).Max();

				if (Operation.PrepareMode == PrepareMode.Incremental && entityStats?.LastIncrementalExportDateTime != null && lastModified < entityStats.LastIncrementalExportDateTime)
					continue;

				SPAvailabilityEntityBucket bucket = new SPAvailabilityEntityBucket();
				MappedAvailability obj = bucket.Product = new MappedAvailability(line, noteID, lastModified, ((int?)line.ParentSyncId.Value));
				bucket.ExternalVariant = productSyncRecord.ProductVariant;
				EntityStatus status = EnsureStatus(obj, SyncDirection.Export);
				if (status == EntityStatus.Deleted) status = EnsureStatus(obj, SyncDirection.Export, resync: true);

				var externId = new object[] { productSyncRecord.PExternID, productSyncRecord.InventoryItemID }.KeyCombine();
				if (obj.ExternID == null || obj.ExternID != externId)
					obj.ExternID = externId;

				if (defaultExtLocation != null && !bucket.LocationMappings.ContainsKey(defaultExtLocation))
				{
					bucket.LocationMappings.Add(defaultExtLocation, new List<StorageDetailsResult>() { line });
				}
				else if (defaultExtLocation == null && mappedLocations?.Length > 1)
				{
					//Handle multiple locations mapping
					foreach (var locationId in mappedLocations)
					{
						var mappingsWithLocation = locationMappings.Where(x => x.ExternalLocationID == locationId && x.MappingDirection == BCMappingDirectionAttribute.Export).ToList();
						StorageDetailsResult result = new StorageDetailsResult()
						{
							InventoryDescription = line.InventoryDescription,
							InventoryCD = line.InventoryCD,
							InventoryNoteID = line.InventoryNoteID,
							Availability = line.Availability,
							NotAvailMode = line.NotAvailMode,
							ItemStatus = line.ItemStatus
						};
						var matchedSiteDetails = line.InventoryDetails.Where(x => mappingsWithLocation.Any(l => string.Equals(l.SiteCD, x.SiteID?.Value, StringComparison.OrdinalIgnoreCase)));
						result.SiteOnHand = matchedSiteDetails.Sum(k => k.SiteOnHand?.Value ?? 0m).ValueField();
						result.SiteAvailable = matchedSiteDetails.Sum(k => k.SiteAvailable?.Value ?? 0m).ValueField();
						result.SiteAvailableforIssue = matchedSiteDetails.Sum(k => k.SiteAvailableforIssue?.Value ?? 0m).ValueField();
						result.SiteAvailableforShipping = matchedSiteDetails.Sum(k => k.SiteAvailableforShipping?.Value ?? 0m).ValueField();
						if (matchedSiteDetails.Any(i => i.LocationID?.Value != null))
						{
							var matchedLocationDetails = matchedSiteDetails.Where(x => mappingsWithLocation.Any(l => string.Equals(l.SiteCD, x.SiteID?.Value, StringComparison.OrdinalIgnoreCase) && (l.LocationID == null || (l.LocationID != null && string.Equals(l.LocationCD, x.LocationID?.Value, StringComparison.OrdinalIgnoreCase)))));
							result.LocationOnHand = matchedLocationDetails.Sum(k => k.LocationOnHand?.Value ?? 0m).ValueField();
							result.LocationAvailable = matchedLocationDetails.Sum(k => k.LocationAvailable?.Value ?? 0m).ValueField();
							result.LocationAvailableforIssue = matchedLocationDetails.Sum(k => k.LocationAvailableforIssue?.Value ?? 0m).ValueField();
							result.LocationAvailableforShipping = matchedLocationDetails.Sum(k => k.LocationAvailableforShipping?.Value ?? 0m).ValueField();
						}
						else
							result.LocationOnHand = result.LocationAvailable = result.LocationAvailableforIssue = result.LocationAvailableforShipping = null;
						bucket.LocationMappings.Add(locationId, new List<StorageDetailsResult>() { result });
					}
				}

				if (Operation.PrepareMode != PrepareMode.Reconciliation && status != EntityStatus.Pending && Operation.SyncMethod != SyncMode.Force) continue;

				buckets.Add(bucket);
			}

			return buckets;
		}

		public virtual List<StorageDetailsResult> FetchStorageDetails(List<BCSyncStatus> statuses, String[] warehouses, bool anyLocation)
		{
			StorageDetails request = new StorageDetails();
			request.Warehouse = string.Join(",", warehouses).ValueField();
			request.SplitByLocation = anyLocation.ValueField();
			request.BindingID = GetBinding().BindingID.ValueField();
			List<StorageDetailsResult> results = new List<StorageDetailsResult>();
			if (Operation.PrepareMode == PrepareMode.None && statuses?.Count < (BCConstants.GenericInquiryPageSize / 2))
			{
				List<PXFilterRow> filters = new List<PXFilterRow>();
				foreach (BCSyncStatus status in statuses)
				{
					if (status?.LocalID == null) continue;

					filters.Add(new PXFilterRow() { DataField = nameof(StorageDetailsResult.IdentifyNoteID), Condition = PXCondition.LIKE, Value = status.LocalID.Value.ToString(), OrOperator = true });
					if (filters.Count % BCConstants.GerericInquiryFetchByBatch == 0)
					{
						filters.Last().OrOperator = false;
						var response = cbapi.GetGIResult<StorageDetailsResult>(request, BCConstants.GenericInquiryAvailability, filters.ToArray());
						if (response != null)
							results.AddRange(response);

						filters.Clear();
					}
				}
				if (filters.Count != 0)
				{
					filters.Last().OrOperator = false;
					var response = cbapi.GetGIResult<StorageDetailsResult>(request, BCConstants.GenericInquiryAvailability, filters.ToArray());
					if (response != null)
						results.AddRange(response);
					filters.Clear();
				}
			}
			if (Operation.PrepareMode != PrepareMode.None)
			{
				var response = cbapi.GetGIResult<StorageDetailsResult>(request, BCConstants.GenericInquiryAvailability);
				if (response != null)
					results.AddRange(response);
			}

			return results;
		}

		public int GetInventoryLevel(BCBindingExt bindingExt, StorageDetailsResult detailsResult)
		{
			switch (bindingExt.AvailabilityCalcRule)
			{
				case BCAvailabilityLevelsAttribute.Available:
					return (int)(detailsResult.LocationAvailable?.Value ?? detailsResult.SiteAvailable.Value);
				case BCAvailabilityLevelsAttribute.AvailableForShipping:
					return (int)(detailsResult.LocationAvailableforShipping?.Value ?? detailsResult.SiteAvailableforShipping.Value);
				case BCAvailabilityLevelsAttribute.OnHand:
					return (int)(detailsResult.LocationOnHand?.Value ?? detailsResult.SiteOnHand.Value);
				default:
					return 0;
			}
		}

		public override void SaveBucketsExport(List<SPAvailabilityEntityBucket> buckets)
		{
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			foreach (var bucket in buckets)
			{
				MappedAvailability obj = bucket.Product;
				StorageDetailsResult impl = obj.Local;
				obj.Extern = new InventoryLevelData();
				InventoryLevelData data = null;

				var errorMsg = string.Empty;
				Boolean isItemActive = !(impl.ItemStatus?.Value == InventoryItemStatus.Inactive || impl.ItemStatus?.Value == InventoryItemStatus.MarkedForDeletion || impl.ItemStatus?.Value == InventoryItemStatus.NoSales);
				string availability = impl.Availability?.Value;
				if (availability == null || availability == BCItemAvailabilities.StoreDefault)
				{
					availability = bindingExt.Availability;
				}
				string notAvailMode = impl.NotAvailMode?.Value;
				if (notAvailMode == null || notAvailMode == BCItemNotAvailModes.StoreDefault)
				{
					notAvailMode = bindingExt.NotAvailMode;
				}

				errorMsg += UpdateVariantInfo(bucket.ExternalVariant, availability, notAvailMode, isItemActive);
				//Update invenotry only if availability is set to "track qty".
				if (availability == BCItemAvailabilities.AvailableTrack)
				{
					foreach (var locationItem in bucket.LocationMappings)
					{
						data = new InventoryLevelData();
						var externId = obj.ExternID.KeySplit(1, obj.ExternID.KeySplit(0));
						data.InventoryItemId = externId.ToLong();
						data.LocationId = locationItem.Key.ToLong();
						data.Available = isItemActive ? locationItem.Value.Sum(x => GetInventoryLevel(bindingExt, x)) : 0;
						data.DisconnectIfNecessary = true;
						try
						{
							data = levelProvider.SetInventory(data);
						}
						catch (Exception ex)
						{
							Log(bucket?.Primary?.SyncID, SyncDirection.Export, ex);
							errorMsg += ex.InnerException?.Message ?? ex.Message + "\n";
						}
					}
				}

				if (!string.IsNullOrEmpty(errorMsg))
				{
					UpdateStatus(bucket.Product, BCSyncOperationAttribute.ExternFailed, errorMsg);
					Operation.Callback?.Invoke(new SyncInfo(bucket?.Primary?.SyncID ?? 0, SyncDirection.Export, SyncResult.Error, new Exception(errorMsg)));
				}
				else
				{
					bucket.Product.ExternID = new object[] { obj.ExternID }.KeyCombine();
					bucket.Product.AddExtern(data, obj.ExternID, data?.DateModifiedAt.ToDate(false));
					UpdateStatus(bucket.Product, BCSyncOperationAttribute.ExternUpdate);
					Operation.Callback?.Invoke(new SyncInfo(bucket?.Primary?.SyncID ?? 0, SyncDirection.Export, SyncResult.Processed));
				}
			}

		}

		public string UpdateVariantInfo(ProductVariantData variant, string availability, string notAvailMode, bool isItemActive)

		{
			string errorMsg = string.Empty;
			var variantData = new ProductVariantData();
			variantData.Id = variant.Id;
			variantData.ProductId = variant.ProductId;
			variantData.Price = variant.Price;
			variantData.OriginalPrice = variant.OriginalPrice;

			if (availability == BCItemAvailabilities.AvailableTrack)
			{
				variantData.InventoryManagement = ShopifyConstants.InventoryManagement_Shopify;
			}
			else
			{
				variantData.InventoryManagement = null;
			}

			switch (notAvailMode)
			{
				case BCItemNotAvailModes.DisableItem:
					{
						variantData.InventoryPolicy = InventoryPolicy.Deny;
						break;
					}
				case BCItemNotAvailModes.DoNothing:
				case BCItemNotAvailModes.PreOrderItem:
					{
						variantData.InventoryPolicy = isItemActive ? InventoryPolicy.Continue : InventoryPolicy.Deny;
						break;
					}
			}
			try
			{
				productVariantDataProvider.Update(variantData, variantData.ProductId.ToString(), variantData.Id.ToString());
			}
			catch (Exception ex)
			{
				errorMsg = ex.InnerException?.Message ?? ex.Message;
			}
			return errorMsg;
		}
		#endregion
	}
}
