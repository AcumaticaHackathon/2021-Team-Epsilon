using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.Matrix.DAC.Projections
{
	[PXCacheName(Messages.INMatrixExcludedAttribute)]
	[PXBreakInheritance]
	[PXProjection(typeof(Select<INMatrixExcludedData,
		Where<INMatrixExcludedData.type, Equal<INMatrixExcludedData.type.attribute>>>), Persistent = true)]
	public class ExcludedAttribute : INMatrixExcludedData
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ExcludedAttribute>.By<templateID, type, tableName, fieldName>
		{
			public static ExcludedAttribute Find(PXGraph graph, int? templateID, string type, string tableName, string fieldName)
				=> FindBy(graph, templateID, type, tableName, fieldName);
		}
		#endregion

		#region Type
		public abstract new class type : PX.Data.BQL.BqlString.Field<type> { }

		/// <summary>
		/// Type of row: 'F' - field, 'A' - attribute.
		/// </summary>
		[PXDBString(1, IsKey = true, IsFixed = true, IsUnicode = false)]
		[INMatrixExcludedData.type.List]
		[PXDefault(INMatrixExcludedData.type.Attribute)]
		public override string Type
		{
			get => base.Type;
			set => base.Type = value;
		}
		#endregion
		#region TableName
		public abstract new class tableName : PX.Data.BQL.BqlString.Field<tableName> { }

		/// <summary>
		/// References to a DAC name.
		/// </summary>
		[PXDBString(255, IsKey = true)]
		[PXUIField(DisplayName = "Table Name", Required = true)]
		[PXDefault(typeof(Common.Constants.DACName<CSAttribute>))]
		public override string TableName
		{
			get => base.TableName;
			set => base.TableName = value;
		}
		#endregion
		#region FieldName
		public abstract new class fieldName : PX.Data.BQL.BqlString.Field<fieldName> { }

		/// <summary>
		/// References to field name of related DAC <see cref="TableName"/>.
		/// </summary>
		[PXDBString(255, IsKey = true)]
		[PXUIField(DisplayName = "Attribute", Required = true)]
		[PXDefault]
		[ExcludedFieldSelector(INMatrixExcludedData.type.Attribute)]
		public override string FieldName
		{
			get => base.FieldName;
			set => base.FieldName = value;
		}
		#endregion
		#region TemplateID
		public abstract new class templateID : PX.Data.BQL.BqlInt.Field<templateID> { }

		/// <summary>
		/// Template Inventory Item identifier.
		/// </summary>
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		[PXParent(typeof(FK.TemplateInventoryItem))]
		public override int? TemplateID
		{
			get => base.TemplateID;
			set => base.TemplateID = value;
		}
		#endregion
	}
}
