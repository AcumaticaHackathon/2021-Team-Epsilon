using System;
using System.Collections;

using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.RQ
{
	using State = RQRequisitionStatus;
	using Self = RQRequisitionEntry_ApprovalWorkflow;
	using Context = WorkflowContext<RQRequisitionEntry, RQRequisition>;
	using static RQRequisition;
	using static BoundedTo<RQRequisitionEntry, RQRequisition>;

	public class RQRequisitionEntry_ApprovalWorkflow : PXGraphExtension<RQRequisitionEntry_Workflow, RQRequisitionEntry>
	{
		private class RQRequisitionApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<RQRequisitionApproval>(nameof(RQRequisitionApproval), typeof(RQSetup)).RequireApproval;

			private bool RequireApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord rqSetup = PXDatabase.SelectSingle<RQSetup>(new PXDataField<RQSetup.requisitionApproval>()))
				{
					if (rqSetup != null)
						RequireApproval = rqSetup.GetBoolean(0) ?? false;
				}
			}
		}

		[PXWorkflowDependsOnType(typeof(RQSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			if (RQRequisitionApproval.IsActive)
				Configure(config.GetScreenConfigurationContext<RQRequisitionEntry, RQRequisition>());
			else
				HideApprovalActions(config.GetScreenConfigurationContext<RQRequisitionEntry, RQRequisition>());
		}

		protected virtual void Configure(Context context)
		{
			#region Conditions
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			Condition Existing(string name) => (Condition)context.Conditions.Get(name);
			var conditions = new
			{
				IsApproved
					= Bql<approved.IsEqual<True>>(),

				IsRejected
					= Bql<rejected.IsEqual<True>>(),

				IsQuoted
					= Existing("IsQuoted"),

				IsBiddingCompleted
					= Existing("IsBiddingCompleted"),
			}.AutoNameConditions();
			#endregion

			(var approve, var reject) = GetApprovalActions(context, hidden: false);

			const string initialState = "_";

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.UpdateDefaultFlow(flow =>
						flow
						.WithFlowStates(states =>
						{
							states.Add<State.pendingApproval>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
										actions.Add(approve, a => a.IsDuplicatedInToolbar());
										actions.Add(reject, a => a.IsDuplicatedInToolbar());
									});
							});
							states.Add<State.rejected>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.UpdateGroupFrom(initialState, ts =>
							{
								ts.Add(t => t
									.To<State.pendingApproval>()
									.IsTriggeredOn(g => g.initializeState)
									.When(!conditions.IsApproved)
									.PlaceAfter(rt => rt.From(initialState).To<State.hold>().IsTriggeredOn(g => g.initializeState)));
							});
							transitions.UpdateGroupFrom<State.hold>(ts =>
							{
								ts.Add(t => t
									.To<State.pendingApproval>()
									.IsTriggeredOn(g => g.releaseFromHold)
									.When(!conditions.IsApproved)
									.PlaceBefore(rt => rt.From<State.hold>().To<State.open>().IsTriggeredOn(g => g.releaseFromHold)));
							});
							transitions.AddGroupFrom<State.pendingApproval>(ts =>
							{
								ts.Add(t => t
									.To<State.open>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved && conditions.IsQuoted));
								ts.Add(t => t
									.To<State.pendingQuotation>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved && conditions.IsBiddingCompleted));
								ts.Add(t => t
									.To<State.bidding>()
									.IsTriggeredOn(approve)
									.When(conditions.IsApproved));
								ts.Add(t => t
									.To<State.rejected>()
									.IsTriggeredOn(reject)
									.When(conditions.IsRejected));
								ts.Add(t => t
									.To<State.hold>()
									.IsTriggeredOn(g => g.putOnHold));
							});
							transitions.AddGroupFrom<State.rejected>(ts =>
							{
								ts.Add(t => t
									.To<State.hold>()
									.IsTriggeredOn(g => g.putOnHold));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(approve);
						actions.Add(reject);
						actions.Update(
							g => g.putOnHold,
							a => a.WithFieldAssignments(fas =>
							{
								fas.Add<approved>(f => f.SetFromValue(false));
								fas.Add<rejected>(f => f.SetFromValue(false));
							}));
					});
			});
		}

		protected virtual void HideApprovalActions(Context context)
		{
			(var approve, var reject) = GetApprovalActions(context, hidden: true);

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Add(approve);
						actions.Add(reject);
					});
			});
		}

		protected virtual (ActionDefinition.IConfigured approve, ActionDefinition.IConfigured reject) GetApprovalActions(Context context, bool hidden)
		{
			var approve = context.ActionDefinitions
				.CreateExisting<Self>(g => g.approve, a => a
				.InFolder(FolderType.ActionsFolder)
				.PlaceAfter(g => g.putOnHold)
				.With(it => hidden ? it.IsHiddenAlways() : it)
				.WithFieldAssignments(fa => fa.Add<approved>(e => e.SetFromValue(true))));
			var reject = context.ActionDefinitions
				.CreateExisting<Self>(g => g.reject, a => a
				.InFolder(FolderType.ActionsFolder)
				.PlaceAfter(approve)
				.With(it => hidden ? it.IsHiddenAlways() : it)
				.WithFieldAssignments(fa => fa.Add<rejected>(e => e.SetFromValue(true))));
			return (approve, reject);
		}

		public PXAction<RQRequisition> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<RQRequisition> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}