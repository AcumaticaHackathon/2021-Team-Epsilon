using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;
using PX.Objects.CR.Workflows;

namespace PX.Objects.AR.Workflows
{

	public class CustomerLocationMaint_Workflow : PXGraphExtension<CustomerLocationMaint>
	{
		public static bool IsActive() => false;


		public override void Configure(PXScreenConfiguration configuration)
		{
			LocationWorkflow.Configure(configuration);
		}
	}

	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class CustomerLocationMaint_Workflow_CbApi_Adapter : PXGraphExtension<CustomerLocationMaint>
	{
		public override void Initialize()
		{
			base.Initialize();
			if (!Base.IsContractBasedAPI)
				return;

			Base.RowUpdated.AddHandler<Location>(RowUpdated);

			void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
			{
				if (e.Row is Location row
					&& e.OldRow is Location oldRow
					&& row.IsActive is bool newActive
					&& oldRow.IsActive is bool oldActive
					&& newActive != oldActive)
				{
					// change it only by transition
					row.IsActive = oldActive;
					
					Base.RowUpdated.RemoveHandler<Location>(RowUpdated);

					Base.OnAfterPersist += InvokeTransition;
					void InvokeTransition(PXGraph obj)
					{
						obj.OnAfterPersist -= InvokeTransition;
						(newActive ? Base.Activate : Base.Deactivate).PressImpl(internalCall: true);
					}
				}
			}
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Enabled), true)]
		public virtual void _(Events.CacheAttached<Location.isActive> e) { }
	}
}
