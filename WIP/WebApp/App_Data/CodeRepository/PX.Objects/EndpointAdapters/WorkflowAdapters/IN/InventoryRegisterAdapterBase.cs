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
	internal abstract class InventoryRegisterAdapterBase
	{
		protected void INRegisterInsert(INRegisterEntryBase registerEntry, EntityImpl entity, EntityImpl targetEntity)
		{
			bool isNew = true;

			var nbrField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ReferenceNbr") as EntityValueField;
			var holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;

			INRegister register = nbrField?.Value != null
				? registerEntry.INRegisterDataMember.Search<INRegister.refNbr>(nbrField.Value)
				: null;

			if (register == null)
			{
				register = (INRegister)registerEntry.INRegisterDataMember.Cache.CreateInstance();

				if (nbrField != null)
					register.RefNbr = nbrField.Value;
			}
			else
				isNew = false;

			registerEntry.INRegisterDataMember.Current = isNew
				? registerEntry.INRegisterDataMember.Insert(register)
				: registerEntry.INRegisterDataMember.Update(register);

			registerEntry.SubscribeToPersistDependingOnBoolField(holdField, registerEntry.putOnHold, registerEntry.releaseFromHold);
		}

		protected void INRegisterUpdate(INRegisterEntryBase registerEntry, EntityImpl entity, EntityImpl targetEntity)
		{
			var holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			registerEntry.SubscribeToPersistDependingOnBoolField(holdField, registerEntry.putOnHold, registerEntry.releaseFromHold);
		}
	}
}