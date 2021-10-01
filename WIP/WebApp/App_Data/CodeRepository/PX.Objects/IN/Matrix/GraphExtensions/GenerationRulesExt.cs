using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.DAC;
using PX.Objects.IN.Matrix.DAC.Projections;
using PX.Objects.IN.Matrix.DAC.Unbound;
using PX.Objects.IN.Matrix.Interfaces;
using PX.Objects.IN.Matrix.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.Matrix.GraphExtensions
{
	public abstract class GenerationRulesExt<
		TGraph,
		TParent,
		TParentID,
		TParentType,
		TSampleIDField,
		TSampleDescriptionField>
		: PXGraphExtension<TGraph>

		where TGraph : PXGraph, ICreateMatrixHelperFactory
		where TParent : class, IBqlTable, new()
		where TParentID : IBqlField
		where TParentType : class, IBqlOperand
		where TSampleIDField : IBqlField
		where TSampleDescriptionField : IBqlField
	{
		const string SampleField = "Sample";

		public PXSelect<IDGenerationRule,
			Where<IDGenerationRule.parentID, Equal<Current<TParentID>>,
				And<IDGenerationRule.parentType, Equal<TParentType>>>,
			OrderBy<Asc<IDGenerationRule.sortOrder>>> IDGenerationRules;

		public PXSelect<DescriptionGenerationRule,
			Where<DescriptionGenerationRule.parentID, Equal<Current<TParentID>>,
				And<DescriptionGenerationRule.parentType, Equal<TParentType>>>,
			OrderBy<Asc<DescriptionGenerationRule.sortOrder>>> DescriptionGenerationRules;

		public override void Initialize()
		{
			base.Initialize();

			IDGenerationRules.Cache.Fields.Add(SampleField);
			DescriptionGenerationRules.Cache.Fields.Add(SampleField);

			Base.FieldSelecting.AddHandler(typeof(IDGenerationRule), SampleField, SampleIDFieldSelecting);
			Base.FieldSelecting.AddHandler(typeof(DescriptionGenerationRule), SampleField, SampleDescriptionFieldSelecting);
		}

		#region Cache Attached

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(true)]
		protected virtual void _(Events.CacheAttached<IDGenerationRule.addSpaces> eventArgs)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(false)]
		protected virtual void _(Events.CacheAttached<DescriptionGenerationRule.addSpaces> eventArgs)
		{
		}

		#endregion

		#region Generation Rule Events

		protected virtual void _(Events.RowInserted<IDGenerationRule> eventArgs)
		{
			GenerationRuleRowInserted(eventArgs.Cache, eventArgs.Row);
			ResetValue<TSampleIDField>();
		}

		protected virtual void _(Events.RowInserted<DescriptionGenerationRule> eventArgs)
		{
			GenerationRuleRowInserted(eventArgs.Cache, eventArgs.Row);
			ResetValue<TSampleDescriptionField>();
		}

		protected virtual void GenerationRuleRowInserted(PXCache cache, INMatrixGenerationRule row)
		{
			row.SortOrder = row.LineNbr;
		}

		protected virtual void _(Events.RowUpdated<IDGenerationRule> eventArgs)
			=> ResetValue<TSampleIDField>();

		protected virtual void _(Events.RowUpdated<DescriptionGenerationRule> eventArgs)
			=> ResetValue<TSampleDescriptionField>();

		protected virtual void _(Events.RowDeleted<IDGenerationRule> eventArgs)
			=> ResetValue<TSampleIDField>();

		protected virtual void _(Events.RowDeleted<DescriptionGenerationRule> eventArgs)
			=> ResetValue<TSampleDescriptionField>();

		public void SampleIDFieldSelecting(PXCache sender, PXFieldSelectingEventArgs args)
		{
			args.ReturnState = PXStringState.CreateInstance(args.ReturnState, null, true, SampleField, false, 0, null, null, null, null, null);

			// Acuminator disable once PX1043 SavingChangesInEventHandlers The code uses GenerateMatrixItemID with useLastAutoNumberValue=true parameter, it doesn't save autonumbering new value in database.
			args.ReturnValue = GetGenerationRuleSample<TSampleIDField, IDGenerationRule>(
				IDGenerationRules, Messages.SampleInventoryID);
		}

		public void SampleDescriptionFieldSelecting(PXCache sender, PXFieldSelectingEventArgs args)
		{
			args.ReturnState = PXStringState.CreateInstance(args.ReturnState, null, true, SampleField, false, 0, null, null, null, null, null);

			// Acuminator disable once PX1043 SavingChangesInEventHandlers The code uses GenerateMatrixItemID with useLastAutoNumberValue=true parameter, it doesn't save autonumbering new value in database.
			args.ReturnValue = GetGenerationRuleSample<TSampleDescriptionField, DescriptionGenerationRule>(
				DescriptionGenerationRules, Messages.SampleInventoryDescription);
		}

		public PXAction<InventoryItem> IdRowUp;
		[PXUIField(DisplayName = ActionsMessages.RowUp, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.ArrowUp, Tooltip = ActionsMessages.ttipRowUp)]
		public virtual IEnumerable idRowUp(PXAdapter adapter)
		{
			MoveCurrentRow(IDGenerationRules.Cache, IDGenerationRules.SelectMain(), true);
			IDGenerationRules.View.RequestRefresh();
			return adapter.Get();
		}

		public PXAction<InventoryItem> IdRowDown;
		[PXUIField(DisplayName = ActionsMessages.RowDown, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.ArrowDown, Tooltip = ActionsMessages.ttipRowDown)]
		public virtual IEnumerable idRowDown(PXAdapter adapter)
		{
			MoveCurrentRow(IDGenerationRules.Cache, IDGenerationRules.SelectMain(), false);
			IDGenerationRules.View.RequestRefresh();
			return adapter.Get();
		}

		public PXAction<InventoryItem> DescriptionRowUp;
		[PXUIField(DisplayName = ActionsMessages.RowUp, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.ArrowUp, Tooltip = ActionsMessages.ttipRowUp)]
		public virtual IEnumerable descriptionRowUp(PXAdapter adapter)
		{
			MoveCurrentRow(DescriptionGenerationRules.Cache, DescriptionGenerationRules.SelectMain(), true);
			DescriptionGenerationRules.View.RequestRefresh();
			return adapter.Get();
		}

		public PXAction<InventoryItem> DescriptionRowDown;
		[PXUIField(DisplayName = ActionsMessages.RowDown, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.ArrowDown, Tooltip = ActionsMessages.ttipRowDown)]
		public virtual IEnumerable descriptionRowDown(PXAdapter adapter)
		{
			MoveCurrentRow(DescriptionGenerationRules.Cache, DescriptionGenerationRules.SelectMain(), false);
			DescriptionGenerationRules.View.RequestRefresh();
			return adapter.Get();
		}

		public void MoveCurrentRow(PXCache cache, IEnumerable<INMatrixGenerationRule> allRows, bool up)
		{
			INMatrixGenerationRule currentLine = (INMatrixGenerationRule)cache.Current;
			if (currentLine == null)
				return;

			INMatrixGenerationRule nextLine;

			if (up)
				nextLine = allRows.Where(r => r.SortOrder < currentLine.SortOrder).OrderByDescending(r => r.SortOrder).FirstOrDefault();
			else
				nextLine = allRows.Where(r => r.SortOrder > currentLine.SortOrder).OrderBy(r => r.SortOrder).FirstOrDefault();

			if (nextLine == null)
				return;

			int? currentLineNbr = currentLine.SortOrder;
			int? nextLineNbr = nextLine.SortOrder;

			nextLine.SortOrder = currentLineNbr;
			currentLine.SortOrder = nextLineNbr;

			nextLine = (INMatrixGenerationRule)cache.Update(nextLine);
			currentLine = (INMatrixGenerationRule)cache.Update(currentLine);

			cache.Current = currentLine;
		}


		protected virtual void _(Events.RowSelected<IDGenerationRule> eventArgs)
			=> GenerationRuleRowSelected(eventArgs.Cache, eventArgs.Row);

		protected virtual void _(Events.RowSelected<DescriptionGenerationRule> eventArgs)
			=> GenerationRuleRowSelected(eventArgs.Cache, eventArgs.Row);

		protected virtual void GenerationRuleRowSelected(PXCache cache, INMatrixGenerationRule row)
		{
			if (row == null)
				return;

			bool isAttribute = row.SegmentType.IsIn(INMatrixGenerationRule.segmentType.AttributeCaption, IDGenerationRule.segmentType.AttributeValue);
			bool isConstant = row.SegmentType == INMatrixGenerationRule.segmentType.Constant;
			bool isAutonumber = row.SegmentType == INMatrixGenerationRule.segmentType.AutoNumber;

			cache.Adjust<PXUIFieldAttribute>(row)
				.For<INMatrixGenerationRule.attributeID>(a => a.Enabled = isAttribute)
				.For<INMatrixGenerationRule.constant>(a => a.Enabled = isConstant)
				.For<IDGenerationRule.numberingID>(a => a.Enabled = isAutonumber)
				.For<INMatrixGenerationRule.separator>(a => a.Enabled = row.UseSpaceAsSeparator != true);
		}


		protected virtual void _(Events.FieldUpdated<IDGenerationRule, IDGenerationRule.segmentType> eventArgs)
			=> GenerationRuleSegmentUpdated(eventArgs.Cache, eventArgs.Row);

		protected virtual void _(Events.FieldUpdated<DescriptionGenerationRule, IDGenerationRule.segmentType> eventArgs)
			=> GenerationRuleSegmentUpdated(eventArgs.Cache, eventArgs.Row);

		protected virtual void GenerationRuleSegmentUpdated(PXCache cache, INMatrixGenerationRule row)
		{
			if (row == null)
				return;

			switch (row.SegmentType)
			{
				case INMatrixGenerationRule.segmentType.TemplateDescription:
					row.NumberOfCharacters = GetTemplate()?.Descr?.Length;
					row.Constant = null;
					row.NumberingID = null;
					row.AttributeID = null;
					break;
				case INMatrixGenerationRule.segmentType.TemplateID:
					row.NumberOfCharacters = GetTemplate()?.InventoryCD?.Trim().Length;
					row.Constant = null;
					row.NumberingID = null;
					row.AttributeID = null;
					break;
				case INMatrixGenerationRule.segmentType.AttributeCaption:
				case INMatrixGenerationRule.segmentType.AttributeValue:
					row.Constant = null;
					row.NumberingID = null;
					GenerationRuleAttributeUpdated(cache, row);
					break;
				case INMatrixGenerationRule.segmentType.Constant:
					row.AttributeID = null;
					row.NumberingID = null;
					break;
				case INMatrixGenerationRule.segmentType.AutoNumber:
					row.Constant = null;
					row.AttributeID = null;
					break;
				case INMatrixGenerationRule.segmentType.Space:
					row.NumberOfCharacters = 1;
					row.Constant = null;
					row.NumberingID = null;
					row.AttributeID = null;
					break;
				default:
					row.Constant = null;
					row.NumberingID = null;
					row.AttributeID = null;
					break;
			}
		}

		protected virtual void _(Events.FieldUpdated<IDGenerationRule, IDGenerationRule.attributeID> eventArgs)
			=> GenerationRuleAttributeUpdated(eventArgs.Cache, eventArgs.Row);

		protected virtual void _(Events.FieldUpdated<DescriptionGenerationRule, IDGenerationRule.attributeID> eventArgs)
			=> GenerationRuleAttributeUpdated(eventArgs.Cache, eventArgs.Row);

		protected virtual void GenerationRuleAttributeUpdated(PXCache cache, INMatrixGenerationRule row)
		{
			if (row?.AttributeID == null)
				return;

			var attribute = CRAttribute.Attributes[row.AttributeID];
			
			int? length = 0;

			var itemClassID = GetTemplate().ParentItemClassID?.ToString();

			var attributeGroup = new SelectFrom<CSAttributeGroup>
				.Where<CSAttributeGroup.attributeID.IsEqual<@P.AsString>
					.And<CSAttributeGroup.entityClassID.IsEqual<@P.AsString>>
					.And<CSAttributeGroup.entityType.IsEqual<@P.AsString>>>
				.View(Base).SelectSingle(
					row.AttributeID, itemClassID, typeof(InventoryItem).FullName);

			if (attributeGroup?.AttributeCategory == CSAttributeGroup.attributeCategory.Attribute)
			{
				length = GetAttributeLength(row);
			}
			else
			{
				length = (row.SegmentType == INMatrixGenerationRule.segmentType.AttributeValue) ?
					attribute.Values.Max(v => v.ValueID?.Length) : attribute.Values.Max(v => v.Description?.Length);
			}

			row.NumberOfCharacters = length;
		}

		protected virtual int? GetAttributeLength(INMatrixGenerationRule row) => 0;

		protected virtual void _(Events.FieldUpdated<IDGenerationRule, IDGenerationRule.constant> eventArgs)
			=> GenerationRuleConstantUpdated(eventArgs.Cache, eventArgs.Row);

		protected virtual void _(Events.FieldUpdated<DescriptionGenerationRule, IDGenerationRule.constant> eventArgs)
			=> GenerationRuleConstantUpdated(eventArgs.Cache, eventArgs.Row);

		protected virtual void GenerationRuleConstantUpdated(PXCache cache, INMatrixGenerationRule row)
		{
			row.NumberOfCharacters = row?.Constant?.Length;
		}

		protected virtual void _(Events.FieldUpdated<IDGenerationRule, IDGenerationRule.numberingID> eventArgs)
			=> GenerationRuleNumberingUpdated(eventArgs.Cache, eventArgs.Row);

		protected virtual void _(Events.FieldUpdated<DescriptionGenerationRule, IDGenerationRule.numberingID> eventArgs)
			=> GenerationRuleNumberingUpdated(eventArgs.Cache, eventArgs.Row);

		protected virtual void GenerationRuleNumberingUpdated(PXCache cache, INMatrixGenerationRule row)
		{
			if (row?.NumberingID == null)
				return;

			row.NumberOfCharacters = new PXSelect<NumberingSequence,
				Where<NumberingSequence.numberingID, Equal<Required<Numbering.numberingID>>>>(Base)
				.SelectMain(row.NumberingID)
				.Max(s => s.EndNbr?.Length);
		}

		#endregion // Generation Rule Events

		#region Methods

		protected virtual void ResetValue<Field>()
			where Field : IBqlField
		{
			Base.Caches<TParent>().SetValue<Field>(Base.Caches<TParent>().Current, null);
		}

		protected virtual string GetGenerationRuleSample<Field, Rule>(PXSelectBase<Rule> view, string userMessage)
			where Field : IBqlField
			where Rule : INMatrixGenerationRule, new()
		{
			var cache = Base.Caches<TParent>();
			var current = cache.Current;
			if (current == null)
				return null;

			string oldValue = (string)cache.GetValue<Field>(current);

			if (oldValue != null)
				return oldValue;

			var value = GetGenerationRuleSample(view.SelectMain());
			var userValue = PXLocalizer.LocalizeFormat(userMessage, value);
			cache.SetValue<Field>(current, userValue);
			view.View.RequestRefresh();

			return userValue;
		}

		protected virtual string GetGenerationRuleSample(IEnumerable<INMatrixGenerationRule> rules)
		{
			try
			{
				var helper = Base.GetCreateMatrixItemsHelper();

				var tempItem = new MatrixInventoryItem();

				tempItem.AttributeIDs = GetAttributeIDs();
				tempItem.AttributeValues = new string[tempItem.AttributeIDs.Length];
				tempItem.AttributeValueDescrs = new string[tempItem.AttributeIDs.Length];

				for (int attributeIndex = 0; attributeIndex < tempItem.AttributeIDs.Length; attributeIndex++)
				{
					var attributeID = tempItem.AttributeIDs[attributeIndex];
					var values = CRAttribute.Attributes[attributeID].Values;
					var value = values.Where(v => !v.Disabled).FirstOrDefault();
					tempItem.AttributeValues[attributeIndex] = value?.ValueID;
					tempItem.AttributeValueDescrs[attributeIndex] = value?.Description;
				}

				return helper.GenerateMatrixItemID(GetTemplate(), rules.ToList(), tempItem, true);
			}
			catch (Exception exception)
			{
				return exception.Message;
			}
		}

		protected abstract string[] GetAttributeIDs();

		protected abstract InventoryItem GetTemplate();

		#endregion
	}
}
