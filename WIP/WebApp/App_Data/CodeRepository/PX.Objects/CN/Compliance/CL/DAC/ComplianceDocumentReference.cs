using System;
using PX.Data;
using PX.Objects.CN.Common.DAC;

namespace PX.Objects.CN.Compliance.CL.DAC
{
    [PXCacheName("Compliance Document Reference")]
    public class ComplianceDocumentReference : BaseCache, IBqlTable
    {
        #region ComplianceDocumentReferenceId
        public abstract class complianceDocumentReferenceId : PX.Data.BQL.BqlGuid.Field<complianceDocumentReferenceId>
        {
        }

        [PXDBGuid(IsKey = true)]
        public virtual Guid? ComplianceDocumentReferenceId
        {
            get;
            set;
        }
        #endregion

        #region Type
        public abstract class type : PX.Data.BQL.BqlString.Field<type>
        {
        }

        [PXDBString]
        public virtual string Type
        {
            get;
            set;
        }
        #endregion

        #region ReferenceNumber

        public abstract class referenceNumber : PX.Data.BQL.BqlString.Field<referenceNumber>
        {
        }

        [PXDBString]
        public virtual string ReferenceNumber
        {
            get;
            set;
        }
        #endregion

        #region RefNoteId
        public abstract class refNoteId : PX.Data.BQL.BqlGuid.Field<refNoteId>
        {
        }
        [PXDBGuid]
        public virtual Guid? RefNoteId
        {
            get;
            set;
        } 
        #endregion

        [PXDBCreatedByID(Visibility = PXUIVisibility.Invisible)]
        public override Guid? CreatedById
        {
            get;
            set;
        }
    }
}