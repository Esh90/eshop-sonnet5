using System;

namespace Microsoft.eShopWeb.PublicApi.SubscriptionEndpoints;

public class RecordUsageResponse : BaseResponse
{
    public RecordUsageResponse(Guid correlationId) : base(correlationId)
    {
    }

    public RecordUsageResponse()
    {
    }

    public int QuantityRecorded { get; set; }
    public int? PeriodToDateTotal { get; set; }
    public bool TotalAvailable { get; set; }
}
