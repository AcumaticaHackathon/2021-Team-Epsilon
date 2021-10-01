using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using State = SOOrderStatus;
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public class SOOrderEntry_ApprovalWorkflow : PXGraphExtension<SOOrderEntry_Workflow, SOOrderEntry>
	{
		private class SOSetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<SOSetupApproval>(nameof(SOSetupApproval), typeof(SOSetup)).OrderRequestApproval;

			private bool OrderRequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord soSetup = PXDatabase.SelectSingle<SOSetup>(new PXDataField<SOSetup.orderRequestApproval>()))
				{
					if (soSetup != null)
						OrderRequestApproval = (bool)soSetup.GetBoolean(0);
				}
			}
		}

		protected static bool ApprovalIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() && SOSetupApproval.IsActive;

		protected virtual bool IsAnyApprovalActive() => IsSOApprovalActive() || IsQTApprovalActive() || IsRMApprovalActive() || IsINApprovalActive() || IsCMApprovalActive();
		protected virtual bool IsSOApprovalActive() => ApprovalIsActive() && true;
		protected virtual bool IsQTApprovalActive() => ApprovalIsActive() && true;
		protected virtual bool IsRMApprovalActive() => ApprovalIsActive() && true;
		protected virtual bool IsINApprovalActive() => ApprovalIsActive() && true;
		protected virtual bool IsCMApprovalActive() => ApprovalIsActive() && true;

		[PXWorkflowDependsOnType(typeof(SOSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			if (IsAnyApprovalActive())
				Configure(config.GetScreenConfigurationContext<SOOrderEntry, SOOrder>());
			else
				HideApproveAndRejectActions(config.GetScreenConfigurationContext<SOOrderEntry, SOOrder>());
		}

		public class Conditions : Condition.Pack
		{
			public Condition IsApproved => GetOrCreate(b => b.FromBql<
				approved.IsEqual<True>
			>());

			public Condition IsRejected => GetOrCreate(b => b.FromBql<
				rejected.IsEqual<True>
			>());
		}

		protected virtual void Configure(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();
			var soConditions = context.Conditions.GetPack<WorkflowSO.Conditions>();

			var approve = context.ActionDefinitions
				.CreateExisting<SOOrderEntry_ApprovalWorkflow>(g => g.approve, a => a
				.InFolder(FolderType.ActionsFolder)
				.WithCategory(ScreenConfiguration.ActionCategory.Approval)
				.PlaceAfter(g => g.putOnHold)
				.WithFieldAssignments(fa => fa.Add<approved>(true)));
			var reject = context.ActionDefinitions
				.CreateExisting<SOOrderEntry_ApprovalWorkflow>(g => g.reject, a => a
				.InFolder(FolderType.ActionsFolder)
				.WithCategory(ScreenConfiguration.ActionCategory.Approval)
				.PlaceAfter(approve)
				.WithFieldAssignments(fa => fa.Add<rejected>(true)));

			Workflow.ConfiguratorFlow InjectApprovalWorkflow(Workflow.ConfiguratorFlow flow, string behavior)
			{
				bool includeCreditHold = behavior.IsIn(SOBehavior.SO, SOBehavior.IN, SOBehavior.RM);
				bool inclCustOpenOrders = behavior.IsIn(SOBehavior.SO, SOBehavior.IN, SOBehavior.RM, SOBehavior.CM);

				const string initialState = "_";

				return flow
					.WithFlowStates(states =>
					{
						states.Add<State.pendingApproval>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add(approve, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(reject, a => a.IsDuplicatedInToolbar());
								});
						});
						states.Add<State.voided>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.copyOrder);
								});
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.UpdateGroupFrom(initialState, ts =>
						{
							ts.Add(t => t // New Pending Approval
								.To<State.pendingApproval>()
								.IsTriggeredOn(g => g.initializeState)
								.When(!conditions.IsApproved)
								.PlaceAfter(tr => tr.To<State.hold>()
								.WithFieldAssignments(fas =>
								{
									if (inclCustOpenOrders)
										fas.Add<inclCustOpenOrders>(false);
								})));
						});

						transitions.UpdateGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.pendingApproval>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.When(!conditions.IsApproved)
								.PlaceBefore(tr => behavior == SOBehavior.SO
									? tr.To<State.pendingProcessing>()
									: tr.To<State.open>()));

							if (behavior == SOBehavior.QT)
							{
								ts.Add(t => t
									.To<State.pendingApproval>()
									.IsTriggeredOn(g => g.openOrder)
									.When(!conditions.IsApproved)
									.PlaceBefore(tr => tr.To<State.open>()));
							}
						});

						transitions.UpdateGroupFrom<State.cancelled>(ts =>
						{
							ts.Update(
								t => t.To<State.open>().IsTriggeredOn(g => g.reopenOrder),
								t => t.When(conditions.IsApproved)); // null -> IsApproved

							ts.Add(t => t
								.To<State.pendingApproval>()
								.IsTriggeredOn(g => g.reopenOrder)
								.When(!conditions.IsApproved));
						});

						if (includeCreditHold)
						{
							transitions.UpdateGroupFrom<State.creditHold>(ts =>
							{
								ts.Add(t => t
									.To<State.pendingApproval>()
									.IsTriggeredOn(g => g.releaseFromCreditHold)
									.When(!conditions.IsApproved)
									.PlaceBefore(tr => tr.To<State.open>()
									.WithFieldAssignments(fas =>
									{
										fas.Add<creditHold>(false);
									})));
								ts.Add(t => t
									.To<State.pendingApproval>()
									.IsTriggeredOn(g => g.OnCreditLimitSatisfied)
									.When(!conditions.IsApproved)
									.PlaceBefore(tr => tr.To<State.open>()
									.WithFieldAssignments(fas =>
									{
										fas.Add<creditHold>(false);
									})));
							});
						}

						transitions.AddGroupFrom<State.pendingApproval>(ts =>
						{
							if (behavior == SOBehavior.SO)
							{
								ts.Add(t => t
									.To<State.pendingProcessing>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved && soConditions.HasPaymentsInPendingProcessing));
								ts.Add(t => t
									.To<State.awaitingPayment>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved && soConditions.IsPaymentRequirementsViolated));
							}

							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(approve)
								.When(conditions.IsApproved)
								.WithFieldAssignments(fas =>
								{
									if (inclCustOpenOrders)
										fas.Add<inclCustOpenOrders>(true);
								}));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(reject)
								.When(conditions.IsRejected));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold));
						});

						transitions.AddGroupFrom<State.voided>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold));
						});
					});
			}

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.WithFlows(flows =>
					{
						if (IsSOApprovalActive()) flows.Update<SOBehavior.sO>(f => InjectApprovalWorkflow(f, SOBehavior.SO));
						if (IsQTApprovalActive()) flows.Update<SOBehavior.qT>(f => InjectApprovalWorkflow(f, SOBehavior.QT));
						if (IsRMApprovalActive()) flows.Update<SOBehavior.rM>(f => InjectApprovalWorkflow(f, SOBehavior.RM));
						if (IsINApprovalActive()) flows.Update<SOBehavior.iN>(f => InjectApprovalWorkflow(f, SOBehavior.IN));
						if (IsCMApprovalActive()) flows.Update<SOBehavior.cM>(f => InjectApprovalWorkflow(f, SOBehavior.CM));
					})
					.WithActions(actions =>
					{
						actions.Add(approve);
						actions.Add(reject);
						actions.Update(
							g => g.putOnHold,
							a => a.WithFieldAssignments(fas =>
							{
								fas.Add<approved>(false);
								fas.Add<rejected>(false);
							}));
						actions.Update(
							g => g.openOrder,
							a => a.WithFieldAssignments(fas =>
							{
								fas.Add<hold>(false);
							}));
					});
			});
		}

		protected virtual void HideApproveAndRejectActions(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var approveHidden = context.ActionDefinitions
				.CreateExisting<SOOrderEntry_ApprovalWorkflow>(g => g.approve, a => a
				.InFolder(FolderType.ActionsFolder)
				.IsHiddenAlways());
			var rejectHidden = context.ActionDefinitions
				.CreateExisting<SOOrderEntry_ApprovalWorkflow>(g => g.reject, a => a
				.InFolder(FolderType.ActionsFolder)
				.IsHiddenAlways());

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Add(approveHidden);
						actions.Add(rejectHidden);
					});
			});
		}

		public PXAction<SOOrder> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<SOOrder> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}