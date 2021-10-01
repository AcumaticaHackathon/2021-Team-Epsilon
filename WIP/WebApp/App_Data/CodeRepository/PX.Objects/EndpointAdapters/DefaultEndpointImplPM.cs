using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Adapters;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.PM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.EndpointAdapters
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	[PXVersion("20.200.001", "Default")]
	internal class DefaultEndpointImplPM : IAdapterWithMetadata
	{
		public IEntityMetadataProvider MetadataProvider { protected get; set; }

		protected CbApiWorkflowApplicator.ProjectTemplateApplicator ProjectTemplateApplicator { get; }
		protected CbApiWorkflowApplicator.ProjectTaskApplicator ProjectTaskApplicator { get; }

		public DefaultEndpointImplPM(
			CbApiWorkflowApplicator.ProjectTemplateApplicator projectTemplateApplicator,
			CbApiWorkflowApplicator.ProjectTaskApplicator projectTaskApplicator)
		{
			ProjectTemplateApplicator = projectTemplateApplicator;
			ProjectTaskApplicator = projectTaskApplicator;
		}

		[FieldsProcessed(new[] { "ProjectTemplateID", "Status" })]
		protected virtual void ProjectTemplate_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			TemplateMaint templateGraph = (TemplateMaint)graph;

			EntityValueField contractCDField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ProjectTemplateID") as EntityValueField;

			PMProject template = (PMProject)templateGraph.Project.Cache.CreateInstance();

			if (contractCDField == null) templateGraph.Project.Cache.SetDefaultExt<PMProject.contractCD>(template);
			else templateGraph.Project.Cache.SetValueExt<PMProject.contractCD>(template, contractCDField.Value);

			templateGraph.Project.Current = templateGraph.Project.Insert(template);
			
			ProjectTemplateApplicator.ApplyStatusChange(graph, MetadataProvider, entity);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void ProjectTemplate_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			EnsureAndGetCurrentForUpdate<PMProject>(graph);
			ProjectTemplateApplicator.ApplyStatusChange(graph, MetadataProvider, entity);
		}

		[FieldsProcessed(new[] { "ProjectTaskID", "ProjectID", "Status" })]
		protected virtual void ProjectTask_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			ProjectTaskEntry taskGraph = (ProjectTaskEntry)graph;

			EntityValueField contractCDField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ProjectID") as EntityValueField;
			EntityValueField taskCDField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ProjectTaskID") as EntityValueField;

			PMTask task = (PMTask)taskGraph.Task.Cache.CreateInstance();

			if (contractCDField == null) taskGraph.Task.Cache.SetDefaultExt<PMTask.projectID>(task);
			else
			{
				PMProject project = PXSelect<PMProject, Where<PMProject.contractCD, Equal<Required<PMProject.contractCD>>>>.Select(graph, contractCDField.Value);
				taskGraph.Task.Cache.SetValueExt<PMTask.projectID>(task, project.ContractID);
			}

			if (taskCDField == null) taskGraph.Task.Cache.SetDefaultExt<PMTask.taskCD>(task);
			else taskGraph.Task.Cache.SetValueExt<PMTask.taskCD>(task, taskCDField.Value);

			taskGraph.Task.Current = taskGraph.Task.Insert(task);

			ProjectTaskApplicator.ApplyStatusChange(graph, MetadataProvider, entity);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void ProjectTask_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			EnsureAndGetCurrentForUpdate<PMTask>(graph);
			ProjectTaskApplicator.ApplyStatusChange(graph, MetadataProvider, entity);
		}

		private T EnsureAndGetCurrentForUpdate<T>(PXGraph graph) where T : class, IBqlTable, new()
		{
			var cache = graph.Caches[typeof(T)];
			if (cache.Current as T == null)
			{
				PXTrace.WriteWarning("No entity in cache for update. Create new entity instead.");

				// just for sure, if there is trash too
				graph.Clear();

				return cache.Insert() as T;
			}

			return cache.Current as T;
		}
	}
}
