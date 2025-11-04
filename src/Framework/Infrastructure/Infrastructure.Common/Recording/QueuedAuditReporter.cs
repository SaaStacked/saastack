using Application.Interfaces.Services;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Provides an <see cref="IAuditReporter" /> that asynchronously brokers the audit to a reliable queue for future
///     delivery
/// </summary>
public class QueuedAuditReporter : IAuditReporter
{
    private readonly IHostSettings _hostSettings;
    private readonly IAuditMessageQueueRepository _repository;

    public QueuedAuditReporter(IHostSettings hostSettings, IMessageQueueMessageIdFactory messageIdFactory,
        IQueueStore queueStore)
        : this(new AuditMessageQueueRepository(NoOpRecorder.Instance, hostSettings, messageIdFactory, queueStore),
            hostSettings)
    {
    }

    private QueuedAuditReporter(IAuditMessageQueueRepository repository, IHostSettings hostSettings)
    {
        _repository = repository;
        _hostSettings = hostSettings;
    }

    public void Audit(ICallContext? call, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
        ArgumentException.ThrowIfNullOrEmpty(againstId);
        ArgumentException.ThrowIfNullOrEmpty(auditCode);

        var region = _hostSettings.GetRegion();
        var safeCall = call ?? CallContext.CreateUnknown(region);
        var message = new AuditMessage
        {
            AuditCode = auditCode,
            AgainstId = againstId,
            MessageTemplate = messageTemplate,
            Arguments = templateArgs.HasAny()
                ? templateArgs.Select(arg => arg.ToString()!)
                    .ToList()
                : new List<string>()
        };

        _repository.PushAsync(safeCall, message, CancellationToken.None).GetAwaiter().GetResult();
    }
}