using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Api.WorkerHost.Functions;

[UsedImplicitly]
public sealed class DeliverSubscriptionTrialEvent
{
    private readonly IQueueMonitoringApiRelayWorker<SubscriptionTrialEventMessage> _worker;

    public DeliverSubscriptionTrialEvent(IQueueMonitoringApiRelayWorker<SubscriptionTrialEventMessage> worker)
    {
        _worker = worker;
    }

    [Function(nameof(DeliverSubscriptionTrialEvent))]
    public Task Run(
        [QueueTrigger(WorkerConstants.Queues.SubscriptionTrialEvents)] SubscriptionTrialEventMessage message,
        FunctionContext context)
    {
        return _worker.RelayMessageOrThrowAsync(message, context.CancellationToken);
    }
}