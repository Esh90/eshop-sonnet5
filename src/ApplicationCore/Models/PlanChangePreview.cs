namespace Microsoft.eShopWeb.ApplicationCore.Models;

public class PlanChangePreview
{
    public long SubscriptionId { get; set; }
    public string CurrentProductHandle { get; set; } = string.Empty;
    public string TargetProductHandle { get; set; } = string.Empty;
    public string TargetProductName { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public bool AtRenewal { get; set; }
    public long ProratedAdjustmentInCents { get; set; }
    public long ChargeInCents { get; set; }
    public long PaymentDueInCents { get; set; }
    public long CreditAppliedInCents { get; set; }
}
