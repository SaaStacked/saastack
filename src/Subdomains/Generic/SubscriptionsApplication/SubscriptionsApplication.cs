using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Shared;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Shared;
using Domain.Shared.Subscriptions;
using SubscriptionsApplication.Persistence;
using SubscriptionsDomain;
using Subscription = SubscriptionsApplication.Persistence.ReadModels.Subscription;
using Validations = SubscriptionsDomain.Validations;

namespace SubscriptionsApplication;

public partial class SubscriptionsApplication : ISubscriptionsApplication
{
    private readonly IBillingProvider _billingProvider;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IRecorder _recorder;
    private readonly ISubscriptionRepository _repository;
    private readonly ISubscriptionOwningEntityService _subscriptionOwningEntityService;
    private readonly IUserProfilesService _userProfilesService;

    public SubscriptionsApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        IUserProfilesService userProfilesService, IBillingProvider billingProvider,
        ISubscriptionOwningEntityService subscriptionOwningEntityService,
        ISubscriptionTrialEventMessageQueueRepository trialEventMessageRepository,
        ISubscriptionRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _userProfilesService = userProfilesService;
        _billingProvider = billingProvider;
        _subscriptionOwningEntityService = subscriptionOwningEntityService;
        _trialEventMessageRepository = trialEventMessageRepository;
        _repository = repository;
    }

    public async Task<Result<SubscriptionWithPlan, Error>> CancelSubscriptionAsync(ICallerContext caller,
        string owningEntityId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await CancelSubscriptionInternalAsync(caller, subscription, CancelSubscriptionOptions.EndOfTerm,
            cancellationToken);
    }

    public async Task<Result<SubscriptionWithPlan, Error>> ChangePlanAsync(ICallerContext caller, string owningEntityId,
        string planId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await ChangeSubscriptionPlanInternalAsync(caller, subscription, planId, cancellationToken);
    }

#if TESTINGONLY
    public async Task<Result<SubscriptionWithPlan, Error>> ConvertSubscriptionAsync(ICallerContext caller,
        string owningEntityId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var providerSubscription =
            await subscription.ForceConvertSubscriptionAsync(_billingProvider.StateInterpreter, OnDispatchTrialEvent);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.SubscriptionConverted,
            subscription.ToSubscriptionChangedUsageEvent());

        return subscription.ToSubscription(providerSubscription.Value);

        async Task<Result<Error>> OnDispatchTrialEvent(SubscriptionRoot root, TrialScheduledEvent next,
            DateTime relativeTo)
        {
            return await OnDispatchManagedTrialEventAsync(caller, root, next, relativeTo, cancellationToken);
        }
    }
#endif

#if TESTINGONLY
    public async Task<Result<SubscriptionWithPlan, Error>> ExpireTrialAsync(ICallerContext caller,
        string owningEntityId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var providerSubscription =
            await subscription.ForceExpireManagedTrialAsync(_billingProvider.StateInterpreter, OnExpired,
                OnDispatchTrialEvent);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.SubscriptionManagedTrialExpired,
            subscription.ToSubscriptionChangedUsageEvent());

        return subscription.ToSubscription(providerSubscription.Value);

        async Task<Result<Error>> OnExpired(SubscriptionRoot root)
        {
            return await OnExpireManagedTrialAsync(caller, root, cancellationToken);
        }

        async Task<Result<Error>> OnDispatchTrialEvent(SubscriptionRoot root, TrialScheduledEvent next,
            DateTime relativeTo)
        {
            return await OnDispatchManagedTrialEventAsync(caller, root, next, relativeTo, cancellationToken);
        }
    }
#endif

    public async Task<Result<SearchResults<SubscriptionToMigrate>, Error>> ExportSubscriptionsToMigrateAsync(
        ICallerContext caller, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var searched =
            await _repository.SearchAllByProviderAsync(_billingProvider.StateInterpreter.ProviderName,
                searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var subscriptions = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All subscriptions were fetched");

        return await subscriptions.ToSearchResultsAsync(searchOptions, async subscription =>
        {
            var owningEntity = await _subscriptionOwningEntityService.GetEntityAsync(caller,
                subscription.OwningEntityId,
                cancellationToken);
            if (owningEntity.IsFailure)
            {
                return subscription.ToSubscriptionForMigration(null);
            }

            var buyer = CreateBuyerAsync(caller, subscription.BuyerId.Value.ToId(),
                    owningEntity.Value, cancellationToken)
                .GetAwaiter().GetResult();
            if (buyer.IsFailure)
            {
                return subscription.ToSubscriptionForMigration(null);
            }

            return subscription.ToSubscriptionForMigration(buyer.Value);
        });
    }

    public async Task<Result<SubscriptionWithPlan, Error>> ForceCancelSubscriptionAsync(ICallerContext caller,
        string owningEntityId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await CancelSubscriptionInternalAsync(caller, subscription, CancelSubscriptionOptions.Immediately,
            cancellationToken);
    }

    public async Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionByIdAsync(ICallerContext caller,
        string id, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await GetSubscriptionInternalAsync(caller, subscription, cancellationToken);
    }

    public async Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionByOwningEntityIdAsync(ICallerContext caller,
        string owningEntityId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        return await GetSubscriptionInternalAsync(caller, subscription, cancellationToken);
    }

    public async Task<Result<Error>> IncrementSubscriptionUsageAsync(ICallerContext caller, string owningEntityId,
        string eventName, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var incremented =
            await subscription.IncrementUsageAsync(_billingProvider.StateInterpreter, eventName, OnIncrement);
        if (incremented.IsFailure)
        {
            return incremented.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Incremented usage for {Subscription} for event {EventName}",
            subscription.Id, eventName);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.SubscriptionMeteredUsageIncremented,
            subscription.ToSubscriptionChangedUsageEvent()
                .With(UsageConstants.Properties.BillingMeterName, eventName));
        return Result.Ok;

        async Task<Result<SubscriptionMetadata, Error>> OnIncrement(SubscriptionRoot root)
        {
            return await _billingProvider.GatewayService.IncrementMeterAsync(caller, eventName, root.Provider,
                cancellationToken);
        }
    }

    public async Task<Result<PricingPlans, Error>> ListPricingPlansAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var plans = await _billingProvider.GatewayService.ListAllPricingPlansAsync(caller, cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Retrieved all pricing plans");

        return plans;
    }

    public async Task<Result<SubscriptionWithPlan, Error>> MigrateSubscriptionAsync(ICallerContext caller,
        string? owningEntityId, string providerName, Dictionary<string, string> providerState,
        CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var provider = BillingProvider.Create(providerName, new SubscriptionMetadata(providerState));
        if (provider.IsFailure)
        {
            return provider.Error;
        }

        var providerBefore = subscription.Provider.Value.Name;
        var modifierId = caller.ToCallerId();
        var changed = subscription.ChangeProvider(provider.Value, modifierId, _billingProvider.StateInterpreter);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        var providerAfter = subscription.Provider.Value.Name;
        _recorder.TraceInformation(caller.ToCall(),
            "Subscription {Id} has changed its provider from: {ProviderBefore}, to: {ProviderAfter}", subscription.Id,
            providerBefore, providerAfter);

        var providerSubscription = _billingProvider.StateInterpreter.GetSubscriptionDetails(subscription.Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return subscription.ToSubscription(providerSubscription.Value);
    }

    public async Task<Result<SearchResults<Invoice>, Error>> SearchSubscriptionHistoryAsync(ICallerContext caller,
        string owningEntityId, DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var (from, to) = CalculatedSearchRange(fromUtc, toUtc);
        var searched = await _billingProvider.GatewayService.SearchAllInvoicesAsync(caller, subscription.Provider,
            from, to, searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved subscription invoices for: {OwningEntity}",
            owningEntityId);

        return searched;
    }

    public async Task<Result<SubscriptionWithPlan, Error>> TransferSubscriptionAsync(ICallerContext caller,
        string owningEntityId, string billingAdminId, CancellationToken cancellationToken)
    {
        var retrieved = await GetSubscriptionByOwningEntityAsync(owningEntityId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var subscription = retrieved.Value;
        var transfererId = caller.ToCallerId();
        var transfereeId = billingAdminId.ToId();
        var beforeBuyerId = subscription.BuyerId;
        var transferred = await subscription.TransferSubscriptionAsync(_billingProvider.StateInterpreter, transfererId,
            transfereeId, CanTransfer, OnTransfer);
        if (transferred.IsFailure)
        {
            return transferred.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        var afterBuyerId = subscription.BuyerId;
        _recorder.TraceInformation(caller.ToCall(),
            "Transferred subscription: {Id} for entity: {OwningEntity}, from {BeforeBuyer}, to: {AfterBuyer}",
            subscription.Id, subscription.OwningEntityId, beforeBuyerId, afterBuyerId);
        _recorder.AuditAgainst(caller.ToCall(), transfererId, Audits.SubscriptionsApplication_BuyerTransferred,
            "EndUser {TransfererId} transferred subscription {Id} to: {Buyer}", transfererId, subscription.Id,
            afterBuyerId);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.SubscriptionTransfered,
            subscription.ToSubscriptionChangedUsageEvent());

        var providerSubscription = _billingProvider.StateInterpreter.GetSubscriptionDetails(subscription.Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return subscription.ToSubscription(providerSubscription.Value);

        async Task<Permission> CanTransfer(SubscriptionRoot root, Identifier transfererId1,
            Identifier transfereeId1)
        {
            return (await _subscriptionOwningEntityService.CanTransferSubscriptionAsync(caller,
                    root.OwningEntityId, transfererId1, transfereeId1, cancellationToken))
                .Match(optional => optional.Value, Permission.Denied_Evaluating);
        }

        async Task<Result<SubscriptionMetadata, Error>> OnTransfer(SubscriptionRoot root, Identifier transfereeId1)
        {
            var owningEntity =
                await _subscriptionOwningEntityService.GetEntityAsync(caller, root.OwningEntityId, cancellationToken);
            if (owningEntity.IsFailure)
            {
                return owningEntity.Error;
            }

            var maintenance = Caller.CreateAsMaintenance(caller);
            var transferee = CreateBuyerAsync(maintenance, transfereeId1, owningEntity.Value,
                cancellationToken);
            if (transferee.Result.IsFailure)
            {
                return transferee.Result.Error;
            }

            return await _billingProvider.GatewayService.TransferSubscriptionAsync(caller,
                new TransferSubscriptionOptions
                {
                    TransfereeBuyer = transferee.Result.Value
                }, root.Provider, cancellationToken);
        }
    }

    private async Task<Result<SubscriptionWithPlan, Error>> ChangeSubscriptionPlanInternalAsync(ICallerContext caller,
        SubscriptionRoot subscription, string planId, CancellationToken cancellationToken)
    {
        var buyerIdBeforeChange = subscription.BuyerId;
        var modifierId = caller.ToCallerId();
        var changed = await subscription.ChangePlanAsync(_billingProvider.StateInterpreter, modifierId, planId,
            CanChange, OnChange, OnTransfer);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        var buyerIdAfterChange = subscription.BuyerId;
        if (buyerIdAfterChange != buyerIdBeforeChange)
        {
            _recorder.TraceInformation(caller.ToCall(),
                "Subscription {Id} has been transferred from {FromBuyer} to {ToBuyer}", subscription.Id,
                buyerIdBeforeChange, buyerIdAfterChange);
            _recorder.TrackUsage(caller.ToCall(),
                UsageConstants.Events.UsageScenarios.Generic.SubscriptionTransfered,
                subscription.ToSubscriptionChangedUsageEvent());
        }
        else
        {
            _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} changed its plan: {Plan}",
                subscription.Id, planId);
            _recorder.TrackUsage(caller.ToCall(),
                UsageConstants.Events.UsageScenarios.Generic.SubscriptionPlanChanged,
                subscription.ToSubscriptionChangedUsageEvent());
        }

        var providerSubscription = _billingProvider.StateInterpreter.GetSubscriptionDetails(subscription.Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return subscription.ToSubscription(providerSubscription.Value);

        async Task<Result<SubscriptionMetadata, Error>> OnChange(SubscriptionRoot root, string planId1)
        {
            var owningEntity =
                await _subscriptionOwningEntityService.GetEntityAsync(caller, root.OwningEntityId, cancellationToken);
            if (owningEntity.IsFailure)
            {
                return owningEntity.Error;
            }

            var options = new ChangePlanOptions
            {
                PlanId = planId1,
                Subscriber = new Subscriber
                {
                    EntityId = owningEntity.Value.Id,
                    EntityType = owningEntity.Value.Type,
                    EntityName = owningEntity.Value.Name
                }
            };

            var planChanged = _billingProvider.GatewayService.ChangeSubscriptionPlanAsync(caller, options,
                root.Provider.Value, cancellationToken);
            return planChanged.Result;
        }

        async Task<Permission> CanChange(SubscriptionRoot root, Identifier modifierId1)
        {
            return (await _subscriptionOwningEntityService.CanChangeSubscriptionPlanAsync(caller,
                    root.OwningEntityId,
                    modifierId1, cancellationToken))
                .Match(optional => optional.Value, Permission.Denied_Evaluating);
        }

        async Task<Result<SubscriptionMetadata, Error>> OnTransfer(SubscriptionRoot root, Identifier transfereeId)
        {
            var owningEntity =
                await _subscriptionOwningEntityService.GetEntityAsync(caller, root.OwningEntityId, cancellationToken);
            if (owningEntity.IsFailure)
            {
                return owningEntity.Error;
            }

            var transferee = CreateBuyerAsync(caller, transfereeId, owningEntity.Value, cancellationToken);
            if (transferee.Result.IsFailure)
            {
                return transferee.Result.Error;
            }

            return await _billingProvider.GatewayService.TransferSubscriptionAsync(caller,
                new TransferSubscriptionOptions
                {
                    TransfereeBuyer = transferee.Result.Value,
                    PlanId = planId
                }, root.Provider, cancellationToken);
        }
    }

    private async Task<Result<SubscriptionWithPlan, Error>> CancelSubscriptionInternalAsync(ICallerContext caller,
        SubscriptionRoot subscription, CancelSubscriptionOptions options, CancellationToken cancellationToken)
    {
        var cancellerId = caller.ToCallerId();
        var cancellerRoles = Roles.Create(caller.Roles.All);
        if (cancellerRoles.IsFailure)
        {
            return cancellerRoles.Error;
        }

        var canceled =
            await subscription.CancelSubscriptionAsync(_billingProvider.StateInterpreter, cancellerId,
                cancellerRoles.Value, CanCancel, OnCancel, false);
        if (canceled.IsFailure)
        {
            return canceled.Error;
        }

        var saved = await _repository.SaveAsync(subscription, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Canceled subscription: {Id} for entity: {OwningEntity}",
            subscription.Id, subscription.OwningEntityId);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.SubscriptionCanceled,
            subscription.ToSubscriptionChangedUsageEvent());

        var providerSubscription = _billingProvider.StateInterpreter.GetSubscriptionDetails(subscription.Provider);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        return subscription.ToSubscription(providerSubscription.Value);

        Task<Result<SubscriptionMetadata, Error>> OnCancel(SubscriptionRoot root)
        {
            var canceledSubscription = _billingProvider.GatewayService.CancelSubscriptionAsync(caller,
                options, root.Provider.Value, cancellationToken);
            return Task.FromResult(canceledSubscription.Result);
        }

        async Task<Permission> CanCancel(SubscriptionRoot root, Identifier cancellerId1)
        {
            return (await _subscriptionOwningEntityService.CanCancelSubscriptionAsync(caller,
                    root.OwningEntityId, cancellerId1, cancellationToken))
                .Match(optional => optional.Value, Permission.Denied_Evaluating);
        }
    }

    private async Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionInternalAsync(ICallerContext caller,
        SubscriptionRoot subscription, CancellationToken cancellationToken)
    {
        var viewerId = caller.ToCallerId();
        var providerSubscription = await subscription.ViewSubscriptionAsync(_billingProvider.StateInterpreter, viewerId,
            CanView);
        if (providerSubscription.IsFailure)
        {
            return providerSubscription.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved subscription: {Id} for entity: {OwningEntity}",
            subscription.Id, subscription.OwningEntityId);
        return subscription.ToSubscription(providerSubscription.Value);

        async Task<Permission> CanView(SubscriptionRoot root, Identifier viewerId1)
        {
            return (await _subscriptionOwningEntityService.CanViewSubscriptionAsync(caller,
                    root.OwningEntityId,
                    viewerId1, cancellationToken))
                .Match(optional => optional.Value, Permission.Denied_Evaluating);
        }
    }

    /// <summary>
    ///     Calculate the date range based on inputs and defaults
    ///     Note: If not explicitly specified, the range should be
    ///     <see cref="SubscriptionsDomain.Validations.Subscription.DefaultInvoicePeriod" />
    ///     in length, and as muh as possible include those past months
    /// </summary>
    private static (DateTime From, DateTime To) CalculatedSearchRange(DateTime? fromUtc, DateTime? toUtc)
    {
        var to = toUtc
                 ?? fromUtc?.Add(Validations.Subscription.DefaultInvoicePeriod) ?? DateTime.UtcNow.ToNearestMinute();
        var from = fromUtc ?? to.Add(-Validations.Subscription.DefaultInvoicePeriod);

        return (from, to);
    }

    private async Task<Result<SubscriptionRoot, Error>> GetSubscriptionByOwningEntityAsync(Identifier owningEntityId,
        CancellationToken cancellationToken)
    {
        var retrievedSubscription =
            await _repository.FindByOwningEntityIdAsync(owningEntityId, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        return retrievedSubscription.Value.Value;
    }

    private async Task<Result<Optional<SubscriptionBuyer>, Error>> CreateBuyerAsync(ICallerContext caller,
        Identifier buyerId, OwningEntity owningEntity, CancellationToken cancellationToken)
    {
        var retrievedProfile = await _userProfilesService.GetProfilePrivateAsync(caller, buyerId, cancellationToken);
        if (retrievedProfile.IsFailure)
        {
            if (retrievedProfile.Error.Is(ErrorCode.EntityNotFound))
            {
                return Optional<SubscriptionBuyer>.None;
            }

            return retrievedProfile.Error;
        }

        var profile = retrievedProfile.Value;
        var buyer = ToSubscriptionBuyer(buyerId, owningEntity, profile);

        return buyer.ToOptional();
    }

    private static SubscriptionBuyer ToSubscriptionBuyer(Identifier buyerId,
        OwningEntity entity, UserProfile user)
    {
        return new SubscriptionBuyer
        {
            Id = buyerId.ToString(),
            Name = user.Name,
            PhoneNumber = user.PhoneNumber,
            EmailAddress = user.EmailAddress!.Address,
            Address = user.Address,
            Subscriber = new Subscriber
            {
                EntityId = entity.Id,
                EntityType = entity.Type,
                EntityName = entity.Name
            }
        };
    }
}

internal static class SubscriptionConversionExtensions
{
    public static SubscriptionWithPlan ToSubscription(this SubscriptionRoot subscription,
        ProviderSubscription providerSubscription)
    {
        //Infer from provider
        var isTrial = subscription.ManagedTrial.HasValue || providerSubscription.Plan.Trial.HasValue;
        var trialEndDate = subscription.ManagedTrial.HasValue
            ? (DateTime?)subscription.ManagedTrial.Value.ExpiryDueAt
            : providerSubscription.Plan.Trial.HasValue
                ? providerSubscription.Plan.Trial.Value.ExpiryDueAt
                : null;

        return new SubscriptionWithPlan
        {
            Id = subscription.Id,
            OwningEntityId = subscription.OwningEntityId,
            BuyerId = subscription.BuyerId,
            ProviderName = subscription.Provider.ToNullable(pro => pro.Name),
            ProviderState = subscription.Provider.ToNullable(pro => pro.State) ?? new Dictionary<string, string>(),
            SubscriptionReference = providerSubscription.SubscriptionReference.ToNullable(sr => sr.Text),
            BuyerReference = subscription.ProviderBuyerReference.ValueOrDefault!,
            Status = providerSubscription.Status.Status.ToEnumOrDefault(SubscriptionStatus.Unsubscribed),
            CanceledDateUtc = providerSubscription.Status.CanceledDateUtc.ToNullable(),
            Plan = new SubscriptionPlan
            {
                Id = providerSubscription.Plan.PlanId.ValueOrDefault,
                IsTrial = isTrial,
                TrialEndDateUtc = trialEndDate,
                Tier = providerSubscription.Plan.Tier.ToEnum<BillingSubscriptionTier, SubscriptionTier>()
            },
            Period = new PlanPeriod
            {
                Frequency = providerSubscription.Period.Frequency,
                Unit = providerSubscription.Period.Unit.ToEnumOrDefault(PeriodFrequencyUnit.Eternity)
            },
            UpcomingInvoice = new InvoiceSummary
            {
                Amount = providerSubscription.UpcomingInvoice.Amount,
                Currency = providerSubscription.UpcomingInvoice.CurrencyCode.Currency.Code,
                NextUtc = providerSubscription.UpcomingInvoice.NextUtc.ToNullable()
            },
            PaymentMethod = new SubscriptionPaymentMethod
            {
                Status = providerSubscription.PaymentMethod.Status.ToEnumOrDefault(PaymentMethodStatus.Invalid),
                Type = providerSubscription.PaymentMethod.Type.ToEnumOrDefault(PaymentMethodType.None),
                ExpiresOn = providerSubscription.PaymentMethod.ExpiresOn.ToNullable()
            },
            CheckoutUrl = providerSubscription.PaymentMethod.CheckoutUrl,
            CanBeUnsubscribed = providerSubscription.Status.CanBeUnsubscribed,
            CanBeCanceled = providerSubscription.Status.CanBeCanceled
        };
    }

    public static Dictionary<string, object> ToSubscriptionChangedUsageEvent(this SubscriptionRoot subscription)
    {
        return new Dictionary<string, object>
        {
            { UsageConstants.Properties.Id, subscription.Id },
            { UsageConstants.Properties.TenantId, subscription.OwningEntityId },
            { UsageConstants.Properties.BillingProvider, subscription.Provider.Value.Name },
            { UsageConstants.Properties.CreatedById, subscription.BuyerId }
        };
    }

    public static SubscriptionToMigrate ToSubscriptionForMigration(this Subscription subscription,
        SubscriptionBuyer? buyer)
    {
        var dto = new SubscriptionToMigrate
        {
            Id = subscription.Id,
            BuyerId = subscription.BuyerId,
            OwningEntityId = subscription.OwningEntityId,
            ProviderName = subscription.ProviderName,
            ProviderState = subscription.ProviderState.Value.FromJson<Dictionary<string, string>>()!,
            Buyer = buyer.Exists()
                ? new Dictionary<string, string>(buyer.ToStringDictionary())
                : new Dictionary<string, string>()
        };

        return dto;
    }
}