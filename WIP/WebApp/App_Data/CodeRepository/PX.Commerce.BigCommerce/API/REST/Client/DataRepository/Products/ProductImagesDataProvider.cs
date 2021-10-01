using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class ProductImagesDataProvider : RestDataProviderV3, IChildRestDataProvider<ProductsImageData>
	{
		protected override string GetListUrl { get; } = "v3/catalog/products/{parent_id}/images";
		protected override string GetSingleUrl { get; } = "v3/catalog/products/{parent_id}/images/{id}";
		protected override string GetCountUrl { get; } = string.Empty;

		public ProductImagesDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}

		public ProductsImageData Create(ProductsImageData productsImageData, string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);
			return Create<ProductsImageData, ProductsImage>(productsImageData, segments)?.Data;
		}

		public ProductsImageData Update(ProductsImageData productsImageData, string id,string parentId)
		{
			var segments = MakeUrlSegments(id, parentId);
			return Update<ProductsImageData, ProductsImage>(productsImageData, segments)?.Data;
		}
		public bool Delete(string id, string parentId)
		{
			var segments = MakeUrlSegments(id, parentId);
			return base.Delete(segments);
		}

		public List<ProductsImageData> Get(string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);
			return base.Get<ProductsImageData, ProductsImageList>(null, segments).Data;
		}

		public IEnumerable<ProductsImageData> GetAll(string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);
			return GetAll<ProductsImageData, ProductsImageList>(null, segments);
		}

		#region Not implemented 

		public int Count(string parentId)
		{
			throw new System.NotImplementedException();
		}

		public ProductsImageData GetByID(string id, string parentId)
		{
			throw new System.NotImplementedException();
		}

		#endregion
	}
}
