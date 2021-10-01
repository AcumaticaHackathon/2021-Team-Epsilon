using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.AP;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.TX
{
	using State = APDocStatus;
	using static APInvoice;
	using static BoundedTo<TXInvoiceEntry, APInvoice>;

	public partial class TXInvoiceEntry_Workflow : PXGraphExtension<TXInvoiceEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<TXInvoiceEntry, APInvoice>());

		protected virtual void Configure(WorkflowContext<TXInvoiceEntry, APInvoice> context)
		{
			#region Conditions

			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOnHold
					= Bql<hold.IsEqual<True>>(),

				IsNotOnHold
					= Bql<hold.IsEqual<False>>(),

				IsOpen
					= Bql<openDoc.IsEqual<True>.And<released.IsEqual<True>>>(),

				IsClosed
					= Bql<openDoc.IsEqual<False>.And<released.IsEqual<True>>>(),



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

									});
							});
							fss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold);

									});
							});
							fss.Add<State.open>();
							fss.Add<State.closed>();

						}
						)
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)); // New Hold
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsNotOnHold)); // New Hold
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t.To<State.open>()
								.IsTriggeredOn(g => g.release)
								.When(conditions.IsOpen));
							ts.Add(t => t.To<State.closed>()
								.IsTriggeredOn(g => g.release)
								.When(conditions.IsClosed));

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
						actions.Add(g => g.release, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.vendorDocuments, c => c.InFolder(FolderType.InquiriesFolder).IsHiddenAlways());
						actions.Add(g => g.printAPEdit, c => c.InFolder(FolderType.ReportsFolder).IsHiddenAlways());
						actions.Add(g => g.printAPRegister, c => c.InFolder(FolderType.ReportsFolder).IsHiddenAlways());

					})
			);
		}
	}
}