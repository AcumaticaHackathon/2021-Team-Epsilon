using System;
using System.Collections.Generic;
using System.Data;
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
	using static APPayment;
	using static BoundedTo<APPaymentEntry, APPayment>;

	public partial class APPaymentEntry_Workflow : PXGraphExtension<APPaymentEntry>
	{

		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<APPaymentEntry, APPayment>());

		protected virtual void Configure(WorkflowContext<APPaymentEntry, APPayment> context)
		{
			#region Conditions

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>.And<released.IsEqual<False>>>(),
				IsReserved
					= Bql<hold.IsEqual<True>.And<released.IsEqual<True>>>(),
				IsNotPrintable
					= Bql<docType.IsNotIn<APDocType.check, APDocType.prepayment>>(),
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
					= Bql<hold.IsEqual<False>.And<printCheck.IsEqual<False>.Or<printed.IsEqual<True>>>>(),
				IsBalanced
					= Bql<hold.IsEqual<False>
						.And<released.IsEqual<False>>
						.And<APRegister.approved.IsEqual<True>>
						.And<printCheck.IsEqual<False>.Or<printed.IsEqual<True>>>>(),
				IsVoidHidden
					= Bql<docType.IsIn<APDocType.voidCheck, APDocType.voidRefund, APDocType.debitAdj>>(),
				IsDebitAdj
					= Bql<docType.IsEqual<APDocType.debitAdj>>(),

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
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnUpdateStatus);
										});
									});
							fss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
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
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnPrintCheck);
										handlers.Add(g => g.OnUpdateStatus);
										});
							});
							fss.Add<State.printed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.validateAddresses);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnCancelPrintCheck);
									});
							});
							fss.Add<State.prebooked>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnVoidDocument);
										handlers.Add(g => g.OnCloseDocument);
										});
										});

							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.voidCheck);
										actions.Add(g => g.reverseApplication);
										actions.Add(g => g.initializeState, act => act.IsAutoAction());
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnVoidDocument);
										handlers.Add(g => g.OnCloseDocument);
										});
										});
							fss.Add<State.reserved>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
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
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.reverseApplication);
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnVoidDocument);
										handlers.Add(g => g.OnOpenDocument);
										});
							});
							fss.Add<State.voided>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.printAPRegister);
									});
							});
						})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)
								.DoesNotPersist()); // New Hold
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsNotPrinted)
								.DoesNotPersist()); // Pending Print
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsBalanced)
								.DoesNotPersist()); // New Balance
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOpen)); // New Open
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsClosed)); // New Closed
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.releaseFromHold)
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
						transitions.AddGroupFrom<State.pendingPrint>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t
								.To<State.printed>()
								.IsTriggeredOn(g => g.OnPrintCheck));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsPrinted));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
						});
						transitions.AddGroupFrom<State.printed>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.release)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.release)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.OnCancelPrintCheck)
								.WithFieldAssignments(fas =>
									{
										fas.Add<hold>(f => f.SetFromValue(false));
										fas.Add<printed>(f => f.SetFromValue(false));
										fas.Add<extRefNbr>(f => f.SetFromValue(null));
									}
								));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t
								.To<State.pendingPrint>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsNotPrinted));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
						});
						transitions.AddGroupFrom<State.prebooked>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnCloseDocument));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.reserved>()
								.IsTriggeredOn(g => g.putOnHold));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnCloseDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsClosed));
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
								.To<State.open>()
								.IsTriggeredOn(g => g.OnOpenDocument));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reverseApplication)
								.DoesNotPersist());
						});
					}
					))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a
							.IsHiddenAlways());
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
							.IsHiddenWhen(conditions.IsNotPrintable));
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.voidCheck, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsVoidHidden));
						actions.Add(g => g.validateAddresses, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.reverseApplication, g=> g
							.WithPersistOptions(ActionPersistOptions.NoPersist)
						);
						actions.Add(g => g.vendorDocuments, c => c.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.printAPEdit, c => c
							.InFolder(FolderType.ReportsFolder)
							.IsHiddenWhen(conditions.IsDebitAdj));
						actions.Add(g => g.printAPRegister, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printAPPayment, c => c
							.InFolder(FolderType.ReportsFolder)
							.IsHiddenWhen(conditions.IsDebitAdj));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.PrintCheck)
							.Is(g => g.OnPrintCheck)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.CancelPrintCheck)
							.Is(g => g.OnCancelPrintCheck)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.ReleaseDocument)
							.Is(g => g.OnReleaseDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.VoidDocument)
							.Is(g => g.OnVoidDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.OpenDocument)
							.Is(g => g.OnOpenDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.CloseDocument)
							.Is(g => g.OnCloseDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
					.ForbidFurtherChanges()
				);
		}
		#region Update Workflow Status
		public class PXUpdateStatus : PXSelect<APPayment>
		{
			public PXUpdateStatus(PXGraph graph)
				: base(graph)
			{
				graph.Initialized += g => g.RowUpdated.AddHandler<APPayment>((PXCache sender, PXRowUpdatedEventArgs e) =>
				{
					if (!sender.ObjectsEqual<APPayment.hold, APPayment.printCheck, APPayment.printed>(e.Row, e.OldRow))
					{
						APPayment.Events.Select(ev => ev.UpdateStatus).FireOn(g, (APPayment)e.Row);
					}
				});
			}
		}
		public PXUpdateStatus updateStatus;
		#endregion
	}
}