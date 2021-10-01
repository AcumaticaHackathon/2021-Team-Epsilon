using PX.Data.WorkflowAPI;

namespace PX.Objects.IN
{
	using State = INDocStatus;

	public class INReceiptEntry_Workflow : INRegisterEntryBase_Workflow<INReceiptEntry, INDocType.receipt>
	{
		protected override void Configure(WorkflowContext<INReceiptEntry, INRegister> context)
		{
			base.Configure(context);
			context.UpdateScreenConfigurationFor(screen =>
				screen
				.WithActions(actions =>
					actions.Add(g => g.iNItemLabels, a => a.InFolder(FolderType.ReportsFolder)))
				.UpdateDefaultFlow(flow =>
					flow.WithFlowStates(flowStates =>
						flowStates.Update<State.released>(flowState =>
							flowState.WithActions(actions =>
								actions.Add(g => g.iNItemLabels))))));
		}
	}
}