using System;
using System.Linq;
using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.AR;

namespace PX.Objects.EndpointAdapters.WorkflowAdapters.AR
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	[PXVersion("20.200.001", "Default")]
	internal class PaymentAdapter
	{
		/// <summary>
		/// Makes Branch value to be set first on AR Payment
		/// </summary>
		[FieldsProcessed(new[] { "Type", "ReferenceNbr", "Branch", "Hold" })]
		protected void Payment_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			ARPaymentEntry paymentGraph = (ARPaymentEntry)graph;

			EntityValueField typeField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Type") as EntityValueField;
			EntityValueField nbrField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ReferenceNbr") as EntityValueField;
			EntityValueField holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			EntityValueField branchField = targetEntity.Fields.OfType<EntityValueField>().SingleOrDefault(f => f.Name == "Branch");

			ARPayment payment = (ARPayment)paymentGraph.Document.Cache.CreateInstance();

			if (typeField == null) paymentGraph.Document.Cache.SetDefaultExt<ARRegister.docType>(payment);
			else paymentGraph.SetDropDownValue<ARPayment.docType, ARPayment>(typeField.Value, payment);

			if (nbrField == null) paymentGraph.Document.Cache.SetDefaultExt<ARPayment.refNbr>(payment);
			else paymentGraph.Document.Cache.SetValueExt<ARPayment.refNbr>(payment, nbrField.Value);

			if (branchField?.Value == null) paymentGraph.Document.Cache.SetDefaultExt<ARPayment.branchID>(payment);
			else paymentGraph.Document.SetValueExt<ARPayment.branchID>(payment, branchField.Value);

			paymentGraph.Document.Current = paymentGraph.Document.Insert(payment);

			paymentGraph.SubscribeToPersistDependingOnBoolField(holdField, paymentGraph.putOnHold, paymentGraph.releaseFromHold);
		}

		[FieldsProcessed(new[] { "Hold" })]
		protected void Payment_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			ARPaymentEntry paymentGraph = (ARPaymentEntry)graph;
			EntityValueField holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			paymentGraph.SubscribeToPersistDependingOnBoolField(holdField, paymentGraph.putOnHold, paymentGraph.releaseFromHold);
		}

		protected void Action_ReleasePayment(PXGraph graph, ActionImpl action)
		{
			ARPaymentEntry checkGraph = (ARPaymentEntry)graph;
			checkGraph.Save.Press();
			checkGraph.release.Press();
		}
	}
}
