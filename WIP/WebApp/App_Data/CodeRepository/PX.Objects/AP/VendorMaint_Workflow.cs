using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;

namespace PX.Objects.AP
{
	using static PX.Data.WorkflowAPI.BoundedTo<VendorMaint, VendorR>;

	public class VendorMaint_Workflow : PXGraphExtension<VendorMaint>
	{
		public static bool IsActive() => false;

		internal static void SetStatusTo(BAccount entity, string targetStatus)
		{
			var vendorMaint = PXGraph.CreateInstance<VendorMaint>();
			vendorMaint.BAccount.Current = PXSelect<VendorR>.Search<VendorR.bAccountID>(vendorMaint, entity.BAccountID);
			vendorMaint.Cancel.Press();

			string action = null;

			switch (targetStatus)
			{
				case VendorStatus.Active:
					action = entity.Status == VendorStatus.OneTime
						? nameof(VendorMaint.SetAsRegular)
						: nameof(VendorMaint.Activate);
					break;

				case VendorStatus.Hold:
					action = nameof(VendorMaint.PutOnHold);
					break;

				case VendorStatus.HoldPayments:
					action = nameof(VendorMaint.HoldPayments);
					break;

				case VendorStatus.OneTime:
					action = nameof(VendorMaint.SetAsOneTime);
					break;

				case VendorStatus.Inactive:
					action = nameof(VendorMaint.Deactivate);
					break;
			}

			if (action == null)
				return;

			vendorMaint.Actions[action].Press();
		}

		public override void Configure(PXScreenConfiguration configuration)
		{
			var context = configuration.GetScreenConfigurationContext<VendorMaint, VendorR>();

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<VendorR.vStatus>()
					.AddDefaultFlow(DefaultCustomerFlow)
					.WithActions(actions =>
					{
						// "Actions" folder
						actions.Add(g => g.Activate, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.PutOnHold, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.HoldPayments, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.Deactivate, a => a.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.newBillAdjustment, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.newManualCheck, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add<VendorMaint.ExtendToCustomer>(e => e.extendToCustomer, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add<VendorMaint.ExtendToCustomer>(e => e.viewCustomer, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add<VendorMaint.DefContactAddressExt>(e => e.ValidateAddresses, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.viewBusnessAccount, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.viewRestrictionGroups, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add<VendorMaint.CreateContactFromVendorGraphExt>(g => g.CreateContact, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.ChangeID, a => a.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.SetAsOneTime, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.SetAsRegular, a => a.InFolder(FolderType.ActionsFolder));

						// "Inquiries" folder
						actions.Add(g => g.vendorDetails, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.approveBillsForPayments, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.payBills, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.vendorPrice, a => a.InFolder(FolderType.InquiriesFolder));

						// "Reports" folder
						actions.Add(g => g.balanceByVendor, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.vendorHistory, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.aPAgedPastDue, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.aPAgedOutstanding, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.aPDocumentRegister, a => a.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.repVendorDetails, a => a.InFolder(FolderType.ReportsFolder));
					})
					.ForbidFurtherChanges();
			});

			Workflow.IConfigured DefaultCustomerFlow(Workflow.INeedStatesFlow flow)
			{
				#region States

				var activeState = context.FlowStates.Create(VendorStatus.Active, state => state
					.IsInitial()
					.WithActions(actions =>
					{
						actions.Add(g => g.PutOnHold);
						actions.Add(g => g.HoldPayments);
						actions.Add(g => g.Deactivate);
						actions.Add(g => g.SetAsOneTime);

						actions.Add(g => g.newBillAdjustment);
						actions.Add(g => g.newManualCheck);
					}));

				var holdState = context.FlowStates.Create(VendorStatus.Hold, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.Activate);
						actions.Add(g => g.Deactivate);
					}));

				var creditHoldState = context.FlowStates.Create(VendorStatus.HoldPayments, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.PutOnHold);
						actions.Add(g => g.Activate);
						actions.Add(g => g.Deactivate);

						actions.Add(g => g.newBillAdjustment);
					}));

				var oneTimeState = context.FlowStates.Create(VendorStatus.OneTime, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.SetAsRegular);
						actions.Add(g => g.Deactivate);

						actions.Add(g => g.newBillAdjustment);
						actions.Add(g => g.newManualCheck);
					}));

				var inactiveState = context.FlowStates.Create(VendorStatus.Inactive, state => state
					.WithActions(actions =>
					{
						actions.Add(g => g.Activate);
					}));

				#endregion

				return flow
					.WithFlowStates(states =>
					{
						states.Add(activeState);
						states.Add(holdState);
						states.Add(creditHoldState);
						states.Add(oneTimeState);
						states.Add(inactiveState);
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(VendorStatus.Active, ts =>
						{
							ts.Add(t => t
								.To(VendorStatus.Hold)
								.IsTriggeredOn(g => g.PutOnHold));

							ts.Add(t => t
								.To(VendorStatus.HoldPayments)
								.IsTriggeredOn(g => g.HoldPayments));

							ts.Add(t => t
								.To(VendorStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));

							ts.Add(t => t
								.To(VendorStatus.OneTime)
								.IsTriggeredOn(g => g.SetAsOneTime));
						});

						transitions.AddGroupFrom(VendorStatus.Hold, ts =>
						{
							ts.Add(t => t
								.To(VendorStatus.Active)
								.IsTriggeredOn(g => g.Activate));

							ts.Add(t => t
								.To(VendorStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(VendorStatus.HoldPayments, ts =>
						{
							ts.Add(t => t
								.To(VendorStatus.Hold)
								.IsTriggeredOn(g => g.PutOnHold));

							ts.Add(t => t
								.To(VendorStatus.Active)
								.IsTriggeredOn(g => g.Activate));

							ts.Add(t => t
								.To(VendorStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(VendorStatus.OneTime, ts =>
						{
							ts.Add(t => t
								.To(VendorStatus.Active)
								.IsTriggeredOn(g => g.SetAsRegular));

							ts.Add(t => t
								.To(VendorStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));
						});

						transitions.AddGroupFrom(VendorStatus.Inactive, ts =>
						{
							ts.Add(t => t
								.To(VendorStatus.Active)
								.IsTriggeredOn(g => g.Activate));
						});
					});
			}
		}
	}
}
