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

namespace PX.Objects.AR
{
	using State = ARDocStatus;
	using static ARPayment;
	using static BoundedTo<ARPaymentEntry, ARPayment>;

	public class ARPaymentEntry_Workflow : PXGraphExtension<ARPaymentEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<ARPaymentEntry, ARPayment>());

		[PXWorkflowDependsOnType(typeof(ARSetup))]
		protected virtual void Configure(WorkflowContext<ARPaymentEntry, ARPayment> context)
		{
			var _Definition = ARSetupDefinition.GetSlot();
			#region Conditions

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>.And<released.IsEqual<False>>>(),

				IsNotOnHold
					= Bql<hold.IsEqual<False>.And<released.IsEqual<False>>>(),

				IsReserved
					= Bql<hold.IsEqual<True>.And<released.IsEqual<True>>>(),
				
				IsOpen
					= Bql<openDoc.IsEqual<True>.And<released.IsEqual<True>>>(),

				IsClosed
					= Bql<openDoc.IsEqual<False>.And<released.IsEqual<True>>>(),

				IsNotVoidable
					= Bql<docType.IsIn<ARDocType.voidPayment, ARDocType.voidRefund, ARDocType.creditMemo>
						.Or<docType.IsEqual<ARDocType.smallBalanceWO>.And<status.IsEqual<ARDocStatus.reserved>>>>(),
				
				IsCCIntegrated = _Definition.IntegratedCCProcessing == true && _Definition.MigrationMode !=true 
					? Bql<status.IsEqual<ARDocStatus.cCHold>>()
					: Bql<True.IsEqual<False>>(),

				IsPendingProcessing
					= Bql<hold.IsEqual<False>
						.And<released.IsEqual<False>>
						.And<ARRegister.pendingProcessing.IsEqual<True>>
						.And<ARRegister.approved.IsEqual<True>.Or<ARRegister.dontApprove.IsEqual<True>>>>(),
				
				IsBalanced
					= Bql<ARRegister.approved.IsEqual<True>.And<ARRegister.pendingProcessing.IsEqual<False>>>(),
				
				IsVoided
					= Bql<voided.IsEqual<True>>(),
				IsNotCapturable 
					= Bql<docType.IsIn<ARDocType.voidPayment, ARDocType.refund, ARDocType.voidRefund, ARDocType.cashReturn>>(),

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
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
									}).WithEventHandlers(handlers =>
                                    {
                                      	handlers.Add(g => g.OnUpdateStatus);
                                    });
							});
							fss.Add<State.cCHold>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(a=>a.putOnHold);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureCCPayment, a => a.IsDuplicatedInToolbar());
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.authorizeCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.voidCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.creditCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.recordCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureOnlyCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.validateCCPayment);
										actions.Add(g => g.release);
										actions.Add(a=>a.voidCheck);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnVoidDocument);
									});
							});
							fss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.authorizeCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.voidCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.creditCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.recordCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureOnlyCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.validateCCPayment);
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnVoidDocument);
									});
							});
							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.authorizeCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.voidCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.creditCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.recordCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureOnlyCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.validateCCPayment);
										actions.Add(g => g.reverseApplication);
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
										actions.Add(g => g.initializeState, act => act.IsAutoAction());
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnCloseDocument);
										handlers.Add(g => g.OnVoidDocument);
									});
							});
							fss.Add<State.reserved>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
										actions.Add(g => g.voidCheck);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnVoidDocument);
									});
							});
							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.authorizeCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.voidCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.creditCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.recordCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureOnlyCCPayment);
										actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.validateCCPayment);
										actions.Add(g => g.reverseApplication);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnOpenDocument);
										handlers.Add(g => g.OnVoidDocument);
									});
							});
						}
						)
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t // New Balance
								.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)
								.DoesNotPersist());
							ts.Add(t => t // New Pending Processing
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsPendingProcessing)
								.DoesNotPersist());
							ts.Add(t => t // New Balance
								.To<State.balanced>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsBalanced)
								.DoesNotPersist()
							);ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOpen)
								.DoesNotPersist()); // New Open
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsClosed)
								.DoesNotPersist()); // New Closed
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsPendingProcessing)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.When(conditions.IsBalanced)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsBalanced));
						});
						transitions.AddGroupFrom<State.cCHold>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));							
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsBalanced));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g=>g.OnVoidDocument));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.DoesNotPersist()
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.DoesNotPersist()
								.When(conditions.IsOnHold));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g=>g.OnVoidDocument));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.reserved>()
								.IsTriggeredOn(g => g.putOnHold));
							ts.Add(t => t.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnCloseDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsVoided));
						});
						transitions.AddGroupFrom<State.reserved>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.releaseFromHold));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
						});
						transitions.AddGroupFrom<State.closed>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnOpenDocument));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reverseApplication)
								.DoesNotPersist());
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsVoided));
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
						actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotCapturable));
						actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.authorizeCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.voidCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.creditCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.recordCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.captureOnlyCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARPaymentEntry.PaymentTransaction>(a=>a.validateCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsCCIntegrated)
						);
						actions.Add(g => g.voidCheck, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotVoidable));
						actions.Add(g => g.reverseApplication, g => g
							.WithPersistOptions(ActionPersistOptions.NoPersist)
						);
						actions.Add(g => g.customerDocuments, c => c.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.printAREdit, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printARRegister, c => c.InFolder(FolderType.ReportsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<ARPayment>()
							.OfEntityEvent<ARPayment.Events>(e => e.ReleaseDocument)
							.Is(g => g.OnReleaseDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARPayment>()
							.OfEntityEvent<ARPayment.Events>(e => e.OpenDocument)
							.Is(g => g.OnOpenDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARPayment>()
							.OfEntityEvent<ARPayment.Events>(e => e.CloseDocument)
							.Is(g => g.OnCloseDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARPayment>()
							.OfEntityEvent<ARPayment.Events>(e => e.VoidDocument)
							.Is(g => g.OnVoidDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARPayment>()
							.OfEntityEvent<ARPayment.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
					.ForbidFurtherChanges()
				);
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<ARPayment>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<ARPayment>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<ARPayment.hold,ARPayment.pendingProcessing>(e.Row, e.OldRow))
					{
						ARPayment.Events.Select(ev => ev.UpdateStatus).FireOn(g, (ARPayment)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}