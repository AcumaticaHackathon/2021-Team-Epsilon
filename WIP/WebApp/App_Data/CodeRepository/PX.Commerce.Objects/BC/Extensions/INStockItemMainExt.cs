using PX.Commerce.Core;
using PX.Data;
using PX.Objects.IN;
using System;

namespace PX.Commerce.Objects
{
	public class BCInventoryItemMaintExt : PXGraphExtension<InventoryItemMaint>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		public PXSelect<BCInventoryFileUrls, Where<BCInventoryFileUrls.inventoryID, Equal<Current<InventoryItem.inventoryID>>>> InventoryFileUrls;

		//Sync Time 
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PX.Commerce.Core.BCSyncExactTime()]
		public void InventoryItem_LastModifiedDateTime_CacheAttached(PXCache sender) { }

		protected virtual void BCInventoryFileUrls_FileURL_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			var row = (BCInventoryFileUrls)e.Row;
			if (row == null) return;

			string val = e.NewValue?.ToString();
			foreach (BCInventoryFileUrls item in InventoryFileUrls.Select())
			{
				if (item.FileURL == val)
				{
					throw new PXSetPropertyException(PX.Commerce.Core.BCMessages.URLAlreadyExists);
				}
			}

			if (val != null)
			{
				if (!IsValidUrl(val, row.FileType))
				{
					throw new PXSetPropertyException(PX.Commerce.Core.BCMessages.InvalidURL);
				}
			}
		}

		protected void InventoryItem_CustomURL_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			var row = (InventoryItem)e.Row;
			if (row == null || e.NewValue == null) return;
			String url = e.NewValue.ToString();

			if (url.StartsWith("/")) return;
			e.NewValue = url = "/" + url;
		}

		protected virtual void BCInventoryFileUrls_FileType_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			var row = (BCInventoryFileUrls)e.Row;
			if (row == null) return;

			string val = e.NewValue?.ToString();
			if (val != null)
			{
				if (!IsValidUrl(row.FileURL, val))
				{
					sender.RaiseExceptionHandling<BCInventoryFileUrls.fileURL>(row, row.FileURL, new PXSetPropertyException(PX.Commerce.Core.BCMessages.InvalidURL, PXErrorLevel.Error));
				}
			}
		}

		protected virtual void _(Events.RowSelected<InventoryItem> e)
		{
			var row = (InventoryItem)e.Row;
			if (row == null) return;
			var ext = row.GetExtension<BCInventoryItem>();
			if (ext == null) return;
			string val = row.ItemStatus;
			if (val == InventoryItemStatus.Inactive || val == InventoryItemStatus.MarkedForDeletion || val == InventoryItemStatus.NoSales)
			{
				PXUIFieldAttribute.SetEnabled<BCInventoryItem.visibility>(e.Cache, row, false);
			}
			else
				PXUIFieldAttribute.SetEnabled<BCInventoryItem.visibility>(e.Cache, row, true);

		}

		private static bool IsValidUrl(string val, string type)
		{
			if (val == null) return false;
			return Uri.TryCreate(val, UriKind.Absolute, out var _);
		}
	}
}
