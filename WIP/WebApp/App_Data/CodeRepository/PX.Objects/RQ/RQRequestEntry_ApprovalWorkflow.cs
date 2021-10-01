using System;
using System.Collections;

using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.RQ
{
	using State = RQRequestStatus;
	using Self = RQRequestEntry_ApprovalWorkflow;
	using Context = WorkflowContext<RQRequestEntry, RQRequest>;
	using static RQRequest;
	using static BoundedTo<RQRequestEntry, RQRequest>;

	public class RQRequestEntry_ApprovalWorkflow : PXGraphExtension<RQRequestEntry_Workflow, RQRequestEntry>
	{
		private class RQRequestApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<RQRequestApproval>(nameof(RQRequestApproval), typeof(RQSetup)).RequireApproval;

			private bool RequireApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord rqSetup = PXDatabase.SelectSingle<RQSetup>(new PXDataField<RQSetup.requestApproval>()))
				{
					if (rqSetup != null)
						RequireApproval = rqSetup.GetBoolean(0) ?? false;
				}
			}
		}

		[PXWorkflowDependsOnType(typeof(RQSetup))]
		public override void Configure(PXScreenConfiguration config)
		{
			if (RQRequestApproval.IsActive)
				Configure(config.GetScreenConfigurationContext<RQRequestEntry, RQRequest>());
			else
				HideApprovalActions(config.GetScreenConfigurationContext<RQRequestEntry, RQRequest>());
		}

		protected virtual void Configure(Context context)
		{
			#region Conditions
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsNotApproved
					= Bql<approved.IsEqual<False>>(),

				IsApproved
					= Bql<approved.IsEqual<True>>(),

				IsRejected
					= Bql<rejected.IsEqual<True>>(),
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
									.When(conditions.IsNotApproved)
									.PlaceAfter(rt => rt.From(initialState).To<State.hold>().IsTriggeredOn(g => g.initializeState)));
							});
							transitions.UpdateGroupFrom<State.hold>(ts =>
							{
								ts.Add(t => t
									.To<State.pendingApproval>()
									.IsTriggeredOn(g => g.releaseFromHold)
									.When(conditions.IsNotApproved)
									.PlaceBefore(rt => rt.From<State.hold>().To<State.open>().IsTriggeredOn(g => g.releaseFromHold)));
							});
							transitions.AddGroupFrom<State.pendingApproval>(ts =>
							{
								ts.Add(t => t
									.To<State.open>()
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

		public PXAction<RQRequest> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<RQRequest> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}