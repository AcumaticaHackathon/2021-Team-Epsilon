using System;
using System.Linq;
using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.SO;

namespace PX.Objects.EndpointAdapters.WorkflowAdapters.SO
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	[PXVersion("20.200.001", "Default")]
	internal class SalesOrderAdapter
	{
		[FieldsProcessed(new[] {
			"OrderType",
			"OrderNbr",
			"Hold",
			"CreditHold"
		})]
		protected virtual void SalesOrder_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var salesOrderGraph = (SOOrderEntry)graph;

			var orderTypeField = targetEntity.Fields.SingleOrDefault(f => f.Name == "OrderType") as EntityValueField;
			var orderNbrField = targetEntity.Fields.SingleOrDefault(f => f.Name == "OrderNbr") as EntityValueField;
			var holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			var creditHoldField = targetEntity.Fields.SingleOrDefault(f => f.Name == "CreditHold") as EntityValueField;

			var soOrder = (SOOrder)salesOrderGraph.Document.Cache.CreateInstance();

			if (orderTypeField != null)
				soOrder.OrderType = orderTypeField.Value;

			if (orderNbrField != null)
				soOrder.OrderNbr = orderNbrField.Value;

			salesOrderGraph.Document.Current = salesOrderGraph.Document.Insert(soOrder);
			salesOrderGraph.SubscribeToPersistDependingOnBoolField(holdField, salesOrderGraph.putOnHold, salesOrderGraph.releaseFromHold);
			salesOrderGraph.SubscribeToPersistDependingOnBoolField(creditHoldField, null, salesOrderGraph.releaseFromCreditHold);
		}

		[FieldsProcessed(new[] {
			"Hold",
			"CreditHold"
		})]
		protected virtual void SalesOrder_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var salesOrderGraph = (SOOrderEntry)graph;

			salesOrderGraph.Document.View.Answer = WebDialogResult.Yes;

			var holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			var creditHoldField = targetEntity.Fields.SingleOrDefault(f => f.Name == "CreditHold") as EntityValueField;

			salesOrderGraph.SubscribeToPersistDependingOnBoolField(holdField, salesOrderGraph.putOnHold, salesOrderGraph.releaseFromHold);
			salesOrderGraph.SubscribeToPersistDependingOnBoolField(creditHoldField, null, salesOrderGraph.releaseFromCreditHold);
		}
	}
}