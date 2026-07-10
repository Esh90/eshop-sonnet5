namespace Microsoft.eShopWeb.ApplicationCore.Models;

public class UsageRecordResult
{
    public long SubscriptionId { get; set; }
    public int QuantityRecorded { get; set; }
    public int? PeriodToDateTotal { get; set; }
    public bool TotalAvailable { get; set; }
}
