using System.Reflection;
using AncillaryApplication;
using AncillaryApplication.Persistence;
using AncillaryDomain;
using AncillaryInfrastructure.Api.Usages;
using AncillaryInfrastructure.ApplicationServices;
using AncillaryInfrastructure.Persistence;
using AncillaryInfrastructure.Persistence.ReadModels;
using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Domain.Interfaces;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AncillaryInfrastructure;

public class AncillaryModule : ISubdomainModule
{
    public Action<WebHostOptions, WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (_, app, _) => app.RegisterRoutes(); }
    }

    public Assembly DomainAssembly => typeof(AuditRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(AuditRoot), "audit" },
        { typeof(EmailDeliveryRoot), "emaildelivery" },
        { typeof(SmsDeliveryRoot), "smsdelivery" }
    };

    public Assembly InfrastructureAssembly => typeof(UsagesApi).Assembly;

    public Action<WebHostOptions, ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, _, services) =>
            {
                services.AddPerHttpRequest<IRecordingApplication, RecordingApplication>();
                services.AddPerHttpRequest<IFeatureFlagsApplication, FeatureFlagsApplication>();
                services.AddPerHttpRequest<IAncillaryApplication, AncillaryApplication.AncillaryApplication>();
                services.AddSingleton<IUsageMessageQueue>(c =>
                    new UsageMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddPerHttpRequest<IAuditMessageQueueRepository>(c =>
                    new AuditMessageQueueRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddSingleton<IEmailMessageQueue>(c =>
                    new EmailMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddSingleton<ISmsMessageQueue>(c =>
                    new SmsMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddPerHttpRequest<IAuditRepository>(c =>
                    new AuditRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<AuditRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<AuditRoot, AuditProjection>(
                    c => new AuditProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddPerHttpRequest<IEmailDeliveryRepository>(c =>
                    new EmailDeliveryRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<EmailDeliveryRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<EmailDeliveryRoot, EmailDeliveryProjection>(
                    c => new EmailDeliveryProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddPerHttpRequest<ISmsDeliveryRepository>(c =>
                    new SmsDeliveryRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<SmsDeliveryRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<SmsDeliveryRoot, SmsDeliveryProjection>(
                    c => new SmsDeliveryProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddSingleton<IProvisioningMessageQueue>(c =>
                    new ProvisioningMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services
                    .AddPerHttpRequest<IProvisioningNotificationService, OrganizationProvisioningNotificationService>();
                services
                    .AddPerHttpRequest<IAncillaryService, AncillaryInProcessServiceClient>();

                // EXTEND: Change these services for your preferred 3rd party providers
                services.AddSingleton<IUsageDeliveryService, NoOpUsageDeliveryService>();
                services.AddSingleton<IEmailDeliveryService, NoOpEmailDeliveryService>();
                services.AddSingleton<ISmsDeliveryService, NoOpSmsDeliveryService>();
            };
        }
    }
}