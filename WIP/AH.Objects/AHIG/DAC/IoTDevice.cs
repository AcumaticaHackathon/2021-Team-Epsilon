
using PX.Data;
using PX.Data.BQL;
using System;
using System.Diagnostics.CodeAnalysis;
using PX.Objects.CS;

namespace AH.Objects.AHIG.DAC
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class IoTDevice : IBqlTable
    {
        #region Device ID
        public abstract class deviceID : BqlInt.Field<deviceID> { }
        [PXDBIdentity]
        public virtual int? DeviceID { get; set; }

        #endregion

        #region Device CD
        public abstract class deviceCD : BqlString.Field<deviceCD> { }
        [PXDBString(50, IsUnicode = true, IsKey = true)]
        [PXSelector(typeof(Search<deviceCD>), DescriptionField = typeof(deviceName))]
        [PXUIField(DisplayName = "Device Code")]
        
        public virtual string DeviceCD { get; set; }
        
        #endregion

        #region Device Name
        public abstract class deviceName : BqlString.Field<deviceName> { }
        [PXDBString(60, IsUnicode = true)]
        [PXUIField(DisplayName = "Device Name")]
        public virtual string DeviceName { get; set; }

        #endregion

        #region Acumatica Entity Type
        public abstract class  acumaticaEntityType : BqlString.Field<acumaticaEntityType> { }
        [PXDBString(30, IsUnicode = true)]
        [PXUIField(DisplayName = "Entity Type")]
        public virtual string AcumaticaEntityType { get; set; }

        #endregion

        #region Ref Note ID
        public abstract class refNoteID : BqlGuid.Field<refNoteID> { }
        [PXDBGuid]
        public virtual Guid? RefNoteID { get; set; }

        #endregion

        #region Latitude
        public abstract class  latitude : BqlDecimal.Field<latitude> { }
        [PXDBDecimal(8)]
        [PXUIField(DisplayName = "Latitude")]
        public virtual decimal? Latitude { get; set; }

        #endregion
        
        #region Longitude
        public abstract class  longitude : BqlDecimal.Field<longitude> { }
        [PXDBDecimal(8)]
        [PXUIField(DisplayName = "Longitude")]
        public virtual decimal? Longitude { get; set; }

        #endregion

        #region Zone
        public abstract class zone : BqlString.Field<zone> { }
        [PXDBString(60, IsUnicode = true)]
        [PXUIField(DisplayName = "Zone")]
        public virtual string Zone { get; set; }

        #endregion

        #region Address ID
        public abstract class addressID : BqlInt.Field<addressID> { }
        [PXDBInt]
        public virtual int? AddressID { get; set; }

        #endregion

        #region PayLineCtr
        public abstract class payLineCtr : BqlInt.Field<payLineCtr> { }
        [PXDBInt]
        [PXDefault(TypeCode.Int32, "0")]
        public virtual int? PayLineCtr { get; set; }

        #endregion
        
        #region LocLineCtr
        public abstract class locLineCtr : BqlInt.Field<locLineCtr> { }
        [PXDBInt]
        [PXDefault(TypeCode.Int32, "0")]
        public virtual int? LocLineCtr { get; set; }

        #endregion

        #region System Fields

        #region NoteID
        public abstract class noteID : BqlGuid.Field<noteID> {}
        [PXNote(new Type[0])]
        [PXUIField(Visible = false, Visibility = PXUIVisibility.Invisible)]
        public virtual Guid? NoteID { get; set; }

        #endregion
		
        #region CreatedByID
        public abstract class createdByID : BqlGuid.Field<createdByID> { }
        [PXDBCreatedByID]
        public virtual Guid? CreatedByID { get; set; }

        #endregion

        #region CreatedByScreenID
        public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
        [PXDBCreatedByScreenID]
        public virtual string CreatedByScreenID { get; set; }

        #endregion

        #region CreatedDateTime
        public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
        [PXDBCreatedDateTime]
        public virtual DateTime? CreatedDateTime { get; set; }

        #endregion

        #region LastModifiedByID
        public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
        [PXDBLastModifiedByID]
        public virtual Guid? LastModifiedByID { get; set; }

        #endregion

        #region LastModifiedByScreenID
        public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
        [PXDBLastModifiedByScreenID]
        public virtual string LastModifiedByScreenID { get; set; }

        #endregion

        #region LastModifiedDateTime
        public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
        [PXDBLastModifiedDateTime]
        public virtual DateTime? LastModifiedDateTime { get; set; }

        #endregion

        #region tstamp
        public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
        [PXDBTimestamp]
        public virtual byte[] tstamp { get; set; }

        #endregion

        #endregion
    }
}