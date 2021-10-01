using PX.Data;
using PX.Objects.SO.Workflow.SalesOrder;

namespace PX.Objects.SO
{
	// just for backward compitibility
	public class SOOrderEntry_Workflow : PXGraphExtension<
		WorkflowCM,
		WorkflowIN,
		WorkflowRM,
		WorkflowQT,
		WorkflowSO,
		ScreenConfiguration,
		SOOrderEntry
	> { }
}