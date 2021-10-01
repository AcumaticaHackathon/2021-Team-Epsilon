using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Exceptions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.Attributes;
using PX.Objects.IN.Matrix.DAC;
using PX.Objects.IN.Matrix.DAC.Projections;
using PX.Objects.IN.Matrix.DAC.Unbound;
using PX.Objects.PO;

namespace PX.Objects.IN.Matrix.Utility
{
	public class CreateMatrixItemsHelper
	{
		protected const char MultiComboAttributeSeparator = ',';

		protected PXGraph _graph;

		public CreateMatrixItemsHelper(PXGraph graph)
		{
			_graph = graph;
		}

		public virtual MatrixInventoryItem CreateMatrixItemFromTemplate(
			EntryMatrix row, int attributeNumber, InventoryItem template,
			List<INMatrixGenerationRule> idGenRules, List<INMatrixGenerationRule> descrGenRules)
		{
			int? inventoryId = GetValueFromArray(row?.InventoryIDs, attributeNumber);
			bool? selected = GetValueFromArray(row?.Selected, attributeNumber);
			if (inventoryId != null || selected != true)
				return null;

			var currentHeader = (EntryHeader)_graph.Caches[typeof(EntryHeader)].Current;
			var currentAdditionalAttr = (AdditionalAttributes)_graph.Caches[typeof(AdditionalAttributes)].Current;
			var matrixCache = _graph.Caches[typeof(MatrixInventoryItem)];

			var newItem = PropertyTransfer.Transfer(template, new MatrixInventoryItem());

			List<string> attributes = new List<string>(currentAdditionalAttr?.AttributeIdentifiers);
			attributes.Add(currentHeader?.RowAttributeID);
			attributes.Add(currentHeader?.ColAttributeID);
			List<string> attrValues = new List<string>(currentAdditionalAttr?.Values);
			attrValues.Add(row.RowAttributeValue);
			attrValues.Add(GetValueFromArray(row?.ColAttributeValues, attributeNumber));
			List<string> attrValueDescrs = new List<string>(currentAdditionalAttr?.Descriptions);
			attrValueDescrs.Add(row.RowAttributeValueDescr);
			attrValueDescrs.Add(GetValueFromArray(row?.ColAttributeValueDescrs, attributeNumber));

			newItem.AttributeIDs = attributes.ToArray();
			newItem.AttributeValues = attrValues.ToArray();
			newItem.AttributeValueDescrs = attrValueDescrs.ToArray();

			object newCD = GenerateMatrixItemID(template, idGenRules, newItem);
			matrixCache.RaiseFieldUpdating<MatrixInventoryItem.inventoryCD>(newItem, ref newCD);
			newItem.InventoryCD = (string)newCD;

			if (PXDBLocalizableStringAttribute.IsEnabled)
			{
				PXCache templateCache = _graph.Caches<InventoryItem>();

				DBMatrixLocalizableDescriptionAttribute.SetTranslations<MatrixInventoryItem.descr>
					(matrixCache, newItem, (locale) =>
				{
					object newTranslation = GenerateMatrixItemID(template, descrGenRules, newItem, locale: locale);
					matrixCache.RaiseFieldUpdating<MatrixInventoryItem.descr>(newItem, ref newTranslation);
					return (string)newTranslation;
				});
			}
			else
			{
				object newDescr = GenerateMatrixItemID(template, descrGenRules, newItem);
				matrixCache.RaiseFieldUpdating<MatrixInventoryItem.descr>(newItem, ref newDescr);
				newItem.Descr = (string)newDescr;
			}

			newItem.Exists = (InventoryItem.UK.Find(_graph, newItem.InventoryCD) != null);
			newItem.Duplicate = matrixCache.Cached.RowCast<MatrixInventoryItem>().Any(mi => mi.InventoryCD == newItem.InventoryCD);
			newItem.Selected = (newItem.Exists != true && newItem.Duplicate != true);
			newItem.IsTemplate = false;

			return newItem;
		}

		public virtual void GetGenerationRules(int? templateItemID, out List<INMatrixGenerationRule> idGenerationRules, out List<INMatrixGenerationRule> descrGenerationRules)
		{
			var generationRules =
				PXSelectReadonly<INMatrixGenerationRule,
					Where<INMatrixGenerationRule.parentID, Equal<Required<INMatrixGenerationRule.parentID>>,
						And<INMatrixGenerationRule.parentType, Equal<INMatrixGenerationRule.parentType.templateItem>>>,
					OrderBy<Asc<INMatrixGenerationRule.sortOrder>>>
				.Select(_graph, templateItemID)
				.RowCast<INMatrixGenerationRule>()
				.ToList();
			idGenerationRules = generationRules.Where(r => r.Type == INMatrixGenerationRule.type.ID).ToList();
			descrGenerationRules = generationRules.Where(r => r.Type == INMatrixGenerationRule.type.Description).ToList();
		}

		public virtual string GenerateMatrixItemID(
			InventoryItem template, List<INMatrixGenerationRule> genRules, MatrixInventoryItem newItem, bool useLastAutoNumberValue = false, string locale = null)
		{
			StringBuilder res = new StringBuilder();

			for (int i = 0; i < genRules.Count; i++)
			{
				bool isLastSegment = (i == genRules.Count - 1);

				AppendMatrixItemIDSegment(res, template, genRules[i], isLastSegment, newItem, useLastAutoNumberValue, locale);
			}

			return res.ToString();
		}

		protected virtual void AppendMatrixItemIDSegment(
			StringBuilder res, InventoryItem template,
			INMatrixGenerationRule genRule, bool isLastSegment, MatrixInventoryItem newItem, bool useLastAutoNumberValue, string locale)
		{
			string segValue = string.Empty;
			switch (genRule.SegmentType)
			{
				case INMatrixGenerationRule.segmentType.TemplateID:
					segValue = template.InventoryCD;
					break;
				case INMatrixGenerationRule.segmentType.TemplateDescription:
					segValue = GetTemplateDescription(template, locale);
					break;
				case INMatrixGenerationRule.segmentType.AttributeCaption:
					segValue = GetAttributeCaption(genRule, template, newItem, locale);
					break;
				case INMatrixGenerationRule.segmentType.AttributeValue:
					segValue = GetAttributeValue(genRule, template, newItem);
					break;
				case INMatrixGenerationRule.segmentType.Constant:
					segValue = genRule.Constant;
					break;
				case INMatrixGenerationRule.segmentType.Space:
					segValue = " ";
					break;
				case INMatrixGenerationRule.segmentType.AutoNumber when !useLastAutoNumberValue:
					segValue = AutoNumberAttribute.GetNextNumber(_graph.Caches[typeof(InventoryItem)], null, genRule.NumberingID, _graph.Accessinfo.BusinessDate);
					break;
				case INMatrixGenerationRule.segmentType.AutoNumber when useLastAutoNumberValue:
					var numberingSequence = AutoNumberAttribute.GetNumberingSequence(genRule.NumberingID, _graph.Accessinfo.BranchID, _graph.Accessinfo.BusinessDate);
					segValue = numberingSequence?.LastNbr;
					if (string.IsNullOrEmpty(segValue))
						segValue = AutoNumberAttribute.GetNextNumber(_graph.Caches[typeof(InventoryItem)], null, genRule.NumberingID, _graph.Accessinfo.BusinessDate);
					break;

				default:
					throw new PXArgumentException(nameof(INMatrixGenerationRule));
			}

			segValue = segValue ?? string.Empty;

			if (segValue.Length > genRule.NumberOfCharacters)
			{
				segValue = segValue.Substring(0, (int)genRule.NumberOfCharacters);
			}
			else if (segValue.Length < genRule.NumberOfCharacters && genRule.AddSpaces == true)
			{
				segValue = segValue.PadRight((int)genRule.NumberOfCharacters);
			}

			if (genRule.AddSpaces != true)
				segValue = segValue.TrimEnd();

			res.Append(segValue);

			if (!isLastSegment)
			{
				if (genRule.UseSpaceAsSeparator == true)
				{
					res.Append(' ');
				}
				else
				{
					res.Append(genRule.Separator);
				}
			}
		}

		protected virtual string GetTemplateDescription(InventoryItem template, string locale)
		{
			string segValue;
			if (!string.IsNullOrEmpty(locale))
			{
				segValue = PXDBLocalizableStringAttribute.GetTranslation<InventoryItem.descr>
					(_graph.Caches[typeof(InventoryItem)], template, locale);

				if (string.IsNullOrEmpty(segValue))
					segValue = template.Descr;
			}
			else
			{
				segValue = template.Descr;
			}

			return segValue;
		}

		public virtual string GetAttributeCaption(INMatrixGenerationRule genRule, InventoryItem template, MatrixInventoryItem newItem, string locale)
		{
			var attributeGroup = CSAttributeGroup.PK.Find(_graph,
				genRule.AttributeID, template.ParentItemClassID?.ToString(), typeof(InventoryItem).FullName);

			if (attributeGroup?.AttributeCategory == CSAttributeGroup.attributeCategory.Attribute)
			{
				CSAnswers answer = SelectFrom<CSAnswers>.
					Where<CSAnswers.refNoteID.IsEqual<@P.AsGuid>.And<CSAnswers.attributeID.IsEqual<@P.AsString>>>
					.View.Select(_graph, template.NoteID, genRule.AttributeID);

				return string.IsNullOrEmpty(answer?.Value) ? string.Empty : GetAttributeCaption(genRule, answer.Value, locale);
			}
			else
			{
				return GetAttributeCaption(genRule.AttributeID, newItem, locale);
			}
		}

		protected virtual string GetAttributeCaption(string attributeID, MatrixInventoryItem newItem, string locale)
		{
			string segValue = string.Empty;

			for (int i = 0; i < newItem.AttributeIDs.Length; i++)
			{
				if (newItem.AttributeIDs[i].Equals(attributeID, StringComparison.OrdinalIgnoreCase))
				{
					if (!string.IsNullOrEmpty(locale))
					{
						string valueID = newItem.AttributeValues[i];

						segValue = GetComboAttributeCaption(attributeID, valueID, locale);
					}
					else
					{
						segValue = newItem.AttributeValueDescrs[i];
					}
					break;
				}
			}

			return segValue;
		}

		protected virtual string GetAttributeCaption(INMatrixGenerationRule genRule, string valueID, string locale)
		{
			var attribute = CRAttribute.Attributes[genRule.AttributeID];

			switch (attribute.ControlType)
			{
				case CSAttribute.Text:
				case CSAttribute.CheckBox:
				case CSAttribute.Datetime:
					return valueID;
				case CSAttribute.Combo:
					return GetComboAttributeCaption(genRule.AttributeID, valueID, locale);
				case CSAttribute.MultiSelectCombo:
					return GetMultiComboAttributeCaption(genRule, valueID, locale);
				default:
					throw new NotImplementedException();
			}
		}

		protected virtual string GetComboAttributeCaption(string attributeID, string valueID, string locale)
		{ 
			var attribute = CSAttributeDetail.PK.Find(_graph, attributeID, valueID);

			string segValue = null;

			if (!string.IsNullOrEmpty(locale))
			{
				segValue = PXDBLocalizableStringAttribute.GetTranslation<CSAttributeDetail.description>
				(_graph.Caches[typeof(CSAttributeDetail)], attribute, locale);
			}

			if (string.IsNullOrEmpty(segValue))
				segValue = attribute.Description;

			return segValue;
		}

		protected virtual string GetMultiComboAttributeCaption(INMatrixGenerationRule genRule, string valueID, string locale)
		{
			if (string.IsNullOrEmpty(valueID))
				return string.Empty;

			var values = valueID.Split(MultiComboAttributeSeparator);
			var captions = values.Select(v => GetComboAttributeCaption(genRule.AttributeID, v, locale));

			return string.Join(genRule.UseSpaceAsSeparator == true ? " " : genRule.Separator, captions);
		}

		public virtual string GetAttributeValue(INMatrixGenerationRule genRule, InventoryItem template, MatrixInventoryItem newItem)
		{
			string segValue = string.Empty;

			var attributeGroup = CSAttributeGroup.PK.Find(_graph,
				genRule.AttributeID, template.ParentItemClassID?.ToString(), typeof(InventoryItem).FullName);

			if (attributeGroup?.AttributeCategory == CSAttributeGroup.attributeCategory.Attribute)
			{
				CSAnswers answer = SelectFrom<CSAnswers>.
					Where<CSAnswers.refNoteID.IsEqual<@P.AsGuid>.And<CSAnswers.attributeID.IsEqual<@P.AsString>>>
					.View.Select(_graph, template.NoteID, genRule.AttributeID);

				segValue = answer?.Value ?? string.Empty;

				var attribute = CRAttribute.Attributes[genRule.AttributeID];

				if (attribute.ControlType == CSAttribute.MultiSelectCombo)
				{
					var values = segValue.Split(MultiComboAttributeSeparator);
					segValue = string.Join(genRule.UseSpaceAsSeparator == true ? " " : genRule.Separator, values);
				}
			}
			else
			{
				for (int i = 0; i < newItem.AttributeIDs.Length; i++)
				{
					if (newItem.AttributeIDs[i].Equals(genRule.AttributeID, StringComparison.OrdinalIgnoreCase))
					{
						segValue = newItem.AttributeValues[i];
						break;
					}
			}
			}

			return segValue;
		}

		public virtual void CreateUpdateMatrixItems(InventoryItemMaintBase graph, InventoryItem templateItem, IEnumerable<MatrixInventoryItem> itemsToCreateUpdate, bool create,
			Action<MatrixInventoryItem, InventoryItem> beforeSave = null)
		{
			Dictionary<string, string> templateAttrValues =
				PXSelectReadonly<CSAnswers, Where<CSAnswers.refNoteID, Equal<Required<InventoryItem.noteID>>>>
				.Select(graph, templateItem.NoteID)
				.RowCast<CSAnswers>()
				.ToDictionary(a => a.AttributeID, a => a.Value, StringComparer.OrdinalIgnoreCase);
			IEnumerable<POVendorInventory> templateVendorInvs =
				graph.VendorItems.View.SelectMultiBound(new[] { templateItem })
				.RowCast<POVendorInventory>()
				.ToArray();
			IEnumerable<INUnit> templateItemConvs =
				graph.itemunits.View.SelectMultiBound(new[] { templateItem })
				.RowCast<INUnit>()
				.ToArray();
			IEnumerable<INItemCategory> templateItemCategs =
				graph.Category.View.SelectMultiBound(new[] { templateItem })
				.RowCast<INItemCategory>()
				.ToArray();
			IEnumerable<INItemBoxEx> templateBoxes = null;
			InventoryItemMaint stockItemGraph = null;
			if (templateItem.StkItem == true)
			{
				stockItemGraph = (InventoryItemMaint)graph;
				templateBoxes = stockItemGraph.Boxes.View.SelectMultiBound(new[] { templateItem })
					.RowCast<INItemBoxEx>()
					.ToArray();
			}

			foreach (MatrixInventoryItem itemToCreateUpdate in itemsToCreateUpdate)
			{
				graph.Clear();

				InventoryItem item;
				if (create)
				{
					PXDimensionAttribute.SuppressAutoNumbering<InventoryItem.inventoryCD>(graph.Item.Cache, true);

					item = new InventoryItem
					{
						InventoryCD = itemToCreateUpdate.InventoryCD
					};
					item = graph.Item.Insert(item);
				}
				else
				{
					item = graph.Item.Current = graph.Item.Search<InventoryItem.inventoryCD>(itemToCreateUpdate.InventoryCD);
				}
				if (item == null)
				{
					throw new PXInvalidOperationException();
				}

				item = AssignInventoryFields(graph, templateItem, item, itemToCreateUpdate, create);
				AssignInventoryAttributes(graph, itemToCreateUpdate, templateItem, templateAttrValues, create);
				AssignVendorInventory(graph, templateVendorInvs);
				AssignInventoryConversions(graph, templateItemConvs);
				AssignInventoryCategories(graph, templateItemCategs);
				if (templateItem.StkItem == true)
					AssignInventoryBoxes(stockItemGraph, templateBoxes);

				beforeSave?.Invoke(itemToCreateUpdate, item);

				graph.Save.Press();

				itemToCreateUpdate.InventoryID = item.InventoryID;
			}
		}

		protected virtual InventoryItem AssignInventoryFields(InventoryItemMaintBase graph, InventoryItem templateItem, InventoryItem item,
			MatrixInventoryItem itemToCreateUpdate, bool create)
		{
			HashSet<string> userExcludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (create)
			{
				item = AssignInventoryField<InventoryItem.descr>(graph, item, itemToCreateUpdate.Descr);
				PXDBLocalizableStringAttribute.CopyTranslations
					<MatrixInventoryItem.descr, InventoryItem.descr>(graph, itemToCreateUpdate, item);
			}
			else
			{
				userExcludedFields.AddRange(
					GetExcludedFields(templateItem.InventoryID, typeof(InventoryItem)));
			}

			item = AssignInventoryField<InventoryItem.itemClassID>(graph, item, templateItem.ItemClassID);

			if (create || !userExcludedFields.Contains(nameof(InventoryItem.postClassID)))
				item = AssignInventoryField<InventoryItem.postClassID>(graph, item, templateItem.PostClassID);

			AssignConversionsSettings(graph, item, templateItem);
			item = AssignRestInventoryFields(graph, item, templateItem, userExcludedFields);

			item = AssignInventoryField<InventoryItem.templateItemID>(graph, item, templateItem.InventoryID);

			return item;
		}

		protected virtual InventoryItem AssignInventoryField<TField>(InventoryItemMaintBase graph, InventoryItem item, object value)
			where TField : IBqlField
		{
			var copy = (InventoryItem)graph.Item.Cache.CreateCopy(item);
			graph.Item.Cache.SetValue<TField>(copy, value);
			return graph.Item.Update(copy);
		}
		
		protected virtual void AssignConversionsSettings(InventoryItemMaintBase graph, InventoryItem item, InventoryItem templateItem)
		{
			//sales and purchase units must be cleared not to be added to item unit conversions on base unit change.
			PXCache cache = graph.Item.Cache;

			if (templateItem.BaseUnit != item.BaseUnit ||
				templateItem.SalesUnit != item.SalesUnit ||
				templateItem.PurchaseUnit != item.PurchaseUnit)
			{
				cache.SetValueExt<InventoryItem.baseUnit>(item, null);
				cache.SetValue<InventoryItem.salesUnit>(item, null);
				cache.SetValue<InventoryItem.purchaseUnit>(item, null);

				cache.SetValueExt<InventoryItem.baseUnit>(item, templateItem.BaseUnit);
				cache.SetValueExt<InventoryItem.salesUnit>(item, templateItem.SalesUnit);
				cache.SetValueExt<InventoryItem.purchaseUnit>(item, templateItem.PurchaseUnit);
			}

			cache.SetValueExt<InventoryItem.decimalBaseUnit>(item, templateItem.DecimalBaseUnit);
			cache.SetValueExt<InventoryItem.decimalSalesUnit>(item, templateItem.DecimalSalesUnit);
			cache.SetValueExt<InventoryItem.decimalPurchaseUnit>(item, templateItem.DecimalPurchaseUnit);
		}

		protected virtual InventoryItem AssignRestInventoryFields(InventoryItemMaintBase graph, InventoryItem item, InventoryItem templateItem,
			HashSet<string> userExcludedFields)
		{
			var copy = (InventoryItem)graph.Item.Cache.CreateCopy(item);
			graph.Item.Cache.RestoreCopy(copy, templateItem);

			var excludeFields = new string[]
			{
				nameof(InventoryItem.inventoryID),
				nameof(InventoryItem.inventoryCD),
				nameof(InventoryItem.descr),
				nameof(InventoryItem.preferredVendorID),
				nameof(InventoryItem.preferredVendorLocationID),
				nameof(InventoryItem.templateItemID),
				nameof(InventoryItem.isTemplate),
				nameof(InventoryItem.Tstamp),
				nameof(InventoryItem.createdByID),
				nameof(InventoryItem.createdByScreenID),
				nameof(InventoryItem.createdDateTime),
				nameof(InventoryItem.lastModifiedByID),
				nameof(InventoryItem.lastModifiedByScreenID),
				nameof(InventoryItem.lastModifiedDateTime),
				nameof(InventoryItem.noteID),
				nameof(InventoryItem.columnAttributeValue),
				nameof(InventoryItem.rowAttributeValue),
				nameof(InventoryItem.defaultColumnMatrixAttributeID),
				nameof(InventoryItem.defaultRowMatrixAttributeID),
				nameof(InventoryItem.attributeDescriptionGroupID),
			}.Concat(userExcludedFields);

			foreach (string excludeField in excludeFields)
			{
				graph.Item.Cache.SetValue(copy, excludeField,
					graph.Item.Cache.GetValue(item, excludeField));
			}

			return graph.Item.Update(copy);
		}

		protected virtual void AssignInventoryAttributes(InventoryItemMaintBase graph, MatrixInventoryItem itemToCreateUpdate,
			InventoryItem templateItem, Dictionary<string, string> templateAttrValues, bool create)
		{
			var excludeAttributes = create ? null :
				GetExcludedAttributes(itemToCreateUpdate.TemplateItemID)
					.ToHashSet(StringComparer.OrdinalIgnoreCase);

			CSAnswers[] answers = graph.Answers.Select().RowCast<CSAnswers>().ToArray();
			foreach (CSAnswers answer in answers)
			{
				if (!create && excludeAttributes.Contains(answer.AttributeID))
					continue;

				string value = null;
				for (int i = 0; i < itemToCreateUpdate.AttributeIDs.Length; i++)
				{
					if (itemToCreateUpdate.AttributeIDs[i].Equals(answer.AttributeID, StringComparison.OrdinalIgnoreCase))
					{
						value = itemToCreateUpdate.AttributeValues[i];
						break;
					}
				}
				if (value == null)
				{
					templateAttrValues.TryGetValue(answer.AttributeID, out value);
				}

				answer.Value = value;
				graph.Answers.Update(answer);
			}

			AssignDummyAttribute(graph, templateItem, answers);
		}

		protected virtual void AssignDummyAttribute(InventoryItemMaintBase graph, InventoryItem template, CSAnswers[] answers)
		{
			const string dummyAttribute = MatrixAttributeSelectorAttribute.DummyAttributeName;

			if ((template.DefaultColumnMatrixAttributeID == dummyAttribute ||
					template.DefaultRowMatrixAttributeID == dummyAttribute) &&
				!answers.Any(a => a.AttributeID == dummyAttribute))
			{
				var newAnswer = new CSAnswers()
				{
					AttributeCategory = CSAttributeGroup.attributeCategory.Variant,
					AttributeID = dummyAttribute,
					Value = MatrixAttributeSelectorAttribute.DummyAttributeValue,
					RefNoteID = graph.Item.Current.NoteID
				};

				graph.Answers.Insert(newAnswer);
			}
		}

		protected virtual void AssignVendorInventory(InventoryItemMaintBase graph, IEnumerable<POVendorInventory> templateVendorInvs)
		{
			string[] excludeFieldsCreate = null;
			string[] excludeFieldsUpdate = null;
			bool skipNewRows = false;

			POVendorInventory[] vendorInvs = graph.VendorItems.Select().RowCast<POVendorInventory>().ToArray();
			foreach (POVendorInventory templateVendorInv in templateVendorInvs)
			{
				POVendorInventory vendorInv =
					vendorInvs.FirstOrDefault(vi
						=> vi.SubItemID == templateVendorInv.SubItemID
						&& vi.VendorID == templateVendorInv.VendorID
						&& vi.VendorLocationID == templateVendorInv.VendorLocationID
						&& vi.PurchaseUnit == templateVendorInv.PurchaseUnit);

				bool createVendor = (vendorInv == null);

				var excludeFields = createVendor ? excludeFieldsCreate : excludeFieldsUpdate;
				if (excludeFields == null)
				{
					excludeFields = new string[]
					{
						nameof(POVendorInventory.recordID),
						nameof(POVendorInventory.inventoryID),
						nameof(POVendorInventory.Tstamp),
						nameof(POVendorInventory.createdByID),
						nameof(POVendorInventory.createdByScreenID),
						nameof(POVendorInventory.createdDateTime),
						nameof(POVendorInventory.lastModifiedByID),
						nameof(POVendorInventory.lastModifiedByScreenID),
						nameof(POVendorInventory.lastModifiedDateTime),
					};

					var userExcludedFields = GetExcludedFields(templateVendorInv.InventoryID, typeof(POVendorInventory));

					if (createVendor)
					{
						skipNewRows = userExcludedFields.Any(f => new[] {
							nameof(vendorInv.SubItemID),
							nameof(vendorInv.VendorID),
							nameof(vendorInv.VendorLocationID),
							nameof(vendorInv.PurchaseUnit)
							}.Contains(f, StringComparer.OrdinalIgnoreCase));

						excludeFieldsCreate = excludeFields;
					}
					else
					{
						excludeFields = excludeFields.Concat(userExcludedFields).ToArray();
						excludeFieldsUpdate = excludeFields;
					}
				}

				if (createVendor)
				{
					if (skipNewRows)
						continue;

					vendorInv = graph.VendorItems.Insert();
				}
				var copy = (POVendorInventory)graph.VendorItems.Cache.CreateCopy(vendorInv);
				graph.VendorItems.Cache.RestoreCopy(copy, templateVendorInv);

				foreach (string excludeField in excludeFields)
				{
					graph.VendorItems.Cache.SetValue(copy, excludeField,
						graph.VendorItems.Cache.GetValue(vendorInv, excludeField));
				}

				vendorInv = graph.VendorItems.Update(copy);
			}
		}

		protected virtual void AssignInventoryConversions(InventoryItemMaintBase graph, IEnumerable<INUnit> templateItemConvs)
		{
			string[] excludeFieldsCreate = null;
			string[] excludeFieldsUpdate = null;
			bool skipNewRows = false;

			INUnit[] itemConvs = graph.itemunits.Select().RowCast<INUnit>().ToArray();
			foreach (INUnit templateItemConv in templateItemConvs)
			{
				INUnit itemConv =
					itemConvs.FirstOrDefault(ic
						=> ic.FromUnit == templateItemConv.FromUnit
						&& ic.ToUnit == templateItemConv.ToUnit);

				bool createItemConv = (itemConv == null);

				var excludeFields = createItemConv ? excludeFieldsCreate : excludeFieldsUpdate;
				if (excludeFields == null)
				{
					excludeFields = new string[]
					{
						nameof(INUnit.recordID),
						nameof(INUnit.inventoryID),
						nameof(INUnit.Tstamp),
						nameof(INUnit.createdByID),
						nameof(INUnit.createdByScreenID),
						nameof(INUnit.createdDateTime),
						nameof(INUnit.lastModifiedByID),
						nameof(INUnit.lastModifiedByScreenID),
						nameof(INUnit.lastModifiedDateTime)
					};

					var userExcludedFields = GetExcludedFields(templateItemConv.InventoryID, typeof(INUnit));

					if (createItemConv)
					{
						skipNewRows = userExcludedFields.Any(f => new[] {
							nameof(INUnit.unitType),
							nameof(INUnit.fromUnit),
							nameof(INUnit.toUnit)
							}.Contains(f, StringComparer.OrdinalIgnoreCase));

						excludeFieldsCreate = excludeFields;
					}
					else
					{
						excludeFields = excludeFields.Concat(userExcludedFields).ToArray();
						excludeFieldsUpdate = excludeFields;
					}
				}

				if (createItemConv)
				{
					if (skipNewRows)
						continue;

					itemConv = graph.itemunits.Insert(new INUnit { FromUnit = templateItemConv.FromUnit });
				}
				var copy = (INUnit)graph.itemunits.Cache.CreateCopy(itemConv);
				graph.itemunits.Cache.RestoreCopy(copy, templateItemConv);

				foreach (string excludeField in excludeFields)
				{
					graph.itemunits.Cache.SetValue(copy, excludeField,
						graph.itemunits.Cache.GetValue(itemConv, excludeField));
				}

				itemConv = graph.itemunits.Update(copy);
			}
		}

		protected virtual void AssignInventoryCategories(InventoryItemMaintBase graph, IEnumerable<INItemCategory> templateItemCategs)
		{
			IEnumerable<string> excludeFieldsCreate = null;

			INItemCategory[] itemCategs = graph.Category.Select().RowCast<INItemCategory>().ToArray();
			foreach (INItemCategory templateItemCateg in templateItemCategs)
			{
				INItemCategory itemCateg =
					itemCategs.FirstOrDefault(ic => ic.CategoryID == templateItemCateg.CategoryID);

				bool createItemCateg = (itemCateg == null);
				if (!createItemCateg)
					continue;

				if (excludeFieldsCreate == null)
				{
					excludeFieldsCreate = GetExcludedFields(templateItemCateg.InventoryID, typeof(INItemCategory));

					if (excludeFieldsCreate.Contains(nameof(INItemCategory.categoryID), StringComparer.OrdinalIgnoreCase))
						return;
				}

				graph.Category.Insert(new INItemCategory { CategoryID = templateItemCateg.CategoryID });
			}
		}

		protected virtual void AssignInventoryBoxes(InventoryItemMaint graph, IEnumerable<INItemBoxEx> templateItemBoxes)
		{
			string[] excludeFieldsCreate = null;
			string[] excludeFieldsUpdate = null;
			bool skipNewRows = false;

			INItemBoxEx[] itemBoxes = graph.Boxes.Select().RowCast<INItemBoxEx>().ToArray();
			foreach (INItemBoxEx templateItemBox in templateItemBoxes)
			{
				INItemBoxEx itemBox =
					itemBoxes.FirstOrDefault(ic => ic.BoxID == templateItemBox.BoxID);

				bool createItemBox = (itemBox == null);

				var excludeFields = createItemBox ? excludeFieldsCreate : excludeFieldsUpdate;
				if (excludeFields == null)
				{
					excludeFields = new string[]
					{
						nameof(INItemBoxEx.inventoryID),
						nameof(INItemBoxEx.Tstamp),
						nameof(INItemBoxEx.createdByID),
						nameof(INItemBoxEx.createdByScreenID),
						nameof(INItemBoxEx.createdDateTime),
						nameof(INItemBoxEx.lastModifiedByID),
						nameof(INItemBoxEx.lastModifiedByScreenID),
						nameof(INItemBoxEx.lastModifiedDateTime),
						nameof(INItemBoxEx.noteID)
					};

					var userExcludedFields = GetExcludedFields(templateItemBox.InventoryID, typeof(INItemBox));

					if (createItemBox)
					{
						skipNewRows = userExcludedFields.Contains(
							nameof(INItemBoxEx.BoxID), StringComparer.OrdinalIgnoreCase);

						excludeFieldsCreate = excludeFields;
					}
					else
					{
						excludeFields = excludeFields.Concat(userExcludedFields).ToArray();
						excludeFieldsUpdate = excludeFields;
					}
				}

				if (createItemBox)
				{
					if (skipNewRows)
						continue;

					itemBox = graph.Boxes.Insert(new INItemBoxEx { BoxID = templateItemBox.BoxID });
				}
				var copy = (INItemBoxEx)graph.Boxes.Cache.CreateCopy(itemBox);
				graph.Boxes.Cache.RestoreCopy(copy, templateItemBox);

				foreach (string excludeField in excludeFields)
				{
					graph.Boxes.Cache.SetValue(copy, excludeField,
						graph.Boxes.Cache.GetValue(itemBox, excludeField));
				}

				itemBox = graph.Boxes.Update(copy);
			}
		}

		protected static TResult GetValueFromArray<TResult>(TResult[] array, int index)
		{
			if (index >= 0 && index < array?.Length)
				return array[index];

			return default;
		}

		#region User Excluded Fields / Attributes

		public virtual (string FieldName, string DisplayName)[] GetAttributesToUpdateItem(InventoryItem item)
		{
			return new PXSelectJoin<CSAttributeGroup,
				InnerJoin<CSAttribute, On<CSAttribute.attributeID, Equal<CSAttributeGroup.attributeID>>>,
				Where<CSAttributeGroup.entityClassID, Equal<Required<InventoryItem.itemClassID>>,
					And<CSAttributeGroup.entityType, Equal<Common.Constants.DACName<InventoryItem>>,
					And<CSAttributeGroup.isActive, Equal<True>,
					And<CSAttributeGroup.attributeCategory, Equal<CSAttributeGroup.attributeCategory.attribute>>>>>,
				OrderBy<Asc<CSAttributeGroup.sortOrder, Asc<CSAttributeGroup.attributeID>>>>(_graph)
			.Select(item.ItemClassID)
			.AsEnumerable()
			.Select(r => (((CSAttributeGroup)r).AttributeID, PXResult.Unwrap<CSAttribute>(r).Description)).ToArray();
		}

		protected virtual IEnumerable<string> GetExcludedAttributes(int? templateID)
		{
			return new PXSelect<ExcludedAttribute,
				Where<ExcludedAttribute.templateID, Equal<Required<InventoryItem.templateItemID>>,
					And<ExcludedAttribute.isActive, Equal<True>>>>(_graph)
					.SelectMain(templateID).Select(excludeAttribute => excludeAttribute.FieldName);
		}

		public virtual (Type Dac, string DisplayName)[] GetTablesToUpdateItem()
		{
			var tables = new Type[]
			{
				typeof(InventoryItem),
				typeof(POVendorInventory),
				typeof(INUnit),
				typeof(INItemCategory),
				typeof(INItemBox)
			};

			return tables.Select(t => (t, _graph.Caches[t].DisplayName)).ToArray();
		}

		public virtual (string FieldName, string DisplayName)[] GetFieldsToUpdateItem(Type table)
		{
			var cache = _graph.Caches[table];
			IEnumerable<string> result = cache.BqlFields
				.Select(f => f.Name)
				.Where(field =>
					cache.GetAttributesOfType<PXDBFieldAttribute>(null, field).Any() &&
					cache.GetAttributesOfType<PXUIFieldAttribute>(null, field).Any(a => a.Enabled));


			var systemFields = new string[]
			{
				nameof(InventoryItem.Tstamp),
				nameof(InventoryItem.createdByID),
				nameof(InventoryItem.createdByScreenID),
				nameof(InventoryItem.createdDateTime),
				nameof(InventoryItem.lastModifiedByID),
				nameof(InventoryItem.lastModifiedByScreenID),
				nameof(InventoryItem.lastModifiedDateTime),
				nameof(InventoryItem.noteID),

			};

			result = result.Except(systemFields, StringComparer.OrdinalIgnoreCase);

			var keyFields = new string[]
			{
				nameof(InventoryItem.inventoryID),
				nameof(InventoryItem.inventoryCD),
				nameof(POVendorInventory.recordID),
			};

			result = result.Except(keyFields, StringComparer.OrdinalIgnoreCase);

			if (table == typeof(InventoryItem))
			{
				var excludeFields = new string[]
				{
					nameof(InventoryItem.itemClassID),
					nameof(InventoryItem.baseUnit),
					nameof(InventoryItem.decimalBaseUnit),
					nameof(InventoryItem.purchaseUnit),
					nameof(InventoryItem.decimalPurchaseUnit),
					nameof(InventoryItem.salesUnit),
					nameof(InventoryItem.decimalSalesUnit),
					nameof(InventoryItem.itemType),
					nameof(InventoryItem.taxCategoryID),
					nameof(InventoryItem.kitItem),
					nameof(InventoryItem.valMethod),
					nameof(InventoryItem.completePOLine),
					nameof(InventoryItem.nonStockReceipt),
					nameof(InventoryItem.nonStockShip),
					nameof(InventoryItem.preferredVendorID),
					nameof(InventoryItem.preferredVendorLocationID),
					nameof(InventoryItem.templateItemID),
					nameof(InventoryItem.isTemplate),
					nameof(InventoryItem.columnAttributeValue),
					nameof(InventoryItem.rowAttributeValue),
					nameof(InventoryItem.defaultColumnMatrixAttributeID),
					nameof(InventoryItem.defaultRowMatrixAttributeID),
					nameof(InventoryItem.attributeDescriptionGroupID),
				};

				result = result.Except(excludeFields, StringComparer.OrdinalIgnoreCase);
			}

			return result.Select(field => (field, PXUIFieldAttribute.GetDisplayName(cache, field) ?? field)).ToArray();
		}

		protected virtual IEnumerable<string> GetExcludedFields(int? templateID, Type tableName)
		{
			return new PXSelect<ExcludedField,
				Where<ExcludedField.templateID, Equal<Required<InventoryItem.templateItemID>>,
					And<ExcludedField.tableName, Equal<Required<ExcludedField.tableName>>,
					And<ExcludedField.isActive, Equal<True>>>>>(_graph)
					.SelectMain(templateID, tableName.FullName).Select(excludeField => excludeField.FieldName);
		}

		#endregion // User Excluded Fields / Attributes
	}
}
