using System;
using Autofac;

namespace PX.Objects.AP.InvoiceRecognition
{
    internal class ServiceRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<InvoiceRecognitionService>()
                .As<IInvoiceRecognitionService>()
                .PreserveExistingDefaults();
        }
    }
}
