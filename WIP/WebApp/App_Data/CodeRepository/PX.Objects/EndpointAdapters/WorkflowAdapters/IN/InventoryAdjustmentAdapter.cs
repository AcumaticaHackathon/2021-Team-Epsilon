using System;
using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.EndpointAdapters.WorkflowAdapters.IN
{
	[PXVersion("20.200.001", "Default")]
	internal class InventoryAdjustmentAdapter : InventoryRegisterAdapterBase
	{
		[FieldsProcessed(new[] {
			"ReferenceNbr",
			"Hold"
		})]
		protected virtual void InventoryAdjustment_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
			=> INRegisterInsert((INRegisterEntryBase)graph, entity, targetEntity);

		[FieldsProcessed(new[] {
			"Hold"
		})]
		protected virtual void InventoryAdjustment_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
			=> INRegisterUpdate((INRegisterEntryBase)graph, entity, targetEntity);
	}

	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	internal class AdjustmentAdapter : InventoryRegisterAdapterBase
	{
		[FieldsProcessed(new[] {
			"ReferenceNbr",
			"Hold"
		})]
		protected virtual void Adjustment_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
			=> INRegisterInsert((INRegisterEntryBase)graph, entity, targetEntity);

		[FieldsProcessed(new[] {
			"Hold"
		})]
		protected virtual void Adjustment_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
			=> INRegisterUpdate((INRegisterEntryBase)graph, entity, targetEntity);
	}
}