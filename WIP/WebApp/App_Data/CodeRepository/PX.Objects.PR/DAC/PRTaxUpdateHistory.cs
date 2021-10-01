using PX.Data;
using System;

namespace PX.Objects.PR
{
	[Serializable]
	[PXHidden]
	public partial class PRTaxUpdateHistory : IBqlTable
	{
		#region LastUpdateTime
		public abstract class lastUpdateTime : PX.Data.BQL.BqlDateTime.Field<lastUpdateTime> { }
		[PXDBDateAndTime]
		[PXDefault]
		public virtual DateTime? LastUpdateTime { get; set; }
		#endregion
		#region LastCheckTime
		public abstract class lastCheckTime : PX.Data.BQL.BqlDateTime.Field<lastCheckTime> { }
		[PXDBDateAndTime]
		public virtual DateTime? LastCheckTime { get; set; }
		#endregion
		#region ServerTaxDefinitionTimestamp
		public abstract class serverTaxDefinitionTimestamp : PX.Data.BQL.BqlDateTime.Field<serverTaxDefinitionTimestamp> { }
		[PXDBDateAndTime]
		public virtual DateTime? ServerTaxDefinitionTimestamp { get; set; }
		#endregion
	}
}