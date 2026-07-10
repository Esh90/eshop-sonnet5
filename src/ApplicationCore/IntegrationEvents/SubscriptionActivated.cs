using MediatR;

namespace Microsoft.eShopWeb.ApplicationCore.IntegrationEvents;

public class SubscriptionActivated : INotification
{
    public string CustomerReference { get; }
    public long SubscriptionId { get; }
    public string ProductHandle { get; }

    public SubscriptionActivated(string customerReference, long subscriptionId, string productHandle)
    {
        CustomerReference = customerReference;
        SubscriptionId = subscriptionId;
        ProductHandle = productHandle;
    }
}
