using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Models;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces;

/// <summary>
/// Provider-agnostic abstraction over the recurring-billing provider. Implemented exactly once,
/// in Infrastructure, by the concrete Maxio client. Nothing else talks to the provider directly.
/// </summary>
public interface IBillingClient
{
    Task<IReadOnlyList<BillingPlan>> ListPlansAsync();
    Task<BillingComponent> GetMeteredComponentAsync();
    Task<long> EnsureCustomerAsync(string customerReference, string email, string firstName, string lastName);
    Task<BillingSubscription> CreateSubscriptionAsync(long customerId, string productHandle);
    Task<IReadOnlyList<BillingSubscription>> ListSubscriptionsForCustomerAsync(string customerReference);
    Task<BillingSubscription> GetSubscriptionAsync(long subscriptionId);
    Task<UsageRecordResult> RecordUsageAsync(long subscriptionId, int quantity, string? memo);
    Task<int?> GetComponentPeriodUsageAsync(long subscriptionId);
    Task<PlanChangePreview> PreviewPlanChangeAsync(long subscriptionId, string targetProductHandle);
    Task<BillingSubscription> CommitPlanChangeAsync(long subscriptionId, string targetProductHandle, bool atRenewal);
    Task<BillingSubscription> PauseSubscriptionAsync(long subscriptionId);
    Task<BillingSubscription> ResumeSubscriptionAsync(long subscriptionId);
    Task<BillingSubscription> CancelSubscriptionAsync(long subscriptionId, bool endOfPeriod, string? reason);
    Task<BillingSubscription> ReactivateSubscriptionAsync(long subscriptionId);
}
