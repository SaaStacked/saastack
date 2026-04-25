using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Services.Shared;
using Infrastructure.Shared.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class MeteredBillingUsageDeliveryServiceSpec
{
    private readonly Mock<IBillingProvider> _billingProvider;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IUsageDeliveryService> _primaryService;
    private readonly Mock<ISubscriptionsService> _subscriptionsService;
    private readonly Mock<IRecorder> _recorder;

    public MeteredBillingUsageDeliveryServiceSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.TenantId)
            .Returns("atenantid");
        _recorder = new Mock<IRecorder>();
        _primaryService = new Mock<IUsageDeliveryService>();
        _primaryService.Setup(ps => ps.DeliverAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        _billingProvider = new Mock<IBillingProvider>();
        _billingProvider.Setup(bp => bp.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                MeteredEvents = []
            });
        _subscriptionsService = new Mock<ISubscriptionsService>();
        _subscriptionsService.Setup(ss => ss.IncrementSubscriptionUsageAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
    }

    [Fact]
    public async Task WhenDeliverAsyncAndNotAMeteredEvent_ThenOnlyDeliversToPrimaryService()
    {
        var service = new MeteredBillingUsageDeliveryService(_recorder.Object, _primaryService.Object,
            _billingProvider.Object, _subscriptionsService.Object);

        var result = await service.DeliverAsync(_caller.Object, "aforid", "aneventname",
            new Dictionary<string, string>(), CancellationToken.None);

        result.Should().BeSuccess();
        _primaryService.Setup(ps => ps.DeliverAsync(_caller.Object, "aforid", "aneventname",
            It.Is<Dictionary<string, string>>(dic => dic.HasNone()), It.IsAny<CancellationToken>()));
        _subscriptionsService.Verify(
            ss => ss.IncrementSubscriptionUsageAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenDeliverAsyncAndMeteredEvent_ThenDeliversToPrimaryServiceAndThenMetersBillingProvider()
    {
        _billingProvider.Setup(bp => bp.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                MeteredEvents = ["aneventname"]
            });

        var service = new MeteredBillingUsageDeliveryService(_recorder.Object, _primaryService.Object,
            _billingProvider.Object, _subscriptionsService.Object);

        var result = await service.DeliverAsync(_caller.Object, "aforid", "aneventname",
            new Dictionary<string, string>(), CancellationToken.None);

        result.Should().BeSuccess();
        _primaryService.Setup(ps => ps.DeliverAsync(_caller.Object, "aforid", "aneventname",
            It.Is<Dictionary<string, string>>(dic => dic.HasNone()), It.IsAny<CancellationToken>()));
        _subscriptionsService.Verify(ss =>
            ss.IncrementSubscriptionUsageAsync(_caller.Object, "atenantid", "aneventname",
                It.IsAny<CancellationToken>()));
    }
}