using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace EventNotificationsInfrastructure.IntegrationTests.Stubs;

public class StubDomainEventingConsumerService : IDomainEventingConsumerService
{
    public string? LastEventId { get; private set; }

    public string? LastEventSubscriptionName { get; private set; }

    public async Task<Result<Error>> NotifySubscriberAsync(string subscriptionName,
        DomainEventNotification domainEventNotification,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        LastEventId = domainEventNotification.Id;
        LastEventSubscriptionName = subscriptionName;
        return Result.Ok;
    }

    public IReadOnlyList<string> SubscriptionNames => [];

    public void Reset()
    {
        LastEventId = null;
        LastEventSubscriptionName = null;
    }
}