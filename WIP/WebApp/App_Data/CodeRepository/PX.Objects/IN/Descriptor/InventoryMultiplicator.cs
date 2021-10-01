using System;
using PX.Data.BQL;

namespace PX.Objects.IN
{
	public static class InventoryMultiplicator
	{
		public const short NoUpdate =  0;
		public class noUpdate : BqlShort.Constant<noUpdate> { public noUpdate() : base(NoUpdate) { } }

		public const short Decrease = -1;
		public class decrease : BqlShort.Constant<decrease> { public decrease() : base(Decrease) { } }

		public const short Increase = +1;
		public class increase : BqlShort.Constant<increase> { public increase() : base(Increase) { } }
	}
}