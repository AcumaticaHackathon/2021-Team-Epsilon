using PX.CS.Contracts.Interfaces;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;
using PX.Objects.CR;
using PX.Objects.IN;
using System.Linq;
using PX.Objects.AP;

namespace PX.Objects.FS
{
    public class SM_APInvoiceEntryExternalTax : PXGraphExtension<APInvoiceEntryExternalTax, APInvoiceEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public delegate IAddressBase GetFromAddressLineDelegate(APInvoice invoice, APTran tran);
        public delegate IAddressBase GetToAddressLineDelegate(APInvoice invoice, APTran tran);

        [PXOverride]
        public virtual IAddressBase GetFromAddress(APInvoice invoice, APTran tran, GetFromAddressLineDelegate del)
        {
            string srvOrderType;
            string refNbr;
            GetServiceOrderKeys(tran, out srvOrderType, out refNbr);

            if (string.IsNullOrEmpty(refNbr) == false)
            {
                IAddressBase returnAddress = null;

                returnAddress = PXSelectJoin<FSAddress,
                                InnerJoin<FSServiceOrder,
                                    On<FSServiceOrder.serviceOrderAddressID, Equal<FSAddress.addressID>>>,
                                Where<
                                    FSServiceOrder.srvOrdType, Equal<Required<FSServiceOrder.srvOrdType>>,
                                    And<FSServiceOrder.refNbr, Equal<Required<FSServiceOrder.refNbr>>>>>
                                    .Select(Base, srvOrderType, refNbr)
                                    .RowCast<FSAddress>()
                                    .FirstOrDefault();

                return returnAddress;
            }

            return del(invoice, tran);
        }

        [PXOverride]
        public virtual IAddressBase GetToAddress(APInvoice invoice, APTran tran, GetToAddressLineDelegate del)
        {
            string srvOrderType;
            string refNbr;
            GetServiceOrderKeys(tran, out srvOrderType, out refNbr);

            if (string.IsNullOrEmpty(refNbr) == false)
            {
                IAddressBase returnAddress = null;

                returnAddress = PXSelectJoin<FSAddress,
                               InnerJoin<
                                   FSBranchLocation,
                                   On<FSBranchLocation.branchLocationAddressID, Equal<FSAddress.addressID>>,
                               InnerJoin<FSServiceOrder,
                                   On<FSServiceOrder.branchLocationID, Equal<FSBranchLocation.branchLocationID>>>>,
                               Where<
                                   FSServiceOrder.srvOrdType, Equal<Required<FSServiceOrder.srvOrdType>>,
                                    And<FSServiceOrder.refNbr, Equal<Required<FSServiceOrder.refNbr>>>>>
                                   .Select(Base, srvOrderType, refNbr)
                                   .RowCast<FSAddress>()
                                   .FirstOrDefault();

                return returnAddress;
            }
            
            return del(invoice, tran);
        }

        protected void GetServiceOrderKeys(APTran line, out string srvOrderType, out string refNbr)
        {
            FSxAPTran row = PXCache<APTran>.GetExtension<FSxAPTran>(line);
            srvOrderType = row?.SrvOrdType;
            refNbr = row?.ServiceOrderRefNbr;
        }
    }
}
