using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Text;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.FS
{
    public class FSARTran : IBqlTable, IFSRelatedDoc
    {
        #region Keys
        public class PK : PrimaryKeyOf<FSARTran>.By<tranType, refNbr, lineNbr>
        {
            public static FSARTran Find(PXGraph graph, string tranType, string refNbr, int? lineNbr) => FindBy(graph, tranType, refNbr, lineNbr);

            public static FSARTran FindDirty(PXGraph graph, string tranType, string refNbr, int? lineNbr)
                => (FSARTran)PXSelect<FSARTran,
                        Where<tranType, Equal<Required<tranType>>,
                            And<refNbr, Equal<Required<refNbr>>,
                            And<lineNbr, Equal<Required<lineNbr>>>>>>.
                SelectWindowed(graph, 0, 1,
                        tranType, refNbr, lineNbr);
        }

        public static class FK
        {
            public class Appointment : FSAppointment.PK.ForeignKeyOf<FSARTran>.By<srvOrdType, appointmentRefNbr> { }

            public class ServiceOrder : FSAppointment.PK.ForeignKeyOf<FSARTran>.By<srvOrdType, serviceOrderRefNbr> { }

            public class ServiceOrderLine : FSSODet.PK.ForeignKeyOf<FSARTran>.By<srvOrdType, serviceOrderRefNbr, serviceOrderLineNbr> { }

            public class AppointmentLine : FSAppointmentDet.PK.ForeignKeyOf<FSARTran>.By<srvOrdType, appointmentRefNbr, appointmentLineNbr> { }

            public class ServiceContract : FSServiceContract.PK.ForeignKeyOf<FSARTran>.By<serviceContractRefNbr> { }

            public class ARTranLine : ARTran.PK.ForeignKeyOf<FSARTran>.By<tranType, refNbr, lineNbr> { }
        }
        #endregion

        #region TranType
        public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }
        protected String _TranType;
        [PXDBString(3, IsKey = true, IsFixed = true)]
        [PXUIField(DisplayName = "Tran. Type", Visibility = PXUIVisibility.Visible, Visible = false, Enabled = false)]
        public virtual String TranType { get; set; }
        #endregion
        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
        protected String _RefNbr;
        [PXDBString(15, IsUnicode = true, IsKey = true)]
        [PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.Visible, Visible = false, Enabled = false)]
        [PXDBDefault(typeof(ARInvoice.refNbr), DefaultForInsert = true, DefaultForUpdate = false)]
        public virtual String RefNbr { get; set; }
        #endregion
        #region LineNbr
        public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
        protected Int32? _LineNbr;
        [PXDBInt(IsKey = true)]
        [PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false)]
        public virtual Int32? LineNbr { get; set; }
        #endregion

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
        #region ServiceContractRefNbr
        public abstract class serviceContractRefNbr : PX.Data.BQL.BqlString.Field<serviceContractRefNbr> { }

        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Service Contract Nbr.", Visibility = PXUIVisibility.SelectorVisible, Visible = true, Enabled = true, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual string ServiceContractRefNbr { get; set; }
        #endregion
        #region ServiceContractPeriodID
        public abstract class serviceContractPeriodID : PX.Data.BQL.BqlInt.Field<serviceContractPeriodID> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Service Contract Period", Visibility = PXUIVisibility.SelectorVisible, Visible = true, Enabled = true, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual int? ServiceContractPeriodID { get; set; }
        #endregion

        #region Equipment Customization
        #region EquipmentAction
        public abstract class equipmentAction : ListField_EquipmentAction
        {
        }

        [PXDBString(2, IsFixed = true)]
        [equipmentAction.ListAtrribute]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Equipment Action", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass, Enabled = false)]
        public virtual string EquipmentAction { get; set; }
        #endregion
        #region ReplaceSMEquipmentID
        public abstract class replaceSMEquipmentID : PX.Data.BQL.BqlInt.Field<replaceSMEquipmentID> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Suspended Target Equipment ID", FieldClass = FSSetup.EquipmentManagementFieldClass, Enabled = false)]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXSelector(typeof(Search<FSEquipment.SMequipmentID>), SubstituteKey = typeof(FSEquipment.refNbr))]
        public virtual int? ReplaceSMEquipmentID { get; set; }
        #endregion
        #region SMEquipmentID
        public abstract class sMEquipmentID : PX.Data.BQL.BqlInt.Field<sMEquipmentID> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Target Equipment ID", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass, Enabled = false)]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [FSSelectorExtensionMaintenanceEquipment(typeof(ARTran.customerID))]
        public virtual int? SMEquipmentID { get; set; }
        #endregion
        #region NewEquipmentLineNbr
        public abstract class newEquipmentLineNbr : PX.Data.BQL.BqlInt.Field<newEquipmentLineNbr> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Model Equipment Line Nbr.", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass, Enabled = false)]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [FSSelectorNewTargetEquipmentSOInvoice(ValidateValue = false)]
        public virtual int? NewEquipmentLineNbr { get; set; }
        #endregion
        #region ComponentID
        public abstract class componentID : PX.Data.BQL.BqlInt.Field<componentID> { }

        [PXDBInt]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Component ID", FieldClass = FSSetup.EquipmentManagementFieldClass, Enabled = false)]
        [PXSelector(typeof(Search<FSModelTemplateComponent.componentID>), SubstituteKey = typeof(FSModelTemplateComponent.componentCD))]
        public virtual int? ComponentID { get; set; }
        #endregion
        #region EquipmentComponentLineNbr
        public abstract class equipmentComponentLineNbr : PX.Data.BQL.BqlInt.Field<equipmentComponentLineNbr> { }

        [PXDBInt]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Component Line Nbr.", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass, Enabled = false)]
        [FSSelectorEquipmentLineRefSOInvoice]
        public virtual int? EquipmentComponentLineNbr
        { get; set; }
        #endregion
        #region Comment
        public abstract class comment : PX.Data.BQL.BqlString.Field<comment> { }

        [PXDBString(int.MaxValue, IsUnicode = true)]
        [PXUIField(DisplayName = "Equipment Action Comment", FieldClass = FSSetup.EquipmentManagementFieldClass, Visible = false, Enabled = false)]
        [SkipSetExtensionVisibleInvisible]
        public virtual string Comment { get; set; }
        #endregion
        #endregion

        #region SOOrderType
        public abstract class sOOrderType : PX.Data.BQL.BqlString.Field<sOOrderType> { }

        [PXString(2, IsFixed = true)]
        [PXUIField(Visible = false, Enabled = false)]
        public virtual String SOOrderType { get; set; }
        #endregion
        #region SOOrderNbr
        public abstract class sOOrderNbr : PX.Data.BQL.BqlString.Field<sOOrderNbr> { }

        [PXString(15, IsUnicode = true)]
        [PXUIField(Visible = false, Enabled = false)]
        public virtual String SOOrderNbr { get; set; }
        #endregion

        #region RelatedDocument
        public abstract class relatedDocument : PX.Data.BQL.BqlString.Field<relatedDocument> { }

        [FSRelatedDocument(typeof(ARTran))]
        [PXUIField(DisplayName = "Related Svc. Doc. Nbr.", Enabled = false, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual string RelatedDocument { get; set; }
        #endregion

        #region IsFSRelated
        public virtual bool IsFSRelated 
        {
            get
            {
                return string.IsNullOrEmpty(AppointmentRefNbr) == false
                    || string.IsNullOrEmpty(ServiceOrderRefNbr) == false
                    || string.IsNullOrEmpty(ServiceContractRefNbr) == false;
            }
        }
        #endregion

    }
}