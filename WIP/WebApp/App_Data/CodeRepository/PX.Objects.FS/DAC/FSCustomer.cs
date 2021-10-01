using PX.Data;
using PX.Objects.AR;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.FS
{
    [System.SerializableAttribute]
    [PXPrimaryGraph(typeof(CustomerMaintBridge))]
    // TODO: AC-137974 Delete this DAC
    public partial class FSCustomer : Customer
    {
        #region Keys
        public new class PK : PrimaryKeyOf<FSCustomer>.By<acctCD>
        {
            public static FSCustomer Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
        }
        #endregion

        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }

        public new abstract class cOrgBAccountID : PX.Data.BQL.BqlInt.Field<cOrgBAccountID> { }
    }
}
