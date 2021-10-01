using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PX.CloudServices.DocumentRecognition;

namespace PX.Objects.AP.InvoiceRecognition
{
    internal class InvoiceRecognitionService : IInvoiceRecognitionService, IInvoiceRecognitionFeedback
    {
        private const string ModelName = "tex-document-01";

        private readonly IDocumentRecognitionClient _documentRecognitionClient;

        public InvoiceRecognitionService(IDocumentRecognitionClient documentRecognitionClient)
        {
            _documentRecognitionClient = documentRecognitionClient;
        }

        string IInvoiceRecognitionService.ModelName => ModelName;

        bool IInvoiceRecognitionService.IsConfigured() => _documentRecognitionClient.IsConfigured();

        async Task<DocumentRecognitionResponse> IInvoiceRecognitionService.SendFile(Guid fileId, byte[] file, string contentType, CancellationToken cancellationToken) =>
            new DocumentRecognitionResponse(
                ToState(await _documentRecognitionClient.SendFile(ModelName, fileId, file, contentType, cancellationToken))
            );

        async Task<DocumentRecognitionResponse> IInvoiceRecognitionService.GetResult(string state, CancellationToken cancellationToken)
        {
            var (result, uri) = await _documentRecognitionClient.GetResult(FromState(state), cancellationToken);
            if (result != null)
                return new DocumentRecognitionResponse(result);
            if (uri != null)
                return new DocumentRecognitionResponse(ToState(uri));
            throw new InvalidOperationException($"The result from {nameof(IDocumentRecognitionClient.GetResult)} is completely empty");
        }

        private static Uri FromState(string state) => new Uri(state);
        private static string ToState(Uri uri) => uri.ToString();

        Task IInvoiceRecognitionFeedback.Send(Uri address, HttpContent content, CancellationToken cancellationToken) =>
            _documentRecognitionClient.Feedback(address, content, cancellationToken);
    }
}