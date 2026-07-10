using System.Collections.Generic;
using System.Linq;
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
public class PlansModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAppLogger<PlansModel> _logger;

    public PlansModel(ISubscriptionService subscriptionService, IAppLogger<PlansModel> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public IReadOnlyList<BillingPlan> Plans { get; set; } = new List<BillingPlan>();
    public BillingSubscription? CurrentSubscription { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGet()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSubscribe(string productHandle)
    {
        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
        var email = User.Identity!.Name!;

        try
        {
            await _subscriptionService.SubscribeAsync(email, email, DeriveFirstName(email), "eShopOnWeb Customer", productHandle);
        }
        catch (BillingProviderException ex)
        {
            _logger.LogWarning("Subscribe failed for {Email}: {Error}", email, ex.Message);
            await LoadAsync();
            ErrorMessage = "We couldn't complete your subscription right now. Please try again shortly.";
            return Page();
        }
        catch (SubscriptionConfigurationException ex)
        {
            _logger.LogWarning("Subscribe configuration error: {Error}", ex.Message);
            await LoadAsync();
            ErrorMessage = "Subscriptions are temporarily unavailable due to a configuration issue.";
            return Page();
        }

        return RedirectToPage("Mine");
    }

    private async Task LoadAsync()
    {
        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));

        try
        {
            Plans = await _subscriptionService.ListPlansAsync();
        }
        catch (BillingProviderException ex)
        {
            _logger.LogWarning("Listing plans failed: {Error}", ex.Message);
            Plans = new List<BillingPlan>();
            ErrorMessage = "Plans are temporarily unavailable. Please try again shortly.";
            return;
        }

        var subscriptions = await _subscriptionService.GetSubscriptionsForCustomerAsync(User.Identity!.Name!);
        CurrentSubscription = subscriptions.FirstOrDefault(s => s.State is not ("canceled" or "expired" or "failed_to_create"));
    }

    private static string DeriveFirstName(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }
}
