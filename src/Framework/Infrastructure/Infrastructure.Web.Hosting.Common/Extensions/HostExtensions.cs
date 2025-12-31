using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.FeatureFlags;
using Common.Recording;
using Domain.Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Shared;
using Infrastructure.Common;
using Infrastructure.Common.Extensions;
using Infrastructure.Common.Recording;
using Infrastructure.Eventing.Common;
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Common.Projections.ReadModels;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Hosting.Common;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Hosting.Common.Recording;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Auth;
using Infrastructure.Web.Hosting.Common.Documentation;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
#if TESTINGONLY
using Domain.Events.Shared.TestingOnly;
using Infrastructure.External.TestingOnly.ApplicationServices;
#endif

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class HostExtensions
{
    private const string AllowedCORSOriginsSettingName = "Hosts:AllowedCORSOrigins";
    private const string CheckPointAggregatePrefix = "check";
    private const string LoggingSettingName = "Logging";
    private static readonly char[] AllowedCORSOriginsDelimiters = [',', ';', ' '];

    /// <summary>
    ///     Configures a WebHost
    /// </summary>
    public static WebApplication ConfigureApiHost(this WebApplicationBuilder appBuilder, SubdomainModules modules,
        WebHostOptions hostOptions)
    {
        var services = appBuilder.Services;
        RegisterSharedServices();
        RegisterConfiguration(hostOptions.IsMultiTenanted);
        RegisterRecording();
        RegisterMultiTenancy(hostOptions.IsMultiTenanted);
        RegisterAuthenticationAuthorization(hostOptions.Authorization, hostOptions.IsMultiTenanted);
        RegisterWireFormats();
        RegisterApiRequests();
        RegisterApiDocumentation(hostOptions.HostName, hostOptions.HostVersion, hostOptions.UsesApiDocumentation);
        RegisterNotifications(hostOptions.UsesNotifications);
        RegisterApplicationServices(hostOptions.IsMultiTenanted,
            hostOptions.ReceivesWebhooks);
        RegisterPersistence(hostOptions.Persistence.UsesQueues, hostOptions.IsMultiTenanted);
        RegisterEventing(hostOptions.Persistence.UsesEventing);
        RegisterCors(hostOptions.CORS);
        modules.RegisterServices(hostOptions, appBuilder.Configuration, services);

        var app = appBuilder.Build();

        // Note: The order of the middleware matters!
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0#middleware-order
        var middlewares = new List<MiddlewareRegistration>();
        app.EnableRequestRewind(middlewares);
        app.AddExceptionShielding(middlewares);
        app.AddBEFFE(middlewares, hostOptions.IsBackendForFrontEnd);
        app.EnableCORS(middlewares, hostOptions.CORS);
        app.EnableSecureAccess(middlewares, hostOptions.Authorization);
        app.EnableMultiTenancy(middlewares, hostOptions.IsMultiTenanted);
        app.EnableEventingPropagation(middlewares, hostOptions.Persistence.UsesEventing);
        app.EnableOtherFeatures(middlewares, hostOptions);

        modules.ConfigureMiddleware(hostOptions, app, middlewares);

        middlewares
            .OrderBy(mw => mw.Priority)
            .ToList()
            .ForEach(mw => mw.Register(app));

        return app;

        void RegisterSharedServices()
        {
            services.AddAntiforgery();
            services.AddHttpContextAccessor();

            // EXTEND: Any global technology adapters
            services.AddSingleton<ICrashReporter, NoOpCrashReporter>();
            services.AddSingleton<IMetricReporter, NoOpMetricReporter>();
#if TESTINGONLY
            services.AddSingleton<IAuditReporter>(c => new QueuedAuditReporter(
                c.GetRequiredService<IHostSettings>(),
                c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                c.GetRequiredServiceForPlatform<IQueueStore>())); // Uses LocalMachineJsonFileStore
            services.AddSingleton<IUsageReporter>(c => new QueuedUsageReporter(
                c.GetRequiredService<IHostSettings>(),
                c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                c.GetRequiredServiceForPlatform<IQueueStore>())); // Uses LocalMachineJsonFileStore
#else
            services.AddSingleton<IAuditReporter, NoOpAuditReporter>();
            services.AddSingleton<IUsageReporter, NoOpUsageReporter>();
#endif
#if TESTINGONLY
            services.AddSingleton<IFeatureFlags>(c => new FakeFeatureFlagProviderServiceClient(
                c.GetRequiredService<IRecorder>(),
                c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                c.GetRequiredService<IHttpClientFactory>(),
                c.GetRequiredService<JsonSerializerOptions>()));
#else
            services.AddSingleton<IFeatureFlags, EmptyFeatureFlags>();
#endif
        }

        void RegisterConfiguration(bool isMultiTenanted)
        {
#if HOSTEDONAZURE
            appBuilder.Configuration.AddJsonFile("appsettings.Azure.json", true);
#elif HOSTEDONAWS
            appBuilder.Configuration.AddJsonFile("appsettings.AWS.json", true);
#endif
            appBuilder.Configuration.AddJsonFile("appsettings.External.json", true);
            appBuilder.Configuration.AddJsonFile("appsettings.local.json", true);

            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<IConfigurationSettings>(c =>
                    new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>(),
                        c.GetRequiredService<ITenancyContext>()));
            }
            else
            {
                services.AddSingleton<IConfigurationSettings>(c =>
                    new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            }

            services.AddForPlatform<IConfigurationSettings>(c =>
                new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            services.AddSingleton<IHostSettings>(c =>
                new HostSettings(c.GetRequiredServiceForPlatform<IConfigurationSettings>()));
        }

        void RegisterRecording()
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConfiguration(appBuilder.Configuration.GetSection(LoggingSettingName));
#if TESTINGONLY
                builder.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "hh:mm:ss ";
                    options.SingleLine = true;
                    options.IncludeScopes = false;
                });
                builder.AddDebug();
#endif
                builder.AddEventSourceLogger();
            });

            // Note: IRecorder should always be not tenanted
            services.AddSingleton<IRecorder>(c =>
                new HostRecorder(c.GetRequiredServiceForPlatform<IDependencyContainer>(),
                    c.GetRequiredService<ILoggerFactory>(),
                    hostOptions));
        }

        void RegisterMultiTenancy(bool isMultiTenanted)
        {
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<ITenancyContext, SimpleTenancyContext>();
                services.AddPerHttpRequest<ITenantDetective, RequestTenantDetective>();
            }
            else
            {
                services.AddSingleton<ITenancyContext, NoOpTenancyContext>();
            }
        }

        void RegisterAuthenticationAuthorization(AuthorizationOptions authentication, bool isMultiTenanted)
        {
            // We are either using tokens or cookies, or neither, but never both
            var defaultScheme = string.Empty;
            if (authentication is { UsesTokens: true, UsesAuthNCookie: false })
            {
                defaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }

            if (authentication is { UsesTokens: false, UsesAuthNCookie: true })
            {
                defaultScheme = BeffeCookieAuthenticationHandler.AuthenticationScheme;
            }

            var onlyHMAC = authentication is
                { UsesHMAC: true, UsesTokens: false, UsesApiKeys: false };
            var onlyApiKey = authentication is
                { UsesApiKeys: true, UsesTokens: false, UsesHMAC: false };
            if (onlyHMAC || onlyApiKey)
            {
                // Note: This is necessary in some versions of dotnet so that the only scheme is not applied to all endpoints by default
                AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme", true);
            }

            var authBuilder = defaultScheme.HasValue()
                ? services.AddAuthentication(defaultScheme)
                : services.AddAuthentication();

            if (authentication.UsesHMAC)
            {
                authBuilder.AddScheme<HMACOptions, HMACAuthenticationHandler>(
                    HMACAuthenticationHandler.AuthenticationScheme,
                    _ => { });
                services.AddAuthorization(configure =>
                {
                    configure.AddPolicy(AuthenticationConstants.Authorization.HMACPolicyName, builder =>
                    {
                        builder.AddAuthenticationSchemes(HMACAuthenticationHandler.AuthenticationScheme);
                        builder.RequireAuthenticatedUser();
                        builder.RequireRole(ClaimExtensions.ToPlatformClaimValue(PlatformRoles.ServiceAccount));
                    });
                });
            }

            if (authentication.UsesApiKeys)
            {
                authBuilder.AddScheme<APIKeyOptions, APIKeyAuthenticationHandler>(
                    APIKeyAuthenticationHandler.AuthenticationScheme,
                    _ => { });
            }

            if (authentication.UsesAuthNCookie)
            {
                authBuilder.AddScheme<BeffeCookieOptions, BeffeCookieAuthenticationHandler>(
                    BeffeCookieAuthenticationHandler.AuthenticationScheme,
                    _ => { });
            }

            if (authentication.UsesTokens)
            {
                authBuilder.AddScheme<PrivateInterHostOptions, PrivateInterHostAuthenticationHandler>(
                    PrivateInterHostAuthenticationHandler.AuthenticationScheme,
                    _ => { });
                services.AddAuthorization(configure =>
                {
                    configure.AddPolicy(AuthenticationConstants.Authorization.PrivateInterHostPolicyName, builder =>
                    {
                        builder.AddAuthenticationSchemes(PrivateInterHostAuthenticationHandler.AuthenticationScheme);
                        builder.RequireAuthenticatedUser();
                    });
                });

                var configuration = appBuilder.Configuration;
                authBuilder.AddJwtBearer(jwtOptions =>
                {
                    jwtOptions.MapInboundClaims = false;
                    jwtOptions.RequireHttpsMetadata = true;
                    jwtOptions.TokenValidationParameters = JWTTokensService.GetTokenValidationParameters(configuration);
                });
            }

            services.AddAuthorization();
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<IAuthorizationHandler, RolesAndFeaturesAuthorizationHandler>();
                services.AddPerHttpRequest<IAuthorizationHandler, AnonymousAuthenticationHandler>();
            }
            else
            {
                services.AddSingleton<IAuthorizationHandler, RolesAndFeaturesAuthorizationHandler>();
                services.AddSingleton<IAuthorizationHandler, AnonymousAuthenticationHandler>();
            }

            // We cannot declaratively configure the RolesAndFeaturesAuthorizationRequirement here,
            // we have to build the policy dynamically with a custom IAuthorizationPolicyProvider
            services
                .AddSingleton<IAuthorizationPolicyProvider, AllAuthorizationPoliciesProvider>();

            if (authentication.UsesTokens || authentication.UsesApiKeys)
            {
                services.AddAuthorization(configure =>
                {
                    configure.AddPolicy(AuthenticationConstants.Authorization.TokenPolicyName, builder =>
                    {
                        builder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme,
                            APIKeyAuthenticationHandler.AuthenticationScheme);
                        builder.RequireAuthenticatedUser();
                    });
                });
            }

            // We need a custom used on endpoints that are marked as anonymous, to verify bad auth proofs,
            // if they exist on the request
            if (authentication.UsesTokens || authentication.UsesApiKeys || authentication.UsesHMAC ||
                authentication.UsesAuthNCookie)
            {
                services.AddAuthorization(configure =>
                {
                    configure.AddPolicy(AuthenticationConstants.Authorization.AnonymousPolicyName, builder =>
                    {
                        var supportedSchemes = new List<string>();
                        if (authentication.UsesTokens)
                        {
                            supportedSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                        }

                        if (authentication.UsesApiKeys)
                        {
                            supportedSchemes.Add(APIKeyAuthenticationHandler.AuthenticationScheme);
                        }

                        if (authentication.UsesHMAC)
                        {
                            supportedSchemes.Add(HMACAuthenticationHandler.AuthenticationScheme);
                        }

                        if (authentication.UsesAuthNCookie)
                        {
                            supportedSchemes.Add(BeffeCookieAuthenticationHandler.AuthenticationScheme);
                        }

                        builder.AddAuthenticationSchemes(supportedSchemes.ToArray());

                        builder.AddRequirements(new AnonymousAuthorizationRequirement());
                    });
                });
            }
        }

        void RegisterApiRequests()
        {
            services.AddSingleton<IHasSearchOptionsValidator, HasSearchOptionsValidator>();
            services.AddSingleton<IHasGetOptionsValidator, HasGetOptionsValidator>();
            services.RegisterFluentValidators(modules.ApiAssemblies);
        }

        void RegisterApiDocumentation(string name, string version, bool usesApiDocumentation)
        {
            if (!usesApiDocumentation)
            {
                return;
            }

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.ParameterFilter<DataAnnotationsParameterFilter>();
                options.SchemaFilter<AllSchemaFilter>();
                options.OperationFilter<ParameterFilter>();
                options.OperationFilter<DefaultBodyFilter>();
                options
                    .OperationFilter<
                        XmlDocumentationOperationFilter>(); // must declare before the DefaultResponsesFilter
                options.OperationFilter<DefaultResponsesFilter>();
                options.OperationFilter<RouteAuthenticationSecurityFilter>();
                options.SwaggerDoc(version, new OpenApiInfo
                {
                    Version = version,
                    Title = name,
                    Description = name
                });

                if (hostOptions.Authorization.UsesTokens)
                {
                    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        Name = HttpConstants.Headers.Authorization,
                        Description =
                            Resources.HostExtensions_ApiDocumentation_TokenDescription.Format(JwtBearerDefaults
                                .AuthenticationScheme),
                        In = ParameterLocation.Header,
                        Scheme = JwtBearerDefaults.AuthenticationScheme,
                        BearerFormat = "JWT"
                    });
                }

                if (hostOptions.Authorization.UsesApiKeys)
                {
                    options.AddSecurityDefinition(APIKeyAuthenticationHandler.AuthenticationScheme,
                        new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.ApiKey,
                            Name = HttpConstants.QueryParams.APIKey,
                            Description =
                                Resources.HostExtensions_ApiDocumentation_APIKeyQueryDescription.Format(HttpConstants.QueryParams
                                    .APIKey),
                            In = ParameterLocation.Query,
                            Scheme = APIKeyAuthenticationHandler.AuthenticationScheme
                        });
                }
            });
        }

        void RegisterNotifications(bool usesNotifications)
        {
            if (usesNotifications)
            {
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
                services.AddSingleton<IEmailSchedulingService, QueuingEmailSchedulingService>();
                services.AddSingleton<ISmsSchedulingService, QueuingSmsSchedulingService>();
                services.AddSingleton<IWebsiteUiService, WebsiteUiService>();
                services.AddSingleton<IUserNotificationsService>(c =>
                    new MessageUserNotificationsService(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<IHostSettings>(), c.GetRequiredService<IWebsiteUiService>(),
                        c.GetRequiredService<IEmailSchedulingService>(),
                        c.GetRequiredService<ISmsSchedulingService>()));
            }
        }

        void RegisterWireFormats()
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
            serializerOptions.Converters.Add(new JsonDateTimeConverter(DateFormat.Iso8601));

            services.AddSingleton(serializerOptions);
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = serializerOptions.PropertyNameCaseInsensitive;
                options.SerializerOptions.PropertyNamingPolicy = serializerOptions.PropertyNamingPolicy;
                options.SerializerOptions.WriteIndented = serializerOptions.WriteIndented;
                options.SerializerOptions.DefaultIgnoreCondition = serializerOptions.DefaultIgnoreCondition;
                foreach (var converter in serializerOptions.Converters)
                {
                    options.SerializerOptions.Converters.Add(converter);
                }
            });

            services.ConfigureHttpXmlOptions(options => { options.SerializerOptions.WriteIndented = false; });
        }

        void RegisterApplicationServices(bool isMultiTenanted, bool receivesWebhooks)
        {
            services.AddHttpClient();
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                {
                    var container = builder.Services;
                    builder.AdditionalHandlers.Add(new HttpClientLoggingHandler(
                        container.GetRequiredService<IRecorder>(),
                        container.GetRequiredService<ICallerContextFactory>()));
                });
            });

            var prefixes = modules.EntityPrefixes;
            prefixes.Add(typeof(Checkpoint), CheckPointAggregatePrefix);
            services.AddSingleton<IIdentifierFactory>(_ => new HostIdentifierFactory(prefixes));

            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<ICallerContextFactory, AspNetHttpContextCallerFactory>();
            }
            else
            {
                services.AddSingleton<ICallerContextFactory, AspNetHttpContextCallerFactory>();
            }

            if (isMultiTenanted)
            {
                if (receivesWebhooks)
                {
                    services
                        .AddPerHttpRequest<IWebhookNotificationAuditRepository, WebhookNotificationAuditRepository>();
                    services.AddPerHttpRequest<IWebhookNotificationAuditService, WebhookNotificationAuditService>();
                }
            }
            else
            {
                if (receivesWebhooks)
                {
                    services.AddSingleton<IWebhookNotificationAuditRepository, WebhookNotificationAuditRepository>();
                    services.AddSingleton<IWebhookNotificationAuditService, WebhookNotificationAuditService>();
                }
            }
        }

        void RegisterPersistence(bool usesQueues, bool isMultiTenanted)
        {
            var domainAssemblies = modules.SubdomainAssemblies
                .Concat([typeof(DomainCommonMarker).Assembly, typeof(DomainSharedMarker).Assembly])
                .ToArray();

            services.AddForPlatform<IDependencyContainer, DotNetDependencyContainer>();
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<IDependencyContainer, DotNetDependencyContainer>();
                services.AddPerHttpRequest<IDomainFactory>(c => DomainFactory.CreateRegistered(
                    c.GetRequiredService<IDependencyContainer>(), domainAssemblies));
            }
            else
            {
                services.AddSingleton<IDependencyContainer, DotNetDependencyContainer>();
                services.AddSingleton<IDomainFactory>(c => DomainFactory.CreateRegistered(
                    c.GetRequiredServiceForPlatform<IDependencyContainer>(), domainAssemblies));
            }

            services.AddSingleton<IMessageQueueMessageIdFactory, MessageQueueMessageIdFactory>();
            services.AddSingleton<IMessageBusTopicMessageIdFactory, MessageBusTopicMessageIdFactory>();
            services.AddSingleton<IEventSourcedChangeEventMigrator>(_ => new ChangeEventTypeMigrator(
                new Dictionary<Type, Type>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    //EXTEND: Add your other domain event migrations here
#if TESTINGONLY
                    { typeof(Happened), typeof(HappenedV2) }
#endif
#pragma warning restore CS0618 // Type or member is obsolete
                }));
#if TESTINGONLY
            TestingOnlyHostExtensions.RegisterStoreForTestingOnly(services, usesQueues, isMultiTenanted);
#endif
        }

        void RegisterEventing(bool usesEventing)
        {
            if (usesEventing)
            {
                //EXTEND: Add support for other eventing mechanisms
                // Note: we are sending "domain events" via a message bus back to each ApiHost,
                // and sending "integration events" to some external message broker

                services.AddPerHttpRequest<IDomainEventConsumerRelay, AsynchronousQueueConsumerRelay>();
                services.AddPerHttpRequest<IEventNotificationMessageBroker, NoOpEventNotificationMessageBroker>();
            }
        }

        void RegisterCors(CORSOption cors)
        {
            if (cors == CORSOption.None)
            {
                return;
            }

            services.AddCors(options =>
            {
                if (cors == CORSOption.SameOrigin)
                {
                    var allowedOrigins = appBuilder.Configuration.GetValue<string>(AllowedCORSOriginsSettingName)
                                         ?? string.Empty;
                    if (allowedOrigins.HasNoValue())
                    {
                        throw new InvalidOperationException(
                            Resources.CORS_MissingSameOrigins.Format(AllowedCORSOriginsSettingName));
                    }

                    var origins = allowedOrigins.Split(AllowedCORSOriginsDelimiters);
                    options.AddDefaultPolicy(corsBuilder =>
                    {
                        corsBuilder.WithOrigins(origins);
                        corsBuilder.AllowAnyMethod();
                        corsBuilder.WithHeaders(HttpConstants.Headers.ContentType, HttpConstants.Headers.Authorization);
                        corsBuilder.DisallowCredentials();
                        corsBuilder.SetPreflightMaxAge(TimeSpan.FromSeconds(600));
                    });
                }

                if (cors == CORSOption.AnyOrigin)
                {
                    options.AddDefaultPolicy(corsBuilder =>
                    {
                        corsBuilder.AllowAnyOrigin();
                        corsBuilder.AllowAnyMethod();
                        corsBuilder.WithHeaders(HttpConstants.Headers.ContentType, HttpConstants.Headers.Authorization);
                        corsBuilder.DisallowCredentials();
                        corsBuilder.SetPreflightMaxAge(TimeSpan.FromSeconds(600));
                    });
                }
            });
        }
    }
}