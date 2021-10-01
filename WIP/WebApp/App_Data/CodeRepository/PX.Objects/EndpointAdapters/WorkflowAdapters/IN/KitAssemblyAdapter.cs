using System;
using System.Linq;
using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Adapters;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.EndpointAdapters.WorkflowAdapters.IN
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	[PXVersion("20.200.001", "Default")]
	internal class KitAssemblyAdapter
	{
		[FieldsProcessed(new[] {
			"ReferenceNbr",
			"Type",
			"Hold"
		})]
		protected virtual void KitAssembly_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			// Note that for v18 endpoint the DefaultEndpointImpl18.KitAssembly_Insert is called instead of this method
			var kitEntry = (KitAssemblyEntry)graph;

			var nbrField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ReferenceNbr") as EntityValueField;
			var typeField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Type") as EntityValueField;
			var holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;

			var kit = (INKitRegister)kitEntry.Document.Cache.CreateInstance();

			if (typeField != null)
				kitEntry.Document.Cache.SetValueExt<INKitRegister.docType>(kit, typeField.Value);

			if (nbrField != null)
				kit.RefNbr = nbrField.Value;

			kitEntry.Document.Current = kitEntry.Document.Insert(kit);
			kitEntry.SubscribeToPersistDependingOnBoolField(holdField, kitEntry.putOnHold, kitEntry.releaseFromHold);
		}

		[FieldsProcessed(new[] {
			"Hold"
		})]
		protected virtual void KitAssembly_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var kitEntry = (KitAssemblyEntry)graph;
			var holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			kitEntry.SubscribeToPersistDependingOnBoolField(holdField, kitEntry.putOnHold, kitEntry.releaseFromHold);
		}
	}
}