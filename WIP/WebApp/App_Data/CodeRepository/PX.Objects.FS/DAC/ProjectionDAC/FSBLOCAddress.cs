using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CR;
using PX.Objects.CS;
using System;

namespace PX.Objects.FS
{
    [Serializable]
    [PXBreakInheritance]
    [PXProjection(typeof(
        Select<FSAddress,
                Where<FSAddress.entityType, Equal<FSAddress.entityType.BranchLocation>>>))]
    public partial class FSBLOCAddress : FSAddress
    {
        #region Keys
        public new class PK : PrimaryKeyOf<FSBLOCAddress>.By<addressID>
        {
            public static FSBLOCAddress Find(PXGraph graph, Int32? addressID) => FindBy(graph, addressID);
        }
        public new static class FK
        {
            public class BusinessAccount : CR.BAccount.PK.ForeignKeyOf<FSBLOCAddress>.By<bAccountID> { }
            public class Country : CS.Country.PK.ForeignKeyOf<FSBLOCAddress>.By<countryID> { }
            public class State : CS.State.PK.ForeignKeyOf<FSBLOCAddress>.By<countryID, state> { }
        }
        #endregion

        #region addressID
        public new abstract class addressID : PX.Data.IBqlField
        {
        }
        #endregion

        #region EntityType
        public new abstract class entityType : ListField.ACEntityType
        {
        }

        [PXDBString(4, IsFixed = true)]
        [PXDefault(ID.ACEntityType.BRANCH_LOCATION)]
        [PXUIField(DisplayName = "Entity Type", Visible = false, Enabled = false)]
        public override string EntityType { get; set; }
        #endregion

        #region RevisionID
        public new abstract class revisionID : PX.Data.IBqlField
        {
        }
        #endregion

        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
        public new abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
        public new abstract class state : PX.Data.BQL.BqlString.Field<state> { }
    }
}