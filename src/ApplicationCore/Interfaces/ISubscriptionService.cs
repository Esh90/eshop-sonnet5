using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Models;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces;

/// <summary>
/// Use-case surface for the subscription feature (mirrors <see cref="IOrderService"/>).
/// </summary>
public interface ISubscriptionService
{
    Task<IReadOnlyList<BillingPlan>> ListPlansAsync();

    Task<BillingSubscription> SubscribeAsync(string customerReference, string email, string firstName, string lastName, string productHandle);

    Task<IReadOnlyList<BillingSubscription>> GetSubscriptionsForCustomerAsync(string customerReference);

    Task<BillingSubscription> GetSubscriptionAsync(long subscriptionId, string actingCustomerReference, bool isAdmin);

    Task<UsageRecordResult> RecordUsageAsync(long subscriptionId, int quantity, string? memo, string actingCustomerReference, bool isAdmin);

    Task RecordOrderPlacedUsageAsync(string customerReference);

    Task<PlanChangePreview> PreviewPlanChangeAsync(long subscriptionId, string targetProductHandle, bool atRenewal, string actingCustomerReference, bool isAdmin);

    Task<BillingSubscription> CommitPlanChangeAsync(long subscriptionId, string targetProductHandle, bool atRenewal, long expectedProratedAdjustmentInCents, string actingCustomerReference, bool isAdmin);

    Task<BillingSubscription> PauseAsync(long subscriptionId, string actingCustomerReference, bool isAdmin);

    Task<BillingSubscription> ResumeAsync(long subscriptionId, string actingCustomerReference, bool isAdmin);

    Task<BillingSubscription> CancelAsync(long subscriptionId, bool endOfPeriod, string? reason, string actingCustomerReference, bool isAdmin);

    Task<BillingSubscription> ReactivateAsync(long subscriptionId, string actingCustomerReference, bool isAdmin);
}
