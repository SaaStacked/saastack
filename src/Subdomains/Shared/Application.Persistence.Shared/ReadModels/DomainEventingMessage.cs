using Application.Interfaces;
using Application.Resources.Shared;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName(WorkerConstants.MessageBuses.Topics.DomainEvents)]
public class DomainEventingMessage : QueuedMessage
{
    public DomainEventNotification? Event { get; set; }
}