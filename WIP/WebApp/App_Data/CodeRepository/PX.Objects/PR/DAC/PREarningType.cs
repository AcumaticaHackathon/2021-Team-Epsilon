using PX.Data;
using PX.Objects.EP;
using System;

namespace PX.Objects.PR.Standalone
{
	[PXCacheName("Payroll Earning Type")]
	[Serializable]
	public partial class PREarningType : IBqlTable
	{
		#region TypeCD
		public abstract class typeCD : PX.Data.BQL.BqlString.Field<typeCD> { }
		[PXDefault]
		[PXDBString(EPEarningType.typeCD.Length, IsUnicode = false, IsKey = true, InputMask = EPEarningType.typeCD.InputMask)]
		[PXUIField(DisplayName = "Code", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string TypeCD { get; set; }
		#endregion
	}
}
