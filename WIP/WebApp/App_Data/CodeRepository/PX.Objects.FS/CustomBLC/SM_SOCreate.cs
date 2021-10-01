using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using static PX.Objects.SO.SOCreate;

namespace PX.Objects.FS
{
    public class SM_SOCreate : PXGraphExtension<SOCreate>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();

            Base.FixedDemand.Join<LeftJoin<FSServiceOrder,
                On<FSServiceOrder.noteID, Equal<SOFixedDemand.refNoteID>>>>();

            Base.FixedDemand.WhereAnd<Where<FSServiceOrder.refNbr, IsNull>>();
        }

        protected virtual IEnumerable fixedDemand()
        {
            PXView select = new PXView(Base, false, Base.FixedDemand.View.BqlSelect);

            Int32 totalrow = 0;
            Int32 startrow = PXView.StartRow;
            List<object> result = select.Select(PXView.Currents, PXView.Parameters, PXView.Searches,
                PXView.SortColumns, PXView.Descendings, PXView.Filters, ref startrow, PXView.MaximumRows, ref totalrow);
            PXView.StartRow = 0;

            return result;
        }
    }
}