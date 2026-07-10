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
/// Commits a previously-previewed plan change (UC3), either now (prorated) or at the next renewal.
/// </summary>
public class CommitPlanChangeEndpoint : IEndpoint<IResult, CommitPlanChangeRequest, ISubscriptionService>
{
    public void AddRoute(IEndpointRouteBuilder app)
    {
        app.MapPost("api/subscriptions/{subscriptionId}/plan-change",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async
            (long subscriptionId, CommitPlanChangeRequest request, ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
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
            .Produces<CommitPlanChangeResponse>()
            .WithTags("SubscriptionEndpoints");
    }

    public async Task<IResult> HandleAsync(CommitPlanChangeRequest request, ISubscriptionService subscriptionService)
    {
        var response = new CommitPlanChangeResponse(request.CorrelationId());

        var subscription = await subscriptionService.CommitPlanChangeAsync(
            request.SubscriptionId,
            request.TargetProductHandle,
            request.AtRenewal,
            request.ExpectedProratedAdjustmentInCents,
            request.ActingCustomerReference,
            request.IsAdmin);

        response.Subscription = SubscriptionDto.From(subscription);
        return Results.Ok(response);
    }
}
