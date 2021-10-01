using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.FA
{
	using State = FixedAssetStatus;
	using static FixedAsset;
	using static BoundedTo<AssetMaint, FixedAsset>;

	public partial class AssetMaint_Workflow : PXGraphExtension<AssetMaint>
	{

		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<AssetMaint, FixedAsset>());

		protected virtual void Configure(WorkflowContext<AssetMaint, FixedAsset> context)
		{
			#region Conditions

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				HoldEntry = Bql<holdEntry.IsEqual<True>.And<isAcquired.IsNull>>(),
				NotHoldEntry = Bql<holdEntry.IsEqual<False>>(),
				IsNotDepreciable = Bql<depreciable.IsNotEqual<True>.Or<underConstruction.IsEqual<True>>>(),
			}.AutoNameConditions();

			#endregion
			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(flow =>
						flow
							.WithFlowStates(fss =>
								{
									fss.Add(initialState, flowState => 
										flowState.IsInitial(g => g.initializeState));
									fss.Add<State.hold>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar());
											}).WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnUpdateStatus);
												handlers.Add(g => g.OnActivateAsset);
												handlers.Add(g => g.OnSuspendAsset);
											});
									});
									fss.Add<State.active>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.putOnHold);
												actions.Add(g => g.runDisposal);
												actions.Add(g => g.Suspend);
												actions.Add(g => g.runReversal);
												actions.Add(g => g.CalculateDepreciation);
												actions.Add(g => g.runSplit);
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnUpdateStatus);
												handlers.Add(g => g.OnDisposeAsset);
												handlers.Add(g => g.OnSuspendAsset);
												handlers.Add(g => g.OnReverseAsset);
												handlers.Add(g => g.OnFullyDepreciateAsset);
											});
									});
									fss.Add<State.suspended>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.Suspend, a => a.IsDuplicatedInToolbar());
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnActivateAsset);
											});
									});
									fss.Add<State.fullyDepreciated>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.runDisposal);
												actions.Add(g => g.runReversal);
												actions.Add(g => g.runSplit);
											}).WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnDisposeAsset);
												handlers.Add(g => g.OnReverseAsset);
											});
									});
									fss.Add<State.disposed>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.runDispReversal);
											}).WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnDisposeAsset);
												handlers.Add(g => g.OnFullyDepreciateAsset);
												handlers.Add(g => g.OnActivateAsset);
											});
									});
									fss.Add<State.reversed>();
								}
							)
							.WithTransitions(transitions =>
							{
								transitions.AddGroupFrom(initialState, ts =>
								{
									ts.Add(t => t.To<State.active>()
										.IsTriggeredOn(g => g.initializeState)); // New Hold
								});
								transitions.AddGroupFrom<State.hold>(ts =>
								{
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.OnUpdateStatus)
										.When(conditions.NotHoldEntry));
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.releaseFromHold)
										.DoesNotPersist());
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.OnActivateAsset));
									ts.Add(t => t
										.To<State.suspended>()
										.IsTriggeredOn(g => g.OnSuspendAsset));
								});
								transitions.AddGroupFrom<State.active>(ts =>
								{
									ts.Add(t => t
										.To<State.hold>()
										.IsTriggeredOn(g => g.OnUpdateStatus)
										.When(conditions.HoldEntry));
									ts.Add(t => t
										.To<State.hold>()
										.IsTriggeredOn(g => g.putOnHold)
										.DoesNotPersist());
									ts.Add(t => t
										.To<State.disposed>()
										.IsTriggeredOn(g => g.OnDisposeAsset));
									ts.Add(t => t
										.To<State.suspended>()
										.IsTriggeredOn(g => g.OnSuspendAsset)
										.WithFieldAssignments(fas=> fas.Add<suspended>(f => f.SetFromValue(true))));
									ts.Add(t => t
										.To<State.reversed>()
										.IsTriggeredOn(g => g.OnReverseAsset));
									ts.Add(t => t
										.To<State.fullyDepreciated>()
										.IsTriggeredOn(g => g.OnFullyDepreciateAsset));
								});
								transitions.AddGroupFrom<State.suspended>(ts =>
								{
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.OnActivateAsset)
										.WithFieldAssignments(fas=> fas.Add<suspended>(f => f.SetFromValue(false))));
								});
								transitions.AddGroupFrom<State.fullyDepreciated>(ts =>
								{
									ts.Add(t => t
										.To<State.disposed>()
										.IsTriggeredOn(g => g.OnDisposeAsset));
									ts.Add(t => t
										.To<State.reversed>()
										.IsTriggeredOn(g => g.OnReverseAsset));
								});
								transitions.AddGroupFrom<State.reversed>(ts =>
								{
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.OnActivateAsset));
								});
								transitions.AddGroupFrom<State.disposed>(ts =>
								{
									ts.Add(t => t
										.To<State.active>()
										.IsTriggeredOn(g => g.OnActivateAsset));
									ts.Add(t => t
										.To<State.fullyDepreciated>()
										.IsTriggeredOn(g => g.OnFullyDepreciateAsset));
								});
							}))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.putOnHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithPersistOptions(ActionPersistOptions.NoPersist));
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithPersistOptions(ActionPersistOptions.NoPersist));
						actions.Add(g => g.runDisposal, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.runDispReversal, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.Suspend, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.runReversal, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.CalculateDepreciation, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsNotDepreciable));
						actions.Add(g => g.runSplit, c => c
							.InFolder(FolderType.ActionsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<FixedAsset>()
							.OfEntityEvent<FixedAsset.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<FixedAsset>()
							.OfEntityEvent<FixedAsset.Events>(e => e.ActivateAsset)
							.Is(g => g.OnActivateAsset)
							.UsesTargetAsPrimaryEntity()
							.WithFieldAssignments(fas =>
							{
								fas.Add<active>(f => f.SetFromValue(true)); 
								fas.Add<suspended>(f => f.SetFromValue(false));
							}));
						handlers.Add(handler => handler
							.WithTargetOf<FixedAsset>()
							.OfEntityEvent<FixedAsset.Events>(e => e.DisposeAsset)
							.Is(g => g.OnDisposeAsset)
							.UsesTargetAsPrimaryEntity()
							.WithFieldAssignments(fas => 
								fas.Add<suspended>(f => f.SetFromValue(true))));
						handlers.Add(handler => handler
							.WithTargetOf<FixedAsset>()
							.OfEntityEvent<FixedAsset.Events>(e => e.SuspendAsset)
							.Is(g => g.OnSuspendAsset)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<FixedAsset>()
							.OfEntityEvent<FixedAsset.Events>(e => e.FullyDepreciateAsset)
							.Is(g => g.OnFullyDepreciateAsset)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<FixedAsset>()
							.OfEntityEvent<FixedAsset.Events>(e => e.ReverseAsset)
							.Is(g => g.OnReverseAsset)
							.UsesTargetAsPrimaryEntity());
						
					})
			);
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<FixedAsset>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<FixedAsset>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<FixedAsset.classID>(e.Row, e.OldRow))
					{
						FixedAsset.Events.Select(ev => ev.UpdateStatus).FireOn(g, (FixedAsset)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}