namespace Microsoft.eShopWeb.ApplicationCore.Models;

public class BillingComponent
{
    public long ComponentId { get; set; }
    public string Handle { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public bool IsMetered => Kind == "metered_component";
}
