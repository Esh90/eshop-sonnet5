namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class LifecycleRequest : BaseRequest
{
    public long SubscriptionId { get; set; }

    /// <summary>One of "pause", "resume", "cancel", "reactivate" (case-insensitive).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Only meaningful for the "cancel" action.</summary>
    public bool EndOfPeriod { get; set; }

    /// <summary>Only meaningful for the "cancel" action.</summary>
    public string? Reason { get; set; }

    /// <summary>Set from the caller's JWT identity, not from client input.</summary>
    public string ActingCustomerReference { get; set; } = string.Empty;

    /// <summary>Set from the caller's JWT role, not from client input.</summary>
    public bool IsAdmin { get; set; }
}
