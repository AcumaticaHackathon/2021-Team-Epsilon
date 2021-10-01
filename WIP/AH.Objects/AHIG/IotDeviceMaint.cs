
using PX.Data;
using AH.Objects.AHIG.DAC;

namespace AH.Objects.AHIG
{
    public class IotDeviceMaint : PXGraph<IotDeviceMaint>
    {
        #region Selects

        public PXSelect<IoTDevice> PagePrimaryView;
        public PXSelect<IoTDevicePayloadData, Where<IoTDevicePayloadData.deviceID, Equal<Current<IoTDevice.deviceID>>>> PayLoadView;
        public PXSelect<IoTDeviceLocationBreadCrumb, Where<IoTDeviceLocationBreadCrumb.deviceID, Equal<Current<IoTDevice.deviceID>>>> BreadCrumbView;

        public PXSelect<IoTDevice, Where<IoTDevice.deviceCD, Equal<Required<IoTDevice.deviceCD>>>> SingleDeviceView;
        
        #endregion

        #region Actions

        public PXCancel<IoTDevice> Cancel;
        public PXSave<IoTDevice> Save;

        #endregion
    }
}