#if TESTINGONLY
namespace SubscriptionsInfrastructure.Api._3rdParties;

public static class FakeBillingProviderConstants
{
    public const string AuditSourceName = ProviderName;
    public const string ProviderName = "fake_billing_provider";

    public static class MetadataProperties
    {
        public const string CustomerId = "CustomerId";

        public const string PaymentMethodId = "PaymentMethodId";

        public const string PaymentMethodStatus = "PaymentMethodStatus";

        public const string PaymentMethodType = "PaymentMethodType";

        public const string SubscriptionId = "SubscriptionId";
        public const string PlanId = "PlanId";
    }
}
#endif