using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
	public class DimensionMaintExt : PXGraphExtension<DimensionMaint>
	{
		public PXSelect<Dimension, Where<Dimension.dimensionID, InFieldClassActivated,
			Or<Dimension.dimensionID, IsNull,
			Or<Dimension.dimensionID, Equal<PM.ProjectAttribute.dimension>,
			Or<Dimension.dimensionID, Equal<PM.ProjectAttribute.dimensionTM>,
			Or<Dimension.dimensionID, Equal<CT.ContractAttribute.dimension>,
			Or<Dimension.dimensionID, Equal<CT.ContractTemplateAttribute.dimension>>>>>>>> Header;

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search<Dimension.dimensionID, Where<Dimension.dimensionID, InFieldClassActivated,
			Or<Dimension.dimensionID, Equal<PM.ProjectAttribute.dimension>,
			Or<Dimension.dimensionID, Equal<PM.ProjectAttribute.dimensionTM>,
			Or<Dimension.dimensionID, Equal<CT.ContractAttribute.dimension>,
			Or<Dimension.dimensionID, Equal<CT.ContractTemplateAttribute.dimension>>>>>>>))]
		protected virtual void _(Events.CacheAttached<Dimension.dimensionID> e) { }

		[PXOverride]
		public virtual IEnumerable GetSimpleDetails(Dimension dim)
		{
			var select = new PXSelect<Segment,
					Where<Segment.dimensionID, Equal<Required<Segment.dimensionID>>,
					And<Where<Segment.dimensionID, InFieldClassActivated,
					Or<Segment.dimensionID, Equal<PM.ProjectAttribute.dimension>,
					Or<Segment.dimensionID, Equal<PM.ProjectAttribute.dimensionTM>,
					Or<Segment.dimensionID, Equal<CT.ContractAttribute.dimension>,
					Or<Segment.dimensionID, Equal<CT.ContractTemplateAttribute.dimension>>>>>>>>>(Base);

			foreach (Segment item in select.Select(dim.DimensionID))
			{
				yield return item;
			}
		}

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>() || 
				PXAccess.FeatureInstalled<FeaturesSet.contractManagement>();
		}
	}
}
