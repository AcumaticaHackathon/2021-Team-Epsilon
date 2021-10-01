using System;
using System.Linq;
using PX.Objects.PJ.RequestsForInformation.PJ.DAC;
using PX.Objects.PJ.RequestsForInformation.PJ.Descriptor.Attributes;
using PX.Data;
using PX.Objects.PM;
using PX.Objects.PM.ChangeRequest;
using PX.Objects.PJ.ProjectManagement.PM.CacheExtensions;
using PX.Data.WorkflowAPI;

namespace PX.Objects.PJ.ProjectManagement.PM.Services
{
    public class RequestForInformationConversionService : ConversionServiceBase
    {
        public RequestForInformationConversionService(ChangeRequestEntry graph)
            : base(graph)
        {
        }

        public override void UpdateConvertedEntity(PMChangeRequest changeRequest)
        {
            var requestForInformation = GetRequestForInformation(changeRequest);
            if (requestForInformation != null)
            {
                UpdateRequestForInformation(requestForInformation, null,
                    RequestForInformation.Events.Select(ev => ev.DeleteChangeRequest));
            }
        }

        public override void SetFieldReadonly(PMChangeRequest changeRequest)
        {
            SetFieldReadOnly<PmChangeRequestExtension.rfiID>(changeRequest);
        }

        protected override void ProcessConvertedChangeRequest(PMChangeRequest changeRequest)
        {
            var requestForInformation = GetRequestForInformation(changeRequest);
            UpdateRequestForInformation(requestForInformation, changeRequest.NoteID, RequestForInformation.Events.Select(ev => ev.ConvertToChangeRequest));
            CopyFilesToChangeRequest<RequestForInformation>(requestForInformation, changeRequest);
        }

        private void UpdateRequestForInformation(RequestForInformation requestForInformation,
            Guid? noteId, SelectedEntityEvent<RequestForInformation> rfiEvent)
        {
            requestForInformation.ConvertedTo = noteId;
            RaiseRequestForInformationEvent(requestForInformation, rfiEvent);
            Graph.Caches<RequestForInformation>().PersistUpdated(requestForInformation);
        }

        private RequestForInformation GetRequestForInformation(PMChangeRequest changeRequest)
        {
            var changeRequestExt = PXCache<PMChangeRequest>.GetExtension<PmChangeRequestExtension>(changeRequest);
            return Graph.Select<RequestForInformation>()
                .SingleOrDefault(rfi => rfi.RequestForInformationId == changeRequestExt.RFIID);
        }

        protected virtual void RaiseRequestForInformationEvent(RequestForInformation requestForInformation, 
            SelectedEntityEvent<RequestForInformation> rfiEvent)
		{
            rfiEvent.FireOn<RequestForInformation>(Graph, requestForInformation);
		}
    }
}
