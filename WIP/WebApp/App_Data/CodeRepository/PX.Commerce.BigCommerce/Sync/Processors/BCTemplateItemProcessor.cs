using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce
{
	public class BCProductWithVariantEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Product;
		public IMappedEntity[] Entities => new IMappedEntity[] { Primary };
		public override IMappedEntity[] PreProcessors { get => Categories.ToArray(); }

		public MappedTemplateItem Product;

		public List<MappedCategory> Categories = new List<MappedCategory>();
	}

	public class BCTemplateItem : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
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

				if (obj.Local != null && (obj.Local.Categories == null || obj.Local.Categories.Count <= 0))
				{
					var defaultCategories = obj.Local.IsStockItem?.Value == false ? processor.GetBindingExt<BCBindingExt>().NonStockSalesCategoriesIDs : processor.GetBindingExt<BCBindingExt>().StockSalesCategoriesIDs;
					if (defaultCategories == null)
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogItemSkippedNoCategories, obj.Local.InventoryID?.Value ?? obj.Local.SyncID.ToString()));
					}
				}

				return null;
			});
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			return null;
		}
	}


	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.ProductWithVariant, BCCaptions.TemplateItem,
		IsInternal = false,
		Direction = SyncDirection.Export,
		PrimaryDirection = SyncDirection.Export,
		PrimarySystem = PrimarySystem.Local,
		PrimaryGraph = typeof(PX.Objects.IN.InventoryItemMaint),
		ExternTypes = new Type[] { typeof(ProductData) },
		LocalTypes = new Type[] { typeof(TemplateItems) },
		DetailTypes = new String[] { BCEntitiesAttribute.ProductOption, BCCaptions.ProductOption,
			BCEntitiesAttribute.ProductOptionValue, BCCaptions.ProductOptionValue,
			BCEntitiesAttribute.Variant, BCCaptions.Variant,
			BCEntitiesAttribute.ProductVideo, BCCaptions.ProductVideo },
		AcumaticaPrimaryType = typeof(PX.Objects.IN.InventoryItem),
		AcumaticaPrimarySelect = typeof(Search<PX.Objects.IN.InventoryItem.inventoryCD, Where<PX.Objects.IN.InventoryItem.isTemplate, Equal<True>>>),
		AcumaticaFeaturesSet = typeof(FeaturesSet.matrixItem),
		URL = "products/{0}/edit",
		Requires = new string[] { BCEntitiesAttribute.SalesCategory }
	)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = false,
		PushSources = new String[] { "BC-PUSH-Variants" })]
	[BCProcessorExternCustomField(BCConstants.CustomFields, BigCommerceCaptions.CustomFields, nameof(ProductData.CustomFields), typeof(ProductData))]
	public class BCTemplateItemProcessor : BCProductProcessor<BCTemplateItemProcessor, BCProductWithVariantEntityBucket, MappedTemplateItem>, IProcessor
	{
		private IChildRestDataProvider<ProductsOptionData> productsOptionRestDataProvider;
		private ISubChildRestDataProvider<ProductOptionValueData> productsOptionValueRestDataProvider;
		private IChildRestDataProvider<ProductsVariantData> productVariantRestDataProvider;
		protected ProductVariantBatchRestDataProvider productvariantBatchProvider;

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			productsOptionRestDataProvider = new ProductsOptionRestDataProvider(client);
			productsOptionValueRestDataProvider = new ProductOptionValueRestDataProvider(client);
			productvariantBatchProvider = new ProductVariantBatchRestDataProvider(client);
			productVariantRestDataProvider = new ProductVariantRestDataProvider(client);
		}
		#endregion

		#region Common
		public override MappedTemplateItem PullEntity(Guid? localID, Dictionary<string, object> fields)
		{
			TemplateItems impl = cbapi.GetByID<TemplateItems>(localID);
			if (impl == null) return null;

			MappedTemplateItem obj = new MappedTemplateItem(impl, impl.SyncID, impl.SyncTime);

			return obj;
		}

		public override MappedTemplateItem PullEntity(String externID, String jsonObject)
		{
			ProductData data = productDataProvider.GetByID(externID);
			if (data == null) return null;

			MappedTemplateItem obj = new MappedTemplateItem(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());

			return obj;
		}
		#endregion

		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			FilterProducts filter = new FilterProducts
			{
				MinDateModified = minDateTime == null ? null : minDateTime,
				MaxDateModified = maxDateTime == null ? null : maxDateTime,
			};

			IEnumerable<ProductData> datas = productDataProvider.GetAll(filter);

			foreach (ProductData data in datas)
			{
				BCProductWithVariantEntityBucket bucket = CreateBucket();

				MappedTemplateItem obj = bucket.Product = bucket.Product.Set(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());
				EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
			}
		}
		public override EntityStatus GetBucketForImport(BCProductWithVariantEntityBucket bucket, BCSyncStatus syncstatus)
		{
			FilterProducts filter = new FilterProducts { Include = "variants,options,images,modifiers" };
			ProductData data = productDataProvider.GetByID(syncstatus.ExternID, filter);
			if (data == null) return EntityStatus.None;

			MappedTemplateItem obj = bucket.Product = bucket.Product.Set(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());
			EntityStatus status = EnsureStatus(obj, SyncDirection.Import);

			return status;
		}
		public override void MapBucketImport(BCProductWithVariantEntityBucket bucket, IMappedEntity existing)
		{
			MappedTemplateItem obj = bucket.Product;

			ProductData data = obj.Extern;
			// Following lines added because a stock items and non-stock item processors also have this tax category resolution, 
			// but currently there are not importing processes being used to test this code. We might still need this in future.

			//StringValue tax = obj.Extern?.TaxClassId != null ? GetSubstituteLocalByExtern(
			//		BCSubstitute.TaxClasses,
			//		taxClasses?.Find(i => i.Id == obj.Extern?.TaxClassId)?.Name, "").ValueField() :
			//		obj.Local.TaxCategory;
			TemplateItems impl = obj.Local = new TemplateItems();
			impl.Custom = GetCustomFieldsForImport();

			//Product
			impl.InventoryID = GetEntityKey(PX.Objects.IN.InventoryAttribute.DimensionName, data.Name).ValueField();
			impl.Description = data.Name.ValueField();
			impl.ItemClass = obj.LocalID == null || existing?.Local == null ? PX.Objects.IN.INItemClass.PK.Find(this, GetBindingExt<BCBindingExt>().StockItemClassID)?.ItemClassCD.ValueField() : null;
			impl.DefaultPrice = data.Price.ValueField();
			//impl.TaxCategory = tax;

			foreach (int cat in data.Categories)
			{
				PX.Objects.IN.INCategory incategory = PXSelectJoin<PX.Objects.IN.INCategory,
				LeftJoin<BCSyncStatus, On<PX.Objects.IN.INCategory.noteID, Equal<BCSyncStatus.localID>>>,
				Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
					And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
					And<BCSyncStatus.entityType, Equal<Current<BCEntity.entityType>>,
					And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>.Select(this, cat);

				if (incategory == null || incategory.CategoryID == null) throw new PXException(BCMessages.CategoryNotSyncronizedForItem, data.Name);

				impl.Categories.Add(new CategoryStockItem() { CategoryID = incategory.CategoryID.ValueField() });
			}
		}
		public override void SaveBucketImport(BCProductWithVariantEntityBucket bucket, IMappedEntity existing, string operation)
		{
			MappedTemplateItem obj = bucket.Product;

			if (existing?.Local != null) obj.Local.InventoryID = ((TemplateItems)existing.Local).InventoryID.Value.SearchField();

			TemplateItems impl = cbapi.Put<TemplateItems>(obj.Local, obj.LocalID);

			bucket.Product.AddLocal(impl, impl.SyncID, impl.SyncTime);
			UpdateStatus(obj, operation);
		}

		#endregion

		#region Export
		public override IEnumerable<MappedTemplateItem> PullSimilar(ILocalEntity entity, out string uniqueField)
		{
			List<ProductData> datas = PullSimilar(((TemplateItems)entity)?.Description?.Value, ((TemplateItems)entity)?.InventoryID?.Value, out uniqueField);
			return datas == null ? null : datas.Select(data => new MappedTemplateItem(data, data.Id.ToString(), data.DateModifiedUT.ToDate()));
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
				if(mappedList.Count > 0)
				{
					ProcessMappedListForExport(ref mappedList);
				}
			}
		}

		public override EntityStatus GetBucketForExport(BCProductWithVariantEntityBucket bucket, BCSyncStatus syncstatus)
		{
			TemplateItems impl = cbapi.GetByID<TemplateItems>(syncstatus.LocalID, GetCustomFieldsForExport());
			if (impl == null) return EntityStatus.None;

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
				def.AttributeID = attribute.AttributeID.ValueField();
				def.Description = attribute.Description.ValueField();
				def.NoteID = attribute.NoteID.ValueField();
				def.Values = new List<AttributeDefinitionValue>();
				var attributedetails = PXSelect<CSAttributeDetail, Where<CSAttributeDetail.attributeID, Equal<Required<CSAttributeDetail.attributeID>>>>.Select(this, def.AttributeID.Value);
				foreach (CSAttributeDetail value in attributedetails)
				{
					AttributeDefinitionValue defValue = new AttributeDefinitionValue();
					defValue.NoteID = value.NoteID.ValueField();
					defValue.ValueID = value.ValueID.ValueField();
					defValue.Description = value.Description.ValueField();
					defValue.SortOrder = value.SortOrder.ToInt().ValueField();
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

			if (obj.Local.Categories != null)
			{
				foreach (CategoryStockItem item in obj.Local.Categories)
				{
					BCSyncStatus result = PXSelectJoin<BCSyncStatus,
						InnerJoin<PX.Objects.IN.INCategory, On<PX.Objects.IN.INCategory.noteID, Equal<BCSyncStatus.localID>>>,
						Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
							And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
							And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
							And<PX.Objects.IN.INCategory.categoryID, Equal<Required<PX.Objects.IN.INCategory.categoryID>>>>>>>
						.Select(this, BCEntitiesAttribute.SalesCategory, item.CategoryID.Value);
					if (result != null && result.ExternID != null && result.LocalID != null) continue;

					BCItemSalesCategory implCat = cbapi.Get<BCItemSalesCategory>(new BCItemSalesCategory() { CategoryID = new IntSearch() { Value = item.CategoryID.Value } });
					if (implCat == null) continue;

					MappedCategory mappedCategory = new MappedCategory(implCat, implCat.SyncID, implCat.SyncTime);
					EntityStatus mappedCategoryStatus = EnsureStatus(mappedCategory, SyncDirection.Export);
					if (mappedCategoryStatus == EntityStatus.Deleted)
						throw new PXException(BCMessages.CategoryIsDeletedForItem, item.CategoryID.Value, impl.Description.Value);
					if (mappedCategory.IsNew)
					{
						LogWarning(Operation.LogScope(obj), BCMessages.LogItemCategoryNotSynchronized,
							implCat.Description?.Value, impl.InventoryID?.Value, mappedCategoryStatus.ToString());
						continue;
					}

					bucket.Categories.Add(mappedCategory);
				}
			}
			return status;
		}
		public override void MapBucketExport(BCProductWithVariantEntityBucket bucket, IMappedEntity existing)
		{
			MappedTemplateItem obj = bucket.Product;

			TemplateItems impl = obj.Local;

			//Existing item and store Availability Policies
			string storeAvailability = BCItemAvailabilities.Convert(GetBindingExt<BCBindingExt>().Availability);
			string currentAvail = obj.Extern?.Availability;

			ProductData data = obj.Extern = new ProductData();
			data.ProductsOptionData = new List<ProductsOptionData>();

			if (impl.Matrix == null || impl.Matrix?.Count == 0)
			{
				throw new PXException(BCMessages.NoMatrixCreated);
			}
			//Inventory Item
			data.Name = impl.Description?.Value;
			data.Description = ClearHTMLContent(impl.Content?.Value);
			if (impl.IsStockItem?.Value == false)
				data.Type = ProductsType.Digital.ToEnumMemberAttrValue();
			else
			{
				data.Type = ProductsType.Physical.ToEnumMemberAttrValue();
				data.BinPickingNumber = impl.DefaultIssueLocationID?.Value;

			}
			data.Price = impl.DefaultPrice.Value;
			data.Weight = impl.DimensionWeight.Value;
			data.CostPrice = impl.CurrentStdCost.Value;
			data.RetailPrice = impl.MSRP.Value;
			data.Sku = impl.InventoryID?.Value;
			data.TaxClassId = taxClasses?.Find(i => i.Name.Equals(GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxCategorySubstitutionListID, impl.TaxCategory?.Value, String.Empty)))?.Id;

			//custom fields mapping
			data.PageTitle = impl.PageTitle?.Value;
			data.MetaDescription = impl.MetaDescription?.Value;
			data.MetaKeywords = impl.MetaKeywords?.Value != null ? impl.MetaKeywords?.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : null;
			data.SearchKeywords = impl.SearchKeywords?.Value;
			//var vendor = impl.VendorDetails?.FirstOrDefault(v => v.Default?.Value == true);
			//if (vendor != null)
			//	data.GTIN = impl.CrossReferences?.FirstOrDefault(x => x.AlternateType?.Value == "Vendor Part Number" && x.VendorOrCustomer?.Value == vendor.VendorID?.Value)?.AlternateID?.Value;
			//if (!string.IsNullOrWhiteSpace(impl.BaseUOM?.Value))
			//	data.MPN = impl.CrossReferences?.FirstOrDefault(x => x.AlternateType?.Value == "Barcode" && x.UOM?.Value == impl.BaseUOM?.Value)?.AlternateID?.Value;
			if (impl.CustomURL?.Value != null) data.CustomUrl = new ProductCustomUrl() { Url = impl.CustomURL?.Value, IsCustomized = true };
			if (obj.IsNew) data.InventoryTracking = "none"; // TODO CHECK

			string visibility = impl?.Visibility?.Value;
			if (visibility == null || visibility == BCCaptions.StoreDefault) visibility = BCItemVisibility.Convert(GetBindingExt<BCBindingExt>().Visibility);
			switch (visibility)
			{
				case BCCaptions.Visible:
					data.IsVisible = true;
					break;
				case BCCaptions.Featured:
					{
						data.IsVisible = true;
						data.IsFeatured = true;
						break;
					}
				case BCCaptions.Invisible:
				default:
					{
						data.IsFeatured = false;
						data.IsVisible = false;
						break;
					}
			}
			Boolean isItemActive = !(impl.ItemStatus?.Value == PX.Objects.IN.Messages.Inactive || impl.ItemStatus?.Value == PX.Objects.IN.Messages.ToDelete || impl.ItemStatus?.Value == PX.Objects.IN.Messages.NoSales);
			string availability = impl?.Availability?.Value;
			if (availability == null || availability == BCCaptions.StoreDefault) availability = storeAvailability;
			if (availability != BCCaptions.DoNotUpdate)
			{
				data.Availability = "disabled";
				if (isItemActive)
				{
					switch (availability)
					{
						case BCCaptions.AvailableTrack: data.Availability = "available"; break;
						case BCCaptions.AvailableSkip: data.Availability = "available"; break;
						case BCCaptions.PreOrder: data.Availability = "preorder"; break;
						case BCCaptions.Disabled: data.Availability = "disabled"; break;
					}
				}
			}
			else data.Availability = currentAvail;

			if (impl.AttributesDef?.Count > 0)
			{
				foreach (var item in obj.Local.Matrix)
				{
					var def = obj.Local.AttributesValues.Where(x => x.NoteID.Value == item.Id).ToList();

					foreach (var attrvalue in def)
					{
						var attribute = obj.Local.AttributesDef.FirstOrDefault(x => x.AttributeID.Value == attrvalue.AttributeID.Value);
						if (attribute == null) continue;
						var value = attribute.Values.FirstOrDefault(y => y.ValueID.Value == attrvalue.Value.Value);
						if (value == null) continue;
						ProductsOptionData productsOptionData = data.ProductsOptionData.FirstOrDefault(x => x.LocalID == attribute.NoteID.Value);
						if (productsOptionData == null)
						{
							productsOptionData = new ProductsOptionData();
							productsOptionData.Name = attribute.AttributeID?.Value;
							productsOptionData.DisplayName = attribute.Description?.Value;
							productsOptionData.LocalID = attribute.NoteID?.Value;
							productsOptionData.Type = "dropdown";
							data.ProductsOptionData.Add(productsOptionData);

						}

						if (!productsOptionData.OptionValues.Any(x => x.LocalID == value.NoteID.Value))
						{
							ProductOptionValueData productOptionValueData = new ProductOptionValueData();
							productOptionValueData.Label = value.Description?.Value ?? value.ValueID?.Value;
							productOptionValueData.LocalID = value.NoteID?.Value;
							productOptionValueData.SortOrder = value.SortOrder?.Value ?? 0;
							productsOptionData.OptionValues.Add(productOptionValueData);
						}

					}
				}
			}

			foreach (PXResult<PX.Objects.IN.INCategory, PX.Objects.IN.INItemCategory, PX.Objects.IN.InventoryItem, BCSyncStatus> result in PXSelectJoin<PX.Objects.IN.INCategory,
					InnerJoin<PX.Objects.IN.INItemCategory, On<PX.Objects.IN.INItemCategory.categoryID, Equal<PX.Objects.IN.INCategory.categoryID>>,
					InnerJoin<PX.Objects.IN.InventoryItem, On<PX.Objects.IN.InventoryItem.inventoryID, Equal<PX.Objects.IN.INItemCategory.inventoryID>>,
					LeftJoin<BCSyncStatus, On<PX.Objects.IN.INCategory.noteID, Equal<BCSyncStatus.localID>>>>>,
					Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						And<PX.Objects.IN.InventoryItem.noteID, Equal<Required<PX.Objects.IN.InventoryItem.noteID>>>>>>>
							.Select(this, BCEntitiesAttribute.SalesCategory, obj.LocalID))
			{
				BCSyncStatus status = result.GetItem<BCSyncStatus>();
				if (status == null || status.ExternID == null) throw new PXException(BCMessages.CategoryNotSyncronizedForItem, impl.Description.Value);

				data.Categories.Add(status.ExternID.ToInt().Value);
			}
			if (data.Categories.Count <= 0)
			{
				String categories = null;
				if (impl.IsStockItem?.Value == false)
					categories = GetBindingExt<BCBindingExt>().NonStockSalesCategoriesIDs;
				else
					categories = GetBindingExt<BCBindingExt>().StockSalesCategoriesIDs;

				if (!String.IsNullOrEmpty(categories))
				{
					Int32?[] categoriesArray = categories.Split(',').Select(c => { return Int32.TryParse(c, out Int32 i) ? (int?)i : null; }).Where(i => i != null).ToArray();

					foreach (BCSyncStatus status in PXSelectJoin<BCSyncStatus,
						LeftJoin<PX.Objects.IN.INCategory, On<PX.Objects.IN.INCategory.noteID, Equal<BCSyncStatus.localID>>>,
						Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
							And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
							And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
							And<PX.Objects.IN.INCategory.categoryID, In<Required<PX.Objects.IN.INCategory.categoryID>>>>>>>
							.Select(this, BCEntitiesAttribute.SalesCategory, categoriesArray))
					{
						if (status == null || status.ExternID == null) throw new PXException(BCMessages.CategoryNotSyncronizedForItem, impl.Description.Value);

						if (status != null) data.Categories.Add(status.ExternID.ToInt().Value);
					}
				}
			}
		}

		public override object GetAttribute(BCProductWithVariantEntityBucket bucket, string attributeID)
		{
			MappedTemplateItem obj = bucket.Product;
			TemplateItems impl = obj.Local;
			return impl.Attributes?.Where(x => string.Equals(x?.AttributeDescription?.Value, attributeID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

		}

		public override void SaveBucketExport(BCProductWithVariantEntityBucket bucket, IMappedEntity existing, string operation)
		{
			MappedTemplateItem obj = bucket.Product;

			ProductData data = null;
			List<DetailInfo> existingList = null;
			try
			{
				ValidateLinks(existing, obj);
				if (obj.ExternID == null)
					data = productDataProvider.Create(obj.Extern);
				else
					data = productDataProvider.Update(obj.Extern, obj.ExternID.ToInt().Value);
				existingList = new List<DetailInfo>(obj.Details);
				obj.ClearDetails();
				//copy back other child entities
				existingList.Where(x => x.EntityType != BCEntitiesAttribute.ProductOptionValue && x.EntityType != BCEntitiesAttribute.ProductOption && x.EntityType != BCEntitiesAttribute.Variant)?.ToList().ForEach(x => obj.AddDetail(x.EntityType, x.LocalID, x.ExternID));
				UpdateProductVariantOptions(obj, data, existingList, existing);
				UpdateProductVariant(obj, data, existingList, existing);

			}
			catch
			{
				existingList?.ForEach(x =>
				{
					if (!obj.Details.Any(y => y.LocalID == x.LocalID))
						obj.AddDetail(x.EntityType, x.LocalID, x.ExternID);
				});

				throw;
			}

			ExportCustomFields(obj, obj.Extern.CustomFields, data);
			obj.AddExtern(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());

			SaveImages(obj, obj.Local.FileURLs);
			SaveVideos(obj, obj.Local.FileURLs);

			UpdateStatus(obj, operation);
		}

		public virtual void ValidateLinks(IMappedEntity existing, MappedTemplateItem obj)
		{
			if (existing != null && (obj.Details == null || obj.Details?.Count() == 0))//only while linking to existing 
			{
				var existingProduct = existing.Extern as ProductData;
				if (existingProduct.ProductsOptionData.Count() != obj.Extern.ProductsOptionData.Count() || existingProduct.ProductsOptionData.Any(x => obj.Extern.ProductsOptionData.All(y => !string.Equals(y.DisplayName.Trim(), x.DisplayName?.Trim(), StringComparison.InvariantCultureIgnoreCase))))
				{
					throw new PXException(BigCommerceMessages.OptionsNotMatched, obj.ExternID);

				}
				foreach (var option in obj.Extern.ProductsOptionData)
				{
					var existingoption = existingProduct?.ProductsOptionData?.FirstOrDefault(x => string.Equals(x.DisplayName.Trim(), option.DisplayName?.Trim(), StringComparison.InvariantCultureIgnoreCase));
					if (existingoption.OptionValues.Count() != option.OptionValues.Count() || existingoption.OptionValues.Any(a => option.OptionValues.All(b => !string.Equals(b.Label?.Trim(), a.Label?.Trim(), StringComparison.InvariantCultureIgnoreCase))))
					{
						throw new PXException(BigCommerceMessages.OptionValuesNotMatched, obj.ExternID);
					}
				}

				if (!existingProduct.Variants.All(i => obj.Local.Matrix.Select(x => x.InventoryID.Value).Contains(i.Sku, StringComparer.InvariantCultureIgnoreCase)))
					throw new PXException(BigCommerceMessages.VariantsNotMatched, obj.ExternID);

			}
		}

		public virtual void UpdateProductVariant(MappedTemplateItem obj, ProductData data, List<DetailInfo> existingList, IMappedEntity existing)
		{
			//remove deleted variants  from BC
			List<DetailInfo> deletedVariant = existingList.Where(x => obj.Local.Matrix.All(y => x.LocalID != y.Id && x.EntityType == BCEntitiesAttribute.Variant)).ToList();
			//delete inactive variants from BC
			foreach (var item in obj.Local.Matrix.Where(x => !IsVariantActive(x))) 
			{
				DetailInfo found = existingList.FirstOrDefault(x => x.LocalID == item.Id && x.EntityType == BCEntitiesAttribute.Variant);
				if (found != null && found.ExternID != null && !deletedVariant.Any(x => x.ExternID == found.ExternID))
					deletedVariant.Add(found);
			}
			if (deletedVariant != null)
			{
				foreach (var option in deletedVariant)
				{
					productVariantRestDataProvider.Delete(option?.ExternID, data.Id.ToString());
					existingList.RemoveAll(x => x.LocalID == option.LocalID);
				}
			}

			List<ProductsVariantData> variantData = new List<ProductsVariantData>();
			var results = PXSelectJoin<InventoryItem,
			LeftJoin<INItemXRef, On<InventoryItem.inventoryID, Equal<INItemXRef.inventoryID>,
				And<Where2<Where<INItemXRef.alternateType, Equal<INAlternateType.vPN>,
								And<INItemXRef.bAccountID, Equal<InventoryItem.preferredVendorID>>>,
					 Or<INItemXRef.alternateType, Equal<INAlternateType.barcode>>>>>>, Where<InventoryItem.templateItemID, Equal<Required<InventoryItem.templateItemID>>>>.
					 Select(this, obj.Local.InventoryItemID).Cast<PXResult<InventoryItem, INItemXRef>>()?.ToList();
			foreach (var item in obj.Local.Matrix.Where(x => IsVariantActive(x)))
			{
				var existingId = existingList?.FirstOrDefault(x => x.LocalID == item.Id)?.ExternID?.ToInt();
				if (existingId == null && existing != null)
				{
					var existingProduct = existing.Extern as ProductData;
					existingId = existingProduct?.Variants?.FirstOrDefault(x => string.Equals(x.Sku?.Trim(), item.InventoryID.Value?.Trim(), StringComparison.InvariantCultureIgnoreCase))?.Id;
				}

				List<PXResult<InventoryItem, INItemXRef>> matchedInventoryItems = results?.Where(x => x.GetItem<InventoryItem>().InventoryCD.Trim() == item.InventoryID?.Value?.Trim()).ToList();
				InventoryItem matchedItem = matchedInventoryItems.FirstOrDefault()?.GetItem<InventoryItem>();

				ProductsVariantData variant = new ProductsVariantData();
				variant.LocalID = item.Id;
				variant.ProductId = data.Id;
				if (existingId != null) variant.Id = existingId;
				variant.PurchasingDisabled = !IsVariantPurchasable(item, matchedItem);
				variant.Sku = item.InventoryID.Value;
				variant.Price = item.DefaultPrice.Value;
				variant.RetailPrice = item.MSRP.Value;
				variant.Mpn = matchedInventoryItems?.FirstOrDefault(x => x.GetItem<INItemXRef>().AlternateType == INAlternateType.VPN)?.GetItem<INItemXRef>()?.AlternateID;
				if (!string.IsNullOrWhiteSpace(obj.Local.BaseUOM?.Value))
					variant.Upc = (matchedInventoryItems?.FirstOrDefault(x => x.GetItem<INItemXRef>().AlternateType == INAlternateType.Barcode
								&& x.GetItem<INItemXRef>().UOM == obj.Local.BaseUOM.Value) ??
								matchedInventoryItems?.FirstOrDefault(x => x.GetItem<INItemXRef>().AlternateType == INAlternateType.Barcode
								&& string.IsNullOrEmpty(x.GetItem<INItemXRef>().UOM)))?.GetItem<INItemXRef>().AlternateID;
				variant.Weight = (matchedItem.BaseItemWeight ?? 0) != 0 ? matchedItem.BaseItemWeight : obj.Local.DimensionWeight?.Value;
				variant.OptionValues = new List<ProductVariantOptionValueData>();
				var def = obj.Local.AttributesValues.Where(x => x.NoteID.Value == item.Id).ToList();
				foreach (var value in def)
				{
					ProductVariantOptionValueData optionValueData = new ProductVariantOptionValueData();
					var optionObj = obj.Local.AttributesDef.FirstOrDefault(x => x.AttributeID.Value == value.AttributeID.Value);
					if (optionObj == null) continue;
					var optionValueObj = optionObj.Values.FirstOrDefault(y => y.ValueID.Value == value.Value.Value);
					var detailObj = obj.Details.FirstOrDefault(x => x.LocalID == optionValueObj?.NoteID?.Value);
					if (detailObj == null) continue;
					optionValueData.OptionId = detailObj.ExternID.KeySplit(0).ToInt();
					optionValueData.Id = detailObj.ExternID.KeySplit(1).ToInt();
					variant.OptionValues.Add(optionValueData);
				}
				variantData.Add(variant);
			}
			productvariantBatchProvider.UpdateAll(variantData, delegate (ItemProcessCallback<ProductsVariantData> callback)
			{
				ProductsVariantData request = variantData[callback.Index];
				if (callback.IsSuccess)
				{
					ProductsVariantData productsVariantData = callback.Result;
					obj.AddDetail(BCEntitiesAttribute.Variant, request.LocalID, productsVariantData.Id.ToString());

				}
				else
				{
					throw callback.Error;
				}
			});


		}

		public virtual void UpdateProductVariantOptions(MappedTemplateItem obj, ProductData data, List<DetailInfo> existingList, IMappedEntity existing)
		{
			//remove deleted attributes and values from BC
			var deletedOption = existingList.Where(x => obj.Extern.ProductsOptionData.All(y => x.LocalID != y.LocalID && x.EntityType == BCEntitiesAttribute.ProductOption)).ToList();
			if (deletedOption != null)
			{
				foreach (var option in deletedOption)
				{
					productsOptionRestDataProvider.Delete(option?.ExternID, data.Id.ToString());
					existingList.RemoveAll(x => x.LocalID == option.LocalID);
				}
			}

			var allOptionValues = obj.Extern.ProductsOptionData.SelectMany(y => y.OptionValues);
			var deletedValues = existingList.Where(x => allOptionValues.All(y => x.LocalID != y.LocalID && x.EntityType == BCEntitiesAttribute.ProductOptionValue)).ToList();
			if (deletedValues != null)
			{
				foreach (var value in deletedValues)
				{
					productsOptionValueRestDataProvider.Delete(data.Id.ToString(), value?.ExternID?.KeySplit(0), value?.ExternID?.KeySplit(1));
					existingList.RemoveAll(x => x.LocalID == value.LocalID);
				}
			}
			foreach (var option in obj.Extern.ProductsOptionData)
			{
				var localObj = obj.Local.AttributesDef.FirstOrDefault(x => x.NoteID?.Value == option.LocalID);
				var detailObj = existingList?.Where(x => x.LocalID == localObj?.NoteID?.Value)?.ToList();
				ProductsOptionData existingOption = null;

				if ((detailObj == null || detailObj?.Count() == 0) && existing != null)
				{
					var existingProduct = existing.Extern as ProductData;
					existingOption = existingProduct?.ProductsOptionData?.FirstOrDefault(x => string.Equals(x.DisplayName?.Trim(), option.DisplayName?.Trim(), StringComparison.InvariantCultureIgnoreCase));
				}
				var optionID = detailObj?.FirstOrDefault()?.ExternID ?? existingOption?.Id?.ToString();
				ProductsOptionData response = null;
				if (optionID != null)
				{
					obj.AddDetail(BCEntitiesAttribute.ProductOption, localObj?.NoteID?.Value, optionID);
					foreach (var value in localObj.Values)
					{
						option.Id = optionID.ToInt();
						var optionValue = option.OptionValues.FirstOrDefault(x => x.LocalID == value.NoteID?.Value);
						if (optionValue == null) continue;
						var existingDetail = existingList.FirstOrDefault(x => x.LocalID == value.NoteID.Value);
						string optionValueID = existingDetail?.ExternID?.KeySplit(1);
						if (optionValueID == null)//check if there is existing non synced optionvalue at BC
							optionValueID = existingOption?.OptionValues?.FirstOrDefault(x => string.Equals(x.Label?.Trim(), optionValue.Label?.Trim(), StringComparison.InvariantCultureIgnoreCase))?.Id?.ToString();
						if (optionValueID != null)
						{
							optionValue.Id = optionValueID.ToInt();
						}
					}
					response = productsOptionRestDataProvider.Update(option, optionID, data.Id.ToString());
				}
				else
				{

					response = productsOptionRestDataProvider.Create(option, data.Id.ToString());
						if (response != null)
					obj.AddDetail(BCEntitiesAttribute.ProductOption, localObj?.NoteID?.Value, response.Id.ToString());

				}
				if (response != null)
				{
					foreach (var value in response.OptionValues)
					{
						var localId = localObj.Values.FirstOrDefault(x => string.Equals(x.Description?.Value, value.Label, StringComparison.InvariantCultureIgnoreCase) || string.Equals(x.ValueID?.Value, value.Label, StringComparison.InvariantCultureIgnoreCase))?.NoteID?.Value;
						obj.AddDetail(BCEntitiesAttribute.ProductOptionValue, localId, new object[] { response.Id.ToString(), value.Id.ToString() }.KeyCombine());
					}
			}
			}


		}
		#endregion

		public virtual bool IsVariantActive(MatrixItems item)
		{
			return !(item.ItemStatus?.Value == PX.Objects.IN.Messages.Inactive || item.ItemStatus?.Value == PX.Objects.IN.Messages.ToDelete || item.ItemStatus?.Value == PX.Objects.IN.Messages.NoSales);
		}
		public virtual bool IsVariantPurchasable(MatrixItems item, InventoryItem matchedItem)
		{
			return BCItemAvailabilities.Resolve(BCItemAvailabilities.Convert(matchedItem.Availability), GetBindingExt<BCBindingExt>().Availability) != BCCaptions.Disabled;
		}
	}
}
