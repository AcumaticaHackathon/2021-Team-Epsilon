using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using State = SOOrderStatus;
	using DropshipReturn = GraphExtensions.SOOrderEntryExt.DropshipReturn;
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public class WorkflowRM : WorkflowBase
	{
		protected override void Configure(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var conditions = context.Conditions.GetPack<WorkflowSO.Conditions>();
			context.UpdateScreenConfigurationFor(screen => screen.WithFlows(flows =>
			{
				flows.Add<SOBehavior.rM>(flow => flow
					.WithFlowStates(flowStates =>
					{
						flowStates.Add(State.Initial, fs => fs.IsInitial(g => g.initializeState));
						flowStates.Add<State.hold>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.recalculateDiscountsAction);
								});
						});
						flowStates.Add<State.creditHold>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.releaseFromCreditHold, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.emailSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnCreditLimitSatisfied);
								});
						});
						flowStates.Add<State.open>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.createShipmentReceipt, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.createShipmentIssue, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.completeOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.recalculateDiscountsAction);
									actions.Add(g => g.prepareInvoice);
									actions.Add(g => g.createPurchaseOrder);
									actions.Add<DropshipReturn>(e => e.createVendorReturn);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnShipmentConfirmed);
									handlers.Add(g => g.OnCreditLimitViolated);
								});
						});
						flowStates.Add<State.completed>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.prepareInvoice, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.reopenOrder);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnShipmentCorrected);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.cancelled>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.reopenOrder);
								})
								.WithFieldStates(DisableWholeScreen);
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(State.Initial, ts =>
						{
							ts.Add(t => t
								.To<State.hold>() // RM New Hold
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<inclCustOpenOrders>(false);
								}));
							ts.Add(t => t
								.To<State.creditHold>() // RM New Credit Hold
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnCreditHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<hold>(e => e.SetFromValue(false));
									fas.Add<inclCustOpenOrders>(e => e.SetFromValue(false));
								}));
							ts.Add(t => t
								.To<State.open>() // RM New Open
								.IsTriggeredOn(g => g.initializeState)
								.WithFieldAssignments(fas =>
								{
									fas.Add<inclCustOpenOrders>(true);
								}));
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<inclCustOpenOrders>(true);
								}));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
						});
						transitions.AddGroupFrom<State.creditHold>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<creditHold>(e => e.SetFromValue(false));
								}));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.releaseFromCreditHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<creditHold>(e => e.SetFromValue(false));
									fas.Add<inclCustOpenOrders>(e => e.SetFromValue(true));
								}));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnCreditLimitSatisfied)
								.WithFieldAssignments(fas =>
								{
									fas.Add<creditHold>(e => e.SetFromValue(false));
									fas.Add<inclCustOpenOrders>(e => e.SetFromValue(true));
								}));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<inclCustOpenOrders>(false);
								}));
							ts.Add(t => t
								.To<State.completed>() // RM Shipping Completed / RM New Completed
								.IsTriggeredOn(g => g.OnShipmentConfirmed)
								.When(conditions.IsShippingCompleted)
								.WithFieldAssignments(fas =>
								{
									fas.Add<completed>(true);
								}));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.OnCreditLimitViolated)
								.WithFieldAssignments(fas =>
								{
									fas.Add<creditHold>(e => e.SetFromValue(true));
									fas.Add<hold>(e => e.SetFromValue(false));
									fas.Add<inclCustOpenOrders>(e => e.SetFromValue(false));
								}));
						});
						transitions.AddGroupFrom<State.cancelled>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reopenOrder)
								.WithFieldAssignments(fas => fas.Add<inclCustOpenOrders>(true)));
						});
						transitions.AddGroupFrom<State.completed>(ts =>
						{
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.OnShipmentCorrected));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reopenOrder)
								.WithFieldAssignments(fas => fas.Add<inclCustOpenOrders>(true)));
						});
					}));
			}));
		}
	}
}