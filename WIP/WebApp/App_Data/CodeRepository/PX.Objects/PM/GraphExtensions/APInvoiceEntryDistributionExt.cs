using PX.Data;
using PX.Objects.AP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.PO;
using PX.Objects.CM;
using PX.Objects.IN;

namespace PX.Objects.PM
{
	/// <summary>
	/// Extends AP Invoice Entry with Project related functionality. Requires Project Accounting feature and Distribution feature.
	/// </summary>
	public class APInvoiceEntryDistributionExt : PXGraphExtension<APInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.projectAccounting>() && PXAccess.FeatureInstalled<CS.FeaturesSet.distributionModule>();
		}

		public virtual void POOrderRS_Selected_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((bool)e.NewValue == true)
			{
				POOrder row = (POOrder)e.Row;
				List<POLineS> orderLines = Base.GetPOOrderLines(row, Base.Document.Current, true);

				HashSet<string> inactiveProjects = new HashSet<string>();
				HashSet<string> inactiveProjectTasks = new HashSet<string>();
				foreach (POLineS line in orderLines)
				{
					if (line.TaskID != null)
					{
						PMProject project = PXSelect<PMProject, Where<PMProject.contractID, Equal<Required<PMProject.contractID>>>>.Select(Base, line.ProjectID);
						if (project.IsActive != true)
						{
							inactiveProjects.Add(string.Format(Messages.ProjectTraceItem, project.ContractCD));
						}
						else
						{
							PMTask task = PXSelect<PMTask, Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
								And<PMTask.taskID, Equal<Required<PMTask.taskID>>>>>.Select(Base, line.ProjectID, line.TaskID);
							if (task.IsActive != true)
							{
								inactiveProjectTasks.Add(string.Format(Messages.ProjectTaskTraceItem, project.ContractCD, task.TaskCD));
							}
						}
					}
				}

				if (inactiveProjects.Count > 0)
				{
					string traceMessage =
						Messages.POOrderWithProjectIsNotActiveTraceCaption + Environment.NewLine +
						string.Join(Environment.NewLine, inactiveProjects.OrderBy(p => p));
					PXTrace.WriteError(traceMessage);
					throw new PXSetPropertyException(Messages.POOrderWithProjectIsNotActive, PXErrorLevel.RowError);
				}
				else if (inactiveProjectTasks.Count > 0)
				{
					string traceMessage =
						Messages.POOrderWithProjectTaskIsNotActiveTraceCaption + Environment.NewLine +
						string.Join(Environment.NewLine, inactiveProjectTasks.OrderBy(p => p));
					PXTrace.WriteError(traceMessage);
					throw new PXSetPropertyException(Messages.POOrderWithProjectTaskIsNotActive, PXErrorLevel.RowError);
				}
			}
		}

		public virtual void POLineRS_Selected_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			POLineRS line = (POLineRS)e.Row;

			string errorMessage = null;
			if ((bool)e.NewValue == true && line.TaskID != null)
			{
				PMProject project = PXSelect<PMProject, Where<PMProject.contractID, Equal<Required<PMProject.contractID>>>>.Select(Base, line.ProjectID);
				if (project.IsActive != true)
				{
					errorMessage = PO.Messages.ProjectIsNotActive;
				}
				else
				{
					PMTask task = PXSelect<PMTask, Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
								And<PMTask.taskID, Equal<Required<PMTask.taskID>>>>>.Select(Base, line.ProjectID, line.TaskID);
					if (task.IsActive != true)
					{
						errorMessage = PO.Messages.ProjectTaskIsNotActive;
					}
				}
			}

			if (errorMessage != null)
			{
				throw new PXSetPropertyException(errorMessage, PXErrorLevel.RowError);
			}
		}

		public virtual void POReceipt_Selected_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((bool)e.NewValue == true)
			{
				POReceipt row = (POReceipt)e.Row;
				List<POReceiptLineS> receiptLines = Base.GetInvoicePOReceiptLines(row, Base.Document.Current);

				HashSet<string> inactiveProjects = new HashSet<string>();
				HashSet<string> inactiveProjectTasks = new HashSet<string>();
				foreach (POReceiptLineS line in receiptLines)
				{
					if (line.TaskID != null)
					{
						PMProject project = PXSelect<PMProject, Where<PMProject.contractID, Equal<Required<PMProject.contractID>>>>.Select(Base, line.ProjectID);
						if (project.IsActive != true)
						{
							inactiveProjects.Add(string.Format(Messages.ProjectTraceItem, project.ContractCD));
						}
						else
						{
							PMTask task = PXSelect<PMTask, Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
								And<PMTask.taskID, Equal<Required<PMTask.taskID>>>>>.Select(Base, line.ProjectID, line.TaskID);
							if (task.IsActive != true)
							{
								inactiveProjectTasks.Add(string.Format(Messages.ProjectTaskTraceItem, project.ContractCD, task.TaskCD));
							}
						}
					}
				}

				if (inactiveProjects.Count > 0)
				{
					string traceMessage =
						Messages.POReceiptWithProjectIsNotActiveTraceCaption + Environment.NewLine +
						string.Join(Environment.NewLine, inactiveProjects.OrderBy(p => p));
					PXTrace.WriteError(traceMessage);
					throw new PXSetPropertyException(Messages.POReceiptWithProjectIsNotActive, PXErrorLevel.RowError);
				}
				else if (inactiveProjectTasks.Count > 0)
				{
					string traceMessage =
						Messages.POReceiptWithProjectTaskIsNotActiveTraceCaption + Environment.NewLine +
						string.Join(Environment.NewLine, inactiveProjectTasks.OrderBy(p => p));
					PXTrace.WriteError(traceMessage);
					throw new PXSetPropertyException(Messages.POReceiptWithProjectTaskIsNotActive, PXErrorLevel.RowError);
				}
			}
		}

		public virtual void POReceiptLineAdd_Selected_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			POReceiptLineS line = (POReceiptLineS)e.Row;

			string errorMessage = null;
			if ((bool)e.NewValue == true && line.TaskID != null)
			{
				PMProject project = PXSelect<PMProject, Where<PMProject.contractID, Equal<Required<PMProject.contractID>>>>.Select(Base, line.ProjectID);
				if (project.IsActive != true)
				{
					errorMessage = PO.Messages.ProjectIsNotActive;
				}
				else
				{
					PMTask task = PXSelect<PMTask, Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
								And<PMTask.taskID, Equal<Required<PMTask.taskID>>>>>.Select(Base, line.ProjectID, line.TaskID);
					if (task.IsActive != true)
					{
						errorMessage = PO.Messages.ProjectTaskIsNotActive;
					}
				}
			}

			if (errorMessage != null)
			{
				throw new PXSetPropertyException(errorMessage, PXErrorLevel.RowError);
			}
		}
	}
}
