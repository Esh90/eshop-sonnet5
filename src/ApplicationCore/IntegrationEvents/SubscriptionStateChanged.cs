using MediatR;

namespace Microsoft.eShopWeb.ApplicationCore.IntegrationEvents;

public class SubscriptionStateChanged : INotification
{
    public string CustomerReference { get; }
    public long SubscriptionId { get; }
    public string OldState { get; }
    public string NewState { get; }

    public SubscriptionStateChanged(string customerReference, long subscriptionId, string oldState, string newState)
    {
        CustomerReference = customerReference;
        SubscriptionId = subscriptionId;
        OldState = oldState;
        NewState = newState;
    }
}
