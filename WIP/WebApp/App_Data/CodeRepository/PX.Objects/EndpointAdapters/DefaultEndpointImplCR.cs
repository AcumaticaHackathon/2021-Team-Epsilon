using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Adapters;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.AP;
using PX.Objects.AR;
using System;
using System.Linq;

namespace PX.Objects.EndpointAdapters
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	internal class DefaultEndpointImplCR : DefaultEndpointImplCRBase
	{
		public DefaultEndpointImplCR(
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
	}
}
