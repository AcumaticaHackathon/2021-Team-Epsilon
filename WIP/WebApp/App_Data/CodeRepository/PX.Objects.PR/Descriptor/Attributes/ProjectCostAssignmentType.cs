using PX.Data;

namespace PX.Objects.PR
{
	public class ProjectCostAssignmentType : PX.Objects.PR.Standalone.ProjectCostAssignmentType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				new string[] { NoCostAssigned, WageCostAssigned, WageLaborBurdenAssigned },
				new string[] { Messages.NoCostAssigned, Messages.WageCostAssigned, Messages.WageLaborBurdenAssigned })
			{ }
		}

		#region Avoid 2020R1 breaking changes
		public new class noCostAssigned : Standalone.ProjectCostAssignmentType.noCostAssigned { }
		public new class wageCostAssigned : Standalone.ProjectCostAssignmentType.wageCostAssigned { }
		public new class wageLaborBurdenAssigned : Standalone.ProjectCostAssignmentType.wageLaborBurdenAssigned { }

		public new const string NoCostAssigned = Standalone.ProjectCostAssignmentType.NoCostAssigned;
		public new const string WageCostAssigned = Standalone.ProjectCostAssignmentType.WageCostAssigned;
		public new const string WageLaborBurdenAssigned = Standalone.ProjectCostAssignmentType.WageLaborBurdenAssigned; 
		#endregion
	}
}
