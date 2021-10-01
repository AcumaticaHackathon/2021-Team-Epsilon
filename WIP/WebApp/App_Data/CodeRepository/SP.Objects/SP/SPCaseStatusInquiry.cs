using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using SP.Objects.CR;

namespace SP.Objects.SP
{
	/// <exclude/>
	public class SPCaseStatusInquiry : PXGraph<SPCaseStatusInquiry>
	{
		[Serializable]
		[PXHidden]
		public class CaseStatusFilter : IBqlTable
		{
			#region Status
			public abstract class status : BqlString.Field<status> { }
			[PXString]
			[PXStringList(new string[0], new string[0], BqlField = typeof(CRCase.status))]
			[PXUIField(DisplayName = "Status", Required = true, Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string Status { get; set; }
			#endregion
		}

		public PXFilter<CaseStatusFilter> Filter;

		[PXFilterable]
		public
			SelectFrom<CRCase>
			.LeftJoin<CRCaseClass>.On<CRCaseClass.caseClassID.IsEqual<CRCase.caseClassID>>
			.Where<CRCase.status.IsEqual<CaseStatusFilter.status.FromCurrent>
				.And<MatchWithBAccountNotNull<CRCase.customerID>>
				.And<Brackets<CRCaseClass.isInternal.IsEqual<False>.Or<CRCaseClass.isInternal.IsNull>>>>
			.View.ReadOnly
			FilteredItems;

		#region Actions

		public PXAction<CaseStatusFilter> ViewCase;
		[PXUIField(DisplayName = "View Case", Visible = false)]
		[PXButton]
		public virtual IEnumerable viewCase(PXAdapter adapter)
		{
			if (FilteredItems.Current != null)
			{
				CRCaseMaint graph = CreateInstance<CRCaseMaint>();
				PXResult result = graph.Case.Search<CRCase.caseCD>(FilteredItems.Current.CaseCD);
				CRCase @case = result[typeof(CRCase)] as CRCase;
				graph.Case.Current = @case;
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
			}
			return adapter.Get();
		}
		#endregion
	}
}
