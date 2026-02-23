using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using OrganizationsApplication.Persistence;
using OrganizationsApplication.Persistence.ReadModels;

namespace OrganizationsInfrastructure.Persistence;

public class OnboardingCustomWorkflowRepository : IOnboardingCustomWorkflowRepository
{
    private readonly IReadModelStore<OnboardingCustomWorkflow> _schemas;

    public OnboardingCustomWorkflowRepository(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _schemas = new ReadModelStore<OnboardingCustomWorkflow>(recorder, domainFactory, store);
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await _schemas.DestroyAllAsync(cancellationToken);
    }
#endif

    public async Task<Result<OnboardingCustomWorkflow, Error>> LoadAsync(Identifier organizationId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _schemas.GetAsync(organizationId, true, false, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        return retrieved.Value.Value;
    }

    public async Task<Result<OnboardingCustomWorkflow, Error>> SaveAsync(OnboardingCustomWorkflow customWorkflow,
        CancellationToken cancellationToken)
    {
        var upserted = await _schemas.UpsertAsync(customWorkflow, false, cancellationToken);
        if (upserted.IsFailure)
        {
            return upserted.Error;
        }

        return upserted.Value;
    }
}