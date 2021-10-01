using PX.Commerce.Core;
using PX.CS;
using PX.Data;
using PX.Data.EP;
using PX.Objects.AR;
using PX.Objects.SO;
using System;

namespace PX.Commerce.Objects
{
	public class BCAttributeExt : PXCacheExtension<CSAttribute>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }
		#region Attribute ID
		public abstract class attributeID : PX.Data.BQL.BqlString.Field<attributeID> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public virtual string AttributeID { get; set; }

		#endregion
		
	}

	public class BCAttributeValueExt : PXCacheExtension<CSAttributeDetail>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }
		#region Value ID
		public abstract class valueID : PX.Data.BQL.BqlString.Field<valueID> { }
		protected String _OrderType;
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public virtual string ValueID { get; set; }

		#endregion
		
	}

}
