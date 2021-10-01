using PX.Data;
using PX.Data.Description.GI;
using PX.Data.WorkflowAPI;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Objects.PO.GraphExtensions.POReceiptEntryExt;

namespace PX.Objects.PO
{
	using State = POReceiptStatus;
	using static POReceipt;
	using static BoundedTo<POReceiptEntry, POReceipt>;

	public class POReceiptEntry_Workflow : PXGraphExtension<POReceiptEntry>
	{
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<POReceiptEntry, POReceipt>());

		protected virtual void Configure(WorkflowContext<POReceiptEntry, POReceipt> context)
		{
            #region Conditions
            Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>>(),
				IsReleased
					= Bql<released.IsEqual<True>>(),

				IsNotIntercompany
					= Bql<isIntercompany.IsEqual<False>>(),
				IsIntercompanySalesReturnGenerated
					= Bql<intercompanySONbr.IsNotNull>(),
			}
			.AutoNameConditions();
			#endregion

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<status>()
					.FlowTypeIdentifierIs<receiptType>(true)
					.WithFlows(flows =>
					{
						flows.Add<POReceiptType.poreceipt>(flow =>
						{
							return flow
								.WithFlowStates(states =>
								{
									states.Add(State.Initial, state => state.IsInitial(g => g.initializeState));
									states.Add<State.hold>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.releaseFromHold, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.assign);
												actions.Add(g => g.printAllocated);
											});
									});
									states.Add<State.balanced>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.putOnHold);
												actions.Add(g => g.assign);
												actions.Add(g => g.emailPurchaseReceipt);
												actions.Add(g => g.printPurchaseReceipt);
												actions.Add(g => g.printAllocated);
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnInventoryReceiptCreatedFromPOReceipt);
											});
									});
									states.Add<State.released>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.createReturn, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.assign);
												actions.Add(g => g.createAPDocument, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.createLCDocument);
												actions.Add(g => g.printPurchaseReceipt);
												actions.Add(g => g.printBillingDetail);
												actions.Add(g => g.printAllocated);
											});
									});
								})
								.WithTransitions(transitions =>
								{
									transitions.AddGroupFrom(State.Initial, ts =>
									{
										ts.Add(t => t
											.To<State.hold>()
											.IsTriggeredOn(g => g.initializeState)
											.When(conditions.IsOnHold));
										ts.Add(t => t
											.To<State.released>()
											.IsTriggeredOn(g => g.initializeState)
											.When(conditions.IsReleased));
										ts.Add(t => t
											.To<State.balanced>()
											.IsTriggeredOn(g => g.initializeState));
									});
									transitions.AddGroupFrom<State.hold>(ts =>
									{
										ts.Add(t => t
											.To<State.balanced>()
											.IsTriggeredOn(g => g.releaseFromHold));
									});
									transitions.AddGroupFrom<State.balanced>(ts =>
									{
										ts.Add(t => t
											.To<State.hold>()
											.IsTriggeredOn(g => g.putOnHold)
											.When(conditions.IsOnHold));
										ts.Add(t => t
											.To<State.released>()
											.IsTriggeredOn(g => g.OnInventoryReceiptCreatedFromPOReceipt)
											.WithFieldAssignments(fields =>
											{
												fields.Add<released>(true);
											}));
									});
								});
						});
						flows.Add<POReceiptType.poreturn>(flow =>
						{
							return flow
								.WithFlowStates(states =>
								{
									states.Add(State.Initial, state => state.IsInitial(g => g.initializeState));
									states.Add<State.hold>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.releaseFromHold, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.assign);
											});
									});
									states.Add<State.balanced>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.putOnHold);
												actions.Add(g => g.assign);
												actions.Add(g => g.emailPurchaseReceipt);
												actions.Add(g => g.printPurchaseReceipt);
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnInventoryIssueCreatedFromPOReturn);
											});
									});
									states.Add<State.released>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.assign);
												actions.Add(g => g.createAPDocument, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.printPurchaseReceipt);
												actions.Add(g => g.printBillingDetail);
												actions.Add<Intercompany>(e => e.generateSalesReturn);
											});
									});
								})
								.WithTransitions(transitions =>
								{
									transitions.AddGroupFrom(State.Initial, ts =>
									{
										ts.Add(t => t
											.To<State.hold>()
											.IsTriggeredOn(g => g.initializeState)
											.When(conditions.IsOnHold));
										ts.Add(t => t
											.To<State.released>()
											.IsTriggeredOn(g => g.initializeState)
											.When(conditions.IsReleased));
										ts.Add(t => t
											.To<State.balanced>()
											.IsTriggeredOn(g => g.initializeState));
									});
									transitions.AddGroupFrom<State.hold>(ts =>
									{
										ts.Add(t => t
											.To<State.balanced>()
											.IsTriggeredOn(g => g.releaseFromHold));
									});
									transitions.AddGroupFrom<State.balanced>(ts =>
									{
										ts.Add(t => t
											.To<State.hold>()
											.IsTriggeredOn(g => g.putOnHold)
											.When(conditions.IsOnHold));
										ts.Add(t => t
											.To<State.released>()
											.IsTriggeredOn(g => g.OnInventoryIssueCreatedFromPOReturn)
											.WithFieldAssignments(fields =>
											{
												fields.Add<released>(true);
											}));
									});
								});
						});
						flows.Add<POReceiptType.transferreceipt>(flow =>
						{
							return flow
								.WithFlowStates(states =>
								{
									states.Add(State.Initial, state => state.IsInitial(g => g.initializeState));
									states.Add<State.hold>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.releaseFromHold, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.assign);
											});
									});
									states.Add<State.balanced>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.putOnHold);
												actions.Add(g => g.assign);
												actions.Add(g => g.emailPurchaseReceipt);
												actions.Add(g => g.printPurchaseReceipt);
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.OnInventoryReceiptCreatedFromPOTransfer);
											});
									});
									states.Add<State.released>(state =>
									{
										return state
											.WithActions(actions =>
											{
												actions.Add(g => g.assign);
												actions.Add(g => g.createLCDocument, c => c.IsDuplicatedInToolbar());
												actions.Add(g => g.printPurchaseReceipt);
												actions.Add(g => g.printBillingDetail);
											});
									});
								})
								.WithTransitions(transitions =>
								{
									transitions.AddGroupFrom(State.Initial, ts =>
									{
										ts.Add(t => t
											.To<State.hold>()
											.IsTriggeredOn(g => g.initializeState)
											.When(conditions.IsOnHold));
										ts.Add(t => t
											.To<State.released>()
											.IsTriggeredOn(g => g.initializeState)
											.When(conditions.IsReleased));
										ts.Add(t => t
											.To<State.balanced>()
											.IsTriggeredOn(g => g.initializeState));
									});
									transitions.AddGroupFrom<State.hold>(ts =>
									{
										ts.Add(t => t
											.To<State.balanced>()
											.IsTriggeredOn(g => g.releaseFromHold));
									});
									transitions.AddGroupFrom<State.balanced>(ts =>
									{
										ts.Add(t => t
											.To<State.hold>()
											.IsTriggeredOn(g => g.putOnHold)
											.When(conditions.IsOnHold));
										ts.Add(t => t
											.To<State.released>()
											.IsTriggeredOn(g => g.OnInventoryReceiptCreatedFromPOTransfer)
											.WithFieldAssignments(fields =>
											{
												fields.Add<released>(true);
											}));
									});
								});
						});
					})
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState);

						actions.Add(g => g.release, c => c.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.putOnHold, c => c
							.InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(true)));

						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(false)));

						actions.Add(g => g.assign, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.createAPDocument, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.createReturn, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.createLCDocument, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.emailPurchaseReceipt, c => c.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.printPurchaseReceipt, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printBillingDetail, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printAllocated, c => c.InFolder(FolderType.ReportsFolder));

						actions.Add<Intercompany>(e => e.generateSalesReturn, a => a
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotIntercompany)
							.IsDisabledWhen(conditions.IsIntercompanySalesReturnGenerated));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<POReceipt>()
							.OfEntityEvent<Events>(e => e.InventoryReceiptCreated)
							.Is(g => g.OnInventoryReceiptCreatedFromPOReceipt)
							.UsesTargetAsPrimaryEntity()
							.DisplayName("IN Receipt Created"));

						handlers.Add(handler => handler
							.WithTargetOf<POReceipt>()
							.OfEntityEvent<Events>(e => e.InventoryIssueCreated)
							.Is(g => g.OnInventoryIssueCreatedFromPOReturn)
							.UsesTargetAsPrimaryEntity()
							.DisplayName("IN Issue Created"));

						handlers.Add(handler => handler
							.WithTargetOf<POReceipt>()
							.OfEntityEvent<Events>(e => e.InventoryReceiptCreated)
							.Is(g => g.OnInventoryReceiptCreatedFromPOTransfer)
							.UsesTargetAsPrimaryEntity()
							.DisplayName("IN Receipt Created"));
					});
			});
		}
	}
}
