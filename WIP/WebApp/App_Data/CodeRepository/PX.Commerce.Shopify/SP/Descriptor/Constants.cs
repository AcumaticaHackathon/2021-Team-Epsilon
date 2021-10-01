using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Common;

namespace PX.Commerce.Shopify
{
	public static class ShopifyConstants
	{
		//Constant Value
		public const int ApiCallLimitDefault = 2;
		public const int ApiCallLimitPlus = 4;
		public const int ProductOptionsLimit = 3;
		public const int ProductVarantsLimit = 100;

		public const string ApiVersion_202010 = "2020-10";
		public const string ApiVersion_202101 = "2021-01";
		public const string ApiVersion_202007 = "2020-07";
		public const string ApiVersion_202001 = "2020-01";
		public const string ApiVersion_202004 = "2020-04";
		public const string InventoryManagement_Shopify = "shopify";
		public const string Namespace_Global = "global";
		public const string ValueType_String = "string";
		public const string Variant = "Variant";
		public const string ImportedInAcumatica = "ERP";
		public const string POSSource = "pos";
		public const string ProductImage = "ProductImage";
		public const string Bogus = "bogus";
		public const string ShopifyPayments = "shopify_payments";
	}
}
