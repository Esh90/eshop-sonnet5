using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.IntegrationEvents;
using Microsoft.eShopWeb.ApplicationCore.Models;
using Microsoft.eShopWeb.ApplicationCore.Services;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services.SubscriptionServiceTests;

public class SubscribeAsync
{
    private const string CustomerReference = "buyer@example.com";
    private const string ProductHandle = "eshop-pro";

    private readonly IBillingClient _mockBillingClient = Substitute.For<IBillingClient>();
    private readonly IPublisher _mockPublisher = Substitute.For<IPublisher>();
    private readonly IAppLogger<SubscriptionService> _mockLogger = Substitute.For<IAppLogger<SubscriptionService>>();

    private SubscriptionService BuildService() => new(_mockBillingClient, _mockPublisher, _mockLogger);

    [Fact]
    public async Task WhenCustomerHasNoLiveSubscription_CreatesSubscriptionAndPublishesActivated()
    {
        _mockBillingClient.ListSubscriptionsForCustomerAsync(CustomerReference)
            .Returns(new List<BillingSubscription>());
        _mockBillingClient.EnsureCustomerAsync(CustomerReference, CustomerReference, "buyer", "eShopOnWeb Customer")
            .Returns(42L);
        var created = new BillingSubscription { SubscriptionId = 1, CustomerReference = CustomerReference, ProductHandle = ProductHandle, State = "active" };
        _mockBillingClient.CreateSubscriptionAsync(42L, ProductHandle).Returns(created);

        var service = BuildService();
        var result = await service.SubscribeAsync(CustomerReference, CustomerReference, "buyer", "eShopOnWeb Customer", ProductHandle);

        Assert.Equal(created, result);
        await _mockBillingClient.Received(1).CreateSubscriptionAsync(42L, ProductHandle);
        await _mockPublisher.Received(1).Publish(Arg.Is<INotification>(n => IsMatchingActivatedNotification(n)), default);
    }

    private static bool IsMatchingActivatedNotification(INotification notification)
    {
        return notification is SubscriptionActivated activated
            && activated.CustomerReference == CustomerReference
            && activated.SubscriptionId == 1
            && activated.ProductHandle == ProductHandle;
    }

    [Fact]
    public async Task WhenCustomerAlreadyHasLiveSubscription_ReturnsExistingWithoutCreatingANewOne()
    {
        var existing = new BillingSubscription { SubscriptionId = 7, CustomerReference = CustomerReference, ProductHandle = ProductHandle, State = "active" };
        _mockBillingClient.ListSubscriptionsForCustomerAsync(CustomerReference)
            .Returns(new List<BillingSubscription> { existing });

        var service = BuildService();
        var result = await service.SubscribeAsync(CustomerReference, CustomerReference, "buyer", "eShopOnWeb Customer", ProductHandle);

        Assert.Equal(existing, result);
        await _mockBillingClient.DidNotReceive().EnsureCustomerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _mockBillingClient.DidNotReceive().CreateSubscriptionAsync(Arg.Any<long>(), Arg.Any<string>());
        await _mockPublisher.DidNotReceive().Publish(Arg.Any<INotification>(), default);
    }
}
