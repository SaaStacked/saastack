using System.Net;
using ApiHost1;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
#if TESTINGONLY
using SubscriptionsInfrastructure.IntegrationTests.Stubs;
#endif

namespace SubscriptionsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class TrialEventsApiSpec : WebApiSpec<Program>
{
#if TESTINGONLY
    private readonly StubManagedTrialBillingProvider _stubBillingProvider;
#endif
    private readonly ISubscriptionTrialEventMessageQueueRepository _trialEventMessageRepository;

    public TrialEventsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _trialEventMessageRepository = setup.GetRequiredService<ISubscriptionTrialEventMessageQueueRepository>();
#if TESTINGONLY
        _trialEventMessageRepository.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        _stubBillingProvider = setup.GetRequiredService<IBillingProvider>().As<StubManagedTrialBillingProvider>();
        _stubBillingProvider.Reset();
#endif
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDeliverTrialEvent_ThenDelivers()
    {
        // Creating new subscription (personal) organization, already starts the trial, and dispatches the first Active event 
        await LoginUserAsync(LoginUser.Operator);

        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anactiveeventid1");
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllTrialEventsAndNone_ThenDoesNotDrainAny()
    {
        var request = new DrainAllSubscriptionTrialEventsRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _stubBillingProvider.LastTrialEvent.Should().BeNull();
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenDrainAllTrialEventsAndSome_ThenDrains()
    {
        var login = await LoginUserAsync(LoginUser.Operator);
        var tenantId = login.DefaultOrganizationId!;

        var call = CallContext.CreateCustom("acallid", "acallerid", tenantId, DatacenterLocations.Local);
        await _trialEventMessageRepository.PushAsync(call, new SubscriptionTrialEventMessage
        {
            MessageId = "amessageid1",
            ProviderName = _stubBillingProvider.ProviderName,
            OwningEntityId = tenantId,
            Event = new QueuedTrialEvent
            {
                EventId = "aneventid1",
                DelayInDays = 0,
                Action = nameof(TrialScheduledEventAction.Notification),
                Track = nameof(TrialScheduledEventTrack.Active),
                Metadata = new Dictionary<string, string>()
            }
        }, CancellationToken.None);
        await _trialEventMessageRepository.PushAsync(call, new SubscriptionTrialEventMessage
        {
            MessageId = "amessageid2",
            ProviderName = _stubBillingProvider.ProviderName,
            OwningEntityId = tenantId,
            Event = new QueuedTrialEvent
            {
                EventId = "aneventid2",
                DelayInDays = 0,
                Action = nameof(TrialScheduledEventAction.Notification),
                Track = nameof(TrialScheduledEventTrack.Active),
                Metadata = new Dictionary<string, string>()
            }
        }, CancellationToken.None);
        await _trialEventMessageRepository.PushAsync(call, new SubscriptionTrialEventMessage
        {
            MessageId = "amessageid3",
            ProviderName = _stubBillingProvider.ProviderName,
            OwningEntityId = tenantId,
            Event = new QueuedTrialEvent
            {
                EventId = "aneventid3",
                DelayInDays = 0,
                Action = nameof(TrialScheduledEventAction.Notification),
                Track = nameof(TrialScheduledEventTrack.Active),
                Metadata = new Dictionary<string, string>()
            }
        }, CancellationToken.None);

        var request = new DrainAllSubscriptionTrialEventsRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anactiveeventid1");
    }
#endif

    [Fact]
    public async Task WhenCycleThroughAllTrialStatesAndDeliverFirstEventManually_ThenSendsNotifications()
    {
        // Creating new subscription (personal) organization, already starts the trial, and dispatches the first Active event 
        var login = await LoginUserAsync(LoginUser.Operator);
        var organizationId = login.DefaultOrganizationId!;

#if TESTINGONLY
        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anactiveeventid1");

        await ExpireTrialAsync(organizationId);
        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anexpiredeventid1");

        await ConvertSubscriptionAsync(organizationId);
        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("aconvertedeventid1");
#endif
    }

    [Fact]
    public async Task WhenCycleThroughActiveTrialStateEventsAndDeliverThemManually_ThenSendsAllNotifications()
    {
        // Creating new subscription (personal) organization, already starts the trial, and dispatches the first Active event 
        var login = await LoginUserAsync(LoginUser.Operator);
        var organizationId = login.DefaultOrganizationId!;

#if TESTINGONLY
        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anactiveeventid1");

        // We manually deliver this event to the API, as the same event is waiting on the queue, scheduled for the far future
        await DeliverTrialEventNotificationAsync(organizationId, "anactiveeventid2", TrialScheduledEventTrack.Active);

        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anactiveeventid2");

        // We manually deliver this event to the API, as the same event is waiting on the queue, scheduled for the far future
        await DeliverTrialEventNotificationAsync(organizationId, "anactiveeventid3", TrialScheduledEventTrack.Active);

        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anactiveeventid3");
#endif
    }

    [Fact]
    public async Task WhenCycleThroughExpireTrialStateEventsAndDeliverThemManually_ThenSendsNotifications()
    {
        // Creating new subscription (personal) organization, already starts the trial, and dispatches the first Active event 
        var login = await LoginUserAsync(LoginUser.Operator);
        var organizationId = login.DefaultOrganizationId!;

#if TESTINGONLY
        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anactiveeventid1");

        await ExpireTrialAsync(organizationId);
        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anexpiredeventid1");

        // We manually deliver this event to the API, as the same event is waiting on the queue, scheduled for the far future
        await DeliverTrialEventNotificationAsync(organizationId, "anexpiredeventid2", TrialScheduledEventTrack.Expired);

        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anexpiredeventid2");

        // We manually deliver this event to the API, as the same event is waiting on the queue, scheduled for the far future
        await DeliverTrialEventNotificationAsync(organizationId, "anexpiredeventid3", TrialScheduledEventTrack.Expired);

        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anexpiredeventid3");
#endif
    }

    [Fact]
    public async Task
        WhenCycleThroughConversionTrialStateEventsAndDeliverThemManually_ThenSendsNotificationsAndDoesNotExpire()
    {
        // Creating new subscription (personal) organization, already starts the trial, and dispatches the first Active event 
        var login = await LoginUserAsync(LoginUser.Operator);
        var organizationId = login.DefaultOrganizationId!;

#if TESTINGONLY
        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("anactiveeventid1");

        await ConvertSubscriptionAsync(organizationId);
        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("aconvertedeventid1");

        // We manually deliver this event to the API, as the same event is waiting on the queue, scheduled for the far future
        await DeliverTrialEventNotificationAsync(organizationId, "aconvertedeventid2",
            TrialScheduledEventTrack.Converted);

        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("aconvertedeventid2");

        // We manually deliver this event to the API, as the same event is waiting on the queue, scheduled for the far future
        await DeliverTrialEventNotificationAsync(organizationId, "aconvertedeventid3",
            TrialScheduledEventTrack.Converted);

        await DeliverAllTrialEventsAsync();
        _stubBillingProvider.LastTrialEvent.Should().NotBeNull();
        _stubBillingProvider.LastTrialEvent!.Id.Should().Be("aconvertedeventid3");
#endif
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenTrialExpires_ThenSubscriptionCanceled()
    {
        var login = await LoginUserAsync(LoginUser.Operator);
        var organizationId = login.DefaultOrganizationId!;

        await DeliverAllTrialEventsAsync();

        var beforeExpiry = (await Api.GetAsync(new GetSubscriptionRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Subscription;
        beforeExpiry.Status.Should().Be(SubscriptionStatus.Activated);
        beforeExpiry.Plan.Tier.Should().Be(SubscriptionTier.Standard);
        beforeExpiry.Plan.IsTrial.Should().BeTrue();
        beforeExpiry.Plan.TrialEndDateUtc.Should().BeAfter(DateTime.UtcNow);

        await ExpireTrialAsync(organizationId);

        var afterExpiry = (await Api.GetAsync(new GetSubscriptionRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Subscription;
        afterExpiry.Status.Should().Be(SubscriptionStatus.Canceled);
        afterExpiry.CanceledDateUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        afterExpiry.Plan.Tier.Should().Be(SubscriptionTier.Unsubscribed);
        afterExpiry.Plan.Id.Should().Be(StubManagedTrialBillingGatewayService.InitialPlanId);
    }
#endif

    private async Task ExpireTrialAsync(string organizationId)
    {
#if TESTINGONLY
        _stubBillingProvider.TimeTravelPastEndOfTrial();
        var expired = await Api.PatchAsync(new ExpireSubscriptionTrialRequest
        {
            Id = organizationId
        });

        expired.StatusCode.Should().Be(HttpStatusCode.Accepted);
#endif
    }

    private async Task ConvertSubscriptionAsync(string organizationId)
    {
#if TESTINGONLY
        _stubBillingProvider.AddPaymentMethod();
        var converted = await Api.PatchAsync(new ConvertSubscriptionRequest
        {
            Id = organizationId
        });

        converted.StatusCode.Should().Be(HttpStatusCode.Accepted);
#endif
    }

    private async Task DeliverAllTrialEventsAsync()
    {
#if TESTINGONLY
        var request = new DrainAllSubscriptionTrialEventsRequest();
        await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));
#endif
    }

    private async Task DeliverTrialEventNotificationAsync(string organizationId, string eventId,
        TrialScheduledEventTrack track)
    {
        var request = new DeliverSubscriptionTrialEventRequest
        {
            Message = new SubscriptionTrialEventMessage
            {
                MessageId = "amessageid",
                TenantId = organizationId,
                CallId = "acallid",
                CallerId = "acallerid",
#if TESTINGONLY
                ProviderName = _stubBillingProvider.ProviderName,
#endif
                OwningEntityId = organizationId,
                Event = new QueuedTrialEvent
                {
                    EventId = eventId,
                    DelayInDays = 0,
                    Action = nameof(TrialScheduledEventAction.Notification),
                    Track = track.ToString(),
                    Metadata = new Dictionary<string, string>()
                }
            }.ToJson()!
        };
        var result = await Api.PostAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.Content.Value.IsSent.Should().BeTrue();
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
#if TESTINGONLY
        services.AddSingleton<IBillingProvider, StubManagedTrialBillingProvider>();
#endif
    }
}