using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Workers.Api.Workers;

public sealed class
    DeliverSubscriptionTrialEventRelayWorker : IQueueMonitoringApiRelayWorker<SubscriptionTrialEventMessage>
{
    private readonly string _hmacSecret;
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public DeliverSubscriptionTrialEventRelayWorker(IRecorder recorder, IHostSettings settings,
        IServiceClientFactory serviceClientFactory) : this(recorder, serviceClientFactory,
        WorkerConstants.Queues.QueueDeliveryApiEndpoints[WorkerConstants.Queues.SubscriptionTrialEvents](settings))
    {
    }

    private DeliverSubscriptionTrialEventRelayWorker(IRecorder recorder, IServiceClientFactory serviceClientFactory,
        (string BaseUrl, string HmacSecret) apiEndpointSettings) : this(recorder,
        serviceClientFactory.CreateServiceClient(apiEndpointSettings.BaseUrl), apiEndpointSettings.HmacSecret)
    {
    }

    private DeliverSubscriptionTrialEventRelayWorker(IRecorder recorder, IServiceClient serviceClient,
        string hmacSecret)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _hmacSecret = hmacSecret;
    }

    public async Task RelayMessageOrThrowAsync(SubscriptionTrialEventMessage message,
        CancellationToken cancellationToken)
    {
        await _serviceClient.PostQueuedMessageToApiOrThrowAsync(_recorder,
            message, new DeliverSubscriptionTrialEventRequest
            {
                Message = message.ToJson()!
            }, _hmacSecret, cancellationToken);
    }
}