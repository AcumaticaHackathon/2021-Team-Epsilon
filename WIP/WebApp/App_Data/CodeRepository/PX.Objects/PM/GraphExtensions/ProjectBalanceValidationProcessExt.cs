using PX.Data;

namespace PX.Objects.PM
{
    // Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
    public class ProjectBalanceValidationProcessExt : CommitmentTracking<ProjectBalanceValidationProcess>
    {		
		[PXOverride]
		public virtual void ProcessCommitments(PMProject project)
		{
			foreach (PMCommitment item in Base.ExternalCommitments.Select(project.ContractID))
			{
				RollUpCommitmentBalance(item, 1);
			}
		}
	}
}
