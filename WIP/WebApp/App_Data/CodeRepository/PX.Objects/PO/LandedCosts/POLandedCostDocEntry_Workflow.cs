using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PO.LandedCosts.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
    using State = POLandedCostDocStatus;
    using static POLandedCostDoc;
    using static BoundedTo<POLandedCostDocEntry, POLandedCostDoc>;

    public class POLandedCostDocEntry_Workflow : PXGraphExtension<POLandedCostDocEntry>
    {
        public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<POLandedCostDocEntry, POLandedCostDoc>());

        protected virtual void Configure(WorkflowContext<POLandedCostDocEntry, POLandedCostDoc> context)
        {
            #region Conditions
            Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
            var conditions = new
            {
                IsOnHold
                    = Bql<hold.IsEqual<True>>(),
                IsReleased
                    = Bql<released.IsEqual<True>>()
            }
            .AutoNameConditions();
            #endregion

            context.AddScreenConfigurationFor(screen =>
            {
                return screen
                    .StateIdentifierIs<status>()
                    .FlowTypeIdentifierIs<docType>()
                    .WithFlows(flows =>
                    {
                        flows.Add<POLandedCostDocType.landedCost>(flow =>
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
                                            });
                                    });
                                    states.Add<State.balanced>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.release, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.putOnHold);
                                            })
                                            .WithEventHandlers(handlers =>
                                            {
                                                handlers.Add(g => g.OnInventoryAdjustmentCreated);
                                            });
                                    });
                                    states.Add<State.released>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.createAPInvoice, c => c.IsDuplicatedInToolbar());
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
                                            .IsTriggeredOn(g => g.OnInventoryAdjustmentCreated)
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

                        actions.Add(g => g.createAPInvoice, c => c.InFolder(FolderType.ActionsFolder));

                    })
                    .WithHandlers(handlers =>
                    {
                        handlers.Add(handler => handler
                            .WithTargetOf<POLandedCostDoc>()
                            .OfEntityEvent<Events>(e => e.InventoryAdjustmentCreated)
                            .Is(g => g.OnInventoryAdjustmentCreated)
                            .UsesTargetAsPrimaryEntity()
                            .DisplayName("IN Adjustment Created"));
                    });
            });
        }
    }
}
