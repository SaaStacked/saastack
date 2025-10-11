using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserProfilesApplication;
using UserProfilesApplication.Persistence;
using UserProfilesDomain;
using UserProfilesInfrastructure.Api.Profiles;
using UserProfilesInfrastructure.ApplicationServices;
using UserProfilesInfrastructure.Notifications;
using UserProfilesInfrastructure.Persistence;
using UserProfilesInfrastructure.Persistence.ReadModels;

namespace UserProfilesInfrastructure;

public class UserProfilesModule : ISubdomainModule
{
    public Action<WebHostOptions, WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (_, app, _) => app.RegisterRoutes(); }
    }

    public Assembly DomainAssembly => typeof(UserProfileRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(UserProfileRoot), "profile" }
    };

    public Assembly InfrastructureAssembly => typeof(UserProfilesApi).Assembly;

    public Action<WebHostOptions, ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, _, services) =>
            {
                // EXTEND: Change this service for your preferred 3rd party provider
                services.AddSingleton<IAvatarService, NoOpAvatarService>();

                services.AddPerHttpRequest<IUserProfilesApplication>(c =>
                    new UserProfilesApplication.UserProfilesApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredService<IImagesService>(),
                        c.GetRequiredService<IAvatarService>(),
                        c.GetRequiredService<IUserProfileRepository>()));
                services.AddPerHttpRequest<IUserProfileRepository>(c =>
                    new UserProfileRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<UserProfileRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services
                    .AddPerHttpRequest<IDomainEventNotificationConsumer>(c =>
                        new NotificationConsumer(c.GetRequiredService<ICallerContextFactory>(),
                            c.GetRequiredService<IUserProfilesApplication>()));
                services.RegisterEventing<UserProfileRoot, UserProfileProjection, UserProfileNotifier>(
                    c => new UserProfileProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()),
                    _ => new UserProfileNotifier());

                services.AddPerHttpRequest<IUserProfilesService, UserProfilesInProcessServiceClient>();
            };
        }
    }
}