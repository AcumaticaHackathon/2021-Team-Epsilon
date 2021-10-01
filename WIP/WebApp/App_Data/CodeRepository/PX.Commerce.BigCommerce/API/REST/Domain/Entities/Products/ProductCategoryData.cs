
using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
    [JsonObject(Description = "ProductCategory")]
	[CommerceDescription(BigCommerceCaptions.ProductCategoryData)]
    public class ProductCategoryData : BCAPIEntity
	{

        [JsonProperty("id")]
		[CommerceDescription(BigCommerceCaptions.ID, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public int? Id { get; set; }

        [JsonProperty("parent_id")]
		[CommerceDescription(BigCommerceCaptions.ParentID, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public int? ParentId { get; set; }
        [JsonProperty("name")]
		[CommerceDescription(BigCommerceCaptions.CategoryName, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired()]
        public string Name { get; set; }

        [JsonProperty("description")]
		[CommerceDescription(BigCommerceCaptions.CategoryDescription, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string Description { get; set; }

        [JsonProperty("views")]
        public int? Views { get; set; }

        [JsonProperty("sort_order")]
		[CommerceDescription(BigCommerceCaptions.SortOrder, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public int? SortOrder { get; set; }

        [JsonProperty("page_title")]
		[CommerceDescription(BigCommerceCaptions.PageTitle, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string PageTitle { get; set; }

        [JsonProperty("meta_keywords")]
		public string[] MetaKeywords { get; set; }

        [JsonProperty("meta_description")]
		[CommerceDescription(BigCommerceCaptions.MetaDescription, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string MetaDescription { get; set; }

        [JsonProperty("layout_file")]
		[CommerceDescription(BigCommerceCaptions.LayoutFile, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string LayoutFile { get; set; }

        [JsonProperty("image_url")]
		[CommerceDescription(BigCommerceCaptions.ImageUrl, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string ImageUrl { get; set; }

        [JsonProperty("is_visible")]
		[CommerceDescription(BigCommerceCaptions.IsVisible, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public bool? IsVisible { get; set; }

        [JsonProperty("search_keywords")]
		[CommerceDescription(BigCommerceCaptions.SearchKeywords, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string SearchKeywords { get; set; }

        [JsonProperty("default_product_sort")]
		[CommerceDescription(BigCommerceCaptions.DefaultProductSort, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string DefaultProductSort { get; set; }

        [JsonProperty("custom_url")]
		[CommerceDescription(BigCommerceCaptions.CustomUrl)]
		public ProductCustomUrl CustomUrl { get; set; }

        public override string ToString()
        {
            return $"{Name}, ID: {Id}, ParentID: {ParentId} ";
        }
	}
}
