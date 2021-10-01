using PX.Data;
using PX.Data.WorkflowAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PX.Objects.CR.ContactMaint;

namespace PX.Objects.CR.Workflows
{
	public class ContactWorkflow : PXGraphExtension<ContactMaint>
	{
		public static bool IsActive() => false;

		internal static void SetStatusTo(Contact entity, string targetStatus)
		{
			var contactMaint = PXGraph.CreateInstance<ContactMaint>();
			contactMaint.Contact.Current = PXSelect<Contact>.Search<Contact.contactID>(contactMaint, entity.ContactID);
			contactMaint.Cancel.Press();

			string action = null;

			switch (targetStatus)
			{
				case ContactStatus.Active:
					action = nameof(ContactMaint.Activate);
					break;

				case ContactStatus.Inactive:
					action = nameof(ContactMaint.Deactivate);
					break;
			}

			if (action == null)
				return;

			contactMaint.Actions[action].Press();
		}

		public override void Configure(PXScreenConfiguration configuration)
		{
			var config = configuration.GetScreenConfigurationContext<ContactMaint, Contact>();

			config.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<Contact.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(states =>
						{
							states.Add(ContactStatus.Active, state => state
								.IsInitial()
								.WithActions(actions =>
								{
									actions.Add(g => g.Deactivate);
									actions.Add<CreateLeadFromContactGraphExt>(e => e.CreateLead);
								}));

							states.Add(ContactStatus.Inactive, state => state
								.WithActions(actions =>
								{
									actions.Add(g => g.Activate, a => a.IsDuplicatedInToolbar());
								}));
						})
						.WithTransitions(transitions =>
						{
							transitions.Add(t => t
								.From(ContactStatus.Active)
								.To(ContactStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));

							transitions.Add(t => t
								.From(ContactStatus.Inactive)
								.To(ContactStatus.Active)
								.IsTriggeredOn(g => g.Activate));
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.Activate, a => a
							.WithFieldAssignments(fields =>
							{
								fields.Add<Contact.isActive>(f => f.SetFromValue(true));
								fields.Add<Contact.duplicateStatus>(f => f.SetFromValue(DuplicateStatusAttribute.NotValidated));
							})
							.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.Deactivate, a => a
							.WithFieldAssignments(fields =>
							{
								fields.Add<Contact.isActive>(f => f.SetFromValue(false));
							})
							.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.copyBAccountContactInfo, c => c.InFolder(FolderType.ActionsFolder));

						actions.Add<CreateLeadFromContactGraphExt>(g => g.CreateLead, c => c.InFolder(FolderType.ActionsFolder));
						actions.Add<CreateAccountFromContactGraphExt>(g => g.CreateBAccount, c => c.InFolder(FolderType.ActionsFolder));
					})
					.ForbidFurtherChanges();
			});

		}
	}

	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class ContactWorkflow_CbApi_Adapter : PXGraphExtension<ContactMaint>
	{
		public override void Initialize()
		{
			base.Initialize();
			if (!Base.IsContractBasedAPI)
				return;

			Base.RowUpdated.AddHandler<Contact>(RowUpdated);

			void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
			{
				if (e.Row is Contact row
					&& e.OldRow is Contact oldRow
					&& row.IsActive is bool newActive
					&& oldRow.IsActive is bool oldActive
					&& newActive != oldActive)
				{
					// change it only by transition
					row.IsActive = oldActive;
					
					Base.RowUpdated.RemoveHandler<Contact>(RowUpdated);

					Base.OnAfterPersist += InvokeTransition;
					void InvokeTransition(PXGraph obj)
					{
						obj.OnAfterPersist -= InvokeTransition;
						(newActive ? Base.Activate : Base.Deactivate).PressImpl(internalCall: true);
					}
				}
			}
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Enabled), true)]
		public virtual void _(Events.CacheAttached<Contact.isActive> e) { }
	}
}
