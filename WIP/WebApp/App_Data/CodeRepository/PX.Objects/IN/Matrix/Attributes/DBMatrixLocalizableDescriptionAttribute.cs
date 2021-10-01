using PX.Data;
using PX.Objects.IN.Matrix.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.Matrix.Attributes
{
	/// <exclude/>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
	public class DBMatrixLocalizableDescriptionAttribute : PXDBLocalizableStringAttribute
	{
		public bool CopyTranslationsToInventoryItem { get; set; }

		public DBMatrixLocalizableDescriptionAttribute(int length) : base(length)
		{
		}

		public static void SetTranslations<TDestinationField>(
			PXCache destinationCache, object destinationData,
			Func<string, string> processTranslation)
			where TDestinationField : IBqlField
		{
			if (IsEnabled)
			{
				string[] translations = new string[EnabledLocales.Count];

				for (int translationIndex = 0; translationIndex < EnabledLocales.Count; translationIndex++)
				{
					string locale = EnabledLocales[translationIndex];
					translations[translationIndex] = processTranslation(locale);
				}

				destinationCache.Adjust<DBMatrixLocalizableDescriptionAttribute>(destinationData).For<TDestinationField>(a =>
					a.SetTranslations(destinationCache, destinationData, translations));
			}
		}

		protected override void Translations_FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (!CopyTranslationsToInventoryItem)
			{
				base.Translations_FieldUpdating(sender, e);
				return;
			}

			int? inventoryID = (int?)sender.GetValue<InventoryItem.inventoryID>(e.Row);
			if (inventoryID == null)
			{
				base.Translations_FieldUpdating(sender, e);
				return;
			}

			var oldTranslations = (string[])GetTranslations(sender, e.Row)?.Clone();

			base.Translations_FieldUpdating(sender, e);

			var newTranslations = GetTranslations(sender, e.Row);

			if ((oldTranslations != null && newTranslations != null &&
					!oldTranslations.SequenceEqual(newTranslations))
				|| (oldTranslations == null && newTranslations != null)
				|| (oldTranslations != null && newTranslations == null))
			{
				var inventoryItem = InventoryItem.PK.FindDirty(sender.Graph, inventoryID);
				
				foreach (DBMatrixLocalizableDescriptionAttribute attribute in
					sender.Graph.Caches<InventoryItem>().GetAttributes<InventoryItem.descr>(inventoryItem)
						.Where(attribute => (attribute is PXDBLocalizableStringAttribute)))
				{
					attribute.SetTranslations(sender.Graph.Caches<InventoryItem>(), inventoryItem, newTranslations);

					if (sender.Graph is TemplateInventoryItemMaint templateMaint)
					{
						templateMaint.UpdateChild(inventoryItem);
					}
					else
					{
						sender.Graph.Caches<InventoryItem>().Update(inventoryItem);
					}
				}
			}
		}

	}
}
