using System;

namespace Microsoft.eShopWeb.ApplicationCore.Models;

public class BillingSubscription
{
    public long SubscriptionId { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public string ProductHandle { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTimeOffset? NextAssessmentAt { get; set; }
    public bool CancelAtEndOfPeriod { get; set; }

    /// <summary>Set when a delayed (at-renewal) plan change is pending; the plan it will move to.</summary>
    public string? PendingProductHandle { get; set; }
}
