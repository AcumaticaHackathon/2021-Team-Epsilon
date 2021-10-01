using System;
using PX.Common;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.Common;
using PX.Objects.Common.Attributes;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.DAC.Projections;

namespace PX.Objects.IN.Matrix.DAC
{
	[PXCacheName(Messages.INMatrixGenerationRuleDAC)]
	public class INMatrixGenerationRule : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<INMatrixGenerationRule>.By<parentType, parentID, type, lineNbr>
		{
			public static INMatrixGenerationRule Find(PXGraph graph, string parentType, int? parentID, string type, int? lineNbr)
				=> FindBy(graph, parentType, parentID, type, lineNbr);
		}
		public static class FK
		{
			public class TemplateInventoryItem : InventoryItem.PK.ForeignKeyOf<INMatrixGenerationRule>.By<parentID> { }
			public class ItemClass : INItemClass.PK.ForeignKeyOf<INMatrixGenerationRule>.By<parentID> { }
			public class Attribute : CSAttribute.PK.ForeignKeyOf<INMatrixGenerationRule>.By<attributeID> { }
			public class Numbering : CS.Numbering.PK.ForeignKeyOf<INMatrixGenerationRule>.By<numberingID> { }
		}
		#endregion

		#region ParentID
		public abstract class parentID : PX.Data.BQL.BqlInt.Field<parentID> { }

		/// <summary>
		/// Template Inventory Item identifier or Item Class identifier (depends on <see cref="ParentType"/>).
		/// </summary>
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		public virtual int? ParentID
		{
			get;
			set;
		}
		#endregion
		#region ParentType
		public abstract class parentType : PX.Data.BQL.BqlInt.Field<parentType>
		{
			public const string TemplateItem = "T";
			public const string ItemClass = "C";

			[PXLocalizable]
			public class DisplayNames
			{
				public const string TemplateItem = "Template Inventory Item";
				public const string ItemClass = "Item Class";
			}

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base(
					new[]
					{
						Pair(TemplateItem, DisplayNames.TemplateItem),
						Pair(ItemClass, DisplayNames.ItemClass),
					})
				{ }
			}

			public class templateItem : PX.Data.BQL.BqlString.Constant<templateItem>
			{
				public templateItem() : base(TemplateItem) { }
			}

			public class itemClass : PX.Data.BQL.BqlString.Constant<itemClass>
			{
				public itemClass() : base(ItemClass) { }
			}
		}

		/// <summary>
		/// Type of ParentID value: C - Item Class, T - Template Inventory Item.
		/// </summary>
		[PXDBString(1, IsUnicode = false, IsFixed = true, IsKey = true)]
		[parentType.List]
		[PXDefault(parentType.TemplateItem)]
		public virtual string ParentType
		{
			get;
			set;
		}
		#endregion
		#region Type
		public abstract class type : PX.Data.BQL.BqlString.Field<type>
		{
			public const string ID = "I";
			public const string Description = "D";

			[PXLocalizable]
			public class DisplayNames
			{
				public const string ID = "ID";
				public const string Description = "Description";
			}

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base(
					new[]
					{
						Pair(ID, DisplayNames.ID),
						Pair(Description, DisplayNames.Description),
					})
				{ }
			}

			public class id : PX.Data.BQL.BqlString.Constant<id>
			{
				public id() : base(ID) { }
			}

			public class description : PX.Data.BQL.BqlString.Constant<description>
			{
				public description() : base(Description) { }
			}
		}

		[PXDBString(1, IsKey = true, IsFixed = true, IsUnicode = false)]
		[type.List]
		public virtual string Type
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(InventoryItem.generationRuleCntr))]
		[PXUIField(DisplayName = "Line Nbr.", Visible = false)]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion

		#region SegmentType
		public abstract class segmentType : PX.Data.BQL.BqlString.Field<segmentType>
		{
			public const string TemplateID = "TI";
			public const string TemplateDescription = "TD";
			public const string AttributeCaption = "AC";
			public const string AttributeValue = "AV";
			public const string Constant = "CO";
			public const string AutoNumber = "AN";
			public const string Space = "SP";

			[PXLocalizable]
			public class DisplayNames
			{
				public const string TemplateID = "Template ID";
				public const string TemplateDescription = "Template Description";
				public const string AttributeCaption = "Attribute Caption";
				public const string AttributeValue = "Attribute Value";
				public const string Constant = "Constant";
				public const string AutoNumber = "Auto Number";
				public const string Space = "Space";
			}

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base(
					new[]
					{
						Pair(TemplateID, DisplayNames.TemplateID),
						Pair(TemplateDescription, DisplayNames.TemplateDescription),
						Pair(AttributeCaption, DisplayNames.AttributeCaption),
						Pair(AttributeValue, DisplayNames.AttributeValue),
						Pair(Constant, DisplayNames.Constant),
						Pair(Space, DisplayNames.Space),
						Pair(AutoNumber, DisplayNames.AutoNumber),
					})
				{ }
			}
		}

		[PXDBString(2, IsFixed = true, IsUnicode = false)]
		[PXUIField(DisplayName = "Segment Type")]
		[segmentType.List]
		public virtual string SegmentType
		{
			get;
			set;
		}
		#endregion
		#region AttributeID
		public abstract class attributeID : PX.Data.BQL.BqlString.Field<attributeID> { }

		/// <summary>
		/// References to Attribute which will be put as part of result string.
		/// </summary>
		[DefaultConditional(typeof(IDGenerationRule.segmentType), IDGenerationRule.segmentType.AttributeCaption, IDGenerationRule.segmentType.AttributeValue)]
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Attribute ID", Required = true)]
		[PXSelector(typeof(Search<CSAttributeGroup.attributeID,
			Where<CSAttributeGroup.entityClassID, Equal<Current<InventoryItem.parentItemClassID>>,
				And<CSAttributeGroup.entityType, Equal<Constants.DACName<InventoryItem>>>>>),
			typeof(CSAttributeGroup.attributeID), typeof(CSAttributeGroup.description))]
		[PXRestrictor(typeof(Where<CSAttributeGroup.isActive, Equal<True>>), Messages.AttributeIsInactive, typeof(CSAttributeGroup.attributeID))]
		public virtual string AttributeID
		{
			get;
			set;
		}
		#endregion
		#region Constant
		public abstract class constant : PX.Data.BQL.BqlString.Field<constant> { }

		/// <summary>
		/// User text which will be put as part of result string.
		/// </summary>
		[PXDBString(100, IsUnicode = true)]
		[PXUIField(DisplayName = "Constant")]
		public virtual string Constant
		{
			get;
			set;
		}
		#endregion
		#region NumberingID
		public abstract class numberingID : PX.Data.BQL.BqlString.Field<numberingID> { }
		[DefaultConditional(typeof(IDGenerationRule.segmentType), IDGenerationRule.segmentType.AutoNumber)]
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Numbering ID", Required = true)]
		[PXSelector(typeof(Numbering.numberingID))]
		public virtual string NumberingID
		{
			get;
			set;
		}
		#endregion
		#region NumberOfCharacters
		public abstract class numberOfCharacters : PX.Data.BQL.BqlInt.Field<numberOfCharacters> { }

		/// <summary>
		/// Number of characters for this part in result string.
		/// </summary>
		[PXDBInt]
		[PXUIField(DisplayName = "Number of Characters", Required = true)]
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		public virtual int? NumberOfCharacters
		{
			get;
			set;
		}
		#endregion
		#region UseSpaceAsSeparator
		public abstract class useSpaceAsSeparator : PX.Data.BQL.BqlBool.Field<useSpaceAsSeparator> { }

		/// <summary>
		/// Use a Space as Separator after this part.
		/// </summary>
		[PXDBBool]
		[PXUIField(DisplayName = "Use Space as Separator")]
		[PXDefault(false)]
		public virtual bool? UseSpaceAsSeparator
		{
			get;
			set;
		}
		#endregion
		#region Separator
		public abstract class separator : PX.Data.BQL.BqlString.Field<separator> { }

		/// <summary>
		/// Separator after this part.
		/// </summary>
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Separator")]
		[PXDefault("-", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string Separator
		{
			get;
			set;
		}
		#endregion
		#region SortOrder
		public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
		[PXUIField(DisplayName = "Line Order", Visible = false, Enabled = false)]
		[PXDBInt]
		public virtual int? SortOrder
		{
			get;
			set;
		}
		#endregion
		#region AddSpaces
		public abstract class addSpaces : PX.Data.BQL.BqlBool.Field<addSpaces> { }

		/// <summary>
		/// If true and length of the result string is less than number of characters (<see cref="NumberOfCharacters"/>), the system will add spaces.
		/// </summary>
		[PXUIField(DisplayName = "Add Spaces")]
		[PXDefault]
		[PXDBBool]
		public virtual bool? AddSpaces
		{
			get;
			set;
		}
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID
		{
			get;
			set;
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get;
			set;
		}
		#endregion
	}
}
