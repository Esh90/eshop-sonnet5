namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class PreviewPlanChangeRequest : BaseRequest
{
    public long SubscriptionId { get; set; }
    public string TargetProductHandle { get; set; } = string.Empty;
    public bool AtRenewal { get; set; }

    /// <summary>Set from the caller's JWT identity, not from client input.</summary>
    public string ActingCustomerReference { get; set; } = string.Empty;

    /// <summary>Set from the caller's JWT role, not from client input.</summary>
    public bool IsAdmin { get; set; }
}
