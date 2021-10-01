using PX.Objects.PM;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class ExpenseDetailSplitGrouping : IGrouping<ExpenseDetailSplitKey, PREarningDetail>
	{
		public ExpenseDetailSplitKey Key => _EarningGroup.Key;

		private IGrouping<ExpenseDetailSplitKey, PREarningDetail> _EarningGroup;

		public ExpenseDetailSplitGrouping(IGrouping<ExpenseDetailSplitKey, PREarningDetail> earningGroup)
		{
			_EarningGroup = earningGroup;
		}

		public IEnumerator<PREarningDetail> GetEnumerator() => _EarningGroup.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _EarningGroup.GetEnumerator();
	}

	public struct ExpenseDetailSplitKey
	{
		public int? BranchID;
		public int? CostCodeID;
		public int? ProjectID;
		public int? ProjectTaskID;
		public int? LaborItemID;
		public string EarningTypeCD;
	}

	public static class ExpenseDetailSplitExtensions
	{
		public static IEnumerable<ExpenseDetailSplitGrouping> SplitExpenseDetails(this List<PREarningDetail> earnings, ExpenseDetailSplitType splitType)
		{
			return earnings.GroupBy(earning =>
			{
				ExpenseDetailSplitKey key = new ExpenseDetailSplitKey();
				if ((splitType & ExpenseDetailSplitType.Branch) == ExpenseDetailSplitType.Branch)
				{
					key.BranchID = earning.BranchID;
				}
				if ((splitType & ExpenseDetailSplitType.CostCode) == ExpenseDetailSplitType.CostCode)
				{
					key.CostCodeID = earning.CostCodeID;
				}
				if ((splitType & ExpenseDetailSplitType.ProjectTask) == ExpenseDetailSplitType.ProjectTask)
				{
					key.ProjectID = GetProjectIDForGrouping(earning);
					key.ProjectTaskID = earning.ProjectTaskID;
				}
				if ((splitType & ExpenseDetailSplitType.EarningType) == ExpenseDetailSplitType.EarningType)
				{
					key.EarningTypeCD = earning.TypeCD;
				}
				if ((splitType & ExpenseDetailSplitType.LaborItem) == ExpenseDetailSplitType.LaborItem)
				{
					key.LaborItemID = earning.LabourItemID;
				}
				return key;
			}).Select(x => new ExpenseDetailSplitGrouping(x));
		}

		private static int? GetProjectIDForGrouping(PREarningDetail earning)
		{
			if (ProjectDefaultAttribute.IsNonProject(earning.ProjectID))
			{
				return null;
			}

			return earning.ProjectID;
		}

	}

	public enum ExpenseDetailSplitType
	{
		Branch = 0x01,
		CostCode = 0x02,
		ProjectTask = Branch | CostCode | 0x04,
		EarningType = 0x08,
		LaborItem = 0x10
	}
}
