using PX.Data;
using PX.Objects.CS;
using PX.Objects.CT;

namespace PX.Objects.CN.CacheExtensions
{
    public sealed class ContractExt : PXCacheExtension<Contract>
    {
        [PXDBBool]
        [PXUIField(DisplayName = "Allow Adding New Items on the Fly")]
        public bool? AllowNonProjectAccountGroups
        {
            get;
            set;
        }

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.construction>();
        }

        public abstract class allowNonProjectAccountGroups : IBqlField
        {
        }
    }
}