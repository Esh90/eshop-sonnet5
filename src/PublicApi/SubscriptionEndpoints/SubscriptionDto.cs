using System;
using Microsoft.eShopWeb.ApplicationCore.Models;

namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class SubscriptionDto
{
    public long SubscriptionId { get; set; }
    public string ProductHandle { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTimeOffset? NextAssessmentAt { get; set; }
    public bool CancelAtEndOfPeriod { get; set; }
    public string? PendingProductHandle { get; set; }

    public static SubscriptionDto From(BillingSubscription subscription) => new()
    {
        SubscriptionId = subscription.SubscriptionId,
        ProductHandle = subscription.ProductHandle,
        ProductName = subscription.ProductName,
        Price = subscription.Price,
        State = subscription.State,
        NextAssessmentAt = subscription.NextAssessmentAt,
        CancelAtEndOfPeriod = subscription.CancelAtEndOfPeriod,
        PendingProductHandle = subscription.PendingProductHandle
    };
}
