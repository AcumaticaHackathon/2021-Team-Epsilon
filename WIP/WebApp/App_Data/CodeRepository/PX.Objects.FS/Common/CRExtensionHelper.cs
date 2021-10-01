using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.IN;
using System;

namespace PX.Objects.FS
{
    public static class CRExtensionHelper
    {
        public static void LaunchEmployeeBoard(PXGraph graph, int? sOID)
        {
            if (sOID == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = GetServiceOrder(graph, sOID);

            if (fsServiceOrderRow != null)
            {
                ServiceOrderEntry graphServiceOrder = PXGraph.CreateInstance<ServiceOrderEntry>();

                graphServiceOrder.ServiceOrderRecords.Current = graphServiceOrder.ServiceOrderRecords
                                .Search<FSServiceOrder.refNbr>(fsServiceOrderRow.RefNbr, fsServiceOrderRow.SrvOrdType);

                graphServiceOrder.OpenEmployeeBoard();
            }
        }

        public static void LaunchServiceOrderScreen(PXGraph graph, int? sOID)
        {
            if (sOID == null)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = GetServiceOrder(graph, sOID);

            if (fsServiceOrderRow != null)
            {
                ServiceOrderEntry graphServiceOrder = PXGraph.CreateInstance<ServiceOrderEntry>();

                graphServiceOrder.ServiceOrderRecords.Current = graphServiceOrder.ServiceOrderRecords
                                .Search<FSServiceOrder.refNbr>(fsServiceOrderRow.RefNbr, fsServiceOrderRow.SrvOrdType);

                throw new PXRedirectRequiredException(graphServiceOrder, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow};
            }
        }

        public static FSServiceOrder GetServiceOrder(PXGraph graph, int? sOID)
        {
            return PXSelect<FSServiceOrder,
                   Where<
                       FSServiceOrder.sOID, Equal<Required<FSServiceOrder.sOID>>>>
                   .Select(graph, sOID);
        }

        public static FSSrvOrdType GetServiceOrderType(PXGraph graph, string srvOrdType)
        {
            if (string.IsNullOrEmpty(srvOrdType))
            {
                return null;
            }

            return PXSelect<FSSrvOrdType,
                   Where<
                       FSSrvOrdType.srvOrdType, Equal<Required<FSSrvOrdType.srvOrdType>>>>
                   .Select(graph, srvOrdType);
        }

        public static int? GetSalesPersonID(PXGraph graph, int? ownerID)
        {
            EPEmployee epeEmployeeRow = PXSelect<EPEmployee,
                                        Where<
                                            EPEmployee.defContactID, Equal<Required<EPEmployee.defContactID>>>>
                                        .Select(graph, ownerID);

            if (epeEmployeeRow != null)
            {
                return epeEmployeeRow.SalesPersonID;
            }

            return null;
        }

        public static FSServiceOrder InitNewServiceOrder(string srvOrdType, string sourceType)
        {
            FSServiceOrder fsServiceOrderRow = new FSServiceOrder();
            fsServiceOrderRow.SrvOrdType = srvOrdType;
            fsServiceOrderRow.SourceType = sourceType;

            return fsServiceOrderRow;
        }

        public static FSServiceOrder GetRelatedServiceOrder(PXGraph graph, PXCache chache, IBqlTable crTable, int? sOID)
        {
            FSServiceOrder fsServiceOrderRow = null;

            if (sOID != null && chache.GetStatus(crTable) != PXEntryStatus.Inserted)
            {
                fsServiceOrderRow = PXSelect<FSServiceOrder,
                                    Where<
                                        FSServiceOrder.sOID, Equal<Required<FSServiceOrder.sOID>>>>
                                    .Select(graph, sOID);
            }

            return fsServiceOrderRow;
        }
    }
}