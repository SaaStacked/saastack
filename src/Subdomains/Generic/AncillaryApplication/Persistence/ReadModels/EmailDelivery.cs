using AncillaryDomain;
using Application.Persistence.Common;
using Common;
using Domain.Shared.Ancillary;
using QueryAny;

namespace AncillaryApplication.Persistence.ReadModels;

[EntityName("EmailDelivery")]
public class EmailDelivery : ReadModelEntity
{
    public Optional<SendingAttempts> Attempts { get; set; }

    public Optional<string> Body { get; set; }

    public DeliveredEmailContentType ContentType { get; set; }

    public Optional<DateTime?> CreatedAt { get; set; }

    public Optional<DateTime?> DeliveredAt { get; set; }

    public Optional<DateTime?> DeliveryFailedAt { get; set; }

    public Optional<string> DeliveryFailedReason { get; set; }

    public Optional<DateTime?> LastAttemptedAt { get; set; }

    public Optional<string> MessageId { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public Optional<string> ReceiptId { get; set; }

    public Optional<string> RegisteredRegion { get; set; }

    public Optional<DateTime?> SendFailedAt { get; set; }

    public Optional<DateTime?> SentAt { get; set; }

    public Optional<string> Subject { get; set; }

    public Optional<string> Substitutions { get; set; }

    public Optional<string> Tags { get; set; }

    public Optional<string> TemplateId { get; set; }

    public Optional<string> ToDisplayName { get; set; }

    public Optional<string> ToEmailAddress { get; set; }
}