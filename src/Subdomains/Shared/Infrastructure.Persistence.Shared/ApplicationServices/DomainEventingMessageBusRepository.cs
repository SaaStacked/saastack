using Application.Interfaces;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

public class DomainEventingMessageBusRepository : IDomainEventingMessageBusRepository
{
    private readonly MessageBusTopicStore<DomainEventingMessage> _busTopic;

    public DomainEventingMessageBusRepository(IRecorder recorder,
        IMessageBusTopicMessageIdFactory messageBusTopicMessageIdFactory,
        IMessageBusStore store)
    {
        _busTopic = new MessageBusTopicStore<DomainEventingMessage>(recorder,
            EventingConstants.Topics.DomainEvents, messageBusTopicMessageIdFactory, store);
    }

#if TESTINGONLY
    public Task<Result<long, Error>> CountAsync(string subscriptionName, CancellationToken cancellationToken)
    {
        return _busTopic.CountAsync(subscriptionName, cancellationToken);
    }
#endif

#if TESTINGONLY
    public Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return _busTopic.DestroyAllAsync(cancellationToken);
    }
#endif

#if TESTINGONLY
    Task<Result<Error>> IApplicationRepository.DestroyAllAsync(CancellationToken cancellationToken)
    {
        return DestroyAllAsync(cancellationToken);
    }
#endif

#if TESTINGONLY
    public Task<Result<bool, Error>> ReceiveSingleAsync(string subscriptionName,
        Func<DomainEventingMessage, CancellationToken, Task<Result<Error>>> onMessageReceivedAsync,
        CancellationToken cancellationToken)
    {
        return _busTopic.ReceiveSingleAsync(subscriptionName, onMessageReceivedAsync, cancellationToken);
    }
#endif

    public Task<Result<DomainEventingMessage, Error>> SendAsync(ICallContext call, DomainEventingMessage message,
        CancellationToken cancellationToken)
    {
        return _busTopic.SendAsync(call, message, cancellationToken);
    }
}