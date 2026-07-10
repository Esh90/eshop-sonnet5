using Microsoft.AspNetCore.Mvc;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.MaxioBillingTestApi.Dtos;

namespace Microsoft.eShopWeb.MaxioBillingTestApi.Controllers;

/// <summary>
/// Exposes every <see cref="IBillingClient"/> method (implemented by MaxioBillingClient) as an HTTP
/// route, one-to-one, per docs/maxio-billing-service-route-map.md. Actions only bind -> call the
/// client -> return its typed result untouched; no validation, reshaping, or per-case logic here.
/// </summary>
[ApiController]
[Route("api/maxio")]
public class MaxioBillingController : ControllerBase
{
    private readonly IBillingClient _billingClient;

    public MaxioBillingController(IBillingClient billingClient)
    {
        _billingClient = billingClient;
    }

    // GET /product_families/{product_family_id}/products.json
    [HttpGet("product-families/{productFamilyId}/products")]
    public async Task<IActionResult> ListPlans(string productFamilyId)
    {
        var result = await _billingClient.ListPlansAsync();
        return Ok(result);
    }

    // GET /customers/lookup.json -> POST /customers.json
    [HttpPost("customers")]
    public async Task<IActionResult> EnsureCustomer([FromBody] CreateCustomerApiRequest request)
    {
        var customerId = await _billingClient.EnsureCustomerAsync(
            request.Customer.Reference, request.Customer.Email, request.Customer.FirstName, request.Customer.LastName);
        return Ok(customerId);
    }

    // GET /customers/{customer_id}/subscriptions.json
    [HttpGet("customers/{customerId}/subscriptions")]
    public async Task<IActionResult> ListSubscriptionsForCustomer(string customerId)
    {
        var result = await _billingClient.ListSubscriptionsForCustomerAsync(customerId);
        return Ok(result);
    }

    // POST /subscriptions.json
    [HttpPost("subscriptions")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionApiRequest request)
    {
        var result = await _billingClient.CreateSubscriptionAsync(request.Subscription.CustomerId, request.Subscription.ProductHandle);
        return Ok(result);
    }

    // GET /subscriptions/{subscription_id}.json
    [HttpGet("subscriptions/{subscriptionId}")]
    public async Task<IActionResult> GetSubscription(long subscriptionId)
    {
        var result = await _billingClient.GetSubscriptionAsync(subscriptionId);
        return Ok(result);
    }

    // POST /subscriptions/{subscription_id}/migrations/preview.json
    [HttpPost("subscriptions/{subscriptionId}/migrations/preview")]
    public async Task<IActionResult> PreviewPlanChange(long subscriptionId, [FromBody] MigrationApiRequest request)
    {
        var result = await _billingClient.PreviewPlanChangeAsync(subscriptionId, request.Migration.ProductHandle);
        return Ok(result);
    }

    // POST /subscriptions/{subscription_id}/migrations.json (preserve_period=true) - immediate variant;
    // this repo's CommitPlanChangeAsync unifies immediate/at-renewal via a timing argument, so only the
    // immediate route is exposed (see route map row 7 vs the unexposed delayed row 30).
    [HttpPost("subscriptions/{subscriptionId}/migrations")]
    public async Task<IActionResult> CommitPlanChange(long subscriptionId, [FromBody] MigrationApiRequest request)
    {
        var result = await _billingClient.CommitPlanChangeAsync(subscriptionId, request.Migration.ProductHandle, atRenewal: false);
        return Ok(result);
    }

    // POST /subscriptions/{subscription_id}/hold.json
    [HttpPost("subscriptions/{subscriptionId}/hold")]
    public async Task<IActionResult> PauseSubscription(long subscriptionId)
    {
        var result = await _billingClient.PauseSubscriptionAsync(subscriptionId);
        return Ok(result);
    }

    // POST /subscriptions/{subscription_id}/resume.json
    [HttpPost("subscriptions/{subscriptionId}/resume")]
    public async Task<IActionResult> ResumeSubscription(long subscriptionId)
    {
        var result = await _billingClient.ResumeSubscriptionAsync(subscriptionId);
        return Ok(result);
    }

    // DELETE /subscriptions/{subscription_id}.json - immediate variant; this repo's CancelSubscriptionAsync
    // unifies immediate/delayed via a timing argument, so only the immediate route is exposed (see route
    // map row 10 vs the unexposed delayed_cancel row 31).
    [HttpDelete("subscriptions/{subscriptionId}")]
    public async Task<IActionResult> CancelSubscription(long subscriptionId, [FromBody] CancellationApiRequest? request)
    {
        var result = await _billingClient.CancelSubscriptionAsync(subscriptionId, endOfPeriod: false, request?.Subscription?.CancellationMessage);
        return Ok(result);
    }

    // PUT /subscriptions/{subscription_id}/reactivate.json
    [HttpPut("subscriptions/{subscriptionId}/reactivate")]
    public async Task<IActionResult> ReactivateSubscription(long subscriptionId)
    {
        var result = await _billingClient.ReactivateSubscriptionAsync(subscriptionId);
        return Ok(result);
    }

    // GET /subscriptions/{subscription_id}/components/{component_id}.json
    [HttpGet("subscriptions/{subscriptionId}/component-balance")]
    public async Task<IActionResult> GetComponentPeriodUsage(long subscriptionId)
    {
        var result = await _billingClient.GetComponentPeriodUsageAsync(subscriptionId);
        return Ok(result);
    }

    // GET /components/lookup.json?handle={handle}
    [HttpGet("metered-component/verify")]
    public async Task<IActionResult> GetMeteredComponent()
    {
        var result = await _billingClient.GetMeteredComponentAsync();
        return Ok(result);
    }

    // POST /subscriptions/{subscription_id}/components/{component_id}/usages.json - componentId is an
    // ignored path param: this client always records against the configured metered component.
    [HttpPost("subscriptions/{subscriptionId}/components/{componentId}/usages")]
    public async Task<IActionResult> RecordUsage(long subscriptionId, string componentId, [FromBody] CreateUsageApiRequest request)
    {
        var result = await _billingClient.RecordUsageAsync(subscriptionId, request.Usage.Quantity, request.Usage.Memo);
        return Ok(result);
    }
}
