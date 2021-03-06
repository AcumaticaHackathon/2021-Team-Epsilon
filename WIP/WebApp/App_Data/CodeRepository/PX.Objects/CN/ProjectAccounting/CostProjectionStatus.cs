using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CN.ProjectAccounting
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public static class CostProjectionStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { OnHold, PendingApproval, Open, Released, Rejected },
				new string[] { PX.Objects.PM.Messages.OnHold, PX.Objects.PM.Messages.PendingApproval, PX.Objects.PM.Messages.Open, PX.Objects.PM.Messages.Released, PX.Objects.PM.Messages.Rejected })
			{; }
		}
		public const string OnHold = "H";
		public const string PendingApproval = "A";
		public const string Open = "O";
		public const string Released = "C";
		public const string Rejected = "R";

		public class onHold : PX.Data.BQL.BqlString.Constant<onHold>
		{
			public onHold() : base(OnHold) {; }
		}

		public class pendingApproval : PX.Data.BQL.BqlString.Constant<pendingApproval>
		{
			public pendingApproval() : base(PendingApproval) {; }
		}

		public class open : PX.Data.BQL.BqlString.Constant<open>
		{
			public open() : base(Open) {; }
		}

		public class rejected : PX.Data.BQL.BqlString.Constant<rejected>
		{
			public rejected() : base(Rejected) {; }
		}

		public class released : PX.Data.BQL.BqlString.Constant<released>
		{
			public released() : base(Released) {; }
		}
	}
}
