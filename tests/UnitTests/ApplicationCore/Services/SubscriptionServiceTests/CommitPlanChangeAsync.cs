using System.Threading.Tasks;
using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Models;
using Microsoft.eShopWeb.ApplicationCore.Services;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services.SubscriptionServiceTests;

public class CommitPlanChangeAsync
{
    private const string CustomerReference = "buyer@example.com";
    private const long SubscriptionId = 1;
    private const string CurrentHandle = "eshop-pro";
    private const string TargetHandle = "basic-plan";

    private readonly IBillingClient _mockBillingClient = Substitute.For<IBillingClient>();
    private readonly IPublisher _mockPublisher = Substitute.For<IPublisher>();
    private readonly IAppLogger<SubscriptionService> _mockLogger = Substitute.For<IAppLogger<SubscriptionService>>();

    private SubscriptionService BuildService() => new(_mockBillingClient, _mockPublisher, _mockLogger);

    [Fact]
    public async Task WhenPreviewedAmountHasChanged_RejectsAndDoesNotCommit()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = CustomerReference, ProductHandle = CurrentHandle, State = "active" });
        _mockBillingClient.PreviewPlanChangeAsync(SubscriptionId, TargetHandle)
            .Returns(new PlanChangePreview { SubscriptionId = SubscriptionId, ProratedAdjustmentInCents = 500 });

        var service = BuildService();

        await Assert.ThrowsAsync<StalePlanChangePreviewException>(() =>
            service.CommitPlanChangeAsync(SubscriptionId, TargetHandle, atRenewal: false, expectedProratedAdjustmentInCents: 100, CustomerReference, isAdmin: false));

        await _mockBillingClient.DidNotReceive().CommitPlanChangeAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task WhenTargetPlanMatchesCurrentPlan_RejectsAsNoOp()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = CustomerReference, ProductHandle = CurrentHandle, State = "active" });

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidSubscriptionStateException>(() =>
            service.CommitPlanChangeAsync(SubscriptionId, CurrentHandle, atRenewal: false, expectedProratedAdjustmentInCents: 0, CustomerReference, isAdmin: false));
    }

    [Fact]
    public async Task WhenPreviewedAmountMatches_CommitsAndPublishesPlanChanged()
    {
        _mockBillingClient.GetSubscriptionAsync(SubscriptionId)
            .Returns(new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = CustomerReference, ProductHandle = CurrentHandle, State = "active" });
        _mockBillingClient.PreviewPlanChangeAsync(SubscriptionId, TargetHandle)
            .Returns(new PlanChangePreview { SubscriptionId = SubscriptionId, ProratedAdjustmentInCents = 500 });
        var updated = new BillingSubscription { SubscriptionId = SubscriptionId, CustomerReference = CustomerReference, ProductHandle = TargetHandle, State = "active" };
        _mockBillingClient.CommitPlanChangeAsync(SubscriptionId, TargetHandle, false).Returns(updated);

        var service = BuildService();
        var result = await service.CommitPlanChangeAsync(SubscriptionId, TargetHandle, atRenewal: false, expectedProratedAdjustmentInCents: 500, CustomerReference, isAdmin: false);

        Assert.Equal(updated, result);
        await _mockPublisher.Received(1).Publish(Arg.Any<INotification>(), default);
    }
}
