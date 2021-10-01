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
	using State = VendorShipmentStatus;
	using static AMVendorShipment;
	using static BoundedTo<VendorShipmentEntry, AMVendorShipment>;

	public class VendorShipmentEntry_Workflow : PXGraphExtension<VendorShipmentEntry>
	{
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<VendorShipmentEntry, AMVendorShipment>());

		protected virtual void Configure(WorkflowContext<VendorShipmentEntry, AMVendorShipment> context)
		{


			#region Conditions
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>>(),
				IsOpen
					= Bql<hold.IsEqual<False>>(),
				IsCompleted
					= Bql<status.IsEqual<State.completed>>(),
				IsCancelled
					= Bql<status.IsEqual<State.cancelled>>(),
				IsCompletedOrCanceled
					= Bql<status.IsEqual<State.completed>.Or<status.IsEqual<State.cancelled>>>()
			}.AutoNameConditions();
			#endregion

			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
							fss.Add<State.hold>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.removeHold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printPackingList);
										actions.Add(g => g.printPickList);
									});
							});
							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.confirm, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.hold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.cancelShip, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printPackingList);
										actions.Add(g => g.printPickList);
									});
							});
							fss.Add<State.completed>(flowState =>
							{
								return flowState
									.WithActions(actions =>	{});
							});
							fss.Add<State.cancelled>(flowState =>
							{
								return flowState
									.WithActions(actions => { });
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom(initialState, ts =>
							{
								ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold));
							});
							transitions.AddGroupFrom<State.hold>(ts =>
							{
								ts.Add(t => t
									.To<State.open>()
									.IsTriggeredOn(g => g.removeHold)
									.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							});
							transitions.AddGroupFrom<State.open>(ts =>
							{
								ts.Add(t => t
									.To<State.completed>()
									.IsTriggeredOn(g => g.confirm));
								ts.Add(t => t
									.To<State.hold>()
									.IsTriggeredOn(g => g.hold)
									.WithFieldAssignments(fas =>fas.Add<hold>(f => f.SetFromValue(true))));
								ts.Add(t => t
									.To<State.cancelled>()
									.IsTriggeredOn(g => g.cancelShip));
							});
						})
					)
					.WithActions(actions => {
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.hold, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.removeHold, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.cancelShip, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.confirm, c => c.InFolder(FolderType.ActionsFolder));						
						actions.Add(g => g.printPackingList, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printPickList, c => c.InFolder(FolderType.ReportsFolder));
					});
			});
		}
	}
}
