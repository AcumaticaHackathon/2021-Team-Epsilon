using PX.Data;
using PX.Objects.CR;
using System;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.FS
{
    [Serializable]
    [PXCacheName(TX.TableName.FSBillHistory)]
    public class FSBillHistory : PX.Data.IBqlTable, IFSRelatedDoc
    {
        #region Keys
        public class PK : PrimaryKeyOf<FSBillHistory>.By<recordID>
        {
            public static FSBillHistory Find(PXGraph graph, int? recordID) => FindBy(graph, recordID);
        }

        public class UK : PrimaryKeyOf<FSBillHistory>.
                            By<srvOrdType, serviceOrderRefNbr, appointmentRefNbr,
                                parentEntityType, parentDocType, parentRefNbr,
                                childEntityType, childDocType, childRefNbr>
        {
            public static FSBillHistory FindDirty(PXGraph graph,
                            string srvOrdType, string serviceOrderRefNbr, string appointmentRefNbr,
                            string parentEntityType, string parentDocType, string parentRefNbr,
                            string childEntityType, string childDocType, string childRefNbr)
            => (FSBillHistory)PXSelect<FSBillHistory,
                    Where2<
                        Where<srvOrdType, Equal<Required<srvOrdType>>, Or<Where<Required<srvOrdType>, IsNull, And<srvOrdType, IsNull>>>>,
                        And2<Where<serviceOrderRefNbr, Equal<Required<serviceOrderRefNbr>>, Or<Where<Required<serviceOrderRefNbr>, IsNull, And<serviceOrderRefNbr, IsNull>>>>,
                        And2<Where<appointmentRefNbr, Equal<Required<appointmentRefNbr>>, Or<Where<Required<appointmentRefNbr>, IsNull, And<appointmentRefNbr, IsNull>>>>,
                        And2<Where<parentEntityType, Equal<Required<parentEntityType>>, Or<Where<Required<parentEntityType>, IsNull, And<parentEntityType, IsNull>>>>,
                        And2<Where<parentDocType, Equal<Required<parentDocType>>, Or<Where<Required<parentDocType>, IsNull, And<parentDocType, IsNull>>>>,
                        And2<Where<parentRefNbr, Equal<Required<parentRefNbr>>, Or<Where<Required<parentRefNbr>, IsNull, And<parentRefNbr, IsNull>>>>,
                        And2<Where<childEntityType, Equal<Required<childEntityType>>, Or<Where<Required<childEntityType>, IsNull, And<childEntityType, IsNull>>>>,
                        And2<Where<childDocType, Equal<Required<childDocType>>, Or<Where<Required<childDocType>, IsNull, And<childDocType, IsNull>>>>,
                        And<Where<childRefNbr, Equal<Required<childRefNbr>>, Or<Where<Required<childRefNbr>, IsNull, And<childRefNbr, IsNull>>>>>>>>>>>>>>.
                SelectWindowed(graph, 0, 1,
                        srvOrdType, srvOrdType,
                        serviceOrderRefNbr, serviceOrderRefNbr,
                        appointmentRefNbr, appointmentRefNbr,
                        parentEntityType, parentEntityType,
                        parentDocType, parentDocType,
                        parentRefNbr, parentRefNbr,
                        childEntityType, childEntityType,
                        childDocType, childDocType,
                        childRefNbr, childRefNbr);
        }

        public static class FK
        {
            public class Appointment : FSAppointment.PK.ForeignKeyOf<FSARTran>.By<srvOrdType, appointmentRefNbr> { }

            public class ServiceOrder : FSAppointment.PK.ForeignKeyOf<FSARTran>.By<srvOrdType, serviceOrderRefNbr> { }

            public class ServiceContract : FSServiceContract.PK.ForeignKeyOf<FSARTran>.By<serviceContractRefNbr> { }
        }
        #endregion

        #region RecordID
        public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }

        [PXDBIdentity(IsKey = true)]
        public virtual int? RecordID { get; set; }
        #endregion
        #region BatchID
        public abstract class batchID : PX.Data.BQL.BqlInt.Field<batchID> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Batch Nbr.", Enabled = false, Visible = false)]
        [FSPostBatch]
        public virtual int? BatchID { get; set; }
        #endregion
        #region SrvOrdType
        public abstract class srvOrdType : PX.Data.BQL.BqlString.Field<srvOrdType> { }

        [PXDBString(4, IsFixed = true, InputMask = ">AAAA")]
        [PXUIField(DisplayName = "Service Order Type", Enabled = false)]
        [FSSelectorSrvOrdType]
        public virtual string SrvOrdType { get; set; }
        #endregion
        #region ServiceOrderRefNbr
        public abstract class serviceOrderRefNbr : PX.Data.BQL.BqlString.Field<serviceOrderRefNbr> { }

        [PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
        [PXUIField(DisplayName = "Service Order Nbr.", Enabled = false)]
        [PXSelector(typeof(Search<FSServiceOrder.refNbr,
                                Where<FSServiceOrder.srvOrdType, Equal<Current<srvOrdType>>>>))]
        public virtual string ServiceOrderRefNbr { get; set; }
        #endregion
        #region AppointmentRefNbr
        public abstract class appointmentRefNbr : PX.Data.BQL.BqlString.Field<appointmentRefNbr> { }

        [PXDBString(20, IsUnicode = true, InputMask = "CCCCCCCCCCCCCCCCCCCC")]
        [PXUIField(DisplayName = "Appointment Nbr.", Enabled = false)]
        [PXSelector(typeof(Search<FSAppointment.refNbr,
                                Where<FSAppointment.srvOrdType, Equal<Current<srvOrdType>>>>))]
        public virtual string AppointmentRefNbr { get; set; }
        #endregion
        #region ServiceContractRefNbr
        public abstract class serviceContractRefNbr : PX.Data.BQL.BqlString.Field<serviceContractRefNbr> { }

        [PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
        [PXUIField(DisplayName = "Service Contract Nbr.", Enabled = false)]
        [PXSelector(typeof(Search<FSServiceContract.refNbr>))]
        public virtual string ServiceContractRefNbr { get; set; }
        #endregion
        #region ParentEntityType
        public abstract class parentEntityType : PX.Data.BQL.BqlString.Field<parentEntityType> { }

        [PXDBString(4)]
        [FSEntityType.List]
        [PXUIField(DisplayName = "Origin Entity Type", Enabled = false, Visible = false)]
        public virtual string ParentEntityType { get; set; }
        #endregion
        #region ParentDocType
        public abstract class parentDocType : PX.Data.BQL.BqlString.Field<parentDocType> { }

        [PXDBString(4, IsFixed = true, InputMask = ">AAAA")]
        [PXUIField(DisplayName = "Origin Doc Type", Enabled = false)]
        public virtual string ParentDocType { get; set; }
        #endregion
        #region ParentRefNbr
        public abstract class parentRefNbr : PX.Data.BQL.BqlString.Field<parentRefNbr> { }

        [PXDBString(20, IsUnicode = true, InputMask = "CCCCCCCCCCCCCCCCCCCC")]
        [PXUIField(DisplayName = "Origin Doc Nbr.", Enabled = false)]
        public virtual string ParentRefNbr { get; set; }
        #endregion
        #region ChildEntityType
        public abstract class childEntityType : PX.Data.BQL.BqlString.Field<childEntityType>
        {
            public abstract class Values : FSEntityType { }
        }

        [PXDBString(4)]
        [FSEntityType.List]
        [PXUIField(DisplayName = "Doc. Type", Enabled = false)]
        public virtual string ChildEntityType { get; set; }
        #endregion
        #region ChildDocType
        public abstract class childDocType : PX.Data.BQL.BqlString.Field<childDocType> { }

        [PXDBString(4, IsFixed = true, InputMask = ">AAAA")]
        [PXUIField(DisplayName = "Created Doc. Type", Enabled = false)]
        public virtual string ChildDocType { get; set; }
        #endregion
        #region ChildRefNbr
        public abstract class childRefNbr : PX.Data.BQL.BqlString.Field<childRefNbr> { }

        [PXDBString(20, IsUnicode = true, InputMask = "CCCCCCCCCCCCCCCCCCCC")]
        [PXUIField(DisplayName = "Created Doc. Nbr.", Enabled = false)]
        public virtual string ChildRefNbr { get; set; }
        #endregion

        #region ChildDocLink
        public abstract class childDocLink : PX.Data.BQL.BqlString.Field<childDocLink> { }

        [FSRelatedDocument(typeof(FSBillHistory), typeof(childEntityType), typeof(childDocType), typeof(childRefNbr))]
        [PXUIField(DisplayName = "Reference Nbr.", Enabled = false)]
        public virtual string ChildDocLink { get; set; }
        #endregion
        #region ParentDocLink
        public abstract class parentDocLink : PX.Data.BQL.BqlString.Field<parentDocLink> { }

        [FSRelatedDocument(typeof(FSBillHistory), typeof(parentEntityType), typeof(parentDocType), typeof(parentRefNbr))]
        [PXUIField(DisplayName = "Origin Doc. Ref. Nbr.", Enabled = false)]
        public virtual string ParentDocLink { get; set; }
        #endregion
        
        #region ChildDocDesc
        public abstract class childDocDesc : PX.Data.BQL.BqlString.Field<childDocDesc> { }

        [PXString(Common.Constants.TranDescLength, IsUnicode = true)]
        [PXUIField(DisplayName = "Description", Enabled = false)]
        public virtual string ChildDocDesc { get; set; }
        #endregion
        #region ChildDocDate
        public abstract class childDocDate : PX.Data.BQL.BqlDateTime.Field<childDocDate> { }

        [PXDate]
        [PXUIField(DisplayName = "Date", Enabled = false)]
        public virtual DateTime? ChildDocDate { get; set; }
        #endregion
        #region ChildDocStatus
        public abstract class childDocStatus : PX.Data.BQL.BqlString.Field<childDocStatus> { }

        [PXString(30, IsUnicode = true)]
        [PXUIField(DisplayName = "Status", Enabled = false)]
        public virtual string ChildDocStatus { get; set; }
        #endregion
        #region ChildAmount
        public abstract class childAmount : PX.Data.BQL.BqlDecimal.Field<childAmount> { }

        [PXDecimal(2)]
        [PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Amount")]
        public virtual Decimal? ChildAmount { get; set; }
        #endregion

        #region ServiceContractPeriodID 
        public abstract class serviceContractPeriodID : PX.Data.BQL.BqlInt.Field<serviceContractPeriodID> { }

        [PXInt]
        [PXSelector(typeof(Search<FSContractPeriod.contractPeriodID>), SubstituteKey = typeof(FSContractPeriod.billingPeriod))]
        [PXUIField(DisplayName = "Service Contract Billing Period", Enabled = false)]
        public virtual int? ServiceContractPeriodID { get; set; }
        #endregion
        #region ContractPeriodStatus
        public abstract class contractPeriodStatus : PX.Data.BQL.BqlString.Field<contractPeriodStatus>
        {
            public abstract class Values : ListField_Status_ContractPeriod { }
        }

        [PXString(1, IsUnicode = true)]
        [contractPeriodStatus.Values.List]
        [PXUIField(DisplayName = "Contract Period Status", Enabled = false)]
        public virtual string ContractPeriodStatus { get; set; }
        #endregion

        #region CreatedByID
        public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
        [PXDBCreatedByID]
        [PXUIField(DisplayName = "Created By")]
        public virtual Guid? CreatedByID { get; set; }
        #endregion
        #region CreatedByScreenID
        public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
        [PXDBCreatedByScreenID]
        [PXUIField(DisplayName = "CreatedByScreenID")]
        public virtual string CreatedByScreenID { get; set; }
        #endregion
        #region CreatedDateTime
        public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
        [PXDBCreatedDateTime]
        [PXUIField(DisplayName = "Created On")]
        public virtual DateTime? CreatedDateTime { get; set; }
        #endregion
        #region LastModifiedByID
        public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
        [PXDBLastModifiedByID]
        [PXUIField(DisplayName = "Last Modified By")]
        public virtual Guid? LastModifiedByID { get; set; }
        #endregion
        #region LastModifiedByScreenID
        public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
        [PXDBLastModifiedByScreenID]
        [PXUIField(DisplayName = "LastModifiedByScreenID")]
        public virtual string LastModifiedByScreenID { get; set; }
        #endregion
        #region LastModifiedDateTime
        public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
        [PXDBLastModifiedDateTime]
        [PXUIField(DisplayName = "Last Modified On")]
        public virtual DateTime? LastModifiedDateTime { get; set; }
        #endregion
        #region tstamp
        public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
        [PXDBTimestamp]
        [PXUIField(DisplayName = "tstamp")]
        public virtual byte[] tstamp { get; set; }
        #endregion

        #region RelatedDocumentType
        public abstract class relatedDocumentType : PX.Data.BQL.BqlString.Field<relatedDocumentType>
        {
            public abstract class Values : FSEntityType { }
        }
        [PXString()]
        [FSEntityType.List]
        [PXFormula(typeof(Switch<Case<Where<appointmentRefNbr, IsNotNull>, relatedDocumentType.Values.appointment,
                                 Case<Where<serviceOrderRefNbr, IsNotNull>, relatedDocumentType.Values.serviceOrder,
                                 Case<Where<serviceContractRefNbr, IsNotNull>, relatedDocumentType.Values.serviceContract>>>,
                                 Null>))]
        [PXUIField(DisplayName = "Svc. Doc. Type", Enabled = false, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual string RelatedDocumentType { get; set; }
        #endregion
        #region RelatedDocument
        public abstract class relatedDocument : PX.Data.BQL.BqlString.Field<relatedDocument> { }

        [FSRelatedDocument(typeof(FSBillHistory))]
        [PXUIField(DisplayName = "Svc. Ref. Nbr.", Enabled = false, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual string RelatedDocument { get; set; }
        #endregion

        #region ServiceOrderLineNbr
        public Int32? ServiceOrderLineNbr
        {
            get
            {
                return null;
            }
        }
        #endregion
        #region AppointmentLineNbr
        public Int32? AppointmentLineNbr
        {
            get
            {
                return null;
            }
        }
        #endregion
    }
}
