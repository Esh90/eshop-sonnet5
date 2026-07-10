using System.Text.Json.Serialization;

namespace Microsoft.eShopWeb.MaxioBillingTestApi.Dtos;

// Request DTOs shaped after the real Maxio Advanced Billing wire contract for each operation
// (envelope wrapper + snake_case field names), NOT merely MaxioBillingClient's C# parameter list.
// Fields present in the Maxio API but unused by MaxioBillingClient are accepted and ignored.

public class CreateCustomerApiRequest
{
    [JsonPropertyName("customer")]
    public CustomerFields Customer { get; set; } = new();
}

public class CustomerFields
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;
}

public class CreateSubscriptionApiRequest
{
    [JsonPropertyName("subscription")]
    public CreateSubscriptionFields Subscription { get; set; } = new();
}

public class CreateSubscriptionFields
{
    [JsonPropertyName("product_handle")]
    public string ProductHandle { get; set; } = string.Empty;

    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }

    /// <summary>Accepted per the Maxio Create-Subscription contract but not used by this client - it always sends "remittance".</summary>
    [JsonPropertyName("payment_collection_method")]
    public string? PaymentCollectionMethod { get; set; }
}

public class MigrationApiRequest
{
    [JsonPropertyName("migration")]
    public MigrationFields Migration { get; set; } = new();
}

public class MigrationFields
{
    [JsonPropertyName("product_handle")]
    public string ProductHandle { get; set; } = string.Empty;
}

public class CancellationApiRequest
{
    [JsonPropertyName("subscription")]
    public CancellationFields? Subscription { get; set; }
}

public class CancellationFields
{
    [JsonPropertyName("cancellation_message")]
    public string? CancellationMessage { get; set; }

    /// <summary>Accepted per the Maxio Cancellation-Options contract but not used by this client.</summary>
    [JsonPropertyName("reason_code")]
    public string? ReasonCode { get; set; }
}

public class CreateUsageApiRequest
{
    [JsonPropertyName("usage")]
    public UsageFields Usage { get; set; } = new();
}

public class UsageFields
{
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("memo")]
    public string? Memo { get; set; }
}
