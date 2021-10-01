using System;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.WorkflowAPI;

namespace PX.Objects.IN
{
	using State = INDocStatus;
	using static INRegister;

	public abstract class INRegisterEntryBase_Workflow<TGraph, TDocType> : PXGraphExtension<TGraph>
		where TGraph : INRegisterEntryBase, new()
		where TDocType : IConstant, IBqlOperand, IImplement<IBqlString>
	{
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<TGraph, INRegister>());

		protected virtual void Configure(WorkflowContext<TGraph, INRegister> context)
		{
			BoundedTo<TGraph, INRegister>.Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsReleased
					= Bql<released.IsEqual<True>>(),

				IsOnHold
					= Bql<hold.IsEqual<True>>(),

				MatchDocumentType
					= Bql<docType.IsEqual<TDocType>>(),

				HasBatchNbr
					= Bql<batchNbr.IsNotNull.And<batchNbr.IsNotEqual<Empty>>>(),
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
											actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar());
										});
								});
								flowStates.Add<State.balanced>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
											actions.Add(g => g.putOnHold);
											actions.Add(g => g.iNEdit);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnDocumentReleased);
										});
								});
								flowStates.Add<State.released>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.iNRegisterDetails);
										});
								});
							})
							.WithTransitions(transitions =>
							{
								transitions.AddGroupFrom(initialState, ts =>
								{
									ts.Add(t => t.To<State.released>().IsTriggeredOn(g => g.initializeState).When(conditions.IsReleased));
									ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold));
									ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.initializeState));
								});

								transitions.Add(t => t.From<State.hold>().To<State.balanced>().IsTriggeredOn(g => g.releaseFromHold).When(!conditions.IsOnHold));
								transitions.Add(t => t.From<State.balanced>().To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
								transitions.Add(t => t.From<State.balanced>().To<State.released>().IsTriggeredOn(g => g.OnDocumentReleased).When(conditions.IsReleased));
							});
					})
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.putOnHold, a => a
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fass => fass.Add<hold>(true)));
						actions.Add(g => g.releaseFromHold, a => a
							.InFolder(FolderType.ActionsFolder)
							.WithFieldAssignments(fass => fass.Add<hold>(false)));

						actions.Add(g => g.release, a => a
							.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.iNEdit, a => a
							.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.iNRegisterDetails, a => a
							.InFolder(FolderType.ReportsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler =>
							handler
							.WithTargetOf<INRegister>()
							.OfEntityEvent<Events>(e => e.DocumentReleased)
							.Is(g => g.OnDocumentReleased)
							.UsesTargetAsPrimaryEntity()
							.AppliesWhen(conditions.MatchDocumentType)
							.WithFieldAssignments(fass =>
							{
								fass.Add<released>(true);
								fass.Add<releasedToVerify>(false);
							}));
					});
			});
		}
	}
}