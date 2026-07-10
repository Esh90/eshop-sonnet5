using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Models;

namespace Microsoft.eShopWeb.Web.Pages.Subscriptions;

[Authorize]
public class MineModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAppLogger<MineModel> _logger;

    public MineModel(ISubscriptionService subscriptionService, IAppLogger<MineModel> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public IReadOnlyList<BillingSubscription> Subscriptions { get; set; } = new List<BillingSubscription>();
    public IReadOnlyList<BillingPlan> Plans { get; set; } = new List<BillingPlan>();
    public PlanChangePreview? Preview { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StatusMessage { get; set; }

    public async Task OnGet()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostPreviewPlanChange(long subscriptionId, string targetProductHandle, bool atRenewal)
    {
        var customerReference = RequireUserName();

        try
        {
            Preview = await _subscriptionService.PreviewPlanChangeAsync(subscriptionId, targetProductHandle, atRenewal, customerReference, isAdmin: false);
        }
        catch (Exception ex) when (ex is BillingProviderException or InvalidSubscriptionStateException
            or SubscriptionConfigurationException or SubscriptionNotFoundException)
        {
            _logger.LogWarning("Previewing plan change failed for subscription {SubscriptionId}: {Error}", subscriptionId, ex.Message);
            StatusMessage = ex.Message;
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCommitPlanChange(long subscriptionId, string targetProductHandle, bool atRenewal, long expectedProratedAdjustmentInCents)
    {
        var customerReference = RequireUserName();

        try
        {
            var subscription = await _subscriptionService.CommitPlanChangeAsync(
                subscriptionId, targetProductHandle, atRenewal, expectedProratedAdjustmentInCents, customerReference, isAdmin: false);
            StatusMessage = $"Plan changed to {subscription.ProductName}.";
        }
        catch (Exception ex) when (ex is BillingProviderException or InvalidSubscriptionStateException
            or SubscriptionConfigurationException or StalePlanChangePreviewException or SubscriptionNotFoundException)
        {
            _logger.LogWarning("Committing plan change failed for subscription {SubscriptionId}: {Error}", subscriptionId, ex.Message);
            StatusMessage = ex.Message;
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostPause(long subscriptionId)
    {
        var customerReference = RequireUserName();
        await RunLifecycleActionAsync(subscriptionId, () => _subscriptionService.PauseAsync(subscriptionId, customerReference, isAdmin: false), "paused");
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostResume(long subscriptionId)
    {
        var customerReference = RequireUserName();
        await RunLifecycleActionAsync(subscriptionId, () => _subscriptionService.ResumeAsync(subscriptionId, customerReference, isAdmin: false), "resumed");
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCancel(long subscriptionId, bool endOfPeriod, string? reason)
    {
        var customerReference = RequireUserName();
        await RunLifecycleActionAsync(subscriptionId, () => _subscriptionService.CancelAsync(subscriptionId, endOfPeriod, reason, customerReference, isAdmin: false), "canceled");
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostReactivate(long subscriptionId)
    {
        var customerReference = RequireUserName();
        await RunLifecycleActionAsync(subscriptionId, () => _subscriptionService.ReactivateAsync(subscriptionId, customerReference, isAdmin: false), "reactivated");
        await LoadAsync();
        return Page();
    }

    private async Task RunLifecycleActionAsync(long subscriptionId, Func<Task<BillingSubscription>> action, string pastTenseVerb)
    {
        try
        {
            var subscription = await action();
            StatusMessage = $"Subscription {pastTenseVerb} (state: {subscription.State}).";
        }
        catch (Exception ex) when (ex is BillingProviderException or InvalidSubscriptionStateException or SubscriptionNotFoundException)
        {
            _logger.LogWarning("Lifecycle action failed for subscription {SubscriptionId}: {Error}", subscriptionId, ex.Message);
            StatusMessage = ex.Message;
        }
    }

    private string RequireUserName()
    {
        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
        return User.Identity!.Name!;
    }

    public async Task<IActionResult> OnPostRecordUsage(long subscriptionId, int quantity, string? memo)
    {
        var customerReference = RequireUserName();

        try
        {
            var result = await _subscriptionService.RecordUsageAsync(subscriptionId, quantity, memo, customerReference, isAdmin: false);
            StatusMessage = result.TotalAvailable
                ? $"Recorded {result.QuantityRecorded} unit(s). Period-to-date total: {result.PeriodToDateTotal}."
                : $"Recorded {result.QuantityRecorded} unit(s). (Period-to-date total is temporarily unavailable.)";
        }
        catch (Exception ex) when (ex is BillingProviderException or InvalidSubscriptionStateException
            or SubscriptionConfigurationException or ArgumentException or SubscriptionNotFoundException)
        {
            _logger.LogWarning("Recording usage failed for subscription {SubscriptionId}: {Error}", subscriptionId, ex.Message);
            StatusMessage = ex.Message;
        }

        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        var customerReference = RequireUserName();

        try
        {
            Subscriptions = await _subscriptionService.GetSubscriptionsForCustomerAsync(customerReference);
            Plans = await _subscriptionService.ListPlansAsync();
        }
        catch (BillingProviderException ex)
        {
            _logger.LogWarning("Loading subscriptions failed for {User}: {Error}", customerReference, ex.Message);
            ErrorMessage = "We couldn't load your subscriptions right now. Please try again shortly.";
        }
    }
}
