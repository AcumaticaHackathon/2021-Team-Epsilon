using PX.Common;
using PX.Data;
using PX.Objects.Common.Attributes;
using PX.Objects.IN.Matrix.Attributes;
using PX.Objects.IN.Matrix.DAC;
using PX.Objects.IN.Matrix.DAC.Projections;
using PX.Objects.IN.Matrix.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.Matrix.GraphExtensions
{
	public class TemplateGenerationRulesExt : GenerationRulesExt<
		TemplateInventoryItemMaint,
		InventoryItem,
		InventoryItem.inventoryID,
		INMatrixGenerationRule.parentType.templateItem,
		InventoryItem.sampleID,
		InventoryItem.sampleDescription>
	{
		#region Cache Attached

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[DefaultConditional(typeof(Search<INItemClass.defaultRowMatrixAttributeID,
			Where<INItemClass.itemClassID, Equal<Current<InventoryItem.parentItemClassID>>>>), typeof(InventoryItem.isTemplate), true)]
		[PXUIRequired(typeof(Where<InventoryItem.defaultRowMatrixAttributeID,
			NotEqual<MatrixAttributeSelectorAttribute.dummyAttributeName>>))]
		protected virtual void _(Events.CacheAttached<InventoryItem.defaultRowMatrixAttributeID> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[DefaultConditional(typeof(Search<INItemClass.defaultColumnMatrixAttributeID,
			Where<INItemClass.itemClassID, Equal<Current<InventoryItem.parentItemClassID>>>>), typeof(InventoryItem.isTemplate), true)]
		[PXUIRequired(typeof(Where<InventoryItem.defaultColumnMatrixAttributeID,
			NotEqual<MatrixAttributeSelectorAttribute.dummyAttributeName>>))]
		protected virtual void _(Events.CacheAttached<InventoryItem.defaultColumnMatrixAttributeID> eventArgs)
		{
		}

		#endregion // Cache Attached

		#region Overrides

		protected override string[] GetAttributeIDs()
			=> Base.Answers.SelectMain().Select(a => a.AttributeID).ToArray();

		protected override InventoryItem GetTemplate()
			=> Base.ItemSettings.Current;

		[PXOverride]
		public virtual void ResetDefaultsOnItemClassChange(InventoryItem row, Action<InventoryItem> baseMethod)
		{
			baseMethod?.Invoke(row);

			var cache = Base.Item.Cache;

			cache.SetDefaultExt<InventoryItem.defaultColumnMatrixAttributeID>(row);
			cache.SetDefaultExt<InventoryItem.defaultRowMatrixAttributeID>(row);

			var classIdRules = new PXSelectReadonly<IDGenerationRule,
				Where<IDGenerationRule.parentType, Equal<INMatrixGenerationRule.parentType.itemClass>,
					And<IDGenerationRule.parentID, Equal<Current<InventoryItem.itemClassID>>>>>(Base)
				.SelectMain();

			IDGenerationRules.SelectMain().ForEach(rule =>
				IDGenerationRules.Delete(rule));

			classIdRules.ForEach(classIdRule =>
			{
				var newRule = PropertyTransfer.Transfer(classIdRule, new IDGenerationRule());
				newRule.ParentID = row.InventoryID;
				newRule.ParentType = INMatrixGenerationRule.parentType.TemplateItem;
				newRule.LineNbr = null;

				IDGenerationRules.Insert(newRule);
			});

			var classDescriptionRules = new PXSelectReadonly<DescriptionGenerationRule,
				Where<DescriptionGenerationRule.parentType, Equal<INMatrixGenerationRule.parentType.itemClass>,
					And<DescriptionGenerationRule.parentID, Equal<Current<InventoryItem.itemClassID>>>>>(Base)
				.SelectMain();

			DescriptionGenerationRules.SelectMain().ForEach(rule =>
				DescriptionGenerationRules.Delete(rule));

			classDescriptionRules.ForEach(classIdRule =>
			{
				var newRule = PropertyTransfer.Transfer(classIdRule, new DescriptionGenerationRule());
				newRule.ParentID = row.InventoryID;
				newRule.ParentType = INMatrixGenerationRule.parentType.TemplateItem;

				DescriptionGenerationRules.Insert(newRule);
			});
		}

		protected override int? GetAttributeLength(INMatrixGenerationRule row)
		{
			var helper = Base.GetCreateMatrixItemsHelper();

			return (row.SegmentType == INMatrixGenerationRule.segmentType.AttributeValue) ?
				helper.GetAttributeValue(row, Base.Item.Current, null).Length :
				helper.GetAttributeCaption(row, Base.Item.Current, null, null).Length;
		}

		#endregion // Overrides
	}
}
