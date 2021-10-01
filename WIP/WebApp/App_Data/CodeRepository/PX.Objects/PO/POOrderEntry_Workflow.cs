using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.PO.GraphExtensions.POOrderEntryExt;

namespace PX.Objects.PO
{
    using static BoundedTo<POOrderEntry, POOrder>;
    using static POOrder;
    using Prepayments = GraphExtensions.POOrderEntryExt.Prepayments;
    using State = POOrderStatus;
    using DropShipLinksExt = GraphExtensions.POOrderEntryExt.DropShipLinksExt;
    using PurchaseToSOLinksExt = GraphExtensions.POOrderEntryExt.PurchaseToSOLinksExt;

    public class POOrderEntry_Workflow : PXGraphExtension<POOrderEntry>
    {
        public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<POOrderEntry, POOrder>());

        protected virtual void Configure(WorkflowContext<POOrderEntry, POOrder> context)
        {
            #region Conditions
            Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
            var conditions = new
            {
                IsOnHold
                    = Bql<hold.IsEqual<True>>(),
                IsCancelled
                    = Bql<cancelled.IsEqual<True>>(),

                IsPrinted
                    = Bql<printedExt.IsEqual<True>>(),
                IsEmailed
                    = Bql<emailedExt.IsEqual<True>>(),

                IsChangeOrder
                    = Bql<behavior.IsEqual<POBehavior.changeOrder>>(),

                HasAllLinesClosed
                    = Bql<linesToCloseCntr.IsEqual<Zero>>(),
                HasAllLinesCompleted
                    = Bql<linesToCloseCntr.IsNotEqual<Zero>
                        .And<linesToCompleteCntr.IsEqual<Zero>>>(),

				IsNotIntercompany
					= Bql<isIntercompany.IsEqual<False>>(),

				IsIntercompanyOrderGenerated
					= Bql<intercompanySONbr.IsNotNull>(),

                HasAllDropShipLinesLinked
                    = Bql<dropShipLinesCount.IsEqual<Zero>
                        .Or<dropShipLinesCount.IsEqual<dropShipActiveLinksCount>>>(),

                IsNewDropShipOrder
                    = Bql<orderType.IsEqual<POOrderType.dropShip>
                        .And<isLegacyDropShip.IsNotEqual<True>>>(),
                IsLinkedToSalesOrder
                    = Bql<orderType.IsEqual<POOrderType.dropShip>
                        .And<sOOrderNbr.IsNotNull>>()
            }
            .AutoNameConditions();
            #endregion

            context.AddScreenConfigurationFor(screen =>
            {
                return screen
                    .StateIdentifierIs<status>()
                    .FlowTypeIdentifierIs<orderType>(true)
                    .WithFlows(flows =>
                    {
                        flows.Add<POOrderType.regularOrder>(flow =>
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
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add(g => g.addPOOrder);
                                                actions.Add(g => g.addPOOrderLine);
                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
                                                actions.Add(g => g.addInvBySite);
                                                actions.Add(g => g.addInvSelBySite);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>();
                                                fields.AddTable<POLine>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());
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

                                                actions.Add(g => g.printPurchaseOrder, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.vendorDetails);

                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();
                                                fields.AddField<POOrder.dontPrint>();
												fields.AddField<POOrder.excludeFromIntercompanyProc>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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
                                                actions.Add(g => g.emailPurchaseOrder, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.markAsDontEmail, c => c.IsDuplicatedInToolbar());

                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.vendorDetails);

                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();
                                                fields.AddField<POOrder.dontEmail>();
												fields.AddField<POOrder.excludeFromIntercompanyProc>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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

                                                actions.Add(g => g.emailPurchaseOrder);
                                                actions.Add(g => g.createPOReceipt, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.createAPInvoice);

                                                actions.Add(g => g.validateAddresses);
                                                actions.Add<Prepayments>(g => g.createPrepayment);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

												actions.Add<Intercompany>(e => e.generateSalesOrder);
                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();
												fields.AddField<POOrder.excludeFromIntercompanyProc>();

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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
                                                actions.Add<Prepayments>(g => g.createPrepayment);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
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
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
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
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
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
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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
                                            .IsTriggeredOn(g => g.emailPurchaseOrder)
                                            .When(conditions.HasAllLinesClosed));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.emailPurchaseOrder)
                                            .When(conditions.HasAllLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.emailPurchaseOrder));

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
                        flows.Add<POOrderType.dropShip>(flow =>
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
                                                actions.Add(g => g.cancelOrder);
                                                actions.Add(g => g.validateAddresses);
                                                actions.Add(g => g.recalculateDiscountsAction);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add(g => g.addPOOrder);
                                                actions.Add(g => g.addPOOrderLine);
                                                actions.Add(g => g.addInvBySite);
                                                actions.Add(g => g.addInvSelBySite);

                                                actions.Add<DropShipLinksExt>(g => g.unlinkFromSO);
                                                actions.Add<DropShipLinksExt>(g => g.convertToNormal);
                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>();
                                                fields.AddField<POOrder.approved>(c => c.IsDisabled());
                                                fields.AddField<POOrder.ownerID>();
                                                fields.AddField<POOrder.workgroupID>(c => c.IsDisabled());

                                                fields.AddTable<POLine>();
                                                fields.AddTable<POTax>();
                                                fields.AddAllFields<POShipAddress>();
                                                fields.AddAllFields<POShipContact>();
                                                
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

                                                actions.Add(g => g.printPurchaseOrder, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.vendorDetails);

                                                actions.Add<DropShipLinksExt>(g => g.unlinkFromSO);
                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
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
                                                actions.Add(g => g.emailPurchaseOrder, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.markAsDontEmail, c => c.IsDuplicatedInToolbar());

                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.vendorDetails);

                                                actions.Add<DropShipLinksExt>(g => g.unlinkFromSO);
                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
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
                                    states.Add<State.awaitingLink>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.putOnHold);

                                                actions.Add(g => g.complete);
                                                actions.Add(g => g.cancelOrder);

                                                actions.Add(g => g.emailPurchaseOrder);
                                                actions.Add(g => g.createAPInvoice);

                                                actions.Add(g => g.validateAddresses);
                                                actions.Add<Prepayments>(g => g.createPrepayment);
                                                actions.Add<DropShipLinksExt>(g => g.unlinkFromSO);
                                                actions.Add<DropShipLinksExt>(g => g.convertToNormal);
                                                actions.Add<DropShipLinksExt>(g => g.createSalesOrder);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();

                                                fields.AddField<POOrder.ownerID>(c => c.IsHidden());
                                                fields.AddField<POOrder.workgroupID>(c => c.IsHidden());

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
                                                handlers.Add(g => g.OnLinesLinked);
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

                                                actions.Add(g => g.emailPurchaseOrder);
                                                actions.Add(g => g.createPOReceipt, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.createAPInvoice);

                                                actions.Add(g => g.validateAddresses);
                                                actions.Add<Prepayments>(g => g.createPrepayment);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add<DropShipLinksExt>(g => g.unlinkFromSO);
                                                actions.Add<DropShipLinksExt>(g => g.convertToNormal);
                                                actions.Add<DropShipLinksExt>(g => g.createSalesOrder);
                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();

                                                fields.AddField<POOrder.ownerID>(c => c.IsHidden());
                                                fields.AddField<POOrder.workgroupID>(c => c.IsHidden());

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
                                                handlers.Add(g => g.OnLinesUnlinked);
                                                handlers.Add(g => g.OnReleaseChangeOrder);
                                            });
                                    });
                                    states.Add<State.completed>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.reopenOrder);
                                                actions.Add(g => g.createAPInvoice);
                                                actions.Add<Prepayments>(g => g.createPrepayment);
                                                actions.Add<DropShipLinksExt>(g => g.createSalesOrder);
                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
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
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
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
                                            }); ;
                                    });
                                    states.Add<State.closed>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.reopenOrder);
                                                actions.Add(g => g.validateAddresses);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add<DropShipLinksExt>(g => g.createSalesOrder);
                                                actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand);
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
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder));
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
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!conditions.IsOnHold));
                                        ts.Add(t => t
                                            .To<State.cancelled>()
                                            .IsTriggeredOn(g => g.cancelOrder)
                                            .When(conditions.IsCancelled));
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
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.OnPrinted)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder)
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
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.OnDoNotPrintChecked)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder)
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
                                            .IsTriggeredOn(g => g.emailPurchaseOrder)
                                            .When(conditions.HasAllLinesClosed));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.emailPurchaseOrder)
                                            .When(conditions.HasAllLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.emailPurchaseOrder)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.emailPurchaseOrder));

                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.OnDoNotEmailChecked)
                                            .When(conditions.HasAllLinesClosed));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.OnDoNotEmailChecked)
                                            .When(conditions.HasAllLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.OnDoNotEmailChecked)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnDoNotEmailChecked));
                                    });
                                    transitions.AddGroupFrom<State.awaitingLink>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.putOnHold));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnLinesLinked));
                                        ts.Add(t => t
                                            .To<State.cancelled>()
                                            .IsTriggeredOn(g => g.cancelOrder));
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
                                    });
                                    transitions.AddGroupFrom<State.open>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.putOnHold)
                                            .When(conditions.IsOnHold));
                                        ts.Add(t => t
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.OnLinesUnlinked)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder));
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
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.OnLinesReopened)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder));
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
                                            .To<State.awaitingLink>()
                                            .IsTriggeredOn(g => g.OnLinesReopened)
                                            .When(!conditions.HasAllDropShipLinesLinked && conditions.IsNewDropShipOrder));
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
                        flows.Add<POOrderType.standardBlanket>(flow =>
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
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);

                                                actions.Add(g => g.addInvBySite);
                                                actions.Add(g => g.addInvSelBySite);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddTable<POLine>();
                                                fields.AddField<POLine.completed>(x => x.IsDisabled().IsHidden());
                                                fields.AddField<POLine.closed>(x => x.IsDisabled().IsHidden());
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

                                                fields.AddTable<POTax>();
                                                fields.AddAllFields<POShipAddress>();
                                                fields.AddAllFields<POShipContact>();
                                                fields.AddTable<POOrderPrepayment>(c => c.IsHidden());

                                                fields.AddAllFields<POOrder>();
                                                fields.AddField<POOrder.approved>(c => c.IsHidden());
                                                fields.AddField<POOrder.printed>(c => c.IsHidden());
                                                fields.AddField<POOrder.emailed>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontPrint>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontEmail>(c => c.IsHidden());
                                                fields.AddField<POOrder.ownerID>(c => c.IsDisabled().IsHidden());
                                                fields.AddField<POOrder.workgroupID>(c => c.IsDisabled().IsHidden());
                                            });

                                    });
                                    states.Add<State.open>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.putOnHold);
                                                actions.Add(g => g.cancelOrder);

                                                actions.Add(g => g.emailPurchaseOrder, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.validateAddresses);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();
                                                fields.AddField<POOrder.approved>(c => c.IsHidden());
                                                fields.AddField<POOrder.printed>(c => c.IsHidden());
                                                fields.AddField<POOrder.emailed>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontPrint>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontEmail>(c => c.IsHidden());
                                                fields.AddField<POOrder.ownerID>(c => c.IsHidden());
                                                fields.AddField<POOrder.workgroupID>(c => c.IsHidden());

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.completed>(x => x.IsDisabled().IsHidden());
                                                fields.AddField<POLine.closed>(x => x.IsDisabled().IsHidden());
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

                                                fields.AddTable<POTax>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());
                                                fields.AddTable<CurrencyInfo>(c => c.IsDisabled());
                                                fields.AddTable<POOrderPrepayment>(c => c.IsHidden());
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
                                                handlers.Add(g => g.OnLinesCompleted);
                                                handlers.Add(g => g.OnLinesClosed);
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
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.viewPurchaseOrderReceipt);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddTable<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.approved>(c => c.IsHidden());
                                                fields.AddField<POOrder.printed>(c => c.IsHidden());
                                                fields.AddField<POOrder.emailed>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontPrint>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontEmail>(c => c.IsHidden());
                                                fields.AddField<POOrder.ownerID>(c => c.IsHidden());
                                                fields.AddField<POOrder.workgroupID>(c => c.IsHidden());

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.completed>(x => x.IsDisabled().IsHidden());
                                                fields.AddField<POLine.closed>(x => x.IsDisabled().IsHidden());
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

                                                fields.AddAllFields<PORemitAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<PORemitContact>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());
                                                fields.AddTable<POTax>(c => c.IsDisabled());

                                                fields.AddTable<CurrencyInfo>(c => c.IsDisabled());
                                                fields.AddTable<POOrderPrepayment>(c => c.IsHidden());
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
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
                                            .To<State.cancelled>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(conditions.IsCancelled));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.initializeState));
                                    });
                                    transitions.AddGroupFrom<State.hold>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!conditions.IsOnHold));
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
                                            .When(!conditions.IsOnHold));
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
                        flows.Add<POOrderType.blanket>(flow =>
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
                                                actions.Add(g => g.printPurchaseOrder);

                                                actions.Add(g => g.addInvBySite);
                                                actions.Add(g => g.addInvSelBySite);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>();
                                                fields.AddTable<POLine>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());
                                                fields.AddTable<POTax>();
                                                fields.AddAllFields<POShipAddress>();
                                                fields.AddAllFields<POShipContact>();
                                                fields.AddField<POOrder.approved>(c => c.IsDisabled());
                                                fields.AddField<POOrder.ownerID>();
                                                fields.AddField<POOrder.workgroupID>(c => c.IsDisabled());
                                                fields.AddField<POOrder.printed>(c => c.IsHidden());
                                                fields.AddField<POOrder.emailed>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontPrint>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontEmail>(c => c.IsHidden());
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
                                                actions.Add(g => g.emailPurchaseOrder);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();
                                                fields.AddField<POOrder.approved>(c => c.IsDisabled());
                                                fields.AddField<POOrder.printed>(c => c.IsHidden());
                                                fields.AddField<POOrder.emailed>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontPrint>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontEmail>(c => c.IsHidden());

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.validateAddresses);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddTable<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.printed>(c => c.IsHidden());
                                                fields.AddField<POOrder.emailed>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontPrint>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontEmail>(c => c.IsHidden());

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

                                                fields.AddAllFields<PORemitAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<PORemitContact>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipAddress>(c => c.IsDisabled());
                                                fields.AddAllFields<POShipContact>(c => c.IsDisabled());
                                                fields.AddTable<POTax>(c => c.IsDisabled());
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

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.validateAddresses);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddTable<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.printed>(c => c.IsHidden());
                                                fields.AddField<POOrder.emailed>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontPrint>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontEmail>(c => c.IsHidden());

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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
                                                actions.Add(g => g.reopenOrder, c => c.IsDuplicatedInToolbar());

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printPurchaseOrder);
                                                actions.Add(g => g.validateAddresses);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddTable<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.printed>(c => c.IsHidden());
                                                fields.AddField<POOrder.emailed>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontPrint>(c => c.IsHidden());
                                                fields.AddField<POOrder.dontEmail>(c => c.IsHidden());

                                                fields.AddTable<POLine>(c => c.IsDisabled());
                                                fields.AddField<POLine.cancelled>();
                                                fields.AddField<POLine.completed>();
                                                fields.AddField<POLine.promisedDate>();
                                                fields.AddField<POLine.closed>();
                                                fields.AddField<POLine.sOOrderNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLineNbr>(c => c.IsHidden());
                                                fields.AddField<POLine.sOOrderStatus>(c => c.IsHidden());
                                                fields.AddField<POLine.sOLinkActive>(c => c.IsHidden());

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
                                            .To<State.closed>()
                                            .IsTriggeredOn(g => g.OnLinesClosed));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnLinesReopened));
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
                                            .To<State.open>()
                                            .IsTriggeredOn(g => g.OnLinesReopened));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(g => g.OnLinesCompleted));
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
                            .InFolder(FolderType.ActionsFolder)
                            .WithPersistOptions(ActionPersistOptions.NoPersist)
                            .WithFieldAssignments(fas =>
                            {
                                fas.Add<hold>(false);
                            }));

                        actions.Add(g => g.emailPurchaseOrder, c => c.
                            InFolder(FolderType.ActionsFolder)
                            .MassProcessingScreen<POPrintOrder>()
                            .InBatchMode()
                            .WithFieldAssignments(fas => fas.Add<emailed>(true)));

                        actions.Add(g => g.markAsDontEmail, c => c
                            .InFolder(FolderType.ActionsFolder)
                            .MassProcessingScreen<POPrintOrder>()
                            .InBatchMode()
                            .WithFieldAssignments(fas => fas.Add<dontEmail>(true)));

                        actions.Add(g => g.createPOReceipt, c => c
                            .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.createAPInvoice, c => c
                            .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.complete, c => c
                           .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.cancelOrder, c => c
                            .InFolder(FolderType.ActionsFolder)
                            .WithFieldAssignments(fas => fas.Add<cancelled>(true)));

                        actions.Add(g => g.reopenOrder, c => c
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

                        actions.Add<Prepayments>(g => g.createPrepayment, c => c
                            .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.vendorDetails, c => c
                            .InFolder(FolderType.ReportsFolder));

                        actions.Add(g => g.printPurchaseOrder, c => c
                            .InFolder(FolderType.ReportsFolder)
                            .MassProcessingScreen<POPrintOrder>()
                            .InBatchMode());

                        actions.Add(g => g.markAsDontPrint, c => c
                            .InFolder(FolderType.ActionsFolder)
                            .MassProcessingScreen<POPrintOrder>()
                            .InBatchMode()
                            .WithFieldAssignments(fas => fas.Add<dontPrint>(true)));

                        actions.Add(g => g.viewPurchaseOrderReceipt, c => c
                            .InFolder(FolderType.ReportsFolder));

                        actions.Add<DropShipLinksExt>(g => g.unlinkFromSO, c => c
                            .InFolder(FolderType.ActionsFolder)
                            .IsHiddenWhen(!conditions.IsNewDropShipOrder)
                            .IsDisabledWhen(!conditions.IsLinkedToSalesOrder));

                        actions.Add<DropShipLinksExt>(g => g.convertToNormal, c => c
                            .InFolder(FolderType.ActionsFolder)
                            .IsHiddenWhen(!conditions.IsNewDropShipOrder));

                        actions.Add<DropShipLinksExt>(g => g.createSalesOrder, c => c
                            .InFolder(FolderType.ActionsFolder));

                        actions.Add(g => g.addPOOrder);
                        actions.Add(g => g.addPOOrderLine);
                        actions.Add<PurchaseToSOLinksExt>(g => g.viewDemand, c => c.IsHiddenWhen(conditions.IsNewDropShipOrder));
                        actions.Add(g => g.addInvBySite);
                        actions.Add(g => g.addInvSelBySite);

						actions.Add<Intercompany>(e => e.generateSalesOrder, a => a
							.InFolder(FolderType.ActionsFolder)
							.IsHiddenWhen(conditions.IsNotIntercompany)
							.IsDisabledWhen(conditions.IsIntercompanyOrderGenerated));
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
                            .OfEntityEvent<Events>(e => e.LinesLinked)
                            .Is(g => g.OnLinesLinked)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("PO Lines Linked"));

                        handlers.Add(handler => handler
                            .WithTargetOf<POOrder>()
                            .OfEntityEvent<Events>(e => e.LinesUnlinked)
                            .Is(g => g.OnLinesUnlinked)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("PO Lines Unlinked"));

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
