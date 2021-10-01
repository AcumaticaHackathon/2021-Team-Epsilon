using System;
using System.Collections;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CN.Common.DAC;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.PJ.DrawingLogs.PJ.DAC;
using PX.Objects.PJ.OutlookIntegration.OU.DAC;
using PX.Objects.PJ.RequestsForInformation.PJ.DAC;
using PX.Objects.PJ.RequestsForInformation.PJ.Descriptor.Attributes;

namespace PX.Objects.PJ.RequestsForInformation.PJ.Graphs
{
	using static BoundedTo<RequestForInformationMaint, RequestForInformation>;

	public partial class RequestForInformationMaint_Workflow : PXGraphExtension<RequestForInformationMaint>
	{
		protected static bool ChangeRequestIsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.changeRequest>();

		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<RequestForInformationMaint, RequestForInformation>());
		}

		protected virtual void Configure(WorkflowContext<RequestForInformationMaint, RequestForInformation> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsOutcoming
					= Bql<RequestForInformation.incoming.IsEqual<False>>()
			}.AutoNameConditions();

			var formClose = context.Forms.Create("FormClose", form => form
					.Prompt("Select Reason")
					.WithFields(fields =>
					{
						fields.Add("Reason", field => field
							.WithSchemaOf<RequestForInformation.reason>()
							.IsRequired()
							.Prompt("Reason")
							.DefaultValue(RequestForInformationReasonAttribute.Answered)
							.OnlyComboBoxValues(RequestForInformationReasonAttribute.Answered, RequestForInformationReasonAttribute.NoResponseNeeded));
					}));

			var open = context.ActionDefinitions
				.CreateExisting<RequestForInformationMaint_Workflow>(g => g.open, a => a
					.IsDisabledWhen(conditions.IsOutcoming)
					.InFolder(FolderType.ActionsFolder));

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<RequestForInformation.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<RequestForInformationStatusAttribute.newStatus>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithFieldStates(fields =>
									{
										fields.AddField<RequestForInformation.reason>(field => field
											.ComboBoxValues(RequestForInformationReasonAttribute.Unassigned)
											.IsDisabled());
									})
									.WithActions(actions =>
									{
										actions.Add(open, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.RequestForInformationEmail, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.RequestForInformationPrint, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<RequestForInformationStatusAttribute.openStatus>(flowState =>
							{
								return flowState
									.WithFieldStates(fields =>
									{
										fields.AddField<RequestForInformation.projectId>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.projectTaskId>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.classId>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.summary>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.incoming>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.requestDetails>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.creationDate>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.reason>(field => field
											.ComboBoxValues(
												RequestForInformationReasonAttribute.Unanswered,
												RequestForInformationReasonAttribute.FollowUpNeeded,
												RequestForInformationReasonAttribute.WaitingInformation));
									})
									.WithActions(actions =>
									{
										actions.Add(g => g.close, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.ConvertToOutgoingRequestForInformation, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.ConvertToChangeRequest, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.RequestForInformationEmail, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.RequestForInformationPrint, c => c.IsDuplicatedInToolbar());
									})
									.WithEventHandlers(handlers =>
										handlers.Add(g => g.OnConvertToChangeRequest));
							});
							fss.Add<RequestForInformationStatusAttribute.closedStatus>(flowState =>
							{
								return flowState
									.WithFieldStates(fields =>
									{
										fields.AddTable<CRPMTimeActivity>(table => table.IsDisabled());
										fields.AddTable<RequestForInformationRelation>(table => table.IsDisabled());
										fields.AddTable<DrawingLog>(table => table.IsDisabled());
										fields.AddField<CSAnswers.value>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.projectId>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.projectTaskId>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.classId>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.summary>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.incoming>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.requestDetails>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.requestAnswer>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.contactID>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.creationDate>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.ownerID>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.workgroupID>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.designChange>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.businessAccountId>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.scheduleImpact>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.isScheduleImpact>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.costImpact>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.isCostImpact>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.documentationLink>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.createdById>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.priorityId>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.specSection>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.dueResponseDate>(field => field.IsDisabled());
										fields.AddField<RequestForInformation.reason>(field => field
											.ComboBoxValues(
												RequestForInformationReasonAttribute.Answered,
												RequestForInformationReasonAttribute.NoResponseNeeded,
												RequestForInformationReasonAttribute.ConvertedToChangeRequest)
											.IsDisabled());
									})
									.WithActions(actions =>
									{
										actions.Add(g => g.reopen, c => c.IsDuplicatedInToolbar());
										actions.Add(g => g.RequestForInformationPrint, c => c.IsDuplicatedInToolbar());
									})
									.WithEventHandlers(handlers =>
										handlers.Add(g => g.OnDeleteChangeRequest));
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<RequestForInformationStatusAttribute.newStatus>(ts =>
							{
								ts.Add(t => t
									.To<RequestForInformationStatusAttribute.openStatus>()
									.IsTriggeredOn(open)
									.WithFieldAssignments(fields =>
									{
										fields.Add<RequestForInformation.reason>(f => f.SetFromValue(RequestForInformationReasonAttribute.Unanswered));
									}));
							});
							transitions.AddGroupFrom<RequestForInformationStatusAttribute.openStatus>(ts =>
							{
								ts.Add(t => t
									.To<RequestForInformationStatusAttribute.closedStatus>()
									.IsTriggeredOn(g => g.close));
								ts.Add(t => t
									.To<RequestForInformationStatusAttribute.closedStatus>()
									.IsTriggeredOn(g => g.OnConvertToChangeRequest));
							});
							transitions.AddGroupFrom<RequestForInformationStatusAttribute.closedStatus>(ts =>
							{
								ts.Add(t => t
									.To<RequestForInformationStatusAttribute.openStatus>()
									.IsTriggeredOn(g => g.reopen)
									.WithFieldAssignments(fields =>
									{
										fields.Add<RequestForInformation.reason>(f => f.SetFromValue(RequestForInformationReasonAttribute.Unanswered));
									}));
								ts.Add(t => t
									.To<RequestForInformationStatusAttribute.openStatus>()
									.IsTriggeredOn(g => g.OnDeleteChangeRequest));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(open);
						actions.Add(g => g.close, c => c
							.InFolder(FolderType.ActionsFolder)
							.WithForm(formClose)
							.WithFieldAssignments(fields =>
							{
								fields.Add<RequestForInformation.reason>(f => f.SetFromFormField(formClose, "Reason"));
							}));
						actions.Add(g => g.reopen, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.ConvertToOutgoingRequestForInformation, c => c
							.InFolder(FolderType.ActionsFolder)
							.IsDisabledWhen(conditions.IsOutcoming));
						actions.Add(g => g.ConvertToChangeRequest, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.RequestForInformationEmail, c => c
							.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.RequestForInformationPrint, c => c
							.InFolder(FolderType.ActionsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<RequestForInformation>()
							.OfEntityEvent<RequestForInformation.Events>(e => e.ConvertToChangeRequest)
							.Is(g => g.OnConvertToChangeRequest)
							.UsesTargetAsPrimaryEntity()
							.WithFieldAssignments(fa => fa.Add<RequestForInformation.reason>(f => f.SetFromValue(RequestForInformationReasonAttribute.ConvertedToChangeRequest))));
						handlers.Add(handler => handler
							.WithTargetOf<RequestForInformation>()
							.OfEntityEvent<RequestForInformation.Events>(e => e.DeleteChangeRequest)
							.Is(g => g.OnDeleteChangeRequest)
							.UsesTargetAsPrimaryEntity()
							.WithFieldAssignments(fa => fa.Add<RequestForInformation.reason>(f => f.SetFromValue(RequestForInformationReasonAttribute.FollowUpNeeded))));
					})
					.WithForms(forms =>
					{
						forms.Add(formClose);
					}));
		}

		public PXAction<RequestForInformation> open;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Open")]
		protected virtual IEnumerable Open(PXAdapter adapter) => adapter.Get();
	}
}