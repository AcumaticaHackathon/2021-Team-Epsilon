using Newtonsoft.Json;
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
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Api.ContractBased.Models;
using Serilog.Context;
using PX.Common;

namespace PX.Commerce.Shopify
{
	public class SPTemplateItemEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Product;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };
		public MappedTemplateItem Product;
		public Dictionary<string, Tuple<long?, string, InventoryPolicy?>> VariantMappings = new Dictionary<string, Tuple<long?, string, InventoryPolicy?>>();
	}

	public class SPTemplateItemRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			#region TemplateItemss
			return base.Restrict<MappedTemplateItem>(mapped, delegate (MappedTemplateItem obj)
			{
				if (obj.Local != null && (obj.Local.Matrix == null || obj.Local.Matrix?.Count == 0))
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogTemplateSkippedNoMatrix, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
				}

				if (obj.Local != null && obj.Local.ExportToExternal?.Value == false)
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogItemNoExport, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
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

	[BCProcessor(typeof(SPConnector), BCEntitiesAttribute.ProductWithVariant, BCCaptions.TemplateItem,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		PrimaryGraph = typeof(PX.Objects.IN.InventoryItemMaint),
		ExternTypes = new Type[] { typeof(ProductData) },
		LocalTypes = new Type[] { typeof(TemplateItems) },
		DetailTypes = new String[] { BCEntitiesAttribute.Variant, BCCaptions.Variant },
		AcumaticaPrimaryType = typeof(PX.Objects.IN.InventoryItem),
		AcumaticaPrimarySelect = typeof(Search<PX.Objects.IN.InventoryItem.inventoryCD, Where<PX.Objects.IN.InventoryItem.isTemplate, Equal<True>>>),
		AcumaticaFeaturesSet = typeof(FeaturesSet.matrixItem),
		URL = "products/{0}"
	)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-Variants" })]
	public class SPTemplateItemProcessor : SPProductProcessor<SPTemplateItemProcessor, SPTemplateItemEntityBucket, MappedTemplateItem>, IProcessor
	{
		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());
		}
		#endregion

		#region Common
		public override MappedTemplateItem PullEntity(Guid? localID, Dictionary<string, object> externalInfo)
		{
			TemplateItems impl = cbapi.GetByID<TemplateItems>(localID);
			if (impl == null) return null;

			MappedTemplateItem obj = new MappedTemplateItem(impl, impl.SyncID, impl.SyncTime);

			return obj;
		}
		public override MappedTemplateItem PullEntity(String externID, String externalInfo)
		{
			ProductData data = productDataProvider.GetByID(externID);
			if (data == null) return null;

			MappedTemplateItem obj = new MappedTemplateItem(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));

			return obj;
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			//No DateTime filtering for Category
			FilterProducts filter = new FilterProducts
			{
				UpdatedAtMin = minDateTime == null ? (DateTime?)null : minDateTime.Value.ToLocalTime().AddSeconds(-GetBindingExt<BCBindingShopify>().ApiDelaySeconds ?? 0),
				UpdatedAtMax = maxDateTime == null ? (DateTime?)null : maxDateTime.Value.ToLocalTime()
			};

			IEnumerable<ProductData> datas = productDataProvider.GetAll(filter);
			if (datas?.Count() > 0)
			{
				foreach (ProductData data in datas)
				{
					SPTemplateItemEntityBucket bucket = CreateBucket();

					MappedTemplateItem obj = bucket.Product = bucket.Product.Set(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));
					EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
					if (data.Variants?.Count > 0)
					{
						data.Variants.ForEach(x => { bucket.VariantMappings[x.Sku] = Tuple.Create(x.Id, x.InventoryManagement, x.InventoryPolicy); });
					}
				}
			}
		}
		public override EntityStatus GetBucketForImport(SPTemplateItemEntityBucket bucket, BCSyncStatus syncstatus)
		{
			ProductData data = productDataProvider.GetByID(syncstatus.ExternID);
			if (data == null) return EntityStatus.None;

			if (data.Variants?.Count > 0)
			{
				data.Variants.ForEach(x =>
				{
					if (bucket.VariantMappings.ContainsKey(x.Sku))
						bucket.VariantMappings[x.Sku] = Tuple.Create(x.Id, x.InventoryManagement, x.InventoryPolicy);
					else
						bucket.VariantMappings.Add(x.Sku, Tuple.Create(x.Id, x.InventoryManagement, x.InventoryPolicy));
				});
			}
			MappedTemplateItem obj = bucket.Product = bucket.Product.Set(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));
			EntityStatus status = EnsureStatus(obj, SyncDirection.Import);

			return status;
		}

		public override void MapBucketImport(SPTemplateItemEntityBucket bucket, IMappedEntity existing)
		{

		}
		public override void SaveBucketImport(SPTemplateItemEntityBucket bucket, IMappedEntity existing, String operation)
		{

		}
		#endregion

		#region Export
		public override IEnumerable<MappedTemplateItem> PullSimilar(ILocalEntity entity, out string uniqueField)
		{
			TemplateItems localEnity = (TemplateItems)entity;
			uniqueField = localEnity?.InventoryID?.Value;
			IEnumerable<ProductData> datas = null;
			List<string> matrixIds = new List<string>();
			if (localEnity?.Matrix?.Count > 0)
			{
				matrixIds = localEnity.Matrix.Select(x => x?.InventoryID?.Value).ToList();
				var ExternProductVariantData = PXContext.GetSlot<List<ProductVariantData>>(nameof(ProductVariantData));
				if (ExternProductVariantData == null || ExternProductVariantData?.Count == 0 || !ExternProductVariantData.Any(x => matrixIds.Any(id => string.Equals(id, x.Sku, StringComparison.OrdinalIgnoreCase))))
				{
					ExternProductVariantData = productVariantDataProvider.GetAllWithoutParent(new FilterWithFields() { Fields = "id,product_id,sku,title" })?.ToList();
					PXContext.SetSlot<List<ProductVariantData>>(nameof(ProductVariantData), ExternProductVariantData);
				}
				var existedItems = ExternProductVariantData?.Where(x => matrixIds.Any(id => string.Equals(id, x.Sku, StringComparison.OrdinalIgnoreCase)));
				if (existedItems != null && existedItems?.Count() > 0)
				{
					var matchedVariants = existedItems.Select(x => x.ProductId).Distinct().ToList();
					if (matchedVariants != null && matchedVariants.Count > 0)
					{
						datas = productDataProvider.GetAll(new FilterProducts() { IDs = string.Join(",", matchedVariants) });
					}
				}
			}

			return datas == null ? null : datas.Select(data => new MappedTemplateItem(data, data.Id.ToString(), data.DateModifiedAt.ToDate(false)));
		}

		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			TemplateItems item = new TemplateItems()
			{
				InventoryID = new StringReturn(),
				IsStockItem = new BooleanReturn(),
				Matrix = new List<MatrixItems>() { new MatrixItems() { InventoryID = new StringReturn() } },
				Categories = new List<CategoryStockItem>() { new CategoryStockItem() { CategoryID = new IntReturn() } },
				ExportToExternal = new BooleanReturn()
			};
			IEnumerable<TemplateItems> impls = cbapi.GetAll<TemplateItems>(item, minDateTime, maxDateTime, filters);

			if (impls != null)
			{
				int countNum = 0;
				List<IMappedEntity> mappedList = new List<IMappedEntity>();
				foreach (TemplateItems impl in impls)
				{
					IMappedEntity obj = new MappedTemplateItem(impl, impl.SyncID, impl.SyncTime);

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
		public override EntityStatus GetBucketForExport(SPTemplateItemEntityBucket bucket, BCSyncStatus syncstatus)
		{
			TemplateItems impl = cbapi.GetByID<TemplateItems>(syncstatus.LocalID, GetCustomFieldsForExport());
			if (impl == null || impl.Matrix?.Count == 0) return EntityStatus.None;

			impl.AttributesDef = new List<AttributeDefinition>();
			impl.AttributesValues = new List<AttributeValue>();
			int? inventoryID = null;
			foreach (PXResult<CSAttribute, CSAttributeGroup, INItemClass, InventoryItem> attributeDef in PXSelectJoin<CSAttribute,
			   InnerJoin<CSAttributeGroup, On<CSAttributeGroup.attributeID, Equal<CSAttribute.attributeID>>,
			   InnerJoin<INItemClass, On<INItemClass.itemClassID, Equal<CSAttributeGroup.entityClassID>>,
			   InnerJoin<InventoryItem, On<InventoryItem.itemClassID, Equal<INItemClass.itemClassID>>>>>,
			  Where<InventoryItem.isTemplate, Equal<True>,
			  And<InventoryItem.noteID, Equal<Required<InventoryItem.noteID>>,
			  And<CSAttribute.controlType, Equal<Required<CSAttribute.controlType>>,
			  And<CSAttributeGroup.isActive, Equal<True>,
			  And<CSAttributeGroup.attributeCategory, Equal<CSAttributeGroup.attributeCategory.variant>
			  >>>>>>.Select(this, impl.Id, 2))
			{
				AttributeDefinition def = new AttributeDefinition();
				var inventory = (InventoryItem)attributeDef;
				inventoryID = inventory.InventoryID;
				var attribute = (CSAttribute)attributeDef;
				var attributeGroup = (CSAttributeGroup)attributeDef;
				def.AttributeID = attribute.AttributeID.ValueField();
				def.Description = attribute.Description.ValueField();
				def.NoteID = attribute.NoteID.ValueField();
				def.Order = attributeGroup.SortOrder.ValueField();

				def.Values = new List<AttributeDefinitionValue>();
				var attributedetails = PXSelect<CSAttributeDetail, Where<CSAttributeDetail.attributeID, Equal<Required<CSAttributeDetail.attributeID>>>>.Select(this, def.AttributeID.Value);
				foreach (CSAttributeDetail value in attributedetails)
				{
					AttributeDefinitionValue defValue = new AttributeDefinitionValue();
					defValue.NoteID = value.NoteID.ValueField();
					defValue.ValueID = value.ValueID.ValueField();
					defValue.Description = value.Description.ValueField();
					defValue.SortOrder = (value.SortOrder ?? 0).ToInt().ValueField();
					def.Values.Add(defValue);
				}

				if (def != null)
					impl.AttributesDef.Add(def);
			}

			foreach (PXResult<InventoryItem, CSAnswers> attributeDef in PXSelectJoin<InventoryItem,
			   InnerJoin<CSAnswers, On<InventoryItem.noteID, Equal<CSAnswers.refNoteID>>>,
			  Where<InventoryItem.templateItemID, Equal<Required<InventoryItem.templateItemID>>
			  >>.Select(this, inventoryID))
			{
				var inventory = (InventoryItem)attributeDef;
				var attribute = (CSAnswers)attributeDef;
				AttributeValue def = new AttributeValue();
				def.AttributeID = attribute.AttributeID.ValueField();
				def.NoteID = inventory.NoteID.ValueField();
				def.InventoryID = inventory.InventoryCD.ValueField();
				def.Value = attribute.Value.ValueField();
				impl.AttributesValues.Add(def);
			}
			impl.InventoryItemID = inventoryID;

			MappedTemplateItem obj = bucket.Product = bucket.Product.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(obj, SyncDirection.Export);
			if (impl.AttributesDef.Count > ShopifyConstants.ProductOptionsLimit)
			{
				throw new PXException(ShopifyMessages.ProductOptionsOutOfScope, impl.AttributesDef.Count, impl.InventoryID.Value, ShopifyConstants.ProductOptionsLimit);
			}
			if (impl.Matrix?.Count > 0)
			{
				var activeMatrixItems = impl.Matrix.Where(x => x?.ItemStatus?.Value == PX.Objects.IN.Messages.Active);
				if (activeMatrixItems.Count() == 0)
				{
					throw new PXException(BCMessages.NoMatrixCreated);
				}
				if (activeMatrixItems.Count() > ShopifyConstants.ProductVarantsLimit)
				{
					throw new PXException(ShopifyMessages.ProductVariantsOutOfScope, activeMatrixItems.Count(), impl.InventoryID.Value, ShopifyConstants.ProductVarantsLimit);
				}
				foreach (var item in activeMatrixItems)
				{
					if (!bucket.VariantMappings.ContainsKey(item.InventoryID?.Value))
						bucket.VariantMappings.Add(item.InventoryID?.Value, null);
				}
			}
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

		public override void MapBucketExport(SPTemplateItemEntityBucket bucket, IMappedEntity existing)
		{
			MappedTemplateItem obj = bucket.Product;
			ProductData presented = existing?.Extern as ProductData;
			TemplateItems impl = obj.Local;

			//Existing item and store Availability Policies
			string storeAvailability = BCItemAvailabilities.Convert(GetBindingExt<BCBindingExt>().Availability);
			string storeNotAvailableMode = BCItemNotAvailModes.Convert(GetBindingExt<BCBindingExt>().NotAvailMode);

			InventoryPolicy? currentPolicy = obj.Extern?.Variants[0]?.InventoryPolicy;
			// Value can be null or 'shopify' but if the external item does not exist, we need to go along with the SP standard behaviour - set 'shopify'
			string currentAvail = presented != null ? presented?.Variants[0]?.InventoryManagement : ShopifyConstants.InventoryManagement_Shopify;
			bool? currentlyPublished = presented?.Published;

			ProductData data = obj.Extern = new ProductData();

			data.Title = impl.Description?.Value;
			data.BodyHTML = ClearHTMLContent(impl.Content?.Value);
			data.ProductType = impl.ItemClassDescription?.Value;
			//data.Vendor = (impl.VendorDetails?.FirstOrDefault(v => v.Default?.Value == true)?? impl.VendorDetails?.FirstOrDefault())?.VendorName?.Value;
			//Put all categories to the Tags later if CombineCategoriesToTags setting is true
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
			if (availability == null || availability == BCCaptions.StoreDefault) availability = storeAvailability;
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
				data.Metafields.Add(new MetafieldData() { Key = ShopifyCaptions.ProductId, Value = impl.InventoryID.Value, ValueType = ShopifyConstants.ValueType_String, Namespace = ShopifyConstants.Namespace_Global });
				data.Metafields.Add(new MetafieldData() { Key = nameof(ProductTypes), Value = BCCaptions.TemplateItem, ValueType = ShopifyConstants.ValueType_String, Namespace = ShopifyConstants.Namespace_Global });
			}
			/////
			if (impl.AttributesDef?.Count > 0)
			{
				data.Options = new List<ProductOptionData>();
				int optionSortOrder = 1;
				//Shopify only allows maximum 3 options
				foreach (var attribute in impl.AttributesDef.OrderBy(x=>x.Order?.Value?? short.MaxValue).Take(ShopifyConstants.ProductOptionsLimit))
				{
					data.Options.Add(new ProductOptionData() { Name = attribute.Description?.Value, Position = optionSortOrder });
					optionSortOrder++;
				}
			}
			var results = PXSelectJoin<InventoryItem,
			LeftJoin<INItemXRef, On<InventoryItem.inventoryID, Equal<INItemXRef.inventoryID>,
				And<Where2<Where<INItemXRef.alternateType, Equal<INAlternateType.vPN>,
								And<INItemXRef.bAccountID, Equal<InventoryItem.preferredVendorID>>>,
					 Or<INItemXRef.alternateType, Equal<INAlternateType.barcode>>>>>>, Where<InventoryItem.templateItemID, Equal<Required<InventoryItem.templateItemID>>>>.
					 Select(this, impl.InventoryItemID).Cast<PXResult<InventoryItem, INItemXRef>>()?.ToList();


			var variants = data.Variants = new List<ProductVariantData>();
			foreach (var item in obj.Local.Matrix.Where(x => IsVariantActive(x)).Take(ShopifyConstants.ProductVarantsLimit))
			{
				bool hasExternalVariantWithItemID = bucket.VariantMappings.ContainsKey(item.InventoryID?.Value) && bucket.VariantMappings[item.InventoryID?.Value] != null;
				var matchedInventoryItems = results?.Where(x => x.GetItem<InventoryItem>().InventoryCD.Trim() == item.InventoryID?.Value?.Trim()).ToList();
				var matchedItem = matchedInventoryItems.FirstOrDefault()?.GetItem<InventoryItem>();

				//Boolean isItemPurcahsable = BCItemAvailabilities.Resolve(BCItemAvailabilities.Convert(matchedItem.Availability), GetBindingExt<BCBindingExt>().Availability) != BCItemAvailabilities.Disabled;

				ProductVariantData variant = new ProductVariantData();
				variant.LocalID = item.Id.Value;
				variant.Id = hasExternalVariantWithItemID ? bucket.VariantMappings[item.InventoryID?.Value]?.Item1 : null;
				variant.Title = item.Description?.Value ?? impl.Description?.Value;
				variant.Price = item.DefaultPrice.Value;
				variant.Sku = item.InventoryID?.Value;
				variant.OriginalPrice = item.MSRP?.Value != null && item.MSRP?.Value > item.DefaultPrice.Value ? item.MSRP?.Value : null;
				variant.Weight = (matchedItem?.BaseItemWeight ?? 0) != 0 ? matchedItem?.BaseItemWeight : impl.DimensionWeight?.Value;
				variant.WeightUnit = (matchedItem?.WeightUOM ?? impl.WeightUOM?.Value)?.ToLower();
				bool isTaxable;
				bool.TryParse(GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxCategorySubstitutionListID, impl.TaxCategory?.Value, String.Empty), out isTaxable);
				variant.Taxable = isTaxable;
				if (!string.IsNullOrWhiteSpace(obj.Local.BaseUOM?.Value))
				{
					variant.Barcode = (matchedInventoryItems?.FirstOrDefault(x => x.GetItem<INItemXRef>()?.AlternateType == INAlternateType.Barcode
								&& x.GetItem<INItemXRef>()?.UOM == obj.Local.BaseUOM.Value) ??
								matchedInventoryItems?.FirstOrDefault(x => x.GetItem<INItemXRef>()?.AlternateType == INAlternateType.Barcode
								&& string.IsNullOrEmpty(x.GetItem<INItemXRef>().UOM)))?.GetItem<INItemXRef>().AlternateID;
				}
				if (impl.IsStockItem?.Value == true)
				{
					variant.InventoryPolicy = hasExternalVariantWithItemID ? bucket.VariantMappings[item.InventoryID?.Value]?.Item3 : InventoryPolicy.Continue;
					if (availability != BCCaptions.DoNotUpdate)
					{
						variant.InventoryManagement = availability == BCCaptions.AvailableTrack ? ShopifyConstants.InventoryManagement_Shopify : null;
                    }
                    else
					{
						variant.InventoryManagement = hasExternalVariantWithItemID ? bucket.VariantMappings[item.InventoryID?.Value]?.Item2 : ShopifyConstants.InventoryManagement_Shopify;
					}

				}
				else
				{
					variant.InventoryPolicy = InventoryPolicy.Continue;
					variant.InventoryManagement = null;
					variant.RequiresShipping = presented?.Variants?.FirstOrDefault(x => x != null && string.Equals(item.InventoryID?.Value, x?.Sku, StringComparison.OrdinalIgnoreCase)) == null ? impl.RequireShipment.Value ?? false : (bool?)null;
				}
				var def = obj.Local.AttributesValues.Where(x => x.NoteID.Value == item.Id).ToList();
				foreach (var attrItem in def)
				{
					var optionObj = obj.Local.AttributesDef.FirstOrDefault(x => x.AttributeID.Value == attrItem.AttributeID.Value);
					if (optionObj != null)
					{

						var option = data.Options.FirstOrDefault(x => optionObj != null && x.Name == optionObj.Description?.Value);
						if (option == null) continue;
						var attrValue = optionObj.Values.FirstOrDefault(x => x.ValueID?.Value == attrItem?.Value.Value);

						switch (option.Position)
						{
							case 1:
								{
									variant.Option1 = attrValue?.Description?.Value;
									variant.OptionSortOrder1 = attrValue.SortOrder.Value.Value;
									break;
								}
							case 2:
								{
									variant.Option2 = attrValue?.Description?.Value;
									variant.OptionSortOrder2 = attrValue.SortOrder.Value.Value;
									break;
								}
							case 3:
								{
									variant.Option3 = attrValue?.Description?.Value;
									variant.OptionSortOrder3 = attrValue.SortOrder.Value.Value;
									break;
								}
							default:
								break;
						}
					}
				}
				if (variant.Id == null || variant.Id == 0)
					variant.Metafields = new List<MetafieldData>() { new MetafieldData() { Key = ShopifyConstants.Variant, Value = item.Id.Value.ToString(), ValueType = ShopifyConstants.ValueType_String, Namespace = ShopifyConstants.Namespace_Global } };
				variants.Add(variant);
			}
			if (variants.Count > 0)
			{
				int i = 1;
				foreach (var item in variants.OrderBy(x => x.OptionSortOrder1).ThenBy(x => x.OptionSortOrder2).ThenBy(x => x.OptionSortOrder3))
				{
					item.Position = i;
					i++;
				}
			}

		}

		public override object GetAttribute(SPTemplateItemEntityBucket bucket, string attributeID)
		{
			MappedTemplateItem obj = bucket.Product;
			TemplateItems impl = obj.Local;
			return impl.Attributes?.Where(x => string.Equals(x?.AttributeDescription?.Value, attributeID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

		}

		public override void SaveBucketExport(SPTemplateItemEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedTemplateItem obj = bucket.Product;
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
					var notExistedVariantIds = bucket.VariantMappings.Where(x => !skus.Contains(x.Key)).Select(x => x.Value.Item1).ToList();
					if (notExistedVariantIds?.Count > 0)
					{
						obj.Details.ToList().RemoveAll(x => notExistedVariantIds.Contains(x.ExternID.ToLong()));
						notExistedVariantIds.ForEach(x =>
						{
							if (x != null) productVariantDataProvider.Delete(obj.ExternID, x.ToString());
						});
					}
					data = productDataProvider.Update(obj.Extern, obj.ExternID);
				}
			}
			catch (Exception ex)
			{
				throw new PXException(ex.Message);
			}

			obj.AddExtern(data, data.Id?.ToString(), data.DateModifiedAt.ToDate(false));
			if (data.Variants?.Count > 0)
			{
				var localVariants = obj.Local.Matrix;
				foreach (var externVariant in data.Variants)
				{
					var matchItem = localVariants.FirstOrDefault(x => x.InventoryID?.Value == externVariant.Sku);
					if (matchItem != null)
					{
						obj.AddDetail(BCEntitiesAttribute.Variant, matchItem.Id.Value, externVariant.Id.ToString());
					}
				}
			}

			SaveImages(obj, obj.Local?.FileURLs);

			UpdateStatus(obj, operation);
		}
		#endregion
		
		public virtual bool IsVariantActive(MatrixItems item)
		{
			return !(item.ItemStatus?.Value == PX.Objects.IN.Messages.Inactive || item.ItemStatus?.Value == PX.Objects.IN.Messages.ToDelete || item.ItemStatus?.Value == PX.Objects.IN.Messages.NoSales);
		}
	}
}