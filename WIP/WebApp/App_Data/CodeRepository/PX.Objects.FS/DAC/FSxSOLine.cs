using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.SO;
using System;

namespace PX.Objects.FS
{
    [PXTable(IsOptional = true)]
    public class FSxSOLine : PXCacheExtension<SOLine>, IFSRelatedDoc
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        #region SDPosted
        public abstract class sDPosted : PX.Data.BQL.BqlBool.Field<sDPosted> { }

        [PXDBBool]
        [PXDefault(false)]
        public virtual bool? SDPosted { get; set; }
        #endregion
        #region SDSelected
        public abstract class sDSelected : PX.Data.BQL.BqlBool.Field<sDSelected> { }

        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Require Appointment", Visible = false)]
        public virtual bool? SDSelected { get; set; }
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
        #region SMEquipmentID
        public abstract class sMEquipmentID : PX.Data.BQL.BqlInt.Field<sMEquipmentID> { }

        [PXDBInt]
        [PXUIField(DisplayName = "Target Equipment ID", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass)]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [FSSelectorExtensionMaintenanceEquipment(typeof(SOOrder.customerID))]
        public virtual int? SMEquipmentID { get; set; }
        #endregion
        #region NewEquipmentLineNbr
        public abstract class newEquipmentLineNbr : PX.Data.BQL.BqlInt.Field<newEquipmentLineNbr> { }

        [PXDBInt]
        [PXUIVisible(typeof(FeatureInstalled<FeaturesSet.inventory>))]
        [PXUIField(DisplayName = "Model Equipment Line Nbr.", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass)]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [FSSelectorNewTargetEquipmentSalesOrder]
        public virtual int? NewEquipmentLineNbr { get; set; }
        #endregion
        #region ComponentID
        public abstract class componentID : PX.Data.BQL.BqlInt.Field<componentID> { }

        [PXDBInt]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Component ID", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass)]
        [FSSelectorComponentIDSalesOrder]
        public virtual int? ComponentID { get; set; }
        #endregion
        #region EquipmentComponentLineNbr
        public abstract class equipmentComponentLineNbr : PX.Data.BQL.BqlInt.Field<equipmentComponentLineNbr> { }

        [PXDBInt]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Component Line Nbr.", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass)]
        [FSSelectorEquipmentLineRefSalesOrder]
        public virtual int? EquipmentComponentLineNbr { get; set; }
        #endregion
        #region EquipmentAction
        public abstract class equipmentAction : ListField_EquipmentAction
        {
        }

        [PXDBString(2, IsFixed = true)]
        [PXUIVisible(typeof(FeatureInstalled<FeaturesSet.inventory>))]
        [equipmentAction.ListAtrribute]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Equipment Action", Visible = false, FieldClass = FSSetup.EquipmentManagementFieldClass)]
        public virtual string EquipmentAction { get; set; }
        #endregion
        #region Comment
        public abstract class comment : PX.Data.BQL.BqlString.Field<comment> { }

        [PXDBString(int.MaxValue, IsUnicode = true)]
        [PXUIVisible(typeof(FeatureInstalled<FeaturesSet.inventory>))]
        [PXUIField(DisplayName = "Equipment Action Comment", FieldClass = FSSetup.EquipmentManagementFieldClass, Visible = false)]
        [SkipSetExtensionVisibleInvisible]
        public virtual string Comment { get; set; }
        #endregion
        #region EquipmentItemClass
        public abstract class equipmentItemClass : PX.Data.BQL.BqlString.Field<equipmentItemClass>
		{
        }

        [PXDBString(2, IsFixed = true)]
        public virtual string EquipmentItemClass { get; set; }
        #endregion
        #endregion
        
        #region RelatedDocument
        public abstract class relatedDocument : PX.Data.BQL.BqlString.Field<relatedDocument> { }

        [FSRelatedDocument(typeof(SOLine))]
        [PXUIField(DisplayName = "Related Svc. Doc. Nbr.", Enabled = false, FieldClass = FSSetup.ServiceManagementFieldClass)]
        public virtual string RelatedDocument { get; set; }
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
    }
}
