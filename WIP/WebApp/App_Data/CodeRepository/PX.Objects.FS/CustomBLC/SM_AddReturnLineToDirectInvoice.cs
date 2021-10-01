using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.SO;
using PX.Objects.SO.DAC.Projections;
using PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt;
using System;

namespace PX.Objects.FS
{
    public class SM_AddReturnLineToDirectInvoice : PXGraphExtension<AddReturnLineToDirectInvoice, SOInvoiceEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();

            Base1.arTranList.Join<LeftJoin<FSARTran,
                                  On<
                                      FSARTran.tranType, Equal<ARTranForDirectInvoice.tranType>,
                                      And<FSARTran.refNbr, Equal<ARTranForDirectInvoice.refNbr>,
                                      And<FSARTran.lineNbr, Equal<ARTranForDirectInvoice.lineNbr>>>>>>();
        }
    }
}
