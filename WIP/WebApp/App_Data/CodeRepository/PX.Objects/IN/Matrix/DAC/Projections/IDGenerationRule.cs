using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.IN.Matrix.DAC.Projections
{
	[PXCacheName(Messages.IDGenerationRuleDAC)]
	[PXBreakInheritance]
	[PXProjection(typeof(Select<INMatrixGenerationRule,
		Where<IDGenerationRule.type, Equal<INMatrixGenerationRule.type.id>>>), Persistent = true)]
	public class IDGenerationRule : INMatrixGenerationRule
	{
		#region Keys
		public new class PK : PrimaryKeyOf<IDGenerationRule>.By<parentType, parentID, type, lineNbr>
		{
			public static IDGenerationRule Find(PXGraph graph, string parentType, int? parentID, string type, int? lineNbr)
				=> FindBy(graph, parentType, parentID, type, lineNbr);
		}
		public static new class FK
		{
			public class TemplateInventoryItem : InventoryItem.PK.ForeignKeyOf<IDGenerationRule>.By<parentID> { }
			public class ItemClass : INItemClass.PK.ForeignKeyOf<IDGenerationRule>.By<parentID> { }
		}
		#endregion

		#region ParentID
		public abstract new class parentID : PX.Data.BQL.BqlInt.Field<parentID> { }

		/// <summary>
		/// Template Inventory Item identifier.
		/// </summary>
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		[PXParent(typeof(FK.TemplateInventoryItem))]
		public override int? ParentID
		{
			get => base.ParentID;
			set => base.ParentID = value;
		}
		#endregion
		#region ParentType
		public abstract new class parentType : PX.Data.BQL.BqlString.Field<parentType> { }
		#endregion
		#region Type
		public abstract new class type : PX.Data.BQL.BqlString.Field<type> { }

		[PXDBString(1, IsKey = true, IsFixed = true, IsUnicode = false)]
		[INMatrixGenerationRule.type.List]
		[PXDefault(INMatrixGenerationRule.type.ID)]
		public override string Type
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract new class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(InventoryItem.generationRuleCntr))]
		[PXUIField(DisplayName = "Line Nbr.", Visible = false)]
		public override int? LineNbr
		{
			get => base.LineNbr;
			set => base.LineNbr = value;
		}
		#endregion
		#region SortOrder
		public abstract new class sortOrder : PX.Data.BQL.BqlString.Field<sortOrder> { }
		#endregion
		#region AttributeID
		public abstract new class attributeID : PX.Data.BQL.BqlString.Field<attributeID> { }
		#endregion
		#region AddSpaces
		public abstract new class addSpaces : PX.Data.BQL.BqlBool.Field<addSpaces> { }
		#endregion

	}
}
