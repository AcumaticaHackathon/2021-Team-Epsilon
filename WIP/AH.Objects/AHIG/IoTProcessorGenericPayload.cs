using AH.Objects.AHIG.DAC;

namespace AH.Objects.AHIG
{
    internal class IoTProcessorGenericPayload
    {
        private IoTDevicePayloadData _payload;

        public IoTProcessorGenericPayload(IoTDevicePayloadData payload)
        {
            _payload = payload;
        }

            public string Process()
        {
            //right now this does noting so the all we have is a saved payload.
            //we will follow up and get it properly processed.
            //todo: parse the Jason and update records
            
            var resultHtml = @"
<!DOCTYPE html>
<html>
<body>
<h1>Processed {0} Type{1}</h1>
</body>
</html>
";
            return string.Format(resultHtml, _payload.DeviceID, _payload.PayloadType);
        }
    }
}