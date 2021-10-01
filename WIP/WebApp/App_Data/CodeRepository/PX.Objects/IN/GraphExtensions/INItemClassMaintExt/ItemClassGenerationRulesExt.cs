using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.DAC;
using PX.Objects.IN.Matrix.DAC.Projections;
using PX.Objects.IN.Matrix.GraphExtensions;
using PX.Objects.IN.Matrix.Graphs;
using PX.Objects.IN.Matrix.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.GraphExtensions.INItemClassMaintExt
{
	public class ItemClassGenerationRulesExt : GenerationRulesExt<
		INItemClassMaint,
		INItemClass,
		INItemClass.itemClassID,
		INMatrixGenerationRule.parentType.itemClass,
		INItemClass.sampleID,
		INItemClass.sampleDescription>
	{
		#region Methods

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.matrixItem>();
		}

		protected override InventoryItem GetTemplate()
		{
			var currentItemClass = Base.itemclasssettings.Current;

			return new InventoryItem()
			{
				InventoryCD = currentItemClass?.ItemClassCD,
				Descr = currentItemClass?.Descr,
				ItemClassID = currentItemClass.ItemClassID,
				ParentItemClassID = currentItemClass.ItemClassID,
			};
		}

		protected override string[] GetAttributeIDs()
			=> Base.Mapping.SelectMain().Select(a => a.AttributeID).ToArray();

		protected virtual void VerifyAttributes()
		{
			VerifyAttribute<INItemClass.defaultColumnMatrixAttributeID>();
			VerifyAttribute<INItemClass.defaultRowMatrixAttributeID>();

			VerifyRuleAttributes(DescriptionGenerationRules.Cache, DescriptionGenerationRules.SelectMain());
			VerifyRuleAttributes(IDGenerationRules.Cache, IDGenerationRules.SelectMain());
		}

		protected virtual void VerifyAttribute<Field>()
			where Field : IBqlField
		{
			var value = Base.itemclasssettings.Cache.GetValue<Field>(Base.itemclasssettings.Current);

			try
			{
				Base.itemclasssettings.Cache.RaiseFieldVerifying
					<Field>(Base.itemclasssettings.Current, ref value);
			}
			catch (PXSetPropertyException exception)
			{
				Base.itemclasssettings.Cache.RaiseExceptionHandling
					<Field>(Base.itemclasssettings.Current, value, exception);
			}
		}

		protected virtual void VerifyRuleAttributes(PXCache cache, IEnumerable<INMatrixGenerationRule> items)
		{
			foreach (var row in items)
			{
				object value = row.AttributeID;
				if (value != null)
				{
					try
					{
						cache.RaiseFieldVerifying<DescriptionGenerationRule.attributeID>(row, ref value);
					}
					catch (PXSetPropertyException exception)
					{
						cache.RaiseExceptionHandling<DescriptionGenerationRule.attributeID>(row, value, exception);
					}
				}
			}
		}

		protected override int? GetAttributeLength(INMatrixGenerationRule row)
		{
			var attribute = CRAttribute.Attributes[row.AttributeID];
			switch (attribute.ControlType)
			{
				case CSAttribute.Combo:
					return (row.SegmentType == INMatrixGenerationRule.segmentType.AttributeValue) ?
						attribute.Values.Max(v => v.ValueID?.Length) : attribute.Values.Max(v => v.Description?.Length);

				case CSAttribute.CheckBox:
					return 1;

				default:
					return base.GetAttributeLength(row);
			}
		}

		#endregion // Methods

		#region Cache Attached

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(INMatrixGenerationRule.parentType.ItemClass)]
		protected virtual void _(Events.CacheAttached<IDGenerationRule.parentType> eventArgs)
		{
		}


		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(INMatrixGenerationRule.parentType.ItemClass)]
		protected virtual void _(Events.CacheAttached<DescriptionGenerationRule.parentType> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBDefault(typeof(INItemClass.itemClassID))]
		[PXParent(typeof(IDGenerationRule.FK.ItemClass))]
		protected virtual void _(Events.CacheAttached<IDGenerationRule.parentID> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBDefault(typeof(INItemClass.itemClassID))]
		[PXParent(typeof(DescriptionGenerationRule.FK.ItemClass))]
		protected virtual void _(Events.CacheAttached<DescriptionGenerationRule.parentID> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXLineNbr(typeof(INItemClass.generationRuleCntr))]
		protected virtual void _(Events.CacheAttached<IDGenerationRule.lineNbr> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXLineNbr(typeof(INItemClass.generationRuleCntr))]
		protected virtual void _(Events.CacheAttached<DescriptionGenerationRule.lineNbr> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search<CSAttributeGroup.attributeID,
		Where<CSAttributeGroup.entityClassID, Equal<RTrim<Current<INItemClass.itemClassID>>>,
			And<CSAttributeGroup.entityType, Equal<Common.Constants.DACName<InventoryItem>>>>>),
		typeof(CSAttributeGroup.attributeID), typeof(CSAttributeGroup.description), DirtyRead = true)]
		protected virtual void _(Events.CacheAttached<IDGenerationRule.attributeID> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search<CSAttributeGroup.attributeID,
		Where<CSAttributeGroup.entityClassID, Equal<RTrim<Current<INItemClass.itemClassID>>>,
			And<CSAttributeGroup.entityType, Equal<Common.Constants.DACName<InventoryItem>>>>>),
		typeof(CSAttributeGroup.attributeID), typeof(CSAttributeGroup.description), DirtyRead = true)]
		protected virtual void _(Events.CacheAttached<DescriptionGenerationRule.attributeID> eventArgs)
		{
		}

		#endregion // Cache Attached

		#region Events

		protected virtual void _(Events.RowUpdated<CSAttributeGroup> eventArgs)
		{
			if (!eventArgs.Cache.ObjectsEqual<
				CSAttributeGroup.attributeCategory,
				CSAttributeGroup.attributeID,
				CSAttributeGroup.isActive>(eventArgs.Row, eventArgs.OldRow))
			{
				VerifyAttributes();
			}
		}

		protected virtual void _(Events.RowDeleted<CSAttributeGroup> eventArgs)
		{
			if (eventArgs.Row?.AttributeCategory == CSAttributeGroup.attributeCategory.Variant)
			{
				VerifyAttributes();
			}
		}

		protected virtual void _(Events.RowPersisting<INItemClass> eventArgs)
			=> VerifyAttributes();

		#endregion // Events
	}
}
