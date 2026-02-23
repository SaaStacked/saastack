using System.Reflection;
using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Interfaces;
using Domain.Interfaces.Services;
using Infrastructure.Common.DomainServices;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrganizationsApplication;
using OrganizationsApplication.ApplicationServices;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using OrganizationsDomain.DomainServices;
using OrganizationsInfrastructure.ApplicationServices;
using OrganizationsInfrastructure.DomainServices;
using OrganizationsInfrastructure.Notifications;
using OrganizationsInfrastructure.Persistence;
using OrganizationsInfrastructure.Persistence.ReadModels;

namespace OrganizationsInfrastructure;

public class OrganizationsModule : ISubdomainModule
{
    public Action<WebHostOptions, WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (_, app, _) => app.RegisterRoutes(); }
    }

    public Assembly DomainAssembly => typeof(OrganizationRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(OrganizationRoot), "org" },
        { typeof(OrganizationOnboardingRoot), "onbrd" }
    };

    public Assembly InfrastructureAssembly => typeof(OrganizationsModule).Assembly;

    public Action<WebHostOptions, ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, _, services) =>
            {
                services.AddSingleton<ITenantSettingsService, AspNetHostLocalFileTenantSettingsService>();
                services.AddSingleton<ITenantSettingService>(c =>
                    new TenantSettingService(new AesEncryptionService(c
                        .GetRequiredServiceForPlatform<IConfigurationSettings>()
                        .GetString(TenantSettingService.EncryptionServiceSecretSettingName))));
                services.AddPerHttpRequest<IOrganizationEmailDomainService, OrganizationEmailDomainService>();
                services
                    .AddPerHttpRequest<IOrganizationsApplication, OrganizationsApplication.OrganizationsApplication>();
                services.AddPerHttpRequest<IOrganizationRepository>(c =>
                    new OrganizationRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<OrganizationRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddPerHttpRequest<IOnboardingApplication, OnboardingApplication>();
                services.AddPerHttpRequest<ICustomOnboardingWorkflowService, CustomOnboardingWorkflowService>();
                services.AddPerHttpRequest<IOnboardingWorkflowService, OnboardingWorkflowGraphService>();
                services.AddPerHttpRequest<IOnboardingCustomWorkflowRepository>(c =>
                    new OnboardingCustomWorkflowRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddPerHttpRequest<IOnboardingRepository>(c =>
                    new OnboardingRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<OrganizationOnboardingRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services
                    .AddPerHttpRequest<IDomainEventNotificationConsumer>(c =>
                        new NotificationConsumer(c.GetRequiredService<ICallerContextFactory>(),
                            c.GetRequiredService<IOrganizationsApplication>()));
                services.RegisterEventing<OrganizationRoot, OrganizationProjection, OrganizationNotifier>(
                    c => new OrganizationProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()),
                    _ => new OrganizationNotifier());
                services.RegisterEventing<OrganizationOnboardingRoot, OrganizationOnboardingProjection>(c =>
                    new OrganizationOnboardingProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));

                services.AddPerHttpRequest<IOrganizationsService>(c =>
                    new OrganizationsInProcessServiceClient(c.LazyGetRequiredService<IOrganizationsApplication>()));
                services.AddPerHttpRequest<ISubscriptionOwningEntityService>(c =>
                    new OrganizationsInProcessServiceClient(c.LazyGetRequiredService<IOrganizationsApplication>()));
            };
        }
    }
}