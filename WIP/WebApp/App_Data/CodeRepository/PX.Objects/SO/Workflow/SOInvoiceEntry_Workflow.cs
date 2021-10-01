using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.AR;
using PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt;

namespace PX.Objects.SO
{
	using State = ARDocStatus;
	using static ARInvoice;
	using static BoundedTo<SOInvoiceEntry, ARInvoice>;

	public class SOInvoiceEntry_Workflow : PXGraphExtension<SOInvoiceEntry>
	{
		[PXWorkflowDependsOnType(typeof(ARSetup))]
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<SOInvoiceEntry, ARInvoice>());

		protected virtual void Configure(WorkflowContext<SOInvoiceEntry, ARInvoice> context)
		{
			var _Definition = ARSetupDefinition.GetSlot();
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<released.IsEqual<False>.And<hold.IsEqual<True>>>(),

				IsOnCreditHold
					= Bql<released.IsEqual<False>.And<creditHold.IsEqual<True>>>(),
				
				IsReleased
					= Bql<released.IsEqual<True>.And<openDoc.IsEqual<True>>>(),

				IsClosed
					= Bql<released.IsEqual<True>.And<openDoc.IsEqual<False>>>(),

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

				IsPendingProcessingPure = Bql<pendingProcessing.IsEqual<True>.And<origModule.IsEqual<GL.BatchModule.moduleSO>>>(),
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
						:_Definition.PrintBeforeRelease == true
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
				IsCreditType
					= Bql<docType.IsIn<ARDocType.creditMemo, ARDocType.cashReturn, ARDocType.cashSale>>(),
			}.AutoNameConditions();

			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(flow =>
					{
						return flow
							.WithFlowStates(flowStates =>
							{
								flowStates.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
								flowStates.Add<State.hold>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.releaseFromHold, act => act.IsDuplicatedInToolbar());
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.recalculateDiscountsAction);
										});
								});
								flowStates.Add<State.cCHold>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.putOnHold);
											actions.Add(g => g.putOnCreditHold);
											actions.Add(g => g.printInvoice);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.recalculateDiscountsAction);
											actions.Add<CreatePaymentExt>(e => e.createAndCapturePayment);
										});
								});
								flowStates.Add<State.creditHold>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.releaseFromCreditHold, act => act.IsDuplicatedInToolbar());
											actions.Add(g => g.putOnHold, act => act.IsDuplicatedInToolbar());
											actions.Add(g => g.validateAddresses);
											actions.Add<CreatePaymentExt>(e => e.createAndCapturePayment);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnUpdateStatus);
										});
								});
								flowStates.Add<State.pendingPrint>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.printInvoice, act => act.IsDuplicatedInToolbar());
											actions.Add(g => g.emailInvoice);
											actions.Add(g => g.putOnHold);
											actions.Add(g => g.putOnCreditHold);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.recalculateDiscountsAction);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnUpdateStatus);
										});;
								});
								flowStates.Add<State.pendingEmail>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.emailInvoice, act => act.IsDuplicatedInToolbar());
											actions.Add(g => g.printInvoice);
											actions.Add(g => g.putOnHold);
											actions.Add(g => g.putOnCreditHold);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.recalculateDiscountsAction);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnUpdateStatus);
										});
								});
								flowStates.Add<State.balanced>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.release, act => act.IsDuplicatedInToolbar());
											actions.Add(g => g.putOnHold, act => act.IsDuplicatedInToolbar());
											actions.Add(g => g.putOnCreditHold);
											actions.Add(g => g.printInvoice);
											actions.Add(g => g.emailInvoice);
											actions.Add(g => g.arEdit);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.recalculateDiscountsAction);
											actions.Add<CreatePaymentExt>(e => e.createAndCapturePayment);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnUpdateStatus);
										});;
								});
								flowStates.Add<State.open>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.post);
											actions.Add(g => g.writeOff);
											actions.Add(g => g.payInvoice);
											actions.Add(g => g.printInvoice);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.reclassifyBatch);
											actions.Add<Correction>(g => g.cancelInvoice);
											actions.Add<Correction>(g => g.correctInvoice);
										});
								});
								flowStates.Add<State.closed>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.post);
											actions.Add(g => g.printInvoice);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.reclassifyBatch);
											actions.Add<Correction>(g => g.cancelInvoice);
											actions.Add<Correction>(g => g.correctInvoice);
										})
										.WithFieldStates(states =>
										{
											states.AddTable<ARInvoice>(state => state.IsDisabled());
										});
								});
								flowStates.Add<State.canceled>(flowState =>
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
							})
							.WithTransitions(transitions =>
							{
								transitions.AddGroupFrom(initialState, ts =>
								{
									ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold));
									ts.Add(t => t.To<State.creditHold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnCreditHold));
									ts.Add(t => t.To<State.pendingPrint>().IsTriggeredOn(g => g.initializeState).When(conditions.IsPendingPrint));
									ts.Add(t => t.To<State.pendingEmail>().IsTriggeredOn(g => g.initializeState).When(conditions.IsPendingEmail));
									ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.initializeState).When(conditions.IsBalanced));
									ts.Add(t => t.To<State.cCHold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsPendingProcessing));
									ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState).When(conditions.IsReleased));
									ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.initializeState).When(conditions.IsClosed));
								});
								transitions.AddGroupFrom<State.hold>(ts =>
								{
									ts.Add(t => t.To<State.pendingPrint>().IsTriggeredOn(g => g.releaseFromHold).When(conditions.IsPendingPrint));
									ts.Add(t => t.To<State.pendingEmail>().IsTriggeredOn(g => g.releaseFromHold).When(conditions.IsPendingEmail));
									ts.Add(t => t.To<State.cCHold>().IsTriggeredOn(g => g.releaseFromHold).When(conditions.IsPendingProcessing));
									ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.releaseFromHold).When(conditions.IsBalanced));
								});
								transitions.AddGroupFrom<State.creditHold>(ts =>
								{
									ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold));
									ts.Add(t => t.To<State.pendingPrint>().IsTriggeredOn(g => g.releaseFromCreditHold).When(conditions.IsPendingPrint));
									ts.Add(t => t.To<State.pendingEmail>().IsTriggeredOn(g => g.releaseFromCreditHold).When(conditions.IsPendingEmail));
									ts.Add(t => t.To<State.cCHold>().IsTriggeredOn(g => g.releaseFromCreditHold).When(conditions.IsPendingProcessing));
									ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.releaseFromCreditHold).When(conditions.IsBalanced));
									ts.Add(t => t.To<State.cCHold>().IsTriggeredOn(g => g.OnUpdateStatus).When(conditions.IsPendingProcessingPure));
								});
								transitions.AddGroupFrom<State.pendingPrint>(ts =>
								{
									ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold));
									ts.Add(t => t.To<State.creditHold>().IsTriggeredOn(g => g.putOnCreditHold));
									ts.Add(t => t.To<State.pendingEmail>().IsTriggeredOn(g => g.printInvoice).When(conditions.IsPendingEmail));
									ts.Add(t => t.To<State.cCHold>().IsTriggeredOn(g => g.printInvoice).When(conditions.IsPendingProcessing));
									ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.printInvoice).When(conditions.IsBalanced));
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
								transitions.AddGroupFrom<State.pendingEmail>(ts =>
								{
									ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold));
									ts.Add(t => t.To<State.creditHold>().IsTriggeredOn(g => g.putOnCreditHold));
									ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.emailInvoice));
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
								});
								transitions.AddGroupFrom<State.cCHold>(ts =>
								{
									ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold));
									ts.Add(t => t.To<State.creditHold>().IsTriggeredOn(g => g.putOnCreditHold));
								});
								transitions.AddGroupFrom<State.balanced>(ts =>
								{
									ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold));
									ts.Add(t => t.To<State.creditHold>().IsTriggeredOn(g => g.putOnCreditHold));
									ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.release).When(conditions.IsReleased));
									ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.release).When(conditions.IsClosed));
									ts.Add(t => t
										.To<State.pendingPrint>()
										.IsTriggeredOn(g => g.OnUpdateStatus)
										.When(conditions.IsPendingPrint));
									ts.Add(t => t
										.To<State.pendingEmail>()
										.IsTriggeredOn(g => g.OnUpdateStatus)
										.When(conditions.IsPendingEmail));
								});
								transitions.AddGroupFrom<State.open>(ts =>
								{
									//ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.OnApplicationReleased).When(conditions.IsClosed));
								});
								transitions.AddGroupFrom<State.closed>(ts =>
								{
									// terminal status
								});
							});
					})
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder)
							.MassProcessingScreen<SOReleaseInvoice>()
							.InBatchMode());
						actions.Add(g => g.post, c => c
							.InFolder(FolderType.ActionsFolder)
							.MassProcessingScreen<SOReleaseInvoice>()
							.InBatchMode());

						actions.Add(g => g.putOnHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fass => fass.Add<hold>(true)));
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fass => fass.Add<hold>(false)));

						actions.Add(g => g.putOnCreditHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsCreditType)
							.WithFieldAssignments(fass =>
							{
								fass.Add<hold>(false);
								fass.Add<creditHold>(true);
								fass.Add<approvedCredit>(false);
								fass.Add<approvedCreditAmt>(0);
								fass.Add<approvedCaptureFailed>(false);
								fass.Add<approvedPrepaymentRequired>(false);
							}));
						actions.Add(g => g.releaseFromCreditHold, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsCreditType)
							.MassProcessingScreen<SOReleaseInvoice>()
							.WithFieldAssignments(fass =>
							{
								fass.Add<creditHold>(false);
							}));

						actions.Add(g => g.emailInvoice, c => c
							.InFolder(FolderType.ActionsFolder)
							.MassProcessingScreen<SOReleaseInvoice>()
							.WithFieldAssignments(fass => fass.Add<emailed>(true)));

						actions.Add(g => g.recalculateDiscountsAction, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.writeOff, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.reclassifyBatch, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.payInvoice, c => c
							.InFolder(FolderType.ActionsFolder));

						actions.Add<Correction>(g => g.cancelInvoice, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add<Correction>(g => g.correctInvoice, c => c
							.InFolder(FolderType.ActionsFolder));

						actions.Add<CreatePaymentExt>(e => e.createAndCapturePayment, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenAlways() // only for mass processing
							.MassProcessingScreen<SOReleaseInvoice>());

						actions.Add(g => g.validateAddresses, c => c
							.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.arEdit, c => c
							.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.printInvoice, c => c
							.InFolder(FolderType.ReportsFolder)
							.MassProcessingScreen<SOReleaseInvoice>()
							.InBatchMode()
							.WithFieldAssignments(fass => fass.Add<printed>(true)));
						actions.Add(g => g.reverseInvoiceAndApplyToMemo, c=> c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenAlways());
						actions.Add(g => g.sendEmail, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenAlways());
						actions.Add(g => g.printAREdit, c=> c
							.InFolder(FolderType.ReportsFolder)
							.IsHiddenAlways());
						actions.Add(g => g.printARRegister, c=> c
							.InFolder(FolderType.ReportsFolder)
							.IsHiddenAlways());
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.UpdateStatus)
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity()
							.DisplayName("Invoice Updated"));
					});
			});
		}
	}
}