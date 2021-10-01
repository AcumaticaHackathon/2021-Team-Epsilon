using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.CM
{
    public class BaseCurrencySelectorAttribute : PXSelectorAttribute
    {
        public BaseCurrencySelectorAttribute()
            : base(typeof(Search<Currency.curyID,
                Where<Exists<Select<Branch, Where<Branch.baseCuryID, Equal<Currency.curyID>>>>>>))
        {
        }

        public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
        {
            base.FieldSelecting(sender, e);
            if (e.ReturnState is PXFieldState state)
            {
                state.Visible = PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
            }
        }
    }
}