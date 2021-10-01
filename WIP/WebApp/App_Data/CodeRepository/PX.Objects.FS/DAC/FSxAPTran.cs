using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;
using System;

namespace PX.Objects.FS
{
    [PXTable(IsOptional = true)]
    public class FSxAPTran : PXCacheExtension<APTran>, IFSRelatedDoc
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        #region SrvOrdType
        public abstract class srvOrdType : PX.Data.BQL.BqlString.Field<srvOrdType> { }

        [PXDBString(4, IsFixed = true)]
        [PXUIField(DisplayName = "Service Order Type", FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual string SrvOrdType { get; set; }
        #endregion
        #region AppointmentRefNbr
        public abstract class appointmentRefNbr : PX.Data.BQL.BqlString.Field<appointmentRefNbr> { }

        [PXDBString(20, IsUnicode = true)]
        [PXUIField(DisplayName = "Appointment Nbr.", Visibility = PXUIVisibility.SelectorVisible, Visible = true, Enabled = true, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual string AppointmentRefNbr { get; set; }
        #endregion
        #region AppointmentLineNbr
        public abstract class appointmentLineNbr : PX.Data.BQL.BqlInt.Field<appointmentLineNbr> { }
        [PXDBInt]
        [PXUIField(DisplayName = "Appointment Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual Int32? AppointmentLineNbr { get; set; }
        #endregion
        #region ServiceOrderRefNbr
        public abstract class serviceOrderRefNbr : PX.Data.BQL.BqlString.Field<serviceOrderRefNbr> { }

        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Service Order Nbr.", Visibility = PXUIVisibility.SelectorVisible, Visible = true, Enabled = true, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual string ServiceOrderRefNbr { get; set; }
        #endregion
        #region ServiceOrderLineNbr
        public abstract class serviceOrderLineNbr : PX.Data.BQL.BqlInt.Field<serviceOrderLineNbr> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Service Order Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual Int32? ServiceOrderLineNbr { get; set; }
        #endregion

        #region RelatedEntityType
        public abstract class relatedEntityType : PX.Data.BQL.BqlString.Field<relatedEntityType> { }

        [PXDBString(40)]
        [PXDefault(typeof(CreateAPFilter.relatedEntityType), PersistingCheck = PXPersistingCheck.Nothing)]
        [PXStringList(new string[] { ID.FSEntityType.ServiceOrder, ID.FSEntityType.Appointment }, new string[] { TX.TableName.SERVICE_ORDER, TX.TableName.APPOINTMENT })]
        [PXUIField(DisplayName = "Related Svc. Doc. Type", Visible = false)]
        public virtual string RelatedEntityType { get; set; }
        #endregion
        #region RelatedDocNoteID
        public abstract class relatedDocNoteID : PX.Data.BQL.BqlGuid.Field<relatedDocNoteID> { }

        [FSEntityIDAPInvoiceSelector(typeof(relatedEntityType))]
        [PXDBGuid()]
        [PXDefault(typeof(CreateAPFilter.relatedDocNoteID), PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Related Svc. Doc. Nbr.", Visible = false)]
        [PXFormula(typeof(Default<FSxAPTran.relatedEntityType>))]
        public virtual Guid? RelatedDocNoteID { get; set; }
        #endregion

        #region Mem_PreviousPostID
        public abstract class mem_PreviousPostID : PX.Data.BQL.BqlInt.Field<mem_PreviousPostID> { }

        [PXInt]
        public virtual int? Mem_PreviousPostID { get; set; }
        #endregion
        #region Mem_TableSource
        public abstract class mem_TableSource : PX.Data.BQL.BqlString.Field<mem_TableSource> { }

        [PXString]
        public virtual string Mem_TableSource { get; set; }
        #endregion

        #region ServiceContractPeriodID
        public int? ServiceContractPeriodID
        {
            get
            {
                return null;
            }
        }
        #endregion
        #region ServiceContractRefNbr
        public string ServiceContractRefNbr
        {
            get 
            {
                return string.Empty;
            }
        }
        #endregion
        #region IsDocBilledOrClosed
        public abstract class isDocBilledOrClosed : PX.Data.BQL.BqlBool.Field<isDocBilledOrClosed> { }

        [PXBool]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual bool? IsDocBilledOrClosed { get; set; }
        #endregion
    }
}
