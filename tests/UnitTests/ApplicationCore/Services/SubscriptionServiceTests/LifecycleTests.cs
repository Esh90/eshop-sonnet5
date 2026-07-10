using System.Threading.Tasks;
using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Models;
using Microsoft.eShopWeb.ApplicationCore.Services;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services.SubscriptionServiceTests;

public class LifecycleTests
{
    private const string Owner = "owner@example.com";
    private const string Stranger = "stranger@example.com";
    private const long SubscriptionId = 1;

    private readonly IBillingClient _mockBillingClient = Substitute.For<IBillingClient>();
    private readonly IPublisher _mockPublisher = Substitute.For<IPublisher>();
    private readonly IAppLogger<SubscriptionService> _mockLogger = Substitute.For<IAppLogger<SubscriptionService>>();

    private SubscriptionService BuildService() => new(_mockBillingClient, _mockPublisher, _mockLogger);

    [Fact]
    public async Task Pause_WhenSubscriptionIsNotActive_ThrowsWithoutCallingProvider()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = Owner, State = "on_hold" });

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidSubscriptionStateException>(() =>
            service.PauseAsync(SubscriptionId, Owner, isAdmin: false));

        await _mockBillingClient.DidNotReceive().PauseSubscriptionAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task Resume_WhenSubscriptionIsNotOnHold_ThrowsWithoutCallingProvider()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = Owner, State = "active" });

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidSubscriptionStateException>(() =>
            service.ResumeAsync(SubscriptionId, Owner, isAdmin: false));

        await _mockBillingClient.DidNotReceive().ResumeSubscriptionAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task Cancel_WhenAlreadyCanceled_ThrowsWithoutCallingProvider()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = Owner, State = "canceled" });

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidSubscriptionStateException>(() =>
            service.CancelAsync(SubscriptionId, false, null, Owner, isAdmin: false));

        await _mockBillingClient.DidNotReceive().CancelSubscriptionAsync(Arg.Any<long>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Reactivate_WhenSubscriptionIsActive_ThrowsIllegalTransition()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = Owner, State = "active" });

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidSubscriptionStateException>(() =>
            service.ReactivateAsync(SubscriptionId, Owner, isAdmin: false));
    }

    [Fact]
    public async Task GetSubscriptionAsync_WhenCallerIsNotOwnerAndNotAdmin_ThrowsSubscriptionNotFoundException()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = Owner, State = "active" });

        var service = BuildService();

        await Assert.ThrowsAsync<SubscriptionNotFoundException>(() =>
            service.GetSubscriptionAsync(SubscriptionId, Stranger, isAdmin: false));
    }

    [Fact]
    public async Task GetSubscriptionAsync_WhenCallerIsAdmin_BypassesOwnershipCheck()
    {
        var subscription = new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = Owner, State = "active" };
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId).Returns(subscription);

        var service = BuildService();
        var result = await service.GetSubscriptionAsync(SubscriptionId, Stranger, isAdmin: true);

        Assert.Equal(subscription, result);
    }
}
