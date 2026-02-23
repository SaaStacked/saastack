using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Organizations.Onboarding;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using OrganizationsApplication.Persistence.ReadModels;
using OrganizationsDomain;
using OrganizationsDomain.Extensions;

namespace OrganizationsInfrastructure.Persistence.ReadModels;

public class OrganizationOnboardingProjection : IReadModelProjection
{
    private readonly IReadModelStore<Onboarding> _onboardings;

    public OrganizationOnboardingProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _onboardings = new ReadModelStore<Onboarding>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _onboardings.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.OrganizationId = e.OrganizationId;
                        dto.Status = OnboardingStatus.InProgress;
                        dto.PreviousStepId = Optional<string>.None;
                        dto.CurrentStepId = Optional<string>.None;
                    },
                    cancellationToken);

            case Completed e:
                return await _onboardings.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.CompletedBy = e.CompletedBy;
                        dto.Status = OnboardingStatus.Complete;
                    },
                    cancellationToken);

            case StepStateChanged e:
                return await _onboardings.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.CurrentStepId = e.CurrentStepId;
                        dto.AllValues = dto.AllValues.HasValue
                            ? dto.AllValues.Value.Merge(e.Values, DictionaryExtensions.MergeStrategy.Upsert)
                            : e.Values;
                    },
                    cancellationToken);

            case StepNavigated e:
                return await _onboardings.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.PreviousStepId = e.FromStepId;
                        dto.CurrentStepId = e.ToStepId;
                        dto.NavigatedById = e.NavigatedById;
                    },
                    cancellationToken);

#if TESTINGONLY
            case Deleted e:
                return await _onboardings.HandleDeleteAsync(e.RootId, cancellationToken);
#endif

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(OrganizationOnboardingRoot);
}