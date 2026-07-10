namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class RecordUsageRequest : BaseRequest
{
    public long SubscriptionId { get; set; }
    public int Quantity { get; set; }
    public string? Memo { get; set; }

    /// <summary>Set from the caller's JWT identity, not from client input - see RecordUsageEndpoint.AddRoute.</summary>
    public string ActingCustomerReference { get; set; } = string.Empty;

    /// <summary>Set from the caller's JWT role, not from client input - see RecordUsageEndpoint.AddRoute.</summary>
    public bool IsAdmin { get; set; }
}
