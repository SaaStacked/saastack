using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Ancillary.Audits;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace AncillaryDomain;

public sealed class AuditRoot : AggregateRootBase
{
    public static Result<AuditRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier againstId, Optional<Identifier> organizationId, string auditCode, Optional<string> messageTemplate,
        TemplateArguments templateArguments, DatacenterLocation hostRegion)
    {
        var root = new AuditRoot(recorder, idFactory);
        root.RaiseCreateEvent(AncillaryDomain.Events.Audits.Created(root.Id, againstId, organizationId,
            auditCode, messageTemplate, templateArguments, hostRegion));
        return root;
    }

    private AuditRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AuditRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Optional<Identifier> AgainstId { get; private set; }

    public Optional<string> AuditCode { get; private set; }

    public Optional<string> MessageTemplate { get; private set; }

    public Optional<Identifier> OrganizationId { get; private set; }

    public Optional<TemplateArguments> TemplateArguments { get; private set; }

    public DatacenterLocation HostRegion { get; private set; } = DatacenterLocations.Unknown;

    [UsedImplicitly]
    public static AggregateRootFactory<AuditRoot> Rehydrate()
    {
        return (identifier, container, _) => new AuditRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OrganizationId = created.OrganizationId.HasValue()
                    ? created.OrganizationId.ToId()
                    : Optional<Identifier>.None;
                AgainstId = created.AgainstId.ToId();
                AuditCode = created.AuditCode;
                MessageTemplate = Optional<string>.Some(created.MessageTemplate);
                TemplateArguments = AncillaryDomain.TemplateArguments.Create(created.TemplateArguments).Value;
                HostRegion = DatacenterLocations.FindOrDefault(created.HostRegion);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }
}