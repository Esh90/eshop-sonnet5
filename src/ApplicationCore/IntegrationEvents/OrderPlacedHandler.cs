using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;

namespace Microsoft.eShopWeb.ApplicationCore.IntegrationEvents;

/// <summary>
/// UC2's "automatic" usage hook: one order placed records one billable unit against the buyer's
/// active subscription, if they have one. Best-effort - a failure here must never affect checkout.
/// </summary>
public class OrderPlacedHandler : INotificationHandler<OrderPlaced>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAppLogger<OrderPlacedHandler> _logger;

    public OrderPlacedHandler(ISubscriptionService subscriptionService, IAppLogger<OrderPlacedHandler> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriptionService.RecordOrderPlacedUsageAsync(notification.BuyerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not record order-placed usage for buyer {BuyerId}: {Error}", notification.BuyerId, ex.Message);
        }
    }
}
