using PX.CS.Contracts.Interfaces;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;
using PX.Objects.CR;
using PX.Objects.IN;
using System.Linq;

namespace PX.Objects.FS
{
    public class SM_SOOrderEntryExternalTax : PXGraphExtension<SOOrderEntryExternalTax, SOOrderEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public delegate IAddressBase GetFromAddressLineDelegate(SOOrder order, SOLine line);
        public delegate IAddressBase GetToAddressLineDelegate(SOOrder order, SOLine line);

        [PXOverride]
        public virtual IAddressBase GetFromAddress(SOOrder order, SOLine line, GetFromAddressLineDelegate del)
        {
            string srvOrderType;
            string refNbr;
            GetServiceOrderKeys(line, out srvOrderType, out refNbr);

            if (string.IsNullOrEmpty(refNbr) == false && line.SiteID == null)
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

            return del(order, line);
        }

        [PXOverride]
        public virtual IAddressBase GetToAddress(SOOrder order, SOLine line, GetToAddressLineDelegate del)
        {
            string srvOrderType;
            string refNbr;
            GetServiceOrderKeys(line, out srvOrderType, out refNbr);

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
            
            return del(order, line);
        }

        protected void GetServiceOrderKeys(SOLine line, out string srvOrderType, out string refNbr)
        {
            FSxSOLine row = PXCache<SOLine>.GetExtension<FSxSOLine>(line);
            srvOrderType = row?.SrvOrdType;
            refNbr = row?.ServiceOrderRefNbr;
        }
    }
}
