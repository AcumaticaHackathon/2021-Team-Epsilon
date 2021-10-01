using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PX.Commerce.Shopify.API.REST
{
	/// <summary>
	/// The status of the product. Valid values: active, archived,draft.
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public enum ProductStatus
	{
		/// <summary>
		/// active: The product is ready to sell and is available to customers on the online store, sales channels, and apps. 
		/// By default, existing products are set to active.
		/// </summary>
		[EnumMember(Value = "active")]
		Active = 0,

		/// <summary>
		/// archived: The product is no longer being sold and isn't available to customers on sales channels and apps.
		/// </summary>
		[EnumMember(Value = "archived")]
		Archived = 1,

		/// <summary>
		/// draft: The product isn't ready to sell and is unavailable to customers on sales channels and apps. 
		/// By default, duplicated and unarchived products are set to draft.
		/// </summary>
		[EnumMember(Value = "draft")]
		Draft = 2

	}
}
