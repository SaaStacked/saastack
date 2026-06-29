using Application.Persistence.Interfaces;
using Common;

namespace Application.Persistence.Common;

/// <summary>
///     Provides a read model for snapshotted aggregates
/// </summary>
public class SnapshottedReadModelEntity : IReadModelEntity, ISnapshottedDto
{
    public Optional<string> Id { get; set; }

    public Optional<bool> IsDeleted { get; set; }

    public Optional<DateTime> LastPersistedAt { get; set; }

    public Optional<DateTime> CreatedAt { get; set; }

    public Optional<DateTime> LastModifiedAt { get; set; }
}