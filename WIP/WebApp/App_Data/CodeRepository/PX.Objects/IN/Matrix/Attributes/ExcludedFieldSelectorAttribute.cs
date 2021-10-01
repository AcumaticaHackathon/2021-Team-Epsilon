using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.DAC;
using PX.Objects.IN.Matrix.DAC.Projections;
using PX.Objects.IN.Matrix.DAC.Unbound;
using PX.Objects.IN.Matrix.Interfaces;
using PX.Objects.IN.Matrix.Utility;
using PX.Objects.PO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.IN.Matrix.Attributes
{
	public class ExcludedFieldSelectorAttribute : PXCustomSelectorAttribute
	{
		[PXCacheName(Messages.INMatrixExcludedFieldName)]
		[PXVirtual]
		public class ExcludedFieldName : IBqlTable
		{
			#region TableName
			public abstract class tableName : PX.Data.BQL.BqlString.Field<tableName> { }

			[PXString(IsKey = true)]
			[PXUIField(DisplayName = "Table Name")]
			public string TableName
			{
				get;
				set;
			}
			#endregion // TableName
			#region Name
			public abstract class name : PX.Data.BQL.BqlString.Field<name> { }

			[PXString(IsKey = true)]
			[PXUIField(DisplayName = "DB Field Name")]
			public string Name
			{
				get;
				set;
			}
			#endregion // Name
			#region Description
			public abstract class description : PX.Data.BQL.BqlString.Field<description> { }

			[PXString]
			[PXUIField(DisplayName = "Box Name")]
			public string Description
			{
				get;
				set;
			}
			#endregion // Description
		}

		public virtual ICreateMatrixHelperFactory CreateMatrixHelperFactory { get; set; }
		protected string _type;

		public ExcludedFieldSelectorAttribute(string type)
			: base(type == INMatrixExcludedData.type.Field ?
				  typeof(Search<ExcludedFieldName.name, Where<ExcludedFieldName.tableName, Equal<Current<ExcludedField.tableName>>>>) :
				  typeof(Search<ExcludedFieldName.name, Where<ExcludedFieldName.tableName, Equal<Common.Constants.DACName<CSAttribute>>>>),
				  new Type[] { typeof(ExcludedFieldName.description), typeof(ExcludedFieldName.name) })
		{
			_type = type;
			base.CacheGlobal = false;
			base.ValidateValue = true;
			base.DescriptionField = typeof(ExcludedFieldName.description);
			base.SelectorMode = PXSelectorMode.DisplayMode;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			if (CreateMatrixHelperFactory == null)
				CreateMatrixHelperFactory = sender.Graph as ICreateMatrixHelperFactory;
		}

		protected virtual IEnumerable GetRecords()
		{
			var values = new List<ExcludedFieldName>();

			var itemsHelper = CreateMatrixHelperFactory?.GetCreateMatrixItemsHelper();
			if (itemsHelper == null)
				return values;

			switch (_type)
			{
				case INMatrixExcludedData.type.Attribute:
					values.AddRange(GetAttributes(itemsHelper));
					break;

				case INMatrixExcludedData.type.Field:
					if (PXView.Parameters?.Length > 0)
					{
						var tableName = PXView.Parameters[0] as string;
						var table = itemsHelper.GetTablesToUpdateItem()?
							.Where(t => t.Dac.FullName == tableName).FirstOrDefault().Dac;

						if (table != null)
							values.AddRange(GetFields(table, itemsHelper));
					}
					break;

				default:
					throw new NotImplementedException();
			}

			return values;
		}

		protected virtual IEnumerable<ExcludedFieldName> GetFields(Type table, CreateMatrixItemsHelper itemsHelper)
		{
			return itemsHelper.GetFieldsToUpdateItem(table).Select(field => new ExcludedFieldName()
			{
				TableName = table.FullName,
				Name = field.FieldName,
				Description = field.DisplayName
			});
		}

		protected virtual IEnumerable<ExcludedFieldName> GetAttributes(CreateMatrixItemsHelper itemsHelper)
		{
			var templateItem = (InventoryItem)_Graph.Caches<InventoryItem>().Current;
			if (templateItem == null)
				return new ExcludedFieldName[0];

			return itemsHelper.GetAttributesToUpdateItem(templateItem).Select(field => new ExcludedFieldName()
			{
				TableName = typeof(CSAttribute).FullName,
				Name = field.FieldName,
				Description = field.DisplayName
			});
		}

	}
}
