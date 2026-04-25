using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPaymentMethod : ValueObjectBase<ProviderPaymentMethod>
{
    public static readonly ProviderPaymentMethod Empty =
        Create(BillingPaymentMethodType.None, BillingPaymentMethodStatus.Invalid, Optional<DateOnly>.None,
            Optional<string>.None).Value;

    public static Result<ProviderPaymentMethod, Error> Create(BillingPaymentMethodType type,
        BillingPaymentMethodStatus status, Optional<DateOnly> expiresOn, Optional<string> checkoutUrl)
    {
        return new ProviderPaymentMethod(type, status, expiresOn, checkoutUrl);
    }

    public static Result<ProviderPaymentMethod, Error> Create(string checkoutUrl)
    {
        return new ProviderPaymentMethod(BillingPaymentMethodType.None, BillingPaymentMethodStatus.Invalid,
            Optional<DateOnly>.None, checkoutUrl);
    }

    private ProviderPaymentMethod(BillingPaymentMethodType type, BillingPaymentMethodStatus status,
        Optional<DateOnly> expiresOn, Optional<string> checkoutUrl)
    {
        Type = type;
        Status = status;
        ExpiresOn = expiresOn;
        CheckoutUrl = checkoutUrl;
    }

    public Optional<DateOnly> ExpiresOn { get; }

    public BillingPaymentMethodStatus Status { get; }

    public BillingPaymentMethodType Type { get; }

    public Optional<string> CheckoutUrl { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPaymentMethod> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderPaymentMethod(
                parts[0].Value.ToEnumOrDefault(BillingPaymentMethodType.None),
                parts[1].Value.ToEnumOrDefault(BillingPaymentMethodStatus.Invalid),
                parts[2].ToOptional(value => value.FromIso8601DateOnly()),
                parts[3].ToOptional());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Type, Status, ExpiresOn, CheckoutUrl];
    }
}

public enum BillingPaymentMethodType
{
    None = 0,
    Card = 1, // debit or credit
    Other = 2
}

public enum BillingPaymentMethodStatus
{
    Invalid = 0,
    Valid = 1
}