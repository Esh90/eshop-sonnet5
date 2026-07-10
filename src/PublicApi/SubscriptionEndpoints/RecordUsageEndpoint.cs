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
/// Records pay-as-you-go usage on a subscription (UC2). Customers may only record usage on their
/// own subscription; Administrators may record usage on any subscription.
/// </summary>
public class RecordUsageEndpoint : IEndpoint<IResult, RecordUsageRequest, ISubscriptionService>
{
    public void AddRoute(IEndpointRouteBuilder app)
    {
        app.MapPost("api/subscriptions/{subscriptionId}/usage",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async
            (long subscriptionId, RecordUsageRequest request, ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
            {
                var customerReference = user.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrEmpty(customerReference))
                {
                    return Results.Unauthorized();
                }

                request.SubscriptionId = subscriptionId;
                request.ActingCustomerReference = customerReference;
                request.IsAdmin = user.IsInRole(BlazorShared.Authorization.Constants.Roles.ADMINISTRATORS);
                return await HandleAsync(request, subscriptionService);
            })
            .Produces<RecordUsageResponse>()
            .WithTags("SubscriptionEndpoints");
    }

    public async Task<IResult> HandleAsync(RecordUsageRequest request, ISubscriptionService subscriptionService)
    {
        var response = new RecordUsageResponse(request.CorrelationId());

        var result = await subscriptionService.RecordUsageAsync(
            request.SubscriptionId, request.Quantity, request.Memo, request.ActingCustomerReference, request.IsAdmin);

        response.QuantityRecorded = result.QuantityRecorded;
        response.PeriodToDateTotal = result.PeriodToDateTotal;
        response.TotalAvailable = result.TotalAvailable;
        return Results.Ok(response);
    }
}
