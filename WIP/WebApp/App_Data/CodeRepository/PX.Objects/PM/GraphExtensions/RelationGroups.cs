using PX.Data;
using PX.Objects.CT;
using PX.Objects.IN;
using PX.SM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
	public class RelationGroupsExt : PXGraphExtension<RelationGroups>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.projectModule>();
		}

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		protected void _(Events.CacheAttached<Contract.templateID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		protected void _(Events.CacheAttached<Contract.duration> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBString(1)]
		protected void _(Events.CacheAttached<Contract.durationType> e) { }

		[PXOverride]
		public bool CanBeRestricted(Type entityType, object instance)
		{
			if (entityType == typeof(InventoryItem))
			{
				InventoryItem item = instance as InventoryItem;
				if (item != null)
				{
					return item.ItemStatus != InventoryItemStatus.Unknown;
				}
			}

			if (entityType == typeof(Contract) || entityType == typeof(PMProject))
			{
				Contract item = instance as Contract;
				if (item != null)
				{
					return item.NonProject != true && item.BaseType == CTPRType.Project;
				}
			}

			return true;
		}
	}
}
