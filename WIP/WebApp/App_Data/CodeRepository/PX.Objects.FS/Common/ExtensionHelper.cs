using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;

namespace PX.Objects.FS
{
    public static class ExtensionHelper
    {
        public static CurrencyInfo SelectCurrencyInfo(PXSelectBase<CurrencyInfo> currencyInfoView, long? curyInfoID)
        {
            if (curyInfoID == null)
            {
                return null;
            }

            var result = (CurrencyInfo)currencyInfoView.Cache.Current;
            return result != null && curyInfoID == result.CuryInfoID ? result : currencyInfoView.SelectSingle();
        }
    }
}
