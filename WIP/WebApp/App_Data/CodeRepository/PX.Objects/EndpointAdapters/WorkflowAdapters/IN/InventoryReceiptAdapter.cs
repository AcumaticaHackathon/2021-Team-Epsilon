using System;
using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.EndpointAdapters.WorkflowAdapters.IN
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	[PXVersion("20.200.001", "Default")]
	internal class InventoryReceiptAdapter : InventoryRegisterAdapterBase
	{
		[FieldsProcessed(new[] {
			"ReferenceNbr",
			"Hold"
		})]
		protected virtual void InventoryReceipt_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
			=> INRegisterInsert((INRegisterEntryBase)graph, entity, targetEntity);

		[FieldsProcessed(new[] {
			"Hold"
		})]
		protected virtual void InventoryReceipt_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
			=> INRegisterUpdate((INRegisterEntryBase)graph, entity, targetEntity);
	}
}