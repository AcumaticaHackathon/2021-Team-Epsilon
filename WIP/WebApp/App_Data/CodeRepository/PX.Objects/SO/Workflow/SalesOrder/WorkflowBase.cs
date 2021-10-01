using PX.Data;
using PX.Data.BQL;
using PX.Data.WorkflowAPI;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public abstract class WorkflowBase : PXGraphExtension<ScreenConfiguration, SOOrderEntry>
	{
		public class Conditions : Condition.Pack
		{
			public Condition IsOnHold => GetOrCreate(b => b.FromBql<
				hold.IsEqual<True>
			>());

			public Condition IsCompleted => GetOrCreate(b => b.FromBql<
				completed.IsEqual<True>
			>());

			public Condition IsOnCreditHold => GetOrCreate(b => b.FromBql<
				creditHold.IsEqual<True>
			>());
		}

		public virtual void DisableWholeScreen(FieldState.IContainerFillerFields states)
		{
			states.AddTable<SOOrder>(state => state.IsDisabled());
			states.AddTable<SOLine>(state => state.IsDisabled());
			states.AddTable<SOTaxTran>(state => state.IsDisabled());
			states.AddTable<SOBillingAddress>(state => state.IsDisabled());
			states.AddTable<SOBillingContact>(state => state.IsDisabled());
			states.AddTable<SOShippingAddress>(state => state.IsDisabled());
			states.AddTable<SOShippingContact>(state => state.IsDisabled());
			states.AddTable<SOLineSplit>(state => state.IsDisabled());
		}

		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<SOOrderEntry, SOOrder>());
		protected abstract void Configure(WorkflowContext<SOOrderEntry, SOOrder> context);
	}
}