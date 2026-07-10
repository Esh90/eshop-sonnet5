using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Models;
using MinimalApi.Endpoint;

namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

/// <summary>
/// One management surface, four lifecycle actions (UC4): pause / resume / cancel / reactivate.
/// </summary>
public class LifecycleEndpoint : IEndpoint<IResult, LifecycleRequest, ISubscriptionService>
{
    public void AddRoute(IEndpointRouteBuilder app)
    {
        app.MapPost("api/subscriptions/{subscriptionId}/lifecycle",
            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async
            (long subscriptionId, LifecycleRequest request, ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
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
            .Produces<LifecycleResponse>()
            .WithTags("SubscriptionEndpoints");
    }

    public async Task<IResult> HandleAsync(LifecycleRequest request, ISubscriptionService subscriptionService)
    {
        var response = new LifecycleResponse(request.CorrelationId());

        BillingSubscription subscription = request.Action.ToLowerInvariant() switch
        {
            "pause" => await subscriptionService.PauseAsync(request.SubscriptionId, request.ActingCustomerReference, request.IsAdmin),
            "resume" => await subscriptionService.ResumeAsync(request.SubscriptionId, request.ActingCustomerReference, request.IsAdmin),
            "cancel" => await subscriptionService.CancelAsync(request.SubscriptionId, request.EndOfPeriod, request.Reason, request.ActingCustomerReference, request.IsAdmin),
            "reactivate" => await subscriptionService.ReactivateAsync(request.SubscriptionId, request.ActingCustomerReference, request.IsAdmin),
            _ => throw new ArgumentException($"Unknown lifecycle action '{request.Action}'. Expected pause, resume, cancel, or reactivate.", nameof(request.Action))
        };

        response.Subscription = SubscriptionDto.From(subscription);
        return Results.Ok(response);
    }
}
