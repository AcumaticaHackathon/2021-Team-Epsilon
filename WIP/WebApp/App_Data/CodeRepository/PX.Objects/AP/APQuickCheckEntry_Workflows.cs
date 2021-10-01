using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.AP.Standalone;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	using State = APDocStatus;
	using static APQuickCheck;
	using static BoundedTo<APQuickCheckEntry, APQuickCheck>;

	public class APQuickCheckEntry_Workflow : PXGraphExtension<APQuickCheckEntry>
	{

		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<APQuickCheckEntry, APQuickCheck>());

		protected virtual void Configure(WorkflowContext<APQuickCheckEntry, APQuickCheck> context)
		{
			#region Conditions

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>.And<released.IsEqual<False>>>(),
				IsReserved
					= Bql<hold.IsEqual<True>.And<released.IsEqual<True>>>(),
				IsNotQuickCheck
					= Bql<docType.IsNotEqual<APDocType.quickCheck>>(),
				IsOpen
					= Bql<openDoc.IsEqual<True>.And<released.IsEqual<True>>>(),
				IsClosed
					= Bql<openDoc.IsEqual<False>.And<released.IsEqual<True>>>(),
				IsNotPrinted
					= Bql<hold.IsEqual<False>
						.And<APRegister.approved.IsEqual<True>>
						.And<released.IsEqual<False>>
						.And<printCheck.IsEqual<True>>
						.And<printed.IsEqual<False>>>(),
				IsPrinted
					= Bql<printCheck.IsEqual<False>.Or<printed.IsEqual<True>>>(),
				IsBalanced
					= Bql<hold.IsEqual<False>
						.And<released.IsEqual<False>>
						.And<APRegister.approved.IsEqual<True>>
						.And<printCheck.IsEqual<False>.Or<printed.IsEqual<True>>>>(),
				IsVoided 
					= Bql<docType.IsEqual<APDocType.voidQuickCheck>>(),
				IsMigrationMode =
					APSetupDefinition.GetSlot().MigrationMode == true
						? Bql<True.IsEqual<True>>()
						: Bql<True.IsEqual<False>>(),

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
							fss.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
							fss.Add<State.hold>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.prebook, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.pendingPrint>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.printed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.prebook, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									});
							});
							fss.Add<State.prebooked>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									});
							});
							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.reclassifyBatch);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									});
							});
							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.reclassifyBatch);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									});
							});
						}
						)
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold)); // New Hold
							ts.Add(t => t.To<State.pendingPrint>().IsTriggeredOn(g => g.initializeState).When(conditions.IsNotPrinted)); // Pending Print
							ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.initializeState).When(conditions.IsBalanced)); // New Balance
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOpen)); // New Open
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.initializeState).When(conditions.IsClosed)); // New Closed
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsNotPrinted)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsBalanced)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsNotPrinted));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsBalanced));

						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t
								.To<State.prebooked>()
								.IsTriggeredOn(g => g.prebook));

							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsNotPrinted));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
						});
						transitions.AddGroupFrom<State.printed>(ts =>
						{
							ts.Add(t => t
								.To<State.prebooked>()
								.IsTriggeredOn(g => g.prebook));
						});
						
						transitions.AddGroupFrom<State.pendingPrint>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPrinted));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
						});
						
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.voidCheck));
						});
					}
					))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.putOnHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.printCheck, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotQuickCheck));
						actions.Add(g => g.prebook, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotQuickCheck || conditions.IsMigrationMode));
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.voidCheck, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsVoided));
						actions.Add(g => g.validateAddresses, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.reclassifyBatch, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.vendorDocuments, c => c.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.printAPEdit, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printAPRegister, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printAPPayment, c => c.InFolder(FolderType.ReportsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<APQuickCheck>()
							.OfEntityEvent<APQuickCheck.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
					.ForbidFurtherChanges()
				);
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<APQuickCheck>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<APQuickCheck>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<APPayment.hold, APPayment.printCheck>(e.Row, e.OldRow))
					{
						APQuickCheck.Events.Select(ev => ev.UpdateStatus).FireOn(g, (APQuickCheck)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}