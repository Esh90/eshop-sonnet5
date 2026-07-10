namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class CommitPlanChangeRequest : BaseRequest
{
    public long SubscriptionId { get; set; }
    public string TargetProductHandle { get; set; } = string.Empty;
    public bool AtRenewal { get; set; }

    /// <summary>
    /// The prorated adjustment shown by the most recent preview. The commit re-previews and rejects
    /// with a stale-preview error if this no longer matches (plan.md UC3: never apply a different
    /// amount than the one shown). Pass 0 when AtRenewal is true.
    /// </summary>
    public long ExpectedProratedAdjustmentInCents { get; set; }

    /// <summary>Set from the caller's JWT identity, not from client input.</summary>
    public string ActingCustomerReference { get; set; } = string.Empty;

    /// <summary>Set from the caller's JWT role, not from client input.</summary>
    public bool IsAdmin { get; set; }
}
