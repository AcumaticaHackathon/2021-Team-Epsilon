
using PX.Data;
using PX.Data.BQL;
using System;
using System.Diagnostics.CodeAnalysis;

namespace AH.Objects.AHIG.DAC
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class IoTDeviceLocationBreadCrumb : IBqlTable
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
        [PXLineNbr(typeof(IoTDevice.locLineCtr))]
        public virtual int? LineNbr { get; set; }

        #endregion
        
        #region Device CD
        public abstract class deviceCD : BqlString.Field<deviceCD> { }
        [PXDBString(50, IsUnicode = true, IsKey = true)]
        [PXSelector(typeof(Search<deviceCD>), DescriptionField = typeof(IoTDevice.deviceName))]
        [PXUIField(DisplayName = "Device Code")]
        public virtual string DeviceCD { get; set; }
        
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
        
        #region ZoneX Coordinate
        public abstract class  zoneXCoordinate : BqlDecimal.Field<zoneXCoordinate> { }
        [PXDBDecimal(8)]
        [PXUIField(DisplayName = "ZoneX Coordinate")]
        public virtual decimal? ZoneXCoordinate { get; set; }

        #endregion
        
        #region ZoneY Cooridinate
        public abstract class  zoneYCooridinate : BqlDecimal.Field<zoneYCooridinate> { }
        [PXDBDecimal(8)]
        [PXUIField(DisplayName = "ZoneY Cooridinate")]
        public virtual decimal? ZoneYCooridinate { get; set; }

        #endregion

        #region Time

        public abstract class time : BqlDateTime.Field<time> { }
        [PXDBDateAndTime]
        [PXUIField(DisplayName = "Time")]
        public virtual DateTime? Time { get; set; }
        #endregion
    }
}