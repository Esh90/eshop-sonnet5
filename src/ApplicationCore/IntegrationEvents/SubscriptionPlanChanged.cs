using MediatR;

namespace Microsoft.eShopWeb.ApplicationCore.IntegrationEvents;

public class SubscriptionPlanChanged : INotification
{
    public string CustomerReference { get; }
    public long SubscriptionId { get; }
    public string OldProductHandle { get; }
    public string NewProductHandle { get; }
    public bool AtRenewal { get; }

    public SubscriptionPlanChanged(string customerReference, long subscriptionId, string oldProductHandle, string newProductHandle, bool atRenewal)
    {
        CustomerReference = customerReference;
        SubscriptionId = subscriptionId;
        OldProductHandle = oldProductHandle;
        NewProductHandle = newProductHandle;
        AtRenewal = atRenewal;
    }
}
