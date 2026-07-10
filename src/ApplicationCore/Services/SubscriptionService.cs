using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using MediatR;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.IntegrationEvents;
using Microsoft.eShopWeb.ApplicationCore.Models;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class SubscriptionService : ISubscriptionService
{
    private static readonly HashSet<string> TerminalStates = new() { "canceled", "expired", "failed_to_create" };
    private static readonly HashSet<string> MigratableStates = new() { "active", "trialing" };
    private static readonly HashSet<string> ReactivatableStates = new() { "canceled", "unpaid", "trial_ended" };

    private readonly IBillingClient _billingClient;
    private readonly IPublisher _publisher;
    private readonly IAppLogger<SubscriptionService> _logger;

    public SubscriptionService(IBillingClient billingClient, IPublisher publisher, IAppLogger<SubscriptionService> logger)
    {
        _billingClient = billingClient;
        _publisher = publisher;
        _logger = logger;
    }

    public Task<IReadOnlyList<BillingPlan>> ListPlansAsync() => _billingClient.ListPlansAsync();

    public async Task<BillingSubscription> SubscribeAsync(string customerReference, string email, string firstName, string lastName, string productHandle)
    {
        Guard.Against.NullOrEmpty(customerReference, nameof(customerReference));
        Guard.Against.NullOrEmpty(productHandle, nameof(productHandle));

        var existingSubscriptions = await _billingClient.ListSubscriptionsForCustomerAsync(customerReference);
        var existingLive = existingSubscriptions.FirstOrDefault(s => !TerminalStates.Contains(s.State));
        if (existingLive != null)
        {
            return existingLive;
        }

        var customerId = await _billingClient.EnsureCustomerAsync(customerReference, email, firstName, lastName);
        var subscription = await _billingClient.CreateSubscriptionAsync(customerId, productHandle);

        await PublishBestEffortAsync(new SubscriptionActivated(customerReference, subscription.SubscriptionId, subscription.ProductHandle));

        return subscription;
    }

    public Task<IReadOnlyList<BillingSubscription>> GetSubscriptionsForCustomerAsync(string customerReference)
    {
        Guard.Against.NullOrEmpty(customerReference, nameof(customerReference));
        return _billingClient.ListSubscriptionsForCustomerAsync(customerReference);
    }

    public async Task<BillingSubscription> GetSubscriptionAsync(long subscriptionId, string actingCustomerReference, bool isAdmin)
    {
        var subscription = await _billingClient.GetSubscriptionAsync(subscriptionId);
        EnsureOwnership(subscription, actingCustomerReference, isAdmin);
        return subscription;
    }

    public async Task<UsageRecordResult> RecordUsageAsync(long subscriptionId, int quantity, string? memo, string actingCustomerReference, bool isAdmin)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be a positive integer.", nameof(quantity));
        }

        var subscription = await GetSubscriptionAsync(subscriptionId, actingCustomerReference, isAdmin);
        if (subscription.State != "active")
        {
            throw new InvalidSubscriptionStateException($"Subscription {subscriptionId} is not active (state: '{subscription.State}'); usage cannot be recorded.");
        }

        await EnsureMeteredComponentConfiguredAsync();

        var result = await _billingClient.RecordUsageAsync(subscriptionId, quantity, memo);

        try
        {
            result.PeriodToDateTotal = await _billingClient.GetComponentPeriodUsageAsync(subscriptionId);
            result.TotalAvailable = true;
        }
        catch (BillingProviderException ex)
        {
            _logger.LogWarning("Usage for subscription {SubscriptionId} was recorded, but reading back the period-to-date total failed: {Error}", subscriptionId, ex.Message);
            result.TotalAvailable = false;
        }

        return result;
    }

    public async Task RecordOrderPlacedUsageAsync(string customerReference)
    {
        Guard.Against.NullOrEmpty(customerReference, nameof(customerReference));

        var subscriptions = await _billingClient.ListSubscriptionsForCustomerAsync(customerReference);
        var active = subscriptions.FirstOrDefault(s => s.State == "active");
        if (active == null)
        {
            return;
        }

        await RecordUsageAsync(active.SubscriptionId, 1, "Order placed", customerReference, isAdmin: true);
    }

    public async Task<PlanChangePreview> PreviewPlanChangeAsync(long subscriptionId, string targetProductHandle, bool atRenewal, string actingCustomerReference, bool isAdmin)
    {
        Guard.Against.NullOrEmpty(targetProductHandle, nameof(targetProductHandle));

        var subscription = await GetSubscriptionAsync(subscriptionId, actingCustomerReference, isAdmin);
        EnsureCanChangePlan(subscription, targetProductHandle);

        if (atRenewal)
        {
            var targetPlan = await ResolvePlanAsync(targetProductHandle);
            return new PlanChangePreview
            {
                SubscriptionId = subscriptionId,
                CurrentProductHandle = subscription.ProductHandle,
                TargetProductHandle = targetPlan.Handle,
                TargetProductName = targetPlan.Name,
                TargetPrice = targetPlan.Price,
                AtRenewal = true
            };
        }

        var preview = await _billingClient.PreviewPlanChangeAsync(subscriptionId, targetProductHandle);
        preview.CurrentProductHandle = subscription.ProductHandle;
        preview.AtRenewal = false;
        return preview;
    }

    public async Task<BillingSubscription> CommitPlanChangeAsync(long subscriptionId, string targetProductHandle, bool atRenewal, long expectedProratedAdjustmentInCents, string actingCustomerReference, bool isAdmin)
    {
        Guard.Against.NullOrEmpty(targetProductHandle, nameof(targetProductHandle));

        var subscription = await GetSubscriptionAsync(subscriptionId, actingCustomerReference, isAdmin);
        EnsureCanChangePlan(subscription, targetProductHandle);

        if (!atRenewal)
        {
            var freshPreview = await _billingClient.PreviewPlanChangeAsync(subscriptionId, targetProductHandle);
            if (freshPreview.ProratedAdjustmentInCents != expectedProratedAdjustmentInCents)
            {
                throw new StalePlanChangePreviewException(
                    "The previewed proration amount has changed since it was shown. Request a fresh preview before confirming the plan change.");
            }
        }

        var oldProductHandle = subscription.ProductHandle;
        var updated = await _billingClient.CommitPlanChangeAsync(subscriptionId, targetProductHandle, atRenewal);

        await PublishBestEffortAsync(new SubscriptionPlanChanged(subscription.CustomerReference, subscriptionId, oldProductHandle, targetProductHandle, atRenewal));

        return updated;
    }

    public async Task<BillingSubscription> PauseAsync(long subscriptionId, string actingCustomerReference, bool isAdmin)
    {
        var subscription = await GetSubscriptionAsync(subscriptionId, actingCustomerReference, isAdmin);
        if (subscription.State != "active")
        {
            throw new InvalidSubscriptionStateException($"Cannot pause subscription {subscriptionId}: only an active subscription can be paused (current state: '{subscription.State}').");
        }

        var updated = await _billingClient.PauseSubscriptionAsync(subscriptionId);
        await PublishStateChangedBestEffortAsync(subscription, updated);
        return updated;
    }

    public async Task<BillingSubscription> ResumeAsync(long subscriptionId, string actingCustomerReference, bool isAdmin)
    {
        var subscription = await GetSubscriptionAsync(subscriptionId, actingCustomerReference, isAdmin);
        if (subscription.State != "on_hold")
        {
            throw new InvalidSubscriptionStateException($"Cannot resume subscription {subscriptionId}: it is not paused (current state: '{subscription.State}').");
        }

        var updated = await _billingClient.ResumeSubscriptionAsync(subscriptionId);
        await PublishStateChangedBestEffortAsync(subscription, updated);
        return updated;
    }

    public async Task<BillingSubscription> CancelAsync(long subscriptionId, bool endOfPeriod, string? reason, string actingCustomerReference, bool isAdmin)
    {
        var subscription = await GetSubscriptionAsync(subscriptionId, actingCustomerReference, isAdmin);
        if (subscription.State == "canceled")
        {
            throw new InvalidSubscriptionStateException($"Subscription {subscriptionId} is already canceled.");
        }

        var updated = await _billingClient.CancelSubscriptionAsync(subscriptionId, endOfPeriod, reason);
        await PublishStateChangedBestEffortAsync(subscription, updated);
        return updated;
    }

    public async Task<BillingSubscription> ReactivateAsync(long subscriptionId, string actingCustomerReference, bool isAdmin)
    {
        var subscription = await GetSubscriptionAsync(subscriptionId, actingCustomerReference, isAdmin);
        if (!ReactivatableStates.Contains(subscription.State))
        {
            throw new InvalidSubscriptionStateException($"Cannot reactivate subscription {subscriptionId} from its current state ('{subscription.State}').");
        }

        var updated = await _billingClient.ReactivateSubscriptionAsync(subscriptionId);
        await PublishStateChangedBestEffortAsync(subscription, updated);
        return updated;
    }

    private async Task<BillingPlan> ResolvePlanAsync(string productHandle)
    {
        var plans = await _billingClient.ListPlansAsync();
        var plan = plans.FirstOrDefault(p => p.Handle == productHandle);
        if (plan == null)
        {
            throw new SubscriptionConfigurationException($"Configured product handle '{productHandle}' does not resolve. Re-check the seed (UC0) and configuration.");
        }

        return plan;
    }

    private static void EnsureCanChangePlan(BillingSubscription subscription, string targetProductHandle)
    {
        if (!MigratableStates.Contains(subscription.State))
        {
            throw new InvalidSubscriptionStateException(
                $"Subscription {subscription.SubscriptionId} cannot change plans from its current state ('{subscription.State}'); reactivate it first.");
        }

        if (subscription.ProductHandle == targetProductHandle)
        {
            throw new InvalidSubscriptionStateException($"Subscription {subscription.SubscriptionId} is already on plan '{targetProductHandle}'.");
        }
    }

    private async Task EnsureMeteredComponentConfiguredAsync()
    {
        var component = await _billingClient.GetMeteredComponentAsync();
        if (!component.IsMetered)
        {
            throw new SubscriptionConfigurationException(
                $"Configured usage component '{component.Handle}' is not of metered kind (kind: '{component.Kind}'). Fix the seed (UC0) before recording usage.");
        }
    }

    private static void EnsureOwnership(BillingSubscription subscription, string actingCustomerReference, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        Guard.Against.NullOrEmpty(actingCustomerReference, nameof(actingCustomerReference));
        if (!string.Equals(subscription.CustomerReference, actingCustomerReference, StringComparison.OrdinalIgnoreCase))
        {
            throw new SubscriptionNotFoundException(subscription.SubscriptionId);
        }
    }

    private async Task PublishStateChangedBestEffortAsync(BillingSubscription before, BillingSubscription after)
    {
        await PublishBestEffortAsync(new SubscriptionStateChanged(before.CustomerReference, before.SubscriptionId, before.State, after.State));
    }

    private async Task PublishBestEffortAsync(INotification notification)
    {
        try
        {
            await _publisher.Publish(notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to publish {NotificationType}: {Error}", notification.GetType().Name, ex.Message);
        }
    }
}
