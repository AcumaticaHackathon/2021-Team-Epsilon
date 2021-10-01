using PX.Data;
using PX.Objects.AP;
using PX.Objects.IN;
using System;

namespace PX.Objects.FS
{
    [System.SerializableAttribute]
    public class CreateAPFilter : IBqlTable
    {
        #region RelatedEntityType
        public abstract class relatedEntityType : PX.Data.BQL.BqlString.Field<relatedEntityType> { }

        [PXString(40)]
        [PXUIField(DisplayName = "Related Entity Type", Visible = false)]
        public virtual string RelatedEntityType { get; set; }
        #endregion
        #region RelatedDocNoteID
        public abstract class relatedDocNoteID : PX.Data.BQL.BqlGuid.Field<relatedDocNoteID> { }

        [PXGuid()]
        [PXUIField(DisplayName = "Related Doc. Nbr.", Visible = false)]
        public virtual Guid? RelatedDocNoteID { get; set; }
        #endregion
        #region RelatedDocDate
        public abstract class relatedDocDate : PX.Data.BQL.BqlDateTime.Field<relatedDocDate> { }

        [PXDate]
        [PXUIField(DisplayName = "Related Doc. Date", Visible = false, IsReadOnly = true)]
        public virtual DateTime? RelatedDocDate { get; set; }
        #endregion
        #region RelatedDocCustomerID
        public abstract class relatedDocCustomerID : PX.Data.BQL.BqlInt.Field<relatedDocCustomerID> { }

        [PXInt]
        [PXUIField(DisplayName = "Related Doc. Customer", Visible = false, IsReadOnly = true)]
        public virtual int? RelatedDocCustomerID { get; set; }
        #endregion
        #region RelatedDocCustomerLocationID
        public abstract class relatedDocCustomerLocationID : PX.Data.BQL.BqlInt.Field<relatedDocCustomerLocationID> { }

        [PXInt]
        [PXUIField(DisplayName = "Related Doc. Customer Location", Enabled = false, Visible = false)]
        public virtual int? RelatedDocCustomerLocationID { get; set; }
        #endregion
        #region RelatedDocProjectID
        public abstract class relatedDocProjectID : PX.Data.BQL.BqlInt.Field<relatedDocProjectID> { }

        [PXInt]
        [PXUIField(Enabled = false, Visible = false)]
        public virtual int? RelatedDocProjectID { get; set; }
        #endregion
        #region RelatedDocProjectTaskID
        public abstract class relatedDocProjectTaskID : PX.Data.BQL.BqlInt.Field<relatedDocProjectTaskID> { }

        [PXInt]
        [PXUIField(Enabled = false, Visible = false)]
        public virtual int? RelatedDocProjectTaskID { get; set; }
        #endregion
        #region RelatedDocCostCodeID
        public abstract class relatedDocCostCodeID : PX.Data.BQL.BqlInt.Field<relatedDocCostCodeID> { }

        [PXInt]
        [PXUIField(Enabled = false, Visible = false)]
        public virtual int? RelatedDocCostCodeID { get; set; }
        #endregion
    }
}
