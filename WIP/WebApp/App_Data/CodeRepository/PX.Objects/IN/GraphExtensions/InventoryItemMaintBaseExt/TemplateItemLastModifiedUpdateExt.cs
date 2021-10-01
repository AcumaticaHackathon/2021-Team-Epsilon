using System;
using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.DAC.Accumulators;

namespace PX.Objects.IN.GraphExtensions.InventoryItemMaintBaseExt
{
	public abstract class TemplateItemLastModifiedUpdateExt<TGraph> : PXGraphExtension<TGraph>
		where TGraph : InventoryItemMaintBase
	{
		public PXSelect<TemplateItemLastModifiedUpdate> TemplateItemLastModifiedUpdate;

		protected virtual void _(Events.RowInserted<InventoryItem> eventArgs)
		{
			InsertAccumulatorRecord(eventArgs.Row);
		}

		protected virtual void _(Events.RowUpdated<InventoryItem> eventArgs)
		{
			InsertAccumulatorRecord(eventArgs.OldRow);
		}

		protected virtual void _(Events.RowDeleted<InventoryItem> eventArgs)
		{
			InsertAccumulatorRecord(eventArgs.Row);
		}

		protected virtual void InsertAccumulatorRecord(InventoryItem row)
		{
			if (row?.TemplateItemID != null)
			{
				TemplateItemLastModifiedUpdate.Insert(new TemplateItemLastModifiedUpdate()
				{
					InventoryID = row.TemplateItemID
				});
			}
		}
	}

	public class StockTemplateItemLastModifiedUpdateExt : TemplateItemLastModifiedUpdateExt<InventoryItemMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.matrixItem>();
		}
	}

	public class NonStockTemplateItemLastModifiedUpdateExt : TemplateItemLastModifiedUpdateExt<NonStockItemMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.matrixItem>();
		}
	}

	[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2021R1)]
	public class TemplateItemLastModifiedUpdateExt : PXGraphExtension<InventoryItemMaintBase>
	{
		public static bool IsActive()
		{
			return false;
		}

		public PXSelect<TemplateItemLastModifiedUpdate> TemplateItemLastModifiedUpdate;

		protected virtual void _(Events.RowPersisting<InventoryItem> eventArgs)
		{
		}
	}
}
