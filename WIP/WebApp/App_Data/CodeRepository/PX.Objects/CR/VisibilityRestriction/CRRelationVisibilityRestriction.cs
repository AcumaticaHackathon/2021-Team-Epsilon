using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.CR
{
	public sealed class CRRelationVisibilityRestriction : PXCacheExtension<CRRelation>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region EntityID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search<
			BAccount.bAccountID,
			Where2<
				Where<BAccount.type, Equal<BAccountType.prospectType>,
					Or<BAccount.type, Equal<BAccountType.customerType>,
					Or<BAccount.type, Equal<BAccountType.combinedType>,
					Or2<
						Where<BAccount.type, Equal<BAccountType.vendorType>>, 
						And<BAccount.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>>>>,
				And<Match<Current<AccessInfo.userName>>>>>),
			new[]
			{
					typeof (BAccount.acctCD), typeof (BAccount.acctName), typeof (BAccount.classID), typeof(BAccount.type),
					typeof (BAccount.parentBAccountID), typeof (BAccount.acctReferenceNbr)
			},
			SubstituteKey = typeof(BAccount.acctCD),
			Filterable = true,
			DirtyRead = true)]
		public int? EntityID { get; set; }

		#endregion
	}
}