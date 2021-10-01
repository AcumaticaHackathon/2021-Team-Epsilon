using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Api.Models;
using PX.Objects.EP;

namespace PX.Objects.CA
{
	using State = CATransferStatus;
	using static CAAdj;
	using static BoundedTo<CATranEntry, CAAdj>;
	
	public class CATranEntry_ApprovalWorkflow : PXGraphExtension<CATranEntry_Workflow, CATranEntry>
	{
		private class SetupApproval : IPrefetchable
		{
			public static bool IsActive => PXDatabase.GetSlot<SetupApproval>(nameof(SetupApproval), typeof(CASetup)).RequestApproval;

			private bool RequestApproval;

			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord CASetup = PXDatabase.SelectSingle<CASetupApproval>(new PXDataField<CASetupApproval.isActive>()))
				{
					if (CASetup != null)
						RequestApproval = (bool)CASetup.GetBoolean(0);
				}
			}
			private static SetupApproval Slot => PXDatabase
				.GetSlot<SetupApproval>(typeof(SetupApproval).FullName, typeof(CASetupApproval));
			public static bool IsRequestApproval =>
				PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>() &&
				Slot.RequestApproval;
		}

		private static bool ApprovalIsActive => PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>();


		[PXWorkflowDependsOnType(typeof(CA.CASetupApproval))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<CATranEntry, CAAdj>());
		}

		protected virtual void Configure(WorkflowContext<CATranEntry, CAAdj> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsNotOnHoldAndIsApproved
					= Bql<hold.IsEqual<False>.And<approved.IsEqual<True>>>(),

				IsNotOnHoldAndIsNotApproved
					= Bql<hold.IsEqual<False>.And<approved.IsEqual<False>>>(),
				
				IsRejected
					= Bql<rejected.IsEqual<True>>(),

				IsApproved
					= Bql<approved.IsEqual<True>>(),

				IsNotApproved
					= Bql<approved.IsEqual<False>.And<rejected.IsEqual<False>>>(),
				
				IsApprovalDisabled
					= SetupApproval.IsRequestApproval
						? Bql<True.IsEqual<False>>()
						: Bql<status.IsNotIn<State.pending, State.rejected>>(),
				
			}.AutoNameConditions();

			var approve = context.ActionDefinitions
				.CreateExisting<CATranEntry_ApprovalWorkflow>(g => g.approve, a => a
				.InFolder(FolderType.ActionsFolder, g => g.releaseFromHold)
				.PlaceAfter(g => g.releaseFromHold)
				.IsHiddenWhen(conditions.IsApprovalDisabled)
				.IsDisabledWhen(context.Conditions.Get("IsNotAdjustment"))
				.WithFieldAssignments(fa => fa.Add<approved>(e => e.SetFromValue(true))));
			
			var reject = context.ActionDefinitions
				.CreateExisting<CATranEntry_ApprovalWorkflow>(g => g.reject, a => a
				.InFolder(FolderType.ActionsFolder, approve)
				.PlaceAfter(approve)
				.IsHiddenWhen(conditions.IsApprovalDisabled)
				.IsDisabledWhen(context.Conditions.Get("IsNotAdjustment"))
				.WithFieldAssignments(fa => fa.Add<rejected>(e => e.SetFromValue(true))));

			Workflow.ConfiguratorFlow InjectApprovalWorkflow(Workflow.ConfiguratorFlow flow)
			{
				const string initialState = "_";

				return flow
					.WithFlowStates(states =>
					{
						states.Add<State.pending>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(approve, a => a.IsDuplicatedInToolbar());
									actions.Add(reject, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.putOnHold);
								})
								.WithFieldStates(fields =>
								{
									fields.AddAllFields<CAAdj>(table => table.IsDisabled());
									fields.AddField<adjRefNbr>();
									fields.AddTable<CASplit>(table => table.IsDisabled());
									fields.AddTable<CATaxTran>(table => table.IsDisabled());
								});
						});
						states.Add<State.rejected>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
								})
								.WithFieldStates(fields =>
								{
									fields.AddAllFields<CAAdj>(table => table.IsDisabled());
									fields.AddField<adjRefNbr>();
									fields.AddTable<CASplit>(table => table.IsDisabled());
									fields.AddTable<CATaxTran>(table => table.IsDisabled());
								});
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.UpdateGroupFrom(initialState, ts =>
						{
							ts.Add(t => t // New Pending Approval
								.To<State.pending>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsNotOnHoldAndIsNotApproved)
								);
							ts.Update(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.initializeState), t => t
								.When(conditions.IsNotOnHoldAndIsApproved)); // IsNotOnHold -> IsNotOnHoldAndIsApproved
						});

						transitions.UpdateGroupFrom<State.hold>(ts =>
						{
							ts.Update(
								t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold), t => t
								.When(conditions.IsApproved));
							ts.Add(t => t
								.To<State.pending>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsNotApproved));
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsRejected));
							ts.Add(t => t
								.To<State.pending>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.DoesNotPersist()
								.When(conditions.IsNotApproved));
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.DoesNotPersist()
								.When(conditions.IsRejected));
						});


						transitions.AddGroupFrom<State.pending>(ts =>
						{
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(approve)
								.When(conditions.IsApproved));
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(reject)
								.When(conditions.IsRejected));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist());
						});

						transitions.AddGroupFrom<State.rejected>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist());
						});
					});
			}

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.UpdateDefaultFlow(InjectApprovalWorkflow)
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

		public PXAction<CAAdj> approve;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<CAAdj> reject;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}