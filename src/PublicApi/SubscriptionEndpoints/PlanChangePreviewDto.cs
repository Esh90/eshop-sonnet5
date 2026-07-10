using Microsoft.eShopWeb.ApplicationCore.Models;

namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class PlanChangePreviewDto
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

    public static PlanChangePreviewDto From(PlanChangePreview preview) => new()
    {
        SubscriptionId = preview.SubscriptionId,
        CurrentProductHandle = preview.CurrentProductHandle,
        TargetProductHandle = preview.TargetProductHandle,
        TargetProductName = preview.TargetProductName,
        TargetPrice = preview.TargetPrice,
        AtRenewal = preview.AtRenewal,
        ProratedAdjustmentInCents = preview.ProratedAdjustmentInCents,
        ChargeInCents = preview.ChargeInCents,
        PaymentDueInCents = preview.PaymentDueInCents,
        CreditAppliedInCents = preview.CreditAppliedInCents
    };
}
