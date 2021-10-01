using PX.Data;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PX.Objects.FS
{
    public static class TreeWFStageHelper
    {
        public class TreeWFStageView : PXSelectOrderBy<FSWFStage, OrderBy<Asc<FSWFStage.sortOrder>>>
        {
            public TreeWFStageView(PXGraph graph) : base(graph)
            {
            }

            public TreeWFStageView(PXGraph graph, Delegate handler) : base(graph, handler)
            {
            }
        }

        public static IEnumerable treeWFStages(PXGraph graph, string srvOrdType, int? wFStageID)
        {
            if (wFStageID == null)
            {
                wFStageID = 0;
            }

            PXResultset<FSWFStage> fsWFStageSet = PXSelectJoin<FSWFStage,
                                                  InnerJoin<FSSrvOrdType,
                                                  On<
                                                      FSSrvOrdType.srvOrdTypeID, Equal<FSWFStage.wFID>>>,
                                                  Where<
                                                      FSSrvOrdType.srvOrdType, Equal<Required<FSSrvOrdType.srvOrdType>>,
                                                      And<FSWFStage.parentWFStageID, Equal<Required<FSWFStage.parentWFStageID>>>>,
                                                  OrderBy<
                                                      Asc<FSWFStage.sortOrder>>>
                                                  .Select(graph, srvOrdType, wFStageID);

            foreach (FSWFStage fsWFStageRow in fsWFStageSet)
            {
                yield return fsWFStageRow;
            }
        }
    }
}
