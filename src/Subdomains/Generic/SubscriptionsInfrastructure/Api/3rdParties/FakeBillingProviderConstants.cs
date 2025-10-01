#if TESTINGONLY
namespace SubscriptionsInfrastructure.Api._3rdParties;

public static class FakeBillingProviderConstants
{
    public const string AuditSourceName = ProviderName;
    public const string ProviderName = "fake_billing_provider";

    public static class MetadataProperties
    {
        public const string CustomerId = "CustomerId";
        public const string IsCancelled = "IsCancelled";
        public const string WhenCanceled = "CancelWhen";
        public const string PaymentMethodId = "PaymentMethodId";
        public const string PlanId = "PlanId";
        public const string SubscriptionId = "SubscriptionId";
    }
}
#endif