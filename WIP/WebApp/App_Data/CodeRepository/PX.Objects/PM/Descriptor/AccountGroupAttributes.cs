using PX.Data;
using PX.Objects.GL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
	/// <summary>
	/// Displays all AccountGroups sorted by SortOrder.
	/// </summary>
	/// 
	[PXDBInt()]
	[PXUIField(DisplayName = "Account Group", Visibility = PXUIVisibility.Visible)]
	public class AccountGroupAttribute : AcctSubAttribute
	{
		public const string DimensionName = "ACCGROUP";
		protected Type showGLAccountGroups;


		public AccountGroupAttribute() : this(typeof(Where<PMAccountGroup.groupID, IsNotNull>))
		{
		}

		public AccountGroupAttribute(Type WhereType)
		{
			Type SearchType =
				BqlCommand.Compose(
				typeof(Search<,,>),
				typeof(PMAccountGroup.groupID),
				WhereType,
				typeof(OrderBy<Asc<PMAccountGroup.sortOrder>>)
				);

			PXDimensionSelectorAttribute select = new PXDimensionSelectorAttribute(DimensionName, SearchType, typeof(PMAccountGroup.groupCD),
				typeof(PMAccountGroup.groupCD), typeof(PMAccountGroup.description), typeof(PMAccountGroup.type), typeof(PMAccountGroup.isActive));
			select.DescriptionField = typeof(PMAccountGroup.description);
			select.CacheGlobal = true;

			_Attributes.Add(select);
			_SelAttrIndex = _Attributes.Count - 1;
		}

	}


	/// <summary>
	/// Base attribute for AccountGroupCD field. Aggregates PXFieldAttribute, PXUIFieldAttribute and DimensionSelector without any restriction.
	/// </summary>
	[PXDBString(30, IsUnicode = true, InputMask = "")]
	[PXUIField(DisplayName = "Account Group", Visibility = PXUIVisibility.Visible)]
	public class AccountGroupRawAttribute : AcctSubAttribute
	{
		public AccountGroupRawAttribute()
			: base()
		{
			PXDimensionAttribute attr = new PXDimensionAttribute(AccountGroupAttribute.DimensionName);
			attr.ValidComboRequired = false;
			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}
	}
}
