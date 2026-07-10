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
/// Previews a plan change (UC3) - the prorated charge/credit for "now", or the new plan's price for
/// "at renewal" - without committing anything.
/// </summary>
public class PreviewPlanChangeEndpoint : IEndpoint<IResult, PreviewPlanChangeRequest, ISubscriptionService>
{
    public void AddRoute(IEndpointRouteBuilder app)
    {
        app.MapPost("api/subscriptions/{subscriptionId}/plan-change/preview",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async
            (long subscriptionId, PreviewPlanChangeRequest request, ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
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
            .Produces<PreviewPlanChangeResponse>()
            .WithTags("SubscriptionEndpoints");
    }

    public async Task<IResult> HandleAsync(PreviewPlanChangeRequest request, ISubscriptionService subscriptionService)
    {
        var response = new PreviewPlanChangeResponse(request.CorrelationId());

        var preview = await subscriptionService.PreviewPlanChangeAsync(
            request.SubscriptionId, request.TargetProductHandle, request.AtRenewal, request.ActingCustomerReference, request.IsAdmin);

        response.Preview = PlanChangePreviewDto.From(preview);
        return Results.Ok(response);
    }
}
