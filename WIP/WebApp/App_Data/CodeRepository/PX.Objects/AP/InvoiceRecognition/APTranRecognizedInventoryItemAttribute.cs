using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP.InvoiceRecognition.DAC;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AP.InvoiceRecognition
{
	[PXInt]
	internal class APTranRecognizedInventoryItemAttribute : APTranInventoryItemAttribute
	{
		private readonly Type[] _inventoryRestrictingConditions;

		public APTranRecognizedInventoryItemAttribute()
		{
			_inventoryRestrictingConditions = GetInventoryRestrictingConditions();
			IsDBField = false;
		}

		private Type[] GetInventoryRestrictingConditions()
		{
			return GetAttributes()
				.OfType<PXRestrictorAttribute>()
				.Select(r => r.RestrictingCondition)
				.Where(r => BqlCommand.Decompose(r).All(c => typeof(IBqlField).IsAssignableFrom(c) == false || c.DeclaringType == typeof(InventoryItem)))
				.ToArray();
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			sender.Graph.FieldUpdating.RemoveHandler(sender.GetItemType(), _FieldName, SelectorAttribute.FieldUpdating);
			sender.Graph.FieldUpdating.AddHandler(sender.GetItemType(), _FieldName, FieldUpdating);
		}

		public virtual void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			var row = e.Row as APRecognizedTran;
			if (row == null)
			{
				return;
			}

			var alternateID = e.NewValue as string;

			try
			{
				SelectorAttribute.FieldUpdating(sender, e);
				return;
			}
			catch (PXSetPropertyException)
			{
			}

			var alternateInventoryList = FindAlternateInventory(sender.Graph, alternateID);
			row.NumOfFoundIDByAlternate = alternateInventoryList.Count;

			if (alternateInventoryList.Count == 0)
			{
				e.NewValue = null;
				return;
			}

			e.NewValue = alternateInventoryList.First().CD;
			SelectorAttribute.FieldUpdating(sender, e);
		}

		private List<(string CD, string Description)> FindAlternateInventory(PXGraph graph, string alternateID)
		{
			BqlCommand alternatesSelect = new
				SelectFrom<INItemXRef>.
				InnerJoin<InventoryItem>.
				On<INItemXRef.FK.InventoryItem>.
				Where<Match<AccessInfo.userName.FromCurrent>.And<
					  INItemXRef.alternateType.IsEqual<@P.AsString>.And<
					  INItemXRef.alternateID.IsEqual<@P.AsString>>>>();

			foreach (var restriction in _inventoryRestrictingConditions)
			{
				alternatesSelect = alternatesSelect.WhereAnd(restriction);
			}

			var vendorAlternatesSelect = alternatesSelect.WhereAnd<Where<INItemXRef.bAccountID.IsEqual<APRecognizedInvoice.vendorID.FromCurrent>>>();
			var vendorAlternatesView = new PXView(graph, true, vendorAlternatesSelect);
			var currentRecognizedInvoice = graph.Caches[typeof(APRecognizedInvoice)].Current;
			var currents = new object[] { currentRecognizedInvoice };
			var vendorAlternates = vendorAlternatesView.SelectMultiBound(currents, INAlternateType.VPN, alternateID)
				.Select(r => ((PXResult)r).GetItem<InventoryItem>())
				.Select(r => (r.InventoryCD, r.Descr))
				.ToList();

			if (vendorAlternates.Count != 0)
			{
				return vendorAlternates;
			}

			var globalAlternatesView = new PXView(graph, true, alternatesSelect);
			var globalAlternates = globalAlternatesView.SelectMulti(INAlternateType.Global, alternateID)
				.Select(r => ((PXResult)r).GetItem<InventoryItem>())
				.Select(r => (r.InventoryCD, r.Descr))
				.ToList();

			return globalAlternates;
		}
	}
}
