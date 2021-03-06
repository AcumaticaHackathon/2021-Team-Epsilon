using System;
using System.Collections.Generic;
using PX.Data;
using System.Collections;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.CT;

namespace PX.Objects.PM
{
	public class ProjectTaskEntry : PXGraph<ProjectTaskEntry, PMTask>
	{
		#region DAC Attributes Override

		#region PMTask

		[Project(typeof(Where<PMProject.nonProject, NotEqual<True>, And<PMProject.baseType, Equal<CT.CTPRType.project>>>), DisplayName = "Project ID", IsKey = true)]
		[PXParent(typeof(Select<PMProject, Where<PMProject.contractID, Equal<Current<PMTask.projectID>>>>))]
		[PXDefault]
		protected virtual void _(Events.CacheAttached<PMTask.projectID> e) { }


		[PXDimensionSelector(ProjectTaskAttribute.DimensionName,
			typeof(Search<PMTask.taskCD, Where<PMTask.projectID, Equal<Current<PMTask.projectID>>>>),
			typeof(PMTask.taskCD),
			typeof(PMTask.taskCD), typeof(PMTask.locationID), typeof(PMTask.description), typeof(PMTask.status), DescriptionField = typeof(PMTask.description))]
		[PXDBString(IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault()]
		[PXUIField(DisplayName = "Task ID", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void _(Events.CacheAttached<PMTask.taskCD> e) { }

		#endregion

		#region CRCampaign

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.AccountedCampaign, Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void _(Events.CacheAttached<CRCampaign.campaignID> e) { }

		#endregion

		#region PMBudget

		[PXDBInt(IsKey = true)]
		[PXParent(typeof(Select<PMTask, Where<PMTask.taskID, Equal<Current<PMBudget.projectTaskID>>>>))]
		protected virtual void _(Events.CacheAttached<PMBudget.projectTaskID> e) { }

		#endregion

		#endregion

		#region Views/Selects

		public PXSelectJoin<PMTask,
			LeftJoin<PMProject, On<PMTask.projectID, Equal<PMProject.contractID>>>,
			Where<PMProject.nonProject, Equal<False>, And<PMProject.baseType, Equal<CT.CTPRType.project>>>> Task;

		[PXViewName(Messages.ProjectTask)]
		public PXSelect<PMTask, Where<PMTask.taskID, Equal<Current<PMTask.taskID>>>> TaskProperties;

		public PXSelect<PMRecurringItem,
			Where<PMRecurringItem.projectID, Equal<Current<PMTask.projectID>>,
			And<PMRecurringItem.taskID, Equal<Current<PMTask.taskID>>>>> BillingItems;

		public PXSelect<PMBudget, Where<PMBudget.projectTaskID, Equal<Current<PMTask.taskID>>>> TaskBudgets;

		[PXViewName(Messages.TaskAnswers)]
		public CRAttributeList<PMTask> Answers;

		[PXFilterable]
		[PXViewName(Messages.Activities)]
		[CRReference(typeof(Select<Customer, Where<Customer.bAccountID, Equal<Current<PMTask.customerID>>>>))]
		public ProjectTaskActivities Activities;

		public PXSetup<PMSetup> Setup;
		public PXSetup<Company> CompanySetup;
		[PXViewName(Messages.Project)]
		public PXSelect<PMProject, Where<PMProject.contractID, Equal<Current<PMTask.projectID>>>> Project;

		[PXReadOnlyView]
		public PXSelect<CRCampaign, Where<CRCampaign.projectID, Equal<Current<PMTask.projectID>>, And<CRCampaign.projectTaskID, Equal<Current<PMTask.taskID>>>>> TaskCampaign;

		#endregion


		public ProjectTaskEntry()
		{
			if (Setup.Current == null)
			{
				throw new PXException(Messages.SetupNotConfigured);
			}
						
			Activities.GetNewEmailAddress =
					() =>
						{
							PMProject current = Project.Select();
							if (current != null)
							{
								Contact customerContact = PXSelectJoin<Contact, InnerJoin<BAccount, On<BAccount.defContactID, Equal<Contact.contactID>>>, Where<BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>.Select(this, current.CustomerID);

								if (customerContact != null && !string.IsNullOrWhiteSpace(customerContact.EMail))
									return PXDBEmailAttribute.FormatAddressesWithSingleDisplayName(customerContact.EMail, customerContact.DisplayName);
							}
							return String.Empty;
						};
		}

		#region

		public PXAction<PMTask> activate;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Activate")]
		protected virtual IEnumerable Activate(PXAdapter adapter)
		{
			if (Task.Current != null)
			{
				if (Task.Current.StartDate == null)
				{
					Task.Current.StartDate = Accessinfo.BusinessDate;
					Task.Update(Task.Current);
				}
			}
			return adapter.Get();
		}

		public PXAction<PMTask> complete;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Complete")]
		protected virtual IEnumerable Complete(PXAdapter adapter)
		{
			if (Task.Current != null)
			{
				Task.Current.EndDate = Accessinfo.BusinessDate;
				Task.Current.CompletedPercent = Math.Max(100, Task.Current.CompletedPercent.GetValueOrDefault());
				Task.Update(Task.Current);
			}
			return adapter.Get();
		}

		#endregion

		#region Event Handlers

		protected virtual void PMTask_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			PMTask row = e.Row as PMTask;
			if (row == null) return;

			PXUIFieldAttribute.SetEnabled<PMTask.visibleInGL>(sender, row, Setup.Current.VisibleInGL == true);
			PXUIFieldAttribute.SetEnabled<PMTask.visibleInAP>(sender, row, Setup.Current.VisibleInAP == true);
			PXUIFieldAttribute.SetEnabled<PMTask.visibleInAR>(sender, row, Setup.Current.VisibleInAR == true);
			PXUIFieldAttribute.SetEnabled<PMTask.visibleInSO>(sender, row, Setup.Current.VisibleInSO == true);
			PXUIFieldAttribute.SetEnabled<PMTask.visibleInPO>(sender, row, Setup.Current.VisibleInPO == true);
			PXUIFieldAttribute.SetEnabled<PMTask.visibleInTA>(sender, row, Setup.Current.VisibleInTA == true);
			PXUIFieldAttribute.SetEnabled<PMTask.visibleInEA>(sender, row, Setup.Current.VisibleInEA == true);
			PXUIFieldAttribute.SetEnabled<PMTask.visibleInIN>(sender, row, Setup.Current.VisibleInIN == true);
			PXUIFieldAttribute.SetEnabled<PMTask.visibleInCA>(sender, row, Setup.Current.VisibleInCA == true);

			PMProject project = Project.Select();
			if (project == null) return;

			string status = GetStatusFromFlags(row);
			bool projectEditable = !(project.Status == ProjectStatus.Completed || project.Status == ProjectStatus.Cancelled);
			PXUIFieldAttribute.SetEnabled<PMTask.description>(sender, row, projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.rateTableID>(sender, row, projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.allocationID>(sender, row, projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.billingID>(sender, row, projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.billingOption>(sender, row, status == ProjectTaskStatus.Planned);
			PXUIFieldAttribute.SetEnabled<PMTask.completedPercent>(sender, row, row.CompletedPctMethod == PMCompletedPctMethod.Manual && status != ProjectTaskStatus.Planned && projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.taxCategoryID>(sender, row, projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.approverID>(sender, row, projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.startDate>(sender, row, (status == ProjectTaskStatus.Planned || status == ProjectTaskStatus.Active) && projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.endDate>(sender, row, status != ProjectTaskStatus.Completed && projectEditable);
			PXUIFieldAttribute.SetEnabled<PMTask.plannedStartDate>(sender, row, status == ProjectTaskStatus.Planned);
			PXUIFieldAttribute.SetEnabled<PMTask.plannedEndDate>(sender, row, status == ProjectTaskStatus.Planned);
			PXUIFieldAttribute.SetEnabled<PMTask.isDefault>(sender, row, projectEditable);
		}

		protected virtual void PMTask_CompletedPercent_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			PMTask row = e.Row as PMTask;
			if (row != null && row.CompletedPctMethod != PMCompletedPctMethod.Manual)
			{
				PXSelectBase<PMCostBudget> select = new PXSelectGroupBy<PMCostBudget,
						   Where<PMCostBudget.projectID, Equal<Required<PMTask.projectID>>,
						   And<PMCostBudget.projectTaskID, Equal<Required<PMTask.taskID>>,
						   And<PMCostBudget.isProduction, Equal<True>>>>,
						   Aggregate<
						   GroupBy<PMCostBudget.accountGroupID,
						   GroupBy<PMCostBudget.inventoryID,
						   GroupBy<PMCostBudget.uOM,
						   Sum<PMCostBudget.amount,
						   Sum<PMCostBudget.qty,
						   Sum<PMCostBudget.curyRevisedAmount,
						   Sum<PMCostBudget.revisedQty,
						   Sum<PMCostBudget.actualAmount,
						   Sum<PMCostBudget.actualQty>>>>>>>>>>>(this);

				PXResultset<PMCostBudget> ps = select.Select(row.ProjectID, row.TaskID);


				if (ps != null)
				{
					double percentSum = 0;
					Int32 recordCount = 0;
					decimal actualAmount = 0;
					decimal revisedAmount = 0;
					foreach (PMCostBudget item in ps)
					{

						if (row.CompletedPctMethod == PMCompletedPctMethod.ByQuantity && item.RevisedQty > 0)
						{
							recordCount ++;
							percentSum += Convert.ToDouble(100 * item.ActualQty / item.RevisedQty);
						}
						else if (row.CompletedPctMethod == PMCompletedPctMethod.ByAmount)
						{
							recordCount++;
							actualAmount += item.CuryActualAmount.GetValueOrDefault(0);
							revisedAmount += item.CuryRevisedAmount.GetValueOrDefault(0);
						}
					}
					if (row.CompletedPctMethod == PMCompletedPctMethod.ByAmount)
						e.ReturnValue = revisedAmount == 0 ? 0 : Convert.ToDecimal(100 * actualAmount / revisedAmount);
					else
						e.ReturnValue = Convert.ToDecimal(percentSum) == 0 ? 0 : Convert.ToDecimal(percentSum / recordCount);
					e.ReturnState = PXFieldState.CreateInstance(e.ReturnValue, typeof(decimal?), false, false, 0, 2, 0, 0, nameof(PMTask.completedPercent), null, null, null, PXErrorLevel.Undefined, false, true, true, PXUIVisibility.Visible, null, null, null);
				}
			}
		}

		protected virtual void PMTask_IsActive_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PMTask row = e.Row as PMTask;
			if (row != null && e.NewValue != null && ((bool)e.NewValue) == true)
			{
				PMProject project = Project.Select();
				if (project != null)
				{
					if (project.IsActive == false)
					{
						sender.RaiseExceptionHandling<PMTask.status>(e.Row, e.NewValue, new PXSetPropertyException(Warnings.ProjectIsNotActive, PXErrorLevel.Warning));
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMTask, PMTask.isDefault> e)
		{
			if (e.Row.IsDefault == true)
			{
				var select = new PXSelect<PMTask, Where<PMTask.projectID, Equal<Required<PMTask.projectID>>>>(this);
				foreach (PMTask task in select.Select(e.Row.ProjectID))
				{
					if (task.IsDefault == true && task.TaskID != e.Row.TaskID)
					{
						Task.Cache.SetValue<PMTask.isDefault>(task, false);
						Task.Cache.SmartSetStatus(task, PXEntryStatus.Updated);
					}
				}
			}
		}


		protected virtual void PMTask_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
		{
			PMTask row = e.Row as PMTask;
			if (row == null)
				return;

			if (row.IsActive == true && row.IsCancelled == false)
			{
				throw new PXException(Messages.OnlyPlannedCanbeDeleted);
			}

			//validate that all child records can be deleted:

			PMTran tran = PXSelect<PMTran, Where<PMTran.projectID, Equal<Required<PMTask.projectID>>, And<PMTran.taskID, Equal<Required<PMTask.taskID>>>>>.SelectWindowed(this, 0, 1, row.ProjectID, row.TaskID);
			if ( tran != null )
			{
				throw new PXException(Messages.HasTranData);
			}

			PMTimeActivity activity = PXSelect<PMTimeActivity, Where<PMTimeActivity.projectID, Equal<Required<PMTask.projectID>>, And<PMTimeActivity.projectTaskID, Equal<Required<PMTask.taskID>>>>>.SelectWindowed(this, 0, 1, row.ProjectID, row.TaskID);
			if (activity != null)
			{
				throw new PXException(Messages.HasActivityData);
			}

            EP.EPTimeCardItem timeCardItem = PXSelect<EP.EPTimeCardItem, Where<EP.EPTimeCardItem.projectID, Equal<Required<PMTask.projectID>>, And<EP.EPTimeCardItem.taskID, Equal<Required<PMTask.taskID>>>>>.SelectWindowed(this, 0, 1, row.ProjectID, row.TaskID);
            if (timeCardItem != null)
            {
                throw new PXException(Messages.HasTimeCardItemData);
            }
		}

		
		
		protected virtual void PMTask_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			PMTask row = e.Row as PMTask;
			PMProject project = PXSelect<PMProject, Where<PMProject.contractID, Equal<Current<PMTask.projectID>>>>.Select(this);
			if (row != null && project != null)
			{
				row.CustomerID = project.CustomerID;
				row.BillingID = project.BillingID;
				row.AllocationID = project.AllocationID;
				row.DefaultSalesAccountID = project.DefaultSalesAccountID;
				row.DefaultSalesSubID = project.DefaultSalesSubID;
				row.DefaultExpenseAccountID = project.DefaultExpenseAccountID;
				row.DefaultExpenseSubID = project.DefaultExpenseSubID;
			}
		}

		protected virtual void PMTask_ProjectID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            PMTask row = e.Row as PMTask;
            if (row != null)
            {
                sender.SetDefaultExt<PMTask.visibleInAP>(row);
                sender.SetDefaultExt<PMTask.visibleInAR>(row);
                sender.SetDefaultExt<PMTask.visibleInCA>(row);
                sender.SetDefaultExt<PMTask.visibleInCR>(row);				
				sender.SetDefaultExt<PMTask.visibleInTA>(row);
				sender.SetDefaultExt<PMTask.visibleInEA>(row);
				sender.SetDefaultExt<PMTask.visibleInGL>(row);
                sender.SetDefaultExt<PMTask.visibleInIN>(row);
                sender.SetDefaultExt<PMTask.visibleInPO>(row);
                sender.SetDefaultExt<PMTask.visibleInSO>(row);
                sender.SetDefaultExt<PMTask.customerID>(row);
                sender.SetDefaultExt<PMTask.locationID>(row);
                sender.SetDefaultExt<PMTask.rateTableID>(row);
            }
        }
		protected virtual void _(Events.FieldUpdated<PMRecurringItem, PMRecurringItem.inventoryID> e)
		{
			e.Cache.SetDefaultExt<PMRecurringItem.description>(e.Row);
			e.Cache.SetDefaultExt<PMRecurringItem.uOM>(e.Row);
			e.Cache.SetDefaultExt<PMRecurringItem.amount>(e.Row);
		}


		protected virtual void _(Events.FieldDefaulting<PMRecurringItem, PMRecurringItem.amount> e)
		{
			if (e.Row == null) return;
			InventoryItem item = PXSelect<InventoryItem, Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>.Select(this, e.Row.InventoryID);
			if (item != null)
			{
				e.NewValue = item.BasePrice;
			}
		}

		protected virtual void _(Events.RowSelected<PMRecurringItem> e)
		{
			if (e.Row != null && Task.Current != null)
            {
                PXUIFieldAttribute.SetEnabled<PMRecurringItem.included>(e.Cache, e.Row, Task.Current.IsActive != true);
				PXUIFieldAttribute.SetEnabled<PMRecurringItem.accountID>(e.Cache, e.Row, e.Row.AccountSource != PMAccountSource.None);
				PXUIFieldAttribute.SetEnabled<PMRecurringItem.subID>(e.Cache, e.Row, e.Row.AccountSource != PMAccountSource.None);
				PXUIFieldAttribute.SetEnabled<PMRecurringItem.subMask>(e.Cache, e.Row, e.Row.AccountSource != PMAccountSource.None);
            }
        }

		#endregion

		public virtual string GetStatusFromFlags(PMTask task)
		{
			if (task == null)
				return ProjectTaskStatus.Planned;

			if (task.IsCancelled == true)
				return ProjectTaskStatus.Canceled;

			if (task.IsCompleted == true)
				return ProjectTaskStatus.Completed;

			if (task.IsActive == true)
				return ProjectTaskStatus.Active;

			return ProjectTaskStatus.Planned;
		}

		public virtual void SetFieldStateByStatus(PMTask task, string status)
		{
			
		}		
	}
}
