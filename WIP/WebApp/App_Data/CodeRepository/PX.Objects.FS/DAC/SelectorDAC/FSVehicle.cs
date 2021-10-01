using System;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.IN;

namespace PX.Objects.FS
{
    [System.Serializable]
    [PXPrimaryGraph(typeof(VehicleMaintBridge))]
    public class FSVehicle : FSEquipment
    {
        #region Keys
        public new class PK : PrimaryKeyOf<FSVehicle>.By<SMequipmentID>
        {
            public static FSVehicle Find(PXGraph graph, int? SMequipmentID) => FindBy(graph, SMequipmentID);
        }

        public new static class FK
        {
            public class Manufacturer : FSManufacturer.PK.ForeignKeyOf<FSVehicle>.By<manufacturerID> { }
            public class ManufacturerModel : FSManufacturerModel.PK.ForeignKeyOf<FSVehicle>.By<manufacturerID, manufacturerModelID> { }
            public class EquipmentType : FSEquipmentType.PK.ForeignKeyOf<FSVehicle>.By<equipmentTypeID> { }
            public class Vendor : AP.Vendor.PK.ForeignKeyOf<FSVehicle>.By<vendorID> { }
            public class Customer : AR.Customer.PK.ForeignKeyOf<FSVehicle>.By<customerID> { }
            public class CustomerLocation : CR.Location.PK.ForeignKeyOf<FSVehicle>.By<customerID, customerLocationID> { }
            public class VehicleType : FSVehicleType.PK.ForeignKeyOf<FSVehicle>.By<vehicleTypeID> { }
            public class Site : INSite.PK.ForeignKeyOf<FSVehicle>.By<siteID> { }
            public class Location : INLocation.PK.ForeignKeyOf<FSVehicle>.By<locationID> { }
            public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<FSVehicle>.By<inventoryID> { }
            public class Owner : AR.Customer.PK.ForeignKeyOf<FSVehicle>.By<ownerID> { }
            public class SubItem : INSubItem.PK.ForeignKeyOf<FSVehicle>.By<subItemID> { }
            public class Branch : GL.Branch.PK.ForeignKeyOf<FSVehicle>.By<branchID> { }
            public class BranchLocation : FSBranchLocation.PK.ForeignKeyOf<FSVehicle>.By<branchLocationID> { }
            public class InstallationServiceOrder : FSServiceOrder.UK.ForeignKeyOf<FSVehicle>.By<instServiceOrderID> { }
            public class InstallationAppointment : FSAppointment.UK.ForeignKeyOf<FSVehicle>.By<instAppointmentID> { }
            public class ReplacedEquipment : FSEquipment.PK.ForeignKeyOf<FSVehicle>.By<replaceEquipmentID> { }
            public class DisposalServiceOrder : FSServiceOrder.UK.ForeignKeyOf<FSVehicle>.By<dispServiceOrderID> { }
            public class DisposalAppointment : FSAppointment.UK.ForeignKeyOf<FSVehicle>.By<dispAppointmentID> { }
        }


        public new class UK : PrimaryKeyOf<FSVehicle>.By<refNbr>
        {
            public static FSVehicle Find(PXGraph graph, string refNbr) => FindBy(graph, refNbr);
        }
        #endregion

        #region SMEquipmentID
        public new abstract class SMequipmentID : PX.Data.BQL.BqlInt.Field<SMequipmentID> { }
        #endregion

        #region RefNbr
        [PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCCCCCCC")]
        [PXUIField(DisplayName = "Vehicle ID", Visibility = PXUIVisibility.SelectorVisible)]
        [PXSelector(typeof(Search<FSEquipment.refNbr, Where<FSEquipment.isVehicle, Equal<True>>>),
                    new Type[]
                    {
                        typeof(FSEquipment.refNbr), 
                        typeof(FSEquipment.status),
                        typeof(FSEquipment.descr),
                        typeof(FSEquipment.registrationNbr),
                        typeof(FSEquipment.manufacturerModelID),
                        typeof(FSEquipment.manufacturerID),
                        typeof(FSEquipment.manufacturingYear),
                        typeof(FSEquipment.color)
                    },
                    DescriptionField = typeof(FSEquipment.descr))]
        [AutoNumber(typeof(Search<FSSetup.equipmentNumberingID>), typeof(AccessInfo.businessDate))]
        public override string RefNbr { get; set; }
        #endregion

        #region SerialNumber
        [PXDBString(60, IsUnicode = true)]
        [PXUIField(DisplayName = "VIN")]
        public override string SerialNumber { get; set; }
        #endregion

        #region RegistrationNbr
        [PXDBString(30, IsUnicode = true)]
        [PXUIField(DisplayName = "License Nbr.")]
        public override string RegistrationNbr { get; set; }
        #endregion
        #region Attributes
        /// <summary>
        /// A service field, which is necessary for the <see cref="CSAnswers">dynamically 
        /// added attributes</see> defined at the <see cref="FSVehicleType">Vehicle
        /// screen</see> level to function correctly.
        /// </summary>
        [CRAttributesField(typeof(FSVehicle.vehicleTypeCD), typeof(FSVehicle.noteID))]
        public override string[] Attributes { get; set; }
        #endregion

        #region SourceID
        public new abstract class sourceID : PX.Data.BQL.BqlInt.Field<sourceID> { }
        #endregion

        #region SourceType
        public new abstract class sourceType : ListField_SourceType_Equipment
        {
        }
        #endregion

        #region Memory Helper
        #region VehicleTypeCD
        // Needed for attributes
        public abstract class vehicleTypeCD : PX.Data.BQL.BqlString.Field<vehicleTypeCD> { }

        [PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC", IsFixed = true)]
        [PXUIField(Visible = false)]
        [PXDBScalar(typeof(Search<FSVehicleType.vehicleTypeCD, Where<FSVehicleType.vehicleTypeID, Equal<FSVehicle.vehicleTypeID>>>))]
        [PXDefault(typeof(Search<FSVehicleType.vehicleTypeCD, Where<FSVehicleType.vehicleTypeID, Equal<Current<FSVehicle.vehicleTypeID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual string VehicleTypeCD { get; set; }
        #endregion
        #endregion

        public new abstract class manufacturerID : PX.Data.BQL.BqlInt.Field<manufacturerID> { }
        public new abstract class manufacturerModelID : PX.Data.BQL.BqlInt.Field<manufacturerModelID> { }
        public new abstract class equipmentTypeID : PX.Data.BQL.BqlInt.Field<equipmentTypeID> { }
        public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
        public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
        public new abstract class customerLocationID : PX.Data.BQL.BqlInt.Field<customerLocationID> { }
        public new abstract class vehicleTypeID : PX.Data.BQL.BqlInt.Field<vehicleTypeID> { }
        public new abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
        public new abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
        public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
        public new abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
        public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
        public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        public new abstract class branchLocationID : PX.Data.BQL.BqlInt.Field<branchLocationID> { }
        public new abstract class instServiceOrderID : PX.Data.BQL.BqlInt.Field<instServiceOrderID> { }
        public new abstract class instAppointmentID : PX.Data.BQL.BqlInt.Field<instAppointmentID> { }
        public new abstract class replaceEquipmentID : PX.Data.BQL.BqlInt.Field<replaceEquipmentID> { }
        public new abstract class dispServiceOrderID : PX.Data.BQL.BqlInt.Field<dispServiceOrderID> { }
        public new abstract class dispAppointmentID : PX.Data.BQL.BqlInt.Field<dispAppointmentID> { }
    }
}
