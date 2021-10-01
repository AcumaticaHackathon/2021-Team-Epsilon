using PX.Data;

namespace PX.Objects.PO
{
	public class ReturnCostMode
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				new[]
				{
					Pair(OriginalCost, Messages.OriginalReceiptCost),
					Pair(CostByIssue, Messages.CostByIssueStategy),
					Pair(ManualCost, Messages.ManualCost)
				})
			{ }
		}

		public const string NotApplicable = "N";
		public const string OriginalCost = "O";
		public const string CostByIssue = "I";
		public const string ManualCost = "M";

		public class notApplicable : PX.Data.BQL.BqlString.Constant<notApplicable>
		{
			public notApplicable() : base(NotApplicable) {; }
		}

		public class originalCost : PX.Data.BQL.BqlString.Constant<originalCost>
		{
			public originalCost() : base(OriginalCost) {; }
		}

		public class costByIssue : PX.Data.BQL.BqlString.Constant<costByIssue>
		{
			public costByIssue() : base(CostByIssue) { }
		}

		public class manualCost : PX.Data.BQL.BqlString.Constant<manualCost>
		{
			public manualCost() : base(ManualCost) { }
		}
	}
}
