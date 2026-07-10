using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using MinimalApi.Endpoint;

namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

/// <summary>
/// Returns the authenticated customer's subscriptions.
/// </summary>
public class MySubscriptionsEndpoint : IEndpoint<IResult, MySubscriptionsRequest, ISubscriptionService>
{
    public void AddRoute(IEndpointRouteBuilder app)
    {
        app.MapGet("api/subscriptions/mine",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async
            (ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
            {
                var customerReference = user.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrEmpty(customerReference))
                {
                    return Results.Unauthorized();
                }

                return await HandleAsync(new MySubscriptionsRequest(customerReference), subscriptionService);
            })
            .Produces<MySubscriptionsResponse>()
            .WithTags("SubscriptionEndpoints");
    }

    public async Task<IResult> HandleAsync(MySubscriptionsRequest request, ISubscriptionService subscriptionService)
    {
        var response = new MySubscriptionsResponse(request.CorrelationId());
        var subscriptions = await subscriptionService.GetSubscriptionsForCustomerAsync(request.CustomerReference);
        response.Subscriptions.AddRange(subscriptions.Select(SubscriptionDto.From));
        return Results.Ok(response);
    }
}
