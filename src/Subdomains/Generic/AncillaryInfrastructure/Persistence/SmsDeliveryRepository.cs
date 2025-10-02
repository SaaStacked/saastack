using AncillaryApplication.Persistence;
using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Tasks = Common.Extensions.Tasks;

namespace AncillaryInfrastructure.Persistence;

public class SmsDeliveryRepository : ISmsDeliveryRepository
{
    private readonly IEventSourcingDddCommandStore<SmsDeliveryRoot> _deliveries;
    private readonly ISnapshottingQueryStore<SmsDelivery> _deliveryQueries;

    public SmsDeliveryRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<SmsDeliveryRoot> deliveriesStore, IDataStore store)
    {
        _deliveryQueries = new SnapshottingQueryStore<SmsDelivery>(recorder, domainFactory, store);
        _deliveries = deliveriesStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _deliveryQueries.DestroyAllAsync(cancellationToken),
            _deliveries.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<SmsDeliveryRoot>, Error>> FindByMessageIdAsync(
        QueuedMessageId messageId, CancellationToken cancellationToken)
    {
        var query = Query.From<SmsDelivery>()
            .Where<string>(at => at.MessageId, ConditionOperator.EqualTo, messageId);

        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<Optional<SmsDeliveryRoot>, Error>> FindByReceiptIdAsync(string receiptId,
        CancellationToken cancellationToken)
    {
        var query = Query.From<SmsDelivery>()
            .Where<string>(at => at.ReceiptId, ConditionOperator.EqualTo, receiptId);

        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<SmsDeliveryRoot, Error>> SaveAsync(SmsDeliveryRoot delivery, bool reload,
        CancellationToken cancellationToken)
    {
        var saved = await _deliveries.SaveAsync(delivery, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(delivery.Id, cancellationToken)
            : delivery;
    }

    public async Task<Result<QueryResults<SmsDelivery>, Error>> SearchAllAsync(DateTime? sinceUtc,
        string? organizationId, IReadOnlyList<string>? tags, SearchOptions searchOptions,
        CancellationToken cancellationToken)
    {
        var query = Query.From<SmsDelivery>().WhereNoOp();
        if (sinceUtc.HasValue)
        {
            query = query.AndWhere<DateTime?>(sd => sd.Created, ConditionOperator.GreaterThan, sinceUtc);
        }

        if (organizationId.HasValue())
        {
            query = query.AndWhere<string?>(sd => sd.OrganizationId, ConditionOperator.EqualTo, organizationId);
        }

        if (tags.Exists() && tags.HasAny())
        {
            foreach (var tag in tags)
            {
                query = query.AndWhere<string>(sd => sd.Tags, ConditionOperator.Like, tag);
            }
        }

        query = query.WithSearchOptions(searchOptions);

        var queried = await _deliveryQueries.QueryAsync(query, cancellationToken: cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        return queried.Value;
    }

    private async Task<Result<SmsDeliveryRoot, Error>> LoadAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var delivery = await _deliveries.LoadAsync(id, cancellationToken);
        if (delivery.IsFailure)
        {
            return delivery.Error;
        }

        return delivery.Value;
    }

    private async Task<Result<Optional<SmsDeliveryRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<SmsDelivery> query,
        CancellationToken cancellationToken)
    {
        var queried = await _deliveryQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<SmsDeliveryRoot>.None;
        }

        var deliveries = await _deliveries.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (deliveries.IsFailure)
        {
            return deliveries.Error;
        }

        return deliveries.Value.ToOptional();
    }
}