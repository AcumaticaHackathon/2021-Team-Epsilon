using System;
using System.Linq;
using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.AP;

namespace PX.Objects.EndpointAdapters.WorkflowAdapters.AP
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	[PXVersion("20.200.001", "Default")]
	internal class BillAdapter
	{
		[FieldsProcessed(new[] { "Type", "ReferenceNbr", "Hold" })]
		protected void Bill_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			APInvoiceEntry billGraph = (APInvoiceEntry)graph;

			EntityValueField typeField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Type") as EntityValueField;
			EntityValueField nbrField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ReferenceNbr") as EntityValueField;
			EntityValueField holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;

			APInvoice bill = (APInvoice)billGraph.Document.Cache.CreateInstance();

			if (typeField == null) billGraph.Document.Cache.SetDefaultExt<APRegister.docType>(bill);
			else billGraph.SetDropDownValue<APInvoice.docType, APInvoice>(typeField.Value, bill);

			if (nbrField == null) billGraph.Document.Cache.SetDefaultExt<APInvoice.refNbr>(bill);
			else billGraph.Document.Cache.SetValueExt<APInvoice.refNbr>(bill, nbrField.Value);

			billGraph.Document.Current = billGraph.Document.Insert(bill);
			billGraph.SubscribeToPersistDependingOnBoolField(holdField, billGraph.putOnHold, billGraph.releaseFromHold, SupressErrors);
		}

		[FieldsProcessed(new[] { "Hold" })]
		protected void Bill_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			APInvoiceEntry billGraph = (APInvoiceEntry)graph;
			EntityValueField holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			billGraph.SubscribeToPersistDependingOnBoolField(holdField, billGraph.putOnHold, billGraph.releaseFromHold, SupressErrors);
		}

		private void SupressErrors(PXCache<APInvoice> bill)
		{
			bill.RaiseExceptionHandling<APRegister.curyOrigDocAmt>(bill.Current, ((APInvoice)bill.Current).CuryOrigDocAmt, null);
		}

		protected void Action_ReleaseBill(PXGraph graph, ActionImpl action)
		{
			APInvoiceEntry billGraph = (APInvoiceEntry)graph;
			billGraph.Save.Press();
			billGraph.release.Press();
		}
	}
}