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
	internal class CheckAdapter
	{
		[FieldsProcessed(new[] { "Type", "ReferenceNbr", "Hold" })]
		protected void Check_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			APPaymentEntry checkGraph = (APPaymentEntry)graph;
			checkGraph.Document.Cache.Current = null;

			EntityValueField typeField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Type") as EntityValueField;
			EntityValueField nbrField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ReferenceNbr") as EntityValueField;
			EntityValueField holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;

			APPayment check = (APPayment)checkGraph.Document.Cache.CreateInstance();

			if (typeField == null) checkGraph.Document.Cache.SetDefaultExt<APRegister.docType>(check);
			else checkGraph.SetDropDownValue<APPayment.docType, APPayment>(typeField.Value, check);

			if (nbrField == null) checkGraph.Document.Cache.SetDefaultExt<APPayment.refNbr>(check);
			else checkGraph.Document.Cache.SetValueExt<APPayment.refNbr>(check, nbrField.Value);

			checkGraph.Document.Current = checkGraph.Document.Insert(check);
			checkGraph.SubscribeToPersistDependingOnBoolField(holdField, checkGraph.putOnHold, checkGraph.releaseFromHold);
		}

		[FieldsProcessed(new[] { "Hold" })]
		protected void Check_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			APPaymentEntry checkGraph = (APPaymentEntry)graph;
			EntityValueField holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			checkGraph.SubscribeToPersistDependingOnBoolField(holdField, checkGraph.putOnHold, checkGraph.releaseFromHold);
		}

		protected void Action_ReleaseCheck(PXGraph graph, ActionImpl action)
		{
			APPaymentEntry checkGraph = (APPaymentEntry)graph;
			checkGraph.Views[checkGraph.PrimaryView].Answer = WebDialogResult.Yes;
			checkGraph.Save.Press();
			checkGraph.release.Press();
		}

		protected void Action_VoidCheck(PXGraph graph, ActionImpl action)
		{
			APPaymentEntry checkGraph = (APPaymentEntry)graph;
			checkGraph.Save.Press();
			checkGraph.voidCheck.Press();
		}
	}
}