using PX.Data;
using PX.Data.BQL.Fluent;

namespace PX.Objects.PM
{
	public abstract class WorkCodeSources<TGraph> : PXGraphExtension<TGraph>
		where TGraph : PXGraph
	{
		public SelectFrom<PMWorkCodeCostCodeRange>
			.Where<PMWorkCodeCostCodeRange.workCodeID.IsEqual<PMWorkCode.workCodeID.FromCurrent>>.View CostCodeRanges;

		public SelectFrom<PMWorkCodeProjectTaskSource>
			.Where<PMWorkCodeProjectTaskSource.workCodeID.IsEqual<PMWorkCode.workCodeID.FromCurrent>>.View ProjectTaskSources;

		public SelectFrom<PMWorkCodeLaborItemSource>
			.Where<PMWorkCodeLaborItemSource.workCodeID.IsEqual<PMWorkCode.workCodeID.FromCurrent>>.View LaborItemSources;
	}
}
