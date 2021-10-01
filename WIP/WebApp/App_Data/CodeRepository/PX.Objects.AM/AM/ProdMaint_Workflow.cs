using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.AM
{
	using State = ProductionOrderStatus;
	using static AMProdItem;
	using static BoundedTo<ProdMaint, AMProdItem>;

	public class ProdMaint_Workflow : PXGraphExtension<ProdMaint>
	{
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<ProdMaint, AMProdItem>());

		protected virtual void Configure(WorkflowContext<ProdMaint, AMProdItem> context)
		{


			#region Conditions
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsCompletedOrCanceled
					= Bql<statusID.IsEqual<State.closed>.Or<statusID.IsEqual<State.cancel>>>()
			}.AutoNameConditions();
			#endregion

			//const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions => {
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.printProdTicket, c => c.InFolder(FolderType.ReportsFolder).IsDisabledWhen(conditions.IsCompletedOrCanceled));
					});
			});
		}
	}
}
