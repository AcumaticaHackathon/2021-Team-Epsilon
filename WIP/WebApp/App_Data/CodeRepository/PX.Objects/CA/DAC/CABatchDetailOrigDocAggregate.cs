using PX.Data;
using PX.Data.BQL.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CA
{
	[Serializable]
	[PXProjection(typeof(SelectFrom<CABatchDetail>.AggregateTo<GroupBy<CABatchDetail.batchNbr>, GroupBy<CABatchDetail.origModule>, GroupBy<CABatchDetail.origDocType>, GroupBy<CABatchDetail.origRefNbr>>))]
	[PXCacheName("Aggregated CA Batch Details")]
	public class CABatchDetailOrigDocAggregate : IBqlTable
	{
		#region BatchNbr
		public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(CABatchDetail.batchNbr))]
		public virtual string BatchNbr { get; set; }
		#endregion
		#region OrigModule
		public abstract class origModule : PX.Data.BQL.BqlString.Field<origModule> { }
		[PXDBString(2, IsFixed = true, IsKey = true, BqlField = typeof(CABatchDetail.origModule))]
		[PXUIField(DisplayName = "Module", Enabled = false)]
		public virtual string OrigModule { get; set; }
		#endregion
		#region OrigDocType
		public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType> { }
		[PXDBString(3, IsFixed = true, IsKey = true, BqlField = typeof(CABatchDetail.origDocType))]
		[PXUIField(DisplayName = "Doc. Type")]
		public virtual string OrigDocType { get; set; }
		#endregion
		#region OrigRefNbr
		public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(CABatchDetail.origRefNbr))]
		[PXUIField(DisplayName = "Reference Nbr.")]
		public virtual string OrigRefNbr { get; set; }
		#endregion
	}
}
