using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Common.Recording;

namespace Application.Persistence.Shared.Extensions;

public static class QueueMessageHandlerExtensions
{
    public static async Task DrainAllQueuedMessagesAsync<TQueuedMessage>(
        this IMessageQueueStore<TQueuedMessage> repository, IRecorder recorder,
        Func<TQueuedMessage, Task<Result<bool, Error>>> handler, CancellationToken cancellationToken)
        where TQueuedMessage : IQueuedMessage, new()
    {
        var found = new Result<bool, Error>(true);
        while (found is { HasValue: true, Value: true })
        {
            try
            {
                found = await repository.PopSingleAsync(OnMessageReceivedAsync, cancellationToken);
            }
            catch (Exception ex)
            {
                recorder.TraceError(null, ex, "Failed to receive message from queue. Error was: {Error}", ex.Message);
                throw;
            }
            continue;

            async Task<Result<Error>> OnMessageReceivedAsync(TQueuedMessage message, CancellationToken _)
            {
                var handled = await handler(message);
                if (handled.IsFailure)
                {
                    handled.Error.Throw<InvalidOperationException>();
                }

                return Result.Ok;
            }
        }

        if (found.IsFailure)
        {
            recorder.Crash(null, CrashLevel.NonCritical, found.Error.ToException<InvalidOperationException>(),
                "Failed to receive message from queue.");
        }
    }

    public static Result<TQueuedMessage, Error> RehydrateQueuedMessage<TQueuedMessage>(this string messageAsJson)
        where TQueuedMessage : IQueuedMessage
    {
        try
        {
            var message = messageAsJson.FromJson<TQueuedMessage>();
            if (message.NotExists())
            {
                return Error.RuleViolation(
                    Resources.QueueMessageHandlerExtensions_InvalidQueuedMessage.Format(typeof(TQueuedMessage).Name,
                        messageAsJson));
            }

            return message;
        }
        catch (Exception)
        {
            return Error.RuleViolation(
                Resources.QueueMessageHandlerExtensions_InvalidQueuedMessage.Format(typeof(TQueuedMessage).Name,
                    messageAsJson));
        }
    }
}