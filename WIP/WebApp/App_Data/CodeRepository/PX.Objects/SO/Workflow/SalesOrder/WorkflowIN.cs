using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using State = SOOrderStatus;
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public class WorkflowIN : WorkflowBase
	{
		public new class Conditions : WorkflowBase.Conditions
		{
			public Condition HasAllBillsReleased => GetOrCreate(b => b.FromBql<
				billedCntr.IsEqual<Zero>.
				And<releasedCntr.IsGreater<Zero>>
			>());

			public Condition IsUnbilled => GetOrCreate(b => b.FromBql<
				billedCntr.IsEqual<Zero>.
				And<releasedCntr.IsEqual<Zero>>
			>());
		}

		protected override void Configure(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();
			context.UpdateScreenConfigurationFor(screen => screen.WithFlows(flows =>
			{
				flows.Add<SOBehavior.iN>(flow => flow
					.WithFlowStates(flowStates =>
					{
						flowStates.Add(State.Initial, fs => fs.IsInitial(g => g.initializeState));
						flowStates.Add<State.hold>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.cancelOrder);
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
									actions.Add(g => g.releaseFromCreditHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.putOnHold);
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
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add<SOOrderEntry.SOQuickProcess>(g => g.quickProcess);
									actions.Add(g => g.prepareInvoice, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.recalculateDiscountsAction);
									actions.Add(g => g.emailSalesOrder);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnInvoiceLinked);
									handlers.Add(g => g.OnCreditLimitViolated);
								});
						});
						flowStates.Add<State.invoiced>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.emailSalesOrder);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnInvoiceReleased);
									handlers.Add(g => g.OnInvoiceUnlinked);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.completed>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.emailSalesOrder);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.cancelled>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.reopenOrder, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.copyOrder, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.validateAddresses);
								})
								.WithFieldStates(DisableWholeScreen);
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(State.Initial, ts =>
						{
							ts.Add(t => t
								.To<State.hold>() // IN New Hold
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<inclCustOpenOrders>(false);
								}));
							ts.Add(t => t // IN New Credit Hold
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnCreditHold));
							ts.Add(t => t
								.To<State.open>() // IN New Open
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
								.To<State.open>()
								.IsTriggeredOn(g => g.releaseFromCreditHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<creditHold>(false);
									fas.Add<inclCustOpenOrders>(true);
								}));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnCreditLimitSatisfied)
								.WithFieldAssignments(fas =>
								{
									fas.Add<creditHold>(false);
									fas.Add<inclCustOpenOrders>(true);
								}));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.WithFieldAssignments(fas =>
								{
									fas.Add<creditHold>(false);
									fas.Add<inclCustOpenOrders>(false);
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
							ts.Add(t => t.To<State.invoiced>().IsTriggeredOn(g => g.OnInvoiceLinked));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.OnCreditLimitViolated)
								.WithFieldAssignments(fas =>
								{
									fas.Add<creditHold>(true);
									fas.Add<hold>(false);
									fas.Add<inclCustOpenOrders>(false);
								}));
						});
						transitions.AddGroupFrom<State.invoiced>(ts =>
						{
							ts.Add(t => t
								.To<State.completed>() // IN New Completed
								.IsTriggeredOn(g => g.OnInvoiceReleased)
								.When(conditions.HasAllBillsReleased)
								.WithFieldAssignments(fas =>
								{
									fas.Add<completed>(true);
								}));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnInvoiceUnlinked)
								.When(conditions.IsUnbilled)); // IN New Unbilled
						});
						transitions.AddGroupFrom<State.cancelled>(ts =>
						{
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