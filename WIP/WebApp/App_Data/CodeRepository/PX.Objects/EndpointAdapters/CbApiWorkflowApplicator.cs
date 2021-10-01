using PX.Api.ContractBased;
using PX.Api.ContractBased.Adapters;
using PX.Api.ContractBased.Automation;
using PX.Api.ContractBased.Models;
using PX.Common;
using PX.Data;
using PX.Data.Automation;
using PX.Data.BQL;
using PX.Objects.CR;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.PM;

namespace PX.Objects.EndpointAdapters
{
	internal static class CbApiWorkflowApplicator
	{
		internal class OpportunityApplicator : CbApiWorkflowApplicator<CROpportunity, CROpportunity.status>
		{
			public OpportunityApplicator(IWorkflowServiceWrapper workflowService) : base(workflowService, "Opportunity")
			{
			}
		}

		internal class CaseApplicator : CbApiWorkflowApplicator<CRCase, CRCase.status>
		{
			public CaseApplicator(IWorkflowServiceWrapper workflowService) : base(workflowService, "Case")
			{
			}
		}

		internal class LeadApplicator : CbApiWorkflowApplicator<CRLead, CRLead.status>
		{
			public LeadApplicator(IWorkflowServiceWrapper workflowService) : base(workflowService, "Lead")
			{
			}
		}

		internal class CustomerApplicator : CbApiWorkflowApplicator<Customer, Customer.status>
		{
			public CustomerApplicator(IWorkflowServiceWrapper workflowService) : base(workflowService, "Customer")
			{
			}
		}

		internal class VendorApplicator : CbApiWorkflowApplicator<VendorR, VendorR.vStatus>
		{
			public VendorApplicator(IWorkflowServiceWrapper workflowService) : base(workflowService, "Vendor")
			{
			}
		}

		internal class BusinessAccountApplicator : CbApiWorkflowApplicator<BAccount, BAccount.status>
		{
			public BusinessAccountApplicator(IWorkflowServiceWrapper workflowService) : base(workflowService, "BusinessAccount")
			{
			}
		}

		internal class ProjectTemplateApplicator : CbApiWorkflowApplicator<PMProject, PMProject.status>
		{
			public ProjectTemplateApplicator(IWorkflowServiceWrapper workflowService) : base(workflowService, "ProjectTemplate")
			{
			}
		}

		internal class ProjectTaskApplicator : CbApiWorkflowApplicator<PMTask, PMTask.status>
		{
			public ProjectTaskApplicator(IWorkflowServiceWrapper workflowService) : base(workflowService, "ProjectTask")
			{
			}
		}
	}

	internal abstract class CbApiWorkflowApplicator<TTable, TStatusField>
	where TTable : class, IBqlTable, new()
	where TStatusField : IBqlField
	{
		private readonly IWorkflowServiceWrapper _workflowService;
		private readonly string _entityName;

		public CbApiWorkflowApplicator(IWorkflowServiceWrapper workflowService, string entityName)
		{
			_workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
			_entityName = entityName;
		}

		public void ApplyStatusChange(PXGraph graph, IEntityMetadataProvider metadataProvider, EntityImpl entity, TTable current = null  /* no need for old version */)
		{
			string status = null;
			try
			{
				ApplyStatusChange(graph, metadataProvider, entity, out status);
			}
			// to FCE remain
			catch (InvalidOperationException ioe)
			{
				if (current == null)
					throw;

				graph.Caches<TTable>().RaiseExceptionHandling<TStatusField>(current, status, ioe);
			}
		}

		private void ApplyStatusChange(PXGraph graph, IEntityMetadataProvider metadataProvider, EntityImpl entity, out string status)
		{
			status = GetStatus(metadataProvider, entity);

			if (status == null || _workflowService.IsInSpecifiedStatus(graph, status))
				return;

			var transition = GetTransition(graph, status);

			var action = graph.Actions[transition.ActionName];
			if (action == null)
				throw new InvalidOperationException($"Action named: {transition.ActionName} for specified status: {status} is not available.");

			graph.OnAfterPersist += handler;

			// need mark as dirty to call persist in any case, even if fields weren't updated,
			// otherwise action wouldn't be triggered
			graph.Caches<TTable>().IsDirty = true;

			void handler(PXGraph g)
			{
				g.OnAfterPersist -= handler;
				_workflowService.FillFormValues(transition, entity, graph, metadataProvider);
				action.Press();
			}
		}

		protected virtual string GetStatus(IEntityMetadataProvider metadataProvider, EntityImpl entity)
		{
			var viewName = metadataProvider.GetPrimaryViewName();
			var statusFieldName = metadataProvider.GetMappedFields()
				.Where(i => i.MappedObject == viewName
							&& i.MappedField.Equals(typeof(TStatusField).Name, StringComparison.OrdinalIgnoreCase))
				.Select(i => i.FieldName)
				.FirstOrDefault();
			if (statusFieldName == null)
				throw new NotSupportedException($"Cannot find mapping for Status field: {typeof(TStatusField).Name}.");

			return entity
				.Fields
				.OfType<EntityValueField>()
				.Where(f => statusFieldName.Equals(f.Name, StringComparison.OrdinalIgnoreCase))
				.Select(f => f.Value)
				.FirstOrDefault();
		}

		private TransitionInfo GetTransition(PXGraph graph, string status)
		{
			var transitions = _workflowService.GetPossibleTransition(graph, status).ToList();

			if (transitions.Count == 0)
			{
				string error = $"Cannot find workflow transition applicable for entity: \"{_entityName}\" to status: \"{status}\".";
				PXTrace.WriteWarning("CB-API Warning: " + error);
				throw new InvalidOperationException(error);
			}
			if (transitions.Count > 1)
			{
				PXTrace.WriteVerbose($"CB-API Info: More than one workflow transition applicable for entity: \"{_entityName}\" to status: \"{status}\" is found. ");
			}

			PXTrace.WriteVerbose($"CB-API Info: Use workflow transition with name: { transitions[0].Name} for entity: \"{_entityName}\" to status: \"{status}\".");

			return transitions[0];
		}
	}
}
