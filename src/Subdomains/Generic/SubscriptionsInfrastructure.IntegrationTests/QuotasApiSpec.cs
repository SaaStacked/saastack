using ApiHost1;
using Application.Resources.Shared;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
#if TESTINGONLY
using System.Net;
using Application.Services.Shared;
using SubscriptionsInfrastructure.IntegrationTests.Stubs;
#endif

namespace SubscriptionsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class QuotasApisSpec : WebApiSpec<Program>
{
#if TESTINGONLY
    private readonly StubManagedTrialBillingProvider _stubBillingProvider;
#endif
    public QuotasApisSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
#if TESTINGONLY
        _stubBillingProvider = setup.GetRequiredService<IBillingProvider>().As<StubManagedTrialBillingProvider>();
        _stubBillingProvider.Reset();
#endif
    }

    [Fact]
    public async Task WhenGetSubscriptionAndStandardTier_ThenReturnsQuotas()
    {
        var login = await LoginUserAsync();

        var result = await Api.GetAsync(new GetSubscriptionRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Subscription.RemainingQuotas!.Count.Should().Be(2);
        result.Content.Value.Subscription.RemainingQuotas[0].Definition.Description.Should().Be("A Standard Quota 1");
        result.Content.Value.Subscription.RemainingQuotas[0].Definition.Period.Should()
            .Be(SubscriptionQuotaPeriod.Eternity);
        result.Content.Value.Subscription.RemainingQuotas[0].Definition.Limit.Should().Be(1);
        result.Content.Value.Subscription.RemainingQuotas[0].Remaining.Should().Be(1);
        result.Content.Value.Subscription.RemainingQuotas[0].Total.Should().Be(0);
        result.Content.Value.Subscription.RemainingQuotas[1].Definition.Description.Should().Be("A Standard Quota 2");
        result.Content.Value.Subscription.RemainingQuotas[1].Definition.Period.Should()
            .Be(SubscriptionQuotaPeriod.Eternity);
        result.Content.Value.Subscription.RemainingQuotas[1].Definition.Limit.Should().Be(1);
        result.Content.Value.Subscription.RemainingQuotas[1].Remaining.Should().Be(1);
        result.Content.Value.Subscription.RemainingQuotas[1].Total.Should().Be(0);
    }

    [Fact]
    public async Task WhenGetSubscriptionAndUpgradedToProfessionalTier_ThenReturnsQuotas()
    {
        var login = await LoginUserAsync();
        await UpgradeSubscriptionAsync(login);

        var result = await Api.GetAsync(new GetSubscriptionRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Subscription.RemainingQuotas!.Count.Should().Be(2);
        result.Content.Value.Subscription.RemainingQuotas[0].Definition.Description.Should()
            .Be("A Professional Quota 1");
        result.Content.Value.Subscription.RemainingQuotas[0].Definition.Period.Should()
            .Be(SubscriptionQuotaPeriod.Eternity);
        result.Content.Value.Subscription.RemainingQuotas[0].Definition.Limit.Should().Be(2);
        result.Content.Value.Subscription.RemainingQuotas[0].Remaining.Should().Be(2);
        result.Content.Value.Subscription.RemainingQuotas[0].Total.Should().Be(0);
        result.Content.Value.Subscription.RemainingQuotas[1].Definition.Description.Should()
            .Be("A Professional Quota 2");
        result.Content.Value.Subscription.RemainingQuotas[1].Definition.Period.Should()
            .Be(SubscriptionQuotaPeriod.Eternity);
        result.Content.Value.Subscription.RemainingQuotas[1].Definition.Limit.Should().Be(-1);
        result.Content.Value.Subscription.RemainingQuotas[1].Remaining.Should().Be(-1);
        result.Content.Value.Subscription.RemainingQuotas[1].Total.Should().Be(0);
    }

    [Fact]
    public async Task WhenCheckQuotaAndStandardTierAndUnderLimit_ThenReturnsOk()
    {
        var login = await LoginUserAsync();
        var organizationId = login.DefaultOrganizationId!;

#if TESTINGONLY
        var result = await Api.PostAsync(new CheckQuotaRequest
        {
            Id = organizationId,
            QuotaId = StubManagedTrialBillingProvider.QuotaId1,
            Total = 1
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
    }

    [Fact]
    public async Task WhenCheckQuotaAndStandardTierAndExceedsLimit_ThenReturnsError()
    {
        var login = await LoginUserAsync();
        var organizationId = login.DefaultOrganizationId!;

#if TESTINGONLY
        var result = await Api.PostAsync(new CheckQuotaRequest
        {
            Id = organizationId,
            QuotaId = StubManagedTrialBillingProvider.QuotaId1,
            Total = 999
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
#endif
    }

    private async Task UpgradeSubscriptionAsync(LoginDetails login)
    {
        var organizationId = login.DefaultOrganizationId!;
#if TESTINGONLY
        _stubBillingProvider.AddPaymentMethod();
        var upgraded = await Api.PutAsync(new ChangeSubscriptionPlanRequest
        {
            Id = organizationId,
            PlanId = StubManagedTrialBillingGatewayService.Tier2PlanId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        upgraded.StatusCode.Should().Be(HttpStatusCode.Accepted);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
#if TESTINGONLY
        services.AddSingleton<IBillingProvider, StubManagedTrialBillingProvider>();
#endif
    }
}