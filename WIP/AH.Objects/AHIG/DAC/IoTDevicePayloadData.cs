
using PX.Data;
using PX.Data.BQL;
using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace AH.Objects.AHIG.DAC
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class IoTDevicePayloadData : IBqlTable
    {
        #region Device ID
        public abstract class deviceID : BqlInt.Field<deviceID> { }
        [PXDBInt(IsKey = true)]
        [PXDBDefault(typeof(IoTDevice.deviceID))]
        //[PXParent(typeof(Select<IoTDevice, Where<IoTDevice.deviceID, Equal<deviceID>>>))]
        public virtual int? DeviceID { get; set; }

        #endregion

        #region Line Nbr
        public abstract class lineNbr : BqlInt.Field<lineNbr> { }
        [PXDBInt(IsKey = true)]
        [PXLineNbr(typeof(IoTDevice.payLineCtr))]
        public virtual int? LineNbr { get; set; }

        #endregion
        
        #region Device CD
        public abstract class deviceCD : BqlString.Field<deviceCD> { }
        [PXDBString(50, IsUnicode = true, IsKey = true)]
        [PXSelector(typeof(Search<deviceCD>), DescriptionField = typeof(IoTDevice.deviceName))]
        [PXUIField(DisplayName = "Device Code")]
        [PXDBDefault(typeof(IoTDevice.deviceCD))]
        public virtual string DeviceCD { get; set; }
        
        #endregion

        #region Payload
        public abstract class payload : BqlString.Field<payload> { }
        [PXDBString(4000, IsUnicode = true)]
        [PXUIField(DisplayName = "PayLoad")]
        public virtual string Payload { get; set; }

        #endregion
        
        #region Payload Type
        public abstract class payloadType : BqlString.Field<payloadType> { }
        [PXDBString(20, IsUnicode = true)]
        [PXUIField(DisplayName = "PayLoad Type")]
        public virtual string PayloadType { get; set; }

        #endregion
        
        #region Query Parameters
        public abstract class queryParameters : BqlString.Field<queryParameters> { }
        [PXDBString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Query Parameters")]
        public virtual string QueryParameters { get; set; }

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