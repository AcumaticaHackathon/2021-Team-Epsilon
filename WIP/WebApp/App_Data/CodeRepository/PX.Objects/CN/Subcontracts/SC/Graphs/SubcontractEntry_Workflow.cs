using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.PO;

namespace PX.Objects.CN.Subcontracts.SC.Graphs
{
    using static BoundedTo<SubcontractEntry, POOrder>;
    using static POOrder;
    using State = POOrderStatus;

    public class SubcontractEntry_Workflow : PXGraphExtension<SubcontractEntry>
    {
        public class Conditions : Condition.Pack
        {
            public Condition IsOnHold => GetOrCreate(b => b.FromBql<
                hold.IsEqual<True>
            >());

            public Condition IsCancelled => GetOrCreate(b => b.FromBql<
                cancelled.IsEqual<True>
            >());

            public Condition IsPrinted => GetOrCreate(b => b.FromBql<
                printedExt.IsEqual<True>
            >());

            public Condition IsEmailed => GetOrCreate(b => b.FromBql<
                emailedExt.IsEqual<True>
            >());

            public Condition IsChangeOrder => GetOrCreate(b => b.FromBql<
                behavior.IsEqual<POBehavior.changeOrder>
            >());

            public Condition HasAllLinesClosed => GetOrCreate(b => b.FromBql<
                linesToCloseCntr.IsEqual<Zero>
            >());

            public Condition HasAllLinesCompleted => GetOrCreate(b => b.FromBql<
                linesToCloseCntr.IsNotEqual<Zero>
                        .And<linesToCompleteCntr.IsEqual<Zero>>
            >());
        }

        public const string CreatePrepaymentActionName = "createPrepayment";
        public const string UnlinkFromSOActionName = "unlinkFromSO";
        public const string ConvertToNormalActionName = "convertToNormal";
        public const string CreateSalesOrderActionName = "createSalesOrder";
        public const string GenerateSalesOrderActionName = "generateSalesOrder";

        public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<SubcontractEntry, POOrder>());

        protected virtual void Configure(WorkflowContext<SubcontractEntry, POOrder> context)
        {
            var conditions = context.Conditions.GetPack<Conditions>();

            context.AddScreenConfigurationFor(screen =>
            {
                return screen
                    .StateIdentifierIs<status>()
                    .FlowTypeIdentifierIs<orderType>(true)
                    .WithFlows(flows =>
                    {
                        flows.Add<POOrderType.regularSubcontract>(flow =>
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
                                                actions.Add(g => g.validateAddresses);
                                                actions.Add(g => g.recalculateDiscountsAction);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printSubcontract);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>();
                                                fields.AddTable<POLine>();
                                                fields.AddTable<POTax>();
                                                fields.AddAllFields<POShipAddress>();
                                                fields.AddAllFields<POShipContact>();
                                                fields.AddField<POOrder.approved>(c => c.IsDisabled());
                                                fields.AddField<POOrder.ownerID>();
                                                fields.AddField<POOrder.workgroupID>(c => c.IsDisabled());
                                            });
                                    });
                                    states.Add<State.pendingPrint>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.putOnHold);
                                                actions.Add(g => g.markAsDontPrint, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.cancelOrder);
                                                actions.Add(g => g.validateAddresses);

                                                actions.Add(g => g.printSubcontract, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.vendorDetails);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();
                                                fields.AddField<POOrder.dontPrint>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();

                                                fields.AddTable<POTax>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());
                                                fields.AddTable<CurrencyInfo>(c => c.IsDisabled());
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
                                                handlers.Add(g => g.OnLinesCompleted);
                                                handlers.Add(g => g.OnLinesClosed);
                                                handlers.Add(g => g.OnPrinted);
                                                handlers.Add(g => g.OnDoNotPrintChecked);
                                            });
                                    });
                                    states.Add<State.pendingEmail>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.putOnHold);
                                                actions.Add(g => g.cancelOrder);
                                                actions.Add(g => g.validateAddresses);
                                                actions.Add(g => g.emailSubcontract, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.markAsDontEmail, c => c.IsDuplicatedInToolbar());

                                                actions.Add(g => g.printSubcontract);
                                                actions.Add(g => g.vendorDetails);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();
                                                fields.AddField<POOrder.dontEmail>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();

                                                fields.AddTable<POTax>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());
                                                fields.AddTable<CurrencyInfo>(c => c.IsDisabled());
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
                                                handlers.Add(g => g.OnLinesCompleted);
                                                handlers.Add(g => g.OnLinesClosed);
                                                handlers.Add(g => g.OnDoNotEmailChecked);
                                            });
                                    });
                                    states.Add<State.open>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.putOnHold);

                                                actions.Add(g => g.complete);
                                                actions.Add(g => g.cancelOrder);

                                                actions.Add(g => g.emailSubcontract);
                                                actions.Add(g => g.createPOReceipt, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.createAPInvoice, c => c.IsDuplicatedInToolbar());

                                                actions.Add(g => g.validateAddresses);
                                                actions.Add(CreatePrepaymentActionName);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printSubcontract);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();

                                                fields.AddTable<POTax>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());
                                                fields.AddTable<CurrencyInfo>(c => c.IsDisabled());
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
                                                handlers.Add(g => g.OnLinesCompleted);
                                                handlers.Add(g => g.OnLinesClosed);
                                                handlers.Add(g => g.OnReleaseChangeOrder);
                                            });
                                    });
                                    states.Add<State.completed>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.reopenOrder);
                                                actions.Add(g => g.validateAddresses);
                                                actions.Add(g => g.createAPInvoice);
                                                actions.Add(CreatePrepaymentActionName);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printSubcontract);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddTable<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();

                                                fields.AddTable<POTax>(c => c.IsDisabled());

                                                fields.AddAllFields<PORemitAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<PORemitContact>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());

                                                fields.AddTable<CurrencyInfo>(c => c.IsDisabled());
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
                                                handlers.Add(g => g.OnLinesClosed);
                                                handlers.Add(g => g.OnLinesReopened);
                                                handlers.Add(g => g.OnReleaseChangeOrder);
                                            });
                                    });
                                    states.Add<State.cancelled>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.reopenOrder);
                                                actions.Add(g => g.validateAddresses);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printSubcontract);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddTable<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();

                                                fields.AddAllFields<PORemitAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<PORemitContact>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());
                                                fields.AddTable<POTax>(c => c.IsDisabled());

                                                fields.AddTable<CurrencyInfo>(c => c.IsDisabled());
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
                                                handlers.Add(g => g.OnReleaseChangeOrder);
                                            });
                                    });
                                    states.Add<State.closed>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.reopenOrder);
                                                actions.Add(g => g.validateAddresses);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printSubcontract);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddTable<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();

                                                fields.AddAllFields<PORemitAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<PORemitContact>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());
                                                fields.AddTable<POTax>(c => c.IsDisabled());

                                                fields.AddTable<CurrencyInfo>(c => c.IsDisabled());
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
                                                handlers.Add(g => g.OnLinesReopened);
                                                handlers.Add(g => g.OnLinesCompleted);
                                                handlers.Add(g => g.OnReleaseChangeOrder);
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
                                            .To<State.pendingPrint>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(!conditions.IsPrinted));
                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(!conditions.IsEmailed));
                                        ts.Add(t => t
                                            .To<State.cancelled>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(conditions.IsCancelled));
                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(conditions.HasAllLinesClosed));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(conditions.HasAllLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.initializeState));
                                    });
                                    transitions.AddGroupFrom<State.hold>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.pendingPrint>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!conditions.IsOnHold && !conditions.IsPrinted));
                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!conditions.IsOnHold && !conditions.IsEmailed));
                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!conditions.IsOnHold && conditions.HasAllLinesClosed));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!conditions.IsOnHold && conditions.HasAllLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!conditions.IsOnHold));
                                    });
                                    transitions.AddGroupFrom<State.pendingPrint>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.putOnHold)
                                            .When(conditions.IsOnHold));
                                        ts.Add(t => t
                                            .To<State.cancelled>()
                                            .IsTriggeredOn(g => g.cancelOrder)
                                            .When(conditions.IsCancelled));

                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(g => g.OnPrinted)
                                            .When(!conditions.IsEmailed)
                                            .WithFieldAssignments(fas => fas.Add<printed>(true)));
                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.OnPrinted)
                                            .When(conditions.HasAllLinesClosed)
                                            .WithFieldAssignments(fas => fas.Add<printed>(true)));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.OnPrinted)
                                            .When(conditions.HasAllLinesCompleted)
                                            .WithFieldAssignments(fas => fas.Add<printed>(true)));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnPrinted)
                                            .WithFieldAssignments(fas => fas.Add<printed>(true)));

                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(g => g.OnDoNotPrintChecked)
                                            .When(!conditions.IsEmailed)
                                            .DoesNotPersist());
                                        ts.Add(t => t
                                           .To<State.closed>()
                                           .IsTriggeredOn(g => g.OnDoNotPrintChecked)
                                           .When(conditions.HasAllLinesClosed)
                                            .DoesNotPersist());
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.OnDoNotPrintChecked)
                                            .When(conditions.HasAllLinesCompleted)
                                            .DoesNotPersist());
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnDoNotPrintChecked)
                                            .DoesNotPersist());
                                    });
                                    transitions.AddGroupFrom<State.pendingEmail>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.putOnHold)
                                            .When(conditions.IsOnHold));

                                        ts.Add(t => t
                                            .To<State.cancelled>()
                                            .IsTriggeredOn(g => g.cancelOrder)
                                            .When(conditions.IsCancelled));

                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.emailSubcontract)
                                            .When(conditions.HasAllLinesClosed)
                                            .WithFieldAssignments(fas => fas.Add<emailed>(true)));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.emailSubcontract)
                                            .When(conditions.HasAllLinesCompleted)
                                            .WithFieldAssignments(fas => fas.Add<emailed>(true)));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.emailSubcontract)
                                            .WithFieldAssignments(fas => fas.Add<emailed>(true)));

                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.OnDoNotEmailChecked)
                                             .When(conditions.HasAllLinesClosed));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.OnDoNotEmailChecked)
                                            .When(conditions.HasAllLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnDoNotEmailChecked));
                                    });
                                    transitions.AddGroupFrom<State.open>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.putOnHold)
                                            .When(conditions.IsOnHold));
                                        ts.Add(t => t
                                            .To<State.cancelled>()
                                            .IsTriggeredOn(g => g.cancelOrder)
                                            .When(conditions.IsCancelled));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.OnLinesCompleted)
                                            .DoesNotPersist());
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.complete)
                                            .When(conditions.HasAllLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.OnLinesClosed));
                                        ts.Add(t => t
                                            .To<State.pendingPrint>()
                                            .IsTriggeredOn(g => g.OnReleaseChangeOrder)
                                            .When(!conditions.IsPrinted));
                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(g => g.OnReleaseChangeOrder)
                                            .When(!conditions.IsEmailed));
                                    });
                                    transitions.AddGroupFrom<State.cancelled>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.reopenOrder)
                                            .When(conditions.IsOnHold));
                                        ts.Add(t => t
                                            .To<State.pendingPrint>()
                                            .IsTriggeredOn(g => g.OnReleaseChangeOrder)
                                            .When(!conditions.IsPrinted));
                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(g => g.OnReleaseChangeOrder)
                                            .When(!conditions.IsEmailed));
                                    });
                                    transitions.AddGroupFrom<State.completed>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.reopenOrder)
                                            .When(conditions.IsOnHold));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnLinesReopened));
                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.OnLinesClosed));
                                        ts.Add(t => t
                                            .To<State.pendingPrint>()
                                            .IsTriggeredOn(g => g.OnReleaseChangeOrder)
                                            .When(!conditions.IsPrinted));
                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(g => g.OnReleaseChangeOrder)
                                            .When(!conditions.IsEmailed));
                                    });
                                    transitions.AddGroupFrom<State.closed>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.OnLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnLinesReopened));
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.reopenOrder)
                                            .When(conditions.IsOnHold));
                                        ts.Add(t => t
                                            .To<State.pendingPrint>()
                                            .IsTriggeredOn(g => g.OnReleaseChangeOrder)
                                            .When(!conditions.IsPrinted));
                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(g => g.OnReleaseChangeOrder)
                                            .When(!conditions.IsEmailed));
                                    });
                                });
                        });
                    })
                    .WithActions(actions =>
                    {
                        actions.Add(g => g.initializeState);

                        actions.Add(g => g.putOnHold, c => c
                            .DisplayName("Hold")
                            .InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.NoPersist)
                            .WithFieldAssignments(fas =>
                            {
                                fas.Add<hold>(true);
                                fas.Add<printed>(false);
                                fas.Add<emailed>(false);
                                fas.Add<cancelled>(false);
                            })
                            .IsDisabledWhen(conditions.IsChangeOrder));

                        actions.Add(g => g.releaseFromHold, c => c
                            .DisplayName("Remove Hold")
                            .InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.NoPersist)
                            .WithFieldAssignments(fas =>
                            {
                                fas.Add<hold>(false);
                            }));

                        actions.Add(g => g.emailSubcontract, c => c
                            .InFolder(FolderType.ActionsFolder)
                            .MassProcessingScreen<PrintSubcontract>()
                            .InBatchMode());

                        actions.Add(g => g.markAsDontEmail, c => c
                            .InFolder(FolderType.ActionsFolder)
                            .MassProcessingScreen<PrintSubcontract>()
                            .InBatchMode()
                            .WithFieldAssignments(fas => fas.Add<dontEmail>(true)));

                        actions.Add(g => g.createAPInvoice, c => c
                            .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.complete, c => c
                           .DisplayName("Complete Subcontract")
                           .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.cancelOrder, c => c
                            .DisplayName("Cancel Subcontract")
                            .InFolder(FolderType.ActionsFolder)
                            .WithFieldAssignments(fas => fas.Add<cancelled>(true)));

                        actions.Add(g => g.reopenOrder, c => c
                            .DisplayName("Reopen Subcontract")
                            .InFolder(FolderType.ActionsFolder)
                            .WithFieldAssignments(fas =>
                            {
                                fas.Add<hold>(true);
                                fas.Add<printed>(false);
                                fas.Add<emailed>(false);
                                fas.Add<cancelled>(false);
                            })
                            .IsDisabledWhen(conditions.IsChangeOrder));

                        actions.Add(g => g.validateAddresses, c => c
                            .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.recalculateDiscountsAction, c => c
                            .InFolder(FolderType.ActionsFolder));

                        actions.Add(CreatePrepaymentActionName, c => c
                            .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.vendorDetails, c => c
                            .InFolder(FolderType.ReportsFolder));

                        actions.Add(g => g.printSubcontract, c => c
                            .InFolder(FolderType.ReportsFolder)
                            .MassProcessingScreen<PrintSubcontract>()
                            .InBatchMode());

                        actions.Add(g => g.markAsDontPrint, c => c
                            .InFolder(FolderType.ActionsFolder)
                            .MassProcessingScreen<PrintSubcontract>()
                            .InBatchMode()
                            .WithFieldAssignments(fas => fas.Add<dontPrint>(true)));

                        actions.Add(g => g.emailPurchaseOrder, c => c
                           .IsHiddenAlways());

                        actions.Add(g => g.printPurchaseOrder, c => c
                           .IsHiddenAlways());

                        actions.Add(g => g.viewPurchaseOrderReceipt, c => c
                            .IsHiddenAlways());

                        actions.Add(UnlinkFromSOActionName, c => c
                            .IsHiddenAlways());

                        actions.Add(ConvertToNormalActionName, c => c
                            .IsHiddenAlways());

                        actions.Add(CreateSalesOrderActionName, c => c
                            .IsHiddenAlways());

                        actions.Add(GenerateSalesOrderActionName, a => a
                            .IsHiddenAlways());
                    })
                    .WithHandlers(handlers =>
                    {
                        handlers.Add(handler => handler
                            .WithTargetOf<POOrder>()
                            .OfEntityEvent<Events>(e => e.LinesCompleted)
                            .Is(g => g.OnLinesCompleted)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("PO Lines Completed"));

                        handlers.Add(handler => handler
                            .WithTargetOf<POOrder>()
                            .OfEntityEvent<Events>(e => e.LinesClosed)
                            .Is(g => g.OnLinesClosed)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("PO Lines Closed"));

                        handlers.Add(handler => handler
                            .WithTargetOf<POOrder>()
                            .OfEntityEvent<Events>(e => e.LinesReopened)
                            .Is(g => g.OnLinesReopened)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("PO Lines Re-Opened"));

                        handlers.Add(handler => handler
                            .WithTargetOf<POOrder>()
                            .OfEntityEvent<Events>(e => e.Printed)
                            .Is(g => g.OnPrinted)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("Printed"));

                        handlers.Add(handler => handler
                            .WithTargetOf<POOrder>()
                            .OfEntityEvent<Events>(e => e.DoNotPrintChecked)
                            .Is(g => g.OnDoNotPrintChecked)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("Do Not Print Checked"));

                        handlers.Add(handler => handler
                            .WithTargetOf<POOrder>()
                            .OfEntityEvent<Events>(e => e.DoNotEmailChecked)
                            .Is(g => g.OnDoNotEmailChecked)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("Do Not Email Checked"));

                        handlers.Add(handler => handler
                            .WithTargetOf<POOrder>()
                            .OfEntityEvent<Events>(e => e.ReleaseChangeOrder)
                            .Is(g => g.OnReleaseChangeOrder)
                            .UsesTargetAsPrimaryEntity()
                            .WithFieldAssignments(fas =>
                            {
                                fas.Add<printed>(false);
                                fas.Add<emailed>(false);
                            })
                            .DisplayName("On Release Change Order"));
                    });
            });
        }
    }
}