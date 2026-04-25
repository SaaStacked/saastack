#if TESTINGONLY
using Common;
using Common.Extensions;
using Infrastructure.External.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.External.Persistence.TestingOnly.ApplicationServices;

partial class LocalMachineJsonFileStore : IQueueStore, IQueueStoreTrigger
{
    private const string MessagePropertyName = "Message";
    private const string QueueStoreQueueContainerName = "Queues";
    private const string VisibleAfterPropertyName = "VisibleAfterUtc";

#if TESTINGONLY
    Task<Result<long, Error>> IQueueStore.CountAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter((string)nameof(queueName),
            Resources.AnyStore_MissingQueueName);

        var queueStore = EnsureContainer(GetQueueStoreContainerPath(queueName));

        return Task.FromResult<Result<long, Error>>(queueStore.Count);
    }
#endif

#if TESTINGONLY
    Task<Result<Error>> IQueueStore.DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter((string)nameof(queueName),
            Resources.AnyStore_MissingQueueName);

        var queueStore = EnsureContainer(GetQueueStoreContainerPath(queueName));
        queueStore.Erase();

        return Task.FromResult(Result.Ok);
    }
#endif
    public TimeSpan MaxMessageDelay => TimeSpan.FromDays(365);

    public async Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter((string)nameof(queueName),
            Resources.AnyStore_MissingQueueName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        var queueStore = EnsureContainer(GetQueueStoreContainerPath(queueName));
        if (queueStore.IsEmpty())
        {
            return false;
        }

        string? fifoMessageId = null;
        string? message = null;
        foreach (var candidateMessageId in queueStore.GetEntityIds().OrderBy(x => x))
        {
            var candidateMessage = await queueStore.ReadAsync(candidateMessageId, cancellationToken);
            var visibleAfterUtc = candidateMessage.TryGetValue(VisibleAfterPropertyName, out var visibleAfterUtcValue)
                                  && visibleAfterUtcValue.HasValue
                ? visibleAfterUtcValue.Value.FromIso8601().ToOptional()
                : Optional<DateTime>.None;
            if (visibleAfterUtc.HasValue
                && visibleAfterUtc.Value >= DateTime.UtcNow)
            {
                continue;
            }

            fifoMessageId = candidateMessageId;
            message = candidateMessage[MessagePropertyName];
            break;
        }

        if (fifoMessageId.HasNoValue() || message.HasNoValue())
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

        queueStore.Remove(fifoMessageId);
        return true;
    }

    public async Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        return await PushAsync(queueName, message, TimeSpan.Zero, cancellationToken);
    }

    public async Task<Result<Error>> PushAsync(string queueName, string message, TimeSpan delay,
        CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter((string)nameof(queueName),
            Resources.AnyStore_MissingQueueName);
        message.ThrowIfNotValuedParameter((string)nameof(message),
            Resources.AnyStore_MissingQueueMessage);
        delay.ThrowIfInvalidParameter(del => del >= TimeSpan.Zero, nameof(delay),
            Resources.AnyQueueStore_DelayNegative);
        delay.ThrowIfInvalidParameter(del => del <= MaxMessageDelay, nameof(delay),
            Resources.AnyQueueStore_DelayToolarge);

        var messageId = DateTime.UtcNow.Ticks.ToString();
        var visibleAfterUtc = DateTime.UtcNow.Add(delay).ToIso8601();
        var queueStore = EnsureContainer(GetQueueStoreContainerPath(queueName));
        await queueStore.WriteAsync(messageId, new Dictionary<string, Optional<string>>
        {
            { MessagePropertyName, message },
            { VisibleAfterPropertyName, visibleAfterUtc }
        }, cancellationToken);

        return Result.Ok;
    }

    public event MessageQueueUpdated? FireQueueMessage;

    private static string GetQueueStoreContainerPath(string? containerName = null, string? entityId = null)
    {
        if (entityId.HasValue())
        {
            return $"{QueueStoreQueueContainerName}/{containerName}/{entityId}";
        }

        if (containerName.HasValue())
        {
            return $"{QueueStoreQueueContainerName}/{containerName}";
        }

        return $"{QueueStoreQueueContainerName}";
    }

    private void NotifyPendingQueuedMessages()
    {
        var container = EnsureContainer(GetQueueStoreContainerPath());
        if (container.Containers().HasNone())
        {
            return;
        }

        if (FireQueueMessage.NotExists())
        {
            return;
        }

        foreach (var queue in container.Containers())
        {
            var messageCount = (int)queue.Count;
            if (messageCount > 0)
            {
                FireQueueMessage?.Invoke(this, new MessagesQueueUpdatedArgs(queue.Name, messageCount));
            }
        }
    }

    private bool TryNotifyQueuedMessage(string createdFilePath)
    {
        var container = EnsureContainer(GetQueueStoreContainerPath());
        if (container.Containers().HasNone())
        {
            return false;
        }

        var file = new FileInfo(createdFilePath);
        var queuesDirectory = file.Directory!.Parent!.FullName;
        if (queuesDirectory.HasNoValue())
        {
            return false;
        }

        if (!container.IsPath(queuesDirectory))
        {
            return false;
        }

        var queueName = file.Directory.Name;
        var queueStore = EnsureContainer(GetQueueStoreContainerPath(queueName));
        var messageCount = (int)queueStore.Count;
        if (messageCount > 0)
        {
            FireQueueMessage?.Invoke(this, new MessagesQueueUpdatedArgs(queueName, messageCount));
        }

        return true;
    }
}
#endif