namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class MySubscriptionsRequest : BaseRequest
{
    public string CustomerReference { get; }

    public MySubscriptionsRequest(string customerReference)
    {
        CustomerReference = customerReference;
    }
}
