using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using OrganizationsApplication.Persistence;
using OrganizationsApplication.Persistence.ReadModels;
using OrganizationsDomain;
using QueryAny;

namespace OrganizationsInfrastructure.Persistence;

public class OnboardingRepository : IOnboardingRepository
{
    private readonly ISnapshottingQueryStore<Onboarding> _onboardingQueries;
    private readonly IEventSourcingDddCommandStore<OrganizationOnboardingRoot> _onboardings;

    public OnboardingRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<OrganizationOnboardingRoot> onboardingsStore, IDataStore store)
    {
        _onboardingQueries = new SnapshottingQueryStore<Onboarding>(recorder, domainFactory, store);
        _onboardings = onboardingsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _onboardingQueries.DestroyAllAsync(cancellationToken),
            _onboardings.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<OrganizationOnboardingRoot>, Error>> FindByOrganizationIdAsync(
        Identifier organizationId, CancellationToken cancellationToken)
    {
        var query = Query.From<Onboarding>()
            .Where<string>(at => at.OrganizationId, ConditionOperator.EqualTo, organizationId);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<OrganizationOnboardingRoot, Error>> LoadAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var organization = await _onboardings.LoadAsync(id, cancellationToken);
        if (organization.IsFailure)
        {
            return organization.Error;
        }

        return organization;
    }

    public async Task<Result<OrganizationOnboardingRoot, Error>> SaveAsync(OrganizationOnboardingRoot onboarding,
        CancellationToken cancellationToken)
    {
        return await SaveAsync(onboarding, false, cancellationToken);
    }

    public async Task<Result<OrganizationOnboardingRoot, Error>> SaveAsync(OrganizationOnboardingRoot onboarding,
        bool reload, CancellationToken cancellationToken)
    {
        var saved = await _onboardings.SaveAsync(onboarding, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(onboarding.Id, cancellationToken)
            : onboarding;
    }

    private async Task<Result<Optional<OrganizationOnboardingRoot>, Error>> FindFirstByQueryAsync(
        QueryClause<Onboarding> query,
        CancellationToken cancellationToken)
    {
        var queried = await _onboardingQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<OrganizationOnboardingRoot>.None;
        }

        var onboardings = await _onboardings.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (onboardings.IsFailure)
        {
            return onboardings.Error;
        }

        return onboardings.Value.ToOptional();
    }
}