using Application.Interfaces;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;
using Moq;
using SubscriptionsApplication.Persistence;
using SubscriptionsApplication.Persistence.ReadModels;
using SubscriptionsDomain;
using UnitTesting.Common;
using Xunit;

namespace SubscriptionsApplication.UnitTests;

[UsedImplicitly]
public class SubscriptionsApplicationQuotasSpec
{
    [Trait("Category", "Unit")]
    public class GivenNoContext
    {
        private readonly SubscriptionsApplication _application;
        private readonly Mock<IBillingProvider> _billingProvider;
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IIdentifierFactory> _identifierFactory;
        private readonly Mock<IRecorder> _recorder;
        private readonly Mock<ISubscriptionRepository> _repository;
        private readonly Mock<ISubscriptionQuotaRepository> _subscriptionQuotaRepository;

        public GivenNoContext()
        {
            _recorder = new Mock<IRecorder>();
            _identifierFactory = new Mock<IIdentifierFactory>();
            _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
                .Returns("anid".ToId());
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.TenantId)
                .Returns("atenantid".ToOptional());
            var userProfilesService = new Mock<IUserProfilesService>();
            _billingProvider = new Mock<IBillingProvider>();
            var capabilities = new BillingProviderCapabilities
            {
                QuotaManagement = ManagementOptions.RequiresManaged
            };
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(capabilities);
            _billingProvider.Setup(bp => bp.StateInterpreter.ProviderName)
                .Returns("aprovidername");
            _billingProvider.Setup(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()))
                .Returns("abuyerreference");
            _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()))
                .Returns("asubscriptionreference".ToOptional());
            _billingProvider.Setup(bp => bp.StateInterpreter.SetInitialProviderState(It.IsAny<BillingProvider>()))
                .Returns((BillingProvider provider) => provider);
            var owningEntityService = new Mock<ISubscriptionOwningEntityService>();
            owningEntityService.Setup(oes =>
                    oes.GetEntityAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OwningEntity
                {
                    Id = "anowningentityid",
                    Type = "anowningentitytype",
                    Name = "anowningentityname"
                });
            var subscriptionEventMessageRepository = new Mock<ISubscriptionTrialEventMessageQueueRepository>();
            _subscriptionQuotaRepository = new Mock<ISubscriptionQuotaRepository>();
            _subscriptionQuotaRepository.Setup(rep =>
                    rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubscriptionQuotaUsageRoot root, CancellationToken _) => root);
            _repository = new Mock<ISubscriptionRepository>();

            _application = new SubscriptionsApplication(_recorder.Object, _identifierFactory.Object,
                userProfilesService.Object, _billingProvider.Object, owningEntityService.Object,
                subscriptionEventMessageRepository.Object, _subscriptionQuotaRepository.Object, _repository.Object);
        }

        [Fact]
        public async Task WhenTryCheckQuotaUsageAsyncWithNoTenantId_ThenReturnsError()
        {
            _caller.Setup(cc => cc.TenantId)
                .Returns(Optional<string>.None);

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 1, CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound);
        }

        [Fact]
        public async Task WhenTryCheckQuotaUsageAsyncWithUnknownSubscription_ThenReturnsError()
        {
            _repository.Setup(rep =>
                    rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<SubscriptionRoot>.None);

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 1, CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound);
        }

        [Fact]
        public async Task WhenTryCheckQuotaUsageAsyncAndProviderHasNoManagedQuotas_ThenReturnsOk()
        {
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            var subscription = SubscriptionRoot
                .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                    _billingProvider.Object.StateInterpreter).Value;
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            _caller.Setup(cc => cc.TenantId).Returns("anowningentityid".ToOptional());
            _repository.Setup(rep =>
                    rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription.ToOptional());
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.SelfManaged
                });

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 1, CancellationToken.None);

            result.Should().BeSuccess();
            _subscriptionQuotaRepository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenTryCheckQuotaUsageAsyncAndSubscriptionHasNoManagedQuotas_ThenReturnsOk()
        {
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            var subscription = SubscriptionRoot
                .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                    _billingProvider.Object.StateInterpreter).Value;
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            _caller.Setup(cc => cc.TenantId).Returns("anowningentityid".ToOptional());
            _repository.Setup(rep =>
                    rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription.ToOptional());

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 1, CancellationToken.None);

            result.Should().BeSuccess();
            _subscriptionQuotaRepository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenTryCheckQuotaUsageAsyncAndProviderHasNoQuotasForTier_ThenReturnsOk()
        {
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            var subscription = SubscriptionRoot
                .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                    _billingProvider.Object.StateInterpreter).Value;
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
#if TESTINGONLY
            subscription.TestingOnly_SetManagedQuotas(
                ProviderTierQuotas.Create(BillingSubscriptionTier.Standard, Optional<ProviderPlanQuotas>.None).Value);
#endif
            _caller.Setup(cc => cc.TenantId).Returns("anowningentityid".ToOptional());
            _repository.Setup(rep =>
                    rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription.ToOptional());
            var quotas = ProviderQuotas.Create(new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
            {
                {
                    BillingSubscriptionTier.Professional,
                    ProviderPlanQuotas.Create("otherid", ProviderPlanQuota.Create("adescription").Value).Value
                }
            }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities { ManagedQuotas = quotas });

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 1, CancellationToken.None);

            result.Should().BeSuccess();
            _subscriptionQuotaRepository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenTryCheckQuotaUsageAsyncAndUnknownQuotaId_ThenReturnsOk()
        {
            var standardQuota = ProviderTierQuotas.Create(BillingSubscriptionTier.Standard, ProviderPlanQuotas
                .Create("aquotaid", ProviderPlanQuota.Create("adescription", 10).Value)
                .Value).Value;
            await SetupSubscriptionAsync(standardQuota);
            SetupProviderQuotas(standardQuota);

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "anotherquotaid", 1, CancellationToken.None);

            result.Should().BeSuccess();
            _subscriptionQuotaRepository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task
            WhenTryCheckQuotaUsageAsyncAndProposedTotalWithinDefinedLimitButNoUsage_ThenReturnsOkAndAddsNewUsage()
        {
            var quotaDefinition = ProviderPlanQuota.Create("adescription", 10).Value;
            var standardQuota = ProviderTierQuotas.Create(BillingSubscriptionTier.Standard,
                ProviderPlanQuotas.Create("aquotaid", quotaDefinition).Value).Value;
            var subscription = await SetupSubscriptionAsync(standardQuota);
            SetupProviderQuotas(standardQuota);
            var usageRoot = SubscriptionQuotaUsageRoot.Create(_recorder.Object, _identifierFactory.Object,
                subscription.Id, "anowningentityid".ToId(), "aprovidername").Value;
            usageRoot.Configure(BillingSubscriptionTier.Standard, "aquotaid", quotaDefinition);
            _subscriptionQuotaRepository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([]));
            _subscriptionQuotaRepository.Setup(rep =>
                    rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(usageRoot);

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 5, CancellationToken.None);

            result.Should().BeSuccess();
            _subscriptionQuotaRepository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == 10
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 5
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenTryCheckQuotaUsageAsyncAndProposedTotalWithinDefinedLimitOfExistingUsage_ThenReturnsOkAndUpdatesUsage()
        {
            var quotaDefinition = ProviderPlanQuota.Create("adescription", 10).Value;
            var standardQuota = ProviderTierQuotas.Create(BillingSubscriptionTier.Standard,
                ProviderPlanQuotas.Create("aquotaid", quotaDefinition).Value).Value;
            var subscription = await SetupSubscriptionAsync(standardQuota);
            SetupProviderQuotas(standardQuota);
            var usageRoot = SubscriptionQuotaUsageRoot.Create(_recorder.Object, new FixedIdentifierFactory("ausageid"),
                subscription.Id, "anowningentityid".ToId(), "aprovidername").Value;
            usageRoot.Configure(BillingSubscriptionTier.Standard, "aquotaid", quotaDefinition);
            _subscriptionQuotaRepository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>(
                [
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        SubscriptionTier = BillingSubscriptionTier.Standard,
                        QuotaId = "aquotaid"
                    }
                ]));
            _subscriptionQuotaRepository.Setup(rep =>
                    rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(usageRoot);

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 5, CancellationToken.None);

            result.Should().BeSuccess();
            _subscriptionQuotaRepository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "ausageid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == 10
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 5
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenTryCheckQuotaUsageAsyncAndProposedTotalWithinInfiniteLimit_ThenReturnsOkAndUpdatesUsage()
        {
            var quotaDefinition = ProviderPlanQuota.Create("adescription").Value;
            var standardQuota = ProviderTierQuotas.Create(BillingSubscriptionTier.Standard,
                ProviderPlanQuotas.Create("aquotaid", quotaDefinition).Value).Value;
            var subscription = await SetupSubscriptionAsync(standardQuota);
            SetupProviderQuotas(standardQuota);
            var usageRoot = SubscriptionQuotaUsageRoot.Create(_recorder.Object, _identifierFactory.Object,
                subscription.Id, "anowningentityid".ToId(), "aprovidername").Value;
            usageRoot.Configure(BillingSubscriptionTier.Standard, "aquotaid", quotaDefinition);
            _subscriptionQuotaRepository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>(
                [
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid",
                        SubscriptionId = subscription.Id.ToString(),
                        SubscriptionTier = BillingSubscriptionTier.Standard,
                        QuotaId = "aquotaid"
                    }
                ]));
            _subscriptionQuotaRepository.Setup(rep =>
                    rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(usageRoot);

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 5, CancellationToken.None);

            result.Should().BeSuccess();
            _subscriptionQuotaRepository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == -1
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 5
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenTryCheckQuotaUsageAsyncAndProposedTotalExceedsLimit_ThenReturnsError()
        {
            var standardQuota = ProviderTierQuotas.Create(BillingSubscriptionTier.Standard,
                ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription", 5).Value).Value).Value;
            await SetupSubscriptionAsync(standardQuota);
            SetupProviderQuotas(standardQuota);
            _subscriptionQuotaRepository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([]));

            var result =
                await _application.TryCheckQuotaUsageAsync(_caller.Object, "aquotaid", 6, CancellationToken.None);

            result.Should().BeError(ErrorCode.FeatureViolation);
            _subscriptionQuotaRepository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private async Task<SubscriptionRoot> SetupSubscriptionAsync(ProviderTierQuotas quotas)
        {
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            var subscription = SubscriptionRoot
                .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                    _billingProvider.Object.StateInterpreter).Value;
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
#if TESTINGONLY
            subscription.TestingOnly_SetManagedQuotas(quotas);
#endif
            _caller.Setup(cc => cc.TenantId).Returns("anowningentityid".ToOptional());
            _repository.Setup(rep =>
                    rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription.ToOptional());

            return subscription;
        }

        private void SetupProviderQuotas(ProviderTierQuotas quotas)
        {
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = ProviderQuotas.Create(new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                    {
                        { quotas.Tier, quotas.Quotas }
                    }).Value
                });
        }
    }

    [Trait("Category", "Unit")]
    public class GivenFirstSync
    {
        private readonly SubscriptionsApplication _application;
        private readonly Mock<IBillingProvider> _billingProvider;
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IIdentifierFactory> _identifierFactory;
        private readonly Mock<IRecorder> _recorder;
        private readonly Mock<ISubscriptionQuotaRepository> _repository;

        public GivenFirstSync()
        {
            _recorder = new Mock<IRecorder>();
            _identifierFactory = new Mock<IIdentifierFactory>();
            _identifierFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
                .Returns("anid".ToId());
            _caller = new Mock<ICallerContext>();
            var userProfilesService = new Mock<IUserProfilesService>();
            _billingProvider = new Mock<IBillingProvider>();
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = null
                });
            _billingProvider.Setup(bp => bp.StateInterpreter.ProviderName)
                .Returns("aprovidername");
            _billingProvider.Setup(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()))
                .Returns("abuyerreference");
            _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()))
                .Returns("asubscriptionreference".ToOptional());
            _billingProvider.Setup(bp => bp.StateInterpreter.SetInitialProviderState(It.IsAny<BillingProvider>()))
                .Returns((BillingProvider provider) => provider);
            var owningEntityService = new Mock<ISubscriptionOwningEntityService>();
            owningEntityService.Setup(oes =>
                    oes.GetEntityAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OwningEntity
                {
                    Id = "anowningentityid",
                    Type = "anowningentitytype",
                    Name = "anowningentityname"
                });
            var trialEventMessageRepository = new Mock<ISubscriptionTrialEventMessageQueueRepository>();
            var subscriptionRepository = new Mock<ISubscriptionRepository>();
            _repository = new Mock<ISubscriptionQuotaRepository>();
            _repository.Setup(rep =>
                    rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubscriptionQuotaUsageRoot root, CancellationToken _) => root);
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([]));

            _application = new SubscriptionsApplication(_recorder.Object, _identifierFactory.Object,
                userProfilesService.Object, _billingProvider.Object, owningEntityService.Object,
                trialEventMessageRepository.Object, _repository.Object, subscriptionRepository.Object);
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncForAndNoManagedQuotas_ThenDoesNothing()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                Optional<BillingSubscriptionTier>.None, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _repository.Verify(rep =>
                    rep.DeleteUsageAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndNoQuotaForCurrentTier_ThenDoesNothing()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Enterprise,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                Optional<BillingSubscriptionTier>.None, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _repository.Verify(rep =>
                    rep.DeleteUsageAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenResyncSubscriptionTierQuotasInternalAsync_ThenCreatesUsage()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Standard,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                Optional<BillingSubscriptionTier>.None, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == -1
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 0
                ), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                    rep.DeleteUsageAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenOtherSyncs
    {
        private readonly SubscriptionsApplication _application;
        private readonly Mock<IBillingProvider> _billingProvider;
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IIdentifierFactory> _identifierFactory;
        private readonly Mock<IRecorder> _recorder;
        private readonly Mock<ISubscriptionQuotaRepository> _repository;

        public GivenOtherSyncs()
        {
            _recorder = new Mock<IRecorder>();
            _identifierFactory = new Mock<IIdentifierFactory>();
            _identifierFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
                .Returns("anid".ToId());
            _caller = new Mock<ICallerContext>();
            var userProfilesService = new Mock<IUserProfilesService>();
            _billingProvider = new Mock<IBillingProvider>();
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = null
                });
            _billingProvider.Setup(bp => bp.StateInterpreter.ProviderName)
                .Returns("aprovidername");
            _billingProvider.Setup(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()))
                .Returns("abuyerreference");
            _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()))
                .Returns("asubscriptionreference".ToOptional());
            _billingProvider.Setup(bp => bp.StateInterpreter.SetInitialProviderState(It.IsAny<BillingProvider>()))
                .Returns((BillingProvider provider) => provider);
            var owningEntityService = new Mock<ISubscriptionOwningEntityService>();
            owningEntityService.Setup(oes =>
                    oes.GetEntityAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OwningEntity
                {
                    Id = "anowningentityid",
                    Type = "anowningentitytype",
                    Name = "anowningentityname"
                });
            var trialEventMessageRepository = new Mock<ISubscriptionTrialEventMessageQueueRepository>();
            var subscriptionRepository = new Mock<ISubscriptionRepository>();
            _repository = new Mock<ISubscriptionQuotaRepository>();
            _repository.Setup(rep =>
                    rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubscriptionQuotaUsageRoot root, CancellationToken _) => root);
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([]));

            _application = new SubscriptionsApplication(_recorder.Object, _identifierFactory.Object,
                userProfilesService.Object, _billingProvider.Object, owningEntityService.Object,
                trialEventMessageRepository.Object, _repository.Object, subscriptionRepository.Object);
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndNoManagedQuotasAndNoneExisting_ThenDoesNothing()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Standard, BillingSubscriptionTier.Professional, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _repository.Verify(rep =>
                    rep.DeleteUsageAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndNoQuotasButSomeExisting_ThenCleansUpAllPastUsages()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([
                    new SubscriptionQuotaUsage { Id = "ausageid1" },
                    new SubscriptionQuotaUsage { Id = "ausageid2" }
                ]));

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Standard, BillingSubscriptionTier.Professional, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid1".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid2".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndNoQuotaForCurrentTier_ThenCleansUpAllPastUsages()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Enterprise,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([
                    new SubscriptionQuotaUsage { Id = "ausageid1" },
                    new SubscriptionQuotaUsage { Id = "ausageid2" }
                ]));

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Standard, BillingSubscriptionTier.Professional, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(
                rep => rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid1".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid2".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndQuotaForPreviousTierNotExistsAndCurrentTierNotExists_ThenCreatesUsageAndCleansUpAllPastUsages()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Standard,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([
                    new SubscriptionQuotaUsage
                        { Id = "ausageid1", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                        { Id = "ausageid2", SubscriptionTier = BillingSubscriptionTier.Enterprise }
                ]));

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Unsubscribed, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == -1
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 0
                ), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid1".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid2".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndQuotaForPreviousTierExistsAndCurrentTierNotExists_ThenCreatesUsageAndCleansUpAllPastUsages()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Standard,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription", 99).Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([
                    new SubscriptionQuotaUsage
                        { Id = "ausageid1", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                        { Id = "ausageid2", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid3",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        QuotaId = "aquotaid",
                        SubscriptionTier = BillingSubscriptionTier.Unsubscribed,
                        Total = 9,
                        LastResetAt = DateTime.UtcNow.SubtractDays(1)
                    }
                ]));

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Unsubscribed, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == 99
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 9
                ), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid1".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid2".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid3".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndQuotaForPreviousTierNotExistsAndCurrentTierExists_ThenCreatesUsageAndCleansUpAllPastUsages()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Standard,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([
                    new SubscriptionQuotaUsage
                        { Id = "ausageid1", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                        { Id = "ausageid2", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid3",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        QuotaId = "aquotaid",
                        SubscriptionTier = BillingSubscriptionTier.Standard,
                        Total = 99,
                        LastResetAt = DateTime.UtcNow.SubtractDays(1)
                    }
                ]));

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Unsubscribed, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == -1
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 0
                ), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid1".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid2".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid3".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndQuotaForPreviousTierExistsAndCurrentTierExists_ThenCreatesUsageWithOldTotalAndCleansUpAllPastUsages()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Standard,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription", 99).Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([
                    new SubscriptionQuotaUsage
                        { Id = "ausageid1", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                        { Id = "ausageid2", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid3",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        QuotaId = "aquotaid",
                        SubscriptionTier = BillingSubscriptionTier.Unsubscribed,
                        Total = 9,
                        LastResetAt = DateTime.UtcNow.SubtractDays(1)
                    },
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid4",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        QuotaId = "aquotaid",
                        SubscriptionTier = BillingSubscriptionTier.Standard,
                        Total = 99,
                        LastResetAt = DateTime.UtcNow.SubtractDays(1)
                    }
                ]));

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Unsubscribed, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == 99
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 9
                ), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid1".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid2".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid3".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid4".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndQuotaForPreviousTierExistsAndExceedsCurrentTierLimit_ThenCreatesUsageWithNewLimitAndCleansUpAllPastUsages()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Standard,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription", 99).Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([
                    new SubscriptionQuotaUsage
                        { Id = "ausageid1", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                        { Id = "ausageid2", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid3",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        QuotaId = "aquotaid",
                        SubscriptionTier = BillingSubscriptionTier.Unsubscribed,
                        Total = 999,
                        LastResetAt = DateTime.UtcNow.SubtractDays(1)
                    },
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid4",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        QuotaId = "aquotaid",
                        SubscriptionTier = BillingSubscriptionTier.Standard,
                        Total = 99,
                        LastResetAt = DateTime.UtcNow.SubtractDays(1)
                    }
                ]));

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Unsubscribed, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == 99
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 99
                ), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid1".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid2".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid3".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid4".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndQuotaForPreviousTierExistsAndCurrentTierLimitIsInfinite_ThenCreatesUsageWithNewLimitAndCleansUpAllPastUsages()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    {
                        BillingSubscriptionTier.Standard,
                        ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value
                    }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });
            _repository.Setup(rep => rep.SearchAllByOwningEntityIdAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResults<SubscriptionQuotaUsage>([
                    new SubscriptionQuotaUsage
                        { Id = "ausageid1", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                        { Id = "ausageid2", SubscriptionTier = BillingSubscriptionTier.Enterprise },
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid3",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        QuotaId = "aquotaid",
                        SubscriptionTier = BillingSubscriptionTier.Unsubscribed,
                        Total = 999,
                        LastResetAt = DateTime.UtcNow.SubtractDays(1)
                    },
                    new SubscriptionQuotaUsage
                    {
                        Id = "ausageid4",
                        ProviderName = "aprovidername",
                        SubscriptionId = subscription.Id.ToString(),
                        QuotaId = "aquotaid",
                        SubscriptionTier = BillingSubscriptionTier.Standard,
                        Total = 99,
                        LastResetAt = DateTime.UtcNow.SubtractDays(1)
                    }
                ]));

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Unsubscribed, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.SaveAsync(It.Is<SubscriptionQuotaUsageRoot>(root =>
                    root.Id == "anid".ToId()
                    && root.ProviderName.Value == "aprovidername"
                    && root.OwningEntityId == "anowningentityid".ToId()
                    && root.SubscriptionId == subscription.Id
                    && root.LastResetAt.Value.IsNear(DateTime.UtcNow)
                    && root.Period == BillingSubscriptionQuotaPeriod.Eternity
                    && root.Limit == -1
                    && root.SubscriptionTier == BillingSubscriptionTier.Standard
                    && root.QuotaId == "aquotaid".ToId()
                    && root.Total == 999
                ), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid1".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid2".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid3".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.DeleteUsageAsync("anowningentityid".ToId(), "ausageid4".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenResyncSubscriptionTierQuotasInternalAsyncAndTiersAreSame_ThenDoesNothing()
        {
            var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
                "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
            var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
            await subscription.SetProviderAsync(BillingProvider.Create("aprovidername", metadata).Value,
                "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
            var standardQuotas = ProviderTierQuotas.Create(BillingSubscriptionTier.Standard,
                ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription", 2).Value).Value
            ).Value;
            var quotas = ProviderQuotas.Create(
                new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
                {
                    { standardQuotas.Tier, standardQuotas.Quotas.Value }
                }).Value;
            _billingProvider.Setup(bp => bp.Capabilities)
                .Returns(new BillingProviderCapabilities
                {
                    QuotaManagement = ManagementOptions.RequiresManaged,
                    ManagedQuotas = quotas
                });
#if TESTINGONLY
            subscription.TestingOnly_SetManagedQuotas(standardQuotas);
#endif

            var result = await _application.ResyncSubscriptionTierQuotasInternalAsync(_caller.Object, subscription,
                BillingSubscriptionTier.Standard, BillingSubscriptionTier.Standard, CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.SaveAsync(It.IsAny<SubscriptionQuotaUsageRoot>(), It.IsAny<CancellationToken>()), Times.Never);
            _repository.Verify(rep =>
                    rep.DeleteUsageAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}