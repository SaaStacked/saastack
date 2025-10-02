using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to messages on a FIFO queue
/// </summary>
public sealed class MessageQueueStore<TMessage> : IMessageQueueStore<TMessage>
    where TMessage : IQueuedMessage, new()
{
    private readonly IHostSettings _hostSettings;
    private readonly IMessageQueueMessageIdFactory _messageQueueMessageIdFactory;
    private readonly string _queueName;
    private readonly IQueueStore _queueStore;
    private readonly IRecorder _recorder;

    public MessageQueueStore(IRecorder recorder, IHostSettings hostSettings,
        IMessageQueueMessageIdFactory messageQueueMessageIdFactory,
        IQueueStore queueStore)
    {
        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _hostSettings = hostSettings;
        _messageQueueMessageIdFactory = messageQueueMessageIdFactory;
        _queueStore = queueStore;
        _queueName = typeof(TMessage).GetEntityNameSafe();
    }

    public Guid InstanceId { get; }

#if TESTINGONLY
    public async Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken)
    {
        return await _queueStore.CountAsync(_queueName, cancellationToken);
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _queueStore.DestroyAllAsync(_queueName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All messages were deleted from the queue {Queue} in the {Store} store",
                _queueName, _queueStore.GetType().Name);
        }

        return deleted;
    }
#endif

    public async Task<Result<bool, Error>> PopSingleAsync(
        Func<TMessage, CancellationToken, Task<Result<Error>>> onMessageReceivedAsync,
        CancellationToken cancellationToken)
    {
        TMessage message;
        return await _queueStore.PopSingleAsync(_queueName, async (messageAsText, cancellation) =>
        {
            if (messageAsText.HasValue())
            {
                message = messageAsText.FromJson<TMessage>()!;
                var handled = await onMessageReceivedAsync(message, cancellation);
                if (handled.IsFailure)
                {
                    return handled.Error;
                }

                _recorder.TraceDebug(null, "Message {Text} was removed from the queue {Queue} in the {Store} store",
                    messageAsText,
                    _queueName, _queueStore.GetType().Name);
            }

            return Result.Ok;
        }, cancellationToken);
    }

    public async Task<Result<TMessage, Error>> PushAsync(ICallContext call, TMessage message,
        CancellationToken cancellationToken)
    {
        message.TenantId = message.TenantId.HasValue()
            ? message.TenantId
            : call.TenantId;
        message.CallId = message.CallId.HasValue()
            ? message.CallId
            : call.CallId;
        message.CallerId = message.CallerId.HasValue()
            ? message.CallerId
            : call.CallerId;
        var messageId = message.MessageId ?? CreateMessageId();
        message.MessageId = messageId;
        var region = message.OriginHostRegion ?? _hostSettings.GetRegion().Code;
        message.OriginHostRegion = region;
        var messageJson = message.ToJson()!;

        var pushed = await _queueStore.PushAsync(_queueName, messageJson, cancellationToken);
        if (pushed.IsFailure)
        {
            return pushed.Error;
        }

        _recorder.TraceDebug(null,
            "Message {Message} was added to the queue {Queue} (in {Region}) by the {Store} store",
            messageJson,
            _queueName, region, _queueStore.GetType().Name);

        return message;
    }

    private string CreateMessageId()
    {
        return _messageQueueMessageIdFactory.Create(_queueName);
    }
}