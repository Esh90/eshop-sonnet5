using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Models;
using Microsoft.eShopWeb.ApplicationCore.Services;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services.SubscriptionServiceTests;

public class RecordUsageAsync
{
    private const string CustomerReference = "buyer@example.com";
    private const long SubscriptionId = 1;

    private readonly IBillingClient _mockBillingClient = Substitute.For<IBillingClient>();
    private readonly IPublisher _mockPublisher = Substitute.For<IPublisher>();
    private readonly IAppLogger<SubscriptionService> _mockLogger = Substitute.For<IAppLogger<SubscriptionService>>();

    private SubscriptionService BuildService() => new(_mockBillingClient, _mockPublisher, _mockLogger);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task WhenQuantityIsNotPositive_ThrowsWithoutCallingProvider(int quantity)
    {
        var service = BuildService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordUsageAsync(SubscriptionId, quantity, null, CustomerReference, isAdmin: false));

        await _mockBillingClient.DidNotReceive().GetSubscriptionAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task WhenSubscriptionIsNotActive_ThrowsInvalidSubscriptionStateException()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = CustomerReference, State = "on_hold" });

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidSubscriptionStateException>(() =>
            service.RecordUsageAsync(SubscriptionId, 1, null, CustomerReference, isAdmin: false));

        await _mockBillingClient.DidNotReceive().RecordUsageAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task WhenComponentIsNotMetered_ThrowsSubscriptionConfigurationException()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = CustomerReference, State = "active" });
        _mockBillingClient.GetMeteredComponentAsync()
            .Returns(new BillingComponent { ComponentId = 1, Handle = "api-call", Kind = "quantity_based_component" });

        var service = BuildService();

        await Assert.ThrowsAsync<SubscriptionConfigurationException>(() =>
            service.RecordUsageAsync(SubscriptionId, 1, null, CustomerReference, isAdmin: false));

        await _mockBillingClient.DidNotReceive().RecordUsageAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task WhenSuccessful_ReadsBackThePeriodToDateTotal()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = CustomerReference, State = "active" });
        _mockBillingClient.GetMeteredComponentAsync()
            .Returns(new BillingComponent { ComponentId = 1, Handle = "api-call", Kind = "metered_component" });
        _mockBillingClient.RecordUsageAsync(SubscriptionId, 3, "memo")
            .Returns(new UsageRecordResult { SubscriptionId = SubscriptionId, QuantityRecorded = 3 });
        _mockBillingClient.GetComponentPeriodUsageAsync(SubscriptionId).Returns(10);

        var service = BuildService();
        var result = await service.RecordUsageAsync(SubscriptionId, 3, "memo", CustomerReference, isAdmin: false);

        Assert.Equal(3, result.QuantityRecorded);
        Assert.Equal(10, result.PeriodToDateTotal);
        Assert.True(result.TotalAvailable);
    }

    [Fact]
    public async Task WhenReadingBackTheTotalFails_StillReturnsSuccessWithTotalMarkedUnavailable()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = CustomerReference, State = "active" });
        _mockBillingClient.GetMeteredComponentAsync()
            .Returns(new BillingComponent { ComponentId = 1, Handle = "api-call", Kind = "metered_component" });
        _mockBillingClient.RecordUsageAsync(SubscriptionId, 1, null)
            .Returns(new UsageRecordResult { SubscriptionId = SubscriptionId, QuantityRecorded = 1 });
        _mockBillingClient.GetComponentPeriodUsageAsync(SubscriptionId)
            .Returns(Task.FromException<int?>(new BillingProviderException("timeout")));

        var service = BuildService();
        var result = await service.RecordUsageAsync(SubscriptionId, 1, null, CustomerReference, isAdmin: false);

        Assert.Equal(1, result.QuantityRecorded);
        Assert.False(result.TotalAvailable);
    }
}
