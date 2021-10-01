using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.FS
{
    using State = ListField.AppointmentStatus;
    using static FSAppointment;
    using static BoundedTo<AppointmentEntry, FSAppointment>;

    public class AppointmentEntryWorkflow : PXGraphExtension<AppointmentEntry>
    {
        // workflow works without checking active
        public static bool IsActive() => false;

        public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<AppointmentEntry, FSAppointment>());

        protected virtual void Configure(WorkflowContext<AppointmentEntry, FSAppointment> context)
        {
            #region Conditions
            BoundedTo<AppointmentEntry, FSAppointment>.Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
            var conditions = new
            {
                IsOnHold
                    = Bql<hold.IsEqual<True>>(),

                IsNotOnHold
                    = Bql<hold.IsEqual<False>>(),

                IsNotStarted
                    = Bql<notStarted.IsEqual<True>>(),

                IsCompleted
                    = Bql<completed.IsEqual<True>>(),

                IsCanceled
                    = Bql<canceled.IsEqual<True>>(),

                IsCLosed
                    = Bql<closed.IsEqual<True>>(),

                IsBilled
                    = Bql<billed.IsEqual<True>>(),

                IsInProcess
                    = Bql<inProcess.IsEqual<True>>(),

                IsPaused
                    = Bql<paused.IsEqual<True>>(),

                IsConfirmed
                    = Bql<confirmed.IsEqual<True>>(),

                IsClosed
                    = Bql<closed.IsEqual<True>>(),

                IsSigned
                    = Bql<customerSignedReport.IsNotNull>(),

                IsWaitingForContractPeriod
                    = Bql<awaiting.IsEqual<True>>(),

                BilledByServiceContract
                    = Bql<billServiceContractID.IsNotNull>(),

                IsInternalBehavior
                    = Bql<FSSrvOrdType.behavior.FromCurrent.IsEqual<FSSrvOrdType.behavior.Values.internalAppointment>>(),

                PostToSOSIPM
                   = Bql<FSSrvOrdType.postToSOSIPM.FromCurrent.IsEqual<True>>(),

                PostToProjects
                   = Bql<FSSrvOrdType.postTo.FromCurrent.IsEqual<FSPostTo.Projects>>(),

                CustomerIsSpecified
                    = Bql<customerID.IsGreater<Zero>>(),

                IsTravelInProcess
                    = Bql<travelInProcess.IsEqual<True>>(),

                UserConfirmedUnclosing
                    = Bql<userConfirmedUnclosing.IsEqual<True>>(),

                CustomerIsProspect
                    = Bql<CR.BAccount.type.FromCurrent.IsEqual<CR.BAccountType.prospectType>>(),

                IsFinalState
                    = Bql<canceled.IsEqual<True>.Or<closed.IsEqual<True>.Or<hold.IsEqual<True>
                          .Or<billed.IsEqual<True>.Or<awaiting.IsEqual<True>>>>>>(),

                IsAllowedForInvoice
                    = Bql<closed.IsEqual<True>
                          .Or<
                             Where<
                             closed.IsEqual<False>
                                   .And<completed.IsEqual<True>
                                   .And<FSSrvOrdType.allowInvoiceOnlyClosedAppointment.FromCurrent.IsEqual<False>
                                   .And<timeRegistered.IsEqual<True>>>>>>>(),

                ServiceOrderIsAllowedForInvoice
                    = Bql<FSBillingCycle.invoiceOnlyCompletedServiceOrder.FromCurrent.IsEqual<False>
                         .Or<
                             Where<FSBillingCycle.invoiceOnlyCompletedServiceOrder.IsEqual<True>
                                   .And<
                                       Where<FSServiceOrder.completed.FromCurrent.IsEqual<True>
                                             .Or<FSServiceOrder.closed.FromCurrent.IsEqual<True>>>>>>>(),

                IsQuickProcessEnabled
                    = Bql<FSSrvOrdType.allowQuickProcess.FromCurrent.IsEqual<True>>(),

                IsBilledByAppointment
                    = Bql<FSServiceOrder.billingBy.FromCurrent.IsEqual<FSServiceOrder.billingBy.Values.Appointment>>(),

                IsBilledByContract
                    = Bql<billServiceContractID.IsNotNull>(),

                HasNoLinesToCreatePurchaseOrder
                   = Bql<pendingPOLineCntr.IsEqual<Zero>>(),

                HasNoStaffLines
                   = Bql<employeeLineCntr.IsEqual<Zero>>(),

                RoomFeatureEnabled
                   = Bql<FSSetup.manageRooms.FromCurrent.IsEqual<True>>(),

                IsExpenseFeatureEnabled 
                   = Bql<FeatureInstalled<FeaturesSet.expenseManagement>>(),

                IsInserted
                   = Bql<appointmentID.IsLess<Zero>>(),

                TravelCanBeStarted
                   = Bql<FSAppointment.travelCanBeStarted.IsEqual<True>>(),
            }.AutoNameConditions();
            #endregion

            #region Macros

            void DisableWholeScreen(FieldState.IContainerFillerFields states)
            {
                states.AddTable<FSAppointment>(state => state.IsDisabled());
                states.AddTable<FSServiceOrder>(state => state.IsDisabled());

                states.AddTable<FSAppointmentDet>(state => state.IsDisabled());
                states.AddTable<FSProfitability>(state => state.IsDisabled());
                states.AddTable<FSApptLineSplit>(state => state.IsDisabled());
                states.AddTable<FSAppointmentResource>(state => state.IsDisabled());
                states.AddTable<FSAppointmentLog>(state => state.IsDisabled());
                states.AddTable<FSAddress>(state => state.IsDisabled());
                states.AddTable<FSContact>(state => state.IsDisabled());
                states.AddTable<FSAppointmentEmployee>(state => state.IsDisabled());
                states.AddTable<FSAppointmentTax>(state => state.IsDisabled());
                states.AddTable<FSAppointmentTaxTran>(state => state.IsDisabled());
                // REVIEW AC-178771
                //states.AddTable<*>(state => state.IsDisabled());
            }
            #endregion

            #region Event Handlers
            WorkflowEventHandlerDefinition.IHandlerConfiguredBase OnServiceContractCleared(WorkflowEventHandlerDefinition.INeedEventTarget handler)
            {
                return handler
                    .WithTargetOf<FSAppointment>()
                    .OfEntityEvent<Events>(e => e.ServiceContractCleared)
                    .Is(g => g.OnServiceContractCleared)
                    .UsesTargetAsPrimaryEntity()
                    .DisplayName("Service Contract Cleared");
            }

            WorkflowEventHandlerDefinition.IHandlerConfiguredBase OnServiceContractPeriodAssigned(WorkflowEventHandlerDefinition.INeedEventTarget handler)
            {
                return handler
                    .WithTargetOf<FSAppointment>()
                    .OfEntityEvent<Events>(e => e.ServiceContractPeriodAssigned)
                    .Is(g => g.OnServiceContractPeriodAssigned)
                    .UsesTargetAsPrimaryEntity()
                    .DisplayName("Service Contract Period Assigned");
            }

            WorkflowEventHandlerDefinition.IHandlerConfiguredBase OnRequiredServiceContractPeriodCleared(WorkflowEventHandlerDefinition.INeedEventTarget handler)
            {
                return handler
                    .WithTargetOf<FSAppointment>()
                    .OfEntityEvent<Events>(e => e.RequiredServiceContractPeriodCleared)
                    .Is(g => g.OnRequiredServiceContractPeriodCleared)
                    .UsesTargetAsPrimaryEntity()
                    .DisplayName("Required Service Contract Period Cleared");
            }

            WorkflowEventHandlerDefinition.IHandlerConfiguredBase OnAppointmentUnposted(WorkflowEventHandlerDefinition.INeedEventTarget handler)
            {
                return handler
                    .WithTargetOf<FSAppointment>()
                    .OfEntityEvent<Events>(e => e.AppointmentUnposted)
                    .Is(g => g.OnAppointmentUnposted)
                    .UsesTargetAsPrimaryEntity()
                    .DisplayName("Appointment Unposted");
            }

            WorkflowEventHandlerDefinition.IHandlerConfiguredBase OnAppointmentPosted(WorkflowEventHandlerDefinition.INeedEventTarget handler)
            {
                return handler
                    .WithTargetOf<FSAppointment>()
                    .OfEntityEvent<Events>(e => e.AppointmentPosted)
                    .Is(g => g.OnAppointmentPosted)
                    .UsesTargetAsPrimaryEntity()
                    .DisplayName("Appointment Posted");
            }
            #endregion

            Workflow.IConfigured SimpleAppointmentFlow(Workflow.INeedStatesFlow flow)
            {
                return flow
                     .WithFlowStates(flowStates =>
                     {
                         flowStates.Add(State.Initial, fs => fs.IsInitial(g => g.initializeState));
                         flowStates.Add<State.notStarted>(flowState =>
                         {
                             return flowState
                                 .WithActions(actions =>
                                 {
                                     actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.startTravel, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.completeTravel, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.startAppointment, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.cancelAppointment);
                                 })
                                 .WithEventHandlers(handlers =>
                                 {
                                     handlers.Add(g => g.OnRequiredServiceContractPeriodCleared);
                                 });
                         });
                         flowStates.Add<State.hold>(flowState =>
                         {
                             return flowState
                                 .WithActions(actions =>
                                 {
                                     actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.cancelAppointment);
                                 })
                                 .WithFieldStates(DisableWholeScreen);
                         });
                         flowStates.Add<State.awaiting>(flowState =>
                         {
                             return flowState
                                 .WithActions(actions =>
                                 {
                                     actions.Add(g => g.cancelAppointment);
                                 })
                                 .WithEventHandlers(handlers =>
                                 {
                                     handlers.Add(g => g.OnServiceContractCleared);
                                     handlers.Add(g => g.OnServiceContractPeriodAssigned);
                                 });
                         });
                         flowStates.Add<State.inProcess>(flowState =>
                         {
                             return flowState
                                 .WithActions(actions =>
                                 {
                                     actions.Add(g => g.startTravel);
                                     actions.Add(g => g.completeTravel, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.pauseAppointment, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.completeAppointment, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.reopenAppointment);
                                 });
                         });
                         flowStates.Add<State.paused>(flowState =>
                         {
                             return flowState
                                 .WithActions(actions =>
                                 {
                                     actions.Add(g => g.startTravel, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.completeTravel, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.resumeAppointment, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.completeAppointment);
                                 });
                         });
                         flowStates.Add<State.completed>(flowState =>
                         {
                             return flowState
                                 .WithActions(actions =>
                                 {
                                     actions.Add(g => g.startTravel, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.completeTravel, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.reopenAppointment);
                                     actions.Add(g => g.closeAppointment, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.invoiceAppointment);
                                     actions.Add<AppointmentEntry.AppointmentQuickProcess>(g => g.quickProcess, a => a.IsDuplicatedInToolbar());
                                 })
                                 .WithEventHandlers(handlers =>
                                 {
                                     handlers.Add(g => g.OnAppointmentPosted);
                                 });
                         });
                         flowStates.Add<State.closed>(flowState =>
                         {
                             return flowState
                                 .WithActions(actions =>
                                 {
                                     actions.Add(g => g.uncloseAppointment, a => a.IsDuplicatedInToolbar());
                                     actions.Add(g => g.invoiceAppointment, a => a.IsDuplicatedInToolbar());
                                     actions.Add<AppointmentEntry.AppointmentQuickProcess>(g => g.quickProcess, a => a.IsDuplicatedInToolbar());
                                 })
                                 .WithEventHandlers(handlers =>
                                 {
                                     handlers.Add(g => g.OnAppointmentPosted);
                                 })
                                 .WithFieldStates(DisableWholeScreen);
                         });
                         flowStates.Add<State.billed>(flowState =>
                         {
                             return flowState
                                 .WithEventHandlers(handlers =>
                                 {
                                     handlers.Add(g => g.OnAppointmentUnposted);
                                 })
                                 .WithFieldStates(DisableWholeScreen);
                         });
                         flowStates.Add<State.canceled>(flowState =>
                         {
                             return flowState
                                 .WithActions(actions =>
                                 {
                                     actions.Add(g => g.reopenAppointment, a => a.IsDuplicatedInToolbar());
                                 })
                                 .WithFieldStates(DisableWholeScreen);
                         });
                     })
                     .WithTransitions(transitions =>
                     {
                         transitions.AddGroupFrom(State.Initial, ts =>
                         {
                             ts.Add(t => t
                                        .To<State.hold>()
                                        .IsTriggeredOn(g => g.initializeState)
                                        .When(conditions.IsOnHold)
                                        .WithFieldAssignments(fas =>
                                        {
                                            fas.Add<notStarted>(f => f.SetFromValue(false));
                                        }));
                             ts.Add(t => t
                                        .To<State.notStarted>()
                                        .IsTriggeredOn(g => g.initializeState)
                                        .When(conditions.IsNotOnHold)
                                        .WithFieldAssignments(fas =>
                                        {
                                            fas.Add<notStarted>(f => f.SetFromValue(true));
                                        }));
                         });
                         transitions.AddGroupFrom<State.notStarted>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.hold>()
                                         .IsTriggeredOn(g => g.putOnHold));

                             ts.Add(t => t
                                         .To<State.inProcess>()
                                         .IsTriggeredOn(g => g.startAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<notStarted>(f => f.SetFromValue(false));
                                             fas.Add<inProcess>(f => f.SetFromValue(true));
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<startActionRunning>(f => f.SetFromValue(false));
                                         }));

                             ts.Add(t => t
                                         .To<State.canceled>()
                                         .IsTriggeredOn(g => g.cancelAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<cancelActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<notStarted>(f => f.SetFromValue(false));
                                             fas.Add<canceled>(f => f.SetFromValue(true));
                                         }));
                             ts.Add(t => t
                                         .To<State.awaiting>()
                                         .IsTriggeredOn(g => g.OnRequiredServiceContractPeriodCleared)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<awaiting>(f => f.SetFromValue(true));
                                         }));
                         });
                         transitions.AddGroupFrom<State.hold>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.notStarted>()
                                         .IsTriggeredOn(g => g.releaseFromHold));
                             ts.Add(t => t
                                         .To<State.canceled>()
                                         .IsTriggeredOn(g => g.cancelAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<cancelActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<hold>(f => f.SetFromValue(false));
                                             fas.Add<canceled>(f => f.SetFromValue(true));
                                         }));
                         });
                         transitions.AddGroupFrom<State.awaiting>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.canceled>()
                                         .IsTriggeredOn(g => g.cancelAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<cancelActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<awaiting>(f => f.SetFromValue(false));
                                             fas.Add<canceled>(f => f.SetFromValue(true));
                                         }));
                             ts.Add(t => t
                                         .To<State.notStarted>()
                                         .IsTriggeredOn(g => g.OnServiceContractCleared)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<awaiting>(f => f.SetFromValue(false));
                                         }));
                             ts.Add(t => t
                                         .To<State.notStarted>()
                                         .IsTriggeredOn(g => g.OnServiceContractPeriodAssigned)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<awaiting>(f => f.SetFromValue(false));
                                         }));
                         });
                         transitions.AddGroupFrom<State.inProcess>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.notStarted>()
                                         .IsTriggeredOn(g => g.reopenAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<reopenActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<inProcess>(f => f.SetFromValue(false));
                                             fas.Add<notStarted>(f => f.SetFromValue(true));
                                         }));
                             ts.Add(t => t
                                         .To<State.paused>()
                                         .IsTriggeredOn(g => g.pauseAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<pauseActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<inProcess>(f => f.SetFromValue(false));
                                             fas.Add<paused>(f => f.SetFromValue(true));
                                         }));
                             ts.Add(t => t
                                         .To<State.completed>()
                                         .IsTriggeredOn(g => g.completeAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<completeActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<reloadServiceOrderRelated>(f => f.SetFromValue(true));
                                             fas.Add<inProcess>(f => f.SetFromValue(false));
                                             fas.Add<completed>(f => f.SetFromValue(true));
                                         }));
                         });
                         transitions.AddGroupFrom<State.paused>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.inProcess>()
                                         .IsTriggeredOn(g => g.resumeAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<resumeActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<paused>(f => f.SetFromValue(false));
                                             fas.Add<inProcess>(f => f.SetFromValue(true));
                                         }));
                             ts.Add(t => t
                                         .To<State.completed>()
                                         .IsTriggeredOn(g => g.completeAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<reloadServiceOrderRelated>(f => f.SetFromValue(true));
                                             fas.Add<completeActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<paused>(f => f.SetFromValue(false));
                                             fas.Add<completed>(f => f.SetFromValue(true));
                                         }));
                         });
                         transitions.AddGroupFrom<State.completed>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.notStarted>()
                                         .IsTriggeredOn(g => g.reopenAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<reloadServiceOrderRelated>(f => f.SetFromValue(true));
                                             fas.Add<reopenActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<completed>(f => f.SetFromValue(false));
                                             fas.Add<notStarted>(f => f.SetFromValue(true));
                                         }));
                             ts.Add(t => t
                                         .To<State.closed>()
                                         .IsTriggeredOn(g => g.closeAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<reloadServiceOrderRelated>(f => f.SetFromValue(true));
                                             fas.Add<closeActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<closed>(f => f.SetFromValue(true));
                                         }));
                             ts.Add(t => t
                                        .To<State.billed>()
                                        .IsTriggeredOn(g => g.OnAppointmentPosted)
                                        .WithFieldAssignments(fas =>
                                        {
                                            fas.Add<closed>(f => f.SetFromValue(true));
                                            fas.Add<billed>(f => f.SetFromValue(true));
                                        }));
                         });
                         transitions.AddGroupFrom<State.closed>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.completed>()
                                         .IsTriggeredOn(g => g.uncloseAppointment)
                                         .When(conditions.UserConfirmedUnclosing)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<closed>(f => f.SetFromValue(false));
                                             fas.Add<reloadServiceOrderRelated>(f => f.SetFromValue(true));
                                             fas.Add<unCloseActionRunning>(f => f.SetFromValue(false));
                                         }));

                             ts.Add(t => t
                                        .To<State.billed>()
                                        .IsTriggeredOn(g => g.OnAppointmentPosted)
                                        .WithFieldAssignments(fas =>
                                        {
                                            fas.Add<billed>(f => f.SetFromValue(true));
                                        }));
                         });
                         transitions.AddGroupFrom<State.billed>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.closed>()
                                         .IsTriggeredOn(g => g.OnAppointmentUnposted)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<billed>(f => f.SetFromValue(false));
                                         }));
                         });
                         transitions.AddGroupFrom<State.canceled>(ts =>
                         {
                             ts.Add(t => t
                                         .To<State.notStarted>()
                                         .IsTriggeredOn(g => g.reopenAppointment)
                                         .WithFieldAssignments(fas =>
                                         {
                                             fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(false));
                                             fas.Add<reopenActionRunning>(f => f.SetFromValue(false));
                                             fas.Add<canceled>(f => f.SetFromValue(false));
                                             fas.Add<notStarted>(f => f.SetFromValue(true));
                                         }));
                         });
                     });
            }

            context.AddScreenConfigurationFor(screen =>
            {
                return screen
                    .StateIdentifierIs<status>()
                    .FlowTypeIdentifierIs<workflowTypeID>()
                    .WithFlows(flows =>
                    {
                        flows.AddDefault(SimpleAppointmentFlow);
                        flows.Add<ListField.ServiceOrderWorkflowTypes.simple>(SimpleAppointmentFlow);
                    })
                    .WithActions(actions =>
                    {
                        actions.Add(g => g.initializeState,
                                    c => c.IsHiddenAlways());

                        actions.Add(g => g.putOnHold,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .DoesNotPersist()
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<hold>(f => f.SetFromValue(true));
                                              fas.Add<notStarted>(f => f.SetFromValue(false));
                                          }));

                        actions.Add(g => g.releaseFromHold,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .DoesNotPersist()
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<hold>(f => f.SetFromValue(false));
                                              fas.Add<notStarted>(f => f.SetFromValue(true));
                                          }));

                        actions.Add(g => g.startTravel,
                                    c => c.InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.completeTravel,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .IsDisabledWhen(!conditions.IsTravelInProcess));

                        actions.Add(g => g.startAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(true));
                                              fas.Add<startActionRunning>(f => f.SetFromValue(true));
                                          }));

                        actions.Add(g => g.pauseAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(true));
                                              fas.Add<pauseActionRunning>(f => f.SetFromValue(true));
                                          })
                                          .IsDisabledWhen(conditions.IsNotStarted || conditions.IsCompleted
                                                            || conditions.IsBilled || conditions.IsCLosed
                                                            || conditions.IsInserted || conditions.IsCanceled));

                        actions.Add(g => g.resumeAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(true));
                                              fas.Add<resumeActionRunning>(f => f.SetFromValue(true));
                                          })
                                          .IsDisabledWhen(conditions.IsNotStarted || conditions.IsCompleted
                                                            || conditions.IsBilled || conditions.IsCLosed
                                                            || conditions.IsInProcess || conditions.IsInserted
                                                            || conditions.IsCanceled));

                        actions.Add(g => g.completeAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(true));
                                              fas.Add<completeActionRunning>(f => f.SetFromValue(true));
                                          })
                                          .IsDisabledWhen(conditions.IsNotStarted || conditions.IsCompleted
                                                            || conditions.IsBilled || conditions.IsCLosed
                                                            || conditions.IsCanceled || conditions.IsInserted));

                        actions.Add(g => g.cancelAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<cancelActionRunning>(f => f.SetFromValue(true));
                                          })
                                          .IsDisabledWhen(conditions.IsCompleted || conditions.IsInProcess
                                                            || conditions.IsBilled || conditions.IsCLosed
                                                            || conditions.IsPaused));

                        actions.Add(g => g.reopenAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(true));
                                              fas.Add<reopenActionRunning>(f => f.SetFromValue(true));
                                          })
                                          .IsDisabledWhen(conditions.IsNotStarted || conditions.IsPaused
                                                            || conditions.IsBilled || conditions.IsCLosed
                                                            || conditions.IsBilled));

                        actions.Add(g => g.closeAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<skipExternalTaxCalculation>(f => f.SetFromValue(true));
                                              fas.Add<closeActionRunning>(f => f.SetFromValue(true));
                                          })
                                          .IsDisabledWhen(conditions.IsNotStarted || conditions.IsInProcess
                                                            || conditions.IsPaused || conditions.IsCLosed
                                                            || conditions.IsCanceled || conditions.IsBilled));

                        actions.Add(g => g.uncloseAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .WithFieldAssignments(fas =>
                                          {
                                              fas.Add<unCloseActionRunning>(f => f.SetFromValue(true));
                                          }));

                        actions.Add(g => g.cloneAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder));

                        actions.Add<AppointmentEntry.AppointmentQuickProcess>(g => g.quickProcess,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .IsDisabledWhen(!conditions.IsBilledByAppointment || !conditions.IsQuickProcessEnabled
                                                            || conditions.IsOnHold || conditions.IsCanceled || conditions.IsInProcess || conditions.IsBilledByContract)
                                          .IsHiddenWhen(!conditions.IsBilledByAppointment || !conditions.IsQuickProcessEnabled
                                                            || conditions.IsOnHold || conditions.IsBilledByContract));

                        actions.Add(g => g.invoiceAppointment,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .IsDisabledWhen(conditions.IsBilled || !conditions.IsAllowedForInvoice || !conditions.IsBilledByAppointment
                                                            || !conditions.ServiceOrderIsAllowedForInvoice || conditions.BilledByServiceContract
                                                            || conditions.IsInternalBehavior)
                                          .IsHiddenWhen(!conditions.IsBilledByAppointment || conditions.IsInternalBehavior));

                        actions.Add(g => g.createPurchaseOrder,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .IsDisabledWhen(conditions.HasNoLinesToCreatePurchaseOrder ||conditions.IsFinalState
                                                            || conditions.IsCompleted || conditions.IsBilled));

                        actions.Add(g => g.openEmployeeBoard,
                                   c => c.InFolder(FolderType.ActionsFolder)
                                         .IsDisabledWhen(conditions.IsOnHold || conditions.IsWaitingForContractPeriod
                                                         || conditions.IsCompleted || conditions.IsCanceled));

                        actions.Add(g => g.openUserCalendar,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .IsDisabledWhen(conditions.IsOnHold || conditions.IsWaitingForContractPeriod
                                                         || conditions.IsCompleted || conditions.IsCanceled));

                        actions.Add(g => g.openRoomBoard,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .IsDisabledWhen(conditions.IsOnHold || conditions.IsWaitingForContractPeriod
                                                         || conditions.IsCompleted || conditions.IsCanceled)
                                          .IsHiddenWhen(!conditions.RoomFeatureEnabled));

                        actions.Add(g => g.emailSignedAppointment,
                                         c => c.InFolder(FolderType.ActionsFolder)
                                               .IsDisabledWhen(conditions.IsOnHold || conditions.IsWaitingForContractPeriod
                                                               || !conditions.IsSigned));

                        actions.Add(g => g.emailConfirmationToCustomer,
                                         c => c.InFolder(FolderType.ActionsFolder)
                                               .IsDisabledWhen(conditions.IsOnHold || conditions.IsWaitingForContractPeriod
                                                                || !conditions.IsConfirmed || conditions.HasNoStaffLines
                                                                || conditions.IsCLosed));

                        actions.Add(g => g.emailConfirmationToStaffMember,
                                         c => c.InFolder(FolderType.ActionsFolder)
                                               .IsDisabledWhen(conditions.IsOnHold || conditions.IsWaitingForContractPeriod
                                                                || !conditions.IsConfirmed || conditions.HasNoStaffLines
                                                                || conditions.IsClosed));

                        actions.Add(g => g.emailConfirmationToGeoZoneStaff,
                                         c => c.InFolder(FolderType.ActionsFolder)
                                               .IsDisabledWhen(conditions.IsOnHold || conditions.IsWaitingForContractPeriod
                                                                || !conditions.HasNoStaffLines));

                        actions.Add(g => g.validateAddress,
                                    c => c.InFolder(FolderType.ActionsFolder));

                        actions.Add<AppointmentEntryExternalTax>(
                                    g => g.recalcExternalTax,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .IsDisabledWhen(conditions.IsBilled || conditions.IsCLosed));

                        actions.Add(g => g.billReversal,
                                    c => c.InFolder(FolderType.ActionsFolder)
                                          .IsDisabledWhen(conditions.IsInserted || !conditions.IsBilledByAppointment
                                                           || !conditions.PostToProjects));

                        actions.Add(g => g.printAppointmentReport,
                                    c => c.InFolder(FolderType.ReportsFolder));

                        actions.Add(g => g.printServiceTimeActivityReport,
                                    c => c.InFolder(FolderType.ReportsFolder));

                        actions.Add(g => g.addInvBySite,
                                    c => c.IsDisabledWhen(conditions.IsCanceled || conditions.IsOnHold || conditions.IsBilled 
                                                            || conditions.IsClosed || conditions.IsInternalBehavior || !conditions.CustomerIsSpecified));

                        actions.Add(g => g.addInvSelBySite,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));

                        actions.Add(g => g.addReceipt,
                                    c => c.IsDisabledWhen(conditions.IsCanceled || conditions.IsInternalBehavior || conditions.IsInserted)
                                          .IsHiddenWhen(!conditions.IsExpenseFeatureEnabled));

                        actions.Add(g => g.addBill,
                                    c => c.IsDisabledWhen(conditions.IsCanceled || conditions.IsInternalBehavior || conditions.IsInserted));

                        actions.Add(g => g.openStaffSelectorFromServiceTab,
                                    c => c.IsDisabledWhen(conditions.IsCanceled || conditions.IsOnHold || conditions.IsBilled || conditions.IsClosed));

                        actions.Add(g => g.openStaffSelectorFromStaffTab,
                                    c => c.IsDisabledWhen(conditions.IsCanceled || conditions.IsOnHold || conditions.IsBilled || conditions.IsClosed));

                        actions.Add(g => g.createPrepayment,
                                    c => c.IsDisabledWhen(conditions.IsFinalState || conditions.IsInserted
                                                            || !conditions.PostToSOSIPM || conditions.IsBilledByContract));

                        actions.Add(g => g.openSourceDocument);
                        actions.Add(g => g.createNewCustomer);
                        actions.Add(g => g.viewDirectionOnMap);
                        actions.Add(g => g.OpenPostingDocument);
                        actions.Add(g => g.viewPayment);

                        actions.Add(g => g.startItemLine,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));
                        actions.Add(g => g.pauseItemLine,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));
                        actions.Add(g => g.resumeItemLine,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));
                        actions.Add(g => g.completeItemLine,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));
                        actions.Add(g => g.cancelItemLine,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));

                        actions.Add(g => g.startStaff,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));
                        actions.Add(g => g.pauseStaff,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));
                        actions.Add(g => g.resumeStaff,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));
                        actions.Add(g => g.completeStaff,
                                    c => c.IsDisabledWhen(conditions.IsFinalState));

                        actions.Add(g => g.quickProcessMobile,
                                    c => c.IsDisabledWhen(!conditions.IsBilledByAppointment || !conditions.IsQuickProcessEnabled
                                                            || conditions.IsOnHold || conditions.IsCanceled
                                                            || conditions.IsInProcess || conditions.IsNotStarted
                                                            || conditions.IsBilledByContract));
                        actions.Add(g => g.openScheduleScreen);

                        actions.Add(g => g.startTravelMobile,
                                    c => c.IsDisabledWhen(!conditions.TravelCanBeStarted || conditions.IsInserted
                                                            || conditions.IsBilled));
                        actions.Add(g => g.completeTravelMobile,
                                    c => c.IsDisabledWhen(!conditions.IsTravelInProcess));

                        actions.Add(g => g.startServiceMobile);
                        actions.Add(g => g.startForAssignedStaffMobile);
                        actions.Add(g => g.completeServiceMobile);
                        actions.Add(g => g.pauseServiceMobile);
                        actions.Add(g => g.resumeServiceMobile);
                        actions.Add(g => g.startStaffMobile);
                        actions.Add(g => g.pauseStaffMobile);
                        actions.Add(g => g.resumeStaffMobile);

                        actions.Add(g => g.createPurchaseOrderMobile,
                                    c => c.IsDisabledWhen(conditions.HasNoLinesToCreatePurchaseOrder || conditions.IsFinalState
                                                            || conditions.IsCompleted));
                    })
                    .WithHandlers(handlers =>
                    {
                        handlers.Add(OnServiceContractCleared);
                        handlers.Add(OnServiceContractPeriodAssigned);
                        handlers.Add(OnRequiredServiceContractPeriodCleared);
                        handlers.Add(OnAppointmentUnposted);
                        handlers.Add(OnAppointmentPosted);
                    });
            });
        }
    }
}