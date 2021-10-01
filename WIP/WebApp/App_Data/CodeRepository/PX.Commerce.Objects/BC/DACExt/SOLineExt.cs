using System;
using PX.Data;
using PX.Objects.SO;
using System.Collections.Generic;
using PX.Commerce.Core;
using PX.Data.WorkflowAPI;
using PX.Data.EP;

namespace PX.Commerce.Objects
{
	[Serializable]
	public class BCSOLineExt : PXCacheExtension<SOLine>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public virtual String OrderType { get; set; }

		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public virtual String OrderNbr { get; set; }
		#endregion

		#region ExternalRef
		public abstract class externalRef : PX.Data.BQL.BqlString.Field<externalRef> { }
		[PXString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "External Ref.")]
		public virtual string ExternalRef { get; set; }
		#endregion
	}
}