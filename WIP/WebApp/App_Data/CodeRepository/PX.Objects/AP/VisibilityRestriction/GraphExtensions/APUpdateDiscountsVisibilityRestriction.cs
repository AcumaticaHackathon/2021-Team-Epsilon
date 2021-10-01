using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
    public class APUpdateDiscountsVisibilityRestriction : PXGraphExtension<APUpdateDiscounts>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        public override void Initialize()
        {
            base.Initialize();
            
            Base.NonPromotionSequences.WhereAnd<Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();
            Base.Sequences.WhereAnd<Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();
        }
    }
}