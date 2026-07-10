namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class SubscribeRequest : BaseRequest
{
    public string ProductHandle { get; set; } = string.Empty;

    /// <summary>Set from the caller's JWT identity, not from client input - see SubscribeEndpoint.AddRoute.</summary>
    public string CustomerReference { get; set; } = string.Empty;
}
