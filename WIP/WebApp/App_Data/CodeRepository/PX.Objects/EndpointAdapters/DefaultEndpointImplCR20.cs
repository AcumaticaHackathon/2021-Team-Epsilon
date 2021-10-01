using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Adapters;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.AR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Compilation;
using PX.Objects.AP;
using EntityHelper = PX.Data.EntityHelper;

namespace PX.Objects.EndpointAdapters
{
	[PXVersion("20.200.001", "Default")]
	internal class DefaultEndpointImplCR20 : DefaultEndpointImplCRBase
	{
		public DefaultEndpointImplCR20(
			CbApiWorkflowApplicator.CaseApplicator caseApplicator,
			CbApiWorkflowApplicator.OpportunityApplicator opportunityApplicator,
			CbApiWorkflowApplicator.LeadApplicator leadApplicator,
			CbApiWorkflowApplicator.CustomerApplicator customerApplicator,
			CbApiWorkflowApplicator.VendorApplicator vendorApplicator,
			CbApiWorkflowApplicator.BusinessAccountApplicator businessAccountApplicator) 
			: base(
				caseApplicator,
				opportunityApplicator,
				leadApplicator,
				customerApplicator,
				vendorApplicator,
				businessAccountApplicator)
		{
		}



		// (almost) copy paster from ServiceManager
		/// <summary>
		/// Returns true when specifeid type is a BqlTable object
		/// </summary>
		static bool IsTable(Type t)
		{

			return t != null
				&& typeof(IBqlTable).IsAssignableFrom(t)
				&& !typeof(PXMappedCacheExtension).IsAssignableFrom(t);
		}

		[FieldsProcessed(new[] {
			"RelatedEntityNoteID",
			"RelatedEntityType",
			"RelatedEntityDescription" })]
		protected virtual void Activity_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			Activity_Insert_Impl<CRActivity>(graph, entity, targetEntity);
		}

		[FieldsProcessed(new[] {
			"RelatedEntityNoteID",
			"RelatedEntityType",
			"RelatedEntityDescription" })]
		protected virtual void Activity_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			Activity_Update_Impl<CRActivity>(graph, entity, targetEntity);
		}

		[FieldsProcessed(new[] {
			"RelatedEntityNoteID",
			"RelatedEntityType",
			"RelatedEntityDescription" })]
		protected virtual void Email_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			Activity_Insert_Impl<CRSMEmail>(graph, entity, targetEntity);
		}

		[FieldsProcessed(new[] {
			"RelatedEntityNoteID",
			"RelatedEntityType",
			"RelatedEntityDescription" })]
		protected virtual void Email_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			Activity_Update_Impl<CRSMEmail>(graph, entity, targetEntity);
		}

		[FieldsProcessed(new[] {
			"RelatedEntityNoteID",
			"RelatedEntityType",
			"RelatedEntityDescription" })]
		protected virtual void Task_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			Activity_Insert_Impl<CRActivity>(graph, entity, targetEntity);
		}

		[FieldsProcessed(new[] {
			"RelatedEntityNoteID",
			"RelatedEntityType",
			"RelatedEntityDescription" })]
		protected virtual void Task_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			Activity_Update_Impl<CRActivity>(graph, entity, targetEntity);
		}

		[FieldsProcessed(new[] {
			"RelatedEntityNoteID",
			"RelatedEntityType",
			"RelatedEntityDescription" })]
		protected virtual void Event_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			Activity_Insert_Impl<CRActivity>(graph, entity, targetEntity);
		}

		[FieldsProcessed(new[] {
			"RelatedEntityNoteID",
			"RelatedEntityType",
			"RelatedEntityDescription" })]
		protected virtual void Event_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			Activity_Update_Impl<CRActivity>(graph, entity, targetEntity);
		}

		private void Activity_Insert_Impl<T>(PXGraph graph, EntityImpl entity, EntityImpl targetEntity) where T : CRActivity, new()
		{
			// process only Top-Level
			if (entity != targetEntity)
				return;

			var activity = EnsureAndGetCurrentForInsert<T>(graph);
			Activity_Insert_Update_Impl(graph, graph.Caches<T>(), activity, targetEntity);
		}

		private void Activity_Update_Impl<T>(PXGraph graph, EntityImpl entity,  EntityImpl targetEntity) where T : CRActivity, new()
		{
			// process only Top-Level
			if (entity != targetEntity)
				return;

			var activity = EnsureAndGetCurrentForUpdate<T>(graph);
			Activity_Insert_Update_Impl(graph, graph.Caches<T>(), activity, targetEntity);
		}

		private void Activity_Insert_Update_Impl(PXGraph graph, PXCache cache, CRActivity activity, EntityImpl targetEntity)
		{
			var refNoteIDstr = GetField(targetEntity, "RelatedEntityNoteID")?.Value;
			var refNoteType = GetField(targetEntity, "RelatedEntityType")?.Value;
			if (refNoteIDstr != null && Guid.TryParse(refNoteIDstr, out Guid refNoteID))
			{
				var helper = new EntityHelper(graph);
				var note = helper.SelectNote(refNoteID);
				Type type;
				if (note == null)
				{
					if (refNoteType == null)
					{
						PXTrace.WriteError("Note cannot be found and RelatedEntityType is not a specified");
						return;
					}
					type = PXBuildManager.GetType(refNoteType, false);
					if (!IsTable(type))
					{
						PXTrace.WriteError("Note cannot be found and RelatedEntityType is not a table: {type}", refNoteType);
						return;
					}
					var noteAttribute = EntityHelper.GetNoteAttribute(type);
					if (noteAttribute == null || !noteAttribute.ShowInReferenceSelector)
					{
						PXTrace.WriteError("RelatedEntityType is not supported as Related Entity. Type: {type}", refNoteType);
						return;
					}

					PXNoteAttribute.InsertNoteRecord(graph.Caches[type], refNoteID);
				}
				else
				{
					type = System.Web.Compilation.PXBuildManager.GetType(note.EntityType, false);
				}
				if (type == typeof(BAccount))
				{
					activity.BAccountID = graph
						.Select<BAccount>()
						.Where(b => b.NoteID == refNoteID)
						.Select(b => b.BAccountID)
						.FirstOrDefault();
				}
				else if (type == typeof(Contact))
				{
					var item = graph
						.Select<Contact>()
						.Where(c => c.NoteID == refNoteID)
						.Select(c => new { c.BAccountID, c.ContactID })
						.FirstOrDefault();

					activity.BAccountID = item?.BAccountID;
					activity.ContactID = item?.ContactID;
				}

				activity.RefNoteID = refNoteID;
				cache.Update(activity);
			}
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Opportunity_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is OpportunityMaint))
				return;

			var current = EnsureAndGetCurrentForInsert<CROpportunity>(graph);
			OpportunityApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Opportunity_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is OpportunityMaint))
				return;

			var current = EnsureAndGetCurrentForUpdate<CROpportunity>(graph);
			OpportunityApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Case_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is CRCaseMaint))
				return;

			var current = EnsureAndGetCurrentForInsert<CRCase>(graph);
			CaseApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Case_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is CRCaseMaint))
				return;

			var current = EnsureAndGetCurrentForUpdate<CRCase>(graph);
			CaseApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Lead_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is LeadMaint))
				return;

			var current = EnsureAndGetCurrentForInsert<CRLead>(graph);
			LeadApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Lead_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is LeadMaint))
				return;

			var current = EnsureAndGetCurrentForUpdate<CRLead>(graph);
			LeadApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status", "CustomerID" })]
		protected virtual void Customer_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is CustomerMaint maint))
				return;

			var current = EnsureAndGetCurrentForInsert<Customer>(graph, (Customer c) =>
			{
				object value = (entity.Fields.SingleOrDefault(f => f.Name == "CustomerID") as EntityValueField)?.Value;

				if (value == null) return c;

				var state = graph.Caches[typeof(Customer)].GetStateExt<Customer.acctCD>(c) as PXStringState;

				if (state == null) return c;

				c.AcctCD = PX.Common.Mask.Parse(state.InputMask, (string)value);

				return c;
			});
			CustomerApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);

			maint.BAccount.View.SetAnswer(null, WebDialogResult.Yes); // for Customer_CustomerClassID_FieldVerifying
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Customer_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is CustomerMaint maint))
				return;

			var current = EnsureAndGetCurrentForUpdate<Customer>(graph);
			CustomerApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);

			maint.BAccount.View.SetAnswer(null, WebDialogResult.Yes); // for Customer_CustomerClassID_FieldVerifying
		}

		[FieldsProcessed(new[] { "Status", "VendorID" })]
		protected virtual void Vendor_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is VendorMaint maint))
				return;

			var current = EnsureAndGetCurrentForInsert<VendorR>(graph, (VendorR v) =>
			{
				object value = (entity.Fields.SingleOrDefault(f => f.Name == "VendorID") as EntityValueField)?.Value;

				if (value == null) return v;

				var state = graph.Caches[typeof(VendorR)].GetStateExt<VendorR.acctCD>(v) as PXStringState;

				if (state == null) return v;

				v.AcctCD = PX.Common.Mask.Parse(state.InputMask, (string)value);

				return v;
			});
			VendorApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);

			maint.BAccount.View.SetAnswer(null, WebDialogResult.Yes); // for Vendor_VendorClassID_FieldVerifying
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Vendor_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is VendorMaint maint))
				return;

			var current = EnsureAndGetCurrentForUpdate<VendorR>(graph);
			VendorApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);

			maint.BAccount.View.SetAnswer(null, WebDialogResult.Yes); // for Vendor_VendorClassID_FieldVerifying
		}

		[FieldsProcessed(new[] { "Status", "BusinessAccountID" })]
		protected virtual void BusinessAccount_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is BusinessAccountMaint))
				return;

			var current = EnsureAndGetCurrentForInsert<BAccount>(graph, (BAccount ba) =>
			{
				object value = (entity.Fields.SingleOrDefault(f => f.Name == "BusinessAccountID") as EntityValueField)?.Value;

				if (value == null) return ba;

				var state = graph.Caches[typeof(BAccount)].GetStateExt<BAccount.acctCD>(ba) as PXStringState;

				if (state == null) return ba;

				ba.AcctCD = PX.Common.Mask.Parse(state.InputMask, (string)value);

				return ba;
			});

			BusinessAccountApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status", "BusinessAccountID" })]
		protected virtual void BusinessAccount_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is BusinessAccountMaint))
				return;

			var current = EnsureAndGetCurrentForUpdate<BAccount>(graph);
			BusinessAccountApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new string[0])]
		protected virtual void ActivityDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			graph.RowPersisting.AddHandler("Activities", (s, e) =>
			{
				throw new PXOuterException(new Dictionary<string, string>(), graph.GetType(),
					e.Row,
					MessagesNoPrefix.CbApi_Activities_ActivityCannotBeInsertUpdatedDeleted);
			});
		}

		[FieldsProcessed(new string[0])]
		protected virtual void ActivityDetail_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			graph.RowPersisting.AddHandler("Activities", (s, e) =>
			{
				throw new PXOuterException(new Dictionary<string, string>(), graph.GetType(),
					e.Row,
					MessagesNoPrefix.CbApi_Activities_ActivityCannotBeInsertUpdatedDeleted);
			});
		}

		protected virtual void ActivityDetail_Delete(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			// TODO: replace with normal error

			throw new PXOuterException(new Dictionary<string, string>(), graph.GetType(),
				graph.Views["Activities"].Cache.Current,
				MessagesNoPrefix.CbApi_Activities_ActivityCannotBeInsertUpdatedDeleted);

		}
	}
}
