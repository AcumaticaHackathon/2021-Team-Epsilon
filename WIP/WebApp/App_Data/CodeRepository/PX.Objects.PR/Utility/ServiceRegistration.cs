using Autofac;
using PX.Payroll.Proxy;

namespace PX.Objects.PR
{
	public class ServiceRegistration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.BindFromConfiguration<PayrollWebServiceConfiguration.Options>("payrollWebServiceConfiguration");
			builder.RegisterType<PayrollWebServiceConfiguration.Initializer>()
				.ActivateOnApplicationStart(i => i.Initialize())
				.SingleInstance();

			builder.BindFromConfiguration<AatrixConfiguration.Options>("aatrixConfiguration");
			builder.RegisterType<AatrixConfiguration.Initializer>()
				.ActivateOnApplicationStart(i => i.Initialize())
				.SingleInstance();
		}
	}
}
