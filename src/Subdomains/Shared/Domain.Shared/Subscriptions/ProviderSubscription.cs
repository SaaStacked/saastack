using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderSubscription : ValueObjectBase<ProviderSubscription>
{
    public static readonly ProviderSubscription Empty = Create(ProviderStatus.Empty).Value;

    public static Result<ProviderSubscription, Error> Create(ProviderStatus status)

    {
        return new ProviderSubscription(Optional<string>.None, status, ProviderPlan.Empty, ProviderPlanPeriod.Empty,
            ProviderInvoice.Default, ProviderPaymentMethod.Empty);
    }

    public static Result<ProviderSubscription, Error> Create(ProviderStatus status,
        ProviderPlan plan, ProviderPlanPeriod period, ProviderPaymentMethod paymentMethod)
    {
        return new ProviderSubscription(Optional<string>.None, status, plan, period, ProviderInvoice.Default,
            paymentMethod);
    }

    public static Result<ProviderSubscription, Error> Create(ProviderStatus status, ProviderPaymentMethod paymentMethod)
    {
        return new ProviderSubscription(Optional<string>.None, status, ProviderPlan.Empty, ProviderPlanPeriod.Empty,
            ProviderInvoice.Default, paymentMethod);
    }

    public static Result<ProviderSubscription, Error> Create(string subscriptionReference, ProviderStatus status,
        ProviderPlan plan, ProviderPlanPeriod period, ProviderInvoice invoice, ProviderPaymentMethod paymentMethod)
    {
        if (subscriptionReference.IsInvalidParameter(sr => sr.HasValue(), nameof(subscriptionReference),
                Resources.ProviderSubscription_InvalidSubscriptionReference, out var error))
        {
            return error;
        }

        return new ProviderSubscription(subscriptionReference, status, plan, period, invoice, paymentMethod);
    }

    private ProviderSubscription(Optional<string> subscriptionReference, ProviderStatus status, ProviderPlan plan,
        ProviderPlanPeriod period, ProviderInvoice invoice, ProviderPaymentMethod paymentMethod)
    {
        SubscriptionReference = subscriptionReference;
        Status = status;
        Plan = plan;
        Period = period;
        UpcomingInvoice = invoice;
        PaymentMethod = paymentMethod;
    }

    public bool IsConvertable => SubscriptionReference.HasValue
                                 && PaymentMethod.Status == BillingPaymentMethodStatus.Valid
                                 && Plan.PlanId.HasValue
                                 && Plan.Tier is not BillingSubscriptionTier.Unsubscribed;

    public ProviderPaymentMethod PaymentMethod { get; }

    public ProviderPlanPeriod Period { get; }

    public ProviderPlan Plan { get; }

    public ProviderStatus Status { get; }

    public Optional<string> SubscriptionReference { get; }

    public ProviderInvoice UpcomingInvoice { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderSubscription> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderSubscription(
                parts[0].ToOptional(),
                ProviderStatus.Rehydrate()(parts[1], container),
                ProviderPlan.Rehydrate()(parts[2], container),
                ProviderPlanPeriod.Rehydrate()(parts[3], container),
                ProviderInvoice.Rehydrate()(parts[4], container),
                ProviderPaymentMethod.Rehydrate()(parts[5], container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [SubscriptionReference, Status, Plan, Period, UpcomingInvoice, PaymentMethod];
    }
}