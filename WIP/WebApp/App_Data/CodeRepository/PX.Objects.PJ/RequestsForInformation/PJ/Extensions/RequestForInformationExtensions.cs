using PX.Objects.PJ.RequestsForInformation.PJ.DAC;
using PX.Objects.PJ.RequestsForInformation.PJ.Descriptor.Attributes;

namespace PX.Objects.PJ.RequestsForInformation.PJ.Extensions
{
    public static class RequestForInformationExtensions
    {
        public static bool IsNew(this RequestForInformation requestsForInformation)
        {
            return requestsForInformation.Status == RequestForInformationStatusAttribute.NewStatus;
        }

        public static bool IsClosed(this RequestForInformation requestsForInformation)
        {
            return requestsForInformation.Status == RequestForInformationStatusAttribute.ClosedStatus;
        }
    }
}