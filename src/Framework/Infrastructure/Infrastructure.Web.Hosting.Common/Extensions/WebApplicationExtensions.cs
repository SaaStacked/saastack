using System.Net;
using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class WebApplicationExtensions
{
    private const int CustomMiddlewareIndex = 300;

    /// <summary>
    ///     Provides request handling for a BEFFE
    /// </summary>
    public static void AddBEFFE(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, bool isBEFFE)
    {
        if (!isBEFFE)
        {
            return;
        }

        middlewares.Add(new MiddlewareRegistration(35, app =>
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            return MiddlewareRegistration.Result.Report("Pipeline: Serving static HTML/CSS/JS is enabled");
        }));
        middlewares.Add(new MiddlewareRegistration(CustomMiddlewareIndex + 100, app =>
        {
            app.UseMiddleware<CSRFMiddleware>();
            app.UseMiddleware<ReverseProxyMiddleware>();
            return MiddlewareRegistration.Result.Report(
                "Pipeline: BEFFE reverse proxy with CSRF protection is enabled");
        }));
    }

    /// <summary>
    ///     Provides a global handler when an exception is encountered, and converts the exception
    ///     to an <see href="https://datatracker.ietf.org/doc/html/rfc7807">RFC7807</see> error.
    ///     Note: Shows the exception stack trace if in development mode
    /// </summary>
    public static void AddExceptionShielding(this WebApplication builder,
        List<MiddlewareRegistration> middlewares)
    {
        middlewares.Add(new MiddlewareRegistration(20, app =>
        {
            app.UseExceptionHandler(configure => configure.Run(async context =>
            {
                var exceptionMessage = string.Empty;
                var exceptionStackTrace = string.Empty;
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (app.Environment.IsTestingOnly())
                {
                    if (contextFeature.Exists())
                    {
                        var exception = contextFeature.Error;
                        exceptionMessage = exception.Message;
                        exceptionStackTrace = exception.ToString();
                    }
                }

                var details = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = Resources.WebApplicationExtensions_AddExceptionShielding_UnexpectedExceptionMessage,
                    Status = (int)HttpStatusCode.InternalServerError,
                    Instance = context.Request.GetDisplayUrl(),
                    Detail = exceptionMessage
                };
                if (exceptionStackTrace.HasValue())
                {
                    details.Extensions.Add(HttpConstants.Responses.ProblemDetails.Extensions.ExceptionPropertyName,
                        exceptionStackTrace);
                }

                //NOTE: Leave a breakpoints here while debugging Integration testing, or running locally
                await Results.Problem(details)
                    .ExecuteAsync(context);
            }));
            return MiddlewareRegistration.Result.Report("Pipeline: Exception Shielding is enabled");
        }));
    }

    /// <summary>
    ///     Enables CORS for the host
    /// </summary>
    public static void EnableCORS(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, CORSOption cors)
    {
        if (cors == CORSOption.None)
        {
            return;
        }

        middlewares.Add(new MiddlewareRegistration(40, app =>
        {
            app.UseCors();

            var httpContext = builder.Services.GetRequiredService<IHttpContextFactory>()
                .Create(new FeatureCollection());
            var policy = builder.Services.GetRequiredService<ICorsPolicyProvider>()
                .GetPolicyAsync(httpContext, WebHostingConstants.DefaultCORSPolicyName).GetAwaiter().GetResult()!
                .ToString();
            return MiddlewareRegistration.Result.Report("Pipeline: CORS is enabled: Policy -> {Policy}", policy);
        }));
    }

    /// <summary>
    ///     Starts the relays for eventing projections and eventing notifications
    /// </summary>
    public static void EnableEventingPropagation(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, bool usesEventing)
    {
        if (!usesEventing)
        {
            return;
        }

        middlewares.Add(new MiddlewareRegistration(CustomMiddlewareIndex + 40, app =>
        {
            app.Use(async (context, next) =>
            {
                var readModelRelay = context.RequestServices.GetRequiredService<IEventNotifyingStoreProjectionRelay>();
                readModelRelay.Start();

                var notificationRelay =
                    context.RequestServices.GetRequiredService<IEventNotifyingStoreNotificationRelay>();
                notificationRelay.Start();

                await next();

                readModelRelay.Stop();
                notificationRelay.Stop();
            });
            return MiddlewareRegistration.Result.Report("Pipeline: Event Projections/Notifications are enabled");
        }));

        middlewares.Add(new MiddlewareRegistration(CustomMiddlewareIndex + 45,
            _ =>
            {
                var subscriberService = builder.Services.GetService<IDomainEventingSubscriberService>();
                if (subscriberService.NotExists())
                {
                    return MiddlewareRegistration.Result.Ignore;
                }

                var subscribers = subscriberService.SubscriptionNames;
                var subscriptionNames = subscribers.JoinAsOredChoices(", ");
                var registered = subscriberService.RegisterAllSubscribersAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();
                if (registered.IsFailure)
                {
                    throw new InvalidOperationException(registered.Error.Message);
                }

                return MiddlewareRegistration.Result.Report(
                    "Feature: Registered the following {Count} subscribers to topic {Topic}, subscribers: {SubscriptionNames}",
                    subscribers.Count, EventingConstants.Topics.DomainEvents, subscriptionNames);
            }));
    }

    /// <summary>
    ///     Enables tenant detection
    /// </summary>
    public static void EnableMultiTenancy(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, bool isEnabled)
    {
        if (!isEnabled)
        {
            return;
        }

        middlewares.Add(new MiddlewareRegistration(52, //Must be after authentication and before Authorization 
            app =>
            {
                app.UseMiddleware<MultiTenancyMiddleware>();
                return MiddlewareRegistration.Result.Report("Pipeline: Multi-Tenancy detection is enabled");
            }));
    }

    /// <summary>
    ///     Enables other options
    /// </summary>
    public static void EnableOtherFeatures(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, WebHostOptions hostOptions)
    {
        middlewares.Add(new MiddlewareRegistration(-80, _ =>
        {
            //Nothing to register, only reporting
            var loggers = builder.Services.GetServices<ILoggerProvider>()
                .Select(logger => logger.GetType().Name).Join(", ");
            return MiddlewareRegistration.Result.Report("Feature: Logging to -> {Providers}", loggers);
        }));

        middlewares.Add(new MiddlewareRegistration(-70, _ =>
        {
            //Nothing to register only reporting
            var appSettings = ((ConfigurationManager)builder.Configuration).Sources
                .OfType<JsonConfigurationSource>()
                .Select(jsonSource => jsonSource.Path)
                .Join(", ");
            return MiddlewareRegistration.Result.Report("Feature: Configuration loaded from -> {Sources}",
                appSettings);
        }));

        middlewares.Add(new MiddlewareRegistration(-60, _ =>
        {
            //Nothing to register only reporting
            var recorder = builder.Services.GetRequiredService<IRecorder>()
                .ToString()!;
            return MiddlewareRegistration.Result.Report("Feature: Recording with -> {Recorder}", recorder);
        }));

        middlewares.Add(new MiddlewareRegistration(-50,
            app =>
            {
                if (!hostOptions.UsesApiDocumentation)
                {
                    return MiddlewareRegistration.Result.Ignore;
                }

                var prefix = hostOptions.IsBackendForFrontEnd
                    ? WebConstants.BackEndForFrontEndDocsPath.Trim('/')
                    : string.Empty; //Note: puts the swagger docs at the root of the API
                var url = builder.Configuration.GetValue<string>(WebHostDefaults.ServerUrlsKey);
                var path = prefix.HasValue()
                    ? $"{url}/{prefix}"
                    : url!;

                app.MapSwagger();
                app.UseSwaggerUI(options =>
                {
                    var jsonEndpoint = WebConstants.SwaggerEndpointFormat.Format(hostOptions.HostVersion);
                    options.DocumentTitle = hostOptions.HostName;
                    options.SwaggerEndpoint(jsonEndpoint, hostOptions.HostName);
                    options.RoutePrefix = prefix;
                });
                return MiddlewareRegistration.Result.Report(
                    "Feature: Open API documentation enabled with Swagger UI -> {Path}", path);
            }));

        middlewares.Add(new MiddlewareRegistration(-40, _ =>
        {
            //Nothing to register only reporting
            var dataStore = builder.Services.GetRequiredServiceForPlatform<IDataStore>().GetType().Name;
            var eventStore = builder.Services.GetRequiredServiceForPlatform<IEventStore>().GetType().Name;
            var queueStore = builder.Services.GetRequiredServiceForPlatform<IQueueStore>().GetType().Name;
            var blobStore = builder.Services.GetRequiredServiceForPlatform<IBlobStore>().GetType().Name;
            var messageBusStore = builder.Services.GetRequiredServiceForPlatform<IMessageBusStore>().GetType().Name;
            return MiddlewareRegistration.Result.Report(
                "Feature: Platform Persistence stores: DataStore -> {DataStore}, EventStore -> {EventStore}, MessageBusStore -> {messageBusStore}, QueueStore -> {QueueStore}, BlobStore -> {BlobStore}",
                dataStore, eventStore, messageBusStore, queueStore, blobStore);
        }));

        middlewares.Add(new MiddlewareRegistration(56, app =>
        {
            app.UseAntiforgery();
            return MiddlewareRegistration.Result.Report("Pipeline: Anti-forgery detection is enabled");
        }));
    }

    /// <summary>
    ///     Enables request buffering, so that request bodies can be read in filters.
    ///     Note: Required to read the request by:
    ///     <see cref="HttpRequestExtensions.VerifyHMACSignatureAsync" /> during HMAC signature verification,
    ///     <see cref="ContentNegotiationFilter" />,
    ///     <see cref="Infrastructure.Web.Api.Common.RequestTenantDetective" />,
    ///     and by <see cref="HttpRecordingFilter"/>
    /// </summary>
    public static void EnableRequestRewind(this WebApplication builder,
        List<MiddlewareRegistration> middlewares)
    {
        middlewares.Add(new MiddlewareRegistration(10, app =>
        {
            app.Use(async (context, next) =>
            {
                context.Request.EnableBuffering();
                await next();
            });
            return MiddlewareRegistration.Result.Report("Pipeline: Rewinding of requests is enabled");
        }));
    }

    /// <summary>
    ///     Enables authentication and authorization
    /// </summary>
    public static void EnableSecureAccess(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, AuthorizationOptions authorization)
    {
        middlewares.Add(new MiddlewareRegistration(50, app =>
        {
            app.UseAuthentication();
            return MiddlewareRegistration.Result.Report(
                "Pipeline: Authentication is enabled: HMAC -> {HMAC}, APIKeys -> {APIKeys}, Tokens -> {Tokens}, Cookie -> {Cookie}",
                authorization.UsesHMAC, authorization.UsesApiKeys, authorization.UsesTokens,
                authorization.UsesAuthNCookie);
        }));
        middlewares.Add(
            new MiddlewareRegistration(54, app =>
            {
                app.UseAuthorization();
                return MiddlewareRegistration.Result.Report(
                    "Pipeline: Authorization is enabled: Anonymous -> Enabled, Roles -> Enabled, Features -> Enabled");
            }));
    }
}