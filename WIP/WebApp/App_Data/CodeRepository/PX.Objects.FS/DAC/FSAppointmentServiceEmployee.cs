using PX.Data;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.Common.Discount;
using PX.Objects.Common.Discount.Attributes;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.PO;
using PX.Objects.SO;
using PX.Objects.TX;
using System;
using System;
using CRLocation = PX.Objects.CR.Standalone.Location;

namespace PX.Objects.FS
{
    [Serializable]
    [PXBreakInheritance]
    [PXProjection(typeof(Select<FSAppointmentDet>), Persistent = false)]
    public class FSAppointmentServiceEmployee : FSAppointmentDet
    {
        #region Keys
        public new class PK : PrimaryKeyOf<FSAppointmentServiceEmployee>.By<srvOrdType, refNbr, lineNbr>
        {
            public static FSAppointmentServiceEmployee Find(PXGraph graph, string srvOrdType, string refNbr, int? lineNbr) => FindBy(graph, srvOrdType, refNbr, lineNbr);
        }
        #endregion

        public new abstract class appointmentID : PX.Data.BQL.BqlInt.Field<appointmentID> { }

        public new abstract class sODetID : PX.Data.BQL.BqlInt.Field<sODetID> { }

        public new abstract class lineType : PX.Data.BQL.BqlString.Field<lineType> { }

        #region LineRef
        public new abstract class lineRef : PX.Data.BQL.BqlString.Field<lineRef> { }

        [PXDBString(4, IsFixed = true)]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        public override string LineRef { get; set; }
        #endregion

        #region InventoryID
        public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
        
        #endregion
    }
}