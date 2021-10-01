using PX.Data;

namespace PX.Objects.PR.Standalone
{
	public class ProjectCostAssignmentType
	{
		public class noCostAssigned : PX.Data.BQL.BqlString.Constant<noCostAssigned>
		{
			public noCostAssigned() : base(NoCostAssigned) { }
		}

		public class wageCostAssigned : PX.Data.BQL.BqlString.Constant<wageCostAssigned>
		{
			public wageCostAssigned() : base(WageCostAssigned) { }
		}

		public class wageLaborBurdenAssigned : PX.Data.BQL.BqlString.Constant<wageLaborBurdenAssigned>
		{
			public wageLaborBurdenAssigned() : base(WageLaborBurdenAssigned) { }
		}

		public const string NoCostAssigned = "NCA";
		public const string WageCostAssigned = "WCA";
		public const string WageLaborBurdenAssigned = "WLB";
	}
}
