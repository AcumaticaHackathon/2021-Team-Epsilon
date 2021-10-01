using System.Collections;
using System.Linq;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.AP;
using PX.Objects.CS;
using CRLocation = PX.Objects.CR.Standalone.Location;

namespace PX.Objects.EP
{
    public sealed class EmployeeMaintMultipleBaseCurrencies: PXGraphExtension<EmployeeMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
        }
        protected void _(Events.FieldUpdated<EPEmployee, EPEmployee.parentBAccountID> e)
        {
            e.Row.BaseCuryID = PXOrgAccess.GetBaseCuryID(e.Row.ParentBAccountID);
        }
        protected void _(Events.FieldUpdating<EPEmployee, EPEmployee.parentBAccountID> e)
        {
            if (SelectFrom<APHistory>
                    .Where<APHistory.vendorID.IsEqual<EPEmployee.bAccountID.FromCurrent>>
                    .View.SelectSingleBound(Base, new object[]{e.Row}, null).Any()
                && e.Row.BaseCuryID != PXOrgAccess.GetBaseCuryID(e.NewValue.ToString()))
            {
                throw new PXSetPropertyException(Messages.BranchCannotBeAssociated, PXErrorLevel.Error,
                    e.Row.BaseCuryID,
                    e.Row.AcctCD);
            }
        }
    }
}