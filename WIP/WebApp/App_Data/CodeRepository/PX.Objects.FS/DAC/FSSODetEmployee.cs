using PX.Data;
using PX.Objects.IN;
using System;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.FS
{
    [Serializable]
    [PXPrimaryGraph(typeof(ServiceOrderEntry))]
    [PXBreakInheritance]
    [PXProjection(typeof(Select<FSSODet>), Persistent = false)]
    public class FSSODetEmployee : FSSODet
    {
        #region Keys
        public new class PK : PrimaryKeyOf<FSSODetEmployee>.By<srvOrdType, refNbr, lineNbr>
        {
            public static FSSODetEmployee Find(PXGraph graph, string srvOrdType, string refNbr, int? lineNbr) => FindBy(graph, srvOrdType, refNbr, lineNbr);
        }
        #endregion

        public new abstract class sOID : PX.Data.BQL.BqlInt.Field<sOID> { }

        public new abstract class sODetID : PX.Data.BQL.BqlInt.Field<sODetID> { }

        public new abstract class lineRef : PX.Data.BQL.BqlString.Field<lineRef> { }
    }
}