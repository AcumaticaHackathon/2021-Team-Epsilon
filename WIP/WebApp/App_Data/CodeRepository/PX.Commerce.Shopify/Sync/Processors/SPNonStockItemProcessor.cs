﻿using Newtonsoft.Json;
using PX.Commerce.Shopify.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using PX.Api.ContractBased.Models;
using Serilog.Context;
using PX.Common;

namespace PX.Commerce.Shopify
{
	public class SPNonStockItemEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Product;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };
		public MappedNonStockItem Product;
		public Dictionary<string, long?> VariantMappings = new Dictionary<string, long?>();
	}

	public class SPNonStockItemRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			#region StockItems
			return base.Restrict<MappedNonStockItem>(mapped, delegate (MappedNonStockItem obj)
			{
				if (obj.Local != null && obj.Local.TemplateItemID?.Value != null)
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogNonStockSkippedVariant, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
				}

				if (obj.Local != null && obj.Local.ExportToExternal?.Value == false)
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogItemNoExport, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
				}

				BCBindingExt bindingExt = processor.GetBindingExt<BCBindingExt>();
				if (bindingExt.GiftCertificateItemID != null && obj.Local?.InventoryID?.Value != null)
				{
					PX.Objects.IN.InventoryItem giftCertificate = bindingExt.GiftCertificateItemID != null ? PX.Objects.IN.InventoryItem.PK.Find((PXGraph)processor, bindingExt.GiftCertificateItemID) : null;
					if (giftCertificate != null && obj.Local?.InventoryID?.Value.Trim() == giftCertificate?.InventoryCD?.Trim())
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogItemSkippedNoCategories, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
					}
				}

				if (bindingExt.RefundAmountItemID != null && obj.Local?.InventoryID?.Value != null)
				{
					PX.Objects.IN.InventoryItem refundItem = bindingExt.RefundAmountItemID != null ? PX.Objects.IN.InventoryItem.PK.Find((PXGraph)processor, bindingExt.RefundAmountItemID) : null;
					if (refundItem != null && obj.Local?.InventoryID?.Value.Trim() == refundItem?.InventoryCD?.Trim())
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogNonStockSkippedRefund, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
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

	[BCProcessor(typeof(SPConnector), BCEntitiesAttribute.NonStockItem, BCCaptions.NonStockItem,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		PrimaryGraph = typeof(PX.Objects.IN.NonStockItemMaint),
		ExternTypes = new Type[] { typeof(ProductData) },
		LocalTypes = new Type[] { typeof(NonStockItem) },
		AcumaticaPrimaryType = typeof(PX.Objects.IN.InventoryItem),
		AcumaticaPrimarySelect = typeof(Search<PX.Objects.IN.InventoryItem.inventoryCD, Where<PX.Objects.IN.InventoryItem.stkItem, Equal<False>>>),
		URL = "products/{0}"
	)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-NonStocks" })]
	public class SPNonStockItemProcessor : SPProductProcessor<SPNonStockItemProcessor, SPNonStockItemEntityBucket, MappedNonStockItem>, IProcessor
	{
		private Dictionary<int, string> SalesCategories;
		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
		}
		#endregion

		#region Common
		public override MappedNonStockItem PullEntity(Guid? localID, Dictionary<string, object> externalInfo)
		{
			NonStockItem impl = cbapi.GetByID<NonStockItem>(localID);
			if (impl == null) return null;

			MappedNonStockItem obj = new MappedNonStockItem(impl, impl.SyncID, impl.SyncTime);

			return obj;
		}
		public override MappedNonStockItem PullEntity(String externID, String externalInfo)
		{
			ProductData data = productDataProvider.GetByID(externID);
			if (data == null) return null;

			MappedNonStockItem obj = new MappedNonStockItem(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));

			return obj;
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			//No DateTime filtering for Category
			FilterProducts filter = new FilterProducts { UpdatedAtMin = minDateTime == null ? (DateTime?)null : minDateTime.Value.ToLocalTime().AddSeconds(-GetBindingExt<BCBindingShopify>().ApiDelaySeconds ?? 0),
				UpdatedAtMax = maxDateTime == null ? (DateTime?)null : maxDateTime.Value.ToLocalTime()
			};

			IEnumerable<ProductData> datas = productDataProvider.GetAll(filter);

			if (datas?.Count() > 0)
			{
				foreach (ProductData data in datas)
				{
					SPNonStockItemEntityBucket bucket = CreateBucket();

					MappedNonStockItem obj = bucket.Product = bucket.Product.Set(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));
					EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
					if (data.Variants?.Count > 0)
					{
						data.Variants.ForEach(x => { bucket.VariantMappings[x.Sku] = x.Id; });
					}
				}
			}
		}
		public override EntityStatus GetBucketForImport(SPNonStockItemEntityBucket bucket, BCSyncStatus syncstatus)
		{
			ProductData data = productDataProvider.GetByID(syncstatus.ExternID);
			if (data == null) return EntityStatus.None;

			if (data.Variants?.Count > 0)
			{
				data.Variants.ForEach(x =>
				{
					if (bucket.VariantMappings.ContainsKey(x.Sku))
						bucket.VariantMappings[x.Sku] = x.Id;
					else
						bucket.VariantMappings.Add(x.Sku, x.Id);
				});
			}
			MappedNonStockItem obj = bucket.Product = bucket.Product.Set(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));
			EntityStatus status = EnsureStatus(obj, SyncDirection.Import);

			return status;
		}

		public override void MapBucketImport(SPNonStockItemEntityBucket bucket, IMappedEntity existing)
		{
			
		}
		public override void SaveBucketImport(SPNonStockItemEntityBucket bucket, IMappedEntity existing, String operation)
		{
			
		}
		#endregion

		#region Export
		public override IEnumerable<MappedNonStockItem> PullSimilar(ILocalEntity entity, out string uniqueField)
		{
			var uniqueStr = uniqueField = ((NonStockItem)entity)?.InventoryID?.Value;

            IEnumerable<ProductData> datas = null;
			if (!string.IsNullOrEmpty(uniqueStr))
			{
				var ExternProductVariantData = PXContext.GetSlot<List<ProductVariantData>>(nameof(ProductVariantData));
				if (ExternProductVariantData == null || ExternProductVariantData?.Count == 0 || !ExternProductVariantData.Any(x => string.Equals(x.Sku, uniqueStr, StringComparison.OrdinalIgnoreCase)))
				{
					ExternProductVariantData = productVariantDataProvider.GetAllWithoutParent(new FilterWithFields() { Fields = "id,product_id,sku,title" })?.ToList();
					PXContext.SetSlot<List<ProductVariantData>>(nameof(ProductVariantData), ExternProductVariantData);
				}
				var existedItems = ExternProductVariantData?.Where(x => string.Equals(x.Sku, uniqueStr, StringComparison.OrdinalIgnoreCase));
				if (existedItems != null && existedItems?.Count() > 0)
				{
					var matchedVariants = existedItems.Select(x => x.ProductId).Distinct().ToList();
					if (matchedVariants != null && matchedVariants.Count > 0)
					{
						datas = productDataProvider.GetAll(new FilterProducts() { IDs = string.Join(",", matchedVariants) });
					}
				}
			}

			return datas == null ? null : datas.Select(data => new MappedNonStockItem(data, data.Id.ToString(), data.DateModifiedAt.ToDate(false)));
		}

		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			NonStockItem item = new NonStockItem()
			{
				InventoryID = new StringReturn(),
				TemplateItemID = new StringReturn(),
				Categories = new List<CategoryStockItem>() { new CategoryStockItem() { CategoryID = new IntReturn() } },
				ExportToExternal = new BooleanReturn()
			};
			IEnumerable<NonStockItem> impls = cbapi.GetAll<NonStockItem>(item, minDateTime, maxDateTime, filters);

			if (impls != null)
			{
				int countNum = 0;
				List<IMappedEntity> mappedList = new List<IMappedEntity>();
				foreach (NonStockItem impl in impls)
				{
                    IMappedEntity obj = new MappedNonStockItem(impl, impl.SyncID, impl.SyncTime);

					mappedList.Add(obj);
					countNum++;
					if (countNum % BatchFetchCount == 0)
					{
                        ProcessMappedListForExport(ref mappedList);
                    }
				}
				if (mappedList.Any())
				{
					ProcessMappedListForExport(ref mappedList);
				}
			}
		}
		public override EntityStatus GetBucketForExport(SPNonStockItemEntityBucket bucket, BCSyncStatus syncstatus)
		{
			NonStockItem impl = cbapi.GetByID<NonStockItem>(syncstatus.LocalID, GetCustomFieldsForExport());
			if (impl == null) return EntityStatus.None;

			MappedNonStockItem obj = bucket.Product = bucket.Product.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);
			if (!bucket.VariantMappings.ContainsKey(impl.InventoryID?.Value))
				bucket.VariantMappings.Add(impl.InventoryID?.Value, null);
			if (obj.Local.Categories != null)
			{
				foreach (CategoryStockItem item in obj.Local.Categories)
				{
					if (!SalesCategories.ContainsKey(item.CategoryID.Value.Value))
					{
						BCItemSalesCategory implCat = cbapi.Get<BCItemSalesCategory>(new BCItemSalesCategory() { CategoryID = new IntSearch() { Value = item.CategoryID.Value } });
						if (implCat == null) continue;
						if (item.CategoryID.Value != null)
						{
							SalesCategories[item.CategoryID.Value.Value] = implCat.Description.Value;
						}
					}
				}
			}
			return status;
		}

		public override void MapBucketExport(SPNonStockItemEntityBucket bucket, IMappedEntity existing)
		{
			MappedNonStockItem obj = bucket.Product;
			ProductData presented = existing?.Extern as ProductData;
			//Existing item and store Availability Policies
			string storeAvailability = BCItemAvailabilities.Convert(GetBindingExt<BCBindingExt>().Availability);

			// Value can be null or 'shopify' but if the external item does not exist, we need to go along with the SP standard behaviour - set 'shopify'
			string currentAvail = presented != null ? presented?.Variants[0]?.InventoryManagement : ShopifyConstants.InventoryManagement_Shopify;
			bool? currentlyPublished = presented?.Published;

			NonStockItem impl = obj.Local;



			ProductData data = obj.Extern = new ProductData();

			data.Title = impl.Description?.Value;
			data.BodyHTML = ClearHTMLContent(impl.Content?.Value);
			data.ProductType = impl.ItemClassDescription?.Value;
			data.Vendor = (impl.VendorDetails?.FirstOrDefault(v => v.Default?.Value == true)?? impl.VendorDetails?.FirstOrDefault())?.VendorName?.Value;
			
			//Put all categories to the Tags
			var categories = impl.Categories?.Select(x => { if (SalesCategories.TryGetValue(x.CategoryID.Value.Value, out var desc)) return desc; else return string.Empty; }).Where(x => !string.IsNullOrEmpty(x)).ToList();
			if (categories != null && categories.Count > 0)
				data.Categories = categories;
			if (!string.IsNullOrEmpty(impl.SearchKeywords?.Value))
				data.Tags = impl.SearchKeywords?.Value;
			if (!string.IsNullOrEmpty(impl.PageTitle?.Value))
				data.GlobalTitleTag = impl.PageTitle?.Value;
			if (!string.IsNullOrEmpty(impl.MetaDescription?.Value))
				data.GlobalDescriptionTag = impl.MetaDescription?.Value;

            string availability = impl.Availability?.Value;
            if (availability == null || availability == BCCaptions.StoreDefault) availability = BCItemAvailabilities.Convert(GetBindingExt<BCBindingExt>().Availability);
			string visibility = impl?.Visibility?.Value;
			if (visibility == null || visibility == BCCaptions.StoreDefault) visibility = BCItemVisibility.Convert(GetBindingExt<BCBindingExt>().Visibility);
			if (availability != BCCaptions.DoNotUpdate)
				data.Published = visibility == BCCaptions.Invisible 
				|| impl.ItemStatus?.Value != PX.Objects.IN.Messages.Active
                || availability == BCCaptions.Disabled ? false : true;
			else
				data.Published = currentlyPublished;
			if (!string.IsNullOrEmpty(obj.ExternID))
			{
				data.Id = obj.ExternID.ToLong();
			}
			else
			{
				data.Metafields = new List<MetafieldData>() { new MetafieldData() { Key = ShopifyCaptions.Product, Value = impl.Id.Value.ToString(), ValueType = ShopifyConstants.ValueType_String, Namespace = ShopifyConstants.Namespace_Global } };
				data.Metafields.Add(new MetafieldData() { Key = nameof(ProductTypes), Value = BCCaptions.NonStockItem, ValueType = ShopifyConstants.ValueType_String, Namespace = ShopifyConstants.Namespace_Global });
			}

			data.Variants = data.Variants ?? new List<ProductVariantData>();
			bool isTaxable;bool.TryParse(GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxCategorySubstitutionListID, impl.TaxCategory?.Value, String.Empty), out isTaxable);
			data.Variants.Add(new ProductVariantData()
			{
				Id = bucket.VariantMappings.ContainsKey(impl.InventoryID?.Value) ? bucket.VariantMappings[impl.InventoryID?.Value] : null,
				Title = impl.Description?.Value,
				Price = impl.DefaultPrice.Value,
				Sku = impl.InventoryID?.Value,
				OriginalPrice = impl.MSRP?.Value != null && impl.MSRP?.Value > impl.DefaultPrice.Value ? impl.MSRP?.Value : null,
				Barcode = !string.IsNullOrWhiteSpace(impl.BaseUnit?.Value) ? ((impl.CrossReferences?.FirstOrDefault(x => x.AlternateType?.Value == "Barcode" && x.UOM?.Value == impl.BaseUnit?.Value) ??
					impl.CrossReferences?.FirstOrDefault(x => x.AlternateType?.Value == "Barcode" && string.IsNullOrWhiteSpace(x.UOM?.Value)))?.AlternateID?.Value) : null,
				Weight = impl.DimensionWeight?.Value,
				WeightUnit = impl.WeightUOM?.Value?.ToLower(),
				Taxable = isTaxable,
				InventoryManagement = availability != BCCaptions.DoNotUpdate ? null : currentAvail,
				InventoryPolicy = bucket.Product.IsNew ? InventoryPolicy.Continue : presented?.Variants.FirstOrDefault()?.InventoryPolicy,
				
				RequiresShipping = presented?.Variants?.FirstOrDefault(x => x != null && string.Equals(impl.InventoryID?.Value, x?.Sku, StringComparison.OrdinalIgnoreCase)) == null ? impl.RequireShipment.Value ?? false : (bool?)null, 
				Metafields = bucket.VariantMappings.ContainsKey(impl.InventoryID?.Value) ? null : 
					new List<MetafieldData>() { new MetafieldData() { Key = ShopifyConstants.Variant, Value = impl.Id.Value.ToString(), ValueType = ShopifyConstants.ValueType_String, Namespace = ShopifyConstants.Namespace_Global } }
			});
		}

		public override object GetAttribute(SPNonStockItemEntityBucket bucket, string attributeID)
		{
			MappedNonStockItem obj = bucket.Product;
			NonStockItem impl = obj.Local;
			return impl.Attributes?.Where(x => string.Equals(x?.AttributeDescription?.Value, attributeID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

		}

		public override void AddAttributeValue(SPNonStockItemEntityBucket bucket, string attributeID, object attributeValue)
		{
			MappedNonStockItem obj = bucket.Product;
			NonStockItem impl = obj.Local;
			impl.Attributes = impl.Attributes ?? new List<AttributeValue>();
			AttributeValue attribute = new AttributeValue();
			attribute.AttributeID = new StringValue() { Value = attributeID };
			attribute.ValueDescription = new StringValue() { Value = attributeValue?.ToString() };
			impl.Attributes.Add(attribute);
		}

		public override void SaveBucketExport(SPNonStockItemEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedNonStockItem obj = bucket.Product;
			ProductData data = null;
			if (obj.Extern.Categories?.Count > 0 && GetBindingExt<BCBindingShopify>()?.CombineCategoriesToTags == BCSalesCategoriesExportAttribute.SyncToProductTags)
			{
				obj.Extern.Tags = string.Join(",", obj.Extern.Categories) + (string.IsNullOrEmpty(obj.Extern.Tags) ? "" : $",{obj.Extern.Tags}");
			}
			try
			{
				if (obj.ExternID == null)
					data = productDataProvider.Create(obj.Extern);
				else
				{
					var skus = obj.Extern.Variants.Select(x => x.Sku).ToList();
					var notExistedVariantIds = bucket.VariantMappings.Where(x => !skus.Contains(x.Key)).Select(x => x.Value).ToList();
					data = productDataProvider.Update(obj.Extern, obj.ExternID);
					if (notExistedVariantIds?.Count > 0)
					{
						notExistedVariantIds.ForEach(x =>
						{
							if (x != null) productVariantDataProvider.Delete(obj.ExternID, x.ToString());
						});
					}
				}
			}
			catch (Exception ex)
			{
				throw new PXException(ex.Message);
			}

			obj.AddExtern(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));
			if (data.Variants?.Count > 0)
			{
				data.Variants.ForEach(x => {
					obj.AddDetail(BCEntitiesAttribute.Variant, obj.LocalID.Value, x.Id.ToString());
				});
			}

			SaveImages(obj, obj.Local?.FileUrls);

			UpdateStatus(obj, operation);
		}
		#endregion
	}
}