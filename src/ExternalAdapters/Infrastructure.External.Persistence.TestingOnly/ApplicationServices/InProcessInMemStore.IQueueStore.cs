#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces;
using Infrastructure.External.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.External.Persistence.TestingOnly.ApplicationServices;

partial class InProcessInMemStore : IQueueStore, IQueueStoreTrigger
{
    private const string MessagePropertyName = "Message";
    private const string VisibleAfterPropertyName = "VisibleAfterUtc";
    private readonly Dictionary<string, Dictionary<string, HydrationProperties>> _queues = new();

#if TESTINGONLY
    Task<Result<long, Error>> IQueueStore.CountAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.AnyStore_MissingQueueName);

        if (_queues.TryGetValue(queueName, out var value))
        {
            return Task.FromResult<Result<long, Error>>(value.Count);
        }

        return Task.FromResult<Result<long, Error>>(0);
    }
#endif

#if TESTINGONLY
    Task<Result<Error>> IQueueStore.DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.AnyStore_MissingQueueName);

        if (_queues.ContainsKey(queueName))
        {
            _queues.Remove(queueName);
        }

        return Task.FromResult(Result.Ok);
    }
#endif

    public TimeSpan MaxMessageDelay => TimeSpan.FromDays(365);

    public async Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.AnyStore_MissingQueueName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        if (!_queues.ContainsKey(queueName)
            || _queues[queueName].HasNone())
        {
            return false;
        }

        KeyValuePair<string, HydrationProperties>? fifoMessage = null;
        string? message = null;
        foreach (var candidateMessage in _queues[queueName].OrderBy(x => x.Key))
        {
            var visibleAfterUtc = candidateMessage.Value.GetValueOrDefault<string>(VisibleAfterPropertyName)
                .ToOptional(value => value.FromIso8601());
            if (visibleAfterUtc.HasValue
                && visibleAfterUtc.Value >= DateTime.UtcNow)
            {
                continue;
            }

            fifoMessage = candidateMessage;
            message = candidateMessage.Value[MessagePropertyName].ToString();
            break;
        }

        if (!fifoMessage.HasValue || message.HasNoValue())
        {
            return false;
        }

        try
        {
            var handled = await messageHandlerAsync(message, cancellationToken);
            if (handled.IsFailure)
            {
                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            return ex.ToError(ErrorCode.Unexpected);
        }

        _queues[queueName].Remove(fifoMessage.Value.Key);
        return true;
    }

    public Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        return PushAsync(queueName, message, TimeSpan.Zero, cancellationToken);
    }

    public Task<Result<Error>> PushAsync(string queueName, string message, TimeSpan delay,
        CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.AnyStore_MissingQueueName);
        message.ThrowIfNotValuedParameter(nameof(message), Resources.AnyStore_MissingQueueMessage);
        delay.ThrowIfInvalidParameter(del => del >= TimeSpan.Zero, nameof(delay),
            Resources.AnyQueueStore_DelayNegative);
        delay.ThrowIfInvalidParameter(del => del <= MaxMessageDelay, nameof(delay),
            Resources.AnyQueueStore_DelayToolarge);

        if (!_queues.ContainsKey(queueName))
        {
            _queues.Add(queueName, new Dictionary<string, HydrationProperties>());
        }

        var messageId = DateTime.UtcNow.Ticks.ToString();
        _queues[queueName]
            .Add(messageId, new HydrationProperties
            {
                { MessagePropertyName, message },
                { VisibleAfterPropertyName, DateTime.UtcNow.Add(delay).ToIso8601() }
            });

        FireQueueMessage?.Invoke(this, new MessagesQueueUpdatedArgs(queueName, _queues[queueName].Count));

        return Task.FromResult(Result.Ok);
    }

    public event MessageQueueUpdated? FireQueueMessage;

    private void NotifyPendingQueuedMessages()
    {
        if (_queues.HasNone() || FireQueueMessage.NotExists())
        {
            return;
        }

        foreach (var (queueName, messages) in _queues)
        {
            var messageCount = messages.Count;
            if (messageCount > 0)
            {
                FireQueueMessage?.Invoke(this, new MessagesQueueUpdatedArgs(queueName, messageCount));
            }
        }
    }
}
#endif