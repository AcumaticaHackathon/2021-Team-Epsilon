using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CM;
using PX.Objects.PO;
using PX.Objects.CS;
using PX.Objects.CN.Subcontracts.PO.CacheExtensions;

namespace PX.Objects.CN.Subcontracts.SC.Graphs
{
    using static BoundedTo<SubcontractEntry, POOrder>;
    using static POOrder;
    using State = POOrderStatus;
    using This = SubcontractEntry_ApprovalWorkflow;

    public class SubcontractEntry_ApprovalWorkflow : PXGraphExtension<SubcontractEntry_Workflow, SubcontractEntry>
    {
        private class SubcontractSetupApproval : IPrefetchable
        {
            public static bool IsActive => PXDatabase.GetSlot<SubcontractSetupApproval>(nameof(SubcontractSetupApproval), typeof(POSetup), typeof(POSetupApproval)).RequestApproval;

            private bool RequestApproval;

            void IPrefetchable.Prefetch()
            {
                RequestApproval = false;
                using (PXDataRecord poSetup = PXDatabase.SelectSingle<POSetup>(new PXDataField<PoSetupExt.subcontractRequestApproval>()))
                {
                    if (poSetup != null)
                        RequestApproval = poSetup.GetBoolean(0) ?? false;
                }
                if (RequestApproval)
                {
                    using (PXDataRecord poApproval = PXDatabase.SelectSingle<POSetupApproval>(
                    new PXDataField<POSetupApproval.approvalID>(),
                    new PXDataFieldValue<POSetupApproval.orderType>(POOrderType.RegularSubcontract)))
                    {
                        if (poApproval != null)
                            RequestApproval = (int?)poApproval.GetInt32(0) != null;
                    }
                }
            }
        }

        public const string ApproveActionName = "Approve";
        public const string RejectActionName = "Reject";

        private static bool ApprovalIsActive => PXAccess.FeatureInstalled<FeaturesSet.approvalWorkflow>() && SubcontractSetupApproval.IsActive;

        [PXWorkflowDependsOnType(typeof(POSetup), typeof(POSetupApproval))]
        public override void Configure(PXScreenConfiguration config)
        {
            if (ApprovalIsActive)
                Configure(config.GetScreenConfigurationContext<SubcontractEntry, POOrder>());
            else
                HideApproveAndRejectActions(config.GetScreenConfigurationContext<SubcontractEntry, POOrder>());
        }

        public class Conditions : Condition.Pack
        {
            public Condition IsApproved => GetOrCreate(b => b.FromBql<
                approved.IsEqual<True>
            >());

            public Condition IsRejected => GetOrCreate(b => b.FromBql<
                rejected.IsEqual<True>
            >());
        }

        protected virtual void HideApproveAndRejectActions(WorkflowContext<SubcontractEntry, POOrder> context)
        {
            var approve = context.ActionDefinitions
                .CreateNew(ApproveActionName, a => a
                .InFolder(FolderType.ActionsFolder, g => g.putOnHold)
                .PlaceAfter(g => g.putOnHold)
                .IsHiddenAlways());
            var reject = context.ActionDefinitions
                .CreateNew(RejectActionName, a => a
                .InFolder(FolderType.ActionsFolder, approve)
                .PlaceAfter(approve)
                .IsHiddenAlways());

            context.UpdateScreenConfigurationFor(screen =>
            {
                return screen
                    .WithActions(actions =>
                    {
                        actions.Add(approve);
                        actions.Add(reject);
                    });
            });
        }

        protected virtual void Configure(WorkflowContext<SubcontractEntry, POOrder> context)
        {
            var conditions = context.Conditions.GetPack<Conditions>();
            var scConditions = context.Conditions.GetPack<SubcontractEntry_Workflow.Conditions>();

            #region Actions
            var approve = context.ActionDefinitions
                .CreateNew(ApproveActionName, a => a
                .InFolder(FolderType.ActionsFolder, g => g.putOnHold)
                .PlaceAfter(g => g.putOnHold)
                .WithFieldAssignments(fas =>
                {
                    fas.Add<approved>(true);
                }));
            var reject = context.ActionDefinitions
	            .CreateNew(RejectActionName, a => a
                .InFolder(FolderType.ActionsFolder, approve)
                .PlaceAfter(approve)
                .WithFieldAssignments(fas =>
                {
                    fas.Add<rejected>(true);
                }));
            #endregion

            context.UpdateScreenConfigurationFor(screen =>
            {
                return screen
                    .WithFlows(flows =>
                    {
                        flows.Update<POOrderType.regularSubcontract>(flow =>
                        {
                            return flow
                                .WithFlowStates(states =>
                                {
                                    states.Add<State.pendingApproval>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.putOnHold);
                                                actions.Add(approve, c => c.IsDuplicatedInToolbar());
                                                actions.Add(reject, c => c.IsDuplicatedInToolbar());
                                                actions.Add(g => g.validateAddresses);

                                                actions.Add(g => g.vendorDetails);
                                                actions.Add(g => g.printSubcontract);
                                            })
                                            .WithFieldStates(fields =>
                                            {
                                                fields.AddAllFields<POOrder>(c => c.IsDisabled());
                                                fields.AddField<POOrder.orderType>();
                                                fields.AddField<POOrder.orderNbr>();
                                                fields.AddField<POOrder.controlTotal>();
                                                fields.AddField<POOrder.workgroupID>();
                                                fields.AddField<POOrder.ownerID>();
                                                fields.AddField<POOrder.printed>();
                                                fields.AddField<POOrder.emailed>();
                                                fields.AddField<POOrder.dontPrint>();
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
                                            });
                                    });
                                    states.Add<State.rejected>(state =>
                                    {
                                        return state
                                            .WithActions(actions =>
                                            {
                                                actions.Add(g => g.putOnHold, c => c.IsDuplicatedInToolbar());
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
                                            });
                                    });
                                })
                                .WithTransitions(transitions =>
                                {
                                    transitions.UpdateGroupFrom(State.Initial, ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.rejected>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(conditions.IsRejected)
                                            .PlaceBefore(tr => tr.To<State.pendingPrint>()));
                                        ts.Add(t => t
                                            .To<State.pendingApproval>()
                                            .IsTriggeredOn(g => g.initializeState)
                                            .When(!conditions.IsApproved)
                                            .PlaceAfter(tr => tr.To<State.rejected>()));
                                    });

                                    transitions.UpdateGroupFrom<State.hold>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.pendingApproval>()
                                            .IsTriggeredOn(g => g.releaseFromHold)
                                            .When(!scConditions.IsOnHold && !conditions.IsApproved)
                                            .PlaceBefore(tr => tr.To<State.pendingPrint>()));
                                    });

                                    transitions.AddGroupFrom<State.pendingApproval>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.pendingPrint>()
                                            .IsTriggeredOn(approve)
                                            .When(conditions.IsApproved && !scConditions.IsPrinted));
                                        ts.Add(t => t
                                            .To<State.pendingEmail>()
                                            .IsTriggeredOn(approve)
                                            .When(conditions.IsApproved && !scConditions.IsEmailed));
                                        ts.Add(t => t
                                            .To<State.closed>()
                                            .IsTriggeredOn(approve)
                                            .When(conditions.IsApproved && scConditions.HasAllLinesClosed));
                                        ts.Add(t => t
                                            .To<State.completed>()
                                            .IsTriggeredOn(approve)
                                            .When(conditions.IsApproved && scConditions.HasAllLinesCompleted));
                                        ts.Add(t => t
                                            .To<State.open>()
                                            .IsTriggeredOn(approve)
                                            .When(conditions.IsApproved));

                                        ts.Add(t => t
                                            .To<State.rejected>()
                                            .IsTriggeredOn(reject)
                                            .When(conditions.IsRejected));

                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.putOnHold)
                                            .When(scConditions.IsOnHold));
                                    });

                                    transitions.AddGroupFrom<State.rejected>(ts =>
                                    {
                                        ts.Add(t => t
                                            .To<State.hold>()
                                            .IsTriggeredOn(g => g.putOnHold)
                                            .When(scConditions.IsOnHold));
                                    });
                                });
                        });
                    })
                    .WithActions(actions =>
                    {
                        actions.Add(approve);
                        actions.Add(reject);
                        actions.Update(
                            g => g.putOnHold,
                            a => a.WithFieldAssignments(fas =>
                            {
                                fas.Add<approved>(false);
                                fas.Add<rejected>(false);
                            }));
                    });
            });
        }
    }
}