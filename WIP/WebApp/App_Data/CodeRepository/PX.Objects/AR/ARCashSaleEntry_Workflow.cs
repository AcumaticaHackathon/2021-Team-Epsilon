using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.AR.Standalone;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	using State = ARDocStatus;
	using static ARCashSale;
	using static BoundedTo<ARCashSaleEntry, ARCashSale>;

	public class ARCashSaleEntry_Workflow : PXGraphExtension<ARCashSaleEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<ARCashSaleEntry, ARCashSale>());
		
		[PXWorkflowDependsOnType(typeof(ARSetup))]
		protected virtual void Configure(WorkflowContext<ARCashSaleEntry, ARCashSale> context)
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
				IsVoid
					= Bql<docType.IsIn<ARDocType.voidPayment, ARDocType.voidRefund>>(),
				IsPendingProcessing
					= Bql<hold.IsEqual<False>
						.And<ARRegister.approved.IsEqual<True>>
						.And<released.IsEqual<False>>
						.And<voided.IsEqual<False>>
						.And<ARRegister.pendingProcessing.IsEqual<True>>
						.And<approved.IsEqual<True>.Or<ARRegister.dontApprove.IsEqual<True>>>>(),
				IsBalanced
					= Bql<ARRegister.approved.IsEqual<True>
						.And<voided.IsEqual<False>>
						.And<ARRegister.pendingProcessing.IsEqual<False>>>(),
				
				IsCCIntegrated = _Definition.IntegratedCCProcessing == true 
					? Bql<status.IsEqual<ARDocStatus.cCHold>>()
					: Bql<True.IsEqual<False>>(),
				IsNotCapturable 
					= Bql<docType.IsEqual<ARDocType.cashReturn>>(),
				IsMigrationMode =
					_Definition.MigrationMode == true
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
										actions.Add(g => g.sendEmail);
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
										actions.Add(g => g.printInvoice);
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
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.captureCCPayment, a => a.IsDuplicatedInToolbar());
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.authorizeCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.voidCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.creditCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.recordCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.captureOnlyCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.validateCCPayment);
										actions.Add(g => g.release);
										actions.Add(a=>a.voidCheck);
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
										actions.Add(g => g.sendEmail);
										actions.Add(g => g.emailInvoice);
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.customerDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.sendEmail);
										actions.Add(g => g.emailInvoice);
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.reclassifyBatch);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.captureCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.authorizeCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.voidCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.creditCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.recordCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.captureOnlyCCPayment);
										actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.validateCCPayment);
									});
							});
						}
						)
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t // New Pending Processing
								.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)
								.DoesNotPersist()
							);
							ts.Add(t => t // New Pending Processing
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsPendingProcessing)
								.DoesNotPersist()
							);
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
								.To<State.voided>()
								.IsTriggeredOn(g=>g.voidCheck));
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
						actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.captureCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotCapturable));
						actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.authorizeCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.voidCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.creditCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.recordCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.captureOnlyCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add<ARCashSaleEntry.PaymentTransaction>(a=>a.validateCCPayment, a=>a
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.printInvoice, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.emailInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fass => fass.Add<emailed>(v => v.SetFromValue(true))));
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsCCIntegrated));
						actions.Add(g => g.voidCheck, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsVoid));
						actions.Add(g => g.reclassifyBatch, c => c
							.IsHiddenWhen(conditions.IsMigrationMode)
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.sendEmail, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.customerDocuments, c => c.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.printAREdit, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printARRegister, c => c.InFolder(FolderType.ReportsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<ARCashSale>()
							.OfEntityEvent<ARCashSale.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
					.ForbidFurtherChanges()
				);
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<ARCashSale>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<ARCashSale>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<ARCashSale.hold,ARCashSale.pendingProcessing>(e.Row, e.OldRow))
					{
						ARCashSale.Events.Select(ev => ev.UpdateStatus).FireOn(g, (ARCashSale)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}