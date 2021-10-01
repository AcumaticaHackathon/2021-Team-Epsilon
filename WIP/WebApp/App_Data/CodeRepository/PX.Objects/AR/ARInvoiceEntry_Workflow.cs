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
	using static ARInvoice;
	using static BoundedTo<ARInvoiceEntry, ARInvoice>;
	public class ARSetupDefinition : IPrefetchable
	{
		public bool? PrintBeforeRelease { get; private set; }
		public bool? EmailBeforeRelease { get; private set; }
		public bool? IntegratedCCProcessing { get; private set; }
		public bool? MigrationMode { get; private set; }
		void IPrefetchable.Prefetch()
		{
			using (PXDataRecord rec =
				PXDatabase.SelectSingle<ARSetup>(
					new PXDataField("PrintBeforeRelease"),
					new PXDataField("EmailBeforeRelease"),
					new PXDataField("IntegratedCCProcessing"),
					new PXDataField("MigrationMode")))
			{
				PrintBeforeRelease = rec != null ? rec.GetBoolean(0) : false;
				EmailBeforeRelease = rec != null ? rec.GetBoolean(1) : false;
				IntegratedCCProcessing = rec != null ? rec.GetBoolean(2) : false;
				MigrationMode = rec != null ? rec.GetBoolean(3) : false;
			}
		}
		public static ARSetupDefinition GetSlot()
		{
			return PXDatabase.GetSlot<ARSetupDefinition>(typeof(ARSetup).FullName, typeof(ARSetup));
		}
	}
	public partial class ARInvoiceEntry_Workflow : PXGraphExtension<ARInvoiceEntry>
	{
		public const string MarkAsDontEmail = "Mark as Do not Email";
		[PXWorkflowDependsOnType(typeof(ARSetup))]
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<ARInvoiceEntry, ARInvoice>());
		
		protected virtual void Configure(WorkflowContext<ARInvoiceEntry, ARInvoice> context)
		{
			#region Conditions

			var _Definition = ARSetupDefinition.GetSlot();
			
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>.And<released.IsEqual<False>>>(),
				IsOnCreditHold
					= Bql<ARRegister.approved.IsEqual<True>.And<released.IsEqual<False>>.And<creditHold.IsEqual<True>>>(),
				IsNotOnHold
					= Bql<hold.IsEqual<False>.And<released.IsEqual<False>>>(),
				IsReserved
					= Bql<hold.IsEqual<True>.And<released.IsEqual<True>>>(),
				IsScheduled
					= Bql<scheduled.IsEqual<True>.And<released.IsEqual<False>>>(),
				IsOpen
					= Bql<openDoc.IsEqual<True>.And<released.IsEqual<True>>>(),
				IsClosed
					= Bql<openDoc.IsEqual<False>.And<released.IsEqual<True>>>(),
				IsPendingProcessing
					=
					_Definition.PrintBeforeRelease == true && _Definition.EmailBeforeRelease == true
						? Bql<hold.IsEqual<False>
							.And<creditHold.IsEqual<False>>
							.And<pendingProcessing.IsEqual<True>>
							.And<released.IsEqual<False>>
							.And<ARRegister.approved.IsEqual<True>>
							.And<printInvoice.IsEqual<False>>
							.And<emailInvoice.IsEqual<False>>>()
						: _Definition.PrintBeforeRelease == true
							? Bql<hold.IsEqual<False>
								.And<creditHold.IsEqual<False>>
								.And<pendingProcessing.IsEqual<True>>
								.And<released.IsEqual<False>>
								.And<ARRegister.approved.IsEqual<True>>
								.And<printInvoice.IsEqual<False>>>()
							: _Definition.EmailBeforeRelease == true
								? Bql<hold.IsEqual<False>
									.And<creditHold.IsEqual<False>>
									.And<pendingProcessing.IsEqual<True>>
									.And<released.IsEqual<False>>
									.And<ARRegister.approved.IsEqual<True>>
									.And<emailInvoice.IsEqual<False>>>()
								: Bql<hold.IsEqual<False>
									.And<creditHold.IsEqual<False>>
									.And<pendingProcessing.IsEqual<True>>
									.And<released.IsEqual<False>>
									.And<ARRegister.approved.IsEqual<True>>>(),
				IsBalanced
					= 
					_Definition.PrintBeforeRelease == true && _Definition.EmailBeforeRelease == true 
						? Bql<hold.IsEqual<False>
							.And<creditHold.IsEqual<False>>
							.And<pendingProcessing.IsEqual<False>>
							.And<released.IsEqual<False>>
							.And<ARRegister.approved.IsEqual<True>>
							.And<printInvoice.IsEqual<False>>
							.And<emailInvoice.IsEqual<False>>>()
					:_Definition.PrintBeforeRelease == true
						? Bql<hold.IsEqual<False>
							.And<creditHold.IsEqual<False>>
							.And<pendingProcessing.IsEqual<False>>
							.And<released.IsEqual<False>>
							.And<ARRegister.approved.IsEqual<True>>
							.And<printInvoice.IsEqual<False>>>()
					: _Definition.EmailBeforeRelease == true 
						? Bql<hold.IsEqual<False>
							.And<creditHold.IsEqual<False>>
							.And<pendingProcessing.IsEqual<False>>
							.And<released.IsEqual<False>>
							.And<ARRegister.approved.IsEqual<True>>
							.And<emailInvoice.IsEqual<False>>>() 
					: Bql<hold.IsEqual<False>
							.And<creditHold.IsEqual<False>>
							.And<pendingProcessing.IsEqual<False>>
							.And<released.IsEqual<False>>
							.And<ARRegister.approved.IsEqual<True>>>(),
				IsPendingPrint
					= _Definition.PrintBeforeRelease == true 
					? Bql<hold.IsEqual<False>
						.And<creditHold.IsEqual<False>>
						.And<released.IsEqual<False>>
						.And<ARRegister.approved.IsEqual<True>>
						.And<printInvoice.IsEqual<True>>>()
					: Bql<True.IsEqual<False>>(),
				IsPendingEmail
					= _Definition.EmailBeforeRelease == true && _Definition.PrintBeforeRelease == true
					? Bql<hold.IsEqual<False>.And<creditHold.IsEqual<False>>
						.And<released.IsEqual<False>>
						.And<ARRegister.approved.IsEqual<True>>
						.And<printInvoice.IsEqual<False>>
						.And<emailInvoice.IsEqual<True>>>()
					: _Definition.EmailBeforeRelease == true
						? Bql<hold.IsEqual<False>.And<creditHold.IsEqual<False>>
							.And<released.IsEqual<False>>
							.And<ARRegister.approved.IsEqual<True>>
							.And<emailInvoice.IsEqual<True>>>()
						: Bql<True.IsEqual<False>>(),
				IsEmailed
					= Bql<dontEmail.IsEqual<True>.Or<emailed.IsEqual<True>>>(),
				IsCreditMemo 
					= Bql<docType.IsEqual<ARDocType.creditMemo>>(),
				IsNotSchedulable
					= Bql<docType.IsNotIn<ARDocType.invoice,ARDocType.creditMemo,ARDocType.debitMemo>>(),
				IsNotCreditMemo 
					= Bql<docType.IsNotEqual<ARDocType.creditMemo>>(),
				IsFinCharge 
					= Bql<docType.IsEqual<ARDocType.finCharge>>(),
				IsSmallCreditWO
					= Bql<docType.IsEqual<ARDocType.smallCreditWO>>(),
				IsRetainage
					= Bql<isRetainageDocument.IsEqual<True>.Or<retainageApply.IsEqual<True>>>(),
				IsNotAllowRecalcPrice
					= Bql<pendingPPD.IsEqual<True>
						.Or<ARRegister.curyRetainageTotal.IsGreater<decimal0>
						.Or<isRetainageDocument.IsEqual<True>>>>(),
				IsNotInvoice = Bql<docType.IsNotIn<ARDocType.invoice, ARDocType.creditMemo>>(),
				IsVoided = Bql<voided.IsEqual<True>>(),
				IsMigrationMode =
					_Definition.MigrationMode == true
						? Bql<True.IsEqual<True>>()
						: Bql<True.IsEqual<False>>(),

			}.AutoNameConditions();

			#endregion
			const string initialState = "_";
			var markDontEmail = context.ActionDefinitions.CreateNew(MarkAsDontEmail, a => a
				.DisplayName("Mark as Do not Email")
				.InFolder(FolderType.ActionsFolder, g => g.createSchedule)
				.MassProcessingScreen<ARPrintInvoices>()
				.PlaceAfter(g => g.createSchedule)
				.IsDisabledWhen(conditions.IsEmailed)
				.WithFieldAssignments(fa => fa.Add<dontEmail>(e => e.SetFromValue(true))));

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
										actions.Add(g => g.recalculateDiscountsAction);
										actions.Add(g => g.sendEmail);
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.customerDocuments);
										actions.Add(markDontEmail);
									}).WithEventHandlers(handlers =>handlers
										.Add(g => g.OnUpdateStatus));
							});
							fss.Add<State.creditHold>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.releaseFromCreditHold, act => act.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.recalculateDiscountsAction);
										actions.Add(g => g.sendEmail);
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.customerDocuments);
										actions.Add(markDontEmail);
									});
							});
							fss.Add<State.pendingPrint>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.printInvoice, act => act.IsDuplicatedInToolbar());
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.createSchedule);
										actions.Add(g => g.emailInvoice);
										actions.Add(g => g.recalculateDiscountsAction);
										actions.Add(g => g.putOnCreditHold);
										actions.Add(g => g.sendEmail);
										actions.Add(markDontEmail);
										actions.Add(g => g.initializeState, act => act.IsAutoAction());
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnReleaseDocument);
									});
							});
							fss.Add<State.pendingEmail>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.createSchedule);
										actions.Add(g => g.emailInvoice, act => act.IsDuplicatedInToolbar());
										actions.Add(g => g.recalculateDiscountsAction);
										actions.Add(g => g.putOnCreditHold);
										actions.Add(g => g.sendEmail);
										actions.Add(markDontEmail);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnReleaseDocument);
									});
							});
							fss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.putOnCreditHold);
										actions.Add(g => g.emailInvoice);
										actions.Add(g => g.createSchedule);
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.recalculateDiscountsAction);
										actions.Add(g => g.sendEmail);
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.customerDocuments);
										actions.Add(markDontEmail);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnReleaseDocument);
									});
							});
							fss.Add<State.scheduled>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.createSchedule, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAREdit);
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.customerDocuments);
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnVoidSchedule);
									});
							});
							
							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.payInvoice, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.emailInvoice);
										actions.Add(g => g.reverseInvoice);
										actions.Add(g => g.reverseInvoiceAndApplyToMemo);
										actions.Add(g=>g.validateAddresses);
										actions.Add(g => g.writeOff);
										actions.Add(g => g.reclassifyBatch);
										actions.Add(g => g.customerRefund);
										actions.Add(g => g.sendEmail);
										actions.Add(markDontEmail);
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
										actions.Add(g => g.sOInvoice);
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnCloseDocument);
										handlers.Add(g => g.OnCancelDocument);
									});
							});

							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.emailInvoice);
										actions.Add(g => g.reverseInvoice);
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.reclassifyBatch);
										
										actions.Add(g => g.printInvoice);
										actions.Add(g => g.printARRegister);
										actions.Add(g => g.customerDocuments);
										actions.Add(g => g.sOInvoice);
										actions.Add(g => g.sendEmail);
										actions.Add(markDontEmail);

									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnOpenDocument);
										handlers.Add(g => g.OnVoidDocument);
										handlers.Add(g => g.OnCancelDocument);
									});
							});
							fss.Add<State.canceled>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printInvoice);
									})
									.WithFieldStates(states =>
									{
										states.AddTable<ARInvoice>(state => state.IsDisabled());
									});
							});
							fss.Add<State.cCHold>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(a => a.putOnHold);
										actions.Add(g => g.release);
										actions.Add(a => a.voidCheck);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnReleaseDocument);
									});
							});
						})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold)); // New Hold
							ts.Add(t => t.To<State.creditHold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnCreditHold));
							ts.Add(t => t.To<State.pendingPrint>().IsTriggeredOn(g => g.initializeState).When(conditions.IsPendingPrint));
							ts.Add(t => t.To<State.pendingEmail>().IsTriggeredOn(g => g.initializeState).When(conditions.IsPendingEmail));
							ts.Add(t => t.To<State.cCHold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsPendingProcessing)); // New Pending Processing
							ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.initializeState).When(conditions.IsBalanced)); // New Balance
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOpen)); // New Open
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.initializeState).When(conditions.IsClosed)); // New Closed
							ts.Add(t => t.To<State.reserved>().IsTriggeredOn(g => g.initializeState).When(conditions.IsReserved)); // New Reserved
							ts.Add(t => t.To<State.scheduled>().IsTriggeredOn(g => g.initializeState).When(conditions.IsScheduled)); // New Reserved
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.When(conditions.IsOnCreditHold));
							ts.Add(t => t
								.To<State.pendingPrint>().IsTriggeredOn(g => g.releaseFromHold)
								.When(conditions.IsPendingPrint));
							ts.Add(t => t
								.To<State.pendingEmail>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.When(conditions.IsPendingEmail));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.When(conditions.IsBalanced));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnCreditHold));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingPrint));
							ts.Add(t => t
								.To<State.pendingEmail>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingEmail));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsBalanced));
						});
						transitions.AddGroupFrom<State.creditHold>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.releaseFromCreditHold)
								.When(conditions.IsPendingPrint));
							ts.Add(t => t
								.To<State.pendingEmail>()
								.IsTriggeredOn(g => g.releaseFromCreditHold)
								.When(conditions.IsPendingEmail));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.releaseFromCreditHold)
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromCreditHold)
								.When(conditions.IsBalanced));
						});
						transitions.AddGroupFrom<State.pendingPrint>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.putOnCreditHold));
							ts.Add(t => t
								.To<State.pendingEmail>()
								.IsTriggeredOn(g => g.printInvoice)
								.When(conditions.IsPendingEmail));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.printInvoice)
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.printInvoice));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g=>g.emailInvoice)
								.WithFieldAssignments(fass => fass.Add<emailed>(v => v.SetFromValue(true))));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnCreditHold));
							ts.Add(t => t
								.To<State.pendingEmail>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingEmail));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsBalanced));
							ts.Add(t => t.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
						});
						transitions.AddGroupFrom<State.pendingEmail>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.putOnCreditHold));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(markDontEmail)
								.When(conditions.IsPendingPrint));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(markDontEmail));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g=>g.emailInvoice)
								.WithFieldAssignments(fass => fass.Add<emailed>(v => v.SetFromValue(true))));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnCreditHold));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingPrint));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsBalanced));
							ts.Add(t => t.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
						});

						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.putOnCreditHold));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnCreditHold));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g=>g.emailInvoice)
								.WithFieldAssignments(fass => fass.Add<emailed>(v => v.SetFromValue(true))));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingPrint));
							ts.Add(t => t
								.To<State.pendingEmail>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingEmail));
							ts.Add(t => t
								.To<State.cCHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPendingProcessing));
							ts.Add(t => t.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));

						});
						
						transitions.AddGroupFrom<State.scheduled>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<voided>(e => e.SetFromValue(true));
									fas.Add<scheduled>(e => e.SetFromValue(false));
									fas.Add<scheduleID>(e => e.SetFromValue(null));
								}));
							ts.Add(t => t.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
						});
						transitions.AddGroupFrom<State.cCHold>(ts =>
						{
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsBalanced));
							ts.Add(t => t
								.To<State.creditHold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnCreditHold));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g=>g.emailInvoice)
								.WithFieldAssignments(fass => fass.Add<emailed>(v => v.SetFromValue(true))));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnCloseDocument));
							ts.Add(t => t
								.To<State.canceled>()
								.IsTriggeredOn(g => g.OnCancelDocument));
						});
						transitions.AddGroupFrom<State.closed>(ts =>
						{
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g=>g.emailInvoice)
								.WithFieldAssignments(fass => fass.Add<emailed>(v => v.SetFromValue(true))));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnOpenDocument));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.canceled>()
								.IsTriggeredOn(g => g.OnCancelDocument));
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
						actions.Add(g => g.putOnCreditHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsCreditMemo)
							.WithFieldAssignments(fass =>
							{
								fass.Add<creditHold>(v => v.SetFromValue(true));
								fass.Add<approvedCredit>(v => v.SetFromValue(false));
								fass.Add<approvedCreditAmt>(v => v.SetFromValue(0));
							}));
						actions.Add(g => g.releaseFromCreditHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsCreditMemo)
							.WithFieldAssignments(fass =>
							{
								fass.Add<creditHold>(v => v.SetFromValue(false));
							}));
						actions.Add(g => g.printInvoice, c => c
							.InFolder(FolderType.ActionsFolder).MassProcessingScreen<ARPrintInvoices>().InBatchMode()
							.WithFieldAssignments(fa => fa.Add<printed>(e => e.SetFromValue(true))));
						actions.Add(g => g.emailInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.MassProcessingScreen<ARPrintInvoices>());
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.payInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.reverseInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsFinCharge || conditions.IsSmallCreditWO));
						actions.Add(g => g.reverseInvoiceAndApplyToMemo, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsFinCharge));
						actions.Add(g => g.customerRefund, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotCreditMemo));
						actions.Add(g => g.writeOff, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.createSchedule, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotSchedulable || conditions.IsMigrationMode));
						actions.Add(markDontEmail);
						actions.Add(g => g.recalculateDiscountsAction, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsMigrationMode)
							.IsDisabledWhen(conditions.IsNotAllowRecalcPrice));
						actions.Add(g => g.reclassifyBatch, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.validateAddresses, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.sendEmail, c => c
							.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.customerDocuments, c => c.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.sOInvoice, c => c.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.printAREdit, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printARRegister, c => c.InFolder(FolderType.ReportsFolder));

					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<ARRegister>()
							.WithParametersOf<GL.Schedule>()
							.OfEntityEvent<ARRegister.Events>(e => e.ConfirmSchedule)
							.Is(g => g.OnConfirmSchedule)
							.UsesPrimaryEntityGetter<
								SelectFrom<ARInvoice>.
								Where<ARInvoice.docType.IsEqual<ARRegister.docType.FromCurrent>.
									And<ARInvoice.refNbr.IsEqual<ARRegister.refNbr.FromCurrent>>>
							>());
						handlers.Add(handler => handler
							.WithTargetOf<ARRegister>()
							.WithParametersOf<GL.Schedule>()
							.OfEntityEvent<ARRegister.Events>(e => e.VoidSchedule)
							.Is(g => g.OnVoidSchedule)
							.UsesPrimaryEntityGetter<
								SelectFrom<ARInvoice>.
								Where<ARInvoice.docType.IsEqual<ARRegister.docType.FromCurrent>.
									And<ARInvoice.refNbr.IsEqual<ARRegister.refNbr.FromCurrent>>>
							>());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.ReleaseDocument)
							.Is(g => g.OnReleaseDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.OpenDocument)
							.Is(g => g.OnOpenDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.CloseDocument)
							.Is(g => g.OnCloseDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.CancelDocument)
							.Is(g => g.OnCancelDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.VoidDocument)
							.Is(g => g.OnVoidDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
					.ForbidFurtherChanges()
				);
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<ARInvoice>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<ARInvoice>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<ARInvoice.hold, ARInvoice.creditHold,
						ARInvoice.printed, ARInvoice.dontPrint,
						ARInvoice.emailed, ARInvoice.dontEmail,
						ARInvoice.pendingProcessing>(e.Row, e.OldRow))
					{
						ARInvoice.Events.Select(ev => ev.UpdateStatus).FireOn(g, (ARInvoice)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion

	}
}