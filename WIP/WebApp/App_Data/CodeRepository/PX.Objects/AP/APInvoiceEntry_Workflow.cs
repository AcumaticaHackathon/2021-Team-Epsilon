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

namespace PX.Objects.AP
{
	using State = APDocStatus;
	using static APInvoice;
	using static BoundedTo<APInvoiceEntry, APInvoice>;
	public class APSetupDefinition : IPrefetchable
	{
		public bool? MigrationMode { get; private set; }
		void IPrefetchable.Prefetch()
		{
			using (PXDataRecord rec =
				PXDatabase.SelectSingle<APSetup>(
					new PXDataField("migrationMode")
					))
			{
				MigrationMode = rec != null ? rec.GetBoolean(0) : false;
			}
		}
		public static APSetupDefinition GetSlot()
		{
			return PXDatabase.GetSlot<APSetupDefinition>(typeof(APSetup).FullName, typeof(APSetup));
		}
	}
	public partial class APInvoiceEntry_Workflow : PXGraphExtension<APInvoiceEntry>
	{
		[PXWorkflowDependsOnType(typeof(APSetup))]
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<APInvoiceEntry, APInvoice>());

		protected virtual void Configure(WorkflowContext<APInvoiceEntry, APInvoice> context)
		{
			#region Conditions
			var _Definition = APSetupDefinition.GetSlot();

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>.And<released.IsEqual<False>>>(),

				IsBalanced
					= Bql<hold.IsEqual<False>.And<APRegister.approved.IsEqual<True>>.And<released.IsEqual<False>>>(),

				IsReserved
					= Bql<hold.IsEqual<True>.And<released.IsEqual<True>>>(),

				IsScheduled
					= Bql<scheduled.IsEqual<True>.And<released.IsEqual<False>>>(),

				IsOpen
					= Bql<openDoc.IsEqual<True>.And<released.IsEqual<True>>>(),

				IsClosed
					= Bql<openDoc.IsEqual<False>.And<released.IsEqual<True>>>(),
				
				IsZeroBalance 
					= Bql<docBal.IsEqual<decimal0>>(),
				
				IsPrepayment
					= Bql<docType.IsEqual<APDocType.prepayment>>(),

				IsNotDebitAdjustment
					= Bql<docType.IsNotEqual<APDocType.debitAdj>>(),

				IsNotAllowRefund
					= Bql<docType.IsNotEqual<APDocType.debitAdj>
						.Or<APRegister.curyRetainageTotal.IsGreater<decimal0>>
						.Or<isRetainageDocument.IsEqual<True>>>(),

				IsNotAllowReclasify
					= Bql<docType.IsEqual<APDocType.prepayment>
						.Or<docType.IsEqual<APDocType.debitAdj>
							.And<APRegister.curyRetainageTotal.IsGreater<decimal0>
								.Or<isRetainageDocument.IsEqual<True>>>>>(),

				IsNotAllowReverce
					= Bql<docType.IsIn<APDocType.prepayment, APDocType.debitAdj>>(),

				IsNotAllowVoidPrepayment
					= Bql<docType.IsNotEqual<APDocType.prepayment>>(),

				IsNotAllowVoidInvoice
					= Bql<docType.IsNotIn<APDocType.invoice, APDocType.creditAdj, APDocType.debitAdj>>(),

				IsRetainage
					= Bql<isRetainageDocument.IsEqual<True>.Or<retainageApply.IsEqual<True>>>(),

				IsNotAllowRecalcPrice
					= Bql<pendingPPD.IsEqual<True>
						.Or<docType.IsEqual<APDocType.debitAdj>
							.And<APRegister.curyRetainageTotal.IsGreater<decimal0>
								.Or<isRetainageDocument.IsEqual<True>>>>>(),

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
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.recalculateDiscountsAction);
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
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.createSchedule);
										actions.Add(g => g.recalculateDiscountsAction);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.scheduled>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.createSchedule, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnVoidSchedule);
									});
							});
							fss.Add<State.voided>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
									});
							});
							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.payInvoice, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.voidDocument);
										actions.Add(g => g.voidInvoice);
										actions.Add(g => g.vendorRefund);
										actions.Add(g => g.reverseInvoice);
										actions.Add(g => g.reclassifyBatch);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnCloseDocument);
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnVoidDocument);
									});
							});

							fss.Add<State.prebooked>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.payInvoice, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.reverseInvoice);
										actions.Add(g => g.voidInvoice);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
									}); ;
							});
							fss.Add<State.printed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.payInvoice, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.voidInvoice);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);

									}); ;
							});
							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.reverseInvoice);
										actions.Add(g => g.reclassifyBatch);
									})
									.WithEventHandlers(handlers =>
										handlers.Add(g => g.OnOpenDocument));
							});
						}
						)
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold)); // New Hold
								ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.initializeState).When(conditions.IsBalanced)); // New Balance
								ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOpen)); // New Open
								ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.initializeState).When(conditions.IsClosed)); // New Closed
								ts.Add(t => t.To<State.reserved>().IsTriggeredOn(g => g.initializeState).When(conditions.IsReserved)); // New Reserved
								ts.Add(t => t.To<State.scheduled>().IsTriggeredOn(g => g.initializeState).When(conditions.IsScheduled)); // New Reserved
							});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
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
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
						});
						transitions.AddGroupFrom<State.prebooked>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.voidInvoice));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
						});
						transitions.AddGroupFrom<State.scheduled>(ts =>
						{
							ts.Add(t => t
								.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<voided>(e => e.SetFromValue(true));
									fas.Add<scheduled>(e => e.SetFromValue(false));
									fas.Add<scheduleID>(e => e.SetFromValue(null));
								}));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.voidInvoice));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.voidDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnCloseDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
						});
						transitions.AddGroupFrom<State.printed>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
						});
						transitions.AddGroupFrom<State.closed>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnOpenDocument));
						});
					}))
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
						actions.Add(g => g.prebook, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsPrepayment || conditions.IsMigrationMode)
							.IsDisabledWhen(conditions.IsRetainage));
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.payInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsMigrationMode)
							.IsDisabledWhen(conditions.IsZeroBalance));
						actions.Add(g => g.vendorRefund, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsNotAllowRefund)
							.IsHiddenWhen(conditions.IsNotDebitAdjustment || conditions.IsMigrationMode));
						actions.Add(g => g.reverseInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsNotAllowReverce)
							.IsHiddenWhen(conditions.IsNotAllowReverce));
						actions.Add(g => g.createSchedule, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.recalculateDiscountsAction, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsMigrationMode)
							.IsDisabledWhen(conditions.IsNotAllowRecalcPrice));
						actions.Add(g => g.voidDocument, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsNotAllowVoidPrepayment)
							.IsHiddenWhen(conditions.IsNotAllowVoidPrepayment || conditions.IsMigrationMode));
						//Complex dependency by PO link & Applications
						actions.Add(g => g.voidInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotAllowVoidInvoice || conditions.IsMigrationMode));
						actions.Add(g => g.reclassifyBatch, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsNotAllowReclasify)
							.IsHiddenWhen(conditions.IsPrepayment || conditions.IsMigrationMode));
						actions.Add(g => g.vendorDocuments, c => c.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.printAPEdit, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printAPRegister, c => c.InFolder(FolderType.ReportsFolder).IsHiddenWhen(conditions.IsPrepayment));

					})
					.WithHandlers(handlers =>
						{
							handlers.Add(handler => handler
								.WithTargetOf<APRegister>()
								.WithParametersOf<GL.Schedule>()
								.OfEntityEvent<APRegister.Events>(e => e.ConfirmSchedule)
								.Is(g => g.OnConfirmSchedule)
								.UsesPrimaryEntityGetter<
									SelectFrom<APInvoice>.
									Where<APInvoice.docType.IsEqual<APRegister.docType.FromCurrent>.
										And<APInvoice.refNbr.IsEqual<APRegister.refNbr.FromCurrent>>>
								>());
							handlers.Add(handler => handler
								.WithTargetOf<APRegister>()
								.WithParametersOf<GL.Schedule>()
								.OfEntityEvent<APRegister.Events>(e => e.VoidSchedule)
								.Is(g => g.OnVoidSchedule)
								.UsesPrimaryEntityGetter<
									SelectFrom<APInvoice>.
									Where<APInvoice.docType.IsEqual<APRegister.docType.FromCurrent>.
										And<APInvoice.refNbr.IsEqual<APRegister.refNbr.FromCurrent>>>
								>());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.OpenDocument)
								.Is(g => g.OnOpenDocument)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.CloseDocument)
								.Is(g => g.OnCloseDocument)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.ReleaseDocument)
								.Is(g => g.OnReleaseDocument)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.VoidDocument)
								.Is(g => g.OnVoidDocument)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.UpdateStatus)
								.Is(g => g.OnUpdateStatus)
								.UsesTargetAsPrimaryEntity());
						})
					.ForbidFurtherChanges()
			);
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<APInvoice>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<APInvoice>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<APInvoice.hold>(e.Row, e.OldRow))
					{
						APInvoice.Events.Select(ev => ev.UpdateStatus).FireOn(g, (APInvoice)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion


	}
}