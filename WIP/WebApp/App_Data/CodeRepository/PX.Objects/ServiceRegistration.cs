using System;
using Autofac;
using PX.Data;
using PX.Data.EP;
using PX.Data.RelativeDates;
using PX.Objects.GL.FinPeriods;
using PX.Objects.SM;
using PX.Objects.CM.Extensions;
using PX.Objects.EndpointAdapters;
using PX.Objects.EndpointAdapters.WorkflowAdapters.AR;
using PX.Objects.EndpointAdapters.WorkflowAdapters.AP;
using PX.Objects.EndpointAdapters.WorkflowAdapters.IN;
using PX.Objects.EndpointAdapters.WorkflowAdapters.PO;
using PX.Objects.EndpointAdapters.WorkflowAdapters.SO;
using PX.Objects.FA;
using PX.Objects.PM;
using PX.Objects.CS;
using PX.Objects.AP;
using PX.Objects.EP;
using PX.Data.Search;
using PX.Objects.AP.InvoiceRecognition;
using PX.Objects.IN.Services;

namespace PX.Objects
{
	public class ServiceRegistration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
			.RegisterType<FinancialPeriodManager>()
			.As<IFinancialPeriodManager>();

			builder
				.RegisterType<TodayBusinessDate>()
				.As<ITodayUtc>();

			builder.RegisterType<EP.NotificationProvider>()
				.As<INotificationSender>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder.RegisterType<EP.NotificationProvider>()
				.As<INotificationSenderWithActivityLink>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<FinPeriodRepository>()
				.As<IFinPeriodRepository>();

			builder
				.RegisterType<FinPeriodUtils>()
				.As<IFinPeriodUtils>();

			builder
				.Register<Func<PXGraph, IPXCurrencyService>>(context
					=>
					{
						return (graph)
						=>
						{
							return new DatabaseCurrencyService(graph);
						};
					});

			builder
				.RegisterType<FABookPeriodRepository>()
				.As<IFABookPeriodRepository>();

			builder
				.RegisterType<FABookPeriodUtils>()
				.As<IFABookPeriodUtils>();

			builder
				.RegisterType<BudgetService>()
				.As<IBudgetService>();

			builder
				.RegisterType<UnitRateService>()
				.As<IUnitRateService>();

			builder
				.RegisterType<PM.ProjectSettingsManager>()
				.As<PM.IProjectSettingsManager>();

			builder
				.RegisterType<PM.CostCodeManager>()
				.As<PM.ICostCodeManager>();

			builder
				.RegisterType<PM.ProjectSettingsManager>()
				.As<PM.IProjectSettingsManager>();

			builder.RegisterType<DefaultEndpointImplCR>().AsSelf();
			builder.RegisterType<DefaultEndpointImplCR20>().AsSelf();
			builder.RegisterType<DefaultEndpointImplPM>().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.CaseApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.OpportunityApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.LeadApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.CustomerApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.VendorApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.BusinessAccountApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.ProjectTemplateApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.ProjectTaskApplicator>().SingleInstance().AsSelf();

			builder.RegisterType<BillAdapter>().AsSelf();
			builder.RegisterType<CheckAdapter>().AsSelf();

			builder.RegisterType<InvoiceAdapter>().AsSelf();
			builder.RegisterType<PaymentAdapter>().AsSelf();

			builder.RegisterType<InventoryReceiptAdapter>().AsSelf();
			builder.RegisterType<AdjustmentAdapter>().AsSelf();
			builder.RegisterType<InventoryAdjustmentAdapter>().AsSelf();
			builder.RegisterType<TransferOrderAdapter>().AsSelf();
			builder.RegisterType<KitAssemblyAdapter>().AsSelf();

			builder.RegisterType<PurchaseOrderAdapter>().AsSelf();
			builder.RegisterType<PurchaseReceiptAdapter>().AsSelf();

			builder.RegisterType<SalesOrderAdapter>().AsSelf();
			builder.RegisterType<ShipmentAdapter>().AsSelf();
			builder.RegisterType<SalesInvoiceAdapter>().AsSelf();

			builder
				.RegisterType<CN.Common.Services.NumberingSequenceUsage>()
				.As<CN.Common.Services.INumberingSequenceUsage>();

			builder
				.RegisterType<AdvancedAuthenticationRestrictor>()
				.As<IAdvancedAuthenticationRestrictor>()
				.SingleInstance();

			builder
				.RegisterType<PXEntitySearchEnriched>()
				.As<IEntitySearchService>();

			builder
				.RegisterType<APInvoiceEmailProcessor>()
				.SingleInstance()
				.ActivateOnApplicationStart(EmailProcessorManager.Register);

			builder
				.RegisterType<InventoryAccountService>()
				.As<IInventoryAccountService>();
		}
	}
}
