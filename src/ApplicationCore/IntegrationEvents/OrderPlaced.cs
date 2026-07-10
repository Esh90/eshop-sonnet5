using MediatR;

namespace Microsoft.eShopWeb.ApplicationCore.IntegrationEvents;

public class OrderPlaced : INotification
{
    public string BuyerId { get; }

    public OrderPlaced(string buyerId)
    {
        BuyerId = buyerId;
    }
}
