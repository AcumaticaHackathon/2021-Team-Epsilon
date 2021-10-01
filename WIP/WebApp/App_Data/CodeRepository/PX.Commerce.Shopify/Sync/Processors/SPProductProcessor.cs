using PX.Commerce.Shopify.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Commerce.Objects.Substitutes;
using PX.Data;
using PX.Objects.IN.RelatedItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
namespace PX.Commerce.Shopify
{
	public abstract class SPProductProcessor<TGraph, TEntityBucket, TPrimaryMapped> : BCProcessorSingleBase<TGraph, TEntityBucket, TPrimaryMapped>
		where TGraph : PXGraph
		where TEntityBucket : class, IEntityBucket, new()
		where TPrimaryMapped : class, IMappedEntity, new()
	{
		protected IParentRestDataProvider<ProductData> productDataProvider;
		protected IChildRestDataProvider<ProductVariantData> productVariantDataProvider;
		protected IChildRestDataProvider<ProductImageData> productImageDataProvider;
		protected IEnumerable<ProductVariantData> ExternProductVariantData = new List<ProductVariantData>();
		protected Dictionary<int, string> SalesCategories;

		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			var client = SPConnector.GetRestClient(GetBindingExt<BCBindingShopify>());

			productDataProvider = new ProductRestDataProvider(client);
			productVariantDataProvider = new ProductVariantRestDataProvider(client);
			productImageDataProvider = new ProductImageRestDataProvider(client);

			SalesCategories = new Dictionary<int, string>();
		}

		public virtual void SaveImages(IMappedEntity obj, List<InventoryFileUrls> urls)
		{
			var fileURLs = urls?.Where(x => x.FileType?.Value == BCCaptions.Image && !string.IsNullOrEmpty(x.FileURL?.Value))?.ToList();
			if (fileURLs == null || fileURLs.Count() == 0) return;

			List<ProductImageData> imageList = null;
			foreach (var image in fileURLs)
			{
				ProductImageData productImageData = null;
				try
				{
					if (imageList == null)
						imageList = productImageDataProvider.GetAll(obj.ExternID, new FilterWithFields() { Fields = "id,product_id,src,variant_ids,position" }).ToList();
					if (imageList?.Count > 0)
					{
						productImageData = imageList.FirstOrDefault(x => (x.Metafields != null && x.Metafields.Any(m => string.Equals(m.Key, ShopifyConstants.ProductImage, StringComparison.OrdinalIgnoreCase) 
							&& string.Equals(m.Value, image.FileURL.Value, StringComparison.OrdinalIgnoreCase))));
						if (productImageData != null)
						{
							if (obj.Details?.Any(x => x.EntityType == BCEntitiesAttribute.ProductImage && x.LocalID == image.NoteID?.Value) == false)
							{
								obj.AddDetail(BCEntitiesAttribute.ProductImage, image.NoteID.Value, productImageData.Id.ToString());
							}
							continue;
						}
					};
					productImageData = new ProductImageData()
					{
						Src = Uri.EscapeUriString(System.Web.HttpUtility.UrlDecode(image.FileURL.Value)),
						Metafields = new List<MetafieldData>() { new MetafieldData() { Key = ShopifyConstants.ProductImage, Value = image.FileURL.Value, ValueType = ShopifyConstants.ValueType_String, Namespace = ShopifyConstants.Namespace_Global } },
					};
					var metafields = productImageData.Metafields;
					productImageData = productImageDataProvider.Create(productImageData, obj.ExternID);
					productImageData.Metafields = metafields;
					if (obj.Details?.Any(x => x.EntityType == BCEntitiesAttribute.ProductImage && x.LocalID == image.NoteID?.Value) == false)
					{
						obj.AddDetail(BCEntitiesAttribute.ProductImage, image.NoteID.Value, productImageData.Id.ToString());
					}
					imageList = imageList ?? new List<ProductImageData>();
					imageList.Add(productImageData);
				}
				catch (Exception ex)
				{
					throw new PXException(ex.Message);
				}
			}
		}
	}
}
