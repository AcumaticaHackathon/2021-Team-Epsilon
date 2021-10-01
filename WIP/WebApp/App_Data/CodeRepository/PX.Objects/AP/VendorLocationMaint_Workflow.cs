using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR.Workflows;

namespace PX.Objects.AP.Workflows
{
	public class VendorLocationMaint_Workflow : PXGraphExtension<VendorLocationMaint>
	{
		public static bool IsActive() => false;

		public override void Configure(PXScreenConfiguration configuration)
		{
			LocationWorkflow.Configure(configuration);
		}
	}
}
